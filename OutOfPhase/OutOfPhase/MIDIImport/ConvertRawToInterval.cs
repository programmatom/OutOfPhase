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
        private const int SYSEXBYTESPERLINE = 8;

        /* record for note event correlation */
        public struct PitchEventRec
        {
            /* flag indicating whether note is on or off */
            public bool NoteOn;

            /* information about state */
            public short StartVelocity;
            public int StartTime;
        }

        private const string HexBuf = "0123456789ABCDEF";

        /* convert raw track into an interval track.  MIDITrackNum should be in 1..16 */
        /* *KeepFlag is set if there were real events in this track. */
        public static void ConvertRawToInterval(
            RawMIDITrackRec RawTrack,
            IntervalTrackRec IntervalTrack,
            short MIDITrackNum,
            out bool KeepFlag)
        {
            int Limit;
            PitchEventRec[] EventTracker = new PitchEventRec[128];

            Debug.Assert((MIDITrackNum >= 1) || (MIDITrackNum <= 16));
            MIDITrackNum -= 1;
            KeepFlag = false;

            /* initialize correlation tracker */
            for (int i = 0; i < 128; i += 1)
            {
                EventTracker[i].NoteOn = false;
            }

            /* scan all low level events */
            Limit = GetRawMIDITrackLength(RawTrack);
            for (int i = 0; i < Limit; i += 1)
            {
                byte TypeByte;

                RawMIDIEventRec RawEvent = GetRawMIDITrackIndexedEvent(RawTrack, i);
                byte StatusByte = GetRawMIDIEventStatus(RawEvent);
                switch ((StatusByte >> 4) & 0x0f)
                {
                    /* ignore events we don't know about */
                    default:
                        break;

                    /* 1000nnnn:  note off event for channel nnnn followed by 2 data bytes */
                    case 0x08:
                    NoteOffPoint:
                        if ((StatusByte & 0x0f) == MIDITrackNum)
                        {
                            short Pitch;
                            short ReleaseVelocity;

                            /* data byte 1:  velocity 0..127 */
                            /* data byte 2:  pitch 0..127 */
                            if (GetRawMIDIEventMessageLength(RawEvent) >= 2)
                            {
                                ReleaseVelocity = (short)(0x7f & GetRawMIDIEventMessageByte(RawEvent, 1));
                            }
                            else
                            {
                                ReleaseVelocity = 0;
                            }
                            if (GetRawMIDIEventMessageLength(RawEvent) >= 1)
                            {
                                Pitch = (short)(0x7f & GetRawMIDIEventMessageByte(RawEvent, 0));
                            }
                            else
                            {
                                Pitch = 0;
                            }
                            if (EventTracker[Pitch].NoteOn)
                            {
                                IntEventRec IntervalEvent = NewIntervalNoteEvent(
                                    EventTracker[Pitch].StartTime,
                                    GetRawMIDIEventTime(RawEvent) - EventTracker[Pitch].StartTime,
                                    Pitch,
                                    EventTracker[Pitch].StartVelocity,
                                    ReleaseVelocity);
                                IntervalTrackInsertEventSorted(IntervalTrack, IntervalEvent);
                                EventTracker[Pitch].NoteOn = false;
                            }
                            else
                            {
                                /* no note-on event? */
                            }
                            KeepFlag = true;
                        }
                        break;

                    /* 1001nnnn:  note on event for channel nnnn followed by 2 data bytes */
                    case 0x09:
                        if ((StatusByte & 0x0f) == MIDITrackNum)
                        {
                            short Pitch;
                            short Velocity;

                            /* data byte 1:  velocity 0..127 */
                            /* data byte 2:  pitch 0..127 */
                            if (GetRawMIDIEventMessageLength(RawEvent) >= 2)
                            {
                                Velocity = (short)(0x7f & GetRawMIDIEventMessageByte(RawEvent, 1));
                            }
                            else
                            {
                                Velocity = 64;
                            }
                            if (Velocity == 0)
                            {
                                /* note on velocity 0 actually means note off */
                                goto NoteOffPoint;
                            }
                            if (GetRawMIDIEventMessageLength(RawEvent) >= 1)
                            {
                                Pitch = (short)(0x7f & GetRawMIDIEventMessageByte(RawEvent, 0));
                            }
                            else
                            {
                                Pitch = 0;
                            }
                            if (!EventTracker[Pitch].NoteOn)
                            {
                                EventTracker[Pitch].StartTime = GetRawMIDIEventTime(RawEvent);
                                EventTracker[Pitch].StartVelocity = Velocity;
                                EventTracker[Pitch].NoteOn = true;
                            }
                            else
                            {
                                /* note already on? */
                            }
                            KeepFlag = true;
                        }
                        break;

                    /* 1010nnnn:  polyphonic key pressure/after touch followed by */
                    /*   2 data bytes */
                    case 0x0a:
                        break;

                    /* 1011nnnn:  control change followed by 2 data bytes */
                    case 0x0b:
                        break;

                    /* 1110nnnn:  pitch bend change followed by 2 data bytes */
                    case 0x0e:
                        break;

                    /* 1100nnnn:  program change followed by 1 data byte */
                    case 0x0c:
                        break;

                    /* 1101nnnn:  channel pressure/after touch followed by 1 data byte */
                    case 0x0d:
                        break;

                    /* assorted thangs */
                    case 0x0f:
                        switch (StatusByte & 0x0f)
                        {
                            default:
                                break;

                            /* 11110000:  initial or solitary SYSEX message.  followed by a */
                            /*   variable length field.  solitary message is terminated by */
                            /*   0xf7 (which is not part of the message, but is included in */
                            /*   length).  continuing message does not end with 0xf7. */
                            /*   NOTE:  SYSEX also cancels the running status. */
                            /* 11110111:  continuing SYSEX message.  same format as initial. */
                            case 0x00:
                            case 0x07:
                                {
                                    StringBuilder sb = new StringBuilder();
                                    if ((StatusByte & 0x0f) == 0x00)
                                    {
                                        sb.AppendLine("System Exclusive $F0");
                                    }
                                    else
                                    {
                                        sb.AppendLine("System Exclusive $F7");
                                    }
                                    int c = GetRawMIDIEventMessageLength(RawEvent);
                                    for (int j = 0; j < c; j += 1)
                                    {
                                        byte b = GetRawMIDIEventMessageByte(RawEvent, j);
                                        sb.Append('$');
                                        sb.Append(HexBuf[(b >> 4) & 0x0f]);
                                        sb.Append(HexBuf[b & 0x0f]);
                                        if (((j + 1) % SYSEXBYTESPERLINE) == 0)
                                        {
                                            sb.AppendLine();
                                        }
                                        else
                                        {
                                            sb.Append(' ');
                                        }
                                    }
                                    IntEventRec IntervalEvent = NewIntervalCommentEvent(GetRawMIDIEventTime(RawEvent), sb.ToString());
                                    IntervalTrackInsertEventSorted(IntervalTrack, IntervalEvent);
                                }
                                break;

                            /* 11110010:  song position pointer, followed by 2 data bytes */
                            case 0x02:
                                {
                                    StringBuilder String;
                                    string Buffer;
                                    IntEventRec IntervalEvent;

                                    String = new StringBuilder();
                                    String.Append("Song Position Pointer" + Environment.NewLine);
                                    if (GetRawMIDIEventMessageLength(RawEvent) >= 2)
                                    {
                                        String.Append((int)((GetRawMIDIEventMessageByte(RawEvent, 0) & 0x7f)
                                            | ((GetRawMIDIEventMessageByte(RawEvent, 1) & 0x7f) << 7)));
                                    }
                                    else
                                    {
                                        String.Append((int)0);
                                    }
                                    Buffer = String.ToString();
                                    IntervalEvent = NewIntervalCommentEvent(GetRawMIDIEventTime(RawEvent), Buffer);
                                    IntervalTrackInsertEventSorted(IntervalTrack, IntervalEvent);
                                }
                                break;

                            /* 11110011:  song select, followed by 2 data bytes */
                            case 0x03:
                                {
                                    StringBuilder String;
                                    string Buffer;
                                    IntEventRec IntervalEvent;

                                    String = new StringBuilder();
                                    String.Append("Song Select" + Environment.NewLine);
                                    if (GetRawMIDIEventMessageLength(RawEvent) >= 2)
                                    {
                                        String.Append((int)((GetRawMIDIEventMessageByte(RawEvent, 0) & 0x7f)
                                            | ((GetRawMIDIEventMessageByte(RawEvent, 1) & 0x7f) << 7)));
                                    }
                                    else
                                    {
                                        String.Append((int)0);
                                    }
                                    Buffer = String.ToString();
                                    IntervalEvent = NewIntervalCommentEvent(GetRawMIDIEventTime(RawEvent), Buffer);
                                    IntervalTrackInsertEventSorted(IntervalTrack, IntervalEvent);
                                }
                                break;

                            /* 11110110:  tune request, no data */
                            case 0x06:
                                {
                                    string String;
                                    IntEventRec IntervalEvent;

                                    String = "Tune Request";
                                    IntervalEvent = NewIntervalCommentEvent(GetRawMIDIEventTime(RawEvent), String);
                                    IntervalTrackInsertEventSorted(IntervalTrack, IntervalEvent);
                                }
                                break;

                            /* 11111111:  meta event.  followed by type byte (0..127) and */
                            /*   then variable length specifier, then data bytes. */
                            case 0x0f:
                                /* check out the auxilliary type */
                                TypeByte = GetRawMIDIEventTypeByte(RawEvent);
                                switch (TypeByte)
                                {
                                    default:
                                        break;

                                    /* (FF) 01 <len> <text>:  text event */
                                    /* (FF) 02 <len> <text>:  copyright */
                                    /* (FF) 03 <len> <text>:  sequence/track name */
                                    /* (FF) 04 <len> <text>:  instrument name */
                                    /* (FF) 05 <len> <text>:  lyric */
                                    /* (FF) 06 <len> <text>:  marker */
                                    /* (FF) 07 <len> <text>:  cue point */
                                    case 0x01:
                                    case 0x02:
                                    case 0x03:
                                    case 0x04:
                                    case 0x05:
                                    case 0x06:
                                    case 0x07:
                                        {
                                            string String;
                                            char[] Text;
                                            IntEventRec IntervalEvent;
                                            string Combined;
                                            int Length;
                                            int Scan2;

                                            switch (GetRawMIDIEventTypeByte(RawEvent))
                                            {
                                                default:
                                                    Debug.Assert(false);
                                                    throw new InvalidOperationException();

                                                /* (FF) 01 <len> <text>:  text event */
                                                /* (FF) 02 <len> <text>:  copyright */
                                                /* (FF) 03 <len> <text>:  sequence/track name */
                                                /* (FF) 04 <len> <text>:  instrument name */
                                                /* (FF) 05 <len> <text>:  lyric */
                                                /* (FF) 06 <len> <text>:  marker */
                                                /* (FF) 07 <len> <text>:  cue point */
                                                case 0x01:
                                                    String = "Comment" + Environment.NewLine;
                                                    break;
                                                case 0x02:
                                                    String = "Copright" + Environment.NewLine;
                                                    break;
                                                case 0x03:
                                                    String = "Track Name" + Environment.NewLine;
                                                    break;
                                                case 0x04:
                                                    String = "Instrument Name" + Environment.NewLine;
                                                    break;
                                                case 0x05:
                                                    String = "Lyric" + Environment.NewLine;
                                                    break;
                                                case 0x06:
                                                    String = "Marker" + Environment.NewLine;
                                                    break;
                                                case 0x07:
                                                    String = "Cue Point" + Environment.NewLine;
                                                    break;
                                            }
                                            Length = GetRawMIDIEventMessageLength(RawEvent);
                                            if (Length > 0)
                                            {
                                                /* don't deal with the first character since it */
                                                /* isn't part of the text */
                                                Length -= 1;
                                            }
                                            Text = new char[Length + Environment.NewLine.Length];
                                            for (Scan2 = 0; Scan2 < Length; Scan2 += 1)
                                            {
                                                Text[Scan2] = (char)GetRawMIDIEventMessageByte(RawEvent, Scan2 + 1/*skip first character*/);
                                            }
                                            Text[Length] = Environment.NewLine[0];
                                            Text[Length + 1] = Environment.NewLine[1];
                                            Combined = String.Concat(String, new String(Text));
                                            IntervalEvent = NewIntervalCommentEvent(GetRawMIDIEventTime(RawEvent), Combined);
                                            IntervalTrackInsertEventSorted(IntervalTrack, IntervalEvent);
                                        }
                                        break;

                                    /* (FF) 00 <02> ss ss:  sequence number */
                                    /* (FF) 2F <00>:  end of track */
                                    /* (FF) 51 <03> tt tt tt:  set tempo (microsec/quarter note) */
                                    /* (FF) 54 <05> hr mn se fr ff:  SMTPE offset */
                                    /* (FF) 58 <04> nn dd cc bb: time signature */
                                    /*    nn/dd is numerator/denominator.  dd is a negative power */
                                    /*    of two (2=quarter note, 3=eighth note). */
                                    /*    cc = midi clocks per metronome click. */
                                    /*    bb = number of 32nd notes per quarter note (24 clocks) */
                                    /* (FF) 59 <02> sf mi:  key signature */
                                    /*    sf: -7..-1 flats, 0 = normal, 1..7 = sharps */
                                    /*    mi = 0 or major, 1 for minor */
                                    /* (FF) 7F <len> <data>:  sequencer meta event */
                                }
                                break;
                        }
                        break;
                }
            }
        }
    }
}
