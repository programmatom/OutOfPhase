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
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public static partial class MIDIImport
    {
        private const int CHUNKSIZE = 128;

        /* one mapping node */
        public class NodeRec
        {
            public QuantEventRec Key;
            public NoteNoteObjectRec Value;
            public NodeRec Next;
        }

        /* chunk of mapping nodes (to reduce heap fragementation) */
        public class ChunkRec
        {
            public ChunkRec Next;
            public NodeRec[] Nodes; // Length == CHUNKSIZE;
        }

        public class TieMappingRec
        {
            public NodeRec List;
            public NodeRec FreeList;
            public ChunkRec ChunkList;
        }

        /* create tie mapping */
        public static TieMappingRec NewTieMapping()
        {
            TieMappingRec Mapping = new TieMappingRec();
            Mapping.List = null;
            Mapping.FreeList = null;
            Mapping.ChunkList = null;
            return Mapping;
        }

        /* make more free list entries available */
        private static void ExpandFreeList(TieMappingRec Mapping)
        {
            ChunkRec NewChunk;
            int Scan;

            NewChunk = new ChunkRec();
            NewChunk.Nodes = new NodeRec[CHUNKSIZE];
            NewChunk.Next = Mapping.ChunkList;
            Mapping.ChunkList = NewChunk;
            for (Scan = CHUNKSIZE - 1; Scan >= 0; Scan -= 1)
            {
                NewChunk.Nodes[Scan] = new NodeRec();
                NewChunk.Nodes[Scan].Next = Mapping.FreeList;
                Mapping.FreeList = NewChunk.Nodes[Scan];
            }
        }

        /* add a quant-note pair */
        public static void TieMappingAddPair(
            TieMappingRec Mapping,
            QuantEventRec QuantEvent,
            NoteNoteObjectRec NoteEvent)
        {
            NodeRec NewNode;

            /* allocate */
            if (Mapping.FreeList == null)
            {
                ExpandFreeList(Mapping);
            }
            /* unlink first mapping entry */
            NewNode = Mapping.FreeList;
            Mapping.FreeList = Mapping.FreeList.Next;
            /* fill in fields */
            NewNode.Key = QuantEvent;
            NewNode.Value = NoteEvent;
            NewNode.Next = Mapping.List;
            Mapping.List = NewNode;
        }

        /* look up note event corresponding to quant event */
        public static NoteNoteObjectRec TieMappingLookup(
            TieMappingRec Mapping,
            QuantEventRec Key)
        {
            NodeRec Scan;

            Scan = Mapping.List;
            while (Scan != null)
            {
                if (Scan.Key == Key)
                {
                    return Scan.Value;
                }
                Scan = Scan.Next;
            }
            Debug.Assert(false); // key not found
            return null;
        }
    }
}
