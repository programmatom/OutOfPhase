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
        /* this is used for loop control */
        public enum LoopType
        {
            eRepeatingLoop1,
            eRepeatingLoop2,
            eRepeatingLoop3,
            eNoLoop,
            eSampleFinished,
        }

        /* sample oscillator template information record */
        public class SampleTemplateRec : IOscillatorTemplate
        {
            /* source information for the sample */
            public MultiSampleRec SampleSourceSelector;

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

            /* miscellaneous control parameters */
            public double StereoBias;
            public double TimeDisplacement;
            public double OverallOscillatorLoudness;

            /* template for the pitch displacement LFO */
            public LFOListSpecRec PitchLFOTemplate;

            /* effect specifier, may be null */
            public EffectSpecListRec OscEffectTemplate;


            /* create a new sample template */
            public static SampleTemplateRec NewSampleTemplate(
                OscillatorRec Oscillator,
                SynthParamRec SynthParams)
            {
#if DEBUG
                if (OscillatorGetWhatKindItIs(Oscillator) != OscillatorTypes.eOscillatorSampled)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                SampleTemplateRec Template = new SampleTemplateRec();

                Template.SampleSourceSelector = NewMultiSample(
                    OscillatorGetSampleIntervalList(Oscillator),
                    SynthParams.Dictionary);

                /* it might be better to handle divisor and multiplier separately -- we would */
                /* want to do that if we were trying to guarantee that all harmonic */
                /* oscillators ran in lock-step */
                Template.FrequencyMultiplier = OscillatorGetFrequencyMultiplier(Oscillator)
                    / OscillatorGetFrequencyDivisor(Oscillator);
                Template.FrequencyAdder = OscillatorGetFrequencyAdder(Oscillator);

                Template.StereoBias = OscillatorGetStereoBias(Oscillator);
                Template.TimeDisplacement = OscillatorGetTimeDisplacement(Oscillator);
                Template.OverallOscillatorLoudness = OscillatorGetOutputLoudness(Oscillator);

                /* these are just references */
                Template.LoudnessEnvelopeTemplate = OscillatorGetLoudnessEnvelope(Oscillator);
                Template.LoudnessLFOTemplate = OscillatorGetLoudnessLFOList(Oscillator);

                /* more references */
                Template.PitchLFOTemplate = GetOscillatorFrequencyLFOList(Oscillator);
                Template.OscEffectTemplate = GetOscillatorEffectList(Oscillator);
                if (GetEffectSpecListLength(Template.OscEffectTemplate) == 0)
                {
                    Template.OscEffectTemplate = null;
                }

                return Template;
            }

            private static readonly SampleGenSamplesMethod _Sample_StereoOut_StereoSamp = SampleStateRec.Sample_StereoOut_StereoSamp;
            private static readonly SampleGenSamplesMethod _Sample_StereoOut_MonoSamp = SampleStateRec.Sample_StereoOut_MonoSamp;
            private static readonly SampleGenSamplesMethod _Sample_StereoOut_StereoSamp_Bidir = SampleStateRec.Sample_StereoOut_StereoSamp_Bidir;
            private static readonly SampleGenSamplesMethod _Sample_StereoOut_MonoSamp_Bidir = SampleStateRec.Sample_StereoOut_MonoSamp_Bidir;
            private static readonly SampleGenSamplesMethod _Sample_NoOutput = SampleStateRec.Sample_NoOutput;

            /* create a new sample state object. */
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
                SampleTemplateRec Template = this;

                int OnePreOrigin;

                StateOut = null;

                SampleStateRec State = new SampleStateRec();

                State.Template = Template;

                int MaxPreOrigin = 0;

                State.SamplePosition = new Fixed64(0);
                /* State.SamplePositionDifferential is specified in a separate call */

                State.NoteLoudnessScaling = Loudness * Template.OverallOscillatorLoudness;
                //State.Loudness = 0;

                State.SampleWasDefined = GetMultiSampleReference(
                    Template.SampleSourceSelector,
                    FreqForMultisampling,
                    out State.Data,
                    out State.NumFrames,
                    out State.NumChannels,
                    out State.Loop1Start,
                    out State.Loop1End,
                    out State.Loop2Start,
                    out State.Loop2End,
                    out State.Loop3Start,
                    out State.Loop3End,
                    out State.OriginPoint,
                    out State.NaturalFrequency,
                    out State.NaturalSamplingRate,
                    out State.Loop1Bidirectional,
                    out State.Loop2Bidirectional,
                    out State.Loop3Bidirectional);

                if (State.SampleWasDefined && (State.NumFrames > 0))
                {
                    /* bounds checking */
                    if (State.NaturalFrequency < Constants.MINNATURALFREQ)
                    {
                        State.NaturalFrequency = Constants.MINNATURALFREQ;
                    }
                    if (State.NaturalFrequency > Constants.MAXNATURALFREQ)
                    {
                        State.NaturalFrequency = Constants.MAXNATURALFREQ;
                    }
                    if (State.NaturalSamplingRate < Constants.MINSAMPLINGRATE)
                    {
                        State.NaturalSamplingRate = Constants.MINSAMPLINGRATE;
                    }
                    if (State.NaturalSamplingRate > Constants.MAXSAMPLINGRATE)
                    {
                        State.NaturalSamplingRate = Constants.MAXSAMPLINGRATE;
                    }
                    if (State.Loop1Start < 0)
                    {
                        State.Loop1Start = 0;
                    }
                    if (State.Loop1End > State.NumFrames - 1)
                    {
                        State.Loop1End = State.NumFrames - 1;
                    }
                    if (State.Loop1End < State.Loop1Start)
                    {
                        State.Loop1End = State.Loop1Start;
                    }
                    if (State.Loop2Start < 0)
                    {
                        State.Loop2Start = 0;
                    }
                    if (State.Loop2End > State.NumFrames - 1)
                    {
                        State.Loop2End = State.NumFrames - 1;
                    }
                    if (State.Loop2End < State.Loop2Start)
                    {
                        State.Loop2End = State.Loop2Start;
                    }
                    if (State.Loop3Start < 0)
                    {
                        State.Loop3Start = 0;
                    }
                    if (State.Loop3End > State.NumFrames - 1)
                    {
                        State.Loop3End = State.NumFrames - 1;
                    }
                    if (State.Loop3End < State.Loop3Start)
                    {
                        State.Loop3End = State.Loop3Start;
                    }

                    if (State.Loop1Bidirectional)
                    {
                        State.Loop1Exists = (State.Loop1End - State.Loop1Start >= 2);
                    }
                    else
                    {
                        State.Loop1Exists = (State.Loop1Start != State.Loop1End);
                    }
                    if (State.Loop2Bidirectional)
                    {
                        State.Loop2Exists = (State.Loop2End - State.Loop2Start >= 2);
                    }
                    else
                    {
                        State.Loop2Exists = (State.Loop2Start != State.Loop2End);
                    }
                    if (State.Loop3Bidirectional)
                    {
                        State.Loop3Exists = (State.Loop3End - State.Loop3Start >= 2);
                    }
                    else
                    {
                        State.Loop3Exists = (State.Loop3Start != State.Loop3End);
                    }

                    /* set the initial state */
                    State.LoopIsReversing = false;
                    if (State.Loop1Exists)
                    {
                        State.LoopState = LoopType.eRepeatingLoop1;
                        State.CurrentLoopStart = State.Loop1Start;
                        State.CurrentLoopEnd = State.Loop1End;
                        State.CurrentLoopLength = State.Loop1End - State.Loop1Start;
                        State.CurrentLoopBidirectionality = State.Loop1Bidirectional;
                    }
                    else if (State.Loop2Exists)
                    {
                        State.LoopState = LoopType.eRepeatingLoop2;
                        State.CurrentLoopStart = State.Loop2Start;
                        State.CurrentLoopEnd = State.Loop2End;
                        State.CurrentLoopLength = State.Loop2End - State.Loop2Start;
                        State.CurrentLoopBidirectionality = State.Loop2Bidirectional;
                    }
                    else if (State.Loop3Exists)
                    {
                        State.LoopState = LoopType.eRepeatingLoop3;
                        State.CurrentLoopStart = State.Loop3Start;
                        State.CurrentLoopEnd = State.Loop3End;
                        State.CurrentLoopLength = State.Loop3End - State.Loop3Start;
                        State.CurrentLoopBidirectionality = State.Loop3Bidirectional;
                    }
                    else
                    {
                        State.LoopState = LoopType.eNoLoop;
                        State.CurrentLoopStart = 0;
                        State.CurrentLoopEnd = State.NumFrames;
                        State.CurrentLoopLength = 0;
                    }
                    State.EffectiveLoopState = State.LoopState;

                    /* compute how much time to wait before starting sample playback.  this */
                    /* is INDEPENDENT of the envelope starting points; the envelope generator */
                    /* setup handles origin alignment on its own. */
                    /* this number will assume the origin starts NOW.  the fixup routine will */
                    /* add some number to this so that the origin is properly determined. */
                    /* if the countdown is still negative, then the sample will sound like it */
                    /* is starting late.  the user can fix this by increasing the scanning gap. */
                    State.PreStartCountdown = (int)(
                        -(
                            ((((double)State.OriginPoint / ((InitialFrequency
                                * Template.FrequencyMultiplier + Template.FrequencyAdder)
                                / State.NaturalFrequency)) / State.NaturalSamplingRate)
                                - Template.TimeDisplacement)
                            * SynthParams.dEnvelopeRate + 0.5));
                    if (-State.PreStartCountdown > MaxPreOrigin)
                    {
                        MaxPreOrigin = -State.PreStartCountdown;
                    }
                }
                else
                {
                    /* no playback */
                    State.LoopState = LoopType.eSampleFinished;
                    State.EffectiveLoopState = State.LoopState;
                }

                /* State.SampleWasDefined: */
                /*   if there is no sample defined for the current pitch, then we don't */
                /*   bother generating any data */
                /* State.NumFrames > 0: */
                /*   if there is no data in the sample, then don't access the array */
                if (State.SampleWasDefined && (State.NumFrames > 0))
                {
                    if ((!State.Loop1Bidirectional || !State.Loop1Exists)
                        && (!State.Loop2Bidirectional || !State.Loop2Exists)
                        && (!State.Loop3Bidirectional || !State.Loop3Exists))
                    {
                        /* use the nice optimized ones if nothing is bidirectional. */
                        if (State.NumChannels == NumChannelsType.eSampleStereo) /* sample data stereo */
                        {
                            State.SampleGenSamples = _Sample_StereoOut_StereoSamp;
                        }
                        else /* sample data mono */
                        {
                            State.SampleGenSamples = _Sample_StereoOut_MonoSamp;
                        }
                    }
                    else
                    {
                        /* use the nasty slow ones if there's a chance of bidirectionality. */
                        if (State.NumChannels == NumChannelsType.eSampleStereo) /* sample data stereo */
                        {
                            State.SampleGenSamples = _Sample_StereoOut_StereoSamp_Bidir;
                        }
                        else /* sample data mono */
                        {
                            State.SampleGenSamples = _Sample_StereoOut_MonoSamp_Bidir;
                        }
                    }
                }
                else
                {
                    State.SampleGenSamples = _Sample_NoOutput;
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
                State.SampleLoudnessEnvelope = NewEnvelopeStateRecord(
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
                // initial value for envelope smoothing
                State.Loudness = (float)LFOGenInitialValue(
                    State.LoudnessLFOGenerator,
                    EnvelopeInitialValue(
                       State.SampleLoudnessEnvelope));

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
                    if (OnePreOrigin > MaxPreOrigin)
                    {
                        MaxPreOrigin = OnePreOrigin;
                    }
                }

                PreOriginTimeOut = MaxPreOrigin;
                StateOut = State;
                return SynthErrorCodes.eSynthDone;
            }

            public OscillatorTypes Type { get { return OscillatorTypes.eOscillatorSampled; } }
        }

        public delegate void SampleGenSamplesMethod(
            SampleStateRec State,
            int nActualFrames,
            float[] workspace,
            int lOffset,
            int rOffset,
            int loudnessWorkspaceOffset);

        /* sample oscillator state record */
        public class SampleStateRec : IOscillator
        {
            /* current sample position into the array */
            public Fixed64 SamplePosition; /* 64-bit fixed point */
            /* current increment value for the sample position */
            public Fixed64 SamplePositionDifferential; /* 64-bit fixed point */
            /* envelope tick countdown for pre-start time */
            public int PreStartCountdown;

            /* function for generating a bunch of sample points */
            public SampleGenSamplesMethod SampleGenSamples;

            /* number of frames in the sample */
            public int NumFrames;
            /* sample data pointer.  for mono, it is just an array.  for stereo, the */
            /* samples are interleaved, left channel first */
            public float[] Data;
            /* stereo or mono sample */
            public NumChannelsType NumChannels;

            /* control parameters for the sample */
            public double NaturalFrequency;
            public int NaturalSamplingRate;
            public int OriginPoint;
            public int Loop1Start;
            public int Loop2Start;
            public int Loop3Start;
            public int Loop1End;
            public int Loop2End;
            public int Loop3End;
            public bool Loop1Bidirectional;
            public bool Loop2Bidirectional;
            public bool Loop3Bidirectional;
            public bool Loop1Exists;
            public bool Loop2Exists;
            public bool Loop3Exists;
            /* state information for the current position */
            public LoopType LoopState; /* which loop is running */
            public int CurrentLoopStart;
            public int CurrentLoopLength; /* current loop length (end - start) */
            public int CurrentLoopEnd; /* current loop/total end point */
            public bool CurrentLoopBidirectionality;
            public bool LoopIsReversing; /* is loop running backwards? */
            public LoopType EffectiveLoopState; /* used until loop starts to go forward */

            // loudness envelope
            public float Loudness;
            public float PreviousLoudness;
            /* panning position for splitting envelope generator into stereo channels */
            /* 0 = left channel, 0.5 = middle, 1 = right channel */
            public float Panning;
            /* envelope that is generating the loudness information */
            public EvalEnvelopeRec SampleLoudnessEnvelope;
            /* LFO generators modifying the output of the loudness envelope generator */
            public LFOGenRec LoudnessLFOGenerator;

            /* this flag is true if the sample data was defined at the specified pitch */
            /* (and the sample vectors are thus valid) or false if there is no sample */
            /* at this pitch (and the arrays are invalid) */
            public bool SampleWasDefined;

            /* this field contains the overall volume scaling for everything so that we */
            /* can treat the envelopes as always going between 0 and 1. */
            public double NoteLoudnessScaling;

            /* this calculates the differential values for periodic pitch displacements */
            public LFOGenRec PitchLFO;
            /* pitch lfo startup counter; negative = expired */
            public int PitchLFOStartCountdown;

            /* postprocessing for this oscillator; may be null */
            public OscEffectGenRec OscEffectGenerator;

            /* static information for the sample */
            public SampleTemplateRec Template;


            /* perform one envelope update cycle, and set a new frequency for a state */
            /* object.  used for portamento and modulation of frequency (vibrato) */
            public SynthErrorCodes UpdateEnvelopes(
                double NewFrequencyHertz,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;
                SampleStateRec State = this;

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
                NewFrequencyHertz = NewFrequencyHertz * State.Template.FrequencyMultiplier
                    + State.Template.FrequencyAdder;
                Differential = (NewFrequencyHertz / State.NaturalFrequency)
                    * SynthParams.dSamplingRateReciprocal
                    * State.NaturalSamplingRate;
                if (Differential < 0)
                {
                    Differential = 0;
                }
                State.SamplePositionDifferential = new Fixed64(Differential);

                /* this is for the benefit of resampling only -- envelope generators do their */
                /* own pre-origin sequencing */
                if (State.PreStartCountdown > 0)
                {
                    State.PreStartCountdown -= 1;
                }

                error = SynthErrorCodes.eSynthDone;
                State.PreviousLoudness = State.Loudness;
                State.Loudness = (float)(State.NoteLoudnessScaling * LFOGenUpdateCycle(
                    State.LoudnessLFOGenerator,
                    EnvelopeUpdate(
                        State.SampleLoudnessEnvelope,
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

            /* fix up pre-origin time for the sample state object */
            public void FixUpPreOrigin(
                int ActualPreOrigin)
            {
                SampleStateRec State = this;

                EnvelopeStateFixUpInitialDelay(
                    State.SampleLoudnessEnvelope,
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
            }

            /* send a key-up signal to one of the oscillators */
            public void KeyUpSustain1()
            {
                SampleStateRec State = this;

                LFOGeneratorKeyUpSustain1(
                    State.LoudnessLFOGenerator);
                EnvelopeKeyUpSustain1(
                    State.SampleLoudnessEnvelope);
                LFOGeneratorKeyUpSustain1(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain1(
                        State.OscEffectGenerator);
                }

                if (State.LoopState == LoopType.eRepeatingLoop1)
                {
                    if (State.Loop2Exists)
                    {
                        State.LoopState = LoopType.eRepeatingLoop2;
                    }
                    else if (State.Loop3Exists)
                    {
                        State.LoopState = LoopType.eRepeatingLoop3;
                    }
                    else
                    {
                        State.LoopState = LoopType.eNoLoop;
                    }
                    if (!State.LoopIsReversing)
                    {
                        /* switch mode immediately if loop is running forward */
                        State.EffectiveLoopState = State.LoopState;
                        if (State.Loop2Exists)
                        {
                            State.CurrentLoopStart = State.Loop2Start;
                            State.CurrentLoopEnd = State.Loop2End;
                            State.CurrentLoopLength = State.Loop2End - State.Loop2Start;
                            State.CurrentLoopBidirectionality = State.Loop2Bidirectional;
                        }
                        else if (State.Loop3Exists)
                        {
                            State.CurrentLoopStart = State.Loop3Start;
                            State.CurrentLoopEnd = State.Loop3End;
                            State.CurrentLoopLength = State.Loop3End - State.Loop3Start;
                            State.CurrentLoopBidirectionality = State.Loop3Bidirectional;
                        }
                        else
                        {
                            State.CurrentLoopStart = 0;
                            State.CurrentLoopEnd = State.NumFrames;
                            State.CurrentLoopLength = 0;
                        }
                    }
                    else
                    {
                        /* otherwise defer change until loop starts going forward */
                    }
                }
            }

            /* send a key-up signal to one of the oscillators */
            public void KeyUpSustain2()
            {
                SampleStateRec State = this;

                LFOGeneratorKeyUpSustain2(
                    State.LoudnessLFOGenerator);
                EnvelopeKeyUpSustain2(
                    State.SampleLoudnessEnvelope);
                LFOGeneratorKeyUpSustain2(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain2(
                        State.OscEffectGenerator);
                }

                if (/*(State.LoopState == eRepeatingLoop1)*/
                    /*||*/ (State.LoopState == LoopType.eRepeatingLoop2))
                {
                    if (State.Loop3Exists)
                    {
                        State.LoopState = LoopType.eRepeatingLoop3;
                    }
                    else
                    {
                        State.LoopState = LoopType.eNoLoop;
                    }
                    if (!State.LoopIsReversing)
                    {
                        /* switch mode immediately if loop is running forward */
                        State.EffectiveLoopState = State.LoopState;
                        if (State.Loop3Exists)
                        {
                            State.CurrentLoopStart = State.Loop3Start;
                            State.CurrentLoopEnd = State.Loop3End;
                            State.CurrentLoopLength = State.Loop3End - State.Loop3Start;
                            State.CurrentLoopBidirectionality = State.Loop3Bidirectional;
                        }
                        else
                        {
                            State.CurrentLoopStart = 0;
                            State.CurrentLoopEnd = State.NumFrames;
                            State.CurrentLoopLength = 0;
                        }
                    }
                    else
                    {
                        /* otherwise defer change until loop starts going forward */
                    }
                }
            }

            /* send a key-up signal to one of the oscillators */
            public void KeyUpSustain3()
            {
                SampleStateRec State = this;

                LFOGeneratorKeyUpSustain3(
                    State.LoudnessLFOGenerator);
                EnvelopeKeyUpSustain3(
                    State.SampleLoudnessEnvelope);
                LFOGeneratorKeyUpSustain3(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain3(
                        State.OscEffectGenerator);
                }

                if (/*(State.LoopState == eRepeatingLoop1)*/
                    /*|| (State.LoopState == eRepeatingLoop2)*/
                    /*||*/ (State.LoopState == LoopType.eRepeatingLoop3))
                {
                    State.LoopState = LoopType.eNoLoop;
                    if (!State.LoopIsReversing)
                    {
                        /* switch mode immediately if loop is running forward */
                        State.EffectiveLoopState = State.LoopState;
                        State.CurrentLoopStart = 0;
                        State.CurrentLoopEnd = State.NumFrames;
                        State.CurrentLoopLength = 0;
                    }
                    else
                    {
                        /* otherwise defer change until loop starts going forward */
                    }
                }
            }

            /* restart a sample oscillator.  this is used for tie continuations */
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
                SampleStateRec State = this;

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

                State.NoteLoudnessScaling = NewLoudness
                    * State.Template.OverallOscillatorLoudness;

                EnvelopeRetriggerFromOrigin(
                    State.SampleLoudnessEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
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
                SynthErrorCodes error = SynthErrorCodes.eSynthDone;

                SampleStateRec State = this;

#if DEBUG
                Debug.Assert(!SynthParams.ScratchWorkspace1InUse);
                SynthParams.ScratchWorkspace1InUse = true;
#endif
                int loudnessWorkspaceOffset = SynthParams.ScratchWorkspace1LOffset;

                if (State.PreStartCountdown <= 0)
                {
                    if (State.OscEffectGenerator == null)
                    {
                        /* normal case */
                        if (State.LoopState != LoopType.eSampleFinished)
                        {
                            State.SampleGenSamples(
                                State,
                                nActualFrames,
                                workspace,
                                RawBufferLOffset,
                                RawBufferROffset,
                                loudnessWorkspaceOffset);
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
                        if (State.LoopState != LoopType.eSampleFinished)
                        {
                            State.SampleGenSamples(
                                State,
                                nActualFrames,
                                workspace,
                                PrivateWorkspaceLOffset,
                                PrivateWorkspaceROffset,
                                loudnessWorkspaceOffset);
                        }

#if DEBUG
                        SynthParams.ScratchWorkspace1InUse = false;
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
#endif

                return error;
            }

            /* find out if the sample oscillator has finished */
            public bool IsItFinished()
            {
                SampleStateRec State = this;

                /* we are finished when one of the following conditions is met: */
                /*  - output volume is zero AND loudness envelope is finished */
                /*  - we have run off the end of the sample */
                /*  - we are not generating any signal */
                if (!State.SampleWasDefined)
                {
                    return true;
                }
                if (State.LoopState == LoopType.eSampleFinished)
                {
                    return true;
                }
                if (IsEnvelopeAtEnd(State.SampleLoudnessEnvelope))
                {
                    if (State.Loudness == 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            /* finalize before termination */
            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                SampleStateRec State = this;

                if (State.OscEffectGenerator != null)
                {
                    FinalizeOscEffectGenerator(
                        State.OscEffectGenerator,
                        SynthParams,
                        writeOutputLogs);
                }
            }

            /* fast playback routines */

            public static void Sample_StereoOut_MonoSamp(
                SampleStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset)
            {
                /* load local values */
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                float LocalLoudness = State.Loudness;
                Fixed64 LocalSamplePosition = State.SamplePosition;
                Fixed64 LocalSamplePositionDifferential = State.SamplePositionDifferential;
                int LocalCurrentLoopEnd = State.CurrentLoopEnd;
                float[] SampleData = State.Data;

#if true // TODO:experimental - smoothing
                if (Program.Config.EnableEnvelopeSmoothing
                    // case of no motion in any smoothed axis, can use fast code path
                    && (State.Loudness != State.PreviousLoudness))
                {
                    // envelope smoothing

                    float LocalPreviousLoudness = State.PreviousLoudness;

                    // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                    if (IsLFOSampleAndHold(State.LoudnessLFOGenerator))
                    {
                        LocalPreviousLoudness = LocalLoudness;
                    }

                    if (!EnvelopeCurrentSegmentExponential(State.SampleLoudnessEnvelope))
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

                    for (int i = 0; i < nActualFrames; i++)
                    {
                        /* load outside buffer values */
                        float OrigLeft = workspace[i + lOffset];
                        float OrigRight = workspace[i + rOffset];

                        /* compute weighting and subscript */
                        float RightWeight = LocalSamplePosition.FracF;
                        int ArraySubscript = LocalSamplePosition.Int;

                        /* L+F(R-L) */
                        float LeftValue = SampleData[ArraySubscript];
                        float RightValue = SampleData[ArraySubscript + 1];
                        float CombinedValue = LeftValue + (RightWeight * (RightValue - LeftValue));

                        /* generate output */
                        LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                        workspace[i + lOffset] = OrigLeft + LeftPan * LocalLoudness * CombinedValue;
                        workspace[i + rOffset] = OrigRight + RightPan * LocalLoudness * CombinedValue;

                        /* increment pitch differential */
                        LocalSamplePosition += LocalSamplePositionDifferential;

                    CheapDoLoop:
                        if (LocalSamplePosition.Int >= LocalCurrentLoopEnd)
                        {
                            if (State.LoopState != LoopType.eNoLoop)
                            {
                                /* handle loop */
                                LocalSamplePosition.SetInt64HighHalf(LocalSamplePosition.Int - State.CurrentLoopLength);
                                goto CheapDoLoop;
                            }
                            else
                            {
                                /* end of sample -- terminate */
                                State.LoopState = LoopType.eSampleFinished;
                                State.EffectiveLoopState = State.LoopState;
                                goto BreakLoopPoint;
                            }
                        }
                    }
                }
                else
#endif
                {
                    // non-smoothing case

                    float LeftLoudness = LocalLoudness * LeftPan;
                    float RightLoudness = LocalLoudness * RightPan;

                    /* process */
                    for (int i = 0; i < nActualFrames; i++)
                    {
                        /* load outside buffer values */
                        float OrigLeft = workspace[i + lOffset];
                        float OrigRight = workspace[i + rOffset];

                        /* compute weighting and subscript */
                        float RightWeight = LocalSamplePosition.FracF;
                        int ArraySubscript = LocalSamplePosition.Int;

                        /* L+F(R-L) */
                        float LeftValue = SampleData[ArraySubscript];
                        float RightValue = SampleData[ArraySubscript + 1];
                        float CombinedValue = LeftValue + (RightWeight * (RightValue - LeftValue));

                        /* generate output */
                        workspace[i + lOffset] = OrigLeft + LeftLoudness * CombinedValue;
                        workspace[i + rOffset] = OrigRight + RightLoudness * CombinedValue;

                        /* increment pitch differential */
                        LocalSamplePosition += LocalSamplePositionDifferential;

                    CheapDoLoop:
                        if (LocalSamplePosition.Int >= LocalCurrentLoopEnd)
                        {
                            if (State.LoopState != LoopType.eNoLoop)
                            {
                                /* handle loop */
                                LocalSamplePosition.SetInt64HighHalf(LocalSamplePosition.Int - State.CurrentLoopLength);
                                goto CheapDoLoop;
                            }
                            else
                            {
                                /* end of sample -- terminate */
                                State.LoopState = LoopType.eSampleFinished;
                                State.EffectiveLoopState = State.LoopState;
                                goto BreakLoopPoint;
                            }
                        }
                    }
                }

            BreakLoopPoint:
                ;

                /* save local state back to record */
                State.SamplePosition = LocalSamplePosition;
            }

            public static void Sample_StereoOut_StereoSamp(
                SampleStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset)
            {
                /* load local values */
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                float LocalLoudness = State.Loudness;
                Fixed64 LocalSamplePosition = State.SamplePosition;
                Fixed64 LocalSamplePositionDifferential = State.SamplePositionDifferential;
                int LocalCurrentLoopEnd = State.CurrentLoopEnd;
                float[] SampleData = State.Data;

#if true // TODO:experimental - smoothing
                if (Program.Config.EnableEnvelopeSmoothing
                    // case of no motion in any smoothed axis, can use fast code path
                    && (State.Loudness != State.PreviousLoudness))
                {
                    // envelope smoothing

                    float LocalPreviousLoudness = State.PreviousLoudness;

                    // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                    if (IsLFOSampleAndHold(State.LoudnessLFOGenerator))
                    {
                        LocalPreviousLoudness = LocalLoudness;
                    }

                    if (!EnvelopeCurrentSegmentExponential(State.SampleLoudnessEnvelope))
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

                    /* process */
                    for (int i = 0; i < nActualFrames; i++)
                    {
                        /* load outside buffer values */
                        float OrigLeft = workspace[i + lOffset];
                        float OrigRight = workspace[i + rOffset];

                        /* compute weighting and subscript */
                        float RightWeight = LocalSamplePosition.FracF;
                        int ArraySubscript = LocalSamplePosition.Int;

                        /* L+F(R-L) */
                        float LeftValue = SampleData[2 * (ArraySubscript + 0)];
                        float RightValue = SampleData[2 * (ArraySubscript + 1)];
                        float CombinedLeft = LeftValue + (RightWeight * (RightValue - LeftValue));

                        /* L+F(R-L) */
                        LeftValue = SampleData[2 * (ArraySubscript + 0) + 1];
                        RightValue = SampleData[2 * (ArraySubscript + 1) + 1];
                        float CombinedRight = LeftValue + (RightWeight * (RightValue - LeftValue));

                        /* generate output */
                        LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                        workspace[i + lOffset] = OrigLeft + LeftPan * LocalLoudness * CombinedLeft;
                        workspace[i + rOffset] = OrigRight + RightPan * LocalLoudness * CombinedRight;

                        /* increment pitch differential */
                        LocalSamplePosition += LocalSamplePositionDifferential;

                    CheapDoLoop:
                        if (LocalSamplePosition.Int >= LocalCurrentLoopEnd)
                        {
                            if (State.LoopState != LoopType.eNoLoop)
                            {
                                /* handle loop */
                                LocalSamplePosition.SetInt64HighHalf(LocalSamplePosition.Int - State.CurrentLoopLength);
                                goto CheapDoLoop;
                            }
                            else
                            {
                                /* end of sample -- terminate */
                                State.LoopState = LoopType.eSampleFinished;
                                State.EffectiveLoopState = State.LoopState;
                                goto BreakLoopPoint;
                            }
                        }
                    }
                }
                else
#endif
                {
                    // non-smoothing case

                    float LeftLoudness = LocalLoudness * LeftPan;
                    float RightLoudness = LocalLoudness * RightPan;

                    /* process */
                    for (int i = 0; i < nActualFrames; i++)
                    {
                        /* load outside buffer values */
                        float OrigLeft = workspace[i + lOffset];
                        float OrigRight = workspace[i + rOffset];

                        /* compute weighting and subscript */
                        float RightWeight = LocalSamplePosition.FracF;
                        int ArraySubscript = LocalSamplePosition.Int;

                        /* L+F(R-L) */
                        float LeftValue = SampleData[2 * (ArraySubscript + 0)];
                        float RightValue = SampleData[2 * (ArraySubscript + 1)];
                        float CombinedLeft = LeftValue + (RightWeight * (RightValue - LeftValue));

                        /* L+F(R-L) */
                        LeftValue = SampleData[2 * (ArraySubscript + 0) + 1];
                        RightValue = SampleData[2 * (ArraySubscript + 1) + 1];
                        float CombinedRight = LeftValue + (RightWeight * (RightValue - LeftValue));

                        /* generate output */
                        workspace[i + lOffset] = OrigLeft + LeftLoudness * CombinedLeft;
                        workspace[i + rOffset] = OrigRight + RightLoudness * CombinedRight;

                        /* increment pitch differential */
                        LocalSamplePosition += LocalSamplePositionDifferential;

                    CheapDoLoop:
                        if (LocalSamplePosition.Int >= LocalCurrentLoopEnd)
                        {
                            if (State.LoopState != LoopType.eNoLoop)
                            {
                                /* handle loop */
                                LocalSamplePosition.SetInt64HighHalf(LocalSamplePosition.Int - State.CurrentLoopLength);
                                goto CheapDoLoop;
                            }
                            else
                            {
                                /* end of sample -- terminate */
                                State.LoopState = LoopType.eSampleFinished;
                                State.EffectiveLoopState = State.LoopState;
                                goto BreakLoopPoint;
                            }
                        }
                    }
                }

            BreakLoopPoint:
                ;

                /* save local state back to record */
                State.SamplePosition = LocalSamplePosition;
            }

            public static void Sample_NoOutput(
                SampleStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset)
            {
            }

            /* slow playback routines */

            public static void Sample_StereoOut_MonoSamp_Bidir(
                SampleStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset)
            {
                /* load local values */
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                float LocalLoudness = State.Loudness;
                Fixed64 LocalSamplePosition = State.SamplePosition;
                Fixed64 LocalSamplePositionDifferential = State.SamplePositionDifferential;
                if (State.LoopIsReversing)
                {
                    LocalSamplePositionDifferential = -LocalSamplePositionDifferential;
                }
                float[] SampleData = State.Data;

#if true // TODO:experimental - smoothing
                if (Program.Config.EnableEnvelopeSmoothing
                    // case of no motion in any smoothed axis, can use faster code path
                    && (State.Loudness != State.PreviousLoudness))
                {
                    // envelope smoothing

                    float LocalPreviousLoudness = State.PreviousLoudness;

                    // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                    if (IsLFOSampleAndHold(State.LoudnessLFOGenerator))
                    {
                        LocalPreviousLoudness = LocalLoudness;
                    }

                    if (!EnvelopeCurrentSegmentExponential(State.SampleLoudnessEnvelope))
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
                }
                else
#endif
                {
                    // no envelope smoothing

                    FloatVectorSet(
                        workspace,
                        loudnessWorkspaceOffset,
                        nActualFrames,
                        LocalLoudness);
                }

                // Multiple loop bodies optimizing envelope smoothing are not provided since this version is already
                // costly due to loop logic and is seldom used in practice.

                /* process */
                for (int i = 0; i < nActualFrames; i++)
                {
                    /* load outside buffer values */
                    float OrigLeft = workspace[i + lOffset];
                    float OrigRight = workspace[i + rOffset];

                    /* compute weighting and subscript */
                    float RightWeight = LocalSamplePosition.FracF;
                    int ArraySubscript = LocalSamplePosition.Int;

                    /* L+F(R-L) */
                    float LeftValue = SampleData[ArraySubscript];
                    float RightValue = SampleData[ArraySubscript + 1];
                    float CombinedValue = LeftValue + (RightWeight * (RightValue - LeftValue));

                    /* generate output */
                    LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                    workspace[i + lOffset] = OrigLeft + LeftPan * LocalLoudness * CombinedValue;
                    workspace[i + rOffset] = OrigRight + RightPan * LocalLoudness * CombinedValue;

                    /* increment pitch differential */
                    LocalSamplePosition += LocalSamplePositionDifferential;

                CheapDoLoop:
                    if ((!State.LoopIsReversing
                        && LocalSamplePosition.Int >= State.CurrentLoopEnd - 1)
                        || (State.LoopIsReversing
                        && LocalSamplePosition.Int < State.CurrentLoopStart))
                    {
                        if (State.EffectiveLoopState != LoopType.eNoLoop)
                        {
                            /* handle loop */
                            if (!State.CurrentLoopBidirectionality)
                            {
                                /* normal way of looping */
                                LocalSamplePosition.SetInt64HighHalf(LocalSamplePosition.Int - State.CurrentLoopLength);
                            }
                            else
                            {
                                /* loop needs to be reversed */
                                /* invert direction */
                                LocalSamplePositionDifferential = -LocalSamplePositionDifferential;
                                /* fix up the loop pointer things */
                                if (State.LoopIsReversing)
                                {
                                    /* make loop go forward again */
                                    /* fold pointer around loop start */
                                    Fixed64.Int32Int64Subtract(
                                        2 * State.CurrentLoopStart,
                                        ref LocalSamplePosition);
                                    /* reload new loop state to handle deferred changes */
                                    if (State.EffectiveLoopState != State.LoopState)
                                    {
                                        State.EffectiveLoopState = State.LoopState;
                                        switch (State.EffectiveLoopState)
                                        {
                                            default:
                                                Debug.Assert(false);
                                                throw new ArgumentException();
                                            case LoopType.eRepeatingLoop1:
                                                // can't enter loop 1
                                                Debug.Assert(false);
                                                throw new InvalidOperationException();
                                            case LoopType.eRepeatingLoop2:
                                                State.CurrentLoopStart = State.Loop2Start;
                                                State.CurrentLoopEnd = State.Loop2End;
                                                State.CurrentLoopLength = State.Loop2End
                                                    - State.Loop2Start;
                                                State.CurrentLoopBidirectionality
                                                    = State.Loop2Bidirectional;
                                                break;
                                            case LoopType.eRepeatingLoop3:
                                                State.CurrentLoopStart = State.Loop3Start;
                                                State.CurrentLoopEnd = State.Loop3End;
                                                State.CurrentLoopLength = State.Loop3End
                                                    - State.Loop3Start;
                                                State.CurrentLoopBidirectionality
                                                    = State.Loop3Bidirectional;
                                                break;
                                            case LoopType.eNoLoop:
                                                State.CurrentLoopStart = 0;
                                                State.CurrentLoopEnd = State.NumFrames;
                                                State.CurrentLoopLength = 0;
                                                break;
                                        }
                                    }
                                    /* set loop forward */
                                    State.LoopIsReversing = false;
                                }
                                else
                                {
                                    /* make loop go in reverse */
                                    /* fold pointer around loop end */
                                    Fixed64.Int32Int64Subtract(
                                        2 * State.CurrentLoopEnd,
                                        ref LocalSamplePosition);
                                    /* set loop backwards */
                                    State.LoopIsReversing = true;
                                }
                            }
                            goto CheapDoLoop;
                        }
                        else
                        {
                            /* end of sample -- terminate */
                            State.LoopState = LoopType.eSampleFinished;
                            State.EffectiveLoopState = State.LoopState;
                            goto BreakLoopPoint;
                        }
                    }
                }
            BreakLoopPoint:
                ;

                /* save local state back to record */
                State.SamplePosition = LocalSamplePosition;
            }

            public static void Sample_StereoOut_StereoSamp_Bidir(
                SampleStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset,
                int loudnessWorkspaceOffset)
            {
                /* load local values */
                float LeftPan = .5f - .5f * State.Panning; // (1 - State.Panning) / 2
                float RightPan = .5f + .5f * State.Panning; // (1 + State.Panning) / 2
                float LocalLoudness = State.Loudness;
                Fixed64 LocalSamplePosition = State.SamplePosition;
                Fixed64 LocalSamplePositionDifferential = State.SamplePositionDifferential;
                if (State.LoopIsReversing)
                {
                    LocalSamplePositionDifferential = -LocalSamplePositionDifferential;
                }
                float[] SampleData = State.Data;

#if true // TODO:experimental - smoothing
                if (Program.Config.EnableEnvelopeSmoothing
                    // case of no motion in any smoothed axis, can use faster code path
                    && (State.Loudness != State.PreviousLoudness))
                {
                    // envelope smoothing

                    float LocalPreviousLoudness = State.PreviousLoudness;

                    // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                    if (IsLFOSampleAndHold(State.LoudnessLFOGenerator))
                    {
                        LocalPreviousLoudness = LocalLoudness;
                    }

                    if (!EnvelopeCurrentSegmentExponential(State.SampleLoudnessEnvelope))
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
                }
                else
#endif
                {
                    // no envelope smoothing

                    FloatVectorSet(
                        workspace,
                        loudnessWorkspaceOffset,
                        nActualFrames,
                        LocalLoudness);
                }

                // Multiple loop bodies optimizing envelope smoothing are not provided since this version is already
                // costly due to loop logic and is seldom used in practice.

                /* process */
                for (int i = 0; i < nActualFrames; i++)
                {
                    /* load outside buffer values */
                    float OrigLeft = workspace[i + lOffset];
                    float OrigRight = workspace[i + rOffset];

                    /* compute weighting and subscript */
                    float RightWeight = LocalSamplePosition.FracF;
                    int ArraySubscript = LocalSamplePosition.Int;

                    /* L+F(R-L) */
                    float LeftValue = SampleData[2 * (ArraySubscript + 0)];
                    float RightValue = SampleData[2 * (ArraySubscript + 1)];
                    float CombinedLeft = LeftValue + (RightWeight * (RightValue - LeftValue));

                    /* L+F(R-L) */
                    LeftValue = SampleData[2 * (ArraySubscript + 0) + 1];
                    RightValue = SampleData[2 * (ArraySubscript + 1) + 1];
                    float CombinedRight = LeftValue + (RightWeight * (RightValue - LeftValue));

                    /* generate output */
                    LocalLoudness = workspace[i + loudnessWorkspaceOffset];
                    workspace[i + lOffset] = OrigLeft + LeftPan * LocalLoudness * CombinedLeft;
                    workspace[i + rOffset] = OrigRight + RightPan * LocalLoudness * CombinedRight;

                    /* increment pitch differential */
                    LocalSamplePosition += LocalSamplePositionDifferential;

                CheapDoLoop:
                    if ((!State.LoopIsReversing
                        && LocalSamplePosition.Int >= State.CurrentLoopEnd - 1)
                        || (State.LoopIsReversing
                        && LocalSamplePosition.Int < State.CurrentLoopStart))
                    {
                        if (State.EffectiveLoopState != LoopType.eNoLoop)
                        {
                            /* handle loop */
                            if (!State.CurrentLoopBidirectionality)
                            {
                                /* normal way of looping */
                                LocalSamplePosition.SetInt64HighHalf(LocalSamplePosition.Int - State.CurrentLoopLength);
                            }
                            else
                            {
                                /* loop needs to be reversed */
                                /* invert direction */
                                LocalSamplePositionDifferential = -LocalSamplePositionDifferential;
                                /* fix up the loop pointer things */
                                if (State.LoopIsReversing)
                                {
                                    /* make loop go forward again */
                                    /* fold pointer around loop start */
                                    Fixed64.Int32Int64Subtract(
                                        2 * State.CurrentLoopStart,
                                        ref LocalSamplePosition);
                                    /* reload new loop state to handle deferred changes */
                                    if (State.EffectiveLoopState != State.LoopState)
                                    {
                                        State.EffectiveLoopState = State.LoopState;
                                        switch (State.EffectiveLoopState)
                                        {
                                            default:
                                                Debug.Assert(false);
                                                throw new ArgumentException();
                                            case LoopType.eRepeatingLoop1:
                                                Debug.Assert(false);
                                                throw new InvalidOperationException();
                                            case LoopType.eRepeatingLoop2:
                                                State.CurrentLoopStart = State.Loop2Start;
                                                State.CurrentLoopEnd = State.Loop2End;
                                                State.CurrentLoopLength = State.Loop2End
                                                    - State.Loop2Start;
                                                State.CurrentLoopBidirectionality
                                                    = State.Loop2Bidirectional;
                                                break;
                                            case LoopType.eRepeatingLoop3:
                                                State.CurrentLoopStart = State.Loop3Start;
                                                State.CurrentLoopEnd = State.Loop3End;
                                                State.CurrentLoopLength = State.Loop3End
                                                    - State.Loop3Start;
                                                State.CurrentLoopBidirectionality
                                                    = State.Loop3Bidirectional;
                                                break;
                                            case LoopType.eNoLoop:
                                                State.CurrentLoopStart = 0;
                                                State.CurrentLoopEnd = State.NumFrames;
                                                State.CurrentLoopLength = 0;
                                                break;
                                        }
                                    }
                                    /* set loop forward */
                                    State.LoopIsReversing = false;
                                }
                                else
                                {
                                    /* make loop go in reverse */
                                    /* fold pointer around loop end */
                                    Fixed64.Int32Int64Subtract(
                                        2 * State.CurrentLoopEnd,
                                        ref LocalSamplePosition);
                                    /* set loop backwards */
                                    State.LoopIsReversing = true;
                                }
                            }
                            goto CheapDoLoop;
                        }
                        else
                        {
                            /* end of sample -- terminate */
                            State.LoopState = LoopType.eSampleFinished;
                            State.EffectiveLoopState = State.LoopState;
                            goto BreakLoopPoint;
                        }
                    }
                }
            BreakLoopPoint:
                ;

                /* save local state back to record */
                State.SamplePosition = LocalSamplePosition;
            }
        }
    }
}
