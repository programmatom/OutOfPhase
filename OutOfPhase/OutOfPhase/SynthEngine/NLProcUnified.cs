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
        /* variant stuff for oscillator processor */
        public class NLOscRec
        {
            /* control parameters */
            public EvalEnvelopeRec InputScalingEnvelope;
            public LFOGenRec InputScalingLFO;
            public EvalEnvelopeRec OutputScalingEnvelope;
            public LFOGenRec OutputScalingLFO;
            public EvalEnvelopeRec WaveTableIndexEnvelope;
            public LFOGenRec WaveTableIndexLFO;
        }

        /* variant stuff for track processor */
        public class NLTrackRec
        {
            /* control parameters */
            public ScalarParamEvalRec InputScaling;
            public ScalarParamEvalRec OutputScaling;
            public ScalarParamEvalRec WaveTableIndex;
        }

        public class NLProcUnifiedRec : ITrackEffect, IOscillatorEffect
        {
            /* number of frames per table */
            public int FramesPerTable;
            public int NumberOfTables;
            /* number of tables - 1 (kept as float since it always multiplies a float) */
            public float NumberOfTablesMinus1;
            /* raw wave table data array */
            public float[][] WaveTableMatrix;

            /* state parameters */
            public double CurrentInputScaling;
            public double CurrentOutputScaling;
            public double CurrentWaveTableIndex; /* 0..NumTables - 1 */

            public bool clampOverflow;

            /* variant stuff */
            // exactly one of these is not null
            public NLOscRec Oscillator;
            public NLTrackRec Track;


            /* initialize common parts of nlproc structure */
            private static NLProcUnifiedRec CommonNLStructAlloc(
                NonlinProcSpecRec Template,
                SynthParamRec SynthParams)
            {
                NLProcUnifiedRec NLProc = new NLProcUnifiedRec();

                /* get the wave table to use for this instance */
                string Name = GetNLProcSpecWaveTableName(Template);
                if (!WaveSampDictGetWaveTableInfo(
                    SynthParams.Dictionary,
                    Name,
                    out NLProc.WaveTableMatrix,
                    out NLProc.FramesPerTable,
                    out NLProc.NumberOfTables))
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                NLProc.NumberOfTablesMinus1 = NLProc.NumberOfTables - 1;

                NLProc.clampOverflow = GetNLProcOverflowMode(Template) == NonlinProcOverflowMode.Clamp;

                return NLProc;
            }

            /* create a new track nonlinear processor */
            public static NLProcUnifiedRec NewTrackNLProcProcessor(
                NonlinProcSpecRec Template,
                SynthParamRec SynthParams)
            {
                /* initialize common portion of structure */
                NLProcUnifiedRec NLProc = CommonNLStructAlloc(
                    Template,
                    SynthParams);

                NLProc.Track = new NLTrackRec();

                /* initialize variant portion */
                GetNLProcInputScalingAgg(
                    Template,
                    out NLProc.Track.InputScaling);
                GetNLProcOutputScalingAgg(
                    Template,
                    out NLProc.Track.OutputScaling);
                GetNLProcWaveTableIndexAgg(
                    Template,
                    out NLProc.Track.WaveTableIndex);

                return NLProc;
            }

            /* create a new oscillator nonlinear processor */
            public static NLProcUnifiedRec NewOscNLProcProcessor(
                NonlinProcSpecRec Template,
                ref AccentRec Accents,
                double HurryUp,
                double InitialFrequency,
                double FreqForMultisampling,
                out int PreOriginTimeOut,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams)
            {
                int OnePreOrigin;

                /* initialize common portion of structure */
                NLProcUnifiedRec NLProc = CommonNLStructAlloc(
                    Template,
                    SynthParams);

                NLProc.Oscillator = new NLOscRec();

                int MaxPreOrigin = 0;

                NLProc.Oscillator.InputScalingEnvelope = NewEnvelopeStateRecord(
                    GetNLProcInputEnvelope(Template),
                    ref Accents,
                    InitialFrequency,
                    1,
                    HurryUp,
                    out OnePreOrigin,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }

                NLProc.Oscillator.InputScalingLFO = NewLFOGenerator(
                    GetNLProcInputLFO(Template),
                    out OnePreOrigin,
                    ref Accents,
                    InitialFrequency,
                    HurryUp,
                    1,
                    1,
                    FreqForMultisampling,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }

                NLProc.Oscillator.OutputScalingEnvelope = NewEnvelopeStateRecord(
                    GetNLProcOutputEnvelope(Template),
                    ref Accents,
                    InitialFrequency,
                    1,
                    HurryUp,
                    out OnePreOrigin,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }

                NLProc.Oscillator.OutputScalingLFO = NewLFOGenerator(
                    GetNLProcOutputLFO(Template),
                    out OnePreOrigin,
                    ref Accents,
                    InitialFrequency,
                    HurryUp,
                    1,
                    1,
                    FreqForMultisampling,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }

                NLProc.Oscillator.WaveTableIndexEnvelope = NewEnvelopeStateRecord(
                    GetNLProcIndexEnvelope(Template),
                    ref Accents,
                    InitialFrequency,
                    1,
                    HurryUp,
                    out OnePreOrigin,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }

                NLProc.Oscillator.WaveTableIndexLFO = NewLFOGenerator(
                    GetNLProcIndexLFO(Template),
                    out OnePreOrigin,
                    ref Accents,
                    InitialFrequency,
                    HurryUp,
                    1,
                    1,
                    FreqForMultisampling,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }

                PreOriginTimeOut = MaxPreOrigin;

                return NLProc;
            }

            /* fix up the origin time so that envelopes start at the proper times */
            public void OscFixEnvelopeOrigins(
                int ActualPreOriginTime)
            {
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.InputScalingEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.OutputScalingEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.WaveTableIndexEnvelope,
                    ActualPreOriginTime);

                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.InputScalingLFO,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.OutputScalingLFO,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.WaveTableIndexLFO,
                    ActualPreOriginTime);
            }

            /* update nonlinear state with accent information */
            public SynthErrorCodes TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;
                double DoubleTemp;

                error = ScalarParamEval(
                    this.Track.InputScaling,
                    ref Accents,
                    SynthParams,
                    out this.CurrentInputScaling);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.OutputScaling,
                    ref Accents,
                    SynthParams,
                    out this.CurrentOutputScaling);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = ScalarParamEval(
                    this.Track.WaveTableIndex,
                    ref Accents,
                    SynthParams,
                    out DoubleTemp);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }
                DoubleTemp = DoubleTemp * this.NumberOfTablesMinus1;
                if (DoubleTemp < 0)
                {
                    DoubleTemp = 0;
                }
                else if (DoubleTemp > this.NumberOfTablesMinus1)
                {
                    DoubleTemp = this.NumberOfTablesMinus1;
                }
                this.CurrentWaveTableIndex = DoubleTemp;

                return SynthErrorCodes.eSynthDone;
            }

            /* update nonlinear state with accent information */
            public SynthErrorCodes OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;
                double DoubleTemp;

                error = SynthErrorCodes.eSynthDone;
                this.CurrentInputScaling = LFOGenUpdateCycle(
                    this.Oscillator.InputScalingLFO,
                    EnvelopeUpdate(
                        this.Oscillator.InputScalingEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                this.CurrentOutputScaling = LFOGenUpdateCycle(
                    this.Oscillator.OutputScalingLFO,
                    EnvelopeUpdate(
                        this.Oscillator.OutputScalingEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                error = SynthErrorCodes.eSynthDone;
                DoubleTemp = this.NumberOfTablesMinus1 *
                    LFOGenUpdateCycle(
                        this.Oscillator.WaveTableIndexLFO,
                        EnvelopeUpdate(
                            this.Oscillator.WaveTableIndexEnvelope,
                            OscillatorFrequency,
                            SynthParams,
                            ref error),
                        OscillatorFrequency,
                        SynthParams,
                        ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }
                if (DoubleTemp < 0)
                {
                    DoubleTemp = 0;
                }
                else if (DoubleTemp > this.NumberOfTablesMinus1)
                {
                    DoubleTemp = this.NumberOfTablesMinus1;
                }
                this.CurrentWaveTableIndex = DoubleTemp;

                return SynthErrorCodes.eSynthDone;
            }

            public void OscKeyUpSustain1()
            {
                EnvelopeKeyUpSustain1(this.Oscillator.InputScalingEnvelope);
                EnvelopeKeyUpSustain1(this.Oscillator.OutputScalingEnvelope);
                EnvelopeKeyUpSustain1(this.Oscillator.WaveTableIndexEnvelope);

                LFOGeneratorKeyUpSustain1(this.Oscillator.InputScalingLFO);
                LFOGeneratorKeyUpSustain1(this.Oscillator.OutputScalingLFO);
                LFOGeneratorKeyUpSustain1(this.Oscillator.WaveTableIndexLFO);
            }

            public void OscKeyUpSustain2()
            {
                EnvelopeKeyUpSustain2(this.Oscillator.InputScalingEnvelope);
                EnvelopeKeyUpSustain2(this.Oscillator.OutputScalingEnvelope);
                EnvelopeKeyUpSustain2(this.Oscillator.WaveTableIndexEnvelope);

                LFOGeneratorKeyUpSustain2(this.Oscillator.InputScalingLFO);
                LFOGeneratorKeyUpSustain2(this.Oscillator.OutputScalingLFO);
                LFOGeneratorKeyUpSustain2(this.Oscillator.WaveTableIndexLFO);
            }

            public void OscKeyUpSustain3()
            {
                EnvelopeKeyUpSustain3(this.Oscillator.InputScalingEnvelope);
                EnvelopeKeyUpSustain3(this.Oscillator.OutputScalingEnvelope);
                EnvelopeKeyUpSustain3(this.Oscillator.WaveTableIndexEnvelope);

                LFOGeneratorKeyUpSustain3(this.Oscillator.InputScalingLFO);
                LFOGeneratorKeyUpSustain3(this.Oscillator.OutputScalingLFO);
                LFOGeneratorKeyUpSustain3(this.Oscillator.WaveTableIndexLFO);
            }

            /* retrigger effect envelopes from the origin point */
            public void OscRetriggerEnvelopes(
                ref AccentRec NewAccents,
                double NewHurryUp,
                double NewInitialFrequency,
                bool ActuallyRetrigger,
                SynthParamRec SynthParams)
            {
                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.InputScalingEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);
                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.OutputScalingEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);
                EnvelopeRetriggerFromOrigin(
                    this.Oscillator.WaveTableIndexEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    ActuallyRetrigger,
                    SynthParams);

                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.InputScalingLFO,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.OutputScalingLFO,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    this.Oscillator.WaveTableIndexLFO,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    ActuallyRetrigger,
                    SynthParams);
            }

            private static void Clamp(
                Fixed64[] FrameIndexBufferBase,
                int FrameIndexBufferOffset,
                int FramesPerTable,
                int nActualFrames)
            {
                // TODO: optimize
                for (int i = 0; i < nActualFrames; i++)
                {
                    if (FrameIndexBufferBase[i + FrameIndexBufferOffset].Int > FramesPerTable)
                    {
                        FrameIndexBufferBase[i + FrameIndexBufferOffset] = new Fixed64(((long)FramesPerTable << 32) - 1);
                    }
                    else if (FrameIndexBufferBase[i + FrameIndexBufferOffset].Int < 0)
                    {
                        FrameIndexBufferBase[i + FrameIndexBufferOffset] = new Fixed64(0x0000000000000000);
                    }
                }
            }

            /* apply processor to stream */
            private static void ApplyUnifiedNLProcHelper(
                float[] Data,
                int Offset,
                int nActualFrames,
                NLProcUnifiedRec NLProc,
                SynthParamRec SynthParams)
            {
                double WaveIndexScaling;
                double WaveIndexScalingTimesInputScaling;

#if DEBUG
                Debug.Assert(!SynthParams.ScratchWorkspace3InUse);
                SynthParams.ScratchWorkspace3InUse = true;
#endif
                Fixed64[] FrameIndexBufferBase = SynthParams.ScratchWorkspace3;
                int FrameIndexBufferOffset = SynthParams.ScratchWorkspace3Offset;

#if DEBUG
                if ((NLProc.CurrentWaveTableIndex < 0) || (NLProc.CurrentWaveTableIndex > NLProc.NumberOfTablesMinus1))
                {
                    // table index out of range
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
#endif

                /* expansion factor for wave table length */
                WaveIndexScaling = .5 * (NLProc.FramesPerTable - 1);

                /* precomputed thang */
                WaveIndexScalingTimesInputScaling = NLProc.CurrentInputScaling * WaveIndexScaling;

                /* compute phases from input data */
                /* (1 + x * inputscaling) / 2 * (framespertable - 1) */
                /* Phase = (1 + AdjustedBase[Scan] * InputScaling) * WaveIndexScaling; */
                /* Phase = WaveIndexScaling + AdjustedBase[Scan] * WaveIndexScalingTimesInputScaling; */
                VectorAssignFixed64FromFloat(
                    FrameIndexBufferBase,
                    FrameIndexBufferOffset,
                    Data,
                    Offset,
                    nActualFrames,
                    (float)WaveIndexScalingTimesInputScaling/*factor */,
                    (float)WaveIndexScaling/*addend*/);

                if (NLProc.clampOverflow)
                {
                    Clamp(
                        FrameIndexBufferBase,
                        FrameIndexBufferOffset,
                        NLProc.FramesPerTable,
                        nActualFrames);
                }

                VectorWaveIndex(
                    NLProc.CurrentWaveTableIndex,
                    NLProc.FramesPerTable,
                    NLProc.NumberOfTables,
                    NLProc.WaveTableMatrix,
                    FrameIndexBufferBase,
                    FrameIndexBufferOffset,
                    Data,
                    Offset,
                    nActualFrames,
                    (float)NLProc.CurrentOutputScaling);

#if DEBUG
                SynthParams.ScratchWorkspace3InUse = false;
#endif
            }

            /* apply nonlinear processing to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                ApplyUnifiedNLProcHelper(
                    workspace,
                    lOffset,
                    nActualFrames,
                    this,
                    SynthParams);
                ApplyUnifiedNLProcHelper(
                    workspace,
                    rOffset,
                    nActualFrames,
                    this,
                    SynthParams);

                return SynthErrorCodes.eSynthDone;
            }

            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
            }
        }
    }
}
