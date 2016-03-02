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
using System.Runtime.InteropServices;
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

        [StructLayout(LayoutKind.Auto)]
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

        [StructLayout(LayoutKind.Auto)]
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
        [StructLayout(LayoutKind.Auto)]
        public struct FilterRec
        {
            /* common state info */
            public float CurrentMultiplier; // aka outputscaling
            public float PreviousMultiplier;
            public FilterTypes FilterType;
            public byte FilterParamMask;
            public IFilter Left;
            public IFilter Right;

            /* variant state info */
            // was a tagged union in C, but given the total size, saving a pointer just doesn't matter enough for C# heroics
            // exactly one is non-null - indicating which class it is
            public OscFilterParamRec OscFilter;
            public TrackFilterParamRec TrackFilter;
        }

        /* structure for the whole overall filter */
        public class FilterArrayRec : ITrackEffect, IOscillatorEffect
        {
            public int count;

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
                    Left = CreateFilter(Template, FilterIndex, SynthParams);
                    Debug.Assert(Left.FilterType == GetFilterType(Template, FilterIndex));
                }

                if (RightChannel)
                {
                    Right = CreateFilter(Template, FilterIndex, SynthParams);
                    Debug.Assert(Right.FilterType == GetFilterType(Template, FilterIndex));
                }
            }

            private static IFilter CreateFilter(
                FilterSpecRec Template,
                int FilterIndex,
                SynthParamRec SynthParams)
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
                        {
                            ResonantLowpassRec filterSpecialized;
                            filter = filterSpecialized = New(ref SynthParams.freelists.resonantLowpassFreeList);
                            filterSpecialized.Init(
                                GetFilterLowpassOrder(Template, FilterIndex),
                                GetFilterBandpassOrder(Template, FilterIndex),
                                SynthParams);
                        }
                        break;
                    case FilterTypes.eFilterResonantLowpass2:
                        {
                            ResonantLowpass2Rec filterSpecialized;
                            filter = filterSpecialized = New(
                                ref SynthParams.freelists.resonantLowpass2FreeList);
                            filterSpecialized.Init(
                                GetFilterLowpassOrder(Template, FilterIndex),
                                GetFilterBroken(Template, FilterIndex));
                        }
                        break;
                    case FilterTypes.eFilterNull:
                        filter = new FilterNullRec();
                        break;
                }
                Debug.Assert(filter.FilterType == Type);
                return filter;
            }

            private static void FreeFilter(
                ref IFilter filter,
                SynthParamRec SynthParams)
            {
                switch (filter.FilterType)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case FilterTypes.eFilterFirstOrderLowpass:
                        break;
                    case FilterTypes.eFilterFirstOrderHighpass:
                        break;
                    case FilterTypes.eFilterSecondOrderResonant:
                        break;
                    case FilterTypes.eFilterSecondOrderZero:
                        break;
                    case FilterTypes.eFilterButterworthLowpass:
                        break;
                    case FilterTypes.eFilterButterworthHighpass:
                        break;
                    case FilterTypes.eFilterButterworthBandpass:
                        break;
                    case FilterTypes.eFilterButterworthBandreject:
                        break;
                    case FilterTypes.eFilterParametricEQ:
                        break;
                    case FilterTypes.eFilterParametricEQ2:
                        break;
                    case FilterTypes.eFilterLowShelfEQ:
                        break;
                    case FilterTypes.eFilterHighShelfEQ:
                        break;
                    case FilterTypes.eFilterResonantLowpass:
                        {
                            ResonantLowpassRec filterSpecialized = (ResonantLowpassRec)filter;
                            filterSpecialized.Dispose(SynthParams); // free contained objects
                            Free(ref SynthParams.freelists.resonantLowpassFreeList, ref filterSpecialized);
                        }
                        break;
                    case FilterTypes.eFilterResonantLowpass2:
                        {
                            ResonantLowpass2Rec filterSpecialized = (ResonantLowpass2Rec)filter;
                            Free(ref SynthParams.freelists.resonantLowpass2FreeList, ref filterSpecialized);
                        }
                        break;
                    case FilterTypes.eFilterNull:
                        break;
                }

                filter = null;
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

                FilterArrayRec Array = New(ref SynthParams.freelists.filterArrayRecFreeList);
                int count = Array.count = GetNumFiltersInSpec(Template);
                FilterRec[] FilterVector = Array.FilterVector = New(ref SynthParams.freelists.filterRecFreeList, count); // zeroed
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }

                // must assign all fields: Array, Array.FilterVector[i].OscFilter

                /* remembers maximum amount of pre-origin time required */
                int MaxPreOrigin = 0;

                /* build filter table */
                for (int i = 0; i < count; i++)
                {
                    FilterVector[i].OscFilter = New(ref SynthParams.freelists.OscFilterParamRecFreeList);

                    /* what kind of filter is this */
                    FilterVector[i].FilterType = GetFilterType(Template, i);
                    FilterVector[i].FilterParamMask = FilterInfos[(int)FilterVector[i].FilterType].ParamsMask;

                    /* envelopes and LFOs */

                    FilterVector[i].OscFilter.OutputMultiplierEnvelope = NewEnvelopeStateRecord(
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

                    FilterVector[i].OscFilter.OutputMultiplierLFO = NewLFOGenerator(
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

                    Debug.Assert((FilterVector[i].FilterParamMask & (1 << (int)FilterParam.Cutoff)) != 0);
                    {
                        FilterVector[i].OscFilter.CutoffEnvelope = NewEnvelopeStateRecord(
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
                        FilterVector[i].OscFilter.CutoffLFO = NewLFOGenerator(
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

                    if ((FilterVector[i].FilterParamMask & (1 << (int)FilterParam.BandwidthOrSlope)) != 0)
                    {
                        FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope = NewEnvelopeStateRecord(
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
                        FilterVector[i].OscFilter.BandwidthOrSlopeLFO = NewLFOGenerator(
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

                    if ((FilterVector[i].FilterParamMask & (1 << (int)FilterParam.Gain)) != 0)
                    {
                        FilterVector[i].OscFilter.GainEnvelope = NewEnvelopeStateRecord(
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
                        FilterVector[i].OscFilter.GainLFO = NewLFOGenerator(
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
                        out FilterVector[i].Left,
                        out FilterVector[i].Right,
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
                FilterArrayRec Array = New(ref SynthParams.freelists.filterArrayRecFreeList);
                int count = Array.count = GetNumFiltersInSpec(Template);
                FilterRec[] FilterVector = Array.FilterVector = New(ref SynthParams.freelists.filterRecFreeList, count); // zeroed
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }

                /* create each filter */
                for (int i = 0; i < count; i++)
                {
                    FilterVector[i].TrackFilter = new TrackFilterParamRec();

                    /* load type parameters */
                    FilterVector[i].FilterType = GetFilterType(Template, i);
                    FilterVector[i].FilterParamMask = FilterInfos[(int)FilterVector[i].FilterType].ParamsMask;

                    /* load control parameters */
                    Debug.Assert((FilterVector[i].FilterParamMask & (1 << (int)FilterParam.Cutoff)) != 0);
                    {
                        GetFilterCutoffAgg(
                            Template,
                            i,
                            out FilterVector[i].TrackFilter.Cutoff);
                    }
                    if ((FilterVector[i].FilterParamMask & (1 << (int)FilterParam.BandwidthOrSlope)) != 0)
                    {
                        GetFilterBandwidthOrSlopeAgg(
                            Template,
                            i,
                            out FilterVector[i].TrackFilter.BandwidthOrSlope);
                    }
                    if ((FilterVector[i].FilterParamMask & (1 << (int)FilterParam.Gain)) != 0)
                    {
                        GetFilterGainAgg(
                            Template,
                            i,
                            out FilterVector[i].TrackFilter.Gain);
                    }
                    GetFilterOutputMultiplierAgg(
                        Template,
                        i,
                        out FilterVector[i].TrackFilter.OutputMultiplier);

                    CreateFilters(
                        out FilterVector[i].Left,
                        out FilterVector[i].Right,
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
                int count = this.count;
                FilterRec[] FilterVector = this.FilterVector;
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < count; i++)
                {
                    EnvelopeStateFixUpInitialDelay(
                        FilterVector[i].OscFilter.OutputMultiplierEnvelope,
                        ActualPreOriginTime);
                    if (FilterVector[i].OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeStateFixUpInitialDelay(
                            FilterVector[i].OscFilter.CutoffEnvelope,
                            ActualPreOriginTime);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeStateFixUpInitialDelay(
                            FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope,
                            ActualPreOriginTime);
                    }
                    if (FilterVector[i].OscFilter.GainEnvelope != null)
                    {
                        EnvelopeStateFixUpInitialDelay(
                            FilterVector[i].OscFilter.GainEnvelope,
                            ActualPreOriginTime);
                    }

                    LFOGeneratorFixEnvelopeOrigins(
                        FilterVector[i].OscFilter.OutputMultiplierLFO,
                        ActualPreOriginTime);
                    if (FilterVector[i].OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorFixEnvelopeOrigins(
                            FilterVector[i].OscFilter.CutoffLFO,
                            ActualPreOriginTime);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorFixEnvelopeOrigins(
                            FilterVector[i].OscFilter.BandwidthOrSlopeLFO,
                            ActualPreOriginTime);
                    }
                    if (FilterVector[i].OscFilter.GainLFO != null)
                    {
                        LFOGeneratorFixEnvelopeOrigins(
                            FilterVector[i].OscFilter.GainLFO,
                            ActualPreOriginTime);
                    }

                    // initial value for envelope smoothing
                    FilterVector[i].CurrentMultiplier = (float)LFOGenInitialValue(
                        FilterVector[i].OscFilter.OutputMultiplierLFO,
                        EnvelopeInitialValue(
                           FilterVector[i].OscFilter.OutputMultiplierEnvelope));
                }
            }

            /* update filter state with accent information */
            public SynthErrorCodes OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams)
            {
                int count = this.count;
                FilterRec[] FilterVector = this.FilterVector;
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < count; i++)
                {
                    SynthErrorCodes error;

                    double Cutoff = Double.NaN;
                    double BandwidthOrSlope = Double.NaN;
                    double Gain = Double.NaN;

                    error = SynthErrorCodes.eSynthDone;
                    FilterVector[i].PreviousMultiplier = FilterVector[i].CurrentMultiplier;
                    FilterVector[i].CurrentMultiplier = (float)LFOGenUpdateCycle(
                        FilterVector[i].OscFilter.OutputMultiplierLFO,
                        EnvelopeUpdate(
                            FilterVector[i].OscFilter.OutputMultiplierEnvelope,
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

                    if (FilterVector[i].OscFilter.CutoffEnvelope != null)
                    {
                        error = SynthErrorCodes.eSynthDone;
                        Cutoff = LFOGenUpdateCycle(
                            FilterVector[i].OscFilter.CutoffLFO,
                            EnvelopeUpdate(
                                FilterVector[i].OscFilter.CutoffEnvelope,
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

                    if (FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        error = SynthErrorCodes.eSynthDone;
                        BandwidthOrSlope = LFOGenUpdateCycle(
                            FilterVector[i].OscFilter.BandwidthOrSlopeLFO,
                            EnvelopeUpdate(
                                FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope,
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

                    if (FilterVector[i].OscFilter.GainEnvelope != null)
                    {
                        error = SynthErrorCodes.eSynthDone;
                        Gain = LFOGenUpdateCycle(
                            FilterVector[i].OscFilter.GainLFO,
                            EnvelopeUpdate(
                                FilterVector[i].OscFilter.GainEnvelope,
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
                    if (FilterVector[i].Left != null)
                    {
                        FilterVector[i].Left.UpdateParams(ref Params);
                    }
                    if (FilterVector[i].Right != null)
                    {
                        FilterVector[i].Right.UpdateParams(ref Params);
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            public void OscKeyUpSustain1()
            {
                int count = this.count;
                FilterRec[] FilterVector = this.FilterVector;
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < count; i++)
                {
                    EnvelopeKeyUpSustain1(FilterVector[i].OscFilter.OutputMultiplierEnvelope);
                    if (FilterVector[i].OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeKeyUpSustain1(FilterVector[i].OscFilter.CutoffEnvelope);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeKeyUpSustain1(FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope);
                    }
                    if (FilterVector[i].OscFilter.GainEnvelope != null)
                    {
                        EnvelopeKeyUpSustain1(FilterVector[i].OscFilter.GainEnvelope);
                    }

                    LFOGeneratorKeyUpSustain1(FilterVector[i].OscFilter.OutputMultiplierLFO);
                    if (FilterVector[i].OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorKeyUpSustain1(FilterVector[i].OscFilter.CutoffLFO);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorKeyUpSustain1(FilterVector[i].OscFilter.BandwidthOrSlopeLFO);
                    }
                    if (FilterVector[i].OscFilter.GainLFO != null)
                    {
                        LFOGeneratorKeyUpSustain1(FilterVector[i].OscFilter.GainLFO);
                    }
                }
            }

            public void OscKeyUpSustain2()
            {
                int count = this.count;
                FilterRec[] FilterVector = this.FilterVector;
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < count; i++)
                {
                    EnvelopeKeyUpSustain2(FilterVector[i].OscFilter.OutputMultiplierEnvelope);
                    if (FilterVector[i].OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeKeyUpSustain2(FilterVector[i].OscFilter.CutoffEnvelope);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeKeyUpSustain2(FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope);
                    }
                    if (FilterVector[i].OscFilter.GainEnvelope != null)
                    {
                        EnvelopeKeyUpSustain2(FilterVector[i].OscFilter.GainEnvelope);
                    }

                    LFOGeneratorKeyUpSustain2(FilterVector[i].OscFilter.OutputMultiplierLFO);
                    if (FilterVector[i].OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorKeyUpSustain2(FilterVector[i].OscFilter.CutoffLFO);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorKeyUpSustain2(FilterVector[i].OscFilter.BandwidthOrSlopeLFO);
                    }
                    if (FilterVector[i].OscFilter.GainLFO != null)
                    {
                        LFOGeneratorKeyUpSustain2(FilterVector[i].OscFilter.GainLFO);
                    }
                }
            }

            public void OscKeyUpSustain3()
            {
                int count = this.count;
                FilterRec[] FilterVector = this.FilterVector;
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < count; i++)
                {
                    EnvelopeKeyUpSustain3(FilterVector[i].OscFilter.OutputMultiplierEnvelope);
                    if (FilterVector[i].OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeKeyUpSustain3(FilterVector[i].OscFilter.CutoffEnvelope);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeKeyUpSustain3(FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope);
                    }
                    if (FilterVector[i].OscFilter.GainEnvelope != null)
                    {
                        EnvelopeKeyUpSustain3(FilterVector[i].OscFilter.GainEnvelope);
                    }

                    LFOGeneratorKeyUpSustain3(FilterVector[i].OscFilter.OutputMultiplierLFO);
                    if (FilterVector[i].OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorKeyUpSustain3(FilterVector[i].OscFilter.CutoffLFO);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorKeyUpSustain3(FilterVector[i].OscFilter.BandwidthOrSlopeLFO);
                    }
                    if (FilterVector[i].OscFilter.GainLFO != null)
                    {
                        LFOGeneratorKeyUpSustain3(FilterVector[i].OscFilter.GainLFO);
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
                int count = this.count;
                FilterRec[] FilterVector = this.FilterVector;
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < count; i++)
                {
                    EnvelopeRetriggerFromOrigin(
                        FilterVector[i].OscFilter.OutputMultiplierEnvelope,
                        ref NewAccents,
                        NewInitialFrequency,
                        1,
                        NewHurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    if (FilterVector[i].OscFilter.CutoffEnvelope != null)
                    {
                        EnvelopeRetriggerFromOrigin(
                            FilterVector[i].OscFilter.CutoffEnvelope,
                            ref NewAccents,
                            NewInitialFrequency,
                            1,
                            NewHurryUp,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope != null)
                    {
                        EnvelopeRetriggerFromOrigin(
                            FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope,
                            ref NewAccents,
                            NewInitialFrequency,
                            1,
                            NewHurryUp,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                    if (FilterVector[i].OscFilter.GainEnvelope != null)
                    {
                        EnvelopeRetriggerFromOrigin(
                            FilterVector[i].OscFilter.GainEnvelope,
                            ref NewAccents,
                            NewInitialFrequency,
                            1,
                            NewHurryUp,
                            ActuallyRetrigger,
                            SynthParams);
                    }

                    LFOGeneratorRetriggerFromOrigin(
                        FilterVector[i].OscFilter.OutputMultiplierLFO,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        1,
                        1,
                        ActuallyRetrigger,
                        SynthParams);
                    if (FilterVector[i].OscFilter.CutoffLFO != null)
                    {
                        LFOGeneratorRetriggerFromOrigin(
                            FilterVector[i].OscFilter.CutoffLFO,
                            ref NewAccents,
                            NewInitialFrequency,
                            NewHurryUp,
                            1,
                            1,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                    if (FilterVector[i].OscFilter.BandwidthOrSlopeLFO != null)
                    {
                        LFOGeneratorRetriggerFromOrigin(
                            FilterVector[i].OscFilter.BandwidthOrSlopeLFO,
                            ref NewAccents,
                            NewInitialFrequency,
                            NewHurryUp,
                            1,
                            1,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                    if (FilterVector[i].OscFilter.GainLFO != null)
                    {
                        LFOGeneratorRetriggerFromOrigin(
                            FilterVector[i].OscFilter.GainLFO,
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
                int count = this.count;
                FilterRec[] FilterVector = this.FilterVector;
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < count; i++)
                {
                    SynthErrorCodes error;

                    double Cutoff = Double.NaN;
                    double BandwidthOrSlope = Double.NaN;
                    double Gain = Double.NaN;

                    double OutputMultiplier;
                    error = ScalarParamEval(
                        FilterVector[i].TrackFilter.OutputMultiplier,
                        ref Accents,
                        SynthParams,
                        out OutputMultiplier);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                    FilterVector[i].PreviousMultiplier = FilterVector[i].CurrentMultiplier;
                    FilterVector[i].CurrentMultiplier = (float)OutputMultiplier;

                    Debug.Assert((FilterVector[i].FilterParamMask & (1 << (int)FilterParam.Cutoff)) != 0);
                    {
                        error = ScalarParamEval(
                            FilterVector[i].TrackFilter.Cutoff,
                            ref Accents,
                            SynthParams,
                            out Cutoff);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                    }

                    if ((FilterVector[i].FilterParamMask & (1 << (int)FilterParam.BandwidthOrSlope)) != 0)
                    {
                        error = ScalarParamEval(
                            FilterVector[i].TrackFilter.BandwidthOrSlope,
                            ref Accents,
                            SynthParams,
                            out BandwidthOrSlope);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                    }

                    if ((FilterVector[i].FilterParamMask & (1 << (int)FilterParam.Gain)) != 0)
                    {
                        error = ScalarParamEval(
                            FilterVector[i].TrackFilter.Gain,
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
                    if (FilterVector[i].Left != null)
                    {
                        FilterVector[i].Left.UpdateParams(ref Params);
                    }
                    if (FilterVector[i].Right != null)
                    {
                        FilterVector[i].Right.UpdateParams(ref Params);
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
                int count = this.count;
                FilterRec[] FilterVector = this.FilterVector;
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < count; i++)
                {
                    if (Program.Config.EnableEnvelopeSmoothing
                        // case of no motion in smoothed axis can use fast code path
                        && (FilterVector[i].CurrentMultiplier != FilterVector[i].PreviousMultiplier))
                    {
                        // envelope smoothing

                        float LocalPreviousMultiplier = FilterVector[i].PreviousMultiplier;

                        // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                        if ((FilterVector[i].OscFilter != null) && IsLFOSampleAndHold(FilterVector[i].OscFilter.OutputMultiplierLFO))
                        {
                            LocalPreviousMultiplier = FilterVector[i].CurrentMultiplier;
                        }

                        if ((FilterVector[i].OscFilter == null)
                            || !EnvelopeCurrentSegmentExponential(FilterVector[i].OscFilter.OutputMultiplierEnvelope))
                        {
                            FloatVectorAdditiveRecurrence(
                                workspace,
                                LoudnessWorkspaceOffset,
                                LocalPreviousMultiplier,
                                FilterVector[i].CurrentMultiplier,
                                nActualFrames);
                        }
                        else
                        {
                            FloatVectorMultiplicativeRecurrence(
                                workspace,
                                LoudnessWorkspaceOffset,
                                LocalPreviousMultiplier,
                                FilterVector[i].CurrentMultiplier,
                                nActualFrames);
                        }

                        if (FilterVector[i].Left != null)
                        {
                            FloatVectorZero(
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                nActualFrames);
                            FilterVector[i].Left.Apply(
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
                        if (FilterVector[i].Right != null)
                        {
                            FloatVectorZero(
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                nActualFrames);
                            FilterVector[i].Right.Apply(
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
                        if (FilterVector[i].Left != null)
                        {
                            FilterVector[i].Left.Apply(
                                workspace,
                                LeftCopyOffset,
                                workspace,
                                lOffset,
                                nActualFrames,
                                FilterVector[i].CurrentMultiplier,
                                synthParams);
                        }
                        if (FilterVector[i].Right != null)
                        {
                            FilterVector[i].Right.Apply(
                                workspace,
                                RightCopyOffset,
                                workspace,
                                rOffset,
                                nActualFrames,
                                FilterVector[i].CurrentMultiplier,
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
                int count = this.count;
                FilterRec[] FilterVector = this.FilterVector;
                if (unchecked((uint)count > (uint)FilterVector.Length))
                {
                    Debug.Assert(false);
                    throw new IndexOutOfRangeException();
                }
                for (int i = 0; i < count; i++)
                {
                    if (FilterVector[i].OscFilter != null)
                    {
                        FreeEnvelopeStateRecord(
                            ref FilterVector[i].OscFilter.OutputMultiplierEnvelope,
                            SynthParams);
                        FreeLFOGenerator(
                            ref FilterVector[i].OscFilter.OutputMultiplierLFO,
                            SynthParams);

                        Debug.Assert((FilterVector[i].OscFilter.CutoffEnvelope != null)
                            == (FilterVector[i].OscFilter.CutoffLFO != null));
                        if (FilterVector[i].OscFilter.CutoffEnvelope != null)
                        {
                            FreeEnvelopeStateRecord(
                                ref FilterVector[i].OscFilter.CutoffEnvelope,
                                SynthParams);
                            FreeLFOGenerator(
                                ref FilterVector[i].OscFilter.CutoffLFO,
                                SynthParams);
                        }

                        Debug.Assert((FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope != null)
                            == (FilterVector[i].OscFilter.BandwidthOrSlopeLFO != null));
                        if (FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope != null)
                        {
                            FreeEnvelopeStateRecord(
                                ref FilterVector[i].OscFilter.BandwidthOrSlopeEnvelope,
                                SynthParams);
                            FreeLFOGenerator(
                                ref FilterVector[i].OscFilter.BandwidthOrSlopeLFO,
                                SynthParams);
                        }

                        Debug.Assert((FilterVector[i].OscFilter.GainEnvelope != null)
                            == (FilterVector[i].OscFilter.GainLFO != null));
                        if (FilterVector[i].OscFilter.GainEnvelope != null)
                        {
                            FreeEnvelopeStateRecord(
                                ref FilterVector[i].OscFilter.GainEnvelope,
                                SynthParams);
                            FreeLFOGenerator(
                                ref FilterVector[i].OscFilter.GainLFO,
                                SynthParams);
                        }

                        Free(ref SynthParams.freelists.OscFilterParamRecFreeList, ref FilterVector[i].OscFilter);
                    }
                    // else: FilterVector[i].TrackFilter is not freelisted because there tends to be only one that lives for most of the score duration
                    FilterVector[i].TrackFilter = null;

                    if (FilterVector[i].Left != null)
                    {
                        FreeFilter(ref FilterVector[i].Left, SynthParams);
                    }

                    if (FilterVector[i].Right != null)
                    {
                        FreeFilter(ref FilterVector[i].Right, SynthParams);
                    }
                }

                Free(ref SynthParams.freelists.filterRecFreeList, ref this.FilterVector);

                FilterArrayRec State = this;
                Free(ref SynthParams.freelists.filterArrayRecFreeList, ref State);
            }
        }
    }
}
