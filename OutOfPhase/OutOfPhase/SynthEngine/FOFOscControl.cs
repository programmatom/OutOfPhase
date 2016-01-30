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
        /* FOF oscillator template information record */
        public class FOFTemplateRec : IOscillatorTemplate
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

            /* FOF compression mode */
            public OscFOFCompressType FOFCompression;
            /* FOF expansion mode */
            public OscFOFExpandType FOFExpansion;
            /* FOF sampling rate for waveform */
            public double FOFSamplingRate;

            /* contouring for FOF sampling rate */
            public EnvelopeRec FOFSamplingRateEnvelopeTemplate;
            public LFOListSpecRec FOFSamplingRateLFOTemplate;


            /* create a new FOF template */
            public static FOFTemplateRec NewFOFTemplate(
                OscillatorRec Oscillator,
                SynthParamRec SynthParams)
            {
                FOFTemplateRec Template = new FOFTemplateRec();

                Template.WaveTableSourceSelector = NewMultiWaveTable(
                    OscillatorGetSampleIntervalList(Oscillator),
                    SynthParams.Dictionary);

                Template.OverallOscillatorLoudness = OscillatorGetOutputLoudness(Oscillator);

                Template.FOFCompression = GetOscillatorFOFCompress(Oscillator);
                Template.FOFExpansion = GetOscillatorFOFExpand(Oscillator);
                Template.FOFSamplingRate = GetOscillatorFOFSamplingRate(Oscillator);

                /* it might be better to handle divisor and multiplier separately -- we would */
                /* want to do that if we were trying to guarantee that all harmonic */
                /* oscillators ran in lock-step */
                Template.FrequencyMultiplier = OscillatorGetFrequencyMultiplier(Oscillator) / OscillatorGetFrequencyDivisor(Oscillator);
                Template.FrequencyAdder = OscillatorGetFrequencyAdder(Oscillator);

                Template.StereoBias = OscillatorGetStereoBias(Oscillator);
                Template.TimeDisplacement = OscillatorGetTimeDisplacement(Oscillator);

                /* these are just references */
                Template.LoudnessEnvelopeTemplate = OscillatorGetLoudnessEnvelope(Oscillator);
                Template.LoudnessLFOTemplate = OscillatorGetLoudnessLFOList(Oscillator);
                Template.IndexEnvelopeTemplate = OscillatorGetExcitationEnvelope(Oscillator);
                Template.IndexLFOTemplate = OscillatorGetExcitationLFOList(Oscillator);
                Template.FOFSamplingRateEnvelopeTemplate = OscillatorGetFOFRateEnvelope(Oscillator);
                Template.FOFSamplingRateLFOTemplate = OscillatorGetFOFRateLFOList(Oscillator);

                /* more references */
                Template.PitchLFOTemplate = GetOscillatorFrequencyLFOList(Oscillator);
                Template.OscEffectTemplate = GetOscillatorEffectList(Oscillator);
                if (GetEffectSpecListLength(Template.OscEffectTemplate) == 0)
                {
                    Template.OscEffectTemplate = null;
                }

                return Template;
            }

            /* create a new FOF state object. */
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
                FOFTemplateRec Template = this;

                int OnePreOrigin;
                int NumberOfTables;

                PreOriginTimeOut = 0;
                StateOut = null;

                FOFStateRec State = new FOFStateRec();

                State.Template = Template;

                int MaxPreOrigin = 0;

                State.ActiveGrainList = null; /* no grains to begin with */

                State.NoteLoudnessScaling = Loudness * Template.OverallOscillatorLoudness;

                State.WaveTableWasDefined = GetMultiWaveTableReference(
                    Template.WaveTableSourceSelector,
                    FreqForMultisampling,
                    out State.WaveTableMatrix,
                    out State.FramesPerTable,
                    out NumberOfTables);
                State.NumberOfTablesMinus1 = NumberOfTables - 1;

                if (State.WaveTableWasDefined)
                {
                    /* we want the first grain to go on the first sampling point, so we */
                    /* set this up to start on the next interval. */
                    State.WaveTableSamplePosition = new Fixed64(State.FramesPerTable - State.FramesPerTable
                        * (InitialFrequency * SynthParams.dSamplingRateReciprocal));
                }
                /* State.WaveTableSamplePositionDifferential specified in separate call */

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

                /* State.WaveTableWasDefined: */
                /*   if there is no wave table defined for the current pitch, then we don't */
                /*   bother generating any data */
                /* (no action required) */

                State.PreStartCountdown = (int)(Template.TimeDisplacement * SynthParams.dEnvelopeRate + 0.5);
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

                State.FOFSamplingRateEnvelope = NewEnvelopeStateRecord(
                    Template.FOFSamplingRateEnvelopeTemplate,
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

                State.FOFSamplingRateLFOGenerator = NewLFOGenerator(
                    Template.FOFSamplingRateLFOTemplate,
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

                if (Template.OscEffectTemplate == null)
                {
                    State.OscEffectGenerator = null;
                }
                else
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

            public OscillatorTypes Type { get { return OscillatorTypes.eOscillatorFOF; } }
        }

        /* FOF oscillator state record */
        public class FOFStateRec : IOscillator
        {
            /* these do not actually scan a wavetable.  instead they are used for */
            /* determining when FOF waves will be triggered. */
            /* current sample position into the wave table */
            public Fixed64 WaveTableSamplePosition; /* 32-bit fixed point */
            /* current increment value for the wave table sample position */
            public Fixed64 WaveTableSamplePositionDifferential; /* 32-bit fixed point */

            /* here's the list of active grains */
            public FOFGrainRec ActiveGrainList;

            /* envelope tick countdown for pre-start time */
            public int PreStartCountdown;

            /* contour generators for FOF sampling rate */
            public EvalEnvelopeRec FOFSamplingRateEnvelope;
            public LFOGenRec FOFSamplingRateLFOGenerator;
            /* thing updated every so often */
            public double FOFSamplingRateContour;

            /* number of frames per table */
            public int FramesPerTable;
            /* number of tables - 1 (kept as float since it always multiplies a float) */
            public float NumberOfTablesMinus1;
            /* raw wave table data array */
            public float[][] WaveTableMatrix;

            /* current index into table of waves.  0 = lowest wave table, NumberOfTables = highest */
            public double WaveTableIndex;
            /* envelope controlling wave table index */
            public EvalEnvelopeRec WaveTableIndexEnvelope;
            /* LFO generators modifying the output of the index envelope generator */
            public LFOGenRec IndexLFOGenerator;

            /* left channel loudness */
            public float LeftLoudness;
            /* right channel loudness */
            public float RightLoudness;
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

            /* static information for the wave table */
            public FOFTemplateRec Template;


            /* perform one envelope update cycle, and set a new frequency for an FOF */
            /* state object.  used for portamento and modulation of frequency (vibrato) */
            public SynthErrorCodes UpdateEnvelopes(
                double NewFrequencyHertz,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;
                FOFStateRec State = this;

                float FloatTemp;
                double DoubleTemp;
                double Differential;
                float OneHalfVol;
                float LeftLoudness;
                float RightLoudness;

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
                Differential = NewFrequencyHertz * SynthParams.dSamplingRateReciprocal * State.FramesPerTable;
                State.WaveTableSamplePositionDifferential = new Fixed64(Differential);

                /* this is for the benefit of resampling only -- envelope generators do their */
                /* own pre-origin sequencing */
                if (State.PreStartCountdown > 0)
                {
                    State.PreStartCountdown -= 1;
                }

                error = SynthErrorCodes.eSynthDone;
                DoubleTemp = State.NumberOfTablesMinus1 *
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
                if (DoubleTemp < 0)
                {
                    DoubleTemp = 0;
                }
                else if (DoubleTemp > State.NumberOfTablesMinus1)
                {
                    DoubleTemp = State.NumberOfTablesMinus1;
                }
                State.WaveTableIndex = DoubleTemp;

                error = SynthErrorCodes.eSynthDone;
                State.FOFSamplingRateContour = LFOGenUpdateCycle(
                    State.FOFSamplingRateLFOGenerator,
                    EnvelopeUpdate(
                        State.FOFSamplingRateEnvelope,
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

                error = SynthErrorCodes.eSynthDone;
                FloatTemp = (float)(State.NoteLoudnessScaling * LFOGenUpdateCycle(
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
                /* left = FloatTemp * .5 * (1 - State.Panning) */
                /* right = FloatTemp * .5 * (1 + State.Panning) */
                OneHalfVol = .5f * FloatTemp;
                LeftLoudness = OneHalfVol - OneHalfVol * State.Panning;
                RightLoudness = OneHalfVol + OneHalfVol * State.Panning;
                State.LeftLoudness = LeftLoudness;
                State.RightLoudness = RightLoudness;

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

            /* fix up pre-origin time for the FOF state object */
            public void FixUpPreOrigin(
                int ActualPreOrigin)
            {
                FOFStateRec State = this;

                EnvelopeStateFixUpInitialDelay(
                    State.WaveTableIndexEnvelope,
                    ActualPreOrigin);
                EnvelopeStateFixUpInitialDelay(
                    State.WaveTableLoudnessEnvelope,
                    ActualPreOrigin);
                EnvelopeStateFixUpInitialDelay(
                    State.FOFSamplingRateEnvelope,
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
                LFOGeneratorFixEnvelopeOrigins(
                    State.FOFSamplingRateLFOGenerator,
                    ActualPreOrigin);
                if (State.OscEffectGenerator != null)
                {
                    FixUpOscEffectGeneratorPreOrigin(
                        State.OscEffectGenerator,
                        ActualPreOrigin);
                }

                State.PreStartCountdown += ActualPreOrigin;
            }

            /* send a key-up signal to one of the oscillators */
            public void KeyUpSustain1()
            {
                FOFStateRec State = this;

                LFOGeneratorKeyUpSustain1(
                    State.IndexLFOGenerator);
                LFOGeneratorKeyUpSustain1(
                    State.LoudnessLFOGenerator);
                LFOGeneratorKeyUpSustain1(
                    State.FOFSamplingRateLFOGenerator);
                EnvelopeKeyUpSustain1(
                    State.WaveTableIndexEnvelope);
                EnvelopeKeyUpSustain1(
                    State.WaveTableLoudnessEnvelope);
                EnvelopeKeyUpSustain1(
                    State.FOFSamplingRateEnvelope);
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
                FOFStateRec State = this;

                LFOGeneratorKeyUpSustain2(
                    State.IndexLFOGenerator);
                LFOGeneratorKeyUpSustain2(
                    State.LoudnessLFOGenerator);
                LFOGeneratorKeyUpSustain2(
                    State.FOFSamplingRateLFOGenerator);
                EnvelopeKeyUpSustain2(
                    State.WaveTableIndexEnvelope);
                EnvelopeKeyUpSustain2(
                    State.WaveTableLoudnessEnvelope);
                EnvelopeKeyUpSustain2(
                    State.FOFSamplingRateEnvelope);
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
                FOFStateRec State = this;

                LFOGeneratorKeyUpSustain3(
                    State.IndexLFOGenerator);
                LFOGeneratorKeyUpSustain3(
                    State.LoudnessLFOGenerator);
                LFOGeneratorKeyUpSustain3(
                    State.FOFSamplingRateLFOGenerator);
                EnvelopeKeyUpSustain3(
                    State.WaveTableIndexEnvelope);
                EnvelopeKeyUpSustain3(
                    State.WaveTableLoudnessEnvelope);
                EnvelopeKeyUpSustain3(
                    State.FOFSamplingRateEnvelope);
                LFOGeneratorKeyUpSustain3(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain3(
                        State.OscEffectGenerator);
                }
            }

            /* restart a FOF oscillator.  this is used for tie continuations */
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
                FOFStateRec State = this;

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
                EnvelopeRetriggerFromOrigin(
                    State.FOFSamplingRateEnvelope,
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
                LFOGeneratorRetriggerFromOrigin(
                    State.FOFSamplingRateLFOGenerator,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
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

            /* generate a sequence of samples (called for each envelope clock) */
            public SynthErrorCodes Generate(
                int nActualFrames,
                float[] workspace,
                int RawBufferLOffset,
                int RawBufferROffset,
                int PrivateWorkspaceLOffset,
                int PrivateWorkspaceROffset,
                SynthParamRec SynthParams)
            {
                FOFStateRec State = this;

                if (State.PreStartCountdown <= 0)
                {
                    if (State.OscEffectGenerator == null)
                    {
                        /* non-effects case */

                        /* generate waveform */
                        FOFGeneratorStereo(
                            State,
                            nActualFrames,
                            workspace,
                            RawBufferLOffset,
                            RawBufferROffset,
                            SynthParams);
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
                        FOFGeneratorStereo(
                            State,
                            nActualFrames,
                            workspace,
                            PrivateWorkspaceLOffset,
                            PrivateWorkspaceROffset,
                            SynthParams);

                        /* apply processor to it */
                        SynthErrorCodes error = ApplyOscEffectGenerator(
                            State.OscEffectGenerator,
                            workspace,
                            PrivateWorkspaceLOffset,
                            PrivateWorkspaceROffset,
                            nActualFrames,
                            SynthParams);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
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

                return SynthErrorCodes.eSynthDone;
            }

            /* single FOF oscillator record */
            public class FOFGrainRec
            {
                /* current sample position into the wave table */
                public Fixed64 FOFSamplePosition; /* 32-bit fixed point */

                /* left hand side wave */
                public float[] WaveData0;
                /* right hand side wave -- will be null if wavetable index = 1 */
                public float[] WaveData1;
                /* left hand wave weight for interpolation (wave interpolation mode only) */
                public float Wave1Weight;
                /* volume controls */
                public float LeftLoudness; /* for stereo */
                public float RightLoudness; /* for stereo */

                /* increment for playing back individual grains */
                public Fixed64 FOFSamplePositionDifferential; /* 32-bit fixed point */

                /* list link */
                public FOFGrainRec Next;
            }

            /* fof pitch start pulse: */
            /* |              |              |              |              |              |    */
            /* 0.0 1.1 2.1 3.2 0.3 1.4 2.4 3.5 0.6 1.6 2.7 3.7 0.8 1.9 2.9 0.0 1.1 2.1 3.2 0.3 */
            /* sampling period pulse: */
            /* *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   */
            /* no interpolation of grains: */
            /* 0   1   2       0   1   2       0   1   2       0   1   2   0   1   2       0   */
            /* with interpolation of grains: */
            /* 0.0 1.0 2.0     0.3 1.3 2.3     0.5 1.5 2.5     0.7 1.7 2.7 0.0 1.0 2.0     0.3 */
            /* pitch start pulse and grain sampling pulse have same units to allow */
            /* start pulse fraction to be used for initial grain fraction. */


            /* check to see if we should launch a new grain */
            private static void GrainLaunchCheck(
                FOFStateRec State,
                bool EnvelopeFinished,
                SynthParamRec SynthParams)
            {
                /* first, handle grain launching.  do this by adding the pitch differential */
                /* to the accumulator and checking for overflow. */
                State.WaveTableSamplePosition += State.WaveTableSamplePositionDifferential;

                /* if this overflows, then truncate it and launch a new grain */
                if (State.WaveTableSamplePosition.Int >= State.FramesPerTable)
                {
                    FOFGrainRec NewGrain;

                    /* time to launch a new grain */

                    /* truncate grain pitch index to keep wave table index inside */
                    State.WaveTableSamplePosition.MaskInt64HighHalf(State.FramesPerTable - 1);

                    /* check for discard mode */
                    if (State.Template.FOFCompression == OscFOFCompressType.eOscFOFDiscard)
                    {
                        /* if we are in discard mode, then dump any active grains */
                        State.ActiveGrainList = null;
                    }

                    /* creating new grain, but only if the envelope generator hasn't terminated */
                    if (!EnvelopeFinished)
                    {
                        NewGrain = new FOFGrainRec();

                        /* set up grain's phase index */
                        NewGrain.FOFSamplePosition = State.WaveTableSamplePosition;

                        /* set up wave data pointers using current wave table index */
#if DEBUG
                        if ((State.WaveTableIndex < 0) || (State.WaveTableIndex > State.NumberOfTablesMinus1))
                        {
                            // table index out of range
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
#endif
                        NewGrain.WaveData0 = State.WaveTableMatrix[(int)(State.WaveTableIndex)];
                        if ((int)(State.WaveTableIndex) < State.NumberOfTablesMinus1)
                        {
                            NewGrain.WaveData1 = State.WaveTableMatrix[(int)(State.WaveTableIndex) + 1];
                            NewGrain.Wave1Weight = (float)(State.WaveTableIndex - (int)(State.WaveTableIndex));
                        }
                        else
                        {
                            NewGrain.WaveData1 = null;
                        }

                        /* set up amplitudes */
                        NewGrain.LeftLoudness = State.LeftLoudness;
                        NewGrain.RightLoudness = State.RightLoudness;

                        /* initialize grain differential to the sampling rate */
                        /* note that grain differential is independent of the */
                        /* number of frames in the table. */
                        NewGrain.FOFSamplePositionDifferential = new Fixed64(
                            State.FOFSamplingRateContour * State.Template.FOFSamplingRate
                                * SynthParams.dSamplingRateReciprocal);

                        /* establish link, only if differential isn't really close to zero */
                        if ((NewGrain.FOFSamplePositionDifferential.FracI < 0x00010000)
                            && (NewGrain.FOFSamplePositionDifferential.Int == 0))
                        {
                        }
                        else
                        {
                            /* inserted at front of list (important later) */
                            NewGrain.Next = State.ActiveGrainList;
                            State.ActiveGrainList = NewGrain;
                        }
                    }
                }
            }

            /* wave resampler helper */
            private static void FOFGeneratorStereo(
                FOFStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                SynthParamRec SynthParams)
            {
                /* see if we shouldn't be here */
                if (!State.WaveTableWasDefined)
                {
                    return;
                }

                /* do some setup */
                bool EnvelopeFinished = IsEnvelopeAtEnd(State.WaveTableLoudnessEnvelope);
                int LocalSamplePositionMask = State.FramesPerTable - 1;

                /* iterate over samples */
                for (int Scan = 0; Scan < nActualFrames; Scan++)
                {
                    FOFGrainRec GrainScan;
                    FOFGrainRec GrainLag;
                    float CurrentLeft;
                    float CurrentRight;

                    /* check for launching */
                    GrainLaunchCheck(
                        State,
                        EnvelopeFinished,
                        SynthParams);

                    /* prepare point */
                    CurrentLeft = workspace[Scan + lOffset];
                    CurrentRight = workspace[Scan + rOffset];

                    /* generate sounds for all registered grains */
                    GrainScan = State.ActiveGrainList;
                    GrainLag = null;
                    while (GrainScan != null)
                    {
                        /* wave generation */

                        float RightWeight = GrainScan.FOFSamplePosition.FracF;
                        int ArraySubscript = GrainScan.FOFSamplePosition.Int & LocalSamplePositionMask;

                        /* L+F(R-L) */
                        float Left0Value = GrainScan.WaveData0[ArraySubscript];
                        float Right0Value = GrainScan.WaveData0[ArraySubscript + 1];
                        float CombinedValue = Left0Value + (RightWeight * (Right0Value - Left0Value));

                        if (GrainScan.WaveData1 != null)
                        {
                            /* L+F(R-L) -- applied twice */
                            float Left1Value = GrainScan.WaveData1[ArraySubscript];
                            float Right1Value = GrainScan.WaveData1[ArraySubscript + 1];
                            float Wave0Temp = CombinedValue;
                            CombinedValue = Wave0Temp + (GrainScan.Wave1Weight * (Left1Value
                                + (RightWeight * (Right1Value - Left1Value)) - Wave0Temp));
                        }
                        /* else index == 1, so no right hand wave to use */

                        CurrentLeft += GrainScan.LeftLoudness * CombinedValue;
                        CurrentRight += GrainScan.RightLoudness * CombinedValue;


                        /* increment phase */

                        bool Advance = true;
                        GrainScan.FOFSamplePosition += GrainScan.FOFSamplePositionDifferential;
                        if (GrainScan.FOFSamplePosition.Int >= State.FramesPerTable)
                        {
                            GrainScan.FOFSamplePosition.MaskInt64HighHalf(LocalSamplePositionMask);
                            if (State.Template.FOFExpansion == OscFOFExpandType.eOscFOFSilenceFill)
                            {
                                /* silence fill. */
                                /* for silence fill, we now terminate this oscillator. */
                                Advance = false;
                                GrainScan = GrainScan.Next;
                                if (GrainLag != null)
                                {
                                    GrainLag.Next = GrainScan;
                                }
                                else
                                {
                                    State.ActiveGrainList = GrainScan;
                                }
                            }
                            else
                            {
                                /* loop around. */
                                /* we only terminate the oscillator if it's not the first */
                                /* one, otherwise we allow it to restart */
                                if (GrainLag != null)
                                {
                                    /* not the first one */
                                    Advance = false;
                                    GrainScan = GrainScan.Next;
                                    GrainLag.Next = GrainScan;
                                }
                            }
                        }

                        if (Advance)
                        {
                            GrainLag = GrainScan;
                            GrainScan = GrainScan.Next;
                        }
                    }

                    /* send value back */
                    workspace[Scan + lOffset] = CurrentLeft;
                    workspace[Scan + rOffset] = CurrentRight;
                }
            }

            /* find out if the FOF oscillator has finished */
            public bool IsItFinished()
            {
                FOFStateRec State = this;

                /* we are finished when one of the following conditions is met: */
                /*  - output volume is zero AND loudness envelope is finished */
                /*  - we are not generating any signal */
                if (!State.WaveTableWasDefined)
                {
                    return true;
                }
                if (State.ActiveGrainList != null)
                {
                    return false; /* allow active grains to finish */
                }
                return IsEnvelopeAtEnd(State.WaveTableLoudnessEnvelope);
            }

            /* finalize before termination */
            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                FOFStateRec State = this;

                if (State.OscEffectGenerator != null)
                {
                    FinalizeOscEffectGenerator(
                        State.OscEffectGenerator,
                        SynthParams,
                        writeOutputLogs);
                }
            }
        }
    }
}
