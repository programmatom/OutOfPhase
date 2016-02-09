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

        public class SecondOrderZeroRec : IFilter
        {
            /* state variables */
            public float Xm1;
            public float Xm2;

            /* coefficients */
            public float A0;
            public float A1;
            public float A2;

            public readonly FilterScalings Scaling;


            public SecondOrderZeroRec(
                FilterScalings Scaling)
            {
                this.Scaling = Scaling;
            }

            public FilterTypes FilterType { get { return FilterTypes.eFilterSecondOrderZero; } }

            public static void SetSecondOrderZeroCoefficients(
                SecondOrderZeroRec Filter,
                double Cutoff,
                double Bandwidth,
                FilterScalings Scaling,
                double SamplingRate)
            {
                double C2;
                double C1;
                double D;
                double HalfSamplingRateMinusEpsilon;
                double OneOverD;
                double OneOverSamplingRate;

                HalfSamplingRateMinusEpsilon = SamplingRate * 0.5 - FILTER_FREQ_EPSILON;
                OneOverSamplingRate = 1.0 / SamplingRate;

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

                C2 = Math.Exp(NEGTWOPI * Bandwidth * OneOverSamplingRate);
                C1 = (-4 * C2 / (1 + C2)) * Math.Cos(TWOPI * Cutoff * OneOverSamplingRate);
                switch (Scaling)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case FilterScalings.eFilterDefaultScaling:
                        D = 1;
                        break;
                    case FilterScalings.eFilterZeroGain1:
                        D = 1 + C1 + C2;
                        break;
                }
                OneOverD = 1 / D;
                Filter.A0 = (float)OneOverD;
                Filter.A1 = (float)(C1 * OneOverD);
                Filter.A2 = (float)(C2 * OneOverD);
            }

            public void UpdateParams(
                ref FilterParams Params)
            {
                SetSecondOrderZeroCoefficients(
                    this,
                    Params.Cutoff,
                    Params.BandwidthOrSlope,
                    Scaling,
                    Params.SamplingRate);
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
                SecondOrderZeroRec Filter = this;

                float Xm1 = Filter.Xm1;
                float Xm2 = Filter.Xm2;
                float A0 = Filter.A0;
                float A1 = Filter.A1;
                float A2 = Filter.A2;

                for (int i = 0; i < VectorLength; i += 1)
                {
                    float Xin = XinVector[i + XinVectorOffset];
                    float OrigYOut = YoutVector[i + YoutVectorOffset];
                    float Y = A0 * Xin + A1 * Xm1 + A2 * Xm2;
                    Xm2 = Xm1;
                    Xm1 = Xin;
                    YoutVector[i + YoutVectorOffset] = OrigYOut + Y * OutputScaling;
                }

                Filter.Xm1 = Xm1;
                Filter.Xm2 = Xm2;
            }
        }
    }
}
