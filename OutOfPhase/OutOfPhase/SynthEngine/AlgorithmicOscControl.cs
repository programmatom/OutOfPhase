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
#if VECTOR
using System.Numerics;
#endif
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        /* algorithmic oscillator template information record */
        public class AlgorithmicTemplateRec : IOscillatorTemplate
        {
            /* values for scaling the frequency of something.  if we were really serious about */
            /* this, we'd traverse all of the oscillators with integral multiples or harmonic */
            /* fractions of the pitch & set their differentials to the same precision as the */
            /* worst oscillator so that they would all stay in sync as time progressed. */
            public double FrequencyMultiplier;
            /* this is added after the frequency multiplier is applied */
            public double FrequencyAdder;

            /* envelope templates */
            public EnvelopeRec LoudnessEnvelopeTemplate;
            public LFOListSpecRec LoudnessLFOTemplate;
            public EnvelopeRec IndexEnvelopeTemplate;
            public LFOListSpecRec IndexLFOTemplate;

            /* miscellaneous control parameters */
            public double StereoBias;
            public double TimeDisplacement;
            public double OverallOscillatorLoudness;

            /* template for the pitch displacement LFO */
            public LFOListSpecRec PitchLFOTemplate;

            /* effect specifier, may be null */
            public EffectSpecListRec OscEffectTemplate;

            /* the algorithm being implemented */
            public OscAlgoType Algorithm;


            /* create a new algorithmic template */
            public static AlgorithmicTemplateRec NewAlgorithmicTemplate(
                OscillatorRec Oscillator,
                SynthParamRec SynthParams)
            {
#if DEBUG
                if (OscillatorGetWhatKindItIs(Oscillator) != OscillatorTypes.eOscillatorAlgorithm)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                AlgorithmicTemplateRec Template = new AlgorithmicTemplateRec();

                Template.OverallOscillatorLoudness = OscillatorGetOutputLoudness(Oscillator);

                /* it might be better to handle divisor and multiplier separately -- we would */
                /* want to do that if we were trying to guarantee that all harmonic */
                /* oscillators ran in lock-step */
                Template.FrequencyMultiplier = OscillatorGetFrequencyMultiplier(Oscillator)
                    / OscillatorGetFrequencyDivisor(Oscillator);
                Template.FrequencyAdder = OscillatorGetFrequencyAdder(Oscillator);

                Template.StereoBias = OscillatorGetStereoBias(Oscillator);
                Template.TimeDisplacement = OscillatorGetTimeDisplacement(Oscillator);
                Template.Algorithm = GetOscillatorAlgorithm(Oscillator);

                /* these are just references */
                Template.LoudnessEnvelopeTemplate = OscillatorGetLoudnessEnvelope(Oscillator);
                Template.LoudnessLFOTemplate = OscillatorGetLoudnessLFOList(Oscillator);
                Template.IndexEnvelopeTemplate = OscillatorGetExcitationEnvelope(Oscillator);
                Template.IndexLFOTemplate = OscillatorGetExcitationLFOList(Oscillator);

                /* more references */
                Template.PitchLFOTemplate = GetOscillatorFrequencyLFOList(Oscillator);
                Template.OscEffectTemplate = GetOscillatorEffectList(Oscillator);
                if (GetEffectSpecListLength(Template.OscEffectTemplate) == 0)
                {
                    Template.OscEffectTemplate = null;
                }

                return Template;
            }

            private static AlgorithmicGenSamplesMethod _Wave_Pulse = AlgorithmicStateRec.Wave_Pulse;
            private static AlgorithmicGenSamplesMethod _Wave_Ramp = AlgorithmicStateRec.Wave_Ramp;

            /* create a new algorithmic state object. */
            public SynthErrorCodes NewState(
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
                out IOscillator StateOut)
            {
                AlgorithmicTemplateRec Template = this;

                int OnePreOrigin;

                PreOriginTimeOut = 0;
                StateOut = null;

                AlgorithmicStateRec State = New(ref SynthParams.freelists.algorithmicStateFreeList);

                // all fields must be assigned: State

                State.WaveTableSamplePositionDifferential_FracI = 0;

                State.Template = Template;

                int MaxPreOrigin = 0;

                //State.WaveTableSamplePosition = new Fixed64(0);
                State.WaveTableSamplePosition_FracI = 0;
                /* State.WaveTableSamplePositionDifferential specified in separate call */

                State.NoteLoudnessScaling = Loudness * Template.OverallOscillatorLoudness;

                State.Algorithm = Template.Algorithm;

                State.PreStartCountdown = (int)((Template.TimeDisplacement * SynthParams.dEnvelopeRate) + 0.5);
                if (-State.PreStartCountdown > MaxPreOrigin)
                {
                    MaxPreOrigin = -State.PreStartCountdown;
                }

                /* State.WaveTableIndex determined by envelope update */
                State.AlgorithmicIndexEnvelope = NewEnvelopeStateRecord(
                    Template.IndexEnvelopeTemplate,
                    ref Accents,
                    InitialFrequency,
                    1,
                    HurryUp,
                    out OnePreOrigin,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }
                State.IndexLFOGenerator = NewLFOGenerator(
                    Template.IndexLFOTemplate,
                    out OnePreOrigin,
                    ref Accents,
                    InitialFrequency,
                    HurryUp,
                    1,
                    1,
                    FreqForMultisampling,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }
                State.PreviousIndex = 0;
                State.Index = 0;

                /* State.MonoLoudness, State.LeftLoudness, State.RightLoudness */
                /* are determined by the envelope update */
                StereoPosition += Template.StereoBias;
                if (StereoPosition < -1)
                {
                    StereoPosition = -1;
                }
                else if (StereoPosition > 1)
                {
                    StereoPosition = 1;
                }
                State.Panning = (float)StereoPosition;
                State.AlgorithmicLoudnessEnvelope = NewEnvelopeStateRecord(
                    Template.LoudnessEnvelopeTemplate,
                    ref Accents,
                    InitialFrequency,
                    1,
                    HurryUp,
                    out OnePreOrigin,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }
                State.LoudnessLFOGenerator = NewLFOGenerator(
                    Template.LoudnessLFOTemplate,
                    out OnePreOrigin,
                    ref Accents,
                    InitialFrequency,
                    HurryUp,
                    1,
                    1,
                    FreqForMultisampling,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }
                State.Loudness = 0;
                State.PreviousLoudness = 0;

                State.PitchLFO = NewLFOGenerator(
                    Template.PitchLFOTemplate,
                    out OnePreOrigin,
                    ref Accents,
                    InitialFrequency,
                    HurryUp,
                    PitchDisplacementDepthLimit,
                    PitchDisplacementRateLimit,
                    FreqForMultisampling,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }
                State.PitchLFOStartCountdown = PitchDisplacementStartPoint;

                State.OscEffectGenerator = null;
                if (Template.OscEffectTemplate != null)
                {
                    SynthErrorCodes Result = NewOscEffectGenerator(
                        Template.OscEffectTemplate,
                        ref Accents,
                        HurryUp,
                        InitialFrequency,
                        FreqForMultisampling,
                        out OnePreOrigin,
                        TrackInfo,
                        SynthParams,
                        out State.OscEffectGenerator);
                    if (Result != SynthErrorCodes.eSynthDone)
                    {
                        return Result;
                    }
                    if (OnePreOrigin > MaxPreOrigin)
                    {
                        MaxPreOrigin = OnePreOrigin;
                    }
                }

                PreOriginTimeOut = MaxPreOrigin;
                StateOut = State;
                return SynthErrorCodes.eSynthDone;
            }

            public OscillatorTypes Type { get { return OscillatorTypes.eOscillatorAlgorithm; } }
        }

        public delegate void AlgorithmicGenSamplesMethod(
            AlgorithmicStateRec State,
            int nActualFrames,
            float[] workspace,
            int lOffset,
            int rOffset,
            int loudnessWorkspaceOffset,
            int indexWorkspaceOffset,
            SynthParamRec SynthParams);

        /* algorithmic oscillator state record */
        public class AlgorithmicStateRec : IOscillator
        {
            /* current sample position into the wave table */
            //public Fixed64 WaveTableSamplePosition; /* 64-bit fixed point */
            // strength reduction (only FracI is used):
            public uint WaveTableSamplePosition_FracI;
            /* current increment value for the wave table sample position */
            //public Fixed64 WaveTableSamplePositionDifferential; /* 64-bit fixed point */
            // strength reduction (only FracI is used):
            public uint WaveTableSamplePositionDifferential_FracI;

            /* envelope tick countdown for pre-start time */
            public int PreStartCountdown;

            /* the algorithm being implemented */
            public OscAlgoType Algorithm;

            /* envelope controlling algorithmic index */
            public EvalEnvelopeRec AlgorithmicIndexEnvelope;
            /* LFO generators modifying the output of the index envelope generator */
            public LFOGenRec IndexLFOGenerator;

            /* index */
            public double Index;
            public double PreviousIndex;

            // loudness envelope
            public float Loudness;
            public float PreviousLoudness;
            /* panning position for splitting envelope generator into stereo channels */
            /* 0 = left channel, 0.5 = middle, 1 = right channel */
            public float Panning;
            /* envelope that is generating the loudness information */
            public EvalEnvelopeRec AlgorithmicLoudnessEnvelope;
            /* LFO generators modifying the output of the loudness envelope generator */
            public LFOGenRec LoudnessLFOGenerator;

            /* this field contains the overall volume scaling for everything so that we */
            /* can treat the envelopes as always going between 0 and 1. */
            public double NoteLoudnessScaling;

            /* this calculates the differential values for periodic pitch displacements */
            public LFOGenRec PitchLFO;
            /* pitch lfo startup counter; negative = expired */
            public int PitchLFOStartCountdown;

            /* postprocessing for this oscillator; may be null */
            public OscEffectGenRec OscEffectGenerator;

            /* static information for the algorithmic oscillator */
            public AlgorithmicTemplateRec Template;


            /* perform one envelope update cycle, and set a new frequency for an algorithmic */
            /* state object.  used for portamento and modulation of frequency (vibrato) */
            public SynthErrorCodes UpdateEnvelopes(
                double NewFrequencyHertz,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;
                AlgorithmicStateRec State = this;

                double Differential;

                if (State.PitchLFOStartCountdown > 0)
                {
                    State.PitchLFOStartCountdown -= 1;
                }
                else
                {
                    /* do some pitch stuff */
                    error = SynthErrorCodes.eSynthDone;
                    NewFrequencyHertz = LFOGenUpdateCycle(
                        State.PitchLFO,
                        NewFrequencyHertz,
                        NewFrequencyHertz,
                        SynthParams,
                        ref error);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }
                NewFrequencyHertz = NewFrequencyHertz * State.Template.FrequencyMultiplier + State.Template.FrequencyAdder;
                Differential = NewFrequencyHertz / SynthParams.dSamplingRate;
                //State.WaveTableSamplePositionDifferential = new Fixed64(Differential);
                // strength reduction:
                Fixed64 LocalWaveTableSamplePositionDifferential = new Fixed64(Differential);
                State.WaveTableSamplePositionDifferential_FracI = LocalWaveTableSamplePositionDifferential.FracI;

                /* this is for the benefit of resampling only -- envelope generators do their */
                /* own pre-origin sequencing */
                if (State.PreStartCountdown > 0)
                {
                    State.PreStartCountdown -= 1;
                }

                error = SynthErrorCodes.eSynthDone;
                State.PreviousIndex = State.Index;
                State.Index = LFOGenUpdateCycle(
                    State.IndexLFOGenerator,
                    EnvelopeUpdate(
                        State.AlgorithmicIndexEnvelope,
                        NewFrequencyHertz,
                        SynthParams,
                        ref error),
                    NewFrequencyHertz,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }
                if (State.Index < 0)
                {
                    State.Index = 0;
                }
                else if (State.Index > 1)
                {
                    State.Index = 1;
                }

                error = SynthErrorCodes.eSynthDone;
                State.PreviousLoudness = State.Loudness;
                State.Loudness = (float)(State.NoteLoudnessScaling *
                    LFOGenUpdateCycle(
                        State.LoudnessLFOGenerator,
                        EnvelopeUpdate(
                            State.AlgorithmicLoudnessEnvelope,
                            NewFrequencyHertz,
                            SynthParams,
                            ref error),
                        NewFrequencyHertz,
                        SynthParams,
                        ref error));
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                if (State.OscEffectGenerator != null)
                {
                    error = OscEffectGeneratorUpdateEnvelopes(
                        State.OscEffectGenerator,
                        NewFrequencyHertz,
                        SynthParams);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            /* fix up pre-origin time for the algorithmic state object */
            public void FixUpPreOrigin(
                int ActualPreOrigin)
            {
                AlgorithmicStateRec State = this;

                EnvelopeStateFixUpInitialDelay(
                    State.AlgorithmicIndexEnvelope,
                    ActualPreOrigin);
                EnvelopeStateFixUpInitialDelay(
                    State.AlgorithmicLoudnessEnvelope,
                    ActualPreOrigin);
                LFOGeneratorFixEnvelopeOrigins(
                    State.IndexLFOGenerator,
                    ActualPreOrigin);
                LFOGeneratorFixEnvelopeOrigins(
                    State.LoudnessLFOGenerator,
                    ActualPreOrigin);
                LFOGeneratorFixEnvelopeOrigins(
                    State.PitchLFO,
                    ActualPreOrigin);
                if (State.OscEffectGenerator != null)
                {
                    FixUpOscEffectGeneratorPreOrigin(
                        State.OscEffectGenerator,
                        ActualPreOrigin);
                }

                State.PreStartCountdown += ActualPreOrigin;

                // initial value for envelope smoothing
                State.PreviousLoudness = (float)(State.NoteLoudnessScaling *
                    LFOGenInitialValue(
                        State.LoudnessLFOGenerator,
                        EnvelopeInitialValue(
                           State.AlgorithmicLoudnessEnvelope)));

                // initial value for envelope smoothing
                State.Index = LFOGenInitialValue(
                    State.IndexLFOGenerator,
                    EnvelopeInitialValue(
                       State.AlgorithmicIndexEnvelope));
                if (State.Index < 0)
                {
                    State.Index = 0;
                }
                else if (State.Index > 1)
                {
                    State.Index = 1;
                }
            }

            /* send a key-up signal to one of the oscillators */
            public void KeyUpSustain1()
            {
                AlgorithmicStateRec State = this;

                LFOGeneratorKeyUpSustain1(
                    State.IndexLFOGenerator);
                LFOGeneratorKeyUpSustain1(
                    State.LoudnessLFOGenerator);
                EnvelopeKeyUpSustain1(
                    State.AlgorithmicIndexEnvelope);
                EnvelopeKeyUpSustain1(
                    State.AlgorithmicLoudnessEnvelope);
                LFOGeneratorKeyUpSustain1(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain1(
                        State.OscEffectGenerator);
                }
            }

            /* send a key-up signal to one of the oscillators */
            public void KeyUpSustain2()
            {
                AlgorithmicStateRec State = this;

                LFOGeneratorKeyUpSustain2(
                    State.IndexLFOGenerator);
                LFOGeneratorKeyUpSustain2(
                    State.LoudnessLFOGenerator);
                EnvelopeKeyUpSustain2(
                    State.AlgorithmicIndexEnvelope);
                EnvelopeKeyUpSustain2(
                    State.AlgorithmicLoudnessEnvelope);
                LFOGeneratorKeyUpSustain2(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain2(
                        State.OscEffectGenerator);
                }
            }

            /* send a key-up signal to one of the oscillators */
            public void KeyUpSustain3()
            {
                AlgorithmicStateRec State = this;

                LFOGeneratorKeyUpSustain3(
                    State.IndexLFOGenerator);
                LFOGeneratorKeyUpSustain3(
                    State.LoudnessLFOGenerator);
                EnvelopeKeyUpSustain3(
                    State.AlgorithmicIndexEnvelope);
                EnvelopeKeyUpSustain3(
                    State.AlgorithmicLoudnessEnvelope);
                LFOGeneratorKeyUpSustain3(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain3(
                        State.OscEffectGenerator);
                }
            }

            /* restart an algorithmic oscillator.  this is used for tie continuations */
            public void Restart(
                ref AccentRec NewAccents,
                double NewLoudness,
                double NewHurryUp,
                bool RetriggerEnvelopes,
                double NewStereoPosition,
                double NewInitialFrequency,
                double PitchDisplacementDepthLimit,
                double PitchDisplacementRateLimit,
                SynthParamRec SynthParams)
            {
                AlgorithmicStateRec State = this;

                NewStereoPosition += State.Template.StereoBias;
                if (NewStereoPosition < -1)
                {
                    NewStereoPosition = -1;
                }
                else if (NewStereoPosition > 1)
                {
                    NewStereoPosition = 1;
                }
                State.Panning = (float)NewStereoPosition;

                State.NoteLoudnessScaling = NewLoudness * State.Template.OverallOscillatorLoudness;

                EnvelopeRetriggerFromOrigin(
                    State.AlgorithmicIndexEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    RetriggerEnvelopes,
                    SynthParams);
                EnvelopeRetriggerFromOrigin(
                    State.AlgorithmicLoudnessEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    RetriggerEnvelopes,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    State.IndexLFOGenerator,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    RetriggerEnvelopes,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    State.LoudnessLFOGenerator,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    RetriggerEnvelopes,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    State.PitchLFO,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    PitchDisplacementDepthLimit,
                    PitchDisplacementRateLimit,
                    RetriggerEnvelopes,
                    SynthParams);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectGeneratorRetriggerFromOrigin(
                        State.OscEffectGenerator,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        RetriggerEnvelopes,
                        SynthParams);
                }
                /* do not reset PitchLFOStartCountdown since we can't give it a proper value */
                /* to do the expected thing, and we'll be interrupting the phase of the LFO */
                /* wave generator */
            }

            /* generate one sequence of samples */
            public SynthErrorCodes Generate(
                int nActualFrames,
                float[] workspace,
                int RawBufferLOffset,
                int RawBufferROffset,
                int PrivateWorkspaceLOffset,
                int PrivateWorkspaceROffset,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error = SynthErrorCodes.eSynthDone;

                AlgorithmicStateRec State = this;

#if DEBUG
                Debug.Assert(!SynthParams.ScratchWorkspace1InUse);
                SynthParams.ScratchWorkspace1InUse = true;
#endif
                int loudnessWorkspaceOffset = SynthParams.ScratchWorkspace1LOffset;
                int indexWorkspaceOffset = SynthParams.ScratchWorkspace1ROffset;

                if (State.PreStartCountdown <= 0)
                {
                    int leftTarget;
                    int rightTarget;

                    if (State.OscEffectGenerator != null)
                    {
                        // effect postprocessing case

                        FloatVectorZero(
                            workspace,
                            PrivateWorkspaceLOffset,
                            nActualFrames);
                        FloatVectorZero(
                            workspace,
                            PrivateWorkspaceROffset,
                            nActualFrames);

                        leftTarget = PrivateWorkspaceLOffset;
                        rightTarget = PrivateWorkspaceROffset;
                    }
                    else
                    {
                        // simple case

                        leftTarget = RawBufferLOffset;
                        rightTarget = RawBufferROffset;
                    }

                    switch (State.Algorithm)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case OscAlgoType.eOscAlgoPulse:
                            Wave_Pulse(
                                State,
                                nActualFrames,
                                workspace,
                                leftTarget,
                                rightTarget,
                                loudnessWorkspaceOffset,
                                indexWorkspaceOffset,
                                SynthParams);
                            break;
                        case OscAlgoType.eOscAlgoRamp:
                            Wave_Ramp(
                                State,
                                nActualFrames,
                                workspace,
                                leftTarget,
                                rightTarget,
                                loudnessWorkspaceOffset,
                                indexWorkspaceOffset,
                                SynthParams);
                            break;
                    }

#if DEBUG
                    SynthParams.ScratchWorkspace1InUse = false;
#endif
                    if (State.OscEffectGenerator != null)
                    {
                        // effect postprocessing case

                        error = ApplyOscEffectGenerator(
                            State.OscEffectGenerator,
                            workspace,
                            PrivateWorkspaceLOffset,
                            PrivateWorkspaceROffset,
                            nActualFrames,
                            SynthParams);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            goto Error;
                        }

                        FloatVectorAcc(
                            workspace,
                            PrivateWorkspaceLOffset,
                            workspace,
                            RawBufferLOffset,
                            nActualFrames);
                        FloatVectorAcc(
                            workspace,
                            PrivateWorkspaceROffset,
                            workspace,
                            RawBufferROffset,
                            nActualFrames);
                    }
                }


            Error:

#if DEBUG
                SynthParams.ScratchWorkspace1InUse = false;
#endif

                return error;
            }


            public static void Wave_Ramp(
                AlgorithmicStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset,
                int indexWorkspaceOffset,
                SynthParamRec SynthParams)
            {
                if (Program.Config.EnableEnvelopeSmoothing
                    // case of no motion in either smoothed axes can use fast code path
                    && ((State.Loudness != State.PreviousLoudness)
                    || (State.Index != State.PreviousIndex)))
                {
                    // envelope smoothing

                    if (State.Index != State.PreviousIndex)
                    {
                        // loudness and index variant

                        Wave_Ramp_LoudnessVariant_IndexVariant(
                            State,
                            nActualFrames,
                            workspace,
                            lOffset,
                            rOffset,
                            loudnessWorkspaceOffset,
                            indexWorkspaceOffset,
                            SynthParams);
                    }
                    else
                    {
                        // loudness variant, index invariant

                        Wave_Ramp_LoudnessVariant_IndexInvariant(
                            State,
                            nActualFrames,
                            workspace,
                            lOffset,
                            rOffset,
                            loudnessWorkspaceOffset,
                            indexWorkspaceOffset,
                            SynthParams);
                    }
                }
                else
                {
                    // no envelope smoothing

                    Wave_Ramp_LoudnessInvariant_IndexInvariant(
                        State,
                        nActualFrames,
                        workspace,
                        lOffset,
                        rOffset,
                        loudnessWorkspaceOffset,
                        indexWorkspaceOffset,
                        SynthParams);
                }
            }

            public static void Wave_Ramp_LoudnessVariant_IndexVariant(
                AlgorithmicStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset,
                int indexWorkspaceOffset,
                SynthParamRec SynthParams)
            {
                /* load local values */
                float LocalLoudness = State.Loudness;
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                //Fixed64 LocalWaveTableSamplePosition = State.WaveTableSamplePosition;
                //Fixed64 LocalWaveTableSamplePositionDifferential = State.WaveTableSamplePositionDifferential;
                // strength reduction:
                uint LocalWaveTableSamplePosition_FracI = State.WaveTableSamplePosition_FracI;
                uint LocalWaveTableSamplePositionDifferential_FracI = State.WaveTableSamplePositionDifferential_FracI;

                Debug.Assert(Program.Config.EnableEnvelopeSmoothing
                    /*&& (State.Loudness != State.PreviousLoudness)*/
                    && (State.Index != State.PreviousIndex));

                // envelope smoothing

                float LocalPreviousLoudness = State.PreviousLoudness;

                // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                // (because index is variant, we still must use this codepath)
                if (IsLFOSampleAndHold(State.LoudnessLFOGenerator))
                {
                    LocalPreviousLoudness = LocalLoudness;
                }

                if (!EnvelopeCurrentSegmentExponential(State.AlgorithmicLoudnessEnvelope))
                {
                    FloatVectorAdditiveRecurrence(
                        workspace,
                        loudnessWorkspaceOffset,
                        LocalPreviousLoudness,
                        LocalLoudness,
                        nActualFrames);
                }
                else
                {
                    FloatVectorMultiplicativeRecurrence(
                        workspace,
                        loudnessWorkspaceOffset,
                        LocalPreviousLoudness,
                        LocalLoudness,
                        nActualFrames);
                }

                int i = 0;

                // loudness and index variant

                double LocalPreviousIndex = State.PreviousIndex;
                double LocalIndex = State.Index;

                // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                // (if this is true and also for loudness, we could use a faster code path, but it's an uncommon
                // case and adds more cost and complexity in loop selection, so hasn't been optimized)
                if (IsLFOSampleAndHold(State.IndexLFOGenerator))
                {
                    LocalPreviousIndex = LocalIndex;
                }

                if (!EnvelopeCurrentSegmentExponential(State.AlgorithmicIndexEnvelope))
                {
                    FloatVectorAdditiveRecurrence(
                        workspace,
                        indexWorkspaceOffset,
                        (float)LocalPreviousIndex * (float)Fixed64.FIXED64_WHOLE,
                        (float)LocalIndex * (float)Fixed64.FIXED64_WHOLE,
                        nActualFrames);
                }
                else
                {
                    FloatVectorMultiplicativeRecurrence(
                        workspace,
                        indexWorkspaceOffset,
                        (float)LocalPreviousIndex * (float)Fixed64.FIXED64_WHOLE,
                        (float)LocalIndex * (float)Fixed64.FIXED64_WHOLE,
                        nActualFrames);
                }

                const float MinFloatOverZero = 1; // in Fixed64 units
                float MaxFloatLessThan1 = UnsafeScalarCast.AsFloat(UnsafeScalarCast.AsInt((float)Fixed64.FIXED64_WHOLE) - 1);
                FloatVectorClamp(
                    workspace,
                    indexWorkspaceOffset,
                    MinFloatOverZero,
                    MaxFloatLessThan1,
                    nActualFrames);

                // TODO: vectorize

                for (; i < nActualFrames; i++)
                {
                    float OrigLeft = workspace[i + lOffset];
                    float OrigRight = workspace[i + rOffset];

                    float LocalIndex_FracI = workspace[i + indexWorkspaceOffset];

                    /* compute triangle function */
                    float X;
                    if (LocalWaveTableSamplePosition_FracI <= LocalIndex_FracI)
                    {
                        /* rising cycle */
                        float First = LocalIndex_FracI;
                        float TwoTimesFactorOverFirst = (float)(2 / First);
                        X = -1 + LocalWaveTableSamplePosition_FracI * TwoTimesFactorOverFirst;
                    }
                    else
                    {
                        /* falling cycle */
                        // X = 1 - (LocalWaveTableSamplePosition.FracI - LocalIndex.FracI) * TwoTimesFactorOverSecond;
                        // algebraically rewritten:
                        float Second = (float)Fixed64.FIXED64_WHOLE - LocalIndex_FracI;
                        float TwoTimesFactorOverSecond = (float)(2 / Second);
                        float NegTwoTimesFactorOverSecond = -TwoTimesFactorOverSecond;
                        float SecondOffset = 1 + LocalIndex_FracI * TwoTimesFactorOverSecond;
                        X = SecondOffset + LocalWaveTableSamplePosition_FracI * NegTwoTimesFactorOverSecond;
                    }

                    LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                    workspace[i + lOffset] = OrigLeft + X * LocalLoudness * LeftPan;
                    workspace[i + rOffset] = OrigRight + X * LocalLoudness * RightPan;

                    //LocalWaveTableSamplePosition += LocalWaveTableSamplePositionDifferential;
                    LocalWaveTableSamplePosition_FracI = unchecked(LocalWaveTableSamplePosition_FracI
                        + LocalWaveTableSamplePositionDifferential_FracI);
                }

                /* save local state back to record */
                //State.WaveTableSamplePosition = LocalWaveTableSamplePosition;
                State.WaveTableSamplePosition_FracI = LocalWaveTableSamplePosition_FracI;
            }

            public static void Wave_Ramp_LoudnessVariant_IndexInvariant(
                AlgorithmicStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset,
                int indexWorkspaceOffset,
                SynthParamRec SynthParams)
            {
                /* load local values */
                float LocalLoudness = State.Loudness;
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                //Fixed64 LocalWaveTableSamplePosition = State.WaveTableSamplePosition;
                //Fixed64 LocalWaveTableSamplePositionDifferential = State.WaveTableSamplePositionDifferential;
                // strength reduction:
                uint LocalWaveTableSamplePosition_FracI = State.WaveTableSamplePosition_FracI;
                uint LocalWaveTableSamplePositionDifferential_FracI = State.WaveTableSamplePositionDifferential_FracI;

                Debug.Assert(Program.Config.EnableEnvelopeSmoothing
                    && (State.Loudness != State.PreviousLoudness)
                    && (State.Index == State.PreviousIndex));

                // envelope smoothing

                float LocalPreviousLoudness = State.PreviousLoudness;

                // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                if (IsLFOSampleAndHold(State.LoudnessLFOGenerator))
                {
                    LocalPreviousLoudness = LocalLoudness;
                }

                if (!EnvelopeCurrentSegmentExponential(State.AlgorithmicLoudnessEnvelope))
                {
                    FloatVectorAdditiveRecurrence(
                        workspace,
                        loudnessWorkspaceOffset,
                        LocalPreviousLoudness,
                        LocalLoudness,
                        nActualFrames);
                }
                else
                {
                    FloatVectorMultiplicativeRecurrence(
                        workspace,
                        loudnessWorkspaceOffset,
                        LocalPreviousLoudness,
                        LocalLoudness,
                        nActualFrames);
                }

                int i = 0;

                // loudness variant, index invariant

                //Fixed64 LocalIndex = State.Index64;
                // strength reduction:
                Fixed64 LocalIndex64 = new Fixed64(State.Index);
                uint LocalIndex_FracI = LocalIndex64.FracI;

                /* eliminate need for 2nd compare for 1.0 case */
                if (LocalIndex64.Int != 0)
                {
                    LocalIndex_FracI = 0xffffffff; // approximate by clamping at largest representable number
                }

                /* computed stuff */
                float First = (float)State.Index;
                float Second = 1 - First;
                float TwoTimesFactorOverFirst = 0;
                if (First > 0)
                {
                    TwoTimesFactorOverFirst = (float)(2 * (1d / (double)Fixed64.FIXED64_WHOLE) / First);
                }
                float TwoTimesFactorOverSecond = 0;
                if (Second > 0)
                {
                    TwoTimesFactorOverSecond = (float)(2 * (1d / (double)Fixed64.FIXED64_WHOLE) / Second);
                }
                float NegTwoTimesFactorOverSecond = -TwoTimesFactorOverSecond;
                float SecondOffset = 1 + LocalIndex_FracI * TwoTimesFactorOverSecond;

                // TODO: vectorize

                for (; i < nActualFrames; i++)
                {
                    float OrigLeft = workspace[i + lOffset];
                    float OrigRight = workspace[i + rOffset];

                    /* compute triangle function */
                    float X;
                    if (LocalWaveTableSamplePosition_FracI <= LocalIndex_FracI)
                    {
                        /* rising cycle */
                        X = -1 + LocalWaveTableSamplePosition_FracI * TwoTimesFactorOverFirst;
                    }
                    else
                    {
                        /* falling cycle */
                        // X = 1 - (LocalWaveTableSamplePosition.FracI - LocalIndex.FracI) * TwoTimesFactorOverSecond;
                        // algebraically rewritten:
                        X = SecondOffset + LocalWaveTableSamplePosition_FracI * NegTwoTimesFactorOverSecond;
                    }

                    LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                    workspace[i + lOffset] = OrigLeft + X * LocalLoudness * LeftPan;
                    workspace[i + rOffset] = OrigRight + X * LocalLoudness * RightPan;

                    //LocalWaveTableSamplePosition += LocalWaveTableSamplePositionDifferential;
                    LocalWaveTableSamplePosition_FracI = unchecked(LocalWaveTableSamplePosition_FracI
                        + LocalWaveTableSamplePositionDifferential_FracI);
                }

                /* save local state back to record */
                //State.WaveTableSamplePosition = LocalWaveTableSamplePosition;
                State.WaveTableSamplePosition_FracI = LocalWaveTableSamplePosition_FracI;
            }

            public static void Wave_Ramp_LoudnessInvariant_IndexInvariant(
                AlgorithmicStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset,
                int indexWorkspaceOffset,
                SynthParamRec SynthParams)
            {
                /* load local values */
                float LocalLoudness = State.Loudness;
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                //Fixed64 LocalWaveTableSamplePosition = State.WaveTableSamplePosition;
                //Fixed64 LocalWaveTableSamplePositionDifferential = State.WaveTableSamplePositionDifferential;
                // strength reduction:
                uint LocalWaveTableSamplePosition_FracI = State.WaveTableSamplePosition_FracI;
                uint LocalWaveTableSamplePositionDifferential_FracI = State.WaveTableSamplePositionDifferential_FracI;

                Debug.Assert(!Program.Config.EnableEnvelopeSmoothing
                    || ((State.Loudness == State.PreviousLoudness)
                    && (State.Index == State.PreviousIndex)));

                // no envelope smoothing

                float LocalLeftLoudness = LeftPan * LocalLoudness;
                float LocalRightLoudness = RightPan * LocalLoudness;

                //Fixed64 LocalIndex = State.Index64;
                // strength reduction:
                Fixed64 LocalIndex64 = new Fixed64(State.Index);
                uint LocalIndex_FracI = LocalIndex64.FracI;

                /* eliminate need for 2nd compare for 1.0 case */
                if (LocalIndex64.Int != 0)
                {
                    LocalIndex_FracI = 0xffffffff; // approximate by clamping at largest representable number
                }

                /* computed stuff */
                float First = (float)State.Index;
                float Second = 1 - First;
                float TwoTimesFactorOverFirst = 0;
                if (First > 0)
                {
                    TwoTimesFactorOverFirst = (float)(2 * (1d / (double)Fixed64.FIXED64_WHOLE) / First);
                }
                float TwoTimesFactorOverSecond = 0;
                if (Second > 0)
                {
                    TwoTimesFactorOverSecond = (float)(2 * (1d / (double)Fixed64.FIXED64_WHOLE) / Second);
                }
                float NegTwoTimesFactorOverSecond = -TwoTimesFactorOverSecond;
                float SecondOffset = 1 + LocalIndex_FracI * TwoTimesFactorOverSecond;

                int i = 0;

#if DEBUG
                AssertVectorAligned(workspace, lOffset);
                AssertVectorAligned(workspace, rOffset);
#endif
#if VECTOR
                if (EnableVector && (nActualFrames >= 2 * Vector<float>.Count))
                {
                    // Vector API currently lacks uint==>float conversion, so to work around, the wave phase recurrence
                    // is computed as float throughout. This has two problems:
                    // 1. reduction of precision vs. historical implementation using 32-bit uint
                    // 2. lack of robustness regarding wrap-around. The recurrence update checks for overflow and
                    //    subtracts 1 (but it is sufficient for a well-behaved score).
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                    float[] fb = SynthParams.workspace;
                    int fOffset = SynthParams.VectorWorkspace2Offset;
#else
                    uint[] uib = UnsafeArrayCast.AsUints(SynthParams.workspace);
                    int uiOffset = SynthParams.VectorWorkspace1Offset;
#endif
                    for (int j = 0; j < Vector<int>.Count; j++)
                    {
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                        fb[j + fOffset] = LocalWaveTableSamplePosition_FracI;
#else
                        uib[j + uiOffset] = LocalWaveTableSamplePosition_FracI;
#endif
                        LocalWaveTableSamplePosition_FracI = unchecked(LocalWaveTableSamplePosition_FracI
                            + LocalWaveTableSamplePositionDifferential_FracI);
                    }
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                    Vector<float> vPosF = new Vector<float>(fb, fOffset);
#else
                    Vector<uint> vPos = new Vector<uint>(uib, uiOffset);
#endif
                    Vector<float> vFirstOffset = new Vector<float>(-1);
                    Vector<float> vSecondOffset = new Vector<float>(SecondOffset);
                    Vector<float> vTwoTimesFactorOverFirst = new Vector<float>(TwoTimesFactorOverFirst);
                    Vector<float> vNegTwoTimesFactorOverSecond = new Vector<float>(NegTwoTimesFactorOverSecond);
                    Vector<float> vLocalLeftLoudness = new Vector<float>(LocalLeftLoudness);
                    Vector<float> vLocalRightLoudness = new Vector<float>(LocalRightLoudness);
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                    Vector<float> vLocalIndexFracF = new Vector<float>((float)LocalIndex_FracI);
                    Vector<float> vDifferentialF = new Vector<float>((float)(Vector<uint>.Count * LocalWaveTableSamplePositionDifferential_FracI));
                    Vector<float> vRolloverLimit = new Vector<float>((float)Fixed64.FIXED64_WHOLE);
#else
                    Vector<uint> vLocalIndexFracI = new Vector<uint>(LocalIndex_FracI);
                    Vector<uint> vDifferential = new Vector<uint>(Vector<uint>.Count * LocalWaveTableSamplePositionDifferential_FracI);
#endif
                    for (; i <= nActualFrames - Vector<float>.Count; i += Vector<float>.Count)
                    {
                        /* compute triangle function */
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                        Vector<int> selector = Vector.LessThan(vPosF, vLocalIndexFracF);
#else
                        Vector<int> selector = Vector.AsVectorInt32(Vector.LessThan(vPos, vLocalIndexFracI));
#endif
                        Vector<float> X =
                            Vector.ConditionalSelect(
                                selector,
                                vFirstOffset,
                                vSecondOffset)
                            +
                            Vector.ConditionalSelect(
                                selector,
                                vTwoTimesFactorOverFirst,
                                vNegTwoTimesFactorOverSecond)
                            // need uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                            *
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                                vPosF;
#else
                                vPos;
#endif

                        Vector<float> l = new Vector<float>(workspace, i + lOffset);
                        Vector<float> r = new Vector<float>(workspace, i + rOffset);
                        l = l + X * vLocalLeftLoudness;
                        r = r + X * vLocalRightLoudness;
                        l.CopyTo(workspace, i + lOffset);
                        r.CopyTo(workspace, i + rOffset);

#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                        vPosF = vPosF + vDifferentialF;
                        vPosF = Vector.ConditionalSelect(
                            Vector.GreaterThanOrEqual(vPosF, vRolloverLimit),
                            vPosF - vRolloverLimit,
                            vPosF);
#else
                        vPos = vPos + vDifferential;
#endif
                    }
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                    LocalWaveTableSamplePosition_FracI = unchecked((uint)vPosF[0]); // capture final-next recurrence value for cleanup loop
#else
                    LocalWaveTableSamplePosition_FracI = vPos[0]; // capture final-next recurrence value for cleanup loop
#endif
                }
#endif

                for (; i < nActualFrames; i++)
                {
                    float OrigLeft = workspace[i + lOffset];
                    float OrigRight = workspace[i + rOffset];

                    /* compute triangle function */
                    float X;
                    if (LocalWaveTableSamplePosition_FracI <= LocalIndex_FracI)
                    {
                        /* rising cycle */
                        X = -1 + LocalWaveTableSamplePosition_FracI * TwoTimesFactorOverFirst;
                    }
                    else
                    {
                        /* falling cycle */
                        // X = 1 - (LocalWaveTableSamplePosition.FracI - LocalIndex.FracI) * TwoTimesFactorOverSecond;
                        // algebraically rewritten:
                        X = SecondOffset + LocalWaveTableSamplePosition_FracI * NegTwoTimesFactorOverSecond;
                    }

                    workspace[i + lOffset] = OrigLeft + X * LocalLeftLoudness;
                    workspace[i + rOffset] = OrigRight + X * LocalRightLoudness;

                    //LocalWaveTableSamplePosition += LocalWaveTableSamplePositionDifferential;
                    LocalWaveTableSamplePosition_FracI = unchecked(LocalWaveTableSamplePosition_FracI
                        + LocalWaveTableSamplePositionDifferential_FracI);
                }

                /* save local state back to record */
                //State.WaveTableSamplePosition = LocalWaveTableSamplePosition;
                State.WaveTableSamplePosition_FracI = LocalWaveTableSamplePosition_FracI;
            }


            public static void Wave_Pulse(
                AlgorithmicStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset,
                int indexWorkspaceOffset,
                SynthParamRec SynthParams)
            {
                if (Program.Config.EnableEnvelopeSmoothing
                    // case of no motion in either smoothed axes can use fast code path
                    && ((State.Loudness != State.PreviousLoudness)
                    || (State.Index != State.PreviousIndex)))
                {
                    // envelope smoothing

                    if (State.Index != State.PreviousIndex)
                    {
                        // loudness and index variant

                        Wave_Pulse_LoudnessVariant_IndexVariant(
                            State,
                            nActualFrames,
                            workspace,
                            lOffset,
                            rOffset,
                            loudnessWorkspaceOffset,
                            indexWorkspaceOffset,
                            SynthParams);
                    }
                    else
                    {
                        // loudness variant, index invariant

                        Wave_Pulse_LoudnessVariant_IndexInvariant(
                            State,
                            nActualFrames,
                            workspace,
                            lOffset,
                            rOffset,
                            loudnessWorkspaceOffset,
                            indexWorkspaceOffset,
                            SynthParams);
                    }
                }
                else
                {
                    // no envelope smoothing

                    Wave_Pulse_LoudnessInvariant_IndexInvariant(
                        State,
                        nActualFrames,
                        workspace,
                        lOffset,
                        rOffset,
                        loudnessWorkspaceOffset,
                        indexWorkspaceOffset,
                        SynthParams);
                }
            }

            public static void Wave_Pulse_LoudnessVariant_IndexVariant(
                AlgorithmicStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset,
                int indexWorkspaceOffset,
                SynthParamRec SynthParams)
            {
                /* load local values */
                float LocalLoudness = State.Loudness;
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                //Fixed64 LocalWaveTableSamplePosition = State.WaveTableSamplePosition;
                //Fixed64 LocalWaveTableSamplePositionDifferential = State.WaveTableSamplePositionDifferential;
                // strength reduction:
                uint LocalWaveTableSamplePosition_FracI = State.WaveTableSamplePosition_FracI;
                uint LocalWaveTableSamplePositionDifferential_FracI = State.WaveTableSamplePositionDifferential_FracI;

                Debug.Assert(Program.Config.EnableEnvelopeSmoothing
                    /*&& (State.Loudness != State.PreviousLoudness)*/
                    && (State.Index != State.PreviousIndex));

                // envelope smoothing

                float LocalPreviousLoudness = State.PreviousLoudness;

                // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                // (because index is variant, we still must use this codepath)
                if (IsLFOSampleAndHold(State.LoudnessLFOGenerator))
                {
                    LocalPreviousLoudness = LocalLoudness;
                }

                if (!EnvelopeCurrentSegmentExponential(State.AlgorithmicLoudnessEnvelope))
                {
                    FloatVectorAdditiveRecurrence(
                        workspace,
                        loudnessWorkspaceOffset,
                        LocalPreviousLoudness,
                        LocalLoudness,
                        nActualFrames);
                }
                else
                {
                    FloatVectorMultiplicativeRecurrence(
                        workspace,
                        loudnessWorkspaceOffset,
                        LocalPreviousLoudness,
                        LocalLoudness,
                        nActualFrames);
                }

                // loudness and index variant

                double LocalPreviousIndex = State.PreviousIndex;
                double LocalIndex = State.Index;

                // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                // (if this is true and also for loudness, we could use a faster code path, but it's an uncommon
                // case and adds more cost and complexity in loop selection, so hasn't been optimized)
                if (IsLFOSampleAndHold(State.IndexLFOGenerator))
                {
                    LocalPreviousIndex = LocalIndex;
                }

                if (!EnvelopeCurrentSegmentExponential(State.AlgorithmicIndexEnvelope))
                {
                    FloatVectorAdditiveRecurrence(
                        workspace,
                        indexWorkspaceOffset,
                        (float)LocalPreviousIndex * (float)Fixed64.FIXED64_WHOLE,
                        (float)LocalIndex * (float)Fixed64.FIXED64_WHOLE,
                        nActualFrames);
                }
                else
                {
                    FloatVectorMultiplicativeRecurrence(
                        workspace,
                        indexWorkspaceOffset,
                        (float)LocalPreviousIndex * (float)Fixed64.FIXED64_WHOLE,
                        (float)LocalIndex * (float)Fixed64.FIXED64_WHOLE,
                        nActualFrames);
                }

                int i = 0;

                // TODO: vectorize

                for (; i < nActualFrames; i++)
                {
                    float OrigLeft = workspace[i + lOffset];
                    float OrigRight = workspace[i + rOffset];

                    // in this variant, using a long, LocalIndex_FracI can be 1.0 (fixed), eliminating the index==1.0
                    // constant special case.
                    long LocalIndex_FracI_Prime = (long)workspace[i + indexWorkspaceOffset];

                    /* compute variable pulse function */
                    float X = 1;
                    if (LocalWaveTableSamplePosition_FracI >= LocalIndex_FracI_Prime)
                    {
                        /* low cycle */
                        X = -1;
                    }

                    LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                    workspace[i + lOffset] = OrigLeft + X * LocalLoudness * LeftPan;
                    workspace[i + rOffset] = OrigRight + X * LocalLoudness * RightPan;

                    LocalWaveTableSamplePosition_FracI = unchecked(LocalWaveTableSamplePosition_FracI
                        + LocalWaveTableSamplePositionDifferential_FracI);
                }

                /* save local state back to record */
                //State.WaveTableSamplePosition = LocalWaveTableSamplePosition;
                State.WaveTableSamplePosition_FracI = LocalWaveTableSamplePosition_FracI;
            }

            public static void Wave_Pulse_LoudnessVariant_IndexInvariant(
                AlgorithmicStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset,
                int indexWorkspaceOffset,
                SynthParamRec SynthParams)
            {
                /* load local values */
                float LocalLoudness = State.Loudness;
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                //Fixed64 LocalWaveTableSamplePosition = State.WaveTableSamplePosition;
                //Fixed64 LocalWaveTableSamplePositionDifferential = State.WaveTableSamplePositionDifferential;
                // strength reduction:
                uint LocalWaveTableSamplePosition_FracI = State.WaveTableSamplePosition_FracI;
                uint LocalWaveTableSamplePositionDifferential_FracI = State.WaveTableSamplePositionDifferential_FracI;

                Debug.Assert(Program.Config.EnableEnvelopeSmoothing
                    && (State.Loudness != State.PreviousLoudness)
                    && (State.Index == State.PreviousIndex));

                // envelope smoothing

                float LocalPreviousLoudness = State.PreviousLoudness;

                // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                if (IsLFOSampleAndHold(State.LoudnessLFOGenerator))
                {
                    LocalPreviousLoudness = LocalLoudness;
                }

                if (!EnvelopeCurrentSegmentExponential(State.AlgorithmicLoudnessEnvelope))
                {
                    FloatVectorAdditiveRecurrence(
                        workspace,
                        loudnessWorkspaceOffset,
                        LocalPreviousLoudness,
                        LocalLoudness,
                        nActualFrames);
                }
                else
                {
                    FloatVectorMultiplicativeRecurrence(
                        workspace,
                        loudnessWorkspaceOffset,
                        LocalPreviousLoudness,
                        LocalLoudness,
                        nActualFrames);
                }

                // loudness variant, index invariant

                // in this variant, using a long, LocalIndex_FracI can be 1.0 (fixed), eliminating the index==1.0
                // constant special case.
                long LocalIndex_FracI_Prime = new Fixed64(State.Index).raw;

                int i = 0;

                // TODO: vectorize

                for (; i < nActualFrames; i++)
                {
                    float OrigLeft = workspace[i + lOffset];
                    float OrigRight = workspace[i + rOffset];

                    /* compute variable pulse function */
                    float X = 1;
                    if (LocalWaveTableSamplePosition_FracI >= LocalIndex_FracI_Prime)
                    {
                        /* low cycle */
                        X = -1;
                    }

                    LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                    workspace[i + lOffset] = OrigLeft + X * LocalLoudness * LeftPan;
                    workspace[i + rOffset] = OrigRight + X * LocalLoudness * RightPan;

                    LocalWaveTableSamplePosition_FracI = unchecked(LocalWaveTableSamplePosition_FracI
                        + LocalWaveTableSamplePositionDifferential_FracI);
                }

                /* save local state back to record */
                //State.WaveTableSamplePosition = LocalWaveTableSamplePosition;
                State.WaveTableSamplePosition_FracI = LocalWaveTableSamplePosition_FracI;
            }

            public static void Wave_Pulse_LoudnessInvariant_IndexInvariant(
                AlgorithmicStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset,
                int indexWorkspaceOffset,
                SynthParamRec SynthParams)
            {
                /* load local values */
                float LocalLoudness = State.Loudness;
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                //Fixed64 LocalWaveTableSamplePosition = State.WaveTableSamplePosition;
                //Fixed64 LocalWaveTableSamplePositionDifferential = State.WaveTableSamplePositionDifferential;
                // strength reduction:
                uint LocalWaveTableSamplePosition_FracI = State.WaveTableSamplePosition_FracI;
                uint LocalWaveTableSamplePositionDifferential_FracI = State.WaveTableSamplePositionDifferential_FracI;

                Debug.Assert(!Program.Config.EnableEnvelopeSmoothing
                    || ((State.Loudness == State.PreviousLoudness)
                    && (State.Index == State.PreviousIndex)));

                // no envelope smoothing

                float LocalLeftLoudness = LeftPan * LocalLoudness;
                float LocalRightLoudness = RightPan * LocalLoudness;

                Fixed64 LocalIndex64 = new Fixed64(State.Index);
                // strength reduction:
                uint LocalIndex_FracI = LocalIndex64.FracI;

                // By using long for LocalIndex_FracI, the 1.0 case could be eliminated. However, the code is retained
                // in present form as a reference, as 64-bit arithmetic was unavailable when originally written.

                if (LocalIndex64.Int == 0)
                {
                    /* everything except the 1.0 case */

                    int i = 0;

#if DEBUG
                    AssertVectorAligned(workspace, lOffset);
                    AssertVectorAligned(workspace, rOffset);
#endif
#if VECTOR
                    if (EnableVector && (nActualFrames >= 2 * Vector<float>.Count))
                    {
                        // Vector API currently lacks uint==>float conversion, so to work around, the wave phase recurrence
                        // is computed as float throughout. This has two problems:
                        // 1. reduction of precision vs. historical implementation using 32-bit uint
                        // 2. lack of robustness regarding wrap-around. The recurrence update checks for overflow and
                        //    subtracts 1 (but it is sufficient for a well-behaved score).
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                        float[] fb = SynthParams.workspace;
                        int fOffset = SynthParams.VectorWorkspace2Offset;
#else
                        uint[] uib = UnsafeArrayCast.AsUints(SynthParams.workspace);
                        int uiOffset = SynthParams.VectorWorkspace1Offset;
#endif
                        for (int j = 0; j < Vector<int>.Count; j++)
                        {
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                            fb[j + fOffset] = LocalWaveTableSamplePosition_FracI;
#else
                            uib[j + uiOffset] = LocalWaveTableSamplePosition_FracI;
#endif
                            LocalWaveTableSamplePosition_FracI = unchecked(LocalWaveTableSamplePosition_FracI
                                + LocalWaveTableSamplePositionDifferential_FracI);
                        }
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                        Vector<float> vPosF = new Vector<float>(fb, fOffset);
#else
                        Vector<uint> vPos = new Vector<uint>(uib, uiOffset);
#endif
                        Vector<float> vOne = new Vector<float>((float)1);
                        Vector<uint> vSignBit = new Vector<uint>((uint)0x80000000);
                        Vector<float> vLocalLeftLoudness = new Vector<float>(LocalLeftLoudness);
                        Vector<float> vLocalRightLoudness = new Vector<float>(LocalRightLoudness);
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                        Vector<float> vLocalIndexFracF = new Vector<float>((float)LocalIndex_FracI);
                        Vector<float> vDifferentialF = new Vector<float>((float)(Vector<uint>.Count * LocalWaveTableSamplePositionDifferential_FracI));
                        Vector<float> vRolloverLimit = new Vector<float>((float)Fixed64.FIXED64_WHOLE);
#else
                        Vector<uint> vLocalIndexFracI = new Vector<uint>(LocalIndex_FracI);
                        Vector<uint> vDifferential = new Vector<uint>(Vector<uint>.Count * LocalWaveTableSamplePositionDifferential_FracI);
#endif
                        for (; i <= nActualFrames - Vector<float>.Count; i += Vector<float>.Count)
                        {
                            /* compute variable pulse function */
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                            Vector<int> selector = Vector.GreaterThanOrEqual(vPosF, vLocalIndexFracF);
#else
                            Vector<int> selector = Vector.AsVectorInt32(Vector.LessThan(vPos, vLocalIndexFracI));
#endif
                            Vector<float> X = Vector.AsVectorSingle((Vector.AsVectorUInt32(selector) & vSignBit) ^ Vector.AsVectorUInt32(vOne));

                            Vector<float> l = new Vector<float>(workspace, i + lOffset);
                            Vector<float> r = new Vector<float>(workspace, i + rOffset);
                            l = l + X * vLocalLeftLoudness;
                            r = r + X * vLocalRightLoudness;
                            l.CopyTo(workspace, i + lOffset);
                            r.CopyTo(workspace, i + rOffset);

#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                            vPosF = vPosF + vDifferentialF;
                            vPosF = Vector.ConditionalSelect(
                                Vector.GreaterThanOrEqual(vPosF, vRolloverLimit),
                                vPosF - vRolloverLimit,
                                vPosF);
#else
                            vPos = vPos + vDifferential;
#endif
                        }
#if true // HACK for missing uint==>float conversion: https://github.com/dotnet/corefx/issues/1605
                        LocalWaveTableSamplePosition_FracI = unchecked((uint)vPosF[0]); // capture final-next recurrence value for cleanup loop
#else
                        LocalWaveTableSamplePosition_FracI = vPos[0]; // capture final-next recurrence value for cleanup loop
#endif
                    }
#endif

                    for (; i < nActualFrames; i++)
                    {
                        float OrigLeft = workspace[i + lOffset];
                        float OrigRight = workspace[i + rOffset];

                        /* compute variable pulse function */
                        float X = 1;
                        if (LocalWaveTableSamplePosition_FracI >= LocalIndex_FracI)
                        {
                            /* low cycle */
                            X = -1;
                        }

                        workspace[i + lOffset] = OrigLeft + X * LocalLeftLoudness;
                        workspace[i + rOffset] = OrigRight + X * LocalRightLoudness;

                        LocalWaveTableSamplePosition_FracI = unchecked(LocalWaveTableSamplePosition_FracI
                            + LocalWaveTableSamplePositionDifferential_FracI);
                    }
                }
                else
                {
                    /* the 1.0 case -- output constant 1 */

                    int i = 0;

                    for (; i < nActualFrames; i++)
                    {
                        float OrigLeft = workspace[i + lOffset];
                        float OrigRight = workspace[i + rOffset];

                        workspace[i + lOffset] = OrigLeft + LocalLeftLoudness;
                        workspace[i + rOffset] = OrigRight + LocalRightLoudness;

                        LocalWaveTableSamplePosition_FracI = unchecked(LocalWaveTableSamplePosition_FracI
                            + LocalWaveTableSamplePositionDifferential_FracI);
                    }
                }

                /* save local state back to record */
                //State.WaveTableSamplePosition = LocalWaveTableSamplePosition;
                State.WaveTableSamplePosition_FracI = LocalWaveTableSamplePosition_FracI;
            }


            /* find out if the algorithmic oscillator has finished */
            public bool IsItFinished()
            {
                AlgorithmicStateRec State = this;

                /* we are finished when one of the following conditions is met: */
                /*  - output volume is zero AND loudness envelope is finished */
                /*  - we are not generating any signal */
                return IsEnvelopeAtEnd(State.AlgorithmicLoudnessEnvelope);
            }

            /* finalize before termination */
            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                AlgorithmicStateRec State = this;

                if (State.OscEffectGenerator != null)
                {
                    FinalizeOscEffectGenerator(
                        State.OscEffectGenerator,
                        SynthParams,
                        writeOutputLogs);
                }

                FreeEnvelopeStateRecord(
                    ref State.AlgorithmicIndexEnvelope,
                    SynthParams);
                FreeLFOGenerator(
                    ref State.IndexLFOGenerator,
                    SynthParams);

                FreeEnvelopeStateRecord(
                    ref State.AlgorithmicLoudnessEnvelope,
                    SynthParams);
                FreeLFOGenerator(
                    ref State.LoudnessLFOGenerator,
                    SynthParams);

                FreeLFOGenerator(
                    ref State.PitchLFO,
                    SynthParams);

                Free(
                    ref SynthParams.freelists.algorithmicStateFreeList,
                    ref State);
            }
        }
    }
}
