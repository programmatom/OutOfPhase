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
using System.Text;

// TODO: port this non-delegate version back to other splay tree sources

namespace OutOfPhase
{
    // Nodes are heap-allocated

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
    public class SplayTree<KeyType, ValueType> where KeyType : IComparable<KeyType>
    {
        private readonly SplayNode Nil = new SplayNode();

        private SplayNode root;

        private class SplayNode
        {
            internal SplayNode left;
            internal SplayNode right;

            internal KeyType key;
            internal ValueType value;
        }

        public SplayTree()
        {
            root = Nil;
        }

        public SplayTree(SplayTree<KeyType, ValueType> original)
        {
            throw new NotImplementedException("clone is not implemented");
        }

        public void Add(KeyType key, ValueType value)
        {
            SplayNode i = new SplayNode();
            i.key = key;
            i.value = value;
            if (!SplayInsert(i, ref root))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        public void Remove(KeyType key)
        {
            Splay(ref root, key);
            if (0 != key.CompareTo(root.key))
            {
                throw new KeyNotFoundException("item not in tree");
            }
            SplayNode node = root;
            if (!SplayRemove(node, ref root))
            {
                throw new KeyNotFoundException("item not in tree");
            }
        }

        public bool ContainsKey(KeyType key)
        {
            Splay(ref root, key);
            return (root != Nil) && (0 == key.CompareTo(root.key));
        }

        public ValueType GetValue(KeyType key)
        {
            Splay(ref root, key);
            if (0 != key.CompareTo(root.key))
            {
                throw new KeyNotFoundException("item not in tree");
            }
            return root.value;
        }

        public void SetValue(KeyType key, ValueType value)
        {
            Splay(ref root, key);
            if (0 != key.CompareTo(root.key))
            {
                throw new KeyNotFoundException("item not in tree");
            }
            root.value = value;
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            if (root == Nil)
            {
                nearestKey = default(KeyType);
                return false;
            }
            Splay(ref root, key);
            int rootComparison = key.CompareTo(root.key);
            if (rootComparison < 0)
            {
                if (root.left == Nil)
                {
                    nearestKey = default(KeyType);
                    return false;
                }
                KeyType rootKey = root.key;
                Splay(ref root.left, rootKey);
                nearestKey = root.left.key;
                return true;
            }
            else
            {
                nearestKey = root.key;
                return true;
            }
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            if (root == Nil)
            {
                nearestKey = default(KeyType);
                return false;
            }
            Splay(ref root, key);
            int rootComparison = key.CompareTo(root.key);
            if (rootComparison == 0)
            {
                if (root.left == Nil)
                {
                    nearestKey = default(KeyType);
                    return false;
                }
                nearestKey = root.left.key;
                return true;
            }
            else
            {
                nearestKey = root.key;
                return true;
            }
        }

        public bool Least(out KeyType leastOut)
        {
            if (root == Nil)
            {
                leastOut = default(KeyType);
                return false;
            }
            SplayNode node = root;
            KeyType least = node.key;
            while (node.left != Nil)
            {
                node = node.left;
                least = node.key;
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
            int rootComparison = key.CompareTo(root.key);
            if (rootComparison > 0)
            {
                if (root.right == Nil)
                {
                    nearestKey = default(KeyType);
                    return false;
                }
                KeyType rootKey = root.key;
                Splay(ref root.right, rootKey);
                nearestKey = root.right.key;
                return true;
            }
            else
            {
                nearestKey = root.key;
                return true;
            }
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            if (root == Nil)
            {
                nearestKey = default(KeyType);
                return false;
            }
            Splay(ref root, key);
            int rootComparison = key.CompareTo(root.key);
            if (rootComparison == 0)
            {
                if (root.right == Nil)
                {
                    nearestKey = default(KeyType);
                    return false;
                }
                nearestKey = root.right.key;
                return true;
            }
            else
            {
                nearestKey = root.key;
                return true;
            }
        }

        public bool Greatest(out KeyType greatestOut)
        {
            if (root == Nil)
            {
                greatestOut = default(KeyType);
                return false;
            }
            SplayNode node = root;
            KeyType greatest = node.key;
            while (node.right != Nil)
            {
                node = node.right;
                greatest = node.key;
            }
            Splay(ref root, greatest);
            greatestOut = greatest;
            return true;
        }

        public delegate void TraverseAction(KeyType key, ValueType ValueType);
        public void Traverse(TraverseAction action)
        {
            Traverse(action, root);
        }
        private void Traverse(TraverseAction action, SplayNode node)
        {
            if (node != Nil)
            {
                Traverse(action, node.left);
                action(node.key, node.value);
                Traverse(action, node.right);
            }
        }

        private void Splay(
            ref SplayNode root,
            KeyType leftComparand)
        {
            SplayNode N = new SplayNode();
            SplayNode t;
            SplayNode l;
            SplayNode r;
            SplayNode y;

            t = root;
            if (t == Nil)
            {
                return;
            }
            N.left = Nil;
            N.right = Nil;

            l = N;
            r = N;

            while (true)
            {
                int c;

                c = leftComparand.CompareTo(t.key);
                if (c < 0)
                {
                    if (t.left == Nil)
                    {
                        break;
                    }
                    c = leftComparand.CompareTo(t.left.key);
                    if (c < 0)
                    {
                        // rotate right
                        y = t.left;
                        t.left = y.right;
                        y.right = t;
                        t = y;
                        if (t.left == Nil)
                        {
                            break;
                        }
                    }
                    /* link right */
                    r.left = t;
                    r = t;
                    t = t.left;
                }
                else if (c > 0)
                {
                    if (t.right == Nil)
                    {
                        break;
                    }
                    c = leftComparand.CompareTo(t.right.key);
                    if (c > 0)
                    {
                        // rotate left
                        y = t.right;
                        t.right = y.left;
                        y.left = t;
                        t = y;
                        if (t.right == Nil)
                        {
                            break;
                        }
                    }
                    // link left
                    l.right = t;
                    l = t;
                    t = t.right;
                }
                else
                {
                    break;
                }
            }
            // reassemble
            l.right = t.left;
            r.left = t.right;
            t.left = N.right;
            t.right = N.left;
            root = t;
        }


        // insert node into tree.  returns true if successful, or false if
        // item was already in the tree. */
        private bool SplayInsert(
            SplayNode i,
            ref SplayNode root)
        {
            int c;

            if (root == Nil)
            {
                i.left = Nil;
                i.right = Nil;
                root = i;
                return true;
            }
            KeyType key = i.key;
            Splay(ref root, key);
            c = i.key.CompareTo(root.key);
            if (c < 0)
            {
                i.left = root.left;
                i.right = root;
                root.left = Nil;
                root = i;
                return true;
            }
            else if (c > 0)
            {
                i.right = root.right;
                i.left = root;
                root.right = Nil;
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
            SplayNode i,
            ref SplayNode root)
        {
            int c;

            if (root == Nil)
            {
                return false;
            }
            KeyType key = i.key;
            Splay(ref root, key);
            c = i.key.CompareTo(root.key);
            if (c == 0)
            {
                SplayNode x;

                if (i.left == Nil)
                {
                    x = root.right;
                }
                else
                {
                    x = root.left;
                    Splay(ref x, key);
                    x.right = root.right;
                }
                root = x;
                return true;
            }
            return false;
        }
    }

    class SplayDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : IComparable<TKey>
    {
        private SplayTree<TKey, TValue> splay = new SplayTree<TKey, TValue>();
        private int count;

        public ICollection<TKey> Keys { get { throw new NotImplementedException(); } }

        public ICollection<TValue> Values { get { throw new NotImplementedException(); } }

        public TValue this[TKey key]
        {
            get
            {
                return splay.GetValue(key);
            }

            set
            {
                TKey nearest;
                if (splay.NearestGreaterOrEqual(key, out nearest) && nearest.Equals(key))
                {
                    splay.SetValue(key, value);
                }
                else
                {
                    splay.Add(key, value);
                    count++;
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            splay.Add(key, value);
            count++;
        }

        public bool ContainsKey(TKey key)
        {
            return splay.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            if (!splay.ContainsKey(key))
            {
                return false;
            }
            splay.Remove(key);
            count--;
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            if (splay.ContainsKey(key))
            {
                value = splay.GetValue(key);
                return true;
            }
            return false;
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            splay = new SplayTree<TKey, TValue>();
            count = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            splay.Traverse(delegate(TKey key, TValue value) { array[arrayIndex++] = new KeyValuePair<TKey, TValue>(key, value); });
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            Remove(item.Key);
            return true;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new SplayEnumerator(splay);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        class SplayEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private SplayTree<TKey, TValue> splay;
            private bool started;
            private bool valid;
            private TKey current;

            public SplayEnumerator(SplayTree<TKey, TValue> splay)
            {
                this.splay = splay;
                Reset();
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (valid)
                    {
                        return new KeyValuePair<TKey, TValue>(current, splay.GetValue(current));
                    }
                    throw new KeyNotFoundException("item not in tree");
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (!started)
                {
                    valid = splay.Least(out current);
                    started = true;
                }
                else if (valid)
                {
                    valid = splay.NearestGreater(current, out current);
                }
                return valid;
            }

            public void Reset()
            {
                started = false;
                valid = false;
                current = default(TKey);
            }
        }
    }
}
