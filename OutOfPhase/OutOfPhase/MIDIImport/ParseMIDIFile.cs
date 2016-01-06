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
        /* possible error codes from parse */
        public enum MIDIParseErrorType
        {
            eMIDIParseNoError,
            eMIDIParseFileReadError,
            eMIDIParseBadFormat,
        }

        /* attempt to parse a MIDI file */
        public static MIDIParseErrorType ParseMIDIFile(
            BinaryReader InputFile,
            RawMIDIScoreRec Score)
        {
            string CharBuff;
            int HeaderChunkLength;
            short FileFormat;
            short TrackCount;
            ushort TimingCode;

            /* parse header */

            /* 'MThd' */
            CharBuff = InputFile.ReadFixedStringASCII(4);
            if (!String.Equals(CharBuff, "MThd"))
            {
                return MIDIParseErrorType.eMIDIParseBadFormat;
            }

            /* 4-byte big endian header length (minus 8 for MThd & this length) */
            HeaderChunkLength = InputFile.ReadInt32BigEndian();
            if (HeaderChunkLength != 6)
            {
                return MIDIParseErrorType.eMIDIParseBadFormat;
            }

            /* 2-byte unsigned big endian format indicator */
            /*   0 = type 0 format (single multi-channel track) */
            /*   1 = type 1 format (multiple tracks in parallel) */
            /*   2 = type 2 format (multiple tracks in sequence) */
            FileFormat = InputFile.ReadInt16BigEndian();
            switch (FileFormat)
            {
                default:
                    return MIDIParseErrorType.eMIDIParseBadFormat;
                    break;
                case 0:
                    RawMIDIScoreSetFormatType(Score, MIDIFileFormatType.eMIDIFormat0SingleTrack);
                    break;
                case 1:
                    RawMIDIScoreSetFormatType(Score, MIDIFileFormatType.eMIDIFormat1ParallelTracks);
                    break;
                case 2:
                    RawMIDIScoreSetFormatType(Score, MIDIFileFormatType.eMIDIFormat2SequentialTracks);
                    break;
            }

            /* 2-byte unsigned big endian track count */
            TrackCount = InputFile.ReadInt16BigEndian();

            /* 2-byte timing code */
            /*   if MSB in 0..127, then value is ticks per quarter note */
            /*  else */
            /*   if MSB in 128..255, then value is real time as follows: */
            /*     negation of MSB (256 - MSB) = frames per second */
            /*     LSB = ticks per frame */
            /*   officially, MSB should be -24, -25, -29, or -30 */
            TimingCode = InputFile.ReadUInt16BigEndian();
            if ((((TimingCode >> 8) & 0xff) >= 0) && (((TimingCode >> 8) & 0xff) <= 127))
            {
                RawMIDIScoreSetMeteredTime(Score, TimingCode);
            }
            else
            {
                RawMIDIScoreSetRealTime(Score, 256 - ((TimingCode >> 8) & 0xff), TimingCode & 0xff);
            }

            /* parse tracks */

            /* loop for all tracks */
            while (TrackCount > 0)
            {
                RawMIDITrackRec Track;
                int TrackChunkLength;
                int TimeAccumulator;
                byte RunningStatus = 0;
                bool RunningStatusValid;

                /* allocate track */
                Track = NewRawMIDITrack();
                RawMIDIScoreAppendTrack(Score, Track);

                /* get chunk type -- skip unknown chunk types */
                do
                {
                    /* get header string */
                    CharBuff = InputFile.ReadFixedStringASCII(4);
                    /* 4-byte big endian header length (minus 8 for header & this) */
                    TrackChunkLength = InputFile.ReadInt32BigEndian();
                    /* skip unknown chunks */
                    if (!String.Equals(CharBuff, "MTrk"))
                    {
                        while (TrackChunkLength > 0)
                        {
                            InputFile.ReadByte();
                            TrackChunkLength -= 1;
                        }
                    }
                } while (!String.Equals(CharBuff, "MTrk"));

                /* parse track contents */

                /* while there is stuff in the track to read */
                TimeAccumulator = 0;
                RunningStatusValid = false;
                while (TrackChunkLength > 0)
                {
                    int DeltaTime;
                    byte UnsignedChar;
                    RawMIDIEventRec Event;

                    /* parse the delta time */
                    DeltaTime = 0;
                    do
                    {
                        UnsignedChar = InputFile.ReadByte();
                        TrackChunkLength -= 1;
                        DeltaTime = (DeltaTime << 7) | (UnsignedChar & 0x7f);
                    } while ((UnsignedChar & 0x80) != 0);
                    /* find out what the absolute time is */
                    TimeAccumulator += DeltaTime;

                    /* get the next byte */
                    UnsignedChar = InputFile.ReadByte();
                    TrackChunkLength -= 1;

                    /* is this byte a status byte? (yes if high bit set) */
                    if (0 != (0x80 & UnsignedChar))
                    {
                        /* it is a status byte */
                        RunningStatusValid = true;
                        RunningStatus = UnsignedChar;
                        /* read another byte since we consumed this one */
                        UnsignedChar = InputFile.ReadByte();
                        TrackChunkLength -= 1;
                    }
                    else
                    {
                        if (!RunningStatusValid)
                        {
                            /* first byte must be a status byte */
                            return MIDIParseErrorType.eMIDIParseBadFormat;
                        }
                    }

                    /* allocate event */
                    Event = NewRawMIDIEvent(TimeAccumulator, RunningStatus);
                    RawMIDITrackAppendEvent(Track, Event);

                    /* handle event */
                    switch ((RunningStatus >> 4) & 0x0f)
                    {
                        default:
                            /* unknown status */
                            return MIDIParseErrorType.eMIDIParseBadFormat;

                        /* 1000nnnn:  note off event for channel nnnn followed by 2 data bytes */
                        /* 1001nnnn:  note on event for channel nnnn followed by 2 data bytes */
                        /* 1010nnnn:  polyphonic key pressure/after touch followed by */
                        /*   2 data bytes */
                        /* 1011nnnn:  control change followed by 2 data bytes */
                        /* 1110nnnn:  pitch bend change followed by 2 data bytes */
                        case 0x08:
                        case 0x09:
                        case 0x0a:
                        case 0x0b:
                        case 0x0e:
                            RawMIDIEventAppendByte(Event, UnsignedChar);
                            UnsignedChar = InputFile.ReadByte();
                            TrackChunkLength -= 1;
                            RawMIDIEventAppendByte(Event, UnsignedChar);
                            break;

                        /* 1100nnnn:  program change followed by 1 data byte */
                        /* 1101nnnn:  channel pressure/after touch followed by 1 data byte */
                        case 0x0c:
                        case 0x0d:
                            RawMIDIEventAppendByte(Event, UnsignedChar);
                            break;

                        /* assorted things */
                        case 0x0f:
                            switch (RunningStatus & 0x0f)
                            {
                                default:
                                    return MIDIParseErrorType.eMIDIParseBadFormat;

                                /* 11110000:  initial or solitary SYSEX message.  followed by a */
                                /*   variable length field.  solitary message is terminated by */
                                /*   0xf7 (which is not part of the message, but is included in */
                                /*   length).  continuing message does not end with 0xf7. */
                                /*   NOTE:  SYSEX also cancels the running status. */
                                /* 11110111:  continuing SYSEX message.  same format as initial. */
                                case 0x00:
                                    {
                                        int SysExLength;

                                        /* cancel running status */
                                        RunningStatusValid = false;

                                        /* read SYSEX length */
                                        SysExLength = UnsignedChar & 0x7f;
                                        if (0 != (UnsignedChar & 0x80))
                                        {
                                            do
                                            {
                                                UnsignedChar = InputFile.ReadByte();
                                                TrackChunkLength -= 1;
                                                SysExLength = (SysExLength << 7) | (UnsignedChar & 0x7f);
                                            } while ((UnsignedChar & 0x80) != 0);
                                        }

                                        /* read in bytes for SYSEX message */
                                        while (SysExLength > 0)
                                        {
                                            UnsignedChar = InputFile.ReadByte();
                                            TrackChunkLength -= 1;
                                            RawMIDIEventAppendByte(Event, UnsignedChar);
                                            SysExLength -= 1;
                                        }
                                    }
                                    break;

                                /* system common messages */
                                /* 11110010:  song position pointer, followed by 2 data bytes */
                                /* 11110011:  song select, followed by 2 data bytes */
                                case 0x02:
                                case 0x03:
                                    RawMIDIEventAppendByte(Event, UnsignedChar);
                                    UnsignedChar = InputFile.ReadByte();
                                    TrackChunkLength -= 1;
                                    RawMIDIEventAppendByte(Event, UnsignedChar);
                                    break;

                                /* 11110110:  tune request, no data */
                                case 0x06:
                                    break;

                                /* 11111111:  meta event.  followed by type byte (0..127) and */
                                /*   then variable length specifier, then data bytes. */
                                case 0x0f:
                                    {
                                        int MetaLength;

                                        /* cancel running status */
                                        RunningStatusValid = false;

                                        /* put meta event type */
                                        RawMIDIEventAppendByte(Event, UnsignedChar);
                                        SetRawMIDIEventTypeByte(Event, UnsignedChar);

                                        /* get length of meta event */
                                        MetaLength = 0;
                                        do
                                        {
                                            UnsignedChar = InputFile.ReadByte();
                                            TrackChunkLength -= 1;
                                            MetaLength = (MetaLength << 7) | (UnsignedChar & 0x7f);
                                        } while ((UnsignedChar & 0x80) != 0);

                                        /* read all meta event bytes in */
                                        while (MetaLength > 0)
                                        {
                                            UnsignedChar = InputFile.ReadByte();
                                            TrackChunkLength -= 1;
                                            RawMIDIEventAppendByte(Event, UnsignedChar);
                                            MetaLength -= 1;
                                        }
                                    }
                                    break;
                            }
                            break;
                    }
                }

                /* end of track parsing */

                /* decrement count */
                TrackCount -= 1;
            }

            /* done */
            return MIDIParseErrorType.eMIDIParseNoError;
        }
    }
}
