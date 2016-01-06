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
        /* types of filters */
        public enum FilterTypes
        {
            eFilterNull,
            eFilterFirstOrderLowpass,
            eFilterFirstOrderHighpass,
            eFilterSecondOrderResonant,
            eFilterSecondOrderZero,
            eFilterButterworthLowpass,
            eFilterButterworthHighpass,
            eFilterButterworthBandpass,
            eFilterButterworthBandreject,
            eFilterParametricEQ,
            eFilterParametricEQ2,
            eFilterLowShelfEQ,
            eFilterHighShelfEQ,
            eFilterResonantLowpass,
            eFilterResonantLowpass2,
        }

        /* signal normalization options */
        public enum FilterScalings
        {
            eFilterDefaultScaling,
            eFilterResonMidbandGain1,
            eFilterResonNoiseGain1,
            eFilterZeroGain1,
        }

        /* filter channels */
        public enum FilterChannels
        {
            eFilterLeft,
            eFilterRight,
            eFilterBoth,
        }

        public class OneFilterRec
        {
            /* what kind of filtering */
            public FilterTypes FilterType;
            public FilterScalings FilterScaling;
            public FilterChannels Channel;

            // flag used for enabling old broken defective buggy behavior of ResonantLowpass2
            public bool Broken;

            /* stuff for resonant lowpass */
            public int LowpassOrder;
            public int BandpassOrder;

            /* these controls only apply to the track effect, not the oscillator effect */
            public double Cutoff;
            public AccentRec CutoffAccent;
            public double BandwidthOrSlope;
            public AccentRec BandwidthOrSlopeAccent;
            public double Gain;
            public AccentRec GainAccent;
            public double OutputMultiplier;
            public AccentRec OutputMultiplierAccent;
            public PcodeRec CutoffFormula;
            public PcodeRec BandwidthOrSlopeFormula;
            public PcodeRec GainFormula;
            public PcodeRec OutputMultiplierFormula;

            /* these controls only apply to the oscillator effect, not the track effect */
            public EnvelopeRec CutoffEnvelope;
            public EnvelopeRec BandwidthOrSlopeEnvelope;
            public EnvelopeRec OutputEnvelope;
            public EnvelopeRec GainEnvelope;
            public LFOListSpecRec CutoffLFO;
            public LFOListSpecRec BandwidthOrSlopeLFO;
            public LFOListSpecRec OutputLFO;
            public LFOListSpecRec GainLFO;
        }

        public class FilterSpecRec
        {
            public OneFilterRec[] List;
        }

        /* create a new parallel filter specification */
        public static FilterSpecRec NewFilterSpec()
        {
            FilterSpecRec Filter = new FilterSpecRec();

            Filter.List = new OneFilterRec[0];

            return Filter;
        }

        /* add a single filter to the list */
        public static void AppendFilterToSpec(
            FilterSpecRec Filter,
            OneFilterRec Spec)
        {
            Array.Resize(ref Filter.List, Filter.List.Length + 1);
            Filter.List[Filter.List.Length - 1] = Spec;
        }

        /* get the number of filters */
        public static int GetNumFiltersInSpec(FilterSpecRec Filter)
        {
            return Filter.List.Length;
        }

        /* get a filter type for the specified filter spec */
        public static FilterTypes GetFilterType(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].FilterType;
        }

        /* get which channel to apply a filter to */
        public static FilterChannels GetFilterChannel(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].Channel;
        }

        /* get aggregate cutoff frequency info for a filter */
        public static void GetFilterCutoffAgg(
            FilterSpecRec Filter,
            int Index,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Filter.List[Index].Cutoff,
                ref Filter.List[Index].CutoffAccent,
                Filter.List[Index].CutoffFormula,
                out ParamsOut);
        }


        /* get aggregate bandwidth (or slope) info for a filter */
        public static void GetFilterBandwidthOrSlopeAgg(
            FilterSpecRec Filter,
            int Index,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Filter.List[Index].BandwidthOrSlope,
                ref Filter.List[Index].BandwidthOrSlopeAccent,
                Filter.List[Index].BandwidthOrSlopeFormula,
                out ParamsOut);
        }

        public static FilterScalings GetFilterScalingMode(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].FilterScaling;
        }

        /* get aggregate output multiplier info for a filter */
        public static void GetFilterOutputMultiplierAgg(
            FilterSpecRec Filter,
            int Index,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Filter.List[Index].OutputMultiplier,
                ref Filter.List[Index].OutputMultiplierAccent,
                Filter.List[Index].OutputMultiplierFormula,
                out ParamsOut);
        }

        /* get aggregate gain info for a filter */
        public static void GetFilterGainAgg(
            FilterSpecRec Filter,
            int Index,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Filter.List[Index].Gain,
                ref Filter.List[Index].GainAccent,
                Filter.List[Index].GainFormula,
                out ParamsOut);
        }

        public static EnvelopeRec GetFilterCutoffEnvelope(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].CutoffEnvelope;
        }

        public static EnvelopeRec GetFilterBandwidthOrSlopeEnvelope(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].BandwidthOrSlopeEnvelope;
        }

        public static EnvelopeRec GetFilterOutputEnvelope(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].OutputEnvelope;
        }

        public static EnvelopeRec GetFilterGainEnvelope(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].GainEnvelope;
        }


        public static LFOListSpecRec GetFilterCutoffLFO(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].CutoffLFO;
        }

        public static LFOListSpecRec GetFilterBandwidthOrSlopeLFO(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].BandwidthOrSlopeLFO;
        }

        public static LFOListSpecRec GetFilterOutputLFO(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].OutputLFO;
        }

        public static LFOListSpecRec GetFilterGainLFO(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].GainLFO;
        }

        public static int GetFilterLowpassOrder(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].LowpassOrder;
        }

        public static int GetFilterBandpassOrder(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index].BandpassOrder;
        }

        public static bool GetFilterBroken(
            FilterSpecRec Filter,
            int Index)
        {
#if DEBUG
            if (Filter.List[Index].FilterType != FilterTypes.eFilterResonantLowpass2)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif
            return Filter.List[Index].Broken;
        }

        /* get a filter specification record */
        public static OneFilterRec GetSingleFilterSpec(
            FilterSpecRec Filter,
            int Index)
        {
            return Filter.List[Index];
        }

        /* create a new filter specification record */
        public static OneFilterRec NewSingleFilterSpec(FilterTypes FilterType)
        {
            OneFilterRec Spec = new OneFilterRec();

            Spec.CutoffEnvelope = NewEnvelope();
            Spec.BandwidthOrSlopeEnvelope = NewEnvelope();
            Spec.OutputEnvelope = NewEnvelope();
            Spec.CutoffLFO = NewLFOListSpecifier();
            Spec.BandwidthOrSlopeLFO = NewLFOListSpecifier();
            Spec.OutputLFO = NewLFOListSpecifier();
            Spec.GainEnvelope = NewEnvelope();
            Spec.GainLFO = NewLFOListSpecifier();
            //InitializeAccentZero(out Spec.CutoffAccent);
            //InitializeAccentZero(out Spec.BandwidthOrSlopeAccent);
            //InitializeAccentZero(out Spec.GainAccent);
            //InitializeAccentZero(out Spec.OutputMultiplierAccent);
            Spec.FilterType = FilterType;
            Spec.FilterScaling = FilterScalings.eFilterDefaultScaling;
            Spec.Channel = FilterChannels.eFilterBoth;
            Spec.LowpassOrder = 2;
            Spec.BandpassOrder = 2;
            //Spec.Cutoff = 0;
            //Spec.BandwidthOrSlope = 0;
            //Spec.Gain = 0;
            Spec.OutputMultiplier = 1;
            //Spec.CutoffFormula = null;
            //Spec.BandwidthOrSlopeFormula = null;
            //Spec.GainFormula = null;
            //Spec.OutputMultiplierFormula = null;

            return Spec;
        }

        public static void SetSingleFilterCutoff(
            OneFilterRec Spec,
            double Cutoff,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.CutoffFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.CutoffFormula = Formula;
            Spec.Cutoff = Cutoff;
        }

        public static void SetSingleFilterCutoffAccent(
            OneFilterRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.CutoffAccent, AccentNum, Value);
        }

        public static void SetSingleFilterBandwidthOrSlope(
            OneFilterRec Spec,
            double BandwidthOrSlope,
            PcodeRec Formula)
        {
#if DEBUG
            if ((Spec.FilterType != FilterTypes.eFilterSecondOrderResonant)
                && (Spec.FilterType != FilterTypes.eFilterSecondOrderZero)
                && (Spec.FilterType != FilterTypes.eFilterButterworthBandpass)
                && (Spec.FilterType != FilterTypes.eFilterButterworthBandreject)
                && (Spec.FilterType != FilterTypes.eFilterParametricEQ)
                && (Spec.FilterType != FilterTypes.eFilterParametricEQ2)
                && (Spec.FilterType != FilterTypes.eFilterLowShelfEQ)
                && (Spec.FilterType != FilterTypes.eFilterHighShelfEQ)
                && (Spec.FilterType != FilterTypes.eFilterResonantLowpass))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (Spec.BandwidthOrSlopeFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.BandwidthOrSlopeFormula = Formula;
            Spec.BandwidthOrSlope = BandwidthOrSlope;
        }

        public static void SetSingleFilterBandwidthOrSlopeAccent(
            OneFilterRec Spec,
            double Value,
            int AccentNum)
        {
#if DEBUG
            if ((Spec.FilterType != FilterTypes.eFilterSecondOrderResonant)
                && (Spec.FilterType != FilterTypes.eFilterSecondOrderZero)
                && (Spec.FilterType != FilterTypes.eFilterButterworthBandpass)
                && (Spec.FilterType != FilterTypes.eFilterButterworthBandreject)
                && (Spec.FilterType != FilterTypes.eFilterParametricEQ)
                && (Spec.FilterType != FilterTypes.eFilterParametricEQ2)
                && (Spec.FilterType != FilterTypes.eFilterLowShelfEQ)
                && (Spec.FilterType != FilterTypes.eFilterHighShelfEQ)
                && (Spec.FilterType != FilterTypes.eFilterResonantLowpass))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            SetAccentMemberValue(ref Spec.BandwidthOrSlopeAccent, AccentNum, Value);
        }

        /* set filter scaling mode */
        public static void SetSingleFilterScalingMode(
            OneFilterRec Spec,
            FilterScalings Scaling)
        {
#if DEBUG
            switch (Spec.FilterType)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case FilterTypes.eFilterNull:
                case FilterTypes.eFilterFirstOrderLowpass:
                case FilterTypes.eFilterFirstOrderHighpass:
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
                    if (Scaling != FilterScalings.eFilterDefaultScaling)
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    break;
                case FilterTypes.eFilterSecondOrderResonant:
                    if ((Scaling != FilterScalings.eFilterDefaultScaling)
                        && (Scaling != FilterScalings.eFilterResonMidbandGain1)
                        && (Scaling != FilterScalings.eFilterResonNoiseGain1))
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    break;
                case FilterTypes.eFilterSecondOrderZero:
                    if ((Scaling != FilterScalings.eFilterDefaultScaling)
                        && (Scaling != FilterScalings.eFilterZeroGain1))
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    break;
            }
#endif
            Spec.FilterScaling = Scaling;
        }

        /* set filter channel */
        public static void SetSingleFilterChannel(
            OneFilterRec Spec,
            FilterChannels Channel)
        {
            Spec.Channel = Channel;
        }

        public static void SetSingleFilterOutputMultiplier(
            OneFilterRec Spec,
            double Output,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.OutputMultiplierFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.OutputMultiplierFormula = Formula;
            Spec.OutputMultiplier = Output;
        }

        public static void SetSingleFilterOutputMultiplierAccent(
            OneFilterRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.OutputMultiplierAccent, AccentNum, Value);
        }

        public static void SetSingleFilterGain(
            OneFilterRec Spec,
            double Gain,
            PcodeRec Formula)
        {
#if DEBUG
            if ((Spec.FilterType != FilterTypes.eFilterParametricEQ)
                && (Spec.FilterType != FilterTypes.eFilterParametricEQ2)
                && (Spec.FilterType != FilterTypes.eFilterLowShelfEQ)
                && (Spec.FilterType != FilterTypes.eFilterHighShelfEQ)
                && (Spec.FilterType != FilterTypes.eFilterResonantLowpass)
                && (Spec.FilterType != FilterTypes.eFilterResonantLowpass2))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (Spec.GainFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.GainFormula = Formula;
            Spec.Gain = Gain;
        }

        public static void SetSingleFilterGainAccent(
            OneFilterRec Spec,
            double Value,
            int AccentNum)
        {
#if DEBUG
            if ((Spec.FilterType != FilterTypes.eFilterParametricEQ)
                && (Spec.FilterType != FilterTypes.eFilterParametricEQ2)
                && (Spec.FilterType != FilterTypes.eFilterLowShelfEQ)
                && (Spec.FilterType != FilterTypes.eFilterHighShelfEQ)
                && (Spec.FilterType != FilterTypes.eFilterResonantLowpass)
                && (Spec.FilterType != FilterTypes.eFilterResonantLowpass2))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            SetAccentMemberValue(ref Spec.GainAccent, AccentNum, Value);
        }

        public static void SetFilterLowpassOrder(
            OneFilterRec Spec,
            int Order)
        {
#if DEBUG
            if ((Spec.FilterType != FilterTypes.eFilterResonantLowpass)
                && (Spec.FilterType != FilterTypes.eFilterResonantLowpass2))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.LowpassOrder = Order;
        }

        public static void SetFilterBandpassOrder(
            OneFilterRec Spec,
            int Order)
        {
#if DEBUG
            if (Spec.FilterType != FilterTypes.eFilterResonantLowpass)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.BandpassOrder = Order;
        }

        public static void SetFilterBroken(
            OneFilterRec Spec,
            bool Broken)
        {
#if DEBUG
            if (Spec.FilterType != FilterTypes.eFilterResonantLowpass2)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.Broken = Broken;
        }
    }
}
