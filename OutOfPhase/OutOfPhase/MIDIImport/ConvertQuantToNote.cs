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
        private const double LN127OVERLN2 = 6.98868468677216585;

        private const int MIDIC = 60;

        /* structure for building list of opcode and duration values */
        public struct OpDurRec
        {
            /* opcode for the note */
            public NoteFlags Opcode;

            /* fractional duration described by the opcode */
            public FractionRec Duration;
        }


        /* compute duration opcode for maximum atomic note that is less than or */
        /* equal to the specified fractional duration */
        private static NoteFlags GetMaxDurationOpcode(
            FractionRec TargetDuration,
            out FractionRec ComputedDuration,
            OpDurRec[] OpcodeDurationTable,
            int OpcodeDurationTableLength)
        {
            int Scan;

            for (Scan = 0; Scan < OpcodeDurationTableLength; Scan += 1)
            {
                if (!FractionRec.FracGreaterThan(OpcodeDurationTable[Scan].Duration, TargetDuration))
                {
                    ComputedDuration = OpcodeDurationTable[Scan].Duration;
                    return OpcodeDurationTable[Scan].Opcode;
                }
            }
            /* probably the start time quantizer is generating intervals that */
            /* can't be represented. */
            Debug.Assert(false); // couldn't find available note
            ComputedDuration = OpcodeDurationTable[OpcodeDurationTableLength - 1].Duration;
            return OpcodeDurationTable[OpcodeDurationTableLength - 1].Opcode;
        }

        /* insert rests */
        private static void InsertRests(
            FractionRec Now,
            FractionRec Target,
            TrackObjectRec NoteTrack,
            OpDurRec[] OpcodeDurationTable,
            int OpcodeDurationTableLength)
        {
            while (FractionRec.FracGreaterThan(Target, Now))
            {
                FractionRec Difference;
                NoteFlags Opcode;
                FractionRec OpcodesDuration;
                NoteNoteObjectRec Note;
                FrameObjectRec Frame;

                /* how much time left */
                FractionRec.SubFractions(Target, Now, out Difference);
                /* search for appropriate opcode */
                Opcode = GetMaxDurationOpcode(Difference, out OpcodesDuration, OpcodeDurationTable, OpcodeDurationTableLength);

                /* add duration to Now */
                FractionRec.AddFractions(Now, OpcodesDuration, out Now);

                /* create the note */
                Note = new NoteNoteObjectRec(NoteTrack);
                Note.PutNoteDuration(Opcode & NoteFlags.eDurationMask);
                Note.PutNoteDurationDivision(Opcode & NoteFlags.eDivisionMask);
                Note.PutNoteDotStatus((Opcode & NoteFlags.eDotModifier) != 0);
                Note.PutNotePitch(Constants.CENTERNOTE);
                Note.PutNoteIsItARest(true);
                /* create the frame */
                Frame = new FrameObjectRec();
                Frame.Add(Note);
                NoteTrack.FrameArray.Add(Frame);
            }
        }

        /* fill in opcode table and sort for largest-duration first */
        private static void InitializeOpcodeDurationTable(
            OpDurRec[] Table, // [4/*divisions*/ * 2/*dot*/ * 9/*notetypes*/]
            out int TableLengthOut)
        {
            Debug.Assert(Table.Length == 4/*divisions*/ * 2/*dot*/ * 9/*notetypes*/);
            /* scan all possible notes */
            TableLengthOut = 0;
            for (int Scan = 0; Scan < 4/*divisions*/ * 2/*dot*/ * 9/*notetypes*/; Scan += 1)
            {
                NoteFlags Descriptor;
                FractionRec DurationFraction;
                int InsertScan;
                int EndScan;

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

                /* don't use ugly rests, except we need the 64th div3 (see below) */
                if (!(((Descriptor & NoteFlags.eDivisionMask) == NoteFlags.eDiv5Modifier)
                    || ((Descriptor & NoteFlags.eDivisionMask) == NoteFlags.eDiv7Modifier)
                    || ((Descriptor & NoteFlags.eDotModifier) != 0)
                    || (((Descriptor & NoteFlags.eDivisionMask) == NoteFlags.eDiv3Modifier)
                        && ((Descriptor & NoteFlags.eDurationMask) != NoteFlags.e64thNote))))
                {
                    /* don't use things that aren't multiples of a 64th div3 note.  see the */
                    /* comment at the top of ConvertIntToQuant for the rationale. */
                    if (IntMultOf64thDiv3(Descriptor))
                    {
                        /* get duration, units of whole notes */
                        NoteNoteObjectRec.ConvertDurationFrac(Descriptor, out DurationFraction);

                        /* add duration to table */
                        InsertScan = 0;
                        while (InsertScan < TableLengthOut)
                        {
                            if (FractionRec.FracGreaterThan(DurationFraction, Table[InsertScan].Duration))
                            {
                                /* insert here */
                                goto InsertNowPoint;
                            }
                            else if (FractionRec.FractionsEqual(DurationFraction, Table[InsertScan].Duration))
                            {
                                /* redundant */
                                goto DoneInsertingPoint;
                            }
                            else
                            {
                                /* try the next one */
                                InsertScan += 1;
                            }
                        }
                    /* this gets executed to insert a new value before Table[InsertScan] */
                    InsertNowPoint:
                        for (EndScan = TableLengthOut - 1; EndScan >= InsertScan; EndScan -= 1)
                        {
                            Table[EndScan + 1] = Table[EndScan];
                        }
                        Table[InsertScan].Opcode = Descriptor;
                        Table[InsertScan].Duration = DurationFraction;
                        TableLengthOut++;

                    DoneInsertingPoint:
                        ;
                    }
                }
            }

            /* verify sort order */
#if DEBUG
            for (int Scan = 0; Scan < TableLengthOut - 1; Scan += 1)
            {
                if (!FractionRec.FracGreaterThan(Table[Scan].Duration, Table[Scan + 1].Duration))
                {
                    // sort failure
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
            }
#endif
        }

        /* convert quantized track into native track */
        public static void ConvertQuantToNote(
            QuantizedTrackRec QuantizedTrack,
            TrackObjectRec NoteTrack)
        {
            FractionRec CurrentTime;
            int Index;
            int Limit;
            List<QuantEventRec> FrameArray;
            TieMappingRec TieMapping;
            int OpcodeDurationTableLength;
            OpDurRec[] OpcodeDurationTable = new OpDurRec[4/*divisions*/ * 2/*dot*/ * 9/*notetypes*/];

            /* initialize variables */
            InitializeOpcodeDurationTable(OpcodeDurationTable, out OpcodeDurationTableLength);
            CurrentTime.Integer = 0;
            CurrentTime.Fraction = 0;
            CurrentTime.Denominator = 1;
            FrameArray = new List<QuantEventRec>();
            TieMapping = NewTieMapping();
            Limit = GetQuantizedTrackLength(QuantizedTrack);
            Index = 0;

            /* iterate over variables */
            while (Index < Limit)
            {
                FractionRec NextTime;
                QuantEventRec QuantEvent;
                bool Continue;
                int InspectScan;

                /* reset frame array */
                FrameArray.Clear();

                /* get the start time of the next available event */
                QuantEvent = GetQuantizedTrackIndexedEvent(QuantizedTrack, Index);
                NextTime = GetQuantizedEventTime(QuantEvent);
                Debug.Assert(FractionIntMultOf64thDiv3(NextTime)); // non-64div3 start time quantization?

                /* sanity check */
                Debug.Assert(!FractionRec.FracGreaterThan(CurrentTime, NextTime)); // next time inconsistency

                /* get all events starting at this time into FrameArray */
                Continue = true;
                while (Continue && (Index < Limit))
                {
                    FractionRec EventTime;

                    /* get the event */
                    QuantEvent = GetQuantizedTrackIndexedEvent(QuantizedTrack, Index);
                    EventTime = GetQuantizedEventTime(QuantEvent);
                    Debug.Assert(FractionIntMultOf64thDiv3(EventTime)); // non-64div3 start time quantization?
                    if (FractionRec.FractionsEqual(EventTime, NextTime))
                    {
                        /* add event to the list */
                        FrameArray.Add(QuantEvent);
                        /* go past this event */
                        Index += 1;
                    }
                    else
                    {
                        /* end hit so stop */
                        Continue = false;
                    }
                }

                /* insert rests to bring current time up to next time */
                InsertRests(CurrentTime, NextTime, NoteTrack, OpcodeDurationTable, OpcodeDurationTableLength);

                /* remove command events from list */
                InspectScan = 0;
                while (InspectScan < FrameArray.Count)
                {
                    /* get the event */
                    QuantEvent = FrameArray[InspectScan];
                    /* determine if event is a command */
                    if (QuantizedEventGetType(QuantEvent) == QuantEventType.eQuantizedNoteEvent)
                    {
                        /* note events should be skipped */
                        InspectScan += 1;
                    }
                    else
                    {
                        /* command events should be handled */
                        switch (QuantizedEventGetType(QuantEvent))
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case QuantEventType.eQuantizedCommentEvent:
                                {
                                    string CommentString;
                                    CommandNoteObjectRec Note;
                                    FrameObjectRec Frame;
                                    FractionRec unusedf;
                                    double unusedd;

                                    GetQuantizedCommentEventInfo(QuantEvent, out unusedf, out unusedd, out CommentString);
                                    Note = new CommandNoteObjectRec(NoteTrack);
                                    Note.PutCommandStringArg1(CommentString);
                                    Note.PutCommandOpcode(NoteCommands.eCmdMarker);
                                    Frame = new FrameObjectRec();
                                    Frame.Add(Note);
                                    NoteTrack.FrameArray.Add(Frame);
                                }
                                break;
                        }
                        /* delete event from array */
                        FrameArray.RemoveAt(InspectScan);
                        /* don't increment InspectScan */
                    }
                }

                /* process remaining notes in FrameArray, computing minimum duration */
                /* and updating CurrentTime with minimum duration. */
                if (FrameArray.Count != 0)
                {
                    NoteFlags DurationOpcode;
                    FrameObjectRec Frame;
                    int Scan;
                    int FrameLimit;
                    FractionRec MinimumDuration;
                    FractionRec unusedf;
                    double unusedd;
                    short unuseds;

                    /* initialize minimum duration */
                    QuantEvent = FrameArray[0];
                    GetQuantizedNoteEventInfo(QuantEvent, out unusedf, out unusedd, out DurationOpcode,
                        out unusedd, out unuseds, out unuseds, out unuseds);
                    NoteNoteObjectRec.ConvertDurationFrac(DurationOpcode, out MinimumDuration);
                    Debug.Assert(FractionIntMultOf64thDiv3(MinimumDuration)); // non-64div3 duration quantization?

                    /* allocate frame */
                    Frame = new FrameObjectRec();

                    /* process notes in frame */
                    FrameLimit = FrameArray.Count;
                    for (Scan = 0; Scan < FrameLimit; Scan += 1)
                    {
                        FractionRec StartTime;
                        double StartTimeAdjust;
                        NoteFlags Duration;
                        double DurationAdjust;
                        short MIDIPitch;
                        short MIDIAttackVelocity;
                        short MIDIReleaseVelocity;
                        NoteNoteObjectRec Note;
                        FractionRec FracDuration;

                        /* get the note */
                        QuantEvent = FrameArray[Scan];
                        Debug.Assert(QuantizedEventGetType(QuantEvent) == QuantEventType.eQuantizedNoteEvent); // non-note in frame array
                        /* get attributes */
                        GetQuantizedNoteEventInfo(QuantEvent, out StartTime, out StartTimeAdjust,
                            out Duration, out DurationAdjust, out MIDIPitch, out MIDIAttackVelocity,
                            out MIDIReleaseVelocity);
                        Debug.Assert(IntMultOf64thDiv3(Duration)); // non-64div3 duration quantization?
                        Debug.Assert(FractionRec.FractionsEqual(StartTime, CurrentTime)); // start time inconsistency
                        /* create note */
                        Note = new NoteNoteObjectRec(NoteTrack);
                        Frame.Add(Note);
                        TieMappingAddPair(TieMapping, QuantEvent, Note);
                        /* set note attributes */
                        Note.PutNoteDuration(Duration & NoteFlags.eDurationMask);
                        Note.PutNoteDurationDivision(Duration & NoteFlags.eDivisionMask);
                        Note.PutNoteDotStatus((Duration & NoteFlags.eDotModifier) != 0);
                        Note.EarlyLateAdjust = StartTimeAdjust;
                        Note.DurationAdjust = DurationAdjust;
                        Note.DurationAdjustMode = NoteFlags.eDurationAdjustMultiplicative;
                        Note.PutNotePitch((short)(MIDIPitch - MIDIC + Constants.CENTERNOTE));
                        Note.Accent1 = (MIDIAttackVelocity > 0)
                            ? -Math.Log(MIDIAttackVelocity) / Constants.LOG2 + LN127OVERLN2 : 7;
                        Note.Accent2 = (MIDIReleaseVelocity > 0)
                            ? -Math.Log(MIDIReleaseVelocity) / Constants.LOG2 + LN127OVERLN2 : 7;
                        switch ((MIDIPitch - MIDIC + ((MIDIC / 12 + 1) * 12)) % 12)
                        {
                            default:
                                // midi sharp/flat problem
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case 0: /* C */
                            case 2: /* D */
                            case 4: /* E */
                            case 5: /* F */
                            case 7: /* G */
                            case 9: /* A */
                            case 11: /* B */
                                break;
                            case 1: /* C# */
                            case 3: /* D# */
                            case 6: /* F# */
                            case 8: /* G# */
                            case 10: /* A# */
                                Note.PutNoteFlatOrSharpStatus(NoteFlags.eSharpModifier);
                                break;
                        }
                        /* do the minimum duration thing */
                        NoteNoteObjectRec.ConvertDurationFrac(Duration, out FracDuration);
                        Debug.Assert(FractionIntMultOf64thDiv3(FracDuration)); // non-64div3 duration quantization?
                        if (FractionRec.FracGreaterThan(MinimumDuration, FracDuration))
                        {
                            MinimumDuration = FracDuration;
                        }
                    }

                    /* add frame to track */
                    NoteTrack.FrameArray.Add(Frame);

                    /* if minimum duration is greater than time to next event, then */
                    /* add rests (one to this frame) to fill in the gap */
                    if (Index < Limit)
                    {
                        FractionRec NextEventTime;
                        FractionRec Difference;

                        /* get the start time of the next available event */
                        QuantEvent = GetQuantizedTrackIndexedEvent(QuantizedTrack, Index);
                        NextEventTime = GetQuantizedEventTime(QuantEvent);
                        Debug.Assert(FractionIntMultOf64thDiv3(NextEventTime)); // non-64div3 start time quantization?
                        FractionRec.SubFractions(NextEventTime, CurrentTime, out Difference);
                        if (FractionRec.FracGreaterThan(MinimumDuration, Difference))
                        {
                            NoteNoteObjectRec Note;
                            NoteFlags RestOpcode;
                            FractionRec OpcodesDuration;

                            /* insert first rest into frame */
                            RestOpcode = GetMaxDurationOpcode(Difference, out OpcodesDuration,
                                OpcodeDurationTable, OpcodeDurationTableLength);
                            Debug.Assert(IntMultOf64thDiv3(RestOpcode)); // non-64div3 duration quantization
                            Note = new NoteNoteObjectRec(NoteTrack);
                            Note.PutNoteDuration(RestOpcode & NoteFlags.eDurationMask);
                            Note.PutNoteDurationDivision(RestOpcode & NoteFlags.eDivisionMask);
                            Note.PutNoteDotStatus((RestOpcode & NoteFlags.eDotModifier) != 0);
                            Note.PutNotePitch(Constants.CENTERNOTE);
                            Note.PutNoteIsItARest(true);
                            Frame.Add(Note);
                            /* put new minimum duration in to reflect new rest we added */
                            NoteNoteObjectRec.ConvertDurationFrac(RestOpcode, out MinimumDuration);
                        }
                    }

                    /* advance thing by minimum duration */
                    FractionRec.AddFractions(MinimumDuration, CurrentTime, out CurrentTime);
                    Debug.Assert(FractionIntMultOf64thDiv3(CurrentTime)); // non-64div3 start time quantization?
                }
            }

            /* patch up ties */
            for (Index = 0; Index < Limit; Index += 1)
            {
                QuantEventRec QuantEvent;

                /* get potential event */
                QuantEvent = GetQuantizedTrackIndexedEvent(QuantizedTrack, Index);
                /* see if it ties somewhere */
                if ((QuantEventType.eQuantizedNoteEvent == QuantizedEventGetType(QuantEvent))
                    && (GetQuantizedEventTieTarget(QuantEvent) != null))
                {
                    QuantEventRec TieTarget;
                    NoteNoteObjectRec Source;
                    NoteNoteObjectRec Target;

                    /* get tie target */
                    TieTarget = GetQuantizedEventTieTarget(QuantEvent);
                    /* look up source and target note events */
                    Source = TieMappingLookup(TieMapping, QuantEvent);
                    Target = TieMappingLookup(TieMapping, TieTarget);
                    /* establish tie */
                    Source.PutNoteTieTarget(Target);
                }
            }

            /* look for track name comment */
            for (Index = 0; Index < Limit; Index += 1)
            {
                QuantEventRec QuantEvent;

                /* get potential event */
                QuantEvent = GetQuantizedTrackIndexedEvent(QuantizedTrack, Index);
                /* see if it ties somewhere */
                if (QuantEventType.eQuantizedCommentEvent == QuantizedEventGetType(QuantEvent))
                {
                    string CommentString;
                    FractionRec unusedf;
                    double unusedd;

                    GetQuantizedCommentEventInfo(QuantEvent, out unusedf, out unusedd, out CommentString);
                    /* check for track name */
                    if ((CommentString.Length > 11/*Prefix*/ + Environment.NewLine.Length)
                        && CommentString.StartsWith("Track Name" + Environment.NewLine))
                    {
                        string NameString = CommentString.Substring(11, CommentString.Length - (11 + 1));
                        NoteTrack.Name = NameString;
                        goto FinishedSettingTrackName;
                    }
                }
            }
            /* if no track name was found, then use the first comment string */
            for (Index = 0; Index < Limit; Index += 1)
            {
                QuantEventRec QuantEvent;

                /* get potential event */
                QuantEvent = GetQuantizedTrackIndexedEvent(QuantizedTrack, Index);
                /* see if it ties somewhere */
                if (QuantEventType.eQuantizedCommentEvent == QuantizedEventGetType(QuantEvent))
                {
                    string CommentString;
                    FractionRec unusedf;
                    double unusedd;

                    GetQuantizedCommentEventInfo(QuantEvent, out unusedf, out unusedd, out CommentString);
                    /* check for track name */
                    if ((CommentString.Length > 8/*Prefix*/ + Environment.NewLine.Length)
                        && CommentString.StartsWith("Comment" + Environment.NewLine))
                    {
                        string NameString;

                        NameString = CommentString.Substring(8, CommentString.Length - (8 + 1));
                        NoteTrack.Name = NameString;
                        goto FinishedSettingTrackName;
                    }
                }
            }
        FinishedSettingTrackName:
            ;
        }
    }
}
