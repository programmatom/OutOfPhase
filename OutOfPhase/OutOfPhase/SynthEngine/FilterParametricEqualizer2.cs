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
        /* From robert@audioheads.win.net Fri Feb  3 10:09:33 MET 1995 */
        /* Article: 15901 of comp.dsp */
        /* Newsgroups: comp.dsp */
        /* Message-ID: <198@audioheads.win.net> */
        /* References: <199501261313.AA15062@ztivax.zfe.siemens.de> */
        /* Reply-To: robert@audioheads.win.net (robert bristow-johnson) */
        /* From: robert@audioheads.win.net (robert bristow-johnson) */
        /* Date: Tue, 31 Jan 1995 18:04:05 GMT */
        /* Subject: Re: good full parametric EQ algorithm ? */
        /* Lines: 49 */
        /*  */
        /*  */
        /* In article <199501261313.AA15062@ztivax.zfe.siemens.de>, VISX80::GRECNER (GRECNER%VISX80.decnet@musx53.zfe.siemens.de) writes: */
        /*  */
        /* >I'm looking for some info on how to implement full parametric equalizer */
        /* >in digital domain. Could someone recommend a book on this topic with */
        /* >practical applications and examples ? */
        /*  */
        /* Quite a few people, myself included, have done AES papers (both */
        /* journal and convention) regarding IIR filters for use in audio.  */
        /* Some papers had to do with topologies and noise performance, */
        /* others had to do with coefficient calculation. */
        /*  */
        /* 1993  Wilson, Rhonda. 'Filter Topologies', JAES vol.41, no.9, p.667 */
        /* 1993  Massie, Dana. 'An Eng. Study ... Normalized Ladder..', JAES vol.41, no.7/8, p.564 */
        /* 1988  Dattorro, Jon. 'Dig. Filters for High Fidelity ..' JAES vol.36, no. 11, p.851 */
        /* 1986  White, Stanley. 'Design of Biquad Peaking...', JAES vol.34, no.6, p.479  */
        /* 1983  Moorer, Andy. 'Manifold Joys of Conformal Mapping...', JAES vol.31, no.11, p.826  */
        /*  */
        /* I did a preprint at last November's AES convention that tried to */
        /* boil down and settle the coeffiecient issue, for the case of a */
        /* biquad IIR or normalized ladder, to some simple and compact  */
        /* equations that nobody could argue with: */
        /*  */
        /* Given 4 parameters:   omega = normalized boost/cut radian frequency (Nyquist = pi) */
        /*                       K = linear boost or cut gain = 10^(dB boost/20) */
        /*                       F = your defined bandedge gain (linear) {K<F<1 or 1<=F<=K} */
        /*                       bw = bandwidth in octaves */
        /*                    or BW = bandwidth in normalized radian frequency */
        /*  */
        /*  */
        /*                 b0*z*z + b1*z + b2 */
        /*         H(z) = -------------------- */
        /*                   z*z + a1*z + a2 */
        /*  */
        /* The coefficients are: */
        /*  */
        /*         b0 = (1 + gamma*sqrt(K))/(1 + gamma/sqrt(K)) */
        /*         b1 = (-2*cos(omega))/(1 + gamma/sqrt(K)) */
        /*         b2 = (1 - gamma*sqrt(K))/(1 + gamma/sqrt(K)) */
        /*         a1 = b1 */
        /*         a2 = (1 - gamma/sqrt(K))/(1 + gamma/sqrt(K)) */
        /*  */
        /* where */
        /*         gamma = sqrt[K*(F*F-1)/(K*K-F*F)]*tan(BW/2) */
        /* or */
        /*         gamma = sqrt[K*(F*F-1)/(K*K-F*F)]*sinh[(ln(2)/2)*bw*(omega/sin(omega))]*sin(omega) */
        /*  */
        /*  */
        /* The gamma is exact for the case where bandwidth is in normalized */
        /* frequency and a quite accurate approximation for the case */
        /* of bandwidth in octaves ( it gets a little less accurate */
        /* for large bw > 2 octaves at large omega > Nyquist/2 ). */
        /*  */
        /* A handy setting for F is sqrt(K) which sets the bandedge gain at */
        /* the midpoint from 0 dB to the dB of boost or cut.  This makes the */
        /* messy sqrt[] stuff in gamma equal to one. */
        /*  */
        /* If you want, you can get a copy of my preprint from AES for US$5  */
        /* (I think that's what they charge).  It's preprint no. 3906. */

        public class ParametricEqualizer2Rec : IFilter
        {
            /* filter state */
            public IIR2DirectIRec iir;

            /* old stuff */
            public double OldCutoff = -1e300;
            public double OldBandwidth = -1e300;
            public double OldGain = -1e300;


            public FilterTypes FilterType { get { return FilterTypes.eFilterParametricEQ2; } }

            /* compute filter coefficients */
            private static void ComputeParametricEqualizer2Coefficients(
                ref IIR2DirectIRec Coeff,
                double Cutoff,
                double Bandwidth,
                double Gain,
                double SamplingRate)
            {
                double Gamma;
                double SqrtK;
                double HalfSamplingRate;
                double OneOverHalfSamplingRateTimesPi;
                double OneOverBigGammaSqrtKThing;
                double CutoffCos;

                HalfSamplingRate = SamplingRate * 0.5;
                OneOverHalfSamplingRateTimesPi = Math.PI / HalfSamplingRate;

                if (Bandwidth < FILTER_FREQ_EPSILON)
                {
                    Bandwidth = FILTER_FREQ_EPSILON;
                }
                if (Bandwidth > HalfSamplingRate - FILTER_FREQ_EPSILON)
                {
                    Bandwidth = HalfSamplingRate - FILTER_FREQ_EPSILON;
                }
                if (Cutoff < FILTER_FREQ_EPSILON)
                {
                    Cutoff = FILTER_FREQ_EPSILON;
                }
                if (Cutoff > HalfSamplingRate - FILTER_FREQ_EPSILON)
                {
                    Cutoff = HalfSamplingRate - FILTER_FREQ_EPSILON;
                }

                Cutoff = Cutoff * OneOverHalfSamplingRateTimesPi;
                Bandwidth = Bandwidth * OneOverHalfSamplingRateTimesPi * 0.5;

                Gamma = Math.Tan(Bandwidth);
                SqrtK = Math.Sqrt(Gain);
                CutoffCos = Math.Cos(Cutoff);

                OneOverBigGammaSqrtKThing = 1 / (1 + Gamma / SqrtK);

                Coeff.A1 = (float)((-2 * CutoffCos) * OneOverBigGammaSqrtKThing);
                Coeff.A0 = (float)((1 + Gamma * SqrtK) * OneOverBigGammaSqrtKThing);
                Coeff.A2 = (float)((1 - Gamma * SqrtK) * OneOverBigGammaSqrtKThing);
                Coeff.B1 = Coeff.A1;
                Coeff.B2 = (float)((1 - Gamma / SqrtK) * OneOverBigGammaSqrtKThing);
            }

            /* adjust filter coefficients */
            public static void SetParametricEqualizer2Coefficients(
                ParametricEqualizer2Rec Filter,
                double Cutoff,
                double Bandwidth,
                double Gain,
                double SamplingRate)
            {
                if ((Cutoff == Filter.OldCutoff) && (Bandwidth == Filter.OldBandwidth) && (Gain == Filter.OldGain))
                {
                    return;
                }
                Filter.OldCutoff = Cutoff;
                Filter.OldBandwidth = Bandwidth;
                Filter.OldGain = Gain;

                ComputeParametricEqualizer2Coefficients(
                    ref Filter.iir,
                    Cutoff,
                    Bandwidth,
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
                ParametricEqualizer2Rec Filter = this;

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
