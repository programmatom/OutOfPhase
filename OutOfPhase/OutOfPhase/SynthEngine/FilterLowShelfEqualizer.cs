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
        /* I found design for this on some website somewhere.  See included comment in */
        /* FilterHighShelfEqualizer.cs */

        public class LowShelfEqualizerRec : IFilter
        {
            /* filter state */
            public IIR2DirectIRec iir;

            /* old stuff */
            public double OldCutoff = -1e300;
            public double OldSlope = -1e300;
            public double OldGain = -1e300;


            public FilterTypes FilterType { get { return FilterTypes.eFilterLowShelfEQ; } }

            private static void ComputeLowShelfEqualizerCoefficients(
                ref IIR2DirectIRec Coeff,
                double Cutoff,
                double Slope,
                double Gain,
                double SamplingRate)
            {
                double HalfSamplingRate;
                double A;
                double Omega;
                double CS;
                double SN;
                double Beta;
                double OneOverB0;

                HalfSamplingRate = SamplingRate * 0.5;

                if (Cutoff < FILTER_FREQ_EPSILON)
                {
                    Cutoff = FILTER_FREQ_EPSILON;
                }
                if (Cutoff > HalfSamplingRate - FILTER_FREQ_EPSILON)
                {
                    Cutoff = HalfSamplingRate - FILTER_FREQ_EPSILON;
                }

                A = Math.Sqrt(Gain);
                Omega = TWOPI * Cutoff / SamplingRate;
                CS = Math.Cos(Omega);
                SN = Math.Sin(Omega);
                Beta = Math.Sqrt((A * A + 1) / Slope - ((A - 1) * (A - 1)));

                OneOverB0 = 1 / ((A + 1) + (A - 1) * CS + Beta * SN);
                Coeff.A0 = (float)(OneOverB0 * A * ((A + 1) - (A - 1) * CS + Beta * SN));
                Coeff.A1 = (float)(OneOverB0 * 2 * A * ((A - 1) - (A + 1) * CS));
                Coeff.A2 = (float)(OneOverB0 * A * ((A + 1) - (A - 1) * CS - Beta * SN));
                Coeff.B1 = (float)(OneOverB0 * -2 * ((A - 1) + (A + 1) * CS));
                Coeff.B2 = (float)(OneOverB0 * ((A + 1) + (A - 1) * CS - Beta * SN));
            }

            public static void SetLowShelfEqualizerCoefficients(
                LowShelfEqualizerRec Filter,
                double Cutoff,
                double Slope,
                double Gain,
                double SamplingRate)
            {
                if ((Cutoff == Filter.OldCutoff) && (Slope == Filter.OldSlope) && (Gain == Filter.OldGain))
                {
                    return;
                }
                Filter.OldCutoff = Cutoff;
                Filter.OldSlope = Slope;
                Filter.OldGain = Gain;

                ComputeLowShelfEqualizerCoefficients(
                    ref Filter.iir,
                    Cutoff,
                    Slope,
                    Gain,
                    SamplingRate);
            }

            public void UpdateParams(
                ref FilterParams Params)
            {
                SetLowShelfEqualizerCoefficients(
                    this,
                    Params.Cutoff,
                    Params.BandwidthOrSlope,
                    Params.Gain,
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
                LowShelfEqualizerRec Filter = this;

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
