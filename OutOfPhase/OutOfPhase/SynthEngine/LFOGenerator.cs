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
using System.Runtime.InteropServices;
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        public enum LFOArithSelect
        {
            eLFOArithAdditive,
            eLFOArithGeometric,
            eLFOArithDefault,
            eLFOArithHalfSteps,
        }

        // TODO: Convert delegates to interfaces (or make static delegates)

        public delegate double LFOGenFunctionMethod(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude);

        [StructLayout(LayoutKind.Auto)]
        public struct LFOOneStateRec
        {
            /* pointer to the LFO generating function */
            public LFOGenFunctionMethod GenFunction;

            /* phase index counter -- double to preserve precision when using really */
            /* int periods.  increment doesn't need double since it is an epsilon, but */
            /* this accumulates and tends toward 1 so it does need double. */
            public double CurrentPhase;

            /* the envelope controlling the amplitude. */
            public EvalEnvelopeRec LFOAmplitudeEnvelope;
            /* the envelope controlling the frequency.  the value from here is periods per second */
            public EvalEnvelopeRec LFOFrequencyEnvelope;
            /* envelope controlling the filter cutoff (null if !LowpassFilterEnabled) */
            public EvalEnvelopeRec LFOFilterCutoffEnvelope;
            /* the lfo augmenting the amplitude */
            public LFOGenRec LFOAmplitudeLFOGenerator;
            /* the lfo augmenting the frequency */
            public LFOGenRec LFOFrequencyLFOGenerator;
            /* the lfo augmenting the filter cutoff (null if !LowpassFilterEnabled) */
            public LFOGenRec LFOFilterCutoffLFOGenerator;

            /* low pass filter */
            public bool LowpassFilterEnabled;
            public FirstOrderLowpassRec LowpassFilter;

            /* if this is true, then modulation is linear, otherwise modulation is */
            /* exponential */
            public LFOArithSelect ModulationMode;

            /* what kind of operator are we using */
            public LFOOscTypes Operator;
            public bool OperatorIsLinearFuzz;

            /* source information for the wave table */
            public MultiWaveTableRec WaveTableSourceSelector;

            /* this is true if the wave table was defined */
            public bool WaveTableWasDefined;
            /* number of frames per table */
            public int FramesPerTable;
            /* number of tables */
            public int NumberOfTables;
            /* raw wave table data array */
            public float[][] WaveTableMatrix;
            /* envelope controlling wave table index */
            public EvalEnvelopeRec WaveTableIndexEnvelope;
            /* lfo processing the wave table index */
            public LFOGenRec WaveTableLFOGenerator;
            // current output of wave table envelope and lfo
            public double CurrentWaveTableIndex;
            // cross-table interpolation option
            public bool EnableCrossWaveTableInterpolation;

            /* modulation method */
            public LFOModulationTypes ModulationMethod;

            /* special extra value */
            public double ExtraValue;
            /* previous noise value */
            public double LeftNoise;
            /* next noise value */
            public double RightNoise;
            /* random number generator seed */
            public ParkAndMiller Seed;

            /* flag enabling sample and hold processing */
            public bool SampleAndHoldEnabled;
            /* envelope controlling sample and hold frequency */
            /* (not valid if SampleAndHoldEnabled == false) */
            public EvalEnvelopeRec LFOSampleHoldEnvelope;
            /* the lfo augmenting sample and hold frequency */
            /* (not valid if SampleAndHoldEnabled == false) */
            public LFOGenRec LFOSampleHoldLFOGenerator;
            /* phase for sample and hold generator */
            public double SampleHoldPhase;
            /* current hold value */
            public double CurrentSampleHold;

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
            // envelope generator for oscillator mode that loops repeatedly through the points of an envelope
            public EvalEnvelopeRec LoopEnvelope;
#endif
        }

        public class LFOGenRec
        {
            /* number of lfos in the vector */
            public int NumLFOs;

            /* list of single LFO entries, which we sum up. */
            public LFOOneStateRec[] LFOVector;

#if DEBUG
            public LFOGenRec()
            {
                if (!EnableFreeLists)
                {
                    GC.SuppressFinalize(this);
                }
            }

            ~LFOGenRec()
            {
                Debug.Assert(false, GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
            }
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif
        }

        /* create a new LFO generator based on a list of specifications */
        /* it returns the largest pre-origin time for all of the envelopes it contains */
        /* AmplitudeScaling and FrequencyScaling are provided primarily for frequency */
        /* LFO control; other LFOs are controlled through the accent parameters, and */
        /* should supply 1 (no scaling) for these parameters. */
        public static LFOGenRec NewLFOGenerator(
            LFOListSpecRec LFOListSpec,
            out int MaxPreOriginTime,
            ref AccentRec Accents,
            double FrequencyHertz,
            double HurryUp,
            double AmplitudeScaling,
            double FrequencyScaling,
            double FreqForMultisampling,
            ParamGetterMethod ParamGetter,
            object ParamGetterContext,
            SynthParamRec SynthParams)
        {
            int count = LFOListSpecGetNumElements(LFOListSpec);

            LFOGenRec LFOGen = New(ref SynthParams.freelists.lfoGenStateFreeList);

            // all fields must be assigned: LFOGen, LFOGen.LFOVector

            LFOGen.NumLFOs = count;
            LFOOneStateRec[] LFOVector = LFOGen.LFOVector = New(ref SynthParams.freelists.lfoGenOneFreeList, count); // cleared

            /* build the list of thingers */
            int MaxPreOrigin = 0;
            for (int i = 0; i < count; i++)
            {
                Debug.Assert(LFOVector[i].Equals(new LFOOneStateRec())); // verify cleared

                int PreOriginTime;

                LFOSpecRec OneLFOSpec = LFOListSpecGetLFOSpec(LFOListSpec, i);

                /* add frequency envelope generator */
                LFOVector[i].LFOFrequencyEnvelope = NewEnvelopeStateRecord(
                    GetLFOSpecFrequencyEnvelope(OneLFOSpec),
                    ref Accents,
                    FrequencyHertz,
                    FrequencyScaling,
                    HurryUp,
                    out PreOriginTime,
                    ParamGetter,
                    ParamGetterContext,
                    SynthParams);
                if (PreOriginTime > MaxPreOrigin)
                {
                    MaxPreOrigin = PreOriginTime;
                }

                /* determine what mode to use and calculate amplitude */
                switch (LFOSpecGetAddingMode(OneLFOSpec))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case LFOAdderMode.eLFOArithmetic:
                        LFOVector[i].ModulationMode = LFOArithSelect.eLFOArithAdditive;
                        break;
                    case LFOAdderMode.eLFOGeometric:
                        LFOVector[i].ModulationMode = LFOArithSelect.eLFOArithGeometric;
                        break;
                    case LFOAdderMode.eLFOHalfSteps:
                        LFOVector[i].ModulationMode = LFOArithSelect.eLFOArithHalfSteps;
                        break;
                }

                /* add the amplitude envelope generator */
                LFOVector[i].LFOAmplitudeEnvelope = NewEnvelopeStateRecord(
                    GetLFOSpecAmplitudeEnvelope(OneLFOSpec),
                    ref Accents,
                    FrequencyHertz,
                    AmplitudeScaling,
                    HurryUp,
                    out PreOriginTime,
                    ParamGetter,
                    ParamGetterContext,
                    SynthParams);
                if (PreOriginTime > MaxPreOrigin)
                {
                    MaxPreOrigin = PreOriginTime;
                }

                /* add the frequency lfo modulator */
                LFOVector[i].LFOFrequencyLFOGenerator = NewLFOGenerator(
                    GetLFOSpecFrequencyLFOList(OneLFOSpec),
                    out PreOriginTime,
                    ref Accents,
                    FrequencyHertz,
                    HurryUp,
                    1,
                    1/*no recursive amplitude or frequency scaling*/,
                    FreqForMultisampling,
                    ParamGetter,
                    ParamGetterContext,
                    SynthParams);
                if (PreOriginTime > MaxPreOrigin)
                {
                    MaxPreOrigin = PreOriginTime;
                }

                /* add the amplitude lfo modulator */
                LFOVector[i].LFOAmplitudeLFOGenerator = NewLFOGenerator(
                    GetLFOSpecAmplitudeLFOList(OneLFOSpec),
                    out PreOriginTime,
                    ref Accents,
                    FrequencyHertz,
                    HurryUp,
                    1,
                    1/*no recursive amplitude or frequency scaling*/,
                    FreqForMultisampling,
                    ParamGetter,
                    ParamGetterContext,
                    SynthParams);
                if (PreOriginTime > MaxPreOrigin)
                {
                    MaxPreOrigin = PreOriginTime;
                }

                /* determine what function to use */
                LFOVector[i].Operator = LFOSpecGetOscillatorType(OneLFOSpec);
                LFOVector[i].OperatorIsLinearFuzz = /* this is an optimization */
                    ((LFOVector[i].Operator == LFOOscTypes.eLFOSignedLinearFuzzTriangle)
                    || (LFOVector[i].Operator == LFOOscTypes.eLFOPositiveLinearFuzzTriangle)
                    || (LFOVector[i].Operator == LFOOscTypes.eLFOSignedLinearFuzzSquare)
                    || (LFOVector[i].Operator == LFOOscTypes.eLFOPositiveLinearFuzzSquare));
                LFOVector[i].ModulationMethod = LFOSpecGetModulationMode(OneLFOSpec);
                switch (LFOVector[i].Operator)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case LFOOscTypes.eLFOConstant1:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddConst;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultConst;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultConst;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedSine:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddSignSine;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultSignSine;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultSignSine;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveSine:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddPosSine;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultPosSine;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultPosSine;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedTriangle:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddSignTriangle;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultSignTriangle;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultSignTriangle;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveTriangle:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddPosTriangle;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultPosTriangle;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultPosTriangle;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedSquare:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddSignSquare;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultSignSquare;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultSignSquare;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveSquare:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddPosSquare;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultPosSquare;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultPosSquare;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedRamp:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddSignRamp;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultSignRamp;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultSignRamp;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveRamp:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddPosRamp;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultPosRamp;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultPosRamp;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedLinearFuzzTriangle:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddSignFuzzTriangle;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultSignFuzzTriangle;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultSignFuzzTriangle;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedLinearFuzzSquare:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddSignFuzzSquare;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultSignFuzzSquare;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultSignFuzzSquare;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveLinearFuzzTriangle:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddPosFuzzTriangle;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultPosFuzzTriangle;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultPosFuzzTriangle;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveLinearFuzzSquare:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddPosFuzzSquare;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultPosFuzzSquare;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultPosFuzzSquare;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOWaveTable:
                        switch (LFOVector[i].ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                LFOVector[i].GenFunction = _AddWaveTable;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                LFOVector[i].GenFunction = _MultWaveTable;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                LFOVector[i].GenFunction = _InvMultWaveTable;
                                break;
                        }

                        LFOVector[i].WaveTableSourceSelector = NewMultiWaveTable(
                            GetLFOSpecSampleSelector(OneLFOSpec),
                            SynthParams.perTrack.Dictionary);
                        LFOVector[i].WaveTableWasDefined = GetMultiWaveTableReference(
                            LFOVector[i].WaveTableSourceSelector,
                            FreqForMultisampling,
                            out LFOVector[i].WaveTableMatrix,
                            out LFOVector[i].FramesPerTable,
                            out LFOVector[i].NumberOfTables);

                        LFOVector[i].WaveTableIndexEnvelope = NewEnvelopeStateRecord(
                            GetLFOSpecWaveTableIndexEnvelope(OneLFOSpec),
                            ref Accents,
                            FrequencyHertz,
                            1,
                            HurryUp,
                            out PreOriginTime,
                            ParamGetter,
                            ParamGetterContext,
                            SynthParams);
                        if (PreOriginTime > MaxPreOrigin)
                        {
                            MaxPreOrigin = PreOriginTime;
                        }

                        /* add the index lfo modulator */
                        LFOVector[i].WaveTableLFOGenerator = NewLFOGenerator(
                            GetLFOSpecWaveTableIndexLFOList(OneLFOSpec),
                            out PreOriginTime,
                            ref Accents,
                            FrequencyHertz,
                            HurryUp,
                            1,
                            1/*no recursive amplitude or frequency scaling*/,
                            FreqForMultisampling,
                            ParamGetter,
                            ParamGetterContext,
                            SynthParams);
                        if (PreOriginTime > MaxPreOrigin)
                        {
                            MaxPreOrigin = PreOriginTime;
                        }

                        LFOVector[i].EnableCrossWaveTableInterpolation = LFOSpecGetEnableCrossWaveTableInterpolation(OneLFOSpec);

                        break;

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
                    case LFOOscTypes.eLFOLoopedEnvelope:
                        {
                            switch (LFOVector[i].ModulationMethod)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case LFOModulationTypes.eLFOAdditive:
                                    LFOVector[i].GenFunction = _AddLoopedEnvelope;
                                    break;
                                case LFOModulationTypes.eLFOMultiplicative:
                                    LFOVector[i].GenFunction = _MultLoopedEnvelope;
                                    break;
                                case LFOModulationTypes.eLFOInverseMultiplicative:
                                    LFOVector[i].GenFunction = _InvMultLoopedEnvelope;
                                    break;
                            }

                            // TODO: Because of the context in which the envelope is evaluated it is not parameterized
                            // by any of the usual values. It would be interesting to permit that.
                            int discardedPreOriginTime; // not used for this case
                            LFOVector[i].LoopEnvelope = NewEnvelopeStateRecord(
                                GetLFOSpecLoopedEnvelope(OneLFOSpec),
                                ref Accents,
                                Constants.MIDDLEC/*FrequencyHertz*/,
                                1/*Loudness*/,
                                1/*HurryUp*/,
                                out discardedPreOriginTime,
                                delegate(object Context, out AccentRec NoteAccents, out AccentRec TrackAccents)
                                {
                                    NoteAccents = new AccentRec();
                                    TrackAccents = new AccentRec();
                                }/*ParamGetter*/,
                                null/*ParamGetterContext*/,
                                SynthParams);
                            LFOVector[i].CurrentPhase = SynthParams.dEnvelopeRate;
                        }
                        break;
#endif
                }

                /* set up special values */
                LFOVector[i].ExtraValue = GetLFOSpecExtraValue(OneLFOSpec);
                if ((LFOVector[i].Operator == LFOOscTypes.eLFOSignedLinearFuzzTriangle)
                    || (LFOVector[i].Operator == LFOOscTypes.eLFOPositiveLinearFuzzTriangle)
                    || (LFOVector[i].Operator == LFOOscTypes.eLFOSignedLinearFuzzSquare)
                    || (LFOVector[i].Operator == LFOOscTypes.eLFOPositiveLinearFuzzSquare))
                {
                    double seed = GetLFOSpecExtraValue(OneLFOSpec);
                    LFOVector[i].Seed.ConstrainedSetSeed(unchecked((int)seed + 1/*legacy bug*/));
                    LFOVector[i].LeftNoise = ParkAndMiller.Double0Through1(LFOVector[i].Seed.Random());
                    LFOVector[i].RightNoise = ParkAndMiller.Double0Through1(LFOVector[i].Seed.Random());
                }

                /* filter */
                LFOVector[i].LowpassFilterEnabled = HasLFOSpecFilterBeenSpecified(OneLFOSpec);
                if (LFOVector[i].LowpassFilterEnabled)
                {
                    LFOVector[i].LowpassFilter = new FirstOrderLowpassRec();

                    LFOVector[i].LFOFilterCutoffEnvelope = NewEnvelopeStateRecord(
                        GetLFOSpecFilterCutoffEnvelope(OneLFOSpec),
                        ref Accents,
                        FrequencyHertz,
                        1,
                        HurryUp,
                        out PreOriginTime,
                        ParamGetter,
                        ParamGetterContext,
                        SynthParams);
                    if (PreOriginTime > MaxPreOrigin)
                    {
                        MaxPreOrigin = PreOriginTime;
                    }

                    LFOVector[i].LFOFilterCutoffLFOGenerator = NewLFOGenerator(
                        GetLFOSpecFilterCutoffLFOList(OneLFOSpec),
                        out PreOriginTime,
                        ref Accents,
                        FrequencyHertz,
                        HurryUp,
                        1,
                        1/*no recursive amplitude or frequency scaling*/,
                        FreqForMultisampling,
                        ParamGetter,
                        ParamGetterContext,
                        SynthParams);
                    if (PreOriginTime > MaxPreOrigin)
                    {
                        MaxPreOrigin = PreOriginTime;
                    }
                }

                /* sample and hold */
                LFOVector[i].SampleAndHoldEnabled = HasLFOSpecSampleHoldBeenSpecified(OneLFOSpec);
                if (LFOVector[i].SampleAndHoldEnabled)
                {
                    LFOVector[i].LFOSampleHoldEnvelope = NewEnvelopeStateRecord(
                        GetLFOSpecSampleHoldFreqEnvelope(OneLFOSpec),
                        ref Accents,
                        FrequencyHertz,
                        1,
                        HurryUp,
                        out PreOriginTime,
                        ParamGetter,
                        ParamGetterContext,
                        SynthParams);
                    if (PreOriginTime > MaxPreOrigin)
                    {
                        MaxPreOrigin = PreOriginTime;
                    }

                    LFOVector[i].LFOSampleHoldLFOGenerator = NewLFOGenerator(
                        GetLFOSpecSampleHoldFreqLFOList(OneLFOSpec),
                        out PreOriginTime,
                        ref Accents,
                        FrequencyHertz,
                        HurryUp,
                        1,
                        1/*no recursive amplitude or frequency scaling*/,
                        FreqForMultisampling,
                        ParamGetter,
                        ParamGetterContext,
                        SynthParams);
                    if (PreOriginTime > MaxPreOrigin)
                    {
                        MaxPreOrigin = PreOriginTime;
                    }
                    LFOVector[i].SampleHoldPhase = 1; /* trigger immediately */
                    LFOVector[i].CurrentSampleHold = 0;
                }
            }

            MaxPreOriginTime = MaxPreOrigin;
            return LFOGen;
        }

        public static void FreeLFOGenerator(
            ref LFOGenRec LFOGen,
            SynthParamRec SynthParams)
        {
            LFOOneStateRec[] LFOVector = LFOGen.LFOVector;
            if (unchecked((uint)LFOGen.NumLFOs > (uint)LFOVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < LFOGen.NumLFOs; i++)
            {
                FreeEnvelopeStateRecord(
                    ref LFOVector[i].LFOAmplitudeEnvelope,
                    SynthParams);
                FreeLFOGenerator(
                    ref LFOVector[i].LFOAmplitudeLFOGenerator,
                    SynthParams);

                FreeEnvelopeStateRecord(
                    ref LFOVector[i].LFOFrequencyEnvelope,
                    SynthParams);
                FreeLFOGenerator(
                    ref LFOVector[i].LFOFrequencyLFOGenerator,
                    SynthParams);

                if (LFOVector[i].Operator == LFOOscTypes.eLFOWaveTable)
                {
                    FreeEnvelopeStateRecord(
                        ref LFOVector[i].WaveTableIndexEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref LFOVector[i].WaveTableLFOGenerator,
                        SynthParams);
                }

                if (LFOVector[i].LowpassFilterEnabled)
                {
                    FreeEnvelopeStateRecord(
                        ref LFOVector[i].LFOFilterCutoffEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref LFOVector[i].LFOFilterCutoffLFOGenerator,
                        SynthParams);
                }

                if (LFOVector[i].SampleAndHoldEnabled)
                {
                    FreeEnvelopeStateRecord(
                        ref LFOVector[i].LFOSampleHoldEnvelope,
                        SynthParams);
                    FreeLFOGenerator(
                        ref LFOVector[i].LFOSampleHoldLFOGenerator,
                        SynthParams);
                }
            }

            Free(ref SynthParams.freelists.lfoGenOneFreeList, ref LFOGen.LFOVector);
            Free(ref SynthParams.freelists.lfoGenStateFreeList, ref LFOGen);
        }

        /* fix up the origin time so that envelopes start at the proper times */
        public static void LFOGeneratorFixEnvelopeOrigins(
            LFOGenRec LFOGen,
            int ActualPreOriginTime)
        {
            LFOOneStateRec[] LFOVector = LFOGen.LFOVector;
            if (unchecked((uint)LFOGen.NumLFOs > (uint)LFOVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < LFOGen.NumLFOs; i++)
            {
                EnvelopeStateFixUpInitialDelay(
                    LFOVector[i].LFOAmplitudeEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    LFOVector[i].LFOFrequencyEnvelope,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    LFOVector[i].LFOAmplitudeLFOGenerator,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    LFOVector[i].LFOFrequencyLFOGenerator,
                    ActualPreOriginTime);
                if (LFOVector[i].Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeStateFixUpInitialDelay(
                        LFOVector[i].WaveTableIndexEnvelope,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        LFOVector[i].WaveTableLFOGenerator,
                        ActualPreOriginTime);
                }
                if (LFOVector[i].LowpassFilterEnabled)
                {
                    EnvelopeStateFixUpInitialDelay(
                        LFOVector[i].LFOFilterCutoffEnvelope,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        LFOVector[i].LFOFilterCutoffLFOGenerator,
                        ActualPreOriginTime);
                }
                if (LFOVector[i].SampleAndHoldEnabled)
                {
                    EnvelopeStateFixUpInitialDelay(
                        LFOVector[i].LFOSampleHoldEnvelope,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        LFOVector[i].LFOSampleHoldLFOGenerator,
                        ActualPreOriginTime);
                }

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
                if (Scan.LoopEnvelope != null)
                {
                    // TODO:review

                    // This is a bit of a hack - if using looped-envelope oscillator, adjust the phase of the lfo generator
                    // so that the envelope is starting at the overall event origin - so sequenced rhythmic effeccts that
                    // match tempo will work right. (For other cases alignment wouldn't matter.)
                    // Note that it won't work unless the frequency envelope is a constant.

                    double envelopeRate = Scan.CurrentPhase; // initialized to that value in the constructor
                    LFOVector[i].CurrentPhase = -EnvelopeInitialValue(Scan.LFOFrequencyEnvelope) * ActualPreOriginTime / envelopeRate;
                    LFOVector[i].CurrentPhase = Scan.CurrentPhase - Math.Floor(Scan.CurrentPhase);

                    // prevent envelope generator from running before event origin.
                    EnvelopeStateFixUpInitialDelay(
                        LFOVector[i].LoopEnvelope,
                        ActualPreOriginTime);

                    // TODO: ought to advance the phase of the envelope generator to the right spot
                }
#endif
            }
        }

        /* this function computes one LFO cycle and returns the value from the LFO generator. */
        /* it should be called on the envelope clock. */
        public static double LFOGenUpdateCycle(
            LFOGenRec LFOGen,
            double OriginalValue,
            double OscillatorFrequency,
            SynthParamRec SynthParams,
            ref SynthErrorCodes ErrorRef)
        {
            if (ErrorRef != SynthErrorCodes.eSynthDone)
            {
                return 0;
            }

            LFOOneStateRec[] LFOVector = LFOGen.LFOVector;
            if (unchecked((uint)LFOGen.NumLFOs > (uint)LFOVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < LFOGen.NumLFOs; i++)
            {
                /* compute amplitude envelope/lfo thing */
                SynthErrorCodes error = SynthErrorCodes.eSynthDone;
                double VariantAmplitude = LFOGenUpdateCycle(
                    LFOVector[i].LFOAmplitudeLFOGenerator,
                    EnvelopeUpdate(
                        LFOVector[i].LFOAmplitudeEnvelope,
                        OscillatorFrequency,
                        SynthParams,
                        ref error),
                    OscillatorFrequency,
                    SynthParams,
                    ref error);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    ErrorRef = error;
                    return 0;
                }

                // specialized additional parameters for certain types
                if (LFOVector[i].Operator == LFOOscTypes.eLFOWaveTable)
                {
                    LFOVector[i].CurrentWaveTableIndex = LFOGenUpdateCycle(
                        LFOVector[i].WaveTableLFOGenerator,
                        EnvelopeUpdate(
                            LFOVector[i].WaveTableIndexEnvelope,
                            OscillatorFrequency,
                            SynthParams,
                            ref error),
                        OscillatorFrequency,
                        SynthParams,
                        ref error);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        ErrorRef = error;
                        return 0;
                    }
                }

                /* perform the calculations */
#if DEBUG
                if ((LFOVector[i].ModulationMode != LFOArithSelect.eLFOArithAdditive)
                    && (LFOVector[i].ModulationMode != LFOArithSelect.eLFOArithGeometric)
                    && (LFOVector[i].ModulationMode != LFOArithSelect.eLFOArithHalfSteps))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
                if (LFOVector[i].ModulationMode == LFOArithSelect.eLFOArithAdditive)
                {
                    OriginalValue = LFOVector[i].GenFunction(
                        ref LFOVector[i],
                        OriginalValue,
                        VariantAmplitude);
                }
                else
                {
                    bool Sign = (OriginalValue < 0);
                    if (Sign)
                    {
                        OriginalValue = -OriginalValue;
                    }
                    if (OriginalValue != 0)
                    {
                        double ScalingConstant;

                        if (LFOVector[i].ModulationMode == LFOArithSelect.eLFOArithGeometric)
                        {
                            /* the LOG2 is to normalize the values, so that 1/12 will */
                            /* be 1 halfstep */
                            ScalingConstant = Constants.LOG2;
                        }
                        else /* if (LFOVector[i].ModulationMode == eLFOArithHalfSteps) */
                        {
                            /* this one means 1 is a halfstep */
                            ScalingConstant = Constants.LOG2 / 12;
                        }
                        OriginalValue = Math.Exp(
                            LFOVector[i].GenFunction(
                                ref LFOVector[i],
                                Math.Log(OriginalValue),
                                VariantAmplitude * ScalingConstant));
                    }
                    if (Sign)
                    {
                        OriginalValue = -OriginalValue;
                    }
                }

                /* apply sample and hold */
                if (LFOVector[i].SampleAndHoldEnabled)
                {
                    /* update sample/hold phase generator */
                    error = SynthErrorCodes.eSynthDone;
                    double SampleHoldPhase = LFOVector[i].SampleHoldPhase;
                    SampleHoldPhase = SampleHoldPhase +
                        LFOGenUpdateCycle(
                            LFOVector[i].LFOSampleHoldLFOGenerator,
                            EnvelopeUpdate(
                                LFOVector[i].LFOSampleHoldEnvelope,
                                OscillatorFrequency,
                                SynthParams,
                                ref error),
                            OscillatorFrequency,
                            SynthParams,
                            ref error)
                        / SynthParams.dEnvelopeRate;
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        ErrorRef = error;
                        return 0;
                    }
                    /* if phase exceeds limit, then update held value to current */
                    if (SampleHoldPhase >= 1)
                    {
                        SampleHoldPhase -= Math.Floor(SampleHoldPhase);
                        LFOVector[i].CurrentSampleHold = OriginalValue;
                    }
                    LFOVector[i].SampleHoldPhase = SampleHoldPhase;
                    /* set current value to held value */
                    OriginalValue = LFOVector[i].CurrentSampleHold;
                }

                /* apply lowpass filter */
                if (LFOVector[i].LowpassFilterEnabled)
                {
                    error = SynthErrorCodes.eSynthDone;
                    FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                        LFOVector[i].LowpassFilter,
                        LFOGenUpdateCycle(
                            LFOVector[i].LFOFilterCutoffLFOGenerator,
                            EnvelopeUpdate(
                                LFOVector[i].LFOFilterCutoffEnvelope,
                                OscillatorFrequency,
                                SynthParams,
                                ref error),
                            OscillatorFrequency,
                            SynthParams,
                            ref error),
                        SynthParams.dEnvelopeRate);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        ErrorRef = error;
                        return 0;
                    }
                    OriginalValue = FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                        LFOVector[i].LowpassFilter,
                        (float)OriginalValue);
                }

                /* update phase of oscillator */
                error = SynthErrorCodes.eSynthDone;
                LFOVector[i].CurrentPhase = LFOVector[i].CurrentPhase +
                    LFOGenUpdateCycle(
                        LFOVector[i].LFOFrequencyLFOGenerator,
                        EnvelopeUpdate(
                            LFOVector[i].LFOFrequencyEnvelope,
                            OscillatorFrequency,
                            SynthParams,
                            ref error),
                        OscillatorFrequency,
                        SynthParams,
                        ref error)
                    / SynthParams.dEnvelopeRate;
                if (error != SynthErrorCodes.eSynthDone)
                {
                    ErrorRef = error;
                    return 0;
                }

                /* generate next random point for fuzz lfos */
