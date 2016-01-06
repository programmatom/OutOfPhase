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

        public class SecondOrderResonRec : IFilter
        {
            /* state variables */
            public float Ym1;
            public float Ym2;

            /* coefficients */
            public float A0;
            public float B1;
            public float B2;


            public FilterTypes FilterType { get { return FilterTypes.eFilterSecondOrderResonant; } }

            /* adjust filter coefficients */
            public static void SetSecondOrderResonCoefficients(
                SecondOrderResonRec Filter,
                double Cutoff,
                double Bandwidth,
                FilterScalings Scaling,
                double SamplingRate)
            {
                double B1;
                double B2;
                double OneOverSamplingRate;
                double HalfSamplingRateMinusEpsilon;

                OneOverSamplingRate = 1 / SamplingRate;
                HalfSamplingRateMinusEpsilon = SamplingRate * 0.5 - FILTER_FREQ_EPSILON;

                if (Bandwidth < FILTER_FREQ_EPSILON)
                {
                    Bandwidth = FILTER_FREQ_EPSILON;
                }
                if (Bandwidth > HalfSamplingRateMinusEpsilon)
                {
                    Bandwidth = HalfSamplingRateMinusEpsilon;
                }
                if (Cutoff < FILTER_FREQ_EPSILON)
                {
                    Cutoff = FILTER_FREQ_EPSILON;
                }
                if (Cutoff > HalfSamplingRateMinusEpsilon)
                {
                    Cutoff = HalfSamplingRateMinusEpsilon;
                }

                B2 = Math.Exp(NEGTWOPI * Bandwidth * OneOverSamplingRate);
                Filter.B2 = (float)B2;
                B1 = ((-4 * B2) / (1 + B2)) * Math.Cos(TWOPI * Cutoff * OneOverSamplingRate);
                Filter.B1 = (float)B1;
                switch (Scaling)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case FilterScalings.eFilterDefaultScaling:
                        Filter.A0 = 1;
                        break;
                    case FilterScalings.eFilterResonMidbandGain1:
                        Filter.A0 = (float)((1 - B2) * Math.Sqrt(1 - B1 * B1 / (4 * B2)));
                        break;
                    case FilterScalings.eFilterResonNoiseGain1:
                        double X = 1 + B2;
                        double Y = 1 - B2;
                        Filter.A0 = (float)(Math.Sqrt((X * X - B1 * B1) * Y / X));
                        break;
                }
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
                SecondOrderResonRec Filter = this;

                float Ym1 = Filter.Ym1;
                float Ym2 = Filter.Ym2;
                float A0 = Filter.A0;
                float B1 = Filter.B1;
                float B2 = Filter.B2;

                for (int i = 0; i < VectorLength; i += 1)
                {
                    float Xin = XinVector[i + XinVectorOffset];
                    float OrigYOut = YoutVector[i + YoutVectorOffset];
                    float Y = A0 * Xin - B1 * Ym1 - B2 * Ym2;
                    Ym2 = Ym1;
                    Ym1 = Y;
                    YoutVector[i + YoutVectorOffset] = OrigYOut + Y * OutputScaling;
                }

                Filter.Ym1 = Ym1;
                Filter.Ym2 = Ym2;
            }
        }
    }
}
