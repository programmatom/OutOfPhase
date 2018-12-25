/*
 *  Copyright © 1994-2002, 2015-2017 Thomas R. Lawrence
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
using System.Diagnostics;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        // This file is based on the following which I found on the web here:
        // http://www.musicdsp.org/showone.php?id=33 (or http://www.musicdsp.org/files/filters004.txt)
        // reproduced in it's entirity (as it was at the time) below:

        // // ----------------- file filterIIR00.c begin -----------------
        // /*
        // Resonant low pass filter source code.
        // By baltrax@hotmail.com (Zxform)
        // */
        //
        // #include <stdlib.h>
        // #include <stdio.h>
        // #include <math.h>
        //
        // /**************************************************************************
        //
        // FILTER.C - Source code for filter functions
        //
        //     iir_filter         IIR filter floats sample by sample (real time)
        //
        // *************************************************************************/
        //
        // /* FILTER INFORMATION STRUCTURE FOR FILTER ROUTINES */
        //
        // typedef struct {
        //     unsigned int length;       /* size of filter */
        //     float *history;            /* pointer to history in filter */
        //     float *coef;               /* pointer to coefficients of filter */
        // } FILTER;
        //
        // #define FILTER_SECTIONS   2   /* 2 filter sections for 24 db/oct filter */
        //
        // typedef struct {
        //         double a0, a1, a2;       /* numerator coefficients */
        //         double b0, b1, b2;       /* denominator coefficients */
        // } BIQUAD;
        //
        // BIQUAD ProtoCoef[FILTER_SECTIONS];      /* Filter prototype coefficients,
        //                                                      1 for each filter section */
        //
        // void szxform(
        //     double *a0, double *a1, double *a2,     /* numerator coefficients */
        //     double *b0, double *b1, double *b2,   /* denominator coefficients */
        //     double fc,           /* Filter cutoff frequency */
        //     double fs,           /* sampling rate */
        //     double *k,           /* overall gain factor */
        //     float *coef);         /* pointer to 4 iir coefficients */
        //
        // /*
        //  * --------------------------------------------------------------------
        //  *
        //  * iir_filter - Perform IIR filtering sample by sample on floats
        //  *
        //  * Implements cascaded direct form II second order sections.
        //  * Requires FILTER structure for history and coefficients.
        //  * The length in the filter structure specifies the number of sections.
        //  * The size of the history array is 2*iir.length.
        //  * The size of the coefficient array is 4*iir.length + 1 because
        //  * the first coefficient is the overall scale factor for the filter.
        //  * Returns one output sample for each input sample.  Allocates history
        //  * array if not previously allocated.
        //  *
        //  * float iir_filter(float input, FILTER *iir)
        //  *
        //  *     float input        new float input sample
        //  *     FILTER *iir        pointer to FILTER structure
        //  *
        //  * Returns float value giving the current output.
        //  *
        //  * Allocation errors cause an error message and a call to exit.
        //  * --------------------------------------------------------------------
        //  */
        // float iir_filter(input, iir)
        //     float input;        /* new input sample */
        //     FILTER *iir;        /* pointer to FILTER structure */
        // {
        //     unsigned int i;
        //     float *hist1_ptr, *hist2_ptr, *coef_ptr;
        //     float output, new_hist, history1, history2;
        //
        // /* allocate history array if different size than last call */
        //
        //     if(!iir.history) {
        //         iir.history = (float *) calloc(2*iir.length, sizeof(float));
        //         if(!iir.history) {
        //             printf("\nUnable to allocate history array in iir_filter\n");
        //             exit(1);
        //         }
        //     }
        //
        //     coef_ptr = iir.coef;                /* coefficient pointer */
        //
        //     hist1_ptr = iir.history;            /* first history */
        //     hist2_ptr = hist1_ptr + 1;           /* next history */
        //
        //         /* 1st number of coefficients array is overall input scale factor,
        //          * or filter gain */
        //     output = input * (*coef_ptr++);
        //
        //     for (i = 0 ; i < iir.length; i++)
        //         {
        //         history1 = *hist1_ptr;           /* history values */
        //         history2 = *hist2_ptr;
        //
        //         output = output - history1 * (*coef_ptr++);
        //         new_hist = output - history2 * (*coef_ptr++);    /* poles */
        //
        //         output = new_hist + history1 * (*coef_ptr++);
        //         output = output + history2 * (*coef_ptr++);      /* zeros */
        //
        //         *hist2_ptr++ = *hist1_ptr;
        //         *hist1_ptr++ = new_hist;
        //         hist1_ptr++;
        //         hist2_ptr++;
        //     }
        //
        //     return(output);
        // }
        //
        //
        // /*
        //  * --------------------------------------------------------------------
        //  *
        //  * main()
        //  *
        //  * Example main function to show how to update filter coefficients.
        //  * We create a 4th order filter (24 db/oct roloff), consisting
        //  * of two second order sections.
        //  * --------------------------------------------------------------------
        //  */
        // int main()
        // {
        //         FILTER   iir;
        //         float    *coef;
        //         double   fs, fc;     /* Sampling frequency, cutoff frequency */
        //         double   Q;     /* Resonance > 1.0 < 1000 */
        //         unsigned nInd;
        //         double   a0, a1, a2, b0, b1, b2;
        //         double   k;           /* overall gain factor */
        //
        // /*
        //  * Setup filter s-domain coefficients
        //  */
        //                  /* Section 1 */
        //         ProtoCoef[0].a0 = 1.0;
        //         ProtoCoef[0].a1 = 0;
        //         ProtoCoef[0].a2 = 0;
        //         ProtoCoef[0].b0 = 1.0;
        //         ProtoCoef[0].b1 = 0.765367;
        //         ProtoCoef[0].b2 = 1.0;
        //
        //                  /* Section 2 */
        //         ProtoCoef[1].a0 = 1.0;
        //         ProtoCoef[1].a1 = 0;
        //         ProtoCoef[1].a2 = 0;
        //         ProtoCoef[1].b0 = 1.0;
        //         ProtoCoef[1].b1 = 1.847759;
        //         ProtoCoef[1].b2 = 1.0;
        //
        //         iir.length = FILTER_SECTIONS;         /* Number of filter sections */
        //
        // /*
        //  * Allocate array of z-domain coefficients for each filter section
        //  * plus filter gain variable
        //  */
        //         iir.coef = (float *) calloc(4 * iir.length + 1, sizeof(float));
        //         if (!iir.coef)
        //         {
        //                  printf("Unable to allocate coef array, exiting\n");
        //                  exit(1);
        //         }
        //
        //         k = 1.0;          /* Set overall filter gain */
        //         coef = iir.coef + 1;     /* Skip k, or gain */
        //
        //         Q = 1;                         /* Resonance */
        //         fc = 5000;                  /* Filter cutoff (Hz) */
        //         fs = 44100;                      /* Sampling frequency (Hz) */
        //
        // /*
        //  * Compute z-domain coefficients for each biquad section
        //  * for new Cutoff Frequency and Resonance
        //  */
        //         for (nInd = 0; nInd < iir.length; nInd++)
        //         {
        //                  a0 = ProtoCoef[nInd].a0;
        //                  a1 = ProtoCoef[nInd].a1;
        //                  a2 = ProtoCoef[nInd].a2;
        //
        //                  b0 = ProtoCoef[nInd].b0;
        //                  b1 = ProtoCoef[nInd].b1 / Q;      /* Divide by resonance or Q */
        //                  b2 = ProtoCoef[nInd].b2;
        //                  szxform(&a0, &a1, &a2, &b0, &b1, &b2, fc, fs, &k, coef);
        //                  coef += 4;                       /* Point to next filter section */
        //         }
        //
        //         /* Update overall filter gain in coef array */
        //         iir.coef[0] = k;
        //
        //         /* Display filter coefficients */
        //         for (nInd = 0; nInd < (iir.length * 4 + 1); nInd++)
        //                  printf("C[%d] = %15.10f\n", nInd, iir.coef[nInd]);
        // /*
        //  * To process audio samples, call function iir_filter()
        //  * for each audio sample
        //  */
        //         return (0);
        // }
        //
        //
        //
        //
        // // ----------------- file filterIIR00.c end -----------------
        // >
        //
        // Reposting bilinear.c just in case the other one was not the latest version.
        //
        // // ----------------- file bilinear.c begin -----------------
        // /*
        //  * ----------------------------------------------------------
        //  *      bilinear.c
        //  *
        //  *      Perform bilinear transformation on s-domain coefficients
        //  *      of 2nd order biquad section.
        //  *      First design an analog filter and use s-domain coefficients
        //  *      as input to szxform() to convert them to z-domain.
        //  *
        //  * Here's the butterworth polinomials for 2nd, 4th and 6th order sections.
        //  *      When we construct a 24 db/oct filter, we take to 2nd order
        //  *      sections and compute the coefficients separately for each section.
        //  *
        //  *      n       Polinomials
        //  * --------------------------------------------------------------------
        //  *      2       s^2 + 1.4142s +1
        //  *      4       (s^2 + 0.765367s + 1) (s^2 + 1.847759s + 1)
        //  *      6       (s^2 + 0.5176387s + 1) (s^2 + 1.414214 + 1) (s^2 + 1.931852s + 1)
        //  *
        //  *      Where n is a filter order.
        //  *      For n=4, or two second order sections, we have following equasions for each
        //  *      2nd order stage:
        //  *
        //  *      (1 / (s^2 + (1/Q) * 0.765367s + 1)) * (1 / (s^2 + (1/Q) * 1.847759s + 1))
        //  *
        //  *      Where Q is filter quality factor in the range of
        //  *      1 to 1000. The overall filter Q is a product of all
        //  *      2nd order stages. For example, the 6th order filter
        //  *      (3 stages, or biquads) with individual Q of 2 will
        //  *      have filter Q = 2 * 2 * 2 = 8.
        //  *
        //  *      The nominator part is just 1.
        //  *      The denominator coefficients for stage 1 of filter are:
        //  *      b2 = 1; b1 = 0.765367; b0 = 1;
        //  *      numerator is
        //  *      a2 = 0; a1 = 0; a0 = 1;
        //  *
        //  *      The denominator coefficients for stage 1 of filter are:
        //  *      b2 = 1; b1 = 1.847759; b0 = 1;
        //  *      numerator is
        //  *      a2 = 0; a1 = 0; a0 = 1;
        //  *
        //  *      These coefficients are used directly by the szxform()
        //  *      and bilinear() functions. For all stages the numerator
        //  *      is the same and the only thing that is different between
        //  *      different stages is 1st order coefficient. The rest of
        //  *      coefficients are the same for any stage and equal to 1.
        //  *
        //  *      Any filter could be constructed using this approach.
        //  *
        //  *      References:
        //  *             Van Valkenburg, "Analog Filter Design"
        //  *             Oxford University Press 1982
        //  *             ISBN 0-19-510734-9
        //  *
        //  *             C Language Algorithms for Digital Signal Processing
        //  *             Paul Embree, Bruce Kimble
        //  *             Prentice Hall, 1991
        //  *             ISBN 0-13-133406-9
        //  *
        //  *             Digital Filter Designer's Handbook
        //  *             With C++ Algorithms
        //  *             Britton Rorabaugh
        //  *             McGraw Hill, 1997
        //  *             ISBN 0-07-053806-9
        //  * ----------------------------------------------------------
        //  */
        //
        // #include <math.h>
        //
        // void prewarp(double *a0, double *a1, double *a2, double fc, double fs);
        // void bilinear(
        //     double a0, double a1, double a2,    /* numerator coefficients */
        //     double b0, double b1, double b2,    /* denominator coefficients */
        //     double *k,                                   /* overall gain factor */
        //     double fs,                                   /* sampling rate */
        //     float *coef);                         /* pointer to 4 iir coefficients */
        //
        //
        // /*
        //  * ----------------------------------------------------------
        //  *      Pre-warp the coefficients of a numerator or denominator.
        //  *      Note that a0 is assumed to be 1, so there is no wrapping
        //  *      of it.
        //  * ----------------------------------------------------------
        //  */
        // void prewarp(
        //     double *a0, double *a1, double *a2,
        //     double fc, double fs)
        // {
        //     double wp, pi;
        //
        //     pi = 4.0 * atan(1.0);
        //     wp = 2.0 * fs * tan(pi * fc / fs);
        //
        //     *a2 = (*a2) / (wp * wp);
        //     *a1 = (*a1) / wp;
        // }
        //
        //
        // /*
        //  * ----------------------------------------------------------
        //  * bilinear()
        //  *
        //  * Transform the numerator and denominator coefficients
        //  * of s-domain biquad section into corresponding
        //  * z-domain coefficients.
        //  *
        //  *      Store the 4 IIR coefficients in array pointed by coef
        //  *      in following order:
        //  *             beta1, beta2    (denominator)
        //  *             alpha1, alpha2  (numerator)
        //  *
        //  * Arguments:
        //  *             a0-a2   - s-domain numerator coefficients
        //  *             b0-b2   - s-domain denominator coefficients
        //  *             k               - filter gain factor. initially set to 1
        //  *                                and modified by each biquad section in such
        //  *                                a way, as to make it the coefficient by
        //  *                                which to multiply the overall filter gain
        //  *                                in order to achieve a desired overall filter gain,
        //  *                                specified in initial value of k.
        //  *             fs             - sampling rate (Hz)
        //  *             coef    - array of z-domain coefficients to be filled in.
        //  *
        //  * Return:
        //  *             On return, set coef z-domain coefficients
        //  * ----------------------------------------------------------
        //  */
        // void bilinear(
        //     double a0, double a1, double a2,    /* numerator coefficients */
        //     double b0, double b1, double b2,    /* denominator coefficients */
        //     double *k,           /* overall gain factor */
        //     double fs,           /* sampling rate */
        //     float *coef         /* pointer to 4 iir coefficients */
        // )
        // {
        //     double ad, bd;
        //
        //                  /* alpha (Numerator in s-domain) */
        //     ad = 4. * a2 * fs * fs + 2. * a1 * fs + a0;
        //                  /* beta (Denominator in s-domain) */
        //     bd = 4. * b2 * fs * fs + 2. * b1* fs + b0;
        //
        //                  /* update gain constant for this section */
        //     *k *= ad/bd;
        //
        //                  /* Denominator */
        //     *coef++ = (2. * b0 - 8. * b2 * fs * fs)
        //                            / bd;         /* beta1 */
        //     *coef++ = (4. * b2 * fs * fs - 2. * b1 * fs + b0)
        //                            / bd; /* beta2 */
        //
        //                  /* Nominator */
        //     *coef++ = (2. * a0 - 8. * a2 * fs * fs)
        //                            / ad;         /* alpha1 */
        //     *coef = (4. * a2 * fs * fs - 2. * a1 * fs + a0)
        //                            / ad;   /* alpha2 */
        // }
        //
        //
        // /*
        //  * ----------------------------------------------------------
        //  * Transform from s to z domain using bilinear transform
        //  * with prewarp.
        //  *
        //  * Arguments:
        //  *      For argument description look at bilinear()
        //  *
        //  *      coef - pointer to array of floating point coefficients,
        //  *                     corresponding to output of bilinear transofrm
        //  *                     (z domain).
        //  *
        //  * Note: frequencies are in Hz.
        //  * ----------------------------------------------------------
        //  */
        // void szxform(
        //     double *a0, double *a1, double *a2, /* numerator coefficients */
        //     double *b0, double *b1, double *b2, /* denominator coefficients */
        //     double fc,         /* Filter cutoff frequency */
        //     double fs,         /* sampling rate */
        //     double *k,         /* overall gain factor */
        //     float *coef)         /* pointer to 4 iir coefficients */
        // {
        //                  /* Calculate a1 and a2 and overwrite the original values */
        //         prewarp(a0, a1, a2, fc, fs);
        //         prewarp(b0, b1, b2, fc, fs);
        //         bilinear(*a0, *a1, *a2, *b0, *b1, *b2, k, fs, coef);
        // }
        //
        //
        // // ----------------- file bilinear.c end -----------------
        //
        // And here is how it all works.
        //
        // // ----------------- file filter.txt begin -----------------
        // How to construct a kewl low pass resonant filter?
        //
        // Lets assume we want to create a filter for analog synth.
        // The filter rolloff is 24 db/oct, which corresponds to 4th
        // order filter. Filter of first order is equivalent to RC circuit
        // and has max rolloff of 6 db/oct.
        //
        // We will use classical Butterworth IIR filter design, as it
        // exactly corresponds to our requirements.
        //
        // A common practice is to chain several 2nd order sections,
        // or biquads, as they commonly called, in order to achive a higher
        // order filter. Each 2nd order section is a 2nd order filter, which
        // has 12 db/oct roloff. So, we need 2 of those sections in series.
        //
        // To compute those sections, we use standard Butterworth polinomials,
        // or so called s-domain representation and convert it into z-domain,
        // or digital domain. The reason we need to do this is because
        // the filter theory exists for analog filters for a long time
        // and there exist no theory of working in digital domain directly.
        // So the common practice is to take standard analog filter design
        // and use so called bilinear transform to convert the butterworth
        // equasion coefficients into z-domain.
        //
        // Once we compute the z-domain coefficients, we can use them in
        // a very simple transfer function, such as iir_filter() in our
        // C source code, in order to perform the filtering function.
        // The filter itself is the simpliest thing in the world.
        // The most complicated thing is computing the coefficients
        // for z-domain.
        //
        // Ok, lets look at butterworth polynomials, arranged as a series
        // of 2nd order sections:
        //
        //  * Note: n is filter order.
        //  *
        //  *      n       Polynomials
        //  *      --------------------------------------------------------------------
        //  *      2       s^2 + 1.4142s +1
        //  *      4       (s^2 + 0.765367s + 1) * (s^2 + 1.847759s + 1)
        //  *      6       (s^2 + 0.5176387s + 1) * (s^2 + 1.414214 + 1) * (s^2 + 1.931852s + 1)
        //  *
        //  * For n=4 we have following equasion for the filter transfer function:
        //  *
        //  *                     1                              1
        //  * T(s) = --------------------------- * ----------------------------
        //  *        s^2 + (1/Q) * 0.765367s + 1   s^2 + (1/Q) * 1.847759s + 1
        //  *
        //
        // The filter consists of two 2nd order secions since highest s power is 2.
        // Now we can take the coefficients, or the numbers by which s is multiplied
        // and plug them into a standard formula to be used by bilinear transform.
        //
        // Our standard form for each 2nd order secion is:
        //
        //        a2 * s^2 + a1 * s + a0
        // H(s) = ----------------------
        //        b2 * s^2 + b1 * s + b0
        //
        // Note that butterworth nominator is 1 for all filter sections,
        // which means s^2 = 0 and s^1 = 0
        //
        // Lets convert standard butterworth polinomials into this form:
        //
        //        0 + 0 + 1                  0 + 0 + 1
        // -------------------------- * --------------------------
        // 1 + ((1/Q) * 0.765367) + 1   1 + ((1/Q) * 1.847759) + 1
        //
        // Section 1:
        // a2 = 0; a1 = 0; a0 = 1;
        // b2 = 1; b1 = 0.5176387; b0 = 1;
        //
        // Section 2:
        // a2 = 0; a1 = 0; a0 = 1;
        // b2 = 1; b1 = 1.847759; b0 = 1;
        //
        // That Q is filter quality factor or resonance, in the range of
        // 1 to 1000. The overall filter Q is a product of all 2nd order stages.
        // For example, the 6th order filter (3 stages, or biquads)
        // with individual Q of 2 will have filter Q = 2 * 2 * 2 = 8.
        //
        // These a and b coefficients are used directly by the szxform()
        // and bilinear() functions.
        //
        // The transfer function for z-domain is:
        //
        //        1 + alpha1 * z^(-1) + alpha2 * z^(-2)
        // H(z) = -------------------------------------
        //        1 + beta1 * z^(-1) + beta2 * z^(-2)
        //
        // When you need to change the filter frequency cutoff or resonance,
        // or Q, you call the szxform() function with proper a and b
        // coefficients and the new filter cutoff frequency or resonance.
        // You also need to supply the sampling rate and filter gain you want
        // to achive. For our purposes the gain = 1.
        //
        // We call szxform() function 2 times becase we have 2 filter sections.
        // Each call provides different coefficients.
        //
        // The gain argument to szxform() is a pointer to desired filter
        // gain variable.
        //
        // double k = 1.0;      /* overall gain factor */
        //
        // Upon return from each call, the k argument will be set to a value,
        // by which to multiply our actual signal in order for the gain
        // to be one. On second call to szxform() we provide k that was
        // changed by the previous section. During actual audio filtering
        // function iir_filter() will use this k
        //
        // Summary:
        //
        // Our filter is pretty close to ideal in terms of all relevant
        // parameters and filter stability even with extremely large values
        // of resonance. This filter design has been verified under all
        // variations of parameters and it all appears to work as advertized.
        //
        // Good luck with it.
        // If you ever make a directX wrapper for it, post it to comp.dsp.
        //
        //
        //  *
        //  * ----------------------------------------------------------
        //  *References:
        //  *Van Valkenburg, "Analog Filter Design"
        //  *Oxford University Press 1982
        //  *ISBN 0-19-510734-9
        //  *
        //  *C Language Algorithms for Digital Signal Processing
        //  *Paul Embree, Bruce Kimble
        //  *Prentice Hall, 1991
        //  *ISBN 0-13-133406-9
        //  *
        //  *Digital Filter Designer's Handbook
        //  *With C++ Algorithms
        //  *Britton Rorabaugh
        //  *McGraw Hill, 1997
        //  *ISBN 0-07-053806-9
        //  * ----------------------------------------------------------
        //
        //
        //
        // // ----------------- file filter.txt end -----------------

        /* max number of sectios (max order is twice this) */
        private const int RLP2_MAXDEGREE = 3;

        public class ResonantLowpass2Rec : IFilter
        {
            /* filter state */
            public IIR2DirectIIRec iir0;
            public IIR2DirectIIRec iir1;
            public IIR2DirectIIRec iir2;

            /* filter parameters */
            public int Order; /* 2, 4, or 6 */

            // switch to enable old buggy behavior (see below)
            public bool broken;

            /* old parameters */
            public double OldCutoff = -1e300;
            public double OldQ = -1e300;


            public FilterTypes FilterType { get { return FilterTypes.eFilterResonantLowpass2; } }

            /* polynomial table */
            private static readonly double[][] IndexedB1Coef = new double[3][]
            {
                /* butterworth polynomial coefficients */

                // Order 2
                new double[1] { 1.4142 },
                // Order 4
                new double[2] { .765367, 1.847759 },
                // Order 6
                new double[3] { .5176387, 1.414214, 1.931852 },
            };

            public void Init(
                int Order,
                bool broken)
            {
                this.OldCutoff = -1e300;
                this.OldQ = -1e300;

                int Order2 = Order / 2;
#if DEBUG
                if ((Order2 < 1) || (Order2 > RLP2_MAXDEGREE) || (Order % 2 != 0))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                if (RLP2_MAXDEGREE != IndexedB1Coef.Length)
                {
                    // internal inconsistency
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                this.Order = Order;

                this.broken = broken;

                this.iir0.Y8 = 0;
                this.iir0.Y9 = 0;
                this.iir1.Y8 = 0;
                this.iir1.Y9 = 0;
                this.iir2.Y8 = 0;
                this.iir2.Y9 = 0;
            }

            private static void RLP2InitCoeffHelper(
                ref IIR2DirectIIRec Coeff,
                float A0,
                float A1,
                float A2,
                float B1,
                float B2)
            {
                Coeff.A0 = A0;
                Coeff.A1 = A1;
                Coeff.A2 = A2;
                Coeff.B1 = B1;
                Coeff.B2 = B2;
            }

            /* compute filter coefficients */
            private static void ComputeResonantLowpass2Coefficients(
                ref IIR2DirectIIRec Coeff0,
                ref IIR2DirectIIRec Coeff1,
                ref IIR2DirectIIRec Coeff2,
                int Order,
                double Cutoff,
                double Q,
                double SamplingRate)
            {
                int Order2 = Order / 2;
#if DEBUG
                if ((Order2 < 1) || (Order2 > RLP2_MAXDEGREE) || (Order % 2 != 0))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                if (RLP2_MAXDEGREE != IndexedB1Coef.Length)
                {
                    // internal inconsistency
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                /* initialize some constants */
                double TwoSamplingRate = 2 * SamplingRate;

                /* invariants */
                double WPInv = 1 / (TwoSamplingRate * Math.Tan(Math.PI * Cutoff / SamplingRate));
                double QInv = 1 / Q;

                /* prepare section coefficients */
                double[] B1Coef = IndexedB1Coef[Order2 - 1];
#if DEBUG
                Debug.Assert(B1Coef.Length == Order2);
#endif
                for (int i = 0; i < Order2; i++)
                {
                    double B1s = B1Coef[i] * WPInv * QInv;
                    double FourSamplingRateSquaredB2s = TwoSamplingRate * TwoSamplingRate * WPInv * WPInv;
                    double BetaI = 1 / (FourSamplingRateSquaredB2s + B1s * TwoSamplingRate + 1);

                    float A0 = (float)BetaI;
                    float A1 = 2;
                    float A2 = 1;
                    float B1 = (float)((2 - 2 * FourSamplingRateSquaredB2s) * BetaI);
                    float B2 = (float)((FourSamplingRateSquaredB2s - B1s * TwoSamplingRate + 1) * BetaI);

                    if (i == 0)
                    {
                        RLP2InitCoeffHelper(ref Coeff0, A0, A1, A2, B1, B2);
                    }
                    else if (i == 1)
                    {
                        RLP2InitCoeffHelper(ref Coeff1, A0, A1, A2, B1, B2);
                    }
                    else
                    {
#if DEBUG
                        Debug.Assert(i == 2);
#endif
                        RLP2InitCoeffHelper(ref Coeff2, A0, A1, A2, B1, B2);
                    }
                }
            }

            public static void SetResonantLowpass2Coefficients(
                ResonantLowpass2Rec Filter,
                double Cutoff,
                double Q,
                double SamplingRate)
            {
                Cutoff = Math.Max(1, Math.Min(SamplingRate / 2 - 1, Cutoff));
                if ((Cutoff == Filter.OldCutoff) && (Q == Filter.OldQ))
                {
                    return;
                }
                Filter.OldCutoff = Cutoff;
                Filter.OldQ = Q;

                ComputeResonantLowpass2Coefficients(
                    ref Filter.iir0,
                    ref Filter.iir1,
                    ref Filter.iir2,
                    Filter.Order,
                    Cutoff,
                    Q,
                    SamplingRate);
            }

            public void UpdateParams(
                ref FilterParams Params)
            {
                SetResonantLowpass2Coefficients(
                    this,
                    Params.Cutoff,
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
                ResonantLowpass2Rec Filter = this;

                // ScratchWorkspace1* is used in ApplyUnifiedFilterArray()
#if DEBUG
                Debug.Assert(!SynthParams.ScratchWorkspace2InUse);
                SynthParams.ScratchWorkspace2InUse = true;
#endif
                float[] ScratchVector = XinVector/*sic*/;
                int ScratchVectorOffset = SynthParams.ScratchWorkspace2LOffset;

                int Order2 = Filter.Order / 2;
                if (Order2 == 1)
                {
                    IIR2DirectIIMAcc(
                        ref Filter.iir0,
                        XinVector,
                        XinVectorOffset,
                        YoutVector,
                        YoutVectorOffset,
                        VectorLength,
                        OutputScaling);
                }
                else
                {
                    // In the old code there was a bad bug where the last biquad section was applied
                    // from the original input to the output, causing all the intermediate results
                    // from previous biquads to be disregarded. Naturally, this causes a very different
                    // sound, which has already been used. Therefore, there is an option to enable
                    // the old behavior.

                    if (!Filter.broken)
                    {
                        // The correct implementation

                        if (Order2 == 2)
                        {
                            /* first section enters local workspace */
                            IIR2DirectII(
                                ref Filter.iir0,
                                XinVector,
                                XinVectorOffset,
                                ScratchVector,
                                ScratchVectorOffset,
                                VectorLength);

                            /* final section goes into output */
                            IIR2DirectIIMAcc(
                                ref Filter.iir1,
                                ScratchVector, // NB: in defective version this was XinVector
                                ScratchVectorOffset,
                                YoutVector,
                                YoutVectorOffset,
                                VectorLength,
                                OutputScaling);
                        }
                        else
                        {
#if DEBUG
                            Debug.Assert(Order2 == 3);
#endif

                            /* first section enters local workspace */
                            IIR2DirectII(
                                ref Filter.iir0,
                                XinVector,
                                XinVectorOffset,
                                ScratchVector,
                                ScratchVectorOffset,
                                VectorLength);

                            /* intermediate sections */
                            IIR2DirectII(
                                ref Filter.iir1,
                                ScratchVector,
                                ScratchVectorOffset,
                                ScratchVector,
                                ScratchVectorOffset,
                                VectorLength);

                            /* final section goes into output */
                            IIR2DirectIIMAcc(
                                ref Filter.iir2,
                                ScratchVector, // NB: in defective version this was XinVector
                                ScratchVectorOffset,
                                YoutVector,
                                YoutVectorOffset,
                                VectorLength,
                                OutputScaling);
                        }
                    }
                    else
                    {
                        // The old broken buggy defective implementation

                        if (Order2 == 2)
                        {
                            /* final section goes into output */
                            IIR2DirectIIMAcc(
                                ref Filter.iir1,
                                XinVector, // this should have been Workspace
                                XinVectorOffset,
                                YoutVector,
                                YoutVectorOffset,
                                VectorLength,
                                OutputScaling);
                        }
                        else
                        {
#if DEBUG
                            Debug.Assert(Order2 == 3);
#endif

                            /* final section goes into output */
                            IIR2DirectIIMAcc(
                                ref Filter.iir2,
                                XinVector, // this should have been Workspace
                                XinVectorOffset,
                                YoutVector,
                                YoutVectorOffset,
                                VectorLength,
                                OutputScaling);
                        }
                    }
                }

#if DEBUG
                SynthParams.ScratchWorkspace2InUse = false;
#endif
            }
        }
    }
}