#if DEBUG
                if (
                    !(
                        (LFOVector[i].OperatorIsLinearFuzz &&
                            ((LFOVector[i].Operator == LFOOscTypes.eLFOSignedLinearFuzzTriangle)
                                || (LFOVector[i].Operator == LFOOscTypes.eLFOPositiveLinearFuzzTriangle)
                                || (LFOVector[i].Operator == LFOOscTypes.eLFOSignedLinearFuzzSquare)
                                || (LFOVector[i].Operator == LFOOscTypes.eLFOPositiveLinearFuzzSquare)))
                        ||
                        (!LFOVector[i].OperatorIsLinearFuzz &&
                            !((LFOVector[i].Operator == LFOOscTypes.eLFOSignedLinearFuzzTriangle)
                                || (LFOVector[i].Operator == LFOOscTypes.eLFOPositiveLinearFuzzTriangle)
                                || (LFOVector[i].Operator == LFOOscTypes.eLFOSignedLinearFuzzSquare)
                                || (LFOVector[i].Operator == LFOOscTypes.eLFOPositiveLinearFuzzSquare)))))
                {
                    // Operator and OperatorIsLinearFuzz are inconsistent
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
                if (LFOVector[i].OperatorIsLinearFuzz)
                {
                    for (int Counter = (int)(Math.Floor(LFOVector[i].CurrentPhase)); Counter >= 1; Counter -= 1)
                    {
                        LFOVector[i].LeftNoise = LFOVector[i].RightNoise;
                        LFOVector[i].RightNoise = ParkAndMiller.Double0Through1(LFOVector[i].Seed.Random());
                    }
                }

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
                if (LFOVector[i].LoopEnvelope != null)
                {
                    // For looped envelopes, retriggering is keyed off of the phase, which requires the user to specify
                    // the period for the oscillator. This complication avoids needing to implement a high-precision envelope
                    // generator that can stay in sync with the note event clock over long periods of time.
                    if (LFOVector[i].CurrentPhase >= 1)
                    {
                        // TODO: Because of the context in which the envelope is evaluated it is not parameterized
                        // by any of the usual values. It would be interesting to permit that.
                        AccentRec zero = new AccentRec();
                        EnvelopeRetriggerFromOrigin(
                            LFOVector[i].LoopEnvelope,
                            ref zero,
                            Constants.MIDDLEC/*FrequencyHertz*/,
                            1/*Loudness*/,
                            1/*HurryUp*/,
                            true/*ActuallyRetrigger*/,
                            SynthParams);
                    }
                }
#endif

                /* wrap phase */
                LFOVector[i].CurrentPhase = LFOVector[i].CurrentPhase - Math.Floor(LFOVector[i].CurrentPhase);
#if DEBUG
                if (LFOVector[i].CurrentPhase >= 1)
                {
                    // phase has integer component
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
            }

            return OriginalValue;
        }

        // like LFOGenUpdateCycle() except phase and envelopes do not advance
        public static double LFOGenInitialValue(
            LFOGenRec LFOGen,
            double OriginalValue)
        {
            LFOOneStateRec[] LFOVector = LFOGen.LFOVector;
            if (unchecked((uint)LFOGen.NumLFOs > (uint)LFOVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < LFOGen.NumLFOs; i++)
            {
                /* compute amplitude envelope/lfo thing */
                double VariantAmplitude = LFOGenInitialValue(
                    LFOVector[i].LFOAmplitudeLFOGenerator,
                    EnvelopeInitialValue(
                        LFOVector[i].LFOAmplitudeEnvelope));

                // specialized additional parameters for certain types
                if (LFOVector[i].Operator == LFOOscTypes.eLFOWaveTable)
                {
                    // WARNING: CurrentWaveTableIndex is updated here for the benefit of the GenFunction() below, but
                    // should be safe beacuse it will be regenerated fresh in the next LFOGenUpdateCycle() 
                    LFOVector[i].CurrentWaveTableIndex = LFOGenInitialValue(
                        LFOVector[i].WaveTableLFOGenerator,
                        EnvelopeInitialValue(
                            LFOVector[i].WaveTableIndexEnvelope));
                }

                /* perform the calculations */
#if DEBUG
                if ((LFOVector[i].ModulationMode != LFOArithSelect.eLFOArithAdditive)
                    && (LFOVector[i].ModulationMode != LFOArithSelect.eLFOArithGeometric)
                    && (LFOVector[i].ModulationMode != LFOArithSelect.eLFOArithHalfSteps))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
                if (LFOVector[i].ModulationMode == LFOArithSelect.eLFOArithAdditive)
                {
                    OriginalValue = LFOVector[i].GenFunction(
                        ref LFOVector[i],
                        OriginalValue,
                        VariantAmplitude);
                }
                else
                {
                    bool Sign = (OriginalValue < 0);
                    if (Sign)
                    {
                        OriginalValue = -OriginalValue;
                    }
                    if (OriginalValue != 0)
                    {
                        double ScalingConstant;

                        if (LFOVector[i].ModulationMode == LFOArithSelect.eLFOArithGeometric)
                        {
                            /* the LOG2 is to normalize the values, so that 1/12 will */
                            /* be 1 halfstep */
                            ScalingConstant = Constants.LOG2;
                        }
                        else /* if (LFOVector[i].ModulationMode == eLFOArithHalfSteps) */
                        {
                            /* this one means 1 is a halfstep */
                            ScalingConstant = Constants.LOG2 / 12;
                        }
                        OriginalValue = Math.Exp(
                            LFOVector[i].GenFunction(
                                ref LFOVector[i],
                                Math.Log(OriginalValue),
                                VariantAmplitude * ScalingConstant));
                    }
                    if (Sign)
                    {
                        OriginalValue = -OriginalValue;
                    }
                }
            }

            return OriginalValue;
        }

        /* pass the key-up impulse on to the envelopes contained inside */
        public static void LFOGeneratorKeyUpSustain1(LFOGenRec LFOGen)
        {
            LFOOneStateRec[] LFOVector = LFOGen.LFOVector;
            if (unchecked((uint)LFOGen.NumLFOs > (uint)LFOVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < LFOGen.NumLFOs; i++)
            {
                EnvelopeKeyUpSustain1(
                    LFOVector[i].LFOAmplitudeEnvelope);
                EnvelopeKeyUpSustain1(
                    LFOVector[i].LFOFrequencyEnvelope);
                LFOGeneratorKeyUpSustain1(
                    LFOVector[i].LFOAmplitudeLFOGenerator);
                LFOGeneratorKeyUpSustain1(
                    LFOVector[i].LFOFrequencyLFOGenerator);
                if (LFOVector[i].Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeKeyUpSustain1(
                        LFOVector[i].WaveTableIndexEnvelope);
                    LFOGeneratorKeyUpSustain1(
                        LFOVector[i].WaveTableLFOGenerator);
                }
                if (LFOVector[i].LowpassFilterEnabled)
                {
                    EnvelopeKeyUpSustain1(
                        LFOVector[i].LFOFilterCutoffEnvelope);
                    LFOGeneratorKeyUpSustain1(
                        LFOVector[i].LFOFilterCutoffLFOGenerator);
                }
                if (LFOVector[i].SampleAndHoldEnabled)
                {
                    EnvelopeKeyUpSustain1(
                        LFOVector[i].LFOSampleHoldEnvelope);
                    LFOGeneratorKeyUpSustain1(
                        LFOVector[i].LFOSampleHoldLFOGenerator);
                }
            }
        }

        /* pass the key-up impulse on to the envelopes contained inside */
        public static void LFOGeneratorKeyUpSustain2(LFOGenRec LFOGen)
        {
            LFOOneStateRec[] LFOVector = LFOGen.LFOVector;
            if (unchecked((uint)LFOGen.NumLFOs > (uint)LFOVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < LFOGen.NumLFOs; i++)
            {
                EnvelopeKeyUpSustain2(
                    LFOVector[i].LFOAmplitudeEnvelope);
                EnvelopeKeyUpSustain2(
                    LFOVector[i].LFOFrequencyEnvelope);
                LFOGeneratorKeyUpSustain2(
                    LFOVector[i].LFOAmplitudeLFOGenerator);
                LFOGeneratorKeyUpSustain2(
                    LFOVector[i].LFOFrequencyLFOGenerator);
                if (LFOVector[i].Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeKeyUpSustain2(
                        LFOVector[i].WaveTableIndexEnvelope);
                    LFOGeneratorKeyUpSustain2(
                        LFOVector[i].WaveTableLFOGenerator);
                }
                if (LFOVector[i].LowpassFilterEnabled)
                {
                    EnvelopeKeyUpSustain2(
                        LFOVector[i].LFOFilterCutoffEnvelope);
                    LFOGeneratorKeyUpSustain2(
                        LFOVector[i].LFOFilterCutoffLFOGenerator);
                }
                if (LFOVector[i].SampleAndHoldEnabled)
                {
                    EnvelopeKeyUpSustain2(
                        LFOVector[i].LFOSampleHoldEnvelope);
                    LFOGeneratorKeyUpSustain2(
                        LFOVector[i].LFOSampleHoldLFOGenerator);
                }
            }
        }

        /* pass the key-up impulse on to the envelopes contained inside */
        public static void LFOGeneratorKeyUpSustain3(LFOGenRec LFOGen)
        {
            LFOOneStateRec[] LFOVector = LFOGen.LFOVector;
            if (unchecked((uint)LFOGen.NumLFOs > (uint)LFOVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < LFOGen.NumLFOs; i++)
            {
                EnvelopeKeyUpSustain3(
                    LFOVector[i].LFOAmplitudeEnvelope);
                EnvelopeKeyUpSustain3(
                    LFOVector[i].LFOFrequencyEnvelope);
                LFOGeneratorKeyUpSustain3(
                    LFOVector[i].LFOAmplitudeLFOGenerator);
                LFOGeneratorKeyUpSustain3(
                    LFOVector[i].LFOFrequencyLFOGenerator);
                if (LFOVector[i].Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeKeyUpSustain3(
                        LFOVector[i].WaveTableIndexEnvelope);
                    LFOGeneratorKeyUpSustain3(
                        LFOVector[i].WaveTableLFOGenerator);
                }
                if (LFOVector[i].LowpassFilterEnabled)
                {
                    EnvelopeKeyUpSustain3(
                        LFOVector[i].LFOFilterCutoffEnvelope);
                    LFOGeneratorKeyUpSustain3(
                        LFOVector[i].LFOFilterCutoffLFOGenerator);
                }
                if (LFOVector[i].SampleAndHoldEnabled)
                {
                    EnvelopeKeyUpSustain3(
                        LFOVector[i].LFOSampleHoldEnvelope);
                    LFOGeneratorKeyUpSustain3(
                        LFOVector[i].LFOSampleHoldLFOGenerator);
                }
            }
        }

        /* retrigger envelopes from the origin point */
        public static void LFOGeneratorRetriggerFromOrigin(
            LFOGenRec LFOGen,
            ref AccentRec Accents,
            double FrequencyHertz,
            double HurryUp,
            double AmplitudeScaling,
            double FrequencyScaling,
            bool ActuallyRetrigger,
            SynthParamRec SynthParams)
        {
            LFOOneStateRec[] LFOVector = LFOGen.LFOVector;
            if (unchecked((uint)LFOGen.NumLFOs > (uint)LFOVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < LFOGen.NumLFOs; i++)
            {
                EnvelopeRetriggerFromOrigin(
                    LFOVector[i].LFOAmplitudeEnvelope,
                    ref Accents,
                    FrequencyHertz,
                    AmplitudeScaling,
                    HurryUp,
                    ActuallyRetrigger,
                    SynthParams);
                EnvelopeRetriggerFromOrigin(
                    LFOVector[i].LFOFrequencyEnvelope,
                    ref Accents,
                    FrequencyHertz,
                    FrequencyScaling,
                    HurryUp,
                    ActuallyRetrigger,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    LFOVector[i].LFOAmplitudeLFOGenerator,
                    ref Accents,
                    FrequencyHertz,
                    HurryUp,
                    1,
                    1/*no recursive scaling*/,
                    ActuallyRetrigger,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    LFOVector[i].LFOFrequencyLFOGenerator,
                    ref Accents,
                    FrequencyHertz,
                    HurryUp,
                    1,
                    1/*no recursive scaling*/,
                    ActuallyRetrigger,
                    SynthParams);
                if (LFOVector[i].Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeRetriggerFromOrigin(
                        LFOVector[i].WaveTableIndexEnvelope,
                        ref Accents,
                        FrequencyHertz,
                        FrequencyScaling,
                        HurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        LFOVector[i].WaveTableLFOGenerator,
                        ref Accents,
                        FrequencyHertz,
                        HurryUp,
                        1,
                        1/*no recursive scaling*/,
                        ActuallyRetrigger,
                        SynthParams);
                }
                if (LFOVector[i].LowpassFilterEnabled)
                {
                    EnvelopeRetriggerFromOrigin(
                        LFOVector[i].LFOFilterCutoffEnvelope,
                        ref Accents,
                        FrequencyHertz,
                        FrequencyScaling,
                        HurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        LFOVector[i].LFOFilterCutoffLFOGenerator,
                        ref Accents,
                        FrequencyHertz,
                        HurryUp,
                        1,
                        1/*no recursive scaling*/,
                        ActuallyRetrigger,
                        SynthParams);
                }
                if (LFOVector[i].SampleAndHoldEnabled)
                {
                    EnvelopeRetriggerFromOrigin(
                        LFOVector[i].LFOSampleHoldEnvelope,
                        ref Accents,
                        FrequencyHertz,
                        FrequencyScaling,
                        HurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        LFOVector[i].LFOSampleHoldLFOGenerator,
                        ref Accents,
                        FrequencyHertz,
                        HurryUp,
                        1,
                        1/*no recursive scaling*/,
                        ActuallyRetrigger,
                        SynthParams);
                }
            }
        }

        /* find out if LFO generator has started yet */
        public static bool HasLFOGeneratorStarted(LFOGenRec LFOGen)
        {
            LFOOneStateRec[] LFOVector = LFOGen.LFOVector;
            if (unchecked((uint)LFOGen.NumLFOs > (uint)LFOVector.Length))
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < LFOGen.NumLFOs; i++)
            {
                if (LFOVector[i].Operator == LFOOscTypes.eLFOWaveTable)
                {
                    if (HasEnvelopeStartedYet(LFOVector[i].WaveTableIndexEnvelope))
                    {
                        return true;
                    }
                    if (HasLFOGeneratorStarted(LFOVector[i].WaveTableLFOGenerator))
                    {
                        return true;
                    }
                }
                if (LFOVector[i].LowpassFilterEnabled)
                {
                    if (HasEnvelopeStartedYet(LFOVector[i].LFOFilterCutoffEnvelope))
                    {
                        return true;
                    }
                    if (HasLFOGeneratorStarted(LFOVector[i].LFOFilterCutoffLFOGenerator))
                    {
                        return true;
                    }
                }
                if (LFOVector[i].SampleAndHoldEnabled)
                {
                    if (HasEnvelopeStartedYet(LFOVector[i].LFOSampleHoldEnvelope))
                    {
                        return true;
                    }
                    if (HasLFOGeneratorStarted(LFOVector[i].LFOSampleHoldLFOGenerator))
                    {
                        return true;
                    }
                }
                if (HasEnvelopeStartedYet(LFOVector[i].LFOAmplitudeEnvelope))
                {
                    return true;
                }
                if (HasEnvelopeStartedYet(LFOVector[i].LFOFrequencyEnvelope))
                {
                    return true;
                }
                if (HasLFOGeneratorStarted(LFOVector[i].LFOAmplitudeLFOGenerator))
                {
                    return true;
                }
                if (HasLFOGeneratorStarted(LFOVector[i].LFOFrequencyLFOGenerator))
                {
                    return true;
                }
            }

            return false;
        }

        // Used by envelope smoothing feature to determine if lfo gen discretization should be preserved
        public static bool IsLFOSampleAndHold(LFOGenRec LFOGen)
        {
            return (LFOGen.NumLFOs > 0)
                && LFOGen.LFOVector[LFOGen.NumLFOs - 1].SampleAndHoldEnabled
                && !LFOGen.LFOVector[LFOGen.NumLFOs - 1].LowpassFilterEnabled;
        }

        private readonly static LFOGenFunctionMethod _AddConst = AddConst;
        private static double AddConst(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue + Amplitude;
        }

        private readonly static LFOGenFunctionMethod _AddSignSine = AddSignSine;
        private static double AddSignSine(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue + Amplitude * Math.Sin(State.CurrentPhase * 2 * Math.PI);
        }

        private readonly static LFOGenFunctionMethod _AddPosSine = AddPosSine;
        private static double AddPosSine(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue + Amplitude * 0.5 * (1 + Math.Sin(State.CurrentPhase * 2 * Math.PI));
        }

        private readonly static LFOGenFunctionMethod _AddSignTriangle = AddSignTriangle;
        private static double AddSignTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Temp;

            if (State.CurrentPhase < 0.25)
            {
                Temp = State.CurrentPhase;
            }
            else if (State.CurrentPhase < 0.75)
            {
                Temp = 0.5 - State.CurrentPhase;
            }
            else
            {
                Temp = State.CurrentPhase - 1;
            }
            return OriginalValue + Temp * 4 * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _AddPosTriangle = AddPosTriangle;
        private static double AddPosTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Temp;

            if (State.CurrentPhase < 0.25)
            {
                Temp = State.CurrentPhase;
            }
            else if (State.CurrentPhase < 0.75)
            {
                Temp = 0.5 - State.CurrentPhase;
            }
            else
            {
                Temp = State.CurrentPhase - 1;
            }
            return OriginalValue + (Temp + 0.25) * 2 * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _AddSignSquare = AddSignSquare;
        private static double AddSignSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Peak1;
            double Peak2;
            double Trough1;
            double Trough2;

            Peak1 = State.ExtraValue * 0.25;
            if (State.CurrentPhase < Peak1)
            {
                return OriginalValue + Amplitude * (State.CurrentPhase / Peak1);
            }
            Peak2 = Peak1 + (0.5 - State.ExtraValue * 0.5);
            if (State.CurrentPhase < Peak2)
            {
                return OriginalValue + Amplitude;
            }
            Trough1 = Peak2 + State.ExtraValue * 0.5;
            if (State.CurrentPhase < Trough1)
            {
                return OriginalValue + Amplitude * ((Trough1 - State.CurrentPhase) / Peak1 - 1);
            }
            Trough2 = 1 - State.ExtraValue * 0.25;
            if (State.CurrentPhase < Trough2)
            {
                return OriginalValue - Amplitude;
            }
            return OriginalValue + Amplitude * ((State.CurrentPhase - Trough2) / (State.ExtraValue * 0.25) - 1);
        }

        private readonly static LFOGenFunctionMethod _AddPosSquare = AddPosSquare;
        private static double AddPosSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Peak1;
            double Peak2;
            double Trough1;
            double Trough2;

            Peak1 = State.ExtraValue * 0.25;
            if (State.CurrentPhase < Peak1)
            {
                return OriginalValue + Amplitude * 0.5 * (1 + (State.CurrentPhase / Peak1));
            }
            Peak2 = Peak1 + (0.5 - State.ExtraValue * 0.5);
            if (State.CurrentPhase < Peak2)
            {
                return OriginalValue + Amplitude;
            }
            Trough1 = Peak2 + State.ExtraValue * 0.5;
            if (State.CurrentPhase < Trough1)
            {
                return OriginalValue + Amplitude * 0.5 * (1 + ((Trough1 - State.CurrentPhase) / Peak1 - 1));
            }
            Trough2 = 1 - State.ExtraValue * 0.25;
            if (State.CurrentPhase < Trough2)
            {
                return OriginalValue;
            }
            return OriginalValue + Amplitude * 0.5 * (1 + ((State.CurrentPhase - Trough2) / (State.ExtraValue * 0.25) - 1));
        }

        private readonly static LFOGenFunctionMethod _AddSignRamp = AddSignRamp;
        private static double AddSignRamp(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            if (State.CurrentPhase < State.ExtraValue)
            {
                return OriginalValue + Amplitude * (1 - 2 * (State.CurrentPhase / State.ExtraValue));
            }
            else
            {
                return OriginalValue + Amplitude * (2 * ((State.CurrentPhase - State.ExtraValue) / (1 - State.ExtraValue)) - 1);
            }
        }

        private readonly static LFOGenFunctionMethod _AddPosRamp = AddPosRamp;
        private static double AddPosRamp(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            if (State.CurrentPhase < State.ExtraValue)
            {
                return OriginalValue + Amplitude * (1 - State.CurrentPhase / State.ExtraValue);
            }
            else
            {
                return OriginalValue + Amplitude * ((State.CurrentPhase - State.ExtraValue) / (1 - State.ExtraValue));
            }
        }

        private readonly static LFOGenFunctionMethod _AddSignFuzzTriangle = AddSignFuzzTriangle;
        private static double AddSignFuzzTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue + (2 * ReturnValue - 1) * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _AddSignFuzzSquare = AddSignFuzzSquare;
        private static double AddSignFuzzSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue + (2 * ReturnValue - 1) * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _AddPosFuzzTriangle = AddPosFuzzTriangle;
        private static double AddPosFuzzTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue + ReturnValue * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _AddPosFuzzSquare = AddPosFuzzSquare;
        private static double AddPosFuzzSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue + ReturnValue * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _MultConst = MultConst;
        private static double MultConst(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _MultSignSine = MultSignSine;
        private static double MultSignSine(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * Amplitude * Math.Sin(State.CurrentPhase * 2 * Math.PI);
        }

        private readonly static LFOGenFunctionMethod _MultPosSine = MultPosSine;
        private static double MultPosSine(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * Amplitude * 0.5 * (1 + Math.Sin(State.CurrentPhase * 2 * Math.PI));
        }

        private readonly static LFOGenFunctionMethod _MultSignTriangle = MultSignTriangle;
        private static double MultSignTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Temp;

            if (State.CurrentPhase < 0.25)
            {
                Temp = State.CurrentPhase;
            }
            else if (State.CurrentPhase < 0.75)
            {
                Temp = 0.5 - State.CurrentPhase;
            }
            else
            {
                Temp = State.CurrentPhase - 1;
            }
            return OriginalValue * Temp * 4 * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _MultPosTriangle = MultPosTriangle;
        private static double MultPosTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Temp;

            if (State.CurrentPhase < 0.25)
            {
                Temp = State.CurrentPhase;
            }
            else if (State.CurrentPhase < 0.75)
            {
                Temp = 0.5 - State.CurrentPhase;
            }
            else
            {
                Temp = State.CurrentPhase - 1;
            }
            return OriginalValue * (Temp + 0.25) * 2 * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _MultSignSquare = MultSignSquare;
        private static double MultSignSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Peak1;
            double Peak2;
            double Trough1;
            double Trough2;

            Peak1 = State.ExtraValue * 0.25;
            if (State.CurrentPhase < Peak1)
            {
                return OriginalValue * Amplitude * (State.CurrentPhase / Peak1);
            }
            Peak2 = Peak1 + (0.5 - State.ExtraValue * 0.5);
            if (State.CurrentPhase < Peak2)
            {
                return OriginalValue * Amplitude;
            }
            Trough1 = Peak2 + State.ExtraValue * 0.5;
            if (State.CurrentPhase < Trough1)
            {
                return OriginalValue * Amplitude * ((Trough1 - State.CurrentPhase) / Peak1 - 1);
            }
            Trough2 = 1 - State.ExtraValue * 0.25;
            if (State.CurrentPhase < Trough2)
            {
                return OriginalValue * -Amplitude;
            }
            return OriginalValue * Amplitude * ((State.CurrentPhase - Trough2) / (State.ExtraValue * 0.25) - 1);
        }

        private readonly static LFOGenFunctionMethod _MultPosSquare = MultPosSquare;
        private static double MultPosSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Peak1;
            double Peak2;
            double Trough1;
            double Trough2;

            Peak1 = State.ExtraValue * 0.25;
            if (State.CurrentPhase < Peak1)
            {
                return OriginalValue * Amplitude * 0.5 * (1 + (State.CurrentPhase / Peak1));
            }
            Peak2 = Peak1 + (0.5 - State.ExtraValue * 0.5);
            if (State.CurrentPhase < Peak2)
            {
                return OriginalValue * Amplitude;
            }
            Trough1 = Peak2 + State.ExtraValue * 0.5;
            if (State.CurrentPhase < Trough1)
            {
                return OriginalValue * Amplitude * 0.5 * (1 + ((Trough1 - State.CurrentPhase) / Peak1 - 1));
            }
            Trough2 = 1 - State.ExtraValue * 0.25;
            if (State.CurrentPhase < Trough2)
            {
                return 0;
            }
            return OriginalValue * Amplitude * 0.5 * (1 + ((State.CurrentPhase - Trough2) / (State.ExtraValue * 0.25) - 1));
        }

        private readonly static LFOGenFunctionMethod _MultSignRamp = MultSignRamp;
        private static double MultSignRamp(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            if (State.CurrentPhase < State.ExtraValue)
            {
                return OriginalValue * Amplitude * (1 - 2 * (State.CurrentPhase / State.ExtraValue));
            }
            else
            {
                return OriginalValue * Amplitude * (2 * ((State.CurrentPhase - State.ExtraValue) / (1 - State.ExtraValue)) - 1);
            }
        }

        private readonly static LFOGenFunctionMethod _MultPosRamp = MultPosRamp;
        private static double MultPosRamp(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            if (State.CurrentPhase < State.ExtraValue)
            {
                return OriginalValue * Amplitude * (1 - State.CurrentPhase / State.ExtraValue);
            }
            else
            {
                return OriginalValue * Amplitude * ((State.CurrentPhase - State.ExtraValue) / (1 - State.ExtraValue));
            }
        }

        private readonly static LFOGenFunctionMethod _MultSignFuzzTriangle = MultSignFuzzTriangle;
        private static double MultSignFuzzTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue * (2 * ReturnValue - 1) * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _MultSignFuzzSquare = MultSignFuzzSquare;
        private static double MultSignFuzzSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue * (2 * ReturnValue - 1) * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _MultPosFuzzTriangle = MultPosFuzzTriangle;
        private static double MultPosFuzzTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue * ReturnValue * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _MultPosFuzzSquare = MultPosFuzzSquare;
        private static double MultPosFuzzSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue * ReturnValue * Amplitude;
        }

        private readonly static LFOGenFunctionMethod _InvMultConst = InvMultConst;
        private static double InvMultConst(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * (1 - Amplitude);
        }

        private readonly static LFOGenFunctionMethod _InvMultSignSine = InvMultSignSine;
        private static double InvMultSignSine(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * (1 - Amplitude * Math.Sin(State.CurrentPhase * 2 * Math.PI));
        }

        private readonly static LFOGenFunctionMethod _InvMultPosSine = InvMultPosSine;
        private static double InvMultPosSine(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * (1 - Amplitude * 0.5 * (1 + Math.Sin(State.CurrentPhase * 2 * Math.PI)));
        }

        private readonly static LFOGenFunctionMethod _InvMultSignTriangle = InvMultSignTriangle;
        private static double InvMultSignTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Temp;

            if (State.CurrentPhase < 0.25)
            {
                Temp = State.CurrentPhase;
            }
            else if (State.CurrentPhase < 0.75)
            {
                Temp = 0.5 - State.CurrentPhase;
            }
            else
            {
                Temp = State.CurrentPhase - 1;
            }
            return OriginalValue * (1 - Temp * 4 * Amplitude);
        }

        private readonly static LFOGenFunctionMethod _InvMultPosTriangle = InvMultPosTriangle;
        private static double InvMultPosTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Temp;

            if (State.CurrentPhase < 0.25)
            {
                Temp = State.CurrentPhase;
            }
            else if (State.CurrentPhase < 0.75)
            {
                Temp = 0.5 - State.CurrentPhase;
            }
            else
            {
                Temp = State.CurrentPhase - 1;
            }
            return OriginalValue * (1 - ((Temp + 0.25) * 2 * Amplitude));
        }

        private readonly static LFOGenFunctionMethod _InvMultSignSquare = InvMultSignSquare;
        private static double InvMultSignSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Peak1;
            double Peak2;
            double Trough1;
            double Trough2;

            Peak1 = State.ExtraValue * 0.25;
            if (State.CurrentPhase < Peak1)
            {
                return OriginalValue * (1 - Amplitude * (State.CurrentPhase / Peak1));
            }
            Peak2 = Peak1 + (0.5 - State.ExtraValue * 0.5);
            if (State.CurrentPhase < Peak2)
            {
                return OriginalValue * (1 - Amplitude);
            }
            Trough1 = Peak2 + State.ExtraValue * 0.5;
            if (State.CurrentPhase < Trough1)
            {
                return OriginalValue * (1 - Amplitude * ((Trough1 - State.CurrentPhase) / Peak1 - 1));
            }
            Trough2 = 1 - State.ExtraValue * 0.25;
            if (State.CurrentPhase < Trough2)
            {
                return OriginalValue * (1 - -Amplitude);
            }
            return OriginalValue * (1 - Amplitude * ((State.CurrentPhase - Trough2) / (State.ExtraValue * 0.25) - 1));
        }

        private readonly static LFOGenFunctionMethod _InvMultPosSquare = InvMultPosSquare;
        private static double InvMultPosSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double Peak1;
            double Peak2;
            double Trough1;
            double Trough2;

            Peak1 = State.ExtraValue * 0.25;
            if (State.CurrentPhase < Peak1)
            {
                return OriginalValue * (1 - Amplitude * 0.5 * (1 + (State.CurrentPhase / Peak1)));
            }
            Peak2 = Peak1 + (0.5 - State.ExtraValue * 0.5);
            if (State.CurrentPhase < Peak2)
            {
                return OriginalValue * (1 - Amplitude);
            }
            Trough1 = Peak2 + State.ExtraValue * 0.5;
            if (State.CurrentPhase < Trough1)
            {
                return OriginalValue * (1 - Amplitude * 0.5 * (1 + ((Trough1 - State.CurrentPhase) / Peak1 - 1)));
            }
            Trough2 = 1 - State.ExtraValue * 0.25;
            if (State.CurrentPhase < Trough2)
            {
                return OriginalValue /* * (1 - 0) */;
            }
            return OriginalValue * (1 - Amplitude * 0.5 * (1 + ((State.CurrentPhase - Trough2) / (State.ExtraValue * 0.25) - 1)));
        }

        private readonly static LFOGenFunctionMethod _InvMultSignRamp = InvMultSignRamp;
        private static double InvMultSignRamp(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            if (State.CurrentPhase < State.ExtraValue)
            {
                return OriginalValue * (1 - Amplitude * (1 - 2 * (State.CurrentPhase / State.ExtraValue)));
            }
            else
            {
                return OriginalValue * (1 - Amplitude * (2 * ((State.CurrentPhase - State.ExtraValue) / (1 - State.ExtraValue)) - 1));
            }
        }

        private readonly static LFOGenFunctionMethod _InvMultPosRamp = InvMultPosRamp;
        private static double InvMultPosRamp(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            if (State.CurrentPhase < State.ExtraValue)
            {
                return OriginalValue * (1 - Amplitude * (1 - State.CurrentPhase / State.ExtraValue));
            }
            else
            {
                return OriginalValue * (1 - Amplitude * ((State.CurrentPhase - State.ExtraValue) / (1 - State.ExtraValue)));
            }
        }

        private readonly static LFOGenFunctionMethod _InvMultSignFuzzTriangle = InvMultSignFuzzTriangle;
        private static double InvMultSignFuzzTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue * (1 - (2 * ReturnValue - 1) * Amplitude);
        }

        private readonly static LFOGenFunctionMethod _InvMultSignFuzzSquare = InvMultSignFuzzSquare;
        private static double InvMultSignFuzzSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue * (1 - (2 * ReturnValue - 1) * Amplitude);
        }

        private readonly static LFOGenFunctionMethod _InvMultPosFuzzTriangle = InvMultPosFuzzTriangle;
        private static double InvMultPosFuzzTriangle(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue * (1 - ReturnValue * Amplitude);
        }

        private readonly static LFOGenFunctionMethod _InvMultPosFuzzSquare = InvMultPosFuzzSquare;
        private static double InvMultPosFuzzSquare(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue * (1 - ReturnValue * Amplitude);
        }

        private readonly static LFOGenFunctionMethod _AddWaveTable = AddWaveTable;
        private static double AddWaveTable(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            if (State.WaveTableWasDefined)
            {
                float WaveIndexerResult = WaveTableIndexer(
                    State.FramesPerTable * State.CurrentPhase,
                    State.CurrentWaveTableIndex * (State.NumberOfTables - 1),
                    State.NumberOfTables,
                    State.FramesPerTable,
                    State.WaveTableMatrix,
                    State.EnableCrossWaveTableInterpolation);
                return OriginalValue + Amplitude * WaveIndexerResult;
            }
            return OriginalValue;
        }

        private readonly static LFOGenFunctionMethod _MultWaveTable = MultWaveTable;
        private static double MultWaveTable(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            if (State.WaveTableWasDefined)
            {
                float WaveIndexerResult = WaveTableIndexer(
                    State.FramesPerTable * State.CurrentPhase,
                    State.CurrentWaveTableIndex * (State.NumberOfTables - 1),
                    State.NumberOfTables,
                    State.FramesPerTable,
                    State.WaveTableMatrix,
                    State.EnableCrossWaveTableInterpolation);
                return OriginalValue * Amplitude * WaveIndexerResult;
            }
            return 0;
        }

        private readonly static LFOGenFunctionMethod _InvMultWaveTable = InvMultWaveTable;
        private static double InvMultWaveTable(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            if (State.WaveTableWasDefined)
            {
                float WaveIndexerResult = WaveTableIndexer(
                    State.FramesPerTable * State.CurrentPhase,
                    State.CurrentWaveTableIndex * (State.NumberOfTables - 1),
                    State.NumberOfTables,
                    State.FramesPerTable,
                    State.WaveTableMatrix,
                    State.EnableCrossWaveTableInterpolation);
                return (1 - Amplitude * WaveIndexerResult) * OriginalValue;
            }
            return OriginalValue;
        }

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
        private static double LoopedEnvelopeHelper(
            ref LFOOneStateRec State,
            double OscillatorFrequency,
            SynthParamRec SynthParams,
            ref SynthErrorCodes ErrorRef)
        {
            SynthErrorCodes error = SynthErrorCodes.eSynthDone;
            double result = EnvelopeUpdate(
                State.LoopEnvelope,
                OscillatorFrequency,
                SynthParams,
                ref error);
            if (error != SynthErrorCodes.eSynthDone)
            {
                ErrorRef = error;
                return 0;
            }
            return result;
        }

        private static double AddLoopedEnvelope(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude,
            double OscillatorFrequency,
            SynthParamRec SynthParams,
            ref SynthErrorCodes ErrorRef)
        {
            double Temp = LoopedEnvelopeHelper(
                State,
                OscillatorFrequency,
                SynthParams,
                ref ErrorRef);
            return OriginalValue + Amplitude * Temp;
        }

        private static double MultLoopedEnvelope(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude,
            double OscillatorFrequency,
            SynthParamRec SynthParams,
            ref SynthErrorCodes ErrorRef)
        {
            double Temp = LoopedEnvelopeHelper(
                State,
                OscillatorFrequency,
                SynthParams,
                ref ErrorRef);
            return OriginalValue * Amplitude * Temp;
        }

        private static double InvMultLoopedEnvelope(
            ref LFOOneStateRec State,
            double OriginalValue,
            double Amplitude,
            double OscillatorFrequency,
            SynthParamRec SynthParams,
            ref SynthErrorCodes ErrorRef)
        {
            double Temp = LoopedEnvelopeHelper(
                State,
                OscillatorFrequency,
                SynthParams,
                ref ErrorRef);
            return (1 - Amplitude * Temp) * OriginalValue;
        }
#endif
    }
}
