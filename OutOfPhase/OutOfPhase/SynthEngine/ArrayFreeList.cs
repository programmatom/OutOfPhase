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
    public static partial class Synthesizer
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SizeOfHack<T>
        {
            public long first;
            public T middle;
        }

        public unsafe static int SizeOf<T>() // approximate - includes padding
        {
            SizeOfHack<T>[] t = new SizeOfHack<T>[2];
            fixed (long* pFirst = &(t[0].first))
            {
                fixed (long* pSecond = &(t[1].first))
                {
                    return (int)((long)pSecond - (long)pFirst - sizeof(long));
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        public struct SharedBlockArrayFreeList<T>
        {
            private readonly SharedBlockFreeList<T[]>[] freelists;
            private readonly int largeStartIndex;

            public SharedBlockArrayFreeList(
                int smallBlockSize,
                int largeBlockSize,
                int smallItemMaxBytes)
            {
                long sizeofT = SizeOf<T>();

                freelists = new SharedBlockFreeList<T[]>[32];
                largeStartIndex = -1;
                for (int i = 0; i < freelists.Length; i++)
                {
                    bool large = sizeofT * (1L << i) > smallItemMaxBytes;
                    if ((largeStartIndex == -1) && large)
                    {
                        largeStartIndex = i;
                    }
                    freelists[i] = new SharedBlockFreeList<T[]>(large ? largeBlockSize : smallBlockSize);
                }
            }

            public SharedBlockFreeList<T[]> this[int index]
            {
                get
                {
                    return freelists[index];
                }
            }

            public int Count
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < freelists.Length; i++)
                    {
                        count += freelists[i].Count;
                    }
                    return count;
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        public struct ArrayFreeList<T>
        {
            private const int MinLength = 1;

            private readonly SharedBlockFreeList<object> sharedEmptySmallBlocksList;
            private readonly SharedBlockFreeList<object> sharedEmptyLargeBlocksList;
            private readonly SimpleFreeList<T[]>[] freeLists;
            private readonly int largeStartIndex;
            private int countInUse;

            public ArrayFreeList(
                int smallBlockSize,
                int largeBlockSize,
                int smallItemMaxBytes,
                SharedBlockArrayFreeList<T> sharedBlockLists,
                SharedBlockFreeList<object> sharedEmptySmallBlocksList,
                SharedBlockFreeList<object> sharedEmptyLargeBlocksList)
            {
                long sizeofT = SizeOf<T>();

                countInUse = 0;

                this.sharedEmptySmallBlocksList = sharedEmptySmallBlocksList;
                this.sharedEmptyLargeBlocksList = sharedEmptyLargeBlocksList;

                freeLists = new SimpleFreeList<T[]>[32];
                largeStartIndex = -1;
                for (int i = 0; i < freeLists.Length; i++)
                {
                    bool large = sizeofT * (1L << i) > smallItemMaxBytes;
                    if ((largeStartIndex == -1) && large)
                    {
                        largeStartIndex = i;
                    }
                    freeLists[i] = new SimpleFreeList<T[]>(
                        large ? largeBlockSize : smallBlockSize,
                        sharedBlockLists[i],
                        large ? sharedEmptyLargeBlocksList : sharedEmptySmallBlocksList);
                }
            }

#if false // TODO: requires x32 array of empty container freelists
            public static int SlotCount(int sizeofT, int countT, int cutoffBytes, int smallBlockSize)
            {
                Debug.Assert(sizeofT > 0);
                int w = sizeofT * countT;
                int c = (cutoffBytes + w - 1) / w;
                c = IntPow2RoundUp(c);
                return Math.Min(Math.Max(c, 1), smallBlockSize);
            }
#endif

            public T[] New(int count)
            {
                int internalCount = Math.Max(count, MinLength);
                int slot = MsbIndex((uint)Math.Max(internalCount - 1, 0)) + 1;

                T[] array;
                if (!freeLists[slot].Empty)
                {
                    array = (T[])freeLists[slot].Pop();
                    Array.Clear(array, 0, array.Length);
                }
                else
                {
                    array = new T[1 << slot];
                }
                Debug.Assert(array.Length >= count);

                countInUse++;
                return array;
            }

            public void Free(T[] array)
            {
                int slot = MsbIndex((uint)Math.Max(array.Length - 1, 0)) + 1;
                Debug.Assert(array.Length == 1 << slot);
                freeLists[slot].Push(array);
                countInUse--;
            }

#if false // TODO:
            public static int IntPow2RoundUp(int x)
            {
                if ((x & (x - 1)) != 0)
                {
                    x |= x >> 1;
                    x |= x >> 2;
                    x |= x >> 4;
                    x |= x >> 8;
                    x |= x >> 16;
                    x++;
                }
                return x;
            }
#endif

            public static int MsbIndex(uint x)
            {
                if (x == 0)
                {
                    return -1;
                }
                int c = 0;
                if (x >= (1 << 16))
                {
                    c += 16;
                    x = x >> 16;
                }
                if (x >= (1 << 8))
                {
                    c += 8;
                    x = x >> 8;
                }
                if (x >= (1 << 4))
                {
                    c += 4;
                    x = x >> 4;
                }
                if (x >= (1 << 2))
                {
                    c += 2;
                    x = x >> 2;
                }
                if (x >= (1 << 1))
                {
                    c += 1;
                    x = x >> 1;
                }
                return c;
            }

            public int CountInUse { get { return countInUse; } }

            public int CountFree
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < freeLists.Length; i++)
                    {
                        count += freeLists[i].CountFree;
                    }
                    return count;
                }
            }
        }

        public static T[] New<T>(ref ArrayFreeList<T> freelist, int count)
        {
            if (!EnableFreeLists)
            {
                return new T[count];
            }

            return freelist.New(count);
        }

        public static void Free<T>(ref ArrayFreeList<T> freelist, ref T[] array)
        {
            T[] localArray = array;
            array = null;

            if (!EnableFreeLists)
            {
                return;
            }

            freelist.Free(localArray);
        }
    }
}
