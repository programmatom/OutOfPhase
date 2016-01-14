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
#if VECTOR
using System.Numerics;
#endif

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        // TODO: this ought to use Convolver FFT implementation instead
        // At least as an option:
        // Advantage of FFT: probably faster
        // Advantage of dot product loop: consistent timing each cycle would be more stable for realtime

        public class IdealLPRec : ITrackEffect, IOscillatorEffect
        {
            public int Order;

            public int OneChannelLength;
            public AlignedWorkspace Coefficients;
            public AlignedWorkspace LeftState;
            public AlignedWorkspace RightState;
            public int LeftIntState;
            public int RightIntState;

            /* flag indicating filter is enabled (false if cutoff > nyquist). */
            public bool FilterEnabled;

            /* filter cutoff (in hertz) */
            public double Cutoff;


            /* convolution of prepared impulse with signal.  */
            /*  Order = number of points in both the impulse and the statespace */
            /*  Impulse = prepared impulse.  length is Order.  impulse should be padded at end */
            /*    with zeroes until physical number of bytes is multiple of CONVOLVE_IMPULSEPAD. */
            /*  FloatState = state buffer (init to zero before first call, do not init again) */
            /*    FloatStateLength must be smallest integer power of two >= (Order + CONVOLVE_BLOCKINGFACTOR) */
            /*  IntState = integer state (init to zero before first call, do not init again) */
            /*  DataLength = number of points in the input and output buffers */
            /*  Input = input signal */
            /*  Output = place to store output signal, may be the same pointer as input */
            private static void Convolution(
                int Order,
                float[] Impulse,
                int ImpulseOffset,
                float[] FloatState,
                int FloatStateOffset,
                int FloatStateLength,
                ref int IntState,
                int DataLength,
                float[] Input,
                int InputOffset,
                float[] Output,
                int OutputOffset)
            {
                int StateBase;
                int StateMask;

                Debug.Assert((FloatStateLength & (FloatStateLength - 1)) == 0);

                StateBase = IntState;
                StateMask = FloatStateLength - 1;

#if DEBUG
                AssertVectorAligned(FloatState, FloatStateOffset);
#endif
                for (int c = 0; c < DataLength; c++)
                {
                    float Acc = 0;

                    int p = (StateBase + Order - 1) & StateMask;
                    FloatState[p + FloatStateOffset] = Input[c + InputOffset];
#if VECTOR
                    if (EnableVector)
                    {
                        if (p < Vector<float>.Count - 1)
                        {
                            FloatState[p + FloatStateLength + FloatStateOffset] = Input[c + InputOffset];
                        }
                    }
#endif

                    int i = 0;

#if VECTOR
                    if (EnableVector)
                    {
                        for (; i <= Order - Vector<float>.Count; i += Vector<float>.Count)
                        {
                            Acc += Vector.Dot(new Vector<float>(Impulse, i + ImpulseOffset), // aligned
                                new Vector<float>(FloatState, ((StateBase + i) & StateMask) + FloatStateOffset)); // unaligned
                        }
                    }
#endif

                    for (; i < Order; i++)
                    {
                        Acc += Impulse[i + ImpulseOffset] * FloatState[((StateBase + i) & StateMask) + FloatStateOffset];
                    }

                    Output[c + OutputOffset] = Acc;

                    StateBase = (StateBase + 1) & StateMask;
                }

                IntState = StateBase;
            }

            /* fill in coefficient table for an ideal lowpass filter. */
            /* returns True if table is good.  if cutoff is out of range, then it returns False. */
            private static bool GenerateIdealLowPassImpulse(
                float[] Table,
                int TableOffset,
                int Order,
                double Cutoff,
                double SamplingRate)
            {
                int AdjustedOrder;
                double Period;
                double Acc;
                float UnityScaling;
                bool IsFilterNeeded = true;

#if DEBUG
                if ((Order < 1) || ((Order % 2) == 0))
                {
                    // order must be positive odd integer
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                if (Cutoff < 0)
                {
                    Cutoff = 0;
                }
                if (Cutoff >= SamplingRate / 2)
                {
                    Cutoff = SamplingRate / 2;
                    IsFilterNeeded = false;
                }

                AdjustedOrder = (Order - 1) / 2;
                Period = (Cutoff / SamplingRate) * 2 * Math.PI;

                /* generate left half of impulse response */
                Acc = 0;
                for (int i = 0; i < AdjustedOrder; i += 1)
                {
                    double Value;
                    double TriangleWindow;

                    /* generate ideal lowpass impulse response */
#if DEBUG
                    if (AdjustedOrder - i == 0)
                    {
                        // Internal error divide by zero
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
#endif
                    // "Sinc"
                    Value = Math.Sin((double)(AdjustedOrder - i) * Period) / ((double)(AdjustedOrder - i) * Period);

                    /* apply triangle window */
                    TriangleWindow = (double)(i + 1) / (double)(AdjustedOrder + 1);
#if DEBUG
                    if (TriangleWindow <= 0)
                    {
                        // window should be greater than zero
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
#endif
                    Value = Value * TriangleWindow;

                    /* set values for symmetric left and right halves */
                    Table[i + TableOffset] = (float)Value;
                    Table[Order - 1 - i + TableOffset] = (float)Value;
                    Acc = Acc + Value + Value; /* twice, since it's symmetric */
                }
                /* center is always 1 */
                Table[AdjustedOrder + TableOffset] = 1;
                Acc += 1;

                /* scale the vector */
                UnityScaling = (float)(1 / Acc);
                for (int i = 0; i < Order; i += 1)
                {
                    Table[i + TableOffset] *= UnityScaling;
                }

                /* return True if filter needed, or False if cutoff exceeds sampling rate, so */
                /* filter is not needed.  We still fill in the table though. */
                return IsFilterNeeded;
            }

            /* create a new ideal lowpass processor */
            public static IdealLPRec NewIdealLP(
                IdealLPSpecRec Template,
                SynthParamRec SynthParams)
            {
                IdealLPRec IdealLP = new IdealLPRec();

                IdealLP.Cutoff = IdealLowpassSpecGetCutoff(Template);
                IdealLP.Order = IdealLowpassSpecGetOrder(Template);
#if DEBUG
                if ((IdealLP.Order < 1) || ((IdealLP.Order % 2) == 0))
                {
                    // order must be positive odd integer
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
                IdealLP.Order &= 0x1fffffff; /* some sanity -- will result in out of mem instead of gpf */

                IdealLP.OneChannelLength = 1;
                while (IdealLP.OneChannelLength < IdealLP.Order)
                {
                    IdealLP.OneChannelLength = IdealLP.OneChannelLength << 1;
                }

                IdealLP.Coefficients = new AlignedWorkspace(IdealLP.Order);

                int vectorPadding = 0;
#if VECTOR
                if (EnableVector)
                {
                    vectorPadding = Vector<float>.Count - 1;
                }
#endif
                IdealLP.LeftState = new AlignedWorkspace(IdealLP.OneChannelLength + vectorPadding);
                IdealLP.RightState = new AlignedWorkspace(IdealLP.OneChannelLength + vectorPadding);

                IdealLP.FilterEnabled = GenerateIdealLowPassImpulse(
                    IdealLP.Coefficients.Base,
                    IdealLP.Coefficients.Offset,
                    IdealLP.Order,
                    IdealLP.Cutoff,
                    SynthParams.dSamplingRate);
                if (SynthParams.iSamplingRate < GetIdealLowpassMinSamplingRate(Template))
                {
                    IdealLP.FilterEnabled = false;
                }

                return IdealLP;
            }

            public SynthErrorCodes TrackUpdateState(ref AccentRec Accents, SynthParamRec SynthParams)
            {
                return SynthErrorCodes.eSynthDone;
            }

            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                Coefficients.Dispose();
                LeftState.Dispose();
                RightState.Dispose();
            }

            public void OscFixEnvelopeOrigins(int ActualPreOriginTime)
            {
            }

            public SynthErrorCodes OscUpdateEnvelopes(double OscillatorFrequency, SynthParamRec SynthParams)
            {
                return SynthErrorCodes.eSynthDone;
            }

            public void OscKeyUpSustain1()
            {
            }

            public void OscKeyUpSustain2()
            {
            }

            public void OscKeyUpSustain3()
            {
            }

            public void OscRetriggerEnvelopes(ref AccentRec NewAccents, double NewHurryUp, double NewInitialFrequency, bool ActuallyRetrigger, SynthParamRec SynthParams)
            {
            }

            /* apply ideal lowpass to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                /* skip this if we don't need to do anything */
                if (!this.FilterEnabled)
                {
                    return SynthErrorCodes.eSynthDone;
                }

                /* left channel */
                Convolution(
                    this.Order,
                    this.Coefficients.Base,
                    this.Coefficients.Offset,
                    this.LeftState.Base,
                    this.LeftState.Offset,
                    this.OneChannelLength,
                    ref this.LeftIntState,
                    nActualFrames,
                    workspace,
                    lOffset,
                    workspace,
                    lOffset);

                /* right channel */
                Convolution(
                    this.Order,
                    this.Coefficients.Base,
                    this.Coefficients.Offset,
                    this.RightState.Base,
                    this.RightState.Offset,
                    this.OneChannelLength,
                    ref this.RightIntState,
                    nActualFrames,
                    workspace,
                    rOffset,
                    workspace,
                    rOffset);

                return SynthErrorCodes.eSynthDone;
            }
        }
    }
}
