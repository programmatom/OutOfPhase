/*
 *  Copyright � 1994-2002, 2015-2016 Thomas R. Lawrence
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

        public class FirstOrderLowpassRec : IFilter
        {
            /* filter state */
            public IIR1AllPoleRec iir;

            /* old stuff */
            public double OldCutoff = -1e300;


            public FilterTypes FilterType { get { return FilterTypes.eFilterFirstOrderLowpass; } }

            private static void ComputeFirstOrderLowpassCoefficients(
                ref IIR1AllPoleRec Coeff,
                double Cutoff,
                double SamplingRate)
            {
                double C;
                double B;
                double HalfSamplingRate;

                HalfSamplingRate = SamplingRate * 0.5;

                if (Cutoff < FILTER_FREQ_EPSILON)
                {
                    Cutoff = FILTER_FREQ_EPSILON;
                }
                if (Cutoff > HalfSamplingRate - FILTER_FREQ_EPSILON)
                {
                    Cutoff = HalfSamplingRate - FILTER_FREQ_EPSILON;
                }

                C = 2 - Math.Cos(TWOPI * Cutoff / SamplingRate);
                B = Math.Sqrt(C * C - 1) - C;
                Coeff.B = (float)B;
                Coeff.A = (float)(1 + B);
            }

            public static void SetFirstOrderLowpassCoefficients(
                FirstOrderLowpassRec Filter,
                double Cutoff,
                double SamplingRate)
            {
                if (Cutoff == Filter.OldCutoff)
                {
                    return;
                }
                Filter.OldCutoff = Cutoff;

                ComputeFirstOrderLowpassCoefficients(
                    ref Filter.iir,
                    Cutoff,
                    SamplingRate);
            }

            public void UpdateParams(
                ref FilterParams Params)
            {
                SetFirstOrderLowpassCoefficients(
                    this,
                    Params.Cutoff,
                    Params.SamplingRate);
            }

            /* apply filter to a sample value */
            public static float ApplyFirstOrderLowpass(
                FirstOrderLowpassRec Filter,
                float Xin)
            {
                Filter.iir.Y9 = Filter.iir.A * Xin - Filter.iir.B * Filter.iir.Y9;
                return Filter.iir.Y9;
            }

            /* apply filter to an array of values, adding result to output array */
            public void Apply(
                float[] XinVector,
                int XinVectorOffset,
                float[] YoutVector,
                int YoutVectorOffset,
                int VectorLength,
                float OutputScaling,
                SynthParamRec SynthParams)
            {
                FirstOrderLowpassRec Filter = this;

                IIR1AllPoleMAcc(
                    ref Filter.iir,
                    XinVector,
                    XinVectorOffset,
                    YoutVector,
                    YoutVectorOffset,
                    VectorLength,
                    OutputScaling);
            }

            /* apply filter to an array of values, modifying array */
            public static void ApplyFirstOrderLowpassVectorModify(
                FirstOrderLowpassRec Filter,
                float[] Vector,
                int VectorOffset,
                int VectorLength)
            {
                IIR1AllPole(
                    ref Filter.iir,
                    Vector,
                    VectorOffset,
                    Vector,
                    VectorOffset,
                    VectorLength);
            }
        }
    }
}
