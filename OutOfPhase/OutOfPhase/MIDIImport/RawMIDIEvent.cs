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
        public class RawMIDIEventRec
        {
            /* absolute time that event occurs at */
            public int Time;

            /* status byte */
            public byte Status;

            /* auxilliary type byte for meta events */
            public byte AuxType;

            /* message bytes */
            public byte[] Message;
        }

        /* create a new MIDI event */
        public static RawMIDIEventRec NewRawMIDIEvent(
            int Time,
            byte Status)
        {
            RawMIDIEventRec RawEvent = new RawMIDIEventRec();

            RawEvent.Message = new byte[0];
            RawEvent.Time = Time;
            RawEvent.Status = Status;
            RawEvent.AuxType = 0;

            return RawEvent;
        }

        /* add another byte to the MIDI event */
        public static void RawMIDIEventAppendByte(
            RawMIDIEventRec RawEvent,
            byte Byte)
        {
            int Length = RawEvent.Message.Length;
            Array.Resize(ref RawEvent.Message, RawEvent.Message.Length + 1);
            RawEvent.Message[Length] = Byte;
        }

        /* get time */
        public static int GetRawMIDIEventTime(RawMIDIEventRec RawEvent)
        {
            return RawEvent.Time;
        }

        /* get status */
        public static byte GetRawMIDIEventStatus(RawMIDIEventRec RawEvent)
        {
            return RawEvent.Status;
        }

        /* get message length */
        public static int GetRawMIDIEventMessageLength(RawMIDIEventRec RawEvent)
        {
            return RawEvent.Message.Length;
        }

        /* get a message byte */
        public static byte GetRawMIDIEventMessageByte(
            RawMIDIEventRec RawEvent,
            int Index)
        {
            return RawEvent.Message[Index];
        }

        /* set auxilliary type byte */
        public static void SetRawMIDIEventTypeByte(
            RawMIDIEventRec RawEvent,
            byte Byte)
        {
            RawEvent.AuxType = Byte;
        }

        /* get auxilliary type byte */
        public static byte GetRawMIDIEventTypeByte(RawMIDIEventRec RawEvent)
        {
            return RawEvent.AuxType;
        }
    }
}
