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

        public class ButterworthBandpassRec : IFilter
        {
            /* filter state */
            public IIR2DirectIRec iir;

            /* old stuff */
            public double OldCutoff = -1e300;
            public double OldBandwidth = -1e300;


            public FilterTypes FilterType { get { return FilterTypes.eFilterButterworthBandpass; } }

            public static void ComputeButterworthBandpassCoefficients(
                ref IIR2DirectIRec Coeff,
                double Cutoff,
                double Bandwidth,
                double SamplingRate)
            {
                double C;
                double D;
                double A0;
                double HalfSamplingRate;
                double OneOverSamplingRateTimesPi;
                double HalfSamplingRateMinusEpsilon;
                double BandwidthTan;
                double CutoffCos;

                HalfSamplingRate = SamplingRate * 0.5;
                OneOverSamplingRateTimesPi = Math.PI / SamplingRate;
                HalfSamplingRateMinusEpsilon = HalfSamplingRate - FILTER_FREQ_EPSILON;

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

                BandwidthTan = Bandwidth * OneOverSamplingRateTimesPi;
                CutoffCos = Cutoff * OneOverSamplingRateTimesPi;

                BandwidthTan = Math.Tan(BandwidthTan);
                CutoffCos = Math.Cos(CutoffCos + CutoffCos); /* 2 * CutoffCos */

                C = 1 / BandwidthTan;
                D = CutoffCos + CutoffCos; /* 2 * CutoffCos */
                A0 = 1 / (1 + C);
                Coeff.A0 = (float)A0;
                Coeff.A1 = 0;
                Coeff.A2 = (float)(-A0);
                Coeff.B1 = (float)(-C * D * A0);
                Coeff.B2 = (float)((C - 1) * A0);
            }

            public static void SetButterworthBandpassCoefficients(
                ButterworthBandpassRec Filter,
                double Cutoff,
                double Bandwidth,
                double SamplingRate)
            {
                if ((Cutoff == Filter.OldCutoff) && (Bandwidth == Filter.OldBandwidth))
                {
                    return;
                }
                Filter.OldCutoff = Cutoff;
                Filter.OldBandwidth = Bandwidth;

                ComputeButterworthBandpassCoefficients(
                    ref Filter.iir,
                    Cutoff,
                    Bandwidth,
                    SamplingRate);
            }

            public void UpdateParams(
                ref FilterParams Params)
            {
                SetButterworthBandpassCoefficients(
                    this,
                    Params.Cutoff,
                    Params.BandwidthOrSlope,
                    Params.SamplingRate);
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
                ButterworthBandpassRec Filter = this;

                IIR2DirectIMAcc(
                    ref Filter.iir,
                    XinVector,
                    XInVectorOffset,
                    YoutVector,
                    YoutVectorOffset,
                    VectorLength,
                    OutputScaling);
            }

            /* apply filter to an array of values, modifying the array in place */
            public static void ApplyButterworthBandpassVectorModify(
                ButterworthBandpassRec Filter,
                float[] Vector,
                int VectorOffset,
                int VectorLength)
            {
                IIR2DirectI(
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
