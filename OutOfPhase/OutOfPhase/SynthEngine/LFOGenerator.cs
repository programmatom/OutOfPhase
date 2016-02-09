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
        public enum LFOArithSelect
        {
            eLFOArithAdditive,
            eLFOArithGeometric,
            eLFOArithDefault,
            eLFOArithHalfSteps,
        }

        // TODO: Convert delegates to interfaces (or make static delegates)

        public delegate double LFOGenFunctionMethod(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude);

        public class LFOOneStateRec
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
            int l = LFOListSpecGetNumElements(LFOListSpec);

            LFOGenRec LFOGen = new LFOGenRec();
            LFOGen.LFOVector = new LFOOneStateRec[l];

            LFOGen.NumLFOs = l;

            /* build the list of thingers */
            int MaxPreOrigin = 0;
            for (int i = 0; i < l; i += 1)
            {
                int PreOriginTime;

                LFOSpecRec OneLFOSpec = LFOListSpecGetLFOSpec(LFOListSpec, i);

                LFOOneStateRec ListNode = LFOGen.LFOVector[i] = new LFOOneStateRec();

                /* add frequency envelope generator */
                ListNode.LFOFrequencyEnvelope = NewEnvelopeStateRecord(
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
                        ListNode.ModulationMode = LFOArithSelect.eLFOArithAdditive;
                        break;
                    case LFOAdderMode.eLFOGeometric:
                        ListNode.ModulationMode = LFOArithSelect.eLFOArithGeometric;
                        break;
                    case LFOAdderMode.eLFOHalfSteps:
                        ListNode.ModulationMode = LFOArithSelect.eLFOArithHalfSteps;
                        break;
                }

                /* add the amplitude envelope generator */
                ListNode.LFOAmplitudeEnvelope = NewEnvelopeStateRecord(
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
                ListNode.LFOFrequencyLFOGenerator = NewLFOGenerator(
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
                ListNode.LFOAmplitudeLFOGenerator = NewLFOGenerator(
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
                ListNode.Operator = LFOSpecGetOscillatorType(OneLFOSpec);
                ListNode.OperatorIsLinearFuzz = /* this is an optimization */
                    ((ListNode.Operator == LFOOscTypes.eLFOSignedLinearFuzzTriangle)
                    || (ListNode.Operator == LFOOscTypes.eLFOPositiveLinearFuzzTriangle)
                    || (ListNode.Operator == LFOOscTypes.eLFOSignedLinearFuzzSquare)
                    || (ListNode.Operator == LFOOscTypes.eLFOPositiveLinearFuzzSquare));
                ListNode.ModulationMethod = LFOSpecGetModulationMode(OneLFOSpec);
                switch (ListNode.Operator)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case LFOOscTypes.eLFOConstant1:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddConst;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultConst;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultConst;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedSine:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddSignSine;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultSignSine;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultSignSine;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveSine:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddPosSine;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultPosSine;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultPosSine;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedTriangle:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddSignTriangle;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultSignTriangle;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultSignTriangle;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveTriangle:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddPosTriangle;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultPosTriangle;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultPosTriangle;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedSquare:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddSignSquare;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultSignSquare;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultSignSquare;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveSquare:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddPosSquare;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultPosSquare;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultPosSquare;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedRamp:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddSignRamp;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultSignRamp;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultSignRamp;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveRamp:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddPosRamp;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultPosRamp;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultPosRamp;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedLinearFuzzTriangle:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddSignFuzzTriangle;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultSignFuzzTriangle;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultSignFuzzTriangle;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOSignedLinearFuzzSquare:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddSignFuzzSquare;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultSignFuzzSquare;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultSignFuzzSquare;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveLinearFuzzTriangle:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddPosFuzzTriangle;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultPosFuzzTriangle;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultPosFuzzTriangle;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOPositiveLinearFuzzSquare:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddPosFuzzSquare;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultPosFuzzSquare;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultPosFuzzSquare;
                                break;
                        }
                        break;

                    case LFOOscTypes.eLFOWaveTable:
                        switch (ListNode.ModulationMethod)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case LFOModulationTypes.eLFOAdditive:
                                ListNode.GenFunction = AddWaveTable;
                                break;
                            case LFOModulationTypes.eLFOMultiplicative:
                                ListNode.GenFunction = MultWaveTable;
                                break;
                            case LFOModulationTypes.eLFOInverseMultiplicative:
                                ListNode.GenFunction = InvMultWaveTable;
                                break;
                        }

                        ListNode.WaveTableSourceSelector = NewMultiWaveTable(
                            GetLFOSpecSampleSelector(OneLFOSpec),
                            SynthParams.Dictionary);
                        ListNode.WaveTableWasDefined = GetMultiWaveTableReference(
                            ListNode.WaveTableSourceSelector,
                            FreqForMultisampling,
                            out ListNode.WaveTableMatrix,
                            out ListNode.FramesPerTable,
                            out ListNode.NumberOfTables);

                        ListNode.WaveTableIndexEnvelope = NewEnvelopeStateRecord(
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
                        ListNode.WaveTableLFOGenerator = NewLFOGenerator(
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

                        ListNode.EnableCrossWaveTableInterpolation = LFOSpecGetEnableCrossWaveTableInterpolation(OneLFOSpec);

                        break;

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
                    case LFOOscTypes.eLFOLoopedEnvelope:
                        {
                            switch (ListNode.ModulationMethod)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case LFOModulationTypes.eLFOAdditive:
                                    ListNode.GenFunction = AddLoopedEnvelope;
                                    break;
                                case LFOModulationTypes.eLFOMultiplicative:
                                    ListNode.GenFunction = MultLoopedEnvelope;
                                    break;
                                case LFOModulationTypes.eLFOInverseMultiplicative:
                                    ListNode.GenFunction = InvMultLoopedEnvelope;
                                    break;
                            }

                            // TODO: Because of the context in which the envelope is evaluated it is not parameterized
                            // by any of the usual values. It would be interesting to permit that.
                            int discardedPreOriginTime; // not used for this case
                            ListNode.LoopEnvelope = NewEnvelopeStateRecord(
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
                            ListNode.CurrentPhase = SynthParams.dEnvelopeRate;
                        }
                        break;
#endif
                }

                /* set up special values */
                ListNode.ExtraValue = GetLFOSpecExtraValue(OneLFOSpec);
                if ((ListNode.Operator == LFOOscTypes.eLFOSignedLinearFuzzTriangle)
                    || (ListNode.Operator == LFOOscTypes.eLFOPositiveLinearFuzzTriangle)
                    || (ListNode.Operator == LFOOscTypes.eLFOSignedLinearFuzzSquare)
                    || (ListNode.Operator == LFOOscTypes.eLFOPositiveLinearFuzzSquare))
                {
                    double seed = GetLFOSpecExtraValue(OneLFOSpec);
                    ListNode.Seed.ConstrainedSetSeed(unchecked((int)seed + 1/*legacy bug*/));
                    ListNode.LeftNoise = ParkAndMiller.Double0Through1(ListNode.Seed.Random());
                    ListNode.RightNoise = ParkAndMiller.Double0Through1(ListNode.Seed.Random());
                }

                /* filter */
                ListNode.LowpassFilterEnabled = HasLFOSpecFilterBeenSpecified(OneLFOSpec);
                if (ListNode.LowpassFilterEnabled)
                {
                    ListNode.LowpassFilter = new FirstOrderLowpassRec();

                    ListNode.LFOFilterCutoffEnvelope = NewEnvelopeStateRecord(
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

                    ListNode.LFOFilterCutoffLFOGenerator = NewLFOGenerator(
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
                ListNode.SampleAndHoldEnabled = HasLFOSpecSampleHoldBeenSpecified(OneLFOSpec);
                if (ListNode.SampleAndHoldEnabled)
                {
                    ListNode.LFOSampleHoldEnvelope = NewEnvelopeStateRecord(
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

                    ListNode.LFOSampleHoldLFOGenerator = NewLFOGenerator(
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
                    ListNode.SampleHoldPhase = 1; /* trigger immediately */
                    ListNode.CurrentSampleHold = 0;
                }
            }

            MaxPreOriginTime = MaxPreOrigin;
            return LFOGen;
        }

        /* fix up the origin time so that envelopes start at the proper times */
        public static void LFOGeneratorFixEnvelopeOrigins(
            LFOGenRec LFOGen,
            int ActualPreOriginTime)
        {
            for (int i = 0; i < LFOGen.NumLFOs; i += 1)
            {
                LFOOneStateRec Scan = LFOGen.LFOVector[i];

                EnvelopeStateFixUpInitialDelay(
                    Scan.LFOAmplitudeEnvelope,
                    ActualPreOriginTime);
                EnvelopeStateFixUpInitialDelay(
                    Scan.LFOFrequencyEnvelope,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    Scan.LFOAmplitudeLFOGenerator,
                    ActualPreOriginTime);
                LFOGeneratorFixEnvelopeOrigins(
                    Scan.LFOFrequencyLFOGenerator,
                    ActualPreOriginTime);
                if (Scan.Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeStateFixUpInitialDelay(
                        Scan.WaveTableIndexEnvelope,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        Scan.WaveTableLFOGenerator,
                        ActualPreOriginTime);
                }
                if (Scan.LowpassFilterEnabled)
                {
                    EnvelopeStateFixUpInitialDelay(
                        Scan.LFOFilterCutoffEnvelope,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        Scan.LFOFilterCutoffLFOGenerator,
                        ActualPreOriginTime);
                }
                if (Scan.SampleAndHoldEnabled)
                {
                    EnvelopeStateFixUpInitialDelay(
                        Scan.LFOSampleHoldEnvelope,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        Scan.LFOSampleHoldLFOGenerator,
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
                    Scan.CurrentPhase = -EnvelopeInitialValue(Scan.LFOFrequencyEnvelope) * ActualPreOriginTime / envelopeRate;
                    Scan.CurrentPhase = Scan.CurrentPhase - Math.Floor(Scan.CurrentPhase);

                    // prevent envelope generator from running before event origin.
                    EnvelopeStateFixUpInitialDelay(
                        Scan.LoopEnvelope,
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

            for (int i = 0; i < LFOGen.NumLFOs; i += 1)
            {
                LFOOneStateRec Scan = LFOGen.LFOVector[i];

                /* compute amplitude envelope/lfo thing */
                SynthErrorCodes error = SynthErrorCodes.eSynthDone;
                double VariantAmplitude = LFOGenUpdateCycle(
                    Scan.LFOAmplitudeLFOGenerator,
                    EnvelopeUpdate(
                        Scan.LFOAmplitudeEnvelope,
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
                if (Scan.Operator == LFOOscTypes.eLFOWaveTable)
                {
                    Scan.CurrentWaveTableIndex = LFOGenUpdateCycle(
                        Scan.WaveTableLFOGenerator,
                        EnvelopeUpdate(
                            Scan.WaveTableIndexEnvelope,
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
                if ((Scan.ModulationMode != LFOArithSelect.eLFOArithAdditive)
                    && (Scan.ModulationMode != LFOArithSelect.eLFOArithGeometric)
                    && (Scan.ModulationMode != LFOArithSelect.eLFOArithHalfSteps))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
                if (Scan.ModulationMode == LFOArithSelect.eLFOArithAdditive)
                {
                    OriginalValue = Scan.GenFunction(
                        Scan,
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

                        if (Scan.ModulationMode == LFOArithSelect.eLFOArithGeometric)
                        {
                            /* the LOG2 is to normalize the values, so that 1/12 will */
                            /* be 1 halfstep */
                            ScalingConstant = Constants.LOG2;
                        }
                        else /* if (Scan.ModulationMode == eLFOArithHalfSteps) */
                        {
                            /* this one means 1 is a halfstep */
                            ScalingConstant = Constants.LOG2 / 12;
                        }
                        OriginalValue = Math.Exp(
                            Scan.GenFunction(
                                Scan,
                                Math.Log(OriginalValue),
                                VariantAmplitude * ScalingConstant));
                    }
                    if (Sign)
                    {
                        OriginalValue = -OriginalValue;
                    }
                }

                /* apply sample and hold */
                if (Scan.SampleAndHoldEnabled)
                {
                    double SampleHoldPhase;

                    /* update sample/hold phase generator */
                    error = SynthErrorCodes.eSynthDone;
                    SampleHoldPhase = Scan.SampleHoldPhase;
                    SampleHoldPhase = SampleHoldPhase +
                        LFOGenUpdateCycle(
                            Scan.LFOSampleHoldLFOGenerator,
                            EnvelopeUpdate(
                                Scan.LFOSampleHoldEnvelope,
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
                        Scan.CurrentSampleHold = OriginalValue;
                    }
                    Scan.SampleHoldPhase = SampleHoldPhase;
                    /* set current value to held value */
                    OriginalValue = Scan.CurrentSampleHold;
                }

                /* apply lowpass filter */
                if (Scan.LowpassFilterEnabled)
                {
                    error = SynthErrorCodes.eSynthDone;
                    FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                        Scan.LowpassFilter,
                        LFOGenUpdateCycle(
                            Scan.LFOFilterCutoffLFOGenerator,
                            EnvelopeUpdate(
                                Scan.LFOFilterCutoffEnvelope,
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
                        Scan.LowpassFilter,
                        (float)OriginalValue);
                }

                /* update phase of oscillator */
                error = SynthErrorCodes.eSynthDone;
                Scan.CurrentPhase = Scan.CurrentPhase +
                    LFOGenUpdateCycle(
                        Scan.LFOFrequencyLFOGenerator,
                        EnvelopeUpdate(
                            Scan.LFOFrequencyEnvelope,
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
                        (Scan.OperatorIsLinearFuzz &&
                            ((Scan.Operator == LFOOscTypes.eLFOSignedLinearFuzzTriangle)
                                || (Scan.Operator == LFOOscTypes.eLFOPositiveLinearFuzzTriangle)
                                || (Scan.Operator == LFOOscTypes.eLFOSignedLinearFuzzSquare)
                                || (Scan.Operator == LFOOscTypes.eLFOPositiveLinearFuzzSquare)))
                        ||
                        (!Scan.OperatorIsLinearFuzz &&
                            !((Scan.Operator == LFOOscTypes.eLFOSignedLinearFuzzTriangle)
                                || (Scan.Operator == LFOOscTypes.eLFOPositiveLinearFuzzTriangle)
                                || (Scan.Operator == LFOOscTypes.eLFOSignedLinearFuzzSquare)
                                || (Scan.Operator == LFOOscTypes.eLFOPositiveLinearFuzzSquare)))))
                {
                    // Operator and OperatorIsLinearFuzz are inconsistent
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
                if (Scan.OperatorIsLinearFuzz)
                {
                    for (int Counter = (int)(Math.Floor(Scan.CurrentPhase)); Counter >= 1; Counter -= 1)
                    {
                        Scan.LeftNoise = Scan.RightNoise;
                        Scan.RightNoise = ParkAndMiller.Double0Through1(Scan.Seed.Random());
                    }
                }

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
                if (Scan.LoopEnvelope != null)
                {
                    // For looped envelopes, retriggering is keyed off of the phase, which requires the user to specify
                    // the period for the oscillator. This complication avoids needing to implement a high-precision envelope
                    // generator that can stay in sync with the note event clock over long periods of time.
                    if (Scan.CurrentPhase >= 1)
                    {
                        // TODO: Because of the context in which the envelope is evaluated it is not parameterized
                        // by any of the usual values. It would be interesting to permit that.
                        AccentRec zero = new AccentRec();
                        EnvelopeRetriggerFromOrigin(
                            Scan.LoopEnvelope,
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
                Scan.CurrentPhase = Scan.CurrentPhase - Math.Floor(Scan.CurrentPhase);
#if DEBUG
                if (Scan.CurrentPhase >= 1)
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
            for (int i = 0; i < LFOGen.NumLFOs; i += 1)
            {
                LFOOneStateRec Scan = LFOGen.LFOVector[i];

                /* compute amplitude envelope/lfo thing */
                double VariantAmplitude = LFOGenInitialValue(
                    Scan.LFOAmplitudeLFOGenerator,
                    EnvelopeInitialValue(
                        Scan.LFOAmplitudeEnvelope));

                // specialized additional parameters for certain types
                if (Scan.Operator == LFOOscTypes.eLFOWaveTable)
                {
                    // WARNING: CurrentWaveTableIndex is updated here for the benefit of the GenFunction() below, but
                    // should be safe beacuse it will be regenerated fresh in the next LFOGenUpdateCycle() 
                    Scan.CurrentWaveTableIndex = LFOGenInitialValue(
                        Scan.WaveTableLFOGenerator,
                        EnvelopeInitialValue(
                            Scan.WaveTableIndexEnvelope));
                }

                /* perform the calculations */
#if DEBUG
                if ((Scan.ModulationMode != LFOArithSelect.eLFOArithAdditive)
                    && (Scan.ModulationMode != LFOArithSelect.eLFOArithGeometric)
                    && (Scan.ModulationMode != LFOArithSelect.eLFOArithHalfSteps))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif
                if (Scan.ModulationMode == LFOArithSelect.eLFOArithAdditive)
                {
                    OriginalValue = Scan.GenFunction(
                        Scan,
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

                        if (Scan.ModulationMode == LFOArithSelect.eLFOArithGeometric)
                        {
                            /* the LOG2 is to normalize the values, so that 1/12 will */
                            /* be 1 halfstep */
                            ScalingConstant = Constants.LOG2;
                        }
                        else /* if (Scan.ModulationMode == eLFOArithHalfSteps) */
                        {
                            /* this one means 1 is a halfstep */
                            ScalingConstant = Constants.LOG2 / 12;
                        }
                        OriginalValue = Math.Exp(
                            Scan.GenFunction(
                                Scan,
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
            for (int i = 0; i < LFOGen.NumLFOs; i += 1)
            {
                LFOOneStateRec Scan = LFOGen.LFOVector[i];

                EnvelopeKeyUpSustain1(
                    Scan.LFOAmplitudeEnvelope);
                EnvelopeKeyUpSustain1(
                    Scan.LFOFrequencyEnvelope);
                LFOGeneratorKeyUpSustain1(
                    Scan.LFOAmplitudeLFOGenerator);
                LFOGeneratorKeyUpSustain1(
                    Scan.LFOFrequencyLFOGenerator);
                if (Scan.Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeKeyUpSustain1(
                        Scan.WaveTableIndexEnvelope);
                    LFOGeneratorKeyUpSustain1(
                        Scan.WaveTableLFOGenerator);
                }
                if (Scan.LowpassFilterEnabled)
                {
                    EnvelopeKeyUpSustain1(
                        Scan.LFOFilterCutoffEnvelope);
                    LFOGeneratorKeyUpSustain1(
                        Scan.LFOFilterCutoffLFOGenerator);
                }
                if (Scan.SampleAndHoldEnabled)
                {
                    EnvelopeKeyUpSustain1(
                        Scan.LFOSampleHoldEnvelope);
                    LFOGeneratorKeyUpSustain1(
                        Scan.LFOSampleHoldLFOGenerator);
                }
            }
        }

        /* pass the key-up impulse on to the envelopes contained inside */
        public static void LFOGeneratorKeyUpSustain2(LFOGenRec LFOGen)
        {
            for (int i = 0; i < LFOGen.NumLFOs; i += 1)
            {
                LFOOneStateRec Scan = LFOGen.LFOVector[i];

                EnvelopeKeyUpSustain2(
                    Scan.LFOAmplitudeEnvelope);
                EnvelopeKeyUpSustain2(
                    Scan.LFOFrequencyEnvelope);
                LFOGeneratorKeyUpSustain2(
                    Scan.LFOAmplitudeLFOGenerator);
                LFOGeneratorKeyUpSustain2(
                    Scan.LFOFrequencyLFOGenerator);
                if (Scan.Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeKeyUpSustain2(
                        Scan.WaveTableIndexEnvelope);
                    LFOGeneratorKeyUpSustain2(
                        Scan.WaveTableLFOGenerator);
                }
                if (Scan.LowpassFilterEnabled)
                {
                    EnvelopeKeyUpSustain2(
                        Scan.LFOFilterCutoffEnvelope);
                    LFOGeneratorKeyUpSustain2(
                        Scan.LFOFilterCutoffLFOGenerator);
                }
                if (Scan.SampleAndHoldEnabled)
                {
                    EnvelopeKeyUpSustain2(
                        Scan.LFOSampleHoldEnvelope);
                    LFOGeneratorKeyUpSustain2(
                        Scan.LFOSampleHoldLFOGenerator);
                }
            }
        }

        /* pass the key-up impulse on to the envelopes contained inside */
        public static void LFOGeneratorKeyUpSustain3(LFOGenRec LFOGen)
        {
            for (int i = 0; i < LFOGen.NumLFOs; i += 1)
            {
                LFOOneStateRec Scan = LFOGen.LFOVector[i];

                EnvelopeKeyUpSustain3(
                    Scan.LFOAmplitudeEnvelope);
                EnvelopeKeyUpSustain3(
                    Scan.LFOFrequencyEnvelope);
                LFOGeneratorKeyUpSustain3(
                    Scan.LFOAmplitudeLFOGenerator);
                LFOGeneratorKeyUpSustain3(
                    Scan.LFOFrequencyLFOGenerator);
                if (Scan.Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeKeyUpSustain3(
                        Scan.WaveTableIndexEnvelope);
                    LFOGeneratorKeyUpSustain3(
                        Scan.WaveTableLFOGenerator);
                }
                if (Scan.LowpassFilterEnabled)
                {
                    EnvelopeKeyUpSustain3(
                        Scan.LFOFilterCutoffEnvelope);
                    LFOGeneratorKeyUpSustain3(
                        Scan.LFOFilterCutoffLFOGenerator);
                }
                if (Scan.SampleAndHoldEnabled)
                {
                    EnvelopeKeyUpSustain3(
                        Scan.LFOSampleHoldEnvelope);
                    LFOGeneratorKeyUpSustain3(
                        Scan.LFOSampleHoldLFOGenerator);
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
            for (int i = 0; i < LFOGen.NumLFOs; i += 1)
            {
                LFOOneStateRec Scan = LFOGen.LFOVector[i];

                EnvelopeRetriggerFromOrigin(
                    Scan.LFOAmplitudeEnvelope,
                    ref Accents,
                    FrequencyHertz,
                    AmplitudeScaling,
                    HurryUp,
                    ActuallyRetrigger,
                    SynthParams);
                EnvelopeRetriggerFromOrigin(
                    Scan.LFOFrequencyEnvelope,
                    ref Accents,
                    FrequencyHertz,
                    FrequencyScaling,
                    HurryUp,
                    ActuallyRetrigger,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    Scan.LFOAmplitudeLFOGenerator,
                    ref Accents,
                    FrequencyHertz,
                    HurryUp,
                    1,
                    1/*no recursive scaling*/,
                    ActuallyRetrigger,
                    SynthParams);
                LFOGeneratorRetriggerFromOrigin(
                    Scan.LFOFrequencyLFOGenerator,
                    ref Accents,
                    FrequencyHertz,
                    HurryUp,
                    1,
                    1/*no recursive scaling*/,
                    ActuallyRetrigger,
                    SynthParams);
                if (Scan.Operator == LFOOscTypes.eLFOWaveTable)
                {
                    EnvelopeRetriggerFromOrigin(
                        Scan.WaveTableIndexEnvelope,
                        ref Accents,
                        FrequencyHertz,
                        FrequencyScaling,
                        HurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        Scan.WaveTableLFOGenerator,
                        ref Accents,
                        FrequencyHertz,
                        HurryUp,
                        1,
                        1/*no recursive scaling*/,
                        ActuallyRetrigger,
                        SynthParams);
                }
                if (Scan.LowpassFilterEnabled)
                {
                    EnvelopeRetriggerFromOrigin(
                        Scan.LFOFilterCutoffEnvelope,
                        ref Accents,
                        FrequencyHertz,
                        FrequencyScaling,
                        HurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        Scan.LFOFilterCutoffLFOGenerator,
                        ref Accents,
                        FrequencyHertz,
                        HurryUp,
                        1,
                        1/*no recursive scaling*/,
                        ActuallyRetrigger,
                        SynthParams);
                }
                if (Scan.SampleAndHoldEnabled)
                {
                    EnvelopeRetriggerFromOrigin(
                        Scan.LFOSampleHoldEnvelope,
                        ref Accents,
                        FrequencyHertz,
                        FrequencyScaling,
                        HurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        Scan.LFOSampleHoldLFOGenerator,
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
            for (int i = 0; i < LFOGen.NumLFOs; i += 1)
            {
                LFOOneStateRec StateScan = LFOGen.LFOVector[i];

                if (StateScan.Operator == LFOOscTypes.eLFOWaveTable)
                {
                    if (HasEnvelopeStartedYet(StateScan.WaveTableIndexEnvelope))
                    {
                        return true;
                    }
                    if (HasLFOGeneratorStarted(StateScan.WaveTableLFOGenerator))
                    {
                        return true;
                    }
                }
                if (StateScan.LowpassFilterEnabled)
                {
                    if (HasEnvelopeStartedYet(StateScan.LFOFilterCutoffEnvelope))
                    {
                        return true;
                    }
                    if (HasLFOGeneratorStarted(StateScan.LFOFilterCutoffLFOGenerator))
                    {
                        return true;
                    }
                }
                if (StateScan.SampleAndHoldEnabled)
                {
                    if (HasEnvelopeStartedYet(StateScan.LFOSampleHoldEnvelope))
                    {
                        return true;
                    }
                    if (HasLFOGeneratorStarted(StateScan.LFOSampleHoldLFOGenerator))
                    {
                        return true;
                    }
                }
                if (HasEnvelopeStartedYet(StateScan.LFOAmplitudeEnvelope))
                {
                    return true;
                }
                if (HasEnvelopeStartedYet(StateScan.LFOFrequencyEnvelope))
                {
                    return true;
                }
                if (HasLFOGeneratorStarted(StateScan.LFOAmplitudeLFOGenerator))
                {
                    return true;
                }
                if (HasLFOGeneratorStarted(StateScan.LFOFrequencyLFOGenerator))
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

        private static double AddConst(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue + Amplitude;
        }

        private static double AddSignSine(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue + Amplitude * Math.Sin(State.CurrentPhase * 2 * Math.PI);
        }

        private static double AddPosSine(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue + Amplitude * 0.5 * (1 + Math.Sin(State.CurrentPhase * 2 * Math.PI));
        }

        private static double AddSignTriangle(
            LFOOneStateRec State,
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

        private static double AddPosTriangle(
            LFOOneStateRec State,
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

        private static double AddSignSquare(
            LFOOneStateRec State,
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

        private static double AddPosSquare(
            LFOOneStateRec State,
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

        private static double AddSignRamp(
            LFOOneStateRec State,
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

        private static double AddPosRamp(
            LFOOneStateRec State,
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

        private static double AddSignFuzzTriangle(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue + (2 * ReturnValue - 1) * Amplitude;
        }

        private static double AddSignFuzzSquare(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue + (2 * ReturnValue - 1) * Amplitude;
        }

        private static double AddPosFuzzTriangle(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue + ReturnValue * Amplitude;
        }

        private static double AddPosFuzzSquare(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue + ReturnValue * Amplitude;
        }

        private static double MultConst(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * Amplitude;
        }

        private static double MultSignSine(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * Amplitude * Math.Sin(State.CurrentPhase * 2 * Math.PI);
        }

        private static double MultPosSine(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * Amplitude * 0.5 * (1 + Math.Sin(State.CurrentPhase * 2 * Math.PI));
        }

        private static double MultSignTriangle(
            LFOOneStateRec State,
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

        private static double MultPosTriangle(
            LFOOneStateRec State,
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

        private static double MultSignSquare(
            LFOOneStateRec State,
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

        private static double MultPosSquare(
            LFOOneStateRec State,
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

        private static double MultSignRamp(
            LFOOneStateRec State,
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

        private static double MultPosRamp(
            LFOOneStateRec State,
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

        private static double MultSignFuzzTriangle(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue * (2 * ReturnValue - 1) * Amplitude;
        }

        private static double MultSignFuzzSquare(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue * (2 * ReturnValue - 1) * Amplitude;
        }

        private static double MultPosFuzzTriangle(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue * ReturnValue * Amplitude;
        }

        private static double MultPosFuzzSquare(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue * ReturnValue * Amplitude;
        }

        private static double InvMultConst(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * (1 - Amplitude);
        }

        private static double InvMultSignSine(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * (1 - Amplitude * Math.Sin(State.CurrentPhase * 2 * Math.PI));
        }

        private static double InvMultPosSine(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            return OriginalValue * (1 - Amplitude * 0.5 * (1 + Math.Sin(State.CurrentPhase * 2 * Math.PI)));
        }

        private static double InvMultSignTriangle(
            LFOOneStateRec State,
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

        private static double InvMultPosTriangle(
            LFOOneStateRec State,
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

        private static double InvMultSignSquare(
            LFOOneStateRec State,
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

        private static double InvMultPosSquare(
            LFOOneStateRec State,
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

        private static double InvMultSignRamp(
            LFOOneStateRec State,
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

        private static double InvMultPosRamp(
            LFOOneStateRec State,
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

        private static double InvMultSignFuzzTriangle(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue * (1 - (2 * ReturnValue - 1) * Amplitude);
        }

        private static double InvMultSignFuzzSquare(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue * (1 - (2 * ReturnValue - 1) * Amplitude);
        }

        private static double InvMultPosFuzzTriangle(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise * (1 - State.CurrentPhase) + State.RightNoise * State.CurrentPhase;
            return OriginalValue * (1 - ReturnValue * Amplitude);
        }

        private static double InvMultPosFuzzSquare(
            LFOOneStateRec State,
            double OriginalValue,
            double Amplitude)
        {
            double ReturnValue = State.LeftNoise;
            return OriginalValue * (1 - ReturnValue * Amplitude);
        }

        private static double AddWaveTable(
            LFOOneStateRec State,
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

        private static double MultWaveTable(
            LFOOneStateRec State,
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

        private static double InvMultWaveTable(
            LFOOneStateRec State,
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
            LFOOneStateRec State,
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
            LFOOneStateRec State,
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
            LFOOneStateRec State,
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
            LFOOneStateRec State,
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
