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
        /* effect types */
        public enum EffectTypes
        {
            eDelayEffect,
            eNLProcEffect,
            eFilterEffect,
            eAnalyzerEffect,
            eHistogramEffect,
            eResamplerEffect,
            eCompressorEffect,
            eVocoderEffect,
            eIdealLowpassEffect,
            eConvolverEffect,
            eUserEffect,
            ePluggableEffect,
        }

        [StructLayout(LayoutKind.Auto)]
        public struct EffectNodeRec
        {
            public EffectTypes Type;
            public object u;
            public bool EnabledFlag;
        }

        public class EffectSpecListRec
        {
            public EffectNodeRec[] List;

            /* stuff for automatic quiescence detection */
            public bool AutoQuiescenceEnabled;
            public bool PrintReport;
            public double Decibels;
            public double WindowDuration;

            /* stuff for suppressing initial silence */
            public bool SuppressInitialSilence;
        }

        /* create a new effect list */
        public static EffectSpecListRec NewEffectSpecList()
        {
            EffectSpecListRec EffectSpecList = new EffectSpecListRec();

            EffectSpecList.List = new EffectNodeRec[0];
            //EffectSpecList.AutoQuiescenceEnabled = false;
            //EffectSpecList.SuppressInitialSilence = false;

            return EffectSpecList;
        }

        private static void AddGenericToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            EffectTypes Type,
            object GenericSpec,
            bool EnabledFlag)
        {
#if DEBUG
            if (!(((GenericSpec is DelayEffectRec) && (Type == EffectTypes.eDelayEffect))
                || ((GenericSpec is NonlinProcSpecRec) && (Type == EffectTypes.eNLProcEffect))
                || ((GenericSpec is FilterSpecRec) && (Type == EffectTypes.eFilterEffect))
                || ((GenericSpec is AnalyzerSpecRec) && (Type == EffectTypes.eAnalyzerEffect))
                || ((GenericSpec is HistogramSpecRec) && (Type == EffectTypes.eHistogramEffect))
                || ((GenericSpec is ResamplerSpecRec) && (Type == EffectTypes.eResamplerEffect))
                || ((GenericSpec is CompressorSpecRec) && (Type == EffectTypes.eCompressorEffect))
                || ((GenericSpec is VocoderSpecRec) && (Type == EffectTypes.eVocoderEffect))
                || ((GenericSpec is IdealLPSpecRec) && (Type == EffectTypes.eIdealLowpassEffect))
                || ((GenericSpec is ConvolverSpecRec) && (Type == EffectTypes.eConvolverEffect))
                || ((GenericSpec is UserEffectSpecRec) && (Type == EffectTypes.eUserEffect))
                || ((GenericSpec is PluggableSpec) && (Type == EffectTypes.ePluggableEffect))))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            EffectNodeRec Effect = new EffectNodeRec();

            Effect.Type = Type;
            Effect.u = GenericSpec;
            Effect.EnabledFlag = EnabledFlag;

            Array.Resize(ref EffectSpecList.List, EffectSpecList.List.Length + 1);
            EffectSpecList.List[EffectSpecList.List.Length - 1] = Effect;
        }

        /* add a delay effect to the spec list */
        public static void AddDelayToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            DelayEffectRec DelaySpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eDelayEffect, DelaySpec, EnabledFlag);
        }

        /* add a nonlinear processor to the spec list */
        public static void AddNLProcToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            NonlinProcSpecRec NLProcSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eNLProcEffect, NLProcSpec, EnabledFlag);
        }

        /* add a parallel filter array to the spec list */
        public static void AddFilterToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            FilterSpecRec FilterSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eFilterEffect, FilterSpec, EnabledFlag);
        }

        /* add an analyzer to the spec list */
        public static void AddAnalyzerToEffectSpecList(
                                                    EffectSpecListRec EffectSpecList,
                                                    AnalyzerSpecRec AnalyzerSpec,
                                                    bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eAnalyzerEffect, AnalyzerSpec, EnabledFlag);
        }

        /* add an histogram to the spec list */
        public static void AddHistogramToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            HistogramSpecRec HistogramSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eHistogramEffect, HistogramSpec, EnabledFlag);
        }

        /* add a resampler to the spec list */
        public static void AddResamplerToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            ResamplerSpecRec ResamplerSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eResamplerEffect, ResamplerSpec, EnabledFlag);
        }

        /* add a compressor to the spec list */
        public static void AddCompressorToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            CompressorSpecRec CompressorSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eCompressorEffect, CompressorSpec, EnabledFlag);
        }

        /* add a vocoder to the spec list */
        public static void AddVocoderToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            VocoderSpecRec VocSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eVocoderEffect, VocSpec, EnabledFlag);
        }

        /* add an ideal lowpass filter to the spec list */
        public static void AddIdealLPToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            IdealLPSpecRec IdealLPSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eIdealLowpassEffect, IdealLPSpec, EnabledFlag);
        }

        /* add a convolver to the spec list */
        public static void AddConvolverToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            ConvolverSpecRec ConvolverSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eConvolverEffect, ConvolverSpec, EnabledFlag);
        }

        /* add a user effect to the spec list */
        public static void AddUserEffectToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            UserEffectSpecRec UserEffectSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.eUserEffect, UserEffectSpec, EnabledFlag);
        }

        /* add a user effect to the spec list */
        public static void AddPluggableEffectToEffectSpecList(
            EffectSpecListRec EffectSpecList,
            PluggableSpec PluggableEffectSpec,
            bool EnabledFlag)
        {
            AddGenericToEffectSpecList(EffectSpecList, EffectTypes.ePluggableEffect, PluggableEffectSpec, EnabledFlag);
        }

        /* find out how many effects are in the list */
        public static int GetEffectSpecListLength(EffectSpecListRec EffectSpecList)
        {
            return EffectSpecList.List.Length;
        }

        public static int GetEffectSpecListEnabledLength(EffectSpecListRec EffectSpecList)
        {
            int count = 0;
            for (int i = 0; i < EffectSpecList.List.Length; i++)
            {
                if (IsEffectFromEffectSpecListEnabled(EffectSpecList, i))
                {
                    count++;
                }
            }
            return count;
        }

        /* get the type of the specified effect */
        public static EffectTypes GetEffectSpecListElementType(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return EffectSpecList.List[Index].Type;
        }

        public static T GetGenericEffectFromEffectSpecList<T>(
            EffectSpecListRec EffectSpecList,
            int Index,
            EffectTypes Type)
        {
#if DEBUG
            if ((EffectSpecList.List[Index].Type != Type)
                || !(EffectSpecList.List[Index].u is T))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return (T)EffectSpecList.List[Index].u;
        }

        /* get the delay effect at the specified index */
        public static DelayEffectRec GetDelayEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<DelayEffectRec>(EffectSpecList, Index, EffectTypes.eDelayEffect);
        }

        /* get the nonlinear processor from the specified index */
        public static NonlinProcSpecRec GetNLProcEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<NonlinProcSpecRec>(EffectSpecList, Index, EffectTypes.eNLProcEffect);
        }

        /* get the filter from the specified index */
        public static FilterSpecRec GetFilterEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<FilterSpecRec>(EffectSpecList, Index, EffectTypes.eFilterEffect);
        }

        public static AnalyzerSpecRec GetAnalyzerEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<AnalyzerSpecRec>(EffectSpecList, Index, EffectTypes.eAnalyzerEffect);
        }

        public static HistogramSpecRec GetHistogramEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<HistogramSpecRec>(EffectSpecList, Index, EffectTypes.eHistogramEffect);
        }

        public static ResamplerSpecRec GetResamplerEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<ResamplerSpecRec>(EffectSpecList, Index, EffectTypes.eResamplerEffect);
        }

        /* get the compressor from the specified index */
        public static CompressorSpecRec GetCompressorEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<CompressorSpecRec>(EffectSpecList, Index, EffectTypes.eCompressorEffect);
        }

        /* get the vocoder from the specified index */
        public static VocoderSpecRec GetVocoderEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<VocoderSpecRec>(EffectSpecList, Index, EffectTypes.eVocoderEffect);
        }

        /* get the ideal lowpass filter from the specified index */
        public static IdealLPSpecRec GetIdealLPEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<IdealLPSpecRec>(EffectSpecList, Index, EffectTypes.eIdealLowpassEffect);
        }

        /* get the convolver from the specified index */
        public static ConvolverSpecRec GetConvolverEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<ConvolverSpecRec>(EffectSpecList, Index, EffectTypes.eConvolverEffect);
        }

        /* get the user effect from the specified index */
        public static UserEffectSpecRec GetUserEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<UserEffectSpecRec>(EffectSpecList, Index, EffectTypes.eUserEffect);
        }

        public static PluggableSpec GetPluggableEffectFromEffectSpecList(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return GetGenericEffectFromEffectSpecList<PluggableSpec>(EffectSpecList, Index, EffectTypes.ePluggableEffect);
        }

        /* find out if specified effect is enabled */
        public static bool IsEffectFromEffectSpecListEnabled(
            EffectSpecListRec EffectSpecList,
            int Index)
        {
            return EffectSpecList.List[Index].EnabledFlag;
        }

        /* enable automatic quiescence detection */
        public static void EffectSpecListEnableAutoQuiescence(
            EffectSpecListRec EffectSpecList,
            double Decibels,
            double WindowDuration,
            bool PrintReport)
        {
#if DEBUG
            if (Decibels < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (WindowDuration < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            EffectSpecList.AutoQuiescenceEnabled = true;
            EffectSpecList.Decibels = Decibels;
            EffectSpecList.WindowDuration = WindowDuration;
            EffectSpecList.PrintReport = PrintReport;
        }

        /* query is auto-quiescence enabled */
        public static bool EffectSpecListIsAutoQuiescenceEnabled(EffectSpecListRec EffectSpecList)
        {
            return EffectSpecList.AutoQuiescenceEnabled;
        }

        /* get decibel level for auto-quiescence */
        public static double EffectSpecListGetAutoQuiescenceDecibels(EffectSpecListRec EffectSpecList)
        {
#if DEBUG
            if (!EffectSpecList.AutoQuiescenceEnabled)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return EffectSpecList.Decibels;
        }

        /* get window duration for auto-quiescence */
        public static double EffectSpecListGetAutoQuiescenceWindowDuration(EffectSpecListRec EffectSpecList)
        {
#if DEBUG
            if (!EffectSpecList.AutoQuiescenceEnabled)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return EffectSpecList.WindowDuration;
        }

        /* get print-report flag for auto-quiescence */
        public static bool EffectSpecListGetAutoQuiescencePrintReport(EffectSpecListRec EffectSpecList)
        {
#if DEBUG
            if (!EffectSpecList.AutoQuiescenceEnabled)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return EffectSpecList.PrintReport;
        }

        /* set flag for suppressing initial silence (score effect only) */
        public static void EffectSpecListPutSuppressInitialSilence(
            EffectSpecListRec EffectSpecList,
            bool Suppress)
        {
            EffectSpecList.SuppressInitialSilence = Suppress;
        }

        /* get flag for suppressing initial silence (score effect only) */
        public static bool EffectSpecListGetSuppressInitialSilence(EffectSpecListRec EffectSpecList)
        {
            return EffectSpecList.SuppressInitialSilence;
        }
    }
}
