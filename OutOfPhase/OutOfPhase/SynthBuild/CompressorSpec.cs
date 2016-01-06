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
        /* types of power estimation methods */
        public enum CompressorPowerEstType
        {
            eCompressPowerAbsVal,
            eCompressPowerRMS,
            eCompressPowerPeak,
            eCompressPowerpeaklookahead,
        }

        public class CompressorSpecRec
        {
            /* stuff for track/score effect */
            public double InputGain;
            public AccentRec InputGainAccent;
            public double OutputGain;
            public AccentRec OutputGainAccent;
            public double NormalPower;
            public AccentRec NormalPowerAccent;
            public double ThreshPower;
            public AccentRec ThreshPowerAccent;
            public double Ratio;
            public AccentRec RatioAccent;
            public double FilterCutoff;
            public AccentRec FilterCutoffAccent;
            public double DecayRate;
            public AccentRec DecayRateAccent;
            public double AttackRate;
            public AccentRec AttackRateAccent;
            public double LimitingExcess;
            public AccentRec LimitingExcessAccent;
            public PcodeRec InputGainFormula;
            public PcodeRec OutputGainFormula;
            public PcodeRec NormalPowerFormula;
            public PcodeRec ThreshPowerFormula;
            public PcodeRec RatioFormula;
            public PcodeRec FilterCutoffFormula;
            public PcodeRec DecayRateFormula;
            public PcodeRec AttackRateFormula;
            public PcodeRec LimitingExcessFormula;

            /* stuff for oscillator effect */
            public EnvelopeRec InputGainEnvelope;
            public LFOListSpecRec InputGainLFOs;
            public EnvelopeRec OutputGainEnvelope;
            public LFOListSpecRec OutputGainLFOs;
            public EnvelopeRec NormalPowerEnvelope;
            public LFOListSpecRec NormalPowerLFOs;
            public EnvelopeRec ThreshPowerEnvelope;
            public LFOListSpecRec ThreshPowerLFOs;
            public EnvelopeRec RatioEnvelope;
            public LFOListSpecRec RatioLFOs;
            public EnvelopeRec FilterCutoffEnvelope;
            public LFOListSpecRec FilterCutoffLFOs;
            public EnvelopeRec DecayRateEnvelope;
            public LFOListSpecRec DecayRateLFOs;
            public EnvelopeRec AttackRateEnvelope;
            public LFOListSpecRec AttackRateLFOs;
            public EnvelopeRec LimitingExcessEnvelope;
            public LFOListSpecRec LimitingExcessLFOs;

            /* stuff for everyone */
            public CompressorPowerEstType PowerEstimatorMode;
        }

        /* create new compressor specification record */
        public static CompressorSpecRec NewCompressorSpec()
        {
            CompressorSpecRec Spec = new CompressorSpecRec();

            Spec.InputGainEnvelope = NewEnvelope();
            Spec.InputGainLFOs = NewLFOListSpecifier();
            Spec.OutputGainEnvelope = NewEnvelope();
            Spec.OutputGainLFOs = NewLFOListSpecifier();
            Spec.NormalPowerEnvelope = NewEnvelope();
            Spec.NormalPowerLFOs = NewLFOListSpecifier();
            Spec.ThreshPowerEnvelope = NewEnvelope();
            Spec.ThreshPowerLFOs = NewLFOListSpecifier();
            Spec.RatioEnvelope = NewEnvelope();
            Spec.RatioLFOs = NewLFOListSpecifier();
            Spec.FilterCutoffEnvelope = NewEnvelope();
            Spec.FilterCutoffLFOs = NewLFOListSpecifier();
            Spec.DecayRateEnvelope = NewEnvelope();
            Spec.DecayRateLFOs = NewLFOListSpecifier();
            Spec.AttackRateEnvelope = NewEnvelope();
            Spec.AttackRateLFOs = NewLFOListSpecifier();
            Spec.LimitingExcessEnvelope = NewEnvelope();
            Spec.LimitingExcessLFOs = NewLFOListSpecifier();
            //InitializeAccentZero(out Spec.InputGainAccent);
            //InitializeAccentZero(out Spec.OutputGainAccent);
            //InitializeAccentZero(out Spec.NormalPowerAccent);
            //InitializeAccentZero(out Spec.ThreshPowerAccent);
            //InitializeAccentZero(out Spec.RatioAccent);
            //InitializeAccentZero(out Spec.FilterCutoffAccent);
            //InitializeAccentZero(out Spec.DecayRateAccent);
            //InitializeAccentZero(out Spec.AttackRateAccent);
            //InitializeAccentZero(out Spec.LimitingExcessAccent);
            Spec.InputGain = 1;
            Spec.OutputGain = 1;
            Spec.NormalPower = 1;
            Spec.ThreshPower = 1;
            Spec.Ratio = 10;
            Spec.FilterCutoff = 10;
            Spec.DecayRate = 1;
            Spec.AttackRate = .01;
            Spec.LimitingExcess = 1.5;
            Spec.PowerEstimatorMode = CompressorPowerEstType.eCompressPowerAbsVal;
            //Spec.InputGainFormula = null;
            //Spec.OutputGainFormula = null;
            //Spec.NormalPowerFormula = null;
            //Spec.ThreshPowerFormula = null;
            //Spec.RatioFormula = null;
            //Spec.FilterCutoffFormula = null;
            //Spec.DecayRateFormula = null;
            //Spec.AttackRateFormula = null;
            //Spec.LimitingExcessFormula = null;

            return Spec;
        }

        /* set input gain factor */
        public static void PutCompressorInputGain(
            CompressorSpecRec Spec,
            double Gain,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.InputGainFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.InputGainFormula = Formula;
            Spec.InputGain = Gain;
        }

        /* set input gain accent */
        public static void PutCompressorInputGainAccent(
            CompressorSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.InputGainAccent, AccentNum, Value);
        }

        /* get aggregated input gain factor info */
        public static void GetCompressorInputGainAgg(
            CompressorSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.InputGain,
                ref Spec.InputGainAccent,
                Spec.InputGainFormula,
                out ParamsOut);
        }

        /* get input gain envelope */
        public static EnvelopeRec GetCompressorInputGainEnvelope(CompressorSpecRec Spec)
        {
            return Spec.InputGainEnvelope;
        }

        /* get input gain LFO list */
        public static LFOListSpecRec GetCompressorInputGainLFOList(CompressorSpecRec Spec)
        {
            return Spec.InputGainLFOs;
        }

        /* set output gain factor */
        public static void PutCompressorOutputGain(
            CompressorSpecRec Spec,
            double Gain,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.OutputGainFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.OutputGainFormula = Formula;
            Spec.OutputGain = Gain;
        }

        /* set output gain accent */
        public static void PutCompressorOutputGainAccent(
            CompressorSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.OutputGainAccent, AccentNum, Value);
        }

        /* get aggregated output gain factor info */
        public static void GetCompressorOutputGainAgg(
            CompressorSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.OutputGain,
                ref Spec.OutputGainAccent,
                Spec.OutputGainFormula,
                out ParamsOut);
        }

        /* get output gain envelope */
        public static EnvelopeRec GetCompressorOutputGainEnvelope(CompressorSpecRec Spec)
        {
            return Spec.OutputGainEnvelope;
        }

        /* get output gain LFO list */
        public static LFOListSpecRec GetCompressorOutputGainLFOList(CompressorSpecRec Spec)
        {
            return Spec.OutputGainLFOs;
        }

        /* set normal power */
        public static void PutCompressorNormalPower(
            CompressorSpecRec Spec,
            double Power,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.NormalPowerFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.NormalPowerFormula = Formula;
            Spec.NormalPower = Power;
        }

        /* set normal power accent */
        public static void PutCompressorNormalPowerAccent(
            CompressorSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.NormalPowerAccent, AccentNum, Value);
        }

        /* get aggregated normal power info */
        public static void GetCompressorNormalPowerAgg(
            CompressorSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.NormalPower,
                ref Spec.NormalPowerAccent,
                Spec.NormalPowerFormula,
                out ParamsOut);
        }


        /* get normal power envelope */
        public static EnvelopeRec GetCompressorNormalPowerEnvelope(CompressorSpecRec Spec)
        {
            return Spec.NormalPowerEnvelope;
        }

        /* get normal power LFO list */
        public static LFOListSpecRec GetCompressorNormalPowerLFOList(CompressorSpecRec Spec)
        {
            return Spec.NormalPowerLFOs;
        }

        /* set threshhold power */
        public static void PutCompressorThreshPower(
            CompressorSpecRec Spec,
            double Power,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.ThreshPowerFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.ThreshPowerFormula = Formula;
            Spec.ThreshPower = Power;
        }

        /* set threshhold power accent */
        public static void PutCompressorThreshPowerAccent(
            CompressorSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.ThreshPowerAccent, AccentNum, Value);
        }


        /* get aggregated threshhold power info */
        public static void GetCompressorThreshPowerAgg(
            CompressorSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.ThreshPower,
                ref Spec.ThreshPowerAccent,
                Spec.ThreshPowerFormula,
                out ParamsOut);
        }

        /* get threshhold power envelope */
        public static EnvelopeRec GetCompressorThreshPowerEnvelope(CompressorSpecRec Spec)
        {
            return Spec.ThreshPowerEnvelope;
        }

        /* get threshhold power LFO list */
        public static LFOListSpecRec GetCompressorThreshPowerLFOList(CompressorSpecRec Spec)
        {
            return Spec.ThreshPowerLFOs;
        }

        /* set compression ratio */
        public static void PutCompressorRatio(
            CompressorSpecRec Spec,
            double Ratio,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.RatioFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.RatioFormula = Formula;
            Spec.Ratio = Ratio;
        }

        /* set compression ratio accent */
        public static void PutCompressorRatioAccent(
            CompressorSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.RatioAccent, AccentNum, Value);
        }

        /* get aggregated compression ratio info */
        public static void GetCompressorRatioAgg(
            CompressorSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.Ratio,
                ref Spec.RatioAccent,
                Spec.RatioFormula,
                out ParamsOut);
        }

        /* get compression ratio envelope */
        public static EnvelopeRec GetCompressorRatioEnvelope(CompressorSpecRec Spec)
        {
            return Spec.RatioEnvelope;
        }

        /* get compression ratio LFO list */
        public static LFOListSpecRec GetCompressorRatioLFOList(CompressorSpecRec Spec)
        {
            return Spec.RatioLFOs;
        }

        /* set power estimator filter cutoff frequency */
        public static void PutCompressorFilterFreq(
            CompressorSpecRec Spec,
            double Cutoff,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.FilterCutoffFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.FilterCutoffFormula = Formula;
            Spec.FilterCutoff = Cutoff;
        }

        /* set power estimator filter cutoff frequency accent */
        public static void PutCompressorFilterFreqAccent(
            CompressorSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.FilterCutoffAccent, AccentNum, Value);
        }

        /* get aggregated power estimator filter cutoff frequency info */
        public static void GetCompressorFilterFreqAgg(
            CompressorSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.FilterCutoff,
                ref Spec.FilterCutoffAccent,
                Spec.FilterCutoffFormula,
                out ParamsOut);
        }

        /* get power estimator filter cutoff frequency envelope */
        public static EnvelopeRec GetCompressorFilterFreqEnvelope(CompressorSpecRec Spec)
        {
            return Spec.FilterCutoffEnvelope;
        }

        /* get power estimator filter cutoff frequency LFO list */
        public static LFOListSpecRec GetCompressorFilterFreqLFOList(CompressorSpecRec Spec)
        {
            return Spec.FilterCutoffLFOs;
        }

        /* set increasing gain decay rate (seconds per doubling) */
        public static void PutCompressorDecayRate(
            CompressorSpecRec Spec,
            double Rate,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.DecayRateFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.DecayRateFormula = Formula;
            Spec.DecayRate = Rate;
        }

        /* set increasing gain decay rate (seconds per doubling) accent */
        public static void PutCompressorDecayRateAccent(
            CompressorSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.DecayRateAccent, AccentNum, Value);
        }

        /* get aggregated increasing gain decay rate (seconds per doubling) info */
        public static void GetCompressorDecayRateAgg(
            CompressorSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.DecayRate,
                ref Spec.DecayRateAccent,
                Spec.DecayRateFormula,
                out ParamsOut);
        }

        /* get increasing gain decay rate (seconds per doubling) envelope */
        public static EnvelopeRec GetCompressorDecayRateEnvelope(CompressorSpecRec Spec)
        {
            return Spec.DecayRateEnvelope;
        }

        /* get increasing gain decay rate (seconds per doubling) LFO list */
        public static LFOListSpecRec GetCompressorDecayRateLFOList(CompressorSpecRec Spec)
        {
            return Spec.DecayRateLFOs;
        }

        /* set decreasing gain attack rate (seconds per doubling) */
        public static void PutCompressorAttackRate(
            CompressorSpecRec Spec,
            double Rate,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.AttackRateFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.AttackRateFormula = Formula;
            Spec.AttackRate = Rate;
        }

        /* set decreasing gain attack rate (seconds per doubling) accent */
        public static void PutCompressorAttackRateAccent(
            CompressorSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.AttackRateAccent, AccentNum, Value);
        }

        /* get aggregated decreasing gain attack rate (seconds per doubling) info */
        public static void GetCompressorAttackRateAgg(
            CompressorSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.AttackRate,
                ref Spec.AttackRateAccent,
                Spec.AttackRateFormula,
                out ParamsOut);
        }

        /* get decreasing gain attack rate (seconds per doubling) envelope */
        public static EnvelopeRec GetCompressorAttackRateEnvelope(CompressorSpecRec Spec)
        {
            return Spec.AttackRateEnvelope;
        }

        /* get decreasing gain attack rate (seconds per doubling) LFO list */
        public static LFOListSpecRec GetCompressorAttackRateLFOList(CompressorSpecRec Spec)
        {
            return Spec.AttackRateLFOs;
        }

        /* set limiting excess (above 1) for soft clipping */
        public static void PutCompressorLimitingExcess(
            CompressorSpecRec Spec,
            double Limit,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.LimitingExcessFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.LimitingExcessFormula = Formula;
            Spec.LimitingExcess = Limit;
        }

        /* set limiting excess (above 1) for soft clipping accent */
        public static void PutCompressorLimitingExcessAccent(
            CompressorSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.LimitingExcessAccent, AccentNum, Value);
        }

        /* get aggregated limiting excess (above 1) for soft clipping info */
        public static void GetCompressorLimitingExcessAgg(
            CompressorSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.LimitingExcess,
                ref Spec.LimitingExcessAccent,
                Spec.LimitingExcessFormula,
                out ParamsOut);
        }

        /* get limiting excess (above 1) for soft clipping envelope */
        public static EnvelopeRec GetCompressorLimitingExcessEnvelope(CompressorSpecRec Spec)
        {
            return Spec.LimitingExcessEnvelope;
        }

        /* get limiting excess (above 1) for soft clipping LFO list */
        public static LFOListSpecRec GetCompressorLimitingExcessLFOList(CompressorSpecRec Spec)
        {
            return Spec.LimitingExcessLFOs;
        }

        /* change the compressor power estimating mode */
        public static void PutCompressorPowerEstimatorMode(
            CompressorSpecRec Spec,
            CompressorPowerEstType PowerMode)
        {
#if DEBUG
            if ((PowerMode != CompressorPowerEstType.eCompressPowerAbsVal)
                && (PowerMode != CompressorPowerEstType.eCompressPowerRMS)
                && (PowerMode != CompressorPowerEstType.eCompressPowerPeak)
                && (PowerMode != CompressorPowerEstType.eCompressPowerpeaklookahead))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.PowerEstimatorMode = PowerMode;
        }

        /* get power estimating mode for compressor */
        public static CompressorPowerEstType GetCompressorPowerEstimatorMode(CompressorSpecRec Spec)
        {
            return Spec.PowerEstimatorMode;
        }
    }
}
