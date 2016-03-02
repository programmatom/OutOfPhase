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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// TODO: port this non-delegate version back to other splay tree sources

namespace OutOfPhase
{
    // Nodes implemented as elements in an array -- better for fixed-sized data sets

    // http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c
    //
    //            An implementation of top-down splaying
    //                D. Sleator <sleator@cs.cmu.edu>
    //                         March 1992
    //
    //"Splay trees", or "self-adjusting search trees" are a simple and
    //efficient data structure for storing an ordered set.  The data
    //structure consists of a binary tree, without parent pointers, and no
    //additional fields.  It allows searching, insertion, deletion,
    //deletemin, deletemax, splitting, joining, and many other operations,
    //all with amortized logarithmic performance.  Since the trees adapt to
    //the sequence of requests, their performance on real access patterns is
    //typically even better.  Splay trees are described in a number of texts
    //and papers [1,2,3,4,5].
    //
    //The code here is adapted from simple top-down splay, at the bottom of
    //page 669 of [3].  It can be obtained via anonymous ftp from
    //spade.pc.cs.cmu.edu in directory /usr/sleator/public.
    //
    //The chief modification here is that the splay operation works even if the
    //item being splayed is not in the tree, and even if the tree root of the
    //tree is NULL.  So the line:
    //
    //                          t = splay(i, t);
    //
    //causes it to search for item with key i in the tree rooted at t.  If it's
    //there, it is splayed to the root.  If it isn't there, then the node put
    //at the root is the last one before NULL that would have been reached in a
    //normal binary search for i.  (It's a neighbor of i in the tree.)  This
    //allows many other operations to be easily implemented, as shown below.
    //
    //[1] "Fundamentals of data structures in C", Horowitz, Sahni,
    //   and Anderson-Freed, Computer Science Press, pp 542-547.
    //[2] "Data Structures and Their Algorithms", Lewis and Denenberg,
    //   Harper Collins, 1991, pp 243-251.
    //[3] "Self-adjusting Binary Search Trees" Sleator and Tarjan,
    //   JACM Volume 32, No 3, July 1985, pp 652-686.
    //[4] "Data Structure and Algorithm Analysis", Mark Weiss,
    //   Benjamin Cummins, 1992, pp 119-130.
    //[5] "Data Structures, Algorithms, and Performance", Derick Wood,
    //   Addison-Wesley, 1993, pp 367-375.
    public class SplayTreeArray<KeyType, ValueType> where KeyType : IComparable<KeyType>
    {
        private const int Nil = -1;
        private const int N = 0;

        private SplayNode[] nodes;
        private int root;
        private int freelist;

        private bool locked;

        [StructLayout(LayoutKind.Auto)]
        private struct SplayNode
        {
            internal int left;
            internal int right;

            internal KeyType key;
            internal ValueType value;
        }

        public SplayTreeArray()
            : this(0)
        {
        }

        public SplayTreeArray(int capacity, bool locked)
        {
            nodes = new SplayNode[capacity + 1];
            freelist = Nil;
            for (int i = capacity - 1; i >= 0; i--)
            {
                Free(i + 1);
            }
            root = Nil;

            this.locked = locked;
        }

        public SplayTreeArray(int capacity)
            : this(capacity, false)
        {
        }

        public SplayTreeArray(SplayTreeArray<KeyType, ValueType> original)
        {
            nodes = (SplayNode[])original.nodes.Clone();
            root = original.root;
            freelist = original.freelist;
        }

        private void Free(int node)
        {
            nodes[node].left = freelist;
            freelist = node;
        }

