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
        /* Originally from the article by P. A. Regalia and S. K. Mitra, */
        /* "Tunable Digital Frequency Response Equalization Filters", */
        /* IEEE Trans. on ASSP Vol. ASSP-35, 1. */
        /* Implementation borrowed from the Music 4C program at the */
        /* University of Illinois, Urbana-Champaign's */
        /* Computer Music Project */

        public class ParametricEqualizerRec : IFilter
        {
            /* old stuff */
            public double OldCutoff = -1e300;
            public double OldBandwidth = -1e300;
            public double OldGain = -1e300;

            /* state variables */
            public float State1;
            public float State0;

            /* coefficients */
            public float A;
            public float B;
            public float K;


            public FilterTypes FilterType { get { return FilterTypes.eFilterParametricEQ; } }

            public static void SetParametricEqualizerCoefficients(
                ParametricEqualizerRec Filter,
                double Cutoff,
                double Bandwidth,
                double Gain,
                double SamplingRate)
            {
                double X;
                double OneOverSamplingRate;

                if ((Cutoff == Filter.OldCutoff) && (Bandwidth == Filter.OldBandwidth) && (Gain == Filter.OldGain))
                {
                    return;
                }
                Filter.OldCutoff = Cutoff;
                Filter.OldBandwidth = Bandwidth;
                Filter.OldGain = Gain;

                OneOverSamplingRate = 1.0 / SamplingRate;

                if (Bandwidth < FILTER_FREQ_EPSILON)
                {
                    Bandwidth = FILTER_FREQ_EPSILON;
                }
                if (Bandwidth > SamplingRate * 0.25 - FILTER_FREQ_EPSILON)
                {
                    Bandwidth = SamplingRate * 0.25 - FILTER_FREQ_EPSILON;
                }
                if (Cutoff < FILTER_FREQ_EPSILON)
                {
                    Cutoff = FILTER_FREQ_EPSILON;
                }
                if (Cutoff > SamplingRate * 0.5 - FILTER_FREQ_EPSILON)
                {
                    Cutoff = SamplingRate * 0.5 - FILTER_FREQ_EPSILON;
                }

                X = Math.Tan(TWOPI * Bandwidth * OneOverSamplingRate);
                Filter.A = (float)((1 - X) / (1 + X));
                Filter.B = (float)(-Math.Cos(TWOPI * Cutoff * OneOverSamplingRate));
                Filter.K = (float)Gain;
            }

            public void UpdateParams(
                ref FilterParams Params)
            {
                SetParametricEqualizerCoefficients(
                    this,
                    Params.Cutoff,
                    Params.BandwidthOrSlope,
                    Params.Gain,
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
                ParametricEqualizerRec Filter = this;

                float State1 = Filter.State1;
                float State0 = Filter.State0;
                float A = Filter.A;
                float B = Filter.B;
                float K = Filter.K;

                float OneMinusK = 1 - K;
                float OnePlusK = 1 + K;

                for (int i = 0; i < VectorLength; i += 1)
                {
                    float Xin = XinVector[i + XinVectorOffset];
                    float OrigYOut = YoutVector[i + YoutVectorOffset];
                    float Allpass = A * (Xin - A * State1) + State1;
                    float Outval = .5f * (OnePlusK * Xin + OneMinusK * Allpass);
                    float Temp = Xin - A * State1 - B * State0;
                    State1 = B * Temp + State0;
                    State0 = Temp;
                    YoutVector[i + YoutVectorOffset] = OrigYOut + Outval * OutputScaling;
                }

                Filter.State1 = State1;
                Filter.State0 = State0;
            }
        }
    }
}
