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
using System.Runtime.InteropServices;
using System.Text;

namespace OutOfPhase
{
    // These are actually safe as long as sizeof(int)==sizeof(float) are the same size. These casts
    // reinterpret the bit pattern rather than coercing.

    [StructLayout(LayoutKind.Explicit)]
    public struct UnsafeArrayCast
    {
        [FieldOffset(0)]
        public float[] floats;
        [FieldOffset(0)]
        public int[] ints;
        [FieldOffset(0)]
        public uint[] uints;

        public static int[] AsInts(float[] a)
        {
            Debug.Assert(sizeof(int) == sizeof(float));
            UnsafeArrayCast cast = new UnsafeArrayCast();
            cast.floats = a;
            return cast.ints;
        }

        public static uint[] AsUints(float[] a)
        {
            Debug.Assert(sizeof(uint) == sizeof(float));
            UnsafeArrayCast cast = new UnsafeArrayCast();
            cast.floats = a;
            return cast.uints;
        }

        public static float[] AsFloats(int[] a)
        {
            Debug.Assert(sizeof(int) == sizeof(float));
            UnsafeArrayCast cast = new UnsafeArrayCast();
            cast.ints = a;
            return cast.floats;
        }

        public static float[] AsFloats(uint[] a)
        {
            Debug.Assert(sizeof(uint) == sizeof(float));
            UnsafeArrayCast cast = new UnsafeArrayCast();
            cast.uints = a;
            return cast.floats;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UnsafeScalarCast
    {
        [FieldOffset(0)]
        public float _float;
        [FieldOffset(0)]
        public int _int;
        [FieldOffset(0)]
        public uint _uint;

        public static int AsInt(float a)
        {
            Debug.Assert(sizeof(int) == sizeof(float));
            UnsafeScalarCast cast = new UnsafeScalarCast();
            cast._float = a;
            return cast._int;
        }

        public static uint AsUint(float a)
        {
            Debug.Assert(sizeof(uint) == sizeof(float));
            UnsafeScalarCast cast = new UnsafeScalarCast();
            cast._float = a;
            return cast._uint;
        }

        public static float AsFloat(int a)
        {
            Debug.Assert(sizeof(int) == sizeof(float));
            UnsafeScalarCast cast = new UnsafeScalarCast();
            cast._int = a;
            return cast._float;
        }

        public static float AsFloat(uint a)
        {
            Debug.Assert(sizeof(uint) == sizeof(float));
            UnsafeScalarCast cast = new UnsafeScalarCast();
            cast._uint = a;
            return cast._float;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UnsafeArrayCastLong
    {
        [FieldOffset(0)]
        public double[] doubles;
        [FieldOffset(0)]
        public long[] longs;
        [FieldOffset(0)]
        public ulong[] ulongs;
        [FieldOffset(0)]
        public Synthesizer.Fixed64[] fixed64s;

        public static long[] AsLongs(double[] a)
        {
            Debug.Assert(sizeof(long) == sizeof(double));
            UnsafeArrayCastLong cast = new UnsafeArrayCastLong();
            cast.doubles = a;
            return cast.longs;
        }

        public static ulong[] AsUlongs(double[] a)
        {
            Debug.Assert(sizeof(ulong) == sizeof(double));
            UnsafeArrayCastLong cast = new UnsafeArrayCastLong();
            cast.doubles = a;
            return cast.ulongs;
        }

        public static double[] AsDoubles(long[] a)
        {
            Debug.Assert(sizeof(long) == sizeof(double));
            UnsafeArrayCastLong cast = new UnsafeArrayCastLong();
            cast.longs = a;
            return cast.doubles;
        }

        public static double[] AsDoubles(ulong[] a)
        {
            Debug.Assert(sizeof(ulong) == sizeof(double));
            UnsafeArrayCastLong cast = new UnsafeArrayCastLong();
            cast.ulongs = a;
            return cast.doubles;
        }

        public static double[] AsDoubles(Synthesizer.Fixed64[] a)
        {
            //Debug.Assert(sizeof(Synthesizer.Fixed64) == sizeof(double));
            UnsafeArrayCastLong cast = new UnsafeArrayCastLong();
            cast.fixed64s = a;
            return cast.doubles;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UnsafeScalarCastLong
    {
        [FieldOffset(0)]
        public double _double;
        [FieldOffset(0)]
        public long _long;
        [FieldOffset(0)]
        public ulong _ulong;

        public static long AsLong(double a)
        {
            Debug.Assert(sizeof(long) == sizeof(double));
            UnsafeScalarCastLong cast = new UnsafeScalarCastLong();
            cast._double = a;
            return cast._long;
        }

        public static ulong AsULong(double a)
        {
            Debug.Assert(sizeof(ulong) == sizeof(double));
            UnsafeScalarCastLong cast = new UnsafeScalarCastLong();
            cast._double = a;
            return cast._ulong;
        }

        public static double AsDouble(long a)
        {
            Debug.Assert(sizeof(long) == sizeof(double));
            UnsafeScalarCastLong cast = new UnsafeScalarCastLong();
            cast._long = a;
            return cast._double;
        }

        public static double AsDouble(ulong a)
        {
            Debug.Assert(sizeof(ulong) == sizeof(double));
            UnsafeScalarCastLong cast = new UnsafeScalarCastLong();
            cast._ulong = a;
            return cast._double;
        }
    }
}