        private int Allocate()
        {
            if (freelist == Nil)
            {
                Debug.Assert(nodes.Length > 0);
                int oldLength = nodes.Length;
                int newLength = nodes.Length * 2;
                {
                    if (locked)
                    {
                        Debug.Assert(false, "Splay tree capacity exhausted but is locked");
                        throw new InvalidOperationException("Splay tree capacity exhausted but is locked");
                    }
                    SplayNode[] newNodes = new SplayNode[newLength];
                    for (int i = 0; i < oldLength; i++)
                    {
                        newNodes[i] = nodes[i];
                    }
                    nodes = newNodes;
                }
                for (int i = newLength - 1; i >= oldLength; i--)
                {
                    Free(i);
                }
            }
            int node = freelist;
            freelist = nodes[freelist].left;
            return node;
        }

        public void Add(KeyType key, ValueType value)
        {
            int i = Allocate();
            nodes[i].key = key;
            nodes[i].value = value;
            if (!SplayInsert(i, ref root))
            {
                Free(i);
                throw new ApplicationException("item already in tree");
            }
        }

        public void Remove(KeyType key)
        {
            Splay(ref root, key);
            if (0 != key.CompareTo(nodes[root].key))
            {
                throw new ApplicationException("item not in tree");
            }
            int node = root;
            if (!SplayRemove(node, ref root))
            {
                throw new ApplicationException("item not in tree");
            }
            Free(node);
        }

        public bool ContainsKey(KeyType key)
        {
            Splay(ref root, key);
            return (root != Nil) && (0 == key.CompareTo(nodes[root].key));
        }

        public ValueType GetValue(KeyType key)
        {
            Splay(ref root, key);
            if (0 != key.CompareTo(nodes[root].key))
            {
                throw new ApplicationException("item not in tree");
            }
            return nodes[root].value;
        }

        public void SetValue(KeyType key, ValueType value)
        {
            Splay(ref root, key);
            if (0 != key.CompareTo(nodes[root].key))
            {
                throw new ApplicationException("item not in tree");
            }
            nodes[root].value = value;
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            if (root == Nil)
            {
                nearestKey = default(KeyType);
                return false;
            }
            Splay(ref root, key);
            int rootComparison = key.CompareTo(nodes[root].key);
            if (rootComparison < 0)
            {
                if (nodes[root].left == Nil)
                {
                    nearestKey = default(KeyType);
                    return false;
                }
                KeyType rootKey = nodes[root].key;
                Splay(ref nodes[root].left, rootKey);
                nearestKey = nodes[nodes[root].left].key;
                return true;
            }
            else
            {
                nearestKey = nodes[root].key;
                return true;
            }
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            if (NearestLessOrEqual(key, out nearestKey))
            {
                if (nodes[root].left == Nil)
                {
                    nearestKey = default(KeyType);
                    return false;
                }
                KeyType rootKey = nodes[root].key;
                Splay(ref nodes[root].left, rootKey);
                nearestKey = nodes[nodes[root].left].key;
                return true;
            }
            return false;
        }

        public bool Least(out KeyType leastOut)
        {
            if (root == Nil)
            {
                leastOut = default(KeyType);
                return false;
            }
            int node = root;
            KeyType least = nodes[node].key;
            while (nodes[node].left != Nil)
            {
                node = nodes[node].left;
                least = nodes[node].key;
            }
            Splay(ref root, least);
            leastOut = least;
            return true;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            if (root == Nil)
            {
                nearestKey = default(KeyType);
                return false;
            }
            Splay(ref root, key);
            int rootComparison = key.CompareTo(nodes[root].key);
            if (rootComparison > 0)
            {
                if (nodes[root].right == Nil)
                {
                    nearestKey = default(KeyType);
                    return false;
                }
                KeyType rootKey = nodes[root].key;
                Splay(ref nodes[root].right, rootKey);
                nearestKey = nodes[nodes[root].right].key;
                return true;
            }
            else
            {
                nearestKey = nodes[root].key;
                return true;
            }
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            if (NearestGreaterOrEqual(key, out nearestKey))
            {
                if (nodes[root].right == Nil)
                {
                    nearestKey = default(KeyType);
                    return false;
                }
                KeyType rootKey = nodes[root].key;
                Splay(ref nodes[root].right, rootKey);
                nearestKey = nodes[nodes[root].right].key;
                return true;
            }
            return false;
        }

