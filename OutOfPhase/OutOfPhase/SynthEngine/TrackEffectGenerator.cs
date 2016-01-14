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
using System.Runtime.InteropServices;
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        public class CommandConsCell
        {
            /* link.  only forward link used since insertion sorting is not necessary */
            public CommandConsCell Next;

            /* command that has been suspended */
            public CommandNoteObjectRec Command;

            /* this is the start time of the note, used for ordering the scanning gap list */
            public int StartTime;
        }

        public class OneEffectRec
        {
            public OneEffectRec Next;
            public ITrackEffect u;
        }

        public class TrackEffectGenRec
        {
            public IncrParamOneNDRec Accents0;
            public IncrParamOneNDRec Accents1;
            public IncrParamOneNDRec Accents2;
            public IncrParamOneNDRec Accents3;
            public IncrParamOneNDRec Accents4;
            public IncrParamOneNDRec Accents5;
            public IncrParamOneNDRec Accents6;
            public IncrParamOneNDRec Accents7;

            public OneEffectRec List;

            /* list of commands that have been scheduled but haven't been executed yet. */
            public CommandConsCell ScanningGapListHead;
            public CommandConsCell ScanningGapListTail;
            /* this is the current duration update index for the scanning gap list */
            public int ExecutionIndex;

            /* track effect enable.  turns off processing, but command handling */
            /* remains enabled (otherwise you couldn't turn processing back on) */
            public bool Enable;
            public bool AutoQuiescence;
            public bool AutoQuiescenceNoSourceSignal;
            public float GateLevel;
            public int WindowDuration;
            public int CurrentWindowDuration;
        }

        /* accent init helper */
        private static void InitAccentTracker(ref IncrParamOneNDRec nd)
        {
            nd.Current = 0;
            nd.ChangeCountdown = 0;
            ResetLinearTransition(
                ref nd.Change,
                0,
                0,
                1);
        }

        /* create a new track effect generator */
        public static SynthErrorCodes NewTrackEffectGenerator(
            EffectSpecListRec SpecList,
            SynthParamRec SynthParams,
            out TrackEffectGenRec GeneratorOut)
        {
            GeneratorOut = null;

            TrackEffectGenRec Generator = new TrackEffectGenRec();

            Generator.Enable = true;
            Generator.AutoQuiescence = EffectSpecListIsAutoQuiescenceEnabled(SpecList);
            if (Generator.AutoQuiescence)
            {
                Generator.Enable = false; /* start with it off in this case */
                Generator.GateLevel = (float)(Math.Pow(
                    2,
                    EffectSpecListGetAutoQuiescenceDecibels(SpecList) * (1 / -6.0205999132796239))
                    / SynthParams.fOverallVolumeScaling);
                Generator.WindowDuration = (int)(SynthParams.dEnvelopeRate
                    * EffectSpecListGetAutoQuiescenceWindowDuration(SpecList));
                Generator.CurrentWindowDuration = 0;
            }

            /* this is the current envelope update index for removing things from the */
            /* scanning gap list (i.e. the back edge of the scanning gap) */
            /* by setting this negative, we cause the scanning gap to open. */
            Generator.ExecutionIndex = -SynthParams.iScanningGapWidthInEnvelopeTicks;

            /* initialize accent trackers */
            InitAccentTracker(ref Generator.Accents0);
            InitAccentTracker(ref Generator.Accents1);
            InitAccentTracker(ref Generator.Accents2);
            InitAccentTracker(ref Generator.Accents3);
            InitAccentTracker(ref Generator.Accents4);
            InitAccentTracker(ref Generator.Accents5);
            InitAccentTracker(ref Generator.Accents6);
            InitAccentTracker(ref Generator.Accents7);

            /* build list of thingers */
            OneEffectRec Appender = null;
            int l = GetEffectSpecListLength(SpecList);
            for (int i = 0; i < l; i += 1)
            {
                /* see if effect is enabled */
                if (IsEffectFromEffectSpecListEnabled(SpecList, i))
                {
                    OneEffectRec Effect = new OneEffectRec();

                    /* link */
                    Effect.Next = null;
                    if (Appender == null)
                    {
                        Generator.List = Effect;
                    }
                    else
                    {
                        Appender.Next = Effect;
                    }
                    Appender = Effect;

                    /* fill in fields */
                    EffectTypes Type = GetEffectSpecListElementType(SpecList, i);
                    switch (Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case EffectTypes.eDelayEffect:
                            Effect.u = DelayUnifiedRec.NewTrackDelayLineProcessor(
                                GetDelayEffectFromEffectSpecList(SpecList, i),
                                SynthParams);
                            break;
                        case EffectTypes.eNLProcEffect:
                            Effect.u = NLProcUnifiedRec.NewTrackNLProcProcessor(
                                GetNLProcEffectFromEffectSpecList(SpecList, i),
                                SynthParams);
                            break;
                        case EffectTypes.eFilterEffect:
                            Effect.u = FilterArrayRec.NewTrackFilterArrayProcessor(
                                GetFilterEffectFromEffectSpecList(SpecList, i),
                                SynthParams);
                            break;
                        case EffectTypes.eAnalyzerEffect:
                            Effect.u = AnalyzerRec.NewAnalyzer(
                                GetAnalyzerEffectFromEffectSpecList(SpecList, i),
                                SynthParams);
                            break;
                        case EffectTypes.eHistogramEffect:
                            Effect.u = HistogramRec.NewHistogram(
                                GetHistogramEffectFromEffectSpecList(SpecList, i),
                                SynthParams);
                            break;
                        case EffectTypes.eResamplerEffect:
                            Effect.u = ResamplerRec.NewResampler(
                                GetResamplerEffectFromEffectSpecList(SpecList, i),
                                SynthParams);
                            break;
                        case EffectTypes.eCompressorEffect:
                            Effect.u = CompressorRec.NewTrackCompressor(
                                GetCompressorEffectFromEffectSpecList(SpecList, i),
                                SynthParams);
                            break;
                        case EffectTypes.eVocoderEffect:
                            Effect.u = VocoderRec.NewTrackVocoder(
                                GetVocoderEffectFromEffectSpecList(SpecList, i),
                                SynthParams);
                            break;
                        case EffectTypes.eIdealLowpassEffect:
                            Effect.u = IdealLPRec.NewIdealLP(
                                GetIdealLPEffectFromEffectSpecList(SpecList, i),
                                SynthParams);
                            break;
                        case EffectTypes.eConvolverEffect:
                            {
                                ConvolverRec ConvolverEffect;
                                SynthErrorCodes Result = ConvolverRec.NewConvolver(
                                    GetConvolverEffectFromEffectSpecList(SpecList, i),
                                    SynthParams,
                                    out ConvolverEffect);
                                Effect.u = ConvolverEffect;
                                if (Result != SynthErrorCodes.eSynthDone)
                                {
                                    return Result;
                                }
                            }
                            break;
                        case EffectTypes.eUserEffect:
                            {
                                UserEffectProcRec userEffect;
                                SynthErrorCodes error = UserEffectProcRec.NewTrackUserEffectProc(
                                    GetUserEffectFromEffectSpecList(SpecList, i),
                                    SynthParams,
                                    out userEffect);
                                if (error != SynthErrorCodes.eSynthDone)
                                {
                                    return error;
                                }
                            }
                            break;
                    }
                }
            }

            GeneratorOut = Generator;
            return SynthErrorCodes.eSynthDone;
        }

        /* update control state for effects processors.  this is called once per envelope */
        /* tick, and constitutes the first half of the control-update cycle. */
        /* returns true if successful, or false if it failed. */
        public static SynthErrorCodes UpdateStateTrackEffectGenerator(
            TrackEffectGenRec Generator,
            SynthParamRec SynthParams)
        {
            if (Generator.List != null)
            {
                AccentRec Accents = new AccentRec();
                InitializeAccent(
                    ref Accents,
                    Generator.Accents0.Current,
                    Generator.Accents1.Current,
                    Generator.Accents2.Current,
                    Generator.Accents3.Current,
                    Generator.Accents4.Current,
                    Generator.Accents5.Current,
                    Generator.Accents6.Current,
                    Generator.Accents7.Current);

                OneEffectRec Scan = Generator.List;
                while (Scan != null)
                {
                    SynthErrorCodes error = Scan.u.TrackUpdateState(
                        ref Accents,
                        SynthParams);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                    Scan = Scan.Next;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* autoquiescence processor */
        /* InputData = true: always happens, when examining input data */
        /* InputData = false: doesn't always happen, examines generated data */
        private static void AutoQuiescenceDetector(
            TrackEffectGenRec Generator,
            float[] Data,
            int lOffset,
            int rOffset,
            int nActualFrames,
            bool InputData)
        {
#if DEBUG
            if (!Generator.AutoQuiescence)
            {
                // being called when auto-quiescence is disabled
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif

            /* short-circuit for case where we have reached quiescence and there are no */
            /* events scheduled. */
            if (!Generator.Enable && Generator.AutoQuiescenceNoSourceSignal)
            {
                return;
            }

            bool exceeded =
                FloatVectorReductionMagnitudeExceeded(
                    Generator.GateLevel,
                    Data,
                    lOffset,
                    nActualFrames)
                &&
                FloatVectorReductionMagnitudeExceeded(
                    Generator.GateLevel,
                    Data,
                    rOffset,
                    nActualFrames);

            /* if a data point ever exceeds gate, then immediately enable processing and */
            /* reset window timer */
            if (exceeded)
            {
                Generator.Enable = true;
                Generator.CurrentWindowDuration = 0;
            }

            /* current duration is the length of time since we saw something exceed gate. */
            /* if we go an entire window without seeing something exceed gate, then turn */
            /* off processing. */
            if (Generator.CurrentWindowDuration > Generator.WindowDuration)
            {
                Generator.Enable = false;
            }

            /* increment window timer, only if doing input data */
            if (InputData)
            {
                Generator.CurrentWindowDuration += 1;
            }
        }

        /* apply effects to data generated during this envelope clock cycle.  this is the */
        /* second half of the control-update cycle. */
        public static SynthErrorCodes ApplyTrackEffectGenerator(
            TrackEffectGenRec Generator,
            float[] workspace,
            int nActualFrames,
            int lOffset,
            int rOffset,
            SynthParamRec SynthParams)
        {
            OneEffectRec Scan = Generator.List;
            if (Scan != null)
            {
                /* auto-quiescence prepass -- if input exceeds level, then enable */
                if (Generator.AutoQuiescence)
                {
                    AutoQuiescenceDetector(
                        Generator,
                        workspace,
                        lOffset,
                        rOffset,
                        nActualFrames,
                        true/*examining input data*/);
                }

                if (Generator.Enable)
                {
                    while (Scan != null)
                    {
                        SynthErrorCodes error = Scan.u.Apply(
                            workspace,
                            lOffset,
                            rOffset,
                            nActualFrames,
                            SynthParams);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                        Scan = Scan.Next;
                    }

                    /* auto-quiescence postpass -- if effects were applied, then */
                    /* examine the output */
                    if (Generator.AutoQuiescence)
                    {
                        AutoQuiescenceDetector(
                            Generator,
                            workspace,
                            lOffset,
                            rOffset,
                            nActualFrames,
                            false/*examining output data*/);
                    }
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* finalize before termination */
        public static void FinalizeTrackEffectGenerator(
            TrackEffectGenRec Generator,
            SynthParamRec SynthParams,
            bool writeOutputLogs)
        {
            OneEffectRec Scan = Generator.List;
            if (Scan != null)
            {
                if (Generator.Enable)
                {
                    while (Scan != null)
                    {
                        Scan.u.Finalize(
                            SynthParams,
                            writeOutputLogs);
                        Scan = Scan.Next;
                    }
                }
            }
        }

        /* hand off command to be handled by effect generator.  the command will be scheduled */
        /* to occur at the time ScanningGapFrontInEnvelopeTicks (which will be */
        /* ScanningGapWidthInEnvelopeTicks in the future from now). */
        public static void TrackEffectHandleCommand(
            TrackEffectGenRec Generator,
            CommandNoteObjectRec Command,
            int ScanningGapFrontInEnvelopeTicks,
            SynthParamRec SynthParams)
        {
            /* don't bother unless there are effects to process */
            if (Generator.List != null)
            {
                CommandConsCell Cell = new CommandConsCell();

                /* fill in data fields */
                Cell.Command = Command;
                Cell.StartTime = ScanningGapFrontInEnvelopeTicks;

                /* insert into list */
                /* there is no need to sort, since the start time of commands can't */
                /* be adjusted.  just append to the list */
                Cell.Next = null;
                if (Generator.ScanningGapListTail == null)
                {
                    Generator.ScanningGapListHead = Cell;
                }
                else
                {
                    Generator.ScanningGapListTail.Next = Cell;
                }
                Generator.ScanningGapListTail = Cell;
            }
        }

        /* increment duration timer.  this is called once per envelope tick to adjust */
        /* all of the parameter transition tracking devices */
        public static void TrackEffectIncrementDurationTimer(
            TrackEffectGenRec Generator,
            int NumDurationTicks)
        {
            /* don't bother unless there are effects to process */
            if (Generator.List != null)
            {
                UpdateOne(ref Generator.Accents0, NumDurationTicks);
                UpdateOne(ref Generator.Accents1, NumDurationTicks);
                UpdateOne(ref Generator.Accents2, NumDurationTicks);
                UpdateOne(ref Generator.Accents3, NumDurationTicks);
                UpdateOne(ref Generator.Accents4, NumDurationTicks);
                UpdateOne(ref Generator.Accents5, NumDurationTicks);
                UpdateOne(ref Generator.Accents6, NumDurationTicks);
                UpdateOne(ref Generator.Accents7, NumDurationTicks);
            }
        }

        /* process commands in the queue that occur now.  this should be called after */
        /* queueing commands, but before incrementing the execution index, and before */
        /* processing the data, so that commands are handled at the beginning of a */
        /* transition. */
        public static void TrackEffectProcessQueuedCommands(
            TrackEffectGenRec Generator,
            SynthParamRec SynthParams)
        {
            if (Generator.List != null)
            {
                while ((Generator.ScanningGapListHead != null)
                    && (Generator.ScanningGapListHead.StartTime <= Generator.ExecutionIndex))
                {
                    CommandConsCell Cell;

                    /* since the start time of commands can't be adjusted, there should */
                    /* never be a command that is strictly less than. */
#if DEBUG
                    if (Generator.ScanningGapListHead.StartTime < Generator.ExecutionIndex)
                    {
                        // early command
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
#endif
                    /* unlink the cons cell */
                    Cell = Generator.ScanningGapListHead;
                    Generator.ScanningGapListHead = Generator.ScanningGapListHead.Next;
                    if (Generator.ScanningGapListHead == null)
                    {
                        Generator.ScanningGapListTail = null;
                    }
                    /* see what we're supposed to do with the command */
                    switch ((NoteCommands)(Cell.Command.Flags & ~NoteFlags.eCommandFlag))
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case NoteCommands.eCmdSetEffectParam1: /* specify the new default effect parameter in <1l> */
                        case NoteCommands.eCmdSetScoreEffectParam1:
                        case NoteCommands.eCmdSetSectionEffectParam1:
                            Generator.Accents0.Current
                                = (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents0.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdIncEffectParam1: /* add <1l> to the default effect parameter */
                        case NoteCommands.eCmdIncScoreEffectParam1:
                        case NoteCommands.eCmdIncSectionEffectParam1:
                            Generator.Accents0.Current
                                += (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents0.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdSweepEffectParamAbs1: /* <1l> = new effect parameter, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamAbs1:
                        case NoteCommands.eCmdSweepSectionEffectParamAbs1:
                            SweepToNewValue(
                                ref Generator.Accents0,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;
                        case NoteCommands.eCmdSweepEffectParamRel1: /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamRel1:
                        case NoteCommands.eCmdSweepSectionEffectParamRel1:
                            SweepToAdjustedValue(
                                ref Generator.Accents0,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;

                        case NoteCommands.eCmdSetEffectParam2: /* specify the new default effect parameter in <1l> */
                        case NoteCommands.eCmdSetScoreEffectParam2:
                        case NoteCommands.eCmdSetSectionEffectParam2:
                            Generator.Accents1.Current
                                = (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents1.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdIncEffectParam2: /* add <1l> to the default effect parameter */
                        case NoteCommands.eCmdIncScoreEffectParam2:
                        case NoteCommands.eCmdIncSectionEffectParam2:
                            Generator.Accents1.Current
                                += (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents1.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdSweepEffectParamAbs2: /* <1l> = new effect parameter, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamAbs2:
                        case NoteCommands.eCmdSweepSectionEffectParamAbs2:
                            SweepToNewValue(
                                ref Generator.Accents1,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;
                        case NoteCommands.eCmdSweepEffectParamRel2: /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamRel2:
                        case NoteCommands.eCmdSweepSectionEffectParamRel2:
                            SweepToAdjustedValue(
                                ref Generator.Accents1,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;

                        case NoteCommands.eCmdSetEffectParam3: /* specify the new default effect parameter in <1l> */
                        case NoteCommands.eCmdSetScoreEffectParam3:
                        case NoteCommands.eCmdSetSectionEffectParam3:
                            Generator.Accents2.Current
                                = (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents2.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdIncEffectParam3: /* add <1l> to the default effect parameter */
                        case NoteCommands.eCmdIncScoreEffectParam3:
                        case NoteCommands.eCmdIncSectionEffectParam3:
                            Generator.Accents2.Current
                                += (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents2.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdSweepEffectParamAbs3: /* <1l> = new effect parameter, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamAbs3:
                        case NoteCommands.eCmdSweepSectionEffectParamAbs3:
                            SweepToNewValue(
                                ref Generator.Accents2,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;
                        case NoteCommands.eCmdSweepEffectParamRel3: /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamRel3:
                        case NoteCommands.eCmdSweepSectionEffectParamRel3:
                            SweepToAdjustedValue(
                                ref Generator.Accents2,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;

                        case NoteCommands.eCmdSetEffectParam4: /* specify the new default effect parameter in <1l> */
                        case NoteCommands.eCmdSetScoreEffectParam4:
                        case NoteCommands.eCmdSetSectionEffectParam4:
                            Generator.Accents3.Current
                                = (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents3.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdIncEffectParam4: /* add <1l> to the default effect parameter */
                        case NoteCommands.eCmdIncScoreEffectParam4:
                        case NoteCommands.eCmdIncSectionEffectParam4:
                            Generator.Accents3.Current
                                += (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents3.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdSweepEffectParamAbs4: /* <1l> = new effect parameter, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamAbs4:
                        case NoteCommands.eCmdSweepSectionEffectParamAbs4:
                            SweepToNewValue(
                                ref Generator.Accents3,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;
                        case NoteCommands.eCmdSweepEffectParamRel4: /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamRel4:
                        case NoteCommands.eCmdSweepSectionEffectParamRel4:
                            SweepToAdjustedValue(
                                ref Generator.Accents3,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;

                        case NoteCommands.eCmdSetEffectParam5: /* specify the new default effect parameter in <1l> */
                        case NoteCommands.eCmdSetScoreEffectParam5:
                        case NoteCommands.eCmdSetSectionEffectParam5:
                            Generator.Accents4.Current
                                = (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents4.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdIncEffectParam5: /* add <1l> to the default effect parameter */
                        case NoteCommands.eCmdIncScoreEffectParam5:
                        case NoteCommands.eCmdIncSectionEffectParam5:
                            Generator.Accents4.Current
                                += (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents4.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdSweepEffectParamAbs5: /* <1l> = new effect parameter, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamAbs5:
                        case NoteCommands.eCmdSweepSectionEffectParamAbs5:
                            SweepToNewValue(
                                ref Generator.Accents4,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;
                        case NoteCommands.eCmdSweepEffectParamRel5: /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamRel5:
                        case NoteCommands.eCmdSweepSectionEffectParamRel5:
                            SweepToAdjustedValue(
                                ref Generator.Accents4,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;

                        case NoteCommands.eCmdSetEffectParam6: /* specify the new default effect parameter in <1l> */
                        case NoteCommands.eCmdSetScoreEffectParam6:
                        case NoteCommands.eCmdSetSectionEffectParam6:
                            Generator.Accents5.Current
                                = (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents5.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdIncEffectParam6: /* add <1l> to the default effect parameter */
                        case NoteCommands.eCmdIncScoreEffectParam6:
                        case NoteCommands.eCmdIncSectionEffectParam6:
                            Generator.Accents5.Current
                                += (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents5.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdSweepEffectParamAbs6: /* <1l> = new effect parameter, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamAbs6:
                        case NoteCommands.eCmdSweepSectionEffectParamAbs6:
                            SweepToNewValue(
                                ref Generator.Accents5,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;
                        case NoteCommands.eCmdSweepEffectParamRel6: /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamRel6:
                        case NoteCommands.eCmdSweepSectionEffectParamRel6:
                            SweepToAdjustedValue(
                                ref Generator.Accents5,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;

                        case NoteCommands.eCmdSetEffectParam7: /* specify the new default effect parameter in <1l> */
                        case NoteCommands.eCmdSetScoreEffectParam7:
                        case NoteCommands.eCmdSetSectionEffectParam7:
                            Generator.Accents6.Current
                                = (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents6.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdIncEffectParam7: /* add <1l> to the default effect parameter */
                        case NoteCommands.eCmdIncScoreEffectParam7:
                        case NoteCommands.eCmdIncSectionEffectParam7:
                            Generator.Accents6.Current
                                += (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents6.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdSweepEffectParamAbs7: /* <1l> = new effect parameter, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamAbs7:
                        case NoteCommands.eCmdSweepSectionEffectParamAbs7:
                            SweepToNewValue(
                                ref Generator.Accents6,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;
                        case NoteCommands.eCmdSweepEffectParamRel7: /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamRel7:
                        case NoteCommands.eCmdSweepSectionEffectParamRel7:
                            SweepToAdjustedValue(
                                ref Generator.Accents6,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;

                        case NoteCommands.eCmdSetEffectParam8: /* specify the new default effect parameter in <1l> */
                        case NoteCommands.eCmdSetScoreEffectParam8:
                        case NoteCommands.eCmdSetSectionEffectParam8:
                            Generator.Accents7.Current
                                = (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents7.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdIncEffectParam8: /* add <1l> to the default effect parameter */
                        case NoteCommands.eCmdIncScoreEffectParam8:
                        case NoteCommands.eCmdIncSectionEffectParam8:
                            Generator.Accents7.Current
                                += (double)LargeBCDType.FromRawInt32(Cell.Command._Argument1);
                            Generator.Accents7.ChangeCountdown = 0;
                            break;
                        case NoteCommands.eCmdSweepEffectParamAbs8: /* <1l> = new effect parameter, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamAbs8:
                        case NoteCommands.eCmdSweepSectionEffectParamAbs8:
                            SweepToNewValue(
                                ref Generator.Accents7,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;
                        case NoteCommands.eCmdSweepEffectParamRel8: /* <1l> = effect parameter adjust, <2xs> = # of beats to get there */
                        case NoteCommands.eCmdSweepScoreEffectParamRel8:
                        case NoteCommands.eCmdSweepSectionEffectParamRel8:
                            SweepToAdjustedValue(
                                ref Generator.Accents7,
                                LargeBCDType.FromRawInt32(Cell.Command._Argument1),
                                SmallExtBCDType.FromRawInt32(Cell.Command._Argument2));
                            break;

                        case NoteCommands.eCmdTrackEffectEnable:
                        case NoteCommands.eCmdScoreEffectEnable:
                        case NoteCommands.eCmdSectionEffectEnable:
                            if (!Generator.AutoQuiescence)
                            {
                                /* if autoquiescence is enabled, ignore the commands */
                                Generator.Enable = (Cell.Command._Argument1 < 0);
                            }
                            break;
                    }
                }

                /* increment our scanning gap back edge clock, after scheduling commands */
                /* (this way, commands are scheduled on the beginning of the clock they */
                /* should occur on). */
                Generator.ExecutionIndex += 1;
                /* since this routine is only called when samples are being generated, */
                /* we don't have to worry about when to increment this counter */
            }
        }

        /* get current accents */
        public static void TrackEffectGetCurrentAccents(
            TrackEffectGenRec Generator,
            out AccentRec TrackAccentsOut)
        {
            TrackAccentsOut = new AccentRec();
            InitializeAccent(
                ref TrackAccentsOut,
                Generator.Accents0.Current,
                Generator.Accents1.Current,
                Generator.Accents2.Current,
                Generator.Accents3.Current,
                Generator.Accents4.Current,
                Generator.Accents5.Current,
                Generator.Accents6.Current,
                Generator.Accents7.Current);
        }

        /* track event scheduler calls this to indicate whether there are track events or */
        /* not.  If not, then autoquiescence detection can be optimized. */
        public static void TrackEffectSetNoSourceSignal(
            TrackEffectGenRec Generator,
            bool NoSourceSignal)
        {
            Generator.AutoQuiescenceNoSourceSignal = NoSourceSignal;
        }
    }
}
