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
}
