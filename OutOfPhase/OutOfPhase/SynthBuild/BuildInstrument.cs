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
        //
        //
        // BuildInstrument
        //
        //


        /* syntax errors */
        public enum BuildInstrErrors
        {
            eBuildInstr_Start,

            eBuildInstrNoError = eBuildInstr_Start,
            eBuildInstrGenericError,
            eBuildInstrUnexpectedInput,
            eBuildInstrExpectedInstrument,
            eBuildInstrExpectedOpenParen,
            eBuildInstrExpectedCloseParen,
            eBuildInstrExpectedSemicolon,
            eBuildInstrExpectedInstrumentMember,
            eBuildInstrMultipleInstrLoudness,
            eBuildInstrExpectedNumber,
            eBuildInstrExpectedStringOrIdentifier,
            eBuildInstrExpectedLFOMember,
            eBuildInstrMultipleLFOFreqEnvelope,
            eBuildInstrMultipleLFOAmpEnvelope,
            eBuildInstrMultipleLFOOscillatorType,
            eBuildInstrExpectedLFOOscillatorType,
            eBuildInstrMultipleLFOModulationType,
            eBuildInstrMultipleLFOAddingMode,
            eBuildInstrExpectedLFOModulationType,
            eBuildInstrExpectedOscillatorMember,
            eBuildInstrMultipleOscType,
            eBuildInstrMultipleOscSampleList,
            eBuildInstrMultipleOscLoudness,
            eBuildInstrMultipleOscLoudnessFactor,
            eBuildInstrMultipleOscFreqMultiplier,
            eBuildInstrMultipleOscFreqDivisor,
            eBuildInstrMultipleOscLoudnessEnvelope,
            eBuildInstrMultipleOscIndexEnvelope,
            eBuildInstrExpectedOscType,
            eBuildInstrExpectedInteger,
            eBuildInstrExpectedEnvelopeMember,
            eBuildInstrMultipleEnvTotalScaling,
            eBuildInstrMultipleEnvFormula,
            eBuildInstrMultipleEnvPoints,
            eBuildInstrExpectedDelayOrOrigin,
            eBuildInstrExpectedLevelOrScale,
            eBuildInstrExpectedEnvPointMember,
            eBuildInstrExpectedIntBetween1And3,
            eBuildInstrEnvSustainPointAlreadyDefined,
            eBuildInstrMultipleEnvPointAmpAccent1,
            eBuildInstrMultipleEnvPointAmpAccent2,
            eBuildInstrMultipleEnvPointAmpAccent3,
            eBuildInstrMultipleEnvPointAmpAccent4,
            eBuildInstrMultipleEnvPointAmpAccent5,
            eBuildInstrMultipleEnvPointAmpAccent6,
            eBuildInstrMultipleEnvPointAmpAccent7,
            eBuildInstrMultipleEnvPointAmpAccent8,
            eBuildInstrMultipleEnvPointAmpFreq,
            eBuildInstrMultipleEnvPointRateAccent1,
            eBuildInstrMultipleEnvPointRateAccent2,
            eBuildInstrMultipleEnvPointRateAccent3,
            eBuildInstrMultipleEnvPointRateAccent4,
            eBuildInstrMultipleEnvPointRateAccent5,
            eBuildInstrMultipleEnvPointRateAccent6,
            eBuildInstrMultipleEnvPointRateAccent7,
            eBuildInstrMultipleEnvPointRateAccent8,
            eBuildInstrMultipleEnvPointRateFreq,
            eBuildInstrMultipleEnvPointCurveSpec,
            eBuildInstrMultipleOscStereoBias,
            eBuildInstrMultipleOscDisplacement,
            eBuildInstrMultipleOscSurroundBias,
            eBuildInstrMultipleOscFreqAdder,
            eBuildInstrExpectedSquareOrTriangle,
            eBuildInstrExpectedEffectName,
            eBuildInstrExpectedDelayLineElem,
            eBuildInstrExpectedTapChannel,
            eBuildInstrExpectedTo,
            eBuildInstrExpectedScale,
            eBuildInstrExpectedTapAttr,
            eBuildInstrMultipleSourceAccent1,
            eBuildInstrMultipleSourceAccent2,
            eBuildInstrMultipleSourceAccent3,
            eBuildInstrMultipleSourceAccent4,
            eBuildInstrMultipleTargetAccent1,
            eBuildInstrMultipleTargetAccent2,
            eBuildInstrMultipleTargetAccent3,
            eBuildInstrMultipleTargetAccent4,
            eBuildInstrMultipleScaleAccent1,
            eBuildInstrMultipleScaleAccent2,
            eBuildInstrMultipleScaleAccent3,
            eBuildInstrMultipleScaleAccent4,
            eBuildInstrMultipleFilter,
            eBuildInstrMultipleMaxDelayTime,
            eBuildInstrExpectedSlope,
            eBuildInstrExpectedCenter,
            eBuildInstrExpectedSamplelist,
            eBuildInstrExpectedEnvelope,
            eBuildInstrExpectedWavetable,
            eBuildInstrExpectedInputscaling,
            eBuildInstrExpectedOutputscaling,
            eBuildInstrExpectedNLAttribute,
            eBuildInstrMultipleInputaccent1,
            eBuildInstrMultipleInputaccent2,
            eBuildInstrMultipleInputaccent3,
            eBuildInstrMultipleInputaccent4,
            eBuildInstrMultipleOutputaccent1,
            eBuildInstrMultipleOutputaccent2,
            eBuildInstrMultipleOutputaccent3,
            eBuildInstrMultipleOutputaccent4,
            eBuildInstrMultipleIndexaccent1,
            eBuildInstrMultipleIndexaccent2,
            eBuildInstrMultipleIndexaccent3,
            eBuildInstrMultipleIndexaccent4,
            eBuildInstrExpectedWavetableindex,
            eBuildInstrExpectedFilterType,
            eBuildInstrExpectedFreq,
            eBuildInstrExpectedBandwidth,
            eBuildInstrExpectedDefaultScaling,
            eBuildInstrExpectedResonScaling,
            eBuildInstrExpectedZeroScaling,
            eBuildInstrExpectedFilterAttr,
            eBuildInstrMultipleFreqaccent1,
            eBuildInstrMultipleFreqaccent2,
            eBuildInstrMultipleFreqaccent3,
            eBuildInstrMultipleFreqaccent4,
            eBuildInstrMultipleBandwidthaccent1,
            eBuildInstrMultipleBandwidthaccent2,
            eBuildInstrMultipleBandwidthaccent3,
            eBuildInstrMultipleBandwidthaccent4,
            eBuildInstrMultipleOutputScaling,
            eBuildInstrMultipleOutputScalingAccent1,
            eBuildInstrMultipleOutputScalingAccent2,
            eBuildInstrMultipleOutputScalingAccent3,
            eBuildInstrMultipleOutputScalingAccent4,
            eBuildInstrExpectedFilterChannel,
            eBuildInstrNullFilterHasNoFreqAccentX,
            eBuildInstrFilterHasNoBandwidthAccentX,
            eBuildInstrExpectedScoreEffect,
            eBuildInstrExpectedOscillatorEffect,
            eBuildInstrExpectedSourceEnvelope,
            eBuildInstrExpectedTargetEnvelope,
            eBuildInstrExpectedScaleEnvelope,
            eBuildInstrExpectedSourceLfoOrTo,
            eBuildInstrExpectedTargetLfoOrScaleEnvelope,
            eBuildInstrExpectedScaleLfoCutoffOrSemicolon,
            eBuildInstrExpectedInputScalingEnvelope,
            eBuildInstrExpectedOutputScalingEnvelope,
            eBuildInstrExpectedIndexEnvelope,
            eBuildInstrExpectedInputScalingLfoOrOutputScalingEnvelope,
            eBuildInstrExpectedOutputScalingLfoOrIndexEnvelope,
            eBuildInstrExpectedIndexLfoOrSemicolon,
            eBuildInstrExpectedFreqEnvelope,
            eBuildInstrExpectedBandwidthEnvelope,
            eBuildInstrExpectedFreqLfoOrScalingOrChannel,
            eBuildInstrExpectedBandwidthLfoOrScalingOrChannel,
            eBuildInstrExpectedOutputScalingLfoOrSemicolon,
            eBuildInstrExpectedFilter,
            eBuildInstrExpectedFreqLfoOrSemicolon,
            eBuildInstrMultipleDelayLowpassFreq,
            eBuildInstrMultipleDelayFilterAccent1,
            eBuildInstrMultipleDelayFilterAccent2,
            eBuildInstrMultipleDelayFilterAccent3,
            eBuildInstrMultipleDelayFilterAccent4,
            eBuildInstrExpectedIndexLFOOrSemicolon,
            eBuildInstrMultiplePitchControl,
            eBuildInstrExpectedGain,
            eBuildInstrMultipleGainAccent1,
            eBuildInstrMultipleGainAccent2,
            eBuildInstrMultipleGainAccent3,
            eBuildInstrMultipleGainAccent4,
            eBuildInstrExpectedGainEnvelope,
            eBuildInstrExpectedGainLfoOrScalingOrChannel,
            eBuildInstrFilterHasNoGainAccentX,
            eBuildInstrExpectedOrder,
            eBuildInstrOrderMustBeNonNegativeEvenInteger,
            eBuildInstrExpectedRate,
            eBuildInstrExpectedTruncateOrInterpolate,
            eBuildInstrMultipleSourceAccent5,
            eBuildInstrMultipleSourceAccent6,
            eBuildInstrMultipleSourceAccent7,
            eBuildInstrMultipleSourceAccent8,
            eBuildInstrMultipleTargetAccent5,
            eBuildInstrMultipleTargetAccent6,
            eBuildInstrMultipleTargetAccent7,
            eBuildInstrMultipleTargetAccent8,
            eBuildInstrMultipleScaleAccent5,
            eBuildInstrMultipleScaleAccent6,
            eBuildInstrMultipleScaleAccent7,
            eBuildInstrMultipleScaleAccent8,
            eBuildInstrMultipleDelayFilterAccent5,
            eBuildInstrMultipleDelayFilterAccent6,
            eBuildInstrMultipleDelayFilterAccent7,
            eBuildInstrMultipleDelayFilterAccent8,
            eBuildInstrMultipleInputaccent5,
            eBuildInstrMultipleInputaccent6,
            eBuildInstrMultipleInputaccent7,
            eBuildInstrMultipleInputaccent8,
            eBuildInstrMultipleOutputaccent5,
            eBuildInstrMultipleOutputaccent6,
            eBuildInstrMultipleOutputaccent7,
            eBuildInstrMultipleOutputaccent8,
            eBuildInstrMultipleIndexaccent5,
            eBuildInstrMultipleIndexaccent6,
            eBuildInstrMultipleIndexaccent7,
            eBuildInstrMultipleIndexaccent8,
            eBuildInstrMultipleOverflow,
            eBuildInstrMultipleFreqaccent5,
            eBuildInstrMultipleFreqaccent6,
            eBuildInstrMultipleFreqaccent7,
            eBuildInstrMultipleFreqaccent8,
            eBuildInstrMultipleBandwidthaccent5,
            eBuildInstrMultipleBandwidthaccent6,
            eBuildInstrMultipleBandwidthaccent7,
            eBuildInstrMultipleBandwidthaccent8,
            eBuildInstrMultipleOutputScalingAccent5,
            eBuildInstrMultipleOutputScalingAccent6,
            eBuildInstrMultipleOutputScalingAccent7,
            eBuildInstrMultipleOutputScalingAccent8,
            eBuildInstrMultipleGainAccent5,
            eBuildInstrMultipleGainAccent6,
            eBuildInstrMultipleGainAccent7,
            eBuildInstrMultipleGainAccent8,
            eBuildInstrMultipleOscFOFSampRate,
            eBuildInstrMultipleOscFOFCompress,
            eBuildInstrExpectedOverlapOrDiscard,
            eBuildInstrMultipleOscFOFExpand,
            eBuildInstrExpectedSilenceOrLoop,
            eBuildInstrMultipleFOFSamplingRateEnvelope,
            eBuildInstrMissingRequiredOscillator,
            eBuildInstrMissingRequiredAmpEnvelope,
            eBuildInstrMissingRequiredOscillatorType,
            eBuildInstrMissingRequiredOscillatorLoudnessEnvelope,
            eBuildInstrMissingRequiredOscillatorSampleList,
            eBuildInstrMissingRequiredOscillatorFOFSampRate,
            eBuildInstrMissingRequiredOscillatorFOFCompress,
            eBuildInstrMissingRequiredOscillatorFOFExpand,
            eBuildInstrMissingRequiredOscillatorFOFEnvelope,
            eBuildInstrMissingRequiredDelayLineMaxTime,
            eBuildInstrMultipleLFOFilter,
            eBuildInstrExpectedNormalPower,
            eBuildInstrExpectedThreshPower,
            eBuildInstrExpectedRatio,
            eBuildInstrExpectedFilterCutoff,
            eBuildInstrExpectedDecayRate,
            eBuildInstrExpectedAttackRate,
            eBuildInstrExpectedLimitingExcess,
            eBuildInstrExpectedCompressorAttribute,
            eBuildInstrMultipleNormalPowerAccent1,
            eBuildInstrMultipleNormalPowerAccent2,
            eBuildInstrMultipleNormalPowerAccent3,
            eBuildInstrMultipleNormalPowerAccent4,
            eBuildInstrMultipleNormalPowerAccent5,
            eBuildInstrMultipleNormalPowerAccent6,
            eBuildInstrMultipleNormalPowerAccent7,
            eBuildInstrMultipleNormalPowerAccent8,
            eBuildInstrMultipleThreshPowerAccent1,
            eBuildInstrMultipleThreshPowerAccent2,
            eBuildInstrMultipleThreshPowerAccent3,
            eBuildInstrMultipleThreshPowerAccent4,
            eBuildInstrMultipleThreshPowerAccent5,
            eBuildInstrMultipleThreshPowerAccent6,
            eBuildInstrMultipleThreshPowerAccent7,
            eBuildInstrMultipleThreshPowerAccent8,
            eBuildInstrMultipleRatioAccent1,
            eBuildInstrMultipleRatioAccent2,
            eBuildInstrMultipleRatioAccent3,
            eBuildInstrMultipleRatioAccent4,
            eBuildInstrMultipleRatioAccent5,
            eBuildInstrMultipleRatioAccent6,
            eBuildInstrMultipleRatioAccent7,
            eBuildInstrMultipleRatioAccent8,
            eBuildInstrMultipleFilterCutoffAccent1,
            eBuildInstrMultipleFilterCutoffAccent2,
            eBuildInstrMultipleFilterCutoffAccent3,
            eBuildInstrMultipleFilterCutoffAccent4,
            eBuildInstrMultipleFilterCutoffAccent5,
            eBuildInstrMultipleFilterCutoffAccent6,
            eBuildInstrMultipleFilterCutoffAccent7,
            eBuildInstrMultipleFilterCutoffAccent8,
            eBuildInstrMultipleDecayRateAccent1,
            eBuildInstrMultipleDecayRateAccent2,
            eBuildInstrMultipleDecayRateAccent3,
            eBuildInstrMultipleDecayRateAccent4,
            eBuildInstrMultipleDecayRateAccent5,
            eBuildInstrMultipleDecayRateAccent6,
            eBuildInstrMultipleDecayRateAccent7,
            eBuildInstrMultipleDecayRateAccent8,
            eBuildInstrMultipleAttackRateAccent1,
            eBuildInstrMultipleAttackRateAccent2,
            eBuildInstrMultipleAttackRateAccent3,
            eBuildInstrMultipleAttackRateAccent4,
            eBuildInstrMultipleAttackRateAccent5,
            eBuildInstrMultipleAttackRateAccent6,
            eBuildInstrMultipleAttackRateAccent7,
            eBuildInstrMultipleAttackRateAccent8,
            eBuildInstrMultipleLimitingExcessAccent1,
            eBuildInstrMultipleLimitingExcessAccent2,
            eBuildInstrMultipleLimitingExcessAccent3,
            eBuildInstrMultipleLimitingExcessAccent4,
            eBuildInstrMultipleLimitingExcessAccent5,
            eBuildInstrMultipleLimitingExcessAccent6,
            eBuildInstrMultipleLimitingExcessAccent7,
            eBuildInstrMultipleLimitingExcessAccent8,
            eBuildInstrExpectedOutputScalingLfoOrNormalPowerEnvelope,
            eBuildInstrExpectedNormalPowerEnvelope,
            eBuildInstrExpectedNormalPowerLfoOrThreshPowerEnvelope,
            eBuildInstrExpectedThreshPowerEnvelope,
            eBuildInstrExpectedThreshPowerLfoOrRatioEnvelope,
            eBuildInstrExpectedRatioEnvelope,
            eBuildInstrExpectedRatioLfoOrFilterCutoffEnvelope,
            eBuildInstrExpectedFilterCutoffEnvelope,
            eBuildInstrExpectedFilterCutoffLfoOrDecayRateEnvelope,
            eBuildInstrExpectedDecayRateEnvelope,
            eBuildInstrExpectedDecayRateLfoOrAttackRateEnvelope,
            eBuildInstrExpectedAttackRateEnvelope,
            eBuildInstrExpectedAttackRateLfoOrLimitingExcessEnvelope,
            eBuildInstrExpectedLimitingExcessEnvelope,
            eBuildInstrExpectedLimitingExcessLfoOrSemicolon,
            eBuildInstrExpectedSemicolonOrEstimatePower,
            eBuildInstrExpectedSourceLfoOrToOrInterpolate,
            eBuildInstrExpectedToOrInterpolate,
            eBuildInstrExpectedEstimatePower,
            eBuildInstrExpectedAbsValOrRMS,
            eBuildInstrExpectedAbsValRMSOrPeak,
            eBuildInstrMultipleLFOSampleHolds,
            eBuildInstrExpectedOscillatorEffectOrDisabled,
            eBuildInstrExpectedEffectNameOrDisabled,
            eBuildInstrMaxBandMustBeOneOrGreater,
            eBuildInstrExpectedOutputgain,
            eBuildInstrExpectedOutputaccentIndexaccentOrSemicolon,
            eBuildInstrExpectedIndexlfoOrOutputgainenvelope,
            eBuildInstrExpectedOutputgainlfoOrSemicolon,
            eBuildInstrExpectedMaxbandcount,
            eBuildInstrOrderMustBePositiveOddInteger,
            eBuildInstrExpectedSectionEffect,
            eBuildInstrParameqObsolete,
            eBuildInstrEnvFormulaMustHaveTypeDouble,
            eBuildInstrEnvFormulaSyntaxError,
            eBuildInstrExpectedQuotedFormula,
            eBuildInstrExpectedDecibels,
            eBuildInstrExpectedWindowduration,
            eBuildInstrExpectedReportOrSemicolon,
            eBuildInstrQuiescenceAlreadySpecified,
            eBuildInstrDecibelsMustBeGEZero,
            eBuildInstrWindowDurationMustBeGEZero,
            eBuildInstrExpectedSlopeEnvelope,
            eBuildInstrExpectedSlopeLfoOrScalingOrChannel,
            eBuildInstrFilterHasNoSlopeAccentX,
            eBuildInstrMultipleSlopeaccent1,
            eBuildInstrMultipleSlopeaccent2,
            eBuildInstrMultipleSlopeaccent3,
            eBuildInstrMultipleSlopeaccent4,
            eBuildInstrMultipleSlopeaccent5,
            eBuildInstrMultipleSlopeaccent6,
            eBuildInstrMultipleSlopeaccent7,
            eBuildInstrMultipleSlopeaccent8,
            eBuildInstrRLP2OrderMustBe24Or6,
            eBuildInstrOnlyAllowedInScoreEffects,
            eBuildInstrExpectedLeftRightAverageMax,
            eBuildInstrExpectedAbsValOrSmoothedx,
            eBuildInstrExpectedLogOrLin,
            eBuildInstrExpectedMin,
            eBuildInstrExpectedMax,
            eBuildInstrExpectedMinMustBePositiveForLog,
            eBuildInstrExpectedMaxMustBeGreaterThanMin,
            eBuildInstrExpectedMinMustBeNonNegForLin,
            eBuildInstrExpectedNumbins,
            eBuildInstrExpectedMustHaveAtLeastOneBin,
            eBuildInstrExpectedBinMustBeInteger,
            eBuildInstrExpectedDiscardOrNoDiscard,
            eBuildInstrExpectedBars,
            eBuildInstrExpectedBarsCantBeNegative,
            eBuildInstrExpectedBarsMustBeInteger,
            eBuildInstrMultipleNetworks,
            eBuildInstrExpectedIdentifier,
            eBuildInstrExpectedEqualOrPlusEqual,
            eBuildInstrExpectedWaveVarOrEnvelope,
            eBuildInstrExpectedPlusOrMinus,
            eBuildInstrExpectedAsterisk,
            eBuildInstrPlusEqualsNotAllowed,
            eBuildInstrExpectedWaveComponent,
            eBuildInstrExpectedGainOrSemicolon,
            eBuildInstrFMWaveFreqMultiplierAlreadySpecified,
            eBuildInstrFMWaveFreqDivisorAlreadySpecified,
            eBuildInstrFMWaveFreqAdderAlreadySpecified,
            eBuildInstrFMWavePhaseAddAlreadySpecified,
            eBuildInstrFMWaveIndexAlreadySpecified,
            eBuildInstrExpectedLFOOrSemicolon,
            eBuildInstrMissingRequiredOscillatorNetwork,
            eBuildInstrFMWaveSamplelistAlreadySpecified,
            eBuildInstrExpectedSampleName,
            eBuildInstrExpectedConvolverInputType,
            eBuildInstrExpectedSample,
            eBuildInstrExpectedLeft,
            eBuildInstrExpectedRight,
            eBuildInstrExpectedLeftIntoLeft,
            eBuildInstrExpectedRightIntoLeft,
            eBuildInstrExpectedLeftIntoRight,
            eBuildInstrExpectedRightIntoRight,
            eBuildInstrExpectedDirectgain,
            eBuildInstrExpectedProcessedgain,
            eBuildInstrExpectedLatencyOrSemicolon,
            eBuildInstrLatencyMustBeAtLeastZero,
            eBuildInstrExpectedInitfunc,
            eBuildInstrExpectedInitfuncString,
            eBuildInstrExpectedDatafunc,
            eBuildInstrExpectedDatafuncString,
            eBuildInstrExpectedParamOrCParen,
            eBuildInstrExpectedAccentOrSemicolon,
            eBuildInstrMultipleAccent1,
            eBuildInstrMultipleAccent2,
            eBuildInstrMultipleAccent3,
            eBuildInstrMultipleAccent4,
            eBuildInstrMultipleAccent5,
            eBuildInstrMultipleAccent6,
            eBuildInstrMultipleAccent7,
            eBuildInstrMultipleAccent8,
            eBuildInstrExpectedWrapOrClamp,
            eBuildInstrExpectedMinsamplingrateOrSemicolon,
#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
            eBuildInstrMultipleLFOLoopEnvelope,
#endif
            eBuildInstrReferencedUnknownPluggableProcessor,
            eBuildInstrExpectedPluggableParameterIdentifier,
            eBuildInstrPluggableProcessorMultiplyReferenced,
            eBuildInstrPluggableRequiredParameterNotSpecified,
            eBuildInstrInvalidPluggableEnumIdentifier,
            eBuildInstrExpectedOpenParenOrColon,
            eBuildInstrExpectedEnvelopeLfoOrSemicolon,
            eBuildInstrPluggableNotCapableOfOscillator,
            eBuildInstrPluggableNotCapableOfEffect,
            eBuildInstrExpectedWorkspaces,
            eBuildInstrExpectedWorkspaceElementOrSemicolon,
            eBuildInstrPluggableConfigAlreadySpecified,
            eBuildInstrPluggableConfigParamsAlreadySpecified,
            eBuildInstrPluggableConfigParamsExpectedColon,
            eBuildInstrPluggableConfigParamsExpectedEquals,
            eBuildInstrPluggableConfigParamsExpectedNumber,
            eBuildInstrPluggableConfigParamsExpectedCommaOrSemicolon,
            eBuildInstrPluggableRequiredConfigParamIsMissing,
            eBuildInstrPluggableConfigParamsWrongNumberOfItems,
            eBuildInstrPluggableConfigParamsMustComeBeforePluggableEffectParams,
            eBuildInstrMultipleInterpolate,
            eBuildInstrExpectedOnOrOff,
            eBuildInstrInterpolateNotMeaningfulForThatOscillatorType,
            eBuildInstrLFOInterpolateOnlyAppliesToWaveTable,

            eBuildInstr_End,
        }

        private static KeywordRec<InstrKeywordType>[] InstrKeywordTable = new KeywordRec<InstrKeywordType>[]
        {
            new KeywordRec<InstrKeywordType>("absval", InstrKeywordType.eKeywordAbsval),
            new KeywordRec<InstrKeywordType>("accent1", InstrKeywordType.eKeywordAccent1),
            new KeywordRec<InstrKeywordType>("accent2", InstrKeywordType.eKeywordAccent2),
            new KeywordRec<InstrKeywordType>("accent3", InstrKeywordType.eKeywordAccent3),
            new KeywordRec<InstrKeywordType>("accent4", InstrKeywordType.eKeywordAccent4),
            new KeywordRec<InstrKeywordType>("accent5", InstrKeywordType.eKeywordAccent5),
            new KeywordRec<InstrKeywordType>("accent6", InstrKeywordType.eKeywordAccent6),
            new KeywordRec<InstrKeywordType>("accent7", InstrKeywordType.eKeywordAccent7),
            new KeywordRec<InstrKeywordType>("accent8", InstrKeywordType.eKeywordAccent8),
            new KeywordRec<InstrKeywordType>("additive", InstrKeywordType.eKeywordAdditive),
            new KeywordRec<InstrKeywordType>("ampaccent1", InstrKeywordType.eKeywordAmpaccent1),
            new KeywordRec<InstrKeywordType>("ampaccent2", InstrKeywordType.eKeywordAmpaccent2),
            new KeywordRec<InstrKeywordType>("ampaccent3", InstrKeywordType.eKeywordAmpaccent3),
            new KeywordRec<InstrKeywordType>("ampaccent4", InstrKeywordType.eKeywordAmpaccent4),
            new KeywordRec<InstrKeywordType>("ampaccent5", InstrKeywordType.eKeywordAmpaccent5),
            new KeywordRec<InstrKeywordType>("ampaccent6", InstrKeywordType.eKeywordAmpaccent6),
            new KeywordRec<InstrKeywordType>("ampaccent7", InstrKeywordType.eKeywordAmpaccent7),
            new KeywordRec<InstrKeywordType>("ampaccent8", InstrKeywordType.eKeywordAmpaccent8),
            new KeywordRec<InstrKeywordType>("ampenvelope", InstrKeywordType.eKeywordAmpenvelope),
            new KeywordRec<InstrKeywordType>("ampfreq", InstrKeywordType.eKeywordAmpfreq),
            new KeywordRec<InstrKeywordType>("amplfo", InstrKeywordType.eKeywordAmplfo),
            new KeywordRec<InstrKeywordType>("analyzer", InstrKeywordType.eKeywordAnalyzer),
            new KeywordRec<InstrKeywordType>("attackrate", InstrKeywordType.eKeywordAttackrate),
            new KeywordRec<InstrKeywordType>("attackrateaccent1", InstrKeywordType.eKeywordAttackrateaccent1),
            new KeywordRec<InstrKeywordType>("attackrateaccent2", InstrKeywordType.eKeywordAttackrateaccent2),
            new KeywordRec<InstrKeywordType>("attackrateaccent3", InstrKeywordType.eKeywordAttackrateaccent3),
            new KeywordRec<InstrKeywordType>("attackrateaccent4", InstrKeywordType.eKeywordAttackrateaccent4),
            new KeywordRec<InstrKeywordType>("attackrateaccent5", InstrKeywordType.eKeywordAttackrateaccent5),
            new KeywordRec<InstrKeywordType>("attackrateaccent6", InstrKeywordType.eKeywordAttackrateaccent6),
            new KeywordRec<InstrKeywordType>("attackrateaccent7", InstrKeywordType.eKeywordAttackrateaccent7),
            new KeywordRec<InstrKeywordType>("attackrateaccent8", InstrKeywordType.eKeywordAttackrateaccent8),
            new KeywordRec<InstrKeywordType>("attackrateenvelope", InstrKeywordType.eKeywordAttackrateenvelope),
            new KeywordRec<InstrKeywordType>("attackratelfo", InstrKeywordType.eKeywordAttackratelfo),
            new KeywordRec<InstrKeywordType>("autoquiescence", InstrKeywordType.eKeywordAutoquiescence),
            new KeywordRec<InstrKeywordType>("averageafter", InstrKeywordType.eKeywordAverageafter),
            new KeywordRec<InstrKeywordType>("averagebefore", InstrKeywordType.eKeywordAveragebefore),
            new KeywordRec<InstrKeywordType>("bandwidth", InstrKeywordType.eKeywordBandwidth),
            new KeywordRec<InstrKeywordType>("bandwidthaccent1", InstrKeywordType.eKeywordBandwidthaccent1),
            new KeywordRec<InstrKeywordType>("bandwidthaccent2", InstrKeywordType.eKeywordBandwidthaccent2),
            new KeywordRec<InstrKeywordType>("bandwidthaccent3", InstrKeywordType.eKeywordBandwidthaccent3),
            new KeywordRec<InstrKeywordType>("bandwidthaccent4", InstrKeywordType.eKeywordBandwidthaccent4),
            new KeywordRec<InstrKeywordType>("bandwidthaccent5", InstrKeywordType.eKeywordBandwidthaccent5),
            new KeywordRec<InstrKeywordType>("bandwidthaccent6", InstrKeywordType.eKeywordBandwidthaccent6),
            new KeywordRec<InstrKeywordType>("bandwidthaccent7", InstrKeywordType.eKeywordBandwidthaccent7),
            new KeywordRec<InstrKeywordType>("bandwidthaccent8", InstrKeywordType.eKeywordBandwidthaccent8),
            new KeywordRec<InstrKeywordType>("bandwidthenvelope", InstrKeywordType.eKeywordBandwidthenvelope),
            new KeywordRec<InstrKeywordType>("bandwidthlfo", InstrKeywordType.eKeywordBandwidthlfo),
            new KeywordRec<InstrKeywordType>("bars", InstrKeywordType.eKeywordBars),
            new KeywordRec<InstrKeywordType>("bistereo", InstrKeywordType.eKeywordBistereo),
            new KeywordRec<InstrKeywordType>("broken", InstrKeywordType.eKeywordBroken),
            new KeywordRec<InstrKeywordType>("butterworthbandpass", InstrKeywordType.eKeywordButterworthbandpass),
            new KeywordRec<InstrKeywordType>("butterworthbandreject", InstrKeywordType.eKeywordButterworthbandreject),
            new KeywordRec<InstrKeywordType>("butterworthhighpass", InstrKeywordType.eKeywordButterworthhighpass),
            new KeywordRec<InstrKeywordType>("butterworthlowpass", InstrKeywordType.eKeywordButterworthlowpass),
            new KeywordRec<InstrKeywordType>("center", InstrKeywordType.eKeywordCenter),
            new KeywordRec<InstrKeywordType>("clamp", InstrKeywordType.eKeywordClamp),
            new KeywordRec<InstrKeywordType>("compressor", InstrKeywordType.eKeywordCompressor),
            new KeywordRec<InstrKeywordType>("constant", InstrKeywordType.eKeywordConstant),
            new KeywordRec<InstrKeywordType>("convolver", InstrKeywordType.eKeywordConvolver),
            new KeywordRec<InstrKeywordType>("datafunc", InstrKeywordType.eKeywordDatafunc),
            new KeywordRec<InstrKeywordType>("decayrate", InstrKeywordType.eKeywordDecayrate),
            new KeywordRec<InstrKeywordType>("decayrateaccent1", InstrKeywordType.eKeywordDecayrateaccent1),
            new KeywordRec<InstrKeywordType>("decayrateaccent2", InstrKeywordType.eKeywordDecayrateaccent2),
            new KeywordRec<InstrKeywordType>("decayrateaccent3", InstrKeywordType.eKeywordDecayrateaccent3),
            new KeywordRec<InstrKeywordType>("decayrateaccent4", InstrKeywordType.eKeywordDecayrateaccent4),
            new KeywordRec<InstrKeywordType>("decayrateaccent5", InstrKeywordType.eKeywordDecayrateaccent5),
            new KeywordRec<InstrKeywordType>("decayrateaccent6", InstrKeywordType.eKeywordDecayrateaccent6),
            new KeywordRec<InstrKeywordType>("decayrateaccent7", InstrKeywordType.eKeywordDecayrateaccent7),
            new KeywordRec<InstrKeywordType>("decayrateaccent8", InstrKeywordType.eKeywordDecayrateaccent8),
            new KeywordRec<InstrKeywordType>("decayrateenvelope", InstrKeywordType.eKeywordDecayrateenvelope),
            new KeywordRec<InstrKeywordType>("decayratelfo", InstrKeywordType.eKeywordDecayratelfo),
            new KeywordRec<InstrKeywordType>("decibels", InstrKeywordType.eKeywordDecibels),
            new KeywordRec<InstrKeywordType>("defaultscaling", InstrKeywordType.eKeywordDefaultscaling),
            new KeywordRec<InstrKeywordType>("delay", InstrKeywordType.eKeywordDelay),
            new KeywordRec<InstrKeywordType>("delayline", InstrKeywordType.eKeywordDelayline),
            new KeywordRec<InstrKeywordType>("directgain", InstrKeywordType.eKeywordDirectgain),
            new KeywordRec<InstrKeywordType>("disabled", InstrKeywordType.eKeywordDisabled),
            new KeywordRec<InstrKeywordType>("discard", InstrKeywordType.eKeywordDiscard),
            new KeywordRec<InstrKeywordType>("discardunders", InstrKeywordType.eKeywordDiscardunders),
            new KeywordRec<InstrKeywordType>("displacement", InstrKeywordType.eKeywordDisplacement),
            new KeywordRec<InstrKeywordType>("doublearray", InstrKeywordType.eKeywordDoublearray),
            new KeywordRec<InstrKeywordType>("effect", InstrKeywordType.eKeywordEffect),
            new KeywordRec<InstrKeywordType>("envelope", InstrKeywordType.eKeywordEnvelope),
            new KeywordRec<InstrKeywordType>("estimatepower", InstrKeywordType.eKeywordEstimatepower),
            new KeywordRec<InstrKeywordType>("exponential", InstrKeywordType.eKeywordExponential),
            new KeywordRec<InstrKeywordType>("filter", InstrKeywordType.eKeywordFilter),
            new KeywordRec<InstrKeywordType>("filtercutoff", InstrKeywordType.eKeywordFiltercutoff),
            new KeywordRec<InstrKeywordType>("filtercutoffaccent1", InstrKeywordType.eKeywordFiltercutoffaccent1),
            new KeywordRec<InstrKeywordType>("filtercutoffaccent2", InstrKeywordType.eKeywordFiltercutoffaccent2),
            new KeywordRec<InstrKeywordType>("filtercutoffaccent3", InstrKeywordType.eKeywordFiltercutoffaccent3),
            new KeywordRec<InstrKeywordType>("filtercutoffaccent4", InstrKeywordType.eKeywordFiltercutoffaccent4),
            new KeywordRec<InstrKeywordType>("filtercutoffaccent5", InstrKeywordType.eKeywordFiltercutoffaccent5),
            new KeywordRec<InstrKeywordType>("filtercutoffaccent6", InstrKeywordType.eKeywordFiltercutoffaccent6),
            new KeywordRec<InstrKeywordType>("filtercutoffaccent7", InstrKeywordType.eKeywordFiltercutoffaccent7),
            new KeywordRec<InstrKeywordType>("filtercutoffaccent8", InstrKeywordType.eKeywordFiltercutoffaccent8),
            new KeywordRec<InstrKeywordType>("filtercutoffenvelope", InstrKeywordType.eKeywordFiltercutoffenvelope),
            new KeywordRec<InstrKeywordType>("filtercutofflfo", InstrKeywordType.eKeywordFiltercutofflfo),
            new KeywordRec<InstrKeywordType>("floatarray", InstrKeywordType.eKeywordFloatarray),
            new KeywordRec<InstrKeywordType>("fof", InstrKeywordType.eKeywordFof),
            new KeywordRec<InstrKeywordType>("fofcompress", InstrKeywordType.eKeywordFofcompress),
            new KeywordRec<InstrKeywordType>("fofexpand", InstrKeywordType.eKeywordFofexpand),
            new KeywordRec<InstrKeywordType>("fofsamprate", InstrKeywordType.eKeywordFofsamprate),
            new KeywordRec<InstrKeywordType>("fofsamprateenvelope", InstrKeywordType.eKeywordFofsamprateenvelope),
            new KeywordRec<InstrKeywordType>("fofsampratelfo", InstrKeywordType.eKeywordFofsampratelfo),
            new KeywordRec<InstrKeywordType>("formula", InstrKeywordType.eKeywordFormula),
            new KeywordRec<InstrKeywordType>("freq", InstrKeywordType.eKeywordFreq),
            new KeywordRec<InstrKeywordType>("freqaccent1", InstrKeywordType.eKeywordFreqaccent1),
            new KeywordRec<InstrKeywordType>("freqaccent2", InstrKeywordType.eKeywordFreqaccent2),
            new KeywordRec<InstrKeywordType>("freqaccent3", InstrKeywordType.eKeywordFreqaccent3),
            new KeywordRec<InstrKeywordType>("freqaccent4", InstrKeywordType.eKeywordFreqaccent4),
            new KeywordRec<InstrKeywordType>("freqaccent5", InstrKeywordType.eKeywordFreqaccent5),
            new KeywordRec<InstrKeywordType>("freqaccent6", InstrKeywordType.eKeywordFreqaccent6),
            new KeywordRec<InstrKeywordType>("freqaccent7", InstrKeywordType.eKeywordFreqaccent7),
            new KeywordRec<InstrKeywordType>("freqaccent8", InstrKeywordType.eKeywordFreqaccent8),
            new KeywordRec<InstrKeywordType>("freqadder", InstrKeywordType.eKeywordFreqadder),
            new KeywordRec<InstrKeywordType>("freqdivisor", InstrKeywordType.eKeywordFreqdivisor),
            new KeywordRec<InstrKeywordType>("freqenvelope", InstrKeywordType.eKeywordFreqenvelope),
            new KeywordRec<InstrKeywordType>("freqlfo", InstrKeywordType.eKeywordFreqlfo),
            new KeywordRec<InstrKeywordType>("freqmultiplier", InstrKeywordType.eKeywordFreqmultiplier),
            new KeywordRec<InstrKeywordType>("frequencylfo", InstrKeywordType.eKeywordFrequencylfo),
            new KeywordRec<InstrKeywordType>("gain", InstrKeywordType.eKeywordGain),
            new KeywordRec<InstrKeywordType>("gainaccent1", InstrKeywordType.eKeywordGainaccent1),
            new KeywordRec<InstrKeywordType>("gainaccent2", InstrKeywordType.eKeywordGainaccent2),
            new KeywordRec<InstrKeywordType>("gainaccent3", InstrKeywordType.eKeywordGainaccent3),
            new KeywordRec<InstrKeywordType>("gainaccent4", InstrKeywordType.eKeywordGainaccent4),
            new KeywordRec<InstrKeywordType>("gainaccent5", InstrKeywordType.eKeywordGainaccent5),
            new KeywordRec<InstrKeywordType>("gainaccent6", InstrKeywordType.eKeywordGainaccent6),
            new KeywordRec<InstrKeywordType>("gainaccent7", InstrKeywordType.eKeywordGainaccent7),
            new KeywordRec<InstrKeywordType>("gainaccent8", InstrKeywordType.eKeywordGainaccent8),
            new KeywordRec<InstrKeywordType>("gainenvelope", InstrKeywordType.eKeywordGainenvelope),
            new KeywordRec<InstrKeywordType>("gainlfo", InstrKeywordType.eKeywordGainlfo),
            new KeywordRec<InstrKeywordType>("good", InstrKeywordType.eKeywordGood),
            new KeywordRec<InstrKeywordType>("halfsteps", InstrKeywordType.eKeywordHalfsteps),
            new KeywordRec<InstrKeywordType>("hertz", InstrKeywordType.eKeywordHertz),
            new KeywordRec<InstrKeywordType>("highpass", InstrKeywordType.eKeywordHighpass),
            new KeywordRec<InstrKeywordType>("highshelfeq", InstrKeywordType.eKeywordHighshelfeq),
            new KeywordRec<InstrKeywordType>("histogram", InstrKeywordType.eKeywordHistogram),
            new KeywordRec<InstrKeywordType>("ideallowpass", InstrKeywordType.eKeywordIdeallowpass),
            new KeywordRec<InstrKeywordType>("indexaccent1", InstrKeywordType.eKeywordIndexaccent1),
            new KeywordRec<InstrKeywordType>("indexaccent2", InstrKeywordType.eKeywordIndexaccent2),
            new KeywordRec<InstrKeywordType>("indexaccent3", InstrKeywordType.eKeywordIndexaccent3),
            new KeywordRec<InstrKeywordType>("indexaccent4", InstrKeywordType.eKeywordIndexaccent4),
            new KeywordRec<InstrKeywordType>("indexaccent5", InstrKeywordType.eKeywordIndexaccent5),
            new KeywordRec<InstrKeywordType>("indexaccent6", InstrKeywordType.eKeywordIndexaccent6),
            new KeywordRec<InstrKeywordType>("indexaccent7", InstrKeywordType.eKeywordIndexaccent7),
            new KeywordRec<InstrKeywordType>("indexaccent8", InstrKeywordType.eKeywordIndexaccent8),
            new KeywordRec<InstrKeywordType>("indexenvelope", InstrKeywordType.eKeywordIndexenvelope),
            new KeywordRec<InstrKeywordType>("indexlfo", InstrKeywordType.eKeywordIndexlfo),
            new KeywordRec<InstrKeywordType>("initfunc", InstrKeywordType.eKeywordInitfunc),
            new KeywordRec<InstrKeywordType>("inputaccent1", InstrKeywordType.eKeywordInputaccent1),
            new KeywordRec<InstrKeywordType>("inputaccent2", InstrKeywordType.eKeywordInputaccent2),
            new KeywordRec<InstrKeywordType>("inputaccent3", InstrKeywordType.eKeywordInputaccent3),
            new KeywordRec<InstrKeywordType>("inputaccent4", InstrKeywordType.eKeywordInputaccent4),
            new KeywordRec<InstrKeywordType>("inputaccent5", InstrKeywordType.eKeywordInputaccent5),
            new KeywordRec<InstrKeywordType>("inputaccent6", InstrKeywordType.eKeywordInputaccent6),
            new KeywordRec<InstrKeywordType>("inputaccent7", InstrKeywordType.eKeywordInputaccent7),
            new KeywordRec<InstrKeywordType>("inputaccent8", InstrKeywordType.eKeywordInputaccent8),
            new KeywordRec<InstrKeywordType>("inputscaling", InstrKeywordType.eKeywordInputscaling),
            new KeywordRec<InstrKeywordType>("inputscalingenvelope", InstrKeywordType.eKeywordInputscalingenvelope),
            new KeywordRec<InstrKeywordType>("inputscalinglfo", InstrKeywordType.eKeywordInputscalinglfo),
            new KeywordRec<InstrKeywordType>("instrument", InstrKeywordType.eKeywordInstrument),
            new KeywordRec<InstrKeywordType>("integerarray", InstrKeywordType.eKeywordIntegerarray),
            new KeywordRec<InstrKeywordType>("interpolate", InstrKeywordType.eKeywordInterpolate),
            new KeywordRec<InstrKeywordType>("inversemult", InstrKeywordType.eKeywordInversemult),
            new KeywordRec<InstrKeywordType>("joint", InstrKeywordType.eKeywordJoint),
            new KeywordRec<InstrKeywordType>("latency", InstrKeywordType.eKeywordLatency),
            new KeywordRec<InstrKeywordType>("left", InstrKeywordType.eKeywordLeft),
            new KeywordRec<InstrKeywordType>("leftintoleft", InstrKeywordType.eKeywordLeftintoleft),
            new KeywordRec<InstrKeywordType>("leftintoright", InstrKeywordType.eKeywordLeftintoright),
            new KeywordRec<InstrKeywordType>("level", InstrKeywordType.eKeywordLevel),
            new KeywordRec<InstrKeywordType>("lfo", InstrKeywordType.eKeywordLfo),
            new KeywordRec<InstrKeywordType>("limitingexcess", InstrKeywordType.eKeywordLimitingexcess),
            new KeywordRec<InstrKeywordType>("limitingexcessaccent1", InstrKeywordType.eKeywordLimitingexcessaccent1),
            new KeywordRec<InstrKeywordType>("limitingexcessaccent2", InstrKeywordType.eKeywordLimitingexcessaccent2),
            new KeywordRec<InstrKeywordType>("limitingexcessaccent3", InstrKeywordType.eKeywordLimitingexcessaccent3),
            new KeywordRec<InstrKeywordType>("limitingexcessaccent4", InstrKeywordType.eKeywordLimitingexcessaccent4),
            new KeywordRec<InstrKeywordType>("limitingexcessaccent5", InstrKeywordType.eKeywordLimitingexcessaccent5),
            new KeywordRec<InstrKeywordType>("limitingexcessaccent6", InstrKeywordType.eKeywordLimitingexcessaccent6),
            new KeywordRec<InstrKeywordType>("limitingexcessaccent7", InstrKeywordType.eKeywordLimitingexcessaccent7),
            new KeywordRec<InstrKeywordType>("limitingexcessaccent8", InstrKeywordType.eKeywordLimitingexcessaccent8),
            new KeywordRec<InstrKeywordType>("limitingexcessenvelope", InstrKeywordType.eKeywordLimitingexcessenvelope),
            new KeywordRec<InstrKeywordType>("limitingexcesslfo", InstrKeywordType.eKeywordLimitingexcesslfo),
            new KeywordRec<InstrKeywordType>("linear", InstrKeywordType.eKeywordLinear),
            new KeywordRec<InstrKeywordType>("logarithmic", InstrKeywordType.eKeywordLogarithmic),
            new KeywordRec<InstrKeywordType>("loop", InstrKeywordType.eKeywordLoop),
#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
            new KeywordRec<InstrKeywordType>("loopenvelope", InstrKeywordType.eKeywordLoopenvelope),
#endif
            new KeywordRec<InstrKeywordType>("loudness", InstrKeywordType.eKeywordLoudness),
            new KeywordRec<InstrKeywordType>("loudnessenvelope", InstrKeywordType.eKeywordLoudnessenvelope),
            new KeywordRec<InstrKeywordType>("loudnessfactor", InstrKeywordType.eKeywordLoudnessfactor),
            new KeywordRec<InstrKeywordType>("loudnesslfo", InstrKeywordType.eKeywordLoudnesslfo),
            new KeywordRec<InstrKeywordType>("lowpass", InstrKeywordType.eKeywordLowpass),
            new KeywordRec<InstrKeywordType>("lowpassfilter", InstrKeywordType.eKeywordLowpassfilter),
            new KeywordRec<InstrKeywordType>("lowshelfeq", InstrKeywordType.eKeywordLowshelfeq),
            new KeywordRec<InstrKeywordType>("max", InstrKeywordType.eKeywordMax),
            new KeywordRec<InstrKeywordType>("maxafter", InstrKeywordType.eKeywordMaxafter),
            new KeywordRec<InstrKeywordType>("maxbandcount", InstrKeywordType.eKeywordMaxbandcount),
            new KeywordRec<InstrKeywordType>("maxdelaytime", InstrKeywordType.eKeywordMaxdelaytime),
            new KeywordRec<InstrKeywordType>("min", InstrKeywordType.eKeywordMin),
            new KeywordRec<InstrKeywordType>("minsamplingrate", InstrKeywordType.eKeywordMinsamplingrate),
            new KeywordRec<InstrKeywordType>("modulation", InstrKeywordType.eKeywordModulation),
            new KeywordRec<InstrKeywordType>("mono", InstrKeywordType.eKeywordMono),
            new KeywordRec<InstrKeywordType>("multiplicative", InstrKeywordType.eKeywordMultiplicative),
            new KeywordRec<InstrKeywordType>("network", InstrKeywordType.eKeywordNetwork),
            new KeywordRec<InstrKeywordType>("nlproc", InstrKeywordType.eKeywordNlproc),
            new KeywordRec<InstrKeywordType>("nodiscardunders", InstrKeywordType.eKeywordNodiscardunders),
            new KeywordRec<InstrKeywordType>("normalpower", InstrKeywordType.eKeywordNormalpower),
            new KeywordRec<InstrKeywordType>("normalpoweraccent1", InstrKeywordType.eKeywordNormalpoweraccent1),
            new KeywordRec<InstrKeywordType>("normalpoweraccent2", InstrKeywordType.eKeywordNormalpoweraccent2),
            new KeywordRec<InstrKeywordType>("normalpoweraccent3", InstrKeywordType.eKeywordNormalpoweraccent3),
            new KeywordRec<InstrKeywordType>("normalpoweraccent4", InstrKeywordType.eKeywordNormalpoweraccent4),
            new KeywordRec<InstrKeywordType>("normalpoweraccent5", InstrKeywordType.eKeywordNormalpoweraccent5),
            new KeywordRec<InstrKeywordType>("normalpoweraccent6", InstrKeywordType.eKeywordNormalpoweraccent6),
            new KeywordRec<InstrKeywordType>("normalpoweraccent7", InstrKeywordType.eKeywordNormalpoweraccent7),
            new KeywordRec<InstrKeywordType>("normalpoweraccent8", InstrKeywordType.eKeywordNormalpoweraccent8),
            new KeywordRec<InstrKeywordType>("normalpowerenvelope", InstrKeywordType.eKeywordNormalpowerenvelope),
            new KeywordRec<InstrKeywordType>("normalpowerlfo", InstrKeywordType.eKeywordNormalpowerlfo),
            new KeywordRec<InstrKeywordType>("null", InstrKeywordType.eKeywordNull),
            new KeywordRec<InstrKeywordType>("numbins", InstrKeywordType.eKeywordNumbins),
            new KeywordRec<InstrKeywordType>("off", InstrKeywordType.eKeywordOff),
            new KeywordRec<InstrKeywordType>("on", InstrKeywordType.eKeywordOn),
            new KeywordRec<InstrKeywordType>("order", InstrKeywordType.eKeywordOrder),
            new KeywordRec<InstrKeywordType>("origin", InstrKeywordType.eKeywordOrigin),
            new KeywordRec<InstrKeywordType>("oscillator", InstrKeywordType.eKeywordOscillator),
            new KeywordRec<InstrKeywordType>("outputaccent1", InstrKeywordType.eKeywordOutputaccent1),
            new KeywordRec<InstrKeywordType>("outputaccent2", InstrKeywordType.eKeywordOutputaccent2),
            new KeywordRec<InstrKeywordType>("outputaccent3", InstrKeywordType.eKeywordOutputaccent3),
            new KeywordRec<InstrKeywordType>("outputaccent4", InstrKeywordType.eKeywordOutputaccent4),
            new KeywordRec<InstrKeywordType>("outputaccent5", InstrKeywordType.eKeywordOutputaccent5),
            new KeywordRec<InstrKeywordType>("outputaccent6", InstrKeywordType.eKeywordOutputaccent6),
            new KeywordRec<InstrKeywordType>("outputaccent7", InstrKeywordType.eKeywordOutputaccent7),
            new KeywordRec<InstrKeywordType>("outputaccent8", InstrKeywordType.eKeywordOutputaccent8),
            new KeywordRec<InstrKeywordType>("outputgain", InstrKeywordType.eKeywordOutputgain),
            new KeywordRec<InstrKeywordType>("outputgainenvelope", InstrKeywordType.eKeywordOutputgainenvelope),
            new KeywordRec<InstrKeywordType>("outputgainlfo", InstrKeywordType.eKeywordOutputgainlfo),
            new KeywordRec<InstrKeywordType>("outputscaling", InstrKeywordType.eKeywordOutputscaling),
            new KeywordRec<InstrKeywordType>("outputscalingenvelope", InstrKeywordType.eKeywordOutputscalingenvelope),
            new KeywordRec<InstrKeywordType>("outputscalinglfo", InstrKeywordType.eKeywordOutputscalinglfo),
            new KeywordRec<InstrKeywordType>("overflow", InstrKeywordType.eKeywordOverflow),
            new KeywordRec<InstrKeywordType>("overlap", InstrKeywordType.eKeywordOverlap),
            new KeywordRec<InstrKeywordType>("oversamplingdisabled", InstrKeywordType.eKeywordOversamplingdisabled),
            new KeywordRec<InstrKeywordType>("param", InstrKeywordType.eKeywordParam),
            new KeywordRec<InstrKeywordType>("parameq", InstrKeywordType.eKeywordParameq),
            new KeywordRec<InstrKeywordType>("parameq2", InstrKeywordType.eKeywordParameq2),
            new KeywordRec<InstrKeywordType>("parameqold", InstrKeywordType.eKeywordParameqold),
            new KeywordRec<InstrKeywordType>("peak", InstrKeywordType.eKeywordPeak),
            new KeywordRec<InstrKeywordType>("peaklookahead", InstrKeywordType.eKeywordPeaklookahead),
            new KeywordRec<InstrKeywordType>("phaseadd", InstrKeywordType.eKeywordPhaseadd),
            new KeywordRec<InstrKeywordType>("phasemodulation", InstrKeywordType.eKeywordPhasemodulation),
            new KeywordRec<InstrKeywordType>("pitchcontrol", InstrKeywordType.eKeywordPitchcontrol),
            new KeywordRec<InstrKeywordType>("pluslinfuzz", InstrKeywordType.eKeywordPluslinfuzz),
            new KeywordRec<InstrKeywordType>("plusramp", InstrKeywordType.eKeywordPlusramp),
            new KeywordRec<InstrKeywordType>("plussine", InstrKeywordType.eKeywordPlussine),
            new KeywordRec<InstrKeywordType>("plussquare", InstrKeywordType.eKeywordPlussquare),
            new KeywordRec<InstrKeywordType>("plustriangle", InstrKeywordType.eKeywordPlustriangle),
            new KeywordRec<InstrKeywordType>("points", InstrKeywordType.eKeywordPoints),
            new KeywordRec<InstrKeywordType>("processedgain", InstrKeywordType.eKeywordProcessedgain),
            new KeywordRec<InstrKeywordType>("pulse", InstrKeywordType.eKeywordPulse),
            new KeywordRec<InstrKeywordType>("ramp", InstrKeywordType.eKeywordRamp),
            new KeywordRec<InstrKeywordType>("rate", InstrKeywordType.eKeywordRate),
            new KeywordRec<InstrKeywordType>("rateaccent1", InstrKeywordType.eKeywordRateaccent1),
            new KeywordRec<InstrKeywordType>("rateaccent2", InstrKeywordType.eKeywordRateaccent2),
            new KeywordRec<InstrKeywordType>("rateaccent3", InstrKeywordType.eKeywordRateaccent3),
            new KeywordRec<InstrKeywordType>("rateaccent4", InstrKeywordType.eKeywordRateaccent4),
            new KeywordRec<InstrKeywordType>("rateaccent5", InstrKeywordType.eKeywordRateaccent5),
            new KeywordRec<InstrKeywordType>("rateaccent6", InstrKeywordType.eKeywordRateaccent6),
            new KeywordRec<InstrKeywordType>("rateaccent7", InstrKeywordType.eKeywordRateaccent7),
            new KeywordRec<InstrKeywordType>("rateaccent8", InstrKeywordType.eKeywordRateaccent8),
            new KeywordRec<InstrKeywordType>("ratefreq", InstrKeywordType.eKeywordRatefreq),
            new KeywordRec<InstrKeywordType>("ratio", InstrKeywordType.eKeywordRatio),
            new KeywordRec<InstrKeywordType>("ratioaccent1", InstrKeywordType.eKeywordRatioaccent1),
            new KeywordRec<InstrKeywordType>("ratioaccent2", InstrKeywordType.eKeywordRatioaccent2),
            new KeywordRec<InstrKeywordType>("ratioaccent3", InstrKeywordType.eKeywordRatioaccent3),
            new KeywordRec<InstrKeywordType>("ratioaccent4", InstrKeywordType.eKeywordRatioaccent4),
            new KeywordRec<InstrKeywordType>("ratioaccent5", InstrKeywordType.eKeywordRatioaccent5),
            new KeywordRec<InstrKeywordType>("ratioaccent6", InstrKeywordType.eKeywordRatioaccent6),
            new KeywordRec<InstrKeywordType>("ratioaccent7", InstrKeywordType.eKeywordRatioaccent7),
            new KeywordRec<InstrKeywordType>("ratioaccent8", InstrKeywordType.eKeywordRatioaccent8),
            new KeywordRec<InstrKeywordType>("ratioenvelope", InstrKeywordType.eKeywordRatioenvelope),
            new KeywordRec<InstrKeywordType>("ratiolfo", InstrKeywordType.eKeywordRatiolfo),
            new KeywordRec<InstrKeywordType>("releasepoint", InstrKeywordType.eKeywordReleasepoint),
            new KeywordRec<InstrKeywordType>("releasepointnoskip", InstrKeywordType.eKeywordReleasepointnoskip),
            new KeywordRec<InstrKeywordType>("report", InstrKeywordType.eKeywordReport),
            new KeywordRec<InstrKeywordType>("resampler", InstrKeywordType.eKeywordResampler),
            new KeywordRec<InstrKeywordType>("reson", InstrKeywordType.eKeywordReson),
            new KeywordRec<InstrKeywordType>("resonantlowpass", InstrKeywordType.eKeywordResonantlowpass),
            new KeywordRec<InstrKeywordType>("resonantlowpass2", InstrKeywordType.eKeywordResonantlowpass2),
            new KeywordRec<InstrKeywordType>("right", InstrKeywordType.eKeywordRight),
            new KeywordRec<InstrKeywordType>("rightintoleft", InstrKeywordType.eKeywordRightintoleft),
            new KeywordRec<InstrKeywordType>("rightintoright", InstrKeywordType.eKeywordRightintoright),
            new KeywordRec<InstrKeywordType>("rms", InstrKeywordType.eKeywordRms),
            new KeywordRec<InstrKeywordType>("sample", InstrKeywordType.eKeywordSample),
            new KeywordRec<InstrKeywordType>("sampleandhold", InstrKeywordType.eKeywordSampleandhold),
            new KeywordRec<InstrKeywordType>("sampled", InstrKeywordType.eKeywordSampled),
            new KeywordRec<InstrKeywordType>("samplelist", InstrKeywordType.eKeywordSamplelist),
            new KeywordRec<InstrKeywordType>("scale", InstrKeywordType.eKeywordScale),
            new KeywordRec<InstrKeywordType>("scaleaccent1", InstrKeywordType.eKeywordScaleaccent1),
            new KeywordRec<InstrKeywordType>("scaleaccent2", InstrKeywordType.eKeywordScaleaccent2),
            new KeywordRec<InstrKeywordType>("scaleaccent3", InstrKeywordType.eKeywordScaleaccent3),
            new KeywordRec<InstrKeywordType>("scaleaccent4", InstrKeywordType.eKeywordScaleaccent4),
            new KeywordRec<InstrKeywordType>("scaleaccent5", InstrKeywordType.eKeywordScaleaccent5),
            new KeywordRec<InstrKeywordType>("scaleaccent6", InstrKeywordType.eKeywordScaleaccent6),
            new KeywordRec<InstrKeywordType>("scaleaccent7", InstrKeywordType.eKeywordScaleaccent7),
            new KeywordRec<InstrKeywordType>("scaleaccent8", InstrKeywordType.eKeywordScaleaccent8),
            new KeywordRec<InstrKeywordType>("scaleenvelope", InstrKeywordType.eKeywordScaleenvelope),
            new KeywordRec<InstrKeywordType>("scalelfo", InstrKeywordType.eKeywordScalelfo),
            new KeywordRec<InstrKeywordType>("scoreeffect", InstrKeywordType.eKeywordScoreeffect),
            new KeywordRec<InstrKeywordType>("sectioneffect", InstrKeywordType.eKeywordSectioneffect),
            new KeywordRec<InstrKeywordType>("signlinfuzz", InstrKeywordType.eKeywordSignlinfuzz),
            new KeywordRec<InstrKeywordType>("signramp", InstrKeywordType.eKeywordSignramp),
            new KeywordRec<InstrKeywordType>("signsine", InstrKeywordType.eKeywordSignsine),
            new KeywordRec<InstrKeywordType>("signsquare", InstrKeywordType.eKeywordSignsquare),
            new KeywordRec<InstrKeywordType>("signtriangle", InstrKeywordType.eKeywordSigntriangle),
            new KeywordRec<InstrKeywordType>("silence", InstrKeywordType.eKeywordSilence),
            new KeywordRec<InstrKeywordType>("slope", InstrKeywordType.eKeywordSlope),
            new KeywordRec<InstrKeywordType>("slopeaccent1", InstrKeywordType.eKeywordSlopeaccent1),
            new KeywordRec<InstrKeywordType>("slopeaccent2", InstrKeywordType.eKeywordSlopeaccent2),
            new KeywordRec<InstrKeywordType>("slopeaccent3", InstrKeywordType.eKeywordSlopeaccent3),
            new KeywordRec<InstrKeywordType>("slopeaccent4", InstrKeywordType.eKeywordSlopeaccent4),
            new KeywordRec<InstrKeywordType>("slopeaccent5", InstrKeywordType.eKeywordSlopeaccent5),
            new KeywordRec<InstrKeywordType>("slopeaccent6", InstrKeywordType.eKeywordSlopeaccent6),
            new KeywordRec<InstrKeywordType>("slopeaccent7", InstrKeywordType.eKeywordSlopeaccent7),
            new KeywordRec<InstrKeywordType>("slopeaccent8", InstrKeywordType.eKeywordSlopeaccent8),
            new KeywordRec<InstrKeywordType>("slopeenvelope", InstrKeywordType.eKeywordSlopeenvelope),
            new KeywordRec<InstrKeywordType>("slopelfo", InstrKeywordType.eKeywordSlopelfo),
            new KeywordRec<InstrKeywordType>("smoothed", InstrKeywordType.eKeywordSmoothed),
            new KeywordRec<InstrKeywordType>("smoothedabsval", InstrKeywordType.eKeywordSmoothedabsval),
            new KeywordRec<InstrKeywordType>("smoothedrms", InstrKeywordType.eKeywordSmoothedrms),
            new KeywordRec<InstrKeywordType>("sourceaccent1", InstrKeywordType.eKeywordSourceaccent1),
            new KeywordRec<InstrKeywordType>("sourceaccent2", InstrKeywordType.eKeywordSourceaccent2),
            new KeywordRec<InstrKeywordType>("sourceaccent3", InstrKeywordType.eKeywordSourceaccent3),
            new KeywordRec<InstrKeywordType>("sourceaccent4", InstrKeywordType.eKeywordSourceaccent4),
            new KeywordRec<InstrKeywordType>("sourceaccent5", InstrKeywordType.eKeywordSourceaccent5),
            new KeywordRec<InstrKeywordType>("sourceaccent6", InstrKeywordType.eKeywordSourceaccent6),
            new KeywordRec<InstrKeywordType>("sourceaccent7", InstrKeywordType.eKeywordSourceaccent7),
            new KeywordRec<InstrKeywordType>("sourceaccent8", InstrKeywordType.eKeywordSourceaccent8),
            new KeywordRec<InstrKeywordType>("sourceenvelope", InstrKeywordType.eKeywordSourceenvelope),
            new KeywordRec<InstrKeywordType>("sourcelfo", InstrKeywordType.eKeywordSourcelfo),
            new KeywordRec<InstrKeywordType>("square", InstrKeywordType.eKeywordSquare),
            new KeywordRec<InstrKeywordType>("stereo", InstrKeywordType.eKeywordStereo),
            new KeywordRec<InstrKeywordType>("stereobias", InstrKeywordType.eKeywordStereobias),
            new KeywordRec<InstrKeywordType>("suppressinitialsilence", InstrKeywordType.eKeywordSuppressinitialsilence),
            new KeywordRec<InstrKeywordType>("surroundbias", InstrKeywordType.eKeywordSurroundbias),
            new KeywordRec<InstrKeywordType>("sustainpoint", InstrKeywordType.eKeywordSustainpoint),
            new KeywordRec<InstrKeywordType>("sustainpointnoskip", InstrKeywordType.eKeywordSustainpointnoskip),
            new KeywordRec<InstrKeywordType>("tap", InstrKeywordType.eKeywordTap),
            new KeywordRec<InstrKeywordType>("targetaccent1", InstrKeywordType.eKeywordTargetaccent1),
            new KeywordRec<InstrKeywordType>("targetaccent2", InstrKeywordType.eKeywordTargetaccent2),
            new KeywordRec<InstrKeywordType>("targetaccent3", InstrKeywordType.eKeywordTargetaccent3),
            new KeywordRec<InstrKeywordType>("targetaccent4", InstrKeywordType.eKeywordTargetaccent4),
            new KeywordRec<InstrKeywordType>("targetaccent5", InstrKeywordType.eKeywordTargetaccent5),
            new KeywordRec<InstrKeywordType>("targetaccent6", InstrKeywordType.eKeywordTargetaccent6),
            new KeywordRec<InstrKeywordType>("targetaccent7", InstrKeywordType.eKeywordTargetaccent7),
            new KeywordRec<InstrKeywordType>("targetaccent8", InstrKeywordType.eKeywordTargetaccent8),
            new KeywordRec<InstrKeywordType>("targetenvelope", InstrKeywordType.eKeywordTargetenvelope),
            new KeywordRec<InstrKeywordType>("targetlfo", InstrKeywordType.eKeywordTargetlfo),
            new KeywordRec<InstrKeywordType>("threshpower", InstrKeywordType.eKeywordThreshpower),
            new KeywordRec<InstrKeywordType>("threshpoweraccent1", InstrKeywordType.eKeywordThreshpoweraccent1),
            new KeywordRec<InstrKeywordType>("threshpoweraccent2", InstrKeywordType.eKeywordThreshpoweraccent2),
            new KeywordRec<InstrKeywordType>("threshpoweraccent3", InstrKeywordType.eKeywordThreshpoweraccent3),
            new KeywordRec<InstrKeywordType>("threshpoweraccent4", InstrKeywordType.eKeywordThreshpoweraccent4),
            new KeywordRec<InstrKeywordType>("threshpoweraccent5", InstrKeywordType.eKeywordThreshpoweraccent5),
            new KeywordRec<InstrKeywordType>("threshpoweraccent6", InstrKeywordType.eKeywordThreshpoweraccent6),
            new KeywordRec<InstrKeywordType>("threshpoweraccent7", InstrKeywordType.eKeywordThreshpoweraccent7),
            new KeywordRec<InstrKeywordType>("threshpoweraccent8", InstrKeywordType.eKeywordThreshpoweraccent8),
            new KeywordRec<InstrKeywordType>("threshpowerenvelope", InstrKeywordType.eKeywordThreshpowerenvelope),
            new KeywordRec<InstrKeywordType>("threshpowerlfo", InstrKeywordType.eKeywordThreshpowerlfo),
            new KeywordRec<InstrKeywordType>("to", InstrKeywordType.eKeywordTo),
            new KeywordRec<InstrKeywordType>("totalscaling", InstrKeywordType.eKeywordTotalscaling),
            new KeywordRec<InstrKeywordType>("trackeffect", InstrKeywordType.eKeywordTrackeffect),
            new KeywordRec<InstrKeywordType>("triangle", InstrKeywordType.eKeywordTriangle),
            new KeywordRec<InstrKeywordType>("truncate", InstrKeywordType.eKeywordTruncate),
            new KeywordRec<InstrKeywordType>("type", InstrKeywordType.eKeywordType),
            new KeywordRec<InstrKeywordType>("unitymidbandgain", InstrKeywordType.eKeywordUnitymidbandgain),
            new KeywordRec<InstrKeywordType>("unitynoisegain", InstrKeywordType.eKeywordUnitynoisegain),
            new KeywordRec<InstrKeywordType>("unityzerohertzgain", InstrKeywordType.eKeywordUnityzerohertzgain),
            new KeywordRec<InstrKeywordType>("usereffect", InstrKeywordType.eKeywordUsereffect),
            new KeywordRec<InstrKeywordType>("vocoder", InstrKeywordType.eKeywordVocoder),
            new KeywordRec<InstrKeywordType>("wave", InstrKeywordType.eKeywordWave),
            new KeywordRec<InstrKeywordType>("wavetable", InstrKeywordType.eKeywordWavetable),
            new KeywordRec<InstrKeywordType>("wavetableindex", InstrKeywordType.eKeywordWavetableindex),
            new KeywordRec<InstrKeywordType>("windowduration", InstrKeywordType.eKeywordWindowduration),
            new KeywordRec<InstrKeywordType>("workspaces", InstrKeywordType.eKeywordWorkspaces),
            new KeywordRec<InstrKeywordType>("wrap", InstrKeywordType.eKeywordWrap),
            new KeywordRec<InstrKeywordType>("zero", InstrKeywordType.eKeywordZero),
        };

        /* error table entry structure */
        public struct InstrErrorRec
        {
            /* for checking index consistency */
            public readonly BuildInstrErrors ErrorCode;

            /* error message to print */
            public readonly string Message;

            public InstrErrorRec(BuildInstrErrors ErrorCode, string Message)
            {
                this.ErrorCode = ErrorCode;
                this.Message = Message;
            }
        }

        private static readonly InstrErrorRec[] InstrErrors = new InstrErrorRec[]
        {
            new InstrErrorRec(BuildInstrErrors.eBuildInstrNoError,
                "No error"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrGenericError,
                ""), // the text of this error provided by the ErrorExtraMessage field of the parse context
            new InstrErrorRec(BuildInstrErrors.eBuildInstrUnexpectedInput,
                "Illegal characters beyond end of input"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedInstrument,
                "Expected 'instrument'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOpenParen,
                "Expected '('"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedCloseParen,
                "Expected ')'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSemicolon,
                "Expected ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedInstrumentMember,
                "Expected 'loudness', 'frequencylfo', 'oscillator', 'effect', " +
                "or 'trackeffect'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInstrLoudness,
                "Instrument parameter 'loudness' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedNumber,
                "Expected a number"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedStringOrIdentifier,
                "Expected a string or identifier"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLFOMember,
                "Expected 'freqenvelope', 'ampenvelope', 'oscillator', 'modulation', " +
                "'linear', 'exponential', 'freqlfo', 'amplfo', 'lowpassfilter', " +
                "or 'sampleandhold'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLFOFreqEnvelope,
                "LFO parameter 'freqenvelope' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLFOAmpEnvelope,
                "LFO parameter 'ampenvelope' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLFOOscillatorType,
                "LFO parameter 'oscillator' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLFOOscillatorType,
                "Expected 'constant', 'signsine', 'plussine', 'signtriangle', "  +
                "'plustriangle', 'signsquare', 'plussquare', 'signramp', 'plusramp', " +
                "'signlinfuzz', 'pluslinfuzz', 'wavetable', or "+
#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
                "'loopenvelope'"
#else
                ""
#endif
                ),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLFOModulationType,
                "LFO parameter 'modulation' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLFOAddingMode,
                "LFO adding mode ('linear' or 'exponential') has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLFOModulationType,
                "Expected 'additive', 'multiplicative', or 'inversemult'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOscillatorMember,
                "Expected 'type', 'samplelist', 'loudness', " +
                "'freqmultiplier', 'freqdivisor', 'freqadder', " +
                "'loudnessenvelope', 'loudnesslfo', 'indexenvelope', 'indexlfo', " +
                "'stereobias', 'displacement', 'frequencylfo', 'fofsamprate', " +
                "'fofcompress', 'fofexpand', 'fofsamprateenvelope', or " +
                "'fofsampratelfo'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscType,
                "Oscillator parameter 'type' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscSampleList,
                "Oscillator parameter 'samplelist' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscLoudness,
                "Oscillator parameter 'loudness' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscLoudnessFactor,
                "Oscillator parameter 'loudnessfactor' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscFreqMultiplier,
                "Oscillator parameter 'freqmultiplier' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscFreqDivisor,
                "Oscillator parameter 'freqdivisor' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscLoudnessEnvelope,
                "Oscillator parameter 'loudnessenvelope' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscIndexEnvelope,
                "Oscillator parameter 'indexenvelope' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOscType,
                "Expected 'sampled', 'wavetable', 'pulse', 'ramp', 'fof', or 'phasemodulation'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedInteger,
                "Expected an integer"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedEnvelopeMember,
                "Expected 'totalscaling', 'points', 'pitchcontrol', or 'formula'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvTotalScaling,
                "Envelope parameter 'totalscaling' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvFormula,
                "Envelope paramater formula has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPoints,
                "Envelope parameter 'points' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDelayOrOrigin,
                "Expected 'delay' or 'origin'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLevelOrScale,
                "Expected 'level' or 'scale'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedEnvPointMember,
                "Expected 'sustainpoint', 'sustainpointnoskip', 'releasepoint', " +
                "'releasepointnoskip', 'ampaccent[1-8]', 'ampfreq', 'rateaccent[1-8]', " +
                "'ratefreq', 'exponential', or 'linear'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedIntBetween1And3,
                "Expected an integer in the range [1..3]"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined,
                "That envelope sustain point has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent1,
                "Envelope parameter 'ampaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent2,
                "Envelope parameter 'ampaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent3,
                "Envelope parameter 'ampaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent4,
                "Envelope parameter 'ampaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent5,
                "Envelope parameter 'ampaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent6,
                "Envelope parameter 'ampaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent7,
                "Envelope parameter 'ampaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent8,
                "Envelope parameter 'ampaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointAmpFreq,
                "Envelope parameter 'ampfreq' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent1,
                "Envelope parameter 'rateaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent2,
                "Envelope parameter 'rateaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent3,
                "Envelope parameter 'rateaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent4,
                "Envelope parameter 'rateaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent5,
                "Envelope parameter 'rateaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent6,
                "Envelope parameter 'rateaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent7,
                "Envelope parameter 'rateaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent8,
                "Envelope parameter 'rateaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointRateFreq,
                "Envelope parameter 'ratefreq' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleEnvPointCurveSpec,
                "Envelope parameter 'exponential' or 'linear' has " +
                "already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscStereoBias,
                "Oscillator parameter 'stereobias' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscDisplacement,
                "Oscillator parameter 'displacement' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscSurroundBias,
                "Oscillator parameter 'surroundbias' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscFreqAdder,
                "Oscillator parameter 'freqadder' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSquareOrTriangle,
                "Expected 'square' or 'triangle'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedEffectName,
                "Expected 'delayline', 'nlproc', 'filter', 'analyzer', 'histogram', 'resampler', " +
                "'compressor', 'vocoder', 'ideallowpass', 'convolver', or 'usereffect'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDelayLineElem,
                "Expected 'tap' or 'maxdelaytime'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedTapChannel,
                "Expected 'left', 'right', or 'mono'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedTo,
                "Expected 'to'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedScale,
                "Expected 'scale'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedTapAttr,
                "Expected 'sourceaccent[1-8]', 'targetaccent[1-8]', 'scaleaccent[1-8]', " +
                "'lowpass', or 'freqaccent[1-8]'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSourceAccent1,
                "Delay tap parameter 'sourceaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSourceAccent2,
                "Delay tap parameter 'sourceaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSourceAccent3,
                "Delay tap parameter 'sourceaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSourceAccent4,
                "Delay tap parameter 'sourceaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleTargetAccent1,
                "Delay tap parameter 'targetaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleTargetAccent2,
                "Delay tap parameter 'targetaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleTargetAccent3,
                "Delay tap parameter 'targetaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleTargetAccent4,
                "Delay tap parameter 'targetaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleScaleAccent1,
                "Delay tap parameter 'scaleaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleScaleAccent2,
                "Delay tap parameter 'scaleaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleScaleAccent3,
                "Delay tap parameter 'scaleaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleScaleAccent4,
                "Delay tap parameter 'scaleaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFilter,
                "Delay tap filter has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleMaxDelayTime,
                "Delay line parameter 'maxdelaytime' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSlope,
                "Expected 'slope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedCenter,
                "Expected 'center'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSamplelist,
                "Expected 'samplelist'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedEnvelope,
                "Expected 'envelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedWavetable,
                "Expected 'wavetable'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedInputscaling,
                "Expected 'inputscaling'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOutputscaling,
                "Expected 'outputscaling'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedNLAttribute,
                "Expected 'inputaccent[1-8]', 'outputaccent[1-8]', or 'indexaccent[1-8]'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInputaccent1,
                "Parameter 'inputaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInputaccent2,
                "Parameter 'inputaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInputaccent3,
                "Parameter 'inputaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInputaccent4,
                "Parameter 'inputaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputaccent1,
                "Parameter 'outputaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputaccent2,
                "Parameter 'outputaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputaccent3,
                "Parameter 'outputaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputaccent4,
                "Parameter 'outputaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleIndexaccent1,
                "Parameter 'indexaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleIndexaccent2,
                "Parameter 'indexaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleIndexaccent3,
                "Parameter 'indexaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleIndexaccent4,
                "Parameter 'indexaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedWavetableindex,
                "Expected 'wavetableindex'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFilterType,
                "Expected 'lowpass', 'highpass', 'reson', 'zero', 'butterworthlowpass', " +
                "'butterworthhighpass', 'butterworthbandpass', 'butterworthbandreject', " +
                "'parameqold', 'parameq2', 'resonantlowpass', 'resonantlowpass2', " +
                "'lowshelfeq', or 'highshelfeq'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFreq,
                "Expected 'freq'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedBandwidth,
                "Expected 'bandwidth'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDefaultScaling,
                "Expected 'defaultscaling'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedResonScaling,
                "Expected 'defaultscaling', 'unitymidbandgain', or 'unitynoisegain'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedZeroScaling,
                "Expected 'defaultscaling' or 'unityzerohertzgain'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFilterAttr,
                "Expected 'freqaccent[1-8]', 'bandwidthaccent[1-8]', 'outputscaling', " +
                "'outputaccent[1-8]', or 'gainaccent[1-8]'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFreqaccent1,
                "Parameter 'freqaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFreqaccent2,
                "Parameter 'freqaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFreqaccent3,
                "Parameter 'freqaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFreqaccent4,
                "Parameter 'freqaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleBandwidthaccent1,
                "Parameter 'bandwidthaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleBandwidthaccent2,
                "Parameter 'bandwidthaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleBandwidthaccent3,
                "Parameter 'bandwidthaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleBandwidthaccent4,
                "Parameter 'bandwidthaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputScaling,
                "Parameter 'outputscaling' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent1,
                "Parameter 'outputaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent2,
                "Parameter 'outputaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent3,
                "Parameter 'outputaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent4,
                "Parameter 'outputaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFilterChannel,
                "Expected 'left', 'right', 'joint', 'defaultscaling', " +
                "'unitymidbandgain', 'unitynoisegain', or 'unityzerohertzgain'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrNullFilterHasNoFreqAccentX,
                "Parameter 'freqaccent[1-8]' can't be specified for null filter"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrFilterHasNoBandwidthAccentX,
                "Parameter 'bandwidthaccent[1-8]' can only be specified for " +
                "reson, zero, butterworthbandpass, butterworthbandreject, " +
                "parameqold, parameq2, resonantlowpass, " +
                "lowshelfeq, and highshelfeq"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedScoreEffect,
                "Expected 'scoreeffect'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOscillatorEffect,
                "Expected 'delayline', 'nlproc', 'filter', 'analyzer', 'histogram', 'resampler', " +
                "'compressor', 'vocoder', 'ideallowpass', or 'usereffect'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSourceEnvelope,
                "Expected 'sourceenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedTargetEnvelope,
                "Expected 'targetenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedScaleEnvelope,
                "Expected 'scaleenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSourceLfoOrTo,
                "Expected 'sourcelfo' or 'to'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedTargetLfoOrScaleEnvelope,
                "Expected 'targetlfo' or 'scaleenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedScaleLfoCutoffOrSemicolon,
                "Expected 'scalelfo', 'lowpass', or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedInputScalingEnvelope,
                "Expected 'inputscalingenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOutputScalingEnvelope,
                "Expected 'outputscalingenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedIndexEnvelope,
                "Expected 'indexenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedInputScalingLfoOrOutputScalingEnvelope,
                "Expected 'inputscalinglfo' or 'outputscalingenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOutputScalingLfoOrIndexEnvelope,
                "Expected 'outputscalinglfo' or 'indexenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedIndexLfoOrSemicolon,
                "Expected 'indexlfo' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFreqEnvelope,
                "Expected 'freqenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedBandwidthEnvelope,
                "Expected 'bandwidthenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFreqLfoOrScalingOrChannel,
                "Expected 'freqlfo', 'left', 'right', 'joint', 'defaultscaling', " +
                "'unitymidbandgain', 'unitynoisegain', 'unityzerohertzgain', " +
                "or 'bandwidthenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedBandwidthLfoOrScalingOrChannel,
                "Expected 'bandwidthlfo', 'gainenvelope', 'left', 'right', 'joint', " +
                "'defaultscaling', 'unitymidbandgain', 'unitynoisegain', " +
                "or 'unityzerohertzgain'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOutputScalingLfoOrSemicolon,
                "Expected 'outputscalinglfo' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFilter,
                "Expected 'filter' or ')'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFreqLfoOrSemicolon,
                "Expected 'freqlfo' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDelayLowpassFreq,
                "Delay tap parameter 'lowpass freq' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent1,
                "Delay tap parameter 'freqaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent2,
                "Delay tap parameter 'freqaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent3,
                "Delay tap parameter 'freqaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent4,
                "Delay tap parameter 'freqaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedIndexLFOOrSemicolon,
                "Expected 'indexlfo' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultiplePitchControl,
                "Envelope parameter 'pitchcontrol' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedGain,
                "Expected 'gain'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleGainAccent1,
                "Parameter 'gainaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleGainAccent2,
                "Parameter 'gainaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleGainAccent3,
                "Parameter 'gainaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleGainAccent4,
                "Parameter 'gainaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedGainEnvelope,
                "Expected 'gainenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedGainLfoOrScalingOrChannel,
                "Expected 'gainlfo', 'left', 'right', 'joint', " +
                "'defaultscaling', 'unitymidbandgain', 'unitynoisegain', " +
                "or 'unityzerohertzgain'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrFilterHasNoGainAccentX,
                "Parameter 'gainaccent[1-8]' can only be applied to parameq, parameq2, " +
                "resonantlowpass, or resonantlowpass2"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOrder,
                "Expected 'order'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrOrderMustBeNonNegativeEvenInteger,
                "Filter order must be a non-negative even integer"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedRate,
                "Expected 'rate'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedTruncateOrInterpolate,
                "Expected 'truncate' or 'interpolate'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSourceAccent5,
                "Delay tap parameter 'sourceaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSourceAccent6,
                "Delay tap parameter 'sourceaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSourceAccent7,
                "Delay tap parameter 'sourceaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSourceAccent8,
                "Delay tap parameter 'sourceaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleTargetAccent5,
                "Delay tap parameter 'targetaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleTargetAccent6,
                "Delay tap parameter 'targetaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleTargetAccent7,
                "Delay tap parameter 'targetaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleTargetAccent8,
                "Delay tap parameter 'targetaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleScaleAccent5,
                "Delay tap parameter 'scaleaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleScaleAccent6,
                "Delay tap parameter 'scaleaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleScaleAccent7,
                "Delay tap parameter 'scaleaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleScaleAccent8,
                "Delay tap parameter 'scaleaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent5,
                "Delay tap parameter 'freqaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent6,
                "Delay tap parameter 'freqaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent7,
                "Delay tap parameter 'freqaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent8,
                "Delay tap parameter 'freqaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInputaccent5,
                "Parameter 'inputaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInputaccent6,
                "Parameter 'inputaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInputaccent7,
                "Parameter 'inputaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInputaccent8,
                "Parameter 'inputaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputaccent5,
                "Parameter 'outputaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputaccent6,
                "Parameter 'outputaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputaccent7,
                "Parameter 'outputaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputaccent8,
                "Parameter 'outputaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleIndexaccent5,
                "Parameter 'indexaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleIndexaccent6,
                "Parameter 'indexaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleIndexaccent7,
                "Parameter 'indexaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleIndexaccent8,
                "Parameter 'indexaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOverflow,
                "Parameter 'overflow' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFreqaccent5,
                "Parameter 'freqaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFreqaccent6,
                "Parameter 'freqaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFreqaccent7,
                "Parameter 'freqaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFreqaccent8,
                "Parameter 'freqaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleBandwidthaccent5,
                "Parameter 'bandwidthaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleBandwidthaccent6,
                "Parameter 'bandwidthaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleBandwidthaccent7,
                "Parameter 'bandwidthaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleBandwidthaccent8,
                "Parameter 'bandwidthaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent5,
                "Parameter 'outputaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent6,
                "Parameter 'outputaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent7,
                "Parameter 'outputaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent8,
                "Parameter 'outputaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleGainAccent5,
                "Parameter 'gainaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleGainAccent6,
                "Parameter 'gainaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleGainAccent7,
                "Parameter 'gainaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleGainAccent8,
                "Parameter 'gainaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscFOFSampRate,
                "Oscillator parameter 'fofsamprate' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscFOFCompress,
                "Oscillator parameter 'fofcompress' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOverlapOrDiscard,
                "Expected 'overlap' or 'discard'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleOscFOFExpand,
                "Oscillator parameter 'fofexpand' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSilenceOrLoop,
                "Expected 'silence' or 'loop'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFOFSamplingRateEnvelope,
                "Oscillator parameter 'fofsamprateenvelope' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredOscillator,
                "At least one 'oscillator' must be specified in an instrument"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredAmpEnvelope,
                "LFO definition requires 'ampenvelope' to be specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredOscillatorType,
                "Oscillator definition requires 'type' to be specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredOscillatorLoudnessEnvelope,
                "Oscillator definition requires 'loudnessenvelope' to be specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredOscillatorSampleList,
                "Oscillator definition requires 'samplelist' to be specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredOscillatorFOFSampRate,
                "FOF Oscillator definition requires 'fofsamprate' to be specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredOscillatorFOFCompress,
                "FOF Oscillator definition requires 'fofcompress' to be specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredOscillatorFOFExpand,
                "FOF Oscillator definition requires 'fofexpand' to be specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredOscillatorFOFEnvelope,
                "FOF Oscillator definition requires 'fofsamprateenvelope' to be specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredDelayLineMaxTime,
                "Delay Line definition requires 'maxdelaytime' to be specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLFOFilter,
                "LFO parameter 'lowpassfilter' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedNormalPower,
                "Expected 'normalpower'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedThreshPower,
                "Expected 'threshpower'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedRatio,
                "Expected 'ratio'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFilterCutoff,
                "Expected 'filtercutoff'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDecayRate,
                "Expected 'decayrate'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedAttackRate,
                "Expected 'attackrate'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLimitingExcess,
                "Expected 'limitingexcess'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedCompressorAttribute,
                "Expected 'inputaccent[1-8]', 'outputaccent[1-8]', " +
                "'normalpoweraccent[1-8]', 'threshpoweraccent[1-8]', 'ratioaccent[1-8]', " +
                "'filtercutoff[1-8]', 'decayrateaccent[1-8]', 'attackrateaccent[1-8]', " +
                "'limitingexcessaccent[1-8]', or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent1,
                "Compressor parameter 'normalpoweraccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent2,
                "Compressor parameter 'normalpoweraccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent3,
                "Compressor parameter 'normalpoweraccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent4,
                "Compressor parameter 'normalpoweraccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent5,
                "Compressor parameter 'normalpoweraccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent6,
                "Compressor parameter 'normalpoweraccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent7,
                "Compressor parameter 'normalpoweraccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent8,
                "Compressor parameter 'normalpoweraccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent1,
                "Compressor parameter 'threshpoweraccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent2,
                "Compressor parameter 'threshpoweraccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent3,
                "Compressor parameter 'threshpoweraccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent4,
                "Compressor parameter 'threshpoweraccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent5,
                "Compressor parameter 'threshpoweraccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent6,
                "Compressor parameter 'threshpoweraccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent7,
                "Compressor parameter 'threshpoweraccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent8,
                "Compressor parameter 'threshpoweraccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleRatioAccent1,
                "Compressor parameter 'ratioaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleRatioAccent2,
                "Compressor parameter 'ratioaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleRatioAccent3,
                "Compressor parameter 'ratioaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleRatioAccent4,
                "Compressor parameter 'ratioaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleRatioAccent5,
                "Compressor parameter 'ratioaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleRatioAccent6,
                "Compressor parameter 'ratioaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleRatioAccent7,
                "Compressor parameter 'ratioaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleRatioAccent8,
                "Compressor parameter 'ratioaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent1,
                "Compressor parameter 'filtercutoffaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent2,
                "Compressor parameter 'filtercutoffaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent3,
                "Compressor parameter 'filtercutoffaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent4,
                "Compressor parameter 'filtercutoffaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent5,
                "Compressor parameter 'filtercutoffaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent6,
                "Compressor parameter 'filtercutoffaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent7,
                "Compressor parameter 'filtercutoffaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent8,
                "Compressor parameter 'filtercutoffaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDecayRateAccent1,
                "Compressor parameter 'decayrateaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDecayRateAccent2,
                "Compressor parameter 'decayrateaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDecayRateAccent3,
                "Compressor parameter 'decayrateaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDecayRateAccent4,
                "Compressor parameter 'decayrateaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDecayRateAccent5,
                "Compressor parameter 'decayrateaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDecayRateAccent6,
                "Compressor parameter 'decayrateaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDecayRateAccent7,
                "Compressor parameter 'decayrateaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleDecayRateAccent8,
                "Compressor parameter 'decayrateaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAttackRateAccent1,
                "Compressor parameter 'attackrateaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAttackRateAccent2,
                "Compressor parameter 'attackrateaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAttackRateAccent3,
                "Compressor parameter 'attackrateaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAttackRateAccent4,
                "Compressor parameter 'attackrateaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAttackRateAccent5,
                "Compressor parameter 'attackrateaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAttackRateAccent6,
                "Compressor parameter 'attackrateaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAttackRateAccent7,
                "Compressor parameter 'attackrateaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAttackRateAccent8,
                "Compressor parameter 'attackrateaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent1,
                "Compressor parameter 'limitingexcessaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent2,
                "Compressor parameter 'limitingexcessaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent3,
                "Compressor parameter 'limitingexcessaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent4,
                "Compressor parameter 'limitingexcessaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent5,
                "Compressor parameter 'limitingexcessaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent6,
                "Compressor parameter 'limitingexcessaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent7,
                "Compressor parameter 'limitingexcessaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent8,
                "Compressor parameter 'limitingexcessaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOutputScalingLfoOrNormalPowerEnvelope,
                "Expected 'outputscalinglfo' or 'normalpowerenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedNormalPowerEnvelope,
                "Expected 'normalpowerenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedNormalPowerLfoOrThreshPowerEnvelope,
                "Expected 'normalpowerlfo' or 'threshpowerenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedThreshPowerEnvelope,
                "Expected 'threshpowerenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedThreshPowerLfoOrRatioEnvelope,
                "Expected 'threshpowerlfo' or 'ratioenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedRatioEnvelope,
                "Expected 'ratioenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedRatioLfoOrFilterCutoffEnvelope,
                "Expected 'ratiolfo' or 'filtercutoffenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFilterCutoffEnvelope,
                "Expected 'filtercutoffenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedFilterCutoffLfoOrDecayRateEnvelope,
                "Expected 'filtercutofflfo' or 'decayrateenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDecayRateEnvelope,
                "Expected 'decayrateenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDecayRateLfoOrAttackRateEnvelope,
                "Expected 'decayratelfo' or 'attackrateenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedAttackRateEnvelope,
                "Expected 'attackrateenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedAttackRateLfoOrLimitingExcessEnvelope,
                "Expected 'attackratelfo' or 'limitingexcessenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLimitingExcessEnvelope,
                "Expected 'limitingexcessenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLimitingExcessLfoOrSemicolon,
                "Expected 'limitingexcesslfo' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSemicolonOrEstimatePower,
                "Expected 'estimatepower' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSourceLfoOrToOrInterpolate,
                "Expected 'sourcelfo', 'to', or 'interpolate'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedToOrInterpolate,
                "Expected 'to' or 'interpolate'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedEstimatePower,
                "Expected 'estimatepower'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedAbsValOrRMS,
                "Expected 'absval' or 'rms'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedAbsValRMSOrPeak,
                "Expected 'absval', 'rms', 'peak', or 'peaklookahead'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLFOSampleHolds,
                "Sample and hold has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOscillatorEffectOrDisabled,
                "Expected 'delayline', 'nlproc', 'filter', 'analyzer', 'histogram', 'resampler', " +
                "'compressor', 'vocoder', 'ideallowpass', 'convolver', 'usereffect', or 'disabled'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedEffectNameOrDisabled,
                "Expected 'delayline', 'nlproc', 'filter', 'analyzer', 'histogram', 'resampler', " +
                "'compressor', 'vocoder', 'ideallowpass', 'convolver', 'usereffect', 'disabled', 'autoquiescence', " +
                "or 'suppressinitialsilence'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMaxBandMustBeOneOrGreater,
                "Maximum band count must be at least 1"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOutputgain,
                "Expected 'outputgain'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOutputaccentIndexaccentOrSemicolon,
                "Expected 'outputaccent[1-8]', 'indexaccent[1-8]', or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedIndexlfoOrOutputgainenvelope,
                "Expected 'indexlfo' or 'outputgainenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOutputgainlfoOrSemicolon,
                "Expected 'outputgainlfo' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedMaxbandcount,
                "Expected 'maxbandcount'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrOrderMustBePositiveOddInteger,
                "Order must be a positive odd integer"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSectionEffect,
                "Expected 'sectioneffect'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrParameqObsolete,
                "The 'parameq' filter is obsolete; use 'parameqold' to access that filter. " +
                "It is recommended to use 'parameq2' for new instruments."),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrEnvFormulaMustHaveTypeDouble,
                "The envelope's formula parameter must return type 'double'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrEnvFormulaSyntaxError,
                "Syntax error in envelope's formula"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedQuotedFormula,
                "Expected quoted formula"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDecibels,
                "Expected 'decibels'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedWindowduration,
                "Expected 'windowduration'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedReportOrSemicolon,
                "Expected 'report' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrQuiescenceAlreadySpecified,
                "Channel option 'autoquiescence' was already specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrDecibelsMustBeGEZero,
                "Decibels must be zero or greater"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrWindowDurationMustBeGEZero,
                "Window duration must be zero or greater"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSlopeEnvelope,
                "Expected 'slopeenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSlopeLfoOrScalingOrChannel,
                "Expected 'slopelfo', 'gainenvelope', 'left', 'right', or 'joint'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrFilterHasNoSlopeAccentX,
                "Parameter 'slopeaccent[1-8]' can only be specified for " +
                "lowshelfeq and highshelfeq"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSlopeaccent1,
                "Parameter 'slopeaccent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSlopeaccent2,
                "Parameter 'slopeaccent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSlopeaccent3,
                "Parameter 'slopeaccent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSlopeaccent4,
                "Parameter 'slopeaccent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSlopeaccent5,
                "Parameter 'slopeaccent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSlopeaccent6,
                "Parameter 'slopeaccent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSlopeaccent7,
                "Parameter 'slopeaccent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleSlopeaccent8,
                "Parameter 'slopeaccent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrRLP2OrderMustBe24Or6,
                "Order for resonantlowpass2 must be 2, 4, or 6"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrOnlyAllowedInScoreEffects,
                "The 'suppressinitialsilence' option is only allowed in score effects"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLeftRightAverageMax,
                "Expected 'left', 'right', 'averagebefore', 'averageafter', or 'maxafter'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedAbsValOrSmoothedx,
                "Expected 'absval', 'smoothedabsval', or 'smoothedrms'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLogOrLin,
                "Expected 'logarithmic' or 'linear'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedMin,
                "Expected 'min'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedMax,
                "Expected 'max'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedMinMustBePositiveForLog,
                "For log histogram, min must be positive"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedMaxMustBeGreaterThanMin,
                "Max must be greater than min"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedMinMustBeNonNegForLin,
                "For linear histogram, min must be at least zero"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedNumbins,
                "Expected 'numbins'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedMustHaveAtLeastOneBin,
                "Numbins must be at least 1"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedBinMustBeInteger,
                "Numbins must be integer"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDiscardOrNoDiscard,
                "Expected 'discardunders' or 'nodiscardunders'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedBars,
                "Expected 'bars'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedBarsCantBeNegative,
                "Bars must be at least zero"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedBarsMustBeInteger,
                "Bars must be integer"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleNetworks,
                "Phase modulation oscillator parameter 'network' was already specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedIdentifier,
                "Expected identifier"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedEqualOrPlusEqual,
                "Expected '=' or '+='"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedWaveVarOrEnvelope,
                "Expected number, 'wave', or 'envelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedPlusOrMinus,
                "Expected '+', '-', or '*'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedAsterisk,
                "Expected '*'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPlusEqualsNotAllowed,
                "Accumulation ('+=') is not allowed for 'wave' or 'envelope' statements"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedWaveComponent,
                "Expected 'samplelist', 'freqmultiplier', 'freqdivisor', 'freqadder', 'phaseadd', or 'indexenvelope'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedGainOrSemicolon,
                "Expected 'gain' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrFMWaveFreqMultiplierAlreadySpecified,
                "Component 'freqmultiplier' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrFMWaveFreqDivisorAlreadySpecified,
                "Component 'freqdivisor' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrFMWaveFreqAdderAlreadySpecified,
                "Component 'freqadder' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrFMWavePhaseAddAlreadySpecified,
                "Component 'phaseadd' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrFMWaveIndexAlreadySpecified,
                "Component 'indexenvelope' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLFOOrSemicolon,
                "Expected 'lfo' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMissingRequiredOscillatorNetwork,
                "Phase modulation oscillator requires 'network' to be defined"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrFMWaveSamplelistAlreadySpecified,
                "Component 'samplelist' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSampleName,
                "Expected sample name"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedConvolverInputType,
                "Expected 'mono', 'stereo', or 'bistereo'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedSample,
                "Expected 'sample'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLeft,
                "Expeced 'left'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedRight,
                "Expected 'right'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLeftIntoLeft,
                "Expected 'leftintoleft'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedRightIntoLeft,
                "Expected 'rightintoleft'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLeftIntoRight,
                "Expected 'leftintoright'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedRightIntoRight,
                "Expected 'rightintoright'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDirectgain,
                "Expected 'directgain'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedProcessedgain,
                "Expected 'processedgain'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedLatencyOrSemicolon,
                "Expected 'latency' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrLatencyMustBeAtLeastZero,
                "Value for 'latency' must be zero or greater"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedInitfunc,
                "Expected 'initfunc', 'datafunc', or 'oversamplingdisabled'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedInitfuncString,
                "Expected name of initialization function, as string"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDatafunc,
                "Expected 'datafunc'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedDatafuncString,
                "Expected name of data processing function, as string"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedParamOrCParen,
                "Expected 'param' or ')'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedAccentOrSemicolon,
                "Expected 'accent[1-8]' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAccent1,
                "The parameter 'accent1' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAccent2,
                "The parameter 'accent2' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAccent3,
                "The parameter 'accent3' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAccent4,
                "The parameter 'accent4' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAccent5,
                "The parameter 'accent5' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAccent6,
                "The parameter 'accent6' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAccent7,
                "The parameter 'accent7' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleAccent8,
                "The parameter 'accent8' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedWrapOrClamp,
                "Expected 'wrap' or 'clamp'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedMinsamplingrateOrSemicolon,
                "Expected 'minsamplingrate' or ';'"),
#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleLFOLoopEnvelope,
                "LFO component 'loopenvelope' has already been specified"),
#endif
            new InstrErrorRec(BuildInstrErrors.eBuildInstrReferencedUnknownPluggableProcessor,
                "Reference to an unknown pluggable processor type"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedPluggableParameterIdentifier,
                "Expected parameter identifier"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableProcessorMultiplyReferenced,
                "Parameter has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableRequiredParameterNotSpecified,
                "Required parameter has not been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrInvalidPluggableEnumIdentifier,
                "Value is not valid for that parameter"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOpenParenOrColon,
                "Expected ':' or '('"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedEnvelopeLfoOrSemicolon,
                "Expected 'envelope', 'lfo' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableNotCapableOfOscillator,
                "The specified processor is not capable of operating as an oscillator"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableNotCapableOfEffect,
                "The specified processor is not capable of operating as an effect"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedWorkspaces,
                "Expected 'workspaces', 'initfunc', 'datafunc', or 'oversamplingdisabled'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedWorkspaceElementOrSemicolon,
                "Expected 'integerarray', 'floatarray', 'doublearray', or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableConfigAlreadySpecified,
                "Configuration parameter has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableConfigParamsAlreadySpecified,
                "Configuration parameter item has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableConfigParamsExpectedColon,
                "Configuration parameter item key and value must be separated by ':'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableConfigParamsExpectedEquals,
                "Configuration parameter item key and value must be separated by '='"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableConfigParamsExpectedNumber,
                "Configuration parameter item value must be a number"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableConfigParamsExpectedCommaOrSemicolon,
                "Expected ',' or ';'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableRequiredConfigParamIsMissing,
                "Required configuration parameter is missing (or is listed after an effect parameter: all configuration parameters must come before effect parameters)"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableConfigParamsWrongNumberOfItems,
                "Configuration parameter contains an invalid number of items"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrPluggableConfigParamsMustComeBeforePluggableEffectParams,
                "Configuration parameter cannot be specified here - all configuration parameters must come before any effect parameters"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrMultipleInterpolate,
                "Parameter 'interpolate' has already been specified"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrExpectedOnOrOff,
                "Expected 'on' or 'off'"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrInterpolateNotMeaningfulForThatOscillatorType,
                "Oscillator parameter 'interpolate' does not apply to oscillator types algorithmic, sampled, or pluggable"),
            new InstrErrorRec(BuildInstrErrors.eBuildInstrLFOInterpolateOnlyAppliesToWaveTable,
                "LFO parameter 'interpolate' only applies to LFO oscillators of type wave table"),
        };

        public class BuildInstrumentContext
        {
            public readonly ScannerRec<InstrKeywordType> Scanner;
            public readonly CodeCenterRec CodeCenter;
            public string ErrorExtraMessage;

            public BuildInstrumentContext(
                ScannerRec<InstrKeywordType> Scanner,
                CodeCenterRec CodeCenter)
            {
                this.Scanner = Scanner;
                this.CodeCenter = CodeCenter;
            }
        }



        /* take a block of text and parse it into an instrument definition.  it returns an */
        /* error code.  if an error occurs, then *InstrOut is invalid, otherwise it will */
        /* be valid.  the text file remains unaltered.  *ErrorLine is numbered from 1. */
        public static BuildInstrErrors BuildInstrumentFromText(
            string TextFile,
            CodeCenterRec CodeCenter,
            out int ErrorLine,
            out string ErrorExtraMessage,
            out InstrumentRec InstrOut)
        {
            ScannerRec<InstrKeywordType> Scanner;
            InstrumentRec Instrument;
            BuildInstrErrors Error;
            TokenRec<InstrKeywordType> Token;

            InstrOut = null;
            ErrorLine = -1;
            ErrorExtraMessage = null;

            Scanner = new ScannerRec<InstrKeywordType>(TextFile, InstrKeywordTable);

            Instrument = NewInstrumentSpecifier();

            BuildInstrumentContext Context = new BuildInstrumentContext(Scanner, CodeCenter);

            Error = ParseInstrDefinition(
                Instrument,
                Context,
                out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                ErrorExtraMessage = Context.ErrorExtraMessage;
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenEndOfInput)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrUnexpectedInput;
            }

            InstrOut = Instrument;
            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* build just a list of effects */
        private static BuildInstrErrors BuildEffectList(
            string TextFile,
            CodeCenterRec CodeCenter,
            out int ErrorLine,
            out string ErrorExtraMessage,
            out EffectSpecListRec EffectOut,
            InstrKeywordType LeadingKeywordTag,
            BuildInstrErrors LeadingKeywordError)
        {
            ScannerRec<InstrKeywordType> Scanner;
            EffectSpecListRec EffectSpec;
            BuildInstrErrors Error;
            TokenRec<InstrKeywordType> Token;

            EffectOut = null;
            ErrorLine = -1;
            ErrorExtraMessage = null;

            Scanner = new ScannerRec<InstrKeywordType>(TextFile, InstrKeywordTable);

            BuildInstrumentContext Context = new BuildInstrumentContext(Scanner, CodeCenter);

            EffectSpec = NewEffectSpecList();

            do
            {
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenEndOfInput)
                {
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != LeadingKeywordTag))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return LeadingKeywordError;
                    }
                    Error = ParseTrackEffect(
                        EffectSpec,
                        Context,
                        out ErrorLine,
                        LeadingKeywordTag);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        ErrorExtraMessage = Context.ErrorExtraMessage;
                        return Error;
                    }
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                    }
                }
            } while (Token.GetTokenType() != TokenTypes.eTokenEndOfInput);

            EffectOut = EffectSpec;
            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* build just a list of effects */
        public static BuildInstrErrors BuildScoreEffectList(
            string TextFile,
            CodeCenterRec CodeCenter,
            out int ErrorLine,
            out string ErrorExtraMessage,
            out EffectSpecListRec EffectOut)
        {
            return BuildEffectList(
                TextFile,
                CodeCenter,
                out ErrorLine,
                out ErrorExtraMessage,
                out EffectOut,
                InstrKeywordType.eKeywordScoreeffect,
                BuildInstrErrors.eBuildInstrExpectedScoreEffect);
        }




        /* build just a list of effects */
        public static BuildInstrErrors BuildSectionEffectList(
            string TextFile,
            CodeCenterRec CodeCenter,
            out int ErrorLine,
            out string ErrorExtraMessage,
            out EffectSpecListRec EffectOut)
        {
            return BuildEffectList(
                TextFile,
                CodeCenter,
                out ErrorLine,
                out ErrorExtraMessage,
                out EffectOut,
                InstrKeywordType.eKeywordSectioneffect,
                BuildInstrErrors.eBuildInstrExpectedSectionEffect);
        }




        /* get a static null terminated string describing the error */
        public static string BuildInstrGetErrorMessageText(
            BuildInstrErrors ErrorCode,
            string extraText)
        {
#if DEBUG
            /* verify structure */
            for (int i = 0; i < InstrErrors.Length; i += 1)
            {
                if (InstrErrors[i].ErrorCode != i + BuildInstrErrors.eBuildInstr_Start)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
            }
#endif

            /* direct index error string */
            int index = ErrorCode - BuildInstrErrors.eBuildInstr_Start;
            if ((index < 1) || (index >= InstrErrors.Length))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            Debug.Assert(InstrErrors[index].ErrorCode == ErrorCode);
            string message = InstrErrors[index].Message;
            if (!String.IsNullOrEmpty(extraText))
            {
                if (!String.IsNullOrEmpty(message))
                {
                    message = String.Format("{0} ({1})", message, extraText);
                }
                else
                {
                    message = extraText;
                }
            }
            return message;
        }




        //
        //
        // BuildInstrument2
        //
        //

        /* token enumeration definitions */
        public enum InstrKeywordType
        {
            eKeywordInstrument,
            eKeywordLoudness,
            eKeywordFrequencylfo,
            eKeywordOscillator,
            eKeywordFreqenvelope,
            eKeywordModulation,
            eKeywordConstant,
            eKeywordSignsine,
            eKeywordPlussine,
            eKeywordSigntriangle,
            eKeywordPlustriangle,
            eKeywordSignsquare,
            eKeywordPlussquare,
            eKeywordSignramp,
            eKeywordPlusramp,
            eKeywordSignlinfuzz,
            eKeywordPluslinfuzz,
            eKeywordAdditive,
            eKeywordMultiplicative,
            eKeywordInversemult,
            eKeywordType,
            eKeywordSampled,
            eKeywordWavetable,
            eKeywordSamplelist,
            eKeywordFreqmultiplier,
            eKeywordFreqdivisor,
            eKeywordFreqadder,
            eKeywordLoudnessenvelope,
            eKeywordLoudnessfactor,
            eKeywordTotalscaling,
            eKeywordExponential,
            eKeywordLinear,
            eKeywordLevel,
            eKeywordDelay,
            eKeywordSustainpoint,
            eKeywordReleasepoint,
            eKeywordSustainpointnoskip,
            eKeywordReleasepointnoskip,
            eKeywordAmpaccent1,
            eKeywordAmpaccent2,
            eKeywordAmpaccent3,
            eKeywordAmpaccent4,
            eKeywordAmpaccent5,
            eKeywordAmpaccent6,
            eKeywordAmpaccent7,
            eKeywordAmpaccent8,
            eKeywordAmpfreq,
            eKeywordRateaccent1,
            eKeywordRateaccent2,
            eKeywordRateaccent3,
            eKeywordRateaccent4,
            eKeywordRateaccent5,
            eKeywordRateaccent6,
            eKeywordRateaccent7,
            eKeywordRateaccent8,
            eKeywordRatefreq,
            eKeywordScale,
            eKeywordAmpenvelope,
            eKeywordLoudnesslfo,
            eKeywordIndexenvelope,
            eKeywordIndexlfo,
            eKeywordPoints,
            eKeywordStereobias,
            eKeywordDisplacement,
            eKeywordSurroundbias,
            eKeywordHertz,
            eKeywordOrigin,
            eKeywordHalfsteps,
            eKeywordSquare,
            eKeywordTriangle,
            eKeywordTrackeffect,
            eKeywordTap,
            eKeywordTo,
            eKeywordSourceaccent1,
            eKeywordSourceaccent2,
            eKeywordSourceaccent3,
            eKeywordSourceaccent4,
            eKeywordTargetaccent1,
            eKeywordTargetaccent2,
            eKeywordTargetaccent3,
            eKeywordTargetaccent4,
            eKeywordScaleaccent1,
            eKeywordScaleaccent2,
            eKeywordScaleaccent3,
            eKeywordScaleaccent4,
            eKeywordLeft,
            eKeywordRight,
            eKeywordMono,
            eKeywordDelayline,
            eKeywordMaxdelaytime,
            eKeywordSlope,
            eKeywordCenter,
            eKeywordEnvelope,
            eKeywordNlproc,
            eKeywordInputscaling,
            eKeywordOutputscaling,
            eKeywordInputaccent1,
            eKeywordInputaccent2,
            eKeywordInputaccent3,
            eKeywordInputaccent4,
            eKeywordOutputaccent1,
            eKeywordOutputaccent2,
            eKeywordOutputaccent3,
            eKeywordOutputaccent4,
            eKeywordWavetableindex,
            eKeywordIndexaccent1,
            eKeywordIndexaccent2,
            eKeywordIndexaccent3,
            eKeywordIndexaccent4,
            eKeywordFilter,
            eKeywordFreq,
            eKeywordBandwidth,
            eKeywordDefaultscaling,
            eKeywordUnitymidbandgain,
            eKeywordUnitynoisegain,
            eKeywordUnityzerohertzgain,
            eKeywordLowpass,
            eKeywordHighpass,
            eKeywordReson,
            eKeywordZero,
            eKeywordButterworthlowpass,
            eKeywordButterworthhighpass,
            eKeywordButterworthbandpass,
            eKeywordButterworthbandreject,
            eKeywordFreqaccent1,
            eKeywordFreqaccent2,
            eKeywordFreqaccent3,
            eKeywordFreqaccent4,
            eKeywordBandwidthaccent1,
            eKeywordBandwidthaccent2,
            eKeywordBandwidthaccent3,
            eKeywordBandwidthaccent4,
            eKeywordNull,
            eKeywordAnalyzer,
            eKeywordScoreeffect,
            eKeywordEffect,
            eKeywordSourceenvelope,
            eKeywordTargetenvelope,
            eKeywordScaleenvelope,
            eKeywordSourcelfo,
            eKeywordTargetlfo,
            eKeywordScalelfo,
            eKeywordInputscalingenvelope,
            eKeywordOutputscalingenvelope,
            eKeywordInputscalinglfo,
            eKeywordOutputscalinglfo,
            eKeywordBandwidthenvelope,
            eKeywordFreqlfo,
            eKeywordBandwidthlfo,
            eKeywordAmplfo,
            eKeywordPitchcontrol,
            eKeywordParameq,
            eKeywordGain,
            eKeywordGainaccent1,
            eKeywordGainaccent2,
            eKeywordGainaccent3,
            eKeywordGainaccent4,
            eKeywordGainenvelope,
            eKeywordGainlfo,
            eKeywordOrder,
            eKeywordResonantlowpass,
            eKeywordResampler,
            eKeywordRate,
            eKeywordTruncate,
            eKeywordInterpolate,
            eKeywordSourceaccent5,
            eKeywordSourceaccent6,
            eKeywordSourceaccent7,
            eKeywordSourceaccent8,
            eKeywordTargetaccent5,
            eKeywordTargetaccent6,
            eKeywordTargetaccent7,
            eKeywordTargetaccent8,
            eKeywordScaleaccent5,
            eKeywordScaleaccent6,
            eKeywordScaleaccent7,
            eKeywordScaleaccent8,
            eKeywordFreqaccent5,
            eKeywordFreqaccent6,
            eKeywordFreqaccent7,
            eKeywordFreqaccent8,
            eKeywordInputaccent5,
            eKeywordInputaccent6,
            eKeywordInputaccent7,
            eKeywordInputaccent8,
            eKeywordOutputaccent5,
            eKeywordOutputaccent6,
            eKeywordOutputaccent7,
            eKeywordOutputaccent8,
            eKeywordIndexaccent5,
            eKeywordIndexaccent6,
            eKeywordIndexaccent7,
            eKeywordIndexaccent8,
            eKeywordBandwidthaccent5,
            eKeywordBandwidthaccent6,
            eKeywordBandwidthaccent7,
            eKeywordBandwidthaccent8,
            eKeywordGainaccent5,
            eKeywordGainaccent6,
            eKeywordGainaccent7,
            eKeywordGainaccent8,
            eKeywordFof,
            eKeywordFofsamprate,
            eKeywordFofcompress,
            eKeywordOverlap,
            eKeywordDiscard,
            eKeywordFofexpand,
            eKeywordSilence,
            eKeywordLoop,
            eKeywordFofsamprateenvelope,
            eKeywordFofsampratelfo,
            eKeywordLowpassfilter,
            eKeywordNormalpower,
            eKeywordThreshpower,
            eKeywordRatio,
            eKeywordFiltercutoff,
            eKeywordDecayrate,
            eKeywordAttackrate,
            eKeywordLimitingexcess,
            eKeywordNormalpoweraccent1,
            eKeywordNormalpoweraccent2,
            eKeywordNormalpoweraccent3,
            eKeywordNormalpoweraccent4,
            eKeywordNormalpoweraccent5,
            eKeywordNormalpoweraccent6,
            eKeywordNormalpoweraccent7,
            eKeywordNormalpoweraccent8,
            eKeywordThreshpoweraccent1,
            eKeywordThreshpoweraccent2,
            eKeywordThreshpoweraccent3,
            eKeywordThreshpoweraccent4,
            eKeywordThreshpoweraccent5,
            eKeywordThreshpoweraccent6,
            eKeywordThreshpoweraccent7,
            eKeywordThreshpoweraccent8,
            eKeywordRatioaccent1,
            eKeywordRatioaccent2,
            eKeywordRatioaccent3,
            eKeywordRatioaccent4,
            eKeywordRatioaccent5,
            eKeywordRatioaccent6,
            eKeywordRatioaccent7,
            eKeywordRatioaccent8,
            eKeywordFiltercutoffaccent1,
            eKeywordFiltercutoffaccent2,
            eKeywordFiltercutoffaccent3,
            eKeywordFiltercutoffaccent4,
            eKeywordFiltercutoffaccent5,
            eKeywordFiltercutoffaccent6,
            eKeywordFiltercutoffaccent7,
            eKeywordFiltercutoffaccent8,
            eKeywordDecayrateaccent1,
            eKeywordDecayrateaccent2,
            eKeywordDecayrateaccent3,
            eKeywordDecayrateaccent4,
            eKeywordDecayrateaccent5,
            eKeywordDecayrateaccent6,
            eKeywordDecayrateaccent7,
            eKeywordDecayrateaccent8,
            eKeywordAttackrateaccent1,
            eKeywordAttackrateaccent2,
            eKeywordAttackrateaccent3,
            eKeywordAttackrateaccent4,
            eKeywordAttackrateaccent5,
            eKeywordAttackrateaccent6,
            eKeywordAttackrateaccent7,
            eKeywordAttackrateaccent8,
            eKeywordLimitingexcessaccent1,
            eKeywordLimitingexcessaccent2,
            eKeywordLimitingexcessaccent3,
            eKeywordLimitingexcessaccent4,
            eKeywordLimitingexcessaccent5,
            eKeywordLimitingexcessaccent6,
            eKeywordLimitingexcessaccent7,
            eKeywordLimitingexcessaccent8,
            eKeywordCompressor,
            eKeywordNormalpowerenvelope,
            eKeywordThreshpowerenvelope,
            eKeywordRatioenvelope,
            eKeywordFiltercutoffenvelope,
            eKeywordDecayrateenvelope,
            eKeywordAttackrateenvelope,
            eKeywordLimitingexcessenvelope,
            eKeywordNormalpowerlfo,
            eKeywordThreshpowerlfo,
            eKeywordRatiolfo,
            eKeywordFiltercutofflfo,
            eKeywordDecayratelfo,
            eKeywordAttackratelfo,
            eKeywordLimitingexcesslfo,
            eKeywordEstimatepower,
            eKeywordAbsval,
            eKeywordRms,
            eKeywordSampleandhold,
            eKeywordDisabled,
            eKeywordVocoder,
            eKeywordMaxbandcount,
            eKeywordOutputgain,
            eKeywordOutputgainenvelope,
            eKeywordOutputgainlfo,
            eKeywordIdeallowpass,
            eKeywordParameq2,
            eKeywordSectioneffect,
            eKeywordParameqold,
            eKeywordFormula,
            eKeywordAutoquiescence,
            eKeywordDecibels,
            eKeywordWindowduration,
            eKeywordReport,
            eKeywordLowshelfeq,
            eKeywordHighshelfeq,
            eKeywordSlopeenvelope,
            eKeywordSlopelfo,
            eKeywordSlopeaccent1,
            eKeywordSlopeaccent2,
            eKeywordSlopeaccent3,
            eKeywordSlopeaccent4,
            eKeywordSlopeaccent5,
            eKeywordSlopeaccent6,
            eKeywordSlopeaccent7,
            eKeywordSlopeaccent8,
            eKeywordResonantlowpass2,
            eKeywordSuppressinitialsilence,
            eKeywordAveragebefore,
            eKeywordAverageafter,
            eKeywordMax,
            eKeywordSmoothedabsval,
            eKeywordSmoothedrms,
            eKeywordLogarithmic,
            eKeywordMin,
            eKeywordNumbins,
            eKeywordDiscardunders,
            eKeywordNodiscardunders,
            eKeywordBars,
            eKeywordHistogram,
            eKeywordPulse,
            eKeywordRamp,
            eKeywordMaxafter,
            eKeywordPeak,
            eKeywordNetwork,
            eKeywordWave,
            eKeywordPhaseadd,
            eKeywordLfo,
            eKeywordPhasemodulation,
            eKeywordConvolver,
            eKeywordStereo,
            eKeywordBistereo,
            eKeywordLeftintoleft,
            eKeywordRightintoleft,
            eKeywordLeftintoright,
            eKeywordRightintoright,
            eKeywordSample,
            eKeywordDirectgain,
            eKeywordProcessedgain,
            eKeywordLatency,
            eKeywordPeaklookahead,
            eKeywordUsereffect,
            eKeywordInitfunc,
            eKeywordDatafunc,
            eKeywordParam,
            eKeywordAccent1,
            eKeywordAccent2,
            eKeywordAccent3,
            eKeywordAccent4,
            eKeywordAccent5,
            eKeywordAccent6,
            eKeywordAccent7,
            eKeywordAccent8,
            eKeywordBroken,
            eKeywordGood,
            eKeywordOversamplingdisabled,
            eKeywordOverflow,
            eKeywordWrap,
            eKeywordClamp,
            eKeywordMinsamplingrate,
#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
            eKeywordLoopenvelope,
#endif
            eKeywordWorkspaces,
            eKeywordIntegerarray,
            eKeywordFloatarray,
            eKeywordDoublearray,
            eKeywordSmoothed,
            eKeywordJoint,
            eKeywordOn,
            eKeywordOff,
        }

        /* symbols for detecting redundant or missing declarations in instruments */
        public enum Req
        {
            INSTRLIST_ONCEONLY_LOUDNESS,

            INSTRLIST_REQUIRED_OSCILLATOR,


            LFODEFINITION_ONCEONLY_FREQENVELOPE,
            LFODEFINITION_ONCEONLY_OSCILLATORTYPE,
            LFODEFINITION_ONCEONLY_MODULATIONTYPE,
            LFODEFINITION_ONCEONLY_ADDINGMODE,
            LFODEFINITION_ONCEONLY_FILTER,
            LFODEFINITION_ONCEONLY_SAMPLEHOLD,

            LFODEFINITION_REQUIREDONCEONLY_AMPENVELOPE,

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
            LFODEFINITION_REQUIREDONCEONLY_LOOPENVELOPE,
#endif

            LFODEFINITION_ONCEONLY_INTERPOLATE,


            OSCILLATORDEFINITION_ONCEONLY_LOUDNESS,
            OSCILLATORDEFINITION_ONCEONLY_FREQMULTIPLIER,
            OSCILLATORDEFINITION_ONCEONLY_FREQDIVISOR,
            OSCILLATORDEFINITION_ONCEONLY_INDEXENVELOPE,
            OSCILLATORDEFINITION_ONCEONLY_STEREOBIAS,
            OSCILLATORDEFINITION_ONCEONLY_TIMEDISPLACEMENT,
            OSCILLATORDEFINITION_ONCEONLY_SURROUNDBIAS,
            OSCILLATORDEFINITION_ONCEONLY_FREQADDER,

            OSCILLATORDEFINITION_REQUIREDONCEONLY_TYPE,
            OSCILLATORDEFINITION_REQUIREDONCEONLY_LOUDNESSENVELOPE,
            OSCILLATORDEFINITION_REQUIREDONCEONLY_SAMPLELIST,

            OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFSAMPRATE,
            OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFCOMPRESS,
            OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFEXPAND,
            OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFENVELOPE,

            OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_NETWORK,

            OSCILLATORDEFINITION_ONCEONLY_INTERPOLATE,


            ENVELOPEDEFINITION_ONCEONLY_TOTALSCALING,
            ENVELOPEDEFINITION_ONCEONLY_POINTS,
            ENVELOPEDEFINITION_ONCEONLY_PITCHCONTROL,
            ENVELOPEDEFINITION_ONCEONLY_FORMULA,


            ENVPOINTDEFINITION_ONCEONLY_AMPACCENT1,
            ENVPOINTDEFINITION_ONCEONLY_AMPACCENT2,
            ENVPOINTDEFINITION_ONCEONLY_AMPACCENT3,
            ENVPOINTDEFINITION_ONCEONLY_AMPACCENT4,
            ENVPOINTDEFINITION_ONCEONLY_AMPACCENT5,
            ENVPOINTDEFINITION_ONCEONLY_AMPACCENT6,
            ENVPOINTDEFINITION_ONCEONLY_AMPACCENT7,
            ENVPOINTDEFINITION_ONCEONLY_AMPACCENT8,
            ENVPOINTDEFINITION_ONCEONLY_AMPFREQ,
            ENVPOINTDEFINITION_ONCEONLY_RATEACCENT1,
            ENVPOINTDEFINITION_ONCEONLY_RATEACCENT2,
            ENVPOINTDEFINITION_ONCEONLY_RATEACCENT3,
            ENVPOINTDEFINITION_ONCEONLY_RATEACCENT4,
            ENVPOINTDEFINITION_ONCEONLY_RATEACCENT5,
            ENVPOINTDEFINITION_ONCEONLY_RATEACCENT6,
            ENVPOINTDEFINITION_ONCEONLY_RATEACCENT7,
            ENVPOINTDEFINITION_ONCEONLY_RATEACCENT8,
            ENVPOINTDEFINITION_ONCEONLY_RATEFREQ,
            ENVPOINTDEFINITION_ONCEONLY_CURVE,


            DELAYEFFECT_REQUIREDONCEONLY_MAXDELAYTIME,


            DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT1,
            DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT2,
            DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT3,
            DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT4,
            DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT5,
            DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT6,
            DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT7,
            DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT8,
            DELAYEFFECTATTR_ONCEONLY_TARGETACCENT1,
            DELAYEFFECTATTR_ONCEONLY_TARGETACCENT2,
            DELAYEFFECTATTR_ONCEONLY_TARGETACCENT3,
            DELAYEFFECTATTR_ONCEONLY_TARGETACCENT4,
            DELAYEFFECTATTR_ONCEONLY_TARGETACCENT5,
            DELAYEFFECTATTR_ONCEONLY_TARGETACCENT6,
            DELAYEFFECTATTR_ONCEONLY_TARGETACCENT7,
            DELAYEFFECTATTR_ONCEONLY_TARGETACCENT8,
            DELAYEFFECTATTR_ONCEONLY_SCALEACCENT1,
            DELAYEFFECTATTR_ONCEONLY_SCALEACCENT2,
            DELAYEFFECTATTR_ONCEONLY_SCALEACCENT3,
            DELAYEFFECTATTR_ONCEONLY_SCALEACCENT4,
            DELAYEFFECTATTR_ONCEONLY_SCALEACCENT5,
            DELAYEFFECTATTR_ONCEONLY_SCALEACCENT6,
            DELAYEFFECTATTR_ONCEONLY_SCALEACCENT7,
            DELAYEFFECTATTR_ONCEONLY_SCALEACCENT8,
            DELAYEFFECTATTR_ONCEONLY_LOWPASSCUTOFF,
            DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT1,
            DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT2,
            DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT3,
            DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT4,
            DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT5,
            DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT6,
            DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT7,
            DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT8,


            NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT1,
            NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT2,
            NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT3,
            NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT4,
            NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT5,
            NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT6,
            NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT7,
            NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT8,
            NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT1,
            NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT2,
            NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT3,
            NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT4,
            NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT5,
            NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT6,
            NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT7,
            NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT8,
            NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT1,
            NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT2,
            NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT3,
            NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT4,
            NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT5,
            NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT6,
            NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT7,
            NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT8,
            NLPROCEFFECTATTR_ONCEONLY_OVERFLOWMODE,


            FILTEREFFECTATTR_ONCEONLY_FREQACCENT1,
            FILTEREFFECTATTR_ONCEONLY_FREQACCENT2,
            FILTEREFFECTATTR_ONCEONLY_FREQACCENT3,
            FILTEREFFECTATTR_ONCEONLY_FREQACCENT4,
            FILTEREFFECTATTR_ONCEONLY_FREQACCENT5,
            FILTEREFFECTATTR_ONCEONLY_FREQACCENT6,
            FILTEREFFECTATTR_ONCEONLY_FREQACCENT7,
            FILTEREFFECTATTR_ONCEONLY_FREQACCENT8,
            FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT1,
            FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT2,
            FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT3,
            FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT4,
            FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT5,
            FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT6,
            FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT7,
            FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT8,
            FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALING,
            FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT1,
            FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT2,
            FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT3,
            FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT4,
            FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT5,
            FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT6,
            FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT7,
            FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT8,
            FILTEREFFECTATTR_ONCEONLY_GAINACCENT1,
            FILTEREFFECTATTR_ONCEONLY_GAINACCENT2,
            FILTEREFFECTATTR_ONCEONLY_GAINACCENT3,
            FILTEREFFECTATTR_ONCEONLY_GAINACCENT4,
            FILTEREFFECTATTR_ONCEONLY_GAINACCENT5,
            FILTEREFFECTATTR_ONCEONLY_GAINACCENT6,
            FILTEREFFECTATTR_ONCEONLY_GAINACCENT7,
            FILTEREFFECTATTR_ONCEONLY_GAINACCENT8,


            COMPRESSOR_ONCEONLY_INPUTACCENT1,
            COMPRESSOR_ONCEONLY_INPUTACCENT2,
            COMPRESSOR_ONCEONLY_INPUTACCENT3,
            COMPRESSOR_ONCEONLY_INPUTACCENT4,
            COMPRESSOR_ONCEONLY_INPUTACCENT5,
            COMPRESSOR_ONCEONLY_INPUTACCENT6,
            COMPRESSOR_ONCEONLY_INPUTACCENT7,
            COMPRESSOR_ONCEONLY_INPUTACCENT8,
            COMPRESSOR_ONCEONLY_OUTPUTACCENT1,
            COMPRESSOR_ONCEONLY_OUTPUTACCENT2,
            COMPRESSOR_ONCEONLY_OUTPUTACCENT3,
            COMPRESSOR_ONCEONLY_OUTPUTACCENT4,
            COMPRESSOR_ONCEONLY_OUTPUTACCENT5,
            COMPRESSOR_ONCEONLY_OUTPUTACCENT6,
            COMPRESSOR_ONCEONLY_OUTPUTACCENT7,
            COMPRESSOR_ONCEONLY_OUTPUTACCENT8,
            COMPRESSOR_ONCEONLY_NORMALPOWERACCENT1,
            COMPRESSOR_ONCEONLY_NORMALPOWERACCENT2,
            COMPRESSOR_ONCEONLY_NORMALPOWERACCENT3,
            COMPRESSOR_ONCEONLY_NORMALPOWERACCENT4,
            COMPRESSOR_ONCEONLY_NORMALPOWERACCENT5,
            COMPRESSOR_ONCEONLY_NORMALPOWERACCENT6,
            COMPRESSOR_ONCEONLY_NORMALPOWERACCENT7,
            COMPRESSOR_ONCEONLY_NORMALPOWERACCENT8,
            COMPRESSOR_ONCEONLY_THRESHPOWERACCENT1,
            COMPRESSOR_ONCEONLY_THRESHPOWERACCENT2,
            COMPRESSOR_ONCEONLY_THRESHPOWERACCENT3,
            COMPRESSOR_ONCEONLY_THRESHPOWERACCENT4,
            COMPRESSOR_ONCEONLY_THRESHPOWERACCENT5,
            COMPRESSOR_ONCEONLY_THRESHPOWERACCENT6,
            COMPRESSOR_ONCEONLY_THRESHPOWERACCENT7,
            COMPRESSOR_ONCEONLY_THRESHPOWERACCENT8,
            COMPRESSOR_ONCEONLY_RATIOACCENT1,
            COMPRESSOR_ONCEONLY_RATIOACCENT2,
            COMPRESSOR_ONCEONLY_RATIOACCENT3,
            COMPRESSOR_ONCEONLY_RATIOACCENT4,
            COMPRESSOR_ONCEONLY_RATIOACCENT5,
            COMPRESSOR_ONCEONLY_RATIOACCENT6,
            COMPRESSOR_ONCEONLY_RATIOACCENT7,
            COMPRESSOR_ONCEONLY_RATIOACCENT8,
            COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT1,
            COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT2,
            COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT3,
            COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT4,
            COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT5,
            COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT6,
            COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT7,
            COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT8,
            COMPRESSOR_ONCEONLY_DECAYRATEACCENT1,
            COMPRESSOR_ONCEONLY_DECAYRATEACCENT2,
            COMPRESSOR_ONCEONLY_DECAYRATEACCENT3,
            COMPRESSOR_ONCEONLY_DECAYRATEACCENT4,
            COMPRESSOR_ONCEONLY_DECAYRATEACCENT5,
            COMPRESSOR_ONCEONLY_DECAYRATEACCENT6,
            COMPRESSOR_ONCEONLY_DECAYRATEACCENT7,
            COMPRESSOR_ONCEONLY_DECAYRATEACCENT8,
            COMPRESSOR_ONCEONLY_ATTACKRATEACCENT1,
            COMPRESSOR_ONCEONLY_ATTACKRATEACCENT2,
            COMPRESSOR_ONCEONLY_ATTACKRATEACCENT3,
            COMPRESSOR_ONCEONLY_ATTACKRATEACCENT4,
            COMPRESSOR_ONCEONLY_ATTACKRATEACCENT5,
            COMPRESSOR_ONCEONLY_ATTACKRATEACCENT6,
            COMPRESSOR_ONCEONLY_ATTACKRATEACCENT7,
            COMPRESSOR_ONCEONLY_ATTACKRATEACCENT8,
            COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT1,
            COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT2,
            COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT3,
            COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT4,
            COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT5,
            COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT6,
            COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT7,
            COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT8,


            VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT1,
            VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT2,
            VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT3,
            VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT4,
            VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT5,
            VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT6,
            VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT7,
            VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT8,
            VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT1,
            VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT2,
            VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT3,
            VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT4,
            VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT5,
            VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT6,
            VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT7,
            VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT8,


            FMWAVEDEFINITION_ONCEONLY_FREQMULTIPLIER,
            FMWAVEDEFINITION_ONCEONLY_FREQDIVISOR,
            FMWAVEDEFINITION_ONCEONLY_FREQADDER,
            FMWAVEDEFINITION_ONCEONLY_PHASEADD,
            FMWAVEDEFINITION_ONCEONLY_INDEX,
            FMWAVEDEFINITION_ONCEONLY_SAMPLELIST,


            USEREFFECTPARAM_ONCEONLY_ACCENT1,
            USEREFFECTPARAM_ONCEONLY_ACCENT2,
            USEREFFECTPARAM_ONCEONLY_ACCENT3,
            USEREFFECTPARAM_ONCEONLY_ACCENT4,
            USEREFFECTPARAM_ONCEONLY_ACCENT5,
            USEREFFECTPARAM_ONCEONLY_ACCENT6,
            USEREFFECTPARAM_ONCEONLY_ACCENT7,
            USEREFFECTPARAM_ONCEONLY_ACCENT8,


            DUMMY_STUPID_THINGIE_PLACEHOLDER,
        }




        /*    1:   <instr_definition>      ::= instrument ( <instr_list> ) */
        /* FIRST SET: */
        /*  <instr_definition>      : {instrument} */
        /* FOLLOW SET: */
        /*  <instr_definition>      : {$$$} */
        public static BuildInstrErrors ParseInstrDefinition(
            InstrumentRec Instrument,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;

            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInstrument))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInstrument;
            }

            return ScanInstrument(Instrument, Context, out ErrorLine);
        }




        /*    2:   <instr_list>            ::= <instr_elem> ; <instr_list> */
        /*    3:                           ::=  */
        /* FIRST SET: */
        /*  <instr_list>            : {loudness, frequencylfo, oscillator, <instr_elem>} */
        /* FOLLOW SET: */
        /*  <instr_list>            : {)} */
        public static BuildInstrErrors ParseInstrList(
            InstrumentRec Instrument,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            else
            {
                Context.Scanner.UngetToken(Token);

                Error = ParseInstrElem(Instrument, Context, out ErrorLine, RequiredOnceOnly);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                /* get semicolon */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }

                return ParseInstrList(Instrument, Context, out ErrorLine, RequiredOnceOnly);
            }
        }




        /*    4:   <instr_elem>            ::= loudness <number> */
        /*    5:                           ::= frequencylfo ( <lfo_definition> ) */
        /*    6:                           ::= oscillator <identifier> ( <oscillator_definition> ) */
        /*  XXX:                           ::= trackeffect <trackeffect> */
        /*  XXX:                           ::= effect <oscillator_effect> */
        /* FIRST SET: */
        /*  <instr_elem>            : {loudness, frequencylfo, oscillator} */
        /* FOLLOW SET: */
        /*  <instr_elem>            : {;} */
        public static BuildInstrErrors ParseInstrElem(
            InstrumentRec Instrument,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInstrumentMember;
            }

            if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordLoudness)
            {
                double Number;

                if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.INSTRLIST_ONCEONLY_LOUDNESS))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrMultipleInstrLoudness;
                }
                Error = ParseNumber(Context, out ErrorLine, out Number);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                InstrumentSetOverallLoudness(Instrument, Number);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordFrequencylfo)
            {
                LFOSpecRec LFO;

                /* allocate the LFO */
                LFO = NewLFOSpecifier();

                /* the default arithmetic mode for pitch LFO is halfsteps */
                SetLFOSpecAddingMode(LFO, LFOAdderMode.eLFOHalfSteps);
                LFOListSpecAppendNewEntry(GetInstrumentFrequencyLFOList(Instrument), LFO);

                return ScanLfoSpec(LFO, Context, out ErrorLine);
            }
            else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordOscillator)
            {
                OscillatorRec Oscillator;

                /* allocate a new oscillator & add it to the instrument */
                Oscillator = NewOscillatorSpecifier();
                AppendOscillatorToList(GetInstrumentOscillatorList(Instrument), Oscillator);

                /* get the oscillator's name or open paren */
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() == TokenTypes.eTokenOpenParen)
                    || (Token.GetTokenType() == TokenTypes.eTokenColon))
                {
                    /* open paren gets pushed back */
                    Context.Scanner.UngetToken(Token);
                }
                else
                {
                    /* name just gets ignored, as long as it's the right type */
                    if (!((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                        || (Token.GetTokenType() == TokenTypes.eTokenIdentifier)
                        || (Token.GetTokenType() == TokenTypes.eTokenString)))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedStringOrIdentifier;
                    }
                }

                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenOpenParen)
                {
                    // legacy form
                    Context.Scanner.UngetToken(Token);
                    Error = ScanOscillatorDefinition(
                        Oscillator,
                        Context,
                        out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                }
                else if (Token.GetTokenType() == TokenTypes.eTokenColon)
                {
                    string classIdentifier;

                    Token = Context.Scanner.GetNextToken();
                    Error = ParseGlobalIdentifierString(
                        Context,
                        Token,
                        out ErrorLine,
                        out classIdentifier);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        Context.ErrorExtraMessage = GetPluggableClassListForError();
                        return Error;
                    }

                    int startLine = Context.Scanner.GetCurrentLineNumber();

                    // pluggable form
                    PluggableOscSpec spec;
                    Error = ParsePluggableOscProcessor(
                        Context,
                        Oscillator,
                        out ErrorLine,
                        true/*enabled*/,
                        classIdentifier,
                        PluggableRole.Oscillator,
                        out spec);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    OscillatorSetTheType(Oscillator, OscillatorTypes.eOscillatorPluggable);
                    PutOscillatorPluggableSpec(Oscillator, spec);
                }
                else
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedOpenParenOrColon;
                }

                MarkPresent(RequiredOnceOnly, Req.INSTRLIST_REQUIRED_OSCILLATOR);

                return BuildInstrErrors.eBuildInstrNoError;
            }
            else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordTrackeffect)
            {
                return ParseTrackEffect(
                    GetInstrumentEffectSpecList(Instrument),
                    Context,
                    out ErrorLine,
                    InstrKeywordType.eKeywordTrackeffect);
            }
            else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordEffect)
            {
                return ParseOscillatorEffect(
                    GetInstrumentCombinedOscEffectSpecList(Instrument),
                    Context,
                    out ErrorLine);
            }
            else
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInstrumentMember;
            }
        }




        /*   80:   <number>                ::= <integertoken> */
        /*   81:                           ::= <floattoken> */
        /* FIRST SET: */
        /*  <number>                : {<integertoken>, <floattoken>} */
        /* FOLLOW SET: */
        /*  <number>                : {<integertoken>, <floattoken>, ), ;, type, */
        /*       exponential, linear, to, sustainpoint, ampaccent1, ampaccent2, */
        /*       ampaccent3, ampaccent4, ampfreq, rateaccent1, rateaccent2, rateaccent3, */
        /*       rateaccent4, ratefreq, originadjust, <number>, <env_point_list>, */
        /*       <env_point_elem>, <env_attributes>, <env_one_attribute>} */
        public static BuildInstrErrors ParseNumber(
            BuildInstrumentContext Context,
            out int ErrorLine,
            out double NumberOut)
        {
            TokenRec<InstrKeywordType> Token;
            double Number;
            double Sign = 1;

            NumberOut = Double.NaN;
            ErrorLine = -1;

        /* NOTE: this function should try to be compatible with ParseTrackEffectNumber */

        Again:
            Token = Context.Scanner.GetNextToken();
            switch (Token.GetTokenType())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedNumber;

                case TokenTypes.eTokenMinus:
                    Sign = -Sign;
                    goto Again;
                case TokenTypes.eTokenPlus:
                    goto Again;

                case TokenTypes.eTokenInteger:
                    Number = Token.GetTokenIntegerValue();
                    break;
                case TokenTypes.eTokenSingle:
                    Number = Token.GetTokenSingleValue();
                    break;
                case TokenTypes.eTokenDouble:
                    Number = Token.GetTokenDoubleValue();
                    break;

                case TokenTypes.eTokenString:
                    {
                        CompileErrors CompileError;
                        int CompileErrorLine;
                        DataTypes ReturnType;
                        Compiler.ASTExpression Expr;
                        PcodeRec FuncOut;

                        CompileError = Compiler.CompileSpecialFunction(
                            Context.CodeCenter,
                            new FunctionParamRec[0],
                            out CompileErrorLine,
                            out ReturnType,
                            Token.GetTokenStringValue(),
                            true/*suppressCILEmission -- this code path expects constant expressions*/,
                            out FuncOut,
                            out Expr);
                        if (CompileError != CompileErrors.eCompileNoError)
                        {
                            ErrorLine = CompileErrorLine + Context.Scanner.GetCurrentLineNumber() - 1;
                            return BuildInstrErrors.eBuildInstrExpectedNumber;
                        }
                        /* ensure expression is compile-time constant */
                        if (Compiler.ExprKind.eExprOperand != Expr.Kind)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedNumber;
                        }
                        switch (ReturnType)
                        {
                            default:
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedNumber;
                            case DataTypes.eBoolean:
                                Number = Expr.InnerOperand.BooleanLiteralValue ? 1 : 0;
                                break;
                            case DataTypes.eInteger:
                                Number = Expr.InnerOperand.IntegerLiteralValue;
                                break;
                            case DataTypes.eFloat:
                                Number = Expr.InnerOperand.SingleLiteralValue;
                                break;
                            case DataTypes.eDouble:
                                Number = Expr.InnerOperand.DoubleLiteralValue;
                                break;
                        }
                    }
                    break;
            }

            NumberOut = Sign * Number;

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /*    7:   <lfo_definition>        ::= <lfo_elem> ; <lfo_definition> */
        /*    8:                           ::=  */
        /* FIRST SET: */
        /*  <lfo_definition>        : {oscillator, freqenvelope, modulation, */
        /*       ampenvelope, <lfo_elem>} */
        /* FOLLOW SET: */
        /*  <lfo_definition>        : {)} */
        public static BuildInstrErrors ParseLfoDefinition(
            LFOSpecRec LFO,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            else
            {
                Context.Scanner.UngetToken(Token);

                Error = ParseLfoElem(LFO, Context, out ErrorLine, RequiredOnceOnly);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                /* get semicolon */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }

                return ParseLfoDefinition(LFO, Context, out ErrorLine, RequiredOnceOnly);
            }
        }




        /* the identifier string returned from here is NOT trash-tracker allocated. */
        /*   78:   <identifier>            ::= <identifiertoken> */
        /*   79:                           ::= <stringtoken> */
        /* FIRST SET: */
        /*  <identifier>            : {<identifiertoken>, <stringtoken>} */
        /* FOLLOW SET: */
        /*  <identifier>            : {<integertoken>, <floattoken>, (, scale, */
        /*       <number>} */
        public static BuildInstrErrors ParseIdentifier(
            BuildInstrumentContext Context,
            out int ErrorLine,
            out string IdentifierOut)
        {
            TokenRec<InstrKeywordType> Token;

            IdentifierOut = null;
            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenIdentifier)
            {
                IdentifierOut = Token.GetTokenIdentifierString();
            }
            else if (Token.GetTokenType() == TokenTypes.eTokenString)
            {
                IdentifierOut = Token.GetTokenStringValue();
            }
            else
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedStringOrIdentifier;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /*   27:   <oscillator_definition> ::= <oscillator_elem> ; */
        /*       <oscillator_definition> */
        /*   28:                           ::=  */
        /* FIRST SET: */
        /*  <oscillator_definition> : {loudness, type, samplelist, */
        /*       freqmultiplier, freqdivisor, loudnessenvelope, */
        /*       loudnesslfo, indexenvelope, indexlfo, <oscillator_elem>} */
        /* FOLLOW SET: */
        /*  <oscillator_definition> : {)} */
        public static BuildInstrErrors ParseOscillatorDefinition(
            OscillatorRec Oscillator,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            else
            {
                Context.Scanner.UngetToken(Token);

                Error = ParseOscillatorElem(
                    Oscillator,
                    Context,
                    out ErrorLine,
                    RequiredOnceOnly);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                /* get semicolon */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }

                return ParseOscillatorDefinition(
                    Oscillator,
                    Context,
                    out ErrorLine,
                    RequiredOnceOnly);
            }
        }




        /*    9:   <lfo_elem>              ::= freqenvelope ( <envelope_definition> ) */
        /*   10:                           ::= ampenvelope ( <envelope_definition> ) */
        /*   11:                           ::= oscillator <oscillator_type> */
        /*   12:                           ::= modulation <modulation_type> */
        /*  XXX:                           ::= freqlfo ( <lfo_definition> ) */
        /*  XXX:                           ::= amplfo ( <lfo_definition> ) */
        /*  XXX:                           ::= lowpassfilter freqenvelope ( */
        /*                                     <envelope_definition> ) { freqlfo ( */
        /*                                     <lfo_definition> ) } */
        /*  XXX:                           ::= sampleandhold freqenvelope ( */
        /*                                     <envelope_definition> ) { freqlfo ( */
        /*                                     <lfo_definition> ) } */
        //  XXX:                           ::= interpolate { on | off }
#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
        /*  XXX:                           ::= loopenvelope ( <envelope_definition> ) */
#endif
        /* FIRST SET: */
        /*  <lfo_elem>              : {oscillator, freqenvelope, modulation, ampenvelope} */
        /* FOLLOW SET: */
        /*  <lfo_elem>              : {;} */
        public static BuildInstrErrors ParseLfoElem(
            LFOSpecRec LFO,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedLFOMember;
            }

            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedLFOMember;

                case InstrKeywordType.eKeywordFreqlfo:
                    {
                        LFOSpecRec LFOSpec;

                        LFOSpec = NewLFOSpecifier();
                        LFOListSpecAppendNewEntry(GetLFOSpecFrequencyLFOList(LFO), LFOSpec);

                        return ScanLfoSpec(LFOSpec, Context, out ErrorLine);
                    }

                case InstrKeywordType.eKeywordAmplfo:
                    {
                        LFOSpecRec LFOSpec;

                        LFOSpec = NewLFOSpecifier();
                        LFOListSpecAppendNewEntry(GetLFOSpecAmplitudeLFOList(LFO), LFOSpec);

                        return ScanLfoSpec(LFOSpec, Context, out ErrorLine);
                    }

                case InstrKeywordType.eKeywordFreqenvelope:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_FREQENVELOPE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOFreqEnvelope;
                    }

                    return ScanEnvelopeSpec(GetLFOSpecFrequencyEnvelope(LFO), Context, out ErrorLine);

                case InstrKeywordType.eKeywordAmpenvelope:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_REQUIREDONCEONLY_AMPENVELOPE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOAmpEnvelope;
                    }

                    return ScanEnvelopeSpec(GetLFOSpecAmplitudeEnvelope(LFO), Context, out ErrorLine);

                case InstrKeywordType.eKeywordOscillator:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_OSCILLATORTYPE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOOscillatorType;
                    }

                    /*  13:   <oscillator_type>       ::= constant */
                    /*  14:                           ::= signsine */
                    /*  15:                           ::= plussine */
                    /*  16:                           ::= signtriangle */
                    /*  17:                           ::= plustriangle */
                    /*  18:                           ::= signsquare <number> */
                    /*  19:                           ::= plussquare <number> */
                    /*  20:                           ::= signramp <number> */
                    /*  21:                           ::= plusramp <number> */
                    /*  22:                           ::= signlinfuzz <number> <seed> */
                    /*                                    (square | triangle) [incseed] */
                    /*  23:                           ::= pluslinfuzz <number> <seed> */
                    /*                                    (square | triangle) [incseed] */
                    /*  XX:                           ::= wavetable ( <samplelist_definition> */
                    /*                                    ) ( <envelope_definition> ) */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedLFOOscillatorType;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedLFOOscillatorType;

                        case InstrKeywordType.eKeywordConstant:
                            SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOConstant1);
                            break;

                        case InstrKeywordType.eKeywordSignsine:
                            SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOSignedSine);
                            break;

                        case InstrKeywordType.eKeywordPlussine:
                            SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOPositiveSine);
                            break;

                        case InstrKeywordType.eKeywordSigntriangle:
                            SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOSignedTriangle);
                            break;

                        case InstrKeywordType.eKeywordPlustriangle:
                            SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOPositiveTriangle);
                            break;

                        case InstrKeywordType.eKeywordSignsquare:
                            {
                                double Number;

                                SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOSignedSquare);

                                Error = ParseNumber(Context, out ErrorLine, out Number);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }
                                SetLFOSpecExtraValue(LFO, Number);
                            }
                            break;

                        case InstrKeywordType.eKeywordPlussquare:
                            {
                                double Number;

                                SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOPositiveSquare);

                                Error = ParseNumber(Context, out ErrorLine, out Number);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }
                                SetLFOSpecExtraValue(LFO, Number);
                            }
                            break;

                        case InstrKeywordType.eKeywordSignramp:
                            {
                                double Number;

                                SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOSignedRamp);

                                Error = ParseNumber(Context, out ErrorLine, out Number);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }
                                SetLFOSpecExtraValue(LFO, Number);
                            }
                            break;

                        case InstrKeywordType.eKeywordPlusramp:
                            {
                                double Number;

                                SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOPositiveRamp);

                                Error = ParseNumber(Context, out ErrorLine, out Number);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }
                                SetLFOSpecExtraValue(LFO, Number);
                            }
                            break;

                        case InstrKeywordType.eKeywordSignlinfuzz:
                            {
                                double Seed;

                                Error = ParseNumber(Context, out ErrorLine, out Seed);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }

                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrExpectedSquareOrTriangle;
                                }

                                switch (Token.GetTokenKeywordTag())
                                {
                                    default:
                                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                        return BuildInstrErrors.eBuildInstrExpectedSquareOrTriangle;
                                    case InstrKeywordType.eKeywordSquare:
                                        SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOSignedLinearFuzzSquare);
                                        break;
                                    case InstrKeywordType.eKeywordTriangle:
                                        SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOSignedLinearFuzzTriangle);
                                        break;
                                }
                                SetLFOSpecExtraValue(LFO, Seed);
                            }
                            break;

                        case InstrKeywordType.eKeywordPluslinfuzz:
                            {
                                double Seed;

                                Error = ParseNumber(Context, out ErrorLine, out Seed);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }

                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrExpectedSquareOrTriangle;
                                }

                                switch (Token.GetTokenKeywordTag())
                                {
                                    default:
                                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                        return BuildInstrErrors.eBuildInstrExpectedSquareOrTriangle;
                                    case InstrKeywordType.eKeywordSquare:
                                        SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOPositiveLinearFuzzSquare);
                                        break;
                                    case InstrKeywordType.eKeywordTriangle:
                                        SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOPositiveLinearFuzzTriangle);
                                        break;
                                }
                                SetLFOSpecExtraValue(LFO, Seed);
                            }
                            break;

                        case InstrKeywordType.eKeywordWavetable:
                            SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOWaveTable);

                            /* samplelist keyword */
                            Token = Context.Scanner.GetNextToken();
                            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordSamplelist))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedSamplelist;
                            }

                            /* open paren */
                            Token = Context.Scanner.GetNextToken();
                            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
                            }

                            Error = ParseSamplelistDefinition(GetLFOSpecSampleSelector(LFO), Context, out ErrorLine);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }

                            /* close paren */
                            Token = Context.Scanner.GetNextToken();
                            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
                            }

                            /* envelope keyword */
                            Token = Context.Scanner.GetNextToken();
                            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordEnvelope))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedEnvelope;
                            }

                            Error = ScanEnvelopeSpec(GetLFOSpecWaveTableIndexEnvelope(LFO), Context, out ErrorLine);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }

                        /* semicolon or indexlfo */
                        IndexLFOParsePoint:
                            Token = Context.Scanner.GetNextToken();
                            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                            {
                                Context.Scanner.UngetToken(Token);
                            }
                            else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordIndexlfo))
                            {
                                LFOSpecRec LFOSpec;

                                LFOSpec = NewLFOSpecifier();
                                LFOListSpecAppendNewEntry(GetLFOSpecWaveTableIndexLFOList(LFO), LFOSpec);

                                Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }

                                goto IndexLFOParsePoint;
                            }
                            else
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedIndexLFOOrSemicolon;
                            }
                            break;

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
                        case InstrKeywordType.eKeywordLoopenvelope:
                            SetLFOSpecOscillatorType(LFO, LFOOscTypes.eLFOLoopedEnvelope);
                            break;
#endif
                    }
                    break;

                case InstrKeywordType.eKeywordModulation:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_MODULATIONTYPE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOModulationType;
                    }

                    /*  24:   <modulation_type>       ::= additive */
                    /*  25:                           ::= multiplicative */
                    /*  26:                           ::= inversemult */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedLFOModulationType;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedLFOModulationType;
                        case InstrKeywordType.eKeywordAdditive:
                            SetLFOSpecModulationMode(LFO, LFOModulationTypes.eLFOAdditive);
                            break;
                        case InstrKeywordType.eKeywordMultiplicative:
                            SetLFOSpecModulationMode(LFO, LFOModulationTypes.eLFOMultiplicative);
                            break;
                        case InstrKeywordType.eKeywordInversemult:
                            SetLFOSpecModulationMode(LFO, LFOModulationTypes.eLFOInverseMultiplicative);
                            break;
                    }
                    break;

                case InstrKeywordType.eKeywordHalfsteps:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_ADDINGMODE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOAddingMode;
                    }

                    SetLFOSpecAddingMode(LFO, LFOAdderMode.eLFOHalfSteps);
                    break;

                case InstrKeywordType.eKeywordLinear:
                case InstrKeywordType.eKeywordHertz:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_ADDINGMODE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOAddingMode;
                    }

                    SetLFOSpecAddingMode(LFO, LFOAdderMode.eLFOArithmetic);
                    break;

                case InstrKeywordType.eKeywordExponential:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_ADDINGMODE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOAddingMode;
                    }

                    SetLFOSpecAddingMode(LFO, LFOAdderMode.eLFOGeometric);
                    break;

                case InstrKeywordType.eKeywordLowpassfilter:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_FILTER))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOFilter;
                    }

                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFreqenvelope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedFreqEnvelope;
                    }

                    Error = ScanEnvelopeSpec(GetLFOSpecFilterCutoffEnvelope(LFO),
                        Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                LFOFilterLFOPoint:
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                        && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordFreqlfo))
                    {
                        LFOSpecRec LFOSpec;

                        LFOSpec = NewLFOSpecifier();
                        LFOListSpecAppendNewEntry(GetLFOSpecFilterCutoffLFOList(LFO), LFOSpec);

                        Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }

                        goto LFOFilterLFOPoint;
                    }
                    else
                    {
                        /* semicolon */

                        if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedFreqLfoOrSemicolon;
                        }

                        Context.Scanner.UngetToken(Token);
                    }

                    LFOSpecFilterHasBeenSpecified(LFO);
                    break;

                case InstrKeywordType.eKeywordSampleandhold:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_SAMPLEHOLD))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOSampleHolds;
                    }

                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFreqenvelope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedFreqEnvelope;
                    }

                    Error = ScanEnvelopeSpec(GetLFOSpecSampleHoldFreqEnvelope(LFO), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                LFOSampleHoldLFOPoint:
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                        && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordFreqlfo))
                    {
                        LFOSpecRec LFOSpec;

                        LFOSpec = NewLFOSpecifier();
                        LFOListSpecAppendNewEntry(GetLFOSpecSampleHoldFreqLFOList(LFO), LFOSpec);

                        Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }

                        goto LFOSampleHoldLFOPoint;
                    }
                    else
                    {
                        /* semicolon */

                        if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedFreqLfoOrSemicolon;
                        }

                        Context.Scanner.UngetToken(Token);
                    }

                    LFOSpecSampleHoldHasBeenSpecified(LFO);
                    break;

#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
                case InstrKeywordType.eKeywordLoopenvelope:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_REQUIREDONCEONLY_LOOPENVELOPE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLFOLoopEnvelope;
                    }

                    return ScanEnvelopeSpec(GetLFOSpecLoopedEnvelope(LFO), Context, out ErrorLine);
#endif

                case InstrKeywordType.eKeywordInterpolate:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_INTERPOLATE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInterpolate;
                    }

                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOnOrOff;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedOnOrOff;
                        case InstrKeywordType.eKeywordOn:
                            LFOSpecSetEnableCrossWaveTableInterpolation(LFO, true);
                            break;
                        case InstrKeywordType.eKeywordOff:
                            LFOSpecSetEnableCrossWaveTableInterpolation(LFO, false);
                            break;
                    }

                    break;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /*   29:   <oscillator_elem>       ::= type <oscillator_type> */
        /*   30:                           ::= samplelist ( <samplelist_definition> ) */
        /*   32:                           ::= loudness <number> */
        /*   33:                           ::= freqmultiplier <number> */
        /*   34:                           ::= freqdivisor <integer> */
        /*   36:                           ::= loudnessenvelope ( <envelope_definition> ) */
        /*   37:                           ::= loudnesslfo ( <lfo_definition> ) */
        /*   38:                           ::= indexenvelope ( <envelope_definition> ) */
        /*   39:                           ::= indexlfo ( <lfo_definition> ) */
        /*   XXX:                          ::= stereobias <number> */
        /*   XXX:                          ::= displacement <number> */
        /*   XXX:                          ::= frequencylfo ( <lfo_definition> ) */
        /*   XXX:                          ::= effect <oscillator_effect> */
        /*   XXX:                          ::= fofsamprate <number> */
        /*   XXX:                          ::= fofcompress { overlap | discard } */
        /*   XXX:                          ::= fofexpand { silence | loop } */
        /*   XXX:                          ::= fofsamprateenvelope ( <envelope_definition> ) */
        /*   XXX:                          ::= fofsampratelfo ( <lfo_definition> ) */
        /*   XXX:                          ::= network ( ... ) */
        /*   XXX:                          ::= interpolate { on | off }*/
        /* FIRST SET: */
        /*  <oscillator_elem>       : {loudness, type, samplelist, */
        /*       freqmultiplier, freqdivisor, loudnessenvelope, */
        /*       loudnesslfo, indexenvelope, indexlfo} */
        /* FOLLOW SET: */
        /*  <oscillator_elem>       : {;} */
        public static BuildInstrErrors ParseOscillatorElem(
            OscillatorRec Oscillator,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOscillatorMember;
            }

            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedOscillatorMember;

                case InstrKeywordType.eKeywordType:
                    if (MarkOnceOnlyPresent(
                        RequiredOnceOnly,
                        Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_TYPE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscType;
                    }

                    /*  40:   <oscillator_type>       ::= sampled */
                    /*  41:                           ::= wavetable */
                    /*  XX:                           ::= fof */
                    /*  XX:                           ::= pulse */
                    /*  XX:                           ::= ramp */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOscType;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedOscType;
                        case InstrKeywordType.eKeywordSampled:
                            OscillatorSetTheType(Oscillator, OscillatorTypes.eOscillatorSampled);
                            break;
                        case InstrKeywordType.eKeywordWavetable:
                            OscillatorSetTheType(Oscillator, OscillatorTypes.eOscillatorWaveTable);
                            break;
                        case InstrKeywordType.eKeywordFof:
                            OscillatorSetTheType(Oscillator, OscillatorTypes.eOscillatorFOF);
                            break;
                        case InstrKeywordType.eKeywordPulse:
                            OscillatorSetTheType(Oscillator, OscillatorTypes.eOscillatorAlgorithm);
                            PutOscillatorAlgorithm(Oscillator, OscAlgoType.eOscAlgoPulse);
                            break;
                        case InstrKeywordType.eKeywordRamp:
                            OscillatorSetTheType(Oscillator, OscillatorTypes.eOscillatorAlgorithm);
                            PutOscillatorAlgorithm(Oscillator, OscAlgoType.eOscAlgoRamp);
                            break;
                        case InstrKeywordType.eKeywordPhasemodulation:
                            OscillatorSetTheType(Oscillator, OscillatorTypes.eOscillatorFMSynth);
                            break;
                    }
                    break;

                case InstrKeywordType.eKeywordSamplelist:
                    if (MarkOnceOnlyPresent(
                        RequiredOnceOnly,
                        Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_SAMPLELIST))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscSampleList;
                    }

                    /* open paren */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOpenParen;
                    }

                    Error = ParseSamplelistDefinition(OscillatorGetSampleIntervalList(Oscillator),
                        Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    /* close paren */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedCloseParen;
                    }
                    break;

                case InstrKeywordType.eKeywordLoudness:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_LOUDNESS))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscLoudness;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutOscillatorNewOutputLoudness(Oscillator, Number);
                    break;

                case InstrKeywordType.eKeywordFreqmultiplier:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQMULTIPLIER))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscFreqMultiplier;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutOscillatorNewFrequencyFactors(Oscillator, Number,
                        OscillatorGetFrequencyDivisor(Oscillator));
                    break;

                case InstrKeywordType.eKeywordFreqdivisor:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQDIVISOR))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscFreqDivisor;
                    }

                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedInteger;
                    }
                    PutOscillatorNewFrequencyFactors(Oscillator,
                        OscillatorGetFrequencyMultiplier(Oscillator), Token.GetTokenIntegerValue());
                    break;

                case InstrKeywordType.eKeywordFreqadder:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQADDER))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscFreqAdder;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutOscillatorFrequencyAdder(Oscillator, Number);
                    break;

                case InstrKeywordType.eKeywordLoudnessenvelope:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_LOUDNESSENVELOPE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscLoudnessEnvelope;
                    }

                    Error = ScanEnvelopeSpec(OscillatorGetLoudnessEnvelope(Oscillator), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    break;

                case InstrKeywordType.eKeywordLoudnesslfo:
                    {
                        LFOSpecRec LFO;

                        /* create the LFO */
                        LFO = NewLFOSpecifier();
                        LFOListSpecAppendNewEntry(OscillatorGetLoudnessLFOList(Oscillator), LFO);

                        Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                    }
                    break;

                case InstrKeywordType.eKeywordIndexenvelope:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_INDEXENVELOPE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscIndexEnvelope;
                    }

                    Error = ScanEnvelopeSpec(OscillatorGetExcitationEnvelope(Oscillator), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    break;

                case InstrKeywordType.eKeywordIndexlfo:
                    {
                        LFOSpecRec LFO;

                        /* create the LFO */
                        LFO = NewLFOSpecifier();
                        LFOListSpecAppendNewEntry(OscillatorGetExcitationLFOList(Oscillator), LFO);

                        Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                    }
                    break;

                case InstrKeywordType.eKeywordStereobias:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_STEREOBIAS))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscStereoBias;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    OscillatorPutStereoBias(Oscillator, Number);
                    break;

                case InstrKeywordType.eKeywordDisplacement:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_TIMEDISPLACEMENT))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscDisplacement;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    OscillatorPutTimeDisplacement(Oscillator, Number);
                    break;

                case InstrKeywordType.eKeywordSurroundbias:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_SURROUNDBIAS))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscSurroundBias;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    OscillatorPutSurroundBias(Oscillator, Number);
                    break;

                case InstrKeywordType.eKeywordFrequencylfo:
                    {
                        LFOSpecRec LFO;

                        /* allocate the LFO */
                        LFO = NewLFOSpecifier();
                        /* the default arithmetic mode for pitch LFO is halfsteps */
                        SetLFOSpecAddingMode(LFO, LFOAdderMode.eLFOHalfSteps);
                        LFOListSpecAppendNewEntry(GetOscillatorFrequencyLFOList(Oscillator), LFO);

                        Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                    }
                    break;

                case InstrKeywordType.eKeywordEffect:
                    Error = ParseOscillatorEffect(GetOscillatorEffectList(Oscillator), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    break;

                case InstrKeywordType.eKeywordFofsamprate:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFSAMPRATE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscFOFSampRate;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutOscillatorFOFSamplingRate(Oscillator, Number);
                    break;

                case InstrKeywordType.eKeywordFofcompress:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFCOMPRESS))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscFOFCompress;
                    }
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOverlapOrDiscard;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedOverlapOrDiscard;
                        case InstrKeywordType.eKeywordOverlap:
                            PutOscillatorFOFCompress(Oscillator, OscFOFCompressType.eOscFOFOverlap);
                            break;
                        case InstrKeywordType.eKeywordDiscard:
                            PutOscillatorFOFCompress(Oscillator, OscFOFCompressType.eOscFOFDiscard);
                            break;
                    }
                    break;

                case InstrKeywordType.eKeywordFofexpand:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFEXPAND))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOscFOFExpand;
                    }
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedSilenceOrLoop;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedSilenceOrLoop;
                        case InstrKeywordType.eKeywordSilence:
                            PutOscillatorFOFExpand(Oscillator, OscFOFExpandType.eOscFOFSilenceFill);
                            break;
                        case InstrKeywordType.eKeywordLoop:
                            PutOscillatorFOFExpand(Oscillator, OscFOFExpandType.eOscFOFRestart);
                            break;
                    }
                    break;

                case InstrKeywordType.eKeywordFofsamprateenvelope:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFENVELOPE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFOFSamplingRateEnvelope;
                    }

                    Error = ScanEnvelopeSpec(OscillatorGetFOFRateEnvelope(Oscillator), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    break;

                case InstrKeywordType.eKeywordFofsampratelfo:
                    {
                        LFOSpecRec LFO;

                        /* create the LFO */
                        LFO = NewLFOSpecifier();
                        LFOListSpecAppendNewEntry(OscillatorGetFOFRateLFOList(Oscillator), LFO);

                        Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                    }
                    break;

                case InstrKeywordType.eKeywordNetwork:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_NETWORK))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleNetworks;
                    }

                    Error = ParseFMSynthNetwork(
                        OscillatorGetFMSynthSpec(Oscillator),
                        Context,
                        out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    break;

                case InstrKeywordType.eKeywordInterpolate:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_INTERPOLATE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInterpolate;
                    }
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOnOrOff;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedOnOrOff;
                        case InstrKeywordType.eKeywordOn:
                            OscillatorSetEnableCrossWaveTableInterpolation(Oscillator, true);
                            break;
                        case InstrKeywordType.eKeywordOff:
                            OscillatorSetEnableCrossWaveTableInterpolation(Oscillator, false);
                            break;
                    }
                    break;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* FOLLOW SET */
        /* { ; } */
        private delegate BuildInstrErrors ParseOscillatorEffectMethod(
            EffectSpecListRec OscEffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag);
        public static BuildInstrErrors ParseOscillatorEffect(
            EffectSpecListRec OscEffectList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;

            bool EnabledFlag = true;


            // shunt for pluggable form (1)
            Token = Context.Scanner.GetNextToken();
        ParsePluggable:
            if (Token.GetTokenType() == TokenTypes.eTokenColon)
            {
                string classIdentifier;

                Token = Context.Scanner.GetNextToken();
                BuildInstrErrors Error = ParseGlobalIdentifierString(
                    Context,
                    Token,
                    out ErrorLine,
                    out classIdentifier);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    Context.ErrorExtraMessage = GetPluggableClassListForError();
                    return Error;
                }

                int startLine = Context.Scanner.GetCurrentLineNumber();

                PluggableOscSpec pluggable;
                Error = ParsePluggableOscProcessor(
                    Context,
                    null/*Oscillator*/,
                    out ErrorLine,
                    true/*enabled*/,
                    classIdentifier,
                    PluggableRole.Effect,
                    out pluggable);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                AddPluggableEffectToEffectSpecList(OscEffectList, pluggable, EnabledFlag);

                return BuildInstrErrors.eBuildInstrNoError;
            }
            Context.Scanner.UngetToken(Token);


            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOscillatorEffectOrDisabled;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedOscillatorEffectOrDisabled;
                case InstrKeywordType.eKeywordAnalyzer:
                case InstrKeywordType.eKeywordHistogram:
                case InstrKeywordType.eKeywordFilter:
                case InstrKeywordType.eKeywordNlproc:
                case InstrKeywordType.eKeywordDelayline:
                case InstrKeywordType.eKeywordResampler:
                case InstrKeywordType.eKeywordCompressor:
                case InstrKeywordType.eKeywordVocoder:
                case InstrKeywordType.eKeywordIdeallowpass:
                case InstrKeywordType.eKeywordConvolver:
                case InstrKeywordType.eKeywordUsereffect:
                    Context.Scanner.UngetToken(Token);
                    break;
                case InstrKeywordType.eKeywordDisabled:
                    EnabledFlag = false;
                    break;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenColon)
            {
                goto ParsePluggable; // shunt for pluggable form (2)
            }
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOscillatorEffect;
            }
            ParseOscillatorEffectMethod Parser;
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedOscillatorEffect;

                case InstrKeywordType.eKeywordAnalyzer:
                    Parser = ParseAnalyzer;
                    break;
                case InstrKeywordType.eKeywordHistogram:
                    Parser = ParseHistogram;
                    break;
                case InstrKeywordType.eKeywordDelayline: /* delayline ( <oscdelayelems> ) */
                    Parser = ParseOscDelayLine;
                    break;
                case InstrKeywordType.eKeywordNlproc:
                    Parser = ParseOscNLProc;
                    break;
                case InstrKeywordType.eKeywordFilter: /* filter ( <oscfilterelems> ) */
                    Parser = ParseOscFilterBank;
                    break;
                case InstrKeywordType.eKeywordResampler:
                    Parser = ParseResampler;
                    break;
                case InstrKeywordType.eKeywordCompressor:
                    Parser = ParseOscCompressor;
                    break;
                case InstrKeywordType.eKeywordVocoder:
                    Parser = ParseOscVocoder;
                    break;
                case InstrKeywordType.eKeywordIdeallowpass:
                    Parser = ParseIdealLowpass;
                    break;
                case InstrKeywordType.eKeywordUsereffect:
                    Parser = ParseOscUserEffect;
                    break;
            }
            return Parser(
                OscEffectList,
                Context,
                out ErrorLine,
                EnabledFlag);
        }




        /*   42:   <envelope_definition>   ::= <envelope_elem> ; <envelope_definition> */
        /*   43:                           ::=  */
        /* FIRST SET: */
        /*  <envelope_definition>   : {totalscaling, points, <envelope_elem>} */
        /* FOLLOW SET: */
        /*  <envelope_definition>   : {)} */
        public static BuildInstrErrors ParseEnvelopeDefinition(
            EnvelopeRec Envelope,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            else
            {
                Context.Scanner.UngetToken(Token);

                Error = ParseEnvelopeElem(Envelope, Context, out ErrorLine, RequiredOnceOnly);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                /* get semicolon */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }

                return ParseEnvelopeDefinition(Envelope, Context, out ErrorLine, RequiredOnceOnly);
            }
        }




        /*   68:   <samplelist_definition> ::= <samplelist_elem> ; */
        /*       <samplelist_definition> */
        /*   69:                           ::=  */
        /* FIRST SET: */
        /*  <samplelist_definition> : {<identifiertoken>, <stringtoken>, */
        /*       <identifier>, <samplelist_elem>} */
        /* FOLLOW SET: */
        /*  <samplelist_definition> : {)} */
        public static BuildInstrErrors ParseSamplelistDefinition(
            SampleSelectorRec SampleList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            else
            {
                Context.Scanner.UngetToken(Token);

                Error = ParseSamplelistElem(SampleList, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                /* get semicolon */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }

                return ParseSamplelistDefinition(SampleList, Context, out ErrorLine);
            }
        }




        /*   45:   <envelope_elem>         ::= totalscaling <number> */
        /*   49:                           ::= points ( <env_point_list> ) */
        /*   XX:                           ::= pitchcontrol slope <number> center <number> */
        /*   XX:                           ::= formula "function" */
        /* FIRST SET: */
        /*  <envelope_elem>         : {totalscaling, points} */
        /* FOLLOW SET: */
        /*  <envelope_elem>         : {;} */
        public static BuildInstrErrors ParseEnvelopeElem(
            EnvelopeRec Envelope,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedEnvelopeMember;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedEnvelopeMember;

                case InstrKeywordType.eKeywordTotalscaling:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVELOPEDEFINITION_ONCEONLY_TOTALSCALING))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvTotalScaling;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetOverallAmplitude(Envelope, Number);
                    break;

                case InstrKeywordType.eKeywordPoints:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVELOPEDEFINITION_ONCEONLY_POINTS))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPoints;
                    }

                    /* open paren */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOpenParen;
                    }

                    Error = ParseEnvPointList(Envelope, Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    /* close paren */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedCloseParen;
                    }
                    break;

                case InstrKeywordType.eKeywordPitchcontrol:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVELOPEDEFINITION_ONCEONLY_PITCHCONTROL))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultiplePitchControl;
                    }

                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordSlope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedSlope;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutGlobalEnvelopePitchRateRolloff(Envelope, Number);

                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordCenter))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedCenter;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutGlobalEnvelopePitchRateNormalization(Envelope, Number);
                    break;

                case InstrKeywordType.eKeywordFormula:
                    {
                        CompileErrors CompileError;
                        int CompileErrorLine;
                        DataTypes ReturnType;
                        PcodeRec FuncCode;
                        Compiler.ASTExpression Expr;

                        if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVELOPEDEFINITION_ONCEONLY_FORMULA))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrMultipleEnvFormula;
                        }

                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() != TokenTypes.eTokenString)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedQuotedFormula;
                        }

                        CompileError = Compiler.CompileSpecialFunction(
                            Context.CodeCenter,
                            EnvelopeFormulaArgsDefs,
                            out CompileErrorLine,
                            out ReturnType,
                            Token.GetTokenStringValue(),
                            false/*suppressCILEmission -- this code path always generates function*/,
                            out FuncCode,
                            out Expr);
                        if (CompileError != CompileErrors.eCompileNoError)
                        {
                            ErrorLine = CompileErrorLine + Context.Scanner.GetCurrentLineNumber() - 1;
                            return BuildInstrErrors.eBuildInstrEnvFormulaSyntaxError;
                        }
                        if (ReturnType != DataTypes.eDouble)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrEnvFormulaMustHaveTypeDouble;
                        }
                        EnvelopeSetFormula(Envelope, FuncCode);
                    }
                    break;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /*   70:   <samplelist_elem>       ::= <identifier> <number> */
        /* FIRST SET: */
        /*  <samplelist_elem>       : {<identifiertoken>, <stringtoken>, <identifier>} */
        /* FOLLOW SET: */
        /*  <samplelist_elem>       : {;} */
        public static BuildInstrErrors ParseSamplelistElem(
            SampleSelectorRec SampleList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            BuildInstrErrors Error;
            string SampleName;
            double Number;

            Error = ParseIdentifier(Context, out ErrorLine, out SampleName);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Error = ParseNumber(Context, out ErrorLine, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            AppendSampleSelector(
                SampleList,
                Number,
                SampleName);

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /*   50:   <env_point_list>        ::= <env_point_elem> ; <env_point_list> */
        /*   51:                           ::=  */
        /* FIRST SET: */
        /*  <env_point_list>        : {<integertoken>, <floattoken>, <number>, */
        /*       <env_point_elem>} */
        /* FOLLOW SET: */
        /*  <env_point_list>        : {)} */
        public static BuildInstrErrors ParseEnvPointList(
            EnvelopeRec Envelope,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            else
            {
                Context.Scanner.UngetToken(Token);

                Error = ParseEnvPointElem(Envelope, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                /* get semicolon */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }

                return ParseEnvPointList(Envelope, Context, out ErrorLine);
            }
        }




        /*   52:   <env_point_elem>        ::= delay <number> level <number> <env_attributes> */
        /*   XX:                           ::= origin */
        /* FIRST SET: */
        /*  <env_point_elem>        : {delay} */
        /* FOLLOW SET: */
        /*  <env_point_elem>        : {<integertoken>, <floattoken>, ), <number>, */
        /*       <env_point_list>, <env_point_elem>} */
        public static BuildInstrErrors ParseEnvPointElem(
            EnvelopeRec Envelope,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDelayOrOrigin;
            }

            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedDelayOrOrigin;

                case InstrKeywordType.eKeywordDelay:

                    double Delay = 0;
                    PcodeRec DelayFunction = null;
                    double Level = 0;
                    PcodeRec LevelFunction = null;

                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenString)
                    {
                        Context.Scanner.UngetToken(Token);
                        Error = ParseNumber(Context, out ErrorLine, out Delay);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                    }
                    else
                    {
                        int CompileErrorLine;
                        DataTypes ReturnType;
                        Compiler.ASTExpression Expr;

                        CompileErrors CompileError = Compiler.CompileSpecialFunction(
                            Context.CodeCenter,
                            EnvelopeInitFormulaArgsDefs,
                            out CompileErrorLine,
                            out ReturnType,
                            Token.GetTokenStringValue(),
                            false/*suppressCILEmission -- this code path always generates function*/,
                            out DelayFunction,
                            out Expr);
                        if (CompileError != CompileErrors.eCompileNoError)
                        {
                            ErrorLine = CompileErrorLine + Context.Scanner.GetCurrentLineNumber() - 1;
                            return BuildInstrErrors.eBuildInstrEnvFormulaSyntaxError;
                        }
                        if (ReturnType != DataTypes.eDouble)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrEnvFormulaMustHaveTypeDouble;
                        }

                        if (Compiler.ExprKind.eExprOperand == Expr.Kind)
                        {
                            // expression evaluates at compile time to a constant
                            switch (ReturnType)
                            {
                                default:
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrEnvFormulaMustHaveTypeDouble;
                                case DataTypes.eInteger:
                                    Delay = Expr.InnerOperand.IntegerLiteralValue;
                                    break;
                                case DataTypes.eFloat:
                                    Delay = Expr.InnerOperand.SingleLiteralValue;
                                    break;
                                case DataTypes.eDouble:
                                    Delay = Expr.InnerOperand.DoubleLiteralValue;
                                    break;
                            }
                            DelayFunction = null; // force use constant
                        }
                        else
                        {
                            // expression must be evaluated at performance time
                        }
                    }

                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || ((Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordLevel)
                            && (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordScale)))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedLevelOrScale;
                    }
                    InstrKeywordType levelOrScale = Token.GetTokenKeywordTag();

                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenString)
                    {
                        Context.Scanner.UngetToken(Token);
                        Error = ParseNumber(Context, out ErrorLine, out Level);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                    }
                    else
                    {
                        int CompileErrorLine;
                        DataTypes ReturnType;
                        Compiler.ASTExpression Expr;

                        CompileErrors CompileError = Compiler.CompileSpecialFunction(
                            Context.CodeCenter,
                            EnvelopeInitFormulaArgsDefs,
                            out CompileErrorLine,
                            out ReturnType,
                            Token.GetTokenStringValue(),
                            false/*suppressCILEmission -- this code path always generates function*/,
                            out LevelFunction,
                            out Expr);
                        if (CompileError != CompileErrors.eCompileNoError)
                        {
                            ErrorLine = CompileErrorLine + Context.Scanner.GetCurrentLineNumber() - 1;
                            return BuildInstrErrors.eBuildInstrEnvFormulaSyntaxError;
                        }
                        if (ReturnType != DataTypes.eDouble)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrEnvFormulaMustHaveTypeDouble;
                        }

                        if (Compiler.ExprKind.eExprOperand == Expr.Kind)
                        {
                            // expression evaluates at compile time to a constant
                            switch (ReturnType)
                            {
                                default:
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrEnvFormulaMustHaveTypeDouble;
                                case DataTypes.eInteger:
                                    Level = Expr.InnerOperand.IntegerLiteralValue;
                                    break;
                                case DataTypes.eFloat:
                                    Level = Expr.InnerOperand.SingleLiteralValue;
                                    break;
                                case DataTypes.eDouble:
                                    Level = Expr.InnerOperand.DoubleLiteralValue;
                                    break;
                            }
                            LevelFunction = null; // force use constant
                        }
                        else
                        {
                            // expression must be evaluated at performance time
                        }
                    }

                    EnvelopeInsertPhase(Envelope, GetEnvelopeNumFrames(Envelope));
                    if (levelOrScale == InstrKeywordType.eKeywordScale)
                    {
                        EnvelopeSetPhaseTargetType(Envelope, GetEnvelopeNumFrames(Envelope) - 1, EnvTargetTypes.eEnvelopeTargetScaling);
                    }
                    else
                    {
                        EnvelopeSetPhaseTargetType(Envelope, GetEnvelopeNumFrames(Envelope) - 1, EnvTargetTypes.eEnvelopeTargetAbsolute);
                    }
                    EnvelopeSetPhaseDuration(Envelope, GetEnvelopeNumFrames(Envelope) - 1, Delay);
                    EnvelopeSetPhaseDurationFormula(Envelope, GetEnvelopeNumFrames(Envelope) - 1, DelayFunction);
                    EnvelopeSetPhaseFinalValue(Envelope, GetEnvelopeNumFrames(Envelope) - 1, Level);
                    EnvelopeSetPhaseFinalValueFormula(Envelope, GetEnvelopeNumFrames(Envelope) - 1, LevelFunction);

                    return ScanEnvAttributes(Envelope, Context, out ErrorLine);

                case InstrKeywordType.eKeywordOrigin:
                    EnvelopeSetOrigin(Envelope, GetEnvelopeNumFrames(Envelope));
                    return BuildInstrErrors.eBuildInstrNoError;
            }
        }




        /*   50:   <env_attributes>        ::= <env_one_attribute> <env_attributes> */
        /*   51:                           ::=  */
        /* FIRST SET: */
        /*  <env_attributes>        : {exponential, linear, sustainpoint, */
        /*       ampaccent1, ampaccent2, ampaccent3, ampaccent4, ampfreq, rateaccent1, */
        /*       rateaccent2, rateaccent3, rateaccent4, ratefreq, <env_one_attribute>} */
        /* FOLLOW SET: */
        /*  <env_attributes>        : {;} */
        public static BuildInstrErrors ParseEnvAttributes(
            EnvelopeRec Envelope,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            else
            {
                Context.Scanner.UngetToken(Token);

                Error = ParseEnvOneAttribute(
                    Envelope,
                    Context,
                    out ErrorLine,
                    RequiredOnceOnly);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                return ParseEnvAttributes(
                    Envelope,
                    Context,
                    out ErrorLine,
                    RequiredOnceOnly);
            }
        }




        /*   52:   <env_one_attribute>     ::= sustainpoint <integertoken> */
        /*   53:                           ::= ampaccent1 <number> */
        /*   54:                           ::= ampaccent2 <number> */
        /*   55:                           ::= ampaccent3 <number> */
        /*   56:                           ::= ampaccent4 <number> */
        /*   XX:                           ::= ampaccent5 <number> */
        /*   XX:                           ::= ampaccent6 <number> */
        /*   XX:                           ::= ampaccent7 <number> */
        /*   XX:                           ::= ampaccent8 <number> */
        /*   57:                           ::= ampfreq <number> <number> */
        /*   58:                           ::= rateaccent1 <number> */
        /*   59:                           ::= rateaccent2 <number> */
        /*   60:                           ::= rateaccent3 <number> */
        /*   61:                           ::= rateaccent4 <number> */
        /*   XX:                           ::= rateaccent5 <number> */
        /*   XX:                           ::= rateaccent6 <number> */
        /*   XX:                           ::= rateaccent7 <number> */
        /*   XX:                           ::= rateaccent8 <number> */
        /*   62:                           ::= ratefreq <number> <number> */
        /*   63:                           ::= exponential */
        /*   64:                           ::= linear */
        /* FIRST SET: */
        /*  <env_one_attribute>     : {exponential, linear, sustainpoint, */
        /*       ampaccent1, ampaccent2, ampaccent3, ampaccent4, ampfreq, */
        /*       rateaccent1, rateaccent2, rateaccent3, rateaccent4, ratefreq} */
        /* FOLLOW SET: */
        /*  <env_one_attribute>     : {;, exponential, linear, */
        /*       sustainpoint, ampaccent1, ampaccent2, ampaccent3, ampaccent4, */
        /*       ampfreq, rateaccent1, rateaccent2, rateaccent3, rateaccent4, */
        /*       ratefreq, <env_attributes>, <env_one_attribute>} */
        public static BuildInstrErrors ParseEnvOneAttribute(
            EnvelopeRec Envelope,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedEnvPointMember;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedEnvPointMember;

                case InstrKeywordType.eKeywordSustainpoint:
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedInteger;
                    }
                    switch (Token.GetTokenIntegerValue())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedIntBetween1And3;
                        case 1:
                            if (GetEnvelopeReleasePoint1(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint1(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeSustainPointSkip);
                            break;
                        case 2:
                            if (GetEnvelopeReleasePoint2(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint2(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeSustainPointSkip);
                            break;
                        case 3:
                            if (GetEnvelopeReleasePoint3(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint3(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeSustainPointSkip);
                            break;
                    }
                    break;

                case InstrKeywordType.eKeywordReleasepoint:
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedInteger;
                    }
                    switch (Token.GetTokenIntegerValue())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedIntBetween1And3;
                        case 1:
                            if (GetEnvelopeReleasePoint1(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint1(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeReleasePointSkip);
                            break;
                        case 2:
                            if (GetEnvelopeReleasePoint2(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint2(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeReleasePointSkip);
                            break;
                        case 3:
                            if (GetEnvelopeReleasePoint3(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint3(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeReleasePointSkip);
                            break;
                    }
                    break;

                case InstrKeywordType.eKeywordSustainpointnoskip:
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedInteger;
                    }
                    switch (Token.GetTokenIntegerValue())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedIntBetween1And3;
                        case 1:
                            if (GetEnvelopeReleasePoint1(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint1(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeSustainPointNoSkip);
                            break;
                        case 2:
                            if (GetEnvelopeReleasePoint2(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint2(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeSustainPointNoSkip);
                            break;
                        case 3:
                            if (GetEnvelopeReleasePoint3(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint3(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeSustainPointNoSkip);
                            break;
                    }
                    break;

                case InstrKeywordType.eKeywordReleasepointnoskip:
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedInteger;
                    }
                    switch (Token.GetTokenIntegerValue())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedIntBetween1And3;
                        case 1:
                            if (GetEnvelopeReleasePoint1(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint1(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeReleasePointNoSkip);
                            break;
                        case 2:
                            if (GetEnvelopeReleasePoint2(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint2(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeReleasePointNoSkip);
                            break;
                        case 3:
                            if (GetEnvelopeReleasePoint3(Envelope) != -1)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvSustainPointAlreadyDefined;
                            }
                            EnvelopeSetReleasePoint3(Envelope, GetEnvelopeNumFrames(Envelope) - 1, SustainTypes.eEnvelopeReleasePointNoSkip);
                            break;
                    }
                    break;

                case InstrKeywordType.eKeywordAmpaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent1;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentAmp(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 1);
                    break;

                case InstrKeywordType.eKeywordAmpaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent2;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentAmp(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 2);
                    break;

                case InstrKeywordType.eKeywordAmpaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent3;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentAmp(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 3);
                    break;

                case InstrKeywordType.eKeywordAmpaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent4;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentAmp(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 4);
                    break;

                case InstrKeywordType.eKeywordAmpaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent5;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentAmp(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 5);
                    break;

                case InstrKeywordType.eKeywordAmpaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent6;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentAmp(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 6);
                    break;

                case InstrKeywordType.eKeywordAmpaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent7;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentAmp(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 7);
                    break;

                case InstrKeywordType.eKeywordAmpaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointAmpAccent8;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentAmp(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 8);
                    break;

                case InstrKeywordType.eKeywordAmpfreq:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPFREQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointAmpFreq;
                    }

                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordSlope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedSlope;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetFreqAmpRolloff(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1);

                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordCenter))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedCenter;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetFreqAmpNormalization(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1);
                    break;

                case InstrKeywordType.eKeywordRateaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent1;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentRate(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 1);
                    break;

                case InstrKeywordType.eKeywordRateaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent2;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentRate(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 2);
                    break;

                case InstrKeywordType.eKeywordRateaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent3;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentRate(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 3);
                    break;

                case InstrKeywordType.eKeywordRateaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent4;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentRate(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 4);
                    break;

                case InstrKeywordType.eKeywordRateaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent5;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentRate(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 5);
                    break;

                case InstrKeywordType.eKeywordRateaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent6;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentRate(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 6);
                    break;

                case InstrKeywordType.eKeywordRateaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent7;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentRate(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 7);
                    break;

                case InstrKeywordType.eKeywordRateaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointRateAccent8;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetAccentRate(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1, 8);
                    break;

                case InstrKeywordType.eKeywordRatefreq:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEFREQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointRateFreq;
                    }

                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordSlope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedSlope;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetFreqRateRolloff(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1);

                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordCenter))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedCenter;
                    }

                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    EnvelopeSetFreqRateNormalization(Envelope, Number, GetEnvelopeNumFrames(Envelope) - 1);
                    break;

                case InstrKeywordType.eKeywordExponential:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_CURVE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointCurveSpec;
                    }

                    EnvelopeSetPhaseTransitionType(Envelope, GetEnvelopeNumFrames(Envelope) - 1, EnvTransTypes.eEnvelopeLinearInDecibels);
                    break;

                case InstrKeywordType.eKeywordLinear:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_CURVE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleEnvPointCurveSpec;
                    }

                    EnvelopeSetPhaseTransitionType(Envelope, GetEnvelopeNumFrames(Envelope) - 1, EnvTransTypes.eEnvelopeLinearInAmplitude);
                    break;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /*    1:                   ::= [disabled] delayline ( <delayelemlist> )  */
        /*  XXX:                   ::= [disabled] nlproc wavetable <string> inputscaling <number> */
        /*                             outputscaling <number> wavetableindex <number> */
        /*                             <nlattributes> */
        /*  XXX:                   ::= [disabled] filter ( <filterstuff> ) */
        /*  XXX:                   ::= [disabled] analyzer <string> */
        /*  XXX:                   ::= [disabled] resampler <resamplerstuff> */
        /*  XXX:                   ::= [disabled] compressor <compressor> */
        /*  XXX:                   ::= [disabled] vocoder <vocoder> */
        /*  XXX:                   ::= [disabled] ideallowpass <ideallowpass> */
        /*  XXX:                   ::= [disabled] autoquiescence <autoquiescence> */
        /*  XXX:                   ::= [disabled] usereffect <usereffect> */
        /* FOLLOW SET */
        /* { ; } */
        private delegate BuildInstrErrors ParseTrackEffectMethod(
            EffectSpecListRec OscEffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag);
        public static BuildInstrErrors ParseTrackEffect(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            InstrKeywordType EffectListType)
        {
            TokenRec<InstrKeywordType> Token;

            ErrorLine = -1;

            bool EnabledFlag = true;


            // shunt for pluggable form (1)
            Token = Context.Scanner.GetNextToken();
        ParsePluggable:
            if (Token.GetTokenType() == TokenTypes.eTokenColon)
            {
                string classIdentifier;

                Token = Context.Scanner.GetNextToken();
                BuildInstrErrors Error = ParseGlobalIdentifierString(
                    Context,
                    Token,
                    out ErrorLine,
                    out classIdentifier);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    Context.ErrorExtraMessage = GetPluggableClassListForError();
                    return Error;
                }

                int startLine = Context.Scanner.GetCurrentLineNumber();

                PluggableTrackSpec pluggable;
                Error = ParsePluggableTrackProcessor(
                    Context,
                    out ErrorLine,
                    true/*enabled*/,
                    classIdentifier,
                    out pluggable);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                AddPluggableEffectToEffectSpecList(EffectList, pluggable, EnabledFlag);

                return BuildInstrErrors.eBuildInstrNoError;
            }
            Context.Scanner.UngetToken(Token);


            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedEffectNameOrDisabled;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedEffectNameOrDisabled;

                case InstrKeywordType.eKeywordAnalyzer:
                case InstrKeywordType.eKeywordHistogram:
                case InstrKeywordType.eKeywordFilter:
                case InstrKeywordType.eKeywordNlproc:
                case InstrKeywordType.eKeywordDelayline:
                case InstrKeywordType.eKeywordResampler:
                case InstrKeywordType.eKeywordCompressor:
                case InstrKeywordType.eKeywordVocoder:
                case InstrKeywordType.eKeywordIdeallowpass:
                case InstrKeywordType.eKeywordConvolver:
                case InstrKeywordType.eKeywordUsereffect:
                    Context.Scanner.UngetToken(Token);
                    break;

                case InstrKeywordType.eKeywordDisabled:
                    EnabledFlag = false;
                    break;

                case InstrKeywordType.eKeywordAutoquiescence:
                    return ParseAutoQuiescence(
                        EffectList,
                        Context,
                        out ErrorLine);

                case InstrKeywordType.eKeywordSuppressinitialsilence:
                    if (EffectListType != InstrKeywordType.eKeywordScoreeffect)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrOnlyAllowedInScoreEffects;
                    }
                    EffectSpecListPutSuppressInitialSilence(
                        EffectList,
                        true);
                    return BuildInstrErrors.eBuildInstrNoError;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenColon)
            {
                goto ParsePluggable; // shunt for pluggable form (2)
            }
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedEffectName;
            }
            ParseTrackEffectMethod Parser;
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedEffectName;

                case InstrKeywordType.eKeywordAnalyzer:
                    Parser = ParseAnalyzer;
                    break;
                case InstrKeywordType.eKeywordHistogram:
                    Parser = ParseHistogram;
                    break;
                case InstrKeywordType.eKeywordFilter:
                    Parser = ParseFilterBank;
                    break;
                case InstrKeywordType.eKeywordNlproc:
                    Parser = ParseNLProc;
                    break;
                case InstrKeywordType.eKeywordDelayline:
                    Parser = ParseDelayLine;
                    break;
                case InstrKeywordType.eKeywordResampler:
                    Parser = ParseResampler;
                    break;
                case InstrKeywordType.eKeywordCompressor:
                    Parser = ParseCompressor;
                    break;
                case InstrKeywordType.eKeywordVocoder:
                    Parser = ParseVocoder;
                    break;
                case InstrKeywordType.eKeywordIdeallowpass:
                    Parser = ParseIdealLowpass;
                    break;
                case InstrKeywordType.eKeywordConvolver:
                    Parser = ParseConvolver;
                    break;
                case InstrKeywordType.eKeywordUsereffect:
                    Parser = ParseTrackUserEffect;
                    break;
            }
            return Parser(
                EffectList,
                Context,
                out ErrorLine,
                EnabledFlag);
        }




        /*    2:   <delayelemlist> ::= <delayelem> ; <delayelemlist>  */
        /*    3:                   ::=   */
        /* FIRST SET */
        /*  <delayelemlist> : {tap, <delayelem>} */
        /* FOLLOW SET */
        /*  <delayelemlist> : {)} */
        public static BuildInstrErrors ParseDelayElemList(
            DelayEffectRec DelayEffect,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }

            Context.Scanner.UngetToken(Token);
            Error = ParseDelayElem(DelayEffect, Context, out ErrorLine, RequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedSemicolon;
            }

            return ParseDelayElemList(DelayEffect, Context, out ErrorLine, RequiredOnceOnly);
        }




        /*    6:                   ::= tap <tapsource> <number> [interpolate] to <tapsource> */
        /*                             <number> scale <number> <tapattributes> */
        /*  XXX:                   ::= maxdelaytime <number> */
        public static BuildInstrErrors ParseDelayElem(
            DelayEffectRec DelayEffect,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDelayLineElem;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedDelayLineElem;

                case InstrKeywordType.eKeywordMaxdelaytime: /* maxdelaytime <number> */
                    {
                        if (MarkOnceOnlyPresent(RequiredOnceOnly,
                            Req.DELAYEFFECT_REQUIREDONCEONLY_MAXDELAYTIME))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrMultipleMaxDelayTime;
                        }
                        PcodeRec Formula;
                        Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                        SetDelayMaxTime(DelayEffect, Number);
                        SetDelayMaxTimeFormula(DelayEffect, Formula);
                    }
                    break;

                case InstrKeywordType.eKeywordTap:
                    {
                        DelayTapRec Tap;
                        PcodeRec Formula;

                        /* tap <tapsource> <number> [interpolate] to <tapsource> <number> scale */
                        /* <number> <tapattributes>  */
                        /* --- <tapsource> == {left, right, mono} */
                        Tap = NewDelayTap();
                        AppendTapToDelayEffect(DelayEffect, Tap);
                        /* do source */
                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedTapChannel;
                        }
                        switch (Token.GetTokenKeywordTag())
                        {
                            default:
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedTapChannel;
                            case InstrKeywordType.eKeywordLeft:
                                SetDelayTapSource(Tap, DelayChannelType.eTapLeftChannel);
                                break;
                            case InstrKeywordType.eKeywordRight:
                                SetDelayTapSource(Tap, DelayChannelType.eTapRightChannel);
                                break;
                            case InstrKeywordType.eKeywordMono:
                                SetDelayTapSource(Tap, DelayChannelType.eTapMonoChannel);
                                break;
                        }
                        /* get source delay time */
                        Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                        SetDelayTapSourceTime(Tap, Number, Formula);
                        /* to or interpolate */
                        Token = Context.Scanner.GetNextToken();
                        if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                            && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordInterpolate))
                        {
                            SetDelayTapInterpolateFlag(Tap, true);
                            Token = Context.Scanner.GetNextToken();
                        }
                        if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                            || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordTo))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedToOrInterpolate;
                        }
                        /* tap target */
                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedTapChannel;
                        }
                        switch (Token.GetTokenKeywordTag())
                        {
                            default:
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedTapChannel;
                            case InstrKeywordType.eKeywordLeft:
                                SetDelayTapTarget(Tap, DelayChannelType.eTapLeftChannel);
                                break;
                            case InstrKeywordType.eKeywordRight:
                                SetDelayTapTarget(Tap, DelayChannelType.eTapRightChannel);
                                break;
                            case InstrKeywordType.eKeywordMono:
                                SetDelayTapTarget(Tap, DelayChannelType.eTapMonoChannel);
                                break;
                        }
                        /* get target delay time */
                        Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                        SetDelayTapTargetTime(Tap, Number, Formula);
                        /* eat "scale" */
                        Token = Context.Scanner.GetNextToken();
                        if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                            || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordScale))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedScale;
                        }
                        /* get scaling number */
                        Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                        SetDelayTapScale(Tap, Number, Formula);
                        /* do attributes */
                        Error = ScanTapAttributes(Tap, Context, out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                    }
                    break;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /*   13:   <tapattributes> ::= <tapattr> <tapattributes> */
        /*   26:                   ::=  */
        /* FOLLOW SET */
        /*  <tapattributes> : {;} */
        public static BuildInstrErrors ParseTapAttributes(
            DelayTapRec Tap,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            Context.Scanner.UngetToken(Token);

            Error = ParseTapAttr(Tap, Context, out ErrorLine, RequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseTapAttributes(Tap, Context, out ErrorLine, RequiredOnceOnly);
        }




        /*  14:                   ::= sourceaccent1 <number> */
        /*  15:                   ::= sourceaccent2 <number> */
        /*  16:                   ::= sourceaccent3 <number> */
        /*  17:                   ::= sourceaccent4 <number> */
        /*  XX:                   ::= sourceaccent5 <number> */
        /*  XX:                   ::= sourceaccent6 <number> */
        /*  XX:                   ::= sourceaccent7 <number> */
        /*  XX:                   ::= sourceaccent8 <number> */
        /*  18:                   ::= targetaccent1 <number> */
        /*  19:                   ::= targetaccent2 <number> */
        /*  20:                   ::= targetaccent3 <number> */
        /*  21:                   ::= targetaccent4 <number> */
        /*  XX:                   ::= targetaccent5 <number> */
        /*  XX:                   ::= targetaccent6 <number> */
        /*  XX:                   ::= targetaccent7 <number> */
        /*  xx:                   ::= targetaccent8 <number> */
        /*  22:                   ::= scaleaccent1 <number> */
        /*  23:                   ::= scaleaccent2 <number> */
        /*  24:                   ::= scaleaccent3 <number> */
        /*  25:                   ::= scaleaccent4 <number> */
        /*  XX:                   ::= scaleaccent5 <number> */
        /*  XX:                   ::= scaleaccent6 <number> */
        /*  XX:                   ::= scaleaccent7 <number> */
        /*  XX:                   ::= scaleaccent8 <number> */
        /*   X:                   ::= lowpass freq <number> */
        /*   X:                   ::= freqaccent1 <number> */
        /*   X:                   ::= freqaccent2 <number> */
        /*   X:                   ::= freqaccent3 <number> */
        /*   X:                   ::= freqaccent4 <number> */
        /*   X:                   ::= freqaccent5 <number> */
        /*   X:                   ::= freqaccent6 <number> */
        /*   X:                   ::= freqaccent7 <number> */
        /*   X:                   ::= freqaccent8 <number> */
        public static BuildInstrErrors ParseTapAttr(
            DelayTapRec Tap,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;
            PcodeRec Formula;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedTapAttr;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedTapAttr;
                case InstrKeywordType.eKeywordSourceaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSourceAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapSourceTimeAccent(Tap, Number, 1);
                    break;
                case InstrKeywordType.eKeywordSourceaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSourceAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapSourceTimeAccent(Tap, Number, 2);
                    break;
                case InstrKeywordType.eKeywordSourceaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSourceAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapSourceTimeAccent(Tap, Number, 3);
                    break;
                case InstrKeywordType.eKeywordSourceaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSourceAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapSourceTimeAccent(Tap, Number, 4);
                    break;
                case InstrKeywordType.eKeywordSourceaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSourceAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapSourceTimeAccent(Tap, Number, 5);
                    break;
                case InstrKeywordType.eKeywordSourceaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSourceAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapSourceTimeAccent(Tap, Number, 6);
                    break;
                case InstrKeywordType.eKeywordSourceaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSourceAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapSourceTimeAccent(Tap, Number, 7);
                    break;
                case InstrKeywordType.eKeywordSourceaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSourceAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapSourceTimeAccent(Tap, Number, 8);
                    break;
                case InstrKeywordType.eKeywordTargetaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleTargetAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapTargetTimeAccent(Tap, Number, 1);
                    break;
                case InstrKeywordType.eKeywordTargetaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleTargetAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapTargetTimeAccent(Tap, Number, 2);
                    break;
                case InstrKeywordType.eKeywordTargetaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleTargetAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapTargetTimeAccent(Tap, Number, 3);
                    break;
                case InstrKeywordType.eKeywordTargetaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleTargetAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapTargetTimeAccent(Tap, Number, 4);
                    break;
                case InstrKeywordType.eKeywordTargetaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleTargetAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapTargetTimeAccent(Tap, Number, 5);
                    break;
                case InstrKeywordType.eKeywordTargetaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleTargetAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapTargetTimeAccent(Tap, Number, 6);
                    break;
                case InstrKeywordType.eKeywordTargetaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleTargetAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapTargetTimeAccent(Tap, Number, 7);
                    break;
                case InstrKeywordType.eKeywordTargetaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleTargetAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapTargetTimeAccent(Tap, Number, 8);
                    break;
                case InstrKeywordType.eKeywordScaleaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleScaleAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapScaleAccent(Tap, Number, 1);
                    break;
                case InstrKeywordType.eKeywordScaleaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleScaleAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapScaleAccent(Tap, Number, 2);
                    break;
                case InstrKeywordType.eKeywordScaleaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleScaleAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapScaleAccent(Tap, Number, 3);
                    break;
                case InstrKeywordType.eKeywordScaleaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleScaleAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapScaleAccent(Tap, Number, 4);
                    break;
                case InstrKeywordType.eKeywordScaleaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleScaleAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapScaleAccent(Tap, Number, 5);
                    break;
                case InstrKeywordType.eKeywordScaleaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleScaleAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapScaleAccent(Tap, Number, 6);
                    break;
                case InstrKeywordType.eKeywordScaleaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleScaleAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapScaleAccent(Tap, Number, 7);
                    break;
                case InstrKeywordType.eKeywordScaleaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleScaleAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapScaleAccent(Tap, Number, 8);
                    break;
                case InstrKeywordType.eKeywordLowpass:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_LOWPASSCUTOFF))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDelayLowpassFreq;
                    }
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFreq))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedFreq;
                    }
                    Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapFilterEnable(Tap, true);
                    SetDelayTapFilterCutoff(Tap, Number, Formula);
                    break;
                case InstrKeywordType.eKeywordFreqaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapFilterEnable(Tap, true);
                    SetDelayTapFilterCutoffAccent(Tap, Number, 1);
                    break;
                case InstrKeywordType.eKeywordFreqaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapFilterEnable(Tap, true);
                    SetDelayTapFilterCutoffAccent(Tap, Number, 2);
                    break;
                case InstrKeywordType.eKeywordFreqaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapFilterEnable(Tap, true);
                    SetDelayTapFilterCutoffAccent(Tap, Number, 3);
                    break;
                case InstrKeywordType.eKeywordFreqaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapFilterEnable(Tap, true);
                    SetDelayTapFilterCutoffAccent(Tap, Number, 4);
                    break;
                case InstrKeywordType.eKeywordFreqaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapFilterEnable(Tap, true);
                    SetDelayTapFilterCutoffAccent(Tap, Number, 5);
                    break;
                case InstrKeywordType.eKeywordFreqaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapFilterEnable(Tap, true);
                    SetDelayTapFilterCutoffAccent(Tap, Number, 6);
                    break;
                case InstrKeywordType.eKeywordFreqaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapFilterEnable(Tap, true);
                    SetDelayTapFilterCutoffAccent(Tap, Number, 7);
                    break;
                case InstrKeywordType.eKeywordFreqaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDelayFilterAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetDelayTapFilterEnable(Tap, true);
                    SetDelayTapFilterCutoffAccent(Tap, Number, 8);
                    break;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* <nlattributes>         ::= inputaccent1 <number> */
        /*                        ::= inputaccent2 <number> */
        /*                        ::= inputaccent3 <number> */
        /*                        ::= inputaccent4 <number> */
        /*                        ::= inputaccent5 <number> */
        /*                        ::= inputaccent6 <number> */
        /*                        ::= inputaccent7 <number> */
        /*                        ::= inputaccent8 <number> */
        /*                        ::= outputaccent1 <number> */
        /*                        ::= outputaccent2 <number> */
        /*                        ::= outputaccent3 <number> */
        /*                        ::= outputaccent4 <number> */
        /*                        ::= outputaccent5 <number> */
        /*                        ::= outputaccent6 <number> */
        /*                        ::= outputaccent7 <number> */
        /*                        ::= outputaccent8 <number> */
        /*                        ::= indexaccent1 <number> */
        /*                        ::= indexaccent2 <number> */
        /*                        ::= indexaccent3 <number> */
        /*                        ::= indexaccent4 <number> */
        /*                        ::= indexaccent5 <number> */
        /*                        ::= indexaccent6 <number> */
        /*                        ::= indexaccent7 <number> */
        /*                        ::= indexaccent8 <number> */
        /*                        ::= overflow {wrap | clamp} */
        /* FOLLOW SET */
        /*  <nlattributes> : {;} */
        public static BuildInstrErrors ParseNLAttributes(
            NonlinProcSpecRec NLProcSpec,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedNLAttribute;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedNLAttribute;
                case InstrKeywordType.eKeywordInputaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcInputAccent(NLProcSpec, Number, 1);
                    break;
                case InstrKeywordType.eKeywordInputaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcInputAccent(NLProcSpec, Number, 2);
                    break;
                case InstrKeywordType.eKeywordInputaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcInputAccent(NLProcSpec, Number, 3);
                    break;
                case InstrKeywordType.eKeywordInputaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcInputAccent(NLProcSpec, Number, 4);
                    break;
                case InstrKeywordType.eKeywordInputaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcInputAccent(NLProcSpec, Number, 5);
                    break;
                case InstrKeywordType.eKeywordInputaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcInputAccent(NLProcSpec, Number, 6);
                    break;
                case InstrKeywordType.eKeywordInputaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcInputAccent(NLProcSpec, Number, 7);
                    break;
                case InstrKeywordType.eKeywordInputaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcInputAccent(NLProcSpec, Number, 8);
                    break;
                case InstrKeywordType.eKeywordOutputaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcOutputAccent(NLProcSpec, Number, 1);
                    break;
                case InstrKeywordType.eKeywordOutputaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcOutputAccent(NLProcSpec, Number, 2);
                    break;
                case InstrKeywordType.eKeywordOutputaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcOutputAccent(NLProcSpec, Number, 3);
                    break;
                case InstrKeywordType.eKeywordOutputaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcOutputAccent(NLProcSpec, Number, 4);
                    break;
                case InstrKeywordType.eKeywordOutputaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcOutputAccent(NLProcSpec, Number, 5);
                    break;
                case InstrKeywordType.eKeywordOutputaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcOutputAccent(NLProcSpec, Number, 6);
                    break;
                case InstrKeywordType.eKeywordOutputaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcOutputAccent(NLProcSpec, Number, 7);
                    break;
                case InstrKeywordType.eKeywordOutputaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcOutputAccent(NLProcSpec, Number, 8);
                    break;
                case InstrKeywordType.eKeywordIndexaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleIndexaccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcIndexAccent(NLProcSpec, Number, 1);
                    break;
                case InstrKeywordType.eKeywordIndexaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleIndexaccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcIndexAccent(NLProcSpec, Number, 2);
                    break;
                case InstrKeywordType.eKeywordIndexaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleIndexaccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcIndexAccent(NLProcSpec, Number, 3);
                    break;
                case InstrKeywordType.eKeywordIndexaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleIndexaccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcIndexAccent(NLProcSpec, Number, 4);
                    break;
                case InstrKeywordType.eKeywordIndexaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleIndexaccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcIndexAccent(NLProcSpec, Number, 5);
                    break;
                case InstrKeywordType.eKeywordIndexaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleIndexaccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcIndexAccent(NLProcSpec, Number, 6);
                    break;
                case InstrKeywordType.eKeywordIndexaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleIndexaccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcIndexAccent(NLProcSpec, Number, 7);
                    break;
                case InstrKeywordType.eKeywordIndexaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleIndexaccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutNLProcIndexAccent(NLProcSpec, Number, 8);
                    break;
                case InstrKeywordType.eKeywordOverflow:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OVERFLOWMODE))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOverflow;
                    }
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedWrapOrClamp;
                    }
                    if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordWrap)
                    {
                        SetNLProcOverflowMode(NLProcSpec, NonlinProcOverflowMode.Wrap);
                    }
                    else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordClamp)
                    {
                        SetNLProcOverflowMode(NLProcSpec, NonlinProcOverflowMode.Clamp);
                    }
                    else
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedWrapOrClamp;
                    }
                    break;
            }

            return ParseNLAttributes(
                NLProcSpec,
                Context,
                out ErrorLine,
                RequiredOnceOnly);
        }




        /*  XXX:                   ::= filter <filter> <filterlist> */
        /*  XXX:                   ::= */
        /* FOLLOW SET */
        /* { ) } */
        public static BuildInstrErrors ParseFilterList(
            FilterSpecRec FilterSpec,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFilter))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilter;
            }

            Error = ParseFilter(FilterSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseFilterList(FilterSpec, Context, out ErrorLine);
        }




        /*  XXX:                   ::= <filtertype> [order <number>] freq <number> */
        /*                           [order <number>]<bandwidth> <gain> */
        /*                           <scaling> <channel> <filterattributes> ; */
        public static BuildInstrErrors ParseFilter(
            FilterSpecRec FilterSpec,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            FilterTypes FilterType;
            OneFilterRec FilterElement;
            double Number;
            PcodeRec Formula;

            /* <filtertype> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilterType;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedFilterType;
                case InstrKeywordType.eKeywordNull:
                    FilterType = FilterTypes.eFilterNull;
                    break;
                case InstrKeywordType.eKeywordLowpass:
                    FilterType = FilterTypes.eFilterFirstOrderLowpass;
                    break;
                case InstrKeywordType.eKeywordHighpass:
                    FilterType = FilterTypes.eFilterFirstOrderHighpass;
                    break;
                case InstrKeywordType.eKeywordReson:
                    FilterType = FilterTypes.eFilterSecondOrderResonant;
                    break;
                case InstrKeywordType.eKeywordZero:
                    FilterType = FilterTypes.eFilterSecondOrderZero;
                    break;
                case InstrKeywordType.eKeywordButterworthlowpass:
                    FilterType = FilterTypes.eFilterButterworthLowpass;
                    break;
                case InstrKeywordType.eKeywordButterworthhighpass:
                    FilterType = FilterTypes.eFilterButterworthHighpass;
                    break;
                case InstrKeywordType.eKeywordButterworthbandpass:
                    FilterType = FilterTypes.eFilterButterworthBandpass;
                    break;
                case InstrKeywordType.eKeywordButterworthbandreject:
                    FilterType = FilterTypes.eFilterButterworthBandreject;
                    break;
                case InstrKeywordType.eKeywordParameq:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrParameqObsolete;
                case InstrKeywordType.eKeywordParameqold:
                    FilterType = FilterTypes.eFilterParametricEQ;
                    break;
                case InstrKeywordType.eKeywordParameq2:
                    FilterType = FilterTypes.eFilterParametricEQ2;
                    break;
                case InstrKeywordType.eKeywordLowshelfeq:
                    FilterType = FilterTypes.eFilterLowShelfEQ;
                    break;
                case InstrKeywordType.eKeywordHighshelfeq:
                    FilterType = FilterTypes.eFilterHighShelfEQ;
                    break;
                case InstrKeywordType.eKeywordResonantlowpass:
                    FilterType = FilterTypes.eFilterResonantLowpass;
                    break;
                case InstrKeywordType.eKeywordResonantlowpass2:
                    FilterType = FilterTypes.eFilterResonantLowpass2;
                    break;
            }

            FilterElement = NewSingleFilterSpec(FilterType);
            AppendFilterToSpec(FilterSpec, FilterElement);

            /* freq <number> */
            if (FilterType != FilterTypes.eFilterNull)
            {
                if ((FilterType == FilterTypes.eFilterResonantLowpass)
                    || (FilterType == FilterTypes.eFilterResonantLowpass2))
                {
                    double Order;
                    int Order2;

                    /* order <number> */
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOrder))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOrder;
                    }
                    /* get order number */
                    Error = ParseNumber(Context, out ErrorLine, out Order);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    Order2 = (int)Order;
                    if ((Order2 < 0) || (Order2 != Order) || ((Order2 % 2) != 0))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrOrderMustBeNonNegativeEvenInteger;
                    }
                    if (FilterType == FilterTypes.eFilterResonantLowpass2)
                    {
                        if ((Order2 != 2) && (Order2 != 4) && (Order2 != 6))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrRLP2OrderMustBe24Or6;
                        }

                        // implement option to enable old broken defective buggy behavior
                        bool broken;
                        if ((Order2 == 4) || (Order2 == 6))
                        {
                            Token = Context.Scanner.GetNextToken();
                            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordBroken))
                            {
                                broken = true;
                            }
                            else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordGood))
                            {
                                broken = false;
                            }
                            else
                            {
                                Context.Scanner.UngetToken(Token);

                                // default to good behavior
                                broken = false;
                            }
                            SetFilterBroken(FilterElement, broken);
                        }
                    }
                    /* remember value */
                    SetFilterLowpassOrder(FilterElement, Order2);
                }
                /* freq */
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFreq))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedFreq;
                }
                /* <number> */
                Error = ParseTrackEffectNumber(
                    Context,
                    out ErrorLine,
                    out Formula,
                    out Number);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                SetSingleFilterCutoff(
                    FilterElement,
                    Number,
                    Formula);
            }

            /* <bandwidth>             ::= bandwidth <number>  */
            /*                         ::= */
            if ((FilterType == FilterTypes.eFilterSecondOrderResonant)
                || (FilterType == FilterTypes.eFilterSecondOrderZero)
                || (FilterType == FilterTypes.eFilterButterworthBandpass)
                || (FilterType == FilterTypes.eFilterButterworthBandreject)
                || (FilterType == FilterTypes.eFilterParametricEQ)
                || (FilterType == FilterTypes.eFilterParametricEQ2)
                || (FilterType == FilterTypes.eFilterLowShelfEQ)
                || (FilterType == FilterTypes.eFilterHighShelfEQ)
                || (FilterType == FilterTypes.eFilterResonantLowpass))
            {
                if (FilterType == FilterTypes.eFilterResonantLowpass)
                {
                    double Order;
                    int Order2;

                    /* order <number> */
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOrder))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOrder;
                    }
                    /* get order number */
                    Error = ParseNumber(
                        Context,
                        out ErrorLine,
                        out Order);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    Order2 = (int)Order;
                    if ((Order2 < 0) || (Order2 != Order) || ((Order2 % 2) != 0))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrOrderMustBeNonNegativeEvenInteger;
                    }
                    /* remember value */
                    SetFilterBandpassOrder(FilterElement, Order2);
                }
                /* bandwidth */
                Token = Context.Scanner.GetNextToken();
                if ((FilterType == FilterTypes.eFilterLowShelfEQ)
                    || (FilterType == FilterTypes.eFilterHighShelfEQ))
                {
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordSlope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedSlope;
                    }
                }
                else
                {
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordBandwidth))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedBandwidth;
                    }
                }
                /* <number> */
                Error = ParseTrackEffectNumber(
                    Context,
                    out ErrorLine,
                    out Formula,
                    out Number);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                SetSingleFilterBandwidthOrSlope(
                    FilterElement,
                    Number,
                    Formula);
            }

            /* <gain>                  ::= gain <number>  */
            /*                         ::= */
            if ((FilterType == FilterTypes.eFilterParametricEQ)
                || (FilterType == FilterTypes.eFilterParametricEQ2)
                || (FilterType == FilterTypes.eFilterLowShelfEQ)
                || (FilterType == FilterTypes.eFilterHighShelfEQ)
                || (FilterType == FilterTypes.eFilterResonantLowpass)
                || (FilterType == FilterTypes.eFilterResonantLowpass2))
            {
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordGain))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedGain;
                }
                Error = ParseTrackEffectNumber(
                    Context,
                    out ErrorLine,
                    out Formula,
                    out Number);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                SetSingleFilterGain(
                    FilterElement,
                    Number,
                    Formula);
            }

            /* scaling mode */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && ((Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordDefaultscaling)
                    || (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnitymidbandgain)
                    || (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnitynoisegain)
                    || (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnityzerohertzgain)))
            {
                switch (FilterType)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case FilterTypes.eFilterNull:
                    case FilterTypes.eFilterFirstOrderLowpass:
                    case FilterTypes.eFilterFirstOrderHighpass:
                    case FilterTypes.eFilterButterworthLowpass:
                    case FilterTypes.eFilterButterworthHighpass:
                    case FilterTypes.eFilterButterworthBandpass:
                    case FilterTypes.eFilterButterworthBandreject:
                    case FilterTypes.eFilterParametricEQ:
                    case FilterTypes.eFilterParametricEQ2:
                    case FilterTypes.eFilterLowShelfEQ:
                    case FilterTypes.eFilterHighShelfEQ:
                    case FilterTypes.eFilterResonantLowpass:
                    case FilterTypes.eFilterResonantLowpass2:
                        if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDefaultscaling)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedDefaultScaling;
                        }
                        break;
                    case FilterTypes.eFilterSecondOrderResonant:
                        if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordDefaultscaling)
                        {
                        }
                        else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnitymidbandgain)
                        {
                            SetSingleFilterScalingMode(FilterElement, FilterScalings.eFilterResonMidbandGain1);
                        }
                        else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnitynoisegain)
                        {
                            SetSingleFilterScalingMode(FilterElement, FilterScalings.eFilterResonNoiseGain1);
                        }
                        else
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedResonScaling;
                        }
                        break;
                    case FilterTypes.eFilterSecondOrderZero:
                        if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordDefaultscaling)
                        {
                        }
                        else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnityzerohertzgain)
                        {
                            SetSingleFilterScalingMode(FilterElement, FilterScalings.eFilterZeroGain1);
                        }
                        else
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedZeroScaling;
                        }
                        break;
                }
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* <channel> ::= {left | right | mono} */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilterChannel;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedFilterChannel;
                case InstrKeywordType.eKeywordLeft:
                    SetSingleFilterChannel(FilterElement, FilterChannels.eFilterLeft);
                    break;
                case InstrKeywordType.eKeywordRight:
                    SetSingleFilterChannel(FilterElement, FilterChannels.eFilterRight);
                    break;
                case InstrKeywordType.eKeywordMono:
                case InstrKeywordType.eKeywordJoint:
                    SetSingleFilterChannel(FilterElement, FilterChannels.eFilterBoth);
                    break;
            }

            /* <filterattributes> */
            Error = ScanFilterAttributes(FilterElement, Context, out ErrorLine, FilterType);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedSemicolon;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* XXX:                    ::= <filterattr> <filterattributes> */
        /*                         ::= */
        /* FOLLOW SET: */
        /* { ; } */
        public static BuildInstrErrors ParseFilterAttributes(
            OneFilterRec FilterElement,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly,
            FilterTypes FilterType)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            Context.Scanner.UngetToken(Token);

            Error = ParseFilterAttr(FilterElement, Context, out ErrorLine, RequiredOnceOnly, FilterType);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseFilterAttributes(FilterElement, Context, out ErrorLine, RequiredOnceOnly, FilterType);
        }




        public static BuildInstrErrors ParseAutoQuiescence(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Decibels;
            double WindowDuration;
            bool PrintReport;

            if (EffectSpecListIsAutoQuiescenceEnabled(EffectList))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrQuiescenceAlreadySpecified;
            }

            /* decibels */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDecibels))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDecibels;
            }

            Error = ParseNumber(Context, out ErrorLine, out Decibels);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (Decibels < 0)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrDecibelsMustBeGEZero;
            }

            /* windowduration */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWindowduration))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedWindowduration;
            }

            Error = ParseNumber(Context, out ErrorLine, out WindowDuration);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (WindowDuration < 0)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrWindowDurationMustBeGEZero;
            }

            /* report */
            PrintReport = false;
            Token = Context.Scanner.GetNextToken();
            switch (Token.GetTokenType())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedReportOrSemicolon;

                case TokenTypes.eTokenSemicolon:
                    Context.Scanner.UngetToken(Token);
                    break;

                case TokenTypes.eTokenKeyword:
                    if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordReport)
                    {
                        PrintReport = true;
                    }
                    else
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedReportOrSemicolon;
                    }
                    break;
            }

            EffectSpecListEnableAutoQuiescence(
                EffectList,
                Decibels,
                WindowDuration,
                PrintReport);

            return BuildInstrErrors.eBuildInstrNoError;
        }




        //
        //
        // BuildInstrument3
        //
        //


        /* <oscfilteroutlfolist>    ::= outputscalinglfo ( <lfo_definition> ) */
        /*                              <oscfilteroutlfolist> */
        /*                          ::= */
        /* FOLLOW SET */
        /* { ; } */
        public static BuildInstrErrors ParseOscFilterOutLFOList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputscalinglfo))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputScalingLfoOrSemicolon;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);

            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseOscFilterOutLFOList(LFOList, Context, out ErrorLine);
        }




        /* <oscbandwidthlfolist>    ::= bandwidthlfo ( <lfo_definition> ) <oscbandwidthlfolist> */
        /*                          ::= */
        public static BuildInstrErrors ParseOscBandwidthLFOList(
            FilterTypes FilterType,
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if ((FilterType == FilterTypes.eFilterLowShelfEQ)
                || (FilterType == FilterTypes.eFilterHighShelfEQ))
            {
                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSlopeLfoOrScalingOrChannel;
                }
                if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordSlopelfo)
                {
                    Context.Scanner.UngetToken(Token);
                    return BuildInstrErrors.eBuildInstrNoError;
                }
            }
            else
            {
                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedBandwidthLfoOrScalingOrChannel;
                }
                if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordBandwidthlfo)
                {
                    Context.Scanner.UngetToken(Token);
                    return BuildInstrErrors.eBuildInstrNoError;
                }
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);

            Error = ScanLfoSpec(
                LFOSpec,
                Context,
                out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseOscBandwidthLFOList(
                FilterType,
                LFOList,
                Context,
                out ErrorLine);
        }




        /* <oscfiltfreqlfolist>     ::= freqlfo ( <lfo_definition> ) <oscfiltfreqlfolist> */
        /*                          ::= */
        public static BuildInstrErrors ParseOscFiltFreqLFOList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFreqLfoOrScalingOrChannel;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFreqlfo)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);

            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseOscFiltFreqLFOList(LFOList, Context, out ErrorLine);
        }




        /* <oscfilterelem>          ::= filter <filtertype> [order <number>] freqenvelope ( */
        /*                              <envelope_definition> ) <oscfiltfreqlfolist> */
        /*                              [order <number>]<oscbandwidth> <oscgain> <scaling> <tapchannel> */
        /*                              outputscalingenvelope ( <envelope_definition> ) */
        /*                              <oscfilteroutlfolist> */
        /* FOLLOW SET */
        /* { ; } */
        public static BuildInstrErrors ParseOscFilterElem(
            FilterSpecRec FilterSpec,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            FilterTypes FilterType;
            OneFilterRec OneFilter;
            int Index;

            /* filter */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFilter))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilter;
            }
            /* <filtertype> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilterType;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedFilterType;
                case InstrKeywordType.eKeywordNull:
                    FilterType = FilterTypes.eFilterNull;
                    break;
                case InstrKeywordType.eKeywordLowpass:
                    FilterType = FilterTypes.eFilterFirstOrderLowpass;
                    break;
                case InstrKeywordType.eKeywordHighpass:
                    FilterType = FilterTypes.eFilterFirstOrderHighpass;
                    break;
                case InstrKeywordType.eKeywordReson:
                    FilterType = FilterTypes.eFilterSecondOrderResonant;
                    break;
                case InstrKeywordType.eKeywordZero:
                    FilterType = FilterTypes.eFilterSecondOrderZero;
                    break;
                case InstrKeywordType.eKeywordButterworthlowpass:
                    FilterType = FilterTypes.eFilterButterworthLowpass;
                    break;
                case InstrKeywordType.eKeywordButterworthhighpass:
                    FilterType = FilterTypes.eFilterButterworthHighpass;
                    break;
                case InstrKeywordType.eKeywordButterworthbandpass:
                    FilterType = FilterTypes.eFilterButterworthBandpass;
                    break;
                case InstrKeywordType.eKeywordButterworthbandreject:
                    FilterType = FilterTypes.eFilterButterworthBandreject;
                    break;
                case InstrKeywordType.eKeywordParameq:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrParameqObsolete;
                case InstrKeywordType.eKeywordParameqold:
                    FilterType = FilterTypes.eFilterParametricEQ;
                    break;
                case InstrKeywordType.eKeywordParameq2:
                    FilterType = FilterTypes.eFilterParametricEQ2;
                    break;
                case InstrKeywordType.eKeywordLowshelfeq:
                    FilterType = FilterTypes.eFilterLowShelfEQ;
                    break;
                case InstrKeywordType.eKeywordHighshelfeq:
                    FilterType = FilterTypes.eFilterHighShelfEQ;
                    break;
                case InstrKeywordType.eKeywordResonantlowpass:
                    FilterType = FilterTypes.eFilterResonantLowpass;
                    break;
                case InstrKeywordType.eKeywordResonantlowpass2:
                    FilterType = FilterTypes.eFilterResonantLowpass2;
                    break;
            }
            Index = GetNumFiltersInSpec(FilterSpec);
            OneFilter = NewSingleFilterSpec(FilterType);
            AppendFilterToSpec(FilterSpec, OneFilter);
            if (FilterType != FilterTypes.eFilterNull)
            {
                if ((FilterType == FilterTypes.eFilterResonantLowpass)
                    || (FilterType == FilterTypes.eFilterResonantLowpass2))
                {
                    double Order;
                    int Order2;

                    /* order <number> */
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOrder))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOrder;
                    }
                    /* get order number */
                    Error = ParseNumber(Context, out ErrorLine, out Order);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    Order2 = (int)Order;
                    if ((Order2 < 0) || (Order2 != Order) || ((Order2 % 2) != 0))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrOrderMustBeNonNegativeEvenInteger;
                    }
                    if (FilterType == FilterTypes.eFilterResonantLowpass2)
                    {
                        if ((Order2 != 2) && (Order2 != 4) && (Order2 != 6))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrRLP2OrderMustBe24Or6;
                        }

                        // implement option to enable old broken defective buggy behavior
                        bool broken;
                        if ((Order2 == 4) || (Order2 == 6))
                        {
                            Token = Context.Scanner.GetNextToken();
                            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordBroken))
                            {
                                broken = true;
                            }
                            else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordGood))
                            {
                                broken = false;
                            }
                            else
                            {
                                Context.Scanner.UngetToken(Token);

                                // default to good behavior
                                broken = false;
                            }
                            SetFilterBroken(GetSingleFilterSpec(FilterSpec, Index), broken);
                        }
                    }
                    /* remember value */
                    SetFilterLowpassOrder(GetSingleFilterSpec(FilterSpec, Index), Order2);
                }
                /* freqenvelope */
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFreqenvelope))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedFreqEnvelope;
                }
                /* ( <envelope_definition> ) */
                Error = ScanEnvelopeSpec(GetFilterCutoffEnvelope(FilterSpec, Index), Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                /* <oscfiltfreqlfolist> */
                Error = ParseOscFiltFreqLFOList(GetFilterCutoffLFO(FilterSpec, Index), Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
            }
            if ((FilterType == FilterTypes.eFilterSecondOrderResonant)
                || (FilterType == FilterTypes.eFilterSecondOrderZero)
                || (FilterType == FilterTypes.eFilterButterworthBandpass)
                || (FilterType == FilterTypes.eFilterButterworthBandreject)
                || (FilterType == FilterTypes.eFilterParametricEQ)
                || (FilterType == FilterTypes.eFilterParametricEQ2)
                || (FilterType == FilterTypes.eFilterLowShelfEQ)
                || (FilterType == FilterTypes.eFilterHighShelfEQ)
                || (FilterType == FilterTypes.eFilterResonantLowpass))
            {
                if (FilterType == FilterTypes.eFilterResonantLowpass)
                {
                    double Order;
                    int Order2;

                    /* order <number> */
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOrder))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOrder;
                    }
                    /* get order number */
                    Error = ParseNumber(Context, out ErrorLine, out Order);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    Order2 = (int)Order;
                    if ((Order2 < 0) || (Order2 != Order) || ((Order2 % 2) != 0))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrOrderMustBeNonNegativeEvenInteger;
                    }
                    /* remember value */
                    SetFilterBandpassOrder(GetSingleFilterSpec(FilterSpec, Index), Order2);
                }
                /* bandwidthenvelope ( <envelope_definition> ) <oscbandwidthlfolist> */
                Token = Context.Scanner.GetNextToken();
                if ((FilterType == FilterTypes.eFilterLowShelfEQ)
                    || (FilterType == FilterTypes.eFilterHighShelfEQ))
                {
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordSlopeenvelope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedSlopeEnvelope;
                    }
                }
                else
                {
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordBandwidthenvelope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedBandwidthEnvelope;
                    }
                }
                /* <envelope_definition> */
                Error = ScanEnvelopeSpec(
                    GetFilterBandwidthOrSlopeEnvelope(FilterSpec, Index),
                    Context,
                    out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                /* <oscbandwidthlfolist> */
                Error = ParseOscBandwidthLFOList(
                    FilterType,
                    GetFilterBandwidthOrSlopeLFO(FilterSpec, Index),
                    Context,
                    out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
            }
            if ((FilterType == FilterTypes.eFilterParametricEQ)
                || (FilterType == FilterTypes.eFilterParametricEQ2)
                || (FilterType == FilterTypes.eFilterLowShelfEQ)
                || (FilterType == FilterTypes.eFilterHighShelfEQ)
                || (FilterType == FilterTypes.eFilterResonantLowpass)
                || (FilterType == FilterTypes.eFilterResonantLowpass2))
            {
                /* gainenvelope ( <envelope_definition> ) <oscgainlfolist> */
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordGainenvelope))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedGainEnvelope;
                }
                /* <envelope_definition> */
                Error = ScanEnvelopeSpec(
                    GetFilterGainEnvelope(
                        FilterSpec,
                        Index),
                    Context,
                    out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                /* <oscgainlfolist> */
                Error = ParseOscGainLFOList(
                    GetFilterGainLFO(
                        FilterSpec,
                        Index),
                    Context,
                    out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
            }
            /* <scaling> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && ((Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordDefaultscaling)
                    || (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnitymidbandgain)
                    || (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnitynoisegain)
                    || (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnityzerohertzgain)))
            {
                switch (FilterType)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case FilterTypes.eFilterNull:
                    case FilterTypes.eFilterFirstOrderLowpass:
                    case FilterTypes.eFilterFirstOrderHighpass:
                    case FilterTypes.eFilterButterworthLowpass:
                    case FilterTypes.eFilterButterworthHighpass:
                    case FilterTypes.eFilterButterworthBandpass:
                    case FilterTypes.eFilterButterworthBandreject:
                    case FilterTypes.eFilterParametricEQ:
                    case FilterTypes.eFilterParametricEQ2:
                    case FilterTypes.eFilterLowShelfEQ:
                    case FilterTypes.eFilterHighShelfEQ:
                    case FilterTypes.eFilterResonantLowpass:
                    case FilterTypes.eFilterResonantLowpass2:
                        if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDefaultscaling)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedDefaultScaling;
                        }
                        break;
                    case FilterTypes.eFilterSecondOrderResonant:
                        if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordDefaultscaling)
                        {
                        }
                        else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnitymidbandgain)
                        {
                            SetSingleFilterScalingMode(OneFilter, FilterScalings.eFilterResonMidbandGain1);
                        }
                        else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnitynoisegain)
                        {
                            SetSingleFilterScalingMode(OneFilter, FilterScalings.eFilterResonNoiseGain1);
                        }
                        else
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedResonScaling;
                        }
                        break;
                    case FilterTypes.eFilterSecondOrderZero:
                        if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordDefaultscaling)
                        {
                        }
                        else if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordUnityzerohertzgain)
                        {
                            SetSingleFilterScalingMode(OneFilter, FilterScalings.eFilterZeroGain1);
                        }
                        else
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedZeroScaling;
                        }
                        break;
                }
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* <tapchannel> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilterChannel;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedFilterChannel;
                case InstrKeywordType.eKeywordLeft:
                    SetSingleFilterChannel(OneFilter, FilterChannels.eFilterLeft);
                    break;
                case InstrKeywordType.eKeywordRight:
                    SetSingleFilterChannel(OneFilter, FilterChannels.eFilterRight);
                    break;
                case InstrKeywordType.eKeywordMono:
                case InstrKeywordType.eKeywordJoint:
                    SetSingleFilterChannel(OneFilter, FilterChannels.eFilterBoth);
                    break;
            }
            /* outputscalingenvelope */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputscalingenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputScalingEnvelope;
            }
            /* <envelope_definition> */
            Error = ScanEnvelopeSpec(GetFilterOutputEnvelope(FilterSpec, Index), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* <oscfilteroutlfolist> */
            Error = ParseOscFilterOutLFOList(GetFilterOutputLFO(FilterSpec, Index), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* <oscfilterelems>         ::= <oscfilterelem> ; <oscfilterelems> */
        /*                          ::= */
        public static BuildInstrErrors ParseOscFilterElems(
            FilterSpecRec FilterSpec,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            Context.Scanner.UngetToken(Token);

            Error = ParseOscFilterElem(FilterSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedSemicolon;
            }

            return ParseOscFilterElems(FilterSpec, Context, out ErrorLine);
        }




        /* <nlindexlfolist>         ::= indexlfo ( <lfo_definition> ) <nlindexlfolist> */
        /*                          ::= */
        /* FOLLOW SET */
        /* { ; } */
        public static BuildInstrErrors ParseNLIndexLFOList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedIndexLfoOrSemicolon;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordIndexlfo)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedIndexLfoOrSemicolon;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseNLIndexLFOList(LFOList, Context, out ErrorLine);
        }




        /* <nloutputscalinglfolist> ::= outputscalinglfo ( <lfo_definition> ) */
        /*                              <nloutputscalinglfolist> */
        /*                          ::= */
        /* FOLLOW SET */
        /* { indexenvelope } */
        public static BuildInstrErrors ParseNLOutputScalingLFOList(
                                                                    LFOListSpecRec LFOList,
                                                                    BuildInstrumentContext Context,
                                                                    out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputScalingLfoOrIndexEnvelope;
            }
            if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordIndexenvelope)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputscalinglfo)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputScalingLfoOrIndexEnvelope;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseNLOutputScalingLFOList(LFOList, Context, out ErrorLine);
        }




        /* <nlinputscalinglfolist>  ::= inputscalinglfo ( <lfo_definition> ) */
        /*                              <nlinputscalinglfolist> */
        /*                          ::= */
        /* FOLLOW SET */
        /* { outputscalingenvelope } */
        public static BuildInstrErrors ParseNLInputScalingLFOList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInputScalingLfoOrOutputScalingEnvelope;
            }
            if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordOutputscalingenvelope)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInputscalinglfo)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInputScalingLfoOrOutputScalingEnvelope;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseNLInputScalingLFOList(LFOList, Context, out ErrorLine);
        }




        /* <oscdelaycutofflfolist>  ::= freqlfo ( <lfo_definition> ) <oscdelaycutofflfolist> */
        /*                          ::= */
        /* FOLLOW SET */
        /* { ; } */
        public static BuildInstrErrors ParseOscDelayCutoffLfoList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFreqLfoOrSemicolon;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedFreqLfoOrSemicolon;
                case InstrKeywordType.eKeywordFreqlfo:
                    break;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseOscDelayCutoffLfoList(LFOList, Context, out ErrorLine);
        }




        /* <oscdelayscalelfolist>   ::= scalelfo ( <lfo_definition> ) <oscdelayscalelfolist> */
        /*                          ::= */
        /* FOLLOW SET */
        /* { ; lowpass } */
        public static BuildInstrErrors ParseOscDelayScaleLfoList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                || ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                    && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordEffect)))
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedScaleLfoCutoffOrSemicolon;
            }
            if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordLowpass)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordScalelfo)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedScaleLfoCutoffOrSemicolon;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseOscDelayScaleLfoList(LFOList, Context, out ErrorLine);
        }




        /* <oscdelaytargetlfolist>  ::= targetlfo ( <lfo_definition> ) <oscdelaytargetlfolist> */
        /*                          ::= */
        /* FOLLOW SET */
        /* { scaleenvelope } */
        public static BuildInstrErrors ParseOscDelayTargetLfoList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedTargetLfoOrScaleEnvelope;
            }
            if (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordScaleenvelope)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordTargetlfo)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedTargetLfoOrScaleEnvelope;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseOscDelayTargetLfoList(LFOList, Context, out ErrorLine);
        }




        /* <oscdelaysourcelfolist>  ::= sourcelfo ( <lfo_definition> ) <oscdelaysourcelfolist> */
        /*                          ::= */
        /* FOLLOW SET */
        /* { to interpolate } */
        public static BuildInstrErrors ParseOscDelaySourceLfoList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedSourceLfoOrTo;
            }
            if ((Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordTo)
                || (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordInterpolate))
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordSourcelfo)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedSourceLfoOrToOrInterpolate;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseOscDelaySourceLfoList(LFOList, Context, out ErrorLine);
        }




        /* <oscdelayelem>           ::= maxdelaytime <number> */
        /*                          ::= tap <tapchannel> sourceenvelope ( */
        /*                              <envelope_definition> ) <oscdelaysourcelfolist> */
        /*                              [interpolate] to <tapchannel> targetenvelope ( */
        /*                              <envelope_definition> ) <oscdelaytargetlfolist> */
        /*                              scaleenvelope ( <envelope_definition> ) */
        /*                              <oscdelayscalelfolist> */
        /*                              [lowpass freqenvelope ( <envelope_definition> ) */
        /*                              <freqlfolist>] */
        /* FOLLOW SET */
        /* { ; } */
        public static BuildInstrErrors ParseOscDelayElem(
            DelayEffectRec DelayEffect,
            DelayTapRec DelayTap,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly,
            int Index)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDelayLineElem;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedDelayLineElem;

                case InstrKeywordType.eKeywordMaxdelaytime:
                    {
                        if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.DELAYEFFECT_REQUIREDONCEONLY_MAXDELAYTIME))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrMultipleMaxDelayTime;
                        }
                        PcodeRec Formula;
                        Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                        SetDelayMaxTime(DelayEffect, Number);
                        SetDelayMaxTimeFormula(DelayEffect, Formula);
                    }
                    break;

                case InstrKeywordType.eKeywordTap:
                    /* tap source <tapchannel> */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedTapChannel;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedTapChannel;
                        case InstrKeywordType.eKeywordLeft:
                            SetDelayTapSource(DelayTap, DelayChannelType.eTapLeftChannel);
                            break;
                        case InstrKeywordType.eKeywordRight:
                            SetDelayTapSource(DelayTap, DelayChannelType.eTapRightChannel);
                            break;
                        case InstrKeywordType.eKeywordMono:
                            SetDelayTapSource(DelayTap, DelayChannelType.eTapMonoChannel);
                            break;
                    }

                    /* sourceenvelope */
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordSourceenvelope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedSourceEnvelope;
                    }

                    /* ( <envelope_definition> ) */
                    Error = ScanEnvelopeSpec(GetDelayTapSourceEnvelope(DelayEffect, Index), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    /* <oscdelaysourcelfolist> */
                    Error = ParseOscDelaySourceLfoList(GetDelayTapSourceLFO(DelayEffect, Index), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    /* to */
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                        && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordInterpolate))
                    {
                        SetDelayTapInterpolateFlag(DelayTap, true);
                        Token = Context.Scanner.GetNextToken();
                    }
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordTo))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedToOrInterpolate;
                    }

                    /* tap target <tapchannel> */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedTapChannel;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedTapChannel;
                        case InstrKeywordType.eKeywordLeft:
                            SetDelayTapTarget(DelayTap, DelayChannelType.eTapLeftChannel);
                            break;
                        case InstrKeywordType.eKeywordRight:
                            SetDelayTapTarget(DelayTap, DelayChannelType.eTapRightChannel);
                            break;
                        case InstrKeywordType.eKeywordMono:
                            SetDelayTapTarget(DelayTap, DelayChannelType.eTapMonoChannel);
                            break;
                    }

                    /* targetenvelope */
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordTargetenvelope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedTargetEnvelope;
                    }

                    /* ( <envelope_definition> ) */
                    Error = ScanEnvelopeSpec(GetDelayTapTargetEnvelope(DelayEffect, Index), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    /* <oscdelaytargetlfolist> */
                    Error = ParseOscDelayTargetLfoList(GetDelayTapTargetLFO(DelayEffect, Index), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    /* scaleenvelope */
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordScaleenvelope))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedScaleEnvelope;
                    }

                    /* ( <envelope_definition> ) */
                    Error = ScanEnvelopeSpec(GetDelayTapScaleEnvelope(DelayEffect, Index), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    /* <oscdelayscalelfolist> */
                    Error = ParseOscDelayScaleLfoList(GetDelayTapTargetLFO(DelayEffect, Index), Context, out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    /* lowpass? */
                    Token = Context.Scanner.GetNextToken();
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordLowpass))
                    {
                        Context.Scanner.UngetToken(Token);
                    }
                    else
                    {
                        SetDelayTapFilterEnable(DelayTap, true);
                        /* freqenvelope */
                        Token = Context.Scanner.GetNextToken();
                        if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                            || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFreqenvelope))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedFreqEnvelope;
                        }
                        /* ( <envelope_definition> ) */
                        Error = ScanEnvelopeSpec(GetDelayTapCutoffEnvelope(DelayEffect, Index), Context, out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                        /* <oscdelaycutofflfolist> */
                        Error = ParseOscDelayCutoffLfoList(GetDelayTapCutoffLFO(DelayEffect, Index), Context, out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                    }

                    break;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* <oscgainlfolist>         ::= gainlfo ( <lfo_definition> ) <oscgainlfolist> */
        /*                          ::= */
        public static BuildInstrErrors ParseOscGainLFOList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedGainLfoOrScalingOrChannel;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordGainlfo)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseOscGainLFOList(LFOList, Context, out ErrorLine);
        }




        /* <resamplerstuff>         ::= rate <number> {truncate | interpolate} */
        /*                              {square | triangle} */
        public static BuildInstrErrors ParseResampler(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;
            ResampleMethodType ResampleMethod;
            ResampOutType OutputMethod;
            ResamplerSpecRec Specifier;

            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordRate))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedRate;
            }
            Error = ParseNumber(Context, out ErrorLine, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedTruncateOrInterpolate;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedTruncateOrInterpolate;
                case InstrKeywordType.eKeywordTruncate:
                    ResampleMethod = ResampleMethodType.eResampleTruncating;
                    break;
                case InstrKeywordType.eKeywordInterpolate:
                    ResampleMethod = ResampleMethodType.eResampleLinearInterpolation;
                    break;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedSquareOrTriangle;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSquareOrTriangle;
                case InstrKeywordType.eKeywordSquare:
                    OutputMethod = ResampOutType.eResampleRectangular;
                    break;
                case InstrKeywordType.eKeywordTriangle:
                    OutputMethod = ResampOutType.eResampleTriangular;
                    break;
            }

            Specifier = NewResamplerSpecifier(ResampleMethod, OutputMethod, Number);
            AddResamplerToEffectSpecList(EffectList, Specifier, EnabledFlag);

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* <ideallowpass>           ::= cutoff <number> order <integer> */
        public static BuildInstrErrors ParseIdealLowpass(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Cutoff;
            double Order;
            int Order2;
            IdealLPSpecRec IdealLowpassSpec;

            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFreq))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFreq;
            }
            Error = ParseNumber(Context, out ErrorLine, out Cutoff);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOrder))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOrder;
            }
            Error = ParseNumber(Context, out ErrorLine, out Order);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            Order2 = (int)Order;
            if ((Order2 < 1) || ((Order2 % 2) == 0))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrOrderMustBePositiveOddInteger;
            }

            IdealLowpassSpec = NewIdealLowpassSpec(Cutoff, Order2);
            AddIdealLPToEffectSpecList(EffectList, IdealLowpassSpec, EnabledFlag);

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
            {
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordMinsamplingrate))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedMinsamplingrateOrSemicolon;
                }
                double MinSamplingRate;
                Error = ParseNumber(Context, out ErrorLine, out MinSamplingRate);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                SetIdealLowpassMinSamplingRate(IdealLowpassSpec, (int)MinSamplingRate);
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* <convolver>              ::= {<mono> | <stereo> | <bistereo>} <props> */
        /* <mono>										::= mono sample <string> */
        /* <stereo>									::= stereo left <string> right <string> */
        /* <bistereo>								::= bistereo leftintoleft <string> rightintoleft <string> leftintoright <string> rightintoright <string> */
        /* <props>									::= directgain <number> processedgain <number> latency <number> */
        private delegate void ParseConvolverSpecSetImpulseResponseMethod(ConvolverSpecRec Spec, string Name);
        private struct ConvolveParseRec
        {
            public readonly InstrKeywordType Keyword;
            public readonly BuildInstrErrors Error;
            public readonly ParseConvolverSpecSetImpulseResponseMethod ConvolverSpecSetImpulseResponse;

            public ConvolveParseRec(InstrKeywordType Keyword, BuildInstrErrors Error, ParseConvolverSpecSetImpulseResponseMethod ConvolverSpecSetImpulseResponse)
            {
                this.Keyword = Keyword;
                this.Error = Error;
                this.ConvolverSpecSetImpulseResponse = ConvolverSpecSetImpulseResponse;
            }
        }
        private static readonly ConvolveParseRec[] ConvolverMonoRule = new ConvolveParseRec[]
        {
            new ConvolveParseRec(
                InstrKeywordType.eKeywordSample,
                BuildInstrErrors.eBuildInstrExpectedSample,
                ConvolverSpecSetImpulseResponseMono),
        };
        private static readonly ConvolveParseRec[] ConvolverStereoRule = new ConvolveParseRec[]
        {
            new ConvolveParseRec(
                InstrKeywordType.eKeywordLeft,
                BuildInstrErrors.eBuildInstrExpectedLeft,
                ConvolverSpecSetImpulseResponseStereoLeft),
            new ConvolveParseRec(
                InstrKeywordType.eKeywordRight,
                BuildInstrErrors.eBuildInstrExpectedRight,
                ConvolverSpecSetImpulseResponseStereoRight),
        };
        private static readonly ConvolveParseRec[] ConvolverBiStereoRule = new ConvolveParseRec[]
        {
            new ConvolveParseRec(
                InstrKeywordType.eKeywordLeftintoleft,
                BuildInstrErrors.eBuildInstrExpectedLeftIntoLeft,
                ConvolverSpecSetImpulseResponseBiStereoLeftIntoLeft),
            new ConvolveParseRec(
                InstrKeywordType.eKeywordRightintoleft,
                BuildInstrErrors.eBuildInstrExpectedRightIntoLeft,
                ConvolverSpecSetImpulseResponseBiStereoRightIntoLeft),
            new ConvolveParseRec(
                InstrKeywordType.eKeywordLeftintoright,
                BuildInstrErrors.eBuildInstrExpectedLeftIntoRight,
                ConvolverSpecSetImpulseResponseBiStereoLeftIntoRight),
            new ConvolveParseRec(
                InstrKeywordType.eKeywordRightintoright,
                BuildInstrErrors.eBuildInstrExpectedRightIntoRight,
                ConvolverSpecSetImpulseResponseBiStereoRightIntoRight),
        };
        public static BuildInstrErrors ParseConvolver(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            ConvolverSpecRec ConvolverSpec;
            ConvolveParseRec[] RuleArray;
            ConvolveSrcType Type;
            double Number;
            PcodeRec Formula;

            /* (mono | stereo | bistereo) */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedConvolverInputType;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedConvolverInputType;

                case InstrKeywordType.eKeywordMono:
                    Type = ConvolveSrcType.eConvolveMono;
                    RuleArray = ConvolverMonoRule;
                    break;

                case InstrKeywordType.eKeywordStereo:
                    Type = ConvolveSrcType.eConvolveStereo;
                    RuleArray = ConvolverStereoRule;
                    break;

                case InstrKeywordType.eKeywordBistereo:
                    Type = ConvolveSrcType.eConvolveBiStereo;
                    RuleArray = ConvolverBiStereoRule;
                    break;
            }

            ConvolverSpec = NewConvolverSpec();
            ConvolverSpecSetSourceType(ConvolverSpec, Type);

            AddConvolverToEffectSpecList(EffectList, ConvolverSpec, EnabledFlag);

            for (int i = 0; i < RuleArray.Length; i += 1)
            {
                /* keyword */
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != RuleArray[i].Keyword))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return RuleArray[i].Error;
                }

                /* <string> */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenString)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSampleName;
                }

                RuleArray[i].ConvolverSpecSetImpulseResponse(
                    ConvolverSpec,
                    Token.GetTokenStringValue());
            }

            /* directgain <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDirectgain))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDirectgain;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            ConvolverSpecSetDirectGain(ConvolverSpec, Number, Formula);

            /* processedgain <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordProcessedgain))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedProcessedgain;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            ConvolverSpecSetProcessedGain(ConvolverSpec, Number, Formula);

            // [optional]

            /* latency <number> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordLatency))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedLatencyOrSemicolon;
            }

            Error = ParseNumber(Context, out ErrorLine, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (Number < 0)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrLatencyMustBeAtLeastZero;
            }
            ConvolverSpecSetLatency(ConvolverSpec, Number);

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* <oscdelayelems>          ::= <oscdelayelem> ; <oscdelayelems> */
        /*                          ::= */
        /* FOLLOW SET */
        /* { ) } */
        public static BuildInstrErrors ParseOscDelayElems(
            DelayEffectRec DelayEffect,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            DelayTapRec DelayTap;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            Context.Scanner.UngetToken(Token);

            DelayTap = NewDelayTap();
            AppendTapToDelayEffect(DelayEffect, DelayTap);

            Error = ParseOscDelayElem(DelayEffect, DelayTap, Context, out ErrorLine,
                RequiredOnceOnly, GetDelayEffectSpecNumTaps(DelayEffect) - 1/*index*/);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedSemicolon;
            }

            return ParseOscDelayElems(DelayEffect, Context, out ErrorLine, RequiredOnceOnly);
        }




        /* XXX:                    ::= freqaccent1 <number> */
        /*                         ::= freqaccent2 <number> */
        /*                         ::= freqaccent3 <number> */
        /*                         ::= freqaccent4 <number> */
        /*                         ::= freqaccent5 <number> */
        /*                         ::= freqaccent6 <number> */
        /*                         ::= freqaccent7 <number> */
        /*                         ::= freqaccent8 <number> */
        /*                         ::= bandwidthaccent1 <number> */
        /*                         ::= bandwidthaccent2 <number> */
        /*                         ::= bandwidthaccent3 <number> */
        /*                         ::= bandwidthaccent4 <number> */
        /*                         ::= bandwidthaccent5 <number> */
        /*                         ::= bandwidthaccent6 <number> */
        /*                         ::= bandwidthaccent7 <number> */
        /*                         ::= bandwidthaccent8 <number> */
        /*                         ::= outputscaling <number> */
        /*                         ::= outputscalingaccent1 <number> */
        /*                         ::= outputscalingaccent2 <number> */
        /*                         ::= outputscalingaccent3 <number> */
        /*                         ::= outputscalingaccent4 <number> */
        /*                         ::= outputscalingaccent5 <number> */
        /*                         ::= outputscalingaccent6 <number> */
        /*                         ::= outputscalingaccent7 <number> */
        /*                         ::= outputscalingaccent8 <number> */
        /*                         ::= gainaccent1 <number> */
        /*                         ::= gainaccent2 <number> */
        /*                         ::= gainaccent3 <number> */
        /*                         ::= gainaccent4 <number> */
        /*                         ::= gainaccent5 <number> */
        /*                         ::= gainaccent6 <number> */
        /*                         ::= gainaccent7 <number> */
        /*                         ::= gainaccent8 <number> */
        public static BuildInstrErrors ParseFilterAttr(
            OneFilterRec FilterElement,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly,
            FilterTypes FilterType)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;
            PcodeRec Formula;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilterAttr;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedFilterAttr;
                case InstrKeywordType.eKeywordFreqaccent1:
                    if (FilterType == FilterTypes.eFilterNull)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrNullFilterHasNoFreqAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFreqaccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterCutoffAccent(FilterElement, Number, 1);
                    break;
                case InstrKeywordType.eKeywordFreqaccent2:
                    if (FilterType == FilterTypes.eFilterNull)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrNullFilterHasNoFreqAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFreqaccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterCutoffAccent(FilterElement, Number, 2);
                    break;
                case InstrKeywordType.eKeywordFreqaccent3:
                    if (FilterType == FilterTypes.eFilterNull)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrNullFilterHasNoFreqAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFreqaccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterCutoffAccent(FilterElement, Number, 3);
                    break;
                case InstrKeywordType.eKeywordFreqaccent4:
                    if (FilterType == FilterTypes.eFilterNull)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrNullFilterHasNoFreqAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFreqaccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterCutoffAccent(FilterElement, Number, 4);
                    break;
                case InstrKeywordType.eKeywordFreqaccent5:
                    if (FilterType == FilterTypes.eFilterNull)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrNullFilterHasNoFreqAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFreqaccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterCutoffAccent(FilterElement, Number, 5);
                    break;
                case InstrKeywordType.eKeywordFreqaccent6:
                    if (FilterType == FilterTypes.eFilterNull)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrNullFilterHasNoFreqAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFreqaccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterCutoffAccent(FilterElement, Number, 6);
                    break;
                case InstrKeywordType.eKeywordFreqaccent7:
                    if (FilterType == FilterTypes.eFilterNull)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrNullFilterHasNoFreqAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFreqaccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterCutoffAccent(FilterElement, Number, 7);
                    break;
                case InstrKeywordType.eKeywordFreqaccent8:
                    if (FilterType == FilterTypes.eFilterNull)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrNullFilterHasNoFreqAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFreqaccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterCutoffAccent(FilterElement, Number, 8);
                    break;
                case InstrKeywordType.eKeywordSlopeaccent1:
                    if ((FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoSlopeAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSlopeaccent1;
                    }
                    goto BandwidthAccent1Point;
                case InstrKeywordType.eKeywordBandwidthaccent1:
                    if ((FilterType != FilterTypes.eFilterSecondOrderResonant)
                        && (FilterType != FilterTypes.eFilterSecondOrderZero)
                        && (FilterType != FilterTypes.eFilterButterworthBandpass)
                        && (FilterType != FilterTypes.eFilterButterworthBandreject)
                        && (FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterResonantLowpass))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoBandwidthAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleBandwidthaccent1;
                    }
                BandwidthAccent1Point:
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterBandwidthOrSlopeAccent(FilterElement, Number, 1);
                    break;
                case InstrKeywordType.eKeywordSlopeaccent2:
                    if ((FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoSlopeAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSlopeaccent2;
                    }
                    goto BandwidthAccent2Point;
                case InstrKeywordType.eKeywordBandwidthaccent2:
                    if ((FilterType != FilterTypes.eFilterSecondOrderResonant)
                        && (FilterType != FilterTypes.eFilterSecondOrderZero)
                        && (FilterType != FilterTypes.eFilterButterworthBandpass)
                        && (FilterType != FilterTypes.eFilterButterworthBandreject)
                        && (FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterResonantLowpass))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoBandwidthAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleBandwidthaccent2;
                    }
                BandwidthAccent2Point:
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterBandwidthOrSlopeAccent(FilterElement, Number, 2);
                    break;
                case InstrKeywordType.eKeywordSlopeaccent3:
                    if ((FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoSlopeAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSlopeaccent3;
                    }
                    goto BandwidthAccent3Point;
                case InstrKeywordType.eKeywordBandwidthaccent3:
                    if ((FilterType != FilterTypes.eFilterSecondOrderResonant)
                        && (FilterType != FilterTypes.eFilterSecondOrderZero)
                        && (FilterType != FilterTypes.eFilterButterworthBandpass)
                        && (FilterType != FilterTypes.eFilterButterworthBandreject)
                        && (FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterResonantLowpass))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoBandwidthAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleBandwidthaccent3;
                    }
                BandwidthAccent3Point:
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterBandwidthOrSlopeAccent(FilterElement, Number, 3);
                    break;
                case InstrKeywordType.eKeywordSlopeaccent4:
                    if ((FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoSlopeAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSlopeaccent4;
                    }
                    goto BandwidthAccent4Point;
                case InstrKeywordType.eKeywordBandwidthaccent4:
                    if ((FilterType != FilterTypes.eFilterSecondOrderResonant)
                        && (FilterType != FilterTypes.eFilterSecondOrderZero)
                        && (FilterType != FilterTypes.eFilterButterworthBandpass)
                        && (FilterType != FilterTypes.eFilterButterworthBandreject)
                        && (FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterResonantLowpass))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoBandwidthAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleBandwidthaccent4;
                    }
                BandwidthAccent4Point:
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterBandwidthOrSlopeAccent(FilterElement, Number, 4);
                    break;
                case InstrKeywordType.eKeywordSlopeaccent5:
                    if ((FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoSlopeAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSlopeaccent5;
                    }
                    goto BandwidthAccent5Point;
                case InstrKeywordType.eKeywordBandwidthaccent5:
                    if ((FilterType != FilterTypes.eFilterSecondOrderResonant)
                        && (FilterType != FilterTypes.eFilterSecondOrderZero)
                        && (FilterType != FilterTypes.eFilterButterworthBandpass)
                        && (FilterType != FilterTypes.eFilterButterworthBandreject)
                        && (FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterResonantLowpass))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoBandwidthAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleBandwidthaccent5;
                    }
                BandwidthAccent5Point:
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterBandwidthOrSlopeAccent(FilterElement, Number, 5);
                    break;
                case InstrKeywordType.eKeywordSlopeaccent6:
                    if ((FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoSlopeAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSlopeaccent6;
                    }
                    goto BandwidthAccent6Point;
                case InstrKeywordType.eKeywordBandwidthaccent6:
                    if ((FilterType != FilterTypes.eFilterSecondOrderResonant)
                        && (FilterType != FilterTypes.eFilterSecondOrderZero)
                        && (FilterType != FilterTypes.eFilterButterworthBandpass)
                        && (FilterType != FilterTypes.eFilterButterworthBandreject)
                        && (FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterResonantLowpass))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoBandwidthAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleBandwidthaccent6;
                    }
                BandwidthAccent6Point:
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterBandwidthOrSlopeAccent(FilterElement, Number, 6);
                    break;
                case InstrKeywordType.eKeywordSlopeaccent7:
                    if ((FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoSlopeAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSlopeaccent7;
                    }
                    goto BandwidthAccent7Point;
                case InstrKeywordType.eKeywordBandwidthaccent7:
                    if ((FilterType != FilterTypes.eFilterSecondOrderResonant)
                        && (FilterType != FilterTypes.eFilterSecondOrderZero)
                        && (FilterType != FilterTypes.eFilterButterworthBandpass)
                        && (FilterType != FilterTypes.eFilterButterworthBandreject)
                        && (FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterResonantLowpass))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoBandwidthAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleBandwidthaccent7;
                    }
                BandwidthAccent7Point:
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterBandwidthOrSlopeAccent(FilterElement, Number, 7);
                    break;
                case InstrKeywordType.eKeywordSlopeaccent8:
                    if ((FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoSlopeAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleSlopeaccent8;
                    }
                    goto BandwidthAccent8Point;
                case InstrKeywordType.eKeywordBandwidthaccent8:
                    if ((FilterType != FilterTypes.eFilterSecondOrderResonant)
                        && (FilterType != FilterTypes.eFilterSecondOrderZero)
                        && (FilterType != FilterTypes.eFilterButterworthBandpass)
                        && (FilterType != FilterTypes.eFilterButterworthBandreject)
                        && (FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterResonantLowpass))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoBandwidthAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleBandwidthaccent8;
                    }
                BandwidthAccent8Point:
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterBandwidthOrSlopeAccent(FilterElement, Number, 8);
                    break;
                case InstrKeywordType.eKeywordOutputscaling:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALING))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputScaling;
                    }
                    Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterOutputMultiplier(FilterElement, Number, Formula);
                    break;
                case InstrKeywordType.eKeywordOutputaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterOutputMultiplierAccent(FilterElement, Number, 1);
                    break;
                case InstrKeywordType.eKeywordOutputaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterOutputMultiplierAccent(FilterElement, Number, 2);
                    break;
                case InstrKeywordType.eKeywordOutputaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterOutputMultiplierAccent(FilterElement, Number, 3);
                    break;
                case InstrKeywordType.eKeywordOutputaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterOutputMultiplierAccent(FilterElement, Number, 4);
                    break;
                case InstrKeywordType.eKeywordOutputaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterOutputMultiplierAccent(FilterElement, Number, 5);
                    break;
                case InstrKeywordType.eKeywordOutputaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterOutputMultiplierAccent(FilterElement, Number, 6);
                    break;
                case InstrKeywordType.eKeywordOutputaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterOutputMultiplierAccent(FilterElement, Number, 7);
                    break;
                case InstrKeywordType.eKeywordOutputaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputScalingAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterOutputMultiplierAccent(FilterElement, Number, 8);
                    break;
                case InstrKeywordType.eKeywordGainaccent1:
                    if ((FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ)
                        && (FilterType != FilterTypes.eFilterResonantLowpass)
                        && (FilterType != FilterTypes.eFilterResonantLowpass2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoGainAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleGainAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterGainAccent(FilterElement, Number, 1);
                    break;
                case InstrKeywordType.eKeywordGainaccent2:
                    if ((FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ)
                        && (FilterType != FilterTypes.eFilterResonantLowpass)
                        && (FilterType != FilterTypes.eFilterResonantLowpass2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoGainAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleGainAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterGainAccent(FilterElement, Number, 2);
                    break;
                case InstrKeywordType.eKeywordGainaccent3:
                    if ((FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ)
                        && (FilterType != FilterTypes.eFilterResonantLowpass)
                        && (FilterType != FilterTypes.eFilterResonantLowpass2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoGainAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleGainAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterGainAccent(FilterElement, Number, 3);
                    break;
                case InstrKeywordType.eKeywordGainaccent4:
                    if ((FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ)
                        && (FilterType != FilterTypes.eFilterResonantLowpass)
                        && (FilterType != FilterTypes.eFilterResonantLowpass2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoGainAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleGainAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterGainAccent(FilterElement, Number, 4);
                    break;
                case InstrKeywordType.eKeywordGainaccent5:
                    if ((FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ)
                        && (FilterType != FilterTypes.eFilterResonantLowpass)
                        && (FilterType != FilterTypes.eFilterResonantLowpass2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoGainAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleGainAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterGainAccent(FilterElement, Number, 5);
                    break;
                case InstrKeywordType.eKeywordGainaccent6:
                    if ((FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ)
                        && (FilterType != FilterTypes.eFilterResonantLowpass)
                        && (FilterType != FilterTypes.eFilterResonantLowpass2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoGainAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleGainAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterGainAccent(FilterElement, Number, 6);
                    break;
                case InstrKeywordType.eKeywordGainaccent7:
                    if ((FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ)
                        && (FilterType != FilterTypes.eFilterResonantLowpass)
                        && (FilterType != FilterTypes.eFilterResonantLowpass2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoGainAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleGainAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterGainAccent(FilterElement, Number, 7);
                    break;
                case InstrKeywordType.eKeywordGainaccent8:
                    if ((FilterType != FilterTypes.eFilterParametricEQ)
                        && (FilterType != FilterTypes.eFilterParametricEQ2)
                        && (FilterType != FilterTypes.eFilterLowShelfEQ)
                        && (FilterType != FilterTypes.eFilterHighShelfEQ)
                        && (FilterType != FilterTypes.eFilterResonantLowpass)
                        && (FilterType != FilterTypes.eFilterResonantLowpass2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrFilterHasNoGainAccentX;
                    }
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleGainAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetSingleFilterGainAccent(FilterElement, Number, 8);
                    break;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* parse a track effect number.  if it's a formula, *FormulaOut is set to the */
        /* formula and *ConstantOut is zero, otherwise it is null and the constant is */
        /* returned in *ConstantOut*/
        public static BuildInstrErrors ParseTrackEffectNumber(
            BuildInstrumentContext Context,
            out int ErrorLine,
            out PcodeRec FormulaOut,
            out double ConstantOut)
        {
            TokenRec<InstrKeywordType> Token;
            double Sign = 1;

            /* NOTE: this function should try to be compatible with ParseNumber */

            FormulaOut = null;
            ConstantOut = Double.NaN;
            ErrorLine = -1;

        Again:
            Token = Context.Scanner.GetNextToken();
            switch (Token.GetTokenType())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedNumber;

                case TokenTypes.eTokenMinus:
                    Sign = -Sign;
                    goto Again;
                case TokenTypes.eTokenPlus:
                    goto Again;

                case TokenTypes.eTokenInteger:
                    ConstantOut = Sign * Token.GetTokenIntegerValue();
                    break;
                case TokenTypes.eTokenSingle:
                    ConstantOut = Sign * Token.GetTokenSingleValue();
                    break;
                case TokenTypes.eTokenDouble:
                    ConstantOut = Sign * Token.GetTokenDoubleValue();
                    break;

                case TokenTypes.eTokenString:
                    {
                        CompileErrors CompileError;
                        int CompileErrorLine;
                        DataTypes ReturnType;
                        Compiler.ASTExpression Expr;
                        PcodeRec FuncCode;

                        if (Sign < 0)
                        {
                            /* don't permit negation of formula */
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedNumber;
                        }

                        // Sometimes constant, sometimes a function. try as constant first, to reduce the number of
                        // assemblies being generated (in CIL mode - in pcode mode it doesn't cost much).
                        foreach (bool suppressCILEmission in new bool[] { true, false })
                        {
                            CompileError = Compiler.CompileSpecialFunction(
                                Context.CodeCenter,
                                TrackEffectFormulaArgsDefs,
                                out CompileErrorLine,
                                out ReturnType,
                                Token.GetTokenStringValue(),
                                suppressCILEmission,
                                out FuncCode,
                                out Expr);
                            if (CompileError != CompileErrors.eCompileNoError)
                            {
                                ErrorLine = CompileErrorLine + Context.Scanner.GetCurrentLineNumber() - 1;
                                return BuildInstrErrors.eBuildInstrEnvFormulaSyntaxError;
                            }
                            if (ReturnType != DataTypes.eDouble)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrEnvFormulaMustHaveTypeDouble;
                            }

                            if (Expr.Kind == Compiler.ExprKind.eExprOperand)
                            {
                                /* expression is compile-time constant */
                                switch (ReturnType)
                                {
                                    default:
                                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                        return BuildInstrErrors.eBuildInstrExpectedNumber;
                                    case DataTypes.eBoolean:
                                        ConstantOut = Sign * (Expr.InnerOperand.BooleanLiteralValue ? 1 : 0);
                                        break;
                                    case DataTypes.eInteger:
                                        ConstantOut = Sign * Expr.InnerOperand.IntegerLiteralValue;
                                        break;
                                    case DataTypes.eFloat:
                                        ConstantOut = Sign * Expr.InnerOperand.SingleLiteralValue;
                                        break;
                                    case DataTypes.eDouble:
                                        ConstantOut = Sign * Expr.InnerOperand.DoubleLiteralValue;
                                        break;
                                }

                                break; // stop with constant
                            }
                            else
                            {
                                /* expression is dynamic */
                                FormulaOut = FuncCode;

                                // fallthrough will iterate loop to generate non-suppressed function code
                            }
                        }
                    }
                    break;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* histogram */
        /*   <string> */
        /*   left | right | averagebefore | averageafter */
        /*   absval {} | smoothedabsval <number> | smoothedrms <number> */
        /*   logarithmic | linear */
        /*   min <number> */
        /*   max <number> */
        /*   numbins <integer> */
        /*   discardunders | nodiscardunders */
        /*   bars <integer> */
        /*   ; */
        public static BuildInstrErrors ParseHistogram(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            string IDString;
            HistogramSpecRec Histogram;
            double Number;

            /* histogram */

            /*   <string> */
            Error = ParseIdentifier(Context, out ErrorLine, out IDString);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            Histogram = NewHistogramSpec(IDString);
            AddHistogramToEffectSpecList(
                EffectList,
                Histogram,
                EnabledFlag);

            /*   left | right | averagebefore | averageafter */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedLeftRightAverageMax;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedLeftRightAverageMax;
                case InstrKeywordType.eKeywordLeft:
                    SetHistogramSpecChannelSelector(Histogram, HistogramChannelType.eHistogramLeft);
                    break;
                case InstrKeywordType.eKeywordRight:
                    SetHistogramSpecChannelSelector(Histogram, HistogramChannelType.eHistogramRight);
                    break;
                case InstrKeywordType.eKeywordAveragebefore:
                    SetHistogramSpecChannelSelector(Histogram, HistogramChannelType.eHistogramAverageBeforeFilter);
                    break;
                case InstrKeywordType.eKeywordAverageafter:
                    SetHistogramSpecChannelSelector(Histogram, HistogramChannelType.eHistogramAverageAfterFilter);
                    break;
                case InstrKeywordType.eKeywordMaxafter:
                    SetHistogramSpecChannelSelector(Histogram, HistogramChannelType.eHistogramMaxAfterFilter);
                    break;
            }

            /*   absval {} | smoothedabsval <number> | smoothedrms <number> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedAbsValOrSmoothedx;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedAbsValOrSmoothedx;
                case InstrKeywordType.eKeywordAbsval:
                    SetHistogramSpecPowerEstimatorMode(Histogram, HistogramPowerEstType.eHistogramAbsVal);
                    break;
                case InstrKeywordType.eKeywordSmoothedabsval:
                    SetHistogramSpecPowerEstimatorMode(Histogram, HistogramPowerEstType.eHistogramSmoothedAbsVal);
                    Error = ParseNumber(
                        Context,
                        out ErrorLine,
                        out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetHistogramSpecPowerEstimatorFilter(
                        Histogram,
                        Number);
                    break;
                case InstrKeywordType.eKeywordSmoothedrms:
                    SetHistogramSpecPowerEstimatorMode(Histogram, HistogramPowerEstType.eHistogramSmoothedRMS);
                    Error = ParseNumber(
                        Context,
                        out ErrorLine,
                        out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    SetHistogramSpecPowerEstimatorFilter(
                        Histogram,
                        Number);
                    break;
            }

            /*   logarithmic | linear */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedLogOrLin;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedLogOrLin;
                case InstrKeywordType.eKeywordLogarithmic:
                    SetHistogramSpecBinDistribution(
                        Histogram,
                        true);
                    break;
                case InstrKeywordType.eKeywordLinear:
                    SetHistogramSpecBinDistribution(
                        Histogram,
                        false);
                    break;
            }

            /*   min <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordMin))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedMin;
            }
            Error = ParseNumber(
                Context,
                out ErrorLine,
                out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (GetHistogramSpecBinDistribution(Histogram))
            {
                if (Number <= 0)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedMinMustBePositiveForLog;
                }
            }
            else
            {
                if (Number < 0)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedMinMustBeNonNegForLin;
                }
            }
            SetHistogramSpecBottom(
                Histogram,
                Number);

            /*   max <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordMax))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedMax;
            }
            Error = ParseNumber(
                Context,
                out ErrorLine,
                out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (Number < GetHistogramSpecBottom(Histogram))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedMaxMustBeGreaterThanMin;
            }
            SetHistogramSpecTop(
                Histogram,
                Number);

            /*   numbins <integer> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordNumbins))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedNumbins;
            }
            Error = ParseNumber(
                Context,
                out ErrorLine,
                out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (Number < 1)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedMustHaveAtLeastOneBin;
            }
            if (Number != Math.Floor(Number))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedBinMustBeInteger;
            }
            SetHistogramSpecNumBins(
                Histogram,
                (int)Number);

            /*   discardunders | nodiscardunders */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDiscardOrNoDiscard;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedDiscardOrNoDiscard;
                case InstrKeywordType.eKeywordDiscardunders:
                    SetHistogramSpecDiscardUnders(
                        Histogram,
                        true);
                    break;
                case InstrKeywordType.eKeywordNodiscardunders:
                    SetHistogramSpecDiscardUnders(
                        Histogram,
                        false);
                    break;
            }

            /*   bars <integer> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordBars))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedBars;
            }
            Error = ParseNumber(
                Context,
                out ErrorLine,
                out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (Number < 0)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedBarsCantBeNegative;
            }
            if (Number != Math.Floor(Number))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedBarsMustBeInteger;
            }
            SetHistogramSpecBarChartWidth(
                Histogram,
                (int)Number);

            /*   ; */

            return BuildInstrErrors.eBuildInstrNoError;
        }




        //
        //
        // BuildInstrument4
        //
        //


        /*  <compressor>          ::= estimatepower {absval | rms | peak} inputscaling <number> */
        /*                            outputscaling <number> normalpower */
        /*                            <number> threshpower <number> ratio <number> */
        /*                            filtercutoff <number> decayrate <number> */
        /*                            attackrate <number> limitingexcess <number> */
        /*                            <compressorattributelist> */
        /*  FOLLOW SET:   {;} */
        public static BuildInstrErrors ParseCompressor(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;
            PcodeRec Formula;
            CompressorSpecRec Compressor;

            Compressor = NewCompressorSpec();
            AddCompressorToEffectSpecList(EffectList, Compressor, EnabledFlag);

            /* estimatepower {absval | rms} */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordEstimatepower))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedEstimatePower;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedAbsValRMSOrPeak;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedAbsValRMSOrPeak;
                case InstrKeywordType.eKeywordAbsval:
                    PutCompressorPowerEstimatorMode(Compressor, CompressorPowerEstType.eCompressPowerAbsVal);
                    break;
                case InstrKeywordType.eKeywordRms:
                    PutCompressorPowerEstimatorMode(Compressor, CompressorPowerEstType.eCompressPowerRMS);
                    break;
                case InstrKeywordType.eKeywordPeak:
                    PutCompressorPowerEstimatorMode(Compressor, CompressorPowerEstType.eCompressPowerPeak);
                    break;
                case InstrKeywordType.eKeywordPeaklookahead:
                    PutCompressorPowerEstimatorMode(Compressor, CompressorPowerEstType.eCompressPowerpeaklookahead);
                    break;
            }

            /* inputscaling <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInputscaling))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInputscaling;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutCompressorInputGain(Compressor, Number, Formula);

            /* outputscaling <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputscaling))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputscaling;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutCompressorOutputGain(Compressor, Number, Formula);

            /* normalpower <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordNormalpower))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedNormalPower;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutCompressorNormalPower(Compressor, Number, Formula);

            /* threshpower <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordThreshpower))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedThreshPower;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutCompressorThreshPower(Compressor, Number, Formula);

            /* ratio <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordRatio))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedRatio;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutCompressorRatio(Compressor, Number, Formula);

            /* filtercutoff <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFiltercutoff))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilterCutoff;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutCompressorFilterFreq(Compressor, Number, Formula);

            /* decayrate <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDecayrate))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDecayRate;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutCompressorDecayRate(Compressor, Number, Formula);

            /* attackrate <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordAttackrate))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedAttackRate;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutCompressorAttackRate(Compressor, Number, Formula);

            /* limitingexcess <number> */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordLimitingexcess))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedLimitingExcess;
            }

            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutCompressorLimitingExcess(Compressor, Number, Formula);

            Error = ScanCompressorAttributes(Compressor, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ParseCompressorAttributes(
            CompressorSpecRec Compressor,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;

            ErrorLine = -1;

            /* inputscaling <number> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            Context.Scanner.UngetToken(Token);

            return ParseCompressorOneAttr(
                Compressor,
                Context,
                out ErrorLine,
                RequiredOnceOnly);
        }




        public static BuildInstrErrors ParseCompressorOneAttr(
            CompressorSpecRec Compressor,
            BuildInstrumentContext Context,
            out int ErrorLine,
            RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCompressorAttribute;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedCompressorAttribute;

                case InstrKeywordType.eKeywordInputaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorInputGainAccent(Compressor, Number, 1);
                    break;
                case InstrKeywordType.eKeywordInputaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorInputGainAccent(Compressor, Number, 2);
                    break;
                case InstrKeywordType.eKeywordInputaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorInputGainAccent(Compressor, Number, 3);
                    break;
                case InstrKeywordType.eKeywordInputaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorInputGainAccent(Compressor, Number, 4);
                    break;
                case InstrKeywordType.eKeywordInputaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorInputGainAccent(Compressor, Number, 5);
                    break;
                case InstrKeywordType.eKeywordInputaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorInputGainAccent(Compressor, Number, 6);
                    break;
                case InstrKeywordType.eKeywordInputaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorInputGainAccent(Compressor, Number, 8);
                    break;
                case InstrKeywordType.eKeywordInputaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleInputaccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorInputGainAccent(Compressor, Number, 8);
                    break;

                case InstrKeywordType.eKeywordOutputaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorOutputGainAccent(Compressor, Number, 1);
                    break;
                case InstrKeywordType.eKeywordOutputaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorOutputGainAccent(Compressor, Number, 2);
                    break;
                case InstrKeywordType.eKeywordOutputaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorOutputGainAccent(Compressor, Number, 3);
                    break;
                case InstrKeywordType.eKeywordOutputaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorOutputGainAccent(Compressor, Number, 4);
                    break;
                case InstrKeywordType.eKeywordOutputaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorOutputGainAccent(Compressor, Number, 5);
                    break;
                case InstrKeywordType.eKeywordOutputaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorOutputGainAccent(Compressor, Number, 6);
                    break;
                case InstrKeywordType.eKeywordOutputaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorOutputGainAccent(Compressor, Number, 7);
                    break;
                case InstrKeywordType.eKeywordOutputaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleOutputaccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorOutputGainAccent(Compressor, Number, 8);
                    break;

                case InstrKeywordType.eKeywordNormalpoweraccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorNormalPowerAccent(Compressor, Number, 1);
                    break;
                case InstrKeywordType.eKeywordNormalpoweraccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorNormalPowerAccent(Compressor, Number, 2);
                    break;
                case InstrKeywordType.eKeywordNormalpoweraccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorNormalPowerAccent(Compressor, Number, 3);
                    break;
                case InstrKeywordType.eKeywordNormalpoweraccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorNormalPowerAccent(Compressor, Number, 4);
                    break;
                case InstrKeywordType.eKeywordNormalpoweraccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorNormalPowerAccent(Compressor, Number, 5);
                    break;
                case InstrKeywordType.eKeywordNormalpoweraccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorNormalPowerAccent(Compressor, Number, 6);
                    break;
                case InstrKeywordType.eKeywordNormalpoweraccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorNormalPowerAccent(Compressor, Number, 7);
                    break;
                case InstrKeywordType.eKeywordNormalpoweraccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleNormalPowerAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorNormalPowerAccent(Compressor, Number, 8);
                    break;

                case InstrKeywordType.eKeywordThreshpoweraccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorThreshPowerAccent(Compressor, Number, 1);
                    break;
                case InstrKeywordType.eKeywordThreshpoweraccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorThreshPowerAccent(Compressor, Number, 2);
                    break;
                case InstrKeywordType.eKeywordThreshpoweraccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorThreshPowerAccent(Compressor, Number, 3);
                    break;
                case InstrKeywordType.eKeywordThreshpoweraccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorThreshPowerAccent(Compressor, Number, 4);
                    break;
                case InstrKeywordType.eKeywordThreshpoweraccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorThreshPowerAccent(Compressor, Number, 5);
                    break;
                case InstrKeywordType.eKeywordThreshpoweraccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorThreshPowerAccent(Compressor, Number, 6);
                    break;
                case InstrKeywordType.eKeywordThreshpoweraccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorThreshPowerAccent(Compressor, Number, 7);
                    break;
                case InstrKeywordType.eKeywordThreshpoweraccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleThreshPowerAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorThreshPowerAccent(Compressor, Number, 8);
                    break;

                case InstrKeywordType.eKeywordRatioaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleRatioAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorRatioAccent(Compressor, Number, 1);
                    break;
                case InstrKeywordType.eKeywordRatioaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleRatioAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorRatioAccent(Compressor, Number, 2);
                    break;
                case InstrKeywordType.eKeywordRatioaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleRatioAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorRatioAccent(Compressor, Number, 3);
                    break;
                case InstrKeywordType.eKeywordRatioaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleRatioAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorRatioAccent(Compressor, Number, 4);
                    break;
                case InstrKeywordType.eKeywordRatioaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleRatioAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorRatioAccent(Compressor, Number, 5);
                    break;
                case InstrKeywordType.eKeywordRatioaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleRatioAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorRatioAccent(Compressor, Number, 6);
                    break;
                case InstrKeywordType.eKeywordRatioaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleRatioAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorRatioAccent(Compressor, Number, 7);
                    break;
                case InstrKeywordType.eKeywordRatioaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleRatioAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorRatioAccent(Compressor, Number, 8);
                    break;

                case InstrKeywordType.eKeywordFiltercutoffaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorFilterFreqAccent(Compressor, Number, 1);
                    break;
                case InstrKeywordType.eKeywordFiltercutoffaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorFilterFreqAccent(Compressor, Number, 2);
                    break;
                case InstrKeywordType.eKeywordFiltercutoffaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorFilterFreqAccent(Compressor, Number, 3);
                    break;
                case InstrKeywordType.eKeywordFiltercutoffaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorFilterFreqAccent(Compressor, Number, 4);
                    break;
                case InstrKeywordType.eKeywordFiltercutoffaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorFilterFreqAccent(Compressor, Number, 5);
                    break;
                case InstrKeywordType.eKeywordFiltercutoffaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorFilterFreqAccent(Compressor, Number, 6);
                    break;
                case InstrKeywordType.eKeywordFiltercutoffaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorFilterFreqAccent(Compressor, Number, 7);
                    break;
                case InstrKeywordType.eKeywordFiltercutoffaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleFilterCutoffAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorFilterFreqAccent(Compressor, Number, 8);
                    break;

                case InstrKeywordType.eKeywordDecayrateaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDecayRateAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorDecayRateAccent(Compressor, Number, 1);
                    break;
                case InstrKeywordType.eKeywordDecayrateaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDecayRateAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorDecayRateAccent(Compressor, Number, 2);
                    break;
                case InstrKeywordType.eKeywordDecayrateaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDecayRateAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorDecayRateAccent(Compressor, Number, 3);
                    break;
                case InstrKeywordType.eKeywordDecayrateaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDecayRateAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorDecayRateAccent(Compressor, Number, 4);
                    break;
                case InstrKeywordType.eKeywordDecayrateaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDecayRateAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorDecayRateAccent(Compressor, Number, 5);
                    break;
                case InstrKeywordType.eKeywordDecayrateaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDecayRateAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorDecayRateAccent(Compressor, Number, 6);
                    break;
                case InstrKeywordType.eKeywordDecayrateaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDecayRateAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorDecayRateAccent(Compressor, Number, 7);
                    break;
                case InstrKeywordType.eKeywordDecayrateaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleDecayRateAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorDecayRateAccent(Compressor, Number, 8);
                    break;

                case InstrKeywordType.eKeywordAttackrateaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleAttackRateAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorAttackRateAccent(Compressor, Number, 1);
                    break;
                case InstrKeywordType.eKeywordAttackrateaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleAttackRateAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorAttackRateAccent(Compressor, Number, 2);
                    break;
                case InstrKeywordType.eKeywordAttackrateaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleAttackRateAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorAttackRateAccent(Compressor, Number, 3);
                    break;
                case InstrKeywordType.eKeywordAttackrateaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleAttackRateAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorAttackRateAccent(Compressor, Number, 4);
                    break;
                case InstrKeywordType.eKeywordAttackrateaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleAttackRateAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorAttackRateAccent(Compressor, Number, 5);
                    break;
                case InstrKeywordType.eKeywordAttackrateaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleAttackRateAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorAttackRateAccent(Compressor, Number, 6);
                    break;
                case InstrKeywordType.eKeywordAttackrateaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleAttackRateAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorAttackRateAccent(Compressor, Number, 7);
                    break;
                case InstrKeywordType.eKeywordAttackrateaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleAttackRateAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorAttackRateAccent(Compressor, Number, 8);
                    break;

                case InstrKeywordType.eKeywordLimitingexcessaccent1:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT1))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent1;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorLimitingExcessAccent(Compressor, Number, 1);
                    break;
                case InstrKeywordType.eKeywordLimitingexcessaccent2:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT2))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent2;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorLimitingExcessAccent(Compressor, Number, 2);
                    break;
                case InstrKeywordType.eKeywordLimitingexcessaccent3:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT3))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent3;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorLimitingExcessAccent(Compressor, Number, 3);
                    break;
                case InstrKeywordType.eKeywordLimitingexcessaccent4:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT4))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent4;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorLimitingExcessAccent(Compressor, Number, 4);
                    break;
                case InstrKeywordType.eKeywordLimitingexcessaccent5:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT5))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent5;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorLimitingExcessAccent(Compressor, Number, 5);
                    break;
                case InstrKeywordType.eKeywordLimitingexcessaccent6:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT6))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent6;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorLimitingExcessAccent(Compressor, Number, 6);
                    break;
                case InstrKeywordType.eKeywordLimitingexcessaccent7:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT7))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent7;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorLimitingExcessAccent(Compressor, Number, 7);
                    break;
                case InstrKeywordType.eKeywordLimitingexcessaccent8:
                    if (MarkOnceOnlyPresent(RequiredOnceOnly,
                        Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT8))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMultipleLimitingExcessAccent8;
                    }
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutCompressorLimitingExcessAccent(Compressor, Number, 8);
                    break;
            }

            return ParseCompressorAttributes(
                Compressor,
                Context,
                out ErrorLine,
                RequiredOnceOnly);
        }




        public static BuildInstrErrors ParseOscCompressor(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            CompressorSpecRec Compressor;

            Compressor = NewCompressorSpec();
            AddCompressorToEffectSpecList(EffectList, Compressor, EnabledFlag);

            /* estimatepower {absval | rms} */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordEstimatepower))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedEstimatePower;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedAbsValRMSOrPeak;
            }
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedAbsValRMSOrPeak;
                case InstrKeywordType.eKeywordAbsval:
                    PutCompressorPowerEstimatorMode(Compressor, CompressorPowerEstType.eCompressPowerAbsVal);
                    break;
                case InstrKeywordType.eKeywordRms:
                    PutCompressorPowerEstimatorMode(Compressor, CompressorPowerEstType.eCompressPowerRMS);
                    break;
                case InstrKeywordType.eKeywordPeak:
                    PutCompressorPowerEstimatorMode(Compressor, CompressorPowerEstType.eCompressPowerPeak);
                    break;
                case InstrKeywordType.eKeywordPeaklookahead:
                    PutCompressorPowerEstimatorMode(Compressor, CompressorPowerEstType.eCompressPowerpeaklookahead);
                    break;
            }

            /* inputscalingenvelope ( <envelope_definition> ) */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInputscalingenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInputScalingEnvelope;
            }
            Error = ScanEnvelopeSpec(GetCompressorInputGainEnvelope(Compressor), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* { inputscalinglfo ( <lfo_definition> ) } */
        InputScalingLFOPoint:
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordInputscalinglfo))
            {
                LFOSpecRec LFO;

                LFO = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(GetCompressorInputGainLFOList(Compressor), LFO);
                Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                goto InputScalingLFOPoint;
            }
            else if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputscalingenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInputScalingLfoOrOutputScalingEnvelope;
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* outputscalingenvelope ( <envelope_definition> ) */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputscalingenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputScalingEnvelope;
            }
            Error = ScanEnvelopeSpec(GetCompressorOutputGainEnvelope(Compressor), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* { outputscalinglfo ( <lfo_definition> ) } */
        OutputScalingLFOPoint:
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordOutputscalinglfo))
            {
                LFOSpecRec LFO;

                LFO = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(GetCompressorOutputGainLFOList(Compressor), LFO);
                Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                goto OutputScalingLFOPoint;
            }
            else if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordNormalpowerenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputScalingLfoOrNormalPowerEnvelope;
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* normalpowerenvelope ( <envelope_definition> ) */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordNormalpowerenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedNormalPowerEnvelope;
            }
            Error = ScanEnvelopeSpec(GetCompressorNormalPowerEnvelope(Compressor), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* { normalpowerlfo ( <lfo_definition> ) } */
        NormalPowerLFOPoint:
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordNormalpowerlfo))
            {
                LFOSpecRec LFO;

                LFO = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(GetCompressorNormalPowerLFOList(Compressor), LFO);
                Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                goto NormalPowerLFOPoint;
            }
            else if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordThreshpowerenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedNormalPowerLfoOrThreshPowerEnvelope;
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* threshpowerenvelope ( <envelope_definition> ) */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordThreshpowerenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedThreshPowerEnvelope;
            }
            Error = ScanEnvelopeSpec(GetCompressorThreshPowerEnvelope(Compressor), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* { threshpowerlfo ( <lfo_definition> ) } */
        ThreshPowerLFOPoint:
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordThreshpowerlfo))
            {
                LFOSpecRec LFO;

                LFO = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(GetCompressorThreshPowerLFOList(Compressor), LFO);
                Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                goto ThreshPowerLFOPoint;
            }
            else if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordRatioenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedThreshPowerLfoOrRatioEnvelope;
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* ratioenvelope ( <envelope_definition> ) */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordRatioenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedRatioEnvelope;
            }
            Error = ScanEnvelopeSpec(GetCompressorRatioEnvelope(Compressor), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* { ratiolfo ( <lfo_definition> ) } */
        RatioLFOPoint:
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordRatiolfo))
            {
                LFOSpecRec LFO;

                LFO = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(GetCompressorRatioLFOList(Compressor), LFO);
                Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                goto RatioLFOPoint;
            }
            else if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFiltercutoffenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedRatioLfoOrFilterCutoffEnvelope;
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* filtercutoffenvelope ( <envelope_definition> ) */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordFiltercutoffenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilterCutoffEnvelope;
            }
            Error = ScanEnvelopeSpec(GetCompressorFilterFreqEnvelope(Compressor),
                Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* { filtercutofflfo ( <lfo_definition> ) } */
        FilterCutoffLFOPoint:
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordFiltercutofflfo))
            {
                LFOSpecRec LFO;

                LFO = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(GetCompressorFilterFreqLFOList(Compressor), LFO);
                Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                goto FilterCutoffLFOPoint;
            }
            else if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDecayrateenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedFilterCutoffLfoOrDecayRateEnvelope;
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* decayrateenvelope ( <envelope_definition> ) */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDecayrateenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDecayRateEnvelope;
            }
            Error = ScanEnvelopeSpec(GetCompressorDecayRateEnvelope(Compressor), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* { decayratelfo ( <lfo_definition> ) } */
        DecayRateLFOPoint:
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordDecayratelfo))
            {
                LFOSpecRec LFO;

                LFO = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(GetCompressorDecayRateLFOList(Compressor), LFO);
                Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                goto DecayRateLFOPoint;
            }
            else if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordAttackrateenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDecayRateLfoOrAttackRateEnvelope;
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* attackrateenvelope ( <envelope_definition> ) */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordAttackrateenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedAttackRateEnvelope;
            }
            Error = ScanEnvelopeSpec(GetCompressorAttackRateEnvelope(Compressor), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* { Attackratelfo ( <lfo_definition> ) } */
        AttackRateLFOPoint:
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordAttackratelfo))
            {
                LFOSpecRec LFO;

                LFO = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(GetCompressorAttackRateLFOList(Compressor), LFO);
                Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                goto AttackRateLFOPoint;
            }
            else if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordLimitingexcessenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedAttackRateLfoOrLimitingExcessEnvelope;
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* limitingexcessenvelope ( <envelope_definition> ) */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordLimitingexcessenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedLimitingExcessEnvelope;
            }
            Error = ScanEnvelopeSpec(GetCompressorLimitingExcessEnvelope(Compressor), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* { limitingexcesslfo ( <lfo_definition> ) } */
        LimitingExcessLFOPoint:
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordLimitingexcesslfo))
            {
                LFOSpecRec LFO;

                LFO = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(GetCompressorLimitingExcessLFOList(Compressor), LFO);
                Error = ScanLfoSpec(LFO, Context, out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                goto LimitingExcessLFOPoint;
            }
            else if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedLimitingExcessLfoOrSemicolon;
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ParseAnalyzer(
            EffectSpecListRec OscEffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            string IDString;
            AnalyzerSpecRec Analyzer;

            Error = ParseIdentifier(Context, out ErrorLine, out IDString);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            Analyzer = NewAnalyzerSpec(IDString);
            AddAnalyzerToEffectSpecList(OscEffectList, Analyzer, EnabledFlag);

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
            }
            else if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordEstimatepower))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedSemicolonOrEstimatePower;
            }
            else
            {
                AnalyzerPowerEstType Method;
                double Number;

                /* get power filter cutoff */
                Error = ParseNumber(Context, out ErrorLine, out Number);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                /* get estimator method */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedAbsValOrRMS;
                }
                switch (Token.GetTokenKeywordTag())
                {
                    default:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedAbsValOrRMS;
                    case InstrKeywordType.eKeywordAbsval:
                        Method = AnalyzerPowerEstType.eAnalyzerPowerAbsVal;
                        break;
                    case InstrKeywordType.eKeywordRms:
                        Method = AnalyzerPowerEstType.eAnalyzerPowerRMS;
                        break;
                }
                /* remember stuff */
                AnalyzerSpecEnablePowerEstimator(Analyzer, Number, Method);
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* nlproc wavetable <identifier> inputscalingenvelope ( */
        /* <envelope_definition> ) <nlinputscalinglfolist> */
        /* outputscalingenvelope ( <envelope_definition> ) */
        /* <nloutputscalinglfolist> indexenvelope ( <envelope_definition> */
        /* ) <nlindexlfolist> */
        public static BuildInstrErrors ParseOscNLProc(
            EffectSpecListRec OscEffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            string WaveTableName;
            NonlinProcSpecRec NLProcSpec;

            /* wavetable */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWavetable))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedWavetable;
            }
            /* get wavetable name */
            Error = ParseIdentifier(Context, out ErrorLine, out WaveTableName);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            NLProcSpec = NewNonlinProcSpec(WaveTableName);
            AddNLProcToEffectSpecList(OscEffectList, NLProcSpec, EnabledFlag);
            /* inputscalingenvelope */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInputscalingenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInputScalingEnvelope;
            }
            /* ( <envelope_definition> ) */
            Error = ScanEnvelopeSpec(GetNLProcInputEnvelope(NLProcSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* <nlinputscalinglfolist> */
            Error = ParseNLInputScalingLFOList(GetNLProcInputLFO(NLProcSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* outputscalingenvelope */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputscalingenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputScalingEnvelope;
            }
            /* ( <envelope_definition> ) */
            Error = ScanEnvelopeSpec(GetNLProcOutputEnvelope(NLProcSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* <nloutputscalinglfolist> */
            Error = ParseNLOutputScalingLFOList(GetNLProcOutputLFO(NLProcSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* indexenvelope */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordIndexenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedIndexEnvelope;
            }
            /* ( <envelope_definition> ) */
            Error = ScanEnvelopeSpec(GetNLProcIndexEnvelope(NLProcSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* <nlindexlfolist> */
            Error = ParseNLIndexLFOList(GetNLProcIndexLFO(NLProcSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* filter ( <oscfilterelems> ) */
        public static BuildInstrErrors ParseOscFilterBank(
            EffectSpecListRec OscEffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            FilterSpecRec FilterSpec;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            FilterSpec = NewFilterSpec();
            AddFilterToEffectSpecList(OscEffectList, FilterSpec, EnabledFlag);

            Error = ParseOscFilterElems(FilterSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* delayline ( <oscdelayelems> ) */
        public static BuildInstrErrors ParseOscDelayLine(
            EffectSpecListRec OscEffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            BuildInstrErrors Error;
            DelayEffectRec DelayEffect;

            DelayEffect = NewDelayLineSpec();
            AddDelayToEffectSpecList(OscEffectList, DelayEffect, EnabledFlag);

            Error = ScanOscDelayElems(DelayEffect, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ParseDelayLine(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            BuildInstrErrors Error;
            DelayEffectRec DelayEffect;

            DelayEffect = NewDelayLineSpec();
            AddDelayToEffectSpecList(EffectList, DelayEffect, EnabledFlag);

            Error = ScanDelayElemList(DelayEffect, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ParseNLProc(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            string WaveTableName;
            NonlinProcSpecRec NLProcSpec;
            double Number;
            PcodeRec Formula;

            /* toss 'wavetable' */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWavetable))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedWavetable;
            }
            /* get wavetable name */
            Error = ParseIdentifier(Context, out ErrorLine, out WaveTableName);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            NLProcSpec = NewNonlinProcSpec(WaveTableName);
            AddNLProcToEffectSpecList(EffectList, NLProcSpec, EnabledFlag);
            /* toss 'inputscaling' */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInputscaling))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInputscaling;
            }
            /* get input scaling value */
            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutNLProcInputScaling(NLProcSpec, Number, Formula);
            /* toss 'outputscaling' */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputscaling))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputscaling;
            }
            /* get the output scaling value */
            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutNLProcOutputScaling(NLProcSpec, Number, Formula);
            /* toss 'wavetableindex' */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWavetableindex))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedWavetableindex;
            }
            /* get wave table index */
            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutNLProcWaveTableIndex(NLProcSpec, Number, Formula);
            /* parse attributes */
            Error = ScanNLAttributes(NLProcSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ParseFilterBank(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            FilterSpecRec FilterSpec;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            FilterSpec = NewFilterSpec();
            AddFilterToEffectSpecList(EffectList, FilterSpec, EnabledFlag);

            Error = ParseFilterList(FilterSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* vocoder */
        /* wavetable <identifier> maxbandcount <integer> order <integer> */
        /*   wavetableindex <number> */
        /*   outputgain <number> [ outputaccent{1-8} <number> | indexaccent{1-8} <number> ]* */
        /* follow set: { ; } */
        public static BuildInstrErrors ParseVocoder(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            string WaveTableName;
            VocoderSpecRec VocSpec;
            double Number;
            PcodeRec Formula;

            /* wavetable */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWavetable))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedWavetable;
            }

            /* identifier */
            Error = ParseIdentifier(Context, out ErrorLine, out WaveTableName);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            /* (allocate the thing) */
            VocSpec = NewVocoderSpec(WaveTableName);
            AddVocoderToEffectSpecList(EffectList, VocSpec, EnabledFlag);

            /* maxbandcount */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordMaxbandcount))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedMaxbandcount;
            }

            /* <integer> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenInteger)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInteger;
            }
            if (Token.GetTokenIntegerValue() < 1)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrMaxBandMustBeOneOrGreater;
            }
            PutVocoderMaxNumBands(VocSpec, Token.GetTokenIntegerValue());

            /* order */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOrder))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOrder;
            }

            /* <integer> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenInteger)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInteger;
            }
            if ((Token.GetTokenIntegerValue() < 2)
                || (Token.GetTokenIntegerValue() % 2 != 0))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrOrderMustBeNonNegativeEvenInteger;
            }
            PutVocoderFilterOrder(VocSpec, Token.GetTokenIntegerValue());

            /* wavetableindex */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWavetableindex))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedWavetableindex;
            }

            /* <number> */
            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutVocoderSpecWaveTableIndex(VocSpec, Number, Formula);

            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordInterpolate))
            {
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedOnOrOff;
                }
                switch (Token.GetTokenKeywordTag())
                {
                    default:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOnOrOff;
                    case InstrKeywordType.eKeywordOn:
                        VocoderSetEnableCrossWaveTableInterpolation(VocSpec, true);
                        break;
                    case InstrKeywordType.eKeywordOff:
                        VocoderSetEnableCrossWaveTableInterpolation(VocSpec, false);
                        break;
                }
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* outputgain */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputgain))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputgain;
            }

            /* <number> */
            Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            PutVocoderSpecOutputGain(VocSpec, Number, Formula);

            /* variant segment */
            Error = ScanVocoderAttributes(VocSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* [ outputaccent{1-8} <number> | indexaccent{1-8} <number> ] */
        public static BuildInstrErrors ParseVocoderAttributes(
                                                                    VocoderSpecRec VocSpec,
                                                                    BuildInstrumentContext Context,
                                                                    out int ErrorLine,
                                                                    RequiredOnceOnlyRec RequiredOnceOnly)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            double Number;
            bool StopFlag;

            ErrorLine = -1;

            StopFlag = false;
            while (!StopFlag)
            {
                /* outputaccent{1-8} or indexaccent{1-8} or ; */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                {
                    Context.Scanner.UngetToken(Token);
                    StopFlag = true;
                }
                else
                {
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOutputaccentIndexaccentOrSemicolon;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedOutputaccentIndexaccentOrSemicolon;

                        case InstrKeywordType.eKeywordOutputaccent1:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT1))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOutputaccent1;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecOutputGainAccent(VocSpec, Number, 1);
                            break;

                        case InstrKeywordType.eKeywordOutputaccent2:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT2))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOutputaccent2;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecOutputGainAccent(VocSpec, Number, 2);
                            break;

                        case InstrKeywordType.eKeywordOutputaccent3:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT3))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOutputaccent3;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecOutputGainAccent(VocSpec, Number, 3);
                            break;

                        case InstrKeywordType.eKeywordOutputaccent4:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT4))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOutputaccent4;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecOutputGainAccent(VocSpec, Number, 4);
                            break;

                        case InstrKeywordType.eKeywordOutputaccent5:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT5))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOutputaccent5;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecOutputGainAccent(VocSpec, Number, 5);
                            break;

                        case InstrKeywordType.eKeywordOutputaccent6:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT6))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOutputaccent6;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecOutputGainAccent(VocSpec, Number, 6);
                            break;

                        case InstrKeywordType.eKeywordOutputaccent7:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT7))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOutputaccent7;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecOutputGainAccent(VocSpec, Number, 7);
                            break;

                        case InstrKeywordType.eKeywordOutputaccent8:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT8))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOutputaccent8;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecOutputGainAccent(VocSpec, Number, 8);
                            break;

                        case InstrKeywordType.eKeywordIndexaccent1:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT1))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleIndexaccent1;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecIndexAccent(VocSpec, Number, 1);
                            break;

                        case InstrKeywordType.eKeywordIndexaccent2:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT2))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleIndexaccent2;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecIndexAccent(VocSpec, Number, 2);
                            break;

                        case InstrKeywordType.eKeywordIndexaccent3:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT3))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleIndexaccent3;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecIndexAccent(VocSpec, Number, 3);
                            break;

                        case InstrKeywordType.eKeywordIndexaccent4:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT4))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleIndexaccent4;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecIndexAccent(VocSpec, Number, 4);
                            break;

                        case InstrKeywordType.eKeywordIndexaccent5:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT5))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleIndexaccent5;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecIndexAccent(VocSpec, Number, 5);
                            break;

                        case InstrKeywordType.eKeywordIndexaccent6:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT6))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleIndexaccent6;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecIndexAccent(VocSpec, Number, 6);
                            break;

                        case InstrKeywordType.eKeywordIndexaccent7:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT7))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleIndexaccent7;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecIndexAccent(VocSpec, Number, 7);
                            break;

                        case InstrKeywordType.eKeywordIndexaccent8:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly,
                                Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT8))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleIndexaccent8;
                            }
                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutVocoderSpecIndexAccent(VocSpec, Number, 8);
                            break;
                    }
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* vocoder */
        /* wavetable <identifier> maxbandcount <integer> order <integer> indexenvelope ( */
        /* <envelope_definition> ) <vocindexlfolist> outputgainenvelope ( */
        /* <envelope_definition> ) <vocoutputgainlfolist> */
        /* follow set: { ; } */
        public static BuildInstrErrors ParseOscVocoder(
            EffectSpecListRec OscEffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            string WaveTableName;
            VocoderSpecRec VocSpec;

            /* wavetable */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWavetable))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedWavetable;
            }

            /* identifier */
            Error = ParseIdentifier(Context, out ErrorLine, out WaveTableName);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            /* (allocate the thing) */
            VocSpec = NewVocoderSpec(WaveTableName);
            AddVocoderToEffectSpecList(OscEffectList, VocSpec, EnabledFlag);

            /* maxbandcount */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordMaxbandcount))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedMaxbandcount;
            }

            /* <integer> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenInteger)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInteger;
            }
            if (Token.GetTokenIntegerValue() < 1)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrMaxBandMustBeOneOrGreater;
            }
            PutVocoderMaxNumBands(VocSpec, Token.GetTokenIntegerValue());

            /* order */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOrder))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOrder;
            }

            /* <integer> */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenInteger)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInteger;
            }
            if ((Token.GetTokenIntegerValue() < 2)
                || (Token.GetTokenIntegerValue() % 2 != 0))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrOrderMustBeNonNegativeEvenInteger;
            }
            PutVocoderFilterOrder(VocSpec, Token.GetTokenIntegerValue());

            /* indexenvelope */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordIndexenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedIndexEnvelope;
            }

            /* ( <envelope_definition> ) */
            Error = ScanEnvelopeSpec(GetVocoderSpecIndexEnvelope(VocSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            /* <vocindexlfolist> */
            Error = ParseVocIndexLFOList(GetVocoderSpecIndexLFO(VocSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordInterpolate))
            {
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedOnOrOff;
                }
                switch (Token.GetTokenKeywordTag())
                {
                    default:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedOnOrOff;
                    case InstrKeywordType.eKeywordOn:
                        VocoderSetEnableCrossWaveTableInterpolation(VocSpec, true);
                        break;
                    case InstrKeywordType.eKeywordOff:
                        VocoderSetEnableCrossWaveTableInterpolation(VocSpec, false);
                        break;
                }
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /* outputgainenvelope */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputgainenvelope))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedIndexlfoOrOutputgainenvelope;
            }

            /* ( <envelope_definition> ) */
            Error = ScanEnvelopeSpec(GetVocoderSpecOutputGainEnvelope(VocSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            /* <vocoutputgainlfolist> */
            Error = ParseVocOutputGainLFOList(GetVocoderSpecOutputGainLFO(VocSpec), Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            /* test ; */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOutputgainlfoOrSemicolon;
            }
            Context.Scanner.UngetToken(Token);

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* follow set: { outputgainenvelope } */
        public static BuildInstrErrors ParseVocIndexLFOList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                /* let caller handle error */
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordIndexlfo)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseVocIndexLFOList(LFOList, Context, out ErrorLine);
        }




        /* follow set: { ; } */
        public static BuildInstrErrors ParseVocOutputGainLFOList(
            LFOListSpecRec LFOList,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            LFOSpecRec LFOSpec;

            ErrorLine = -1;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                /* let caller handle error */
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordOutputgainlfo)
            {
                Context.Scanner.UngetToken(Token);
                return BuildInstrErrors.eBuildInstrNoError;
            }

            LFOSpec = NewLFOSpecifier();
            LFOListSpecAppendNewEntry(LFOList, LFOSpec);
            Error = ScanLfoSpec(LFOSpec, Context, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            return ParseVocOutputGainLFOList(LFOList, Context, out ErrorLine);
        }





        //
        //
        // BuildInstrument5
        //
        //


        /* network */
        /*  <fmsynth>             ::= ( <networkstmts> ) */
        /*  <networkstmts>        ::= <id> = wave ( <wavemembers> ) ; */
        /*                          | <id> (= | +=) <number> * <id> * <id> + <number> ; */
        /*                          | <id> = envelope () [lfo()...] ; */
        public static BuildInstrErrors ParseFMSynthNetwork(
            FMSynthSpecRec FMSynthSpec,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            BuildInstrErrors Error;
            TokenRec<InstrKeywordType> Token;
            string TargetIdentifier;
            bool AccumulateFlag;

            /* oparen */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            while (true)
            {
                /* the lvalue */
                TargetIdentifier = null;
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenIdentifier)
                {
                    TargetIdentifier = Token.GetTokenIdentifierString();
                }
                else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                    && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordLeft))
                {
                    TargetIdentifier = "left";
                }
                else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                    && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordRight))
                {
                    TargetIdentifier = "right";
                }
                else
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedIdentifier;
                }

                /* operator */
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenEqual)
                    && (Token.GetTokenType() != TokenTypes.eTokenPlusEqual))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedEqualOrPlusEqual;
                }
                AccumulateFlag = (Token.GetTokenType() == TokenTypes.eTokenPlusEqual);

                /* right hand side */
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                    && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordWave))
                {
                    /* wave */

                    if (AccumulateFlag)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrPlusEqualsNotAllowed;
                    }

                    Error = ParseFMSynthNetworkWaveStmt(
                        FMSynthSpec,
                        Context,
                        out ErrorLine,
                        TargetIdentifier);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                }
                else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                    && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordEnvelope))
                {
                    /* envelope */

                    if (AccumulateFlag)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrPlusEqualsNotAllowed;
                    }

                    Error = ParseFMSynthNetworkEnvelopeStmt(
                        FMSynthSpec,
                        Context,
                        out ErrorLine,
                        TargetIdentifier);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                }
                else
                {
                    /* muladd expr */

                    Context.Scanner.UngetToken(Token);

                    Error = ParseFMSynthNetworkMulAddStmt(
                        FMSynthSpec,
                        Context,
                        out ErrorLine,
                        AccumulateFlag,
                        TargetIdentifier);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                }

                /* ; */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }

                /* cparen */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
                {
                    break;
                }

                Context.Scanner.UngetToken(Token);
            }

            FMSynthOptimize(FMSynthSpec);

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* <id> (= | +=) <number> * <id> [* <id>] + <number> ; */
        public static BuildInstrErrors ParseFMSynthNetworkMulAddStmt(
            FMSynthSpecRec FMSynthSpec,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool Accumulate,
            string TargetIdentifier)
        {
            BuildInstrErrors Error;
            TokenRec<InstrKeywordType> Token;
            double Factor = 1;
            double Addend = 0;
            string Factor2Identifier = null;
            string SourceIdentifier = null;
            double AddendSign = 1;
            FMSynthStmtMulAddRec MulAddStmt;

            /* first number */
            Error = ParseNumber(
                Context,
                out ErrorLine,
                out Factor);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return BuildInstrErrors.eBuildInstrExpectedWaveVarOrEnvelope;
            }

            /* see if they stopped */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                /* ; */
                Context.Scanner.UngetToken(Token);

                Addend = Factor;
                Factor = 1;
            }
            else
            {
                /* * */
                /* Token = Context.Scanner.GetNextToken(); */
                if (Token.GetTokenType() != TokenTypes.eTokenStar)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedAsterisk;
                }

                /* <identifier> */
                SourceIdentifier = null;
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenIdentifier)
                {
                    SourceIdentifier = Token.GetTokenIdentifierString();
                }
                else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                    && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordLeft))
                {
                    SourceIdentifier = "left";
                }
                else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                    && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordRight))
                {
                    SourceIdentifier = "right";
                }
                else
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedIdentifier;
                }

                /* * <id>    optional */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenStar)
                {
                    Context.Scanner.UngetToken(Token);
                }
                else
                {
                    Factor2Identifier = null;
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() == TokenTypes.eTokenIdentifier)
                    {
                        Factor2Identifier = Token.GetTokenIdentifierString();
                    }
                    else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                        && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordLeft))
                    {
                        Factor2Identifier = "left";
                    }
                    else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                        && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordRight))
                    {
                        Factor2Identifier = "right";
                    }
                    else
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedIdentifier;
                    }
                }

                /* see if they stopped */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                {
                    /* ; */
                    Context.Scanner.UngetToken(Token);
                }
                else
                {
                    /* + or - */
                    /* Token = Context.Scanner.GetNextToken(); */
                    if ((Token.GetTokenType() != TokenTypes.eTokenPlus)
                        && (Token.GetTokenType() != TokenTypes.eTokenMinus))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedPlusOrMinus;
                    }
                    AddendSign = (Token.GetTokenType() == TokenTypes.eTokenPlus) ? 1 : -1;

                    /* second number */
                    Error = ParseNumber(
                        Context,
                        out ErrorLine,
                        out Addend);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                }
            }


            /* modify AST */

            FMSynthAddStatement(FMSynthSpec, FMSynthStmtType.eFMSynthMuladd);
            MulAddStmt = FMSynthGetMulAddStatement(
                FMSynthSpec,
                FMSynthGetNumStatements(FMSynthSpec) - 1);

            if (SourceIdentifier != null)
            {
                FMSynthMulAddStmtPutDataSource(
                    MulAddStmt,
                    SourceIdentifier);
            }

            FMSynthMulAddStmtPutDataTarget(
                MulAddStmt,
                TargetIdentifier);

            FMSynthMulAddStmtPutFactor(
                MulAddStmt,
                Factor);
            FMSynthMulAddStmtPutAddend(
                MulAddStmt,
                Addend * AddendSign);
            if (Accumulate)
            {
                FMSynthMulAddStmtPutDataSource2(
                    MulAddStmt,
                    TargetIdentifier);
            }

            if (Factor2Identifier != null)
            {
                FMSynthMulAddStmtPutDataFactor2(
                    MulAddStmt,
                    Factor2Identifier);
            }


            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* wave */
        /*     samplelist ... ; */
        /*   | freqmultiplier <number> ; */
        /*   | freqdivisor <integer> ; */
        /*   | freqadder <number> ; */
        /*   | phaseadd <identifier> gain <identifier> ; */
        /*   | index <identifier> ; */
        public static BuildInstrErrors ParseFMSynthNetworkWaveStmt(
            FMSynthSpecRec FMSynthSpec,
            BuildInstrumentContext Context,
            out int ErrorLine,
            string TargetIdentifier)
        {
            BuildInstrErrors Error;
            TokenRec<InstrKeywordType> Token;
            double Number;
            FMSynthStmtWaveRec WaveSpec;
            RequiredOnceOnlyRec WaveStmtRequiredOnceOnly;

            ErrorLine = -1;

            WaveStmtRequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(WaveStmtRequiredOnceOnly, Req.FMWAVEDEFINITION_ONCEONLY_FREQMULTIPLIER);
            AddOnceOnlyCode(WaveStmtRequiredOnceOnly, Req.FMWAVEDEFINITION_ONCEONLY_FREQDIVISOR);
            AddOnceOnlyCode(WaveStmtRequiredOnceOnly, Req.FMWAVEDEFINITION_ONCEONLY_FREQADDER);
            AddOnceOnlyCode(WaveStmtRequiredOnceOnly, Req.FMWAVEDEFINITION_ONCEONLY_PHASEADD);
            AddOnceOnlyCode(WaveStmtRequiredOnceOnly, Req.FMWAVEDEFINITION_ONCEONLY_SAMPLELIST);
            AddOnceOnlyCode(WaveStmtRequiredOnceOnly, Req.FMWAVEDEFINITION_ONCEONLY_INDEX);

            FMSynthAddStatement(
                FMSynthSpec,
                FMSynthStmtType.eFMSynthWave);
            WaveSpec = FMSynthGetWaveStatement(
                FMSynthSpec,
                FMSynthGetNumStatements(FMSynthSpec) - 1);
            FMSynthWaveStmtPutTarget(
                WaveSpec,
                TargetIdentifier);

            /* oparen */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            while (true)
            {
                /* terminating close paren */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
                {
                    break;
                }

                /* option keyword */
                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedWaveComponent;
                }
                switch (Token.GetTokenKeywordTag())
                {
                    default:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedWaveComponent;

                    case InstrKeywordType.eKeywordSamplelist:
                        if (MarkOnceOnlyPresent(
                            WaveStmtRequiredOnceOnly,
                            Req.FMWAVEDEFINITION_ONCEONLY_SAMPLELIST))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrFMWaveSamplelistAlreadySpecified;
                        }

                        /* open paren */
                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedOpenParen;
                        }

                        Error = ParseSamplelistDefinition(
                            FMSynthWaveStmtGetSampleIntervalList(WaveSpec),
                            Context,
                            out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }

                        /* close paren */
                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedCloseParen;
                        }
                        break;

                    case InstrKeywordType.eKeywordFreqmultiplier:
                        if (MarkOnceOnlyPresent(
                            WaveStmtRequiredOnceOnly,
                            Req.FMWAVEDEFINITION_ONCEONLY_FREQMULTIPLIER))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrFMWaveFreqMultiplierAlreadySpecified;
                        }

                        Error = ParseNumber(
                            Context,
                            out ErrorLine,
                            out Number);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                        FMSynthWaveStmtPutFrequencyMultiplier(
                            WaveSpec,
                            Number);
                        break;

                    case InstrKeywordType.eKeywordFreqdivisor:
                        if (MarkOnceOnlyPresent(
                            WaveStmtRequiredOnceOnly,
                            Req.FMWAVEDEFINITION_ONCEONLY_FREQDIVISOR))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrFMWaveFreqDivisorAlreadySpecified;
                        }

                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedInteger;
                        }
                        FMSynthWaveStmtPutFrequencyDivisor(
                            WaveSpec,
                            Token.GetTokenIntegerValue());
                        break;

                    case InstrKeywordType.eKeywordFreqadder:
                        if (MarkOnceOnlyPresent(
                            WaveStmtRequiredOnceOnly,
                            Req.FMWAVEDEFINITION_ONCEONLY_FREQADDER))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrFMWaveFreqAdderAlreadySpecified;
                        }

                        Error = ParseNumber(
                            Context,
                            out ErrorLine,
                            out Number);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                        FMSynthWaveStmtPutFrequencyAdder(
                            WaveSpec,
                            Number);
                        break;

                    case InstrKeywordType.eKeywordPhaseadd:
                        if (MarkOnceOnlyPresent(
                            WaveStmtRequiredOnceOnly,
                            Req.FMWAVEDEFINITION_ONCEONLY_PHASEADD))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrFMWavePhaseAddAlreadySpecified;
                        }

                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() != TokenTypes.eTokenIdentifier)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedIdentifier;
                        }
                        FMSynthWaveStmtPutPhaseSource(
                            WaveSpec,
                            Token.GetTokenIdentifierString());

                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                        {
                            Context.Scanner.UngetToken(Token);
                        }
                        else
                        {
                            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordGain))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedGainOrSemicolon;
                            }

                            Token = Context.Scanner.GetNextToken();
                            if (Token.GetTokenType() != TokenTypes.eTokenIdentifier)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedIdentifier;
                            }
                            FMSynthWaveStmtPutPhaseGainSource(
                                WaveSpec,
                                Token.GetTokenIdentifierString());
                        }
                        break;

                    case InstrKeywordType.eKeywordIndexenvelope:
                        if (MarkOnceOnlyPresent(
                            WaveStmtRequiredOnceOnly,
                            Req.FMWAVEDEFINITION_ONCEONLY_INDEX))
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrFMWaveIndexAlreadySpecified;
                        }

                        Error = ScanEnvelopeSpec(
                            FMSynthWaveStmtGetIndexEnvelope(WaveSpec),
                            Context,
                            out ErrorLine);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }

                    LFOParsePoint:

                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                        {
                            Context.Scanner.UngetToken(Token);
                        }
                        else
                        {
                            LFOSpecRec LFOSpec;

                            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordIndexlfo))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedIndexLFOOrSemicolon;
                            }

                            LFOSpec = NewLFOSpecifier();
                            LFOListSpecAppendNewEntry(
                                FMSynthWaveStmtGetIndexLFOList(WaveSpec),
                                LFOSpec);

                            Error = ScanLfoSpec(
                                LFOSpec,
                                Context,
                                out ErrorLine);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }

                            goto LFOParsePoint;
                        }
                        break;
                }

                /* ; */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* ( ... ) [lfo ()]* ; */
        public static BuildInstrErrors ParseFMSynthNetworkEnvelopeStmt(
            FMSynthSpecRec FMSynthSpec,
            BuildInstrumentContext Context,
            out int ErrorLine,
            string TargetIdentifier)
        {
            BuildInstrErrors Error;
            TokenRec<InstrKeywordType> Token;
            FMSynthStmtEnvelopeRec EnvSpec;

            FMSynthAddStatement(
                FMSynthSpec,
                FMSynthStmtType.eFMSynthEnvelope);
            EnvSpec = FMSynthGetEnvelopeStatement(
                FMSynthSpec,
                FMSynthGetNumStatements(FMSynthSpec) - 1);
            FMSynthEnvelopeStmtPutDataTarget(
                EnvSpec,
                TargetIdentifier);

            Error = ScanEnvelopeSpec(
                FMSynthEnvelopeStmtGetEnvelope(EnvSpec),
                Context,
                out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

        /* semicolon or indexlfo */
        LFOParsePoint:
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
            {
                Context.Scanner.UngetToken(Token);
            }
            else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordLfo))
            {
                LFOSpecRec LFOSpec;

                LFOSpec = NewLFOSpecifier();
                LFOListSpecAppendNewEntry(
                    FMSynthEnvelopeStmtGetLFOList(EnvSpec),
                    LFOSpec);

                Error = ScanLfoSpec(
                    LFOSpec,
                    Context,
                    out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                goto LFOParsePoint;
            }
            else
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedLFOOrSemicolon;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /* usereffect */
        /*  <usereffect>          ::= ( [workspaces {integerarray | floatarray | doublearray}* ;] */
        /*                            [initfunc <string> ;] */
        /*                            datafunc <string> ; */
        /*                            [param <number> [accent{1-8} <number>] ;]* ) */
        public static BuildInstrErrors ParseTrackUserEffect(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            string InitFuncIdentifier = null;
            List<string> DataFuncIdentifiers = new List<string>();
            BuildInstrErrors Error;
            UserEffectSpecRec UserEffect;
            List<DataTypes> workspaces = new List<DataTypes>();

            ErrorLine = -1;

            /* oparen */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            bool noOversampling = false;
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordOversamplingdisabled))
            {
                noOversampling = true;

                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /*  <usereffect>          ::= [workspaces ... ;] */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || ((Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWorkspaces)
                    && (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInitfunc)
                    && (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDatafunc)))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedWorkspaces;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWorkspaces)
            {
                Context.Scanner.UngetToken(Token);
            }
            else
            {
                while (true)
                {
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                    {
                        break;
                    }
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedWorkspaceElementOrSemicolon;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedWorkspaceElementOrSemicolon;
                        case InstrKeywordType.eKeywordIntegerarray:
                            workspaces.Add(DataTypes.eArrayOfInteger);
                            break;
                        case InstrKeywordType.eKeywordFloatarray:
                            workspaces.Add(DataTypes.eArrayOfFloat);
                            break;
                        case InstrKeywordType.eKeywordDoublearray:
                            workspaces.Add(DataTypes.eArrayOfDouble);
                            break;
                    }
                }
            }

            /*  <usereffect>          ::= [initfunc <string> ;] */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || ((Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInitfunc)
                    && (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDatafunc)))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInitfunc;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInitfunc)
            {
                Context.Scanner.UngetToken(Token);
            }
            else
            {
                Token = Context.Scanner.GetNextToken();
                Error = ParseGlobalIdentifierString(Context, Token, out ErrorLine, out InitFuncIdentifier);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return BuildInstrErrors.eBuildInstrExpectedInitfuncString;
                }

                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }
            }

            /*                            datafunc {<string>}+ ; */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDatafunc))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDatafunc;
            }
            while (true)
            {
                string DataFuncIdentifier;

                Token = Context.Scanner.GetNextToken();
                Error = ParseGlobalIdentifierString(Context, Token, out ErrorLine, out DataFuncIdentifier);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return BuildInstrErrors.eBuildInstrExpectedDatafuncString;
                }

                DataFuncIdentifiers.Add(DataFuncIdentifier);

                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                {
                    break;
                }
                Context.Scanner.UngetToken(Token);
            }

            UserEffect = NewUserEffectSpec(
                InitFuncIdentifier,
                DataFuncIdentifiers.ToArray(),
                workspaces.ToArray());
            AddUserEffectToEffectSpecList(EffectList, UserEffect, EnabledFlag);

            SetUserEffectSpecNoOversampling(UserEffect, noOversampling);

            /*                            [param [smoothed] <number> [accent{1-8} <number>] ;]* */
            while (true)
            {
                double Number;
                PcodeRec Formula;
                int Index;
                RequiredOnceOnlyRec RequiredOnceOnly;

                /* cparen | param */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
                {
                    break; /* done - stop loop */
                }

                /* param */
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordParam))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedParamOrCParen;
                }

                AddUserEffectSpecParam(UserEffect, out Index);

                // [smoothed]
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                    && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordSmoothed))
                {
                    SetUserEffectSpecParamSmoothed(UserEffect, Index, true/*smoothed*/);
                }
                else
                {
                    Context.Scanner.UngetToken(Token);
                }

                RequiredOnceOnly = NewRequiredOnceOnly();
                AddOnceOnlyCode(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT1);
                AddOnceOnlyCode(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT2);
                AddOnceOnlyCode(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT3);
                AddOnceOnlyCode(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT4);
                AddOnceOnlyCode(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT5);
                AddOnceOnlyCode(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT6);
                AddOnceOnlyCode(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT7);
                AddOnceOnlyCode(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT8);

                /* number */
                Error = ParseTrackEffectNumber(Context, out ErrorLine, out Formula, out Number);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }
                PutUserEffectSpecParam(UserEffect, Index, Number, Formula);

                while (true)
                {
                    int AccentIndex;

                    /* accent[1-8] | ; */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                    {
                        break; /* stop inner loop */
                    }

                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedAccentOrSemicolon;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedAccentOrSemicolon;
                        case InstrKeywordType.eKeywordAccent1:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT1))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleAccent1;
                            }
                            AccentIndex = 1;
                            break;
                        case InstrKeywordType.eKeywordAccent2:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT2))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleAccent2;
                            }
                            AccentIndex = 2;
                            break;
                        case InstrKeywordType.eKeywordAccent3:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT3))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleAccent3;
                            }
                            AccentIndex = 3;
                            break;
                        case InstrKeywordType.eKeywordAccent4:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT4))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleAccent4;
                            }
                            AccentIndex = 4;
                            break;
                        case InstrKeywordType.eKeywordAccent5:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT5))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleAccent5;
                            }
                            AccentIndex = 5;
                            break;
                        case InstrKeywordType.eKeywordAccent6:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT6))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleAccent6;
                            }
                            AccentIndex = 6;
                            break;
                        case InstrKeywordType.eKeywordAccent7:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT7))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleAccent7;
                            }
                            AccentIndex = 7;
                            break;
                        case InstrKeywordType.eKeywordAccent8:
                            if (MarkOnceOnlyPresent(RequiredOnceOnly, Req.USEREFFECTPARAM_ONCEONLY_ACCENT8))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleAccent8;
                            }
                            AccentIndex = 8;
                            break;
                    }

                    /* number */
                    Error = ParseNumber(Context, out ErrorLine, out Number);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                    PutUserEffectSpecParamAccent(UserEffect, Index, Number, AccentIndex);
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        /*  <usereffect>          ::= ( [workspaces {integerarray | floatarray | doublearray}* ;] */
        /*                            [initfunc <string> ;] */
        /*                            datafunc <string> ; */
        /*                            param envelope () [lfo()...] ; ) */
        public static BuildInstrErrors ParseOscUserEffect(
            EffectSpecListRec EffectList,
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag)
        {
            TokenRec<InstrKeywordType> Token;
            string InitFuncIdentifier = null;
            List<string> DataFuncIdentifiers = new List<string>();
            BuildInstrErrors Error;
            UserEffectSpecRec UserEffect;
            List<DataTypes> workspaces = new List<DataTypes>();

            ErrorLine = -1;

            /* oparen */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            bool noOversampling = false;
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordOversamplingdisabled))
            {
                noOversampling = true;

                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }
            }
            else
            {
                Context.Scanner.UngetToken(Token);
            }

            /*  <usereffect>          ::= [workspaces ... ;] */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || ((Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWorkspaces)
                    && (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInitfunc)
                    && (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDatafunc)))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedWorkspaces;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordWorkspaces)
            {
                Context.Scanner.UngetToken(Token);
            }
            else
            {
                while (true)
                {
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                    {
                        break;
                    }
                    if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedWorkspaceElementOrSemicolon;
                    }
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrExpectedWorkspaceElementOrSemicolon;
                        case InstrKeywordType.eKeywordIntegerarray:
                            workspaces.Add(DataTypes.eArrayOfInteger);
                            break;
                        case InstrKeywordType.eKeywordFloatarray:
                            workspaces.Add(DataTypes.eArrayOfFloat);
                            break;
                        case InstrKeywordType.eKeywordDoublearray:
                            workspaces.Add(DataTypes.eArrayOfDouble);
                            break;
                    }
                }
            }

            /*  <usereffect>          ::= [initfunc <string> ;] */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || ((Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInitfunc)
                    && (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDatafunc)))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedInitfunc;
            }
            if (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordInitfunc)
            {
                Context.Scanner.UngetToken(Token);
            }
            else
            {
                Token = Context.Scanner.GetNextToken();
                Error = ParseGlobalIdentifierString(Context, Token, out ErrorLine, out InitFuncIdentifier);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }
            }

            /*                            datafunc <string> ; */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordDatafunc))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedDatafunc;
            }
            while (true)
            {
                string DataFuncIdentifier;

                Token = Context.Scanner.GetNextToken();
                Error = ParseGlobalIdentifierString(Context, Token, out ErrorLine, out DataFuncIdentifier);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return BuildInstrErrors.eBuildInstrExpectedDatafuncString;
                }

                DataFuncIdentifiers.Add(DataFuncIdentifier);

                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                {
                    break;
                }
                Context.Scanner.UngetToken(Token);
            }

            UserEffect = NewUserEffectSpec(
                InitFuncIdentifier,
                DataFuncIdentifiers.ToArray(),
                workspaces.ToArray());
            AddUserEffectToEffectSpecList(EffectList, UserEffect, EnabledFlag);

            SetUserEffectSpecNoOversampling(UserEffect, noOversampling);

            /*                            param envelope () [lfo()...] ; ) */
            while (true)
            {
                int Index;

                /* cparen | param */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
                {
                    break; /* done - stop loop */
                }

                /* param */
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordParam))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedParamOrCParen;
                }

                AddUserEffectSpecParam(UserEffect, out Index);

                // [smoothed]
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                    && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordSmoothed))
                {
                    SetUserEffectSpecParamSmoothed(UserEffect, Index, true/*smoothed*/);
                }
                else
                {
                    Context.Scanner.UngetToken(Token);
                }

                /* envelope */
                Token = Context.Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordEnvelope))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedEnvelope;
                }

                Error = ScanEnvelopeSpec(
                    GetUserEffectSpecParamEnvelope(UserEffect, Index),
                    Context,
                    out ErrorLine);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    return Error;
                }

                while (true)
                {
                    LFOSpecRec LFOSpec;

                    /* lfo | ; */
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                    {
                        break; /* stop inner loop */
                    }

                    /* lfo */
                    if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                        || (Token.GetTokenKeywordTag() != InstrKeywordType.eKeywordLfo))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrExpectedLFOOrSemicolon;
                    }

                    LFOSpec = NewLFOSpecifier();
                    LFOListSpecAppendNewEntry(
                        GetUserEffectSpecParamLFO(UserEffect, Index),
                        LFOSpec);

                    Error = ScanLfoSpec(
                        LFOSpec,
                        Context,
                        out ErrorLine);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        //
        //
        // BuildInstrumentUtils
        //
        //

        public static BuildInstrErrors ScanEnvelopeSpec(
            EnvelopeRec Envelope,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            RequiredOnceOnlyRec EnvelopeRequiredOnceOnly;

            ErrorLine = -1;

            /* open paren or constant value */
            Token = Context.Scanner.GetNextToken();
            switch (Token.GetTokenType())
            {
                default:
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedOpenParen;

                case TokenTypes.eTokenOpenParen:
                    break;

                case TokenTypes.eTokenInteger:
                    EnvelopeSetConstantShortcut(Envelope, Token.GetTokenIntegerValue());
                    return BuildInstrErrors.eBuildInstrNoError;
                case TokenTypes.eTokenSingle:
                    EnvelopeSetConstantShortcut(Envelope, Token.GetTokenSingleValue());
                    return BuildInstrErrors.eBuildInstrNoError;
                case TokenTypes.eTokenDouble:
                    EnvelopeSetConstantShortcut(Envelope, Token.GetTokenDoubleValue());
                    return BuildInstrErrors.eBuildInstrNoError;
            }

            EnvelopeRequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVELOPEDEFINITION_ONCEONLY_TOTALSCALING);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVELOPEDEFINITION_ONCEONLY_POINTS);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVELOPEDEFINITION_ONCEONLY_PITCHCONTROL);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVELOPEDEFINITION_ONCEONLY_FORMULA);
            Error = ParseEnvelopeDefinition(
                Envelope,
                Context,
                out ErrorLine,
                EnvelopeRequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* no definitions required for envelopes */
            if (AnyUnmarkedRequireds(EnvelopeRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(EnvelopeRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                }
            }

            /* close paren */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanLfoSpec(
            LFOSpecRec LFO,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            RequiredOnceOnlyRec LFORequiredOnceOnly;

            /* open paren */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            /* parse the low frequency operator */
            LFORequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(LFORequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_FREQENVELOPE);
            AddOnceOnlyCode(LFORequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_OSCILLATORTYPE);
            AddOnceOnlyCode(LFORequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_MODULATIONTYPE);
            AddOnceOnlyCode(LFORequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_ADDINGMODE);
            AddOnceOnlyCode(LFORequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_FILTER);
            AddOnceOnlyCode(LFORequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_SAMPLEHOLD);
            AddRequiredOnceOnlyCode(LFORequiredOnceOnly, Req.LFODEFINITION_REQUIREDONCEONLY_AMPENVELOPE);
#if LFO_LOOPENV // TODO: experimental - looped-envelope lfo
            AddOnceOnlyCode(LFORequiredOnceOnly, Req.LFODEFINITION_REQUIREDONCEONLY_LOOPENVELOPE);
#endif
            AddOnceOnlyCode(LFORequiredOnceOnly, Req.LFODEFINITION_ONCEONLY_INTERPOLATE);
            Error = ParseLfoDefinition(
                LFO,
                Context,
                out ErrorLine,
                LFORequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (AnyUnmarkedRequireds(LFORequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(LFORequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case Req.LFODEFINITION_REQUIREDONCEONLY_AMPENVELOPE:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredAmpEnvelope;
                }
            }

            if (LFOSpecGetEnableCrossWaveTableInterpolationExplicitlySet(LFO))
            {
                if (LFOSpecGetOscillatorType(LFO) != LFOOscTypes.eLFOWaveTable)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrLFOInterpolateOnlyAppliesToWaveTable;
                }
            }

            /* close paren */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanInstrument(
            InstrumentRec Instrument,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            RequiredOnceOnlyRec RequiredOnceOnly;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            RequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(RequiredOnceOnly, Req.INSTRLIST_ONCEONLY_LOUDNESS);
            AddRequiredCode(RequiredOnceOnly, Req.INSTRLIST_REQUIRED_OSCILLATOR);
            MarkPresent(RequiredOnceOnly, Req.INSTRLIST_REQUIRED_OSCILLATOR); /* allow no oscillators, now that we have sequencing */
            Error = ParseInstrList(
                Instrument,
                Context,
                out ErrorLine,
                RequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (AnyUnmarkedRequireds(RequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(RequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case Req.INSTRLIST_REQUIRED_OSCILLATOR:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredOscillator;
                }
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanOscillatorDefinition(
            OscillatorRec Oscillator,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            RequiredOnceOnlyRec OscillatorRequiredOnceOnly;

            /* eat the open paren */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            /* parse oscillator */
            OscillatorRequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_LOUDNESS);
            AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQMULTIPLIER);
            AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQDIVISOR);
            AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_INDEXENVELOPE);
            AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_STEREOBIAS);
            AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_TIMEDISPLACEMENT);
            AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_SURROUNDBIAS);
            AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQADDER);
            AddRequiredOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_TYPE);
            AddRequiredOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_LOUDNESSENVELOPE);
            AddRequiredOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_SAMPLELIST);
            AddRequiredOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFSAMPRATE);
            AddRequiredOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFCOMPRESS);
            AddRequiredOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFEXPAND);
            AddRequiredOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFENVELOPE);
            AddRequiredOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_NETWORK);
            AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_INTERPOLATE);
            Error = ParseOscillatorDefinition(
                Oscillator,
                Context,
                out ErrorLine,
                OscillatorRequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (OscillatorGetWhatKindItIs(Oscillator) != OscillatorTypes.eOscillatorFOF)
            {
                /* cancel FOF requirements for non-FOF oscillators */
                CancelRequireCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFSAMPRATE);
                CancelRequireCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFCOMPRESS);
                CancelRequireCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFEXPAND);
                CancelRequireCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFENVELOPE);
            }
            if (OscillatorGetWhatKindItIs(Oscillator) != OscillatorTypes.eOscillatorFMSynth)
            {
                CancelRequireCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_NETWORK);
            }
            if ((OscillatorGetWhatKindItIs(Oscillator) == OscillatorTypes.eOscillatorAlgorithm)
                || (OscillatorGetWhatKindItIs(Oscillator) == OscillatorTypes.eOscillatorFMSynth))
            {
                /* algorithm doesn't need sample list */
                CancelRequireCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_SAMPLELIST);
            }
            if ((OscillatorGetWhatKindItIs(Oscillator) == OscillatorTypes.eOscillatorAlgorithm)
                || (OscillatorGetWhatKindItIs(Oscillator) == OscillatorTypes.eOscillatorSampled)
                || (OscillatorGetWhatKindItIs(Oscillator) == OscillatorTypes.eOscillatorPluggable))
            {
                if (OscillatorGetEnableCrossWaveTableInterpolationExplicitlySet(Oscillator))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrInterpolateNotMeaningfulForThatOscillatorType;
                }
            }
            if (AnyUnmarkedRequireds(OscillatorRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(OscillatorRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_TYPE:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredOscillatorType;
                    case Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_LOUDNESSENVELOPE:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredOscillatorLoudnessEnvelope;
                    case Req.OSCILLATORDEFINITION_REQUIREDONCEONLY_SAMPLELIST:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredOscillatorSampleList;
                    case Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFSAMPRATE:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredOscillatorFOFSampRate;
                    case Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFCOMPRESS:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredOscillatorFOFCompress;
                    case Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFEXPAND:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredOscillatorFOFExpand;
                    case Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_FOFENVELOPE:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredOscillatorFOFEnvelope;
                    case Req.OSCILLATORDEFINITION_MAYBEREQUIREDONCEONLY_NETWORK:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredOscillatorNetwork;
                }
            }

            /* eat the close paren */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanOscDelayElems(
            DelayEffectRec DelayEffect,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            RequiredOnceOnlyRec DelayLineRequiredOnceOnly;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            DelayLineRequiredOnceOnly = NewRequiredOnceOnly();
            AddRequiredOnceOnlyCode(DelayLineRequiredOnceOnly, Req.DELAYEFFECT_REQUIREDONCEONLY_MAXDELAYTIME);
            Error = ParseOscDelayElems(
                DelayEffect,
                Context,
                out ErrorLine,
                DelayLineRequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (AnyUnmarkedRequireds(DelayLineRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(DelayLineRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case Req.DELAYEFFECT_REQUIREDONCEONLY_MAXDELAYTIME:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredDelayLineMaxTime;
                }
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanNLAttributes(
            NonlinProcSpecRec NLProcSpec,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            BuildInstrErrors Error;
            RequiredOnceOnlyRec NLProcRequiredOnceOnly;

            NLProcRequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT1);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT2);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT3);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT4);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT5);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT6);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT7);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INPUTACCENT8);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT1);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT2);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT3);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT4);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT5);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT6);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT7);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OUTPUTACCENT8);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT1);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT2);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT3);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT4);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT5);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT6);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT7);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_INDEXACCENT8);
            AddOnceOnlyCode(NLProcRequiredOnceOnly, Req.NLPROCEFFECTATTR_ONCEONLY_OVERFLOWMODE);
            Error = ParseNLAttributes(
                NLProcSpec,
                Context,
                out ErrorLine,
                NLProcRequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* no definitions required for envelopes */
            if (AnyUnmarkedRequireds(NLProcRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(NLProcRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanDelayElemList(
            DelayEffectRec DelayEffect,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            TokenRec<InstrKeywordType> Token;
            BuildInstrErrors Error;
            RequiredOnceOnlyRec DelayLineRequiredOnceOnly;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            DelayLineRequiredOnceOnly = NewRequiredOnceOnly();
            AddRequiredOnceOnlyCode(DelayLineRequiredOnceOnly, Req.DELAYEFFECT_REQUIREDONCEONLY_MAXDELAYTIME);
            Error = ParseDelayElemList(
                DelayEffect,
                Context,
                out ErrorLine,
                DelayLineRequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            if (AnyUnmarkedRequireds(DelayLineRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(DelayLineRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case Req.DELAYEFFECT_REQUIREDONCEONLY_MAXDELAYTIME:
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        return BuildInstrErrors.eBuildInstrMissingRequiredDelayLineMaxTime;
                }
            }

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanTapAttributes(
            DelayTapRec Tap,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            BuildInstrErrors Error;
            RequiredOnceOnlyRec TapRequiredOnceOnly;

            TapRequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT1);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT2);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT3);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT4);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT5);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT6);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT7);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SOURCEACCENT8);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT1);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT2);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT3);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT4);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT5);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT6);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT7);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_TARGETACCENT8);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT1);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT2);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT3);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT4);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT5);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT6);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT7);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_SCALEACCENT8);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_LOWPASSCUTOFF);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT1);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT2);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT3);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT4);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT5);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT6);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT7);
            AddOnceOnlyCode(TapRequiredOnceOnly, Req.DELAYEFFECTATTR_ONCEONLY_CUTOFFACCENT8);
            Error = ParseTapAttributes(
                Tap,
                Context,
                out ErrorLine,
                TapRequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* no definitions required for delay line tap attributes */
            if (AnyUnmarkedRequireds(TapRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(TapRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanFilterAttributes(
            OneFilterRec FilterElement,
            BuildInstrumentContext Context,
            out int ErrorLine,
            FilterTypes FilterType)
        {
            BuildInstrErrors Error;
            RequiredOnceOnlyRec FilterRequiredOnceOnly;

            FilterRequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT1);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT2);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT3);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT4);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT5);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT6);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT7);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_FREQACCENT8);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT1);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT2);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT3);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT4);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT5);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT6);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT7);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_BANDWIDTHACCENT8);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALING);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT1);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT2);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT3);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT4);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT5);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT6);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT7);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_OUTPUTSCALINGACCENT8);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT1);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT2);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT3);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT4);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT5);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT6);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT7);
            AddOnceOnlyCode(FilterRequiredOnceOnly, Req.FILTEREFFECTATTR_ONCEONLY_GAINACCENT8);
            Error = ParseFilterAttributes(
                FilterElement,
                Context,
                out ErrorLine,
                FilterRequiredOnceOnly,
                FilterType);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* no definitions required for filter attributes */
            if (AnyUnmarkedRequireds(FilterRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(FilterRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanEnvAttributes(
            EnvelopeRec Envelope,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            BuildInstrErrors Error;
            RequiredOnceOnlyRec EnvelopeRequiredOnceOnly;

            EnvelopeRequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT1);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT2);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT3);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT4);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT5);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT6);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT7);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPACCENT8);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_AMPFREQ);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT1);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT2);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT3);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT4);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT5);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT6);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT7);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEACCENT8);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_RATEFREQ);
            AddOnceOnlyCode(EnvelopeRequiredOnceOnly, Req.ENVPOINTDEFINITION_ONCEONLY_CURVE);
            Error = ParseEnvAttributes(
                Envelope,
                Context,
                out ErrorLine,
                EnvelopeRequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* no definitions required for envelope attributes */
            if (AnyUnmarkedRequireds(EnvelopeRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(EnvelopeRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanCompressorAttributes(
            CompressorSpecRec Compressor,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            BuildInstrErrors Error;
            RequiredOnceOnlyRec CompressorRequiredOnceOnly;

            CompressorRequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT1);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT2);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT3);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT4);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT5);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT6);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT7);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_INPUTACCENT8);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT1);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT2);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT3);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT4);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT5);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT6);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT7);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_OUTPUTACCENT8);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT1);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT2);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT3);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT4);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT5);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT6);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT7);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_NORMALPOWERACCENT8);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT1);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT2);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT3);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT4);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT5);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT6);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT7);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_THRESHPOWERACCENT8);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT1);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT2);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT3);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT4);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT5);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT6);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT7);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_RATIOACCENT8);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT1);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT2);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT3);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT4);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT5);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT6);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT7);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_FILTERCUTOFFACCENT8);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT1);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT2);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT3);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT4);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT5);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT6);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT7);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_DECAYRATEACCENT8);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT1);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT2);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT3);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT4);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT5);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT6);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT7);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_ATTACKRATEACCENT8);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT1);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT2);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT3);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT4);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT5);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT6);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT7);
            AddOnceOnlyCode(CompressorRequiredOnceOnly, Req.COMPRESSOR_ONCEONLY_LIMITINGEXCESSACCENT8);
            Error = ParseCompressorAttributes(
                Compressor,
                Context,
                out ErrorLine,
                CompressorRequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* no definitions required for envelope attributes */
            if (AnyUnmarkedRequireds(CompressorRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(CompressorRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public static BuildInstrErrors ScanVocoderAttributes(
            VocoderSpecRec Vocoder,
            BuildInstrumentContext Context,
            out int ErrorLine)
        {
            BuildInstrErrors Error;
            RequiredOnceOnlyRec VocoderRequiredOnceOnly;

            VocoderRequiredOnceOnly = NewRequiredOnceOnly();
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT1);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT2);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT3);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT4);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT5);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT6);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT7);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_OUTPUTACCENT8);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT1);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT2);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT3);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT4);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT5);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT6);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT7);
            AddOnceOnlyCode(VocoderRequiredOnceOnly, Req.VOCODEREFFECTATTR_ONCEONLY_INDEXACCENT8);
            Error = ParseVocoderAttributes(
                Vocoder,
                Context,
                out ErrorLine,
                VocoderRequiredOnceOnly);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }
            /* no definitions required for envelope attributes */
            if (AnyUnmarkedRequireds(VocoderRequiredOnceOnly))
            {
                switch (GetFirstUnmarkedRequired(VocoderRequiredOnceOnly))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                }
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }




        public class RequiredOnceOnlyNodeRec
        {
            /* link for making list */
            public RequiredOnceOnlyNodeRec Next;

            /* code type */
            public bool IsRequired;
            public bool IsOnceOnly;

            /* code value */
            public Req Value;

            /* has this one been marked before */
            public bool Marked;
        }

        public class RequiredOnceOnlyRec
        {
            /* list of codes and their status */
            public RequiredOnceOnlyNodeRec NodeList;
        }

        /* create a new required/onceonly tracking thing */
        public static RequiredOnceOnlyRec NewRequiredOnceOnly()
        {
            return new RequiredOnceOnlyRec();
        }

        /* register a new required ID code */
        public static void AddRequiredCode(RequiredOnceOnlyRec RequiredOnceOnly, Req IDCode)
        {
#if DEBUG
            /* make sure code does not already exist */
            RequiredOnceOnlyNodeRec Scan = RequiredOnceOnly.NodeList;
            while (Scan != null)
            {
                if (Scan.Value == IDCode)
                {
                    // code already in list
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                Scan = Scan.Next;
            }
#endif

            RequiredOnceOnlyNodeRec NewNode = new RequiredOnceOnlyNodeRec();
            NewNode.IsRequired = true;
            NewNode.IsOnceOnly = false;
            NewNode.Value = IDCode;
            NewNode.Marked = false;

            NewNode.Next = RequiredOnceOnly.NodeList;
            RequiredOnceOnly.NodeList = NewNode;
        }

        /* register a new once only ID code */
        public static void AddOnceOnlyCode(RequiredOnceOnlyRec RequiredOnceOnly, Req IDCode)
        {
#if DEBUG
            /* make sure code does not already exist */
            RequiredOnceOnlyNodeRec Scan = RequiredOnceOnly.NodeList;
            while (Scan != null)
            {
                if (Scan.Value == IDCode)
                {
                    // code already in list
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                Scan = Scan.Next;
            }
#endif

            /* create new node */
            RequiredOnceOnlyNodeRec NewNode = new RequiredOnceOnlyNodeRec();
            NewNode.IsRequired = false;
            NewNode.IsOnceOnly = true;
            NewNode.Value = IDCode;
            NewNode.Marked = false;


            NewNode.Next = RequiredOnceOnly.NodeList;
            RequiredOnceOnly.NodeList = NewNode;
        }

        /* register a new required and once only ID code */
        public static void AddRequiredOnceOnlyCode(RequiredOnceOnlyRec RequiredOnceOnly, Req IDCode)
        {
#if DEBUG
            /* make sure code does not already exist */
            RequiredOnceOnlyNodeRec Scan = RequiredOnceOnly.NodeList;
            while (Scan != null)
            {
                if (Scan.Value == IDCode)
                {
                    // code already in list
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                Scan = Scan.Next;
            }
#endif

            /* create new node */
            RequiredOnceOnlyNodeRec NewNode = new RequiredOnceOnlyNodeRec();
            NewNode.IsRequired = true;
            NewNode.IsOnceOnly = true;
            NewNode.Value = IDCode;
            NewNode.Marked = false;

            NewNode.Next = RequiredOnceOnly.NodeList;
            RequiredOnceOnly.NodeList = NewNode;
        }

        /* cancel a requirement */
        public static void CancelRequireCode(RequiredOnceOnlyRec RequiredOnceOnly, Req IDCode)
        {
            RequiredOnceOnlyNodeRec Scan = RequiredOnceOnly.NodeList;
            while (Scan != null)
            {
                if (Scan.Value == IDCode)
                {
#if DEBUG
                    if (!Scan.IsRequired)
                    {
                        // code wasn't required
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
#endif
                    Scan.IsRequired = false;
                    return;
                }
                Scan = Scan.Next;
            }
#if DEBUG
            // code not on list
            Debug.Assert(false);
            throw new ArgumentException();
#endif
        }

        /* mark a required code as present */
        public static void MarkPresent(RequiredOnceOnlyRec RequiredOnceOnly, Req IDCode)
        {
            RequiredOnceOnlyNodeRec Scan = RequiredOnceOnly.NodeList;
            while (Scan != null)
            {
                if (Scan.Value == IDCode)
                {
#if DEBUG
                    if (Scan.IsOnceOnly)
                    {
                        // code is once only
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
#endif
                    Scan.Marked = true;
                    return;
                }
                Scan = Scan.Next;
            }
#if DEBUG
            // code not in list
            Debug.Assert(false);
            throw new ArgumentException();
#endif
        }

        /* mark a once only code as present -- returns true if violation occurs */
        public static bool MarkOnceOnlyPresent(RequiredOnceOnlyRec RequiredOnceOnly, Req IDCode)
        {
            RequiredOnceOnlyNodeRec Scan = RequiredOnceOnly.NodeList;
            while (Scan != null)
            {
                if (Scan.Value == IDCode)
                {
                    if (Scan.Marked)
                    {
                        return true;
                    }
                    Scan.Marked = true;
                    return false;
                }
                Scan = Scan.Next;
            }
#if DEBUG
            // code not in list
            Debug.Assert(false);
#endif
            throw new ArgumentException();
        }

        /* return true if any required codes have not been marked */
        public static bool AnyUnmarkedRequireds(RequiredOnceOnlyRec RequiredOnceOnly)
        {
            RequiredOnceOnlyNodeRec Scan = RequiredOnceOnly.NodeList;
            while (Scan != null)
            {
                if (Scan.IsRequired && !Scan.Marked)
                {
                    return true;
                }
                Scan = Scan.Next;
            }
            return false;
        }

        /* return the ID code of the first required code that hasn't been marked */
        public static Req GetFirstUnmarkedRequired(RequiredOnceOnlyRec RequiredOnceOnly)
        {
            RequiredOnceOnlyNodeRec Scan = RequiredOnceOnly.NodeList;
            while (Scan != null)
            {
                if (Scan.IsRequired && !Scan.Marked)
                {
                    return Scan.Value;
                }
                Scan = Scan.Next;
            }
#if DEBUG
            // no unmarked requires
            Debug.Assert(false);
#endif
            throw new ArgumentException();
        }




        public static BuildInstrErrors ParsePluggableTrackProcessor(
            BuildInstrumentContext Context,
            out int ErrorLine,
            bool EnabledFlag,
            string classId,
            out PluggableTrackSpec spec)
        {
            BuildInstrErrors Error;
            TokenRec<InstrKeywordType> Token;


            ErrorLine = -1;
            spec = null;


            IPluggableProcessorFactory classObject;
            if (!QueryPluggableProcessorClass(classId, out classObject))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrReferencedUnknownPluggableProcessor;
            }
            if ((classObject.Roles & PluggableRole.Effect) == 0)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrPluggableNotCapableOfEffect;
            }

            // oparen
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            KeyValuePair<string, string[]>[] configs;
            Error = ParsePluggableConfigs(Context, classObject, out configs, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            IPluggableProcessorTemplate templateObject;
            Error = classObject.Create(
                configs,
                PluggableRole.Effect,
                out templateObject,
                out Context.ErrorExtraMessage);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return Error;
            }

            spec = new PluggableTrackSpec(templateObject);
            spec.Enabled = EnabledFlag;


            // TODO: better error messaging in this section
            // sub-item parsing loop
            while (true)
            {
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
                {
                    Context.Scanner.UngetToken(Token);
                    break;
                }

                string paramId;
                Error = ParseGlobalIdentifierString(
                    Context,
                    Token,
                    out ErrorLine,
                    out paramId);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    Context.ErrorExtraMessage = String.Format("must be one of: {0}", GetPluggableParameterListForError(templateObject)); // add contextually helpful description
                    return Error;
                }

                int paramIndex = spec.FindParameter(paramId);
                if (paramIndex < 0)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    Context.ErrorExtraMessage = String.Format("must be one of: {0}", GetPluggableParameterListForError(templateObject)); // add contextually helpful description
                    return BuildInstrErrors.eBuildInstrReferencedUnknownPluggableProcessor;
                }
                if (spec.IsParameterSpecified(paramIndex))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrPluggableProcessorMultiplyReferenced;
                }


                PluggableParameterType type = spec.GetParameterBaseType(paramIndex);
                Debug.Assert((type & PluggableParameterType.TypeMask) == type);
                switch (type)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case PluggableParameterType.StaticInteger:
                        {
                            int value;

                            if (spec.IsIntegerParameterEnumerated(paramIndex))
                            {
                                string enumIdentifier;
                                Error = ParseGlobalIdentifierString(
                                    Context,
                                    Token,
                                    out ErrorLine,
                                    out enumIdentifier);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    Context.ErrorExtraMessage = GetPluggableParameterEnumValueListForError(templateObject, paramIndex); // add contextually helpful description
                                    return Error;
                                }
                                Error = GetPluggableEnumValueForEnumIdentifier(
                                    templateObject,
                                    paramIndex,
                                    enumIdentifier,
                                    out value);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    Context.ErrorExtraMessage = GetPluggableParameterEnumValueListForError(templateObject, paramIndex); // add contextually helpful description
                                    return Error;
                                }
                                spec.SetStaticIntegerParameter(paramIndex, value);
                            }
                            else
                            {
                                double number;

                                Error = ParseNumber(Context, out ErrorLine, out number);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }
                                value = (int)number;
                                if (number != value)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrExpectedInteger;
                                }
                            }

                            string errorMessage;
                            if (!spec.PluggableTemplate.StaticRangeCheck(paramIndex, value, out errorMessage))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                Context.ErrorExtraMessage = errorMessage;
                                return BuildInstrErrors.eBuildInstrExpectedInteger;
                            }

                            spec.SetStaticIntegerParameter(paramIndex, value);
                        }
                        break;

                    case PluggableParameterType.StaticString:
                        string stringLiteral;
                        Error = ParseIdentifier(Context, out ErrorLine, out stringLiteral);
                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                        {
                            return Error;
                        }
                        spec.SetStaticStringParameter(paramIndex, stringLiteral);
                        break;

                    case PluggableParameterType.StaticDouble:
                    case PluggableParameterType.DynamicDouble:
                        {
                            double value;
                            PcodeRec function;
                            AccentRec accents = new AccentRec();

                            Error = ParseTrackEffectNumber(
                                Context,
                                out ErrorLine,
                                out function,
                                out value);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }

                            bool accent1 = false;
                            bool accent2 = false;
                            bool accent3 = false;
                            bool accent4 = false;
                            bool accent5 = false;
                            bool accent6 = false;
                            bool accent7 = false;
                            bool accent8 = false;
                            while (true)
                            {
                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                                {
                                    Context.Scanner.UngetToken(Token);
                                    break;
                                }
                                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrExpectedAccentOrSemicolon;
                                }
                                switch (Token.GetTokenKeywordTag())
                                {
                                    default:
                                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                        return BuildInstrErrors.eBuildInstrExpectedAccentOrSemicolon;
                                    case InstrKeywordType.eKeywordAccent1:
                                        if (accent1)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent1;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent0);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent1 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent2:
                                        if (accent2)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent2;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent1);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent2 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent3:
                                        if (accent3)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent3;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent2);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent3 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent4:
                                        if (accent4)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent4;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent3);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent4 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent5:
                                        if (accent5)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent5;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent4);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent5 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent6:
                                        if (accent6)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent6;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent5);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent6 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent7:
                                        if (accent7)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent7;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent6);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent7 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent8:
                                        if (accent8)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent8;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent7);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent8 = true;
                                        break;
                                }
                            }

                            if (type == PluggableParameterType.StaticDouble)
                            {
                                spec.SetStaticDoubleParameter(
                                    paramIndex,
                                    new PluggableEvaluableParam(
                                        value,
                                        accents,
                                        function));
                            }
                            else
                            {
                                spec.SetDynamicDoubleParameter(
                                    paramIndex,
                                    new PluggableEvaluableParam(
                                        value,
                                        accents,
                                        function));
                            }
                        }
                        break;
                }

                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }
            }


            // cparen
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            // check for missing required parameters
            for (int i = 0; i < spec.ParameterCount; i++)
            {
                if (!spec.IsParameterOptional(i) && !spec.IsParameterSpecified(i))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    Context.ErrorExtraMessage = String.Format("'{0}'", templateObject.ParametersDefinition[i].ParserName);
                    return BuildInstrErrors.eBuildInstrPluggableRequiredParameterNotSpecified;
                }
            }


            return BuildInstrErrors.eBuildInstrNoError;
        }

        // double-duty as oscillator and osceffect parser
        public static BuildInstrErrors ParsePluggableOscProcessor(
            BuildInstrumentContext Context,
            OscillatorRec Oscillator, // null for osceffect
            out int ErrorLine,
            bool EnabledFlag,
            string classId,
            PluggableRole role,
            out PluggableOscSpec spec)
        {
            BuildInstrErrors Error;
            TokenRec<InstrKeywordType> Token;


            ErrorLine = -1;
            spec = null;


            IPluggableProcessorFactory classObject;
            if (!QueryPluggableProcessorClass(classId, out classObject))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrReferencedUnknownPluggableProcessor;
            }
            if ((classObject.Roles & role) == 0)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                switch (role)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case PluggableRole.Effect:
                        return BuildInstrErrors.eBuildInstrPluggableNotCapableOfEffect;
                    case PluggableRole.Oscillator:
                        return BuildInstrErrors.eBuildInstrPluggableNotCapableOfOscillator;
                }
            }

            // oparen
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedOpenParen;
            }

            KeyValuePair<string, string[]>[] configs;
            Error = ParsePluggableConfigs(Context, classObject, out configs, out ErrorLine);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                return Error;
            }

            IPluggableProcessorTemplate templateObject;
            Error = classObject.Create(
                configs,
                role,
                out templateObject,
                out Context.ErrorExtraMessage);
            if (Error != BuildInstrErrors.eBuildInstrNoError)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return Error;
            }

            spec = new PluggableOscSpec(templateObject);
            spec.Enabled = EnabledFlag;


            // TODO: better error messaging in this section
            // sub-item parsing loop
            RequiredOnceOnlyRec OscillatorRequiredOnceOnly = null;
            if (Oscillator != null)
            {
                OscillatorRequiredOnceOnly = NewRequiredOnceOnly();
                AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_LOUDNESS);
                AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQMULTIPLIER);
                AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQDIVISOR);
                AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_STEREOBIAS);
                AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_TIMEDISPLACEMENT);
                AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_SURROUNDBIAS);
                AddOnceOnlyCode(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQADDER);
            }
            while (true)
            {
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
                {
                    Context.Scanner.UngetToken(Token);
                    break;
                }

                if ((Oscillator != null) && (Token.GetTokenType() == TokenTypes.eTokenKeyword))
                {
                    double Number;

                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            break;

                        case InstrKeywordType.eKeywordLoudnessfactor:
                            if (MarkOnceOnlyPresent(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_LOUDNESS))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOscLoudnessFactor;
                            }

                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutOscillatorNewOutputLoudness(Oscillator, Number);

                            goto SemicolonAndContinue;

                        case InstrKeywordType.eKeywordFreqmultiplier:
                            if (MarkOnceOnlyPresent(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQMULTIPLIER))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOscFreqMultiplier;
                            }

                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutOscillatorNewFrequencyFactors(Oscillator, Number,
                                OscillatorGetFrequencyDivisor(Oscillator));

                            goto SemicolonAndContinue;

                        case InstrKeywordType.eKeywordFreqdivisor:
                            if (MarkOnceOnlyPresent(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQDIVISOR))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOscFreqDivisor;
                            }

                            Token = Context.Scanner.GetNextToken();
                            if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrExpectedInteger;
                            }
                            PutOscillatorNewFrequencyFactors(Oscillator,
                                OscillatorGetFrequencyMultiplier(Oscillator), Token.GetTokenIntegerValue());

                            goto SemicolonAndContinue;

                        case InstrKeywordType.eKeywordFreqadder:
                            if (MarkOnceOnlyPresent(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_FREQADDER))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOscFreqAdder;
                            }

                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            PutOscillatorFrequencyAdder(Oscillator, Number);

                            goto SemicolonAndContinue;

                        case InstrKeywordType.eKeywordStereobias:
                            if (MarkOnceOnlyPresent(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_STEREOBIAS))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOscStereoBias;
                            }

                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            OscillatorPutStereoBias(Oscillator, Number);

                            goto SemicolonAndContinue;

                        case InstrKeywordType.eKeywordDisplacement:
                            if (MarkOnceOnlyPresent(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_TIMEDISPLACEMENT))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOscDisplacement;
                            }

                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            OscillatorPutTimeDisplacement(Oscillator, Number);

                            goto SemicolonAndContinue;

                        case InstrKeywordType.eKeywordSurroundbias:
                            if (MarkOnceOnlyPresent(OscillatorRequiredOnceOnly, Req.OSCILLATORDEFINITION_ONCEONLY_SURROUNDBIAS))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return BuildInstrErrors.eBuildInstrMultipleOscSurroundBias;
                            }

                            Error = ParseNumber(Context, out ErrorLine, out Number);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            OscillatorPutSurroundBias(Oscillator, Number);

                            goto SemicolonAndContinue;

                        case InstrKeywordType.eKeywordEffect:
                            Error = ParseOscillatorEffect(GetOscillatorEffectList(Oscillator), Context, out ErrorLine);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }
                            goto SemicolonAndContinue;
                    }
                }

                string paramId;
                Error = ParseGlobalIdentifierString(
                    Context,
                    Token,
                    out ErrorLine,
                    out paramId);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    Context.ErrorExtraMessage = String.Format("must be one of: {0}", GetPluggableParameterListForError(templateObject)); // add contextually helpful description
                    return Error;
                }

                int paramIndex = spec.FindParameter(paramId);
                if (paramIndex < 0)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();

                    int configIndex = Array.FindIndex(configs, delegate (KeyValuePair<string, string[]> candidate) { return String.Equals(candidate.Key, paramId); });
                    if (configIndex >= 0)
                    {
                        return BuildInstrErrors.eBuildInstrPluggableConfigParamsMustComeBeforePluggableEffectParams;
                    }
                    else
                    {
                        Context.ErrorExtraMessage = String.Format("must be one of: {0}", GetPluggableParameterListForError(templateObject)); // add contextually helpful description
                        return BuildInstrErrors.eBuildInstrExpectedPluggableParameterIdentifier;
                    }
                }
                if (spec.IsParameterSpecified(paramIndex))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrPluggableProcessorMultiplyReferenced;
                }

                PluggableParameterType type = spec.GetParameterBaseType(paramIndex);
                Debug.Assert((type & PluggableParameterType.TypeMask) == type);
                switch (type)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case PluggableParameterType.StaticInteger:
                        {
                            int value;

                            if (spec.IsIntegerParameterEnumerated(paramIndex))
                            {
                                string enumIdentifier;
                                Error = ParseGlobalIdentifierString(
                                    Context,
                                    Token,
                                    out ErrorLine,
                                    out enumIdentifier);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    Context.ErrorExtraMessage = GetPluggableParameterEnumValueListForError(templateObject, paramIndex); // add contextually helpful description
                                    return Error;
                                }

                                Error = GetPluggableEnumValueForEnumIdentifier(
                                    templateObject,
                                    paramIndex,
                                    enumIdentifier,
                                    out value);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    Context.ErrorExtraMessage = GetPluggableParameterEnumValueListForError(templateObject, paramIndex); // add contextually helpful description
                                    return Error;
                                }

                            }
                            else
                            {
                                double number;

                                Error = ParseNumber(Context, out ErrorLine, out number);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }
                                value = (int)number;
                                if (number != value)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrExpectedInteger;
                                }
                            }

                            string errorMessage;
                            if (!spec.PluggableTemplate.StaticRangeCheck(paramIndex, value, out errorMessage))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                Context.ErrorExtraMessage = errorMessage;
                                return BuildInstrErrors.eBuildInstrExpectedInteger;
                            }

                            spec.SetStaticIntegerParameter(paramIndex, value);
                        }
                        break;

                    case PluggableParameterType.StaticString:
                        {
                            string stringLiteral;
                            Error = ParseIdentifier(Context, out ErrorLine, out stringLiteral);
                            if (Error != BuildInstrErrors.eBuildInstrNoError)
                            {
                                return Error;
                            }

                            string errorMessage;
                            if (!spec.PluggableTemplate.StaticRangeCheck(paramIndex, stringLiteral, out errorMessage))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                Context.ErrorExtraMessage = errorMessage;
                                return BuildInstrErrors.eBuildInstrExpectedInteger;
                            }

                            spec.SetStaticStringParameter(paramIndex, stringLiteral);
                        }
                        break;

                    case PluggableParameterType.StaticDouble:
                        {
                            double value = 0;
                            PcodeRec function = null;
                            AccentRec accents = new AccentRec();

                            Token = Context.Scanner.GetNextToken();
                            if (Token.GetTokenType() != TokenTypes.eTokenString)
                            {
                                Context.Scanner.UngetToken(Token);
                                Error = ParseNumber(Context, out ErrorLine, out value);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }
                            }
                            else
                            {
                                int CompileErrorLine;
                                DataTypes ReturnType;
                                Compiler.ASTExpression Expr;

                                CompileErrors CompileError = Compiler.CompileSpecialFunction(
                                    Context.CodeCenter,
                                    EnvelopeInitFormulaArgsDefs,
                                    out CompileErrorLine,
                                    out ReturnType,
                                    Token.GetTokenStringValue(),
                                    false/*suppressCILEmission -- this code path always generates function*/,
                                    out function,
                                    out Expr);
                                if (CompileError != CompileErrors.eCompileNoError)
                                {
                                    ErrorLine = CompileErrorLine + Context.Scanner.GetCurrentLineNumber() - 1;
                                    return BuildInstrErrors.eBuildInstrEnvFormulaSyntaxError;
                                }
                                if (ReturnType != DataTypes.eDouble)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrEnvFormulaMustHaveTypeDouble;
                                }

                                if (Compiler.ExprKind.eExprOperand == Expr.Kind)
                                {
                                    // expression evaluates at compile time to a constant
                                    switch (ReturnType)
                                    {
                                        default:
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrEnvFormulaMustHaveTypeDouble;
                                        case DataTypes.eInteger:
                                            value = Expr.InnerOperand.IntegerLiteralValue;
                                            break;
                                        case DataTypes.eFloat:
                                            value = Expr.InnerOperand.SingleLiteralValue;
                                            break;
                                        case DataTypes.eDouble:
                                            value = Expr.InnerOperand.DoubleLiteralValue;
                                            break;
                                    }
                                    function = null; // force use constant
                                }
                                else
                                {
                                    // expression must be evaluated at performance time
                                }
                            }

                            bool accent1 = false;
                            bool accent2 = false;
                            bool accent3 = false;
                            bool accent4 = false;
                            bool accent5 = false;
                            bool accent6 = false;
                            bool accent7 = false;
                            bool accent8 = false;
                            while (true)
                            {
                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                                {
                                    Context.Scanner.UngetToken(Token);
                                    break;
                                }
                                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrExpectedAccentOrSemicolon;
                                }
                                switch (Token.GetTokenKeywordTag())
                                {
                                    default:
                                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                        return BuildInstrErrors.eBuildInstrExpectedAccentOrSemicolon;
                                    case InstrKeywordType.eKeywordAccent1:
                                        if (accent1)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent1;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent0);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent1 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent2:
                                        if (accent2)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent2;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent1);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent2 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent3:
                                        if (accent3)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent3;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent2);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent3 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent4:
                                        if (accent4)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent4;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent3);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent4 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent5:
                                        if (accent5)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent5;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent4);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent5 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent6:
                                        if (accent6)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent6;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent5);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent6 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent7:
                                        if (accent7)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent7;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent6);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent7 = true;
                                        break;
                                    case InstrKeywordType.eKeywordAccent8:
                                        if (accent8)
                                        {
                                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                            return BuildInstrErrors.eBuildInstrMultipleAccent8;
                                        }
                                        Error = ParseNumber(Context, out ErrorLine, out accents.Accent7);
                                        if (Error != BuildInstrErrors.eBuildInstrNoError)
                                        {
                                            return Error;
                                        }
                                        accent8 = true;
                                        break;
                                }
                            }

                            // StaticRangeCheck() must be deferred to execution time due to dynamic calculation

                            spec.SetStaticDoubleParameter(
                                paramIndex,
                                new PluggableEvaluableParam(
                                    value,
                                    accents,
                                    function));
                        }
                        break;

                    case PluggableParameterType.DynamicDouble:
                        {
                            EnvelopeRec envelope = NewEnvelope();
                            LFOListSpecRec lfos = NewLFOListSpecifier();
                            bool matchedAny = false;

                            Token = Context.Scanner.GetNextToken();
                            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordEnvelope))
                            {
                                matchedAny = true;
                                Error = ScanEnvelopeSpec(
                                    envelope,
                                    Context,
                                    out ErrorLine);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }

                                Token = Context.Scanner.GetNextToken();
                            }

                            while ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                && (Token.GetTokenKeywordTag() == InstrKeywordType.eKeywordLfo))
                            {
                                matchedAny = true;
                                LFOSpecRec lfo = NewLFOSpecifier();
                                LFOListSpecAppendNewEntry(lfos, lfo);
                                Error = ScanLfoSpec(
                                    lfo,
                                    Context,
                                    out ErrorLine);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }

                                Token = Context.Scanner.GetNextToken();
                            }

                            if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                return matchedAny ? BuildInstrErrors.eBuildInstrExpectedSemicolon : BuildInstrErrors.eBuildInstrExpectedEnvelopeLfoOrSemicolon;
                            }
                            Context.Scanner.UngetToken(Token);

                            spec.SetDynamicDoubleParameter(
                                paramIndex,
                                new PluggableEvaluableOscParam(
                                    envelope,
                                    lfos));
                        }
                        break;
                }

            SemicolonAndContinue:
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    return BuildInstrErrors.eBuildInstrExpectedSemicolon;
                }
            }


            // cparen
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedCloseParen;
            }

            // special case for missing 'pitch' parameter -- constant 1
            if (role == PluggableRole.Oscillator)
            {
                Debug.Assert(spec.PitchParamOffset >= 0);
                if (!spec.IsParameterOptional(spec.PitchParamOffset) && !spec.IsParameterSpecified(spec.PitchParamOffset))
                {
                    EnvelopeRec envelope = NewEnvelope();
                    LFOListSpecRec lfos = NewLFOListSpecifier();

                    EnvelopeSetConstantShortcut(envelope, 1d);
                    spec.SetDynamicDoubleParameter(
                        spec.PitchParamOffset,
                        new PluggableEvaluableOscParam(
                            envelope,
                            lfos));
                }
            }

            // check for missing required parameters
            for (int i = 0; i < spec.ParameterCount; i++)
            {
                if (!spec.IsParameterOptional(i) && !spec.IsParameterSpecified(i))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    Context.ErrorExtraMessage = String.Format("'{0}'", templateObject.ParametersDefinition[i].ParserName);
                    return BuildInstrErrors.eBuildInstrPluggableRequiredParameterNotSpecified;
                }
            }


            return BuildInstrErrors.eBuildInstrNoError;
        }

        private static BuildInstrErrors ParseGlobalIdentifierString(
            BuildInstrumentContext Context,
            TokenRec<InstrKeywordType> Token,
            out int ErrorLine,
            out string identifier)
        {
            identifier = null;
            ErrorLine = -1;

            if ((Token.GetTokenType() != TokenTypes.eTokenIdentifier)
                && (Token.GetTokenType() != TokenTypes.eTokenString)
                && (Token.GetTokenType() != TokenTypes.eTokenKeyword))
            {
                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                return BuildInstrErrors.eBuildInstrExpectedPluggableParameterIdentifier;
            }

            if (Token.GetTokenType() == TokenTypes.eTokenIdentifier)
            {
                identifier = Token.GetTokenIdentifierString();
            }
            else if (Token.GetTokenType() == TokenTypes.eTokenString)
            {
                identifier = Token.GetTokenStringValue();
            }
            else
            {
                Debug.Assert(Token.GetTokenType() == TokenTypes.eTokenKeyword);
                identifier = KeywordTypeToString(Token.GetTokenKeywordTag());
            }

            return BuildInstrErrors.eBuildInstrNoError;
        }

        private static string KeywordTypeToString(InstrKeywordType keywordTag)
        {
            for (int i = 0; i < InstrKeywordTable.Length; i++)
            {
                if (InstrKeywordTable[i].TagValue == keywordTag)
                {
                    return InstrKeywordTable[i].KeywordName;
                }
            }
            Debug.Assert(false);
            throw new ArgumentException();
        }

        private static bool QueryPluggableProcessorClass(
            string classIdentifier,
            out IPluggableProcessorFactory classObject)
        {
            classObject = null;
            if (classIdentifier.StartsWith("{"))
            {
                // guid
                Guid classGuid;
                if (Guid.TryParse(classIdentifier, out classGuid))
                {
                    foreach (KeyValuePair<string, IPluggableProcessorFactory> item in AllPluggableProcessors)
                    {
                        if (classGuid == item.Value.UniqueId)
                        {
                            classObject = item.Value;
                            break;
                        }
                    }
                }
            }
            else
            {
                // friendly name - built-in table first
                foreach (KeyValuePair<string, IPluggableProcessorFactory> item in BuiltInPluggableProcessors)
                {
                    if (String.Equals(classIdentifier, item.Key))
                    {
                        classObject = item.Value;
                        break;
                    }
                }
                // name property next
                foreach (KeyValuePair<string, IPluggableProcessorFactory> item in AllPluggableProcessors)
                {
                    if (String.Equals(classIdentifier, item.Value.ParserName))
                    {
                        classObject = item.Value;
                        break;
                    }
                }
            }
            return (classObject != null);
        }

        private static string GetPluggableClassListForError()
        {
            List<string> strings = new List<string>();
            foreach (KeyValuePair<string, IPluggableProcessorFactory> item in AllPluggableProcessors)
            {
                strings.Add(item.Key);
            }
            return GetPluggableIdentifierListForError(strings);
        }

        private static string GetPluggableParameterListForError(
            IPluggableProcessorTemplate classObject)
        {
            PluggableParameter[] parameters = classObject.ParametersDefinition;
            List<string> strings = new List<string>();
            for (int i = 0; i < parameters.Length; i++)
            {
                strings.Add(parameters[i].ParserName);
            }
            return GetPluggableIdentifierListForError(strings);
        }

        private static string GetPluggableParameterEnumValueListForError(
            IPluggableProcessorTemplate classObject,
            int paramIndex)
        {
            KeyValuePair<string, int>[] enums = classObject.GetEnumeratedIntegerTokens(paramIndex);
            List<string> strings = new List<string>();
            for (int i = 0; i < enums.Length; i++)
            {
                strings.Add(enums[i].Key);
            }
            return GetPluggableIdentifierListForError(strings);
        }

        private static BuildInstrErrors GetPluggableEnumValueForEnumIdentifier(
            IPluggableProcessorTemplate classObject,
            int paramIndex,
            string identifier,
            out int value)
        {
            value = Int32.MinValue;
            KeyValuePair<string, int>[] enums = classObject.GetEnumeratedIntegerTokens(paramIndex);
            List<string> strings = new List<string>();
            for (int i = 0; i < enums.Length; i++)
            {
                if (String.Equals(identifier, enums[i].Key))
                {
                    value = enums[i].Value;
                    return BuildInstrErrors.eBuildInstrNoError;
                }
            }
            return BuildInstrErrors.eBuildInstrInvalidPluggableEnumIdentifier;
        }

        private static string GetPluggableIdentifierListForError(IList<string> strings)
        {
            StringBuilder list = new StringBuilder();
            for (int i = 0; i < strings.Count; i++)
            {
                if (list.Length != 0)
                {
                    if (i == strings.Count - 1)
                    {
                        list.Append(" or ");
                    }
                    else
                    {
                        list.Append(", ");
                    }
                }
                list.AppendFormat("'{0}'", strings[i]);
            }
            if (list.Length != 0)
            {
                return list.ToString();
            }
            return String.Empty;
        }

        private static BuildInstrErrors ParsePluggableConfigs(
            BuildInstrumentContext Context,
            IPluggableProcessorFactory classObject,
            out KeyValuePair<string, string[]>[] configs,
            out int ErrorLine)
        {
            ErrorLine = 0;
            configs = null;

            ConfigInfo[] configInfos = classObject.Configs;
            if (configInfos == null)
            {
                configInfos = new ConfigInfo[0];
            }
            List<KeyValuePair<string, string[]>> configsList = new List<KeyValuePair<string, string[]>>(configInfos.Length);

            while (true)
            {
                BuildInstrErrors Error;
                TokenRec<InstrKeywordType> Token;

                Token = Context.Scanner.GetNextToken();
                string configName;
                Error = ParseGlobalIdentifierString(Context, Token, out ErrorLine, out configName);
                if (Error != BuildInstrErrors.eBuildInstrNoError)
                {
                    Context.Scanner.UngetToken(Token);
                    break;
                }
                int configInfoIndex = Array.FindIndex(configInfos, delegate (ConfigInfo candidate) { return String.Equals(candidate.ParserName, configName); });
                if (configInfoIndex < 0)
                {
                    Context.Scanner.UngetToken(Token);
                    break;
                }

                if (configsList.FindIndex(delegate (KeyValuePair<string, string[]> candidate) { return String.Equals(candidate.Key, configName); }) >= 0)
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    Context.ErrorExtraMessage = String.Format("parameter: '{0}'", configName);
                    return BuildInstrErrors.eBuildInstrPluggableConfigAlreadySpecified;
                }

                List<string> tokens = new List<string>();
                List<string> keys = new List<string>();
                while (true)
                {
                    Token = Context.Scanner.GetNextToken();
                    if (Token.GetTokenType() == TokenTypes.eTokenSemicolon)
                    {
                        break;
                    }
                    string tokenName;
                    Error = ParseGlobalIdentifierString(Context, Token, out ErrorLine, out tokenName);
                    if (Error != BuildInstrErrors.eBuildInstrNoError)
                    {
                        return Error;
                    }

                    // key constraints
                    if (((configInfos[configInfoIndex].Form & ConfigForm.Unique) != 0)
                        && (keys.IndexOf(tokenName) >= 0))
                    {
                        ErrorLine = Context.Scanner.GetCurrentLineNumber();
                        Context.ErrorExtraMessage = String.Format("'{0}'", tokenName);
                        return BuildInstrErrors.eBuildInstrPluggableConfigParamsAlreadySpecified;
                    }

                    tokens.Add(tokenName);
                    keys.Add(tokenName);

                    // value/format constraints
                    switch (configInfos[configInfoIndex].Form & ConfigForm.BaseMask)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case ConfigForm.List:
                            // finished
                            break;

                        case ConfigForm.KeyValuePairs:
                            if ((configInfos[configInfoIndex].Form & ConfigForm.KeyValuePairColonDelimited) != 0)
                            {
                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() != TokenTypes.eTokenColon)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrPluggableConfigParamsExpectedColon;
                                }
                            }
                            else if ((configInfos[configInfoIndex].Form & ConfigForm.KeyValuePairEqualsDelimited) != 0)
                            {
                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() != TokenTypes.eTokenColon)
                                {
                                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                    return BuildInstrErrors.eBuildInstrPluggableConfigParamsExpectedEquals;
                                }
                            }
                            // else no key/value separator

                            Token = Context.Scanner.GetNextToken();
                            string value;
                            bool number = false;
                            if (Token.GetTokenType() == TokenTypes.eTokenDouble)
                            {
                                value = Token.GetTokenDoubleValue().ToString();
                                number = true;
                            }
                            else if (Token.GetTokenType() == TokenTypes.eTokenSingle)
                            {
                                value = Token.GetTokenSingleValue().ToString();
                                number = true;
                            }
                            else if (Token.GetTokenType() == TokenTypes.eTokenInteger)
                            {
                                value = Token.GetTokenIntegerValue().ToString();
                                number = true;
                            }
                            else
                            {
                                Error = ParseGlobalIdentifierString(Context, Token, out ErrorLine, out value);
                                if (Error != BuildInstrErrors.eBuildInstrNoError)
                                {
                                    return Error;
                                }
                            }

                            if (!number && ((configInfos[configInfoIndex].Form & ConfigForm.ValueAsDouble) != 0))
                            {
                                ErrorLine = Context.Scanner.GetCurrentLineNumber();
                                Context.ErrorExtraMessage = String.Format("'{0}'", value);
                                return BuildInstrErrors.eBuildInstrPluggableConfigParamsExpectedNumber;
                            }

                            tokens.Add(value);

                            break;
                    }

                    if ((configInfos[configInfoIndex].Form & ConfigForm.CommaSeparated) != 0)
                    {
                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() == TokenTypes.eTokenColon)
                        {
                            Context.Scanner.UngetToken(Token);
                        }
                        else if (Token.GetTokenType() != TokenTypes.eTokenComma)
                        {
                            ErrorLine = Context.Scanner.GetCurrentLineNumber();
                            return BuildInstrErrors.eBuildInstrPluggableConfigParamsExpectedCommaOrSemicolon;
                        }
                    }
                }

                int min = Int32.MinValue, max = Int32.MaxValue;
                if ((configInfos[configInfoIndex].MinCount.HasValue
                        && (keys.Count < (min = configInfos[configInfoIndex].MinCount.Value)))
                    || (configInfos[configInfoIndex].MaxCount.HasValue
                        && (keys.Count > (max = configInfos[configInfoIndex].MaxCount.Value))))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    if ((min == 1) && (max == 1))
                    {
                        Context.ErrorExtraMessage = "must have exactly one item";
                    }
                    else if ((min == 0) && (max == 0))
                    {
                        Context.ErrorExtraMessage = "must not have any items";
                    }
                    else
                    {
                        if (configInfos[configInfoIndex].MinCount.HasValue && configInfos[configInfoIndex].MaxCount.HasValue)
                        {
                            Context.ErrorExtraMessage = String.Format("must contain at least {0} items and no more than {1} items", min, max);
                        }
                        else if (configInfos[configInfoIndex].MinCount.HasValue)
                        {
                            Context.ErrorExtraMessage = String.Format("must contain at least {0} items", min);
                        }
                        else
                        {
                            Debug.Assert(configInfos[configInfoIndex].MaxCount.HasValue);
                            Context.ErrorExtraMessage = String.Format("must contain no more than {0} items", max);
                        }
                    }
                    return BuildInstrErrors.eBuildInstrPluggableConfigParamsWrongNumberOfItems;
                }

                configsList.Add(new KeyValuePair<string, string[]>(configInfos[configInfoIndex].ParserName, tokens.ToArray()));
            }

            // check required
            for (int i = 0; i < configInfos.Length; i++)
            {
                if (((configInfos[i].Form & ConfigForm.Required) != 0)
                    && (configsList.FindIndex(delegate (KeyValuePair<string, string[]> candidate) { return String.Equals(candidate.Key, configInfos[i].ParserName); }) < 0))
                {
                    ErrorLine = Context.Scanner.GetCurrentLineNumber();
                    Context.ErrorExtraMessage = String.Format("'{0}'", configInfos[i].ParserName);
                    return BuildInstrErrors.eBuildInstrPluggableRequiredConfigParamIsMissing;
                }
            }

            configs = configsList.ToArray();

            return BuildInstrErrors.eBuildInstrNoError;
        }
    }
}
