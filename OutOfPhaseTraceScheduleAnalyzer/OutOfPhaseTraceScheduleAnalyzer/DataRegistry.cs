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
using System.IO;
using System.Text;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    public static class DataRegistry
    {
        private class TopInfo
        {
            public string path;
            public Top top;
            public int refcount;

            public TopInfo(string path, Top top)
            {
                this.path = path;
                this.top = top;
                this.refcount = 1;
            }
        }

        private static readonly List<TopInfo> items = new List<TopInfo>();

        public static Top QueryAddref(string path)
        {
            path = Path.GetFullPath(path);
            foreach (TopInfo item in items)
            {
                if (String.Equals(item.path, path, StringComparison.OrdinalIgnoreCase))
                {
                    item.refcount++;
                    return item.top;
                }
            }
            return null;
        }

        public static void Add(string path, Top top)
        {
            path = Path.GetFullPath(path);
            items.Add(new TopInfo(path, top));
        }

        public static void Release(Top top)
        {
            for (int i = 0; i < items.Count; i++)
            {
                TopInfo item = items[i];
                if (item.top == top)
                {
                    item.refcount--;
                    if (item.refcount == 0)
                    {
                        items.RemoveAt(i);
                        item.top.Dispose();
                    }
                    return;
                }
            }
            Debug.Assert(false); // not found
        }
    }
}
