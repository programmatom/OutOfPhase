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
        /* types of events */
        public enum IntEventType
        {
            eIntervalNoteEvent,
            eIntervalCommentEvent,
        }

        public class IntEventRec
        {
            /* type of event */
            public IntEventType Type;

            /* start time of event, in ticks */
            public int StartTime;

            /* stuff for note events */
            public int Duration; /* in ticks */
            public short MIDIPitch;
            public short MIDIAttackVelocity;
            public short MIDIReleaseVelocity;

            /* stuff for comment events */
            public string CommentString;
        }

        /* create new interval note */
        public static IntEventRec NewIntervalNoteEvent(
            int StartTime,
            int Duration,
            short MIDIPitch,
            short MIDIAttackVelocity,
            short MIDIReleaseVelocity)
        {
            IntEventRec Event = new IntEventRec();
            Event.Type = IntEventType.eIntervalNoteEvent;
            Event.StartTime = StartTime;
            Event.Duration = Duration;
            Event.MIDIPitch = MIDIPitch;
            Event.MIDIAttackVelocity = MIDIAttackVelocity;
            Event.MIDIReleaseVelocity = MIDIReleaseVelocity;
            return Event;
        }

        /* create new interval comment.  comment string is a heap allocated */
        /* non-null-terminated string; onwership of string goes to the object */
        public static IntEventRec NewIntervalCommentEvent(
            int EventTime,
            string CommentString)
        {
            IntEventRec Event = new IntEventRec();
            Event.Type = IntEventType.eIntervalCommentEvent;
            Event.StartTime = EventTime;
            Event.CommentString = CommentString;
            return Event;
        }

        /* get type of event */
        public static IntEventType IntervalEventGetType(IntEventRec Event)
        {
            return Event.Type;
        }

        /* get note event information.  fields may be NIL if value is not needed */
        public static void GetIntervalNoteEventInfo(
            IntEventRec Event,
            out int StartTimeOut,
            out int DurationOut,
            out short MIDIPitchOut,
            out short MIDIAttackVelocityOut,
            out short MIDIReleaseVelocityOut)
        {
            Debug.Assert(Event.Type == IntEventType.eIntervalNoteEvent);
            StartTimeOut = Event.StartTime;
            DurationOut = Event.Duration;
            MIDIPitchOut = Event.MIDIPitch;
            MIDIAttackVelocityOut = Event.MIDIAttackVelocity;
            MIDIReleaseVelocityOut = Event.MIDIReleaseVelocity;
        }

        /* get comment event information.  *MessageActualOut is the actual heap block. */
        /* fields may be NIL if value is not needed */
        public static void GetIntervalCommentEventInfo(
            IntEventRec Event,
            out int EventTimeOut,
            out string MessageActualOut)
        {
            Debug.Assert(Event.Type == IntEventType.eIntervalCommentEvent);
            EventTimeOut = Event.StartTime;
            MessageActualOut = Event.CommentString;
        }

        /* get event time */
        public static int GetIntervalEventTime(IntEventRec Event)
        {
            return Event.StartTime;
        }
    }
}
