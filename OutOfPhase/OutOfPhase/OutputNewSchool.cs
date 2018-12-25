/*
 *  Copyright © 1994-2002, 2015-2017 Thomas R. Lawrence
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
using System.Threading;
using TreeLib;

namespace OutOfPhase
{
    public class OutputNewSchool : IDisposable
    {
        public const string EndSequencing = "-";
        public const string DeleteTrack = "/";

        private readonly NewSchoolDocument document;
        private readonly IMainWindowServices mainWindowService;

        private OutputGeneric<OutputDeviceDestination, SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>, OutputDeviceArguments> state;

        private List<TrackRequestRec> trackChangeListRequest;
        private List<TrackStatusRec> activeTracksStatus;
        private readonly Queue<ParamBoardEntry> paramBoardUpdateRequest = new Queue<ParamBoardEntry>();
        private AVLTreeArrayList<ParamBoardEntry> paramBoard = new AVLTreeArrayList<ParamBoardEntry>();

        private readonly Dictionary<TrackObjectRec, Synthesizer.PlayListNodeRec> trackMapping = new Dictionary<TrackObjectRec, Synthesizer.PlayListNodeRec>();
        private readonly List<Synthesizer.PlayListNodeRec> pendingTrackDeletions = new List<Synthesizer.PlayListNodeRec>();
        private bool starting = true;
        private int nextFrameCountDown;
        private int currentFrameCountDownTotal;
        private int criticalFrameCountDownThreshhold;

        private readonly Synthesizer.SkipSegmentsRec emptySkipSchedule = Synthesizer.NewSkipSegments();
        private readonly TrackObjectRec scratchTrack = new TrackObjectRec(new Document());
        private readonly CommandNoteObjectRec command;

        private const float MeteringWindow = .5f;
        private float shortMax, longMax;

        private float dutyCycle;

        private bool mute;

        public OutputNewSchool(NewSchoolDocument document, IMainWindowServices mainWindowService)
        {
            this.document = document;
            this.mainWindowService = mainWindowService;

            this.command = new CommandNoteObjectRec(scratchTrack);
        }

        public void Start()
        {
            if (state != null)
            {
                throw new InvalidOperationException();
            }

            state = SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                String.Empty/*baseName - usually mainWindow.DisplayName*/,
                OutputDeviceEnumerator.OutputDeviceGetDestination,
                OutputDeviceEnumerator.CreateOutputDeviceDestinationHandler,
                new OutputDeviceArguments(document.BufferDuration),
                SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerMainLoop,
                new SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                    mainWindowService,
                    document.Sources[0].Document, // source for score effects
                    null/*listOfTracks*/,
                    null/*keyTrack*/,
                    0/*frameToStartAt*/,
                    document.SamplingRate,
                    document.EnvelopeUpdateRate,
                    NumChannelsType.eSampleStereo, // TODO: parameterize from document?
                    document.BeatsPerMinuteRaw,
                    document.OverallVolumeScalingFactor,
                    document.ScanningGapRaw,
                    document.OutputNumBits,
                    false/*clipWarn*/,
                    document.Oversampling,
                    false/*showSummary*/,
                    document.Deterministic ? (int?)document.Seed : null,
                    true/*stayActiveIfNoFrames*/,
                    true/*robust*/,
                    null/*automationSettings*/,
                    SynthCycleCallback),
                SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerCompletion,
                mainWindowService,
                NumChannelsType.eSampleStereo, // TODO: parameterize from document?
                document.OutputNumBits,
                document.SamplingRate,
                document.Oversampling,
                false/*showProgressWindow*/,
                false/*modal*/,
                (MeteringWindow, MeteringCallback));
        }

        private void MeteringCallback(float shortMax, float longMax, out bool mute)
        {
            lock (this)
            {
                this.shortMax = Math.Max(this.shortMax, shortMax);
                this.longMax = Math.Max(this.longMax, longMax);
            }
            mute = this.mute;
        }

        private Synthesizer.SynthErrorCodes SynthCycleCallback(ref Synthesizer.SynthCycleClientCallbackRec context)
        {
            try
            {
                Synthesizer.SynthErrorCodes result;


                context.synthState.fSuppressingInitialSilence = false; // TODO: should move to a final init callback

                // TODO: move mucking with internals into encapsulating helper methods in the Synthesizer object

                for (int i = 0; i < pendingTrackDeletions.Count; i++)
                {
                    Synthesizer.PlayListNodeRec player = pendingTrackDeletions[i];
                    if (player.ThisTrack.ScanningGapListHead == null)
                    {
                        Synthesizer.PerInstrRec instr = player.ThisTrack.Instrs;
                        while (instr != null)
                        {
                            if (instr.ExecutingOscillatorBanks != null)
                            {
                                goto ContinuePoint;
                            }
                            instr = instr.Next;
                        }

                        pendingTrackDeletions.RemoveAt(i);
                        i--;

                        trackMapping.Remove(player.TrackObject);

                        result = Synthesizer.SynthStateRec.SynthesizerDeleteTrack(
                            context.synthState,
                            player,
                            out context.returnedErrrorInfo);
                        if (result != Synthesizer.SynthErrorCodes.eSynthDone)
                        {
                            return result;
                        }
                    }

                    ContinuePoint:
                    ;
                }

                // volume can update every cycle
                float overallVolumeScaling = (float)(1d / document.OverallVolumeScalingFactor);
                context.synthState.SynthParams0.fOverallVolumeScaling = overallVolumeScaling;
                for (int i = 1; i < context.synthState.concurrency; i++)
                {
                    context.synthState.SynthParamsPerProc[i].fOverallVolumeScaling = overallVolumeScaling;
                }

                if (context.synthState.iScanningGapFrontInEnvelopeTicks < context.synthState.SynthParams0.iScanningGapWidthInEnvelopeTicks)
                {
                    return Synthesizer.SynthErrorCodes.eSynthDone;
                }

                if (!starting)
                {
                    nextFrameCountDown -= context.numNoteDurationTicks; // tempo-sensitive frame countdown
                }
                else
                {
                    starting = false; // avoid adding error to loop countdown
                }
                if (nextFrameCountDown <= 0)
                {
                    // reset countdown time of next loop restart pulse
                    FractionRec loopDuration;
                    NoteNoteObjectRec.ConvertDurationFrac(NoteFlags.e4thNote, out loopDuration);
                    FractionRec.MultFractions(
                        loopDuration,
                        new FractionRec((uint)document.LoopLength, (uint)((int)(document.LoopLength - (int)document.LoopLength) * Constants.Denominator), Constants.Denominator),
                        out loopDuration);
                    Debug.Assert(Synthesizer.DURATIONUPDATECLOCKRESOLUTION % loopDuration.Denominator == 0);
                    currentFrameCountDownTotal = (int)(Synthesizer.DURATIONUPDATECLOCKRESOLUTION * loopDuration.Integer
                        + loopDuration.Fraction * (Synthesizer.DURATIONUPDATECLOCKRESOLUTION / loopDuration.Denominator));
                    nextFrameCountDown += currentFrameCountDownTotal; // add: may contain remainder
                    criticalFrameCountDownThreshhold = context.numNoteDurationTicks;

                    // tempo updates at loop restart
                    context.synthState.SynthParams0.dCurrentBeatsPerMinute = document.BeatsPerMinute;
                    Synthesizer.TempoControlSetBeatsPerMinute(
                        context.synthState.TempoControl,
                        document.BeatsPerMinuteRaw);

                    List<TrackRequestRec> trackChangeList = Interlocked.Exchange<List<TrackRequestRec>>(ref trackChangeListRequest, null);
                    if (trackChangeList != null)
                    {
                        for (int i = 0; i < trackChangeList.Count; i++)
                        {
                            string sequencePart, argumentsPart;
                            {
                                string combined = trackChangeList[i].Sequence;
                                int colon = combined.IndexOf(':');
                                if (colon < 0)
                                {
                                    colon = combined.Length;
                                }
                                sequencePart = combined.Substring(0, colon);
                                argumentsPart = combined.Substring(Math.Min(colon + 1, combined.Length));
                            }

                            Synthesizer.PlayListNodeRec player;
                            if (!trackMapping.TryGetValue(trackChangeList[i].Track, out player) && !String.Equals(sequencePart, DeleteTrack))
                            {
                                result = Synthesizer.SynthStateRec.InitializeSynthesizerAddTrack(
                                    context.synthState,
                                    trackChangeList[i].Track,
                                    trackChangeList[i].Document,
                                    trackChangeList[i].TrackParamProvider,
                                    UpdateFrameArrayNeededMethod,
                                    out player,
                                    out context.returnedErrrorInfo);
                                if (result != Synthesizer.SynthErrorCodes.eSynthDone)
                                {
                                    return result;
                                }
                                player.ThisTrack.FrameArrayIndex = player.ThisTrack.FrameArray.Count;

                                trackMapping.Add(trackChangeList[i].Track, player);
                            }

                            if (player != null)
                            {
                                if (String.Equals(sequencePart, EndSequencing) || String.Equals(sequencePart, DeleteTrack))
                                {
                                    result = Synthesizer.PlayTrackInfoTerminateSequencing(
                                        player.ThisTrack,
                                        context.synthState.iScanningGapFrontInEnvelopeTicks,
                                        null,
                                        player.ThisTrack,
                                        emptySkipSchedule,
                                        context.synthState.SynthParams0);
                                    if (result != Synthesizer.SynthErrorCodes.eSynthDone)
                                    {
                                        return result;
                                    }

                                    if (String.Equals(sequencePart, DeleteTrack))
                                    {
                                        if (!pendingTrackDeletions.Contains(player))
                                        {
                                            pendingTrackDeletions.Add(player);
                                        }
                                    }
                                }
                                else if (!String.IsNullOrEmpty(sequencePart))
                                {
                                    command.PutCommandOpcode(NoteCommands.eCmdSetSequence);
                                    command.PutCommandStringArg1(player.TrackObject.Name); // self-target
                                    command.PutCommandStringArg2(sequencePart);

                                    result = Synthesizer.InvokeSequenceTarget(
                                        player.ThisTrack,
                                        player.TrackObject.Name,
                                        command,
                                        context.synthState.iScanningGapFrontInEnvelopeTicks,
                                        emptySkipSchedule,
                                        context.synthState.SynthParams0,
                                        Synthesizer.PlayTrackInfoJumpToSequence); // immediate jump - break loops halfway through
                                    if (result != Synthesizer.SynthErrorCodes.eSynthDone)
                                    {
                                        return result;
                                    }
                                    player.ThisTrack.ExecutionIndex = context.synthState.iScanningGapFrontInEnvelopeTicks
                                        - context.synthState.SynthParams0.iScanningGapWidthInEnvelopeTicks;
                                }

                                // TODO: handle parse errors gracefully
                                if (!String.IsNullOrEmpty(argumentsPart))
                                {
                                    foreach (string argument in argumentsPart.Split(','))
                                    {
                                        int equals = argument.IndexOf('=');
                                        if (equals < 0)
                                        {
                                            equals = argument.Length;
                                        }

                                        string argumentName = argument.Substring(0, equals).Trim();
                                        string argumentValue = argument.Substring(Math.Min(equals + 1, argument.Length)).Trim();

                                        bool relative;
                                        if (relative = argumentName.StartsWith("+"))
                                        {
                                            argumentName = argumentName.Substring(1);
                                        }
                                        bool reset = false;
                                        if (!relative && (reset = argumentName.StartsWith("/")))
                                        {
                                            argumentName = argumentName.Substring(1);
                                        }

                                        ParamDef cmd;
                                        if (ParamCommands.TryGetValue(argumentName, out cmd))
                                        {
                                            double value = 0, duration = 0;
                                            if (!reset)
                                            {
                                                int slash = argumentValue.IndexOf('/');
                                                if (slash >= 0)
                                                {
                                                    value = cmd.Parse(argumentValue.Substring(0, slash));
                                                    duration = Double.Parse(argumentValue.Substring(slash + 1));
                                                }
                                                else
                                                {
                                                    value = cmd.Parse(argumentValue);
                                                }
                                            }

                                            IssueTrackCommand(
                                                reset ? cmd.ResetOp : (relative ? cmd.SweepRelOp : cmd.SweepAbsOp),
                                                cmd.Target,
                                                value,
                                                duration,
                                                player,
                                                ref context,
                                                command,
                                                scratchTrack);
                                        }
                                        else
                                        {
                                            // special cases
                                            switch (argumentName)
                                            {
                                                default:
                                                    // TODO: report error?
                                                    break;
                                                case "frequencymodel":
                                                case "fm":
                                                    {
                                                        string[] args = argumentValue.Split('.');
                                                        command.PutCommandOpcode(NoteCommands.eCmdLoadFrequencyModel);
                                                        command.PutCommandStringArg1(args[0]);
                                                        int tor = Int32.Parse(args[1])/*tonic offset*/
                                                            * (ParseBool(args[2]) ? -1 : 1)/*relative*/;
                                                    }
                                                    break;

                                                case "bpm":
                                                    break; // TODO:
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                {
                    ParamBoardEntry[] paramBoardRequests = null;
                    lock (paramBoardUpdateRequest)
                    {
                        if (paramBoardUpdateRequest.Count != 0)
                        {
                            paramBoardRequests = paramBoardUpdateRequest.ToArray();
                            paramBoardUpdateRequest.Clear();
                        }
                    }
                    if (paramBoardRequests != null)
                    {
                        AVLTreeArrayList<ParamBoardEntry> paramBoardNew = new AVLTreeArrayList<ParamBoardEntry>(paramBoard);
                        for (int i = 0; i < paramBoardRequests.Length; i++)
                        {
                            if (paramBoardRequests[i] == null) // null entry clears all
                            {
                                paramBoardNew.Clear();
                            }
                            else
                            {
                                if (paramBoardRequests[i].Delete)
                                {
                                    paramBoardNew.TryRemove(paramBoardRequests[i]);
                                }
                                else
                                {
                                    paramBoardNew.TryAdd(paramBoardRequests[i]);
                                }
                                paramBoardRequests[i].ForceUpdateUI();
                            }
                        }
                        paramBoard = paramBoardNew;
                    }
                    foreach (EntryList<ParamBoardEntry> entry in paramBoard)
                    {
                        Synthesizer.PlayListNodeRec player;
                        if (trackMapping.TryGetValue(entry.Key.Track, out player))
                        {
                            ParamDef cmd;
                            if (ParamCommands.TryGetValue(entry.Key.Moniker.Param, out cmd))
                            {
                                if (entry.Key.GetUpdateSynth())
                                {
                                    IssueTrackCommand(
                                        cmd.SweepAbsOp,
                                        cmd.Target,
                                        entry.Key.Value,
                                        0,
                                        player,
                                        ref context,
                                        command,
                                        scratchTrack);
                                }
                                else
                                {
                                    entry.Key.UpdateValueFromSynth(cmd.Peek(player));
                                }
                            }
                        }
                    }
                }

                return Synthesizer.SynthErrorCodes.eSynthDone;
            }
            finally
            {
                List<TrackStatusRec> list = new List<TrackStatusRec>();
                for (int i = 0; i < context.synthState.TrackPlayersInFileOrder.Count; i++)
                {
                    Synthesizer.PlayListNodeRec player = context.synthState.TrackPlayersInFileOrder[i];
                    list.Add(
                        new TrackStatusRec(
                            (Document)player.TrackObject.Parent,
                            player.TrackObject,
                            player.ThisTrack.CurrentSequenceName,
                            pendingTrackDeletions.Contains(player)));
                }
                Interlocked.Exchange(ref this.activeTracksStatus, list);

                this.dutyCycle = context.synthState.dutyCycle;
            }
        }


        private bool UpdateFrameArrayNeededMethod(TrackObjectRec track, uint version)
        {
            if (track.FrameArray.Version != version)
            {
                return true;
            }
            return false;
        }

        public bool Stopped { get { return state.waitFinishedHelper.Finished; } }

        public void Stop()
        {
            state.stopper.Stop();
            state.waitFinishedHelper.WaitFinished();
        }

        public void Dispose()
        {
            Stop();
            state.Dispose();
        }

        // called from main (UI) thread
        public void SetRequest(List<TrackRequestRec> list)
        {
            Interlocked.Exchange(ref trackChangeListRequest, list);
        }

        // called from main (UI) thread
        public List<TrackRequestRec> QueryRequest()
        {
            return trackChangeListRequest;
        }

        // called from main (UI) thread
        public List<TrackStatusRec> DequeueStatus()
        {
            return (List<TrackStatusRec>)Interlocked.Exchange(ref activeTracksStatus, null);
        }

        // called from main (UI) thread
        public float LoopPosition { get { return (currentFrameCountDownTotal - nextFrameCountDown) / Math.Max(1f, currentFrameCountDownTotal); } }

        // called from main (UI) thread
        private const float SecondsOfWarning = 1.5f;
        public float CriticalThreshhold { get { return Math.Max(0, currentFrameCountDownTotal - criticalFrameCountDownThreshhold * document.EnvelopeUpdateRate * SecondsOfWarning) / Math.Max(1f, currentFrameCountDownTotal); } }

        // called from main (UI) thread
        public void EnqueueParamBoardEntry(ParamBoardEntry entry)
        {
            lock (paramBoardUpdateRequest)
            {
                paramBoardUpdateRequest.Enqueue(entry);
            }
        }

        // called from main (UI) thread
        public bool ParamBoardEntriesQueued()
        {
            lock (paramBoardUpdateRequest)
            {
                return paramBoardUpdateRequest.Count != 0;
            }
        }

        // called from main (UI) thread
        public IOrderedList<ParamBoardEntry> ParamBoard { get { return paramBoard; } }

        // called from main (UI) thread
        public (float, float) MeterLevel
        {
            get
            {
                float shortMax, longMax;
                lock (this)
                {
                    shortMax = this.shortMax;
                    this.shortMax = 0;
                    longMax = this.longMax;
                    this.longMax = 0;
                }
                return (shortMax, longMax);
            }
        }

        // called from main (UI) thread
        public bool Mute { get { return mute; } set { mute = value; } }

        // called from main (UI) thread
        public float DutyCycle { get { return dutyCycle; } }

        static OutputNewSchool()
        {
            ParamCommandsArray = CreateCommandArray();
            ParamCommands = CreateCommandMap(ParamCommandsArray);
        }

        public static readonly ParamDef[] ParamCommandsArray;
        public static readonly Dictionary<string, ParamDef> ParamCommands;

        private static ParamDef[] CreateCommandArray()
        {
            List<ParamDef> list = new List<ParamDef>();

            list.Add(new ParamDef(new string[] { "loudness", "volume", "v" }, NoteCommands.eCmdRestoreVolume, NoteCommands.eCmdSweepVolumeAbs, NoteCommands.eCmdSweepVolumeRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Volume.nd.Current));
            list.Add(new ParamDef(new string[] { "pan", "stereoposition", "p" }, NoteCommands.eCmdRestoreStereoPosition, NoteCommands.eCmdSweepStereoAbs, NoteCommands.eCmdSweepStereoRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.StereoPosition.nd.Current));
            list.Add(new ParamDef(new string[] { "release1", "rls1" }, NoteCommands.eCmdRestoreReleasePoint1, NoteCommands.eCmdSweepReleaseAbs1, NoteCommands.eCmdSweepReleaseRel1, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.ReleasePoint1.nd.Current));
            list.Add(new ParamDef(new string[] { "release1origin", "rls1o" }, NoteCommands.eCmdReleasePointOrigin1, NoteCommands.eCmdReleasePointOrigin1, NoteCommands.eCmdReleasePointOrigin1, BoolHelper, Target.Instr, p => p.ThisTrack.ParameterController.ReleasePoint1FromStart ? -1 : 0, "start", "end"));
            list.Add(new ParamDef(new string[] { "release2", "rls2" }, NoteCommands.eCmdRestoreReleasePoint2, NoteCommands.eCmdSweepReleaseAbs2, NoteCommands.eCmdSweepReleaseRel2, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.ReleasePoint2.nd.Current));
            list.Add(new ParamDef(new string[] { "release2origin", "rls2o" }, NoteCommands.eCmdReleasePointOrigin2, NoteCommands.eCmdReleasePointOrigin2, NoteCommands.eCmdReleasePointOrigin2, BoolHelper, Target.Instr, p => p.ThisTrack.ParameterController.ReleasePoint2FromStart ? -1 : 0, "start", "end"));
            list.Add(new ParamDef(new string[] { "pitchdisplacementdepth" }, NoteCommands.eCmdRestorePitchDispDepth, NoteCommands.eCmdSweepPitchDispDepthAbs, NoteCommands.eCmdSweepPitchDispDepthRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.PitchDisplacementDepthLimit.nd.Current));
            list.Add(new ParamDef(new string[] { "pitchdisplacementrate" }, NoteCommands.eCmdRestorePitchDispRate, NoteCommands.eCmdSweepPitchDispRateAbs, NoteCommands.eCmdSweepPitchDispRateRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.PitchDisplacementRateLimit.nd.Current));
            list.Add(new ParamDef(new string[] { "pitchdisplacementoffset" }, NoteCommands.eCmdRestorePitchDispStart, NoteCommands.eCmdSweepPitchDispStartAbs, NoteCommands.eCmdSweepPitchDispStartRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.PitchDisplacementStartPoint.nd.Current));
            list.Add(new ParamDef(new string[] { "pitchdisplacementoffsetorigin" }, NoteCommands.eCmdPitchDispStartOrigin, NoteCommands.eCmdPitchDispStartOrigin, NoteCommands.eCmdPitchDispStartOrigin, BoolHelper, Target.Instr, p => p.ThisTrack.ParameterController.PitchDisplacementStartPointFromStart ? -1 : 0, "start", "end"));
            list.Add(new ParamDef(new string[] { "hurryup", "h" }, NoteCommands.eCmdRestoreHurryUp, NoteCommands.eCmdSweepHurryUpAbs, NoteCommands.eCmdSweepHurryUpRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.HurryUp.nd.Current));
            list.Add(new ParamDef(new string[] { "detune" }, NoteCommands.eCmdRestoreDetune, NoteCommands.eCmdSweepDetuneAbs, NoteCommands.eCmdSweepDetuneRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Detune.nd.Current));
            list.Add(new ParamDef(new string[] { "detunemode" }, NoteCommands.eCmdDetuneMode, NoteCommands.eCmdDetuneMode, NoteCommands.eCmdDetuneMode, BoolHelper, Target.Instr, p => p.ThisTrack.ParameterController.DetuneHertz ? -1 : 0, "hertz", "half-steps"));
            list.Add(new ParamDef(new string[] { "earlylateadjust" }, NoteCommands.eCmdRestoreEarlyLateAdjust, NoteCommands.eCmdSweepEarlyLateAbs, NoteCommands.eCmdSweepEarlyLateRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.EarlyLateAdjust.nd.Current));
            list.Add(new ParamDef(new string[] { "duration" }, NoteCommands.eCmdRestoreDurationAdjust, NoteCommands.eCmdSweepDurationAbs, NoteCommands.eCmdSweepDurationRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.DurationAdjust.nd.Current));
            list.Add(new ParamDef(new string[] { "durationmode" }, NoteCommands.eCmdDurationAdjustMode, NoteCommands.eCmdDurationAdjustMode, NoteCommands.eCmdDurationAdjustMode, BoolHelper, Target.Instr, p => p.ThisTrack.ParameterController.DurationAdjustAdditive ? -1 : 0, "add", "multiply"));
            list.Add(new ParamDef(new string[] { "portamento", "pd" }, NoteCommands.eCmdRestorePortamento, NoteCommands.eCmdSweepPortamentoAbs, NoteCommands.eCmdSweepPortamentoRel, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Portamento.nd.Current));
            list.Add(new ParamDef(new string[] { "accent1", "a1" }, NoteCommands.eCmdRestoreAccent1, NoteCommands.eCmdSweepAccentAbs1, NoteCommands.eCmdSweepAccentRel1, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Accent1.nd.Current));
            list.Add(new ParamDef(new string[] { "accent2", "a2" }, NoteCommands.eCmdRestoreAccent2, NoteCommands.eCmdSweepAccentAbs2, NoteCommands.eCmdSweepAccentRel2, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Accent2.nd.Current));
            list.Add(new ParamDef(new string[] { "accent3", "a3" }, NoteCommands.eCmdRestoreAccent3, NoteCommands.eCmdSweepAccentAbs3, NoteCommands.eCmdSweepAccentRel3, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Accent3.nd.Current));
            list.Add(new ParamDef(new string[] { "accent4", "a4" }, NoteCommands.eCmdRestoreAccent4, NoteCommands.eCmdSweepAccentAbs4, NoteCommands.eCmdSweepAccentRel4, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Accent4.nd.Current));
            list.Add(new ParamDef(new string[] { "accent5", "a5" }, NoteCommands.eCmdRestoreAccent5, NoteCommands.eCmdSweepAccentAbs5, NoteCommands.eCmdSweepAccentRel5, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Accent5.nd.Current));
            list.Add(new ParamDef(new string[] { "accent6", "a6" }, NoteCommands.eCmdRestoreAccent6, NoteCommands.eCmdSweepAccentAbs6, NoteCommands.eCmdSweepAccentRel6, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Accent6.nd.Current));
            list.Add(new ParamDef(new string[] { "accent7", "a7" }, NoteCommands.eCmdRestoreAccent7, NoteCommands.eCmdSweepAccentAbs7, NoteCommands.eCmdSweepAccentRel7, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Accent7.nd.Current));
            list.Add(new ParamDef(new string[] { "accent8", "a8" }, NoteCommands.eCmdRestoreAccent8, NoteCommands.eCmdSweepAccentAbs8, NoteCommands.eCmdSweepAccentRel8, Double.Parse, Target.Instr, p => p.ThisTrack.ParameterController.Accent8.nd.Current));

            list.Add(new ParamDef(new string[] { "trackaccent1", "ta1" }, NoteCommands.eCmdSetEffectParam1, NoteCommands.eCmdSweepEffectParamAbs1, NoteCommands.eCmdSweepEffectParamRel1, Double.Parse, Target.Track, p => p.ThisTrack.Instrs != null ? p.ThisTrack.Instrs.EffectGenerator.Accents0.Current : 0));
            list.Add(new ParamDef(new string[] { "trackaccent2", "ta2" }, NoteCommands.eCmdSetEffectParam2, NoteCommands.eCmdSweepEffectParamAbs2, NoteCommands.eCmdSweepEffectParamRel2, Double.Parse, Target.Track, p => p.ThisTrack.Instrs != null ? p.ThisTrack.Instrs.EffectGenerator.Accents1.Current : 0));
            list.Add(new ParamDef(new string[] { "trackaccent3", "ta3" }, NoteCommands.eCmdSetEffectParam3, NoteCommands.eCmdSweepEffectParamAbs3, NoteCommands.eCmdSweepEffectParamRel3, Double.Parse, Target.Track, p => p.ThisTrack.Instrs != null ? p.ThisTrack.Instrs.EffectGenerator.Accents2.Current : 0));
            list.Add(new ParamDef(new string[] { "trackaccent4", "ta4" }, NoteCommands.eCmdSetEffectParam4, NoteCommands.eCmdSweepEffectParamAbs4, NoteCommands.eCmdSweepEffectParamRel4, Double.Parse, Target.Track, p => p.ThisTrack.Instrs != null ? p.ThisTrack.Instrs.EffectGenerator.Accents3.Current : 0));
            list.Add(new ParamDef(new string[] { "trackaccent5", "ta5" }, NoteCommands.eCmdSetEffectParam5, NoteCommands.eCmdSweepEffectParamAbs5, NoteCommands.eCmdSweepEffectParamRel5, Double.Parse, Target.Track, p => p.ThisTrack.Instrs != null ? p.ThisTrack.Instrs.EffectGenerator.Accents4.Current : 0));
            list.Add(new ParamDef(new string[] { "trackaccent6", "ta6" }, NoteCommands.eCmdSetEffectParam6, NoteCommands.eCmdSweepEffectParamAbs6, NoteCommands.eCmdSweepEffectParamRel6, Double.Parse, Target.Track, p => p.ThisTrack.Instrs != null ? p.ThisTrack.Instrs.EffectGenerator.Accents5.Current : 0));
            list.Add(new ParamDef(new string[] { "trackaccent7", "ta7" }, NoteCommands.eCmdSetEffectParam7, NoteCommands.eCmdSweepEffectParamAbs7, NoteCommands.eCmdSweepEffectParamRel7, Double.Parse, Target.Track, p => p.ThisTrack.Instrs != null ? p.ThisTrack.Instrs.EffectGenerator.Accents6.Current : 0));
            list.Add(new ParamDef(new string[] { "trackaccent8", "ta8" }, NoteCommands.eCmdSetEffectParam8, NoteCommands.eCmdSweepEffectParamAbs8, NoteCommands.eCmdSweepEffectParamRel8, Double.Parse, Target.Track, p => p.ThisTrack.Instrs != null ? p.ThisTrack.Instrs.EffectGenerator.Accents7.Current : 0));

            return list.ToArray();
        }
        // TODO: adapt slider scale to annotations in comments from the track

        private static Dictionary<string, ParamDef> CreateCommandMap(ParamDef[] ParamCommandsArray)
        {
            Dictionary<string, ParamDef> map = new Dictionary<string, ParamDef>();
            foreach (ParamDef command in ParamCommandsArray)
            {
                foreach (string name in command.Names)
                {
                    map.Add(name, command);
                }
            }
            return map;
        }

        public static double BoolHelper(string v)
        {
            return ParseBool(v) ? -1 : 0;
        }

        public static bool ParseBool(string value)
        {
            if (Array.FindIndex(Trues, delegate (string s) { return String.Equals(s, value, StringComparison.OrdinalIgnoreCase); }) >= 0)
            {
                return true;
            }
            else if (Array.FindIndex(Falses, delegate (string s) { return String.Equals(s, value, StringComparison.OrdinalIgnoreCase); }) >= 0)
            {
                return false;
            }
            else
            {
                throw new FormatException();
            }
        }
        private static readonly string[] Trues = new string[] { "true", "t", "yes", "y", "1" };
        private static readonly string[] Falses = new string[] { "false", "f", "no", "n", "0" };


        private static void SetParam(ref Synthesizer.IncrParamOneRec p, double v)
        {
            p.nd.Current = v;
            p.nd.ChangeCountdown = 0;
        }

        private static void IssueTrackCommand(
            NoteCommands op,
            Target target,
            double value,
            double duration,
            Synthesizer.PlayListNodeRec player,
            ref Synthesizer.SynthCycleClientCallbackRec context,
            CommandNoteObjectRec reusableInstrCommand,
            TrackObjectRec scratchTrack)
        {
            // Track effect commands must be enqueued since they are applied in real time, so new
            // command object must be created for each. Instrument parameters are applied directly
            // to the updator which then applies to events scheduled into the scanning gap, so
            // the command object has no lifetime beyond the call and can be reused.
            CommandNoteObjectRec command = target == Target.Instr ? reusableInstrCommand : new CommandNoteObjectRec(scratchTrack);

            command.PutCommandOpcode(op);
            command.PutCommandNumericArg1(((LargeBCDType)value).rawInt32);
            command.PutCommandNumericArg2(((SmallExtBCDType)duration).rawInt32);

            if (target == Target.Instr)
            {
                Synthesizer.ExecuteParamCommandFrame(
                    player.ThisTrack.ParameterController,
                    command,
                    context.synthState.SynthParams0);
            }
            else
            {
                Synthesizer.PerInstrRec instr = player.ThisTrack.Instrs;
                while (instr != null)
                {
                    Synthesizer.TrackEffectHandleCommand(
                        instr.EffectGenerator,
                        command,
                        context.synthState.iScanningGapFrontInEnvelopeTicks,
                        context.synthState.SynthParams0);
                    instr = instr.Next;
                }
            }
        }
    }


    public class TrackRequestRec
    {
        public readonly Document Document;
        public readonly TrackObjectRec Track;
        public readonly Synthesizer.ITrackParameterProvider TrackParamProvider;
        public readonly string Sequence;

        public TrackRequestRec(
            Document document,
            TrackObjectRec track,
            Synthesizer.ITrackParameterProvider trackParamProvider,
            string sequence)
        {
            this.Document = document;
            this.Track = track;
            this.TrackParamProvider = trackParamProvider;
            this.Sequence = sequence;
        }
    }

    public class TrackStatusRec
    {
        public readonly Document Document;
        public readonly TrackObjectRec Track;
        public readonly string Sequence;
        public readonly bool PendingDeletion;

        public TrackStatusRec(
            Document document,
            TrackObjectRec track,
            string sequence,
            bool pendingDeletion)
        {
            this.Document = document;
            this.Track = track;
            this.Sequence = sequence;
            this.PendingDeletion = pendingDeletion;
        }
    }


    public enum Target { Instr, Track };
    public class ParamDef
    {
        public readonly string[] Names;
        public readonly NoteCommands ResetOp;
        public readonly NoteCommands SweepAbsOp;
        public readonly NoteCommands SweepRelOp;
        public readonly Func<string, double> Parse;
        public readonly Target Target;
        public readonly Func<Synthesizer.PlayListNodeRec, double> Peek;
        public readonly string FalseLabel, TrueLabel;

        public string PrimaryName { get { return Names[0]; } }

        public ParamDef(
            string[] names,
            NoteCommands initOp,
            NoteCommands sweepAbsOp,
            NoteCommands sweepRelOp,
            Func<string, double> parse,
            Target target,
            Func<Synthesizer.PlayListNodeRec, double> peek)
        {
            this.Names = names;
            this.ResetOp = initOp;
            this.SweepAbsOp = sweepAbsOp;
            this.SweepRelOp = sweepRelOp;
            this.Parse = parse;
            this.Target = target;
            this.Peek = peek;
        }

        public ParamDef(
            string[] names,
            NoteCommands initOp,
            NoteCommands sweepAbsOp,
            NoteCommands sweepRelOp,
            Func<string, double> parse,
            Target target,
            Func<Synthesizer.PlayListNodeRec, double> peek,
            string falseLabel,
            string trueLabel)
        {
            this.Names = names;
            this.ResetOp = initOp;
            this.SweepAbsOp = sweepAbsOp;
            this.SweepRelOp = sweepRelOp;
            this.Parse = parse;
            this.Target = target;
            this.Peek = peek;
            this.FalseLabel = falseLabel;
            this.TrueLabel = trueLabel;
        }
    }


    public class ParamBoardEntry : IComparable<ParamBoardEntry>
    {
        private readonly TrackObjectRec track;
        private readonly Moniker moniker;
        private double value;
        private int updateSynth, updateDisplay;
        private bool delete;

        public ParamBoardEntry(TrackObjectRec track, Moniker moniker)
        {
            if (moniker.Param == null)
            {
                throw new ArgumentException();
            }
            this.track = track;
            this.moniker = moniker;
        }

        public ParamBoardEntry(TrackObjectRec track, Moniker moniker, bool delete)
        {
            if (moniker.Param == null)
            {
                throw new ArgumentException();
            }
            this.track = track;
            this.moniker = moniker;
            this.delete = delete;
        }

        public ParamBoardEntry(ParamBoardEntry entry, bool delete)
            : this(entry.track, entry.moniker, delete)
        {
        }

        public TrackObjectRec Track { get { return track; } }
        public Moniker Moniker { get { return moniker; } }
        public double Value { get { return value; } }
        public bool UpdateDisplay { get { return updateDisplay != 0; } }
        public bool UpdateSynth { get { return updateSynth != 0; } }
        public bool Delete { get { return delete; } }

        public void SetValueFromUI(double newValue)
        {
            if (Interlocked.Exchange(ref value, newValue) != newValue)
            {
                updateSynth = 1;
            }
        }

        public bool GetUpdateSynth()
        {
            return Interlocked.Exchange(ref updateSynth, 0) != 0;
        }

        public void UpdateValueFromSynth(double currentValue)
        {
            if (Interlocked.Exchange(ref value, currentValue) != currentValue)
            {
                updateDisplay = 1;
            }
        }

        public void ForceUpdateUI()
        {
            updateDisplay = 1;
        }

        public bool GetUpdateUI()
        {
            return Interlocked.Exchange(ref updateDisplay, 0) != 0;
        }

        public int CompareTo(ParamBoardEntry other)
        {
            return this.moniker.CompareTo(other.moniker);
        }
    }

    // TODOs:
    // TODO: failure on track should mute track but continue other playing
    // TODO: robust failure handling and reporting failures on the current particles board
    // TODO: start section workspaces at certain capacity, double it when exceeded when adding
    // TODO: reuse section workspaces that no longer have a section
    // TODO: autosave (be careful about slowing down large situations and interfering with ability to control music)
    // TODO: smoothing of volume/param changes
    // TODO: live track/section output volume control, control parameters
    // TODO: mute control in "current"
    // TODO: section deletion
    // TODO: section merging
    // TODO: contouring of parameters in sequence
    // TODO: ramped adjust of parameters out of sequence
    // TODO: sets of parameter presets that can be applied as a token
    // TODO: redirecting pitch/accents from one track to another
    // TODO: track taking notes from another track
    // TODO: sets of predefined sequences that control multiple tracks with different sequence names each and can be issued as a single token

    // TODO: reattime controls
    // TODO: timed contours
}
