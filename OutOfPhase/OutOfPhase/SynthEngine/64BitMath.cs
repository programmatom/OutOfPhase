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
        public struct Fixed64
        {
            public const long FIXED64_WHOLE = 0x0000000100000000;

            public long raw;

            public Fixed64(long raw)
            {
                this.raw = raw;
            }

            public Fixed64(double d)
            {
                raw = (long)(d * (double)FIXED64_WHOLE);
            }

            public Fixed64(float f)
            {
                raw = (long)(f * (float)FIXED64_WHOLE);
            }

            public static explicit operator Fixed64(double d)
            {
                return new Fixed64(d);
            }

            public double Double
            {
                get
                {
                    return raw / (double)FIXED64_WHOLE;
                }
            }

            public int Int
            {
                get
                {
                    return (int)(raw >> 32);
                }
            }

            public uint FracI
            {
                get
                {
                    return (uint)(raw /*& 0x00000000FFFFFFFF*/);
                }
            }

            public double FracD
            {
                get
                {
                    uint u = (uint)raw;
                    return u / (double)FIXED64_WHOLE;
                }
            }

            public float FracF
            {
                get
                {
                    uint u = (uint)raw;
                    return (float)u / (float)FIXED64_WHOLE;
                }
            }

            public static Fixed64 operator +(Fixed64 a, Fixed64 b)
            {
                return new Fixed64(a.raw + b.raw);
            }

            public static Fixed64 operator -(Fixed64 a, Fixed64 b)
            {
                return new Fixed64(a.raw - b.raw);
            }

            public static Fixed64 operator -(Fixed64 a)
            {
                return new Fixed64(-a.raw);
            }

            public void MaskInt64HighHalf(int mask)
            {
                raw = raw & (((long)mask << 32) | 0x00000000ffffffff);
            }

            public void SetInt64HighHalf(int h)
            {
                raw = (raw & 0x00000000ffffffff) | ((long)h << 32);
            }

            public static void Int32Int64Subtract(int i, ref  Fixed64 v)
            {
                v = new Fixed64(((long)i << 32) - v.raw);
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("{0} ({1:x16})", Double, raw);
            }
#endif
        }
    }
}
