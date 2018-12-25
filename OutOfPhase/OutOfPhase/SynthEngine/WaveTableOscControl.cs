/*
 *  Copyright © 1994-2002, 2015-2017 Thomas R. Lawrence
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
        /* wave table oscillator template information record */
        public class WaveTableTemplateRec : IOscillatorTemplate
        {
            /* source information for the wave table */
            public MultiWaveTableRec WaveTableSourceSelector;

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

            public bool EnableCrossWaveTableInterpolation;


            /* create a new wave table template */
            public static WaveTableTemplateRec NewWaveTableTemplate(
                OscillatorRec Oscillator,
                SynthParamRec SynthParams)
            {
#if DEBUG
                if (OscillatorGetWhatKindItIs(Oscillator) != OscillatorTypes.eOscillatorWaveTable)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                WaveTableTemplateRec Template = new WaveTableTemplateRec();

                Template.WaveTableSourceSelector = NewMultiWaveTable(
                    OscillatorGetSampleIntervalList(Oscillator),
                    SynthParams.perTrack.Dictionary);

                Template.OverallOscillatorLoudness = OscillatorGetOutputLoudness(Oscillator);

                /* it might be better to handle divisor and multiplier separately -- we would */
                /* want to do that if we were trying to guarantee that all harmonic */
                /* oscillators ran in lock-step */
                Template.FrequencyMultiplier = OscillatorGetFrequencyMultiplier(Oscillator)
                    / OscillatorGetFrequencyDivisor(Oscillator);
                Template.FrequencyAdder = OscillatorGetFrequencyAdder(Oscillator);

                Template.StereoBias = OscillatorGetStereoBias(Oscillator);
                Template.TimeDisplacement = OscillatorGetTimeDisplacement(Oscillator);

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

                Template.EnableCrossWaveTableInterpolation = OscillatorGetEnableCrossWaveTableInterpolation(Oscillator);

                return Template;
            }

            /* create a new wave table state object. */
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
                WaveTableTemplateRec Template = this;

                int OnePreOrigin;

                PreOriginTimeOut = 0;
                StateOut = null;

                WaveTableStateRec State = New(ref SynthParams.freelists.waveTableStateFreeList);

                // all fields must be assigned: State

                // conservative zero-initialization
                State.WaveTableSamplePositionDifferential = new Fixed64(0);

                State.Template = Template;

                int MaxPreOrigin = 0;

                State.WaveTableSamplePosition = new Fixed64(0);
                /* State.WaveTableSamplePositionDifferential specified in separate call */

                State.NoteLoudnessScaling = Loudness * Template.OverallOscillatorLoudness;

                int NumberOfTables;
                State.WaveTableWasDefined = GetMultiWaveTableReference(
                    Template.WaveTableSourceSelector,
                    FreqForMultisampling,
                    out State.WaveTableMatrix,
                    out State.FramesPerTable,
                    out NumberOfTables);
                State.NumberOfTablesMinus1 = NumberOfTables - 1;

                State.FramesPerTableOverFinalOutputSamplingRate
                    = (double)State.FramesPerTable / SynthParams.dSamplingRate;

                /* State.FramesPerTable > 0: */
                /*   if the wave table is empty, then we don't do any work (and we must not, */
                /*   since array accesses would cause a crash) */
                if (State.WaveTableWasDefined)
                {
                    if (!(State.FramesPerTable > 0))
                    {
                        State.WaveTableWasDefined = false;
                    }
                }

                State.PreStartCountdown = (int)((Template.TimeDisplacement
                    * SynthParams.dEnvelopeRate) + 0.5);
                if (-State.PreStartCountdown > MaxPreOrigin)
                {
                    MaxPreOrigin = -State.PreStartCountdown;
                }

                /* State.WaveTableIndex determined by envelope update */
                State.WaveTableIndexEnvelope = NewEnvelopeStateRecord(
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
                State.PreviousWaveTableIndex = 0;
                State.WaveTableIndex = 0;

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
                State.WaveTableLoudnessEnvelope = NewEnvelopeStateRecord(
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
                State.PreviousLoudness = 0;
                State.Loudness = 0;

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

                State.EnableCrossWaveTableInterpolation = Template.EnableCrossWaveTableInterpolation;

                PreOriginTimeOut = MaxPreOrigin;
                StateOut = State;
                return SynthErrorCodes.eSynthDone;
            }

            public OscillatorTypes Type { get { return OscillatorTypes.eOscillatorWaveTable; } }
        }

        /* wave table oscillator state record */
        public class WaveTableStateRec : IOscillator
        {
            /* current sample position into the wave table */
            public Fixed64 WaveTableSamplePosition; /* 64-bit fixed point */
            /* current increment value for the wave table sample position */
            public Fixed64 WaveTableSamplePositionDifferential; /* 64-bit fixed point */

            /* envelope tick countdown for pre-start time */
            public int PreStartCountdown;

            /* number of frames per table */
            public int FramesPerTable;
            /* number of tables - 1 (kept as float since it always multiplies a float) */
            public float NumberOfTablesMinus1;
            /* raw wave table data array */
            public float[][] WaveTableMatrix;

            /* current index into table of waves.  0 = lowest wave table, NumberOfTables = highest */
            public double WaveTableIndex; /* 0..NumTables - 1 */
            public double PreviousWaveTableIndex; /* 0..NumTables - 1 */
            /* envelope controlling wave table index */
            public EvalEnvelopeRec WaveTableIndexEnvelope;
            /* LFO generators modifying the output of the index envelope generator */
            public LFOGenRec IndexLFOGenerator;

            // loudness envelope
            public float Loudness;
            public float PreviousLoudness;
            /* panning position for splitting envelope generator into stereo channels */
            /* 0 = left channel, 0.5 = middle, 1 = right channel */
            public float Panning;
            /* envelope that is generating the loudness information */
            public EvalEnvelopeRec WaveTableLoudnessEnvelope;
            /* LFO generators modifying the output of the loudness envelope generator */
            public LFOGenRec LoudnessLFOGenerator;

            /* this flag is true if the wave table data was defined at the specified pitch */
            /* (and the wave table array is thus valid) or false if there is no wave table */
            /* at this pitch (and the array is invalid) */
            public bool WaveTableWasDefined;

            /* this field contains the overall volume scaling for everything so that we */
            /* can treat the envelopes as always going between 0 and 1. */
            public double NoteLoudnessScaling;

            /* this calculates the differential values for periodic pitch displacements */
            public LFOGenRec PitchLFO;
            /* pitch lfo startup counter; negative = expired */
            public int PitchLFOStartCountdown;

            /* postprocessing for this oscillator; may be null */
            public OscEffectGenRec OscEffectGenerator;

            /* precomputed factor for UpdateWaveTableEnvelopes to use */
            public double FramesPerTableOverFinalOutputSamplingRate;

            /* static information for the wave table */
            public WaveTableTemplateRec Template;

            public bool EnableCrossWaveTableInterpolation;


            /* perform one envelope update cycle, and set a new frequency for a wave table */
            /* state object.  used for portamento and modulation of frequency (vibrato) */
            public SynthErrorCodes UpdateEnvelopes(
                double NewFrequencyHertz,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;
                WaveTableStateRec State = this;

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
                double Differential = NewFrequencyHertz * State.FramesPerTableOverFinalOutputSamplingRate;
                State.WaveTableSamplePositionDifferential = new Fixed64(Differential);

                /* this is for the benefit of resampling only -- envelope generators do their */
                /* own pre-origin sequencing */
                if (State.PreStartCountdown > 0)
                {
                    State.PreStartCountdown -= 1;
                }

                error = SynthErrorCodes.eSynthDone;
                double waveTableIndex = State.NumberOfTablesMinus1 *
                    LFOGenUpdateCycle(
                        State.IndexLFOGenerator,
                        EnvelopeUpdate(
                            State.WaveTableIndexEnvelope,
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
                if (waveTableIndex < 0)
                {
                    waveTableIndex = 0;
                }
                else if (waveTableIndex > State.NumberOfTablesMinus1)
                {
                    waveTableIndex = State.NumberOfTablesMinus1;
                }
                State.PreviousWaveTableIndex = State.WaveTableIndex;
                State.WaveTableIndex = waveTableIndex;

                error = SynthErrorCodes.eSynthDone;
                State.PreviousLoudness = State.Loudness;
                State.Loudness = (float)(State.NoteLoudnessScaling *
                    LFOGenUpdateCycle(
                        State.LoudnessLFOGenerator,
                        EnvelopeUpdate(
                            State.WaveTableLoudnessEnvelope,
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

            /* fix up pre-origin time for the wave table state object */
            public void FixUpPreOrigin(
                int ActualPreOrigin)
            {
                WaveTableStateRec State = this;

                EnvelopeStateFixUpInitialDelay(
                    State.WaveTableIndexEnvelope,
                    ActualPreOrigin);
                EnvelopeStateFixUpInitialDelay(
                    State.WaveTableLoudnessEnvelope,
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
                State.Loudness = (float)(State.NoteLoudnessScaling * LFOGenInitialValue(
                    State.LoudnessLFOGenerator,
                    EnvelopeInitialValue(
                       State.WaveTableLoudnessEnvelope)));

                // initial value for envelope smoothing
                double waveTableIndex = State.NumberOfTablesMinus1 * LFOGenInitialValue(
                    State.IndexLFOGenerator,
                    EnvelopeInitialValue(
                       State.WaveTableIndexEnvelope));
                if (waveTableIndex < 0)
                {
                    waveTableIndex = 0;
                }
                else if (waveTableIndex > State.NumberOfTablesMinus1)
                {
                    waveTableIndex = State.NumberOfTablesMinus1;
                }
                State.WaveTableIndex = waveTableIndex;
            }

            /* send a key-up signal to one of the oscillators */
            public void KeyUpSustain1()
            {
                WaveTableStateRec State = this;

                LFOGeneratorKeyUpSustain1(
                    State.IndexLFOGenerator);
                LFOGeneratorKeyUpSustain1(
                    State.LoudnessLFOGenerator);
                EnvelopeKeyUpSustain1(
                    State.WaveTableIndexEnvelope);
                EnvelopeKeyUpSustain1(
                    State.WaveTableLoudnessEnvelope);
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
                WaveTableStateRec State = this;

                LFOGeneratorKeyUpSustain2(
                    State.IndexLFOGenerator);
                LFOGeneratorKeyUpSustain2(
                    State.LoudnessLFOGenerator);
                EnvelopeKeyUpSustain2(
                    State.WaveTableIndexEnvelope);
                EnvelopeKeyUpSustain2(
                    State.WaveTableLoudnessEnvelope);
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
                WaveTableStateRec State = this;

                LFOGeneratorKeyUpSustain3(
                    State.IndexLFOGenerator);
                LFOGeneratorKeyUpSustain3(
                    State.LoudnessLFOGenerator);
                EnvelopeKeyUpSustain3(
                    State.WaveTableIndexEnvelope);
                EnvelopeKeyUpSustain3(
                    State.WaveTableLoudnessEnvelope);
                LFOGeneratorKeyUpSustain3(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain3(
                        State.OscEffectGenerator);
                }
            }

            /* restart a wave table oscillator.  this is used for tie continuations */
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
                WaveTableStateRec State = this;

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
                    State.WaveTableIndexEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    RetriggerEnvelopes,
                    SynthParams);
                EnvelopeRetriggerFromOrigin(
                    State.WaveTableLoudnessEnvelope,
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

                WaveTableStateRec State = this;

#if DEBUG
                Debug.Assert(!SynthParams.ScratchWorkspace1InUse);
                SynthParams.ScratchWorkspace1InUse = true;
                Debug.Assert(!SynthParams.ScratchWorkspace3InUse);
                SynthParams.ScratchWorkspace3InUse = true;
#endif
                int loudnessWorkspaceOffset = SynthParams.ScratchWorkspace1LOffset;
                int indexWorkspaceOffset = SynthParams.ScratchWorkspace3Offset;
                double[] indexWorkspace = UnsafeArrayCastLong.AsDoubles(SynthParams.ScratchWorkspace3);

                if (State.PreStartCountdown <= 0)
                {
                    if (State.OscEffectGenerator == null)
                    {
                        /* normal case */
                        if (State.WaveTableWasDefined)
                        {
                            Wave_Stereo(
                                State,
                                nActualFrames,
                                workspace,
                                RawBufferLOffset,
                                RawBufferROffset,
                                loudnessWorkspaceOffset,
                                indexWorkspace,
                                indexWorkspaceOffset);
                        }
                    }
                    else
                    {
                        /* effect postprocessing case */

                        /* initialize private storage */
                        FloatVectorZero(
                            workspace,
                            PrivateWorkspaceLOffset,
                            nActualFrames);
                        FloatVectorZero(
                            workspace,
                            PrivateWorkspaceROffset,
                            nActualFrames);

                        /* generate waveform */
                        if (State.WaveTableWasDefined)
                        {
                            Wave_Stereo(
                                State,
                                nActualFrames,
                                workspace,
                                PrivateWorkspaceLOffset,
                                PrivateWorkspaceROffset,
                                loudnessWorkspaceOffset,
                                indexWorkspace,
                                indexWorkspaceOffset);
                        }

#if DEBUG
                        SynthParams.ScratchWorkspace1InUse = false;
                        SynthParams.ScratchWorkspace3InUse = false;
#endif

                        /* apply processor to it */
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

                        /* copy out data */
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
                SynthParams.ScratchWorkspace3InUse = false;
#endif

                return error;
            }

            private static void Wave_Stereo(
                WaveTableStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset,
                double[] indexWorkspace,
                int indexWorkspaceOffset)
            {
#if DEBUG
                if ((State.WaveTableIndex < 0) || (State.WaveTableIndex > State.NumberOfTablesMinus1))
                {
                    // wave index out of range
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
#endif

                /* load local values */
                float LocalLoudness = State.Loudness;
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                Fixed64 LocalWaveTableSamplePosition = State.WaveTableSamplePosition;
                Fixed64 LocalWaveTableSamplePositionDifferential = State.WaveTableSamplePositionDifferential;
                int LocalSamplePositionMask = State.FramesPerTable - 1;

                if (Program.Config.EnableEnvelopeSmoothing
                    // case of no motion in either smoothed axes can use fast code path
                    && ((State.Loudness != State.PreviousLoudness)
                    || (State.WaveTableIndex != State.PreviousWaveTableIndex)))
                {
                    // envelope smoothing

                    float LocalPreviousLoudness = State.PreviousLoudness;

                    // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                    if (IsLFOSampleAndHold(State.LoudnessLFOGenerator))
                    {
                        LocalPreviousLoudness = LocalLoudness;
                    }

                    if (!EnvelopeCurrentSegmentExponential(State.WaveTableLoudnessEnvelope))
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

                    /* select method */
                    if (((State.WaveTableIndex == State.NumberOfTablesMinus1)
                            && (State.PreviousWaveTableIndex == State.NumberOfTablesMinus1))
                        || !State.EnableCrossWaveTableInterpolation)
                    {
                        /* process at end of table, where there is no extra table for interpolation */

                        /* load local values */
                        float[] WaveData = State.WaveTableMatrix[(int)(State.WaveTableIndex)];

                        /* process */
                        for (int i = 0; i < nActualFrames; i++)
                        {
                            /* load outside buffer values */
                            float OrigLeft = workspace[i + lOffset];
                            float OrigRight = workspace[i + rOffset];

                            /* compute weighting and subscript */
                            float RightWeight = LocalWaveTableSamplePosition.FracF;
                            int ArraySubscript = LocalWaveTableSamplePosition.Int & LocalSamplePositionMask;

                            /* L+F(R-L) */
                            float LeftValue = WaveData[ArraySubscript];
                            float RightValue = WaveData[ArraySubscript + 1];
                            float CombinedValue = LeftValue + (RightWeight * (RightValue - LeftValue));

                            /* increment pitch differential */
                            LocalWaveTableSamplePosition += LocalWaveTableSamplePositionDifferential;

                            /* generate output */
                            LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                            workspace[i + lOffset] = OrigLeft + LeftPan * LocalLoudness * CombinedValue;
                            workspace[i + rOffset] = OrigRight + RightPan * LocalLoudness * CombinedValue;
                        }
                    }
                    else
                    {
                        double LocalPreviousWaveTableIndex = State.PreviousWaveTableIndex;
                        double LocalWaveTableIndex = State.WaveTableIndex;

                        // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                        if (IsLFOSampleAndHold(State.IndexLFOGenerator))
                        {
                            LocalPreviousWaveTableIndex = LocalWaveTableIndex;
                        }

                        if (!EnvelopeCurrentSegmentExponential(State.WaveTableLoudnessEnvelope))
                        {
                            FloatVectorAdditiveRecurrenceFixed(
                                indexWorkspace,
                                indexWorkspaceOffset,
                                LocalPreviousWaveTableIndex,
                                LocalWaveTableIndex,
                                nActualFrames);
                        }
                        else
                        {
                            FloatVectorMultiplicativeRecurrenceFixed(
                                indexWorkspace,
                                indexWorkspaceOffset,
                                LocalPreviousWaveTableIndex,
                                LocalWaveTableIndex,
                                nActualFrames);
                        }

                        /* process */
                        for (int i = 0; i < nActualFrames; i++)
                        {
                            // unfortunately these are variant in this case
                            LocalWaveTableIndex = indexWorkspace[indexWorkspaceOffset + i];
                            float[] WaveData0 = State.WaveTableMatrix[(int)LocalWaveTableIndex];
                            // wave index can reach max value during iterations, constrain (Wave1Weight becomes unimportant)
                            float[] WaveData1 = State.WaveTableMatrix[(int)Math.Min(LocalWaveTableIndex + 1, State.NumberOfTablesMinus1)];
                            float Wave1Weight = (float)(LocalWaveTableIndex - (int)LocalWaveTableIndex);

                            /* load outside buffer values */
                            float OrigLeft = workspace[i + lOffset];
                            float OrigRight = workspace[i + rOffset];

                            /* compute weighting and subscript */
                            float RightWeight = LocalWaveTableSamplePosition.FracF;
                            int ArraySubscript = LocalWaveTableSamplePosition.Int & LocalSamplePositionMask;

                            /* L+F(R-L) -- applied twice */
                            float Left0Value = WaveData0[ArraySubscript];
                            float Right0Value = WaveData0[ArraySubscript + 1];
                            float Left1Value = WaveData1[ArraySubscript];
                            float Right1Value = WaveData1[ArraySubscript + 1];
                            float Wave0Temp = Left0Value + (RightWeight * (Right0Value - Left0Value));
                            Wave0Temp = Wave0Temp + (Wave1Weight * (Left1Value + (RightWeight
                                * (Right1Value - Left1Value)) - Wave0Temp));

                            /* increment pitch differential */
                            LocalWaveTableSamplePosition += LocalWaveTableSamplePositionDifferential;

                            /* generate output */
                            LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                            workspace[i + lOffset] = OrigLeft + LeftPan * LocalLoudness * Wave0Temp;
                            workspace[i + rOffset] = OrigRight + RightPan * LocalLoudness * Wave0Temp;
                        }
                    }
                }
                else
                {
                    // non-smoothing case

                    float LocalLeftLoudness = LocalLoudness * LeftPan;
                    float LocalRightLoudness = LocalLoudness * RightPan;

                    /* select method */
                    if ((State.WaveTableIndex == State.NumberOfTablesMinus1)
                        || !State.EnableCrossWaveTableInterpolation)
                    {
                        /* process at end of table, where there is no extra table for interpolation */

                        /* load local values */
                        float[] WaveData = State.WaveTableMatrix[(int)(State.WaveTableIndex)];

                        /* process */
                        for (int i = 0; i < nActualFrames; i++)
                        {
                            /* load outside buffer values */
                            float OrigLeft = workspace[i + lOffset];
                            float OrigRight = workspace[i + rOffset];

                            /* compute weighting and subscript */
                            float RightWeight = LocalWaveTableSamplePosition.FracF;
                            int ArraySubscript = LocalWaveTableSamplePosition.Int & LocalSamplePositionMask;

                            /* L+F(R-L) */
                            float LeftValue = WaveData[ArraySubscript];
                            float RightValue = WaveData[ArraySubscript + 1];
                            float CombinedValue = LeftValue + (RightWeight * (RightValue - LeftValue));

                            /* increment pitch differential */
                            LocalWaveTableSamplePosition += LocalWaveTableSamplePositionDifferential;

                            /* generate output */
                            workspace[i + lOffset] = OrigLeft + LocalLeftLoudness * CombinedValue;
                            workspace[i + rOffset] = OrigRight + LocalRightLoudness * CombinedValue;
                        }
                    }
                    else
                    {
                        /* load local values */
                        float[] WaveData0 = State.WaveTableMatrix[(int)(State.WaveTableIndex)];
                        float[] WaveData1 = State.WaveTableMatrix[(int)(State.WaveTableIndex) + 1];
                        float Wave1Weight = (float)(State.WaveTableIndex - (int)(State.WaveTableIndex));

                        /* process */
                        for (int i = 0; i < nActualFrames; i++)
                        {
                            /* load outside buffer values */
                            float OrigLeft = workspace[i + lOffset];
                            float OrigRight = workspace[i + rOffset];

                            /* compute weighting and subscript */
                            float RightWeight = LocalWaveTableSamplePosition.FracF;
                            int ArraySubscript = LocalWaveTableSamplePosition.Int & LocalSamplePositionMask;

                            /* L+F(R-L) -- applied twice */
                            float Left0Value = WaveData0[ArraySubscript];
                            float Right0Value = WaveData0[ArraySubscript + 1];
                            float Left1Value = WaveData1[ArraySubscript];
                            float Right1Value = WaveData1[ArraySubscript + 1];
                            float Wave0Temp = Left0Value + (RightWeight * (Right0Value - Left0Value));
                            Wave0Temp = Wave0Temp + (Wave1Weight * (Left1Value + (RightWeight
                                * (Right1Value - Left1Value)) - Wave0Temp));

                            /* increment pitch differential */
                            LocalWaveTableSamplePosition += LocalWaveTableSamplePositionDifferential;

                            /* generate output */
                            workspace[i + lOffset] = OrigLeft + LocalLeftLoudness * Wave0Temp;
                            workspace[i + rOffset] = OrigRight + LocalRightLoudness * Wave0Temp;
                        }
                    }
                }

                /* save local state back to record */
                State.WaveTableSamplePosition = LocalWaveTableSamplePosition;
            }

            /* find out if the wave table oscillator has finished */
            public bool IsItFinished()
            {
                WaveTableStateRec State = this;

                /* we are finished when one of the following conditions is met: */
                /*  - output volume is zero AND loudness envelope is finished */
                /*  - we are not generating any signal */
                if (!State.WaveTableWasDefined)
                {
                    return true;
                }
                return IsEnvelopeAtEnd(State.WaveTableLoudnessEnvelope);
            }

            /* finalize before termination */
            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                WaveTableStateRec State = this;

                if (State.OscEffectGenerator != null)
                {
                    FinalizeOscEffectGenerator(
                        State.OscEffectGenerator,
                        SynthParams,
                        writeOutputLogs);
                }

                FreeEnvelopeStateRecord(
                    ref State.WaveTableLoudnessEnvelope,
                    SynthParams);
                FreeLFOGenerator(
                    ref State.LoudnessLFOGenerator,
                    SynthParams);

                FreeEnvelopeStateRecord(
                    ref State.WaveTableIndexEnvelope,
                    SynthParams);
                FreeLFOGenerator(
                    ref State.IndexLFOGenerator,
                    SynthParams);

                FreeLFOGenerator(
                    ref State.PitchLFO,
                    SynthParams);

                Free(
                    ref SynthParams.freelists.waveTableStateFreeList,
                    ref State);
            }
        }
    }
}
