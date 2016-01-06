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
        public class VocoderSpecRec
        {
            /* wave table name */
            public string WaveTableName;

            /* maximum number of bands in the vocoder */
            public int MaxBandCount;

            /* order of filter for each band */
            public int FilterOrder;

            /* track/score effect parameters */
            public double WaveTableIndex;
            public AccentRec WaveTableIndexAccent;
            public double OutputGain;
            public AccentRec OutputGainAccent;
            public PcodeRec WaveTableIndexFormula;
            public PcodeRec OutputGainFormula;

            /* oscillator effect parameters */
            public EnvelopeRec IndexEnvelope;
            public EnvelopeRec OutputGainEnvelope;
            public LFOListSpecRec IndexLFO;
            public LFOListSpecRec OutputGainLFO;
        }

        public static VocoderSpecRec NewVocoderSpec(
            string WaveTableName)
        {
            VocoderSpecRec VocSpec = new VocoderSpecRec();

            VocSpec.WaveTableName = WaveTableName;
            VocSpec.IndexEnvelope = NewEnvelope();
            VocSpec.OutputGainEnvelope = NewEnvelope();
            VocSpec.IndexLFO = NewLFOListSpecifier();
            VocSpec.OutputGainLFO = NewLFOListSpecifier();
            //InitializeAccentZero(out VocSpec.WaveTableIndexAccent);
            //InitializeAccentZero(out VocSpec.OutputGainAccent);
            VocSpec.WaveTableIndex = 0;
            VocSpec.OutputGain = 1;
            //VocSpec.MaxBandCount = 0;
            VocSpec.FilterOrder = 2;
            //VocSpec.WaveTableIndexFormula = null;
            //VocSpec.OutputGainFormula = null;

            return VocSpec;
        }

        public static void PutVocoderSpecWaveTableIndex(
            VocoderSpecRec VocSpec,
            double WaveTableIndex,
            PcodeRec Formula)
        {
#if DEBUG
            if (VocSpec.WaveTableIndexFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            VocSpec.WaveTableIndexFormula = Formula;
            VocSpec.WaveTableIndex = WaveTableIndex;
        }

        public static void PutVocoderSpecIndexAccent(
            VocoderSpecRec VocSpec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref VocSpec.WaveTableIndexAccent, AccentNum, Value);
        }

        public static void PutVocoderSpecOutputGain(
            VocoderSpecRec VocSpec,
            double OutputGain,
            PcodeRec Formula)
        {
#if DEBUG
            if (VocSpec.OutputGainFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            VocSpec.OutputGainFormula = Formula;
            VocSpec.OutputGain = OutputGain;
        }

        public static void PutVocoderSpecOutputGainAccent(
            VocoderSpecRec VocSpec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref VocSpec.OutputGainAccent, AccentNum, Value);
        }

        public static void GetVocoderSpecWaveTableIndexAgg(
            VocoderSpecRec VocSpec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                VocSpec.WaveTableIndex,
                ref VocSpec.WaveTableIndexAccent,
                VocSpec.WaveTableIndexFormula,
                out ParamsOut);
        }

        public static void GetVocoderSpecOutputGainAgg(
            VocoderSpecRec VocSpec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                VocSpec.OutputGain,
                ref VocSpec.OutputGainAccent,
                VocSpec.OutputGainFormula,
                out ParamsOut);
        }

        public static string GetVocoderSpecWaveTableName(VocoderSpecRec VocSpec)
        {
            return VocSpec.WaveTableName;
        }

        public static EnvelopeRec GetVocoderSpecIndexEnvelope(VocoderSpecRec VocSpec)
        {
            return VocSpec.IndexEnvelope;
        }

        public static EnvelopeRec GetVocoderSpecOutputGainEnvelope(VocoderSpecRec VocSpec)
        {
            return VocSpec.OutputGainEnvelope;
        }

        public static LFOListSpecRec GetVocoderSpecIndexLFO(VocoderSpecRec VocSpec)
        {
            return VocSpec.IndexLFO;
        }

        public static LFOListSpecRec GetVocoderSpecOutputGainLFO(VocoderSpecRec VocSpec)
        {
            return VocSpec.OutputGainLFO;
        }

        /* specify the maximum number of bands for the vocoder */
        public static void PutVocoderMaxNumBands(
            VocoderSpecRec VocSpec,
            int MaxCount)
        {
#if DEBUG
            if (MaxCount < 1)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            VocSpec.MaxBandCount = MaxCount;
        }

        /* get the maximum number of bands for the vocoder */
        public static int GetVocoderMaxNumBands(VocoderSpecRec VocSpec)
        {
            return VocSpec.MaxBandCount;
        }

        /* put the filter order (positive even number) */
        public static void PutVocoderFilterOrder(
            VocoderSpecRec VocSpec,
            int Order)
        {
#if DEBUG
            if ((Order < 2) || (Order % 2 != 0))
            {
                // order must be positive even number
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            VocSpec.FilterOrder = Order;
        }

        /* get the filter order */
        public static int GetVocoderFilterOrder(VocoderSpecRec VocSpec)
        {
            return VocSpec.FilterOrder;
        }
    }
}
