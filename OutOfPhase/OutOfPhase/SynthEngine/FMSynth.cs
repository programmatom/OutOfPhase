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
        public class FMSynthOneTemplateRec_Wave
        {
            public int PhaseSource;
            public int PhaseGainSource;
            public double FrequencyMultiplier;
            public double FrequencyAdder;

            /* source information for the wave table */
            public MultiWaveTableRec WaveTableSourceSelector;

            /* wavetable array index enveloping */
            public EnvelopeRec IndexEnvelopeTemplate;
            public LFOListSpecRec IndexLFOTemplate;
        }

        public class FMSynthOneTemplateRec_MulAdd
        {
            public int AccumulateFrom;
            public int Source;
            public int Factor2;

            public double Factor;
            public double Addend;
        }

        public class FMSynthOneTemplateRec_Env
        {
            public EnvelopeRec EnvelopeTemplate;
            public LFOListSpecRec LFOTemplate;
        }

        public class FMSynthOneTemplateRec
        {
            public FMSynthStmtType Type;

            /* target for value to be written */
            public int Target;

            public object u;
        }

        public class FMSynthTemplateRec : IOscillatorTemplate
        {
            /* values for scaling the frequency of something.  if we were really serious about */
            /* this, we'd traverse all of the oscillators with integral multiples or harmonic */
            /* fractions of the pitch & set their differentials to the same precision as the */
            /* worst oscillator so that they would all stay in sync as time progressed. */
            public double FrequencyMultiplier;
            /* this is added after the frequency multiplier is applied */
            public double FrequencyAdder;

            /* envelope templates */
            public EnvelopeRec LoudnessEnvelopeTemplate;
            public LFOListSpecRec LoudnessLFOTemplate;

            /* miscellaneous control parameters */
            public double StereoBias;
            public double TimeDisplacement;
            public double OverallOscillatorLoudness;

            /* template for the pitch displacement LFO */
            public LFOListSpecRec PitchLFOTemplate;

            /* effect specifier, may be null */
            public EffectSpecListRec OscEffectTemplate;

            public bool EnableCrossWaveTableInterpolation;

            /* number of statements */
            public int NumStmts;
            public int NumStmtsOptimizable;

            /* number of variables */
            public int NumVars;

            /* statement information */
            /* this thing is constructed so that the ordering has all optimizable statements */
            /* first (preserving original subordering), then all non-optimizable statements */
            /* (again preserving original subordering) */
            public FMSynthOneTemplateRec[] Stmts;


            /* create a new wave table template */
            public static FMSynthTemplateRec NewFMSynthTemplate(
                OscillatorRec Oscillator,
                SynthParamRec SynthParams)
            {
                FMSynthSpecRec FMSynthSpec;
                int NumStmts;
                int iNextStmtPos;

                FMSynthSpec = OscillatorGetFMSynthSpec(Oscillator);
                NumStmts = FMSynthGetNumStatements(FMSynthSpec);

                FMSynthTemplateRec Template = new FMSynthTemplateRec();
                Template.Stmts = new FMSynthOneTemplateRec[NumStmts];
                for (int i = 0; i < NumStmts; i++)
                {
                    Template.Stmts[i] = new FMSynthOneTemplateRec();
                }

                Template.NumStmts = NumStmts;
                Template.NumStmtsOptimizable = FMSynthGetNumOptimizableStatements(FMSynthSpec);
                Template.NumVars = FMSynthGetNumVariables(FMSynthSpec);

                Template.OverallOscillatorLoudness = OscillatorGetOutputLoudness(Oscillator);

                /* it might be better to handle divisor and multiplier separately -- we would */
                /* want to do that if we were trying to guarantee that all harmonic */
                /* oscillators ran in lock-step */
                Template.FrequencyMultiplier = OscillatorGetFrequencyMultiplier(Oscillator)
                    / OscillatorGetFrequencyDivisor(Oscillator);
                Template.FrequencyAdder = OscillatorGetFrequencyAdder(Oscillator);

                Template.StereoBias = OscillatorGetStereoBias(Oscillator);
                Template.TimeDisplacement = OscillatorGetTimeDisplacement(Oscillator);

                /* these are just references */
                Template.LoudnessEnvelopeTemplate = OscillatorGetLoudnessEnvelope(Oscillator);
                Template.LoudnessLFOTemplate = OscillatorGetLoudnessLFOList(Oscillator);

                /* more references */
                Template.PitchLFOTemplate = GetOscillatorFrequencyLFOList(Oscillator);
                Template.OscEffectTemplate = GetOscillatorEffectList(Oscillator);
                if (GetEffectSpecListLength(Template.OscEffectTemplate) == 0)
                {
                    Template.OscEffectTemplate = null;
                }

                Template.EnableCrossWaveTableInterpolation = OscillatorGetEnableCrossWaveTableInterpolation(Oscillator);

                /* two initialization loops, one for the optimized ones, then for the nonoptimized */
                iNextStmtPos = 0;
                for (int i = 0; i < Template.NumStmts; i++)
                {
                    if (FMSynthStmtCanBeOptimized(FMSynthSpec, i))
                    {
                        FMSynthOneTemplateRec Stmt = Template.Stmts[iNextStmtPos];

                        InitTemplateOneStmt(
                            FMSynthSpec,
                            i,
                            Stmt,
                            SynthParams);

                        iNextStmtPos++;
                    }
                }
#if DEBUG
                if (iNextStmtPos != Template.NumStmtsOptimizable)
                {
                    // optimized stmt count error
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
#endif
                for (int i = 0; i < Template.NumStmts; i++)
                {
                    if (!FMSynthStmtCanBeOptimized(FMSynthSpec, i))
                    {
                        FMSynthOneTemplateRec Stmt = Template.Stmts[iNextStmtPos];

                        InitTemplateOneStmt(
                            FMSynthSpec,
                            i,
                            Stmt,
                            SynthParams);

                        iNextStmtPos++;
                    }
                }
#if DEBUG
                if (iNextStmtPos != Template.NumStmts)
                {
                    // nonoptimized stmt count error
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
#endif

                return Template;
            }

            /* helper -- initialize one statement in the template arary */
            private static void InitTemplateOneStmt(
                FMSynthSpecRec FMSynthSpec,
                int iPositionInSpecification,
                FMSynthOneTemplateRec TmplStmt,
                SynthParamRec SynthParams)
            {
                TmplStmt.Type = FMSynthGetStatementType(FMSynthSpec, iPositionInSpecification);

                switch (TmplStmt.Type)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case FMSynthStmtType.eFMSynthWave:
                        {
                            FMSynthStmtWaveRec WaveTmpl;

                            WaveTmpl = FMSynthGetWaveStatement(FMSynthSpec, iPositionInSpecification);

                            FMSynthOneTemplateRec_Wave WaveT = new FMSynthOneTemplateRec_Wave();
                            TmplStmt.u = WaveT;

                            WaveT.WaveTableSourceSelector = NewMultiWaveTable(
                                FMSynthWaveStmtGetSampleIntervalList(WaveTmpl),
                                SynthParams.Dictionary);

                            /* these are just references */
                            WaveT.IndexEnvelopeTemplate = FMSynthWaveStmtGetIndexEnvelope(WaveTmpl);
                            WaveT.IndexLFOTemplate = FMSynthWaveStmtGetIndexLFOList(WaveTmpl);

                            WaveT.FrequencyMultiplier = FMSynthWaveStmtGetFrequencyMultiplier(WaveTmpl)
                                / FMSynthWaveStmtGetFrequencyDivisor(WaveTmpl);
                            WaveT.FrequencyAdder = FMSynthWaveStmtGetFrequencyAdder(WaveTmpl);
                            TmplStmt.Target = FMSynthWaveStmtGetTarget(WaveTmpl);
                            WaveT.PhaseSource = FMSynthWaveStmtGetPhaseSource(WaveTmpl);
                            WaveT.PhaseGainSource = FMSynthWaveStmtGetPhaseGainSource(WaveTmpl);
                        }
                        break;

                    case FMSynthStmtType.eFMSynthMuladd:
                        {
                            FMSynthStmtMulAddRec MulAddTmpl;

                            MulAddTmpl = FMSynthGetMulAddStatement(FMSynthSpec, iPositionInSpecification);

                            FMSynthOneTemplateRec_MulAdd MulAddT = new FMSynthOneTemplateRec_MulAdd();
                            TmplStmt.u = MulAddT;

                            MulAddT.Factor = FMSynthMulAddStmtGetFactor(MulAddTmpl);
                            MulAddT.Addend = FMSynthMulAddStmtGetAddend(MulAddTmpl);
                            MulAddT.Source = FMSynthMulAddStmtGetDataSource(MulAddTmpl);
                            TmplStmt.Target = FMSynthMulAddStmtGetDataTarget(MulAddTmpl);
                            MulAddT.Factor2 = FMSynthMulAddStmtGetDataFactor2(MulAddTmpl);
                            MulAddT.AccumulateFrom = FMSynthMulAddStmtGetDataSource2(MulAddTmpl);
                        }
                        break;

                    case FMSynthStmtType.eFMSynthEnvelope:
                        {
                            FMSynthStmtEnvelopeRec EnvTmpl;

                            EnvTmpl = FMSynthGetEnvelopeStatement(FMSynthSpec, iPositionInSpecification);

                            FMSynthOneTemplateRec_Env EnvT = new FMSynthOneTemplateRec_Env();
                            TmplStmt.u = EnvT;

                            /* these are just references */
                            EnvT.EnvelopeTemplate = FMSynthEnvelopeStmtGetEnvelope(EnvTmpl);
                            EnvT.LFOTemplate = FMSynthEnvelopeStmtGetLFOList(EnvTmpl);

                            TmplStmt.Target = FMSynthEnvelopeStmtGetDataTarget(EnvTmpl);
                        }
                        break;
                }
            }

            /* create a new wave table state object. */
            public SynthErrorCodes NewState(
                double FreqForMultisampling,
                ref AccentRec Accents,
                double Loudness,
                double HurryUp,
                out int PreOriginTimeOut,
                double StereoPosition,
                double InitialFrequency,
                double PitchDisplacementDepthLimit,
                double PitchDisplacementRateLimit,
                int PitchDisplacementStartPoint,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams,
                out IOscillator StateOut)
            {
                FMSynthTemplateRec Template = this;

                int OnePreOrigin;

                PreOriginTimeOut = 0;
                StateOut = null;

                FMSynthStateRec State = new FMSynthStateRec();
                State.Stmts = new FMSynthOneStateRec[Template.NumStmts];
                for (int i = 0; i < Template.NumStmts; i++)
                {
                    State.Stmts[i] = new FMSynthOneStateRec();
                }

                State.Template = Template;

                State.Vars = new double[Template.NumVars];
                State.Vars[FMSYNTH_ONE] = 1;

                int MaxPreOrigin = 0;

                State.NoteLoudnessScaling = Loudness * Template.OverallOscillatorLoudness;

                State.PreStartCountdown = (int)((Template.TimeDisplacement * SynthParams.dEnvelopeRate) + 0.5);
                if (-State.PreStartCountdown > MaxPreOrigin)
                {
                    MaxPreOrigin = -State.PreStartCountdown;
                }

                for (int i = 0; i < Template.NumStmts; i++)
                {
                    FMSynthOneTemplateRec TmplStmt = Template.Stmts[i];
                    FMSynthOneStateRec StateStmt = State.Stmts[i];

                    StateStmt.Type = TmplStmt.Type;

                    StateStmt.Target = TmplStmt.Target;

                    switch (TmplStmt.Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case FMSynthStmtType.eFMSynthWave:
                            {
                                int NumberOfTables;
                                int FramesPerTable;

                                FMSynthOneTemplateRec_Wave WaveT = (FMSynthOneTemplateRec_Wave)TmplStmt.u;

                                FMSynthOneStateRec_Wave Wave = new FMSynthOneStateRec_Wave();
                                StateStmt.u = Wave;

                                Wave.WaveTableSamplePosition = 0;
                                /* Wave.WaveTableSamplePositionDifferential specified in separate call */

                                Wave.WaveTableWasDefined = GetMultiWaveTableReference(
                                    WaveT.WaveTableSourceSelector,
                                    FreqForMultisampling,
                                    out Wave.WaveTableMatrix,
                                    out FramesPerTable,
                                    out NumberOfTables);
                                Wave.NumberOfTablesMinus1 = NumberOfTables - 1;
                                Wave.FloatFramesPerTable = FramesPerTable;
                                Wave.FramesPerTableMinus1 = FramesPerTable - 1;

                                Wave.FramesPerTableOverFinalOutputSamplingRate
                                    = (double)Wave.FloatFramesPerTable / SynthParams.dSamplingRate;

                                /* State.FramesPerTable > 0: */
                                /*   if the wave table is empty, then we don't do any work (and we must not, */
                                /*   since array accesses would cause a crash) */
                                if (Wave.WaveTableWasDefined)
                                {
                                    if (!(FramesPerTable > 0))
                                    {
                                        Wave.WaveTableWasDefined = false;
                                    }
                                }

                                Wave.IndexEnvelope = NewEnvelopeStateRecord(
                                    WaveT.IndexEnvelopeTemplate,
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

                                Wave.IndexLFOGenerator = NewLFOGenerator(
                                    WaveT.IndexLFOTemplate,
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

                                Wave.PhaseSource = WaveT.PhaseSource;
                                Wave.PhaseGainSource = WaveT.PhaseGainSource;
                            }
                            break;

                        case FMSynthStmtType.eFMSynthMuladd:
                            {
                                FMSynthOneTemplateRec_MulAdd MulAddT = (FMSynthOneTemplateRec_MulAdd)TmplStmt.u;

                                FMSynthOneStateRec_MulAdd MulAdd = new FMSynthOneStateRec_MulAdd();
                                StateStmt.u = MulAdd;

                                MulAdd.Factor = MulAddT.Factor;
                                MulAdd.Addend = MulAddT.Addend;
                                MulAdd.AccumulateFrom = MulAddT.AccumulateFrom;
                                MulAdd.Source = MulAddT.Source;
                                MulAdd.Factor2 = MulAddT.Factor2;
                            }
                            break;

                        case FMSynthStmtType.eFMSynthEnvelope:
                            {
                                FMSynthOneTemplateRec_Env EnvT = (FMSynthOneTemplateRec_Env)TmplStmt.u;

                                FMSynthOneStateRec_Env Env = new FMSynthOneStateRec_Env();
                                StateStmt.u = Env;

                                Env.Envelope = NewEnvelopeStateRecord(
                                    EnvT.EnvelopeTemplate,
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

                                Env.LFOGenerator = NewLFOGenerator(
                                    EnvT.LFOTemplate,
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
                            }
                            break;
                    }
                }

                /* State.MonoLoudness, State.LeftLoudness, State.RightLoudness */
                /* are determined by the envelope update */
                StereoPosition += Template.StereoBias;
                if (StereoPosition < -1)
                {
                    StereoPosition = -1;
                }
                else if (StereoPosition > 1)
                {
                    StereoPosition = 1;
                }
                State.Panning = (float)StereoPosition;
                State.LoudnessEnvelope = NewEnvelopeStateRecord(
                    Template.LoudnessEnvelopeTemplate,
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
                State.LoudnessLFOGenerator = NewLFOGenerator(
                    Template.LoudnessLFOTemplate,
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
                State.PitchLFO = NewLFOGenerator(
                    Template.PitchLFOTemplate,
                    out OnePreOrigin,
                    ref Accents,
                    InitialFrequency,
                    HurryUp,
                    PitchDisplacementDepthLimit,
                    PitchDisplacementRateLimit,
                    FreqForMultisampling,
                    _PlayTrackParamGetter,
                    TrackInfo,
                    SynthParams);
                if (OnePreOrigin > MaxPreOrigin)
                {
                    MaxPreOrigin = OnePreOrigin;
                }
                State.PitchLFOStartCountdown = PitchDisplacementStartPoint;
                if (Template.OscEffectTemplate == null)
                {
                    State.OscEffectGenerator = null;
                }
                else
                {
                    SynthErrorCodes Result = NewOscEffectGenerator(
                        Template.OscEffectTemplate,
                        ref Accents,
                        HurryUp,
                        InitialFrequency,
                        FreqForMultisampling,
                        out OnePreOrigin,
                        TrackInfo,
                        SynthParams,
                        out State.OscEffectGenerator);
                    if (Result != SynthErrorCodes.eSynthDone)
                    {
                        return Result;
                    }
                    if (OnePreOrigin > MaxPreOrigin)
                    {
                        MaxPreOrigin = OnePreOrigin;
                    }
                }

                PreOriginTimeOut = MaxPreOrigin;
                StateOut = State;
                return SynthErrorCodes.eSynthDone;
            }

            public OscillatorTypes Type { get { return OscillatorTypes.eOscillatorFMSynth; } }
        }


        public class FMSynthOneStateRec_Wave
        {
            /* copy of indices into variable array */
            public int PhaseSource;
            public int PhaseGainSource;

            /* current sample position into the wave table */
            public double WaveTableSamplePosition;
            /* current increment value for the wave table sample position */
            public double WaveTableSamplePositionDifferential;

            /* number of frames per table */
            public float FloatFramesPerTable;
            public int FramesPerTableMinus1;
            /* number of tables - 1 (kept as float since it always multiplies a float) */
            public float NumberOfTablesMinus1;
            /* raw wave table data array */
            public float[][] WaveTableMatrix;

            /* current index into table of waves.  0 = lowest wave table, NumberOfTables = highest */
            public double WaveTableIndex; /* 0..NumTables - 1 */
            /* envelope controlling wave table index */
            public EvalEnvelopeRec IndexEnvelope;
            /* LFO generators modifying the output of the index envelope generator */
            public LFOGenRec IndexLFOGenerator;

            /* this flag is True if the wave table data was defined at the specified pitch */
            /* (and the wave table array is thus valid) or false if there is no wave table */
            /* at this pitch (and the array is invalid) */
            public bool WaveTableWasDefined;

            /* precomputed factor for UpdateWaveTableEnvelopes to use */
            public double FramesPerTableOverFinalOutputSamplingRate;
        }

        public class FMSynthOneStateRec_MulAdd
        {
            /* copy of indices into variable array */
            public int AccumulateFrom;
            public int Source;
            public int Factor2;

            public double Factor;
            public double Addend;
        }

        public class FMSynthOneStateRec_Env
        {
            /* current value for this envelope cycle */
            public double CurrentValue;

            /* envelope */
            public EvalEnvelopeRec Envelope;
            /* LFO generators modifying the output of the envelope generator */
            public LFOGenRec LFOGenerator;
        }

        public class FMSynthOneStateRec
        {
            public FMSynthStmtType Type;

            /* target for value to be written -- copied from template */
            public int Target;

            public object u;
        }

        public class FMSynthStateRec : IOscillator
        {
            public FMSynthTemplateRec Template;

            /* envelope tick countdown for pre-start time */
            public int PreStartCountdown;

            /* left channel loudness */
            public float LeftLoudness;
            /* right channel loudness */
            public float RightLoudness;
            /* panning position for splitting envelope generator into stereo channels */
            /* 0 = left channel, 0.5 = middle, 1 = right channel */
            public float Panning;
            /* envelope that is generating the loudness information */
            public EvalEnvelopeRec LoudnessEnvelope;
            /* LFO generators modifying the output of the loudness envelope generator */
            public LFOGenRec LoudnessLFOGenerator;

            /* this field contains the overall volume scaling for everything so that we */
            /* can treat the envelopes as always going between 0 and 1. */
            public double NoteLoudnessScaling;

            /* this calculates the differential values for periodic pitch displacements */
            public LFOGenRec PitchLFO;
            /* pitch lfo startup counter; negative = expired */
            public int PitchLFOStartCountdown;

            /* postprocessing for this oscillator; may be null */
            public OscEffectGenRec OscEffectGenerator;

            /* array of variables */
            public double[] Vars;

            /* array of statements */
            /* ordering is same as for template array */
            public FMSynthOneStateRec[] Stmts;


            /* perform one envelope update cycle, and set a new frequency for a wave table */
            /* state object.  used for portamento and modulation of frequency (vibrato) */
            public SynthErrorCodes UpdateEnvelopes(
                double NewFrequencyHertz,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;
                FMSynthStateRec State = this;

                float FloatTemp;
                double DoubleTemp;
                float OneHalfVol;
                float LeftLoudness;
                float RightLoudness;

                if (State.PitchLFOStartCountdown > 0)
                {
                    State.PitchLFOStartCountdown -= 1;
                }
                else
                {
                    /* do some pitch stuff */
                    error = SynthErrorCodes.eSynthDone;
                    NewFrequencyHertz = LFOGenUpdateCycle(
                        State.PitchLFO,
                        NewFrequencyHertz,
                        NewFrequencyHertz,
                        SynthParams,
                        ref error);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }
                NewFrequencyHertz = NewFrequencyHertz * State.Template.FrequencyMultiplier + State.Template.FrequencyAdder;

                /* this is for the benefit of resampling only -- envelope generators do their */
                /* own pre-origin sequencing */
                if (State.PreStartCountdown > 0)
                {
                    State.PreStartCountdown -= 1;
                }

                for (int i = 0; i < State.Template.NumStmts; i++)
                {
                    FMSynthOneStateRec Stmt = State.Stmts[i];

                    switch (Stmt.Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case FMSynthStmtType.eFMSynthWave:
                            {
                                FMSynthOneTemplateRec_Wave WaveTmpl = (FMSynthOneTemplateRec_Wave)State.Template.Stmts[i].u;

                                FMSynthOneStateRec_Wave Wave = (FMSynthOneStateRec_Wave)Stmt.u;

                                double NewFrequencyHertzJustThisWave = (NewFrequencyHertz * WaveTmpl.FrequencyMultiplier)
                                    + WaveTmpl.FrequencyAdder;
                                /* finish phase-delta computation */
                                Wave.WaveTableSamplePositionDifferential = NewFrequencyHertzJustThisWave
                                    * Wave.FramesPerTableOverFinalOutputSamplingRate;

                                error = SynthErrorCodes.eSynthDone;
                                DoubleTemp = Wave.NumberOfTablesMinus1 *
                                    LFOGenUpdateCycle(
                                        Wave.IndexLFOGenerator,
                                        EnvelopeUpdate(
                                            Wave.IndexEnvelope,
                                            NewFrequencyHertzJustThisWave,
                                            SynthParams,
                                            ref error),
                                        NewFrequencyHertzJustThisWave,
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
                                else if (DoubleTemp > Wave.NumberOfTablesMinus1)
                                {
                                    DoubleTemp = Wave.NumberOfTablesMinus1;
                                }
                                Wave.WaveTableIndex = DoubleTemp;
                            }
                            break;

                        case FMSynthStmtType.eFMSynthMuladd:
                            break;

                        case FMSynthStmtType.eFMSynthEnvelope:
                            {
                                FMSynthOneStateRec_Env Env = (FMSynthOneStateRec_Env)Stmt.u;

                                error = SynthErrorCodes.eSynthDone;
                                Env.CurrentValue =
                                    LFOGenUpdateCycle(
                                        Env.LFOGenerator,
                                        EnvelopeUpdate(
                                            Env.Envelope,
                                            NewFrequencyHertz,
                                            SynthParams,
                                            ref error),
                                        NewFrequencyHertz,
                                        SynthParams,
                                        ref error);
                                if (error != SynthErrorCodes.eSynthDone)
                                {
                                    return error;
                                }
                            }
                            break;
                    }
                }


                error = SynthErrorCodes.eSynthDone;
                FloatTemp = (float)(State.NoteLoudnessScaling *
                    LFOGenUpdateCycle(
                        State.LoudnessLFOGenerator,
                        EnvelopeUpdate(
                            State.LoudnessEnvelope,
                            NewFrequencyHertz,
                            SynthParams,
                            ref error),
                        NewFrequencyHertz,
                        SynthParams,
                        ref error));
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }
                /* left = FloatTemp * .5 * (1 - State.Panning) */
                /* right = FloatTemp * .5 * (1 + State.Panning) */
                OneHalfVol = .5f * FloatTemp;
                LeftLoudness = OneHalfVol - OneHalfVol * State.Panning;
                RightLoudness = OneHalfVol + OneHalfVol * State.Panning;
                State.LeftLoudness = LeftLoudness;
                State.RightLoudness = RightLoudness;

                if (State.OscEffectGenerator != null)
                {
                    error = OscEffectGeneratorUpdateEnvelopes(
                        State.OscEffectGenerator,
                        NewFrequencyHertz,
                        SynthParams);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            /* fix up pre-origin time for the wave table state object */
            public void FixUpPreOrigin(
                int ActualPreOrigin)
            {
                FMSynthStateRec State = this;

                for (int i = 0; i < State.Template.NumStmts; i++)
                {
                    FMSynthOneStateRec Stmt = State.Stmts[i];

                    switch (Stmt.Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case FMSynthStmtType.eFMSynthWave:
                            {
                                FMSynthOneStateRec_Wave Wave = (FMSynthOneStateRec_Wave)Stmt.u;

                                EnvelopeStateFixUpInitialDelay(
                                    Wave.IndexEnvelope,
                                    ActualPreOrigin);
                                LFOGeneratorFixEnvelopeOrigins(
                                    Wave.IndexLFOGenerator,
                                    ActualPreOrigin);
                            }
                            break;
                        case FMSynthStmtType.eFMSynthMuladd:
                            break;
                        case FMSynthStmtType.eFMSynthEnvelope:
                            {
                                FMSynthOneStateRec_Env Env = (FMSynthOneStateRec_Env)Stmt.u;

                                EnvelopeStateFixUpInitialDelay(
                                    Env.Envelope,
                                    ActualPreOrigin);
                                LFOGeneratorFixEnvelopeOrigins(
                                    Env.LFOGenerator,
                                    ActualPreOrigin);
                            }
                            break;
                    }
                }

                EnvelopeStateFixUpInitialDelay(
                    State.LoudnessEnvelope,
                    ActualPreOrigin);
                LFOGeneratorFixEnvelopeOrigins(
                    State.LoudnessLFOGenerator,
                    ActualPreOrigin);
                LFOGeneratorFixEnvelopeOrigins(
                    State.PitchLFO,
                    ActualPreOrigin);
                if (State.OscEffectGenerator != null)
                {
                    FixUpOscEffectGeneratorPreOrigin(
                        State.OscEffectGenerator,
                        ActualPreOrigin);
                }

                State.PreStartCountdown += ActualPreOrigin;
            }

            public void KeyUpSustain1()
            {
                FMSynthStateRec State = this;

                for (int i = 0; i < State.Template.NumStmts; i++)
                {
                    FMSynthOneStateRec Stmt = State.Stmts[i];

                    switch (Stmt.Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case FMSynthStmtType.eFMSynthWave:
                            {
                                FMSynthOneStateRec_Wave Wave = (FMSynthOneStateRec_Wave)Stmt.u;

                                EnvelopeKeyUpSustain1(
                                    Wave.IndexEnvelope);
                                LFOGeneratorKeyUpSustain1(
                                    Wave.IndexLFOGenerator);
                            }
                            break;
                        case FMSynthStmtType.eFMSynthMuladd:
                            break;
                        case FMSynthStmtType.eFMSynthEnvelope:
                            {
                                FMSynthOneStateRec_Env Env = (FMSynthOneStateRec_Env)Stmt.u;

                                EnvelopeKeyUpSustain1(
                                    Env.Envelope);
                                LFOGeneratorKeyUpSustain1(
                                    Env.LFOGenerator);
                            }
                            break;
                    }
                }

                EnvelopeKeyUpSustain1(
                    State.LoudnessEnvelope);
                LFOGeneratorKeyUpSustain1(
                    State.LoudnessLFOGenerator);
                LFOGeneratorKeyUpSustain1(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain1(
                        State.OscEffectGenerator);
                }
            }

            public void KeyUpSustain2()
            {
                FMSynthStateRec State = this;

                for (int i = 0; i < State.Template.NumStmts; i++)
                {
                    FMSynthOneStateRec Stmt = State.Stmts[i];

                    switch (Stmt.Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case FMSynthStmtType.eFMSynthWave:
                            {
                                FMSynthOneStateRec_Wave Wave = (FMSynthOneStateRec_Wave)Stmt.u;

                                EnvelopeKeyUpSustain2(
                                    Wave.IndexEnvelope);
                                LFOGeneratorKeyUpSustain2(
                                    Wave.IndexLFOGenerator);
                            }
                            break;
                        case FMSynthStmtType.eFMSynthMuladd:
                            break;
                        case FMSynthStmtType.eFMSynthEnvelope:
                            {
                                FMSynthOneStateRec_Env Env = (FMSynthOneStateRec_Env)Stmt.u;

                                EnvelopeKeyUpSustain2(
                                    Env.Envelope);
                                LFOGeneratorKeyUpSustain2(
                                    Env.LFOGenerator);
                            }
                            break;
                    }
                }

                EnvelopeKeyUpSustain2(
                    State.LoudnessEnvelope);
                LFOGeneratorKeyUpSustain2(
                    State.LoudnessLFOGenerator);
                LFOGeneratorKeyUpSustain2(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain2(
                        State.OscEffectGenerator);
                }
            }

            public void KeyUpSustain3()
            {
                FMSynthStateRec State = this;

                for (int i = 0; i < State.Template.NumStmts; i++)
                {
                    FMSynthOneStateRec Stmt = State.Stmts[i];

                    switch (Stmt.Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case FMSynthStmtType.eFMSynthWave:
                            {
                                FMSynthOneStateRec_Wave Wave = (FMSynthOneStateRec_Wave)Stmt.u;

                                EnvelopeKeyUpSustain3(
                                    Wave.IndexEnvelope);
                                LFOGeneratorKeyUpSustain3(
                                    Wave.IndexLFOGenerator);
                            }
                            break;
                        case FMSynthStmtType.eFMSynthMuladd:
                            break;
                        case FMSynthStmtType.eFMSynthEnvelope:
                            {
                                FMSynthOneStateRec_Env Env = (FMSynthOneStateRec_Env)Stmt.u;

                                EnvelopeKeyUpSustain3(
                                    Env.Envelope);
                                LFOGeneratorKeyUpSustain3(
                                    Env.LFOGenerator);
                            }
                            break;
                    }
                }

                EnvelopeKeyUpSustain3(
                    State.LoudnessEnvelope);
                LFOGeneratorKeyUpSustain3(
                    State.LoudnessLFOGenerator);
                LFOGeneratorKeyUpSustain3(
                    State.PitchLFO);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain3(
                        State.OscEffectGenerator);
                }
            }

            /* restart an oscillator.  this is used for tie continuations */
            public void Restart(
                ref AccentRec NewAccents,
                double NewLoudness,
                double NewHurryUp,
                bool RetriggerEnvelopes,
                double NewStereoPosition,
                double NewInitialFrequency,
                double PitchDisplacementDepthLimit,
                double PitchDisplacementRateLimit,
                SynthParamRec SynthParams)
            {
                FMSynthStateRec State = this;

                NewStereoPosition += State.Template.StereoBias;
                if (NewStereoPosition < -1)
                {
                    NewStereoPosition = -1;
                }
                else if (NewStereoPosition > 1)
                {
                    NewStereoPosition = 1;
                }
                State.Panning = (float)NewStereoPosition;

                State.NoteLoudnessScaling = NewLoudness * State.Template.OverallOscillatorLoudness;

                for (int i = 0; i < State.Template.NumStmts; i++)
                {
                    FMSynthOneStateRec Stmt = State.Stmts[i];

                    switch (Stmt.Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case FMSynthStmtType.eFMSynthWave:
                            {
                                FMSynthOneStateRec_Wave Wave = (FMSynthOneStateRec_Wave)Stmt.u;

                                EnvelopeRetriggerFromOrigin(
                                    Wave.IndexEnvelope,
                                    ref NewAccents,
                                    NewInitialFrequency,
                                    1,
                                    NewHurryUp,
                                    RetriggerEnvelopes,
                                    SynthParams);
                                LFOGeneratorRetriggerFromOrigin(
                                    Wave.IndexLFOGenerator,
                                    ref NewAccents,
                                    NewInitialFrequency,
                                    NewHurryUp,
                                    1,
                                    1,
                                    RetriggerEnvelopes,
                                    SynthParams);
                            }
                            break;
                        case FMSynthStmtType.eFMSynthMuladd:
                            break;
                        case FMSynthStmtType.eFMSynthEnvelope:
                            {
                                FMSynthOneStateRec_Env Env = (FMSynthOneStateRec_Env)Stmt.u;

                                EnvelopeRetriggerFromOrigin(
                                    Env.Envelope,
                                    ref NewAccents,
                                    NewInitialFrequency,
                                    1,
                                    NewHurryUp,
                                    RetriggerEnvelopes,
                                    SynthParams);
                                LFOGeneratorRetriggerFromOrigin(
                                    Env.LFOGenerator,
                                    ref NewAccents,
                                    NewInitialFrequency,
                                    NewHurryUp,
                                    1,
                                    1,
                                    RetriggerEnvelopes,
                                    SynthParams);
                            }
                            break;
                    }
                }

                EnvelopeRetriggerFromOrigin(
                    State.LoudnessEnvelope,
                    ref NewAccents,
                    NewInitialFrequency,
                    1,
                    NewHurryUp,
                    RetriggerEnvelopes,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    State.LoudnessLFOGenerator,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    1,
                    1,
                    RetriggerEnvelopes,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    State.PitchLFO,
                    ref NewAccents,
                    NewInitialFrequency,
                    NewHurryUp,
                    PitchDisplacementDepthLimit,
                    PitchDisplacementRateLimit,
                    RetriggerEnvelopes,
                    SynthParams);
                if (State.OscEffectGenerator != null)
                {
                    OscEffectGeneratorRetriggerFromOrigin(
                        State.OscEffectGenerator,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        RetriggerEnvelopes,
                        SynthParams);
                }
                /* do not reset PitchLFOStartCountdown since we can't give it a proper value */
                /* to do the expected thing, and we'll be interrupting the phase of the LFO */
                /* wave generator */
            }

            /* generate a sequence of samples (called for each envelope clock) */
            public SynthErrorCodes Generate(
                int nActualFrames,
                float[] workspace,
                int RawBufferLOffset,
                int RawBufferROffset,
                int PrivateWorkspaceLOffset,
                int PrivateWorkspaceROffset,
                SynthParamRec SynthParams)
            {
                FMSynthStateRec State = this;

                if (State.PreStartCountdown <= 0)
                {
                    if (State.OscEffectGenerator == null)
                    {
                        /* normal case */
                        FMSynthGenSamplesHelper(
                            State,
                            nActualFrames,
                            workspace,
                            RawBufferLOffset,
                            RawBufferROffset);
                    }
                    else
                    {
                        /* effect postprocessing case */

                        /* initialize private storage */
                        FloatVectorZero(
                            workspace,
                            PrivateWorkspaceLOffset,
                            nActualFrames);
                        FloatVectorZero(
                            workspace,
                            PrivateWorkspaceROffset,
                            nActualFrames);

                        /* generate waveform */
                        FMSynthGenSamplesHelper(
                            State,
                            nActualFrames,
                            workspace,
                            PrivateWorkspaceLOffset,
                            PrivateWorkspaceROffset);

                        /* apply processor to it */
                        SynthErrorCodes error = ApplyOscEffectGenerator(
                            State.OscEffectGenerator,
                            workspace,
                            PrivateWorkspaceLOffset,
                            PrivateWorkspaceROffset,
                            nActualFrames,
                            SynthParams);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }

                        /* copy out data */
                        FloatVectorAcc(
                            workspace,
                            PrivateWorkspaceLOffset,
                            workspace,
                            RawBufferLOffset,
                            nActualFrames);
                        FloatVectorAcc(
                            workspace,
                            PrivateWorkspaceROffset,
                            workspace,
                            RawBufferROffset,
                            nActualFrames);
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            /* wave generation helper */
            private static void FMSynthGenSamplesHelper(
                FMSynthStateRec State,
                int nActualFrames,
                float[] workspace,
                int lOffset,
                int rOffset)
            {
                double[] Vars = State.Vars;

                /* evaluate just the optimized statements once here at the beginning */
                for (int j = 0; j < State.Template.NumStmtsOptimizable; j++)
                {
                    double Result;

                    FMSynthOneStateRec Stmt = State.Stmts[j];

#if DEBUG
                    if (Stmt.Type == FMSynthStmtType.eFMSynthWave)
                    {
                        // wave must never be optimized!
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    if ((Stmt.Type != FMSynthStmtType.eFMSynthWave)
                        && (Stmt.Type != FMSynthStmtType.eFMSynthMuladd)
                        && (Stmt.Type != FMSynthStmtType.eFMSynthEnvelope))
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
#endif

                    if (Stmt.Type == FMSynthStmtType.eFMSynthMuladd)
                    {
                        FMSynthOneStateRec_MulAdd MulAdd = (FMSynthOneStateRec_MulAdd)Stmt.u;

                        Result = Vars[MulAdd.AccumulateFrom]
                            + Vars[MulAdd.Source] * MulAdd.Factor * Vars[MulAdd.Factor2]
                            + MulAdd.Addend;
                    }
                    else /*if (Stmt.Type == FMSynthStmtType.eFMSynthEnvelope)*/
                    {
                        Debug.Assert(Stmt.Type == FMSynthStmtType.eFMSynthEnvelope);

                        FMSynthOneStateRec_Env Env = (FMSynthOneStateRec_Env)Stmt.u;

                        Result = Env.CurrentValue;
                    }

                    /* write result */
                    Vars[Stmt.Target] = Result;
                }

                /* evaluate each sample frame */
                for (int i = 0; i < nActualFrames; i++)
                {
                    /* reset output points */
                    Vars[FMSYNTH_LEFT] = 0;
                    Vars[FMSYNTH_RIGHT] = 0;

                    /* evaluate just the nonoptimized statements */
                    for (int j = State.Template.NumStmtsOptimizable; j < State.Template.NumStmts; j++)
                    {
                        double Result;

                        FMSynthOneStateRec Stmt = State.Stmts[j];

#if DEBUG
                        if ((Stmt.Type != FMSynthStmtType.eFMSynthWave)
                            && (Stmt.Type != FMSynthStmtType.eFMSynthMuladd)
                            && (Stmt.Type != FMSynthStmtType.eFMSynthEnvelope))
                        {
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }
#endif
                        if (Stmt.Type == FMSynthStmtType.eFMSynthWave)
                        {
                            FMSynthOneStateRec_Wave Wave = (FMSynthOneStateRec_Wave)Stmt.u;

                            Result = 0;
                            if (Wave.WaveTableWasDefined)
                            {
                                int LocalSamplePositionMask = Wave.FramesPerTableMinus1;

                                /* compute adjusted phase */
                                double AdjustedPhase = Wave.WaveTableSamplePosition
                                    + Wave.FloatFramesPerTable * Vars[Wave.PhaseSource] * Vars[Wave.PhaseGainSource];

                                /* compute weighting and subscript */
                                double AdjustedPhase2 = AdjustedPhase;
                                if (AdjustedPhase2 < 0)
                                {
                                    /* defeat round-toward-zero, we want always round-toward-lower */
                                    AdjustedPhase2 -= 1;
                                }
                                int IntegerAdjustedPhase = (int)AdjustedPhase2;
                                double RightWeight = AdjustedPhase - IntegerAdjustedPhase; /* fraction */
                                int ArraySubscript = IntegerAdjustedPhase & LocalSamplePositionMask;
                                int IntegerWaveTableIndex = (int)Wave.WaveTableIndex;

                                /* increment pitch differential */
                                Wave.WaveTableSamplePosition +=
                                    Wave.WaveTableSamplePositionDifferential;
                                Wave.WaveTableSamplePosition -=
                                    ((int)Wave.WaveTableSamplePosition & ~LocalSamplePositionMask);

                                /* 2-d table lookup */
                                /* first, left wavetable interpolation  L+F(R-L) */
                                float[] WaveData0 = Wave.WaveTableMatrix[IntegerWaveTableIndex];
                                float Left0Value = WaveData0[ArraySubscript];
                                float Right0Value = WaveData0[ArraySubscript + 1];
                                Result = Left0Value + (RightWeight * (Right0Value - Left0Value));
                                if ((Wave.WaveTableIndex != Wave.NumberOfTablesMinus1)
                                    && State.Template.EnableCrossWaveTableInterpolation)
                                {
                                    /* full-interpolating processing */

                                    float[] WaveData1 = Wave.WaveTableMatrix[IntegerWaveTableIndex + 1];
                                    double Wave1Weight = Wave.WaveTableIndex - IntegerWaveTableIndex;

                                    /* right wavetable interpolation and cross interpolation */
                                    float Left1Value = WaveData1[ArraySubscript];
                                    float Right1Value = WaveData1[ArraySubscript + 1];
                                    double Wave0Temp = Result;
                                    Result = Wave0Temp + (Wave1Weight * (Left1Value + (RightWeight
                                        * (Right1Value - Left1Value)) - Wave0Temp));
                                }
                                /* process at end of table, where there is no extra table for interpolation */
                            }
                        }
                        else if (Stmt.Type == FMSynthStmtType.eFMSynthMuladd)
                        {
                            FMSynthOneStateRec_MulAdd MulAdd = (FMSynthOneStateRec_MulAdd)Stmt.u;

                            Result = Vars[MulAdd.AccumulateFrom]
                                + Vars[MulAdd.Source] * MulAdd.Factor * Vars[MulAdd.Factor2]
                                + MulAdd.Addend;
                        }
                        else /*if (Stmt.Type == FMSynthStmtType.eFMSynthEnvelope)*/
                        {
                            Debug.Assert(Stmt.Type == FMSynthStmtType.eFMSynthEnvelope);

                            FMSynthOneStateRec_Env Env = (FMSynthOneStateRec_Env)Stmt.u;

                            Result = Env.CurrentValue;
                        }

                        /* write result */
                        Vars[Stmt.Target] = Result;
                    }

                    /* copy output points */
                    workspace[i + lOffset] += (float)(State.LeftLoudness * Vars[FMSYNTH_LEFT]);
                    workspace[i + rOffset] += (float)(State.RightLoudness * Vars[FMSYNTH_RIGHT]);
                }
            }

            /* find out if the wave table oscillator has finished */
            public bool IsItFinished()
            {
                FMSynthStateRec State = this;

                return IsEnvelopeAtEnd(State.LoudnessEnvelope);
            }

            /* finalize before termination */
            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                FMSynthStateRec State = this;

                if (State.OscEffectGenerator != null)
                {
                    FinalizeOscEffectGenerator(
                        State.OscEffectGenerator,
                        SynthParams,
                        writeOutputLogs);
                }

                for (int i = 0; i < State.Template.NumStmts; i++)
                {
                    FMSynthOneStateRec Stmt = State.Stmts[i];

                    switch (Stmt.Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case FMSynthStmtType.eFMSynthWave:
                            {
                                FMSynthOneStateRec_Wave Wave = (FMSynthOneStateRec_Wave)Stmt.u;

                                FreeEnvelopeStateRecord(
                                    ref Wave.IndexEnvelope,
                                    SynthParams);
                                FreeLFOGenerator(
                                    ref Wave.IndexLFOGenerator,
                                    SynthParams);
                            }
                            break;
                        case FMSynthStmtType.eFMSynthMuladd:
                            break;
                        case FMSynthStmtType.eFMSynthEnvelope:
                            {
                                FMSynthOneStateRec_Env Env = (FMSynthOneStateRec_Env)Stmt.u;

                                FreeEnvelopeStateRecord(
                                    ref Env.Envelope,
                                    SynthParams);
                                FreeLFOGenerator(
                                    ref Env.LFOGenerator,
                                    SynthParams);
                            }
                            break;
                    }
                }

                FreeEnvelopeStateRecord(
                    ref State.LoudnessEnvelope,
                    SynthParams);
                FreeLFOGenerator(
                    ref State.LoudnessLFOGenerator,
                    SynthParams);

                FreeLFOGenerator(
                    ref State.PitchLFO,
                    SynthParams);
            }
        }
    }
}
