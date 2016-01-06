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
        public enum QuantEventType
        {
            eQuantizedNoteEvent,
            eQuantizedCommentEvent,
        }

        public class QuantEventRec
        {
            /* event type tag */
            public QuantEventType Type;

            /* start time of the event, quantized */
            public FractionRec StartTime;
            public double StartTimeAdjust; /* fractions of note actual duration */

            /* stuff for notes */
            public NoteFlags Duration; /* operators from NoteObject.h */
            public double DurationAdjust; /* multiplicative factor */
            public short MIDIPitch;
            public short MIDIAttackVelocity;
            public short MIDIReleaseVelocity;
            public QuantEventRec TieTarget;

            /* stuff for comment */
            public string CommentString;
        }

        /* create new interval note */
        public static QuantEventRec NewQuantizedNoteEvent(
            FractionRec StartTime,
            double StartTimeAdjust,
            NoteFlags Duration,
            double DurationAdjust,
            short MIDIPitch,
            short MIDIAttackVelocity,
            short MIDIReleaseVelocity)
        {
            QuantEventRec Event = new QuantEventRec();
            Event.Type = QuantEventType.eQuantizedNoteEvent;
            Event.StartTime = StartTime;
            Event.StartTimeAdjust = StartTimeAdjust;
            Event.Duration = Duration;
            Event.DurationAdjust = DurationAdjust;
            Event.MIDIPitch = MIDIPitch;
            Event.MIDIAttackVelocity = MIDIAttackVelocity;
            Event.MIDIReleaseVelocity = MIDIReleaseVelocity;
            Event.TieTarget = null;
            return Event;
        }

        /* create new interval comment.  comment string is a heap allocated */
        /* non-null-terminated string; onwership of string goes to the object */
        public static QuantEventRec NewQuantizedCommentEvent(
            FractionRec StartTime,
            double StartTimeAdjust,
            string CommentString)
        {
            QuantEventRec Event = new QuantEventRec();
            Event.Type = QuantEventType.eQuantizedCommentEvent;
            Event.StartTime = StartTime;
            Event.StartTimeAdjust = StartTimeAdjust;
            Event.CommentString = CommentString;
            return Event;
        }

        /* get type of event */
        public static QuantEventType QuantizedEventGetType(QuantEventRec Event)
        {
            return Event.Type;
        }

        /* get note event information.  fields may be NIL if value is not needed */
        public static void GetQuantizedNoteEventInfo(
            QuantEventRec Event,
            out FractionRec StartTimeOut,
            out double StartTimeAdjustOut,
            out NoteFlags DurationOut,
            out double DurationAdjustOut,
            out short MIDIPitchOut,
            out short MIDIAttackVelocityOut,
            out short MIDIReleaseVelocityOut)
        {
            Debug.Assert(Event.Type == QuantEventType.eQuantizedNoteEvent);
            StartTimeOut = Event.StartTime;
            StartTimeAdjustOut = Event.StartTimeAdjust;
            DurationOut = Event.Duration;
            DurationAdjustOut = Event.DurationAdjust;
            MIDIPitchOut = Event.MIDIPitch;
            MIDIAttackVelocityOut = Event.MIDIAttackVelocity;
            MIDIReleaseVelocityOut = Event.MIDIReleaseVelocity;
        }

        /* get comment event information.  *MessageActualOut is the actual heap block. */
        /* fields may be NIL if value is not needed */
        public static void GetQuantizedCommentEventInfo(
            QuantEventRec Event,
            out FractionRec StartTimeOut,
            out double StartTimeAdjustOut,
            out string CommentStringOut)
        {
            Debug.Assert(Event.Type == QuantEventType.eQuantizedCommentEvent);
            StartTimeOut = Event.StartTime;
            StartTimeAdjustOut = Event.StartTimeAdjust;
            CommentStringOut = Event.CommentString;
        }

        /* get event time */
        public static FractionRec GetQuantizedEventTime(QuantEventRec Event)
        {
            return Event.StartTime;
        }

        /* set tie target */
        public static void PutQuantizedEventTieTarget(
            QuantEventRec Event,
            QuantEventRec TieTarget)
        {
            Debug.Assert(Event.Type == QuantEventType.eQuantizedNoteEvent);
            Event.TieTarget = TieTarget;
        }

        /* get the tie target */
        public static QuantEventRec GetQuantizedEventTieTarget(QuantEventRec Event)
        {
            Debug.Assert(Event.Type == QuantEventType.eQuantizedNoteEvent);
            return Event.TieTarget;
        }
    }
}
