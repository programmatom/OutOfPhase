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
        public interface IFilter
        {
            FilterTypes FilterType { get; }

            void UpdateParams(
                ref FilterParams Params);

            void Apply(
                float[] XinVector,
                int XinVectorOffset,
                float[] YoutVector,
                int YoutVectorOffset,
                int VectorLength,
                float OutputScaling,
                SynthParamRec SynthParams);
        }

        public struct FilterParams
        {
            public readonly double SamplingRate;
            public readonly double Cutoff;
            public readonly double BandwidthOrSlope;
            public readonly double Gain;

            public FilterParams(
                double SamplingRate,
                double Cutoff,
                double BandwidthOrSlope,
                double Gain)
            {
                this.SamplingRate = SamplingRate;
                this.Cutoff = Cutoff;
                this.BandwidthOrSlope = BandwidthOrSlope;
                this.Gain = Gain;
            }
        }

        public enum FilterParam
        {
            Cutoff,
            BandwidthOrSlope,
            Gain,
        }

        public struct FilterInfo
        {
            public readonly FilterTypes FilterType;
            public readonly byte ParamsMask; // bitfield or'd of (1 << FilterParamsEnum.X)

            public FilterInfo(
                FilterTypes FilterType,
                int ParamsMask)
            {
                this.FilterType = FilterType;
                this.ParamsMask = (byte)ParamsMask;
            }
        }

        public static readonly FilterInfo[] FilterInfos = CreateFilterInfo();
        private static FilterInfo[] CreateFilterInfo()
        {
            // All filters specify FilterParam.Cutoff - including FilterTypes.eFilterNull which in fact does not use the
            // cutoff, and it's the only one, so saying it does allows us to eliminate conditionals for the cutoff param generation.
            FilterInfo[] filterInfo = new FilterInfo[(int)FilterTypes.Count];
            filterInfo[(int)FilterTypes.eFilterNull] = new FilterInfo(FilterTypes.eFilterNull,
                (1 << (int)FilterParam.Cutoff));
            filterInfo[(int)FilterTypes.eFilterFirstOrderLowpass] = new FilterInfo(FilterTypes.eFilterFirstOrderLowpass,
                (1 << (int)FilterParam.Cutoff));
            filterInfo[(int)FilterTypes.eFilterFirstOrderHighpass] = new FilterInfo(FilterTypes.eFilterFirstOrderHighpass,
                (1 << (int)FilterParam.Cutoff));
            filterInfo[(int)FilterTypes.eFilterSecondOrderResonant] = new FilterInfo(FilterTypes.eFilterSecondOrderResonant,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.BandwidthOrSlope));
            filterInfo[(int)FilterTypes.eFilterSecondOrderZero] = new FilterInfo(FilterTypes.eFilterSecondOrderZero,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.BandwidthOrSlope));
            filterInfo[(int)FilterTypes.eFilterButterworthLowpass] = new FilterInfo(FilterTypes.eFilterButterworthLowpass,
                (1 << (int)FilterParam.Cutoff));
            filterInfo[(int)FilterTypes.eFilterButterworthHighpass] = new FilterInfo(FilterTypes.eFilterButterworthHighpass,
                (1 << (int)FilterParam.Cutoff));
            filterInfo[(int)FilterTypes.eFilterButterworthBandpass] = new FilterInfo(FilterTypes.eFilterButterworthBandpass,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.BandwidthOrSlope));
            filterInfo[(int)FilterTypes.eFilterButterworthBandreject] = new FilterInfo(FilterTypes.eFilterButterworthBandreject,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.BandwidthOrSlope));
            filterInfo[(int)FilterTypes.eFilterParametricEQ] = new FilterInfo(FilterTypes.eFilterParametricEQ,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.BandwidthOrSlope) | (1 << (int)FilterParam.Gain));
            filterInfo[(int)FilterTypes.eFilterParametricEQ2] = new FilterInfo(FilterTypes.eFilterParametricEQ2,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.BandwidthOrSlope) | (1 << (int)FilterParam.Gain));
            filterInfo[(int)FilterTypes.eFilterLowShelfEQ] = new FilterInfo(FilterTypes.eFilterLowShelfEQ,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.BandwidthOrSlope) | (1 << (int)FilterParam.Gain));
            filterInfo[(int)FilterTypes.eFilterHighShelfEQ] = new FilterInfo(FilterTypes.eFilterHighShelfEQ,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.BandwidthOrSlope) | (1 << (int)FilterParam.Gain));
            filterInfo[(int)FilterTypes.eFilterResonantLowpass] = new FilterInfo(FilterTypes.eFilterResonantLowpass,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.BandwidthOrSlope) | (1 << (int)FilterParam.Gain));
            filterInfo[(int)FilterTypes.eFilterResonantLowpass2] = new FilterInfo(FilterTypes.eFilterResonantLowpass2,
                (1 << (int)FilterParam.Cutoff) | (1 << (int)FilterParam.Gain));
            return filterInfo;
        }

        /* source of parameters for an oscillator filter */
        public class OscFilterParamRec
        {
            public EvalEnvelopeRec CutoffEnvelope;
            public LFOGenRec CutoffLFO;
            public EvalEnvelopeRec BandwidthOrSlopeEnvelope;
            public LFOGenRec BandwidthOrSlopeLFO;
            public EvalEnvelopeRec OutputMultiplierEnvelope;
            public LFOGenRec OutputMultiplierLFO;
            public EvalEnvelopeRec GainEnvelope;
            public LFOGenRec GainLFO;
        }

        /* source of parameters for a track/score filter */
        public class TrackFilterParamRec
        {
            public ScalarParamEvalRec Cutoff;
            public ScalarParamEvalRec BandwidthOrSlope;
            public ScalarParamEvalRec OutputMultiplier;
            public ScalarParamEvalRec Gain;
        }

        /* combined single filter state record */
        public class FilterRec
        {
            /* common state info */
            public float CurrentMultiplier; // aka outputscaling
            public float PreviousMultiplier;
            public FilterTypes FilterType;
            public byte FilterParamMask;
            public IFilter Left;
            public IFilter Right;

            /* variant state info */
            // was a tagged union in C, but given the total size, saving a pointer just doesn't matter
            // exactly one is non-null - indicating which class it is
            public OscFilterParamRec OscFilter;
            public TrackFilterParamRec TrackFilter;
        }

        /* structure for the whole overall filter */
        public class FilterArrayRec : ITrackEffect, IOscillatorEffect
        {
            public FilterRec[] FilterVector;


            private static void CreateFilters(
                out IFilter Left,
                out IFilter Right,
                int FilterIndex,
                FilterSpecRec Template,
                SynthParamRec SynthParams)
            {
                Left = null;
                Right = null;

                bool LeftChannel;
                bool RightChannel;

                /* figure out what channels are needed */
                switch (GetFilterChannel(Template, FilterIndex))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case FilterChannels.eFilterLeft:
                        LeftChannel = true;
                        RightChannel = false;
                        break;

                    case FilterChannels.eFilterRight:
                        LeftChannel = false;
                        RightChannel = true;
                        break;

                    case FilterChannels.eFilterBoth:
                        LeftChannel = true;
                        RightChannel = true;
                        break;
                }

                if (LeftChannel)
                {
                    Left = CreateFilter(Template, FilterIndex);
                    Debug.Assert(Left.FilterType == GetFilterType(Template, FilterIndex));
                }

                if (RightChannel)
                {
                    Right = CreateFilter(Template, FilterIndex);
                    Debug.Assert(Right.FilterType == GetFilterType(Template, FilterIndex));
                }
            }

            private static IFilter CreateFilter(
                FilterSpecRec Template,
                int FilterIndex)
            {
                FilterTypes Type = GetFilterType(Template, FilterIndex);
                IFilter filter;
                switch (Type)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case FilterTypes.eFilterFirstOrderLowpass:
                        filter = new FirstOrderLowpassRec();
                        break;
                    case FilterTypes.eFilterFirstOrderHighpass:
                        filter = new FirstOrderHighpassRec();
                        break;
                    case FilterTypes.eFilterSecondOrderResonant:
                        filter = new SecondOrderResonRec(
                            GetFilterScalingMode(Template, FilterIndex));
                        break;
                    case FilterTypes.eFilterSecondOrderZero:
                        filter = new SecondOrderZeroRec(
                            GetFilterScalingMode(Template, FilterIndex));
                        break;
                    case FilterTypes.eFilterButterworthLowpass:
                        filter = new ButterworthLowpassRec();
                        break;
                    case FilterTypes.eFilterButterworthHighpass:
                        filter = new ButterworthHighpassRec();
                        break;
                    case FilterTypes.eFilterButterworthBandpass:
                        filter = new ButterworthBandpassRec();
                        break;
                    case FilterTypes.eFilterButterworthBandreject:
                        filter = new ButterworthBandrejectRec();
                        break;
                    case FilterTypes.eFilterParametricEQ:
                        filter = new ParametricEqualizerRec();
                        break;
                    case FilterTypes.eFilterParametricEQ2:
                        filter = new ParametricEqualizer2Rec();
                        break;
                    case FilterTypes.eFilterLowShelfEQ:
                        filter = new LowShelfEqualizerRec();
                        break;
                    case FilterTypes.eFilterHighShelfEQ:
                        filter = new HighShelfEqualizerRec();
                        break;
                    case FilterTypes.eFilterResonantLowpass:
                        filter = new ResonantLowpassRec(
                            GetFilterLowpassOrder(Template, FilterIndex),
                            GetFilterBandpassOrder(Template, FilterIndex));
                        break;
                    case FilterTypes.eFilterResonantLowpass2:
                        filter = new ResonantLowpass2Rec(
                            GetFilterLowpassOrder(Template, FilterIndex),
                            GetFilterBroken(Template, FilterIndex));
                        break;
                    case FilterTypes.eFilterNull:
                        filter = new FilterNullRec();
                        break;
                }
                Debug.Assert(filter.FilterType == Type);
                return filter;
            }

            /* create a new parallel filter processor for oscillator effects chain (enveloped) */
            public static FilterArrayRec NewOscFilterArrayProcessor(
                FilterSpecRec Template,
                ref AccentRec Accents,
                double HurryUp,
                double InitialFrequency,
                double FreqForMultisampling,
                out int PreOriginTimeOut,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams)
            {
                int OnePreOrigin;

                int count = GetNumFiltersInSpec(Template);

                FilterArrayRec Array = new FilterArrayRec();
                Array.FilterVector = new FilterRec[count];

                /* remembers maximum amount of pre-origin time required */
                int MaxPreOrigin = 0;

                /* build filter table */
                for (int i = 0; i < count; i++)
                {
                    FilterRec Filter = Array.FilterVector[i] = new FilterRec();
                    Filter.OscFilter = new OscFilterParamRec();

                    /* what kind of filter is this */
                    Filter.FilterType = GetFilterType(Template, i);
                    Filter.FilterParamMask = FilterInfos[(int)Filter.FilterType].ParamsMask;

                    /* envelopes and LFOs */

                    Filter.OscFilter.OutputMultiplierEnvelope = NewEnvelopeStateRecord(
                        GetFilterOutputEnvelope(Template, i),
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

                    Filter.OscFilter.OutputMultiplierLFO = NewLFOGenerator(
                        GetFilterOutputLFO(Template, i),
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
                    Filter.CurrentMultiplier = (float)LFOGenInitialValue(
                        Filter.OscFilter.OutputMultiplierLFO,
                        EnvelopeInitialValue(
                           Filter.OscFilter.OutputMultiplierEnvelope));

                    Debug.Assert((Filter.FilterParamMask & (1 << (int)FilterParam.Cutoff)) != 0);
                    {
                        Filter.OscFilter.CutoffEnvelope = NewEnvelopeStateRecord(
                            GetFilterCutoffEnvelope(Template, i),
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
                        Filter.OscFilter.CutoffLFO = NewLFOGenerator(
                            GetFilterCutoffLFO(Template, i),
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
                    }

                    if ((Filter.FilterParamMask & (1 << (int)FilterParam.BandwidthOrSlope)) != 0)
                    {
                        Filter.OscFilter.BandwidthOrSlopeEnvelope = NewEnvelopeStateRecord(
                            GetFilterBandwidthOrSlopeEnvelope(Template, i),
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
                        Filter.OscFilter.BandwidthOrSlopeLFO = NewLFOGenerator(
                            GetFilterBandwidthOrSlopeLFO(Template, i),
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
                    }

                    if ((Filter.FilterParamMask & (1 << (int)FilterParam.Gain)) != 0)
                    {
                        Filter.OscFilter.GainEnvelope = NewEnvelopeStateRecord(
                            GetFilterGainEnvelope(Template, i),
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
                        Filter.OscFilter.GainLFO = NewLFOGenerator(
                            GetFilterGainLFO(Template, i),
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
                    }

                    CreateFilters(
                        out Filter.Left,
                        out Filter.Right,
                        i,
                        Template,
                        SynthParams);
                }

                PreOriginTimeOut = MaxPreOrigin;

                return Array;
            }

            /* create a new parallel filter processor for track/score effects chain */
            public static FilterArrayRec NewTrackFilterArrayProcessor(
                FilterSpecRec Template,
                SynthParamRec SynthParams)
            {
                int count = GetNumFiltersInSpec(Template);

                FilterArrayRec Array = new FilterArrayRec();
                Array.FilterVector = new FilterRec[count];

                /* create each filter */
                for (int i = 0; i < count; i++)
                {
                    FilterRec Filter = Array.FilterVector[i] = new FilterRec();
                    Filter.TrackFilter = new TrackFilterParamRec();

                    /* load type parameters */
                    Filter.FilterType = GetFilterType(Template, i);
                    Filter.FilterParamMask = FilterInfos[(int)Filter.FilterType].ParamsMask;

                    /* load control parameters */
                    Debug.Assert((Filter.FilterParamMask & (1 << (int)FilterParam.Cutoff)) != 0);
                    {
                        GetFilterCutoffAgg(
                            Template,
                            i,
                            out Filter.TrackFilter.Cutoff);
                    }
                    if ((Filter.FilterParamMask & (1 << (int)FilterParam.BandwidthOrSlope)) != 0)
                    {
                        GetFilterBandwidthOrSlopeAgg(
                            Template,
                            i,
                            out Filter.TrackFilter.BandwidthOrSlope);
                    }
                    if ((Filter.FilterParamMask & (1 << (int)FilterParam.Gain)) != 0)
                    {
                        GetFilterGainAgg(
                            Template,
                            i,
                            out Filter.TrackFilter.Gain);
                    }
                    GetFilterOutputMultiplierAgg(
                        Template,
                        i,
                        out Filter.TrackFilter.OutputMultiplier);

                    CreateFilters(
                        out Filter.Left,
                        out Filter.Right,
                        i,
                        Template,
                        SynthParams);
                }

                return Array;
            }

            /* fix up the origin time so that envelopes start at the proper times */
            public void OscFixEnvelopeOrigins(
                int ActualPreOriginTime)
            {
                for (int i = 0; i < FilterVector.Length; i++)
                {
                    FilterRec Scan = FilterVector[i];

                    EnvelopeStateFixUpInitialDelay(
                        Scan.OscFilter.OutputMultiplierEnvelope,
                        ActualPreOriginTime);
                    if (Scan.OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeStateFixUpInitialDelay(
                            Scan.OscFilter.CutoffEnvelope,
                            ActualPreOriginTime);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeStateFixUpInitialDelay(
                            Scan.OscFilter.BandwidthOrSlopeEnvelope,
                            ActualPreOriginTime);
                    }
                    if (Scan.OscFilter.GainEnvelope != null)
                    {
                        EnvelopeStateFixUpInitialDelay(
                            Scan.OscFilter.GainEnvelope,
                            ActualPreOriginTime);
                    }

                    LFOGeneratorFixEnvelopeOrigins(
                        Scan.OscFilter.OutputMultiplierLFO,
                        ActualPreOriginTime);
                    if (Scan.OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorFixEnvelopeOrigins(
                            Scan.OscFilter.CutoffLFO,
                            ActualPreOriginTime);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorFixEnvelopeOrigins(
                            Scan.OscFilter.BandwidthOrSlopeLFO,
                            ActualPreOriginTime);
                    }
                    if (Scan.OscFilter.GainLFO != null)
                    {
                        LFOGeneratorFixEnvelopeOrigins(
                            Scan.OscFilter.GainLFO,
                            ActualPreOriginTime);
                    }

                }
            }

            /* update filter state with accent information */
            public SynthErrorCodes OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams)
            {
                for (int i = 0; i < FilterVector.Length; i++)
                {
                    SynthErrorCodes error;

                    double Cutoff = Double.NaN;
                    double BandwidthOrSlope = Double.NaN;
                    double Gain = Double.NaN;

                    FilterRec Scan = FilterVector[i];

                    error = SynthErrorCodes.eSynthDone;
                    Scan.PreviousMultiplier = Scan.CurrentMultiplier;
                    Scan.CurrentMultiplier = (float)LFOGenUpdateCycle(
                        Scan.OscFilter.OutputMultiplierLFO,
                        EnvelopeUpdate(
                            Scan.OscFilter.OutputMultiplierEnvelope,
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

                    if (Scan.OscFilter.CutoffEnvelope != null)
                    {
                        error = SynthErrorCodes.eSynthDone;
                        Cutoff = LFOGenUpdateCycle(
                            Scan.OscFilter.CutoffLFO,
                            EnvelopeUpdate(
                                Scan.OscFilter.CutoffEnvelope,
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
                    }

                    if (Scan.OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        error = SynthErrorCodes.eSynthDone;
                        BandwidthOrSlope = LFOGenUpdateCycle(
                            Scan.OscFilter.BandwidthOrSlopeLFO,
                            EnvelopeUpdate(
                                Scan.OscFilter.BandwidthOrSlopeEnvelope,
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
                    }

                    if (Scan.OscFilter.GainEnvelope != null)
                    {
                        error = SynthErrorCodes.eSynthDone;
                        Gain = LFOGenUpdateCycle(
                            Scan.OscFilter.GainLFO,
                            EnvelopeUpdate(
                                Scan.OscFilter.GainEnvelope,
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
                    }

                    FilterParams Params = new FilterParams(
                        SynthParams.dSamplingRate,
                        Cutoff,
                        BandwidthOrSlope,
                        Gain);
                    if (Scan.Left != null)
                    {
                        Scan.Left.UpdateParams(ref Params);
                    }
                    if (Scan.Right != null)
                    {
                        Scan.Right.UpdateParams(ref Params);
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            public void OscKeyUpSustain1()
            {
                for (int i = 0; i < FilterVector.Length; i++)
                {
                    FilterRec Scan = FilterVector[i];

                    EnvelopeKeyUpSustain1(Scan.OscFilter.OutputMultiplierEnvelope);
                    if (Scan.OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeKeyUpSustain1(Scan.OscFilter.CutoffEnvelope);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeKeyUpSustain1(Scan.OscFilter.BandwidthOrSlopeEnvelope);
                    }
                    if (Scan.OscFilter.GainEnvelope != null)
                    {
                        EnvelopeKeyUpSustain1(Scan.OscFilter.GainEnvelope);
                    }

                    LFOGeneratorKeyUpSustain1(Scan.OscFilter.OutputMultiplierLFO);
                    if (Scan.OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorKeyUpSustain1(Scan.OscFilter.CutoffLFO);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorKeyUpSustain1(Scan.OscFilter.BandwidthOrSlopeLFO);
                    }
                    if (Scan.OscFilter.GainLFO != null)
                    {
                        LFOGeneratorKeyUpSustain1(Scan.OscFilter.GainLFO);
                    }
                }
            }

            public void OscKeyUpSustain2()
            {
                for (int i = 0; i < FilterVector.Length; i++)
                {
                    FilterRec Scan = FilterVector[i];

                    EnvelopeKeyUpSustain2(Scan.OscFilter.OutputMultiplierEnvelope);
                    if (Scan.OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeKeyUpSustain2(Scan.OscFilter.CutoffEnvelope);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeKeyUpSustain2(Scan.OscFilter.BandwidthOrSlopeEnvelope);
                    }
                    if (Scan.OscFilter.GainEnvelope != null)
                    {
                        EnvelopeKeyUpSustain2(Scan.OscFilter.GainEnvelope);
                    }

                    LFOGeneratorKeyUpSustain2(Scan.OscFilter.OutputMultiplierLFO);
                    if (Scan.OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorKeyUpSustain2(Scan.OscFilter.CutoffLFO);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorKeyUpSustain2(Scan.OscFilter.BandwidthOrSlopeLFO);
                    }
                    if (Scan.OscFilter.GainLFO != null)
                    {
                        LFOGeneratorKeyUpSustain2(Scan.OscFilter.GainLFO);
                    }
                }
            }

            public void OscKeyUpSustain3()
            {
                for (int i = 0; i < FilterVector.Length; i++)
                {
                    FilterRec Scan = FilterVector[i];

                    EnvelopeKeyUpSustain3(Scan.OscFilter.OutputMultiplierEnvelope);
                    if (Scan.OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeKeyUpSustain3(Scan.OscFilter.CutoffEnvelope);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeKeyUpSustain3(Scan.OscFilter.BandwidthOrSlopeEnvelope);
                    }
                    if (Scan.OscFilter.GainEnvelope != null)
                    {
                        EnvelopeKeyUpSustain3(Scan.OscFilter.GainEnvelope);
                    }

                    LFOGeneratorKeyUpSustain3(Scan.OscFilter.OutputMultiplierLFO);
                    if (Scan.OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorKeyUpSustain3(Scan.OscFilter.CutoffLFO);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorKeyUpSustain3(Scan.OscFilter.BandwidthOrSlopeLFO);
                    }
                    if (Scan.OscFilter.GainLFO != null)
                    {
                        LFOGeneratorKeyUpSustain3(Scan.OscFilter.GainLFO);
                    }
                }
            }

            /* retrigger effect envelopes from the origin point */
            public void OscRetriggerEnvelopes(
                ref AccentRec NewAccents,
                double NewHurryUp,
                double NewInitialFrequency,
                bool ActuallyRetrigger,
                SynthParamRec SynthParams)
            {
                for (int i = 0; i < FilterVector.Length; i++)
                {
                    FilterRec Scan = FilterVector[i];

                    EnvelopeRetriggerFromOrigin(
                        Scan.OscFilter.OutputMultiplierEnvelope,
                        ref NewAccents,
                        NewInitialFrequency,
                        1,
                        NewHurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    if (Scan.OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeRetriggerFromOrigin(
                            Scan.OscFilter.CutoffEnvelope,
                            ref NewAccents,
                            NewInitialFrequency,
                            1,
                            NewHurryUp,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeRetriggerFromOrigin(
                            Scan.OscFilter.BandwidthOrSlopeEnvelope,
                            ref NewAccents,
                            NewInitialFrequency,
                            1,
                            NewHurryUp,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                    if (Scan.OscFilter.GainEnvelope != null)
                    {
                        EnvelopeRetriggerFromOrigin(
                            Scan.OscFilter.GainEnvelope,
                            ref NewAccents,
                            NewInitialFrequency,
                            1,
                            NewHurryUp,
                            ActuallyRetrigger,
                            SynthParams);
                    }

                    LFOGeneratorRetriggerFromOrigin(
                        Scan.OscFilter.OutputMultiplierLFO,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        1,
                        1,
                        ActuallyRetrigger,
                        SynthParams);
                    if (Scan.OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorRetriggerFromOrigin(
                            Scan.OscFilter.CutoffLFO,
                            ref NewAccents,
                            NewInitialFrequency,
                            NewHurryUp,
                            1,
                            1,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                    if (Scan.OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorRetriggerFromOrigin(
                            Scan.OscFilter.BandwidthOrSlopeLFO,
                            ref NewAccents,
                            NewInitialFrequency,
                            NewHurryUp,
                            1,
                            1,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                    if (Scan.OscFilter.GainLFO != null)
                    {
                        LFOGeneratorRetriggerFromOrigin(
                            Scan.OscFilter.GainLFO,
                            ref NewAccents,
                            NewInitialFrequency,
                            NewHurryUp,
                            1,
                            1,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                }
            }

            /* update filter state with accent information */
            public SynthErrorCodes TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams)
            {
                for (int i = 0; i < FilterVector.Length; i++)
                {
                    SynthErrorCodes error;

                    double Cutoff = Double.NaN;
                    double BandwidthOrSlope = Double.NaN;
                    double Gain = Double.NaN;

                    FilterRec Scan = FilterVector[i];

                    double OutputMultiplier;
                    error = ScalarParamEval(
                        Scan.TrackFilter.OutputMultiplier,
                        ref Accents,
                        SynthParams,
                        out OutputMultiplier);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                    Scan.PreviousMultiplier = Scan.CurrentMultiplier;
                    Scan.CurrentMultiplier = (float)OutputMultiplier;

                    Debug.Assert((Scan.FilterParamMask & (1 << (int)FilterParam.Cutoff)) != 0);
                    {
                        error = ScalarParamEval(
                            Scan.TrackFilter.Cutoff,
                            ref Accents,
                            SynthParams,
                            out Cutoff);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                    }

                    if ((Scan.FilterParamMask & (1 << (int)FilterParam.BandwidthOrSlope)) != 0)
                    {
                        error = ScalarParamEval(
                            Scan.TrackFilter.BandwidthOrSlope,
                            ref Accents,
                            SynthParams,
                            out BandwidthOrSlope);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                    }

                    if ((Scan.FilterParamMask & (1 << (int)FilterParam.Gain)) != 0)
                    {
                        error = ScalarParamEval(
                            Scan.TrackFilter.Gain,
                            ref Accents,
                            SynthParams,
                            out Gain);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                    }

                    FilterParams Params = new FilterParams(
                        SynthParams.dSamplingRate,
                        Cutoff,
                        BandwidthOrSlope,
                        Gain);
                    if (Scan.Left != null)
                    {
                        Scan.Left.UpdateParams(ref Params);
                    }
                    if (Scan.Right != null)
                    {
                        Scan.Right.UpdateParams(ref Params);
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            /* apply filter processing to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec synthParams)
            {
#if DEBUG
                Debug.Assert(!synthParams.ScratchWorkspace1InUse);
                synthParams.ScratchWorkspace1InUse = true;
                Debug.Assert(!synthParams.ScratchWorkspace4InUse);
                synthParams.ScratchWorkspace4InUse = true;
#endif
                int LeftCopyOffset = synthParams.ScratchWorkspace1LOffset;
                int RightCopyOffset = synthParams.ScratchWorkspace1ROffset;
                int LoudnessWorkspaceOffset = synthParams.ScratchWorkspace4LOffset;
                int TemporaryOutputWorkspaceOffset = synthParams.ScratchWorkspace4ROffset;

                // NOTE: filter implementations are entitled to use ScratchWorkspace2.
#if DEBUG
                Debug.Assert(!synthParams.ScratchWorkspace2InUse);
#endif

                /* get input data and initialize output data */
                FloatVectorCopy(
                    workspace,
                    lOffset,
                    workspace,
                    LeftCopyOffset,
                    nActualFrames);
                FloatVectorCopy(
                    workspace,
                    rOffset,
                    workspace,
                    RightCopyOffset,
                    nActualFrames);
                FloatVectorZero(
                    workspace,
                    lOffset,
                    nActualFrames);
                FloatVectorZero(
                    workspace,
                    rOffset,
                    nActualFrames);

                /* iterate over parallel filter array, accumulating into output */
                for (int i = 0; i < FilterVector.Length; i++)
                {
                    FilterRec Scan = FilterVector[i];

                    if (Program.Config.EnableEnvelopeSmoothing
                        // case of no motion in smoothed axis can use fast code path
                        && (Scan.CurrentMultiplier != Scan.PreviousMultiplier))
                    {
                        // envelope smoothing

                        float LocalPreviousMultiplier = Scan.PreviousMultiplier;

                        // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                        if ((Scan.OscFilter != null) && IsLFOSampleAndHold(Scan.OscFilter.OutputMultiplierLFO))
                        {
                            LocalPreviousMultiplier = Scan.CurrentMultiplier;
                        }

                        if ((Scan.OscFilter == null)
                            || !EnvelopeCurrentSegmentExponential(Scan.OscFilter.OutputMultiplierEnvelope))
                        {
                            FloatVectorAdditiveRecurrence(
                                workspace,
                                LoudnessWorkspaceOffset,
                                LocalPreviousMultiplier,
                                Scan.CurrentMultiplier,
                                nActualFrames);
                        }
                        else
                        {
                            FloatVectorMultiplicativeRecurrence(
                                workspace,
                                LoudnessWorkspaceOffset,
                                LocalPreviousMultiplier,
                                Scan.CurrentMultiplier,
                                nActualFrames);
                        }

                        if (Scan.Left != null)
                        {
                            FloatVectorZero(
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                nActualFrames);
                            Scan.Left.Apply(
                                workspace,
                                LeftCopyOffset,
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                nActualFrames,
                                1f,
                                synthParams);
                            FloatVectorProductAccumulate(
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                workspace,
                                LoudnessWorkspaceOffset,
                                workspace,
                                lOffset,
                                nActualFrames);
                        }
                        if (Scan.Right != null)
                        {
                            FloatVectorZero(
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                nActualFrames);
                            Scan.Right.Apply(
                                workspace,
                                RightCopyOffset,
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                nActualFrames,
                                1f,
                                synthParams);
                            FloatVectorProductAccumulate(
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                workspace,
                                LoudnessWorkspaceOffset,
                                workspace,
                                rOffset,
                                nActualFrames);
                        }
                    }
                    else
                    {
                        if (Scan.Left != null)
                        {
                            Scan.Left.Apply(
                                workspace,
                                LeftCopyOffset,
                                workspace,
                                lOffset,
                                nActualFrames,
                                Scan.CurrentMultiplier,
                                synthParams);
                        }
                        if (Scan.Right != null)
                        {
                            Scan.Right.Apply(
                                workspace,
                                RightCopyOffset,
                                workspace,
                                rOffset,
                                nActualFrames,
                                Scan.CurrentMultiplier,
                                synthParams);
                        }
                    }
                }

#if DEBUG
                Debug.Assert(!synthParams.ScratchWorkspace2InUse); // ensure filter implementations cleared it
#endif
#if DEBUG
                synthParams.ScratchWorkspace1InUse = false;
                synthParams.ScratchWorkspace4InUse = false;
#endif

                return SynthErrorCodes.eSynthDone;
            }

            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
            }
        }
    }
}
