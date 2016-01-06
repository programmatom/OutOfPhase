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
        // HPTRI was used as the default in the old application
        // the old C code awkwardly used #if statements to compile in one or the other

        public abstract class StereoDither
        {
            public abstract void DoStereo(
                int Length,
                float[] Data,
                int offset);

            public abstract void DoMono(
                int Length,
                float[] Data,
                int offset);
        }

        public class StereoDither_TPDF : StereoDither
        {
            private ChannelStateRec Left;
            private ChannelStateRec Right;

            private struct ChannelStateRec
            {
                public ParkAndMiller Seed1;
                public ParkAndMiller Seed2;
                public float Scale1;
                public float Scale2;
            }

            /* initialize the dither structure */
            public StereoDither_TPDF(
                NumBitsType Precision)
            {
                this.Left.Seed1 = new ParkAndMiller(1);
                this.Left.Seed2 = new ParkAndMiller(2);
                this.Right.Seed1 = new ParkAndMiller(3);
                this.Right.Seed2 = new ParkAndMiller(4);

                switch (Precision)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NumBitsType.eSample8bit:
                        this.Left.Scale1 = .5f / SampConv.FLOATFACTOR8BIT;
                        this.Left.Scale2 = .25f / SampConv.FLOATFACTOR8BIT;
                        break;
                    case NumBitsType.eSample16bit:
                        this.Left.Scale1 = .5f / SampConv.FLOATFACTOR16BIT;
                        this.Left.Scale2 = .25f / SampConv.FLOATFACTOR16BIT;
                        break;
                    case NumBitsType.eSample24bit:
                        // shouldn't bother dithering for 24 bits
                        Debug.Assert(false);
                        throw new ArgumentException();
                }
                this.Right.Scale1 = this.Left.Scale1;
                this.Right.Scale2 = this.Left.Scale2;
            }

            /* apply dither to stereo audio data */
            public override void DoStereo(
                int Length,
                float[] Data,
                int offset)
            {
                ChannelDither(
                    ref this.Left,
                    Length,
                    Data,
                    offset + 0,
                    2);

                ChannelDither(
                    ref this.Right,
                    Length,
                    Data,
                    offset + 1,
                    2);
            }

            /* apply dither to mono audio data */
            public override void DoMono(
                int Length,
                float[] Data,
                int offset)
            {
                ChannelDither(
                    ref this.Left,
                    Length,
                    Data,
                    offset,
                    1);
            }

            /* helper for one channel */
            private static void ChannelDither(
                ref ChannelStateRec State,
                int Length,
                float[] Data,
                int Offset,
                int Stride)
            {
                float Add = (-ParkAndMiller.Minimum) - ((ParkAndMiller.Maximum - ParkAndMiller.Minimum) / 2);
                float Mult = (float)1 / ((ParkAndMiller.Maximum - ParkAndMiller.Minimum) / 2);

                for (int i = 0; i < Length * Stride; i += Stride)
                {
                    float F1;
                    float F2;
                    int S;
                    float Y;
                    float X;

                    /* http://members.chello.nl/~m.heijligers/DAChtml/Digital%20Theory/Digital%20theory.html */
                    /* suggests adding 2 random variables, one at 0-mean uniform peak amplitude */
                    /* q/2, and an additional one with peak amplitude q/4.  Apparently, the limit */
                    /* of the infinite series would approach peak amplitude q, but only the first */
                    /* two variables are needed in practice for audio.  However, I'm not entirely */
                    /* sure I understood the paper, so this implementation may be totally bogus. */

                    X = Data[i + Offset];
                    S = State.Seed1.Random();
                    F1 = (S + Add) * Mult;
                    S = State.Seed2.Random();
                    F2 = (S + Add) * Mult;
                    Y = F1 * State.Scale1 + F2 * State.Scale2 + X;
                    Data[i + Offset] = Y;
                }
            }
        }

        public class StereoDither_HPTRI : StereoDither
        {
            private ChannelStateRec Left;
            private ChannelStateRec Right;

            private struct ChannelStateRec
            {
                public ParkAndMiller Seed;
                /* rectangular-PDF random numbers */
                public int r1;
                public int r2;
                /* error feedback buffers */
                public float s1;
                public float s2;
                /* word length (usually bits=16) -- pow(2.0,bits-1) */
                public float w;
            }

            /* initialize the dither structure */
            public StereoDither_HPTRI(
                NumBitsType Precision)
            {
                this.Left.Seed = new ParkAndMiller(1);
                this.Right.Seed = new ParkAndMiller(2);

                switch (Precision)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NumBitsType.eSample8bit:
                        this.Left.w = SampConv.FLOATFACTOR8BIT;
                        break;
                    case NumBitsType.eSample16bit:
                        this.Left.w = SampConv.FLOATFACTOR16BIT;
                        break;
                    case NumBitsType.eSample24bit:
                        // shouldn't bother dithering for 24 bits
                        Debug.Assert(false);
                        throw new ArgumentException();
                }
                this.Right.w = this.Left.w;
            }

            /* apply dither to stereo audio data */
            public override void DoStereo(
                int Length,
                float[] Data,
                int offset)
            {
                ChannelDither(
                    ref this.Left,
                    Length,
                    Data,
                    offset + 0,
                    2);

                ChannelDither(
                    ref this.Right,
                    Length,
                    Data,
                    offset + 1,
                    2);
            }

            /* apply dither to mono audio data */
            public override void DoMono(
                int Length,
                float[] Data,
                int offset)
            {
                ChannelDither(
                    ref this.Left,
                    Length,
                    Data,
                    offset,
                    1);
            }

            /* helper for one channel */
            private static void ChannelDither(
                ref ChannelStateRec State,
                int Length,
                float[] Data,
                int Offset,
                int Stride)
            {
                /* This is a simple implementation of highpass triangular-PDF dither with */
                /* 2nd-order noise shaping, for use when truncating floating point audio */
                /* data to fixed point. */
                /*  */
                /* The noise shaping lowers the noise floor by 11dB below 5kHz (@ 44100Hz */
                /* sample rate) compared to triangular-PDF dither. The code below assumes */
                /* input data is in the range +1 to -1 and doesn't check for overloads! */
                /*  */
                /* To save time when generating dither for multiple channels you can do */
                /* things like this:  r3=(r1 & 0x7F)<<8; instead of calling rand() again. */
                /*  */
                /* paul.kellett@maxim.abel.co.uk */
                /* http://www.maxim.abel.co.uk */

                float s = 0.5f; /* set to 0.0f for no noise shaping */
                float wi = 1.0f / State.w;
                float d = wi / (ParkAndMiller.Maximum - ParkAndMiller.Minimum); /* dither amplitude (2 lsb) */
                float o = wi * 0.5f; /* remove dc offset */
                float tmp;
                int _out;

                for (int i = 0; i < Length * Stride; i += Stride)
                {
                    float _in;

                    _in = Data[i + Offset];

                    State.r2 = State.r1; /* can make HP-TRI dither by */
                    State.r1 = State.Seed.Random() - ParkAndMiller.Minimum; /* subtracting previous rand() */

                    _in += s * (State.s1 + State.s1 - State.s2); /* error feedback */
                    tmp = _in + o + d * (float)(State.r1 - State.r2); /* dc offset and dither */

                    _out = (int)(State.w * tmp); /* truncate downwards */
                    if (tmp < 0)
                    {
                        _out -= 1; /* this is faster than floor() */
                    }

                    State.s2 = State.s1;
                    State.s1 = _in - wi * (float)_out; /* error */

                    Data[i + Offset] = _out * wi;
                }
            }
        }
    }
}
