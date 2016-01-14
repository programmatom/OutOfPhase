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
        /* delay tap sources */
        public enum DelayChannelType
        {
            eTapLeftChannel,
            eTapRightChannel,
            eTapMonoChannel,
        }

        public class DelayTapRec
        {
            /* tap control parameters */
            public DelayChannelType SourceTap;
            public DelayChannelType TargetTap;
            public bool Interpolate;

            /* these controls only apply to the track effect, not the oscillator effect */
            public double SourceTime;
            public AccentRec SourceTimeAccent;
            public double TargetTime;
            public AccentRec TargetTimeAccent;
            public double ScaleFactor;
            public AccentRec ScaleFactorAccent;
            public bool FilterEnable;
            public double FilterCutoff;
            public AccentRec FilterCutoffAccent;
            public PcodeRec SourceTimeFormula;
            public PcodeRec TargetTimeFormula;
            public PcodeRec ScaleFactorFormula;
            public PcodeRec FilterCutoffFormula;

            /* these controls only apply to the oscillator effect, not the track effect */
            public EnvelopeRec SourceTimeEnvelope;
            public EnvelopeRec TargetTimeEnvelope;
            public EnvelopeRec ScaleFactorEnvelope;
            public EnvelopeRec CutoffEnvelope;
            public LFOListSpecRec SourceTimeLFO;
            public LFOListSpecRec TargetTimeLFO;
            public LFOListSpecRec ScaleFactorLFO;
            public LFOListSpecRec CutoffLFO;
        }

        /* structure for whole delay line */
        public class DelayEffectRec
        {
            /* maximum delay */
            public double MaxDelayTime;
            public PcodeRec MaxDelayTimeFormula;

            /* tap list */
            public DelayTapRec[] List;
        }

        /* create a new delay line specification */
        public static DelayEffectRec NewDelayLineSpec()
        {
            DelayEffectRec DelaySpec = new DelayEffectRec();

            DelaySpec.List = new DelayTapRec[0];
            //DelaySpec.MaxDelayTime = 0;

            return DelaySpec;
        }

        public static void SetDelayMaxTime(
            DelayEffectRec DelaySpec,
            double MaxTime)
        {
            DelaySpec.MaxDelayTime = MaxTime;
        }

        public static void SetDelayMaxTimeFormula(
            DelayEffectRec DelaySpec,
            PcodeRec MaxTimeFormula)
        {
            DelaySpec.MaxDelayTimeFormula = MaxTimeFormula;
        }

        public static double GetDelayMaxTime(DelayEffectRec DelaySpec)
        {
            return DelaySpec.MaxDelayTime;
        }

        public static PcodeRec GetDelayMaxTimeFormula(DelayEffectRec DelaySpec)
        {
            return DelaySpec.MaxDelayTimeFormula;
        }

        /* create a new tap record */
        public static DelayTapRec NewDelayTap()
        {
            DelayTapRec Tap = new DelayTapRec();

            Tap.SourceTimeEnvelope = NewEnvelope();
            Tap.TargetTimeEnvelope = NewEnvelope();
            Tap.ScaleFactorEnvelope = NewEnvelope();
            Tap.SourceTimeLFO = NewLFOListSpecifier();
            Tap.TargetTimeLFO = NewLFOListSpecifier();
            Tap.ScaleFactorLFO = NewLFOListSpecifier();
            Tap.CutoffEnvelope = NewEnvelope();
            Tap.CutoffLFO = NewLFOListSpecifier();
            //InitializeAccentZero(out Tap.SourceTimeAccent);
            //InitializeAccentZero(out Tap.TargetTimeAccent);
            //InitializeAccentZero(out Tap.ScaleFactorAccent);
            //InitializeAccentZero(out Tap.FilterCutoffAccent);
            Tap.SourceTap = DelayChannelType.eTapMonoChannel;
            //Tap.SourceTime = 0;
            Tap.TargetTap = DelayChannelType.eTapMonoChannel;
            //Tap.TargetTime = 0;
            //Tap.ScaleFactor = 0;
            //Tap.FilterEnable = false;
            //Tap.FilterCutoff = 0;
            //Tap.Interpolate = false;
            //Tap.SourceTimeFormula = null;
            //Tap.TargetTimeFormula = null;
            //Tap.ScaleFactorFormula = null;
            //Tap.FilterCutoffFormula = null;

            return Tap;
        }

        public static void SetDelayTapSource(
            DelayTapRec Tap,
            DelayChannelType Source)
        {
            Tap.SourceTap = Source;
        }

        public static void SetDelayTapSourceTime(
            DelayTapRec Tap,
            double Time,
            PcodeRec Formula)
        {
#if DEBUG
            if (Tap.SourceTimeFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Tap.SourceTimeFormula = Formula;
            Tap.SourceTime = Time;
        }

        public static void SetDelayTapSourceTimeAccent(
            DelayTapRec Tap,
            double Value,
            int AccentNumber)
        {
            SetAccentMemberValue(ref Tap.SourceTimeAccent, AccentNumber, Value);
        }

        public static void SetDelayTapTarget(
            DelayTapRec Tap,
            DelayChannelType Target)
        {
            Tap.TargetTap = Target;
        }

        public static void SetDelayTapTargetTime(
            DelayTapRec Tap,
            double Time,
            PcodeRec Formula)
        {
#if DEBUG
            if (Tap.TargetTimeFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Tap.TargetTimeFormula = Formula;
            Tap.TargetTime = Time;
        }

        public static void SetDelayTapTargetTimeAccent(
            DelayTapRec Tap,
            double Value,
            int AccentNumber)
        {
            SetAccentMemberValue(ref Tap.TargetTimeAccent, AccentNumber, Value);
        }

        public static void SetDelayTapScale(
            DelayTapRec Tap,
            double Scale,
            PcodeRec Formula)
        {
#if DEBUG
            if (Tap.ScaleFactorFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Tap.ScaleFactorFormula = Formula;
            Tap.ScaleFactor = Scale;
        }

        public static void SetDelayTapScaleAccent(
            DelayTapRec Tap,
            double Value,
            int AccentNumber)
        {
            SetAccentMemberValue(ref Tap.ScaleFactorAccent, AccentNumber, Value);
        }

        public static void SetDelayTapFilterEnable(
            DelayTapRec Tap,
            bool EnableFilter)
        {
            Tap.FilterEnable = EnableFilter;
        }

        public static void SetDelayTapFilterCutoff(
            DelayTapRec Tap,
            double Cutoff,
            PcodeRec Formula)
        {
#if DEBUG
            if (Tap.FilterCutoffFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Tap.FilterCutoffFormula = Formula;
            Tap.FilterCutoff = Cutoff;
        }

        public static void SetDelayTapFilterCutoffAccent(
            DelayTapRec Tap,
            double Value,
            int AccentNumber)
        {
            SetAccentMemberValue(ref Tap.FilterCutoffAccent, AccentNumber, Value);
        }

        public static void SetDelayTapInterpolateFlag(
            DelayTapRec Tap,
            bool Interpolate)
        {
            Tap.Interpolate = Interpolate;
        }

        /* append tap to list in delay line */
        public static void AppendTapToDelayEffect(
            DelayEffectRec DelaySpec,
            DelayTapRec Tap)
        {
            Array.Resize(ref DelaySpec.List, DelaySpec.List.Length + 1);
            DelaySpec.List[DelaySpec.List.Length - 1] = Tap;
        }

        /* get the number of taps in the delay line */
        public static int GetDelayEffectSpecNumTaps(DelayEffectRec DelaySpec)
        {
            return DelaySpec.List.Length;
        }

        /* get a specified tap from the delay line */
        public static DelayTapRec GetTapFromDelayEffectSpec(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return DelaySpec.List[Index];
        }

        public static DelayChannelType GetDelayTapSource(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).SourceTap;
        }

        public static void GetDelayTapSourceTimeAgg(
            DelayEffectRec DelaySpec,
            int Index,
            out ScalarParamEvalRec ParamsOut)
        {
            DelayTapRec Tap = DelaySpec.List[Index];
            InitScalarParamEval(
                Tap.SourceTime,
                ref Tap.SourceTimeAccent,
                Tap.SourceTimeFormula,
                out ParamsOut);
        }

        public static DelayChannelType GetDelayTapTarget(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).TargetTap;
        }

        public static void GetDelayTapTargetTimeAgg(
            DelayEffectRec DelaySpec,
            int Index,
            out ScalarParamEvalRec ParamsOut)
        {
            DelayTapRec Tap = DelaySpec.List[Index];
            InitScalarParamEval(
                Tap.TargetTime,
                ref Tap.TargetTimeAccent,
                Tap.TargetTimeFormula,
                out ParamsOut);
        }

        public static void GetDelayTapScaleAgg(
            DelayEffectRec DelaySpec,
            int Index,
            out ScalarParamEvalRec ParamsOut)
        {
            DelayTapRec Tap = DelaySpec.List[Index];
            InitScalarParamEval(
                Tap.ScaleFactor,
                ref Tap.ScaleFactorAccent,
                Tap.ScaleFactorFormula,
                out ParamsOut);
        }

        public static bool GetDelayTapFilterEnable(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).FilterEnable;
        }

        public static void GetDelayTapFilterCutoffAgg(
            DelayEffectRec DelaySpec,
            int Index,
            out ScalarParamEvalRec ParamsOut)
        {
            DelayTapRec Tap = DelaySpec.List[Index];
            InitScalarParamEval(
                Tap.FilterCutoff,
                ref Tap.FilterCutoffAccent,
                Tap.FilterCutoffFormula,
                out ParamsOut);
        }

        public static EnvelopeRec GetDelayTapSourceEnvelope(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).SourceTimeEnvelope;
        }

        public static EnvelopeRec GetDelayTapTargetEnvelope(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).TargetTimeEnvelope;
        }

        public static EnvelopeRec GetDelayTapScaleEnvelope(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).ScaleFactorEnvelope;
        }

        public static EnvelopeRec GetDelayTapCutoffEnvelope(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).CutoffEnvelope;
        }

        public static LFOListSpecRec GetDelayTapSourceLFO(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).SourceTimeLFO;
        }

        public static LFOListSpecRec GetDelayTapTargetLFO(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).TargetTimeLFO;
        }

        public static LFOListSpecRec GetDelayTapScaleLFO(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).ScaleFactorLFO;
        }

        public static LFOListSpecRec GetDelayTapCutoffLFO(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).CutoffLFO;
        }

        public static bool GetDelayTapInterpolateFlag(
            DelayEffectRec DelaySpec,
            int Index)
        {
            return GetTapFromDelayEffectSpec(DelaySpec, Index).Interpolate;
        }
    }
}
