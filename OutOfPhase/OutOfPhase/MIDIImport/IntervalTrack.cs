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
        public class IntervalTrackRec
        {
            /* list of IntEventRec objects */
            public List<IntEventRec> EventList;
        }

        /* create a new interval MIDI track */
        public static IntervalTrackRec NewIntervalTrack()
        {
            IntervalTrackRec Track = new IntervalTrackRec();
            Track.EventList = new List<IntEventRec>();
            return Track;
        }

        /* get the number of events in the track */
        public static int GetIntervalTrackLength(IntervalTrackRec Track)
        {
            return Track.EventList.Count;
        }

        /* get an indexed event from the track */
        public static IntEventRec GetIntervalTrackIndexedEvent(
            IntervalTrackRec Track,
            int Index)
        {
            return Track.EventList[Index];
        }

        /* insert new event into track sorted */
        public static void IntervalTrackInsertEventSorted(
            IntervalTrackRec Track,
            IntEventRec Event)
        {
            int OurEventStartTime = GetIntervalEventTime(Event);
            int Scan = Track.EventList.Count - 1;
            while (Scan >= 0)
            {
                IntEventRec OtherEvent = Track.EventList[Scan];
                if (GetIntervalEventTime(OtherEvent) <= OurEventStartTime)
                {
                    Track.EventList.Insert(Scan + 1, Event);
                    return;
                }
                Scan -= 1;
            }
            Track.EventList.Insert(0, Event);
        }
    }
}