        public bool Greatest(out KeyType greatestOut)
        {
            if (root == Nil)
            {
                greatestOut = default(KeyType);
                return false;
            }
            int node = root;
            KeyType greatest = nodes[node].key;
            while (nodes[node].right != Nil)
            {
                node = nodes[node].right;
                greatest = nodes[node].key;
            }
            Splay(ref root, greatest);
            greatestOut = greatest;
            return true;
        }

        private void Splay(
            ref int root,
            KeyType leftComparand)
        {
            int t;
            int l;
            int r;
            int y;

            t = root;
            if (t == Nil)
            {
                return;
            }
            nodes[N].left = Nil;
            nodes[N].right = Nil;

            l = N;
            r = N;

            while (true)
            {
                int c;

                c = leftComparand.CompareTo(nodes[t].key);
                if (c < 0)
                {
                    if (nodes[t].left == Nil)
                    {
                        break;
                    }
                    c = leftComparand.CompareTo(nodes[nodes[t].left].key);
                    if (c < 0)
                    {
                        // rotate right
                        y = nodes[t].left;
                        nodes[t].left = nodes[y].right;
                        nodes[y].right = t;
                        t = y;
                        if (nodes[t].left == Nil)
                        {
                            break;
                        }
                    }
                    /* link right */
                    nodes[r].left = t;
                    r = t;
                    t = nodes[t].left;
                }
                else if (c > 0)
                {
                    if (nodes[t].right == Nil)
                    {
                        break;
                    }
                    c = leftComparand.CompareTo(nodes[nodes[t].right].key);
                    if (c > 0)
                    {
                        // rotate left
                        y = nodes[t].right;
                        nodes[t].right = nodes[y].left;
                        nodes[y].left = t;
                        t = y;
                        if (nodes[t].right == Nil)
                        {
                            break;
                        }
                    }
                    // link left
                    nodes[l].right = t;
                    l = t;
                    t = nodes[t].right;
                }
                else
                {
                    break;
                }
            }
            // reassemble
            nodes[l].right = nodes[t].left;
            nodes[r].left = nodes[t].right;
            nodes[t].left = nodes[N].right;
            nodes[t].right = nodes[N].left;
            root = t;
        }

        // insert node into tree.  returns true if successful, or false if
        // item was already in the tree. */
        private bool SplayInsert(
            int i,
            ref int root)
        {
            int c;

            if (root == Nil)
            {
                nodes[i].left = Nil;
                nodes[i].right = Nil;
                root = i;
                return true;
            }
            KeyType key = nodes[i].key;
            Splay(ref root, key);
            c = nodes[i].key.CompareTo(nodes[root].key);
            if (c < 0)
            {
                nodes[i].left = nodes[root].left;
                nodes[i].right = root;
                nodes[root].left = Nil;
                root = i;
                return true;
            }
            else if (c > 0)
            {
                nodes[i].right = nodes[root].right;
                nodes[i].left = root;
                nodes[root].right = Nil;
                root = i;
                return true;
            }
            else
            {
                // already in tree
                return false;
            }
        }

        // remove node from tree.  returns true if successful, or false if
        // item was not in the tree.
        private bool SplayRemove(
            int i,
            ref int root)
        {
            int c;

            if (root == Nil)
            {
                return false;
            }
            KeyType key = nodes[i].key;
            Splay(ref root, key);
            c = nodes[i].key.CompareTo(nodes[root].key);
            if (c == 0)
            {
                int x;

                if (nodes[i].left == Nil)
                {
                    x = nodes[root].right;
                }
                else
                {
                    x = nodes[root].left;
                    Splay(ref x, key);
                    nodes[x].right = nodes[root].right;
                }
                root = x;
                return true;
            }
            return false;
        }
    }
}
