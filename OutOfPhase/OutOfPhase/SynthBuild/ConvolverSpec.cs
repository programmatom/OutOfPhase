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
        public enum ConvolveSrcType
        {
            eConvolveMono,
            eConvolveStereo,
            eConvolveBiStereo,
        }

        public const double CONVOLVERMAXLATENCY = 1000;

        public class ConvolverSpecRec
        {
            public string ImpulseResponseNameMono;
            /* or */
            public string ImpulseResponseNameStereoLeft;
            public string ImpulseResponseNameStereoRight;
            /* or */
            public string ImpulseResponseNameBiStereoLeftIntoLeft;
            public string ImpulseResponseNameBiStereoRightIntoLeft;
            public string ImpulseResponseNameBiStereoLeftIntoRight;
            public string ImpulseResponseNameBiStereoRightIntoRight;

            public double DirectGain;
            public AccentRec DirectGainAccent;
            public PcodeRec DirectGainFormula;
            public double ProcessedGain;
            public AccentRec ProcessedGainAccent;
            public PcodeRec ProcessedGainFormula;

            public bool LatencySpecified;
            public double Latency;

            public ConvolveSrcType Source;
        }

        /* create new convolver spec */
        public static ConvolverSpecRec NewConvolverSpec()
        {
            ConvolverSpecRec Spec = new ConvolverSpecRec();

            Spec.Source = ConvolveSrcType.eConvolveMono;

            return Spec;
        }

        /* set source channel */
        public static void ConvolverSpecSetSourceType(
            ConvolverSpecRec Spec,
            ConvolveSrcType Source)
        {
#if DEBUG
            if ((Source != ConvolveSrcType.eConvolveMono) && (Source != ConvolveSrcType.eConvolveStereo)
                && (Source != ConvolveSrcType.eConvolveBiStereo))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.Source = Source;
        }

        /* get source channel */
        public static ConvolveSrcType ConvolverSpecGetSourceType(ConvolverSpecRec Spec)
        {
            return Spec.Source;
        }

        /* set latency */
        public static void ConvolverSpecSetLatency(
            ConvolverSpecRec Spec,
            double Latency)
        {
            Spec.Latency = Latency;
            Spec.LatencySpecified = true;
        }

        public static bool ConvolverSpecGetLatencySpecified(ConvolverSpecRec Spec)
        {
            return Spec.LatencySpecified;
        }

        /* get latency */
        public static double ConvolverSpecGetLatency(ConvolverSpecRec Spec)
        {
            Debug.Assert(Spec.LatencySpecified);
            return Spec.Latency;
        }

        /* impulse response name, for mono */
        public static void ConvolverSpecSetImpulseResponseMono(
            ConvolverSpecRec Spec,
            string Name)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameMono != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.ImpulseResponseNameMono = Name;
        }

        /* impulse response name, for mono */
        public static string ConvolverSpecGetImpulseResponseMono(ConvolverSpecRec Spec)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameMono == null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Spec.ImpulseResponseNameMono;
        }

        /* impulse response name, for stereo left */
        public static void ConvolverSpecSetImpulseResponseStereoLeft(
            ConvolverSpecRec Spec,
            string Name)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameStereoLeft != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.ImpulseResponseNameStereoLeft = Name;
        }

        /* impulse response name, for stereo left */
        public static string ConvolverSpecGetImpulseResponseStereoLeft(ConvolverSpecRec Spec)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameStereoLeft == null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Spec.ImpulseResponseNameStereoLeft;
        }

        /* impulse response name, for stereo right */
        public static void ConvolverSpecSetImpulseResponseStereoRight(
            ConvolverSpecRec Spec,
            string Name)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameStereoRight != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.ImpulseResponseNameStereoRight = Name;
        }

        /* impulse response name, for stereo Right */
        public static string ConvolverSpecGetImpulseResponseStereoRight(ConvolverSpecRec Spec)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameStereoRight == null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Spec.ImpulseResponseNameStereoRight;
        }

        /* impulse response name, for bistereo left into left */
        public static void ConvolverSpecSetImpulseResponseBiStereoLeftIntoLeft(
            ConvolverSpecRec Spec,
            string Name)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameBiStereoLeftIntoLeft != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.ImpulseResponseNameBiStereoLeftIntoLeft = Name;
        }

        /* impulse response name, for bistereo left into left */
        public static string ConvolverSpecGetImpulseResponseBiStereoLeftIntoLeft(ConvolverSpecRec Spec)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameBiStereoLeftIntoLeft == null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Spec.ImpulseResponseNameBiStereoLeftIntoLeft;
        }

        /* impulse response name, for bistereo right into left */
        public static void ConvolverSpecSetImpulseResponseBiStereoRightIntoLeft(
            ConvolverSpecRec Spec,
            string Name)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameBiStereoRightIntoLeft != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.ImpulseResponseNameBiStereoRightIntoLeft = Name;
        }

        /* impulse response name, for bistereo right into left */
        public static string ConvolverSpecGetImpulseResponseBiStereoRightIntoLeft(ConvolverSpecRec Spec)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameBiStereoRightIntoLeft == null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Spec.ImpulseResponseNameBiStereoRightIntoLeft;
        }

        /* impulse response name, for bistereo left into right */
        public static void ConvolverSpecSetImpulseResponseBiStereoLeftIntoRight(
            ConvolverSpecRec Spec,
            string Name)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameBiStereoLeftIntoRight != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.ImpulseResponseNameBiStereoLeftIntoRight = Name;
        }

        /* impulse response name, for bistereo left into right */
        public static string ConvolverSpecGetImpulseResponseBiStereoLeftIntoRight(ConvolverSpecRec Spec)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameBiStereoLeftIntoRight == null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Spec.ImpulseResponseNameBiStereoLeftIntoRight;
        }

        /* impulse response name, for bistereo right into right */
        public static void ConvolverSpecSetImpulseResponseBiStereoRightIntoRight(
            ConvolverSpecRec Spec,
            string Name)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameBiStereoRightIntoRight != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.ImpulseResponseNameBiStereoRightIntoRight = Name;
        }

        /* impulse response name, for bistereo right into right */
        public static string ConvolverSpecGetImpulseResponseBiStereoRightIntoRight(ConvolverSpecRec Spec)
        {
#if DEBUG
            if (Spec.ImpulseResponseNameBiStereoRightIntoRight == null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Spec.ImpulseResponseNameBiStereoRightIntoRight;
        }

        /* direct gain factor */
        public static void ConvolverSpecSetDirectGain(
            ConvolverSpecRec Spec,
            double Gain,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.DirectGainFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.DirectGainFormula = Formula;
            Spec.DirectGain = Gain;
        }

        /* direct gain factor */
        public static void ConvolverSpecSetDirectGainAccent(
            ConvolverSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.DirectGainAccent, AccentNum, Value);
        }

        /* direct gain factor */
        public static void ConvolverSpecGetDirectGain(
            ConvolverSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.DirectGain,
                ref Spec.DirectGainAccent,
                Spec.DirectGainFormula,
                out ParamsOut);
        }

        /* processed gain factor */
        public static void ConvolverSpecSetProcessedGain(
            ConvolverSpecRec Spec,
            double Gain,
            PcodeRec Formula)
        {
#if DEBUG
            if (Spec.ProcessedGainFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.ProcessedGainFormula = Formula;
            Spec.ProcessedGain = Gain;
        }

        /* processed gain factor */
        public static void ConvolverSpecSetProcessedGainAccent(
            ConvolverSpecRec Spec,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.ProcessedGainAccent, AccentNum, Value);
        }

        /* processed gain factor */
        public static void ConvolverSpecGetProcessedGain(
            ConvolverSpecRec Spec,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.ProcessedGain,
                ref Spec.ProcessedGainAccent,
                Spec.ProcessedGainFormula,
                out ParamsOut);
        }
    }
}
