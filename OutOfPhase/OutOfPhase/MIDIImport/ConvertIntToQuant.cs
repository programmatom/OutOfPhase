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
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public static partial class MIDIImport
    {
        /*   All notes in our notation system are integer multiples of six base factors, */
        /* which are 64th/3, 64th/5, 64th/7, 64th/3*1.5, 64th/5*1.5, and 64th/7*1.5. */
        /* (70/(64*3*5*7*2), 42/(64*3*5*7*2), 30/(64*3*5*7*2), 105/(64*3*5*7*2), */
        /* 63/(64*3*5*7*2), 45/(64*3*5*7*2)).  However, it is possible by adding and */
        /* subtracting values to derive other values which may be integer multiples */
        /* of 1/(64*3*5*7*2).  These intervals can not be represented using just a */
        /* sum of the base factors.  Such an interval is one which is not an integer */
        /* linear sum of the base factors (so it can't be decomposed). */
        /*   Doing full quantization with all possible note duration values is very */
        /* difficult since equations of the form ax+by+...=z must be solved for positive */
        /* integer values of x and y given integer values a, b, and z.  if no solution */
        /* exists, then z has to be changed by adding a valid decomposable value to it */
        /* until a solution can be obtained. */
        /*   This problem arises because there are multiple prime bases of the duration */
        /* domain.  To avoid the problem, we will require all durations and start times */
        /* to be an integer multiple of the 64th div3, thus forcing all times */
        /* to have a single base. */
        /*   The 64th div3 is chosen because all standard notes are a multiple of it, */
        /* all dotted notes are a multiple of it, all triplets are a multiple of it, */
        /* and because it is a real note as well.  Note that div5 and div7 notes are not */
        /* representable in this scheme. */

        /* structure used for matching -- maps MIDI clock ticks to duration descriptor */
        [StructLayout(LayoutKind.Auto)]
        public struct TimeMatchRec
        {
            public int Ticks;
            public NoteFlags Descriptor;
        }

        /* quantize the duration */
        private static void QuantizeDuration(
            int Duration,
            int QuarterNoteLen,
            TimeMatchRec[] MatchingTable,
            int MatchingTableLength,
            out NoteFlags QuantizedDurationOut,
            out double QuantizedDurationAdjustOut,
            out int WholeNoteOverflowOut)
        {
            double Tolerance;
            bool Matched;
            int Scan;

            /* initialize */
            QuantizedDurationOut = 0;
            QuantizedDurationAdjustOut = 1;
            WholeNoteOverflowOut = 0;

            /* extract whole note overflow */
            while (Duration > 4 * QuarterNoteLen)
            {
                WholeNoteOverflowOut += 1;
                Duration -= 4 * QuarterNoteLen;
            }

            /* determine closest symbolic duration */
            Tolerance = 0;
            Matched = false;
            while (!Matched)
            {
                /* scan duration matching table to see if we can find a match */
                for (Scan = 0; (!Matched) && (Scan < MatchingTableLength); Scan += 1)
                {
                    if (Math.Abs(MatchingTable[Scan].Ticks - Duration) <= Tolerance)
                    {
                        /* match found */
                        Matched = true;
                        QuantizedDurationOut = MatchingTable[Scan].Descriptor;
                        QuantizedDurationAdjustOut = (double)Duration
                            / (double)MatchingTable[Scan].Ticks;
                    }
                }

                /* if no match was found then increase tolerance */
                if (!Matched)
                {
                    if (Tolerance == 0)
                    {
                        Tolerance = 1;
                    }
                    else
                    {
                        Tolerance = Tolerance * 1.5;
                    }
                }
            }
        }

        /* quantize the start time */
        private static void QuantizeStartTime(
            int StartTime,
            int QuarterNoteLen,
            NoteFlags QuantizedDuration,
            double QuantizedDurationAdjust,
            out FractionRec StartTimeOut,
            out double StartTimeAdjustOut,
            out double WholeNoteStartTimeAdjust)
        {
            uint Denominator;
            double QuantizedStartTime;
            double OrigDuration;
            FractionRec OrigDurationFractional;

            /* start times must be a multiple of the 64th note.  see rationale */
            /* in comment at top of this file. */
            Denominator = 64;

            NoteNoteObjectRec.ConvertDurationFrac(QuantizedDuration, out OrigDurationFractional);
            OrigDuration = (4 * QuarterNoteLen) * QuantizedDurationAdjust
                * FractionRec.Fraction2Double(OrigDurationFractional);

            /* compute start time to nearest division they allow */
            FractionRec.Double2Fraction(StartTime / (double)(QuarterNoteLen * 4), Denominator, out StartTimeOut);

            /* set start time adjust (relative to duration) */
            QuantizedStartTime = FractionRec.Fraction2Double(StartTimeOut) * (4 * QuarterNoteLen);
            StartTimeAdjustOut = (QuantizedStartTime - StartTime) / OrigDuration;
            WholeNoteStartTimeAdjust = (QuantizedStartTime - StartTime) / (4 * QuarterNoteLen);
        }

        /* determine if fraction is an integer multiple of a 64th div3 */
        public static bool FractionIntMultOf64thDiv3(FractionRec Fraction)
        {
            FractionRec SixtyFourthDiv3;
            FractionRec SixtyFourthDiv3Reciprocal;
            FractionRec Product;

            NoteNoteObjectRec.ConvertDurationFrac(NoteFlags.e64thNote | NoteFlags.eDiv3Modifier, out SixtyFourthDiv3);
            Debug.Assert((SixtyFourthDiv3.Integer == 0) && (SixtyFourthDiv3.Fraction != 0)); // "cute problem"
            FractionRec.MakeFraction(out SixtyFourthDiv3Reciprocal, 0, (int)SixtyFourthDiv3.Denominator,
                (int)SixtyFourthDiv3.Fraction); /* reciprocal */
            FractionRec.MultFractions(Fraction, SixtyFourthDiv3Reciprocal, out Product);
            /* if fraction == 0 then it is an even multiple of the 64th note */
            return (Product.Fraction == 0);
        }

        /* determine if note is an integer multiple of a 64th div3 */
        public static bool IntMultOf64thDiv3(NoteFlags Opcode)
        {
            FractionRec OpcodesDuration;

            NoteNoteObjectRec.ConvertDurationFrac(Opcode, out OpcodesDuration);
            return FractionIntMultOf64thDiv3(OpcodesDuration);
        }

        /* convert interval track into quantized track. */
        public static bool ConvertIntervalToQuantized(
            IntervalTrackRec IntervalTrack,
            QuantizedTrackRec QuantizedTrack,
            int MidiQuarterNote)
        {
            int Scan;
            int Limit;
            TimeMatchRec[] MatchingTable = new TimeMatchRec[4/*divisions*/ * 2/*dot*/ * 9/*notetypes*/];
            int MatchingTableLength;

            /* build duration matching table */
            MatchingTableLength = 0;
            for (Scan = 0; Scan < 4/*divisions*/ * 2/*dot*/ * 9/*notetypes*/; Scan += 1)
            {
                double MIDIClocks;
                NoteFlags Descriptor;
                FractionRec DurationFraction;

                /* determine root duration and descriptor */
                switch (Scan % 9)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 0:
                        Descriptor = NoteFlags.e64thNote;
                        break;
                    case 1:
                        Descriptor = NoteFlags.e32ndNote;
                        break;
                    case 2:
                        Descriptor = NoteFlags.e16thNote;
                        break;
                    case 3:
                        Descriptor = NoteFlags.e8thNote;
                        break;
                    case 4:
                        Descriptor = NoteFlags.e4thNote;
                        break;
                    case 5:
                        Descriptor = NoteFlags.e2ndNote;
                        break;
                    case 6:
                        Descriptor = NoteFlags.eWholeNote;
                        break;
                    case 7:
                        Descriptor = NoteFlags.eDoubleNote;
                        break;
                    case 8:
                        Descriptor = NoteFlags.eQuadNote;
                        break;
                }

                /* determine if dot is needed */
                if (((Scan / 9) % 2) != 0)
                {
                    /* dot needed */
                    Descriptor |= NoteFlags.eDotModifier;
                }

                /* determine what division is needed */
                switch (((Scan / 9) / 2) % 4)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 0:
                        break;
                    case 1:
                        Descriptor |= NoteFlags.eDiv3Modifier;
                        break;
                    case 2:
                        Descriptor |= NoteFlags.eDiv5Modifier;
                        break;
                    case 3:
                        Descriptor |= NoteFlags.eDiv7Modifier;
                        break;
                }

                /* how int is this note */
                NoteNoteObjectRec.ConvertDurationFrac(Descriptor, out DurationFraction); /* units of whole notes */
                MIDIClocks = MidiQuarterNote * (4 * FractionRec.Fraction2Double(DurationFraction));

                /* add to table if note can be represented in the timing scheme */
                /* AND only if note is an integer multiple of the 64th div3 (see */
                /* comment at the top of this file for the rationale) */
                if ((MIDIClocks == Math.Floor(MIDIClocks)) && (MIDIClocks >= 1) && IntMultOf64thDiv3(Descriptor))
                {
                    MatchingTable[MatchingTableLength].Ticks = (int)MIDIClocks;
                    MatchingTable[MatchingTableLength].Descriptor = Descriptor;
                    MatchingTableLength += 1;
                }
            }

            /* quantize note events */
            Limit = GetIntervalTrackLength(IntervalTrack);
            for (Scan = 0; Scan < Limit; Scan += 1)
            {
                IntEventRec Event;

                Event = GetIntervalTrackIndexedEvent(IntervalTrack, Scan);
                switch (IntervalEventGetType(Event))
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case IntEventType.eIntervalNoteEvent:
                        {
                            /* input values */
                            int StartTime;
                            int Duration;
                            short MIDIPitch;
                            short MIDIAttackVelocity;
                            short MIDIReleaseVelocity;
                            /* output values */
                            NoteFlags QuantizedDuration;
                            double QuantizedDurationAdjust;
                            int WholeNoteOverflow;
                            FractionRec QuantizedStartTime;
                            double QuantizedStartTimeAdjust;
                            double WholeNoteStartTimeAdjust; /* auxiliary value */
                            /* stuff */
                            QuantEventRec QuantEvent;

                            /* get the information */
                            GetIntervalNoteEventInfo(Event, out StartTime, out Duration, out MIDIPitch,
                                out MIDIAttackVelocity, out MIDIReleaseVelocity);

                            /* quantize duration */
                            QuantizeDuration(Duration, MidiQuarterNote, MatchingTable,
                                MatchingTableLength, out QuantizedDuration, out QuantizedDurationAdjust,
                                out WholeNoteOverflow);
                            Debug.Assert(IntMultOf64thDiv3(QuantizedDuration)); // non-64div3 duration quantization?

                            /* quantize start time */
                            QuantizeStartTime(StartTime, MidiQuarterNote, QuantizedDuration,
                                QuantizedDurationAdjust, out QuantizedStartTime, out QuantizedStartTimeAdjust,
                                out WholeNoteStartTimeAdjust);
                            Debug.Assert(FractionIntMultOf64thDiv3(QuantizedStartTime)); // non-64div3 start time quantization?

                            /* bump start time to end of whole note chain */
                            QuantizedStartTime.Integer += (uint)WholeNoteOverflow;

                            /* create new event & insert into track */
                            QuantEvent = NewQuantizedNoteEvent(QuantizedStartTime,
                                QuantizedStartTimeAdjust, QuantizedDuration, QuantizedDurationAdjust,
                                MIDIPitch, MIDIAttackVelocity, MIDIReleaseVelocity);
                            QuantizedTrackInsertEventSorted(QuantizedTrack, QuantEvent);

                            /* insert whole notes behind the last note in reverse order */
                            while (WholeNoteOverflow > 0)
                            {
                                QuantEventRec Predecessor;

                                /* create preceding whole note */
                                QuantizedStartTime.Integer -= 1;
                                Predecessor = NewQuantizedNoteEvent(QuantizedStartTime,
                                    WholeNoteStartTimeAdjust, NoteFlags.eWholeNote, 1, MIDIPitch,
                                    MIDIAttackVelocity, MIDIReleaseVelocity);
                                QuantizedTrackInsertEventSorted(QuantizedTrack, Predecessor);
                                /* set tie */
                                PutQuantizedEventTieTarget(Predecessor, QuantEvent);
                                QuantEvent = Predecessor;
                                /* step */
                                WholeNoteOverflow -= 1;
                            }
                        }
                        break;
                    case IntEventType.eIntervalCommentEvent:
                        {
                            QuantEventRec QuantEvent;
                            /* input values */
                            int StartTime;
                            string OriginalString;
                            /* output values */
                            FractionRec QuantizedStartTime;

                            /* get the information */
                            GetIntervalCommentEventInfo(Event, out StartTime, out OriginalString);

                            /* compute start time to nearest 64th div3 */
                            FractionRec.Double2Fraction(StartTime / (double)MidiQuarterNote,
                                64 * 3, out QuantizedStartTime);
                            Debug.Assert(FractionIntMultOf64thDiv3(QuantizedStartTime)); //non-64div3 start time quantization?

                            /* create new event & insert into track */
                            QuantEvent = NewQuantizedCommentEvent(QuantizedStartTime, 0, OriginalString);
                            QuantizedTrackInsertEventSorted(QuantizedTrack, QuantEvent);
                        }
                        break;
                }
            }

            return true;
        }
    }
}
