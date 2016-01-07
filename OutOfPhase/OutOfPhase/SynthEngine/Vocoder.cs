/*
 *  Copyright � 1994-2002, 2015-2016 Thomas R. Lawrence
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
        /* this uses wave tables to store data.  each wave is a packed set of */
        /* structures with these fields: */
        /*   element 0:  center frequency */
        /*   element 1:  bandwidth */
        /*   element 2:  gain */
        /*   element 3:  unused */
        /* each value in the wave table is on a scale of -1..1.  This is linearly mapped */
        /* to a scale of 0..20000 for center frequency and bandwidth, and unmapped */
        /* for gain. */
        private const int VOC_FIELDCOUNT = 4;
        private const double VOC_MAXFREQ = 20000;

        /* variant stuff for oscillator processor */
        public class VocOscRec
        {
            /* control parameters */
            public EvalEnvelopeRec OutputScalingEnvelope;
            public LFOGenRec OutputScalingLFO;
            public EvalEnvelopeRec WaveTableIndexEnvelope;
            public LFOGenRec WaveTableIndexLFO;
        }

        /* variant stuff for track processor */
        public class VocTrackRec
        {
            /* control parameters */
            public ScalarParamEvalRec OutputScaling;
            public ScalarParamEvalRec WaveTableIndex;
        }

        /* main structure */
        public class VocoderRec : ITrackEffect, IOscillatorEffect
        {
            /* vector size information */
            public int BandCount;
            public int OrderCount;

            /* number of frames per table */
            public int FramesPerTable;
            /* number of tables - 1 (kept as float since it always multiplies a float) */
            public float NumberOfTablesMinus1;
            /* raw wave table data array */
            public float[][] WaveTableMatrix;

            /* state parameters */
            public double CurrentOutputScaling;
            public double CurrentWaveTableIndex; /* 0..NumberOfTables - 1 */

            /* previous state parameters (used for avoiding updates for unchanged values) */
            public bool ForceFilterUpdate;
            public double PreviousOutputScaling;
            public double PreviousWaveTableIndex; /* 0..NumberOfTables - 1 */

            /* variant stuff */
            // exactly one of these is non-null
            public VocOscRec Oscillator;
            public VocTrackRec Track;

            /* pointer into block trailer for start of gain factor array (actual data */
            /* is located after the last filter section).  there is one per band. */
            /* left channel comes first.  size is 2 * BandCount. */
            public float[] CombinedGainFactorVector;

            /* vector of band pass sections starts after last struct member.  major axis */
            /* is left(0) to right(1), next axis is bands, minor axis is order.  */
            /* size is 2 * BandCount * OrderCount. */
            public ButterworthBandpassRec[] FilterVector;


            /* build common stuff */
            private static VocoderRec VocoderBuildCommonStructure(
                VocoderSpecRec Template,
                SynthParamRec SynthParams)
            {
                int BandCount;
                int OrderCount;
                float[][] WaveTableMatrix;
                int FramesPerTable;
                int NumberOfTables;

                if (!WaveSampDictGetWaveTableInfo(
                    SynthParams.Dictionary,
                    GetVocoderSpecWaveTableName(Template),
                    out WaveTableMatrix,
                    out FramesPerTable,
                    out NumberOfTables))
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                BandCount = GetVocoderMaxNumBands(Template);
                OrderCount = GetVocoderFilterOrder(Template) / 2;
                if (BandCount > FramesPerTable / VOC_FIELDCOUNT)
                {
                    BandCount = FramesPerTable / VOC_FIELDCOUNT;
                }

                VocoderRec Vocoder = new VocoderRec();
                Vocoder.CombinedGainFactorVector = new float[2 * BandCount];
                Vocoder.FilterVector = new ButterworthBandpassRec[2 * BandCount * OrderCount];

                Vocoder.BandCount = BandCount;
                Vocoder.OrderCount = OrderCount;

                Vocoder.ForceFilterUpdate = true;
                Vocoder.PreviousOutputScaling = 0;
                Vocoder.PreviousWaveTableIndex = -1;

                Vocoder.WaveTableMatrix = WaveTableMatrix;
                Vocoder.FramesPerTable = FramesPerTable;
                Vocoder.NumberOfTablesMinus1 = NumberOfTables - 1;

                /* initialize band matrix */
                for (int i = 0; i < 2 * BandCount * OrderCount; i += 1)
                {
                    Vocoder.FilterVector[i] = new ButterworthBandpassRec();
                }

                return Vocoder;
            }

            /* create a new track vocoder */
            public static VocoderRec NewTrackVocoder(
                VocoderSpecRec Template,
                SynthParamRec SynthParams)
            {
                /* initialize common portion of structure */
                VocoderRec Vocoder = VocoderBuildCommonStructure(
                    Template,
                    SynthParams);

                Vocoder.Track = new VocTrackRec();

                /* initialize variant portion */
                GetVocoderSpecOutputGainAgg(
                    Template,
                    out Vocoder.Track.OutputScaling);
                GetVocoderSpecWaveTableIndexAgg(
                    Template,
                    out Vocoder.Track.WaveTableIndex);

                return Vocoder;
            }

            /* create a new oscillator vocoder */
            public static VocoderRec NewOscVocoder(
                VocoderSpecRec Template,
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
                VocoderRec Vocoder = VocoderBuildCommonStructure(
                    Template,
                    SynthParams);

                Vocoder.Oscillator = new VocOscRec();

                /* initialize variant portion */
                int MaxPreOrigin = 0;

                Vocoder.Oscillator.OutputScalingEnvelope = NewEnvelopeStateRecord(
                    GetVocoderSpecOutputGainEnvelope(Template),
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

                Vocoder.Oscillator.OutputScalingLFO = NewLFOGenerator(
                    GetVocoderSpecOutputGainLFO(Template),
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

                Vocoder.Oscillator.WaveTableIndexEnvelope = NewEnvelopeStateRecord(
                    GetVocoderSpecIndexEnvelope(Template),
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

                Vocoder.Oscillator.WaveTableIndexLFO = NewLFOGenerator(
                    GetVocoderSpecIndexLFO(Template),
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

                return Vocoder;
            }

            /* fix up the origin time so that envelopes start at the proper times */
            public void OscFixEnvelopeOrigins(
                int ActualPreOriginTime)
            {
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.OutputScalingEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    this.Oscillator.WaveTableIndexEnvelope,
                    ActualPreOriginTime);

                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.OutputScalingLFO,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    this.Oscillator.WaveTableIndexLFO,
                    ActualPreOriginTime);
            }

            /* update vocoder state with accent information */
            public void TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams)
            {
                double DoubleTemp;

                ScalarParamEval(
                    this.Track.OutputScaling,
                    ref Accents,
                    SynthParams,
                    out this.CurrentOutputScaling);

                ScalarParamEval(
                    this.Track.WaveTableIndex,
                    ref Accents,
                    SynthParams,
                    out DoubleTemp);
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
            }

            /* update vocoder state with accent information */
            public void OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams)
            {
                double DoubleTemp;

                this.CurrentOutputScaling = LFOGenUpdateCycle(
                    this.Oscillator.OutputScalingLFO,
                    EnvelopeUpdate(
                        this.Oscillator.OutputScalingEnvelope,
                        OscillatorFrequency,
                        SynthParams),
                    OscillatorFrequency,
                    SynthParams);

                DoubleTemp = this.NumberOfTablesMinus1 *
                    LFOGenUpdateCycle(
                        this.Oscillator.WaveTableIndexLFO,
                        EnvelopeUpdate(
                            this.Oscillator.WaveTableIndexEnvelope,
                            OscillatorFrequency,
                            SynthParams),
                        OscillatorFrequency,
                        SynthParams);
                if (DoubleTemp < 0)
                {
                    DoubleTemp = 0;
                }
                else if (DoubleTemp > this.NumberOfTablesMinus1)
                {
                    DoubleTemp = this.NumberOfTablesMinus1;
                }
                this.CurrentWaveTableIndex = DoubleTemp;
            }

            public void OscKeyUpSustain1()
            {
                EnvelopeKeyUpSustain1(this.Oscillator.OutputScalingEnvelope);
                EnvelopeKeyUpSustain1(this.Oscillator.WaveTableIndexEnvelope);

                LFOGeneratorKeyUpSustain1(this.Oscillator.OutputScalingLFO);
                LFOGeneratorKeyUpSustain1(this.Oscillator.WaveTableIndexLFO);
            }

            public void OscKeyUpSustain2()
            {
                EnvelopeKeyUpSustain2(this.Oscillator.OutputScalingEnvelope);
                EnvelopeKeyUpSustain2(this.Oscillator.WaveTableIndexEnvelope);

                LFOGeneratorKeyUpSustain2(this.Oscillator.OutputScalingLFO);
                LFOGeneratorKeyUpSustain2(this.Oscillator.WaveTableIndexLFO);
            }

            public void OscKeyUpSustain3()
            {
                EnvelopeKeyUpSustain3(this.Oscillator.OutputScalingEnvelope);
                EnvelopeKeyUpSustain3(this.Oscillator.WaveTableIndexEnvelope);

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

            /* update a filter matrix given a wave table index and matrix base pointer */
            /* first element in the list is the lowest ordered element in the wave table. */
            /* wave index is scaled from 1 to #waves - 1 */
            private static void VocoderUpdateMatrix(
                VocoderRec Vocoder,
                ButterworthBandpassRec[] BandMatrix,
                int BandMatrixOffset,
                float[] CombinedGainFactorVector,
                int CombinedGainFactorVectorOffset,
                SynthParamRec SynthParams)
            {
                int BandCount = Vocoder.BandCount;
                int OrderCount = Vocoder.OrderCount;
                float[][] WaveMatrix = Vocoder.WaveTableMatrix;
                float OverallGain = (float)Vocoder.CurrentOutputScaling;

#if DEBUG
                if ((Vocoder.CurrentWaveTableIndex < 0) || (Vocoder.CurrentWaveTableIndex > Vocoder.NumberOfTablesMinus1))
                {
                    // table index out of range
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
#endif

                if ((int)(Vocoder.CurrentWaveTableIndex) == Vocoder.NumberOfTablesMinus1)
                {
                    float[] WaveData;

                    /* end interpolation */

                    WaveData = WaveMatrix[(int)(Vocoder.CurrentWaveTableIndex)];

                    /* start at the beginning */
                    for (int i = 0; i < BandCount; i += 1)
                    {
                        float RawCenterFreq;
                        float RawBandWidth;
                        float RawUncombinedGain;
                        double CookedCenterFreq;
                        double CookedBandWidth;

                        /* L+F(R-L) */

                        /* consult wave table for center frequency */
                        RawCenterFreq = WaveData[i * VOC_FIELDCOUNT + 0];

                        /* consult wave table for bandwidth */
                        RawBandWidth = WaveData[i * VOC_FIELDCOUNT + 1];

                        /* consult wave table for gain */
                        RawUncombinedGain = WaveData[i * VOC_FIELDCOUNT + 2];

                        /* do the mappings */
                        CombinedGainFactorVector[CombinedGainFactorVectorOffset + i] = RawUncombinedGain * OverallGain;
                        CookedCenterFreq = (VOC_MAXFREQ * 0.5) * (1 + (double)RawCenterFreq);
                        CookedBandWidth = (VOC_MAXFREQ * 0.5) * (1 + (double)RawBandWidth);

                        /* set the settings */
                        for (int j = 0; j < OrderCount; j += 1)
                        {
                            ButterworthBandpassRec.SetButterworthBandpassCoefficients(
                                BandMatrix[BandMatrixOffset + i * OrderCount + j],
                                CookedCenterFreq,
                                CookedBandWidth,
                                SynthParams.dSamplingRate);
                        }
                    }
                }
                else
                {
                    float Wave1Weight;
                    float[] WaveData0;
                    float[] WaveData1;

                    /* compute table weighting */
                    Wave1Weight = (float)(Vocoder.CurrentWaveTableIndex - (int)(Vocoder.CurrentWaveTableIndex));

                    /* full interpolation */

                    WaveData0 = WaveMatrix[(int)(Vocoder.CurrentWaveTableIndex)];
                    WaveData1 = WaveMatrix[(int)(Vocoder.CurrentWaveTableIndex) + 1];

                    /* start at the beginning */
                    for (int i = 0; i < BandCount; i += 1)
                    {
                        float RawCenterFreq;
                        float RawBandWidth;
                        float RawUncombinedGain;
                        double CookedCenterFreq;
                        double CookedBandWidth;
                        float Wave0Value;
                        float Wave1Value;

                        /* L+F(R-L) */

                        /* consult wave table for center frequency */
                        Wave0Value = WaveData0[i * VOC_FIELDCOUNT + 0];
                        Wave1Value = WaveData1[i * VOC_FIELDCOUNT + 0];
                        RawCenterFreq = Wave0Value + (Wave1Weight * (Wave1Value - Wave0Value));

                        /* consult wave table for bandwidth */
                        Wave0Value = WaveData0[i * VOC_FIELDCOUNT + 1];
                        Wave1Value = WaveData1[i * VOC_FIELDCOUNT + 1];
                        RawBandWidth = Wave0Value + (Wave1Weight * (Wave1Value - Wave0Value));

                        /* consult wave table for gain */
                        Wave0Value = WaveData0[i * VOC_FIELDCOUNT + 2];
                        Wave1Value = WaveData1[i * VOC_FIELDCOUNT + 2];
                        RawUncombinedGain = Wave0Value + (Wave1Weight * (Wave1Value - Wave0Value));

                        /* do the mappings */
                        CombinedGainFactorVector[CombinedGainFactorVectorOffset + i] = RawUncombinedGain * OverallGain;
                        CookedCenterFreq = (VOC_MAXFREQ * 0.5) * (1 + (double)RawCenterFreq);
                        CookedBandWidth = (VOC_MAXFREQ * 0.5) * (1 + (double)RawBandWidth);

                        /* set the settings */
                        for (int j = 0; j < OrderCount; j += 1)
                        {
                            ButterworthBandpassRec.SetButterworthBandpassCoefficients(
                                BandMatrix[BandMatrixOffset + i * OrderCount + j],
                                CookedCenterFreq,
                                CookedBandWidth,
                                SynthParams.dSamplingRate);
                        }
                    }
                }
            }

            /* apply filter matrix to vector */
            private static void ApplyVocoderMatrix(
                VocoderRec Vocoder,
                ButterworthBandpassRec[] BandMatrix,
                int BandMatrixOffset,
                float[] CombinedGainFactorVector,
                int CombinedGainFactorVectorOffset,
                float[] Vector,
                int VectorOffset,
                float[] SourceBase,
                int SourceOffset,
                float[] MangleBase,
                int MangleOffset,
                int Length,
                SynthParamRec SynthParams)
            {
                int BandCount = Vocoder.BandCount;
                int OrderCount = Vocoder.OrderCount;

                /* build source and initialize vector for output */
                FloatVectorCopy(
                    Vector,
                    VectorOffset,
                    SourceBase,
                    SourceOffset,
                    Length);
                FloatVectorZero(
                    Vector,
                    VectorOffset,
                    Length);

                /* process matrix */
                for (int i = 0; i < BandCount; i += 1)
                {
                    /* initialize mangle vector with raw data */
                    FloatVectorCopy(
                        SourceBase,
                        SourceOffset,
                        MangleBase,
                        MangleOffset,
                        Length);

                    /* apply filters to succession, copying out on the last one */
                    for (int j = 0; j < OrderCount; j += 1)
                    {
                        if (j != OrderCount - 1)
                        {
                            /* interior filters modify the buffer, with unity gain */
                            ButterworthBandpassRec.ApplyButterworthBandpassVectorModify(
                                BandMatrix[BandMatrixOffset + i * OrderCount + j],
                                MangleBase,
                                MangleOffset,
                                Length);
                        }
                        else
                        {
                            /* final filter adds to output */
                            BandMatrix[BandMatrixOffset + i * OrderCount + j].Apply(
                                MangleBase,
                                MangleOffset,
                                Vector,
                                VectorOffset,
                                Length,
                                CombinedGainFactorVector[CombinedGainFactorVectorOffset + i],
                                SynthParams);
                        }
                    }
                }
            }

            /* apply vocoder to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
#if DEBUG
                Debug.Assert(!SynthParams.ScratchWorkspace1InUse);
                SynthParams.ScratchWorkspace1InUse = true;
#endif
                int SourceOffset = SynthParams.ScratchWorkspace1LOffset;
                int MangleOffset = SynthParams.ScratchWorkspace1ROffset;

                /* build matrices if necessary */
                if (this.ForceFilterUpdate
                    || (this.CurrentOutputScaling != this.PreviousOutputScaling)
                    || (this.CurrentWaveTableIndex != this.PreviousWaveTableIndex))
                {
                    /* update previous values */
                    this.ForceFilterUpdate = false; /* turn off force */
                    this.PreviousWaveTableIndex = this.CurrentWaveTableIndex;
                    this.PreviousOutputScaling = this.CurrentOutputScaling;

                    /* update matrix values */
                    VocoderUpdateMatrix(
                        this,
                        this.FilterVector,
                        0,
                        this.CombinedGainFactorVector,
                        0,
                        SynthParams);
                    VocoderUpdateMatrix(
                        this,
                        this.FilterVector,
                        this.BandCount * this.OrderCount,
                        this.CombinedGainFactorVector,
                        this.BandCount,
                        SynthParams);
                }

                /* process left */
                ApplyVocoderMatrix(
                    this,
                    this.FilterVector,
                    0,
                    this.CombinedGainFactorVector,
                    0,
                    workspace,
                    lOffset,
                    workspace,
                    SourceOffset,
                    workspace,
                    MangleOffset,
                    nActualFrames,
                    SynthParams);
                /* process right */
                ApplyVocoderMatrix(
                    this,
                    this.FilterVector,
                    this.BandCount * this.OrderCount,
                    this.CombinedGainFactorVector,
                    this.BandCount,
                    workspace,
                    rOffset,
                    workspace,
                    SourceOffset,
                    workspace,
                    MangleOffset,
                    nActualFrames,
                    SynthParams);

#if DEBUG
                SynthParams.ScratchWorkspace1InUse = false;
#endif

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