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
        private const double MINRATERECIPEXP = .03125; /* represents an absolute gain factor of 2^32 per cycle, at 44.1kHz */

        private const int MAXLOOKAHEAD = 1 << 24; /* maximum number of frames of delay permitted for power lookahead */

        private const float MINIMUMPEAKPOWER = 1f / (1 << 24);

        /* variant portion for track compressor */
        public class TrackComprRec
        {
            public ScalarParamEvalRec InputGain;
            public ScalarParamEvalRec OutputGain;
            public ScalarParamEvalRec NormalPower;
            public ScalarParamEvalRec ThreshPower;
            public ScalarParamEvalRec Ratio;
            public ScalarParamEvalRec FilterCutoff;
            public ScalarParamEvalRec DecayRate;
            public ScalarParamEvalRec AttackRate;
            public ScalarParamEvalRec LimitingExcess;

            public int Delay;
            public int DelayMask;
            public int DelayIndexOld;
            public int DelayIndexNew;
            public float[] LeftDelayLine;
            public float[] RightDelayLine;
            public int MaxAbsValDelayIndexOld;
            public int MaxAbsValDelayIndexNew;
            public float[] MaxAbsValDelayLine;
            // note: SplayTreeArray has better performance than SplayTree
            // TODO: the Single.CompareTo(Single) key comparer is not being inlined - figure out how
            public SplayTreeArray<float, int> PeakAbsValTree; // key: sample data value, value: count seen in window
            public float StartMax;
            public float EndMax;
            public int TransitionCount;
            public int TransitionMax;
        }

        /* variant portion for oscillator compressor */
        public class OscComprRec
        {
            public EvalEnvelopeRec InputGainEnvelope;
            public LFOGenRec InputGainLFOs;
            public EvalEnvelopeRec OutputGainEnvelope;
            public LFOGenRec OutputGainLFOs;
            public EvalEnvelopeRec NormalPowerEnvelope;
            public LFOGenRec NormalPowerLFOs;
            public EvalEnvelopeRec ThreshPowerEnvelope;
            public LFOGenRec ThreshPowerLFOs;
            public EvalEnvelopeRec RatioEnvelope;
            public LFOGenRec RatioLFOs;
            public EvalEnvelopeRec FilterCutoffEnvelope;
            public LFOGenRec FilterCutoffLFOs;
            public EvalEnvelopeRec DecayRateEnvelope;
            public LFOGenRec DecayRateLFOs;
            public EvalEnvelopeRec AttackRateEnvelope;
            public LFOGenRec AttackRateLFOs;
            public EvalEnvelopeRec LimitingExcessEnvelope;
            public LFOGenRec LimitingExcessLFOs;
        }

        public class CompressorRec : ITrackEffect, IOscillatorEffect
        {
            /* state parameters */
            public double CurrentInputGain;
            public double CurrentOutputGain;
            public double CurrentNormalPower;
            public double CurrentThreshPower;
            public double CurrentRatio;
            public double CurrentLimitingExcess;

            /* state variables */
            public FirstOrderLowpassRec LeftLowpass;
            public FirstOrderLowpassRec RightLowpass;
            public double CurrentDecayFactor;
            public double CurrentAttackFactor;
            public double CurrentEffectivePower;

            /* power estimating mode */
            public CompressorPowerEstType PowerMode;

            /* variant stuff */
            // was a tagged union in C, but given the total size, saving a pointer just doesn't matter
            // exactly one is non-null - indicating which class it is
            public TrackComprRec Track;
            public OscComprRec Oscillator;


            /* allocate common structures */
            private static CompressorRec AllocCompressorStructures(
                CompressorSpecRec Template,
                SynthParamRec SynthParams)
            {
                CompressorRec Compressor = new CompressorRec();

                /* initialize state variables here */
                Compressor.CurrentEffectivePower = 0;

                /* set up power function */
                Compressor.PowerMode = GetCompressorPowerEstimatorMode(Template);

                /* initialize filters */
                Compressor.LeftLowpass = new FirstOrderLowpassRec();
                Compressor.RightLowpass = new FirstOrderLowpassRec();

                return Compressor;
            }

            /* create a new compressor */
            public static CompressorRec NewTrackCompressor(
                CompressorSpecRec Template,
                SynthParamRec SynthParams)
            {
                /* allocate common structure */
                CompressorRec Compressor = AllocCompressorStructures(
                    Template,
                    SynthParams);

                Compressor.Track = new TrackComprRec();

                /* load all the parameters */
                GetCompressorInputGainAgg(
                    Template,
                    out Compressor.Track.InputGain);
                GetCompressorOutputGainAgg(
                    Template,
                    out Compressor.Track.OutputGain);
                GetCompressorNormalPowerAgg(
                    Template,
                    out Compressor.Track.NormalPower);
                GetCompressorThreshPowerAgg(
                    Template,
                    out Compressor.Track.ThreshPower);
                GetCompressorRatioAgg(
                    Template,
                    out Compressor.Track.Ratio);
                GetCompressorFilterFreqAgg(
                    Template,
                    out Compressor.Track.FilterCutoff);
                GetCompressorDecayRateAgg(
                    Template,
                    out Compressor.Track.DecayRate);
                GetCompressorAttackRateAgg(
                    Template,
                    out Compressor.Track.AttackRate);
                GetCompressorLimitingExcessAgg(
                    Template,
                    out Compressor.Track.LimitingExcess);

                if (Compressor.PowerMode == CompressorPowerEstType.eCompressPowerpeaklookahead)
                {
                    int DelayMask;

                    Compressor.Track.Delay = (int)(2 * SynthParams.dSamplingRate
                        / (double)Compressor.Track.FilterCutoff.SpecifiedValue);
                    if (Compressor.Track.Delay < 2)
                    {
                        Compressor.Track.Delay = 2;
                    }
                    else if (Compressor.Track.Delay > MAXLOOKAHEAD)
                    {
                        Compressor.Track.Delay = MAXLOOKAHEAD;
                    }
                    Compressor.Track.TransitionMax = Compressor.Track.Delay / 2;
                    Debug.Assert(Compressor.Track.TransitionMax > 0);
                    Compressor.Track.TransitionCount = Compressor.Track.TransitionMax;

                    DelayMask = Compressor.Track.Delay;
                    DelayMask |= (DelayMask >> 1);
                    DelayMask |= (DelayMask >> 2);
                    DelayMask |= (DelayMask >> 4);
                    DelayMask |= (DelayMask >> 8);
                    DelayMask |= (DelayMask >> 16);
                    Compressor.Track.DelayMask = DelayMask;

                    Compressor.Track.DelayIndexOld = 0;
                    Compressor.Track.DelayIndexNew = Compressor.Track.Delay;

                    Compressor.Track.MaxAbsValDelayIndexOld = 0;
                    Compressor.Track.MaxAbsValDelayIndexNew = Compressor.Track.Delay;

                    Compressor.Track.StartMax = MINIMUMPEAKPOWER;
                    Compressor.Track.EndMax = MINIMUMPEAKPOWER;

                    Compressor.Track.LeftDelayLine = new float[DelayMask + 1]; // zeroed
                    //FloatVectorZero(
                    //    Compressor.Track.LeftDelayLine,
                    //    DelayMask + 1);

                    Compressor.Track.RightDelayLine = new float[DelayMask + 1]; // zeroed
                    //FloatVectorZero(
                    //    Compressor.Track.RightDelayLine,
                    //    DelayMask + 1);

                    Compressor.Track.MaxAbsValDelayLine = new float[DelayMask + 1]; // zeroed
                    //FloatVectorZero(
                    //    Compressor.Track.MaxAbsValDelayLine,
                    //    DelayMask + 1);

                    Compressor.Track.PeakAbsValTree = new SplayTreeArray<float, int>(Compressor.Track.Delay, true/*locked*/);

                    /* ensure tree always has an item */
                    // preload tree with one node of amplitude 0, cardinality length of delay line
                    Compressor.Track.PeakAbsValTree.Add(0f, Compressor.Track.Delay);
                }

                return Compressor;
            }

            /* create a new compressor */
            public static CompressorRec NewOscCompressor(
                CompressorSpecRec Template,
                ref AccentRec Accents,
                double HurryUp,
                double InitialFrequency,
                double FreqForMultisampling,
                out int PreOriginTimeOut,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams)
            {
                int OnePreOrigin;

                /* allocate common structure */
                CompressorRec Compressor = AllocCompressorStructures(
                    Template,
                    SynthParams);

                Compressor.Oscillator = new OscComprRec();

#if DEBUG
                if (Compressor.PowerMode == CompressorPowerEstType.eCompressPowerpeaklookahead)
                {
                    // peaklookahead only permitted for track mode
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                int MaxPreOrigin = 0;

                /* allocate processors */
                Compressor.Oscillator.InputGainEnvelope = NewEnvelopeStateRecord(
                    GetCompressorInputGainEnvelope(Template),
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

                Compressor.Oscillator.InputGainLFOs = NewLFOGenerator(
                    GetCompressorInputGainLFOList(Template),
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

                Compressor.Oscillator.OutputGainEnvelope = NewEnvelopeStateRecord(
                    GetCompressorOutputGainEnvelope(Template),
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

                Compressor.Oscillator.OutputGainLFOs = NewLFOGenerator(
                    GetCompressorOutputGainLFOList(Template),
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

                Compressor.Oscillator.NormalPowerEnvelope = NewEnvelopeStateRecord(
                    GetCompressorNormalPowerEnvelope(Template),
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

                Compressor.Oscillator.NormalPowerLFOs = NewLFOGenerator(
                    GetCompressorNormalPowerLFOList(Template),
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

                Compressor.Oscillator.ThreshPowerEnvelope = NewEnvelopeStateRecord(
                    GetCompressorThreshPowerEnvelope(Template),
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

                Compressor.Oscillator.ThreshPowerLFOs = NewLFOGenerator(
                    GetCompressorThreshPowerLFOList(Template),
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

                Compressor.Oscillator.RatioEnvelope = NewEnvelopeStateRecord(
                    GetCompressorRatioEnvelope(Template),
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

                Compressor.Oscillator.RatioLFOs = NewLFOGenerator(
                    GetCompressorRatioLFOList(Template),
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

                Compressor.Oscillator.FilterCutoffEnvelope = NewEnvelopeStateRecord(
                    GetCompressorFilterFreqEnvelope(Template),
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

                Compressor.Oscillator.FilterCutoffLFOs = NewLFOGenerator(
                    GetCompressorFilterFreqLFOList(Template),
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

                Compressor.Oscillator.DecayRateEnvelope = NewEnvelopeStateRecord(
                    GetCompressorDecayRateEnvelope(Template),
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

                Compressor.Oscillator.DecayRateLFOs = NewLFOGenerator(
                    GetCompressorDecayRateLFOList(Template),
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

                Compressor.Oscillator.AttackRateEnvelope = NewEnvelopeStateRecord(
                    GetCompressorAttackRateEnvelope(Template),
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

                Compressor.Oscillator.AttackRateLFOs = NewLFOGenerator(
                    GetCompressorAttackRateLFOList(Template),
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

                Compressor.Oscillator.LimitingExcessEnvelope = NewEnvelopeStateRecord(
                    GetCompressorLimitingExcessEnvelope(Template),
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

                Compressor.Oscillator.LimitingExcessLFOs = NewLFOGenerator(
                    GetCompressorLimitingExcessLFOList(Template),
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

                PreOriginTimeOut = MaxPreOrigin;

                return Compressor;
            }

            /* fix up the origin time so that envelopes start at the proper times */
            public void OscFixEnvelopeOrigins(
                int ActualPreOriginTime)
            {
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.InputGainEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.OutputGainEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.NormalPowerEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.ThreshPowerEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.RatioEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.FilterCutoffEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.DecayRateEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.AttackRateEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.LimitingExcessEnvelope,
                    ActualPreOriginTime);

                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.InputGainLFOs,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.OutputGainLFOs,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.NormalPowerLFOs,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.ThreshPowerLFOs,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.RatioLFOs,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.FilterCutoffLFOs,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.DecayRateLFOs,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.AttackRateLFOs,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.LimitingExcessLFOs,
                    ActualPreOriginTime);
            }

            /* update compressor state with accent information */
            public SynthErrorCodes TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;
                double CurrentFilterCutoff;
                double CurrentDecayRate;
                double CurrentAttackRate;
                double Temp;

                error = ScalarParamEval(
                    this.Track.InputGain,
                    ref Accents,
                    SynthParams,
                    out this.CurrentInputGain);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.OutputGain,
                    ref Accents,
                    SynthParams,
                    out this.CurrentOutputGain);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.NormalPower,
                    ref Accents,
                    SynthParams,
                    out this.CurrentNormalPower);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.ThreshPower,
                    ref Accents,
                    SynthParams,
                    out this.CurrentThreshPower);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.Ratio,
                    ref Accents,
                    SynthParams,
                    out this.CurrentRatio);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.FilterCutoff,
                    ref Accents,
                    SynthParams,
                    out CurrentFilterCutoff);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.DecayRate,
                    ref Accents,
                    SynthParams,
                    out CurrentDecayRate);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.AttackRate,
                    ref Accents,
                    SynthParams,
                    out CurrentAttackRate);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.LimitingExcess,
                    ref Accents,
                    SynthParams,
                    out this.CurrentLimitingExcess);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                    this.LeftLowpass,
                    CurrentFilterCutoff,
                    SynthParams.dSamplingRate);
                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                    this.RightLowpass,
                    CurrentFilterCutoff,
                    SynthParams.dSamplingRate);

                Temp = CurrentDecayRate * SynthParams.dSamplingRate;
                this.CurrentDecayFactor = Math.Pow(2, -1d / Temp);

                Temp = CurrentAttackRate * SynthParams.dSamplingRate;
                if (Temp < MINRATERECIPEXP)
                {
                    Temp = MINRATERECIPEXP;
                }
                this.CurrentAttackFactor = Math.Pow(2, 1d / Temp);

                return SynthErrorCodes.eSynthDone;
            }

            /* update compressor state with accent information */
            public SynthErrorCodes OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;

                double CurrentFilterCutoff;
                double CurrentDecayRate;
                double CurrentAttackRate;
                double Temp;

                error = SynthErrorCodes.eSynthDone;
                this.CurrentInputGain = LFOGenUpdateCycle(
                    this.Oscillator.InputGainLFOs,
                    EnvelopeUpdate(
                        this.Oscillator.InputGainEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                this.CurrentOutputGain = LFOGenUpdateCycle(
                    this.Oscillator.OutputGainLFOs,
                    EnvelopeUpdate(
                        this.Oscillator.OutputGainEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                this.CurrentNormalPower = LFOGenUpdateCycle(
                    this.Oscillator.NormalPowerLFOs,
                    EnvelopeUpdate(
                        this.Oscillator.NormalPowerEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                this.CurrentThreshPower = LFOGenUpdateCycle(
                    this.Oscillator.ThreshPowerLFOs,
                    EnvelopeUpdate(
                        this.Oscillator.ThreshPowerEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                this.CurrentRatio = LFOGenUpdateCycle(
                    this.Oscillator.RatioLFOs,
                    EnvelopeUpdate(
                        this.Oscillator.RatioEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                CurrentFilterCutoff = LFOGenUpdateCycle(
                    this.Oscillator.FilterCutoffLFOs,
                    EnvelopeUpdate(
                        this.Oscillator.FilterCutoffEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                CurrentDecayRate = LFOGenUpdateCycle(
                    this.Oscillator.DecayRateLFOs,
                    EnvelopeUpdate(
                        this.Oscillator.DecayRateEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                CurrentAttackRate = LFOGenUpdateCycle(
                    this.Oscillator.AttackRateLFOs,
                    EnvelopeUpdate(
                        this.Oscillator.AttackRateEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                this.CurrentLimitingExcess = LFOGenUpdateCycle(
                    this.Oscillator.LimitingExcessLFOs,
                    EnvelopeUpdate(
                        this.Oscillator.LimitingExcessEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                    this.LeftLowpass,
                    CurrentFilterCutoff,
                    SynthParams.dSamplingRate);
                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                    this.RightLowpass,
                    CurrentFilterCutoff,
                    SynthParams.dSamplingRate);

                Temp = CurrentDecayRate * SynthParams.dSamplingRate;
                this.CurrentDecayFactor = Math.Pow(2, -1d / Temp);

                Temp = CurrentAttackRate * SynthParams.dSamplingRate;
                if (Temp < MINRATERECIPEXP)
                {
                    Temp = MINRATERECIPEXP;
                }
                this.CurrentAttackFactor = Math.Pow(2, 1d / Temp);

                return SynthErrorCodes.eSynthDone;
            }

            public void OscKeyUpSustain1()
            {
                EnvelopeKeyUpSustain1(
                    this.Oscillator.InputGainEnvelope);
                EnvelopeKeyUpSustain1(
                    this.Oscillator.OutputGainEnvelope);
                EnvelopeKeyUpSustain1(
                    this.Oscillator.NormalPowerEnvelope);
                EnvelopeKeyUpSustain1(
                    this.Oscillator.ThreshPowerEnvelope);
                EnvelopeKeyUpSustain1(
                    this.Oscillator.RatioEnvelope);
                EnvelopeKeyUpSustain1(
                    this.Oscillator.FilterCutoffEnvelope);
                EnvelopeKeyUpSustain1(
                    this.Oscillator.DecayRateEnvelope);
                EnvelopeKeyUpSustain1(
                    this.Oscillator.AttackRateEnvelope);
                EnvelopeKeyUpSustain1(
                    this.Oscillator.LimitingExcessEnvelope);

                LFOGeneratorKeyUpSustain1(
                    this.Oscillator.InputGainLFOs);
                LFOGeneratorKeyUpSustain1(
                    this.Oscillator.OutputGainLFOs);
                LFOGeneratorKeyUpSustain1(
                    this.Oscillator.NormalPowerLFOs);
                LFOGeneratorKeyUpSustain1(
                    this.Oscillator.ThreshPowerLFOs);
                LFOGeneratorKeyUpSustain1(
                    this.Oscillator.RatioLFOs);
                LFOGeneratorKeyUpSustain1(
                    this.Oscillator.FilterCutoffLFOs);
                LFOGeneratorKeyUpSustain1(
                    this.Oscillator.DecayRateLFOs);
                LFOGeneratorKeyUpSustain1(
                    this.Oscillator.AttackRateLFOs);
                LFOGeneratorKeyUpSustain1(
                    this.Oscillator.LimitingExcessLFOs);
            }

            public void OscKeyUpSustain2()
            {
                EnvelopeKeyUpSustain2(
                    this.Oscillator.InputGainEnvelope);
                EnvelopeKeyUpSustain2(
                    this.Oscillator.OutputGainEnvelope);
                EnvelopeKeyUpSustain2(
                    this.Oscillator.NormalPowerEnvelope);
                EnvelopeKeyUpSustain2(
                    this.Oscillator.ThreshPowerEnvelope);
                EnvelopeKeyUpSustain2(
                    this.Oscillator.RatioEnvelope);
                EnvelopeKeyUpSustain2(
                    this.Oscillator.FilterCutoffEnvelope);
                EnvelopeKeyUpSustain2(
                    this.Oscillator.DecayRateEnvelope);
                EnvelopeKeyUpSustain2(
                    this.Oscillator.AttackRateEnvelope);
                EnvelopeKeyUpSustain2(
                    this.Oscillator.LimitingExcessEnvelope);

                LFOGeneratorKeyUpSustain2(
                    this.Oscillator.InputGainLFOs);
                LFOGeneratorKeyUpSustain2(
                    this.Oscillator.OutputGainLFOs);
                LFOGeneratorKeyUpSustain2(
                    this.Oscillator.NormalPowerLFOs);
                LFOGeneratorKeyUpSustain2(
                    this.Oscillator.ThreshPowerLFOs);
                LFOGeneratorKeyUpSustain2(
                    this.Oscillator.RatioLFOs);
                LFOGeneratorKeyUpSustain2(
                    this.Oscillator.FilterCutoffLFOs);
                LFOGeneratorKeyUpSustain2(
                    this.Oscillator.DecayRateLFOs);
                LFOGeneratorKeyUpSustain2(
                    this.Oscillator.AttackRateLFOs);
                LFOGeneratorKeyUpSustain2(
                    this.Oscillator.LimitingExcessLFOs);
            }

            public void OscKeyUpSustain3()
            {
                EnvelopeKeyUpSustain3(
                    this.Oscillator.InputGainEnvelope);
                EnvelopeKeyUpSustain3(
                    this.Oscillator.OutputGainEnvelope);
                EnvelopeKeyUpSustain3(
                    this.Oscillator.NormalPowerEnvelope);
                EnvelopeKeyUpSustain3(
                    this.Oscillator.ThreshPowerEnvelope);
                EnvelopeKeyUpSustain3(
                    this.Oscillator.RatioEnvelope);
                EnvelopeKeyUpSustain3(
                    this.Oscillator.FilterCutoffEnvelope);
                EnvelopeKeyUpSustain3(
                    this.Oscillator.DecayRateEnvelope);
                EnvelopeKeyUpSustain3(
                    this.Oscillator.AttackRateEnvelope);
                EnvelopeKeyUpSustain3(
                    this.Oscillator.LimitingExcessEnvelope);

                LFOGeneratorKeyUpSustain3(
                    this.Oscillator.InputGainLFOs);
                LFOGeneratorKeyUpSustain3(
                    this.Oscillator.OutputGainLFOs);
                LFOGeneratorKeyUpSustain3(
                    this.Oscillator.NormalPowerLFOs);
                LFOGeneratorKeyUpSustain3(
                    this.Oscillator.ThreshPowerLFOs);
                LFOGeneratorKeyUpSustain3(
                    this.Oscillator.RatioLFOs);
                LFOGeneratorKeyUpSustain3(
                    this.Oscillator.FilterCutoffLFOs);
                LFOGeneratorKeyUpSustain3(
                    this.Oscillator.DecayRateLFOs);
                LFOGeneratorKeyUpSustain3(
                    this.Oscillator.AttackRateLFOs);
                LFOGeneratorKeyUpSustain3(
                    this.Oscillator.LimitingExcessLFOs);
            }

            /* retrigger effect envelopes from the origin point */
            public void OscRetriggerEnvelopes(
                ref AccentRec NewAccents,
                double NewHurryUp,
                double NewInitialFrequency,
                bool ActuallyRetrigger,
                SynthParamRec SynthParams)
            {
                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.InputGainEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.OutputGainEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.NormalPowerEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.ThreshPowerEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.RatioEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.FilterCutoffEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.DecayRateEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.AttackRateEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.LimitingExcessEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.InputGainLFOs,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.OutputGainLFOs,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.NormalPowerLFOs,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.ThreshPowerLFOs,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.RatioLFOs,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.FilterCutoffLFOs,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.DecayRateLFOs,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.AttackRateLFOs,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.LimitingExcessLFOs,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);
            }

            /* helper for lookahead peak limiting */
            private static void LookaheadPeakHelper(
                CompressorRec Compressor,
                int nActualFrames,
                int LeftInputOffset,
                int RightInputOffset,
                float[] workspace,
                int lOffset,
                int rOffset,
                int LeftPowerOffset,
                int RightPowerOffset,
                SynthParamRec SynthParams)
            {
                int DelayMask = Compressor.Track.DelayMask;
                float[] LeftDelayLine = Compressor.Track.LeftDelayLine;
                float[] RightDelayLine = Compressor.Track.RightDelayLine;
                float[] MaxAbsValDelayLine = Compressor.Track.MaxAbsValDelayLine;
                float CurrentInputGain = (float)Compressor.CurrentInputGain;
                int TransitionMax = Compressor.Track.TransitionMax;
                float OneOverTransitionMax = 1f / TransitionMax;

                /* construct input -- gets the delayed signal */
                int DelayIndexOld = Compressor.Track.DelayIndexOld;
                int DelayIndexNew = Compressor.Track.DelayIndexNew;
                for (int i = 0; i < nActualFrames; i += 1)
                {
                    workspace[i + LeftInputOffset] = CurrentInputGain * LeftDelayLine[DelayIndexOld];
                    workspace[i + RightInputOffset] = CurrentInputGain * RightDelayLine[DelayIndexOld];

                    LeftDelayLine[DelayIndexNew] = workspace[i + lOffset];
                    RightDelayLine[DelayIndexNew] = workspace[i + rOffset];

                    DelayIndexOld = (DelayIndexOld + 1) & DelayMask;
                    DelayIndexNew = (DelayIndexNew + 1) & DelayMask;
                }
                Compressor.Track.DelayIndexOld = DelayIndexOld;
                Compressor.Track.DelayIndexNew = DelayIndexNew;

                /* compute the max-absval into RightPower */
                FloatVectorAbsVal(
                    workspace,
                    LeftPowerOffset,
                    workspace,
                    LeftPowerOffset,
                    nActualFrames);
                FloatVectorAbsVal(
                    workspace,
                    RightPowerOffset,
                    workspace,
                    RightPowerOffset,
                    nActualFrames);
                FloatVectorMax(
                    workspace,
                    LeftPowerOffset,
                    workspace,
                    RightPowerOffset,
                    workspace,
                    LeftPowerOffset,
                    nActualFrames);
                // NaN and Inf must be eliminated to prevent PeakAbsValTree from malfunctioning
                FloatVectorCopyReplaceNaNInf(
                    workspace,
                    LeftPowerOffset,
                    workspace,
                    LeftPowerOffset,
                    nActualFrames);

                /* tracking iteration */
                DelayIndexOld = Compressor.Track.MaxAbsValDelayIndexOld;
                DelayIndexNew = Compressor.Track.MaxAbsValDelayIndexNew;
                SplayTreeArray<float, int> PeakAbsValTree = Compressor.Track.PeakAbsValTree;
                int TransitionCount = Compressor.Track.TransitionCount;
                float StartMax = Compressor.Track.StartMax;
                float EndMax = Compressor.Track.EndMax;
                for (int i = 0; i < nActualFrames; i += 1)
                {
                    float key;
                    int count;

                    // remove oldest value from heap
                    key = MaxAbsValDelayLine[DelayIndexOld];
#if DEBUG
                    if (!PeakAbsValTree.ContainsKey(key))
                    {
                        // old value should always already be in the heap
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
#endif
                    count = PeakAbsValTree.GetValue(key);
                    count--;
                    if (count > 0)
                    {
                        PeakAbsValTree.SetValue(key, count);
                    }
                    else
                    {
                        PeakAbsValTree.Remove(key);
                    }

                    /* add new value to heap */
                    key = MaxAbsValDelayLine[DelayIndexNew] = workspace[i + LeftPowerOffset];
                    if (PeakAbsValTree.ContainsKey(key))
                    {
                        count = PeakAbsValTree.GetValue(key);
                        count++;
                        PeakAbsValTree.SetValue(key, count);
                    }
                    else
                    {
                        PeakAbsValTree.Add(key, 1);
                    }

                    /* update power envelope */
                    TransitionCount -= 1;
                    float Output = (float)Math.Exp(OneOverTransitionMax
                        * (Math.Log(StartMax) * TransitionCount + Math.Log(EndMax) * (TransitionMax - TransitionCount)));
                    workspace[i + LeftPowerOffset] = Output;
                    if (TransitionCount == 0)
                    {
                        TransitionCount = TransitionMax;
                        StartMax = EndMax;
                        /* compute current maximum */
                        PeakAbsValTree.NearestLessOrEqual(Single.MaxValue, out EndMax);
                        if (EndMax < MINIMUMPEAKPOWER)
                        {
                            EndMax = MINIMUMPEAKPOWER;
                        }
                    }

                    DelayIndexOld = (DelayIndexOld + 1) & DelayMask;
                    DelayIndexNew = (DelayIndexNew + 1) & DelayMask;
                }
                Compressor.Track.MaxAbsValDelayIndexOld = DelayIndexOld;
                Compressor.Track.MaxAbsValDelayIndexNew = DelayIndexNew;
                Compressor.Track.TransitionCount = TransitionCount;
                Compressor.Track.StartMax = StartMax;
                Compressor.Track.EndMax = EndMax;
            }

            /* apply compressor to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                const float HalfPiFloat = (float)HALFPI;

                /* generate constants */
                float Q = (float)Math.Pow(.5, 1 - 1 / this.CurrentRatio);
                float A = (float)((1 / Q - 1) / this.CurrentNormalPower);
                float B = 2 - 1 / Q;

                float TangentInnerConstant = (float)(HalfPiFloat / (this.CurrentLimitingExcess - 1));
                float TangentOuterConstant = (float)(1 / (HalfPiFloat / (this.CurrentLimitingExcess - 1)));

#if DEBUG
                Debug.Assert(!SynthParams.ScratchWorkspace1InUse);
                Debug.Assert(!SynthParams.ScratchWorkspace2InUse);
                SynthParams.ScratchWorkspace1InUse = true;
                SynthParams.ScratchWorkspace2InUse = true;
#endif
                int LeftInputOffset = SynthParams.ScratchWorkspace1LOffset;
                int RightInputOffset = SynthParams.ScratchWorkspace1ROffset;
                int LeftPowerOffset = SynthParams.ScratchWorkspace2LOffset;
                int RightPowerOffset = SynthParams.ScratchWorkspace2ROffset;

                /* compute scaled floating point input data */
                FloatVectorScale(
                    workspace,
                    lOffset,
                    workspace,
                    LeftPowerOffset,
                    nActualFrames,
                    (float)this.CurrentInputGain);
                FloatVectorScale(
                    workspace,
                    rOffset,
                    workspace,
                    RightPowerOffset,
                    nActualFrames,
                    (float)this.CurrentInputGain);
                if (this.PowerMode != CompressorPowerEstType.eCompressPowerpeaklookahead)
                {
                    FloatVectorCopy(
                        workspace,
                        LeftPowerOffset,
                        workspace,
                        LeftInputOffset,
                        nActualFrames);
                    FloatVectorCopy(
                        workspace,
                        RightPowerOffset,
                        workspace,
                        RightInputOffset,
                        nActualFrames);
                }
                /* note: for most powermodes, both input and power are initialized. */
                /* but for peaklookahead, only power is initialized.  input gets initialized */
                /* below (to streamline code) */

                /* estimate power of signal */
                switch (this.PowerMode)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case CompressorPowerEstType.eCompressPowerRMS:
                        /* rms power estimator */

                        /* square the left and right input streams */
                        FloatVectorSquare(
                            workspace,
                            LeftPowerOffset,
                            workspace,
                            LeftPowerOffset,
                            nActualFrames);
                        FloatVectorSquare(
                            workspace,
                            RightPowerOffset,
                            workspace,
                            RightPowerOffset,
                            nActualFrames);

                        /* filter the streams */
                        FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                            this.LeftLowpass,
                            workspace,
                            LeftPowerOffset,
                            nActualFrames);
                        FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                            this.RightLowpass,
                            workspace,
                            RightPowerOffset,
                            nActualFrames);

                        /* compute max power & store into left stream */
                        /* first maximum is computed and then the square root is taken of */
                        /* the maximum because comparison is invariant over square root */
                        FloatVectorMax(
                            workspace,
                            LeftPowerOffset,
                            workspace,
                            RightPowerOffset,
                            workspace,
                            LeftPowerOffset,
                            nActualFrames);

                        /* compute square root */
                        FloatVectorSquareRoot(
                            workspace,
                            LeftPowerOffset,
                            nActualFrames);

                        break;

                    case CompressorPowerEstType.eCompressPowerAbsVal:
                    case CompressorPowerEstType.eCompressPowerPeak:
                        /* abs value OR peak power estimator */

                        /* take absolute value the left and right input streams */
                        FloatVectorAbsVal(
                            workspace,
                            LeftPowerOffset,
                            workspace,
                            LeftPowerOffset,
                            nActualFrames);
                        FloatVectorAbsVal(
                            workspace,
                            RightPowerOffset,
                            workspace,
                            RightPowerOffset,
                            nActualFrames);

                        /* filter the streams */
                        if (this.PowerMode == CompressorPowerEstType.eCompressPowerAbsVal)
                        {
                            FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                                this.LeftLowpass,
                                workspace,
                                LeftPowerOffset,
                                nActualFrames);
                            FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                                this.RightLowpass,
                                workspace,
                                RightPowerOffset,
                                nActualFrames);
                        }

                        /* compute max power & store into left stream */
                        FloatVectorMax(
                            workspace,
                            LeftPowerOffset,
                            workspace,
                            RightPowerOffset,
                            workspace,
                            LeftPowerOffset,
                            nActualFrames);

                        break;

                    case CompressorPowerEstType.eCompressPowerpeaklookahead:
                        LookaheadPeakHelper(
                            this,
                            nActualFrames,
                            LeftInputOffset,
                            RightInputOffset,
                            workspace,
                            lOffset,
                            rOffset,
                            LeftPowerOffset,
                            RightPowerOffset,
                            SynthParams);

                        break;
                }

                /* CompositePower[] = measured maximum power levels */

                /* compute differential lag responses */
                double LocalCurrentEffectivePower = this.CurrentEffectivePower;
                double LocalCurrentDecayFactor = this.CurrentDecayFactor;
                double LocalCurrentAttackFactor = this.CurrentAttackFactor;
                float LocalCurrentThreshPower = (float)this.CurrentThreshPower;
                for (int i = 0; i < nActualFrames; i += 1)
                {
                    double Temp = workspace[i + LeftPowerOffset];
                    if (LocalCurrentEffectivePower > Temp)
                    {
                        /* * true power is less than effective power, so follow towards */
                        /*   true power using decay rate */
                        /* * CurrentDecayFactor < 1, so it will reduce effective power */
                        /*   by multiplication */
                        LocalCurrentEffectivePower = LocalCurrentEffectivePower * LocalCurrentDecayFactor;
                        if (LocalCurrentEffectivePower < Temp)
                        {
                            /* if we overshoot, (power becomes too small) then limit it */
                            /* to go only as low as the true power */
                            LocalCurrentEffectivePower = Temp;
                        }
                    }
                    else
                    {
                        /* * true power is greater than effective power, so follow towards */
                        /*   true power using attack rate */
                        /* * CurrentAttackFactor > 1, so it will increase effective power */
                        /*   by multiplication */
                        LocalCurrentEffectivePower = LocalCurrentEffectivePower * LocalCurrentAttackFactor;
                        if (LocalCurrentEffectivePower > Temp)
                        {
                            /* if we overshoot and power becomes too large, then limit it */
                            /* to go only as high as the true power */
                            LocalCurrentEffectivePower = Temp;
                        }
                    }

                    /* constrain effective power so it does not go below threshhold */
                    if (LocalCurrentEffectivePower < LocalCurrentThreshPower)
                    {
                        LocalCurrentEffectivePower = LocalCurrentThreshPower;
                    }

                    /* save effective power */
                    workspace[i + LeftPowerOffset] = (float)LocalCurrentEffectivePower;
                }
                this.CurrentEffectivePower = LocalCurrentEffectivePower;

                /* CompositePower[] = effective power levels */

                /* nonlinear gain factor computation */
                /* curve looks like this: */
                /*    |                                                                     */
                /*    |------------O                                                        */
                /*  g |             ---                                                     */
                /*  a |                ----                                                 */
                /*  i |                    *----                                            */
                /*  n |                         --------                                    */
                /*    |                                 --------------                      */
                /*    |                                               --------------------- */
                /*    --------------------------------------------------------------------- */
                /*                                input signal power                        */
                /*                                                                          */
                /*                         ^                                                */
                /*                         * = unity gain point                             */
                /*                  ^                                                       */
                /*                  O = X value is threshhold power                         */
                /*    */
                /*  Musings: */
                /*  at compression of 1:1, every time power doubles, gain does not change */
                /*    (i.e. gain is always unity) */
                /*  at compression of 2:1, every time power doubles above threshhold, gain */
                /*    decreases to .707 of what it was.  (sqrt .5) */
                /*  at compression of Inf:1 (limiting), every time power doubles above */
                /*    threshhold, gain decreases to .5 of what it was (leaving output signal */
                /*    always having the same power) */
                /*  */
                /*  thus factor X would be (.5) ^ (1 - 1 / compression factor) */
                /*  */
                /*  fitting gain to reciprocal function */
                /*   given constants: */
                /*    p = unity power */
                /*    c = compression factor */
                /*   constraints: */
                /*    1   1 / (a * p + b) = 1 */
                /*    2   1 / (a * 2p + b) = (1/2) ^ (1 - 1 / c) */
                /*   let */
                /*    3   q = (1/2) ^ (1 - 1 / c) */
                /*   solution for a and b: */
                /*    4   a * p + b = 1   multiply 1 by a*p+b */
                /*    5   a * 2p + b = 1 / q   reciprocal both sides of 2 */
                /*    6   a * p = 1 - b   subtract b from 4 */
                /*    7   a = (1 - b) / p   divide 6 by p */
                /*    8   a * 2p = 1 / q - b   subtract b from 5 */
                /*    9   a = (1 / q - b) / 2p   divide 8 by 2p */
                /*    10  (1 - b) / p = (1 / q - b) / 2p   substitute a from 7 into 9 */
                /*    11  1 - b = (1 / q - b) / 2   multiply 10 by p */
                /*    12  2 - 2b = 1 / q - b   multiply 11 by 2 */
                /*    13  2 - b = 1 / q   add b to 12 */
                /*    14  - b = 1 / q - 2   subtract 2 from 13 */
                /*    15  b = 2 - 1 / q   negate 14 */
                /*    16  a = (1 - (2 - 1 / q)) / p   substitute b from 15 into b in 7 */
                /*    17  a = (1 - 2 + 1 / q) / p   simplify 16 */
                /*    18  a = (1 / q - 1) / p   simplify 17 */
                /* note:  effectivepower has been constrained > threshhold somewhere above */

                /* compute gain from effective power and store into CompositePower vector */
                for (int i = 0; i < nActualFrames; i += 1)
                {
                    workspace[i + LeftPowerOffset] = 1f / (A * workspace[i + LeftPowerOffset] + B);
                }

                /* CompositePower[] = gain values */

                float LocalCurrentOutputGain = (float)this.CurrentOutputGain;

                /* apply gain to input signal and then limit */
                for (int i = 0; i < nActualFrames; i += 1)
                {
                    float PowerThing = workspace[i + LeftPowerOffset];

                    float LeftTemp = workspace[i + LeftInputOffset] * PowerThing;
                    float RightTemp = workspace[i + RightInputOffset] * PowerThing;

                    if (LeftTemp > 1)
                    {
                        /* X exceeds normal channel boundary on top, apply limiter */
                        /* LeftTemp = 1 + Math.Atan((LeftTemp - 1) * HalfPi */
                        /*   / (this.CurrentLimitingExcess - 1)) */
                        /*   / (HalfPi / (this.CurrentLimitingExcess - 1)); */
                        LeftTemp = (float)(1 + TangentOuterConstant * Math.Atan((LeftTemp - 1) * TangentInnerConstant));
                    }
                    if (LeftTemp < -1)
                    {
                        /* X exceeds normal channel boundary on bottom, apply limiter */
                        LeftTemp = (float)(-1 + TangentOuterConstant * Math.Atan((LeftTemp + 1) * TangentInnerConstant));
                    }

                    if (RightTemp > 1)
                    {
                        /* X exceeds normal channel boundary on top, apply limiter */
                        RightTemp = (float)(1 + TangentOuterConstant * Math.Atan((RightTemp - 1) * TangentInnerConstant));
                    }
                    if (RightTemp < -1)
                    {
                        /* X exceeds normal channel boundary on bottom, apply limiter */
                        RightTemp = (float)(-1 + TangentOuterConstant * Math.Atan((RightTemp + 1) * TangentInnerConstant));
                    }

                    workspace[i + lOffset] = LeftTemp * LocalCurrentOutputGain;
                    workspace[i + rOffset] = RightTemp * LocalCurrentOutputGain;
                }

#if DEBUG
                SynthParams.ScratchWorkspace1InUse = false;
                SynthParams.ScratchWorkspace2InUse = false;
#endif

                return SynthErrorCodes.eSynthDone;
            }

            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                if (Oscillator != null)
                {
                    FreeEnvelopeStateRecord(
                        ref Oscillator.InputGainEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref Oscillator.InputGainLFOs,
                        SynthParams);

                    FreeEnvelopeStateRecord(
                        ref Oscillator.OutputGainEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref Oscillator.OutputGainLFOs,
                        SynthParams);

                    FreeEnvelopeStateRecord(
                        ref Oscillator.NormalPowerEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref Oscillator.NormalPowerLFOs,
                        SynthParams);

                    FreeEnvelopeStateRecord(
                        ref Oscillator.ThreshPowerEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref Oscillator.ThreshPowerLFOs,
                        SynthParams);

                    FreeEnvelopeStateRecord(
                        ref Oscillator.RatioEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref Oscillator.RatioLFOs,
                        SynthParams);

                    FreeEnvelopeStateRecord(
                        ref Oscillator.FilterCutoffEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref Oscillator.FilterCutoffLFOs,
                        SynthParams);

                    FreeEnvelopeStateRecord(
                        ref Oscillator.DecayRateEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref Oscillator.DecayRateLFOs,
                        SynthParams);

                    FreeEnvelopeStateRecord(
                        ref Oscillator.AttackRateEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref Oscillator.AttackRateLFOs,
                        SynthParams);

                    FreeEnvelopeStateRecord(
                        ref Oscillator.LimitingExcessEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref Oscillator.LimitingExcessLFOs,
                        SynthParams);
                }
            }
        }
    }
}
