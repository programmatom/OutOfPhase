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
#if VECTOR
using System.Numerics;
#endif
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace OutOfPhase
{
    // TODO: for .NET 4.6: look at use of [MethodImpl(MethodImplOptions.AggressiveInlining)] in general across codebase 
    // to improve performance of small math functions used in kernels (e.g. Fixed64's methods, etc)

    public static partial class Synthesizer
    {
        public interface IStopTask
        {
            void Stop();
        }

        public interface IStoppedTask
        {
            bool Stopped { get; }

            event EventHandler OnStop; // this will be executed on UI thread
        }

        public class StopTask : IStopTask, IStoppedTask
        {
            private volatile int stopped;

            public bool Stopped { get { return stopped != 0; } }

            public void Stop()
            {
                stopped = 1;
                if (OnStop != null)
                {
                    OnStop.Invoke(this, EventArgs.Empty);
                }
            }

            public event EventHandler OnStop;
        }

        /* termination conditions that can occur during synthesis */
        public enum SynthErrorCodes
        {
            eSynthDone,
            eSynthDoneNoData, /* no data was generated, not necessarily an error */
            eSynthUserCancelled, /* user cancelled via the periodic callback */
            eSynthPrereqError, /* the sample/func/instrs couldn't be built */
            eSynthDataSubmitError, /* some error (perhaps disk error) occurred during callback */
            eSynthErrorEx, /* fatal error -- details in struct */
        }

        /* subqualified error codes */
        public enum SynthErrorSubCodes
        {
            eSynthErrorEx_Start,

            eSynthErrorExSequenceSpecifiedMultipleTimes = eSynthErrorEx_Start,
            eSynthErrorExSequenceHasNoDuration,
            eSynthErrorExUndefinedInstrument,
            eSynthErrorExUndefinedWaveTable,
            eSynthErrorExUndefinedSample,
            eSynthErrorExUndefinedFunction,
            eSynthErrorExTypeMismatchFunction,
            eSynthErrorExPossibleInfiniteSequenceLoop,
            eSynthErrorExLaterTrackIssusedCommandToEarlierTrack,
            eSynthErrorExTooManyNestedSkipCommands,
            eSynthErrorExDontUseAsteriskAsTrackOrGroupName,
            eSynthErrorExSomeSamplesHaveSameName,
            eSynthErrorExSomeWaveTablesHaveSameName,
            eSynthErrorExSomeInstrumentsHaveSameName,
            eSynthErrorExUndefinedTrackInGroupTable,
            eSynthErrorExConvolverBadSamplingRate,
            eSynthErrorExConvolverBadNumChannels,
            eSynthErrorExConvolverExplicitLatencyNotAvailable,
            eSynthErrorExExceptionOccurred,
            eSynthErrorExUserParamFunctionEvalError,
            eSynthErrorExUserEffectFunctionEvalError,

            eSynthErrorEx_End,

            eSynthErrorExCustom,
        }

        /* subqualified error information */
        public class SynthErrorInfoRec
        {
            public SynthErrorSubCodes ErrorEx;
            public string TrackName;
            public string SequenceName;
            public string InstrumentName;
            public string SectionName;
            public string WaveTableName;
            public string SampleName;
            public string FunctionName;
            public string IssuingTrackName;
            public string ReceivingTrackName;
            public string CustomError;
            public Exception exception;
            public EvalErrors userEvalErrorCode = EvalErrors.eEvalNoError;
            public EvalErrInfoRec userEvalErrorInfo; // subservient to userEvalErrorCode

            public void CopyFrom(SynthErrorInfoRec source)
            {
                this.ErrorEx = source.ErrorEx;
                this.TrackName = source.TrackName;
                this.SequenceName = source.SequenceName;
                this.InstrumentName = source.InstrumentName;
                this.SectionName = source.SectionName;
                this.WaveTableName = source.WaveTableName;
                this.SampleName = source.SampleName;
                this.FunctionName = source.FunctionName;
                this.IssuingTrackName = source.IssuingTrackName;
                this.ReceivingTrackName = source.ReceivingTrackName;
                this.CustomError = source.CustomError;
                this.exception = source.exception;
                this.userEvalErrorCode = source.userEvalErrorCode;
            }
        }

        public class AutomationSettings
        {
            // initialized to default setting values
            public readonly bool PerfCounters = true;
            public readonly int? Concurrency; // overrides both preferences and autodetermined default
            public readonly long[] BreakFrames;
            public readonly string TraceSchedulePath;
            public readonly TraceFlags Level2TraceFlags;
            public readonly string SummaryTag;

            [Flags]
            public enum TraceFlags : uint
            {
                None = 0,
                All = 0xffffffff,

                Events = 1 << 0,
                Denormals = 1 << 1,

                NonInvasive = Events,
            }

            public AutomationSettings()
            {
            }

            // override default values
            public AutomationSettings(
                bool perfCounters,
                int? concurrency,
                long[] breakFrames,
                string traceSchedulePath,
                TraceFlags Level2TraceFlags,
                string summaryTag)
            {
                this.PerfCounters = perfCounters;
                this.Concurrency = concurrency;
                this.BreakFrames = breakFrames;
                this.TraceSchedulePath = traceSchedulePath;
                this.Level2TraceFlags = Level2TraceFlags;
                this.SummaryTag = summaryTag;
            }
        }

        // Provider of seeds subsidiary random number generators in various processing units.
        // Designed to provide deterministic sequence of initial seeds during sequential phase
        // of processing, while locking out reseeding during parallel phases.
        public class RandomSeedProvider
        {
            private bool frozen;
            private int masterSeed;

            public RandomSeedProvider(int masterSeed)
            {
                this.masterSeed = masterSeed;
            }

            public void Freeze()
            {
                frozen = true;
            }

            public void Unfreeze()
            {
                frozen = false;
            }

            public void ObtainSeed(ref ParkAndMiller seed)
            {
                if (frozen)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                seed.SetSeed(masterSeed);
                /* "shuffle the deck" so the first real use is more random */
                seed.Random();

                masterSeed++;
                if (masterSeed > ParkAndMiller.Maximum)
                {
                    masterSeed = ParkAndMiller.Minimum;
                }
            }
        }

        public static class Timing
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/ms644904%28v=vs.85%29.aspx
            // https://msdn.microsoft.com/en-us/library/windows/desktop/ms644905%28v=vs.85%29.aspx
            // consider using RDTSC instruction to eliminate overhead (would require Managed C++):
            // https://msdn.microsoft.com/en-us/library/windows/desktop/dn553408%28v=vs.85%29.aspx#direct_tsc_usage
            [DllImport("kernel32.dll", EntryPoint = "QueryPerformanceCounter", SetLastError = true)]
            private static extern bool _QueryPerformanceCounter(out long value);
            [DllImport("kernel32.dll", EntryPoint = "QueryPerformanceFrequency", SetLastError = true)]
            public static extern bool QueryPerformanceFrequency(out long value);

            // The system call is potentially expensive - wrap with a method so it shows up on the
            // profiles if it is costing too much.
            // NOTE: calls to this method need accuracy and fidelity to QueryPerformanceFrequency().
            public static bool QueryPerformanceCounter(out long value)
            {
                return _QueryPerformanceCounter(out value);
            }

            // The system call is potentially expensive - wrap with a method so it shows up on the
            // profiles if it is costing too much.
            // NOTE: calls to this one must be very fast but may be inaccurate (fine-grained cost assessments).
            // This call is a candidate for replacement by the RDTSC instruction (requires native library).
            public static bool QueryPerformanceCounterFast(out long value)
            {
                return _QueryPerformanceCounter(out value);
            }
        }

        public class SynthControlRec
        {
            public int nActualFrames;
            public int NumNoteDurationTicks;
            public bool UpdateEnvelopes;
            public bool AreWeStillFastForwarding;
            public bool fScheduledSkip;
        }

        /* structure containing generally useful parameters for passing around in the */
        /* synthesizer */
        // This would be per-thread state
        public class SynthParamRec : IDisposable
        {
            /* number of envelope updates per second (actually an integer, but all users */
            /* use it in floating point calculations) */
            public readonly double dEnvelopeRate;
            /* number of sample frames per second */
            public readonly double dSamplingRate;
            /* integer versions */
            public readonly int iEnvelopeRate;
            public readonly int iSamplingRate;
            public readonly int iOversampling;

            /* scanning gap width */
            public readonly int iScanningGapWidthInEnvelopeTicks;

            /* output log for interaction window */
            private bool _InteractionLogAccessed;
            public bool InteractionLogAccessed { get { return _InteractionLogAccessed; } }
            private readonly TextWriter _InteractionLog;
            public TextWriter InteractionLog { get { _InteractionLogAccessed = true; return _InteractionLog; } }
            /* main data object */
            public readonly Document Document;
            /* dictionary of waveforms */
            public readonly WaveSampDictRec Dictionary;

            /* output scaling (most shouldn't need to use this) */
            public readonly float fOverallVolumeScaling;

            /* elapsed time since start of synthesis */
            public long lElapsedTimeInEnvelopeTicks;
            public double dElapsedTimeInSeconds;

            /* beats per minute */
            public double dCurrentBeatsPerMinute;

            /* code center for functions */
            public readonly CodeCenterRec CodeCenter;
            /* context for evaluating formulas */
            public readonly ParamStackRec FormulaEvalContext;

            public readonly RandomSeedProvider randomSeedProvider; // ref to the one from SynthStateRec

            /* error information for the last error returned. */
            public SynthErrorCodes result;
            public readonly SynthErrorInfoRec ErrorInfo;

            // workspaces capacity
            public readonly int nAllocatedPointsOneChannel;

            public static readonly int VECTORALIGN =
#if VECTOR
                Vector<float>.Count * sizeof(float)
#else
 0
#endif
; // SSE/AVX required alignment (bytes)
            public const int CACHEALIGN = 16; // cache line optimal alignment (bytes)
            public static readonly int WORKSPACEALIGN = Math.Max(VECTORALIGN, CACHEALIGN);

            // float[] workspaces
            public readonly float[] workspace;
            public readonly GCHandle hWorkspace;
            public readonly int ScoreWorkspaceLOffset; // Length == nAllocatedPointsOneChannel
            public readonly int ScoreWorkspaceROffset; // Length == nAllocatedPointsOneChannel
            public readonly int SectionWorkspaceLOffset; // Length == nAllocatedPointsOneChannel
            public readonly int SectionWorkspaceROffset; // Length == nAllocatedPointsOneChannel
            public readonly int TrackWorkspaceLOffset; // Length == nAllocatedPointsOneChannel
            public readonly int TrackWorkspaceROffset; // Length == nAllocatedPointsOneChannel
            public readonly int CombinedOscillatorWorkspaceLOffset; // Length == nAllocatedPointsOneChannel
            public readonly int CombinedOscillatorWorkspaceROffset; // Length == nAllocatedPointsOneChannel
            public readonly int OscillatorWorkspaceLOffset; // Length == nAllocatedPointsOneChannel
            public readonly int OscillatorWorkspaceROffset; // Length == nAllocatedPointsOneChannel
            public readonly int ScratchWorkspace1LOffset; // Length == nAllocatedPointsOneChannel
            public readonly int ScratchWorkspace1ROffset; // Length == nAllocatedPointsOneChannel
            public readonly int ScratchWorkspace2LOffset; // Length == nAllocatedPointsOneChannel
            public readonly int ScratchWorkspace2ROffset; // Length == nAllocatedPointsOneChannel
#if DEBUG
            public bool ScratchWorkspace1InUse;
            public bool ScratchWorkspace2InUse;
#endif
            // offsets to L, R, each section...
            // can't be moved to SynthState because it's tuned for the vector misalignment of this object's workspace array
            public readonly int[] SectionInputAccumulationWorkspaces;

            // Fixed64[] workspaces

            public readonly Fixed64[] ScratchWorkspace3;
            public readonly GCHandle hScratchWorkspace3;
            public readonly int ScratchWorkspace3Offset;
#if DEBUG
            public bool ScratchWorkspace3InUse;
#endif

            // Vector workspaces
#if VECTOR
            public readonly AlignedWorkspace vectorWorkspace1;
            public readonly AlignedWorkspace vectorWorkspace2;
#endif

            // one bool for each processor-section: length SectionInputAccumulationWorkspaces.Length / 2
            public bool[] SectionWorkspaceUsed;

            public PcodeSystem.PcodeThreadContext pcodeThreadContext;

            // tracing support
            public AutomationSettings.TraceFlags level2TraceFlags;


            public SynthParamRec(
                SynthParamRec template)
                : this(
                    template.iEnvelopeRate,
                    template.iSamplingRate,
                    template.iOversampling,
                    template.iScanningGapWidthInEnvelopeTicks,
                    template.Document,
                    template.Dictionary,
                    template.InteractionLog,
                    template.fOverallVolumeScaling,
                    template.CodeCenter,
                    template.randomSeedProvider,
                    template.SectionInputAccumulationWorkspaces.Length / 2)
            {
                this.dEnvelopeRate = template.dEnvelopeRate;
                this.dSamplingRate = template.dSamplingRate;
                this.lElapsedTimeInEnvelopeTicks = template.lElapsedTimeInEnvelopeTicks;
                this.dElapsedTimeInSeconds = template.dElapsedTimeInSeconds;
                this.dCurrentBeatsPerMinute = template.dCurrentBeatsPerMinute;
            }

            public SynthParamRec(
                int iEnvelopeRate,
                int iSamplingRate,
                int iOversampling,
                int iScanningGapWidthInEnvelopeTicks,
                Document Document,
                WaveSampDictRec Dictionary,
                TextWriter InteractionLog,
                float fOverallVolumeScaling,
                CodeCenterRec CodeCenter,
                RandomSeedProvider randomSeedProvider,
                int sectionCount)
            {
                this.dEnvelopeRate = this.iEnvelopeRate = iEnvelopeRate;
                this.dSamplingRate = this.iSamplingRate = iSamplingRate;
                this.iOversampling = iOversampling;
                this.iScanningGapWidthInEnvelopeTicks = iScanningGapWidthInEnvelopeTicks;
                this.Document = Document;
                this.Dictionary = Dictionary;
                this._InteractionLog = InteractionLog;
                this.fOverallVolumeScaling = fOverallVolumeScaling;
                this.CodeCenter = CodeCenter;
                this.randomSeedProvider = randomSeedProvider;

                this.FormulaEvalContext = new ParamStackRec();
                this.ErrorInfo = new SynthErrorInfoRec();

                this.nAllocatedPointsOneChannel = ((iSamplingRate + iEnvelopeRate - 1) / iEnvelopeRate);

                // float[] workspaces
                {
                    /* allocate the workspace */
                    /* allocate block containing all workspace areas */
                    const int NumberOfWorkspaces = 7;
                    this.workspace = new float[(nAllocatedPointsOneChannel + WORKSPACEALIGN) * 2 * (NumberOfWorkspaces + sectionCount)];
                    this.hWorkspace = GCHandle.Alloc(this.workspace, GCHandleType.Pinned);
                    // assign workspace subsections
                    const int SizeOfFloat = 4;
                    int offset = 0;
                    this.ScoreWorkspaceLOffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.ScoreWorkspaceROffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.SectionWorkspaceLOffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.SectionWorkspaceROffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.TrackWorkspaceLOffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.TrackWorkspaceROffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.CombinedOscillatorWorkspaceLOffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.CombinedOscillatorWorkspaceROffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.OscillatorWorkspaceLOffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.OscillatorWorkspaceROffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.ScratchWorkspace1LOffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.ScratchWorkspace1ROffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.ScratchWorkspace2LOffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    this.ScratchWorkspace2ROffset = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                    offset += this.nAllocatedPointsOneChannel;
                    Debug.Assert(offset <= this.workspace.Length);

                    this.SectionInputAccumulationWorkspaces = new int[2 * sectionCount];
                    for (int i = 0; i < 2 * sectionCount; i++)
                    {
                        this.SectionInputAccumulationWorkspaces[i] = Align(this.hWorkspace.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFloat);
                        offset += this.nAllocatedPointsOneChannel;
                    }
                    Debug.Assert(offset <= this.workspace.Length);

#if VECTOR
                    vectorWorkspace1 = new AlignedWorkspace(Vector<float>.Count);
                    vectorWorkspace2 = new AlignedWorkspace(Vector<float>.Count);
#endif

                    this.SectionWorkspaceUsed = new bool[sectionCount];
                }

                // Fixed64[] workspaces
                {
                    const int SizeOfFixed64 = 8;
                    this.ScratchWorkspace3 = new Fixed64[this.nAllocatedPointsOneChannel + WORKSPACEALIGN * 2];
                    this.hScratchWorkspace3 = GCHandle.Alloc(this.ScratchWorkspace3, GCHandleType.Pinned);
                    int offset = 0;
                    this.ScratchWorkspace3Offset = Align(this.hScratchWorkspace3.AddrOfPinnedObject(), ref offset, WORKSPACEALIGN, SizeOfFixed64);
                    offset += this.nAllocatedPointsOneChannel;
                    Debug.Assert(offset <= this.ScratchWorkspace3.Length);
                }
            }

            // Also used by FFTW wrapper
            public static int Align(IntPtr addr0, ref int start, int alignment, int elementSize)
            {
                Debug.Assert(alignment % elementSize == 0);
                long pb0 = addr0.ToInt64();
                long pbStart = pb0 + start * elementSize;
                long pbStartAligned = (pbStart + alignment - 1) & ~(alignment - 1);
                start = (int)(pbStartAligned - pb0) / elementSize;
                return start;
            }

            public void Dispose()
            {
                if (FormulaEvalContext != null)
                {
                    FormulaEvalContext.Dispose();
                }
                if (hScratchWorkspace3.IsAllocated)
                {
                    hScratchWorkspace3.Free();
                }
                if (hWorkspace.IsAllocated)
                {
                    hWorkspace.Free();
                }
#if VECTOR
                if (vectorWorkspace1 != null)
                {
                    vectorWorkspace1.Dispose();
                }
                if (vectorWorkspace2 != null)
                {
                    vectorWorkspace2.Dispose();
                }
#endif
                GC.SuppressFinalize(this);
            }

            ~SynthParamRec()
            {
#if DEBUG
                Debug.Assert(false, "SynthParamRec finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
                Dispose();
            }

#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif
        }

        /* section effect information */
        public class SecEffRec
        {
            /* section effect processor (null if none) */
            public TrackEffectGenRec SectionEffect;

            /* need to keep the template around */
            public EffectSpecListRec SectionTemplate;

            public SectionObjectRec SectionObject;

#if true // PARALLEL
            // leftOffset = SynthParamRec.SectionInputAccumulationWorkspaces[2 * sectionInputAccumulatorIndex + 0];
            // rightOffset = SynthParamRec.SectionInputAccumulationWorkspaces[2 * sectionInputAccumulatorIndex + 1];
            public int sectionInputAccumulatorIndex;

            public int sectionInputCounter;
            public int sectionInputTarget;

            // 0 = not processed; 1 = processed
            public int processed;

            // cpu cost of last cycle
            public long lastCost;

            public PlayListNodeRec[] inputTracks;
#endif

            public TraceInfoRec traceInfo;

            // for tracing
            public int seqGen;
            public List<EventTraceRec> events; // null == not tracing
            public int denormalCount; // iff level2TraceFlags & Denormals != 0
        }

        // for tracing
        public enum EventTraceType
        {
            Start,
            Restart,
            Stop,
            SkipEnter,
            SkipLeave,
        }

        // for tracing
        public class EventTraceRec
        {
            public EventTraceType evt;
            public int seq;
            public int? frameIndex;
            public int? noteIndex;

            public EventTraceRec(
                EventTraceType evt,
                int seq)
            {
                this.evt = evt;
                this.seq = seq;
            }

            public EventTraceRec(
                EventTraceType evt,
                int seq,
                int frameIndex,
                int noteIndex)
            {
                this.evt = evt;
                this.seq = seq;
                this.frameIndex = frameIndex;
                this.noteIndex = noteIndex;
            }
        }

        /* node type for list of tracks to play */
        public class PlayListNodeRec
        {
            /* next track */
            public PlayListNodeRec Next;

            /* pointer to track player object */
            public PlayTrackInfoRec ThisTrack;

            /* section effect processor (null if none) */
            public SecEffRec SectionEffectHandle;

            /* pointer to the source track object */
            public TrackObjectRec TrackObject;

            /* flag indicating whether track object is active */
            public bool IsActive;

#if true // PARALLEL
            // cpu cost of last cycle
            public long lastCost;

            // 0 = not processed; 1 = processed
            public int processed;
#endif

            // for tracing
            public TraceInfoRec traceInfo;
            public int denormalCount; // iff level2TraceFlags & Denormals != 0
        }

        public struct TraceInfoRec
        {
            public int id;
            public int processor;
            public long start;
            public long end;
        }

        public class SynthStateRec : IDisposable
        {
            public List<PlayListNodeRec> TrackPlayersInFileOrder;
            public SequencerTableRec SequencerTable;
            public TrackEffectGenRec ScoreEffectProcessor;
            public PlayListNodeRec PlayTrackList;
#if true // PARALLEL
            public int concurrency; // 0 = old, 1 = new serialized, >1 = new parallelized
            public SecEffRec DefaultSectionEffectSurrogate;
            public int scoreEffectInputAccumulatorIndex; // synonymous with DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex

            // tracks and sections dependency-scheduled and cost-ordered (not including default section / score effect)
            // (all tracks for a section must come earlier than the section)
            public object[] CombinedPlayArray; // each is either PlayListNodeRec or SecEffRec

            public SynthParamRec[] SynthParamsPerProc;
#endif
            public SynthParamRec SynthParams0; // synonymous with SynthParamsPerProc[0]

#if true // PARALLEL
            public ManualResetEvent startBarrier;
            public ManualResetEvent endBarrier;
            public volatile int startBarrierReleaseSpin;
#if DEBUG
            public long epoch;
#endif

            public volatile int startingThreadCount;
            public volatile int completionThreadCount;

            public SecEffRec[] SectionArrayAll; // includes DefaultSectionEffectSurrogate
            public SecEffRec[] SectionArrayExcludesDefault; // excludes DefaultSectionEffectSurrogate

            public Thread[] threads; // [0]=main thread, [1..c-1] for each aux processor
            public volatile int exit; // non-zero causes exit
#endif

            public int initialSeed;
            public RandomSeedProvider randomSeedProvider;

            public SynthControlRec control = new SynthControlRec();

            /* segment list for scheduling of skipped sections */
            public SkipSegmentsRec SkipSchedule;
            /* global tempo controller */
            public TempoControlRec TempoControl;

            public double dMomentOfStartingDurationTick;
            public int iScanningGapFrontInEnvelopeTicks;
            public double dEnvelopeClockAccumulatorFraction;
            public double dNoteDurationClockAccumulatorFraction;
            public double dSamplesPerEnvelopeClock;
            public double dDurationTicksPerEnvelopeClock;
            public bool fSuppressingInitialSilence;
            public bool fLastCycleWasScheduledSkip;
            public long lEnvelopeCyclesEmittingAudio;
            public long lTotalFramesGenerated;

            public List<TrackObjectRec> ListOfTracks;
            public TrackObjectRec KeyTrack;
            public int FrameToStartAt;
            public EffectSpecListRec ScoreEffectSpec;

            public long phase0Time; // SynthGenerateOneCycle() - leading sequential setup
            public long phase1Time; // SynthGenerateOneCycle()- parallel time
            public long phase2Time; // SynthGenerateOneCycle() - trailing sequential operations, including score effects 
            public long phase3Time; // time between calls to SynthGenerateOneCycle()
            public long time3;

            public long[] breakFrames; // debugging helper
            public int breakFramesIndex;

            public string summaryTag;

            public TextWriter traceScheduleWriter;
            public bool traceScheduleEnableLevel2;

            public void Dispose()
            {
                if (SynthParamsPerProc != null)
                {
                    for (int i = 0; i < SynthParamsPerProc.Length; i++)
                    {
                        SynthParamsPerProc[i].Dispose();
                    }
                }
                else
                {
                    SynthParams0.Dispose();
                }

                if (startBarrier != null)
                {
                    startBarrier.Close();
                }
                if (endBarrier != null)
                {
                    endBarrier.Close();
                }

                if (traceScheduleWriter != null)
                {
                    traceScheduleWriter.Dispose();
                }

                GC.SuppressFinalize(this);
            }

            ~SynthStateRec()
            {
#if DEBUG
                Debug.Assert(false, "SynthStateRec finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
                Dispose();
            }

#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

            /* allocate a new synthesizer object. */
            public static SynthErrorCodes InitializeSynthesizer(
                out SynthStateRec SynthStateOut,
                Document Document,
                List<TrackObjectRec> ListOfTracks,
                TrackObjectRec KeyTrack,
                int FrameToStartAt,
                int SamplingRate,
                int Oversampling,
                int EnvelopeRate,
                LargeBCDType DefaultBeatsPerMinute,
                double OverallVolumeScalingReciprocal,
                LargeBCDType ScanningGap,
                out SynthErrorInfoRec ErrorInfoOut,
                TextWriter InteractionLog,
                bool deterministic,// now ignored - control by setting randomSeed to null or int
                int? randomSeed,
                AutomationSettings automationSettings)
            {
                SynthStateRec SynthState = null;

                ErrorInfoOut = null;
                SynthStateOut = null;

                try
                {
                    SynthErrorCodes Result;


                    /* check to see that there aren't objects with the same name */
                    {
                        SynthErrorInfoRec ErrorInfo = new SynthErrorInfoRec();
                        Result = CheckNameUniqueness(
                            Document,
                            ErrorInfo);
                        if (Result != SynthErrorCodes.eSynthDone)
                        {
                            ErrorInfoOut = ErrorInfo;
                            return Result;
                        }
                    }


                    /* constrain scanning gap to be reasonable */
                    if (ScanningGap.rawInt32 < 0)
                    {
                        ScanningGap = LargeBCDType.FromRawInt32(0);
                    }


                    SynthState = new SynthStateRec();

#if true // PARALLEL
                    if (automationSettings.Concurrency.HasValue)
                    {
                        // explicit concurrency value overrides global settings

                        SynthState.concurrency = automationSettings.Concurrency.Value;
                        if ((SynthState.concurrency < 0) || (SynthState.concurrency > 64))
                        {
                            Debug.Assert(false);
                            throw new ArgumentException("AutomationSettings.Concurrency");
                        }
                    }
                    else
                    {
                        // use concurrency from global settings

                        if (Program.Config.Concurrency == 0)
                        {
                            // default
                            SynthState.concurrency = Environment.ProcessorCount;
                        }
                        else if (Program.Config.Concurrency == 1)
                        {
                            // explicitly sequential
                            SynthState.concurrency = 1;
                        }
                        else if (Program.Config.Concurrency > 1)
                        {
                            // explicit processor count
                            SynthState.concurrency = Math.Min(Program.Config.Concurrency, Environment.ProcessorCount);
                        }
                        else
                        {
                            // reserved (unused) processor count
                            SynthState.concurrency = Math.Max(Environment.ProcessorCount + Program.Config.Concurrency, 1);
                        }
#endif

                        SynthState.concurrency = Math.Max(1, SynthState.concurrency);
                    }

#if true // PARALLEL
                    int sectionCount = 0;
                    if (SynthState.concurrency > 0)
                    {
                        // Count sections, to determine how many per-processor input accumulators are needed.
                        // Score Effects count as one. The default (null) section, if used, also counts as another one.
                        // Each explicit section used by one or more present tracks counts as an additional one.
                        sectionCount = 1; // always one: score effects (equivalent to the default 'null' section)
                        List<SectionObjectRec> sectionsSeen = new List<SectionObjectRec>();
                        for (int i = 0; i < ListOfTracks.Count; i++)
                        {
                            if ((ListOfTracks[i].Section != null) && (sectionsSeen.IndexOf(ListOfTracks[i].Section) < 0))
                            {
                                sectionsSeen.Add(ListOfTracks[i].Section);
                                sectionCount++;
                            }
                        }
                    }
#endif

                    int iScanningGapWidthInEnvelopeTicks = (int)((double)ScanningGap * EnvelopeRate);
                    float fOverallVolumeScaling = (float)(1d / OverallVolumeScalingReciprocal);

                    /* allocate sample/wavetable lookup */
                    WaveSampDictRec Dictionary = NewWaveSampDictionary(
                        Document.SampleList,
                        Document.AlgoSampList,
                        Document.WaveTableList,
                        Document.AlgoWaveTableList);

                    /* set random number seed */
                    if (randomSeed.HasValue)
                    {
                        SynthState.initialSeed = randomSeed.Value;
                    }
                    else
                    {
                        SynthState.initialSeed = unchecked((int)DateTime.UtcNow.Ticks);
                    }
                    SynthState.initialSeed = ParkAndMiller.ConstrainSeed(SynthState.initialSeed);
                    SynthState.randomSeedProvider = new RandomSeedProvider(SynthState.initialSeed);

                    SynthParamRec SynthParams = SynthState.SynthParams0 = new SynthParamRec(
                        EnvelopeRate,
                        SamplingRate,
                        Oversampling,
                        iScanningGapWidthInEnvelopeTicks,
                        Document,
                        Dictionary,
                        TextWriter.Synchronized(InteractionLog),
                        fOverallVolumeScaling,
                        Document.CodeCenter,
                        SynthState.randomSeedProvider,
                        sectionCount);

                    SynthState.ListOfTracks = ListOfTracks;
                    SynthState.KeyTrack = KeyTrack;
                    SynthState.FrameToStartAt = FrameToStartAt;

                    SynthParams.lElapsedTimeInEnvelopeTicks = -1;
                    SynthParams.dCurrentBeatsPerMinute = (double)DefaultBeatsPerMinute;

                    /* allocate tempo transition tracker */
                    SynthState.TempoControl = NewTempoControl(DefaultBeatsPerMinute);

                    SynthState.TrackPlayersInFileOrder = new List<PlayListNodeRec>(ListOfTracks.Count);

                    /* create the score effects processor */
                    SynthState.ScoreEffectSpec = Document.ScoreEffects.ScoreEffectSpec;
                    Result = NewTrackEffectGenerator(
                        SynthState.ScoreEffectSpec,
                        SynthParams,
                        out SynthState.ScoreEffectProcessor);
                    if (Result != SynthErrorCodes.eSynthDone)
                    {
                        ErrorInfoOut = SynthParams.ErrorInfo;
                        return Result;
                    }

                    SynthState.SequencerTable = NewSequencerTable();

                    /* create skip schedule holder */
                    SynthState.SkipSchedule = NewSkipSegments();


                    CheckUnrefParamRec Param = new CheckUnrefParamRec();
                    Param.Dictionary = SynthParams.Dictionary;
                    Param.CodeCenter = Document.CodeCenter;
                    Param.ErrorInfo = new SynthErrorInfoRec();

                    /* make sure instruments don't reference any unknown objects */
                    Result = CheckInstrListForUnreferencedSamples(
                        Document.InstrumentList,
                        Param);
                    if (Result != SynthErrorCodes.eSynthDone)
                    {
                        ErrorInfoOut = Param.ErrorInfo;
                        return Result;
                    }

                    /* make sure score effects don't reference any unknown objects */
                    Result = CheckEffectListForUnreferencedSamples(
                        SynthState.ScoreEffectSpec,
                        Param);
                    if (Result != SynthErrorCodes.eSynthDone)
                    {
                        const string StrScoreSectionName = "Score effects list";

                        // assign InitErrorInfo.ErrorEx?
                        ErrorInfoOut = Param.ErrorInfo;
                        ErrorInfoOut.SectionName = StrScoreSectionName;
                        return Result;
                    }


                    /* build list of tracks to play */
                    Result = BuildPlayList(
                        out SynthState.PlayTrackList,
                        SynthState.ListOfTracks,
                        SynthState.ScoreEffectProcessor,
                        SynthState.TrackPlayersInFileOrder,
                        SynthState.TempoControl,
                        SynthParams);
                    if (Result != SynthErrorCodes.eSynthDone)
                    {
                        ErrorInfoOut = SynthParams.ErrorInfo;
                        return Result;
                    }

                    /* build map from track/group names to playtrackinfo's */
                    Result = BuildSequencerTable(
                        SynthParams.Document,
                        SynthState.SequencerTable,
                        SynthState.TrackPlayersInFileOrder,
                        SynthParams.ErrorInfo,
                        SynthParams);
                    if (Result != SynthErrorCodes.eSynthDone)
                    {
                        ErrorInfoOut = SynthParams.ErrorInfo;
                        return Result;
                    }
                    PlayListNodeRec Scan = SynthState.PlayTrackList;
                    while (Scan != null)
                    {
                        PlayTrackInfoSetSequencerTable(
                            Scan.ThisTrack,
                            SynthState.SequencerTable);
                        Scan = Scan.Next;
                    }


                    SynthState.fSuppressingInitialSilence = EffectSpecListGetSuppressInitialSilence(
                        SynthState.ScoreEffectSpec);


                    /* calculate the moment of starting for tracks */
                    FractionRec MomentOfStartingFrac;
                    FindStartPoint(
                        SynthState.KeyTrack,
                        SynthState.FrameToStartAt,
                        out MomentOfStartingFrac);
                    SynthState.dMomentOfStartingDurationTick = FractionRec.Fraction2Double(MomentOfStartingFrac)
                        * DURATIONUPDATECLOCKRESOLUTION;

                    /* this value is for determining when in REAL time (not score time) */
                    /* each note begins */
                    SynthState.iScanningGapFrontInEnvelopeTicks = 0;

                    /* initialize accumulators */
                    SynthState.dEnvelopeClockAccumulatorFraction = 0;
                    SynthState.dNoteDurationClockAccumulatorFraction = 0;
                    /* calculate increment factors */
                    SynthState.dSamplesPerEnvelopeClock = SynthParams.dSamplingRate / SynthParams.dEnvelopeRate;
                    SynthState.dDurationTicksPerEnvelopeClock
                        = ((SynthParams.dCurrentBeatsPerMinute / (4/*beats per whole note*/ * 60/*seconds per minute*/))
                            / SynthParams.dEnvelopeRate) * DURATIONUPDATECLOCKRESOLUTION;


#if true // PARALLEL
                    // Assign section effect input accumulator indices.
                    // Ensure that section count used to allocate workspaces is the same as the count
                    // that BuildPlayList() came up with.
                    if (SynthState.concurrency > 0)
                    {
                        // assign section workspaces
                        {
                            int sectionPairIndex = 0;

                            SynthState.DefaultSectionEffectSurrogate = new SecEffRec();
                            SynthState.scoreEffectInputAccumulatorIndex = sectionPairIndex++; // always one: score effects (equivalent to the default 'null' section)
                            SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex = SynthState.scoreEffectInputAccumulatorIndex; // synonymous

                            Scan = SynthState.PlayTrackList;
                            SecEffRec CurrentEffectHandle = null;
                            List<SecEffRec> sections = new List<SecEffRec>();
                            int trackCount = 0;
                            SynthState.DefaultSectionEffectSurrogate.inputTracks = new PlayListNodeRec[0]; // build input list
                            while (Scan != null)
                            {
                                trackCount++;

                                if (CurrentEffectHandle != Scan.SectionEffectHandle)
                                {
                                    CurrentEffectHandle = Scan.SectionEffectHandle;
                                    if (CurrentEffectHandle != null)
                                    {
                                        // assign section input workspace
                                        sections.Add(CurrentEffectHandle);
                                        CurrentEffectHandle.sectionInputAccumulatorIndex = sectionPairIndex++;

                                        // build input list
                                        CurrentEffectHandle.inputTracks = new PlayListNodeRec[0];
                                    }
                                    // else - default section already assigned before loop
                                }

                                if (CurrentEffectHandle != null)
                                {
                                    // build target count
                                    CurrentEffectHandle.sectionInputTarget++;

                                    // build input list
                                    int l = CurrentEffectHandle.inputTracks.Length;
                                    Array.Resize(ref CurrentEffectHandle.inputTracks, l + 1);
                                    CurrentEffectHandle.inputTracks[l] = Scan;
                                }
                                else
                                {
                                    // build target count
                                    SynthState.DefaultSectionEffectSurrogate.sectionInputTarget++;

                                    // build input list
                                    int l = SynthState.DefaultSectionEffectSurrogate.inputTracks.Length;
                                    Array.Resize(ref SynthState.DefaultSectionEffectSurrogate.inputTracks, l + 1);
                                    SynthState.DefaultSectionEffectSurrogate.inputTracks[l] = Scan;
                                }

                                Scan = Scan.Next;
                            }
                            // sectionCount includes default/score effect
                            Debug.Assert(sectionCount == sectionPairIndex);
                            Debug.Assert(sectionCount == sections.Count + 1); // sections currently excludes default/score effect

                            SynthState.SectionArrayExcludesDefault = sections.ToArray();
                            sections.Add(SynthState.DefaultSectionEffectSurrogate);
                            Debug.Assert(sectionCount == sections.Count);
                            SynthState.SectionArrayAll = sections.ToArray();

                            SynthState.CombinedPlayArray = new object[trackCount + sections.Count - 1];
                            {
                                int index = 0;
                                for (int i = 0; i < SynthState.SectionArrayAll.Length; i++)
                                {
                                    for (int j = 0; j < SynthState.SectionArrayAll[i].inputTracks.Length; j++)
                                    {
                                        SynthState.CombinedPlayArray[index++] = SynthState.SectionArrayAll[i].inputTracks[j];
                                    }
                                    if (SynthState.SectionArrayAll[i] != SynthState.DefaultSectionEffectSurrogate)
                                    {
                                        SynthState.CombinedPlayArray[index++] = SynthState.SectionArrayAll[i];
                                    }
                                }
                                Debug.Assert(index == SynthState.CombinedPlayArray.Length);
                            }

                            // add all sections as inputs to the score effect input count
                            SynthState.DefaultSectionEffectSurrogate.sectionInputTarget += SynthState.SectionArrayAll.Length - 1; // don't include self in input count
                        }

                        // create per-thread contexts
                        SynthState.SynthParamsPerProc = new SynthParamRec[Math.Max(SynthState.concurrency, 1)];
                        SynthState.SynthParamsPerProc[0] = SynthParams;
                        for (int i = 1; i < SynthState.concurrency; i++)
                        {
                            SynthState.SynthParamsPerProc[i] = new SynthParamRec(SynthParams);
                        }

                        // create synchronization objects
                        SynthState.startBarrier = new ManualResetEvent(false);
                        SynthState.endBarrier = new ManualResetEvent(false);

                        // start threads (empty array in case of concurrency == 1)
                        SynthState.threads = new Thread[SynthState.concurrency];
                        for (int i = 1; i < SynthState.concurrency; i++)
                        {
                            SynthState.threads[i] = new Thread(new ParameterizedThreadStart(ThreadMain));
                            SynthState.threads[i].Start(new ThreadContext(i, SynthState));
                        }
                        SynthState.threads[0] = Thread.CurrentThread;
                    }
#endif

                    // initialize debugging helpers
                    if (automationSettings.BreakFrames != null)
                    {
                        SynthState.breakFrames = (long[])automationSettings.BreakFrames.Clone();
                        Array.Sort(SynthState.breakFrames);
                    }
                    SynthState.summaryTag = automationSettings.SummaryTag;
                    if ((automationSettings.TraceSchedulePath != null) && (SynthState.concurrency > 0))
                    {
                        if (automationSettings.Level2TraceFlags != AutomationSettings.TraceFlags.None)
                        {
                            SynthState.traceScheduleEnableLevel2 = true;

                            for (int i = 0; i < SynthState.TrackPlayersInFileOrder.Count; i++)
                            {
                                SynthState.TrackPlayersInFileOrder[i].ThisTrack.events = new List<EventTraceRec>();
                            }
                            for (int i = 0; i < SynthState.SectionArrayAll.Length; i++)
                            {
                                SynthState.SectionArrayAll[i].events = new List<EventTraceRec>();
                            }

                            for (int i = 0; i < SynthState.SynthParamsPerProc.Length; i++)
                            {
                                SynthState.SynthParamsPerProc[i].level2TraceFlags = automationSettings.Level2TraceFlags;
                            }
                        }

                        SynthState.traceScheduleWriter = new StreamWriter(automationSettings.TraceSchedulePath, false/*append*/, Encoding.UTF8, Constants.BufferSize);
                        SynthState.traceScheduleWriter.NewLine = "\r"; // they are very large files - easy to save some storage this way
                        long freq;
                        Timing.QueryPerformanceFrequency(out freq);
                        SynthState.traceScheduleWriter.WriteLine("version\t{0}", 1);
                        SynthState.traceScheduleWriter.WriteLine("level\t{0}", SynthState.traceScheduleEnableLevel2 ? 2 : 1);
                        SynthState.traceScheduleWriter.WriteLine("tres\t{0}", freq);
                        SynthState.traceScheduleWriter.WriteLine("srate\t{0}", SamplingRate);
                        SynthState.traceScheduleWriter.WriteLine("erate\t{0}", EnvelopeRate);
                        SynthState.traceScheduleWriter.WriteLine("threads\t{0}", SynthState.concurrency);
                        SynthState.traceScheduleWriter.WriteLine(":");
                        int id = 0;
                        for (int i = 0; i < SynthState.SectionArrayAll.Length; i++)
                        {
                            SynthState.SectionArrayAll[i].traceInfo.id = id++;

                            string name;
                            if (SynthState.SectionArrayAll[i] == SynthState.DefaultSectionEffectSurrogate)
                            {
                                name = "default";
                            }
                            else
                            {
                                name = String.Concat("\"", SynthState.SectionArrayAll[i].SectionObject.Name, "\"");
                            }
                            SynthState.traceScheduleWriter.WriteLine(
                                "{0}\tsection\t{1}",
                                SynthState.SectionArrayAll[i].traceInfo.id,
                                name);
                        }
                        Scan = SynthState.PlayTrackList;
                        while (Scan != null)
                        {
                            Scan.traceInfo.id = id++;

                            SynthState.traceScheduleWriter.WriteLine(
                                "{0}\ttrack\t{1}\t\"{2}\"",
                                Scan.traceInfo.id, Scan.SectionEffectHandle != null
                                    ? Scan.SectionEffectHandle.traceInfo.id
                                    : SynthState.DefaultSectionEffectSurrogate.traceInfo.id,
                                Scan.TrackObject.Name);
                            Scan = Scan.Next;
                        }
                        SynthState.traceScheduleWriter.WriteLine();
                    }


                    SynthStateOut = SynthState;

                    return SynthErrorCodes.eSynthDone;
                }
                finally
                {
                    if ((SynthStateOut == null) && (SynthState != null))
                    {
                        SynthState.Dispose();
                    }
                }
            }

            /* compare tracks for sorting based on the section they are in.  before sorting, */
            /* set the track objects' AuxVal to their position in the array to enable stable */
            /* sorting. */
            private static int ComparePlayListNodeOnAuxVal(
                PlayListNodeRec Left,
                PlayListNodeRec Right)
            {
                return Left.TrackObject.AuxVal.CompareTo(Right.TrackObject.AuxVal);
            }

            /* build hash table for converting sequence command track/group identifier */
            /* into the actual thing.  The SequencerTableRec contains pointers to */
            /* PlayTrackInfoRec objects. */
            private static SynthErrorCodes BuildSequencerTable(
                Document Document,
                SequencerTableRec Table,
                List<PlayListNodeRec> TrackPlayersInFileOrder,
                SynthErrorInfoRec ErrorInfo,
                SynthParamRec SynthParams)
            {
                SequencerConfigSpecRec SeqConfig = Document.Sequencer.SequencerSpec;

                List<TrackObjectRec> TrackList = new List<TrackObjectRec>(Document.TrackList);


                /* build up groups */
                int l = GetSequencerConfigLength(SeqConfig);
                int lPlayers = TrackPlayersInFileOrder.Count;
                for (int i = 0; i < l; i += 1)
                {
                    string TrackName = SequencerConfigGetTrackName(SeqConfig, i);
                    string GroupName = SequencerConfigGetGroupName(SeqConfig, i);
                    if (String.Equals(GroupName, "*"))
                    {
                        ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExDontUseAsteriskAsTrackOrGroupName;
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                    TrackObjectRec TrackObject = TrackList.Find(delegate(TrackObjectRec candidate) { return String.Equals(candidate.Name, TrackName); });

                    /* make sure the track exists */
                    if (TrackObject == null)
                    {
                        ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedTrackInGroupTable;
                        ErrorInfo.TrackName = TrackName;
                        return SynthErrorCodes.eSynthErrorEx;
                    }

                    /* find the player for that track object (may not find anything if */
                    /* that track has been turned off) */
                    for (int j = 0; j < lPlayers; j += 1)
                    {
                        PlayListNodeRec Player = TrackPlayersInFileOrder[j];
                        if (Player.TrackObject == TrackObject)
                        {
                            SequencerTableInsert(
                                Table,
                                GroupName,
                                Player.ThisTrack);
                            break;
                        }
                    }
                }


                /* build up individual tracks, and the default-all group */
                for (int i = 0; i < lPlayers; i += 1)
                {
                    PlayListNodeRec Player = TrackPlayersInFileOrder[i];

                    string TrackName = Player.TrackObject.Name;
                    if (String.Equals(TrackName, "*"))
                    {
                        ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExDontUseAsteriskAsTrackOrGroupName;
                        return SynthErrorCodes.eSynthErrorEx;
                    }

                    /* add entry for individual track */
                    SequencerTableInsert(
                        Table,
                        TrackName,
                        Player.ThisTrack);

                    /* add entry for default-all group */
                    SequencerTableInsert(
                        Table,
                        "*",
                        Player.ThisTrack);
                }

                return SynthErrorCodes.eSynthDone;
            }

            /* compare tracks for sorting based on the section they are in.  before sorting, */
            /* set the track objects' AuxVal to their position in the array to enable stable */
            /* sorting. */
            // Public for use in SectionEditDialog
            public static int CompareTracksOnSection(
                TrackObjectRec Left,
                TrackObjectRec Right,
                IList<SectionObjectRec> Sections)
            {
                SectionObjectRec LeftSection;
                SectionObjectRec RightSection;
                int LeftValue;
                int RightValue;

                LeftSection = Left.Section;
                RightSection = Right.Section;
                if (LeftSection == null)
                {
                    LeftValue = -1;
                }
                else
                {
                    LeftValue = Sections.IndexOf(LeftSection);
                }
                if (RightSection == null)
                {
                    RightValue = -1;
                }
                else
                {
                    RightValue = Sections.IndexOf(RightSection);
                }
                if (LeftValue == RightValue)
                {
                    /* subkey enables stable sorting */
                    LeftValue = Left.AuxVal;
                    RightValue = Right.AuxVal;
                }
                return LeftValue.CompareTo(RightValue);
            }

            /* build the list of objects involved in playing.  the list can be scanned in */
            /* sequence and all tracks which share the same effect processor will be */
            /* next to each other. */
            private static SynthErrorCodes BuildPlayList(
                out PlayListNodeRec ListOut,
                List<TrackObjectRec> TrackObjectList,
                TrackEffectGenRec ScoreEffectProcessor,
                List<PlayListNodeRec> TrackPlayersInFileOrder,
                TempoControlRec TempoControl,
                SynthParamRec SynthParams)
            {
                ListOut = null;

                SynthErrorCodes Result;
                PlayListNodeRec Tail = null;
                SectionObjectRec CurrentSection = null; /* section or null for default section */
                SecEffRec CurrentSectionHandle = null; /* processor, or null if above is null */

                /* create track list */
                List<TrackObjectRec> SortedTrackList = new List<TrackObjectRec>(TrackObjectList.Count);

                /* fill in the track list */
                int TrackCount = TrackObjectList.Count;
                for (int i = 0; i < TrackCount; i += 1)
                {
                    /* get the track */
                    TrackObjectRec TrackObj = TrackObjectList[i];
                    SortedTrackList.Add(TrackObj);

                    /* set stable sort key */
                    TrackObj.AuxVal = i;
                }
                TrackCount = SortedTrackList.Count; /* may be fewer tracks */

                /* sort, to group all tracks together which share the same section */
                SortedTrackList.Sort(delegate(TrackObjectRec left, TrackObjectRec right) { return CompareTracksOnSection(left, right, SynthParams.Document.SectionList); });

                /* build list of tracks that are being played */
                /* NOTE: this loop iterates in the sorted order, so all tracks in a */
                /* section are adjacent.  This makes it easy to assign all the appropriate */
                /* tracks to a single section processor.  We can also set the position */
                /* numbers for detecting bad sequence commands. */
                for (int i = 0; i < TrackCount; i += 1)
                {
                    /* get the track */
                    TrackObjectRec PossibleTrack = SortedTrackList[i];

                    PlayListNodeRec NewListNode = new PlayListNodeRec();

                    /* establish link */
                    NewListNode.Next = null;
                    if (Tail == null)
                    {
                        Debug.Assert(ListOut == null);
                        ListOut = NewListNode;
                    }
                    else
                    {
                        Tail.Next = NewListNode;
                    }
                    Tail = NewListNode;

                    /* get section for current track */
                    SectionObjectRec ThisTracksSection = PossibleTrack.Section;

                    /* build new section if current track isn't in current section */
                    if (ThisTracksSection != CurrentSection)
                    {
                        /* new section */
                        CurrentSection = ThisTracksSection;
                        if (CurrentSection == null)
                        {
                            /* default section gets no proc */
                            CurrentSectionHandle = null;
                        }
                        else
                        {
                            CurrentSectionHandle = new SecEffRec();

                            CurrentSectionHandle.SectionObject = CurrentSection;

                            CurrentSectionHandle.SectionTemplate = CurrentSection.EffectSpec;
                            if (CurrentSectionHandle.SectionTemplate == null)
                            {
                                return SynthErrorCodes.eSynthPrereqError;
                            }
                            Result = NewTrackEffectGenerator(
                                CurrentSectionHandle.SectionTemplate,
                                SynthParams,
                                out CurrentSectionHandle.SectionEffect);
                            if (Result != SynthErrorCodes.eSynthDone)
                            {
                                return Result;
                            }

                            CheckUnrefParamRec Param = new CheckUnrefParamRec();
                            Param.Dictionary = SynthParams.Dictionary;
                            Param.CodeCenter = SynthParams.Document.CodeCenter;
                            Param.ErrorInfo = SynthParams.ErrorInfo;
                            Result = CheckEffectListForUnreferencedSamples(
                                CurrentSectionHandle.SectionTemplate,
                                Param);
                            if (Result != SynthErrorCodes.eSynthDone)
                            {
                                string Name = CurrentSection.Name;
                                SynthParams.ErrorInfo.SectionName = Name;
                                return Result;
                            }
                        }
                    }

                    /* track starts out active */
                    NewListNode.IsActive = true;

                    /* remember the current effect processor */
                    NewListNode.SectionEffectHandle = CurrentSectionHandle;

                    /* build track player */
                    NewListNode.TrackObject = PossibleTrack;
                    Result = NewPlayTrackInfo(
                        out NewListNode.ThisTrack,
                        PossibleTrack,
                        SynthParams.Document.InstrumentList,
                        ScoreEffectProcessor,
                        CurrentSectionHandle != null
                            ? CurrentSectionHandle.SectionEffect
                            : null,
                        TempoControl,
                        SynthParams);
                    if (Result != SynthErrorCodes.eSynthDone)
                    {
                        return Result;
                    }

                    PlayTrackInfoSetPositionNumber(NewListNode.ThisTrack, i);
                    TrackPlayersInFileOrder.Add(NewListNode);
                }

                /* finish the properly ordered list: */
                /* TrackPlayersInFileOrder currently has PlayListNodeRec in the */
                /* same order that the section-sorted list has */
                /* sort them on AuxVal back to original order */
                TrackPlayersInFileOrder.Sort(ComparePlayListNodeOnAuxVal);
#if DEBUG
                {
                    int j = 0;
                    for (int i = 0; i < TrackPlayersInFileOrder.Count; i += 1)
                    {
                        PlayListNodeRec Node = TrackPlayersInFileOrder[i];

                        while (true)
                        {
                            if (j >= TrackObjectList.Count)
                            {
                                // TrackPlayersInFileOrder is wrong
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                            TrackObjectRec TrackObject = TrackObjectList[j];
                            if (TrackObject == Node.TrackObject)
                            {
                                break;
                            }
                            j += 1;
                        }
                    }
                }
#endif

                return SynthErrorCodes.eSynthDone;
            }

            /* this routine scans through the key playlist and determines the exact point */
            /* at which playback should begin. */
            private static void FindStartPoint(
                TrackObjectRec KeyTrack,
                int FrameToStartAt,
                out FractionRec StartTimeOut)
            {
                const int Denominator = 64 * 3 * 5 * 7 * 2;
                Debug.Assert(Denominator == Constants.Denominator);
                Debug.Assert(Denominator == DURATIONUPDATECLOCKRESOLUTION);
                FractionRec Counter = new FractionRec(0, 0, Denominator);

                if (KeyTrack != null)
                {
#if DEBUG
                    if (FrameToStartAt > KeyTrack.FrameArray.Count)
                    {
                        // start frame is beyond end of track
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
#endif
                    for (int i = 0; i < FrameToStartAt; i += 1)
                    {
                        FrameObjectRec Frame = KeyTrack.FrameArray[i];
                        FractionRec TempDuration;
                        Frame.DurationOfFrame(out TempDuration);
                        FractionRec.AddFractions(TempDuration, Counter, out Counter);
                    }
                }
                StartTimeOut = Counter;
            }

            /* finalize before termination */
            public static void FinalizeSynthesizer(
                SynthStateRec SynthState,
                bool writeOutputLogs)
            {
                // exit aux threads
                if (SynthState.concurrency > 1)
                {
                    SynthState.exit = 1;
                    SpinWaitOnThreadsStarting(SynthState);
#if DEBUG
                    Interlocked.Increment(ref SynthState.epoch);
#endif
                    SynthState.startBarrier.Set();
                    SynthState.endBarrier.Set();

                    // wait for exit (not strictly necessary, but helps ensure correctness)
                    while (true)
                    {
                        bool allExited = true;
                        Debug.Assert(SynthState.concurrency == SynthState.threads.Length);
                        for (int i = 1; i < SynthState.concurrency; i++)
                        {
                            if (SynthState.threads[i].ThreadState != System.Threading.ThreadState.Stopped)
                            {
                                allExited = false;
                                break;
                            }
                        }
                        if (allExited)
                        {
                            break;
                        }
                        Thread.Sleep(50);
                    }
                }

                PlayListNodeRec Scan = SynthState.PlayTrackList;
                SecEffRec CurrentEffectHandle = Scan.SectionEffectHandle;
                while (Scan != null)
                {
                    while ((Scan != null) && (Scan.SectionEffectHandle == CurrentEffectHandle))
                    {
                        FinalizePlayTrack(
                            Scan.ThisTrack,
                            SynthState.SynthParams0,
                            writeOutputLogs);

                        Scan = Scan.Next;
                    }

                    if (CurrentEffectHandle != null)
                    {
                        FinalizeTrackEffectGenerator(
                            CurrentEffectHandle.SectionEffect,
                            SynthState.SynthParams0,
                            writeOutputLogs);
                    }

                    /* grab the next section effect handle */
                    if (Scan != null)
                    {
                        CurrentEffectHandle = Scan.SectionEffectHandle;
                    }
                }

                FinalizeTrackEffectGenerator(
                    SynthState.ScoreEffectProcessor,
                    SynthState.SynthParams0,
                    writeOutputLogs);

                SynthState.Dispose();

                Program.SaveFFTWWisdomIfNeeded();
            }

            // Called by main thread to ensure all aux threads have arrived at the start barrier
            // (prerequisite for releasing threads)
            private static void SpinWaitOnThreadsStarting(
                SynthStateRec SynthState)
            {
                while (SynthState.startingThreadCount != SynthState.concurrency - 1)
                {
                }
            }

            // Called by aux threads to wait for main thread to release them for cycle work.
            private static void SpinWaitOnStartBarrierReleased(
                SynthStateRec SynthState)
            {
                while (SynthState.startBarrierReleaseSpin == 0)
                {
                }
            }

            // Called by main thread to wait for aux threads to finish work and arrive at the end barrier
            // (prerequisite for sequential finishing work on the cycle)
            private static void SpinWaitOnThreadsCompletion(
                SynthStateRec SynthState)
            {
                while (SynthState.completionThreadCount != SynthState.concurrency - 1)
                {
                }
            }

            /* generate one envelope iteration of sample data.  if it worked ok it returns */
            /* eSynthDone, otherwise it returns an error code.  if playback is completely */
            /* finished, it returns with *NumFrames == 0. */
            public static SynthErrorCodes SynthGenerateOneCycle(
                SynthStateRec SynthState,
                out int nActualFramesOut)
            {
                long time0;
                Timing.QueryPerformanceCounter(out time0);
                if (SynthState.time3 != 0)
                {
                    SynthState.phase3Time += time0 - SynthState.time3;
                }

                int effectiveCurrency = SynthState.concurrency;

                SynthErrorCodes Result = SynthErrorCodes.eSynthDone;
                int nActualFrames = 0;
                int NumNoteDurationTicks = 0;
                bool UpdateEnvelopes = false;
                bool AreWeStillFastForwarding = false;
                bool OkToTerminateIfNoData = true;
                bool fScheduledSkip = false;

#if false // TODO: if we can (https://github.com/dotnet/corefx/issues/5183)
        		bool fOldDenormal = FloatingPointEnableDenormals(false); /* use flush-to-zero */
#endif

                /* default results signal to stop playback */
                nActualFramesOut = 0;


                /* play -- this is the primary outer loop for the synthesizer */

                /* are any tracks still active? if not, then just stop */
                bool AnyTrackActive = false;
                {
                    PlayListNodeRec Scan = SynthState.PlayTrackList;
                    while (Scan != null)
                    {
                        if (Scan.IsActive)
                        {
                            AnyTrackActive = true;
                            break;
                        }
                        Scan = Scan.Next;
                    }
                }
                if (AnyTrackActive)
                {
                    OkToTerminateIfNoData = false;

                    /* figure out how many note duration ticks to generate */
                    /* increment counter */
                    SynthState.dNoteDurationClockAccumulatorFraction += SynthState.dDurationTicksPerEnvelopeClock;
                    /* round down */
                    NumNoteDurationTicks = (int)SynthState.dNoteDurationClockAccumulatorFraction;
                    /* subtract off what we're taking out this time around, */
                    /* leaving the extra little bit in there */
                    SynthState.dNoteDurationClockAccumulatorFraction -= NumNoteDurationTicks;

                    /* determine if we are fast forwarding */
                    SynthState.dMomentOfStartingDurationTick -= NumNoteDurationTicks;
                    AreWeStillFastForwarding = (SynthState.dMomentOfStartingDurationTick > 0);

                    /* if we're fast forwarding, change dDurationTicksPerEnvelopeClock */
                    /* to advance us to the next event */
                    if (AreWeStillFastForwarding)
                    {
                        int MinimumTicks = 0x7fffffff;
                        int ExtraTicks;

                        PlayListNodeRec Scan = SynthState.PlayTrackList;
                        while (Scan != null)
                        {
                            if (Scan.IsActive)
                            {
                                int ThisTrackTicks;

                                ThisTrackTicks = PlayTrackEventLookahead(Scan.ThisTrack);
                                if (MinimumTicks > ThisTrackTicks)
                                {
                                    MinimumTicks = ThisTrackTicks;
                                }
                            }

                            Scan = Scan.Next;
                        }
                        if (MinimumTicks > (int)(SynthState.dMomentOfStartingDurationTick - 1))
                        {
                            MinimumTicks = (int)(SynthState.dMomentOfStartingDurationTick - 1);
                        }
                        if (MinimumTicks < NumNoteDurationTicks)
                        {
                            MinimumTicks = NumNoteDurationTicks;
                        }

                        SynthState.dDurationTicksPerEnvelopeClock = MinimumTicks;

                        /* recompute */

                        /* figure out how many note duration ticks to generate */
                        /* increment counter */
                        /* subtract old duration ticks because we've already */
                        /* accumulated it before this if statement */
                        SynthState.dNoteDurationClockAccumulatorFraction
                            += (SynthState.dDurationTicksPerEnvelopeClock - NumNoteDurationTicks);
                        /* round down */
                        ExtraTicks = (int)SynthState.dNoteDurationClockAccumulatorFraction;
                        NumNoteDurationTicks += ExtraTicks;
                        /* subtract off what we're taking out this time around, */
                        /* leaving the extra little bit in there */
                        SynthState.dNoteDurationClockAccumulatorFraction -= ExtraTicks;

                        /* determine if we are still fast forwarding */
                        SynthState.dMomentOfStartingDurationTick -= ExtraTicks;
                        AreWeStillFastForwarding = SynthState.dMomentOfStartingDurationTick > 0;
                    }

                    /* figure out how many samples to do before the next envelope */
                    if ((SynthState.iScanningGapFrontInEnvelopeTicks >= SynthState.SynthParams0.iScanningGapWidthInEnvelopeTicks)
                        && !AreWeStillFastForwarding)
                    {
                        /* we're really sampling stuff */
                        SynthState.dEnvelopeClockAccumulatorFraction += SynthState.dSamplesPerEnvelopeClock;
                        nActualFrames = (int)SynthState.dEnvelopeClockAccumulatorFraction;
                        SynthState.dEnvelopeClockAccumulatorFraction -= nActualFrames;

                        /* increment global clock */
                        SynthState.SynthParams0.lElapsedTimeInEnvelopeTicks += 1;
                        SynthState.SynthParams0.dElapsedTimeInSeconds =
                            (double)SynthState.SynthParams0.lElapsedTimeInEnvelopeTicks
                            / SynthState.SynthParams0.dEnvelopeRate;
                    }
                    else
                    {
                        /* scanning gap is still opening (or we're fast-forwarding), */
                        /* so we're not sampling */
                        nActualFrames = 0;
                    }

                    if ((SynthState.breakFrames != null) && (SynthState.breakFramesIndex < SynthState.breakFrames.Length))
                    {
                        while (SynthState.breakFramesIndex < SynthState.breakFrames.Length
                            && (SynthState.breakFrames[SynthState.breakFramesIndex] >= SynthState.lTotalFramesGenerated)
                            && (SynthState.breakFrames[SynthState.breakFramesIndex] < SynthState.lTotalFramesGenerated + nActualFrames))
                        {
                            Debugger.Break();
                            SynthState.breakFramesIndex++;
                        }
                    }

                    /* this condition is responsible for opening the scanning gap */
                    UpdateEnvelopes = (SynthState.iScanningGapFrontInEnvelopeTicks
                        >= SynthState.SynthParams0.iScanningGapWidthInEnvelopeTicks);
                    double OneOverDurationTicksPerEnvelopeClock = (double)1 / SynthState.dDurationTicksPerEnvelopeClock;

                    /* check fast forward */
                    fScheduledSkip = SkipSegmentUpdateOneCycle(
                        SynthState.SkipSchedule,
                        UpdateEnvelopes ? 1 : 0,
                        NumNoteDurationTicks);
                    if (fScheduledSkip)
                    {
                        nActualFrames = 0;
                    }
                    if (fScheduledSkip != SynthState.fLastCycleWasScheduledSkip)
                    {
                        /* just entered a scheduled skip -- notify all tracks */
                        PlayListNodeRec Scan = SynthState.PlayTrackList;
                        while (Scan != null)
                        {
                            PlayTrackInfoEnteringOrJustLeftScheduledSkip(
                                Scan.ThisTrack,
                                fScheduledSkip);
                            Scan = Scan.Next;
                        }

                        if (SynthState.DefaultSectionEffectSurrogate.events != null)
                        {
                            SynthState.DefaultSectionEffectSurrogate.events.Add(
                                new EventTraceRec(
                                    fScheduledSkip ? EventTraceType.SkipEnter : EventTraceType.SkipLeave,
                                    SynthState.DefaultSectionEffectSurrogate.seqGen++));
                        }
                    }

                    /* verify the output workspace array size */
#if DEBUG
                    if (nActualFrames > SynthState.SynthParams0.nAllocatedPointsOneChannel)
                    {
                        // workspace isn't big enough
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
#endif

                    // If scheduled skip, then prevent activating the threads since they have nothing
                    // to do and the overhead causes severe performance degradation in this case.
                    if (fScheduledSkip && (effectiveCurrency > 1))
                    {
                        effectiveCurrency = 1;
                    }

                    // Prerelease aux threads to absorb OS startup overhead while finishing
                    // initial sequential processing on main thread. Aux threads wait at the
                    // spin-barrier which is released later.
                    if (effectiveCurrency > 1)
                    {
                        SpinWaitOnThreadsStarting(SynthState);

                        SynthState.endBarrier.Reset();
                        SynthState.startBarrierReleaseSpin = 0;
                        SynthState.startBarrier.Set();
                        //SynthState.startBarrierReleaseSpin = 1; -- final bit
                    }

                    /* initialize the array */
                    if (effectiveCurrency == 0)
                    {
#if DEBUG
                        for (int i = 0; i < SynthState.SynthParams0.workspace.Length; i++)
                        {
                            SynthState.SynthParams0.workspace[i] = Single.NaN;
                        }
#endif
                        FloatVectorZero(
                            SynthState.SynthParams0.workspace,
                            SynthState.SynthParams0.ScoreWorkspaceLOffset,
                            nActualFrames);
                        FloatVectorZero(
                            SynthState.SynthParams0.workspace,
                            SynthState.SynthParams0.ScoreWorkspaceROffset,
                            nActualFrames);
                    }


                    // Factored out phase 1 of envelope update (non-parallelizable) to this loop here.
                    // Note that segmentation based on SectionEffectHandle grouping is not needed here
                    {
                        PlayListNodeRec Scan = SynthState.PlayTrackList;
                        /* iterate over all active tracks */
                        while (Scan != null)
                        {
                            /* if track is active, then play it */
                            if (Scan.IsActive)
                            {
                                Result = PlayTrackUpdateControl(
                                    Scan.ThisTrack,
                                    UpdateEnvelopes/*scanning gap control*/,
                                    NumNoteDurationTicks,
                                    OneOverDurationTicksPerEnvelopeClock/*envelope ticks per duration tick*/,
                                    SynthState.iScanningGapFrontInEnvelopeTicks,
                                    AreWeStillFastForwarding,
                                    SynthState.SkipSchedule,
                                    SynthState.SynthParams0);
                                if (Result != SynthErrorCodes.eSynthDone)
                                {
                                    goto Error;
                                }
                            }
                            /* next one please */
                            Scan = Scan.Next;
                        }
                    }
                }

                // break "if (AnyTrackActive)" to log time, then reenter guarded region

                long time1;
                Timing.QueryPerformanceCounter(out time1);
                SynthState.phase0Time += time1 - time0;

                SynthState.randomSeedProvider.Freeze();

#if true // PARALLEL
                long time2 = time1; // bogus initialization
                if (AnyTrackActive)
                {
                    if (effectiveCurrency == 0)
                    {
#endif
                        #region sequential method
                        // sequential method

                        /* perform execution cycle */
                        PlayListNodeRec Scan = SynthState.PlayTrackList;
                        /* start with the first effect handle */
#if DEBUG
                        if (Scan == null)
                        {
                            // empty track list
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }
#endif
                        SecEffRec CurrentEffectHandle = Scan.SectionEffectHandle;
                        /* iterate over all active tracks */
                        while (Scan != null)
                        {
                            int TargetSectionWorkSpaceLOffset;
                            int TargetSectionWorkSpaceROffset;

                            /* initialize section buffer, if not default proc */
                            if (CurrentEffectHandle != null)
                            {
                                /* section processor exists, use special section workspace */
                                TargetSectionWorkSpaceLOffset = SynthState.SynthParams0.SectionWorkspaceLOffset;
                                TargetSectionWorkSpaceROffset = SynthState.SynthParams0.SectionWorkspaceROffset;
                                if (UpdateEnvelopes)
                                {
                                    FloatVectorZero(
                                        SynthState.SynthParams0.workspace,
                                        TargetSectionWorkSpaceLOffset,
                                        nActualFrames);
                                    FloatVectorZero(
                                        SynthState.SynthParams0.workspace,
                                        TargetSectionWorkSpaceROffset,
                                        nActualFrames);
                                }
                            }
                            else
                            {
                                /* no section processor, just write to score workspace */
                                TargetSectionWorkSpaceLOffset = SynthState.SynthParams0.ScoreWorkspaceLOffset;
                                TargetSectionWorkSpaceROffset = SynthState.SynthParams0.ScoreWorkspaceROffset;
                            }

                            /* iterate over all tracks that share the current effects proc */
                            /* (the key is that all tracks in a section are adjacent in the */
                            /* list so we can just scan until we find a different section */
                            /* handle) */
                            while ((Scan != null) && (Scan.SectionEffectHandle == CurrentEffectHandle))
                            {
                                /* if track is active, then play it */
                                if (Scan.IsActive)
                                {
#if false // TODO: remove - factoring out phase 1 of envelope update (non-parallelizable) to this loop here
                                    Result = PlayTrackUpdateControl(
                                        Scan.ThisTrack,
                                        UpdateEnvelopes/*scanning gap control*/,
                                        NumNoteDurationTicks,
                                        OneOverDurationTicksPerEnvelopeClock/*envelope ticks per duration tick*/,
                                        SynthState.iScanningGapFrontInEnvelopeTicks,
                                        AreWeStillFastForwarding,
                                        SynthState.SkipSchedule,
                                        SynthState.SynthParams);
                                    if (Result != SynthErrorCodes.eSynthDone)
                                    {
                                        goto Error;
                                    }
#endif

                                    // phase 2 (parallelizable) of envelope update
                                    Result = PlayTrackUpdateEnvelopes(
                                        Scan.ThisTrack,
                                        UpdateEnvelopes/*scanning gap control*/,
                                        AreWeStillFastForwarding,
                                        SynthState.SynthParams0);
                                    if (Result != SynthErrorCodes.eSynthDone)
                                    {
                                        goto Error;
                                    }

                                    if (!AreWeStillFastForwarding && !fScheduledSkip)
                                    {
                                        /* only generate wave if we're playing for real */
                                        PlayTrackGenerateWave(
                                            Scan.ThisTrack,
                                            UpdateEnvelopes/*scanning gap control*/,
                                            SynthState.SynthParams0.workspace,
                                            nActualFrames,
                                            TargetSectionWorkSpaceLOffset,
                                            TargetSectionWorkSpaceROffset,
                                            SynthState.SynthParams0.TrackWorkspaceLOffset,
                                            SynthState.SynthParams0.TrackWorkspaceROffset,
                                            SynthState.SynthParams0.OscillatorWorkspaceLOffset,
                                            SynthState.SynthParams0.OscillatorWorkspaceROffset,
                                            SynthState.SynthParams0.CombinedOscillatorWorkspaceLOffset,
                                            SynthState.SynthParams0.CombinedOscillatorWorkspaceROffset,
                                            SynthState.SynthParams0);
                                    }

                                    PlayTrackFinish(
                                        Scan.ThisTrack,
                                        UpdateEnvelopes/*scanning gap control*/,
                                        NumNoteDurationTicks,
                                        fScheduledSkip,
                                        SynthState.SynthParams0);
                                    if (!PlayTrackIsItStillActive(Scan.ThisTrack))
                                    {
                                        Scan.IsActive = false;
                                    }
                                }

                                /* next one please */
                                Scan = Scan.Next;
                            }

                            /* apply section effects to workspace, if they exist, accumulating */
                            /* result into score workspace.  (if no section effects, then results */
                            /* are already in score workspace, and we skip this processing.) */
                            if (CurrentEffectHandle != null)
                            {
                                /* apply processor */
                                if (UpdateEnvelopes)
                                {
                                    /* if we are generating samples, then we should */
                                    /* apply the score effects processor */
                                    TrackEffectProcessQueuedCommands(
                                        CurrentEffectHandle.SectionEffect,
                                        SynthState.SynthParams0);

                                    if (!AreWeStillFastForwarding)
                                    {
                                        /* control-update cycle */
                                        UpdateStateTrackEffectGenerator(
                                            CurrentEffectHandle.SectionEffect,
                                            SynthState.SynthParams0);

                                        /* generate wave */
                                        ApplyTrackEffectGenerator(
                                            CurrentEffectHandle.SectionEffect,
                                            SynthState.SynthParams0.workspace,
                                            nActualFrames,
                                            SynthState.SynthParams0.SectionWorkspaceLOffset,
                                            SynthState.SynthParams0.SectionWorkspaceROffset,
                                            SynthState.SynthParams0);
                                        FloatVectorAcc(
                                            SynthState.SynthParams0.workspace,
                                            SynthState.SynthParams0.SectionWorkspaceLOffset,
                                            SynthState.SynthParams0.workspace,
                                            SynthState.SynthParams0.ScoreWorkspaceLOffset,
                                            nActualFrames);
                                        FloatVectorAcc(
                                            SynthState.SynthParams0.workspace,
                                            SynthState.SynthParams0.SectionWorkspaceROffset,
                                            SynthState.SynthParams0.workspace,
                                            SynthState.SynthParams0.ScoreWorkspaceROffset,
                                            nActualFrames);
                                    }
                                }
                                /* update effects, but only AFTER they have been applied, */
                                /* so that parameters come from the leading edge of an */
                                /* envelope period, rather than the trailing edge. */
                                TrackEffectIncrementDurationTimer(
                                    CurrentEffectHandle.SectionEffect,
                                    NumNoteDurationTicks);
                            }

                            /* grab the next section effect handle */
                            if (Scan != null)
                            {
                                CurrentEffectHandle = Scan.SectionEffectHandle;
                            }
                        }

                        Timing.QueryPerformanceCounter(out time2);
                        SynthState.phase1Time += time2 - time1;
                        #endregion
#if true // PARALLEL
                    }
                    else
                    {
                        // parallel method

                        if (effectiveCurrency > 1) // must not reset for scheduled skips since threads are not triggerd this cycle
                        {
                            SynthState.startingThreadCount = 0;
                            SynthState.completionThreadCount = 0;
                        }

                        SynthState.control.nActualFrames = nActualFrames;
                        SynthState.control.NumNoteDurationTicks = NumNoteDurationTicks;
                        SynthState.control.UpdateEnvelopes = UpdateEnvelopes;
                        SynthState.control.AreWeStillFastForwarding = AreWeStillFastForwarding;
                        SynthState.control.fScheduledSkip = fScheduledSkip;

                        for (int i = 1; i < SynthState.SynthParamsPerProc.Length; i++)
                        {
                            SynthState.SynthParamsPerProc[i].lElapsedTimeInEnvelopeTicks = SynthState.SynthParamsPerProc[0].lElapsedTimeInEnvelopeTicks;
                            SynthState.SynthParamsPerProc[i].dElapsedTimeInSeconds = SynthState.SynthParamsPerProc[0].dElapsedTimeInSeconds;
                            SynthState.SynthParamsPerProc[i].dCurrentBeatsPerMinute = SynthState.SynthParamsPerProc[0].dCurrentBeatsPerMinute;
                        }

                        // flags for tracking lazy-init/dirtying of workspaces
                        for (int p = 0; p < SynthState.SynthParamsPerProc.Length; p++)
                        {
                            SynthParamRec SynthParamsPOther = SynthState.SynthParamsPerProc[p];
                            for (int i = 0; i < SynthParamsPOther.SectionWorkspaceUsed.Length; i++)
                            {
                                SynthParamsPOther.SectionWorkspaceUsed[i] = false;
                            }
                        }

                        // Generate combined task array for threads. Constraints:
                        // - all inputs to a section must come before the section (hard requirement)
                        // - more expensive items should come earlier (without violating the first rule)
                        //
                        // TODO: This scheduling scheme may be good enough. If it turns out not to be, the next
                        // thing to try would be using "critical path length" criterion - sorting on the combined
                        // cost the track and it's effect, to get maximum critical path lengths up front.
                        // That could involve moving some long-running tracks across section entries in order
                        // to get them done earlier, when it matters.
                        // 
                        // 'sortify' SynthState.SectionArray on .lastCost
                        // The idea here is to gradually migrate more expensive sections to the beginning of the list
                        // The partial "bubble" sorting is meant to prevent large jumps in case of spurious timings.
                        // - nothing moves more than one slot per cycle
                        //
                        // Do not reorder processing in non-parallel case - improves stability of output by ensuring
                        // deterministic ordering of float accumulation (i.e. round-off error propagation).
                        if (effectiveCurrency > 1)
                        {
                            for (int i = unchecked((int)(SynthState.SynthParamsPerProc[0].lElapsedTimeInEnvelopeTicks & 1));
                                i + 1 < SynthState.SectionArrayAll.Length;
                                i += 2)
                            {
                                long lcost = SynthState.SectionArrayAll[i].lastCost;
                                long rcost = SynthState.SectionArrayAll[i + 1].lastCost;
                                if (lcost.CompareTo(rcost) < 0)
                                {
                                    SecEffRec temp = SynthState.SectionArrayAll[i];
                                    SynthState.SectionArrayAll[i] = SynthState.SectionArrayAll[i + 1];
                                    SynthState.SectionArrayAll[i + 1] = temp;
                                }
                            }
                            int o = 0;
                            for (int j = 0; j < SynthState.SectionArrayAll.Length; j++)
                            {
                                SecEffRec section = SynthState.SectionArrayAll[j];
                                // 'sortify' Section.inputTracks on .lastCost
                                // The idea is to migrate tracks so that the more expensive ones dispatched earlier.
                                // The partial "bubble" sorting is meant to prevent large jumps in case of spurious timings
                                // - nothing moves more than one slot per cycle
                                for (int i = unchecked((int)(SynthState.SynthParamsPerProc[0].lElapsedTimeInEnvelopeTicks & 1));
                                    i + 1 < section.inputTracks.Length;
                                    i += 2)
                                {
                                    long lcost = section.inputTracks[i].lastCost;
                                    long rcost = section.inputTracks[i + 1].lastCost;
                                    if (lcost.CompareTo(rcost) < 0)
                                    {
                                        PlayListNodeRec temp = section.inputTracks[i];
                                        section.inputTracks[i] = section.inputTracks[i + 1];
                                        section.inputTracks[i + 1] = temp;
                                    }
                                }
                                for (int i = 0; i < section.inputTracks.Length; i++)
                                {
                                    SynthState.CombinedPlayArray[o++] = section.inputTracks[i];
                                }
                                if (section != SynthState.DefaultSectionEffectSurrogate)
                                {
                                    SynthState.CombinedPlayArray[o++] = section;
                                }
                            }
                        }

                        // reset status variables
                        PlayListNodeRec Scan = SynthState.PlayTrackList;
                        while (Scan != null)
                        {
                            Scan.processed = 0;
                            Scan = Scan.Next;
                        }
                        for (int i = 0; i < SynthState.SectionArrayAll.Length; i++)
                        {
                            SynthState.SectionArrayAll[i].sectionInputCounter = 0;
                            SynthState.SectionArrayAll[i].processed = 0;
                        }

                        // Each processor initializes it's own accumulation buffers for all sections and
                        // increments the input counter for each one. The input target counts each processor's
                        // initialization as well as the actual inputs. This allows us to avoid having a barrier
                        // between initialization and processing since section effect processing won't be
                        // released until all initializations are done.

                        // release sync barrier for threads (only if really parallel)
#if DEBUG
                        Interlocked.Increment(ref SynthState.epoch);
#endif
                        if (effectiveCurrency > 1)
                        {
                            //SynthState.endBarrier.Reset();
                            //SynthState.startBarrierReleaseSpin = 0;
                            //SynthState.startBarrier.Set();
                            SynthState.startBarrierReleaseSpin = 1;
                        }

                        // synthesis on all threads including main
                        SynthGenerateOneCycleParallelPhase(
                            0,
                            SynthState);

                        // wait for all threads to finish
                        if (effectiveCurrency > 1)
                        {
                            SpinWaitOnThreadsCompletion(SynthState);
                            SynthState.startBarrier.Reset();
                            SynthState.endBarrier.Set();
                        }

                        // validate completion states
#if DEBUG
                        Scan = SynthState.PlayTrackList;
                        while (Scan != null)
                        {
                            Debug.Assert(Scan.processed == 1);
                            Scan = Scan.Next;
                        }
                        for (int i = 0; i < SynthState.SectionArrayExcludesDefault.Length; i++)
                        {
                            Debug.Assert(SynthState.SectionArrayExcludesDefault[i].processed == 1);
                            Debug.Assert(SynthState.SectionArrayExcludesDefault[i].sectionInputCounter
                                == SynthState.SectionArrayExcludesDefault[i].sectionInputTarget);
                        }
                        Debug.Assert(SynthState.DefaultSectionEffectSurrogate.processed == 0); // should not be done
                        Debug.Assert(SynthState.DefaultSectionEffectSurrogate.sectionInputCounter
                            == SynthState.DefaultSectionEffectSurrogate.sectionInputTarget); // should be ready
#endif

                        Timing.QueryPerformanceCounter(out time2);
                        SynthState.phase1Time += time2 - time1;
                        SynthState.DefaultSectionEffectSurrogate.traceInfo.start = time2;
                        SynthState.DefaultSectionEffectSurrogate.traceInfo.processor = 0;

                        // consolidate errors
                        if (SynthState.SynthParamsPerProc[0].result == SynthErrorCodes.eSynthDone)
                        {
                            for (int i = 1; i < SynthState.SynthParamsPerProc.Length; i++)
                            {
                                if (SynthState.SynthParamsPerProc[i].result != SynthErrorCodes.eSynthDone)
                                {
                                    SynthState.SynthParamsPerProc[0].result = SynthState.SynthParamsPerProc[i].result;
                                    SynthState.SynthParamsPerProc[0].ErrorInfo.CopyFrom(SynthState.SynthParamsPerProc[i].ErrorInfo);
                                }
                            }
                        }
                        if (SynthState.SynthParamsPerProc[0].result != SynthErrorCodes.eSynthDone)
                        {
                            Result = SynthState.SynthParamsPerProc[0].result;
                            goto Error;
                        }

                        // only main thread continues from here

                        // accumulate input to score effects to primary processor's buffer
                        bool used = false;
                        for (int p = 0; p < SynthState.SynthParamsPerProc.Length; p++)
                        {
                            if (!SynthState.SynthParamsPerProc[p].SectionWorkspaceUsed[
                                SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex])
                            {
                                continue; // skip workspaces from processors that never worked on inputs for this section
                            }
                            if (!used) // first one copies, subsequent ones accumulate
                            {
                                used = true;
                                FloatVectorCopy(
                                    SynthState.SynthParamsPerProc[p].workspace,
                                    SynthState.SynthParamsPerProc[p].SectionInputAccumulationWorkspaces[
                                        2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 0],
                                    SynthState.SynthParamsPerProc[0].workspace,
                                    SynthState.SynthParamsPerProc[0].ScoreWorkspaceLOffset,
                                    nActualFrames);
                                FloatVectorCopy(
                                    SynthState.SynthParamsPerProc[p].workspace,
                                    SynthState.SynthParamsPerProc[p].SectionInputAccumulationWorkspaces[
                                        2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 1],
                                    SynthState.SynthParamsPerProc[0].workspace,
                                    SynthState.SynthParamsPerProc[0].ScoreWorkspaceROffset,
                                    nActualFrames);
                                // early warning of uninitialized buffer use
                                Debug.Assert((nActualFrames == 0)
                                    || (!Single.IsNaN(SynthState.SynthParamsPerProc[0].workspace[
                                        SynthState.SynthParamsPerProc[0].ScoreWorkspaceLOffset])
                                    && !Single.IsNaN(SynthState.SynthParamsPerProc[0].workspace[
                                        SynthState.SynthParamsPerProc[0].ScoreWorkspaceROffset])));
                            }
                            else
                            {
                                FloatVectorAcc(
                                    SynthState.SynthParamsPerProc[p].workspace,
                                    SynthState.SynthParamsPerProc[p].SectionInputAccumulationWorkspaces[
                                        2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 0],
                                    SynthState.SynthParamsPerProc[0].workspace,
                                    SynthState.SynthParamsPerProc[0].ScoreWorkspaceLOffset,
                                    nActualFrames);
                                FloatVectorAcc(
                                    SynthState.SynthParamsPerProc[p].workspace,
                                    SynthState.SynthParamsPerProc[p].SectionInputAccumulationWorkspaces[
                                        2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 1],
                                    SynthState.SynthParamsPerProc[0].workspace,
                                    SynthState.SynthParamsPerProc[0].ScoreWorkspaceROffset,
                                    nActualFrames);
                                // early warning of uninitialized buffer use
                                Debug.Assert((nActualFrames == 0)
                                    || (!Single.IsNaN(SynthState.SynthParamsPerProc[0].workspace[
                                        SynthState.SynthParamsPerProc[0].ScoreWorkspaceLOffset])
                                    && !Single.IsNaN(SynthState.SynthParamsPerProc[0].workspace[
                                        SynthState.SynthParamsPerProc[0].ScoreWorkspaceROffset])));
                            }
                        }
                        Debug.Assert(used); // by definition, at least one input must have been prepared somewhere
                        // early warning of uninitialized buffer use
                        Debug.Assert((nActualFrames == 0)
                            || (!Single.IsNaN(SynthState.SynthParamsPerProc[0].workspace[
                                SynthState.SynthParamsPerProc[0].ScoreWorkspaceLOffset])
                            && !Single.IsNaN(SynthState.SynthParamsPerProc[0].workspace[
                                SynthState.SynthParamsPerProc[0].ScoreWorkspaceROffset])));

                        // fall through to common code path to continue with score effects processing

                        Debug.Assert(SynthState.SynthParams0 == SynthState.SynthParamsPerProc[0]);
                    }
#endif

                    /* apply score effects to score workspace */
                    if (UpdateEnvelopes)
                    {
                        /* if we are generating samples, then we should */
                        /* apply the score effects processor */
                        TrackEffectProcessQueuedCommands(
                            SynthState.ScoreEffectProcessor,
                            SynthState.SynthParams0);

                        if (!AreWeStillFastForwarding && !fScheduledSkip)
                        {
                            /* control-update cycle */
                            UpdateStateTrackEffectGenerator(
                                SynthState.ScoreEffectProcessor,
                                SynthState.SynthParams0);

                            /* generate wave */
                            Result = ApplyTrackEffectGenerator(
                                SynthState.ScoreEffectProcessor,
                                SynthState.SynthParams0.workspace,
                                nActualFrames,
                                SynthState.SynthParams0.ScoreWorkspaceLOffset,
                                SynthState.SynthParams0.ScoreWorkspaceROffset,
                                SynthState.SynthParams0);
                            if (Result != SynthErrorCodes.eSynthDone)
                            {
                                goto Error;
                            }
                            if ((SynthState.SynthParams0.level2TraceFlags & AutomationSettings.TraceFlags.Denormals) != 0)
                            {
                                FloatVectorCountDenormals(
                                    SynthState.SynthParams0.workspace,
                                    SynthState.SynthParams0.ScoreWorkspaceLOffset,
                                    nActualFrames,
                                    ref SynthState.DefaultSectionEffectSurrogate.denormalCount);
                                FloatVectorCountDenormals(
                                    SynthState.SynthParams0.workspace,
                                    SynthState.SynthParams0.ScoreWorkspaceROffset,
                                    nActualFrames,
                                    ref SynthState.DefaultSectionEffectSurrogate.denormalCount);
                            }

                            /* apply output volume */
                            FloatVectorScale(
                                SynthState.SynthParams0.workspace,
                                SynthState.SynthParams0.ScoreWorkspaceLOffset,
                                SynthState.SynthParams0.workspace,
                                SynthState.SynthParams0.ScoreWorkspaceLOffset,
                                nActualFrames,
                                SynthState.SynthParams0.fOverallVolumeScaling);
                            FloatVectorScale(
                                SynthState.SynthParams0.workspace,
                                SynthState.SynthParams0.ScoreWorkspaceROffset,
                                SynthState.SynthParams0.workspace,
                                SynthState.SynthParams0.ScoreWorkspaceROffset,
                                nActualFrames,
                                SynthState.SynthParams0.fOverallVolumeScaling);
                        }
                    }
                    /* update effects, but only AFTER they have been applied, */
                    /* so that parameters come from the leading edge of an */
                    /* envelope period, rather than the trailing edge. */
                    TrackEffectIncrementDurationTimer(
                        SynthState.ScoreEffectProcessor,
                        NumNoteDurationTicks);

                    /* keep track of what time it is */
                    SynthState.iScanningGapFrontInEnvelopeTicks += 1;

                    /* submit the data */
                    if (!AreWeStillFastForwarding)
                    {
                        /* only write wave data if we're playing for real */

                        /* detect end of initial silence suppression */
                        if (SynthState.fSuppressingInitialSilence)
                        {
                            // 1/(1<<24) would be the obvious threshhold, but that runs into the error of single
                            // precision, e.g. the fft for convolved reverb introduces leading noise. This threshhold
                            // is probably good for most purposes.
                            const float SilenceThreshhold = 1f / (1 << 19);

                            float maxMagnitude = 0;
                            FloatVectorReductionMaxMagnitude(
                                ref maxMagnitude,
                                SynthState.SynthParams0.workspace,
                                SynthState.SynthParams0.ScoreWorkspaceLOffset,
                                nActualFrames);
                            FloatVectorReductionMaxMagnitude(
                                ref maxMagnitude,
                                SynthState.SynthParams0.workspace,
                                SynthState.SynthParams0.ScoreWorkspaceROffset,
                                nActualFrames);
                            if (maxMagnitude >= SilenceThreshhold)
                            {
                                /* non-zero? turn it off */
                                SynthState.fSuppressingInitialSilence = false;

                                // compact leading audio in slice so first sample of output is significant
                                // (improves reproducibility and comparability of output)
                                int i;
                                for (i = 0; i < nActualFrames; i++)
                                {
                                    float m = Math.Max(
                                        Math.Abs(SynthState.SynthParams0.workspace[i + SynthState.SynthParams0.ScoreWorkspaceLOffset]),
                                        Math.Abs(SynthState.SynthParams0.workspace[i + SynthState.SynthParams0.ScoreWorkspaceROffset]));
                                    if (m >= SilenceThreshhold)
                                    {
                                        break;
                                    }
                                }
                                // truncate to start of oversampling frame group boundary - improves reproducibility of
                                // runs and comparability with legacy output files.
                                i = (i / SynthState.SynthParams0.iOversampling) * SynthState.SynthParams0.iOversampling;
                                nActualFrames -= i;
                                for (int j = 0; j < nActualFrames; j++)
                                {
                                    SynthState.SynthParams0.workspace[j + SynthState.SynthParams0.ScoreWorkspaceLOffset] =
                                        SynthState.SynthParams0.workspace[j + i + SynthState.SynthParams0.ScoreWorkspaceLOffset];
                                    SynthState.SynthParams0.workspace[j + SynthState.SynthParams0.ScoreWorkspaceROffset] =
                                        SynthState.SynthParams0.workspace[j + i + SynthState.SynthParams0.ScoreWorkspaceROffset];
                                }
                            }
                        }

                        /* only bother if we've seen some non-zero output */
                        if (!SynthState.fSuppressingInitialSilence && (nActualFrames != 0))
                        {
                            /* set flag so we return to caller.  this stops the loop */
                            nActualFramesOut = nActualFrames;
                            SynthState.lEnvelopeCyclesEmittingAudio++;
                            OkToTerminateIfNoData = true;
                        }
                    }

                    /* and finally update the tempo generator */
                    SynthState.SynthParams0.dCurrentBeatsPerMinute = TempoControlUpdate(
                        SynthState.TempoControl,
                        NumNoteDurationTicks);
                    SynthState.dDurationTicksPerEnvelopeClock = (
                        (SynthState.SynthParams0.dCurrentBeatsPerMinute / (4/*beats per whole note*/
                        * 60/*seconds per minute*/)) / SynthState.SynthParams0.dEnvelopeRate)
                        * DURATIONUPDATECLOCKRESOLUTION;
                }

                /* save this cycle's state so we can detect enter/leave next time through */
                SynthState.fLastCycleWasScheduledSkip = fScheduledSkip;

                SynthState.randomSeedProvider.Unfreeze();

                Timing.QueryPerformanceCounter(out SynthState.time3);
                SynthState.phase2Time += SynthState.time3 - time2;
                if (effectiveCurrency > 0)
                {
                    SynthState.DefaultSectionEffectSurrogate.traceInfo.end = SynthState.time3;
                }

                if (SynthState.traceScheduleWriter != null)
                {
                    PlayListNodeRec Scan;

                    bool level2ThisCycle = false;
                    if (SynthState.traceScheduleEnableLevel2)
                    {
                        for (int i = 0; !level2ThisCycle && (i < SynthState.SectionArrayAll.Length); i++)
                        {
                            if (((SynthState.SectionArrayAll[i].events != null)
                                    && (SynthState.SectionArrayAll[i].events.Count != 0))
                                || (SynthState.SectionArrayAll[i].denormalCount != 0))
                            {
                                level2ThisCycle = true;
                            }
                        }
                        Scan = SynthState.PlayTrackList;
                        while (!level2ThisCycle && (Scan != null))
                        {
                            if (((Scan.ThisTrack.events != null)
                                    && (Scan.ThisTrack.events.Count != 0))
                                || (Scan.denormalCount != 0))
                            {
                                level2ThisCycle = true;
                            }
                            Scan = Scan.Next;
                        }
                    }

                    if (level2ThisCycle)
                    {
                        SynthState.traceScheduleWriter.WriteLine("l2");
                    }
                    SynthState.traceScheduleWriter.WriteLine("t\t{0}", SynthState.SynthParams0.lElapsedTimeInEnvelopeTicks);
                    long basis = time0;
                    SynthState.traceScheduleWriter.WriteLine("b\t{0}", basis);
                    SynthState.traceScheduleWriter.WriteLine("fr\t{0}\t{1}", SynthState.lTotalFramesGenerated, nActualFrames);
                    SynthState.traceScheduleWriter.WriteLine("ph\t{0}\t{1}\t{2}\t{3}", time0 - basis, time1 - basis, time2 - basis, SynthState.time3 - basis);
                    SynthState.traceScheduleWriter.WriteLine(":");
                    for (int i = 0; i < SynthState.SectionArrayAll.Length; i++)
                    {
                        SynthState.traceScheduleWriter.WriteLine(
                            "{0}\t{1}\t{2}\t{3}",
                            SynthState.SectionArrayAll[i].traceInfo.id,
                            SynthState.SectionArrayAll[i].traceInfo.processor,
                            SynthState.SectionArrayAll[i].traceInfo.start - basis,
                            SynthState.SectionArrayAll[i].traceInfo.end - basis);
                        if (level2ThisCycle)
                        {
                            SynthState.traceScheduleWriter.WriteLine("\tdn\t{0}", SynthState.SectionArrayAll[i].denormalCount);
                            SynthState.SectionArrayAll[i].denormalCount = 0;

                            SynthState.traceScheduleWriter.WriteLine("\t{0}", SynthState.SectionArrayAll[i].events.Count);
                            for (int ii = 0; ii < SynthState.SectionArrayAll[i].events.Count; ii++)
                            {
                                EventTraceRec record = SynthState.SectionArrayAll[i].events[ii];
                                SynthState.traceScheduleWriter.WriteLine(
                                    record.frameIndex.HasValue ? "\t{0}\t{1}\t{2}\t{3}" : "\t{0}\t{1}",
                                    record.seq,
                                    record.evt.ToString(),
                                    record.frameIndex,
                                    record.noteIndex);
                            }
                            SynthState.SectionArrayAll[i].events.Clear();
                        }
                    }
                    Scan = SynthState.PlayTrackList;
                    while (Scan != null)
                    {
                        SynthState.traceScheduleWriter.WriteLine(
                            "{0}\t{1}\t{2}\t{3}",
                            Scan.traceInfo.id,
                            Scan.traceInfo.processor,
                            Scan.traceInfo.start - basis,
                            Scan.traceInfo.end - basis);
                        if (level2ThisCycle)
                        {
                            SynthState.traceScheduleWriter.WriteLine("\tdn\t{0}", Scan.denormalCount);
                            Scan.denormalCount = 0;

                            SynthState.traceScheduleWriter.WriteLine("\t{0}", Scan.ThisTrack.events.Count);
                            for (int ii = 0; ii < Scan.ThisTrack.events.Count; ii++)
                            {
                                EventTraceRec record = Scan.ThisTrack.events[ii];
                                SynthState.traceScheduleWriter.WriteLine(
                                    record.frameIndex.HasValue ? "\t{0}\t{1}\t{2}\t{3}" : "\t{0}\t{1}",
                                    record.seq,
                                    record.evt.ToString(),
                                    record.frameIndex,
                                    record.noteIndex);
                            }
                            Scan.ThisTrack.events.Clear();
                        }

                        Scan = Scan.Next;
                    }
                    SynthState.traceScheduleWriter.WriteLine();
                }


            Error:

#if false // TODO: if we can
		        (void)FloatingPointEnableDenormals(fOldDenormal); /* restore old mode */
#endif

                if (Result == SynthErrorCodes.eSynthDone)
                {
                    Result = OkToTerminateIfNoData ? SynthErrorCodes.eSynthDone : SynthErrorCodes.eSynthDoneNoData;
                }
                return Result;
            }

            private class ThreadContext
            {
                public readonly int processor;
                public readonly SynthStateRec SynthState;

                public ThreadContext(
                    int processor,
                    SynthStateRec SynthState)
                {
                    this.processor = processor;
                    this.SynthState = SynthState;
                }
            }

            private static SynthErrorCodes SynthGenerateOneCycleParallelPhaseTrack(
                int processor,
                SynthStateRec SynthState,
                int nActualFrames,
                PlayListNodeRec Scan,
                ref long start)
            {
                SynthParamRec SynthParamsP = SynthState.SynthParamsPerProc[processor];

                Scan.traceInfo.start = start;
                Scan.traceInfo.processor = processor;

                // identify target section
                SecEffRec CurrentEffectHandle;
                if (Scan.SectionEffectHandle != null)
                {
                    CurrentEffectHandle = Scan.SectionEffectHandle;
                }
                else
                {
                    CurrentEffectHandle = SynthState.DefaultSectionEffectSurrogate;
                }

                // lazy init section's input workspace
                if (!SynthParamsP.SectionWorkspaceUsed[CurrentEffectHandle.sectionInputAccumulatorIndex])
                {
                    SynthParamsP.SectionWorkspaceUsed[CurrentEffectHandle.sectionInputAccumulatorIndex] = true;

                    FloatVectorZero(
                        SynthParamsP.workspace,
                        SynthParamsP.SectionInputAccumulationWorkspaces[
                            2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0],
                        nActualFrames);
                    FloatVectorZero(
                        SynthParamsP.workspace,
                        SynthParamsP.SectionInputAccumulationWorkspaces[
                            2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1],
                        nActualFrames);
                }

                /* if track is active, then play it */
                if (Scan.IsActive)
                {
                    // phase 2 (parallelizable) of envelope update
                    SynthErrorCodes error = PlayTrackUpdateEnvelopes(
                        Scan.ThisTrack,
                        SynthState.control.UpdateEnvelopes/*scanning gap control*/,
                        SynthState.control.AreWeStillFastForwarding,
                        SynthParamsP);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }

                    if (!SynthState.control.AreWeStillFastForwarding && !SynthState.control.fScheduledSkip)
                    {
                        /* only generate wave if we're playing for real */
                        error = PlayTrackGenerateWave(
                            Scan.ThisTrack,
                            SynthState.control.UpdateEnvelopes/*scanning gap control*/,
                            SynthParamsP.workspace,
                            nActualFrames,
                            SynthParamsP.SectionInputAccumulationWorkspaces[
                                2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0], // left
                            SynthParamsP.SectionInputAccumulationWorkspaces[
                                2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1], // right
                            SynthParamsP.TrackWorkspaceLOffset,
                            SynthParamsP.TrackWorkspaceROffset,
                            SynthParamsP.OscillatorWorkspaceLOffset,
                            SynthParamsP.OscillatorWorkspaceROffset,
                            SynthParamsP.CombinedOscillatorWorkspaceLOffset,
                            SynthParamsP.CombinedOscillatorWorkspaceROffset,
                            SynthParamsP);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }

                        // early warning of uninitialized buffer use
                        Debug.Assert((nActualFrames == 0)
                            || (!Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0]])
                            && !Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1]])));

                        if ((SynthParamsP.level2TraceFlags & AutomationSettings.TraceFlags.Denormals) != 0)
                        {
                            FloatVectorCountDenormals(
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0], // left
                                nActualFrames,
                                ref Scan.denormalCount);
                            FloatVectorCountDenormals(
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1], // right
                                nActualFrames,
                                ref Scan.denormalCount);
                        }
                    }

                    PlayTrackFinish(
                        Scan.ThisTrack,
                        SynthState.control.UpdateEnvelopes/*scanning gap control*/,
                        SynthState.control.NumNoteDurationTicks,
                        SynthState.control.fScheduledSkip,
                        SynthParamsP);
                    if (!PlayTrackIsItStillActive(Scan.ThisTrack))
                    {
                        Scan.IsActive = false;
                    }
                }

                Interlocked.Increment(ref CurrentEffectHandle.sectionInputCounter);

                long end;
                Timing.QueryPerformanceCounterFast(out end);
                Scan.traceInfo.end = end;
                Scan.lastCost = end - start;
                start = end;

                return SynthErrorCodes.eSynthDone;
            }

            private static SynthErrorCodes SynthGenerateOneCycleParallelPhaseSection(
                int processor,
                SynthStateRec SynthState,
                int nActualFrames,
                SecEffRec CurrentEffectHandle,
                ref long start)
            {
                SynthParamRec SynthParamsP = SynthState.SynthParamsPerProc[processor];

                CurrentEffectHandle.traceInfo.start = start;
                CurrentEffectHandle.traceInfo.processor = processor;

                /* apply processor */
                if (SynthState.control.UpdateEnvelopes)
                {
                    // Accumulate processor results
                    // The old SectionWorkpace is not used. Instead, other processors input workspace are
                    // accumulated into the current processor's input workspace, to reduce by one workspace
                    // the load on the processor cache.
                    bool used = SynthParamsP.SectionWorkspaceUsed[
                        CurrentEffectHandle.sectionInputAccumulatorIndex]; // if used, our own buffer is accumulated by definition
                    for (int p = 0; p < SynthState.SynthParamsPerProc.Length; p++)
                    {
                        if (p == processor)
                        {
                            continue; // do not accumulate our own workspace
                        }
                        SynthParamRec SynthParamsPOther = SynthState.SynthParamsPerProc[p];
                        if (!SynthParamsPOther.SectionWorkspaceUsed[CurrentEffectHandle.sectionInputAccumulatorIndex])
                        {
                            continue; // skip workspaces from processors that never worked on inputs for this section
                        }
                        if (!used) // first one copies, subsequent ones accumulate
                        {
                            used = true;
                            FloatVectorCopy(
                                SynthParamsPOther.workspace,
                                SynthParamsPOther.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0],
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0],
                                nActualFrames);
                            FloatVectorCopy(
                                SynthParamsPOther.workspace,
                                SynthParamsPOther.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1],
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1],
                                nActualFrames);
                            // early warning of uninitialized buffer use
                            Debug.Assert((nActualFrames == 0)
                                || (!Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0]])
                                && !Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1]])));
                        }
                        else
                        {
                            FloatVectorAcc(
                                SynthParamsPOther.workspace,
                                SynthParamsPOther.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0],
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0],
                                nActualFrames);
                            FloatVectorAcc(
                                SynthParamsPOther.workspace,
                                SynthParamsPOther.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1],
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1],
                                nActualFrames);
                            // early warning of uninitialized buffer use
                            Debug.Assert((nActualFrames == 0)
                                || (!Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0]])
                                && !Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1]])));
                        }
                    }
                    Debug.Assert(used); // by definition, at least one input must have been prepared somewhere
                    // early warning of uninitialized buffer use
                    Debug.Assert((nActualFrames == 0)
                        || (!Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                            2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0]])
                        && !Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                            2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1]])));

                    /* if we are generating samples, then we should */
                    /* apply the score effects processor */
                    TrackEffectProcessQueuedCommands(
                        CurrentEffectHandle.SectionEffect,
                        SynthParamsP);

                    if (!SynthState.control.AreWeStillFastForwarding)
                    {
                        /* control-update cycle */
                        UpdateStateTrackEffectGenerator(
                            CurrentEffectHandle.SectionEffect,
                            SynthParamsP);

                        /* generate wave */
                        SynthErrorCodes error = ApplyTrackEffectGenerator(
                            CurrentEffectHandle.SectionEffect,
                            SynthParamsP.workspace,
                            nActualFrames,
                            SynthParamsP.SectionInputAccumulationWorkspaces[
                                2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0], // left
                            SynthParamsP.SectionInputAccumulationWorkspaces[
                                2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1], // right
                            SynthParamsP);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                        if ((SynthParamsP.level2TraceFlags & AutomationSettings.TraceFlags.Denormals) != 0)
                        {
                            FloatVectorCountDenormals(
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0], // left
                                nActualFrames,
                                ref CurrentEffectHandle.denormalCount);
                            FloatVectorCountDenormals(
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1], // right
                                nActualFrames,
                                ref CurrentEffectHandle.denormalCount);
                        }
                        // lazy init default section [score effect] input workspace
                        if (!SynthParamsP.SectionWorkspaceUsed[
                            SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex])
                        {
                            SynthParamsP.SectionWorkspaceUsed[
                                SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex] = true;
                            FloatVectorCopy(
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0],
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 0],
                                nActualFrames);
                            FloatVectorCopy(
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1],
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 1],
                                nActualFrames);
                        }
                        else
                        {
                            FloatVectorAcc(
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 0],
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 0],
                                nActualFrames);
                            FloatVectorAcc(
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * CurrentEffectHandle.sectionInputAccumulatorIndex + 1],
                                SynthParamsP.workspace,
                                SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 1],
                                nActualFrames);
                        }

                        // early warning of uninitialized buffer use
                        Debug.Assert((nActualFrames == 0)
                            || (!Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 0]])
                            && !Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                2 * SynthState.DefaultSectionEffectSurrogate.sectionInputAccumulatorIndex + 1]])));
                    }
                }
                /* update effects, but only AFTER they have been applied, */
                /* so that parameters come from the leading edge of an */
                /* envelope period, rather than the trailing edge. */
                TrackEffectIncrementDurationTimer(
                    CurrentEffectHandle.SectionEffect,
                    SynthState.control.NumNoteDurationTicks);

                // Each completed section increments the score effects count. When all inputs are accumulated
                // (incl9uding the per-proc workspace initializations) the score effect surrogate is signalled.
                // One thread will pick that up and signal sectionDone, allowing all threads to proceed to
                // barrier (and main thread to do score effect processing and output)
                Interlocked.Increment(ref SynthState.DefaultSectionEffectSurrogate.sectionInputCounter);

                long end;
                Timing.QueryPerformanceCounterFast(out end);
                CurrentEffectHandle.traceInfo.end = end;
                CurrentEffectHandle.lastCost = end - start;
                start = end;

                return SynthErrorCodes.eSynthDone;
            }

            private static SynthErrorCodes SynthGenerateOneCycleParallelPhase(
                int processor,
                SynthStateRec SynthState)
            {
                SynthParamRec SynthParamsP = SynthState.SynthParamsPerProc[processor];
                int nActualFrames = SynthState.control.nActualFrames;

#if DEBUG
                for (int i = 0; i < SynthParamsP.workspace.Length; i++)
                {
                    SynthParamsP.workspace[i] = Single.NaN;
                }
                for (int i = 0; i < SynthParamsP.SectionWorkspaceUsed.Length; i++)
                {
                    Debug.Assert(!SynthParamsP.SectionWorkspaceUsed[i]);
                }
#endif

                // process objects

                long start;
                Timing.QueryPerformanceCounterFast(out start);
                for (int i = processor; i < SynthState.CombinedPlayArray.Length; i++)
                {
                    object o = SynthState.CombinedPlayArray[i];
                    Debug.Assert((o is PlayListNodeRec) || (o is SecEffRec));

                    if (o is PlayListNodeRec)
                    {
                        PlayListNodeRec Scan = (PlayListNodeRec)o;

                        if (Interlocked.CompareExchange(ref Scan.processed, 1, 0) == 0)
                        {
                            SynthErrorCodes result = SynthGenerateOneCycleParallelPhaseTrack(
                                processor,
                                SynthState,
                                nActualFrames,
                                Scan,
                                ref start);
                            if (result != SynthErrorCodes.eSynthDone)
                            {
                                return result;
                            }
                        }
                    }
                    else
                    {
                        SecEffRec CurrentEffectHandle = (SecEffRec)o;
                        Debug.Assert(CurrentEffectHandle != SynthState.DefaultSectionEffectSurrogate);

                        // if this test fails, it is guarranteed that another thread will come here and succeed later.
                        if (CurrentEffectHandle.sectionInputCounter == CurrentEffectHandle.sectionInputTarget)
                        {
                            if (Interlocked.CompareExchange(ref CurrentEffectHandle.processed, 1, 0) == 0)
                            {
                                SynthGenerateOneCycleParallelPhaseSection(
                                    processor,
                                    SynthState,
                                    nActualFrames,
                                    CurrentEffectHandle,
                                    ref start);
                            }
                        }
                    }
                }

#if DEBUG
                for (int ii = 0; ii < SynthState.SectionArrayAll.Length; ii++)
                {
                    SecEffRec section = SynthState.SectionArrayAll[ii];
                    if (SynthParamsP.SectionWorkspaceUsed[section.sectionInputAccumulatorIndex])
                    {
                        // early warning of uninitialized buffer use
                        Debug.Assert((nActualFrames == 0)
                            || (!Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * section.sectionInputAccumulatorIndex + 0]])
                            && !Single.IsNaN(SynthParamsP.workspace[SynthParamsP.SectionInputAccumulationWorkspaces[
                                    2 * section.sectionInputAccumulatorIndex + 1]])));
                    }
                }
