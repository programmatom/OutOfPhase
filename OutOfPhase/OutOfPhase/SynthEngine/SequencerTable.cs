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
        public class SequencerTableRec
        {
            /* hash name to collection of thingies */
            public Dictionary<string, List<PlayTrackInfoRec>> NameToArray;
        }

        /* create a new one */
        public static SequencerTableRec NewSequencerTable()
        {
            SequencerTableRec Table = new SequencerTableRec();

            Table.NameToArray = new Dictionary<string, List<PlayTrackInfoRec>>();

            return Table;
        }

        /* add an item */
        public static void SequencerTableInsert(
            SequencerTableRec Table,
            string Name,
            PlayTrackInfoRec Object)
        {
            List<PlayTrackInfoRec> Array;
            if (!Table.NameToArray.TryGetValue(Name, out Array))
            {
                Array = new List<PlayTrackInfoRec>();
                Table.NameToArray.Add(Name, Array);
            }

            Array.Add(Object);
        }

        /* get reference to array of items for given name.  may return NIL if no items */
        public static List<PlayTrackInfoRec> SequencerTableQuery(
            SequencerTableRec Table,
            string Name)
        {
            List<PlayTrackInfoRec> Array;
            Table.NameToArray.TryGetValue(Name, out Array);
            return Array;
        }

        /* remove an object */
        public static void SequencerTableRemoveObject(
            SequencerTableRec Table,
            PlayTrackInfoRec Object)
        {
            foreach (KeyValuePair<string, List<PlayTrackInfoRec>> entry in Table.NameToArray)
            {
                int i = entry.Value.IndexOf(Object);
                if (i >= 0)
                {
                    entry.Value.RemoveAt(i);
                    return;
                }
            }
            // object was not found
            Debug.Assert(false);
            throw new InvalidOperationException();
        }
    }
}
