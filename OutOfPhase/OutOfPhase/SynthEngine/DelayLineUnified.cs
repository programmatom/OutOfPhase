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
        /* type of delay line processor */
        public enum DelayType
        {
            eTypeOscillatorDelay,
            eTypeTrackDelay,
        }

        /* tap algorithm select */
        public enum TapAlgorithm
        {
            eTapLeftToLeft,
            eTapLeftToMono,
            eTapLeftToRight,
            eTapMonoToLeft,
            eTapMonoToMono,
            eTapMonoToRight,
            eTapRightToLeft,
            eTapRightToMono,
            eTapRightToRight,

            eTapLeftToLeftLPF,
            eTapLeftToMonoLPF,
            eTapLeftToRightLPF,
            eTapMonoToLeftLPF,
            eTapMonoToMonoLPF,
            eTapMonoToRightLPF,
            eTapRightToLeftLPF,
            eTapRightToMonoLPF,
            eTapRightToRightLPF,

            eTapLeftToLeftInterpolate,
            eTapLeftToMonoInterpolate,
            eTapLeftToRightInterpolate,
            eTapMonoToLeftInterpolate,
            eTapMonoToMonoInterpolate,
            eTapMonoToRightInterpolate,
            eTapRightToLeftInterpolate,
            eTapRightToMonoInterpolate,
            eTapRightToRightInterpolate,

            eTapLeftToLeftLPFInterpolate,
            eTapLeftToMonoLPFInterpolate,
            eTapLeftToRightLPFInterpolate,
            eTapMonoToLeftLPFInterpolate,
            eTapMonoToMonoLPFInterpolate,
            eTapMonoToRightLPFInterpolate,
            eTapRightToLeftLPFInterpolate,
            eTapRightToMonoLPFInterpolate,
            eTapRightToRightLPFInterpolate,
        }

        /* variant portion for oscillator delay line tap */
        public class OscDelayCtrlRec
        {
            public EvalEnvelopeRec SourceEnvelope;
            public LFOGenRec SourceLFO;
            public EvalEnvelopeRec TargetEnvelope;
            public LFOGenRec TargetLFO;
            public EvalEnvelopeRec ScaleEnvelope;
            public LFOGenRec ScaleLFO;
            public EvalEnvelopeRec CutoffEnvelope; /* null if no filter */
            public LFOGenRec CutoffLFO; /* null if no filter */
        }

        /* variant portion for track delay line tap */
        public class TrackDelayCtrlRec
        {
            public ScalarParamEvalRec SourceTime;
            public ScalarParamEvalRec TargetTime;
            public ScalarParamEvalRec ScaleFactor;
            public ScalarParamEvalRec Cutoff;
        }

        /* combined control record for delay line tap */
        [StructLayout(LayoutKind.Auto)]
        public struct UnifiedDelayCtrlRec
        {
            // exactly one of these is non-null
            public OscDelayCtrlRec Osc;
            public TrackDelayCtrlRec Track;
        }

        /* delay line tap state rec */
        public class DelayLineTapRec
        {
            /* what are we tapping */
            public DelayChannelType SourceTap;
            public DelayChannelType TargetTap;

            /* control information */
            public TapAlgorithm TapProcessAlgorithm;
            public bool LowpassFilterEnabled;

            /* state information */
            public int SourceOffsetInteger;
            public int TargetOffsetInteger;
            public byte SourceLine; // used for simple case: 0 = left, 1 = right
            public byte TargetLine; // used for simple case: 0 = left, 1 = right
            public float Scaling;
            public FirstOrderLowpassRec LowpassFilter;
            public float SourceOffsetFraction;
        }

        public class DelayUnifiedRec : ITrackEffect, IOscillatorEffect
        {
            /* kind of delay processor */
            public DelayType Type;

            /* operator kernel type */
            public bool ComplexCaseRequired;

            /* maximum delay */
            public float MaxDelayTime;

            /* state information */
            public int DelayLineLength; /* power of 2 */
            public int VectorMask; /* power of 2 - 1 for masking wraparound */
            public int DelayLineIndex;
            public float[] LeftDelayLineArray;
            public float[] RightDelayLineArray;

            /* tap arrays */
            public DelayLineTapRec[] TapVector;
            public UnifiedDelayCtrlRec[] CtrlVector;


            /* determine what algorithm to use for a tap.  Sets *ComplexCaseRequired if a */
            /* complex operator is used, otherwise leaves it untouched. */
            private static TapAlgorithm DelayLineGetAlgorithm(
                bool InterpolateFlag,
                DelayChannelType SourceTap,
                DelayChannelType TargetTap,
                bool FilterEnable,
                ref bool ComplexCaseRequired)
            {
                if (!InterpolateFlag)
                {
                    /* no interpolation */
                    switch (SourceTap)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case DelayChannelType.eTapLeftChannel:
                            switch (TargetTap)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case DelayChannelType.eTapLeftChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapLeftToLeft;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapLeftToLeftLPF;
                                    }
                                case DelayChannelType.eTapRightChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapLeftToRight;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapLeftToRightLPF;
                                    }
                                case DelayChannelType.eTapMonoChannel:
                                    ComplexCaseRequired = true;
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapLeftToMono;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapLeftToMonoLPF;
                                    }
                            }

                        case DelayChannelType.eTapRightChannel:
                            switch (TargetTap)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case DelayChannelType.eTapLeftChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapRightToLeft;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapRightToLeftLPF;
                                    }
                                case DelayChannelType.eTapRightChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapRightToRight;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapRightToRightLPF;
                                    }
                                case DelayChannelType.eTapMonoChannel:
                                    ComplexCaseRequired = true;
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapRightToMono;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapRightToMonoLPF;
                                    }
                            }

                        case DelayChannelType.eTapMonoChannel:
                            ComplexCaseRequired = true;
                            switch (TargetTap)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case DelayChannelType.eTapLeftChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapMonoToLeft;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapMonoToLeftLPF;
                                    }
                                case DelayChannelType.eTapRightChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapMonoToRight;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapMonoToRightLPF;
                                    }
                                case DelayChannelType.eTapMonoChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapMonoToMono;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapMonoToMonoLPF;
                                    }
                            }
                    }
                }
                else
                {
                    /* yes interpolation */
                    ComplexCaseRequired = true;
                    switch (SourceTap)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case DelayChannelType.eTapLeftChannel:
                            switch (TargetTap)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case DelayChannelType.eTapLeftChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapLeftToLeftInterpolate;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapLeftToLeftLPFInterpolate;
                                    }
                                case DelayChannelType.eTapRightChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapLeftToRightInterpolate;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapLeftToRightLPFInterpolate;
                                    }
                                case DelayChannelType.eTapMonoChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapLeftToMonoInterpolate;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapLeftToMonoLPFInterpolate;
                                    }
                            }

                        case DelayChannelType.eTapRightChannel:
                            switch (TargetTap)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case DelayChannelType.eTapLeftChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapRightToLeftInterpolate;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapRightToLeftLPFInterpolate;
                                    }
                                case DelayChannelType.eTapRightChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapRightToRightInterpolate;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapRightToRightLPFInterpolate;
                                    }
                                case DelayChannelType.eTapMonoChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapRightToMonoInterpolate;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapRightToMonoLPFInterpolate;
                                    }
                            }

                        case DelayChannelType.eTapMonoChannel:
                            switch (TargetTap)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case DelayChannelType.eTapLeftChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapMonoToLeftInterpolate;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapMonoToLeftLPFInterpolate;
                                    }
                                case DelayChannelType.eTapRightChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapMonoToRightInterpolate;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapMonoToRightLPFInterpolate;
                                    }
                                case DelayChannelType.eTapMonoChannel:
                                    if (!FilterEnable)
                                    {
                                        return TapAlgorithm.eTapMonoToMonoInterpolate;
                                    }
                                    else
                                    {
                                        return TapAlgorithm.eTapMonoToMonoLPFInterpolate;
                                    }
                            }
                    }
                }
            }

            /* common routine for doing setup of global delay line structures */
            private static DelayUnifiedRec AllocDelayLineStructure(
                DelayType Type,
                DelayEffectRec Template,
                SynthParamRec SynthParams)
            {
                DelayUnifiedRec Delay = new DelayUnifiedRec();

                /* set up initial parameters */
                Delay.Type = Type; /* do this right away */
                PcodeRec MaxDelayTimeFormula = GetDelayMaxTimeFormula(Template);
                if (MaxDelayTimeFormula == null)
                {
                    Delay.MaxDelayTime = (float)GetDelayMaxTime(Template);
                }
                else
                {
                    // This parameter function feature is mostly provided so that max delay time can be parameterized on
                    // 'bpm', to facilitate easy moving of delay effects from one document to another. Not all functionality
                    // is supported right now, such as accent parameterization. In any case, this parameter is fixed at the
                    // creation of the delay line processor, so accent changes or changes to the 'bpm' parameter can't be
                    // handled anyway. The convenience is still worth having it.
                    AccentRec zero = new AccentRec();
                    double temp;
                    SynthErrorCodes error = StaticEval(
                        0,
                        MaxDelayTimeFormula,
                        ref zero, // TODO: pass track effect accents in to here
                        SynthParams,
                        out temp);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        // TODO:
                    }
                    Delay.MaxDelayTime = (float)temp;
                }
                int TapCount = GetDelayEffectSpecNumTaps(Template);
                Delay.ComplexCaseRequired = false; /* start out simple case */

                int Temp = (int)(SynthParams.dSamplingRate * Delay.MaxDelayTime + 1);
                Temp = Temp & 0x3fffffff;
                Delay.DelayLineLength = 1;
                while (Temp != 0)
                {
                    Temp = Temp >> 1;
                    Delay.DelayLineLength = Delay.DelayLineLength << 1;
                }
                Delay.VectorMask = Delay.DelayLineLength - 1; /* power of 2 - 1 */
                Delay.DelayLineIndex = 0;

                Delay.TapVector = new DelayLineTapRec[TapCount];
                Delay.CtrlVector = new UnifiedDelayCtrlRec[TapCount];

                Delay.LeftDelayLineArray = New(ref SynthParams.freelists.FloatBufferFreeList, Delay.DelayLineLength); // zeroed
                Delay.RightDelayLineArray = New(ref SynthParams.freelists.FloatBufferFreeList, Delay.DelayLineLength); // zeroed

                return Delay;
            }

            /* create a new track delay line processor */
            public static DelayUnifiedRec NewTrackDelayLineProcessor(
                DelayEffectRec Template,
                SynthParamRec SynthParams)
            {
                /* allocate the global stuff */
                DelayUnifiedRec Delay = AllocDelayLineStructure(
                    DelayType.eTypeTrackDelay,
                    Template,
                    SynthParams);

                /* build tap list */
                DelayLineTapRec[] TapVector = Delay.TapVector;
                UnifiedDelayCtrlRec[] CtrlVector = Delay.CtrlVector;
                for (int i = 0; i < TapVector.Length; i++)
                {
                    TapVector[i] = new DelayLineTapRec();
                    CtrlVector[i].Track = new TrackDelayCtrlRec();

                    /* fill in common template fields */
                    TapVector[i].SourceTap = GetDelayTapSource(Template, i);
                    TapVector[i].TargetTap = GetDelayTapTarget(Template, i);

                    /* initialize all evaluators */
                    GetDelayTapSourceTimeAgg(
                        Template,
                        i,
                        out CtrlVector[i].Track.SourceTime);
                    GetDelayTapTargetTimeAgg(
                        Template,
                        i,
                        out CtrlVector[i].Track.TargetTime);
                    GetDelayTapScaleAgg(
                        Template,
                        i,
                        out CtrlVector[i].Track.ScaleFactor);
                    GetDelayTapFilterCutoffAgg(
                        Template,
                        i,
                        out CtrlVector[i].Track.Cutoff);

                    /* create lowpass filter if necessary, else leave it null */
                    TapVector[i].LowpassFilterEnabled = GetDelayTapFilterEnable(Template, i);
                    if (TapVector[i].LowpassFilterEnabled)
                    {
                        TapVector[i].LowpassFilter = new FirstOrderLowpassRec();
                    }

                    /* determine which algorithm to use */
                    TapVector[i].TapProcessAlgorithm = DelayLineGetAlgorithm(
                        GetDelayTapInterpolateFlag(Template, i),
                        TapVector[i].SourceTap,
                        TapVector[i].TargetTap,
                        GetDelayTapFilterEnable(Template, i),
                        ref Delay.ComplexCaseRequired);

                    /* prepare information for simple case processor */
                    switch (TapVector[i].TapProcessAlgorithm)
                    {
                        default:
                            break;
                        case TapAlgorithm.eTapLeftToLeft:
                        case TapAlgorithm.eTapLeftToLeftLPF:
                            TapVector[i].SourceLine = 0;
                            TapVector[i].TargetLine = 0;
                            break;
                        case TapAlgorithm.eTapLeftToRight:
                        case TapAlgorithm.eTapLeftToRightLPF:
                            TapVector[i].SourceLine = 0;
                            TapVector[i].TargetLine = 1;
                            break;
                        case TapAlgorithm.eTapRightToLeft:
                        case TapAlgorithm.eTapRightToLeftLPF:
                            TapVector[i].SourceLine = 1;
                            TapVector[i].TargetLine = 0;
                            break;
                        case TapAlgorithm.eTapRightToRight:
                        case TapAlgorithm.eTapRightToRightLPF:
                            TapVector[i].SourceLine = 1;
                            TapVector[i].TargetLine = 1;
                            break;
                    }
                }

                return Delay;
            }

            /* create a new oscillator delay line processor */
            public static DelayUnifiedRec NewOscUnifiedDelayLineProcessor(
                DelayEffectRec Template,
                ref AccentRec Accents,
                double HurryUp,
                double InitialFrequency,
                double FreqForMultisampling,
                out int PreOriginTimeOut,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams)
            {
                /* allocate the global stuff */
                DelayUnifiedRec Delay = AllocDelayLineStructure(
                    DelayType.eTypeOscillatorDelay,
                    Template,
                    SynthParams);

                /* build tap list */
                int MaxPreOrigin = 0;
                DelayLineTapRec[] TapVector = Delay.TapVector;
                UnifiedDelayCtrlRec[] CtrlVector = Delay.CtrlVector;
                for (int i = 0; i < TapVector.Length; i++)
                {
                    TapVector[i] = new DelayLineTapRec();
                    CtrlVector[i].Osc = new OscDelayCtrlRec();

                    int OnePreOrigin;

                    /* fill in common fields */
                    TapVector[i].SourceTap = GetDelayTapSource(Template, i);
                    TapVector[i].TargetTap = GetDelayTapTarget(Template, i);

                    /* create envelope generators */

                    CtrlVector[i].Osc.SourceEnvelope = NewEnvelopeStateRecord(
                        GetDelayTapSourceEnvelope(Template, i),
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

                    CtrlVector[i].Osc.SourceLFO = NewLFOGenerator(
                        GetDelayTapSourceLFO(Template, i),
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

                    CtrlVector[i].Osc.TargetEnvelope = NewEnvelopeStateRecord(
                        GetDelayTapTargetEnvelope(Template, i),
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

                    CtrlVector[i].Osc.TargetLFO = NewLFOGenerator(
                        GetDelayTapTargetLFO(Template, i),
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

                    CtrlVector[i].Osc.ScaleEnvelope = NewEnvelopeStateRecord(
                        GetDelayTapScaleEnvelope(Template, i),
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

                    CtrlVector[i].Osc.ScaleLFO = NewLFOGenerator(
                        GetDelayTapScaleLFO(Template, i),
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

                    if (GetDelayTapFilterEnable(Template, i))
                    {
                        CtrlVector[i].Osc.CutoffEnvelope = NewEnvelopeStateRecord(
                            GetDelayTapCutoffEnvelope(Template, i),
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
                    }

                    if (GetDelayTapFilterEnable(Template, i))
                    {
                        CtrlVector[i].Osc.CutoffLFO = NewLFOGenerator(
                            GetDelayTapCutoffLFO(Template, i),
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

                    /* set up filter */
                    TapVector[i].LowpassFilterEnabled = GetDelayTapFilterEnable(Template, i);
                    if (TapVector[i].LowpassFilterEnabled)
                    {
                        TapVector[i].LowpassFilter = new FirstOrderLowpassRec();
                    }

                    /* determine which algorithm to use */
                    TapVector[i].TapProcessAlgorithm = DelayLineGetAlgorithm(
                        GetDelayTapInterpolateFlag(Template, i),
                        TapVector[i].SourceTap,
                        TapVector[i].TargetTap,
                        GetDelayTapFilterEnable(Template, i),
                        ref Delay.ComplexCaseRequired);

                    /* prepare information for simple case processor */
                    switch (TapVector[i].TapProcessAlgorithm)
                    {
                        default:
                            break;
                        case TapAlgorithm.eTapLeftToLeft:
                        case TapAlgorithm.eTapLeftToLeftLPF:
                            TapVector[i].SourceLine = 0;
                            TapVector[i].TargetLine = 0;
                            break;
                        case TapAlgorithm.eTapLeftToRight:
                        case TapAlgorithm.eTapLeftToRightLPF:
                            TapVector[i].SourceLine = 0;
                            TapVector[i].TargetLine = 1;
                            break;
                        case TapAlgorithm.eTapRightToLeft:
                        case TapAlgorithm.eTapRightToLeftLPF:
                            TapVector[i].SourceLine = 1;
                            TapVector[i].TargetLine = 0;
                            break;
                        case TapAlgorithm.eTapRightToRight:
                        case TapAlgorithm.eTapRightToRightLPF:
                            TapVector[i].SourceLine = 1;
                            TapVector[i].TargetLine = 1;
                            break;
                    }
                }

                /* copy out maximum pre-origin time */
                PreOriginTimeOut = MaxPreOrigin;

                return Delay;
            }

            /* fix up the origin time so that envelopes start at the proper times */
            public void OscFixEnvelopeOrigins(
                int ActualPreOriginTime)
            {
#if DEBUG
                if (this.Type != DelayType.eTypeOscillatorDelay)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                UnifiedDelayCtrlRec[] CtrlVector = this.CtrlVector;
                for (int Scan = 0; Scan < CtrlVector.Length; Scan++)
                {
                    EnvelopeStateFixUpInitialDelay(
                        CtrlVector[Scan].Osc.SourceEnvelope,
                        ActualPreOriginTime);
                    EnvelopeStateFixUpInitialDelay(
                        CtrlVector[Scan].Osc.TargetEnvelope,
                        ActualPreOriginTime);
                    EnvelopeStateFixUpInitialDelay(
                        CtrlVector[Scan].Osc.ScaleEnvelope,
                        ActualPreOriginTime);
                    if (CtrlVector[Scan].Osc.CutoffEnvelope != null)
                    {
                        EnvelopeStateFixUpInitialDelay(
                            CtrlVector[Scan].Osc.CutoffEnvelope,
                            ActualPreOriginTime);
                    }

                    LFOGeneratorFixEnvelopeOrigins(
                        CtrlVector[Scan].Osc.SourceLFO,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        CtrlVector[Scan].Osc.TargetLFO,
                        ActualPreOriginTime);
                    LFOGeneratorFixEnvelopeOrigins(
                        CtrlVector[Scan].Osc.ScaleLFO,
                        ActualPreOriginTime);
                    if (CtrlVector[Scan].Osc.CutoffLFO != null)
                    {
                        LFOGeneratorFixEnvelopeOrigins(
                            CtrlVector[Scan].Osc.CutoffLFO,
                            ActualPreOriginTime);
                    }
                }
            }

            /* update delay line state with accent information */
            public SynthErrorCodes TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;

#if DEBUG
                if (this.Type != DelayType.eTypeTrackDelay)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                /* update tap states */
                DelayLineTapRec[] TapVector = this.TapVector;
                UnifiedDelayCtrlRec[] CtrlVector = this.CtrlVector;
                Debug.Assert(TapVector.Length == CtrlVector.Length);
                for (int Scan = 0; Scan < TapVector.Length; Scan++)
                {
                    double Time;
                    double Temp;

                    DelayLineTapRec ThisTap = TapVector[Scan];
                    UnifiedDelayCtrlRec ThisCtrl = CtrlVector[Scan];

                    /* determine source offset */
                    error = ScalarParamEval(
                        ThisCtrl.Track.SourceTime,
                        ref Accents,
                        SynthParams,
                        out Time);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                    if (Time < 0)
                    {
                        Time = 0;
                    }
                    if (Time > this.MaxDelayTime)
                    {
                        Time = this.MaxDelayTime;
                    }
                    Temp = Time * SynthParams.dSamplingRate;
                    ThisTap.SourceOffsetInteger = (int)Temp;
                    ThisTap.SourceOffsetFraction = (float)(Temp - ThisTap.SourceOffsetInteger);

                    /* determine target offset */
                    error = ScalarParamEval(
                        ThisCtrl.Track.TargetTime,
                        ref Accents,
                        SynthParams,
                        out Time);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                    if (Time < 0)
                    {
                        Time = 0;
                    }
                    if (Time > this.MaxDelayTime)
                    {
                        Time = this.MaxDelayTime;
                    }
                    ThisTap.TargetOffsetInteger = (int)(Time * SynthParams.dSamplingRate);

                    /* determine scale factor */
                    error = ScalarParamEval(
                        ThisCtrl.Track.ScaleFactor,
                        ref Accents,
                        SynthParams,
                        out Temp);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                    ThisTap.Scaling = (float)Temp;

                    /* update filter, if it exists */
                    if (ThisTap.LowpassFilterEnabled)
                    {
                        error = ScalarParamEval(
                            ThisCtrl.Track.Cutoff,
                            ref Accents,
                            SynthParams,
                            out Temp);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                        FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                            ThisTap.LowpassFilter,
                            Temp,
                            SynthParams.dSamplingRate);
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            /* generate delays from envelopes */
            public SynthErrorCodes OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams)
            {
#if DEBUG
                if (this.Type != DelayType.eTypeOscillatorDelay)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                /* update tap states */
                DelayLineTapRec[] TapVector = this.TapVector;
                UnifiedDelayCtrlRec[] CtrlVector = this.CtrlVector;
                Debug.Assert(TapVector.Length == CtrlVector.Length);
                for (int Scan = 0; Scan < TapVector.Length; Scan++)
                {
                    double Time;
                    double Temp;

                    /* determine source offset */
                    SynthErrorCodes error = SynthErrorCodes.eSynthDone;
                    Time = LFOGenUpdateCycle(
                        CtrlVector[Scan].Osc.SourceLFO,
                        EnvelopeUpdate(
                            CtrlVector[Scan].Osc.SourceEnvelope,
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
                    if (Time < 0)
                    {
                        Time = 0;
                    }
                    if (Time > this.MaxDelayTime)
                    {
                        Time = this.MaxDelayTime;
                    }
                    Temp = Time * SynthParams.dSamplingRate;
                    TapVector[Scan].SourceOffsetInteger = (int)Temp;
                    TapVector[Scan].SourceOffsetFraction = (float)(Temp - TapVector[Scan].SourceOffsetInteger);

                    /* determine target offset */
                    error = SynthErrorCodes.eSynthDone;
                    Time = LFOGenUpdateCycle(
                        CtrlVector[Scan].Osc.TargetLFO,
                        EnvelopeUpdate(
                            CtrlVector[Scan].Osc.TargetEnvelope,
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
                    if (Time < 0)
                    {
                        Time = 0;
                    }
                    if (Time > this.MaxDelayTime)
                    {
                        Time = this.MaxDelayTime;
                    }
                    TapVector[Scan].TargetOffsetInteger = (int)(Time * SynthParams.dSamplingRate);

                    /* update lowpass filter */
                    if (TapVector[Scan].LowpassFilterEnabled)
                    {
                        error = SynthErrorCodes.eSynthDone;
                        double cutoff = LFOGenUpdateCycle(
                            CtrlVector[Scan].Osc.CutoffLFO,
                            EnvelopeUpdate(
                                CtrlVector[Scan].Osc.CutoffEnvelope,
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
                        FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                            TapVector[Scan].LowpassFilter,
                            cutoff,
                            SynthParams.dSamplingRate);
                    }

                    /* determine scale factor */
                    error = SynthErrorCodes.eSynthDone;
                    TapVector[Scan].Scaling = (float)LFOGenUpdateCycle(
                        CtrlVector[Scan].Osc.ScaleLFO,
                        EnvelopeUpdate(
                            CtrlVector[Scan].Osc.ScaleEnvelope,
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
                }

                return SynthErrorCodes.eSynthDone;
            }

            public void OscKeyUpSustain1()
            {
#if DEBUG
                if (this.Type != DelayType.eTypeOscillatorDelay)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                UnifiedDelayCtrlRec[] CtrlVector = this.CtrlVector;
                for (int Scan = 0; Scan < CtrlVector.Length; Scan++)
                {
                    EnvelopeKeyUpSustain1(CtrlVector[Scan].Osc.SourceEnvelope);
                    EnvelopeKeyUpSustain1(CtrlVector[Scan].Osc.TargetEnvelope);
                    EnvelopeKeyUpSustain1(CtrlVector[Scan].Osc.ScaleEnvelope);
                    if (CtrlVector[Scan].Osc.CutoffEnvelope != null)
                    {
                        EnvelopeKeyUpSustain1(CtrlVector[Scan].Osc.CutoffEnvelope);
                    }

                    LFOGeneratorKeyUpSustain1(CtrlVector[Scan].Osc.SourceLFO);
                    LFOGeneratorKeyUpSustain1(CtrlVector[Scan].Osc.TargetLFO);
                    LFOGeneratorKeyUpSustain1(CtrlVector[Scan].Osc.ScaleLFO);
                    if (CtrlVector[Scan].Osc.CutoffLFO != null)
                    {
                        LFOGeneratorKeyUpSustain1(CtrlVector[Scan].Osc.CutoffLFO);
                    }
                }
            }

            public void OscKeyUpSustain2()
            {
#if DEBUG
                if (this.Type != DelayType.eTypeOscillatorDelay)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                UnifiedDelayCtrlRec[] CtrlVector = this.CtrlVector;
                for (int Scan = 0; Scan < CtrlVector.Length; Scan++)
                {
                    EnvelopeKeyUpSustain2(CtrlVector[Scan].Osc.SourceEnvelope);
                    EnvelopeKeyUpSustain2(CtrlVector[Scan].Osc.TargetEnvelope);
                    EnvelopeKeyUpSustain2(CtrlVector[Scan].Osc.ScaleEnvelope);
                    if (CtrlVector[Scan].Osc.CutoffEnvelope != null)
                    {
                        EnvelopeKeyUpSustain2(CtrlVector[Scan].Osc.CutoffEnvelope);
                    }

                    LFOGeneratorKeyUpSustain2(CtrlVector[Scan].Osc.SourceLFO);
                    LFOGeneratorKeyUpSustain2(CtrlVector[Scan].Osc.TargetLFO);
                    LFOGeneratorKeyUpSustain2(CtrlVector[Scan].Osc.ScaleLFO);
                    if (CtrlVector[Scan].Osc.CutoffLFO != null)
                    {
                        LFOGeneratorKeyUpSustain2(CtrlVector[Scan].Osc.CutoffLFO);
                    }
                }
            }

            public void OscKeyUpSustain3()
            {
#if DEBUG
                if (this.Type != DelayType.eTypeOscillatorDelay)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                UnifiedDelayCtrlRec[] CtrlVector = this.CtrlVector;
                for (int Scan = 0; Scan < CtrlVector.Length; Scan++)
                {
                    EnvelopeKeyUpSustain3(CtrlVector[Scan].Osc.SourceEnvelope);
                    EnvelopeKeyUpSustain3(CtrlVector[Scan].Osc.TargetEnvelope);
                    EnvelopeKeyUpSustain3(CtrlVector[Scan].Osc.ScaleEnvelope);
                    if (CtrlVector[Scan].Osc.CutoffEnvelope != null)
                    {
                        EnvelopeKeyUpSustain3(CtrlVector[Scan].Osc.CutoffEnvelope);
                    }

                    LFOGeneratorKeyUpSustain3(CtrlVector[Scan].Osc.SourceLFO);
                    LFOGeneratorKeyUpSustain3(CtrlVector[Scan].Osc.TargetLFO);
                    LFOGeneratorKeyUpSustain3(CtrlVector[Scan].Osc.ScaleLFO);
                    if (CtrlVector[Scan].Osc.CutoffLFO != null)
                    {
                        LFOGeneratorKeyUpSustain3(CtrlVector[Scan].Osc.CutoffLFO);
                    }
                }
            }

            /* retrigger effect envelopes from the origin point */
            public void OscRetriggerEnvelopes(
                ref AccentRec NewAccents,
                double NewHurryUp,
                double NewInitialFrequency,
                bool ActuallyRetrigger,
                SynthParamRec SynthParams)
            {
#if DEBUG
                if (this.Type != DelayType.eTypeOscillatorDelay)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
#endif

                UnifiedDelayCtrlRec[] CtrlVector = this.CtrlVector;
                for (int Scan = 0; Scan < CtrlVector.Length; Scan++)
                {
                    EnvelopeRetriggerFromOrigin(
                        CtrlVector[Scan].Osc.SourceEnvelope,
                        ref NewAccents,
                        NewInitialFrequency,
                        1,
                        NewHurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    EnvelopeRetriggerFromOrigin(
                        CtrlVector[Scan].Osc.TargetEnvelope,
                        ref NewAccents,
                        NewInitialFrequency,
                        1,
                        NewHurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    EnvelopeRetriggerFromOrigin(
                        CtrlVector[Scan].Osc.ScaleEnvelope,
                        ref NewAccents,
                        NewInitialFrequency,
                        1,
                        NewHurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    if (CtrlVector[Scan].Osc.CutoffEnvelope != null)
                    {
                        EnvelopeRetriggerFromOrigin(
                            CtrlVector[Scan].Osc.CutoffEnvelope,
                            ref NewAccents,
                            NewInitialFrequency,
                            1,
                            NewHurryUp,
                            ActuallyRetrigger,
                            SynthParams);
                    }

                    LFOGeneratorRetriggerFromOrigin(
                        CtrlVector[Scan].Osc.SourceLFO,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        1,
                        1,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        CtrlVector[Scan].Osc.TargetLFO,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        1,
                        1,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        CtrlVector[Scan].Osc.ScaleLFO,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        1,
                        1,
                        ActuallyRetrigger,
                        SynthParams);
                    if (CtrlVector[Scan].Osc.CutoffLFO != null)
                    {
                        LFOGeneratorRetriggerFromOrigin(
                            CtrlVector[Scan].Osc.CutoffLFO,
                            ref NewAccents,
                            NewInitialFrequency,
                            NewHurryUp,
                            1,
                            1,
                            ActuallyRetrigger,
                            SynthParams);
                    }
                }
            }

            private static void StereoDelayLineProcessorComplex(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                DelayUnifiedRec Delay)
            {
                int VectorMask = Delay.VectorMask;
                int VectorIndex = Delay.DelayLineIndex;
                float[] LeftDelayLineArray = Delay.LeftDelayLineArray;
                float[] RightDelayLineArray = Delay.RightDelayLineArray;
                int DelayLineLength = Delay.DelayLineLength;
                DelayLineTapRec[] TapVector = Delay.TapVector;

                for (int Scan = 0; Scan < nActualFrames; Scan++)
                {
                    /* initialize current point with dry data */
                    float LT = workspace[Scan + lOffset];
                    float RT = workspace[Scan + rOffset];
                    LeftDelayLineArray[VectorIndex] = LT;
                    RightDelayLineArray[VectorIndex] = RT;

                    /* iterate over taps */
                    for (int TapScan = 0; TapScan < TapVector.Length; TapScan++)
                    {
                        /* initialize base pointer */
                        DelayLineTapRec Tap = TapVector[TapScan];

                        /* precompute some stuff */
                        int SourceIndex = VectorMask & (VectorIndex + Tap.SourceOffsetInteger);
                        int TargetIndex = VectorMask & (VectorIndex + Tap.TargetOffsetInteger);

                        /* useful stuff */
                        float LocalScaling = Tap.Scaling;

                        /* get algorithm */
                        TapAlgorithm Algorithm = Tap.TapProcessAlgorithm;

                        /* apply transformation */
                        float Temp;
                        float LS;
                        float RS;
                        float LS1;
                        float RS1;
                        switch (Algorithm)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();

                            case TapAlgorithm.eTapLeftToLeft:
                                LS = LeftDelayLineArray[SourceIndex];
                                LT = LeftDelayLineArray[TargetIndex];
                                LeftDelayLineArray[TargetIndex] = LT + LS * LocalScaling;
                                break;
                            case TapAlgorithm.eTapLeftToMono:
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                Temp = LS * LocalScaling;
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapLeftToRight:
                                LS = LeftDelayLineArray[SourceIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                RightDelayLineArray[TargetIndex] = RT + LS * LocalScaling;
                                break;
                            case TapAlgorithm.eTapMonoToLeft:
                                LS = LeftDelayLineArray[SourceIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                LT = LeftDelayLineArray[TargetIndex];
                                LeftDelayLineArray[TargetIndex] = LT + ((LS + RS) * .5f) * LocalScaling;
                                break;
                            case TapAlgorithm.eTapMonoToMono:
                                LS = LeftDelayLineArray[SourceIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = ((LS + RS) * .5f) * LocalScaling;
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapMonoToRight:
                                RT = RightDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                RightDelayLineArray[TargetIndex] = RT + ((LS + RS) * .5f) * LocalScaling;
                                break;
                            case TapAlgorithm.eTapRightToLeft:
                                LT = LeftDelayLineArray[TargetIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                LeftDelayLineArray[TargetIndex] = LT + RS * LocalScaling;
                                break;
                            case TapAlgorithm.eTapRightToMono:
                                RS = RightDelayLineArray[SourceIndex];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = RS * LocalScaling;
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapRightToRight:
                                RT = RightDelayLineArray[TargetIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                RightDelayLineArray[TargetIndex] = RT + RS * LocalScaling;
                                break;

                            case TapAlgorithm.eTapLeftToLeftLPF:
                                LT = LeftDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                LeftDelayLineArray[TargetIndex] = LT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(Tap.LowpassFilter, LS * LocalScaling);
                                break;
                            case TapAlgorithm.eTapLeftToMonoLPF:
                                LS = LeftDelayLineArray[SourceIndex];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = FirstOrderLowpassRec.ApplyFirstOrderLowpass(Tap.LowpassFilter, LS * LocalScaling);
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapLeftToRightLPF:
                                RT = RightDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                RightDelayLineArray[TargetIndex] = RT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(Tap.LowpassFilter, LS * LocalScaling);
                                break;
                            case TapAlgorithm.eTapMonoToLeftLPF:
                                LT = LeftDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                LeftDelayLineArray[TargetIndex] = LT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                        Tap.LowpassFilter,
                                        ((LS + RS) * .5f) * LocalScaling);
                                break;
                            case TapAlgorithm.eTapMonoToMonoLPF:
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                Temp = FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                    Tap.LowpassFilter,
                                    ((LS + RS) * .5f) * LocalScaling);
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapMonoToRightLPF:
                                RT = RightDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                RightDelayLineArray[TargetIndex] = RT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                        Tap.LowpassFilter,
                                        ((LS + RS) * .5f) * LocalScaling);
                                break;
                            case TapAlgorithm.eTapRightToLeftLPF:
                                LT = LeftDelayLineArray[TargetIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                LeftDelayLineArray[TargetIndex] = LT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(Tap.LowpassFilter, RS * LocalScaling);
                                break;
                            case TapAlgorithm.eTapRightToMonoLPF:
                                RS = RightDelayLineArray[SourceIndex];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = FirstOrderLowpassRec.ApplyFirstOrderLowpass(Tap.LowpassFilter, RS * LocalScaling);
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapRightToRightLPF:
                                RT = RightDelayLineArray[TargetIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                RightDelayLineArray[TargetIndex] = RT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(Tap.LowpassFilter, RS * LocalScaling);
                                break;

                            /* L+F(R-L) */
                            case TapAlgorithm.eTapLeftToLeftInterpolate:
                                LT = LeftDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LeftDelayLineArray[TargetIndex] = LT +
                                    (LS + Tap.SourceOffsetFraction * (LS1 - LS)) * LocalScaling;
                                break;
                            case TapAlgorithm.eTapLeftToMonoInterpolate:
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = (LS + Tap.SourceOffsetFraction * (LS1 - LS)) * LocalScaling;
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapLeftToRightInterpolate:
                                RT = RightDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RightDelayLineArray[TargetIndex] = RT +
                                    (LS + Tap.SourceOffsetFraction * (LS1 - LS)) * LocalScaling;
                                break;
                            case TapAlgorithm.eTapMonoToLeftInterpolate:
                                LT = LeftDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LeftDelayLineArray[TargetIndex] = LT +
                                    (((LS + Tap.SourceOffsetFraction * (LS1 - LS))
                                    + (RS + Tap.SourceOffsetFraction * (RS1 - RS))) * .5f)
                                    * LocalScaling;
                                break;
                            case TapAlgorithm.eTapMonoToMonoInterpolate:
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = (((LS
                                    + Tap.SourceOffsetFraction * (LS1 - LS))
                                    + (RS + Tap.SourceOffsetFraction * (RS1 - RS))) * .5f)
                                    * LocalScaling;
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapMonoToRightInterpolate:
                                RT = RightDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RightDelayLineArray[TargetIndex] = RT +
                                    (((LS + Tap.SourceOffsetFraction * (LS1 - LS))
                                    + (RS + Tap.SourceOffsetFraction * (RS1 - RS))) * .5f)
                                    * LocalScaling;
                                break;
                            case TapAlgorithm.eTapRightToLeftInterpolate:
                                LT = LeftDelayLineArray[TargetIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LeftDelayLineArray[TargetIndex] = LT +
                                    (RS + Tap.SourceOffsetFraction * (RS1 - RS)) * LocalScaling;
                                break;
                            case TapAlgorithm.eTapRightToMonoInterpolate:
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = (RS + Tap.SourceOffsetFraction * (RS1 - RS)) * LocalScaling;
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapRightToRightInterpolate:
                                RT = RightDelayLineArray[TargetIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RightDelayLineArray[TargetIndex] = RT +
                                    (RS + Tap.SourceOffsetFraction * (RS1 - RS)) * LocalScaling;
                                break;

                            case TapAlgorithm.eTapLeftToLeftLPFInterpolate:
                                LT = LeftDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LeftDelayLineArray[TargetIndex] = LT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                        Tap.LowpassFilter,
                                        (LS + Tap.SourceOffsetFraction * (LS1 - LS)) * LocalScaling);
                                break;
                            case TapAlgorithm.eTapLeftToMonoLPFInterpolate:
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                    Tap.LowpassFilter,
                                    (LS + Tap.SourceOffsetFraction * (LS1 - LS)) * LocalScaling);
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapLeftToRightLPFInterpolate:
                                RT = RightDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RightDelayLineArray[TargetIndex] = RT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                        Tap.LowpassFilter,
                                        (LS + Tap.SourceOffsetFraction * (LS1 - LS)) * LocalScaling);
                                break;
                            case TapAlgorithm.eTapMonoToLeftLPFInterpolate:
                                LT = LeftDelayLineArray[TargetIndex];
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LeftDelayLineArray[TargetIndex] = LT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                        Tap.LowpassFilter,
                                        ((LS + RS) * .5f + Tap.SourceOffsetFraction
                                            * ((LS1 + RS1) * .5f - (LS + RS) * .5f)) * LocalScaling);
                                break;
                            case TapAlgorithm.eTapMonoToMonoLPFInterpolate:
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                    Tap.LowpassFilter,
                                    ((LS + RS) * .5f + Tap.SourceOffsetFraction
                                        * ((LS1 + RS1) * .5f - (LS + RS) * .5f)) * LocalScaling);
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapMonoToRightLPFInterpolate:
                                LS = LeftDelayLineArray[SourceIndex];
                                LS1 = LeftDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RT = RightDelayLineArray[TargetIndex];
                                RightDelayLineArray[TargetIndex] = RT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                        Tap.LowpassFilter,
                                        ((LS + RS) * .5f + Tap.SourceOffsetFraction
                                            * ((LS1 + RS1) * .5f - (LS + RS) * .5f)) * LocalScaling);
                                break;
                            case TapAlgorithm.eTapRightToLeftLPFInterpolate:
                                LT = LeftDelayLineArray[TargetIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LeftDelayLineArray[TargetIndex] = LT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                        Tap.LowpassFilter,
                                        (RS + Tap.SourceOffsetFraction * (RS1 - RS)) * LocalScaling);
                                break;
                            case TapAlgorithm.eTapRightToMonoLPFInterpolate:
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                LT = LeftDelayLineArray[TargetIndex];
                                RT = RightDelayLineArray[TargetIndex];
                                Temp = FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                    Tap.LowpassFilter,
                                    (RS + Tap.SourceOffsetFraction * (RS1 - RS)) * LocalScaling);
                                LeftDelayLineArray[TargetIndex] = LT + Temp;
                                RightDelayLineArray[TargetIndex] = RT + Temp;
                                break;
                            case TapAlgorithm.eTapRightToRightLPFInterpolate:
                                RT = RightDelayLineArray[TargetIndex];
                                RS = RightDelayLineArray[SourceIndex];
                                RS1 = RightDelayLineArray[VectorMask & (SourceIndex + 1)];
                                RightDelayLineArray[TargetIndex] = RT +
                                    FirstOrderLowpassRec.ApplyFirstOrderLowpass(
                                        Tap.LowpassFilter,
                                        (RS + Tap.SourceOffsetFraction * (RS1 - RS)) * LocalScaling);
                                break;
                        }
                    }

                    /* perform data update */
                    workspace[Scan + lOffset] = LeftDelayLineArray[VectorIndex];
                    workspace[Scan + rOffset] = RightDelayLineArray[VectorIndex];

                    /* increment index */
                    VectorIndex = (VectorIndex - 1) & VectorMask;
                }

                /* write back index */
                Delay.DelayLineIndex = VectorIndex;
            }

            // The following loop showed a 7% improvement in the "unsafe" "fixed" form eliminating array bounds checks.
            // (on Pentium N3520)

            private unsafe static void StereoDelayLineProcessorSimple(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                DelayUnifiedRec Delay)
            {
                int VectorMask = Delay.VectorMask;
                int VectorIndex = Delay.DelayLineIndex;
                float[] LeftDelayLineArray = Delay.LeftDelayLineArray;
                float[] RightDelayLineArray = Delay.RightDelayLineArray;
                DelayLineTapRec[] TapVector = Delay.TapVector;

                fixed (float* pLeftDelayLineArray = LeftDelayLineArray)
                {
                    fixed (float* pRightDelayLineArray = RightDelayLineArray)
                    {
                        fixed (float* pWorkspace = workspace)
                        {
                            for (int frame = 0; frame < nActualFrames; frame++)
                            {
                                /* initialize current point with dry data */
                                LeftDelayLineArray[VectorIndex] = pWorkspace[frame + lOffset];
                                RightDelayLineArray[VectorIndex] = pWorkspace[frame + rOffset];

                                /* iterate over taps */
                                for (int TapScan = 0; TapScan < TapVector.Length; TapScan++)
                                {
                                    /* initialize base pointer */
                                    DelayLineTapRec Tap = TapVector[TapScan];

#if DEBUG
                                    if ((Tap.TapProcessAlgorithm != TapAlgorithm.eTapLeftToLeft)
                                        && (Tap.TapProcessAlgorithm != TapAlgorithm.eTapLeftToRight)
                                        && (Tap.TapProcessAlgorithm != TapAlgorithm.eTapRightToLeft)
                                        && (Tap.TapProcessAlgorithm != TapAlgorithm.eTapRightToRight)
                                        && (Tap.TapProcessAlgorithm != TapAlgorithm.eTapLeftToLeftLPF)
                                        && (Tap.TapProcessAlgorithm != TapAlgorithm.eTapLeftToRightLPF)
                                        && (Tap.TapProcessAlgorithm != TapAlgorithm.eTapRightToLeftLPF)
                                        && (Tap.TapProcessAlgorithm != TapAlgorithm.eTapRightToRightLPF))
                                    {
                                        // complex opcode encountered
                                        Debug.Assert(false);
                                        throw new ArgumentException();
                                    }
#endif

                                    /* get line arrays */
                                    float* pSourceLine = Tap.SourceLine == 0 ? pLeftDelayLineArray : pRightDelayLineArray;
                                    float* pTargetLine = Tap.TargetLine == 0 ? pLeftDelayLineArray : pRightDelayLineArray;

                                    /* precompute some stuff */
                                    int SourceIndex = VectorMask & (VectorIndex + Tap.SourceOffsetInteger);
                                    int TargetIndex = VectorMask & (VectorIndex + Tap.TargetOffsetInteger);

                                    /* load source and target values */
                                    float S = pSourceLine[SourceIndex];
                                    float T = pTargetLine[TargetIndex];

                                    /* compute */
                                    S = S * Tap.Scaling;

                                    /* apply filter if necessary */
                                    if (Tap.LowpassFilterEnabled)
                                    {
                                        S = FirstOrderLowpassRec.ApplyFirstOrderLowpass(Tap.LowpassFilter, S);
                                    }

                                    /* store result back */
                                    pTargetLine[TargetIndex] = T + S;
                                }

                                /* perform data update */
                                pWorkspace[frame + lOffset] = pLeftDelayLineArray[VectorIndex];
                                pWorkspace[frame + rOffset] = pRightDelayLineArray[VectorIndex];

                                /* increment index */
                                VectorIndex = (VectorIndex - 1) & VectorMask;
                            }
                        }
                    }
                }

                /* write back index */
                Delay.DelayLineIndex = VectorIndex;
            }

            /* apply delay processing to some stuff to mono or stereo data */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                if (this.ComplexCaseRequired)
                {
                    StereoDelayLineProcessorComplex(
                        workspace,
                        lOffset,
                        rOffset,
                        nActualFrames,
                        this);
                }
                else
                {
                    StereoDelayLineProcessorSimple(
                        workspace,
                        lOffset,
                        rOffset,
                        nActualFrames,
                        this);
                }

                return SynthErrorCodes.eSynthDone;
            }

            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                if (this.Type == DelayType.eTypeOscillatorDelay)
                {
                    UnifiedDelayCtrlRec[] CtrlVector = this.CtrlVector;
                    for (int Scan = 0; Scan < CtrlVector.Length; Scan++)
                    {
                        FreeEnvelopeStateRecord(
                            ref CtrlVector[Scan].Osc.SourceEnvelope,
                            SynthParams);
                        FreeLFOGenerator(
                            ref CtrlVector[Scan].Osc.SourceLFO,
                            SynthParams);

                        FreeEnvelopeStateRecord(
                            ref CtrlVector[Scan].Osc.TargetEnvelope,
                            SynthParams);
                        FreeLFOGenerator(
                            ref CtrlVector[Scan].Osc.TargetLFO,
                            SynthParams);

                        FreeEnvelopeStateRecord(
                            ref CtrlVector[Scan].Osc.ScaleEnvelope,
                            SynthParams);
                        FreeLFOGenerator(
                            ref CtrlVector[Scan].Osc.ScaleLFO,
                            SynthParams);

                        Debug.Assert((CtrlVector[Scan].Osc.CutoffEnvelope != null)
                            == (CtrlVector[Scan].Osc.CutoffLFO != null));
                        if (CtrlVector[Scan].Osc.CutoffEnvelope != null)
                        {
                            FreeEnvelopeStateRecord(
                                ref CtrlVector[Scan].Osc.CutoffEnvelope,
                                SynthParams);
                        }
                        if (CtrlVector[Scan].Osc.CutoffLFO != null)
                        {
                            FreeLFOGenerator(
                                ref CtrlVector[Scan].Osc.CutoffLFO,
                                SynthParams);
                        }
                    }
                }

                Free(ref SynthParams.freelists.FloatBufferFreeList, ref this.LeftDelayLineArray);
                Free(ref SynthParams.freelists.FloatBufferFreeList, ref this.RightDelayLineArray);
            }
        }
    }
}
