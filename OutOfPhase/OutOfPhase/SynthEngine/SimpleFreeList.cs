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
using System.Threading;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        public static bool EnableFreeLists;

        public class Block
        {
            private readonly object[] items;
            private Block next;

            public Block(int blockSize)
            {
                Debug.Assert((blockSize & (blockSize - 1)) == 0); // must be integral power of 2
                this.items = new object[blockSize];
            }

            public int BlockSize { get { return items.Length; } }

            public object[] Items { get { return items; } }

            public Block Next { get { return next; } set { next = value; } }
        }

        public class SharedBlockFreeList<T> where T : class
        {
            private readonly int blockSize;
            //private int count;
            private Block head;

            public SharedBlockFreeList(int blockSize)
            {
                Debug.Assert((blockSize & (blockSize - 1)) == 0); // must be integral power of 2
                this.blockSize = blockSize;
                this.head = null;
            }

            public void Push(Block block)
            {
                Debug.Assert(block.BlockSize == blockSize);
                while (true)
                {
                    Block localHead = this.head;

                    block.Next = localHead;
                    if (Interlocked.CompareExchange(ref this.head, block, localHead) == localHead)
                    {
                        //Interlocked.Add(ref count, blockSize);
                        return;
                    }
                }
            }

            public Block TryPop()
            {
                while (true)
                {
                    Block localHead = this.head;
                    if (localHead == null)
                    {
                        return null; // none available at this time
                    }

                    Block next = localHead.Next;
                    if (Interlocked.CompareExchange(ref this.head, next, localHead) == localHead)
                    {
                        localHead.Next = null;
                        //Interlocked.Add(ref count, -blockSize);
                        return localHead;
                    }
                }
            }

            public int BlockSize { get { return blockSize; } }

            // informational - only safe during quiescent times (used for pruning)
            public int Count
            {
                get
                {
                    int count = 0;
                    Block scan = this.head;
                    while (scan != null)
                    {
                        count += blockSize;
                        scan = scan.Next;
                    }
                    return count;
                }
            }

#if DEBUG
            public void SuppressFinalizers()
            {
                Block scan = this.head;
                while (scan != null)
                {
                    for (int i = 0; i < scan.Items.Length; i++)
                    {
                        if (scan.Items[i] != null) // this code is also used for empty containers freelist, so content may be null
                        {
                            GC.SuppressFinalize(scan.Items[i]);
                        }
                    }
                    scan = scan.Next;
                }
            }
#endif
        }

        [StructLayout(LayoutKind.Auto)]
        public struct SimpleFreeList<T> where T : class
        {
            private readonly SharedBlockFreeList<T> sharedFreeItemsList;
            private readonly SharedBlockFreeList<object> sharedEmptyBlocksList;

            private readonly int blockSize;

            private Block currentFreeItemsBlock;
            private int countFree;
            private int countInUse;
            private Block extraFreeItemsBlock;

#if DEBUG
            private readonly Dictionary<object, bool> freed;
#endif


            public SimpleFreeList(
                int blockSize,
                SharedBlockFreeList<T> sharedFreeList,
                SharedBlockFreeList<object> sharedEmptyBlocksList)
            {
                Debug.Assert((blockSize & (blockSize - 1)) == 0); // must be integral power of 2

                this.sharedFreeItemsList = sharedFreeList;
                this.sharedEmptyBlocksList = sharedEmptyBlocksList;

                this.blockSize = blockSize;

                this.currentFreeItemsBlock = null;
                this.countFree = 0;
                this.countInUse = 0;
                this.extraFreeItemsBlock = null;

#if DEBUG
                this.freed = new Dictionary<object, bool>();
#endif
            }

            public int CountFree { get { return countFree; } }

            // CountInUse is a partial picture, and may be negative if more objects were handed in than taken out.
            // In order to get the correct value, it must be summed across all instances (threads). It also doesn't
            // account for leaks.
            public int CountInUse { get { return countInUse; } }

            public void IncInUse() // because callers allocate the new ones when there are none on the free list
            {
                countInUse++;
            }

            public bool Empty { get { return countFree == 0; } }

            public T TryNew()
            {
                T item = null;
                if (!Empty)
                {
                    item = (T)Pop();
                }
                //else
                //{
                //    item = new T();
                //    countInUse++;
                //}
                return item;
            }

            public void Push(object item)
            {
#if DEBUG
                if (freed.ContainsKey(item))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                int i = countFree & (blockSize - 1);
                if (i == 0) // full
                {
                    // we're drowning in free objects - push to shared freelist
                    if (extraFreeItemsBlock != null)
                    {
#if DEBUG
                        for (int j = 0; j < extraFreeItemsBlock.Items.Length; j++)
                        {
                            freed.Remove(extraFreeItemsBlock.Items[j]);
                        }
#endif
                        countFree -= blockSize;
                        sharedFreeItemsList.Push(extraFreeItemsBlock);
                        extraFreeItemsBlock = null;
#if DEBUG
                        Debug.Assert(countFree == freed.Count);
#endif
                    }

                    // transfer current free block to extra (or might be null)
                    extraFreeItemsBlock = currentFreeItemsBlock;
                    currentFreeItemsBlock = null;

                    // create new current container for freed items
                    currentFreeItemsBlock = sharedEmptyBlocksList.TryPop();
                    if (currentFreeItemsBlock == null)
                    {
                        currentFreeItemsBlock = new Block(blockSize);
                    }
                }

                currentFreeItemsBlock.Items[i] = item;
                countFree++;
                countInUse--;
#if DEBUG
                freed.Add(item, false);
                Debug.Assert(countFree == freed.Count);
#endif
            }

            public object Pop()
            {
                if (countFree == 0)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                countFree--;
                countInUse++;
                int i = countFree & (blockSize - 1);
                object item = currentFreeItemsBlock.Items[i];
                currentFreeItemsBlock.Items[i] = null;
#if DEBUG
                if (!freed.ContainsKey(item))
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                freed.Remove(item);
#endif

                if (i == 0) // this container is now empty
                {
                    sharedEmptyBlocksList.Push(currentFreeItemsBlock); // give back

                    // transfer extra to here (or it's null)
                    currentFreeItemsBlock = extraFreeItemsBlock;
                    extraFreeItemsBlock = null;

                    if (currentFreeItemsBlock == null)
                    {
                        currentFreeItemsBlock = sharedFreeItemsList.TryPop(); // try to get a new set of free items
                        if (currentFreeItemsBlock != null)
                        {
                            Debug.Assert(currentFreeItemsBlock.BlockSize == blockSize);

                            countFree += blockSize;
#if DEBUG
                            for (int j = 0; j < currentFreeItemsBlock.Items.Length; j++)
                            {
                                freed.Add(currentFreeItemsBlock.Items[j], false);
                            }
                            Debug.Assert(countFree == freed.Count);
#endif
                        }
                    }
                }

                return item;
            }

#if DEBUG
            public void SuppressFinalizers()
            {
                if (currentFreeItemsBlock != null)
                {
                    for (int i = 0; i < currentFreeItemsBlock.Items.Length; i++)
                    {
                        if (currentFreeItemsBlock.Items[i] != null)
                        {
                            GC.SuppressFinalize(currentFreeItemsBlock.Items[i]);
                        }
                    }
                }
                if (extraFreeItemsBlock != null)
                {
                    for (int i = 0; i < extraFreeItemsBlock.Items.Length; i++)
                    {
                        if (extraFreeItemsBlock.Items[i] != null)
                        {
                            GC.SuppressFinalize(extraFreeItemsBlock.Items[i]);
                        }
                    }
                }
            }
#endif
        }

        public static T New<T>(ref SimpleFreeList<T> freelist) where T : class, new()
        {
            if (!EnableFreeLists)
            {
                return new T();
            }

            T item = freelist.TryNew();
            if (item == null)
            {
                freelist.IncInUse();
                item = new T();
            }
            return item;
        }

        public static void Free<T>(ref SimpleFreeList<T> freelist, ref T item) where T : class, new()
        {
            T localItem = item;
            item = null;

            if (!EnableFreeLists)
            {
                return;
            }

            freelist.Push(localItem);
        }
    }
}
