/*
 *  Copyright © 1994-2002, 2015-2016 Thomas R. Lawrence
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
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        [Flags]
        public enum WhichEnvType
        {
            eRls1 = 1,
            eRls2 = 2,
            eRls3 = 4,

            eRlsAll = eRls1 | eRls2 | eRls3,
        }

        public interface IOscillatorTemplate
        {
            SynthErrorCodes NewState(
                double FreqForMultisampling,
                ref AccentRec Accents,
                double Loudness,
                double HurryUp,
                out int PreOriginTimeOut,
                double StereoPosition,
                double InitialFrequency,
                double PitchDisplacementDepthLimit,
                double PitchDisplacementRateLimit,
                int PitchDisplacementStartPoint,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams,
                out IOscillator StateOut);

            OscillatorTypes Type { get; }
        }

        public interface IOscillator
        {
            SynthErrorCodes UpdateEnvelopes(
                double NewFrequencyHertz,
                SynthParamRec SynthParams);

            void FixUpPreOrigin(
                int ActualPreOrigin);

            void KeyUpSustain1();
            void KeyUpSustain2();
            void KeyUpSustain3();

            void Restart(
                ref AccentRec NewAccents,
                double NewLoudness,
                double NewHurryUp,
                bool RetriggerEnvelopes,
                double NewStereoPosition,
                double NewInitialFrequency,
                double PitchDisplacementDepthLimit,
                double PitchDisplacementRateLimit,
                SynthParamRec SynthParams);

            SynthErrorCodes Generate(
                int nActualFrames,
                float[] workspace,
                int RawBufferLOffset,
                int RawBufferROffset,
                int PrivateWorkspaceLOffset,
                int PrivateWorkspaceROffset,
                SynthParamRec SynthParams);

            bool IsItFinished();

            void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs);
        }

        public class OscBankVectorRec
        {
            /* this is the reference to the template object */
            public IOscillatorTemplate TemplateReference;
        }

        public class OscBankTemplateRec
        {
            /* parameter updator for track (so we can set up individual notes properly) */
            public IncrParamUpdateRec ParamUpdator;

            /* template for the pitch displacement LFO */
            public LFOListSpecRec PitchLFOTemplate;

            /* instrument overall loudness */
            public double InstrOverallLoudness;

            /* list of template records describing each oscillator */
            public OscBankVectorRec[] TemplateArray;
            /* this is the number of oscillators in the array */
            public int NumOscillatorsInBank;

            /* combined oscillator effect template, null if it is empty */
            public EffectSpecListRec CombinedOscillatorEffects;
        }

        public class OscStateRec
        {
            /* this is a reference to the state object for this oscillator */
            public IOscillator StateReference;

            /* copy of the routine vectors */
            public OscBankVectorRec Template;

            /* next oscillator in the list */
            public OscStateRec Next;
        }

        public class OscStateBankRec
        {
            /* what are we derived from */
            public OscBankTemplateRec BankTemplate;

            /* list of oscillators that this oscillator bank is comprised of */
            public OscStateRec OscillatorList;

            /* this calculates the differential values for periodic pitch displacements */
            public LFOGenRec PitchLFO;

            /* if this object ties to a note, then this is the note to tie to.  this is */
            /* used for finding existing oscillators for tie continuations. */
            public NoteNoteObjectRec TieToNote;

            /* portamento control parameters */
            public int PortamentoCounter; /* 0 = done */
            public int TotalPortamentoTicks;
            public double InitialFrequency;
            public double FinalFrequency;
            public double CurrentFrequency;
            /* True = portamento linear to Hertz; false = portamento linear to half-steps */
            public bool PortamentoHertz;

            /* various counters (in terms of envelope ticks) */
            /* negative = expired */
            public int Release1Countdown;
            public int Release2Countdown;
            public int Release3Countdown;
            public int PitchLFOStartCountdown;

            /* combined oscillator effect generator, null if not in use */
            public OscEffectGenRec CombinedOscEffectGenerator;
        }

        /* construct an oscillator bank template record.  various parameters are passed in */
        /* which are needed for synthesis.  ParameterUpdator is the parameter information */
        /* record for the whole track of which this is a part. */
        public static OscBankTemplateRec NewOscBankTemplate(
            InstrumentRec InstrumentDefinition,
            IncrParamUpdateRec ParameterUpdator,
            SynthParamRec SynthParams)
        {
            OscillatorListRec OscillatorListObject;

            OscBankTemplateRec Template = new OscBankTemplateRec();

            /* the oscillator bank template contains all of the information needed for */
            /* constructing oscillators as notes are to be executed. */
            /* number of oscillators in a bank. */
            OscillatorListObject = GetInstrumentOscillatorList(InstrumentDefinition);

            /* get LFO information */
            Template.PitchLFOTemplate = GetInstrumentFrequencyLFOList(InstrumentDefinition);

            Template.ParamUpdator = ParameterUpdator;

            Template.InstrOverallLoudness = GetInstrumentOverallLoudness(InstrumentDefinition);

            Template.CombinedOscillatorEffects = GetInstrumentCombinedOscEffectSpecList(InstrumentDefinition);
            if (0 == GetEffectSpecListLength(Template.CombinedOscillatorEffects))
            {
                /* if no effects, then set to null, so we don't do any processing */
                Template.CombinedOscillatorEffects = null;
            }

            /* vector containing templates for all of the oscillators */
            Template.NumOscillatorsInBank = GetOscillatorListLength(OscillatorListObject);
            Template.TemplateArray = new OscBankVectorRec[Template.NumOscillatorsInBank];

            /* build entry for each oscillator */
            for (int Scan = 0; Scan < Template.NumOscillatorsInBank; Scan += 1)
            {
                OscillatorRec Osc = GetOscillatorFromList(OscillatorListObject, Scan);
                OscBankVectorRec Osc1 = Template.TemplateArray[Scan] = new OscBankVectorRec();

                switch (OscillatorGetWhatKindItIs(Osc))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case OscillatorTypes.eOscillatorSampled:
                        Osc1.TemplateReference = SampleTemplateRec.NewSampleTemplate(Osc, SynthParams);
                        break;
                    case OscillatorTypes.eOscillatorWaveTable:
                        Osc1.TemplateReference = WaveTableTemplateRec.NewWaveTableTemplate(Osc, SynthParams);
                        break;
                    case OscillatorTypes.eOscillatorFOF:
                        Osc1.TemplateReference = FOFTemplateRec.NewFOFTemplate(Osc, SynthParams);
                        break;
                    case OscillatorTypes.eOscillatorAlgorithm:
                        Osc1.TemplateReference = AlgorithmicTemplateRec.NewAlgorithmicTemplate(Osc, SynthParams);
                        break;
                    case OscillatorTypes.eOscillatorFMSynth:
                        Osc1.TemplateReference = FMSynthTemplateRec.NewFMSynthTemplate(Osc, SynthParams);
                        break;
                    case OscillatorTypes.eOscillatorPluggable:
                        Osc1.TemplateReference = new PluggableOscillatorTemplate(Osc, SynthParams);
                        break;
                }
            }

            return Template;
        }

        /* construct a new oscillator bank state object based on the note.  the note is */
        /* assumed to start "now" in terms of the parameters in the ParameterUpdator.  */
        /* the ScanningGapWidth is the number of envelope clock ticks in the current scanning */
        /* gap.  this is used to determine how far later than "now" in terms of the back */
        /* edge of the scanning gap (different from above) the osc bank should start playing. */
        /* *WhenToStartPlayingOut returns the number of envelope ticks after the back edge */
        /* of the scanning gap that the note should be started. */
        /*     <already played>       |    <scanning gap>     |    <not yet analyzed> */
        /*   time ---.    time ---.    time ---.    time ---.    time ---.   time ---. */
        /*                            ^A                      ^B     */
        /* point A is the back edge of the scanning gap.  as this edge moves forward in time, */
        /*   oscillator bank state objects are removed from the queue and playback is commenced */
        /*   for them. */
        /* point B is the front edge of the scanning gap.  as this edge moves forward in time, */
        /*   notes are extracted from the track and state bank objects are created for them. */
        /*   ParameterUpdator always reflects parameters at this point in time. */
        public static SynthErrorCodes NewOscBankState(
            OscBankTemplateRec Template,
            out int WhenToStartPlayingOut,
            NoteNoteObjectRec Note,
            double EnvelopeTicksPerDurationTick,
            short PitchIndexAdjust,
            PlayTrackInfoRec TrackInfo,
            SynthParamRec SynthParams,
            out OscStateBankRec StateOut)
        {
            FrozenNoteRec FrozenNote = null;
            int ThisPreOriginTime;
            int StartPointAdjust;
            OscStateRec StateScan;

            WhenToStartPlayingOut = 0;
            StateOut = null;

            int MaxOscillatorPreOriginTime = 0;

            OscStateBankRec State = new OscStateBankRec();

            State.BankTemplate = Template;

            /* freeze the parameters */
            FrozenNote = FixNoteParameters(
                Template.ParamUpdator,
                Note,
                out StartPointAdjust,
                EnvelopeTicksPerDurationTick,
                PitchIndexAdjust,
                SynthParams);


            /* this calculates the differential values for periodic pitch displacements */
            State.PitchLFO = NewLFOGenerator(
                Template.PitchLFOTemplate,
                out ThisPreOriginTime,
                ref FrozenNote.Accents,
                FrozenNote.NominalFrequency,
                FrozenNote.HurryUpFactor,
                FrozenNote.PitchDisplacementDepthLimit,
                FrozenNote.PitchDisplacementRateLimit,
                FrozenNote.MultisampleFrequency,
                _PlayTrackParamGetter,
                TrackInfo,
                SynthParams);
            if (ThisPreOriginTime > MaxOscillatorPreOriginTime)
            {
                MaxOscillatorPreOriginTime = ThisPreOriginTime;
            }

            /* list of oscillators that this oscillator bank is comprised of */
            for (int Scan = 0; Scan < Template.NumOscillatorsInBank; Scan += 1)
            {
                OscStateRec OneState = new OscStateRec();

                /* link it in */
                OneState.Next = State.OscillatorList;
                State.OscillatorList = OneState;

                /* copy over the function vectors */
                OneState.Template = Template.TemplateArray[Scan];

                /* create the oscillator */
                SynthErrorCodes Result = OneState.Template.TemplateReference.NewState(
                    FrozenNote.MultisampleFrequency,
                    ref FrozenNote.Accents,
                    FrozenNote.LoudnessAdjust * Template.InstrOverallLoudness,
                    FrozenNote.HurryUpFactor,
                    out ThisPreOriginTime,
                    FrozenNote.StereoPosition,
                    FrozenNote.NominalFrequency,
                    FrozenNote.PitchDisplacementDepthLimit,
                    FrozenNote.PitchDisplacementRateLimit,
                    FrozenNote.PitchDisplacementStartPoint,
                    TrackInfo,
                    SynthParams,
                    out OneState.StateReference);
                if (Result != SynthErrorCodes.eSynthDone)
                {
                    return Result;
                }
                if (ThisPreOriginTime > MaxOscillatorPreOriginTime)
                {
                    MaxOscillatorPreOriginTime = ThisPreOriginTime;
                }
            }

            if ((Template.CombinedOscillatorEffects != null) && (GetEffectSpecListLength(Template.CombinedOscillatorEffects)>0))
            {
                SynthErrorCodes Result = NewOscEffectGenerator(
                   Template.CombinedOscillatorEffects,
                   ref FrozenNote.Accents,
                   FrozenNote.HurryUpFactor,
                   FrozenNote.NominalFrequency,
                   FrozenNote.MultisampleFrequency,
                   out ThisPreOriginTime,
                   TrackInfo,
                   SynthParams,
                   out State.CombinedOscEffectGenerator);
                if (Result != SynthErrorCodes.eSynthDone)
                {
                    return Result;
                }
                if (ThisPreOriginTime > MaxOscillatorPreOriginTime)
                {
                    MaxOscillatorPreOriginTime = ThisPreOriginTime;
                }
            }
            /* else no combined oscillator effects, State.CombinedOscEffectGenerator is null */

            /* if this object ties to a note, then this is the note */
            /* to tie to.  this is used for finding existing oscillators */
            /* for tie continuations. */
            State.TieToNote = Note._Tie;

            /* portamento control parameters */
            State.PortamentoCounter = 0;
            State.CurrentFrequency = FrozenNote.NominalFrequency;


            /* fix up pre-origin times */
            StateScan = State.OscillatorList;
            while (StateScan != null)
            {
                StateScan.StateReference.FixUpPreOrigin(
                    MaxOscillatorPreOriginTime);
                StateScan = StateScan.Next;
            }
            LFOGeneratorFixEnvelopeOrigins(
                State.PitchLFO,
                MaxOscillatorPreOriginTime);
            if (State.CombinedOscEffectGenerator != null)
            {
                FixUpOscEffectGeneratorPreOrigin(
                    State.CombinedOscEffectGenerator,
                    MaxOscillatorPreOriginTime);
            }

            /* various counters (in terms of envelope ticks) */
            if (State.TieToNote == null)
            {
                State.Release1Countdown = FrozenNote.ReleasePoint1 + MaxOscillatorPreOriginTime;
                State.Release2Countdown = FrozenNote.ReleasePoint2 + MaxOscillatorPreOriginTime;
                State.Release3Countdown = FrozenNote.ReleasePoint3 + MaxOscillatorPreOriginTime;
            }
            else
            {
                /* for ties, only honor releases from start */
                if (FrozenNote.Release1FromStart)
                {
                    State.Release1Countdown = FrozenNote.ReleasePoint1 + MaxOscillatorPreOriginTime;
                }
                else
                {
                    State.Release1Countdown = -1;
                }
                if (FrozenNote.Release2FromStart)
                {
                    State.Release2Countdown = FrozenNote.ReleasePoint2 + MaxOscillatorPreOriginTime;
                }
                else
                {
                    State.Release2Countdown = -1;
                }
                if (FrozenNote.Release3FromStart)
                {
                    State.Release3Countdown = FrozenNote.ReleasePoint3 + MaxOscillatorPreOriginTime;
                }
                else
                {
                    State.Release3Countdown = -1;
                }
            }
            State.PitchLFOStartCountdown = FrozenNote.PitchDisplacementStartPoint
                /*+ MaxOscillatorPreOriginTime*/;
            /* pre origin relationship must be preserved for */
            /* pitch LFO trigger */

            /* done */
            WhenToStartPlayingOut = StartPointAdjust - MaxOscillatorPreOriginTime;
            StateOut = State;
            return SynthErrorCodes.eSynthDone;
        }

        /* this is used for resetting a note for a tie */
        /* the FrozenNote object is NOT disposed */
        public static void ResetOscBankState(
            OscStateBankRec State,
            FrozenNoteRec FrozenNote,
            SynthParamRec SynthParams)
        {
            OscStateRec OneState;
            bool RetriggerEnvelopes;

            RetriggerEnvelopes = ((FrozenNote.OriginalNote.Flags & NoteFlags.eRetriggerEnvelopesOnTieFlag) != 0);

            /* go through the oscillators and retrigger them */
            OneState = State.OscillatorList;
            while (OneState != null)
            {
                OneState.StateReference.Restart(
                    ref FrozenNote.Accents,
                    FrozenNote.LoudnessAdjust * State.BankTemplate.InstrOverallLoudness,
                    FrozenNote.HurryUpFactor,
                    RetriggerEnvelopes,
                    FrozenNote.StereoPosition,
                    FrozenNote.NominalFrequency,
                    FrozenNote.PitchDisplacementDepthLimit,
                    FrozenNote.PitchDisplacementRateLimit,
                    SynthParams);
                OneState = OneState.Next;
            }

            LFOGeneratorRetriggerFromOrigin(
                State.PitchLFO,
                ref FrozenNote.Accents,
                FrozenNote.NominalFrequency,
                FrozenNote.HurryUpFactor,
                FrozenNote.PitchDisplacementDepthLimit,
                FrozenNote.PitchDisplacementRateLimit,
                RetriggerEnvelopes,
                SynthParams);

            if (State.CombinedOscEffectGenerator != null)
            {
                OscEffectGeneratorRetriggerFromOrigin(
                    State.CombinedOscEffectGenerator,
                    ref FrozenNote.Accents,
                    FrozenNote.NominalFrequency,
                    FrozenNote.HurryUpFactor,
                    RetriggerEnvelopes,
                    SynthParams);
            }

            /* if this object ties to a note, then this is the note to tie to.  this is */
            /* used for finding existing oscillators for tie continuations. */
            State.TieToNote = FrozenNote.OriginalNote._Tie;

            /* portamento control parameters */
            if (!FrozenNote.PortamentoBeforeNote)
            {
                /* if PortamentoBeforeNote is not set, then we have to restart the portamento */
                /* with the current note, otherwise it has already been restarted earlier */
                RestartOscBankStatePortamento(State, FrozenNote);
            }

            /* various counters (in terms of envelope ticks) */
            if (State.TieToNote == null)
            {
                State.Release1Countdown = FrozenNote.ReleasePoint1;
                State.Release2Countdown = FrozenNote.ReleasePoint2;
                State.Release3Countdown = FrozenNote.ReleasePoint3;
            }
            else
            {
                /* for ties, only honor releases from start */
                if (FrozenNote.Release1FromStart)
                {
                    State.Release1Countdown = FrozenNote.ReleasePoint1;
                }
                else
                {
                    State.Release1Countdown = -1;
                }
                if (FrozenNote.Release2FromStart)
                {
                    State.Release2Countdown = FrozenNote.ReleasePoint2;
                }
                else
                {
                    State.Release2Countdown = -1;
                }
                if (FrozenNote.Release3FromStart)
                {
                    State.Release3Countdown = FrozenNote.ReleasePoint3;
                }
                else
                {
                    State.Release3Countdown = -1;
                }
            }
            /* do not reset PitchLFOStartCountdown since we can't give it a proper value */
            /* to do the expected thing, and we'd be interrupting the phase of the LFO */
            /* wave generator */
        }

        /* restart portamento cycle prior to full restart for tie continuation */
        /* only the portamento stuff from FrozenNote is used */
        public static void RestartOscBankStatePortamento(
            OscStateBankRec State,
            FrozenNoteRec FrozenNote)
        {
            if (FrozenNote.PortamentoDuration > 0)
            {
                State.PortamentoCounter = FrozenNote.PortamentoDuration;
                State.TotalPortamentoTicks = FrozenNote.PortamentoDuration;
                State.InitialFrequency = State.CurrentFrequency; /* save current pitch */
                State.FinalFrequency = FrozenNote.NominalFrequency;
                State.PortamentoHertz = ((FrozenNote.OriginalNote.Flags & NoteFlags.ePortamentoUnitsHertzNotHalfsteps) != 0);
            }
            else
            {
                State.PortamentoCounter = 0;
                State.CurrentFrequency = FrozenNote.NominalFrequency;
            }
        }

        /* get the reference to the note that this bank ties to.  null if it doesn't */
        public static NoteNoteObjectRec GetOscStateTieTarget(OscStateBankRec State)
        {
            return State.TieToNote;
        }

        // Perform one envelope clock cycle on a state bank. This returns False in OscillatorsRunning when
        // all oscillators are 'finished' (at end of loudness envelope cycle).
        public static SynthErrorCodes OscStateBankGenerateEnvelopes(
            OscStateBankRec State,
            bool fReleaseTimerOnly,
            SynthParamRec SynthParams,
            out bool OscillatorsRunning)
        {
            SynthErrorCodes error;
            OscStateRec OneStateScan;

            OscillatorsRunning = false;

            if (State.Release1Countdown >= 0)
            {
                if (State.Release1Countdown == 0)
                {
                    OneStateScan = State.OscillatorList;
                    while (OneStateScan != null)
                    {
                        OneStateScan.StateReference.KeyUpSustain1();
                        OneStateScan = OneStateScan.Next;
                    }
                    LFOGeneratorKeyUpSustain1(State.PitchLFO);
                    if (State.CombinedOscEffectGenerator != null)
                    {
                        OscEffectKeyUpSustain1(State.CombinedOscEffectGenerator);
                    }
                }
                State.Release1Countdown -= 1;
            }

            if (State.Release2Countdown >= 0)
            {
                if (State.Release2Countdown == 0)
                {
                    OneStateScan = State.OscillatorList;
                    while (OneStateScan != null)
                    {
                        OneStateScan.StateReference.KeyUpSustain2();
                        OneStateScan = OneStateScan.Next;
                    }
                    LFOGeneratorKeyUpSustain2(State.PitchLFO);
                    if (State.CombinedOscEffectGenerator != null)
                    {
                        OscEffectKeyUpSustain2(State.CombinedOscEffectGenerator);
                    }
                }
                State.Release2Countdown -= 1;
            }

            if (State.Release3Countdown >= 0)
            {
                if (State.Release3Countdown == 0)
                {
                    OneStateScan = State.OscillatorList;
                    while (OneStateScan != null)
                    {
                        OneStateScan.StateReference.KeyUpSustain3();
                        OneStateScan = OneStateScan.Next;
                    }
                    LFOGeneratorKeyUpSustain3(State.PitchLFO);
                    if (State.CombinedOscEffectGenerator != null)
                    {
                        OscEffectKeyUpSustain3(State.CombinedOscEffectGenerator);
                    }
                }
                State.Release3Countdown -= 1;
            }

            if (!fReleaseTimerOnly)
            {
                /* perform portamento */
                if (State.PortamentoCounter > 0)
                {
                    /* decrement is done before interpolation so that the final frequency */
                    /* will actually be reached. */
                    State.PortamentoCounter -= 1;
                    if (State.PortamentoHertz)
                    {
                        /* this transition is linear, so it's easy to compute */
                        /* L+F(R-L) */
                        State.CurrentFrequency = State.InitialFrequency
                            + ((double)(State.TotalPortamentoTicks
                            - State.PortamentoCounter) / State.TotalPortamentoTicks)
                            * (State.FinalFrequency - State.InitialFrequency);
                    }
                    else
                    {
                        /* this transition is log-linear, so it's a bit messier */
                        State.CurrentFrequency = State.InitialFrequency * Math.Exp(
                            ((double)(State.TotalPortamentoTicks - State.PortamentoCounter)
                            / State.TotalPortamentoTicks)
                            * ((Math.Log(State.FinalFrequency) * Constants.INVLOG2)
                            - (Math.Log(State.InitialFrequency) * Constants.INVLOG2)) * Constants.LOG2);
                    }
                }

                /* update the pitch LFO modulation & figure out what the current pitch is */
                double Frequency;
                if (State.PitchLFOStartCountdown > 0)
                {
                    State.PitchLFOStartCountdown -= 1;
                    Frequency = State.CurrentFrequency;
                }
                else
                {
                    /* do some pitch stuff */
                    error = SynthErrorCodes.eSynthDone;
                    Frequency = LFOGenUpdateCycle(
                        State.PitchLFO,
                        State.CurrentFrequency,
                        State.CurrentFrequency,
                        SynthParams,
                        ref error);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                /* perform a cycle of resampling */
                OscillatorsRunning = false;
                /* do oscillator processing */
                OneStateScan = State.OscillatorList;
                while (OneStateScan != null)
                {
                    OneStateScan.StateReference.UpdateEnvelopes(
                        Frequency,
                        SynthParams);
                    OscillatorsRunning = OscillatorsRunning || !OneStateScan.StateReference.IsItFinished();
                    OneStateScan = OneStateScan.Next;
                }
                /* process combined effects */
                if (State.CombinedOscEffectGenerator != null)
                {
                    error = OscEffectGeneratorUpdateEnvelopes(
                        State.CombinedOscEffectGenerator,
                        State.CurrentFrequency,
                        SynthParams);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }
            }
            else
            {
                // If osc bank has entered fReleaseTimerOnly mode (aka fDontUpdateEnvelopes, aka fScheduledSkip),
                // it remains in suspended animation until scheduled skip period is over - so that it can finish
                // it's release phases without abrupt glitch of termination.
                OscillatorsRunning = true;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* use the state of the object to generate a cycle of waveform. */
        public static SynthErrorCodes ApplyOscStateBank(
            OscStateBankRec State,
            float[] workspace,
            int nActualFrames,
            int OutputDataLOffset,
            int OutputDataROffset,
            int PrivateOscillatorWorkspaceLOffset,
            int PrivateOscillatorWorkspaceROffset,
            int PrivateCombinedOscillatorWorkspaceLOffset,
            int PrivateCombinedOscillatorWorkspaceROffset,
            SynthParamRec SynthParams)
        {
            OscStateRec OneStateScan;
            OneStateScan = State.OscillatorList;
            if (State.CombinedOscEffectGenerator != null)
            {
                /* resampling when you have combined oscillator effects */

                /* initialize combined workspace */
                FloatVectorZero(
                    workspace,
                    PrivateCombinedOscillatorWorkspaceLOffset,
                    nActualFrames);
                FloatVectorZero(
                    workspace,
                    PrivateCombinedOscillatorWorkspaceROffset,
                    nActualFrames);

                /* do oscillator processing */
                while (OneStateScan != null)
                {
                    OneStateScan.StateReference.Generate(
                        nActualFrames,
                        workspace,
                        PrivateCombinedOscillatorWorkspaceLOffset,
                        PrivateCombinedOscillatorWorkspaceROffset,
                        PrivateOscillatorWorkspaceLOffset,
                        PrivateOscillatorWorkspaceROffset,
                        SynthParams);
                    OneStateScan = OneStateScan.Next;
                }

                /* process combined effects */
                SynthErrorCodes error = ApplyOscEffectGenerator(
                    State.CombinedOscEffectGenerator,
                    workspace,
                    PrivateCombinedOscillatorWorkspaceLOffset,
                    PrivateCombinedOscillatorWorkspaceROffset,
                    nActualFrames,
                    SynthParams);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                /* accumulate combined oscillator buffer to output buffer */
                FloatVectorAcc(
                    workspace,
                    PrivateCombinedOscillatorWorkspaceLOffset,
                    workspace,
                    OutputDataLOffset,
                    nActualFrames);
                FloatVectorAcc(
                    workspace,
                    PrivateCombinedOscillatorWorkspaceROffset,
                    workspace,
                    OutputDataROffset,
                    nActualFrames);
            }
            else
            {
                /* resampling without combined oscillator effects */
                while (OneStateScan != null)
                {
                    OneStateScan.StateReference.Generate(
                        nActualFrames,
                        workspace,
                        OutputDataLOffset,
                        OutputDataROffset,
                        PrivateOscillatorWorkspaceLOffset,
                        PrivateOscillatorWorkspaceROffset,
                        SynthParams);
                    OneStateScan = OneStateScan.Next;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* trigger immediate release of envelopes */
        public static void OscStateBankReleaseEnvelopesNow(
            OscStateBankRec State,
            WhichEnvType eWhichEnv,
            bool ForceIfScheduled)
        {
            /* If release is scheduled, allow it to go on schedule, but if not, */
            /* then force it to happen on the next cycle.  (If it already happened, */
            /* we make it happen again, but that's ok because envelope generator */
            /* does nothing once it's past the phase with the releasepoint.) */
            /* If it's already scheduled, but ForceIfScheduled is set, then force */
            /* it right now anyway. */
            if ((eWhichEnv & WhichEnvType.eRls1) != 0)
            {
                if (ForceIfScheduled || (State.Release1Countdown < 0))
                {
                    State.Release1Countdown = 0;
                }
            }
            if ((eWhichEnv & WhichEnvType.eRls2) != 0)
            {
                if (ForceIfScheduled || (State.Release2Countdown < 0))
                {
                    State.Release2Countdown = 0;
                }
            }
            if ((eWhichEnv & WhichEnvType.eRls3) != 0)
            {
                if (ForceIfScheduled || (State.Release3Countdown < 0))
                {
                    State.Release3Countdown = 0;
                }
            }
        }

        /* finalize before termination */
        public static void FinalizeOscStateBank(
            OscStateBankRec State,
            SynthParamRec SynthParams,
            bool writeOutputLogs)
        {
            OscStateRec OneStateScan;

            OneStateScan = State.OscillatorList;
            while (OneStateScan != null)
            {
                OneStateScan.StateReference.Finalize(
                    SynthParams,
                    writeOutputLogs);
                OneStateScan = OneStateScan.Next;
            }

            if (State.CombinedOscEffectGenerator != null)
            {
                FinalizeOscEffectGenerator(
                    State.CombinedOscEffectGenerator,
                    SynthParams,
                    writeOutputLogs);
            }
        }

        /* ask if there are sample oscillators */
        public static bool OscStateBankContainsSampled(
            OscStateBankRec State)
        {
            OscStateRec OneStateScan;

            OneStateScan = State.OscillatorList;
            while (OneStateScan != null)
            {
                if (OneStateScan.Template.TemplateReference.Type == OscillatorTypes.eOscillatorSampled)
                {
                    return true;
                }

                OneStateScan = OneStateScan.Next;
            }

            return false;
        }
    }
}
