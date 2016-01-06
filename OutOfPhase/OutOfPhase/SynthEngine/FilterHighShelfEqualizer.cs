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
        /* I found this information on some website somewhere */
        /*     */
        /* FILE: Audio-EQ-Cookbook.txt */
        /*     */
        /* Cookbook formulae for audio EQ biquad filter coefficients     */
        /* ---------------------------------------------------------     */
        /* by Robert Bristow-Johnson, pbjrbj@viconet.com  a.k.a. robert@audioheads.com     */
        /*      */
        /* All filter transfer functions were derived from analog prototypes (that      */
        /* are shown below for each EQ filter type) and had been digitized using the      */
        /* bilinear transform.  BLT frequency warping has been taken into account      */
        /* for both significant frequency relocation and for bandwidth readjustment.     */
        /*      */
        /* first, given a biquad transfer function defined as:     */
        /*      */
        /*             b0 + b1*z^-1 + b2*z^-2       */
        /*     H(z) = ------------------------     */
        /*             a0 + a1*z^-1 + a2*z^-2      */
        /*      */
        /* this shows 6 coefficients instead of 5 so, depending on your architechture,     */
        /* you will likely normalize a0 to be 1 and perhaps also b0 to 1 (and collect     */
        /* that into an overall gain coefficient).  then your transfer function would     */
        /* look like:     */
        /*      */
        /*             (b0/a0) + (b1/a0)*z^-1 + (b2/a0)*z^-2     */
        /*     H(z) = ---------------------------------------     */
        /*                1 + (a1/a0)*z^-1 + (a2/a0)*z^-2     */
        /*      */
        /* or     */
        /*      */
        /*                       1 + (b1/b0)*z^-1 + (b2/b0)*z^-2     */
        /*     H(z) = (b0/a0) * ---------------------------------     */
        /*                       1 + (a1/a0)*z^-1 + (a2/a0)*z^-2     */
        /*      */
        /*      */
        /* the most straight forward implementation would be the Direct I form (second equation):     */
        /*      */
        /*    y[n] = (b0/a0)*x[n] + (b1/a0)*x[n-1] + (b2/a0)*x[n-2] - (a1/a0)*y[n-1] - (a2/a0)*y[n-2]     */
        /*      */
        /* this is probably both the best and the easiest method to implement in the 56K.     */
        /*      */
        /*      */
        /*      */
        /* now, given:     */
        /*      */
        /*     frequency (wherever it's happenin', man.  "center" frequency or      */
        /*         "corner" (-3 dB) frequency, or shelf midpoint frequency,      */
        /*         depending on which filter type)     */
        /*          */
        /*     dBgain (used only for peaking and shelving filters)     */
        /*      */
        /*     bandwidth in octaves (between -3 dB frequencies for BPF and notch     */
        /*         or between midpoint (dBgain/2) gain frequencies for peaking )     */
        /*      */
        /*      _or_ Q (the EE kinda definition)     */
        /*      */
        /*      _or_ S, a "shelf slope" parameter.  when S = 1, the shelf slope is      */
        /*         as steep as you can get it and remain monotonically increasing      */
        /*         or decreasing gain with frequency.     */
        /*      */
        /*      */
        /*              */
        /* first compute a few intermediate variables:     */
        /*      */
        /*     A     = sqrt[ 10^(dBgain/20) ]   = 10^(dBgain/40)    (for for peaking and shelving EQ     */
        /* filters only)     */
        /*      */
        /*     omega = 2*PI*frequency/sample_rate     */
        /*          */
        /*     sn    = sin(omega)     */
        /*     cs    = cos(omega)     */
        /*      */
        /*     alpha = sn/(2*Q)     */
        /*           = sn*sinh[ ln(2)/2 * bandwidth * omega/sn ]     (if bandwidth is specified instead of     */
        /* Q)     */
        /*      */
        /*     beta  = sqrt[ (A^2 + 1)/S - (A-1)^2 ]   (for shelving EQ filters only)     */
        /*      */
        /*      */
        /* then compute the coefs for whichever filter type you want:     */
        /*      */
        /*      */
        /*   the analog prototypes are shown for normalized frequency.     */
        /*   the bilinear transform substitutes     */
        /*        */
        /*                 1          1 - z^-1     */
        /*   s  <-  -------------- * ----------     */
        /*           tan(omega/2)     1 + z^-1     */
        /*      */
        /*      */
        /*      */
        /* LPF:            H(s) = 1 / (s^2 + s/Q + 1)     */
        /*      */
        /*                 b0 =  (1 - cs)/2     */
        /*                 b1 =   1 - cs     */
        /*                 b2 =  (1 - cs)/2     */
        /*                 a0 =   1 + alpha     */
        /*                 a1 =  -2*cs     */
        /*                 a2 =   1 - alpha     */
        /*      */
        /*      */
        /*      */
        /* HPF:            H(s) = s^2 / (s^2 + s/Q + 1)     */
        /*      */
        /*                 b0 =  (1 + cs)/2     */
        /*                 b1 = -(1 + cs)     */
        /*                 b2 =  (1 + cs)/2     */
        /*                 a0 =   1 + alpha     */
        /*                 a1 =  -2*cs     */
        /*                 a2 =   1 - alpha     */
        /*      */
        /*      */
        /*      */
        /* BPF:            H(s) = (s/Q) / (s^2 + s/Q + 1)     */
        /*      */
        /*                 b0 =   alpha     */
        /*                 b1 =   0     */
        /*                 b2 =  -alpha     */
        /*                 a0 =   1 + alpha     */
        /*                 a1 =  -2*cs     */
        /*                 a2 =   1 - alpha     */
        /*      */
        /*      */
        /*      */
        /* notch:          H(s) = (s^2 + 1) / (s^2 + s/Q + 1)     */
        /*      */
        /*                 b0 =   1     */
        /*                 b1 =  -2*cs     */
        /*                 b2 =   1     */
        /*                 a0 =   1 + alpha     */
        /*                 a1 =  -2*cs     */
        /*                 a2 =   1 - alpha     */
        /*      */
        /*      */
        /*      */
        /* peakingEQ:      H(s) = (s^2 + s*(A/Q) + 1) / (s^2 + s/(A*Q) + 1)     */
        /*      */
        /*                 b0 =   1 + alpha*A     */
        /*                 b1 =  -2*cs     */
        /*                 b2 =   1 - alpha*A     */
        /*                 a0 =   1 + alpha/A     */
        /*                 a1 =  -2*cs     */
        /*                 a2 =   1 - alpha/A     */
        /*      */
        /*      */
        /*      */
        /* lowShelf:       H(s) = A * (A + beta*s + s^2) / (1 + beta*s + A*s^2)     */
        /*      */
        /*                 b0 =    A*[ (A+1) - (A-1)*cs + beta*sn ]     */
        /*                 b1 =  2*A*[ (A-1) - (A+1)*cs           ]     */
        /*                 b2 =    A*[ (A+1) - (A-1)*cs - beta*sn ]     */
        /*                 a0 =        (A+1) + (A-1)*cs + beta*sn     */
        /*                 a1 =   -2*[ (A-1) + (A+1)*cs           ]     */
        /*                 a2 =        (A+1) + (A-1)*cs - beta*sn     */
        /*      */
        /*      */
        /*      */
        /* highShelf:      H(s) = A * (1 + beta*s + A*s^2) / (A + beta*s + s^2)     */
        /*      */
        /*                 b0 =    A*[ (A+1) + (A-1)*cs + beta*sn ]     */
        /*                 b1 = -2*A*[ (A-1) + (A+1)*cs           ]     */
        /*                 b2 =    A*[ (A+1) + (A-1)*cs - beta*sn ]     */
        /*                 a0 =        (A+1) - (A-1)*cs + beta*sn     */
        /*                 a1 =    2*[ (A-1) - (A+1)*cs           ]     */
        /*                 a2 =        (A+1) - (A-1)*cs - beta*sn     */
        /*      */
        /*      */

        public class HighShelfEqualizerRec : IFilter
        {
            /* filter state */
            public IIR2DirectIRec iir;

            /* old stuff */
            public double OldCutoff = -1e300;
            public double OldSlope = -1e300;
            public double OldGain = -1e300;


            public FilterTypes FilterType { get { return FilterTypes.eFilterHighShelfEQ; } }

            /* compute filter coefficients */
            private static void ComputeHighShelfEqualizerCoefficients(
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

                OneOverB0 = 1 / ((A + 1) - (A - 1) * CS + Beta * SN);
                Coeff.A0 = (float)(OneOverB0 * A * ((A + 1) + (A - 1) * CS + Beta * SN));
                Coeff.A1 = (float)(OneOverB0 * -2 * A * ((A - 1) + (A + 1) * CS));
                Coeff.A2 = (float)(OneOverB0 * A * ((A + 1) + (A - 1) * CS - Beta * SN));
                Coeff.B1 = (float)(OneOverB0 * 2 * ((A - 1) - (A + 1) * CS));
                Coeff.B2 = (float)(OneOverB0 * ((A + 1) - (A - 1) * CS - Beta * SN));
            }

            /* adjust filter coefficients */
            public static void SetHighShelfEqualizerCoefficients(
                HighShelfEqualizerRec Filter,
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

                ComputeHighShelfEqualizerCoefficients(
                    ref Filter.iir,
                    Cutoff,
                    Slope,
                    Gain,
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
                HighShelfEqualizerRec Filter = this;

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
