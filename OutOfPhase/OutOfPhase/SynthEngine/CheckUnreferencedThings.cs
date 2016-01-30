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
        /* parameter struct */
        public class CheckUnrefParamRec
        {
            public WaveSampDictRec Dictionary;
            public CodeCenterRec CodeCenter;
            public SynthErrorInfoRec ErrorInfo;
        }

        /* make sure all instruments in list refer to existing samples and wave tables */
        /* all instruments should be up to date when this is called. */
        public static SynthErrorCodes CheckInstrListForUnreferencedSamples(
            IList<InstrObjectRec> InstrList,
            CheckUnrefParamRec Param)
        {
            for (int Scan = 0; Scan < InstrList.Count; Scan += 1)
            {
                InstrObjectRec InstrObject = InstrList[Scan];
                InstrumentRec InstrumentDefinition = InstrObject.BuiltInstrument;
                SynthErrorCodes Error = CheckInstrumentForUnreferencedSamples(
                    InstrumentDefinition,
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    Param.ErrorInfo.InstrumentName = InstrObject.Name;
                    return Error;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* make sure all referenced samples and wave tables really exist. */
        public static SynthErrorCodes CheckInstrumentForUnreferencedSamples(
            InstrumentRec Instrument,
            CheckUnrefParamRec Param)
        {
            SynthErrorCodes Error;

            Error = CheckOscillatorListForUnreferencedSamples(
                GetInstrumentOscillatorList(Instrument),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetInstrumentFrequencyLFOList(Instrument),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEffectListForUnreferencedSamples(
                GetInstrumentEffectSpecList(Instrument),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEffectListForUnreferencedSamples(
                GetInstrumentCombinedOscEffectSpecList(Instrument),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check list of oscillators */
        public static SynthErrorCodes CheckOscillatorListForUnreferencedSamples(
            OscillatorListRec OscillatorList,
            CheckUnrefParamRec Param)
        {
            int Limit = GetOscillatorListLength(OscillatorList);
            for (int Scan = 0; Scan < Limit; Scan += 1)
            {
                SynthErrorCodes Error = CheckOscillatorForUnreferencedSamples(
                    GetOscillatorFromList(OscillatorList, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check list of LFOs */
        public static SynthErrorCodes CheckLFOListForUnreferencedSamples(
            LFOListSpecRec LFOList,
            CheckUnrefParamRec Param)
        {
            int Limit = LFOListSpecGetNumElements(LFOList);
            for (int Scan = 0; Scan < Limit; Scan += 1)
            {
                SynthErrorCodes Error = CheckLFOForUnreferencedSamples(
                    LFOListSpecGetLFOSpec(LFOList, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check list of effects */
        public static SynthErrorCodes CheckEffectListForUnreferencedSamples(
            EffectSpecListRec EffectList,
            CheckUnrefParamRec Param)
        {
            int Limit = GetEffectSpecListLength(EffectList);
            for (int Scan = 0; Scan < Limit; Scan += 1)
            {
                SynthErrorCodes Error = SynthErrorCodes.eSynthDone;
                switch (GetEffectSpecListElementType(EffectList, Scan))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case EffectTypes.eDelayEffect:
                        Error = CheckDelayEffectForUnreferencedSamples(
                            GetDelayEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eNLProcEffect:
                        Error = CheckNonlinearEffectForUnreferencedSamples(
                            GetNLProcEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eFilterEffect:
                        Error = CheckFilterEffectForUnreferencedSamples(
                            GetFilterEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eAnalyzerEffect:
                        Error = CheckAnalyzerEffectForUnreferencedSamples(
                            GetAnalyzerEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eHistogramEffect:
                        Error = CheckHistogramEffectForUnreferencedSamples(
                            GetHistogramEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eResamplerEffect:
                        Error = CheckResamplerEffectForUnreferencedSamples(
                            GetResamplerEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eCompressorEffect:
                        Error = CheckCompressorEffectForUnreferencedSamples(
                            GetCompressorEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eVocoderEffect:
                        Error = CheckVocoderEffectForUnreferencedSamples(
                            GetVocoderEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eIdealLowpassEffect:
                        Error = CheckIdealLowpassEffectForUnreferencedSamples(
                            GetIdealLPEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eConvolverEffect:
                        Error = CheckConvolverEffectForUnreferencedSamples(
                            GetConvolverEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.eUserEffect:
                        Error = CheckUserEffectForUnreferencedSamples(
                            GetUserEffectFromEffectSpecList(EffectList, Scan),
                            Param);
                        break;
                    case EffectTypes.ePluggableEffect:
                        PluggableSpec PluggableEffect = GetPluggableEffectFromEffectSpecList(EffectList, Scan);
                        Error = PluggableEffect.PluggableTemplate.CheckUnreferencedObjects(
                            PluggableEffect.GetStaticStrings(),
                            Param);
                        break;
                }

                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check oscillator */
        public static SynthErrorCodes CheckOscillatorForUnreferencedSamples(
            OscillatorRec Oscillator,
            CheckUnrefParamRec Param)
        {
            SynthErrorCodes Error = SynthErrorCodes.eSynthDone;
            switch (OscillatorGetWhatKindItIs(Oscillator))
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case OscillatorTypes.eOscillatorSampled:
                    Error = CheckSampleSelectorForUnreferencedSamples(
                        OscillatorGetSampleIntervalList(Oscillator),
                        Param);
                    break;
                case OscillatorTypes.eOscillatorWaveTable:
                case OscillatorTypes.eOscillatorFOF:
                    Error = CheckWaveTableSelectorForUnreferencedSamples(
                        OscillatorGetSampleIntervalList(Oscillator),
                        Param);
                    break;
                case OscillatorTypes.eOscillatorAlgorithm:
                    break;
                case OscillatorTypes.eOscillatorFMSynth:
                    Error = CheckFMNetworkForUnreferencedSamples(
                        OscillatorGetFMSynthSpec(Oscillator),
                        Param);
                    break;
                case OscillatorTypes.eOscillatorPluggable:
                    IPluggableProcessorTemplate pluggable = GetOscillatorPluggableSpec(Oscillator).PluggableTemplate;
                    Error = pluggable.CheckUnreferencedObjects(
                        GetOscillatorPluggableSpec(Oscillator).GetStaticStrings(),
                        Param);
                    break;
            }
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                OscillatorGetLoudnessEnvelope(Oscillator),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                OscillatorGetLoudnessLFOList(Oscillator),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                OscillatorGetExcitationEnvelope(Oscillator),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                OscillatorGetExcitationLFOList(Oscillator),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetOscillatorFrequencyLFOList(Oscillator),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEffectListForUnreferencedSamples(
                GetOscillatorEffectList(Oscillator),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                OscillatorGetFOFRateEnvelope(Oscillator),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                OscillatorGetFOFRateLFOList(Oscillator),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check fm network */
        public static SynthErrorCodes CheckFMNetworkForUnreferencedSamples(
            FMSynthSpecRec FMSynthSpec,
            CheckUnrefParamRec Param)
        {
            int c = FMSynthGetNumStatements(FMSynthSpec);
            for (int i = 0; i < c; i += 1)
            {
                SynthErrorCodes Error;
                switch (FMSynthGetStatementType(FMSynthSpec, i))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case FMSynthStmtType.eFMSynthWave:
                        Error = CheckWaveTableSelectorForUnreferencedSamples(
                            FMSynthWaveStmtGetSampleIntervalList(
                                FMSynthGetWaveStatement(FMSynthSpec, i)),
                                Param);
                        if (Error != SynthErrorCodes.eSynthDone)
                        {
                            return Error;
                        }
                        break;

                    case FMSynthStmtType.eFMSynthMuladd:
                        break;

                    case FMSynthStmtType.eFMSynthEnvelope:
                        Error = CheckEnvelopeForUnreferencedSamples(
                            FMSynthEnvelopeStmtGetEnvelope(
                                FMSynthGetEnvelopeStatement(FMSynthSpec, i)),
                                Param);
                        if (Error != SynthErrorCodes.eSynthDone)
                        {
                            return Error;
                        }
                        Error = CheckLFOListForUnreferencedSamples(
                            FMSynthEnvelopeStmtGetLFOList(
                                FMSynthGetEnvelopeStatement(FMSynthSpec, i)),
                                Param);
                        if (Error != SynthErrorCodes.eSynthDone)
                        {
                            return Error;
                        }
                        break;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check LFO */
        public static SynthErrorCodes CheckLFOForUnreferencedSamples(
            LFOSpecRec LFO,
            CheckUnrefParamRec Param)
        {
            SynthErrorCodes Error;

            Error = CheckEnvelopeForUnreferencedSamples(
                GetLFOSpecFrequencyEnvelope(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetLFOSpecFrequencyLFOList(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetLFOSpecAmplitudeEnvelope(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetLFOSpecAmplitudeLFOList(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetLFOSpecWaveTableIndexEnvelope(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetLFOSpecWaveTableIndexLFOList(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckWaveTableSelectorForUnreferencedSamples(
                GetLFOSpecSampleSelector(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetLFOSpecFilterCutoffEnvelope(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetLFOSpecFilterCutoffLFOList(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetLFOSpecSampleHoldFreqEnvelope(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetLFOSpecSampleHoldFreqLFOList(LFO),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check delay effect */
        public static SynthErrorCodes CheckDelayEffectForUnreferencedSamples(
            DelayEffectRec DelayEffect,
            CheckUnrefParamRec Param)
        {
            int Limit = GetDelayEffectSpecNumTaps(DelayEffect);
            for (int Scan = 0; Scan < Limit; Scan += 1)
            {
                SynthErrorCodes Error;

                Error = CheckEnvelopeForUnreferencedSamples(
                    GetDelayTapSourceEnvelope(DelayEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckEnvelopeForUnreferencedSamples(
                    GetDelayTapTargetEnvelope(DelayEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckEnvelopeForUnreferencedSamples(
                    GetDelayTapScaleEnvelope(DelayEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckEnvelopeForUnreferencedSamples(
                    GetDelayTapCutoffEnvelope(DelayEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckLFOListForUnreferencedSamples(
                    GetDelayTapSourceLFO(DelayEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckLFOListForUnreferencedSamples(
                    GetDelayTapTargetLFO(DelayEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckLFOListForUnreferencedSamples(
                    GetDelayTapScaleLFO(DelayEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckLFOListForUnreferencedSamples(
                    GetDelayTapCutoffLFO(DelayEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check nonlinear processor effect */
        public static SynthErrorCodes CheckNonlinearEffectForUnreferencedSamples(
            NonlinProcSpecRec NonlinearEffect,
            CheckUnrefParamRec Param)
        {
            SynthErrorCodes Error;

            string Name = GetNLProcSpecWaveTableName(NonlinearEffect);
            if (!WaveSampDictDoesWaveTableExist(
                Param.Dictionary,
                Name))
            {
                Param.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedWaveTable;
                Param.ErrorInfo.WaveTableName = Name;
                return SynthErrorCodes.eSynthErrorEx;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetNLProcInputEnvelope(NonlinearEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetNLProcOutputEnvelope(NonlinearEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetNLProcIndexEnvelope(NonlinearEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetNLProcInputLFO(NonlinearEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetNLProcOutputLFO(NonlinearEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetNLProcIndexLFO(NonlinearEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check filter effect */
        public static SynthErrorCodes CheckFilterEffectForUnreferencedSamples(
                                                FilterSpecRec FilterEffect,
                                                CheckUnrefParamRec Param)
        {
            int Limit = GetNumFiltersInSpec(FilterEffect);
            for (int Scan = 0; Scan < Limit; Scan += 1)
            {
                SynthErrorCodes Error;

                Error = CheckEnvelopeForUnreferencedSamples(
                    GetFilterCutoffEnvelope(FilterEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckEnvelopeForUnreferencedSamples(
                    GetFilterBandwidthOrSlopeEnvelope(FilterEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckEnvelopeForUnreferencedSamples(
                    GetFilterOutputEnvelope(FilterEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckEnvelopeForUnreferencedSamples(
                    GetFilterGainEnvelope(FilterEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckLFOListForUnreferencedSamples(
                    GetFilterCutoffLFO(FilterEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckLFOListForUnreferencedSamples(
                    GetFilterBandwidthOrSlopeLFO(FilterEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckLFOListForUnreferencedSamples(
                    GetFilterOutputLFO(FilterEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }

                Error = CheckLFOListForUnreferencedSamples(
                    GetFilterGainLFO(FilterEffect, Scan),
                    Param);
                if (Error != SynthErrorCodes.eSynthDone)
                {
                    return Error;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check analyzer effect */
        public static SynthErrorCodes CheckAnalyzerEffectForUnreferencedSamples(
            AnalyzerSpecRec AnalyzerEffect,
            CheckUnrefParamRec Param)
        {
            return SynthErrorCodes.eSynthDone;
        }

        /* check histogram effect */
        public static SynthErrorCodes CheckHistogramEffectForUnreferencedSamples(
            HistogramSpecRec HistogramEffect,
            CheckUnrefParamRec Param)
        {
            return SynthErrorCodes.eSynthDone;
        }

        /* check resampler effect */
        public static SynthErrorCodes CheckResamplerEffectForUnreferencedSamples(
            ResamplerSpecRec ResamplerEffect,
            CheckUnrefParamRec Param)
        {
            return SynthErrorCodes.eSynthDone;
        }

        /* check compressor effect */
        public static SynthErrorCodes CheckCompressorEffectForUnreferencedSamples(
            CompressorSpecRec CompressorEffect,
            CheckUnrefParamRec Param)
        {
            SynthErrorCodes Error;

            Error = CheckEnvelopeForUnreferencedSamples(
                GetCompressorInputGainEnvelope(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetCompressorInputGainLFOList(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetCompressorOutputGainEnvelope(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetCompressorOutputGainLFOList(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetCompressorNormalPowerEnvelope(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetCompressorNormalPowerLFOList(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetCompressorThreshPowerEnvelope(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetCompressorThreshPowerLFOList(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetCompressorRatioEnvelope(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetCompressorRatioLFOList(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetCompressorFilterFreqEnvelope(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetCompressorFilterFreqLFOList(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetCompressorDecayRateEnvelope(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetCompressorDecayRateLFOList(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetCompressorAttackRateEnvelope(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetCompressorAttackRateLFOList(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetCompressorLimitingExcessEnvelope(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetCompressorLimitingExcessLFOList(CompressorEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check vocoder effect */
        public static SynthErrorCodes CheckVocoderEffectForUnreferencedSamples(
            VocoderSpecRec VocoderEffect,
            CheckUnrefParamRec Param)
        {
            SynthErrorCodes Error;

            string Name = GetVocoderSpecWaveTableName(VocoderEffect);
            if (!WaveSampDictDoesWaveTableExist(
                Param.Dictionary,
                Name))
            {
                Param.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedWaveTable;
                Param.ErrorInfo.WaveTableName = Name;
                return SynthErrorCodes.eSynthErrorEx;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetVocoderSpecIndexEnvelope(VocoderEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckEnvelopeForUnreferencedSamples(
                GetVocoderSpecOutputGainEnvelope(VocoderEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetVocoderSpecIndexLFO(VocoderEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            Error = CheckLFOListForUnreferencedSamples(
                GetVocoderSpecOutputGainLFO(VocoderEffect),
                Param);
            if (Error != SynthErrorCodes.eSynthDone)
            {
                return Error;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check ideal lowpass filter effect */
        public static SynthErrorCodes CheckIdealLowpassEffectForUnreferencedSamples(
            IdealLPSpecRec IdealLPEffect,
            CheckUnrefParamRec Param)
        {
            return SynthErrorCodes.eSynthDone;
        }

        /* check convolver effect */
        private delegate string ConvolverSpecGetImpulseResponseForCheckUnrefMethod(ConvolverSpecRec Spec);
        private struct ConvolverCheckRec
        {
            public ConvolverSpecGetImpulseResponseForCheckUnrefMethod ConvolverSpecGetImpulseResponse;

            public ConvolverCheckRec(ConvolverSpecGetImpulseResponseForCheckUnrefMethod ConvolverSpecGetImpulseResponse)
            {
                this.ConvolverSpecGetImpulseResponse = ConvolverSpecGetImpulseResponse;
            }
        }
        private static readonly ConvolverCheckRec[] ConvolverCheckMono = new ConvolverCheckRec[]
        {
            new ConvolverCheckRec(ConvolverSpecGetImpulseResponseMono),
        };
        private static readonly ConvolverCheckRec[] ConvolverCheckStereo = new ConvolverCheckRec[]
        {
            new ConvolverCheckRec(ConvolverSpecGetImpulseResponseStereoLeft),
            new ConvolverCheckRec(ConvolverSpecGetImpulseResponseStereoRight),
        };
        private static readonly ConvolverCheckRec[] ConvolverCheckBiStereo = new ConvolverCheckRec[]
        {
            new ConvolverCheckRec(ConvolverSpecGetImpulseResponseBiStereoLeftIntoLeft),
            new ConvolverCheckRec(ConvolverSpecGetImpulseResponseBiStereoRightIntoLeft),
            new ConvolverCheckRec(ConvolverSpecGetImpulseResponseBiStereoLeftIntoRight),
            new ConvolverCheckRec(ConvolverSpecGetImpulseResponseBiStereoRightIntoRight),
        };
        public static SynthErrorCodes CheckConvolverEffectForUnreferencedSamples(
            ConvolverSpecRec ConvolverEffect,
            CheckUnrefParamRec Param)
        {
            ConvolverCheckRec[] RuleArray;

            switch (ConvolverSpecGetSourceType(ConvolverEffect))
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();

                case ConvolveSrcType.eConvolveMono:
                    RuleArray = ConvolverCheckMono;
                    break;

                case ConvolveSrcType.eConvolveStereo:
                    RuleArray = ConvolverCheckStereo;
                    break;

                case ConvolveSrcType.eConvolveBiStereo:
                    RuleArray = ConvolverCheckBiStereo;
                    break;
            }

            for (int i = 0; i < RuleArray.Length; i += 1)
            {
                string ImpulseResponse = RuleArray[i].ConvolverSpecGetImpulseResponse(ConvolverEffect);
                if (!WaveSampDictDoesSampleExist(
                    Param.Dictionary,
                    ImpulseResponse))
                {
                    Param.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedSample;
                    Param.ErrorInfo.SampleName = ImpulseResponse;
                    return SynthErrorCodes.eSynthErrorEx;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check user effect */
        public static SynthErrorCodes CheckUserEffectForUnreferencedSamples(
            UserEffectSpecRec UserEffect,
            CheckUnrefParamRec Param)
        {
            /* init func */
            {
                string FuncName = GetUserEffectSpecInitFuncName(UserEffect);
                if (FuncName != null) /* optional */
                {
                    FuncCodeRec FuncCode = Param.CodeCenter.ObtainFunctionHandle(FuncName);
                    if (FuncCode == null)
                    {
                        Param.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedFunction;
                        Param.ErrorInfo.FunctionName = FuncName;
                        return SynthErrorCodes.eSynthErrorEx;
                    }

                    DataTypes[] argsTypes;
                    DataTypes returnType;
                    UserEffectGetInitSignature(UserEffect, out argsTypes, out returnType);
                    FunctionSignature expectedSignature = new FunctionSignature(argsTypes, returnType);
                    FunctionSignature actualSignature = new FunctionSignature(
                        FuncCode.GetFunctionParameterTypeList(),
                        FuncCode.GetFunctionReturnType());
                    if (!FunctionSignature.Equals(expectedSignature, actualSignature))
                    {
                        Param.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExTypeMismatchFunction;
                        Param.ErrorInfo.FunctionName = FuncName;
                        Param.ErrorInfo.ExtraInfo = String.Format(
                            "{0}{0}Expected:{0}{1}{0}{0}Actual:{0}{2}",
                            Environment.NewLine,
                            expectedSignature,
                            actualSignature);
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                }
            }

            /* data func */
            {
                DataTypes[] argsTypes;
                DataTypes returnType;
                UserEffectGetDataSignature(UserEffect, out argsTypes, out returnType);
                FunctionSignature expectedSignature = new FunctionSignature(argsTypes, returnType);

                bool matched = false;
                List<KeyValuePair<string, FunctionSignature>> actualSignatures = new List<KeyValuePair<string, FunctionSignature>>();
                foreach (string FuncName in GetUserEffectSpecProcessDataFuncNames(UserEffect))
                {
                    FuncCodeRec FuncCode = Param.CodeCenter.ObtainFunctionHandle(FuncName);
                    if (FuncCode == null)
                    {
                        Param.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedFunction;
                        Param.ErrorInfo.FunctionName = FuncName;
                        return SynthErrorCodes.eSynthErrorEx;
                    }

                    FunctionSignature actualSignature = new FunctionSignature(
                        FuncCode.GetFunctionParameterTypeList(),
                        FuncCode.GetFunctionReturnType());
                    actualSignatures.Add(new KeyValuePair<string, FunctionSignature>(FuncName, actualSignature));
                    if (FunctionSignature.Equals(expectedSignature, actualSignature))
                    {
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    StringBuilder extraInfo = new StringBuilder();
                    extraInfo.AppendLine();
                    extraInfo.AppendLine();
                    extraInfo.AppendLine(String.Format("Expected - {0}", expectedSignature));
                    foreach (KeyValuePair<string, FunctionSignature> actualSignature in actualSignatures)
                    {
                        extraInfo.AppendLine();
                        extraInfo.AppendLine(String.Format("Actual - {0}{1}", actualSignature.Key, actualSignature.Value));
                    }
                    Param.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExTypeMismatchFunctionMultiple;
                    Param.ErrorInfo.ExtraInfo = extraInfo.ToString();
                    return SynthErrorCodes.eSynthErrorEx;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check envelope */
        public static SynthErrorCodes CheckEnvelopeForUnreferencedSamples(
            EnvelopeRec Envelope,
            CheckUnrefParamRec Param)
        {
            return SynthErrorCodes.eSynthDone;
        }

        /* check sample selector */
        public static SynthErrorCodes CheckSampleSelectorForUnreferencedSamples(
            SampleSelectorRec SampleSelector,
            CheckUnrefParamRec Param)
        {
            int Limit = GetSampleSelectorListLength(SampleSelector);
            for (int Scan = 0; Scan < Limit; Scan += 1)
            {
                string Name = GetSampleListEntryName(SampleSelector, Scan);
                if (!WaveSampDictDoesSampleExist(
                    Param.Dictionary,
                    Name))
                {
                    Param.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedSample;
                    Param.ErrorInfo.SampleName = Name;
                    return SynthErrorCodes.eSynthErrorEx;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* check wave table selector */
        public static SynthErrorCodes CheckWaveTableSelectorForUnreferencedSamples(
            SampleSelectorRec WaveTableSelector,
            CheckUnrefParamRec Param)
        {
            int Limit = GetSampleSelectorListLength(WaveTableSelector);
            for (int Scan = 0; Scan < Limit; Scan += 1)
            {
                string Name = GetSampleListEntryName(WaveTableSelector, Scan);
                if (!WaveSampDictDoesWaveTableExist(
                    Param.Dictionary,
                    Name))
                {
                    Param.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedWaveTable;
                    Param.ErrorInfo.WaveTableName = Name;
                    return SynthErrorCodes.eSynthErrorEx;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }
    }
}
