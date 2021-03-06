/*
 *  Copyright � 1994-2002, 2015-2016 Thomas R. Lawrence
 * 
 *  GNU General Public License
 * 
 *  This file is part of Out Of Phase (Music Synthesis Software)
 * 
 *  Out Of Phase is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program. If not, see <http://www.gnu.org/licenses/>.
 * 
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        public const double DECIBELEXPANDER = 32768d;
        public const double DECIBELTHRESHHOLD = 1d / DECIBELEXPANDER;

        [StructLayout(LayoutKind.Auto)]
        public struct OneEnvPhaseRec
        {
            /* what amplitude are we trying to attain */
            public double FinalAmplitude;
            /* how many envelope cycles does this phase last */
            public int Duration;
            /* what does the curve look like */
            public EnvTransTypes TransitionType;
            /* way of deriving the target */
            public EnvTargetTypes TargetType;
        }

        // Assign delegates allocates a new heap object each time. Since there is no state, save these
        // references statically to eliminate per-iteration allocations.
        private readonly static EnvelopeUpdateMethod _EnvUpdateLinearAbsolute = EnvUpdateLinearAbsolute;
        private readonly static EnvelopeUpdateMethod _EnvUpdateLinearDecibels = EnvUpdateLinearDecibels;
        private readonly static EnvelopeUpdateMethod _EnvUpdateSustain = EnvUpdateSustain;

        public delegate double EnvelopeUpdateMethod(EvalEnvelopeRec State);

        public class EvalEnvelopeRec
        {
            /* number of envelope phases. 0 phases means the envelope produces constant value */
            public int NumPhases;
            /* current phase.  when it becomes equal to NumPhases, we are done. */
            public int CurrentPhase;

            /* phase at which first sustain occurs. (released by key-up) */
            /* if the value of this is N, then sustain will occur after phase N has */
            /* completed.  if it is 0, then sustain will occur after phase 0 has completed. */
            /* if it is -1, then sustain will not occur.  sustain may not be NumPhases since */
            /* that would be the end of envelope and would be the same as final value hold. */
            public int SustainPhase1;
            public int OriginalSustainPhase1;
            public SustainTypes SustainPhase1Type;
            /* phase at which second sustain occurs. */
            public int SustainPhase2;
            public int OriginalSustainPhase2;
            public SustainTypes SustainPhase2Type;
            /* phase at which note-end sustain occurs */
            public int SustainPhase3;
            public int OriginalSustainPhase3;
            public SustainTypes SustainPhase3Type;

            /* what is the origin phase */
            public int Origin;

            /* this is the envelope transition generator */
            public LinearTransRec LinearTransition;
            /* this is the countdown for this transition. */
            public int LinearTransitionCounter;
            public int LinearTransitionTotalDuration;
            /* hold value for when we are sustaining */
            public double LastOutputtedValue;

            /* number of cycles of the envelope that occur before the origin */
            public int PreOriginTime;

            /* this function performs one update cycle */
            public EnvelopeUpdateMethod EnvelopeUpdate;

            /* flag indicating that envelope has finished evaluating the last phase */
            public bool EnvelopeHasFinished;

            /* flag indicating whether global pitch scaling is enabled */
            public bool PerformGlobalPitchScaling;

            /* function used to ask container for parameters */
            public ParamGetterMethod ParamGetter;
            public object ParamGetterContext;

            /* saved frozen-note differential accent values, used to incorporate note's */
            /* accents while also tracking the "live" accent values */
            public AccentRec FrozenNoteAccentDifferential;

            /* we remember the template that was used to construct us */
            public EnvelopeRec Template;

            /* phase vector follows last field of struct */
            public OneEnvPhaseRec[] PhaseVector;

#if DEBUG
            public EvalEnvelopeRec()
            {
                if (!EnableFreeLists)
                {
                    GC.SuppressFinalize(this);
                }
            }

            ~EvalEnvelopeRec()
            {
                Debug.Assert(false, GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
            }
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif
        }

        /* create a new envelope state record.  Accent factors have no effect with a value */
        /* of 1, attenuate at smaller values, and amplify at larger values. */
        public static EvalEnvelopeRec NewEnvelopeStateRecord(
            EnvelopeRec Template,
            ref AccentRec Accents,
            double FrequencyHertz,
            double Loudness,
            double HurryUp,
            out int PreOriginTime,
            ParamGetterMethod ParamGetter,
            object ParamGetterContext,
            SynthParamRec SynthParams)
        {
            EvalEnvelopeRec State = New(ref SynthParams.freelists.envelopeStateFreeList);

            // must assign all fields: State, State.PhaseVector

            State.NumPhases = Template.NumPhases;
            State.PhaseVector = New(ref SynthParams.freelists.envelopeOnePhaseFreeList, Template.NumPhases); // cleared

#if DEBUG
            if ((Template.NumPhases != 0) && Template.ConstantShortcut)
            {
                // shouldn't specify shortcut and phase list at the same time
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            State.Template = Template;

            State.CurrentPhase = -1;
            State.SustainPhase1 = Template.SustainPhase1;
            State.OriginalSustainPhase1 = Template.SustainPhase1;
            State.SustainPhase1Type = Template.SustainPhase1Type;
            State.SustainPhase2 = Template.SustainPhase2;
            State.OriginalSustainPhase2 = Template.SustainPhase2;
            State.SustainPhase2Type = Template.SustainPhase2Type;
            State.SustainPhase3 = Template.SustainPhase3;
            State.OriginalSustainPhase3 = Template.SustainPhase3;
            State.SustainPhase3Type = Template.SustainPhase3Type;
            State.Origin = Template.Origin;
            State.ParamGetter = ParamGetter;
            State.ParamGetterContext = ParamGetterContext;
            State.LastOutputtedValue = 0;
            State.PreOriginTime = 0;
            State.FrozenNoteAccentDifferential = new AccentRec();

            /* build initial delay transition */
            ResetLinearTransition(
                ref State.LinearTransition,
                0,
                0,
                1);
            State.LinearTransitionCounter = 0;
            State.LinearTransitionTotalDuration = 0;
            State.EnvelopeUpdate = _EnvUpdateLinearAbsolute;
            State.EnvelopeHasFinished = false;

            State.PerformGlobalPitchScaling = (0 != Template.GlobalPitchRateRolloff);

            // A note about accents to follow:
            // - The "Accents" parameter to this function comes from the FrozenNote - so they are the accent values in effect
            //   at the time the note was "frozen" (i.e. the accent defaults added to the individual note's adjustments).
            // - The "LiveAccents" parameter is the current accent defaults at time t (which should be equal to "Accents", at the
            //   the a note is initiated, but not necessarily before or after), since this event is occurring at time t.
            // - The "LiveTrackAccents" parameter is the current track effect accent values.
            // There is some redundancy here, but it is not being changed in case this is wrong and there are legacy
            // interactions that need to be preserved.
            AccentRec LiveAccents;
            AccentRec LiveTrackAccents;
#if DEBUG
            bool liveAccentInit = false;
#endif
            if (EnvelopeContainsFormula(Template)) // an optimization
            {
                /* get current live accents that do not contain any info from the note */
                ParamGetter(
                    ParamGetterContext,
                    out LiveAccents,
                    out LiveTrackAccents);
#if DEBUG
                liveAccentInit = true;
#endif
            }
            else
            {
                LiveAccents = new AccentRec();
                LiveTrackAccents = new AccentRec();
            }

            /* build list of nodes */
            if (Template.ConstantShortcut)
            {
                State.NumPhases = 0; /* make sure */
                State.CurrentPhase = 0;
                State.LastOutputtedValue = Template.ConstantShortcutValue;
                State.EnvelopeUpdate = _EnvUpdateSustain;
                State.EnvelopeHasFinished = false;
            }
            else
            {
                State.LastOutputtedValue = 0;

                double accumulatedError = 0;
                OneEnvPhaseRec[] PhaseVector = State.PhaseVector;
                if (unchecked((uint)State.NumPhases > (uint)PhaseVector.Length))
                {
                    throw new IndexOutOfRangeException();
                }
                EnvStepRec[] TemplatePhaseArray = Template.PhaseArray;
                if (unchecked((uint)State.NumPhases > (uint)TemplatePhaseArray.Length))
                {
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < State.NumPhases; i++)
                {
                    Debug.Assert(PhaseVector[i].Equals(new OneEnvPhaseRec())); // verify cleared

                    PhaseVector[i].TransitionType = TemplatePhaseArray[i].TransitionType;
                    PhaseVector[i].TargetType = TemplatePhaseArray[i].TargetType;
                    /* calculate the total duration.  the effect of accents is this: */
                    /*  - the accent is the base-2 log of a multiplier for the rate.  a value of 0 */
                    /*    does not change the rate.  -1 halves the rate, and 1 doubles the rate. */
                    /*  - the accent scaling factor is the base-2 log for scaling the accent. */
                    /*    a value of 0 eliminates the effect of the accent, a value of 1 does not */
                    /*    scale the accent. */
                    /*  - pitch has two factors:  normalization point and rolloff.  rolloff */
                    /*    determines how much the signal will decrease with each octave.  0 */
                    /*    removes effect, 1 halfs signal with each octave.  normalization point */
                    /*    determines what pitch will be the invariant point. */
                    double Temp;
                    if (TemplatePhaseArray[i].DurationFunction == null)
                    {
                        Temp = TemplatePhaseArray[i].Duration;
                    }
                    else
                    {
#if DEBUG
                        Debug.Assert(liveAccentInit);
#endif
                        SynthErrorCodes error = EnvelopeInitParamEval(
                            TemplatePhaseArray[i].DurationFunction,
                            ref Accents,
                            ref LiveTrackAccents,
                            SynthParams,
                            out Temp);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            // TODO:
                        }
                    }
                    double preciseDuration = SynthParams.dEnvelopeRate
                        * HurryUp
                        * Temp
                        * Math.Pow(2, -(AccentProduct(ref Accents, ref TemplatePhaseArray[i].AccentRate)
                            + (Math.Log(FrequencyHertz
                                / TemplatePhaseArray[i].FrequencyRateNormalization)
                            * Constants.INVLOG2) * TemplatePhaseArray[i].FrequencyRateRolloff))
                        + accumulatedError;
                    PhaseVector[i].Duration = (int)Math.Round(preciseDuration);
                    accumulatedError = preciseDuration - PhaseVector[i].Duration;
                    /* the final amplitude scaling values are computed similarly to the rate */
                    /* scaling values. */
                    if (TemplatePhaseArray[i].EndPointFunction == null)
                    {
                        Temp = TemplatePhaseArray[i].EndPoint;
                    }
                    else
                    {
#if DEBUG
                        Debug.Assert(liveAccentInit);
#endif
                        SynthErrorCodes error = EnvelopeInitParamEval(
                            TemplatePhaseArray[i].EndPointFunction,
                            ref Accents,
                            ref LiveTrackAccents,
                            SynthParams,
                            out Temp);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            // TODO:
                        }
                    }
                    PhaseVector[i].FinalAmplitude = Temp
                        * Template.OverallScalingFactor * Loudness
                        * Math.Pow(2, -(AccentProduct(ref Accents, ref TemplatePhaseArray[i].AccentAmp)
                            + (Math.Log(FrequencyHertz
                                / TemplatePhaseArray[i].FrequencyAmpNormalization)
                            * Constants.INVLOG2) * TemplatePhaseArray[i].FrequencyAmpRolloff));

                    /* adjust initial countdown */
                    if (i < Template.Origin)
                    {
                        /* this occurs before the origin, so add it in */
                        State.PreOriginTime += PhaseVector[i].Duration;
                    }
                }
            }
            PreOriginTime = State.PreOriginTime;

            /* compute accent differentials */
            if (State.Template.Formula != null)
            {
#if DEBUG
                Debug.Assert(liveAccentInit);
#endif

                /* subtract composite accents ("Accents") from current live accents */
                AccentAdd(
                    -1,
                    ref Accents,
                    ref LiveAccents,
                    ref State.FrozenNoteAccentDifferential);
            }

            return State;
        }

        public static void FreeEnvelopeStateRecord(
            ref EvalEnvelopeRec State,
            SynthParamRec SynthParams)
        {
            Free(ref SynthParams.freelists.envelopeOnePhaseFreeList, ref State.PhaseVector);
            Free(ref SynthParams.freelists.envelopeStateFreeList, ref State);
        }

        /* when all envelopes have been computed, then the total (i.e. largest) pre-origin */
        /* time will be known and we can tell all envelopes how long they must wait */
        /* before starting */
        public static void EnvelopeStateFixUpInitialDelay(
            EvalEnvelopeRec State,
            int MaximumPreOriginTime)
        {
            State.LinearTransitionCounter = MaximumPreOriginTime - State.PreOriginTime;
            State.LinearTransitionTotalDuration = MaximumPreOriginTime - State.PreOriginTime;
        }

        /* perform a single cycle of the envelope and return the amplitude for it's */
        /* point.  should be called at key-down to obtain initial amplitude. */
        public static double EnvelopeUpdate(
            EvalEnvelopeRec State,
            double OscillatorPitch,
            SynthParamRec SynthParams,
            ref SynthErrorCodes ErrorRef)
        {
            if (ErrorRef != SynthErrorCodes.eSynthDone)
            {
                return 0;
            }

            /* evaluate the segment generator */
            double Temp = State.EnvelopeUpdate(State);

            /* apply optional pitch scaling */
            if (State.PerformGlobalPitchScaling)
            {
                Temp = Temp * Math.Pow(2, -(Math.Log(OscillatorPitch
                    / State.Template.GlobalPitchRateNormalization) * Constants.INVLOG2)
                    * State.Template.GlobalPitchRateRolloff);
            }

            /* apply optional transformation formula */
            if (State.Template.Formula != null)
            {
                AccentRec Accents;
                AccentRec TrackAccents;

                /* get current live accent info */
                State.ParamGetter(
                    State.ParamGetterContext,
                    out Accents,
                    out TrackAccents);

                /* add in the original differential that the note provided */
                AccentAdd(
                    1,
                    ref State.FrozenNoteAccentDifferential,
                    ref Accents,
                    ref Accents);

                /* evaluate */
                SynthErrorCodes error = EnvelopeParamEval(
                    Temp,
                    State.Template.Formula,
                    ref Accents,
                    ref TrackAccents,
                    SynthParams,
                    out Temp);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    ErrorRef = error;
                    return 0;
                }
            }

            return Temp;
        }

        /* find out if envelope has reached the end */
        public static bool IsEnvelopeAtEnd(EvalEnvelopeRec State)
        {
            return State.EnvelopeHasFinished;
        }

        /* create key-up impulse.  call this before calling EnvelopeUpdate during a */
        /* given cycle.  this call preserves the current level of the envelope but */
        /* skips to the phase after the particular sustain. */
        public static void EnvelopeKeyUpSustain1(EvalEnvelopeRec State)
        {
            if (State.CurrentPhase <= State.SustainPhase1)
            {
                /* find out if we should skip ahead to the sustain point */
                if ((State.SustainPhase1Type == SustainTypes.eEnvelopeSustainPointSkip)
                    || (State.SustainPhase1Type == SustainTypes.eEnvelopeReleasePointSkip))
                {
                    while (State.CurrentPhase < State.SustainPhase1)
                    {
                        State.CurrentPhase += 1;
                    }
                    State.SustainPhase1 = -1;
                    EnvStepToNextInterval(State);
                    return;
                }
            }
            if (State.CurrentPhase < State.SustainPhase1)
            {
                /* if we haven't even reached the sustain phase, then cancel the sustain */
                /* phase so that it can't happen */
                State.SustainPhase1 = -1;
            }
            else if (State.CurrentPhase == State.SustainPhase1)
            {
                /* or, if we are sustaining, then break the sustain */
                State.SustainPhase1 = -1;
                if (State.EnvelopeUpdate == _EnvUpdateSustain)
                {
                    /* we are sustaining, so break it */
                    EnvStepToNextInterval(State);
                }
                /* else we haven't reached it, but we broke it above */
            }
            /* otherwise, we must be past it so just ignore */
        }

        /* create key-up impulse.  call this before calling EnvelopeUpdate during a */
        /* given cycle.  this call preserves the current level of the envelope but */
        /* skips to the phase after the particular sustain. */
        public static void EnvelopeKeyUpSustain2(EvalEnvelopeRec State)
        {
            if (State.CurrentPhase <= State.SustainPhase2)
            {
                /* find out if we should skip ahead to the sustain point */
                if ((State.SustainPhase2Type == SustainTypes.eEnvelopeSustainPointSkip)
                    || (State.SustainPhase2Type == SustainTypes.eEnvelopeReleasePointSkip))
                {
                    while (State.CurrentPhase < State.SustainPhase2)
                    {
                        State.CurrentPhase += 1;
                    }
                    State.SustainPhase2 = -1;
                    EnvStepToNextInterval(State);
                    return;
                }
            }
            if (State.CurrentPhase < State.SustainPhase2)
            {
                /* if we haven't even reached the sustain phase, then cancel the sustain */
                /* phase so that it can't happen */
                State.SustainPhase2 = -1;
            }
            else if (State.CurrentPhase == State.SustainPhase2)
            {
                /* or, if we are sustaining, then break the sustain */
                State.SustainPhase2 = -1;
                if (State.EnvelopeUpdate == _EnvUpdateSustain)
                {
                    /* we are sustaining, so break it */
                    EnvStepToNextInterval(State);
                }
                /* else we haven't reached it, but we broke it above */
            }
            /* otherwise, we must be past it so just ignore */
        }

        /* create key-up impulse.  call this before calling EnvelopeUpdate during a */
        /* given cycle.  this call preserves the current level of the envelope but */
        /* skips to the phase after the particular sustain. */
        public static void EnvelopeKeyUpSustain3(EvalEnvelopeRec State)
        {
            if (State.CurrentPhase <= State.SustainPhase3)
            {
                /* find out if we should skip ahead to the sustain point */
                if ((State.SustainPhase3Type == SustainTypes.eEnvelopeSustainPointSkip)
                    || (State.SustainPhase3Type == SustainTypes.eEnvelopeReleasePointSkip))
                {
                    while (State.CurrentPhase < State.SustainPhase3)
                    {
                        State.CurrentPhase += 1;
                    }
                    State.SustainPhase3 = -1;
                    EnvStepToNextInterval(State);
                    return;
                }
            }
            if (State.CurrentPhase < State.SustainPhase3)
            {
                /* if we haven't even reached the sustain phase, then cancel the sustain */
                /* phase so that it can't happen */
                State.SustainPhase3 = -1;
            }
            else if (State.CurrentPhase == State.SustainPhase3)
            {
                /* or, if we are sustaining, then break the sustain */
                State.SustainPhase3 = -1;
                if (State.EnvelopeUpdate == _EnvUpdateSustain)
                {
                    /* we are sustaining, so break it */
                    EnvStepToNextInterval(State);
                }
                /* else we haven't reached it, but we broke it above */
            }
            /* otherwise, we must be past it so just ignore */
        }

        /* update routine for linear-absolute intervals */
        private static double EnvUpdateLinearAbsolute(EvalEnvelopeRec State)
        {
            /* decrement the counter */
            State.LinearTransitionCounter -= 1;

            /* see if we should advance to the next state */
            if (State.LinearTransitionCounter < 0)
            {
                /* yup */
                EnvStepToNextInterval(State);
                /* a new function is now in charge, so defer to it */
                return State.EnvelopeUpdate(State);
            }
            else
            {
                /* nope, we need to compute the next value */
                State.LastOutputtedValue = LinearTransitionUpdate(ref State.LinearTransition);
                return State.LastOutputtedValue;
            }
        }

        /* update routine for linear-decibel intervals */
        private static double EnvUpdateLinearDecibels(EvalEnvelopeRec State)
        {
            /* decrement the counter */
            State.LinearTransitionCounter -= 1;

            /* see if we should advance to the next state */
            if (State.LinearTransitionCounter < 0)
            {
                /* yup */
                EnvStepToNextInterval(State);
                /* a new function is now in charge, so defer to it */
                return State.EnvelopeUpdate(State);
            }
            else
            {
                /* we need to compute the next value */
                double Temp = LinearTransitionUpdate(ref State.LinearTransition);
                State.LastOutputtedValue = ExpSegEndpointToLinear(Temp);
                return State.LastOutputtedValue;
            }
        }

        /* sustain on a particular value */
        private static double EnvUpdateSustain(EvalEnvelopeRec State)
        {
            return State.LastOutputtedValue;
        }

        // Used by loop-envelope lfo initialization (kind of a hack)
        // Also used by envelope smoothing
        private static double EnvelopeInitialValue(EvalEnvelopeRec State)
        {
            double v = State.LastOutputtedValue; // covers default/zero and ConstantShortcut cases
            if (State.LinearTransitionCounter <= 0)
            {
                // if envelope start is not deferred, iterate through segments until non-zero delay is encountered
                for (int i = 0; i < State.PhaseVector.Length; i++)
                {
                    // TODO: If envelope rate is low, this could be zero when it was really a small number that rounded to
                    // zero. In that case, we ought to treat it as non-zero and smooth the transition, since that is probably
                    // what was intended.
                    if (State.PhaseVector[i].Duration != 0)
                    {
                        break;
                    }
                    switch (State.PhaseVector[i].TargetType)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case EnvTargetTypes.eEnvelopeTargetAbsolute:
                            v = State.PhaseVector[i].FinalAmplitude;
                            break;
                        case EnvTargetTypes.eEnvelopeTargetScaling:
                            v *= State.PhaseVector[i].FinalAmplitude;
                            break;
                    }
                }
            }
            return v;
        }

        /* routine to step to the next non-zero width interval */
        private static void EnvStepToNextInterval(EvalEnvelopeRec State)
        {
            /* first, check to see if we should sustain */
            if ((State.CurrentPhase >= 0)
                && (((State.CurrentPhase == State.SustainPhase1)
                    && ((State.SustainPhase1Type == SustainTypes.eEnvelopeSustainPointSkip)
                    || (State.SustainPhase1Type == SustainTypes.eEnvelopeSustainPointNoSkip)))
                ||
                ((State.CurrentPhase == State.SustainPhase2)
                    && ((State.SustainPhase2Type == SustainTypes.eEnvelopeSustainPointSkip)
                    || (State.SustainPhase2Type == SustainTypes.eEnvelopeSustainPointNoSkip)))
                ||
                ((State.CurrentPhase == State.SustainPhase3)
                    && ((State.SustainPhase3Type == SustainTypes.eEnvelopeSustainPointSkip)
                    || (State.SustainPhase3Type == SustainTypes.eEnvelopeSustainPointNoSkip)))))
            {
                /* yup, sustain */
                State.EnvelopeUpdate = _EnvUpdateSustain;
                return;
            }

            /* if no sustain, then we can advance to the next phase */
            State.CurrentPhase += 1;
            if (State.CurrentPhase >= State.NumPhases)
            {
                /* no more phases, so we must be done.  just sustain the last value indefinitely */
                State.EnvelopeUpdate = _EnvUpdateSustain;
                State.EnvelopeHasFinished = true;
            }
            else
            {
                OneEnvPhaseRec[] PhaseVector = State.PhaseVector;
                int CurrentPhase = State.CurrentPhase;
                if (unchecked((uint)CurrentPhase >= (uint)PhaseVector.Length))
                {
                    throw new IndexOutOfRangeException();
                }

                if (PhaseVector[CurrentPhase].Duration > 0)
                {
                    /* if duration is greater than 0, then we go normally */
                    State.LinearTransitionTotalDuration = PhaseVector[CurrentPhase].Duration;
                    State.LinearTransitionCounter = PhaseVector[CurrentPhase].Duration;
                    /* figure out what routine to use */
                    switch (PhaseVector[CurrentPhase].TransitionType)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();

                        case EnvTransTypes.eEnvelopeLinearInAmplitude:
                            State.EnvelopeUpdate = _EnvUpdateLinearAbsolute;
                            switch (PhaseVector[CurrentPhase].TargetType)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();

                                case EnvTargetTypes.eEnvelopeTargetAbsolute:
                                    ResetLinearTransition(
                                        ref State.LinearTransition,
                                        State.LastOutputtedValue,
                                        PhaseVector[CurrentPhase].FinalAmplitude,
                                        State.LinearTransitionTotalDuration);
                                    break;
                                case EnvTargetTypes.eEnvelopeTargetScaling:
                                    ResetLinearTransition(
                                        ref State.LinearTransition,
                                        State.LastOutputtedValue,
                                        State.LastOutputtedValue * PhaseVector[CurrentPhase].FinalAmplitude,
                                        State.LinearTransitionTotalDuration);
                                    break;
                            }
                            break;

                        case EnvTransTypes.eEnvelopeLinearInDecibels:
                            {
                                /* figure out end points */
                                /* this is set so that the magnitude of the thing always indicates */
                                /* the log of the value, thus the linear transition is linear in the */
                                /* log.  the sign is for the actual value.  we normalize the */
                                /* log using DECIBELEXPANDER so that the log is always positive, */
                                /* freeing up the sign for us.  signed transitions (positive to */
                                /* negative, for instance) are weird. */

                                State.EnvelopeUpdate = _EnvUpdateLinearDecibels;

                                double InitialDecibels = ExpSegEndpointToLog(State.LastOutputtedValue);

                                double FinalDecibels;
                                switch (PhaseVector[CurrentPhase].TargetType)
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case EnvTargetTypes.eEnvelopeTargetAbsolute:
                                        FinalDecibels = ExpSegEndpointToLog(PhaseVector[CurrentPhase].FinalAmplitude);
                                        break;
                                    case EnvTargetTypes.eEnvelopeTargetScaling:
                                        FinalDecibels = ExpSegEndpointToLog(PhaseVector[CurrentPhase].FinalAmplitude * State.LastOutputtedValue);
                                        break;
                                }

                                ResetLinearTransition(
                                    ref State.LinearTransition,
                                    InitialDecibels,
                                    FinalDecibels,
                                    State.LinearTransitionTotalDuration);
                            }
                            break;
                    }
                }
                else
                {
                    /* they want the transition immediately */
                    switch (PhaseVector[CurrentPhase].TargetType)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case EnvTargetTypes.eEnvelopeTargetAbsolute:
                            State.LastOutputtedValue = PhaseVector[CurrentPhase].FinalAmplitude;
                            break;
                        case EnvTargetTypes.eEnvelopeTargetScaling:
                            State.LastOutputtedValue = PhaseVector[CurrentPhase].FinalAmplitude * State.LastOutputtedValue;
                            break;
                    }
                    /* do it again.  this will handle ties nicely too */
                    EnvStepToNextInterval(State);
                }
            }
        }

        // shared utility function for exponential segment calculation
        public static double ExpSegEndpointToLog(double linearValue)
        {
            /* this is set so that the magnitude of the thing always indicates */
            /* the log of the value, thus the linear transition is linear in the */
            /* log.  the sign is for the actual value.  we normalize the */
            /* log using DECIBELEXPANDER so that the log is always positive, */
            /* freeing up the sign for us.  signed transitions (positive to */
            /* negative, for instance) are weird. */
            double Temp = linearValue;
            bool Negative = false;
            if (Temp < 0)
            {
                Temp = -Temp;
                Negative = true;
            }
            Temp = Math.Max(Temp, DECIBELTHRESHHOLD);
            if (Temp < DECIBELTHRESHHOLD)
            {
                // we used to use FastFixedType [a fixed point type with a 15-bit fraction], so we use same epsilon value
                Temp = DECIBELTHRESHHOLD;
            }
            double decibelsValue = Math.Log(DECIBELEXPANDER * Temp);
            if (Negative)
            {
                decibelsValue = -decibelsValue;
            }
            return decibelsValue;
        }

        // shared utility function for exponential segment calculation
        public static double ExpSegEndpointToLinear(double decibelsValue)
        {
            double Temp = decibelsValue;
            bool Negative = false;
            if (Temp < 0)
            {
                Temp = -Temp;
                Negative = true;
            }
            Temp = Math.Exp(Temp) * (1d / DECIBELEXPANDER);
            if (Negative)
            {
                Temp = -Temp;
            }
            double linearValue = Temp;
            return linearValue;
        }

        /* retrigger envelopes from the origin point */
        public static void EnvelopeRetriggerFromOrigin(
            EvalEnvelopeRec State,
            ref AccentRec Accents,
            double FrequencyHertz,
            double Loudness,
            double HurryUp,
            bool ActuallyRetrigger,
            SynthParamRec SynthParams)
        {
            /* if we actually retrigger, then reset the state */
            if (ActuallyRetrigger)
            {
                State.CurrentPhase = -1;
                State.EnvelopeHasFinished = false;
                while (State.CurrentPhase < State.Origin - 1)
                {
                    State.CurrentPhase += 1;
                }
                State.SustainPhase1 = State.OriginalSustainPhase1;
                State.SustainPhase2 = State.OriginalSustainPhase2;
                State.SustainPhase3 = State.OriginalSustainPhase3;
                State.LinearTransitionCounter = 0; /* force transition on next update */
                State.EnvelopeUpdate = _EnvUpdateLinearAbsolute;
            }

            // A note about accents to follow:
            // - The "Accents" parameter to this function comes from the FrozenNote - so they are the accent values in effect
            //   at the time the note was "frozen" (i.e. the accent defaults added to the individual note's adjustments).
            // - The "LiveAccents" parameter is the current accent defaults at time t (which should be equal to "Accents", at the
            //   the a note is initiated, but not necessarily before or after), since this event is occurring at time t.
            // - The "LiveTrackAccents" parameter is the current track effect accent values.
            // There is some redundancy here, but it is not being changed in case this is wrong and there are legacy
            // interactions that need to be preserved.
            AccentRec LiveAccents;
            AccentRec LiveTrackAccents;
#if DEBUG
            bool liveAccentInit = false;
#endif
            if (EnvelopeContainsFormula(State.Template)) // an optimization
            {
                /* get current live accents that do not contain any info from the note */
                State.ParamGetter(
                    State.ParamGetterContext,
                    out LiveAccents,
                    out LiveTrackAccents);
#if DEBUG
                liveAccentInit = true;
#endif
            }
            else
            {
                LiveAccents = new AccentRec();
                LiveTrackAccents = new AccentRec();
            }

            OneEnvPhaseRec[] PhaseVector = State.PhaseVector;
            if (unchecked((uint)State.NumPhases > (uint)PhaseVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            EnvStepRec[] TemplatePhaseArray = State.Template.PhaseArray;
            if (unchecked((uint)State.NumPhases > (uint)TemplatePhaseArray.Length))
            {
                throw new IndexOutOfRangeException();
            }

            /* no matter what, refill the parameters */
            double accumulatedError = 0;
            for (int i = 0; i < State.NumPhases; i++)
            {
                /* calculate the total duration.  the effect of accents is this: */
                /*  - the accent is the base-2 log of a multiplier for the rate.  a value of 0 */
                /*    does not change the rate.  -1 halves the rate, and 1 doubles the rate. */
                /*  - the accent scaling factor is the base-2 log for scaling the accent. */
                /*    a value of 0 eliminates the effect of the accent, a value of 1 does not */
                /*    scale the accent. */
                /*  - pitch has two factors:  normalization point and rolloff.  rolloff */
                /*    determines how much the signal will decrease with each octave.  0 */
                /*    removes effect, 1 halfs signal with each octave.  normalization point */
                /*    determines what pitch will be the invariant point. */
                double Temp;
                if (TemplatePhaseArray[i].DurationFunction == null)
                {
                    Temp = TemplatePhaseArray[i].Duration;
                }
                else
                {
#if DEBUG
                    Debug.Assert(liveAccentInit);
#endif
                    SynthErrorCodes error = EnvelopeInitParamEval(
                        TemplatePhaseArray[i].DurationFunction,
                        ref Accents,
                        ref LiveTrackAccents,
                        SynthParams,
                        out Temp);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        // TODO:
                    }
                }
                double preciseDuration = SynthParams.dEnvelopeRate
                    * HurryUp
                    * Temp
                    * Math.Pow(2, -(AccentProduct(ref Accents, ref TemplatePhaseArray[i].AccentRate)
                        + (Math.Log(FrequencyHertz
                            / TemplatePhaseArray[i].FrequencyRateNormalization)
                        * Constants.INVLOG2) * TemplatePhaseArray[i].FrequencyRateRolloff))
                    + accumulatedError;
                PhaseVector[i].Duration = (int)Math.Round(preciseDuration);
                accumulatedError = preciseDuration - PhaseVector[i].Duration;
                /* the final amplitude scaling values are computed similarly to the rate */
                /* scaling values. */
                if (TemplatePhaseArray[i].EndPointFunction == null)
                {
                    Temp = TemplatePhaseArray[i].EndPoint;
                }
                else
                {
#if DEBUG
                    Debug.Assert(liveAccentInit);
#endif
                    SynthErrorCodes error = EnvelopeInitParamEval(
                        TemplatePhaseArray[i].EndPointFunction,
                        ref Accents,
                        ref LiveTrackAccents,
                        SynthParams,
                        out Temp);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        // TODO:
                    }
                }
                PhaseVector[i].FinalAmplitude = Temp
                    * State.Template.OverallScalingFactor * Loudness
                    * Math.Pow(2, -(AccentProduct(ref Accents, ref TemplatePhaseArray[i].AccentAmp)
                        + (Math.Log(FrequencyHertz
                            / TemplatePhaseArray[i].FrequencyAmpNormalization)
                        * Constants.INVLOG2) * TemplatePhaseArray[i].FrequencyAmpRolloff));
            }

            /* recompute accent differentials incorporating new note's info */
            if (State.Template.Formula != null)
            {
#if DEBUG
                Debug.Assert(liveAccentInit);
#endif

                /* subtract composite accents ("Accents") from current live accents */
                AccentAdd(
                    -1,
                    ref Accents,
                    ref LiveAccents,
                    ref State.FrozenNoteAccentDifferential);
            }
        }

        /* find out if the envelope generator has started yet */
        public static bool HasEnvelopeStartedYet(EvalEnvelopeRec State)
        {
            return State.CurrentPhase >= 0;
        }

        public static bool EnvelopeCurrentSegmentExponential(EvalEnvelopeRec State)
        {
            if ((State.CurrentPhase < 0) || (State.PhaseVector.Length == 0))
            {
                return false;
            }
            else if (State.CurrentPhase < State.PhaseVector.Length)
            {
                Debug.Assert((State.PhaseVector[State.CurrentPhase].TransitionType == EnvTransTypes.eEnvelopeLinearInDecibels)
                    || (State.PhaseVector[State.CurrentPhase].TransitionType == EnvTransTypes.eEnvelopeLinearInAmplitude));
                return State.PhaseVector[State.CurrentPhase].TransitionType == EnvTransTypes.eEnvelopeLinearInDecibels;
            }
            else
            {
                int n = State.PhaseVector.Length;
                Debug.Assert((State.PhaseVector[n - 1].TransitionType == EnvTransTypes.eEnvelopeLinearInDecibels)
                    || (State.PhaseVector[n - 1].TransitionType == EnvTransTypes.eEnvelopeLinearInAmplitude));
                return State.PhaseVector[n - 1].TransitionType == EnvTransTypes.eEnvelopeLinearInDecibels;
            }
        }
    }
}