#endif

                return SynthErrorCodes.eSynthDone;
            }

            private static void AuxThreadLoop(
                int processor,
                SynthStateRec SynthState)
            {
                Debug.Assert(processor > 0);

                bool error = false;
#if DEBUG
                long lastEpoch = -1;
#endif
                while (true)
                {
#pragma warning disable 420 // ref volatile int loses the volatile, but Interlocked.Increment does the right thing, so suppress warning
                    Interlocked.Increment(ref SynthState.startingThreadCount);
#pragma warning restore 420
                    // loop required to allow thread.Suspend() to be used for killing run-away pcode evals
                    while (!SynthState.startBarrier.WaitOne(500))
                    {
                    }
                    SpinWaitOnStartBarrierReleased(SynthState);
#if DEBUG
                    long epoch = Interlocked.Read(ref SynthState.epoch);
                    Debug.Assert(lastEpoch != epoch);
                    lastEpoch = epoch;
#endif

                    if (SynthState.exit != 0)
                    {
                        break;
                    }

                    try
                    {
                        Debug.Assert(!error); // should not be permitted to do another cycle after error occurs
                        SynthState.SynthParamsPerProc[processor].result = SynthGenerateOneCycleParallelPhase(
                            processor,
                            SynthState);
                        if (SynthState.SynthParamsPerProc[processor].result != SynthErrorCodes.eSynthDone)
                        {
                            error = true;
                        }
                    }
                    catch (Exception exception)
                    {
                        // the presence of an error will terminate the primary thread's synthesis loop
                        SynthState.SynthParamsPerProc[processor].result = SynthErrorCodes.eSynthErrorEx;
                        SynthState.SynthParamsPerProc[processor].ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExExceptionOccurred;
                        SynthState.SynthParamsPerProc[processor].ErrorInfo.exception = exception;
                    }
                    finally
                    {
                        // allow threads to return to barrier (in event of completion or error)
#if DEBUG
                        Debug.Assert(lastEpoch == Interlocked.Read(ref SynthState.epoch));
#endif
#pragma warning disable 420 // ref volatile int loses the volatile, but Interlocked.Increment does the right thing, so suppress warning
                        Interlocked.Increment(ref SynthState.completionThreadCount); // signal completion of cycle to main thread
#pragma warning restore 420
                    }

                    // loop required to allow thread.Suspend() to be used for killing run-away pcode evals
                    while (!SynthState.endBarrier.WaitOne(1000))
                    {
                    }
                }
            }

            private static void ThreadMain(object o)
            {
                ThreadContext context = (ThreadContext)o;

                int processor = context.processor;
                Debug.Assert(processor > 0);
                SynthStateRec SynthState = context.SynthState;

                Thread.BeginThreadAffinity(); // tie to OS thread
                AuxThreadLoop(processor, SynthState);
                Thread.EndThreadAffinity();
            }
        }

        public delegate SynthErrorCodes DataOutCallbackMethod<V>(
            V refcon,
            float[] addr,
            int offset,
            int nActualFrames);

        /* main loop helper method for synthesizer, pulled out to get profile info */
        private static SynthErrorCodes SynthesizerMainLoop<V>(
            SynthStateRec SynthState,
            DataOutCallbackMethod<V> DataOutCallback,
            V DataOutRefcon,
            IStoppedTask Stopped,
            out SynthErrorInfoRec ErrorInfoOut)
        {
            ErrorInfoOut = null;

            while (true)
            {
                /* generate some data */
                int nActualFrames;
                // Results are posted to ScoreWorkspace
                SynthErrorCodes Result = SynthStateRec.SynthGenerateOneCycle(
                    SynthState,
                    out nActualFrames);
                if (((Result != SynthErrorCodes.eSynthDone) && (Result != SynthErrorCodes.eSynthDoneNoData))
                    || ((Result == SynthErrorCodes.eSynthDone) && (nActualFrames == 0)))
                {
                    ErrorInfoOut = SynthState.SynthParams0.ErrorInfo;
                    return Result;
                }

                SynthState.lTotalFramesGenerated += nActualFrames;

                /* transpose to interleaved format - use SectionWorkspace as staging area */
                FloatVectorMakeInterleaved(
                    SynthState.SynthParams0.workspace,
                    SynthState.SynthParams0.ScoreWorkspaceLOffset,
                    SynthState.SynthParams0.workspace,
                    SynthState.SynthParams0.ScoreWorkspaceROffset,
                    nActualFrames,
                    SynthState.SynthParams0.workspace,
                    SynthState.SynthParams0.SectionWorkspaceLOffset);

                /* attempt to send data to device */
                Result = DataOutCallback(
                    DataOutRefcon,
                    SynthState.SynthParams0.workspace,
                    SynthState.SynthParams0.SectionWorkspaceLOffset,
                    nActualFrames);

                if (Result != SynthErrorCodes.eSynthDone)
                {
                    return Result;
                }

                // cancelling comes from UI thread via this object
                if (Stopped.Stopped)
                {
                    return SynthErrorCodes.eSynthUserCancelled;
                }
            }
        }

        /* This routine does all of the work. */
        /* The DataOutCallback is called every time a block of data is */
        /* ready to be sent to the target device; this is provided so that data can be */
        /* redirected to a file or postprocessed in some way before playback. */
        /* the KeyTrack and FrameToStartAt provide a reference point indicating where */
        /* playback should occur.  if KeyTrack is null, then playback begins at the beginning. */
        /* the rate parameters are in operations per second. */
        public static SynthErrorCodes DoSynthesizer<V>(
            Document Document,
            DataOutCallbackMethod<V> DataOutCallback,
            V DataOutRefcon,
            List<TrackObjectRec> ListOfTracks,
            TrackObjectRec KeyTrack,
            int FrameToStartAt,
            int SamplingRate,
            int Oversampling,
            int EnvelopeRate,
            LargeBCDType DefaultBeatsPerMinute,
            double OverallVolumeScalingReciprocal,
            LargeBCDType ScanningGap,
            IStoppedTask Stopper,
            bool WriteSummary,
            out SynthErrorInfoRec ErrorInfoOut,
            TextWriter InteractionLog,
            bool deterministic,// now ignored - control by setting randomSeed to null or int
            int? randomSeed,
            AutomationSettings automationSettings)
        {
            SynthErrorCodes Error;
            DateTime StartTime;
            DateTime EndTime;
            SynthStateRec SynthState;


            ErrorInfoOut = null;

            if (automationSettings == null)
            {
                automationSettings = new AutomationSettings();
            }

            // TODO: determine if ensuring a full GC (GC.Collect()) has occurred will help. Most likely, we should just
            // let the GC do what it will, but if full collections are occurring and causing problems, triggering and
            // finishing one here might reduce the occurrence of it.

            float gc2Initial = 0, gc2Final = 0;
            float gc1Initial = 0, gc1Final = 0;
            float gc0Initial = 0, gc0Final = 0;
            float allocRate = 0;

            string perfCounterFail = null;
            using (PerformanceCounter perfGC2 = automationSettings.PerfCounters ? CreatePerformanceCounter(".NET CLR Memory", "# Gen 2 Collections", ref perfCounterFail) : null)
            {
                using (PerformanceCounter perfGC1 = automationSettings.PerfCounters ? CreatePerformanceCounter(".NET CLR Memory", "# Gen 1 Collections", ref perfCounterFail) : null)
                {
                    using (PerformanceCounter perfGC0 = automationSettings.PerfCounters ? CreatePerformanceCounter(".NET CLR Memory", "# Gen 0 Collections", ref perfCounterFail) : null)
                    {
                        using (PerformanceCounter perfAllocRate = automationSettings.PerfCounters ? CreatePerformanceCounter(".NET CLR Memory", "Allocated Bytes/sec", ref perfCounterFail) : null)
                        {
                            if (automationSettings.PerfCounters && (perfCounterFail == null))
                            {
                                try
                                {
                                    gc2Initial = perfGC2.NextValue();
                                    gc1Initial = perfGC1.NextValue();
                                    gc0Initial = perfGC0.NextValue();
                                    perfAllocRate.NextValue();
                                }
                                catch (Exception exception)
                                {
                                    perfCounterFail = exception.ToString();
                                }
                            }


                            /* construct the synthesizer */
                            {
                                SynthErrorInfoRec InitErrorInfo;
                                Error = SynthStateRec.InitializeSynthesizer(
                                    out SynthState,
                                    Document,
                                    ListOfTracks,
                                    KeyTrack,
                                    FrameToStartAt,
                                    SamplingRate,
                                    Oversampling,
                                    EnvelopeRate,
                                    DefaultBeatsPerMinute,
                                    OverallVolumeScalingReciprocal,
                                    ScanningGap,
                                    out InitErrorInfo,
                                    InteractionLog,
                                    deterministic,// now ignored - control by setting randomSeed to null or int
                                    randomSeed,
                                    automationSettings);
                                if (Error != SynthErrorCodes.eSynthDone)
                                {
                                    if (SynthState != null)
                                    {
                                        SynthStateRec.FinalizeSynthesizer(
                                            SynthState,
                                            false/*writeOutputLogs*/);
                                    }
                                    ErrorInfoOut = InitErrorInfo;
                                    return Error;
                                }
                            }


                            /* enter main play loop */

                            // TODO: when moving to .NET 4 or 4.5, enable low latency mode to reduce impact
                            // of garbage collections.
                            // See: http://blogs.msdn.com/b/dotnet/archive/2012/07/20/the-net-framework-4-5-includes-new-garbage-collector-enhancements-for-client-and-server-apps.aspx

                            // TODO: when implementing real-time synthesis, boost thread priority (not as simple as
                            // it sounds); see this:
                            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/73860618-21cb-459f-af6d-6ecb77c9c5f1/latency-for-realtime-audio-in-clr?forum=clr
                            // In case it disappears, basically:
                            // System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency; -- anything better on later versions?
                            // System.Threading.Thread.BeginThreadAffinity()
                            // HANDLE = hThread = GetCurrentThread();
                            // int priority = ::GetThreadPriority(hThread); -- save for restore later
                            // ::SetThreadPriority(hThread,THREAD_PRIORITY_TIME_CRITICAL); -- "Native highest priority is higher than .net highest priority."
                            // DwmEnableMMCSS(TRUE); -- Prevent Dwm from pre-empting audio thread.
                            // System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime; -- boost the whole process.
                            // DWORD taskIndex = 0;
                            // HANDLE hAvTask = 0;
                            // hAvTask = AvSetMmThreadCharacteristics(TEXT("Pro Audio"), &taskIndex); -- Ask MMCSS to boost the thread priority
                            // ... start and service audio client ...
                            // revert with
                            // if (hAvTask != 0) { ::AvRevertMmThreadCharacteristics(hAvTask); }
                            // System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
                            // System.Runtime.GCSettings.LatencyMode = defaultLatencyMode;
                            // ::SetThreadPriority(hThread,THREAD_PRIORITY_NORMAL);
                            // System.Threading.Thread.EndThreadAffinity();

                            {
                                SynthErrorInfoRec ErrorInfo;

                                // this ensures that pcode evals that get stuck in infinite loops can be cancelled.
                                EventHandler onStopHandler = new EventHandler(delegate(object sender, EventArgs e)
                                {
#if true // TODO: eventually hope to remove legacy loop
                                    if (SynthState.SynthParamsPerProc != null)
                                    {
#endif
                                        for (int i = 0; i < SynthState.SynthParamsPerProc.Length; i++)
                                        {
                                            PcodeSystem.EvaluatePcodeThread.SafeCancel(
                                                SynthState.threads[i],
                                                ref SynthState.SynthParamsPerProc[i].pcodeThreadContext);
                                        }
#if true // TODO: eventually hope to remove legacy loop
                                    }
                                    else
                                    {
                                        // TODO: in the legacy mode, we don't have access to the thread object
                                        SynthState.SynthParams0.pcodeThreadContext.GlobalCancelPending = 1;
                                    }
#endif
                                });
                                Stopper.OnStop += onStopHandler; // this will be executed on UI thread
                                try
                                {

                                    Thread.BeginThreadAffinity(); // tie to OS thread
                                    StartTime = DateTime.UtcNow;
                                    Error = SynthesizerMainLoop(
                                        SynthState,
                                        DataOutCallback,
                                        DataOutRefcon,
                                        Stopper,
                                        out ErrorInfo);
                                    EndTime = DateTime.UtcNow;
                                    Thread.EndThreadAffinity();

                                }
                                finally
                                {
                                    Stopper.OnStop -= onStopHandler;
                                }

                                if (Error != SynthErrorCodes.eSynthDone)
                                {
                                    SynthStateRec.FinalizeSynthesizer(
                                        SynthState,
                                        false/*writeOutputLogs*/);
                                    ErrorInfoOut = ErrorInfo;
                                    return Error;
                                }
                            }


                            if (automationSettings.PerfCounters && (perfCounterFail == null))
                            {
                                try
                                {
                                    gc2Final = perfGC2.NextValue();
                                    gc1Final = perfGC1.NextValue();
                                    gc0Final = perfGC0.NextValue();
                                    allocRate = perfAllocRate.NextValue();
                                }
                                catch (Exception exception)
                                {
                                    perfCounterFail = exception.ToString();
                                }
                            }
                        }
                    }
                }
            }


            /* output any analyzer text */
            SynthStateRec.FinalizeSynthesizer(
                SynthState,
                true/*writeOutputLogs*/);

            /* output separator to interaction window (if anything else was logged */
            /* or window is already open) */
            {
                StringWriter summaryWriter = new StringWriter();

                summaryWriter.WriteLine();
                summaryWriter.WriteLine("---------------------------------- end of run {0}----------------------------------", SynthState.summaryTag != null ? String.Concat("\"", SynthState.summaryTag, "\" ") : null);

                summaryWriter.WriteLine("  initial random seed:    {0}", SynthState.initialSeed);
                summaryWriter.WriteLine("  concurrency:            {0}", SynthState.concurrency);

                if (perfCounterFail == null)
                {
                    if (automationSettings.PerfCounters)
                    {
                        summaryWriter.WriteLine("  gc0:                    {0:0}", gc0Final - gc0Initial);
                        summaryWriter.WriteLine("  gc1:                    {0:0}", gc1Final - gc1Initial);
                        summaryWriter.WriteLine("  gc2:                    {0:0}", gc2Final - gc2Initial);
                        summaryWriter.WriteLine("  allocations:            {0:0} bytes/wall-sec, {1:0} bytes/audio-sec, {2:0} bytes total",
                            allocRate,
                            allocRate * (EndTime - StartTime).TotalSeconds * EnvelopeRate / SynthState.lEnvelopeCyclesEmittingAudio,
                            allocRate * (EndTime - StartTime).TotalSeconds);
                    }
                }
                else
                {
                    summaryWriter.WriteLine("  opening performance counters failed:");
                    summaryWriter.WriteLine(perfCounterFail);
                }

                summaryWriter.WriteLine("  elapsed real time:      {0:0.00} sec", (EndTime - StartTime).TotalSeconds);

                long freq;
                Timing.QueryPerformanceFrequency(out freq);
                summaryWriter.WriteLine("  1. leading seq. time:   {0:0.00} sec", (double)SynthState.phase0Time / freq);
                summaryWriter.WriteLine("  2. concurrent time:     {0:0.00} sec", (double)SynthState.phase1Time / freq);
                summaryWriter.WriteLine("  3. trailing seq. time:  {0:0.00} sec - includes score effects", (double)SynthState.phase2Time / freq);
                summaryWriter.WriteLine("  4. external time:       {0:0.00} sec", (double)SynthState.phase3Time / freq);

                summaryWriter.WriteLine();
                summaryWriter.WriteLine();

                Program.WriteLog("summary", summaryWriter.ToString());

                if (WriteSummary || SynthState.SynthParams0.InteractionLogAccessed)
                {
                    SynthState.SynthParams0.InteractionLog.Write(summaryWriter.ToString());
                }
            }


            return SynthErrorCodes.eSynthDone;
        }

        private static string processInstanceName;
        private static PerformanceCounter CreatePerformanceCounter(string category, string counter, ref string failure)
        {
            if (failure != null)
            {
                return null;
            }
            try
            {
                if (processInstanceName == null)
                {
                    processInstanceName = GetProcessInstanceName(Process.GetCurrentProcess().Id);
                }
                return new PerformanceCounter(category, counter, processInstanceName);
            }
            catch (Exception exception)
            {
                failure = exception.ToString();
                return null;
            }
        }

        // from http://weblogs.thinktecture.com/ingo/2004/06/getting-the-current-process-your-own-cpu-usage.html
        private static string GetProcessInstanceName(int pid)
        {
            PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");

            string[] instances = cat.GetInstanceNames();
            foreach (string instance in instances)
            {
                using (PerformanceCounter cnt = new PerformanceCounter("Process", "ID Process", instance, true))
                {
                    int val = (int)cnt.RawValue;
                    if (val == pid)
                    {
                        return instance;
                    }
                }
            }
            throw new Exception("Could not find performance counter instance name for current process. This is truly strange ...");
        }

        private struct SERec
        {
            public SynthErrorSubCodes ErrorEx;
            public string Message;

            public SERec(SynthErrorSubCodes ErrorEx, string Message)
            {
                this.ErrorEx = ErrorEx;
                this.Message = Message;
            }
        }

        private const string StrSequenceSpecifiedMultipleTimes = "Sequence name is specified multiple times.";
        private const string StrSequenceHasNoDuration = "At least one sequence has no duration.";
        private const string StrUndefinedInstrument = "A track uses an undefined instrument.";
        private const string StrUndefinedWaveTable = "Undefined wave table referenced.";
        private const string StrUndefinedSample = "Undefined sample referenced.";
        private const string StrUndefinedFunction = "Undefined function referenced.";
        private const string StrTypeMismatchFunction = "Actual function type does not match expected type.";
        private const string StrPossibleInfiniteSequenceLoop = "The track appeared to have an infinite loop in sequencing.";
        private const string StrLaterTrackIssusedCommandToEarlierTrack = "One track issued a command to another track that is ealier in execution order.";
        private const string StrTooManyNestedSkipCommands = "Track is involved in too many nested skip commands.";
        private const string StrDontUseAsteriskAsTrackOrGroupName = "A track or group name can't be '*'.";
        private const string StrSomeSamplesHaveSameName = "Some samples or algorithmic samples have the same name.";
        private const string StrSomeWaveTablesHaveSameName = "Some wave tables or algorithmic wave tables have the same name.";
        private const string StrSomeInstrumentsHaveSameName = "Some instruments have the same name.";
        private const string StrUndefinedTrackInGroupTable = "An undefined track is specified as a member of a group.";
        private const string StrConvolverBadSamplingRate = "The sample's rate is not compatible with the output sampling rate.";
        private const string StrConvolverBadNumChannels = "The sample does not have exactly one channel.";
        private const string StrConvolverExplicitLatencyNotAvailable = "The explicit latency convolver effect is not available in this build.";
        private const string StrExceptionOccurred = "An exception occurred during processing.";
        private const string StrUserParamFunctionEvalError = "An error ocurred evaluating user-provided function in instrument parameter.";
        private const string StrUserEffectFunctionEvalError = "An error ocurred evaluating function in user effect.";

        private static readonly SERec[] ErrorMessages = new SERec[]
        {
            new SERec(SynthErrorSubCodes.eSynthErrorExSequenceSpecifiedMultipleTimes, StrSequenceSpecifiedMultipleTimes),
            new SERec(SynthErrorSubCodes.eSynthErrorExSequenceHasNoDuration, StrSequenceHasNoDuration),
            new SERec(SynthErrorSubCodes.eSynthErrorExUndefinedInstrument, StrUndefinedInstrument),
            new SERec(SynthErrorSubCodes.eSynthErrorExUndefinedWaveTable, StrUndefinedWaveTable),
            new SERec(SynthErrorSubCodes.eSynthErrorExUndefinedSample, StrUndefinedSample),
            new SERec(SynthErrorSubCodes.eSynthErrorExUndefinedFunction, StrUndefinedFunction),
            new SERec(SynthErrorSubCodes.eSynthErrorExTypeMismatchFunction, StrTypeMismatchFunction),
            new SERec(SynthErrorSubCodes.eSynthErrorExPossibleInfiniteSequenceLoop, StrPossibleInfiniteSequenceLoop),
            new SERec(SynthErrorSubCodes.eSynthErrorExLaterTrackIssusedCommandToEarlierTrack, StrLaterTrackIssusedCommandToEarlierTrack),
            new SERec(SynthErrorSubCodes.eSynthErrorExTooManyNestedSkipCommands, StrTooManyNestedSkipCommands),
            new SERec(SynthErrorSubCodes.eSynthErrorExDontUseAsteriskAsTrackOrGroupName, StrDontUseAsteriskAsTrackOrGroupName),
            new SERec(SynthErrorSubCodes.eSynthErrorExSomeSamplesHaveSameName, StrSomeSamplesHaveSameName),
            new SERec(SynthErrorSubCodes.eSynthErrorExSomeWaveTablesHaveSameName, StrSomeWaveTablesHaveSameName),
            new SERec(SynthErrorSubCodes.eSynthErrorExSomeInstrumentsHaveSameName, StrSomeInstrumentsHaveSameName),
            new SERec(SynthErrorSubCodes.eSynthErrorExUndefinedTrackInGroupTable, StrUndefinedTrackInGroupTable),
            new SERec(SynthErrorSubCodes.eSynthErrorExConvolverBadSamplingRate, StrConvolverBadSamplingRate),
            new SERec(SynthErrorSubCodes.eSynthErrorExConvolverBadNumChannels, StrConvolverBadNumChannels),
            new SERec(SynthErrorSubCodes.eSynthErrorExConvolverExplicitLatencyNotAvailable, StrConvolverExplicitLatencyNotAvailable),
            new SERec(SynthErrorSubCodes.eSynthErrorExExceptionOccurred, StrExceptionOccurred),
            new SERec(SynthErrorSubCodes.eSynthErrorExUserParamFunctionEvalError, StrUserParamFunctionEvalError),
            new SERec(SynthErrorSubCodes.eSynthErrorExUserEffectFunctionEvalError, StrUserEffectFunctionEvalError),
        };

        /* display synth error */
        public static string GetErrorMessage(
            SynthErrorCodes Error,
            SynthErrorInfoRec ErrorEx)
        {
#if DEBUG
            for (int i = 0; i < ErrorMessages.Length; i += 1)
            {
                if (ErrorMessages[i].ErrorEx != i + SynthErrorSubCodes.eSynthErrorEx_Start)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
            }
#endif

            switch (Error)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();

                case SynthErrorCodes.eSynthDone:
                case SynthErrorCodes.eSynthDoneNoData:
                case SynthErrorCodes.eSynthUserCancelled:
                    /* not an error */
                    return null;

                case SynthErrorCodes.eSynthPrereqError:
                case SynthErrorCodes.eSynthDataSubmitError:
                    /* subcomponent is responsible for displaying these */
                    return null;

                case SynthErrorCodes.eSynthErrorEx:
                    Debug.Assert(ErrorEx != null);

                    StringBuilder Message = new StringBuilder();

                    if (ErrorEx.ErrorEx != SynthErrorSubCodes.eSynthErrorExCustom)
                    {
#if DEBUG
                        if ((ErrorEx.ErrorEx - SynthErrorSubCodes.eSynthErrorEx_Start < 0)
                            || (ErrorEx.ErrorEx - SynthErrorSubCodes.eSynthErrorEx_Start >= SynthErrorSubCodes.eSynthErrorEx_End - SynthErrorSubCodes.eSynthErrorEx_Start)
                            || (ErrorEx.ErrorEx - SynthErrorSubCodes.eSynthErrorEx_Start >= ErrorMessages.Length))
                        {
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }
#endif
                        Message.Append(ErrorMessages[ErrorEx.ErrorEx - SynthErrorSubCodes.eSynthErrorEx_Start].Message);
                    }
                    else
                    {
                        Message.Append(ErrorEx.CustomError);
                    }

                    if (ErrorEx.TrackName != null)
                    {
                        Message.AppendFormat("  Track '{0}'.", ErrorEx.TrackName);
                    }
                    if (ErrorEx.SequenceName != null)
                    {
                        Message.AppendFormat("  Sequence '{0}'.", ErrorEx.SequenceName);
                    }
                    if (ErrorEx.InstrumentName != null)
                    {
                        Message.AppendFormat("  Instrument '{0}'.", ErrorEx.InstrumentName);
                    }
                    if (ErrorEx.SectionName != null)
                    {
                        Message.AppendFormat("  Section '{0}'.", ErrorEx.SectionName);
                    }
                    if (ErrorEx.WaveTableName != null)
                    {
                        Message.AppendFormat("  Wave table '{0}'.", ErrorEx.WaveTableName);
                    }
                    if (ErrorEx.SampleName != null)
                    {
                        Message.AppendFormat("  Sample '{0}'.", ErrorEx.SampleName);
                    }
                    if (ErrorEx.FunctionName != null)
                    {
                        Message.AppendFormat("  Function '{0}'.", ErrorEx.FunctionName);
                    }
                    if (ErrorEx.IssuingTrackName != null)
                    {
                        Message.AppendFormat("  Issuing Track '{0}'.", ErrorEx.IssuingTrackName);
                    }
                    if (ErrorEx.ReceivingTrackName != null)
                    {
                        Message.AppendFormat("  Receiving Track '{0}'.", ErrorEx.ReceivingTrackName);
                    }
                    if (ErrorEx.exception != null)
                    {
                        Message.AppendFormat("  Exception: {0}", ErrorEx.exception);
                    }
                    if (ErrorEx.userEvalErrorCode != EvalErrors.eEvalNoError)
                    {
                        Message.AppendFormat(
                            "  Function Evaluation Error: {0}",
                            PcodeSystem.GetPcodeErrorMessage(ErrorEx.userEvalErrorCode));
                    }

                    return Message.ToString();
            }
        }
    }
}
