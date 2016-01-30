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
        public interface IConvolution : IDisposable
        {
            void Apply(
                float[] Input,
                int InputOffset,
                float[] Output,
                int OutputOffset,
                int DataLength,
                float DirectGain,
                float ProcessedGain);
        }

        public static class ConvolveStreamFactory
        {
            public static SynthErrorCodes NewConvStream(
                float[] ImpulseResponse,
                int ImpulseResponseLength,
                bool LatencySpecified,
                int Latency,
                int lOversampling,
                SynthErrorInfoRec errorInfo,
                out IConvolution convolveStream)
            {
                convolveStream = null;
                if (LatencySpecified)
                {
                    if (!ConvolveStreamExplicitLatency.Available)
                    {
                        errorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExConvolverExplicitLatencyNotAvailable;
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                    convolveStream = ConvolveStreamExplicitLatency.NewConvStream(
                        ImpulseResponse,
                        ImpulseResponseLength,
                        Latency,
                        lOversampling);
                }
                else
                {
                    convolveStream = new ConvolveStreamSimple(
                        ImpulseResponse,
                        ImpulseResponseLength,
                        Latency,
                        lOversampling);
                }
                return SynthErrorCodes.eSynthDone;
            }
        }

        public class ConvolveStreamExplicitLatency : IConvolution
        {
#if ELCONV
#else
            public const bool Available = false;
#endif

#if ELCONV
#endif


            /* initialize convolver stream.  The impulse response should be sampled at */
            /* the nominal rate.  If oversampling is being employed, it will check SynthParams */
            /* and interpolate the impulse response to the real sampling rate. */
            public static ConvolveStreamExplicitLatency NewConvStream(
                float[] ImpulseResponse,
                int ImpulseResponseLength,
                int Latency,
                int lOversampling)
            {
#if ELCONV
#else
                return null;
#endif
            }

            /* apply the convolution, accumulating into output */
            public void Apply(
                float[] Input,
                int InputOffset,
                float[] Output,
                int OutputOffset,
                int DataLength,
                float DirectGain,
                float ProcessedGain)
            {
#if ELCONV
#endif
            }

            public void Dispose()
            {
#if ELCONV
#endif

                GC.SuppressFinalize(this);
            }

            ~ConvolveStreamExplicitLatency()
            {
#if DEBUG
                Debug.Assert(false, "ConvolveStreamExplicitLatency finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
                Dispose();
            }

#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif
        }

        public class ConvolveStreamSimple : IConvolution
        {
            public int blockLength;

            public int index;
            public AlignedWorkspace inBuf; /* input data stream, BlockLength*2, for overlap-save and delayline */
            public AlignedWorkspace outBuf; /* holds output, BlockLength*4+2 */

            public AlignedWorkspace impulseResponseFFT; /* fft of segment, BlockLength*4+2 */

            private FFT fft; // BlockLength*4+2


            /* initialize convolver stream.  The impulse response should be sampled at */
            /* the nominal rate.  If oversampling is being employed, it will check SynthParams */
            /* and interpolate the impulse response to the real sampling rate. */
            public ConvolveStreamSimple(
                float[] impulseResponse,
                int impulseResponseLength,
                int latency,
                int lOversampling)
            {
                if (lOversampling > 1)
                {
                    float[] impulseResponseConverted = RateConvert(
                        impulseResponse,
                        impulseResponseLength,
                        lOversampling);
                    impulseResponse = impulseResponseConverted;
                    impulseResponseLength *= lOversampling;
                }

                /* compute truncated latency (smallest block size we need to process) */
                Debug.Assert(latency >= impulseResponseLength);
                latency |= latency >> 1;
                latency |= latency >> 2;
                latency |= latency >> 4;
                latency |= latency >> 8;
                latency |= latency >> 16;

                blockLength = latency + 1;

                inBuf = new AlignedWorkspace(this.blockLength * 2);
                outBuf = new AlignedWorkspace(this.blockLength * 4 + 2/*for convlv*/);
                impulseResponseFFT = new AlignedWorkspace(this.blockLength * 4 + 2/*for convlv*/);

                fft = FFT.Create(this.blockLength * 4);

                /* transform impulse response section to frequency domain */
                FloatVectorCopyUnaligned(
                    impulseResponse, // not guarranteed to be vector-aligned
                    0,
                    fft.Base,
                    fft.Offset,
                    impulseResponseLength);
                fft.FFTfwd();
                FloatVectorCopy(
                    fft.Base,
                    fft.Offset,
                    impulseResponseFFT.Base,
                    impulseResponseFFT.Offset,
                    blockLength * 4 + 2);
            }

            /* band-limited upsampling for integer multiples of base sampling rate */
            public static float[] RateConvert(
                float[] data,
                int length,
                int factor)
            {
#if DEBUG
                if (factor < 2)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                int count = 1;
                int newLength;
                while ((newLength = (1 << count)) < length * factor + 1/*one extra at end*/)
                {
                    if (count == 31)
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    count += 1;
                }

                float[] newData;
                using (FFT fft = FFT.Create(newLength))
                {
                    float[] b = fft.Base;
                    int offset = fft.Offset;
                    for (int i = 0; i < length; i++)
                    {
                        b[offset + i * factor + (factor - 1)] = data[i];
                    }

                    fft.FFTfwd();
                    //b[offset + 1] = 0; /* kill top frequency bin */
                    /* kill all bins above original cutoff */
                    for (int i = ((newLength / factor) & ~1); i < newLength + 2; i += 2)
                    {
                        b[offset + i + 0] = 0;
                        b[offset + i + 1] = 0;
                    }
                    fft.FFTinv();

                    newData = new float[newLength];
                    for (int i = 0; i < newLength; i++)
                    {
                        newData[i] = b[offset + i] * fft.ScaleFactor;
                    }
                }

                return newData;
            }

            /* apply the convolution, accumulating into output */
            public void Apply(
                float[] input,
                int inputOffset,
                float[] output,
                int outputOffset,
                int count,
                float directGain,
                float processedGain)
            {
                for (int i = 0; i < count; i++)
                {
                    inBuf.Base[index + blockLength + inBuf.Offset] = input[i + inputOffset];
                    index++;
                    if (index == blockLength)
                    {
                        index = 0;

                        // prepare workspace with input buffer -- first half is data, second half+2 is zero
                        FloatVectorCopy(
                            inBuf.Base,
                            inBuf.Offset,
                            fft.Base,
                            fft.Offset,
                            blockLength * 2);
                        FloatVectorZero(
                            fft.Base,
                            fft.Offset + blockLength * 2,
                            blockLength * 2 + 2);

                        // frequency domain convolution
                        fft.FFTfwd();
                        FloatVectorMultiplyComplex(
                            impulseResponseFFT.Base,
                            impulseResponseFFT.Offset,
                            fft.Base,
                            fft.Offset,
                            fft.Base,
                            fft.Offset,
                            blockLength * 4 + 2);
                        fft.FFTinv();

                        FloatVectorScale(
                            fft.Base,
                            fft.Offset,
                            outBuf.Base,
                            outBuf.Offset,
                            blockLength * 4,
                            fft.ScaleFactor);

                        // shift input buffer by half; restart accumulate of next block into second half
                        FloatVectorCopy(
                            inBuf.Base,
                            blockLength + inBuf.Offset,
                            inBuf.Base,
                            0 + inBuf.Offset,
                            blockLength);
                    }

                    output[i + outputOffset] += outBuf.Base[index + blockLength + outBuf.Offset] * processedGain
                        + inBuf.Base[index + inBuf.Offset] * directGain;
                }
            }

            public void Dispose()
            {
                inBuf.Dispose();
                outBuf.Dispose();
                impulseResponseFFT.Dispose();
                fft.Dispose();

                GC.SuppressFinalize(this);
            }

            ~ConvolveStreamSimple()
            {
#if DEBUG
                Debug.Assert(false, "ConvolveStreamSimple finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
                Dispose();
            }

#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif
        }
    }
}
