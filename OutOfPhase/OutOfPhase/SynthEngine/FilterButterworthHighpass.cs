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
        /* Based on material from pages 184-190 of */
        /* Dodge, Charles and Jerse, Thomas A. */
        /* Computer Music:  Synthesis, Composition, and Performance */
        /* Schirmer Books, New York, 1985 */

        public class ButterworthHighpassRec : IFilter
        {
            /* filter state */
            public IIR2DirectIRec iir;

            /* old stuff */
            public double OldCutoff = -1e300;


            public FilterTypes FilterType { get { return FilterTypes.eFilterButterworthHighpass; } }

            /* compute filter coefficients */
            private static void ComputeButterworthHighpassCoefficients(
                ref IIR2DirectIRec Coeff,
                double Cutoff,
                double SamplingRate)
            {
                double C;
                double A0;
                double HalfSamplingRate;
                double HalfSamplingRateMinusEpsilon;

                HalfSamplingRate = SamplingRate * 0.5;
                HalfSamplingRateMinusEpsilon = HalfSamplingRate - FILTER_FREQ_EPSILON;

                if (Cutoff < FILTER_FREQ_EPSILON)
                {
                    Cutoff = FILTER_FREQ_EPSILON;
                }
                if (Cutoff > HalfSamplingRateMinusEpsilon)
                {
                    Cutoff = HalfSamplingRateMinusEpsilon;
                }

                C = Math.Tan(Math.PI * Cutoff / SamplingRate);
                A0 = 1 / (1 + SQRT2 * C + C * C);
                Coeff.A0 = (float)A0;
                Coeff.A1 = (float)(-2 * A0);
                Coeff.A2 = (float)A0;
                Coeff.B1 = (float)(2 * (C * C - 1) * A0);
                Coeff.B2 = (float)((1 - SQRT2 * C + C * C) * A0);
            }

            /* adjust filter coefficients */
            public static void SetButterworthHighpassCoefficients(
                ButterworthHighpassRec Filter,
                double Cutoff,
                double SamplingRate)
            {
                if (Cutoff == Filter.OldCutoff)
                {
                    return;
                }
                Filter.OldCutoff = Cutoff;

                ComputeButterworthHighpassCoefficients(
                    ref Filter.iir,
                    Cutoff,
                    SamplingRate);
            }

            /* apply filter to an array of values, adding result to output array */
            public void Apply(
                float[] XinVector,
                int XInVectorOffset,
                float[] YoutVector,
                int YoutVectorOffset,
                int VectorLength,
                float OutputScaling,
                SynthParamRec SynthParams)
            {
                ButterworthHighpassRec Filter = this;

                IIR2DirectIMAcc(
                    ref Filter.iir,
                    XinVector,
                    XInVectorOffset,
                    YoutVector,
                    YoutVectorOffset,
                    VectorLength,
                    OutputScaling);
            }
        }
    }
}
