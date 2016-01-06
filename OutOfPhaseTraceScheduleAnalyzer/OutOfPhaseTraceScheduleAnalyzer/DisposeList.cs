/*
 *  Copyright © 2015-2016 Thomas R. Lawrence
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

namespace OutOfPhaseTraceScheduleAnalyzer
{
    public class DisposeList<T> : IDisposable
    {
        private readonly List<T> list = new List<T>();

        public DisposeList()
        {
        }

        public void Dispose()
        {
            Clear();
        }

        public int Count { get { return list.Count; } }

        public void Add(T item)
        {
            list.Add(item);
        }

        public void RemoveAt(int index)
        {
            if (list[index] is IDisposable)
            {
                ((IDisposable)list[index]).Dispose();
            }
            list.RemoveAt(index);
        }

        public void Clear()
        {
            foreach (T item in list)
            {
                if (item is IDisposable)
                {
                    ((IDisposable)item).Dispose();
                }
            }
            list.Clear();
        }

        public T this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                if (list[index] is IDisposable)
                {
                    ((IDisposable)list[index]).Dispose();
                }
                list[index] = value;
            }
        }
    }
}
