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
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        /* if we see more jumps than this during one cycle, assume an infinite loop */
        private const int MAXALLOWEDJUMPS = 4000;

        public class FrozenNoteConsCell
        {
            /* list link */
            public FrozenNoteConsCell Next;

            /* good information to be used for tied notes */
            public FrozenNoteRec FrozenNote;

            /* tie target for tied notes, null if there is no tie */
            public NoteNoteObjectRec TieTarget;
            public int TieTargetJumpCountVal;

            /* when this note should take over (in absolute envelope ticks) */
            public int ContinuationTime;

            /* flag indicating that portamento should lead the beat */
            /* it gets cleared when the leading portamento has been initiated */
            /* (unlike the FrozenNote's flag which must remain set to prevent reinitiating */
            /* portamento on note start) */
            public bool LeadingPortamentoNeedsInitiation;

            // for tracing (otherwise not used)
            public int frameIndex, noteIndex;
        }

        public class OscBankConsCell
        {
            /* link for listing */
            public OscBankConsCell Next;
            public OscBankConsCell Previous;

            /* which instrument was this for */
            public PerInstrRec MyInstr;

            /* the oscillator bank */
            public OscStateBankRec OscBank;

            /* still active flag */
            public bool StillActive;

            /* flag indicating not to update the envelopes during the scheduled skip. */
            /* this is set for all banks operating before the scheduled skip started. */
            /* it allows them to be released by timing triggers, but run out their release */
            /* sequence after the skip is over to provide continuity of the waveform. */
            /* Note, when not in a scheduled skip period, all of these flags should be false. */
            public bool fDontUpdateEnvelopes;

            /* tie target for tied notes, null if there is no tie */
            public NoteNoteObjectRec TieTarget;
            public int TieTargetJumpCountVal;

            /* this is the start time of the note, used for ordering the scanning gap list */
            public int StartTime;

            /* this is an ordered list of frozen notes ready for tie continuation */
            /* this is in sorted ascending order of TieContinuationList.ContinuationTime */
            public FrozenNoteConsCell TieContinuationList;

            /* which iteration did this note come from */
            public int JumpIteration;

            // for tracing (otherwise not used)
            public int seq;
            public int frameIndex, noteIndex;
        }

        /* info for pending channel releases */
        public enum RelType { eReleaser = 1, eTieBreaker = 2 };
        public class ReleaseRec
        {
            /* execution index when this happens */
            public int When;

            /* pointer to next one */
            public ReleaseRec Next;

            /* type of record */
            public RelType Type;
            // only valid for Type==eReleaser
            /* which one is being released */
            public WhichEnvType Releaser_Which;
            // only valid for Type==eTieBreaker
            /* which iteration should we cancel */
            public int TieBreaker_JumpIteration;
        }

        public class PerInstrRec
        {
            /* note range that this instrument handles */
            public short BasePitch;
            public short TopPitch;
            public short EffectiveBasePitch;

            /* this is the template used for creating oscillator banks */
            public OscBankTemplateRec OscillatorBankTemplate;

            /* this is a list of all currently executing oscillator banks.  there is one */
            /* entry for each note that is currently being played. */
            public OscBankConsCell ExecutingOscillatorBanks;

            /* track effects */
            public TrackEffectGenRec EffectGenerator;

            /* next instr block */
            public PerInstrRec Next;
        }

        public class PlayTrackInfoRec
        {
            /* frame source */
            public TrackObjectRec TrackObject;
            /* total number of frames in the track object */
            public int TotalNumberOfFrames;
            /* index into frame array. */
            public int FrameArrayIndex;
            /* number of cycles until the next frame should be processed.  this is in */
            /* units of duration update cycles.  when this runs out, then another frame */
            /* should be processed. */
            public int NextFrameCountDown;

            /* pointer to list of per instrument entities.  this contains currently executing */
            /* note and effect state for each of the instruments used by this track. */
            /* information from the scanning gap list is routed into one of the nodes in this */
            /* list as appropriate. */
            public PerInstrRec Instrs;

            /* list of notes that have been scheduled but haven't been executed yet. */
            /* it contains objects of type OscBankConsCell. */
            public OscBankConsCell ScanningGapListHead; /* of OscBankConsCell's */
            public OscBankConsCell ScanningGapListTail; /* of OscBankConsCell's */
            /* this is the current duration update index for the scanning gap list */
            public int ExecutionIndex;
            /* when ExecutionIndex hits the StartTime of the first element of the scanning */
            /* gap list, the oscillator bank is removed from the scanning gap list and */
            /* added to the execution list. */

            /* this object keeps track of the current value of all parameters, updates them */
            /* as time passes, and evaluates commands passed in from here.  the state of */
            /* this object reflects the state at the front of scanning gap, since notes */
            /* are frozen immediately upon entering scanning gap. */
            public IncrParamUpdateRec ParameterController;

            /* score effects.  we don't actually apply this, but we have it so that */
            /* we can send it commands.  it is a reference to a shared object. */
            public TrackEffectGenRec ScoreEffectGenerator;

            /* effect generator for this section (may be null).  just like the score effect */
            /* generator, we don't apply it, but we send commands to it. */
            public TrackEffectGenRec SectionEffectGenerator;

            /* information for sequence looping */
            public int CurrentSequenceStartFrameIndex; /* -1 == not looping */
            public Dictionary<string, int> SequenceIndexMap; /* string to int */
            public int JumpCounter; /* used to detect infinite loops & keep ties straight */
            /* for terminating sequencing */
            public bool TerminationPending;
            public int TerminationIndex; /* when ExecutionIndex reaches this, stop */
            /* for detecting sequence commands from later to earlier tracks */
            public int PositionNumber;
            /* the track where commands are currently being sent (null == to myself) */
            public string CurrentCommandRedirection;
            /* this object tells maps from track/group name to array of tracks */
            public SequencerTableRec SequencerTable;

            /* information for pending channel releases and tie breaking */
            public ReleaseRec PendingChannelOperations;

            public ParkAndMiller Seed;

            // for tracing
            public int seqGen;
            public List<EventTraceRec> events; // null == not tracing
        }

        /* fill in the sequence index map for the track by finding the location */
        /* of all sequence begin commands. */
        /* NOTE: the frame index is stored in the "stack location" property of the symbol */
        private static SynthErrorCodes FillInSequenceIndexMap(
            PlayTrackInfoRec TrackInfo,
            SynthParamRec SynthParams)
        {
            bool SequenceBeginSeenWithNoDuration = false;
            int c = TrackInfo.TrackObject.FrameArray.Count;
            for (int i = 0; i < c; i += 1)
            {
                FrameObjectRec Frame = TrackInfo.TrackObject.FrameArray[i];
                if (Frame.IsThisACommandFrame)
                {
                    CommandNoteObjectRec Command;

#if DEBUG
                    if (1 != Frame.Count)
                    {
                        // command frame doesn't have one command
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
#endif
                    Command = (CommandNoteObjectRec)Frame[0];
                    if (Command.GetCommandOpcode() == NoteCommands.eCmdSequenceBegin)
                    {
                        /* one or more sequences have immediately opened */
                        SequenceBeginSeenWithNoDuration = true;

                        string SequenceName = Command.GetCommandStringArg1();
                        if (TrackInfo.SequenceIndexMap.ContainsKey(SequenceName))
                        {
                            SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExSequenceSpecifiedMultipleTimes;
                            SynthParams.ErrorInfo.SequenceName = SequenceName;
                            SynthParams.ErrorInfo.TrackName = TrackInfo.TrackObject.Name;
                            return SynthErrorCodes.eSynthErrorEx;
                        }
                        TrackInfo.SequenceIndexMap.Add(SequenceName, i);
                    }
                    else if (Command.GetCommandOpcode() == NoteCommands.eCmdSequenceEnd)
                    {
                        if (SequenceBeginSeenWithNoDuration)
                        {
                            SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExSequenceHasNoDuration;
                            SynthParams.ErrorInfo.TrackName = TrackInfo.TrackObject.Name;
                            return SynthErrorCodes.eSynthErrorEx;
                        }
                    }
                }
                else /* it's a note frame */
                {
                    /* now the last sequence has some real duration */
                    SequenceBeginSeenWithNoDuration = false;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* helper for initializing the per-instrument records */
        private static SynthErrorCodes InitializePerInstrTemplates(
            PlayTrackInfoRec TrackInfo,
            MyBindingList<InstrObjectRec> InstrList,
            TrackObjectRec TrackObject,
            SynthParamRec SynthParams)
        {
            MultiInstrSpecRec Spec;

            /* create a spec for the instrument list */
            if (TrackObject.MultiInstrument)
            {
                MultiInstrParseError Error = MultiInstrParse(out Spec, TrackObject.InstrumentName);
                if (Error != MultiInstrParseError.eMultiInstrParseOK)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedInstrument;
                    SynthParams.ErrorInfo.TrackName = TrackObject.Name;
                    SynthParams.ErrorInfo.CustomError = MultiInstrGetErrorString(Error);
                    return SynthErrorCodes.eSynthErrorEx;
                }
            }
            else
            {
                Spec = NewMultiInstrSpec();
                MultiInstrSpecAddMaximumDefaultZone(
                    Spec,
                    TrackObject.InstrumentName);
            }

            int l = MultiInstrSpecGetLength(Spec);
            for (int i = l - 1; i >= 0; i -= 1)
            {
                /* create a list node */
                PerInstrRec OneInstr = new PerInstrRec();

                /* push onto front of list (that's why loop is in reverse order) */
                OneInstr.Next = TrackInfo.Instrs;
                TrackInfo.Instrs = OneInstr;

                /* set boundaries */
                OneInstr.BasePitch = MultiInstrSpecGetIndexedBasePitch(Spec, i);
                OneInstr.TopPitch = MultiInstrSpecGetIndexedTopPitch(Spec, i);
                OneInstr.EffectiveBasePitch = MultiInstrSpecGetIndexedEffectiveBasePitch(Spec, i);

                /* get the instrument */
                string InstrName = MultiInstrSpecGetIndexedActualInstrName(Spec, i);
                InstrObjectRec BaseInstrument = InstrList.Find(delegate (InstrObjectRec candidate) { return String.Equals(candidate.Name, InstrName); });
                if (BaseInstrument == null)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedInstrument;
                    SynthParams.ErrorInfo.TrackName = TrackInfo.TrackObject.Name;
                    SynthParams.ErrorInfo.InstrumentName = InstrName;
                    return SynthErrorCodes.eSynthErrorEx;
                }

                /* this is the template used for creating oscillator banks */
                InstrumentRec InstrumentSpecification = BaseInstrument.GetInstrObjectRawData();
                OneInstr.OscillatorBankTemplate = NewOscBankTemplate(
                    InstrumentSpecification,
                    TrackInfo.ParameterController,
                    SynthParams);

                /* set up track effects */
                SynthErrorCodes Result = NewTrackEffectGenerator(
                    GetInstrumentEffectSpecList(InstrumentSpecification),
                    SynthParams,
                    out OneInstr.EffectGenerator);
                if (Result != SynthErrorCodes.eSynthDone)
                {
                    return Result;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* create a new track play info thing and set a bunch of parameters.  this also */
        /* builds the internal representations for instruments & oscillators for this track. */
        public static SynthErrorCodes NewPlayTrackInfo(
            out PlayTrackInfoRec TrackInfoOut,
            TrackObjectRec TheTrack,
            MyBindingList<InstrObjectRec> InstrList,
            TrackEffectGenRec ScoreEffectGenerator,
            TrackEffectGenRec SectionEffectGenerator,
            TempoControlRec TempoControl,
            SynthParamRec SynthParams)
        {
            TrackInfoOut = null;

            SynthErrorCodes Result;

            PlayTrackInfoRec TrackInfo = new PlayTrackInfoRec();

            /* score effect generator */
            TrackInfo.ScoreEffectGenerator = ScoreEffectGenerator;
            TrackInfo.SectionEffectGenerator = SectionEffectGenerator; /* may be null */

            /* frame source */
            TrackInfo.TrackObject = TheTrack;

            /* total number of frames in the track object */
            TrackInfo.TotalNumberOfFrames = TheTrack.FrameArray.Count;

            /* this is the current envelope update index for removing things from the */
            /* scanning gap list (i.e. the back edge of the scanning gap) */
            /* by setting this negative, we cause the scanning gap to open. */
            TrackInfo.ExecutionIndex = -SynthParams.iScanningGapWidthInEnvelopeTicks;

            /* this object keeps track of the current value of all parameters, updates */
            /* them as time passes, and evaluates commands passed in from here. */
            TrackInfo.ParameterController = NewInitializedParamUpdator(
                TheTrack,
                TempoControl,
                SynthParams);

            /* build the per-instrument template/state list */
            Result = InitializePerInstrTemplates(
                TrackInfo,
                InstrList,
                TheTrack,
                SynthParams);
            if (Result != SynthErrorCodes.eSynthDone)
            {
                return Result;
            }

            TrackInfo.SequenceIndexMap = new Dictionary<string, int>();

            /* no looping yet */
            TrackInfo.CurrentSequenceStartFrameIndex = -1;

            Result = FillInSequenceIndexMap(
                TrackInfo,
                SynthParams);
            if (Result != SynthErrorCodes.eSynthDone)
            {
                return Result;
            }

            SynthParams.randomSeedProvider.ObtainSeed(ref TrackInfo.Seed);

            TrackInfoOut = TrackInfo;
            return SynthErrorCodes.eSynthDone;
        }

        /* set the iteration position number (used for detecting a later track trying */
        /* to command an earlier track). */
        public static void PlayTrackInfoSetPositionNumber(
            PlayTrackInfoRec TrackInfo,
            int Position)
        {
            TrackInfo.PositionNumber = Position;
        }

        /* after tracks are created and the sequencer table is computed, provide it */
        /* to all the tracks by calling this. */
        public static void PlayTrackInfoSetSequencerTable(
            PlayTrackInfoRec TrackInfo,
            SequencerTableRec SequencerTable)
        {
            TrackInfo.SequencerTable = SequencerTable;
        }

        /* check to see if the track has finished and can be dropped. */
        public static bool PlayTrackIsItStillActive(PlayTrackInfoRec TrackInfo)
        {
            /* in order to still be active, at least one of the following conditions */
            /* must be satisfied: */
            /*  - there are still active oscillators. */
            /*  - there are still oscillators in the scanning gap list */
            /*  - there are still notes which haven't been scanned yet */
            /*  - there is a termination time that hasn't been reached */

            if (TrackInfo.FrameArrayIndex < TrackInfo.TotalNumberOfFrames)
            {
                return true;
            }

            if (TrackInfo.ScanningGapListHead != null)
            {
                return true;
            }

            if (TrackInfo.TerminationPending
                && (TrackInfo.ExecutionIndex < TrackInfo.TerminationIndex))
            {
                return true;
            }

            PerInstrRec InstrScan = TrackInfo.Instrs;
            while (InstrScan != null)
            {
                OscBankConsCell OscBankScan;

                OscBankScan = InstrScan.ExecutingOscillatorBanks;
                while (OscBankScan != null)
                {
                    if (OscBankScan.StillActive)
                    {
                        return true;
                    }
                    OscBankScan = OscBankScan.Next;
                }
                InstrScan = InstrScan.Next;
            }

            return false;
        }

        /* aux. routine to insert into list */
        private static void InsertChannelOperation(
            PlayTrackInfoRec TrackInfo,
            ReleaseRec Op)
        {
            ReleaseRec List;
            ReleaseRec ListLag;

            /* insert into sorted list.  this sort puts new things after any existing */
            /* things that occur at the same time, so that they happen in the same order */
            /* as they were specified in the track */
            List = TrackInfo.PendingChannelOperations;
            ListLag = null;
            while ((List != null) && (Op.When >= List.When))
            {
                ListLag = List;
                List = List.Next;
            }
            /* List is the first one that was later or equal to us */
            Op.Next = List;
            if (ListLag != null)
            {
                ListLag.Next = Op;
            }
            else
            {
                TrackInfo.PendingChannelOperations = Op;
            }
        }

        /* aux. routine called when jump counter is incremented to break any ties that */
        /* are still waiting, since they may never be seen.  This schedules all notes */
        /* from the current JumpCounter iteration to be cancelled, so it should be called */
        /* before JumpCounter is incremented. */
        private static void ScheduleBreakPendingTies(
            PlayTrackInfoRec TrackInfo,
            int ScanningGapFrontInEnvelopeTicks,
            SynthParamRec SynthParams)
        {
            ReleaseRec TieBreakTime = new ReleaseRec();
            TieBreakTime.Type = RelType.eTieBreaker;

            /* remember which jump we're breaking for, so we don't break ones that */
            /* are for the next iteration */
            TieBreakTime.TieBreaker_JumpIteration = TrackInfo.JumpCounter;

            /* do it when notes we're scheduling now finally execute */
            TieBreakTime.When = ScanningGapFrontInEnvelopeTicks;

            /* insert into sorted list */
            InsertChannelOperation(
                TrackInfo,
                TieBreakTime);
        }

        /* auxiliary routine which searches a list for a tie source and installs the note */
        /* in the list if necessary.  true is returned if it is installed. */
        /* Instr is used for computing the pitch adjust based on segments.  If it is null, */
        /* then the OscBankScan list is the scanning gap list, which is the aggregate of */
        /* all pending notes for all instruments; in that case, the MyInstr field of each */
        /* list member is used instead. */
        private static bool SearchForTieSource(
            PerInstrRec Instr,
            OscBankConsCell OscBankScan,
            NoteNoteObjectRec Note,
            int frameIndex,
            int noteIndex,
            int ScanningGapFrontInEnvelopeTicks,
            PlayTrackInfoRec TrackInfo,
            double EnvelopeTicksPerDurationTick,
            SynthParamRec SynthParams)
        {
            /* scan the oscillator banks */
            while (OscBankScan != null)
            {
                /* look to see if this note is the target of any ties */
                bool OKToAdd = false;

                /* if note is the current note's tie target */
                if ((OscBankScan.TieTarget == Note)
                    && (OscBankScan.TieTargetJumpCountVal == TrackInfo.JumpCounter))
                {
                    OKToAdd = true;
                }
                else
                {
                    FrozenNoteConsCell TargScan;
                    FrozenNoteConsCell TargLag;

                    /* search tie continuation list to see if note is on the list */
                    TargScan = OscBankScan.TieContinuationList;
                    TargLag = null;
                    while (!OKToAdd && (TargScan != null))
                    {
                        if ((TargScan.TieTarget == Note)
                            && (TargScan.TieTargetJumpCountVal == TrackInfo.JumpCounter))
                        {
                            /* found one */
                            OKToAdd = true;
                        }
                        else
                        {
                            TargLag = TargScan;
                            TargScan = TargScan.Next;
                        }
                    }
                }

                /* allocate new cons cell */
                if (OKToAdd)
                {
                    FrozenNoteConsCell InsertScan;
                    FrozenNoteConsCell InsertLag;
                    int StartAdjust;

                    FrozenNoteConsCell PlaceToPut = new FrozenNoteConsCell();

                    /* fill in the fields */
                    PlaceToPut.FrozenNote = FixNoteParameters(
                        TrackInfo.ParameterController,
                        Note,
                        out StartAdjust,
                        EnvelopeTicksPerDurationTick,
                        (Instr != null)
                            ? (short)(Instr.EffectiveBasePitch - Instr.BasePitch)
                            : (short)(OscBankScan.MyInstr.EffectiveBasePitch - OscBankScan.MyInstr.BasePitch),
                        SynthParams);
                    PlaceToPut.ContinuationTime = StartAdjust + ScanningGapFrontInEnvelopeTicks;
                    PlaceToPut.TieTarget = Note.GetNoteTieTarget();
                    PlaceToPut.TieTargetJumpCountVal = TrackInfo.JumpCounter;
                    PlaceToPut.LeadingPortamentoNeedsInitiation = PlaceToPut.FrozenNote.PortamentoBeforeNote;
                    PlaceToPut.frameIndex = frameIndex;
                    PlaceToPut.noteIndex = noteIndex;

                    /* insert it into the proper place */
                    InsertScan = OscBankScan.TieContinuationList;
                    InsertLag = null;
                    while ((InsertScan != null) && (InsertScan.ContinuationTime <= PlaceToPut.ContinuationTime))
                    {
                        InsertLag = InsertScan;
                        InsertScan = InsertScan.Next;
                    }
                    PlaceToPut.Next = InsertScan;
                    if (InsertLag == null)
                    {
                        OscBankScan.TieContinuationList = PlaceToPut;
                    }
                    else
                    {
                        InsertLag.Next = PlaceToPut;
                    }

                    /* we found it! */
                    return true;
                }

                OscBankScan = OscBankScan.Next;
            }

            return false;
        }

        private delegate SynthErrorCodes InvokeSequenceTargetFunctionMethod(
            PlayTrackInfoRec TrackInfo,
            int ScanningGapFrontInEnvelopeTicks,
            CommandNoteObjectRec SequenceCommand,
            PlayTrackInfoRec CommandSourceTrackInfo,
            SkipSegmentsRec SkipSchedule,
            SynthParamRec SynthParams);

        /* aux routine to invoke a sequence control on target tracks */
        private static SynthErrorCodes InvokeSequenceTarget(
            PlayTrackInfoRec TrackInfo,
            string TrackOrGroupName,
            CommandNoteObjectRec Command,
            int ScanningGapFrontInEnvelopeTicks,
            SkipSegmentsRec SkipSchedule,
            SynthParamRec SynthParams,
            InvokeSequenceTargetFunctionMethod Function)
        {
            List<PlayTrackInfoRec> Players = SequencerTableQuery(
                TrackInfo.SequencerTable,
                TrackOrGroupName);
            if (Players != null)
            {
                /* that track or group does actually exist */
                for (int i = 0; i < Players.Count; i += 1)
                {
                    PlayTrackInfoRec TrackPlayer = Players[i];
                    SynthErrorCodes Error = Function(
                        TrackPlayer/*them*/,
                        ScanningGapFrontInEnvelopeTicks,
                        Command,
                        TrackInfo/*us*/,
                        SkipSchedule,
                        SynthParams);
                    if (Error != SynthErrorCodes.eSynthDone)
                    {
                        return Error;
                    }
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* auxiliary routine for PlayTrackUpdate */
        /* schedule any notes out of the track list into the scanning gap */
        private static SynthErrorCodes ScheduleNotes(
            PlayTrackInfoRec TrackInfo,
            int ScanningGapFrontInEnvelopeTicks,
            double EnvelopeTicksPerDurationTick,
            bool FastForwardMode,
            SkipSegmentsRec SkipSchedule,
            SynthParamRec SynthParams)
        {
            int InitialJumpCount = TrackInfo.JumpCounter;

            while ((TrackInfo.NextFrameCountDown <= 0)
                && (TrackInfo.FrameArrayIndex < TrackInfo.TotalNumberOfFrames))
            {
                int frameIndex = TrackInfo.FrameArrayIndex;

                /* schedule a frame */
                FrameObjectRec Frame = TrackInfo.TrackObject.FrameArray[TrackInfo.FrameArrayIndex];
                TrackInfo.FrameArrayIndex += 1;

                if (Frame.IsThisACommandFrame)
                {
                    /* it's a command */
                    CommandNoteObjectRec Command = (CommandNoteObjectRec)Frame[0];
                    NoteCommands CmdOpcode = Command.GetCommandOpcode();

                    /* dispatch to the correct target */
                    SynthErrorCodes Error;
                    if ((TrackInfo.CurrentCommandRedirection == null)
                        || (CmdOpcode == NoteCommands.eCmdRedirect) || (CmdOpcode == NoteCommands.eCmdRedirectEnd))
                    {
                        /* if no redirection, or it's a redirection control command, */
                        /* execute directly on our track */
                        Error = PlayTrackInfoExecuteCommand(
                            TrackInfo,
                            ScanningGapFrontInEnvelopeTicks,
                            Command,
                            TrackInfo,
                            SkipSchedule,
                            SynthParams);
                    }
                    else
                    {
                        /* if redirecting, then dispatch to target tracks */
                        Error = InvokeSequenceTarget(
                            TrackInfo,
                            TrackInfo.CurrentCommandRedirection,
                            Command,
                            ScanningGapFrontInEnvelopeTicks,
                            SkipSchedule,
                            SynthParams,
                            PlayTrackInfoExecuteCommand);
                    }
                    if (Error != SynthErrorCodes.eSynthDone)
                    {
                        return Error;
                    }
                }
                else
                {
                    /* increment the frame counter */
                    FractionRec FrameDuration;
                    Frame.DurationOfFrame(out FrameDuration);
#if DEBUG
                    if (DURATIONUPDATECLOCKRESOLUTION != FrameDuration.Denominator)
                    {
                        // strange denominator in frame duration
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
#endif
                    TrackInfo.NextFrameCountDown += (int)(FrameDuration.Denominator
                        * FrameDuration.Integer + FrameDuration.Fraction);

                    /* it's a real note */
                    if (!FastForwardMode)
                    {
                        /* don't actually queue up the note unless we're not in fast-forward */
                        for (int noteIndex = 0; noteIndex < Frame.Count; noteIndex++)
                        {
                            PerInstrRec InstrScan;
                            OscBankConsCell NewOscBank;
                            OscBankConsCell LinkingScan;

                            NoteNoteObjectRec Note = (NoteNoteObjectRec)Frame[noteIndex];
                            if (Note.GetNoteIsItARest())
                            {
                                /* just ignore rests */
                                goto EndFrameScanPoint;
                            }

                            /* first, scan the oscillator list to see if this is a note that */
                            /* someone wants to tie to.  if it is, then build a frozen note */
                            /* structure and add it to the object.  otherwise, build an */
                            /* oscillator bank and add it to the scanning gap. */

                            /* search all oscillator banks to see if it's a tie target */
                            InstrScan = TrackInfo.Instrs;
                            while (InstrScan != null)
                            {
                                if (SearchForTieSource(
                                    InstrScan,
                                    InstrScan.ExecutingOscillatorBanks,
                                    Note,
                                    frameIndex,
                                    noteIndex,
                                    ScanningGapFrontInEnvelopeTicks,
                                    TrackInfo,
                                    EnvelopeTicksPerDurationTick,
                                    SynthParams))
                                {
                                    goto EndFrameScanPoint;
                                }
                                InstrScan = InstrScan.Next;
                            }
                            if (SearchForTieSource(
                                null,
                                TrackInfo.ScanningGapListHead,
                                Note,
                                frameIndex,
                                noteIndex,
                                ScanningGapFrontInEnvelopeTicks,
                                TrackInfo,
                                EnvelopeTicksPerDurationTick,
                                SynthParams))
                            {
                                goto EndFrameScanPoint;
                            }


                            /* determine which instrument its for */
                            InstrScan = TrackInfo.Instrs;
                            while ((InstrScan != null)
                                && !((Note.GetNotePitch() >= InstrScan.BasePitch)
                                && (Note.GetNotePitch() <= InstrScan.TopPitch)))
                            {
                                InstrScan = InstrScan.Next;
                            }
                            if (InstrScan == null)
                            {
                                /* no instrument handling this pitch range -- ignore */
                                goto EndFrameScanPoint;
                            }


                            /* if we got here, then it's not a tie target */
                            NewOscBank = new OscBankConsCell();

                            SynthErrorCodes Error = NewOscBankState(
                                InstrScan.OscillatorBankTemplate,
                                out NewOscBank.StartTime,
                                Note,
                                EnvelopeTicksPerDurationTick,
                                (short)(InstrScan.EffectiveBasePitch - InstrScan.BasePitch),
                                TrackInfo,
                                SynthParams,
                                out NewOscBank.OscBank);
                            if (Error != SynthErrorCodes.eSynthDone)
                            {
                                return Error;
                            }
                            NewOscBank.MyInstr = InstrScan;
                            NewOscBank.StartTime += ScanningGapFrontInEnvelopeTicks; /* fix up start time */
                            NewOscBank.StillActive = true;
                            NewOscBank.fDontUpdateEnvelopes = false;
                            NewOscBank.TieTarget = GetOscStateTieTarget(NewOscBank.OscBank);
                            NewOscBank.TieTargetJumpCountVal = TrackInfo.JumpCounter;
                            NewOscBank.TieContinuationList = null;
                            NewOscBank.JumpIteration = TrackInfo.JumpCounter;
                            NewOscBank.frameIndex = frameIndex;
                            NewOscBank.noteIndex = noteIndex;

                            /* link it in */
                            LinkingScan = TrackInfo.ScanningGapListTail;
                            while ((LinkingScan != null)
                                && (LinkingScan.StartTime > NewOscBank.StartTime))
                            {
                                LinkingScan = LinkingScan.Previous;
                            }

                            if (LinkingScan == null)
                            {
                                NewOscBank.Previous = null;
                                NewOscBank.Next = TrackInfo.ScanningGapListHead;
                                if (TrackInfo.ScanningGapListHead != null)
                                {
                                    TrackInfo.ScanningGapListHead.Previous = NewOscBank;
                                }
                                TrackInfo.ScanningGapListHead = NewOscBank;
                                if (TrackInfo.ScanningGapListTail == null)
                                {
                                    /* this happens if there were no nodes at all */
                                    TrackInfo.ScanningGapListTail = NewOscBank;
                                }
                            }
                            else
                            {
                                /* insert after Scan */
                                NewOscBank.Previous = LinkingScan;
                                NewOscBank.Next = LinkingScan.Next;
                                LinkingScan.Next = NewOscBank;
                                if (LinkingScan == TrackInfo.ScanningGapListTail)
                                {
                                    /* this happens if Scan was the last element; */
                                    /* NewNode becomes last element */
                                    TrackInfo.ScanningGapListTail = NewOscBank;
                                }
                            }

                        EndFrameScanPoint:
                            ;
                        }
                    }
                }

                if (TrackInfo.JumpCounter - InitialJumpCount > MAXALLOWEDJUMPS)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExPossibleInfiniteSequenceLoop;
                    SynthParams.ErrorInfo.TrackName = TrackInfo.TrackObject.Name;
                    return SynthErrorCodes.eSynthErrorEx;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* auxiliary routine for PlayTrackUpdate */
        /* initiate portamento for any active oscillator banks requiring it */
        private static void GeneratePortamentoInitiation(PlayTrackInfoRec TrackInfo)
        {
            /* see if any portamentos leading the note have to be initiated */
            PerInstrRec InstrScan = TrackInfo.Instrs;
            while (InstrScan != null)
            {
                OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                while (OscBankScan != null)
                {
                    FrozenNoteConsCell FirstTie = OscBankScan.TieContinuationList;
                    if (FirstTie != null)
                    {
                        /* if there is a tie target */
                        if (FirstTie.LeadingPortamentoNeedsInitiation)
                        {
                            if (FirstTie.ContinuationTime - FirstTie.FrozenNote.PortamentoDuration
                                <= TrackInfo.ExecutionIndex)
                            {
                                /* time to initiate this portamento */
                                FirstTie.LeadingPortamentoNeedsInitiation = false; /* mark */
                                RestartOscBankStatePortamento(
                                    OscBankScan.OscBank,
                                    FirstTie.FrozenNote);
                            }
                        }
                    }

                    OscBankScan = OscBankScan.Next;
                }

                InstrScan = InstrScan.Next;
            }
        }

        /* auxiliary routine for PlayTrackUpdate */
        /* chain any ties that need to be processed */
        private static void GenerateTieContinuation(
            PlayTrackInfoRec TrackInfo,
            SynthParamRec SynthParams)
        {
            /* see if any ties have to be tripped */
            PerInstrRec InstrScan = TrackInfo.Instrs;
            while (InstrScan != null)
            {
                OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                while (OscBankScan != null)
                {
                    /* a loop ideally isn't needed since there usually wouldn't be zero */
                    /* time between ties, but with hit time adjustments you never know. */
                    while ((OscBankScan.TieContinuationList != null)
                        && (OscBankScan.TieContinuationList.ContinuationTime
                        <= TrackInfo.ExecutionIndex))
                    {
                        /* yow, let's do this one! */

                        /* delink */
                        FrozenNoteConsCell Temp = OscBankScan.TieContinuationList;
                        OscBankScan.TieContinuationList = OscBankScan.TieContinuationList.Next;

                        /* execute */
                        ResetOscBankState(
                            OscBankScan.OscBank,
                            Temp.FrozenNote,
                            SynthParams);
                        OscBankScan.TieTarget = Temp.TieTarget;

                        if (TrackInfo.events != null)
                        {
                            TrackInfo.events.Add(
                                new EventTraceRec(
                                    EventTraceType.Restart,
                                    OscBankScan.seq,
                                    Temp.frameIndex,
                                    Temp.noteIndex));
                        }

#if DEBUG
                        if (OscBankScan.TieTargetJumpCountVal != Temp.TieTargetJumpCountVal)
                        {
                            // ties from different sequence iterations
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
#endif
                    }

                    OscBankScan = OscBankScan.Next;
                }

                InstrScan = InstrScan.Next;
            }
        }

        /* auxiliary routine for PlayTrackUpdate */
        /* do any channel releases need during this cycle */
        private static void GenerateChannelOperations(
            PlayTrackInfoRec TrackInfo,
            int When,
            SynthParamRec SynthParams)
        {
            while ((TrackInfo.PendingChannelOperations != null)
                && (TrackInfo.PendingChannelOperations.When <= When))
            {
                ReleaseRec Temp = TrackInfo.PendingChannelOperations;
                TrackInfo.PendingChannelOperations = TrackInfo.PendingChannelOperations.Next;

                switch (Temp.Type)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case RelType.eReleaser:
                        {
                            /* release all oscillator banks */
                            PerInstrRec InstrScan = TrackInfo.Instrs;
                            while (InstrScan != null)
                            {
                                OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                                while (OscBankScan != null)
                                {
                                    bool force = true;

                                    OscStateBankReleaseEnvelopesNow(
                                        OscBankScan.OscBank,
                                        Temp.Releaser_Which,
                                        force);

#if false // TODO:later
                                    if (TrackInfo.events != null)
                                    {
                                        if ((Temp.Releaser_Which == WhichEnvType.eRls1)
                                            || (Temp.Releaser_Which == WhichEnvType.eRlsAll))
                                        {
                                            TrackInfo.events.Add(
                                                new EventTraceRec(
                                                    EventTraceType.Rls1,
                                                    OscBankScan.seq));
                                        }
                                        if ((Temp.Releaser_Which == WhichEnvType.eRls2)
                                            || (Temp.Releaser_Which == WhichEnvType.eRlsAll))
                                        {
                                            TrackInfo.events.Add(
                                                new EventTraceRec(
                                                    EventTraceType.Rls2,
                                                    OscBankScan.seq));
                                        }
                                        if ((Temp.Releaser_Which == WhichEnvType.eRls3)
                                            || (Temp.Releaser_Which == WhichEnvType.eRlsAll))
                                        {
                                            TrackInfo.events.Add(
                                                new EventTraceRec(
                                                    EventTraceType.Rls3,
                                                    OscBankScan.seq));
                                        }
                                    }
#endif

                                    OscBankScan = OscBankScan.Next;
                                }

                                InstrScan = InstrScan.Next;
                            }
                        }
                        break;

                    case RelType.eTieBreaker:
                        {
                            PerInstrRec InstrScan = TrackInfo.Instrs;
                            while (InstrScan != null)
                            {
                                OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                                while (OscBankScan != null)
                                {
                                    /* only break ties from the iteration just finished */
                                    if (Temp.TieBreaker_JumpIteration - OscBankScan.JumpIteration >= 0)
                                    {
#if false // TODO: diagnose -- maybe this is expected for sufficiently short loops
                                        if (Temp.TieBreaker_JumpIteration - OscBankScan.JumpIteration > 0)
                                        {
                                            // tie-breaker found one from earlier than the previous iteration
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        }
#endif

                                        /* break the tie */
                                        if (OscBankScan.TieContinuationList == null)
                                        {
                                            /* no scheduled chain -- just break this one */
                                            OscBankScan.TieTarget = null;
                                        }
                                        else
                                        {
                                            /* break at end of scheduled chain */
                                            FrozenNoteConsCell TargScan = OscBankScan.TieContinuationList;
                                            while (TargScan.Next != null)
                                            {
                                                TargScan = TargScan.Next;
                                            }
                                            TargScan.TieTarget = null;
                                        }

                                        /* release all oscillator banks for "from end" envelopes */
                                        OscStateBankReleaseEnvelopesNow(
                                            OscBankScan.OscBank,
                                            WhichEnvType.eRlsAll,
                                            false/*allow scheduled ones*/);

#if false // TODO: later
                                        if (TrackInfo.events != null)
                                        {
                                            if ((OscBankScan.OscBank.Release1Countdown < 0) &&
                                                ((Temp.Releaser_Which == WhichEnvType.eRls1)
                                                || (Temp.Releaser_Which == WhichEnvType.eRlsAll)))
                                            {
                                                TrackInfo.events.Add(
                                                    new EventTraceRec(
                                                        EventTraceType.Rls1,
                                                        OscBankScan.seq));
                                            }
                                            if ((OscBankScan.OscBank.Release2Countdown < 0) &&
                                                ((Temp.Releaser_Which == WhichEnvType.eRls2)
                                                || (Temp.Releaser_Which == WhichEnvType.eRlsAll)))
                                            {
                                                TrackInfo.events.Add(
                                                    new EventTraceRec(
                                                        EventTraceType.Rls2,
                                                        OscBankScan.seq));
                                            }
                                            if ((OscBankScan.OscBank.Release3Countdown < 0) &&
                                                ((Temp.Releaser_Which == WhichEnvType.eRls3)
                                                || (Temp.Releaser_Which == WhichEnvType.eRlsAll)))
                                            {
                                                TrackInfo.events.Add(
                                                    new EventTraceRec(
                                                        EventTraceType.Rls3,
                                                        OscBankScan.seq));
                                            }
                                        }
#endif
                                    }

                                    OscBankScan = OscBankScan.Next;
                                }

                                InstrScan = InstrScan.Next;
                            }
                        }
                        break;
                }
            }
        }

        /* auxiliary routine for PlayTrackUpdate */
        /* schedule any notes out of the scanning gap */
        private static void GenerateScanningGapScheduler(
            PlayTrackInfoRec TrackInfo,
            SynthParamRec SynthParams)
        {
            /* schedule a note from the scanning gap */
            while ((TrackInfo.ScanningGapListHead != null)
                && (TrackInfo.ScanningGapListHead.StartTime <= TrackInfo.ExecutionIndex))
            {
                /* first, do any channel releases on currently playing notes BEFORE */
                /* scheduling this note (so we don't whack it) */
                GenerateChannelOperations(
                    TrackInfo,
                    TrackInfo.ScanningGapListHead.StartTime,
                    SynthParams);

                /* yup, schedule the oscillator */
                /* [technically, the start time should never be strictly less than the */
                /* execution index (only equal), but it can occur if the user specifies */
                /* a scanning gap that's too narrow.] */
                OscBankConsCell NewConsCell = TrackInfo.ScanningGapListHead;
                TrackInfo.ScanningGapListHead = TrackInfo.ScanningGapListHead.Next;
                if (TrackInfo.ScanningGapListHead != null)
                {
                    TrackInfo.ScanningGapListHead.Previous = null;
                }
                else
                {
                    TrackInfo.ScanningGapListTail = null;
                }

                /* link it in */
                NewConsCell.Next = NewConsCell.MyInstr.ExecutingOscillatorBanks;
                NewConsCell.MyInstr.ExecutingOscillatorBanks = NewConsCell;
                NewConsCell.Previous = null;
                NewConsCell.MyInstr = null;

                if (TrackInfo.events != null)
                {
                    NewConsCell.seq = TrackInfo.seqGen++;

                    TrackInfo.events.Add(
                        new EventTraceRec(
                            EventTraceType.Start,
                            NewConsCell.seq,
                            NewConsCell.frameIndex,
                            NewConsCell.noteIndex));
                }
            }

            /* finally, clean out anything that came after the last note we scheduled */
            /* during this cycle */
            GenerateChannelOperations(
                TrackInfo,
                TrackInfo.ExecutionIndex,
                SynthParams);
        }

        /* auxiliary routine for PlayTrackUpdate */
        /* check to see if oscillators should be terminated */
        private static void OscillatorCycleCheckPendingTerminate(
            PlayTrackInfoRec TrackInfo,
            SynthParamRec SynthParams)
        {
            /* see if we should release all active notes (when this condition is */
            /* true, scanning gap should be empty since index was set to end of */
            /* track as well). */
            if (TrackInfo.TerminationPending
                && (TrackInfo.ExecutionIndex >= TrackInfo.TerminationIndex))
            {
#if DEBUG
                if (TrackInfo.ScanningGapListHead != null)
                {
                    // scanning gap not empty at termination time
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
#endif

                PerInstrRec InstrScan = TrackInfo.Instrs;
                while (InstrScan != null)
                {
                    OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                    while (OscBankScan != null)
                    {
                        /* release all envelope generators */
                        OscStateBankReleaseEnvelopesNow(
                            OscBankScan.OscBank,
                            WhichEnvType.eRlsAll,
                            false/*allow existing schedule to hold*/);

                        /* break any ties still pending */
                        while (OscBankScan.TieContinuationList != null)
                        {
                            FrozenNoteConsCell FrozenNote = OscBankScan.TieContinuationList;
                            OscBankScan.TieContinuationList = OscBankScan.TieContinuationList.Next;
                        }
                        OscBankScan.TieTarget = null;

                        OscBankScan = OscBankScan.Next;
                    }

                    InstrScan = InstrScan.Next;
                }
            }
        }

        /* auxiliary routine for PlayTrackUpdate */
        /* check for invalid sequence commanding */
        private static SynthErrorCodes CheckInvalidSequenceCommanding(
            PlayTrackInfoRec TrackInfo/*target*/,
            PlayTrackInfoRec CommandSourceTrackInfo/*issuer*/,
            SynthParamRec SynthParams)
        {
            if (CommandSourceTrackInfo.PositionNumber > TrackInfo.PositionNumber)
            {
                SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExLaterTrackIssusedCommandToEarlierTrack;
                SynthParams.ErrorInfo.IssuingTrackName = CommandSourceTrackInfo.TrackObject.Name;
                SynthParams.ErrorInfo.ReceivingTrackName = TrackInfo.TrackObject.Name;
                return SynthErrorCodes.eSynthErrorEx;
            }
            return SynthErrorCodes.eSynthDone;
        }

        /* aux routine to fast-forward a bunch of tracks */
        private static void SkipHelper(
            LargeBCDType Beats,
            int ScanningGapFrontInEnvelopeTicks,
            SkipSegmentsRec SkipSchedule,
            SynthParamRec SynthParams)
        {
            FractionRec FracBeats;
            double dDurationTicks;

            /* compute exact amount to skip */
            FractionRec.Double2Fraction((double)Beats, (DURATIONUPDATECLOCKRESOLUTION / 4), out FracBeats);
            dDurationTicks = FractionRec.Fraction2Double(FracBeats) * (DURATIONUPDATECLOCKRESOLUTION / 4);

            /* schedule the skip */
            SkipSegmentsAdd(
                SkipSchedule,
                ScanningGapFrontInEnvelopeTicks
                    + SynthParams.iScanningGapWidthInEnvelopeTicks/*start 1 whole scanning gap from now*/,
                dDurationTicks/*go for the specified number of beats*/);
        }

        /* auxiliary routine for PlayTrackUpdate */
        /* update envelopes */
        private static SynthErrorCodes OscillatorCycleUpdateEnvelope(
            PlayTrackInfoRec TrackInfo,
            SynthParamRec SynthParams)
        {
            /* wave generator and envelope update loop */
            PerInstrRec InstrScan = TrackInfo.Instrs;
            while (InstrScan != null)
            {
                OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                while (OscBankScan != null)
                {
                    SynthErrorCodes error = OscStateBankGenerateEnvelopes(
                        OscBankScan.OscBank,
                        OscBankScan.fDontUpdateEnvelopes,
                        SynthParams,
                        out OscBankScan.StillActive);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }

                    OscBankScan.StillActive = OscBankScan.StillActive
                        || (OscBankScan.TieContinuationList != null);

                    OscBankScan.StillActive = OscBankScan.StillActive
                        || (OscBankScan.TieTarget != null);

                    OscBankScan = OscBankScan.Next;
                }

                InstrScan = InstrScan.Next;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* this is called to execute a command on the track */
        public static SynthErrorCodes PlayTrackInfoExecuteCommand(
            PlayTrackInfoRec TrackInfo,
            int ScanningGapFrontInEnvelopeTicks,
            CommandNoteObjectRec Command,
            PlayTrackInfoRec CommandSourceTrackInfo,
            SkipSegmentsRec SkipSchedule,
            SynthParamRec SynthParams)
        {
            SynthErrorCodes Error;

            /* validate */
            Error = CheckInvalidSequenceCommanding(
                TrackInfo,
                CommandSourceTrackInfo,
                SynthParams);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            /* if it's an effect command, then send it to the effect processor */
            /* otherwise we handle it */
            NoteCommands CmdOpcode = Command.GetCommandOpcode();
            switch (CmdOpcode)
            {
                default:
                    ExecuteParamCommandFrame(
                        TrackInfo.ParameterController,
                        Command,
                        SynthParams);
                    break;

                case NoteCommands.eCmdSetEffectParam1:
                case NoteCommands.eCmdIncEffectParam1:
                case NoteCommands.eCmdSweepEffectParamAbs1:
                case NoteCommands.eCmdSweepEffectParamRel1:
                case NoteCommands.eCmdSetEffectParam2:
                case NoteCommands.eCmdIncEffectParam2:
                case NoteCommands.eCmdSweepEffectParamAbs2:
                case NoteCommands.eCmdSweepEffectParamRel2:
                case NoteCommands.eCmdSetEffectParam3:
                case NoteCommands.eCmdIncEffectParam3:
                case NoteCommands.eCmdSweepEffectParamAbs3:
                case NoteCommands.eCmdSweepEffectParamRel3:
                case NoteCommands.eCmdSetEffectParam4:
                case NoteCommands.eCmdIncEffectParam4:
                case NoteCommands.eCmdSweepEffectParamAbs4:
                case NoteCommands.eCmdSweepEffectParamRel4:
                case NoteCommands.eCmdSetEffectParam5:
                case NoteCommands.eCmdIncEffectParam5:
                case NoteCommands.eCmdSweepEffectParamAbs5:
                case NoteCommands.eCmdSweepEffectParamRel5:
                case NoteCommands.eCmdSetEffectParam6:
                case NoteCommands.eCmdIncEffectParam6:
                case NoteCommands.eCmdSweepEffectParamAbs6:
                case NoteCommands.eCmdSweepEffectParamRel6:
                case NoteCommands.eCmdSetEffectParam7:
                case NoteCommands.eCmdIncEffectParam7:
                case NoteCommands.eCmdSweepEffectParamAbs7:
                case NoteCommands.eCmdSweepEffectParamRel7:
                case NoteCommands.eCmdSetEffectParam8:
                case NoteCommands.eCmdIncEffectParam8:
                case NoteCommands.eCmdSweepEffectParamAbs8:
                case NoteCommands.eCmdSweepEffectParamRel8:
                case NoteCommands.eCmdTrackEffectEnable:
                    {
                        PerInstrRec InstrScan = TrackInfo.Instrs;
                        while (InstrScan != null)
                        {
                            TrackEffectHandleCommand(
                                InstrScan.EffectGenerator,
                                Command,
                                ScanningGapFrontInEnvelopeTicks,
                                SynthParams);

                            InstrScan = InstrScan.Next;
                        }
                    }
                    break;

                case NoteCommands.eCmdSetScoreEffectParam1:
                case NoteCommands.eCmdIncScoreEffectParam1:
                case NoteCommands.eCmdSweepScoreEffectParamAbs1:
                case NoteCommands.eCmdSweepScoreEffectParamRel1:
                case NoteCommands.eCmdSetScoreEffectParam2:
                case NoteCommands.eCmdIncScoreEffectParam2:
                case NoteCommands.eCmdSweepScoreEffectParamAbs2:
                case NoteCommands.eCmdSweepScoreEffectParamRel2:
                case NoteCommands.eCmdSetScoreEffectParam3:
                case NoteCommands.eCmdIncScoreEffectParam3:
                case NoteCommands.eCmdSweepScoreEffectParamAbs3:
                case NoteCommands.eCmdSweepScoreEffectParamRel3:
                case NoteCommands.eCmdSetScoreEffectParam4:
                case NoteCommands.eCmdIncScoreEffectParam4:
                case NoteCommands.eCmdSweepScoreEffectParamAbs4:
                case NoteCommands.eCmdSweepScoreEffectParamRel4:
                case NoteCommands.eCmdSetScoreEffectParam5:
                case NoteCommands.eCmdIncScoreEffectParam5:
                case NoteCommands.eCmdSweepScoreEffectParamAbs5:
                case NoteCommands.eCmdSweepScoreEffectParamRel5:
                case NoteCommands.eCmdSetScoreEffectParam6:
                case NoteCommands.eCmdIncScoreEffectParam6:
                case NoteCommands.eCmdSweepScoreEffectParamAbs6:
                case NoteCommands.eCmdSweepScoreEffectParamRel6:
                case NoteCommands.eCmdSetScoreEffectParam7:
                case NoteCommands.eCmdIncScoreEffectParam7:
                case NoteCommands.eCmdSweepScoreEffectParamAbs7:
                case NoteCommands.eCmdSweepScoreEffectParamRel7:
                case NoteCommands.eCmdSetScoreEffectParam8:
                case NoteCommands.eCmdIncScoreEffectParam8:
                case NoteCommands.eCmdSweepScoreEffectParamAbs8:
                case NoteCommands.eCmdSweepScoreEffectParamRel8:
                case NoteCommands.eCmdScoreEffectEnable:
                    TrackEffectHandleCommand(
                        TrackInfo.ScoreEffectGenerator,
                        Command,
                        ScanningGapFrontInEnvelopeTicks,
                        SynthParams);
                    break;

                case NoteCommands.eCmdSetSectionEffectParam1:
                case NoteCommands.eCmdIncSectionEffectParam1:
                case NoteCommands.eCmdSweepSectionEffectParamAbs1:
                case NoteCommands.eCmdSweepSectionEffectParamRel1:
                case NoteCommands.eCmdSetSectionEffectParam2:
                case NoteCommands.eCmdIncSectionEffectParam2:
                case NoteCommands.eCmdSweepSectionEffectParamAbs2:
                case NoteCommands.eCmdSweepSectionEffectParamRel2:
                case NoteCommands.eCmdSetSectionEffectParam3:
                case NoteCommands.eCmdIncSectionEffectParam3:
                case NoteCommands.eCmdSweepSectionEffectParamAbs3:
                case NoteCommands.eCmdSweepSectionEffectParamRel3:
                case NoteCommands.eCmdSetSectionEffectParam4:
                case NoteCommands.eCmdIncSectionEffectParam4:
                case NoteCommands.eCmdSweepSectionEffectParamAbs4:
                case NoteCommands.eCmdSweepSectionEffectParamRel4:
                case NoteCommands.eCmdSetSectionEffectParam5:
                case NoteCommands.eCmdIncSectionEffectParam5:
                case NoteCommands.eCmdSweepSectionEffectParamAbs5:
                case NoteCommands.eCmdSweepSectionEffectParamRel5:
                case NoteCommands.eCmdSetSectionEffectParam6:
                case NoteCommands.eCmdIncSectionEffectParam6:
                case NoteCommands.eCmdSweepSectionEffectParamAbs6:
                case NoteCommands.eCmdSweepSectionEffectParamRel6:
                case NoteCommands.eCmdSetSectionEffectParam7:
                case NoteCommands.eCmdIncSectionEffectParam7:
                case NoteCommands.eCmdSweepSectionEffectParamAbs7:
                case NoteCommands.eCmdSweepSectionEffectParamRel7:
                case NoteCommands.eCmdSetSectionEffectParam8:
                case NoteCommands.eCmdIncSectionEffectParam8:
                case NoteCommands.eCmdSweepSectionEffectParamAbs8:
                case NoteCommands.eCmdSweepSectionEffectParamRel8:
                case NoteCommands.eCmdSectionEffectEnable:
                    if (TrackInfo.SectionEffectGenerator != null)
                    {
                        TrackEffectHandleCommand(
                            TrackInfo.SectionEffectGenerator,
                            Command,
                            ScanningGapFrontInEnvelopeTicks,
                            SynthParams);
                    }
                    break;

                case NoteCommands.eCmdSequenceEnd:
                    /* go back to beginning, if looping is engaged */
                    if (TrackInfo.CurrentSequenceStartFrameIndex != -1)
                    {
                        TrackInfo.FrameArrayIndex = TrackInfo.CurrentSequenceStartFrameIndex;
                        ScheduleBreakPendingTies(
                            TrackInfo,
                            ScanningGapFrontInEnvelopeTicks,
                            SynthParams); /* break ties that are still waiting */
                        TrackInfo.JumpCounter += 1; /* this constitutes a jump, must be AFTER scheduler */
                    }
                    break;

                case NoteCommands.eCmdSetSequence: /* <string1> holds track/group name, <string2> hold sequence name */
                    {
                        Error = InvokeSequenceTarget(
                            TrackInfo,
                            Command.GetCommandStringArg1(),
                            Command,
                            ScanningGapFrontInEnvelopeTicks,
                            SkipSchedule,
                            SynthParams,
                            PlayTrackInfoJumpToSequence);
                        if (Error != SynthErrorCodes.eSynthDone)
                        {
                            return Error;
                        }
                    }
                    break;

                case NoteCommands.eCmdSetSequenceDeferred: /* <string1> holds track/group name, <string2> hold sequence name */
                    {
                        Error = InvokeSequenceTarget(
                            TrackInfo,
                            Command.GetCommandStringArg1(),
                            Command,
                            ScanningGapFrontInEnvelopeTicks,
                            SkipSchedule,
                            SynthParams,
                            PlayTrackInfoJumpToSequenceAtNextLoop);
                        if (Error != SynthErrorCodes.eSynthDone)
                        {
                            return Error;
                        }
                    }
                    break;

                case NoteCommands.eCmdEndSequencing: /* <string1> holds track/group name */
                    {
                        Error = InvokeSequenceTarget(
                            TrackInfo,
                            Command.GetCommandStringArg1(),
                            null,
                            ScanningGapFrontInEnvelopeTicks,
                            SkipSchedule,
                            SynthParams,
                            PlayTrackInfoTerminateSequencing);
                        if (Error != SynthErrorCodes.eSynthDone)
                        {
                            return Error;
                        }
                    }
                    break;

                case NoteCommands.eCmdSkip: /* <string1> holds track/group name */
                    {
                        SkipHelper(
                            LargeBCDType.FromRawInt32(Command.GetCommandNumericArg1()),
                            ScanningGapFrontInEnvelopeTicks,
                            SkipSchedule,
                            SynthParams);
                    }
                    break;

                case NoteCommands.eCmdIgnoreNextCmd: /* <1l> = probability of ignoring next command */
                    {
                        double Probability;

                        Probability = (double)LargeBCDType.FromRawInt32(Command.GetCommandNumericArg1());
                        if (Probability > 1)
                        {
                            Probability = 1;
                        }
                        else if (Probability < 0)
                        {
                            Probability = 0;
                        }

                        /* at this point FrameArrayIndex is the next command (already */
                        /* been incremented past us), so we don't have to add 1 */
                        if (TrackInfo.FrameArrayIndex < TrackInfo.TotalNumberOfFrames)
                        {
                            /* make sure thing after us is a command */
                            FrameObjectRec Frame = TrackInfo.TrackObject.FrameArray[TrackInfo.FrameArrayIndex];
                            if (Frame.IsThisACommandFrame)
                            {
                                /* for probability == 1, don't waste our precious random number stream. */
                                bool DoIt = (Probability == 1);
                                if (!DoIt)
                                {
                                    /* the random number is always less than 1. */
                                    double RandomNumber = ParkAndMiller.Double0ToExcluding1(TrackInfo.Seed.Random());
                                    /* never happens if probability is zero */
                                    DoIt = (RandomNumber < Probability);
                                }
                                if (DoIt)
                                {
                                    TrackInfo.FrameArrayIndex += 1; /* skip */
                                }
                            }
                        }
                    }
                    break;

                case NoteCommands.eCmdReleaseAll1:
                case NoteCommands.eCmdReleaseAll2:
                case NoteCommands.eCmdReleaseAll3:
                    {
                        /* allocate new release record */
                        ReleaseRec ReleaseTime = new ReleaseRec();

                        ReleaseTime.Type = RelType.eReleaser;

                        /* initialize it */
                        if (CmdOpcode == NoteCommands.eCmdReleaseAll1)
                        {
                            ReleaseTime.Releaser_Which = WhichEnvType.eRls1;
                        }
                        else if (CmdOpcode == NoteCommands.eCmdReleaseAll2)
                        {
                            ReleaseTime.Releaser_Which = WhichEnvType.eRls2;
                        }
                        else
                        {
                            ReleaseTime.Releaser_Which = WhichEnvType.eRls3;
                        }
                        /* do it when notes we're scheduling now finally execute */
                        ReleaseTime.When = ScanningGapFrontInEnvelopeTicks;

                        /* insert into sorted list */
                        InsertChannelOperation(TrackInfo, ReleaseTime);
                    }
                    break;

                case NoteCommands.eCmdRedirect: /* <string> holds target track/group name */
                    TrackInfo.CurrentCommandRedirection = Command.GetCommandStringArg1();
                    break;
                case NoteCommands.eCmdRedirectEnd:
                    TrackInfo.CurrentCommandRedirection = null;
                    break;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* this is called to set the track to the beginning of a named sequence */
        private static SynthErrorCodes PlayTrackInfoJumpToSequenceHelper(
            PlayTrackInfoRec TrackInfo,
            int ScanningGapFrontInEnvelopeTicks,
            CommandNoteObjectRec SequenceCommand,
            PlayTrackInfoRec CommandSourceTrackInfo,
            bool ResetCurrentPosition,
            SynthParamRec SynthParams)
        {
            SynthErrorCodes Error;

            /* get sequence name */
#if DEBUG
            if (!((SequenceCommand.GetCommandOpcode() == NoteCommands.eCmdSetSequence)
                || (SequenceCommand.GetCommandOpcode() == NoteCommands.eCmdSetSequenceDeferred)))
            {
                // wrong kind of command
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            /* validate */
            Error = CheckInvalidSequenceCommanding(
                TrackInfo,
                CommandSourceTrackInfo,
                SynthParams);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            /* only if sequencing hasn't been terminated */
            if (!TrackInfo.TerminationPending)
            {
                string SequenceName = SequenceCommand.GetCommandStringArg2();

                /* do we know about this sequence? (ignore if we don't) */
                int NewSequenceStartFrameIndex;
                if (TrackInfo.SequenceIndexMap.TryGetValue(SequenceName, out NewSequenceStartFrameIndex))
                {
                    /* remember that as the start point for the current sequence. */
#if DEBUG
                    if ((NewSequenceStartFrameIndex >= TrackInfo.TotalNumberOfFrames)
                        || (NewSequenceStartFrameIndex < 0))
                    {
                        // screwed up sequence index
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
#endif
                    TrackInfo.CurrentSequenceStartFrameIndex = NewSequenceStartFrameIndex;

                    /* if desired, immediately go there */
                    if (ResetCurrentPosition)
                    {
                        TrackInfo.FrameArrayIndex = TrackInfo.CurrentSequenceStartFrameIndex;
                        TrackInfo.NextFrameCountDown = 0;
                        ScheduleBreakPendingTies(
                            TrackInfo,
                            ScanningGapFrontInEnvelopeTicks,
                            SynthParams); /* break ties that are still waiting */
                        TrackInfo.JumpCounter += 1; /* must be AFTER scheduler */
                    }
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* this is called to set the track to the beginning of a named sequence */
        public static SynthErrorCodes PlayTrackInfoJumpToSequence(
            PlayTrackInfoRec TrackInfo,
            int ScanningGapFrontInEnvelopeTicks,
            CommandNoteObjectRec SequenceCommand,
            PlayTrackInfoRec CommandSourceTrackInfo,
            SkipSegmentsRec SkipSchedule,
            SynthParamRec SynthParams)
        {
#if DEBUG
            if (!(SequenceCommand.GetCommandOpcode() == NoteCommands.eCmdSetSequence))
            {
                // wrong kind of command
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return PlayTrackInfoJumpToSequenceHelper(
                TrackInfo,
                ScanningGapFrontInEnvelopeTicks,
                SequenceCommand,
                CommandSourceTrackInfo,
                true/*go there now*/,
                SynthParams);
        }

        /* this is called to set the track to the beginning of a named sequence, but not have it */
        /* happen until the next loop occurs */
        public static SynthErrorCodes PlayTrackInfoJumpToSequenceAtNextLoop(
            PlayTrackInfoRec TrackInfo,
            int ScanningGapFrontInEnvelopeTicks,
            CommandNoteObjectRec SequenceCommand,
            PlayTrackInfoRec CommandSourceTrackInfo,
            SkipSegmentsRec SkipSchedule,
            SynthParamRec SynthParams)
        {
#if DEBUG
            if (!(SequenceCommand.GetCommandOpcode() == NoteCommands.eCmdSetSequenceDeferred))
            {
                // wrong kind of command
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return PlayTrackInfoJumpToSequenceHelper(
                TrackInfo,
                ScanningGapFrontInEnvelopeTicks,
                SequenceCommand,
                CommandSourceTrackInfo,
                false/*don't go there now*/,
                SynthParams);
        }

        /* this is called to make the track stop sequencing and go to the end */
        public static SynthErrorCodes PlayTrackInfoTerminateSequencing(
            PlayTrackInfoRec TrackInfo,
            int ScanningGapFrontInEnvelopeTicks,
            CommandNoteObjectRec NotUsed,
            PlayTrackInfoRec CommandSourceTrackInfo,
            SkipSegmentsRec SkipSchedule,
            SynthParamRec SynthParams)
        {
            SynthErrorCodes Error;

            /* validate */
            Error = CheckInvalidSequenceCommanding(
                TrackInfo,
                CommandSourceTrackInfo,
                SynthParams);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            /* stop any active loops */
            /* TrackInfo.CurrentSequenceStartFrameIndex = -1; */
            /* not needed because the next statement will prevent us from looking */
            /* at an end-sequence command ever again. */

            /* jump to end, so no more notes will be scheduled (i.e. whoever is in */
            /* the scanning gap right now are the last ones to go) */
            TrackInfo.FrameArrayIndex = TrackInfo.TotalNumberOfFrames;

            /* this constitutes a jump */
            ScheduleBreakPendingTies(
                TrackInfo,
                ScanningGapFrontInEnvelopeTicks,
                SynthParams);
            TrackInfo.JumpCounter += 1; /* must be AFTER scheduler */

            /* indicate termination is pending, as soon as the current batch of */
            /* notes in the scanning gap have been executed */
            TrackInfo.TerminationPending = true;
            TrackInfo.TerminationIndex = ScanningGapFrontInEnvelopeTicks;

            return SynthErrorCodes.eSynthDone;
        }

        /* notification (comes before PlayTrackUpdateEnvelopes) that this cycle */
        /* is the first cycle (entry) of a scheduled skip period. */
        public static void PlayTrackInfoEnteringOrJustLeftScheduledSkip(
            PlayTrackInfoRec TrackInfo,
            bool fEntry)
        {
            /* mark all oscillator banks as scheduled skips */
            PerInstrRec InstrScan = TrackInfo.Instrs;
            while (InstrScan != null)
            {
                OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                while (OscBankScan != null)
                {
                    /* for entry, set them to true, for exit, set to false */
                    OscBankScan.fDontUpdateEnvelopes = fEntry;
                    OscBankScan = OscBankScan.Next;
                }
                InstrScan = InstrScan.Next;
            }
        }

        /* perform one envelope clock cycle update.  if UpdateEnvelopes is true, then */
        /* envelopes should be updated, otherwise only note scheduling should be performed */
        /* (it's used to open the scanning gap initially). */
        /* if FastForwardMode is true, then not even note scheduling occurs, instead we */
        /* just increment the counters and track position indices (this is used to quickly */
        /* fast-forward to a given point, but have all the sequencing and commands in */
        /* the state they should be in at that point). */
        // This is now split into two methods:
        //   PlayTrackUpdateControl() which may affect global state
        //   PlayTrackUpdateEnvelopes() which is local only, therefore can be concurrent

        // Part 1
        public static SynthErrorCodes PlayTrackUpdateControl(
            PlayTrackInfoRec TrackInfo,
            bool UpdateEnvelopes,
            int NumDurationTicks,
            double EnvelopeTicksPerDurationTick,
            int ScanningGapFrontInEnvelopeTicks,
            bool FastForwardMode,
            SkipSegmentsRec SkipSchedule,
            SynthParamRec SynthParams)
        {
            SynthErrorCodes Result;

            /* update envelope generators & notes, etc. */
            /* there are 3 stages to stuff */
            /*  1. when UpdateEnvelopes is false, the only action is to queue up notes */
            /*     to be played.  this opens the scanning gap. */
            /*  2. when UpdateEnvelopes is true, but ExecutionIndex is still less than zero, */
            /*     notes that start before the start of the song (due to pre-origin segments */
            /*     or other things) are started and processed. */
            /*  3. Eventually, ExecutionIndex will be >= 0, and the official start of the */
            /*     song will have been passed. */

            /* schedule any notes out of the track list into the scanning gap */
            Result = ScheduleNotes(
                TrackInfo,
                ScanningGapFrontInEnvelopeTicks,
                EnvelopeTicksPerDurationTick,
                FastForwardMode,
                SkipSchedule,
                SynthParams);
            if (Result != SynthErrorCodes.eSynthDone)
            {
                return Result;
            }

            /* do timing update */
            TrackInfo.NextFrameCountDown -= NumDurationTicks;

            /* update track parameters */
            ExecuteParamUpdate(
                TrackInfo.ParameterController,
                NumDurationTicks);

            /* generate waveforms, update envelope generators & notes, etc. */
            if (UpdateEnvelopes)
            {
                /* actually do the generation */

                /* note scheduling */
                GeneratePortamentoInitiation(
                    TrackInfo);
                GenerateTieContinuation(
                    TrackInfo,
                    SynthParams);
                GenerateScanningGapScheduler(
                    TrackInfo,
                    SynthParams);

                /* let track effect generator schedule commands */
                PerInstrRec InstrScan = TrackInfo.Instrs;
                while (InstrScan != null)
                {
                    TrackEffectProcessQueuedCommands(
                        InstrScan.EffectGenerator,
                        SynthParams);
                    InstrScan = InstrScan.Next;
                }

                /* increment our scanning gap back edge clock, after scheduling commands */
                /* (this way, commands are scheduled on the beginning of the clock they */
                /* should occur on). */
                TrackInfo.ExecutionIndex += 1;
            }

            return SynthErrorCodes.eSynthDone;
        }

        // Part 2
        public static SynthErrorCodes PlayTrackUpdateEnvelopes(
            PlayTrackInfoRec TrackInfo,
            bool UpdateEnvelopes,
            bool FastForwardMode,
            SynthParamRec SynthParams)
        {
            if (UpdateEnvelopes)
            {
                /* apply oscillators */
                SynthErrorCodes error = OscillatorCycleUpdateEnvelope(
                    TrackInfo,
                    SynthParams);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }


                /* set up effects (only if not fast forwarding) */
                if (!FastForwardMode)
                {
                    PerInstrRec InstrScan = TrackInfo.Instrs;
                    while (InstrScan != null)
                    {
                        error = UpdateStateTrackEffectGenerator(
                            InstrScan.EffectGenerator,
                            SynthParams);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                        TrackEffectSetNoSourceSignal(
                            InstrScan.EffectGenerator,
                            InstrScan.ExecutingOscillatorBanks == null);

                        InstrScan = InstrScan.Next;
                    }
                }


                /* see if we should release all active notes. */
                OscillatorCycleCheckPendingTerminate(
                    TrackInfo,
                    SynthParams);
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* perform one waveform generation cycle.  this is called after */
        /* PlayTrackUpdateEnvelopes.  final output data is added into OutputData. */
        /* TrackWorkspaceArray and OscillatorWorkspaceArray are used for scratch storage, */
        /* and should not be considered to return meaningful data. */
        public static SynthErrorCodes PlayTrackGenerateWave(
            PlayTrackInfoRec TrackInfo,
            bool UpdateEnvelopes,
            float[] workspace,
            int nActualFrames,
            int OutputDataLOffset,
            int OutputDataROffset,
            int TrackWorkspaceLOffset,
            int TrackWorkspaceROffset,
            int OscillatorWorkspaceLOffset,
            int OscillatorWorkspaceROffset,
            int CombinedOscillatorWorkspaceLOffset,
            int CombinedOscillatorWorkspaceROffset,
            SynthParamRec SynthParams)
        {
            /* generate waveforms, etc. */
            if (UpdateEnvelopes)
            {
                PerInstrRec InstrScan = TrackInfo.Instrs;
                while (InstrScan != null)
                {
                    /* initialize array */
                    FloatVectorZero(
                        workspace,
                        TrackWorkspaceLOffset,
                        nActualFrames);
                    FloatVectorZero(
                        workspace,
                        TrackWorkspaceROffset,
                        nActualFrames);


                    /* actually do the generation */

                    /* generate waveforms */
                    OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                    while (OscBankScan != null)
                    {
                        ApplyOscStateBank(
                            OscBankScan.OscBank,
                            workspace,
                            nActualFrames,
                            TrackWorkspaceLOffset,
                            TrackWorkspaceROffset,
                            OscillatorWorkspaceLOffset,
                            OscillatorWorkspaceROffset,
                            CombinedOscillatorWorkspaceLOffset,
                            CombinedOscillatorWorkspaceROffset,
                            SynthParams);

                        OscBankScan = OscBankScan.Next;
                    }

                    /* apply effects to local array */
                    SynthErrorCodes error = ApplyTrackEffectGenerator(
                        InstrScan.EffectGenerator,
                        workspace,
                        nActualFrames,
                        TrackWorkspaceLOffset,
                        TrackWorkspaceROffset,
                        SynthParams);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }


                    /* copy data from local array to global array */
                    FloatVectorAcc(
                        workspace,
                        TrackWorkspaceLOffset,
                        workspace,
                        OutputDataLOffset,
                        nActualFrames);
                    FloatVectorAcc(
                        workspace,
                        TrackWorkspaceROffset,
                        workspace,
                        OutputDataROffset,
                        nActualFrames);


                    InstrScan = InstrScan.Next;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* terminate any oscillators that finished during this envelope clock cycle. */
        public static void PlayTrackFinish(
            PlayTrackInfoRec TrackInfo,
            bool UpdateEnvelopes,
            int NumDurationTicks,
            bool fScheduledSkip,
            SynthParamRec SynthParams)
        {
            /* cleanup */
            PerInstrRec InstrScan = TrackInfo.Instrs;
            while (InstrScan != null)
            {
                if (UpdateEnvelopes)
                {
                    /* actually do the generation */

                    /* clean up finished oscillators */
                    OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                    OscBankConsCell OscBankLag = null;
                    while (OscBankScan != null)
                    {
                        /* If there are no active oscillators, then kill it */
                        /* Also, if we're skipping, and there is a sample-based oscillator, */
                        /* kill it as well.  This is rather hacky, but comes from the fact */
                        /* that we can't deal with skipping samples just by running the envelopes */
                        /* without the wave generator, since the sample scanner doesn't run so */
                        /* without this they'll all bunch up at the beginning and run all at */
                        /* once when the scheduled skip period ends.  Aborting them could cause */
                        /* some pops and unnecessary omissions, but it doesn't seem possible to */
                        /* do it right and maintain efficiency (or at least not without a lot */
                        /* of work).  After all, the whole reason we do it at all (rather than */
                        /* using fast-forward) is to fix tied oscillators which are typically */
                        /* not sampled, so terminating the samples isn't that bad. */
                        //
                        // Further commentary on how this works:
                        // Upon entry to a scheduled skip period, all running osc banks get the
                        // fDontUpdateEnvelopes set to true. [see PlayTrackInfoEnteringOrJustLeftScheduledSkip()]
                        // This gives them a "grace", where they will not be cancelled by the below condition.
                        // All osc banks that initiate within the scheduled skip period do not get that flag
                        // set, so if they contain a sampled oscillator, they'll be terminated by the condition
                        // below. In effect, this nullifies the skipped events (by definition of initiating within
                        // the skip period), but puts anything already initiated when skip starts into a state
                        // of suspended animation (though the release timers still run), so that they can run out
                        // their final release phases naturally when the skip period ends, to reduce glitching.
                        if (!OscBankScan.StillActive
                            || (fScheduledSkip
                                && !OscBankScan.fDontUpdateEnvelopes
                                && OscStateBankContainsSampled(OscBankScan.OscBank)))
                        {
                            if (TrackInfo.events != null)
                            {
                                TrackInfo.events.Add(
                                    new EventTraceRec(
                                        EventTraceType.Stop,
                                        OscBankScan.seq));
                            }

                            /* not tied to anybody, so kill it */
                            FinalizeOscStateBank(
                                OscBankScan.OscBank,
                                SynthParams,
                                true/*writeOutputLogs*/);
                            if (OscBankLag == null)
                            {
                                InstrScan.ExecutingOscillatorBanks = OscBankScan.Next;
                            }
                            else
                            {
                                OscBankLag.Next = OscBankScan.Next;
                            }

                            OscBankScan = OscBankScan.Next;
                        }
                        else
                        {
                            OscBankLag = OscBankScan;
                            OscBankScan = OscBankScan.Next;
                        }
                    }
                }

                /* update effect control parameters (accents), but only after they have been */
                /* applied, so that parameters come from the leading edge of an envelope */
                /* period, rather than the trailing edge. */
                TrackEffectIncrementDurationTimer(
                    InstrScan.EffectGenerator,
                    NumDurationTicks);

                InstrScan = InstrScan.Next;
            }
        }

        /* find out how many duration ticks until the next event */
        public static int PlayTrackEventLookahead(PlayTrackInfoRec TrackInfo)
        {
            return (TrackInfo.NextFrameCountDown > 0) ? TrackInfo.NextFrameCountDown : 0;
        }

        // PlayTrackParamGetter is used without captured arguments, so to avoid allocating a multicast
        // delegate for each use, use this static instance.
        public static readonly ParamGetterMethod _PlayTrackParamGetter = PlayTrackParamGetter;

        public delegate void ParamGetterMethod(
            object ParamGetterContext,
            out AccentRec AccentsOut,
            out AccentRec TrackAccentsOut);

        public static void PlayTrackParamGetter(
            object TrackInfo,
            out AccentRec AccentsOut,
            out AccentRec TrackAccentsOut)
        {
            PlayTrackParamGetter_Specific((PlayTrackInfoRec)TrackInfo, out AccentsOut, out TrackAccentsOut);
        }

        /* used by envelope generator to get parameters from the track for formula evaluation. */
        public static void PlayTrackParamGetter_Specific(
            PlayTrackInfoRec TrackInfo,
            out AccentRec AccentsOut,
            out AccentRec TrackAccentsOut)
        {
            AccentsOut = new AccentRec();
            InitializeAccent(
                ref AccentsOut,
                TrackInfo.ParameterController.Accent1.nd.Current,
                TrackInfo.ParameterController.Accent2.nd.Current,
                TrackInfo.ParameterController.Accent3.nd.Current,
                TrackInfo.ParameterController.Accent4.nd.Current,
                TrackInfo.ParameterController.Accent5.nd.Current,
                TrackInfo.ParameterController.Accent6.nd.Current,
                TrackInfo.ParameterController.Accent7.nd.Current,
                TrackInfo.ParameterController.Accent8.nd.Current);

            /* this is kind of hacky -- pick the first instrument and use it's track */
            /* effect to get the parameters. */
#if DEBUG
            if (TrackInfo.Instrs == null)
            {
                // must have an instrument for this
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif
            TrackEffectGetCurrentAccents(
                TrackInfo.Instrs.EffectGenerator,
                out TrackAccentsOut);
        }

        /* finalize before termination */
        public static void FinalizePlayTrack(
            PlayTrackInfoRec TrackInfo,
            SynthParamRec SynthParams,
            bool writeOutputLogs)
        {
            PerInstrRec InstrScan = TrackInfo.Instrs;
            while (InstrScan != null)
            {
                OscBankConsCell OscBankScan = InstrScan.ExecutingOscillatorBanks;
                while (OscBankScan != null)
                {
                    FinalizeOscStateBank(
                        OscBankScan.OscBank,
                        SynthParams,
                        writeOutputLogs);

                    OscBankScan = OscBankScan.Next;
                }

                FinalizeTrackEffectGenerator(
                    InstrScan.EffectGenerator,
                    SynthParams,
                    writeOutputLogs);

                InstrScan = InstrScan.Next;
            }
        }
    }
}
