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
        public class RawMIDITrackRec
        {
            public List<RawMIDIEventRec> EventList;
        }

        /* create a new raw MIDI track */
        public static RawMIDITrackRec NewRawMIDITrack()
        {
            RawMIDITrackRec RawTrack = new RawMIDITrackRec();
            RawTrack.EventList = new List<RawMIDIEventRec>();
            return RawTrack;
        }

        /* append a new MIDI event to the track */
        public static void RawMIDITrackAppendEvent(
            RawMIDITrackRec RawTrack,
            RawMIDIEventRec Event)
        {
            RawTrack.EventList.Add(Event);
        }

        /* get the number of events in the track */
        public static int GetRawMIDITrackLength(RawMIDITrackRec RawTrack)
        {
            return RawTrack.EventList.Count;
        }

        /* get an indexed event from the track */
        public static RawMIDIEventRec GetRawMIDITrackIndexedEvent(
            RawMIDITrackRec RawTrack,
            int Index)
        {
            return RawTrack.EventList[Index];
        }
    }
}
