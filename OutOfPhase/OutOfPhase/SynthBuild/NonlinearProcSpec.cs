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
        public class NonlinProcSpecRec
        {
            /* wave table name */
            public string WaveTableName;

            /* these controls only apply to the track effect, not the oscillator effect */
            public double InputScaling;
            public AccentRec InputScalingAccent;
            public double OutputScaling;
            public AccentRec OutputScalingAccent;
            public double WaveTableIndex;
            public AccentRec WaveTableIndexAccent;
            public PcodeRec InputScalingFormula;
            public PcodeRec OutputScalingFormula;
            public PcodeRec WaveTableIndexFormula;

            /* these controls apply only to the oscillator effect, not the track effect */
            public EnvelopeRec InputEnvelope;
            public EnvelopeRec OutputEvelope;
            public EnvelopeRec IndexEnvelope;
            public LFOListSpecRec InputLFO;
            public LFOListSpecRec OutputLFO;
            public LFOListSpecRec IndexLFO;
        }

        /* create a new nonlinear processor specifier.  name block is deleted */
        public static NonlinProcSpecRec NewNonlinProcSpec(string WaveTableName)
        {
            NonlinProcSpecRec NLProc = new NonlinProcSpecRec();

            NLProc.WaveTableName = WaveTableName;

            NLProc.InputEnvelope = NewEnvelope();
            NLProc.OutputEvelope = NewEnvelope();
            NLProc.IndexEnvelope = NewEnvelope();
            NLProc.InputLFO = NewLFOListSpecifier();
            NLProc.OutputLFO = NewLFOListSpecifier();
            NLProc.IndexLFO = NewLFOListSpecifier();
            //InitializeAccentZero(out NLProc.InputScalingAccent);
            //InitializeAccentZero(out NLProc.OutputScalingAccent);
            //InitializeAccentZero(out NLProc.WaveTableIndexAccent);
            NLProc.InputScaling = 1;
            NLProc.OutputScaling = 1;
            //NLProc.WaveTableIndex = 0;
            //NLProc.InputScalingFormula = null;
            //NLProc.OutputScalingFormula = null;
            //NLProc.WaveTableIndexFormula = null;

            return NLProc;
        }

        public static void PutNLProcInputScaling(
            NonlinProcSpecRec NLProc,
            double InputScaling,
            PcodeRec Formula)
        {
#if DEBUG
            if (NLProc.InputScalingFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            NLProc.InputScalingFormula = Formula;
            NLProc.InputScaling = InputScaling;
        }


        public static void PutNLProcInputAccent(
            NonlinProcSpecRec NLProc,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref NLProc.InputScalingAccent, AccentNum, Value);
        }

        public static void PutNLProcOutputScaling(
            NonlinProcSpecRec NLProc,
            double OutputScaling,
            PcodeRec Formula)
        {
#if DEBUG
            if (NLProc.OutputScalingFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            NLProc.OutputScalingFormula = Formula;
            NLProc.OutputScaling = OutputScaling;
        }

        public static void PutNLProcOutputAccent(
            NonlinProcSpecRec NLProc,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref NLProc.OutputScalingAccent, AccentNum, Value);
        }

        public static void PutNLProcWaveTableIndex(
            NonlinProcSpecRec NLProc,
            double WaveTableIndex,
            PcodeRec Formula)
        {
#if DEBUG
            if (NLProc.WaveTableIndexFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            NLProc.WaveTableIndexFormula = Formula;
            NLProc.WaveTableIndex = WaveTableIndex;
        }

        public static void PutNLProcIndexAccent(
            NonlinProcSpecRec NLProc,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref NLProc.WaveTableIndexAccent, AccentNum, Value);
        }

        public static void GetNLProcInputScalingAgg(
            NonlinProcSpecRec NLProc,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                NLProc.InputScaling,
                ref NLProc.InputScalingAccent,
                NLProc.InputScalingFormula,
                out ParamsOut);
        }

        public static void GetNLProcOutputScalingAgg(
            NonlinProcSpecRec NLProc,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                NLProc.OutputScaling,
                ref NLProc.OutputScalingAccent,
                NLProc.OutputScalingFormula,
                out ParamsOut);
        }

        public static void GetNLProcWaveTableIndexAgg(
            NonlinProcSpecRec NLProc,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                NLProc.WaveTableIndex,
                ref NLProc.WaveTableIndexAccent,
                NLProc.WaveTableIndexFormula,
                out ParamsOut);
        }

        /* get the actual wave table name heap block */
        public static string GetNLProcSpecWaveTableName(NonlinProcSpecRec NLProc)
        {
            return NLProc.WaveTableName;
        }

        public static EnvelopeRec GetNLProcInputEnvelope(NonlinProcSpecRec NLProc)
        {
            return NLProc.InputEnvelope;
        }

        public static EnvelopeRec GetNLProcOutputEnvelope(NonlinProcSpecRec NLProc)
        {
            return NLProc.OutputEvelope;
        }

        public static EnvelopeRec GetNLProcIndexEnvelope(NonlinProcSpecRec NLProc)
        {
            return NLProc.IndexEnvelope;
        }

        public static LFOListSpecRec GetNLProcInputLFO(NonlinProcSpecRec NLProc)
        {
            return NLProc.InputLFO;
        }

        public static LFOListSpecRec GetNLProcOutputLFO(NonlinProcSpecRec NLProc)
        {
            return NLProc.OutputLFO;
        }

        public static LFOListSpecRec GetNLProcIndexLFO(NonlinProcSpecRec NLProc)
        {
            return NLProc.IndexLFO;
        }
    }
}
