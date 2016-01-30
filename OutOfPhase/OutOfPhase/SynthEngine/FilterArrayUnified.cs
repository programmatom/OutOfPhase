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

            void Apply(
                float[] XinVector,
                int XinVectorOffset,
                float[] YoutVector,
                int YoutVectorOffset,
                int VectorLength,
                float OutputScaling,
                SynthParamRec SynthParams);
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
            public bool LeftEnabled; // TODO: remove - use Left != null
            public bool RightEnabled; // TODO: remove - use Right != null
            public FilterTypes FilterType;
            public FilterScalings FilterScaling;
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
            /* number of filters */
            public int NumFilters;

            /* vector of filters starts after last member of struct */
            public FilterRec[] FilterVector;


            /* obtain filter processing routine & allocate filter state records */
            /* returns true if successful, or false if allocation failed.  state records are */
            /* not disposed of if failure occurs, so caller must see that they are deleted. */
            private static void SetFilterProcessing(
                out bool LeftEnabled,
                out IFilter LeftOut,
                out bool RightEnabled,
                out IFilter RightOut,
                int FilterIndex,
                FilterSpecRec Template,
                SynthParamRec SynthParams)
            {
                LeftOut = null;
                RightOut = null;

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

                LeftEnabled = false;
                RightEnabled = false;

                FilterTypes Type = GetFilterType(Template, FilterIndex);

                /* initialize left channel filter */
                if (LeftChannel)
                {
                    switch (Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case FilterTypes.eFilterFirstOrderLowpass:
                            LeftOut = new FirstOrderLowpassRec();
                            break;
                        case FilterTypes.eFilterFirstOrderHighpass:
                            LeftOut = new FirstOrderHighpassRec();
                            break;
                        case FilterTypes.eFilterSecondOrderResonant:
                            LeftOut = new SecondOrderResonRec();
                            break;
                        case FilterTypes.eFilterSecondOrderZero:
                            LeftOut = new SecondOrderZeroRec();
                            break;
                        case FilterTypes.eFilterButterworthLowpass:
                            LeftOut = new ButterworthLowpassRec();
                            break;
                        case FilterTypes.eFilterButterworthHighpass:
                            LeftOut = new ButterworthHighpassRec();
                            break;
                        case FilterTypes.eFilterButterworthBandpass:
                            LeftOut = new ButterworthBandpassRec();
                            break;
                        case FilterTypes.eFilterButterworthBandreject:
                            LeftOut = new ButterworthBandrejectRec();
                            break;
                        case FilterTypes.eFilterParametricEQ:
                            LeftOut = new ParametricEqualizerRec();
                            break;
                        case FilterTypes.eFilterParametricEQ2:
                            LeftOut = new ParametricEqualizer2Rec();
                            break;
                        case FilterTypes.eFilterLowShelfEQ:
                            LeftOut = new LowShelfEqualizerRec();
                            break;
                        case FilterTypes.eFilterHighShelfEQ:
                            LeftOut = new HighShelfEqualizerRec();
                            break;
                        case FilterTypes.eFilterResonantLowpass:
                            LeftOut = new ResonantLowpassRec(
                                GetFilterLowpassOrder(Template, FilterIndex),
                                GetFilterBandpassOrder(Template, FilterIndex));
                            break;
                        case FilterTypes.eFilterResonantLowpass2:
                            LeftOut = new ResonantLowpass2Rec(
                                GetFilterLowpassOrder(Template, FilterIndex),
                                GetFilterBroken(Template, FilterIndex));
                            break;
                        case FilterTypes.eFilterNull:
                            LeftOut = new FilterNullRec();
                            break;
                    }
                    Debug.Assert(LeftOut.FilterType == Type);
                }

                /* initialize right channel filter */
                if (RightChannel)
                {
                    switch (Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case FilterTypes.eFilterFirstOrderLowpass:
                            RightOut = new FirstOrderLowpassRec();
                            break;
                        case FilterTypes.eFilterFirstOrderHighpass:
                            RightOut = new FirstOrderHighpassRec();
                            break;
                        case FilterTypes.eFilterSecondOrderResonant:
                            RightOut = new SecondOrderResonRec();
                            break;
                        case FilterTypes.eFilterSecondOrderZero:
                            RightOut = new SecondOrderZeroRec();
                            break;
                        case FilterTypes.eFilterButterworthLowpass:
                            RightOut = new ButterworthLowpassRec();
                            break;
                        case FilterTypes.eFilterButterworthHighpass:
                            RightOut = new ButterworthHighpassRec();
                            break;
                        case FilterTypes.eFilterButterworthBandpass:
                            RightOut = new ButterworthBandpassRec();
                            break;
                        case FilterTypes.eFilterButterworthBandreject:
                            RightOut = new ButterworthBandrejectRec();
                            break;
                        case FilterTypes.eFilterParametricEQ:
                            RightOut = new ParametricEqualizerRec();
                            break;
                        case FilterTypes.eFilterParametricEQ2:
                            RightOut = new ParametricEqualizer2Rec();
                            break;
                        case FilterTypes.eFilterLowShelfEQ:
                            RightOut = new LowShelfEqualizerRec();
                            break;
                        case FilterTypes.eFilterHighShelfEQ:
                            RightOut = new HighShelfEqualizerRec();
                            break;
                        case FilterTypes.eFilterResonantLowpass:
                            RightOut = new ResonantLowpassRec(
                                GetFilterLowpassOrder(Template, FilterIndex),
                                GetFilterBandpassOrder(Template, FilterIndex));
                            break;
                        case FilterTypes.eFilterResonantLowpass2:
                            RightOut = new ResonantLowpass2Rec(
                                GetFilterLowpassOrder(Template, FilterIndex),
                                GetFilterBroken(Template, FilterIndex));
                            break;
                        case FilterTypes.eFilterNull:
                            RightOut = new FilterNullRec();
                            break;
                    }
                    Debug.Assert(RightOut.FilterType == Type);
                }

                LeftEnabled = LeftChannel;
                RightEnabled = RightChannel;
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

                int Limit = GetNumFiltersInSpec(Template);

                FilterArrayRec Array = new FilterArrayRec();
                Array.FilterVector = new FilterRec[Limit];
                Array.NumFilters = Limit;

                /* remembers maximum amount of pre-origin time required */
                int MaxPreOrigin = 0;

                /* build filter table */
                for (int Scan = 0; Scan < Limit; Scan += 1)
                {
                    FilterRec Filter = Array.FilterVector[Scan] = new FilterRec();
                    Filter.OscFilter = new OscFilterParamRec();

                    /* what kind of filter is this */
                    Filter.FilterType = GetFilterType(Template, Scan); /* do this right away */
                    Filter.FilterScaling = GetFilterScalingMode(Template, Scan);

                    /* envelopes and LFOs */

                    Filter.OscFilter.OutputMultiplierEnvelope = NewEnvelopeStateRecord(
                        GetFilterOutputEnvelope(Template, Scan),
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
                        GetFilterOutputLFO(Template, Scan),
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

                    switch (Filter.FilterType)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case FilterTypes.eFilterNull:
                            break;

                        case FilterTypes.eFilterFirstOrderLowpass:
                        case FilterTypes.eFilterFirstOrderHighpass:
                        case FilterTypes.eFilterSecondOrderResonant:
                        case FilterTypes.eFilterSecondOrderZero:
                        case FilterTypes.eFilterButterworthLowpass:
                        case FilterTypes.eFilterButterworthHighpass:
                        case FilterTypes.eFilterButterworthBandpass:
                        case FilterTypes.eFilterButterworthBandreject:
                        case FilterTypes.eFilterParametricEQ:
                        case FilterTypes.eFilterParametricEQ2:
                        case FilterTypes.eFilterLowShelfEQ:
                        case FilterTypes.eFilterHighShelfEQ:
                        case FilterTypes.eFilterResonantLowpass:
                        case FilterTypes.eFilterResonantLowpass2:
                            Filter.OscFilter.CutoffEnvelope = NewEnvelopeStateRecord(
                                GetFilterCutoffEnvelope(Template, Scan),
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
                                GetFilterCutoffLFO(Template, Scan),
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
                            break;
                    }

                    switch (Filter.FilterType)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case FilterTypes.eFilterNull:
                        case FilterTypes.eFilterFirstOrderLowpass:
                        case FilterTypes.eFilterFirstOrderHighpass:
                        case FilterTypes.eFilterButterworthLowpass:
                        case FilterTypes.eFilterButterworthHighpass:
                        case FilterTypes.eFilterResonantLowpass2:
                            break;

                        case FilterTypes.eFilterSecondOrderResonant:
                        case FilterTypes.eFilterSecondOrderZero:
                        case FilterTypes.eFilterButterworthBandpass:
                        case FilterTypes.eFilterButterworthBandreject:
                        case FilterTypes.eFilterParametricEQ:
                        case FilterTypes.eFilterParametricEQ2:
                        case FilterTypes.eFilterLowShelfEQ:
                        case FilterTypes.eFilterHighShelfEQ:
                        case FilterTypes.eFilterResonantLowpass:
                            Filter.OscFilter.BandwidthOrSlopeEnvelope = NewEnvelopeStateRecord(
                                GetFilterBandwidthOrSlopeEnvelope(Template, Scan),
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
                                GetFilterBandwidthOrSlopeLFO(Template, Scan),
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
                            break;
                    }

                    switch (Filter.FilterType)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case FilterTypes.eFilterNull:
                        case FilterTypes.eFilterFirstOrderLowpass:
                        case FilterTypes.eFilterFirstOrderHighpass:
                        case FilterTypes.eFilterButterworthLowpass:
                        case FilterTypes.eFilterButterworthHighpass:
                        case FilterTypes.eFilterSecondOrderResonant:
                        case FilterTypes.eFilterSecondOrderZero:
                        case FilterTypes.eFilterButterworthBandpass:
                        case FilterTypes.eFilterButterworthBandreject:
                            break;

                        case FilterTypes.eFilterParametricEQ:
                        case FilterTypes.eFilterParametricEQ2:
                        case FilterTypes.eFilterLowShelfEQ:
                        case FilterTypes.eFilterHighShelfEQ:
                        case FilterTypes.eFilterResonantLowpass:
                        case FilterTypes.eFilterResonantLowpass2:
                            Filter.OscFilter.GainEnvelope = NewEnvelopeStateRecord(
                                GetFilterGainEnvelope(Template, Scan),
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
                                GetFilterGainLFO(Template, Scan),
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
                            break;
                    }

                    /* set up filter state records and filter processor function */
                    SetFilterProcessing(
                        out Filter.LeftEnabled,
                        out Filter.Left,
                        out Filter.RightEnabled,
                        out Filter.Right,
                        Scan,
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
                int Limit = GetNumFiltersInSpec(Template);

                FilterArrayRec Array = new FilterArrayRec();
                Array.FilterVector = new FilterRec[Limit];
                Array.NumFilters = Limit;

                /* create each filter */
                for (int Scan = 0; Scan < Limit; Scan += 1)
                {
                    FilterRec Filter = Array.FilterVector[Scan] = new FilterRec();
                    Filter.TrackFilter = new TrackFilterParamRec();

                    /* load type parameters */
                    Filter.FilterType = GetFilterType(Template, Scan); /* do this right away */
                    Filter.FilterScaling = GetFilterScalingMode(Template, Scan);

                    /* zap filter parameters so delete routine won't try to delete dead storage */
                    Filter.LeftEnabled = false;
                    Filter.RightEnabled = false;

                    /* load control parameters */
                    GetFilterCutoffAgg(
                        Template,
                        Scan,
                        out Filter.TrackFilter.Cutoff);
                    GetFilterBandwidthOrSlopeAgg(
                        Template,
                        Scan,
                        out Filter.TrackFilter.BandwidthOrSlope);
                    GetFilterOutputMultiplierAgg(
                        Template,
                        Scan,
                        out Filter.TrackFilter.OutputMultiplier);
                    GetFilterGainAgg(
                        Template,
                        Scan,
                        out Filter.TrackFilter.Gain);

                    /* set up filter state records and filter processor function */
                    SetFilterProcessing(
                        out Filter.LeftEnabled,
                        out Filter.Left,
                        out Filter.RightEnabled,
                        out Filter.Right,
                        Scan,
                        Template,
                        SynthParams);
                }

                return Array;
            }

            /* fix up the origin time so that envelopes start at the proper times */
            public void OscFixEnvelopeOrigins(
                int ActualPreOriginTime)
            {
                for (int i = 0; i < this.NumFilters; i += 1)
                {
                    FilterRec Scan = this.FilterVector[i];

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
                for (int i = 0; i < this.NumFilters; i += 1)
                {
                    SynthErrorCodes error;
                    double Cutoff = Double.NaN;
                    double BandwidthOrSlope = Double.NaN;
                    double Gain = Double.NaN;

                    FilterRec Scan = this.FilterVector[i];

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

                    switch (Scan.FilterType)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case FilterTypes.eFilterFirstOrderLowpass:
                            if (Scan.LeftEnabled)
                            {
                                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                                    (FirstOrderLowpassRec)Scan.Left,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                                    (FirstOrderLowpassRec)Scan.Right,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterFirstOrderHighpass:
                            if (Scan.LeftEnabled)
                            {
                                FirstOrderHighpassRec.SetFirstOrderHighpassCoefficients(
                                    (FirstOrderHighpassRec)Scan.Left,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                FirstOrderHighpassRec.SetFirstOrderHighpassCoefficients(
                                    (FirstOrderHighpassRec)Scan.Right,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterSecondOrderResonant:
                            if (Scan.LeftEnabled)
                            {
                                SecondOrderResonRec.SetSecondOrderResonCoefficients(
                                    (SecondOrderResonRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Scan.FilterScaling,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                SecondOrderResonRec.SetSecondOrderResonCoefficients(
                                    (SecondOrderResonRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Scan.FilterScaling,
                                    SynthParams.dSamplingRate);
                            }
                            break;

                        case FilterTypes.eFilterSecondOrderZero:
                            if (Scan.LeftEnabled)
                            {
                                SecondOrderZeroRec.SetSecondOrderZeroCoefficients(
                                    (SecondOrderZeroRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Scan.FilterScaling,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                SecondOrderZeroRec.SetSecondOrderZeroCoefficients(
                                    (SecondOrderZeroRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Scan.FilterScaling,
                                    SynthParams.dSamplingRate);
                            }
                            break;

                        case FilterTypes.eFilterButterworthLowpass:
                            if (Scan.LeftEnabled)
                            {
                                ButterworthLowpassRec.SetButterworthLowpassCoefficients(
                                    (ButterworthLowpassRec)Scan.Left,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ButterworthLowpassRec.SetButterworthLowpassCoefficients(
                                    (ButterworthLowpassRec)Scan.Right,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterButterworthHighpass:
                            if (Scan.LeftEnabled)
                            {
                                ButterworthHighpassRec.SetButterworthHighpassCoefficients(
                                    (ButterworthHighpassRec)Scan.Left,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ButterworthHighpassRec.SetButterworthHighpassCoefficients(
                                    (ButterworthHighpassRec)Scan.Right,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterButterworthBandpass:
                            if (Scan.LeftEnabled)
                            {
                                ButterworthBandpassRec.SetButterworthBandpassCoefficients(
                                    (ButterworthBandpassRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ButterworthBandpassRec.SetButterworthBandpassCoefficients(
                                    (ButterworthBandpassRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterButterworthBandreject:
                            if (Scan.LeftEnabled)
                            {
                                ButterworthBandrejectRec.SetButterworthBandrejectCoefficients(
                                    (ButterworthBandrejectRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ButterworthBandrejectRec.SetButterworthBandrejectCoefficients(
                                    (ButterworthBandrejectRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterParametricEQ:
                            if (Scan.LeftEnabled)
                            {
                                ParametricEqualizerRec.SetParametricEqualizerCoefficients(
                                    (ParametricEqualizerRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ParametricEqualizerRec.SetParametricEqualizerCoefficients(
                                    (ParametricEqualizerRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterParametricEQ2:
                            if (Scan.LeftEnabled)
                            {
                                ParametricEqualizer2Rec.SetParametricEqualizer2Coefficients(
                                    (ParametricEqualizer2Rec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ParametricEqualizer2Rec.SetParametricEqualizer2Coefficients(
                                    (ParametricEqualizer2Rec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterLowShelfEQ:
                            if (Scan.LeftEnabled)
                            {
                                LowShelfEqualizerRec.SetLowShelfEqualizerCoefficients(
                                    (LowShelfEqualizerRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                LowShelfEqualizerRec.SetLowShelfEqualizerCoefficients(
                                    (LowShelfEqualizerRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterHighShelfEQ:
                            if (Scan.LeftEnabled)
                            {
                                HighShelfEqualizerRec.SetHighShelfEqualizerCoefficients(
                                    (HighShelfEqualizerRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                HighShelfEqualizerRec.SetHighShelfEqualizerCoefficients(
                                    (HighShelfEqualizerRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterResonantLowpass:
                            if (Scan.LeftEnabled)
                            {
                                ResonantLowpassRec.SetResonantLowpassCoefficients(
                                    (ResonantLowpassRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ResonantLowpassRec.SetResonantLowpassCoefficients(
                                    (ResonantLowpassRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterResonantLowpass2:
                            if (Scan.LeftEnabled)
                            {
                                ResonantLowpass2Rec.SetResonantLowpass2Coefficients(
                                    (ResonantLowpass2Rec)Scan.Left,
                                    Cutoff,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ResonantLowpass2Rec.SetResonantLowpass2Coefficients(
                                    (ResonantLowpass2Rec)Scan.Right,
                                    Cutoff,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterNull:
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            public void OscKeyUpSustain1()
            {
                for (int i = 0; i < this.NumFilters; i += 1)
                {
                    FilterRec Scan = this.FilterVector[i];

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
                for (int i = 0; i < this.NumFilters; i += 1)
                {
                    FilterRec Scan = this.FilterVector[i];

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
                for (int i = 0; i < this.NumFilters; i += 1)
                {
                    FilterRec Scan = this.FilterVector[i];

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
                for (int i = 0; i < this.NumFilters; i += 1)
                {
                    FilterRec Scan = this.FilterVector[i];

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
                for (int i = 0; i < this.NumFilters; i += 1)
                {
                    SynthErrorCodes error;
                    double Cutoff = Double.NaN;
                    double BandwidthOrSlope = Double.NaN;
#if DEBUG
                    bool BandwidthOrSlopeSet = false;
#endif
                    double Gain = Double.NaN;
#if DEBUG
                    bool GainSet = false;
#endif
                    double DoubleTemp;

                    FilterRec Scan = this.FilterVector[i];

                    error = ScalarParamEval(
                        Scan.TrackFilter.OutputMultiplier,
                        ref Accents,
                        SynthParams,
                        out DoubleTemp);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                    Scan.PreviousMultiplier = Scan.CurrentMultiplier;
                    Scan.CurrentMultiplier = (float)DoubleTemp;

                    error = ScalarParamEval(
                        Scan.TrackFilter.Cutoff,
                        ref Accents,
                        SynthParams,
                        out Cutoff);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }

                    switch (Scan.FilterType)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case FilterTypes.eFilterFirstOrderLowpass:
                        case FilterTypes.eFilterFirstOrderHighpass:
                        case FilterTypes.eFilterButterworthLowpass:
                        case FilterTypes.eFilterButterworthHighpass:
                        case FilterTypes.eFilterNull:
                            break;

                        case FilterTypes.eFilterSecondOrderResonant:
                        case FilterTypes.eFilterSecondOrderZero:
                        case FilterTypes.eFilterButterworthBandpass:
                        case FilterTypes.eFilterButterworthBandreject:
                            error = ScalarParamEval(
                                Scan.TrackFilter.BandwidthOrSlope,
                                ref Accents,
                                SynthParams,
                                out BandwidthOrSlope);
                            if (error != SynthErrorCodes.eSynthDone)
                            {
                                return error;
                            }
#if DEBUG
                            BandwidthOrSlopeSet = true;
#endif
                            break;

                        case FilterTypes.eFilterParametricEQ:
                        case FilterTypes.eFilterParametricEQ2:
                        case FilterTypes.eFilterLowShelfEQ:
                        case FilterTypes.eFilterHighShelfEQ:
                        case FilterTypes.eFilterResonantLowpass:
                            error = ScalarParamEval(
                                Scan.TrackFilter.BandwidthOrSlope,
                                ref Accents,
                                SynthParams,
                                out BandwidthOrSlope);
                            if (error != SynthErrorCodes.eSynthDone)
                            {
                                return error;
                            }
#if DEBUG
                            BandwidthOrSlopeSet = true;
#endif
                            /* FALL THROUGH */
                            goto Next;

                        case FilterTypes.eFilterResonantLowpass2: /* just gain for him */
                        Next:
                            error = ScalarParamEval(
                                Scan.TrackFilter.Gain,
                                ref Accents,
                                SynthParams,
                                out Gain);
                            if (error != SynthErrorCodes.eSynthDone)
                            {
                                return error;
                            }
#if DEBUG
                            GainSet = true;
#endif
                            break;
                    }
                    switch (Scan.FilterType)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case FilterTypes.eFilterFirstOrderLowpass:
                            if (Scan.LeftEnabled)
                            {
                                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                                    (FirstOrderLowpassRec)Scan.Left,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                                    (FirstOrderLowpassRec)Scan.Right,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterFirstOrderHighpass:
                            if (Scan.LeftEnabled)
                            {
                                FirstOrderHighpassRec.SetFirstOrderHighpassCoefficients(
                                    (FirstOrderHighpassRec)Scan.Left,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                FirstOrderHighpassRec.SetFirstOrderHighpassCoefficients(
                                    (FirstOrderHighpassRec)Scan.Right,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterSecondOrderResonant:
#if DEBUG
                            if (!BandwidthOrSlopeSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                SecondOrderResonRec.SetSecondOrderResonCoefficients(
                                    (SecondOrderResonRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Scan.FilterScaling,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                SecondOrderResonRec.SetSecondOrderResonCoefficients(
                                    (SecondOrderResonRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Scan.FilterScaling,
                                    SynthParams.dSamplingRate);
                            }
                            break;

                        case FilterTypes.eFilterSecondOrderZero:
#if DEBUG
                            if (!BandwidthOrSlopeSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                SecondOrderZeroRec.SetSecondOrderZeroCoefficients(
                                    (SecondOrderZeroRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Scan.FilterScaling,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                SecondOrderZeroRec.SetSecondOrderZeroCoefficients(
                                    (SecondOrderZeroRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Scan.FilterScaling,
                                    SynthParams.dSamplingRate);
                            }
                            break;

                        case FilterTypes.eFilterButterworthLowpass:
                            if (Scan.LeftEnabled)
                            {
                                ButterworthLowpassRec.SetButterworthLowpassCoefficients(
                                    (ButterworthLowpassRec)Scan.Left,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ButterworthLowpassRec.SetButterworthLowpassCoefficients(
                                    (ButterworthLowpassRec)Scan.Right,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterButterworthHighpass:
                            if (Scan.LeftEnabled)
                            {
                                ButterworthHighpassRec.SetButterworthHighpassCoefficients(
                                    (ButterworthHighpassRec)Scan.Left,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ButterworthHighpassRec.SetButterworthHighpassCoefficients(
                                    (ButterworthHighpassRec)Scan.Right,
                                    Cutoff,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterButterworthBandpass:
#if DEBUG
                            if (!BandwidthOrSlopeSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                ButterworthBandpassRec.SetButterworthBandpassCoefficients(
                                    (ButterworthBandpassRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ButterworthBandpassRec.SetButterworthBandpassCoefficients(
                                    (ButterworthBandpassRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterButterworthBandreject:
#if DEBUG
                            if (!BandwidthOrSlopeSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                ButterworthBandrejectRec.SetButterworthBandrejectCoefficients(
                                    (ButterworthBandrejectRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ButterworthBandrejectRec.SetButterworthBandrejectCoefficients(
                                    (ButterworthBandrejectRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterNull:
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterParametricEQ:
#if DEBUG
                            if (!BandwidthOrSlopeSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
#if DEBUG
                            if (!GainSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                ParametricEqualizerRec.SetParametricEqualizerCoefficients(
                                    (ParametricEqualizerRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ParametricEqualizerRec.SetParametricEqualizerCoefficients(
                                    (ParametricEqualizerRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterParametricEQ2:
#if DEBUG
                            if (!BandwidthOrSlopeSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
#if DEBUG
                            if (!GainSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                ParametricEqualizer2Rec.SetParametricEqualizer2Coefficients(
                                    (ParametricEqualizer2Rec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ParametricEqualizer2Rec.SetParametricEqualizer2Coefficients(
                                    (ParametricEqualizer2Rec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterLowShelfEQ:
#if DEBUG
                            if (!BandwidthOrSlopeSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
#if DEBUG
                            if (!GainSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                LowShelfEqualizerRec.SetLowShelfEqualizerCoefficients(
                                    (LowShelfEqualizerRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                LowShelfEqualizerRec.SetLowShelfEqualizerCoefficients(
                                    (LowShelfEqualizerRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterHighShelfEQ:
#if DEBUG
                            if (!BandwidthOrSlopeSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
#if DEBUG
                            if (!GainSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                HighShelfEqualizerRec.SetHighShelfEqualizerCoefficients(
                                    (HighShelfEqualizerRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                HighShelfEqualizerRec.SetHighShelfEqualizerCoefficients(
                                    (HighShelfEqualizerRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterResonantLowpass:
#if DEBUG
                            if (!BandwidthOrSlopeSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
#if DEBUG
                            if (!GainSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                ResonantLowpassRec.SetResonantLowpassCoefficients(
                                    (ResonantLowpassRec)Scan.Left,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ResonantLowpassRec.SetResonantLowpassCoefficients(
                                    (ResonantLowpassRec)Scan.Right,
                                    Cutoff,
                                    BandwidthOrSlope,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;

                        case FilterTypes.eFilterResonantLowpass2:
#if DEBUG
                            if (!GainSet)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            if (Scan.LeftEnabled)
                            {
                                ResonantLowpass2Rec.SetResonantLowpass2Coefficients(
                                    (ResonantLowpass2Rec)Scan.Left,
                                    Cutoff,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
                            if (Scan.RightEnabled)
                            {
                                ResonantLowpass2Rec.SetResonantLowpass2Coefficients(
                                    (ResonantLowpass2Rec)Scan.Right,
                                    Cutoff,
                                    Gain,
                                    SynthParams.dSamplingRate);
                            }
#if DEBUG
                            if (Scan.FilterScaling != FilterScalings.eFilterDefaultScaling)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
#endif
                            break;
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
                for (int i = 0; i < this.NumFilters; i += 1)
                {
                    FilterRec FilterScan = this.FilterVector[i];

#if true // TODO:experimental - smoothing
                    if (Program.Config.EnableEnvelopeSmoothing
                        // case of no motion in smoothed axis can use fast code path
                        && (FilterScan.CurrentMultiplier != FilterScan.PreviousMultiplier))
                    {
                        // envelope smoothing

                        float LocalPreviousMultiplier = FilterScan.PreviousMultiplier;

                        // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                        if ((FilterScan.OscFilter != null) && IsLFOSampleAndHold(FilterScan.OscFilter.OutputMultiplierLFO))
                        {
                            LocalPreviousMultiplier = FilterScan.CurrentMultiplier;
                        }

                        if ((FilterScan.OscFilter == null)
                            || !EnvelopeCurrentSegmentExponential(FilterScan.OscFilter.OutputMultiplierEnvelope))
                        {
                            FloatVectorAdditiveRecurrence(
                                workspace,
                                LoudnessWorkspaceOffset,
                                LocalPreviousMultiplier,
                                FilterScan.CurrentMultiplier,
                                nActualFrames);
                        }
                        else
                        {
                            FloatVectorMultiplicativeRecurrence(
                                workspace,
                                LoudnessWorkspaceOffset,
                                LocalPreviousMultiplier,
                                FilterScan.CurrentMultiplier,
                                nActualFrames);
                        }

                        if (FilterScan.LeftEnabled)
                        {
                            FloatVectorZero(
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                nActualFrames);
                            FilterScan.Left.Apply(
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
                        if (FilterScan.RightEnabled)
                        {
                            FloatVectorZero(
                                workspace,
                                TemporaryOutputWorkspaceOffset,
                                nActualFrames);
                            FilterScan.Right.Apply(
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
#endif
                    {
                        if (FilterScan.LeftEnabled)
                        {
                            FilterScan.Left.Apply(
                                workspace,
                                LeftCopyOffset,
                                workspace,
                                lOffset,
                                nActualFrames,
                                FilterScan.CurrentMultiplier,
                                synthParams);
                        }
                        if (FilterScan.RightEnabled)
                        {
                            FilterScan.Right.Apply(
                                workspace,
                                RightCopyOffset,
                                workspace,
                                rOffset,
                                nActualFrames,
                                FilterScan.CurrentMultiplier,
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
