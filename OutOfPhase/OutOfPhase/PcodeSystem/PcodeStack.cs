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
    /* reference counting suggested by Abe Megahed */
    // (long before I heard of COM -- back when I needed to figure out when to release memory
    // held by arrays.)
    public class RefCountHandle
    {
        public IDisposable disposable;
        public int refcount;

        public void AddRef()
        {
            refcount++;
        }

        public void Release()
        {
            if (--refcount == 0)
            {
                disposable.Dispose();
                disposable = null;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public abstract class ArrayHandle
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ArrayHandleByte : ArrayHandle
    {
        [FieldOffset(0)]
        public byte[] bytes; // bools or string - when string, is UTF8

        public ArrayHandleByte(byte[] a)
        {
            this.bytes = a;
        }

        // Try to hide for now that we're doing string/char[] as UTF8 encoded byte[]
        // It's some work to add first-class support for char[] and not that important right now.
        // But we want to protect client code from knowing that we're using UTF8 conversions
        // to emulate.

        public ArrayHandleByte(string s)
        {
            this.bytes = Encoding.UTF8.GetBytes(s);
        }

        public ArrayHandleByte(char[] s)
        {
            this.bytes = Encoding.UTF8.GetBytes(s);
        }

        public string strings
        {
            get
            {
                return Encoding.UTF8.GetString(bytes);
            }
        }

        public char[] chars
        {
            get
            {
                return Encoding.UTF8.GetChars(bytes);
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ArrayHandleInt32 : ArrayHandle
    {
        [FieldOffset(0)]
        public int[] ints;

        public ArrayHandleInt32(int[] a)
        {
            this.ints = a;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ArrayHandleFloat : ArrayHandle
    {
        [FieldOffset(0)]
        public float[] floats;

        public ArrayHandleFloat(float[] a)
        {
            this.floats = a;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ArrayHandleDouble : ArrayHandle
    {
        [FieldOffset(0)]
        public double[] doubles;

        public ArrayHandleDouble(double[] a)
        {
            this.doubles = a;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ReferenceOverlayType
    {
        [FieldOffset(0)]
        public ArrayHandle arrayHandleGeneric;
        [FieldOffset(0)]
        public ArrayHandleByte arrayHandleByte;
        [FieldOffset(0)]
        public ArrayHandleInt32 arrayHandleInt32;
        [FieldOffset(0)]
        public ArrayHandleFloat arrayHandleFloat;
        [FieldOffset(0)]
        public ArrayHandleDouble arrayHandleDouble;

        [FieldOffset(0)]
        public PcodeRec Procedure;

        [FieldOffset(0)]
        public object generic;

        [FieldOffset(0)]
        public RefCountHandle handle;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ScalarOverlayType
    {
        [FieldOffset(0)]
        public int Integer; // integer and bool
        [FieldOffset(0)]
        public long int64; // not used in evaluation - only for checking/zeroing
        [FieldOffset(0)]
        public float Float;
        [FieldOffset(0)]
        public double Double;
    }

    public struct StackElement
    {
        // In managed code, pointer and value types can't overlay (because the garbage collector
        // can't tell if a memory location contains a reference or numeric data), so these are
        // stored consecutively.
        public ScalarOverlayType Data;
        public ReferenceOverlayType reference;


#if DEBUG
        public void AssertClear()
        {
            Debug.Assert(Data.int64 == 0);
            Debug.Assert(reference.generic == null);
        }

        public void AssertScalar()
        {
            Debug.Assert(reference.generic == null);
        }

        public void AssertReturnAddress()
        {
            Debug.Assert(reference.generic is PcodeRec);
        }

        public void AssertByteArray()
        {
            Debug.Assert(Data.int64 == 0);
            Debug.Assert((reference.generic == null) || (reference.generic is ArrayHandleByte));
        }

        public void AssertIntegerArray()
        {
            Debug.Assert(Data.int64 == 0);
            Debug.Assert((reference.generic == null) || (reference.generic is ArrayHandleInt32));
        }

        public void AssertFloatArray()
        {
            Debug.Assert(Data.int64 == 0);
            Debug.Assert((reference.generic == null) || (reference.generic is ArrayHandleFloat));
        }

        public void AssertDoubleArray()
        {
            Debug.Assert(Data.int64 == 0);
            Debug.Assert((reference.generic == null) || (reference.generic is ArrayHandleDouble));
        }

        public void AssertAnyArray()
        {
            Debug.Assert(Data.int64 == 0);
            Debug.Assert((reference.generic == null) || (reference.generic is ArrayHandle));
        }
#endif

        public void Clear()
        {
            Data.int64 = 0;

            if (reference.generic != null)
            {
                RefCountHandle refCountHandle = reference.generic as RefCountHandle;
                if (refCountHandle != null)
                {
                    refCountHandle.Release();
                }
            }
            reference.generic = null;
        }

        public void ClearScalar()
        {
#if DEBUG
            Debug.Assert(reference.generic == null);
#endif
            Data.int64 = 0;
        }

        public void ClearArray()
        {
#if DEBUG
            Debug.Assert(Data.int64 == 0);
            AssertAnyArray();
#endif
            reference.generic = null;
        }

        public void CopyFrom(ref StackElement source)
        {
            Data.int64 = source.Data.int64;
            reference.generic = source.reference.generic;
            if (reference.generic != null)
            {
                RefCountHandle refCountHandle = reference.generic as RefCountHandle;
                if (refCountHandle != null)
                {
                    refCountHandle.AddRef();
                }
            }
        }

#if DEBUG
        public override string ToString()
        {
            if (reference.generic == null)
            {
                return String.Format("[? {0} {1} {2} 0x{3:x16}]", Data.Integer, Data.Float, Data.Double, Data.int64);
            }
            else if (reference.generic is ArrayHandleByte)
            {
                return String.Format("[ArrayHandleByte({0})]", reference.arrayHandleByte.bytes);
            }
            else if (reference.generic is ArrayHandleInt32)
            {
                return String.Format("[ArrayHandleInt32({0})]", reference.arrayHandleInt32.ints);
            }
            else if (reference.generic is ArrayHandleFloat)
            {
                return String.Format("[ArrayHandleFloat({0})]", reference.arrayHandleFloat.floats);
            }
            else if (reference.generic is ArrayHandleDouble)
            {
                return String.Format("[ArrayHandleDouble({0})]", reference.arrayHandleDouble.doubles);
            }
            else if (reference.generic is PcodeRec)
            {
                return String.Format("[RetAddr({0}:{1})]", reference.Procedure, Data.Integer);
            }
            else
            {
                return base.ToString();
            }
        }
#endif
    }

    public class ParamStackRec : IDisposable
    {
        private const int INITIALNUMSTACKCELLS = 16;

        private StackElement[] ElementArray = new StackElement[INITIALNUMSTACKCELLS];
        private int NumElements = 0;
#if DEBUG
        private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

        /* helper to ensure stack has adequate capacity */
        private void EnsureStackSize(int MinElements)
        {
            if (ElementArray.Length < MinElements)
            {
                int NewCapacity = Math.Max(ElementArray.Length * 2, MinElements);
                Array.Resize(ref ElementArray, NewCapacity);
            }
        }

        /* helper to ensure at least one available slot */
        private void EnsureStackSize1()
        {
            EnsureStackSize(NumElements + 1);
        }

        /* restore the stack to it's initial (empty) state. */
        public void Clear() // was EmptyParamStack
        {
            for (int i = 0; i < NumElements; i++)
            {
                ElementArray[i].Clear();
            }
            NumElements = 0;
        }

        public int GetStackNumElements()
        {
            return NumElements;
        }

        /* restore stack to empty state and ensure at least a certain capacity.  returns */
        /* False if there wasn't enough memory available to ensure capacity. */
        public void EmptyParamStackEnsureCapacity(int MinElements)
        {
            Clear();
            EnsureStackSize(MinElements);
        }

        /* this should be called when the parameter list is completely finished to */
        /* dispose of all arrays that might still be stored in it */
        public void Dispose()
        {
            Clear();
            GC.SuppressFinalize(this);
        }

        ~ParamStackRec()
        {
#if DEBUG
            Debug.Assert(false, "ParamStackRec finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            Dispose();
        }

        public void AddIntegerToStack(int value)
        {
            EnsureStackSize1();
            ElementArray[NumElements].Data.Integer = value;
            NumElements++;
        }

        public void AddFloatToStack(float value)
        {
            EnsureStackSize1();
            ElementArray[NumElements].Data.Float = value;
            NumElements++;
        }

        public void AddDoubleToStack(double value)
        {
            EnsureStackSize1();
            ElementArray[NumElements].Data.Double = value;
            NumElements++;
        }

        /* the ACTUAL array is added, so you no longer own it after this! */
        public void AddArrayToStack(ArrayHandleByte value)
        {
            EnsureStackSize1();
            ElementArray[NumElements].reference.arrayHandleByte = value;
            NumElements++;
        }
        public void AddArrayToStack(ArrayHandleInt32 value)
        {
            EnsureStackSize1();
            ElementArray[NumElements].reference.arrayHandleInt32 = value;
            NumElements++;
        }
        public void AddArrayToStack(ArrayHandleFloat value)
        {
            EnsureStackSize1();
            ElementArray[NumElements].reference.arrayHandleFloat = value;
            NumElements++;
        }
        public void AddArrayToStack(ArrayHandleDouble value)
        {
            EnsureStackSize1();
            ElementArray[NumElements].reference.arrayHandleDouble = value;
            NumElements++;
        }

        public void AddDisposableObjectToStack(IDisposable value)
        {
            EnsureStackSize1();
            if (value != null)
            {
                ElementArray[NumElements].reference.handle = new RefCountHandle();
                ElementArray[NumElements].reference.handle.refcount = 1;
                ElementArray[NumElements].reference.handle.disposable = value;
            }
            NumElements++;
        }

        public int GetStackInteger(int i)
        {
#if DEBUG
            if (ElementArray[i].reference.generic != null)
            {
                // not a scalar value
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return ElementArray[i].Data.Integer;
        }

        public float GetStackFloat(int i)
        {
#if DEBUG
            if (ElementArray[i].reference.generic != null)
            {
                // not a scalar value
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return ElementArray[i].Data.Float;
        }

        public double GetStackDouble(int i)
        {
#if DEBUG
            if (ElementArray[i].reference.generic != null)
            {
                // not a scalar value
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return ElementArray[i].Data.Double;
        }

        /* It returns the actual array, so you only get to use it on a loan basis */
        public object GetStackArray(int Index)
        {
#if DEBUG
            if (ElementArray[Index].Data.int64 != 0)
            {
                // not an array value
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            if (ElementArray[Index].reference.generic == null)
            {
                return null;
            }
#if DEBUG
            if (!(ElementArray[Index].reference.generic is ArrayHandleByte)
                && !(ElementArray[Index].reference.generic is ArrayHandleInt32)
                && !(ElementArray[Index].reference.generic is ArrayHandleFloat)
                && !(ElementArray[Index].reference.generic is ArrayHandleDouble))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            if (ElementArray[Index].reference.generic is ArrayHandleByte)
            {
                return ElementArray[Index].reference.arrayHandleByte.bytes;
            }
            else if (ElementArray[Index].reference.generic is ArrayHandleInt32)
            {
                return ElementArray[Index].reference.arrayHandleInt32.ints;
            }
            else if (ElementArray[Index].reference.generic is ArrayHandleFloat)
            {
                return ElementArray[Index].reference.arrayHandleFloat.floats;
            }
            else if (ElementArray[Index].reference.generic is ArrayHandleDouble)
            {
                return ElementArray[Index].reference.arrayHandleDouble.doubles;
            }
            else
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
        }

        public void GetRawStack(out StackElement[] ElementArray, out int NumElements)
        {
            ElementArray = this.ElementArray;
            NumElements = this.NumElements;
        }

        public void UpdateRawStack(StackElement[] ElementArray, int NumElements)
        {
#if DEBUG
            if (unchecked((uint)NumElements > (uint)ElementArray.Length))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            this.ElementArray = ElementArray;
            this.NumElements = NumElements;
        }
    }
}
