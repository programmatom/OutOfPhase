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
        public class OscEffectGenRec
        {
            public int count;

            public IOscillatorEffect[] List;
        }

        /* create a new oscillator effect generator */
        public static SynthErrorCodes NewOscEffectGenerator(
            EffectSpecListRec SpecList,
            ref AccentRec Accents,
            double HurryUp,
            double InitialFrequency,
            double FreqForMultisampling,
            out int PreOriginTimeOut,
            PlayTrackInfoRec TrackInfo,
            SynthParamRec SynthParams,
            out OscEffectGenRec GeneratorOut)
        {
            int OnePreOrigin;

            GeneratorOut = null;
            PreOriginTimeOut = 0;

            OscEffectGenRec Generator = New(ref SynthParams.freelists.OscEffectGenRecFreeList);
            int count = Generator.count = GetEffectSpecListEnabledLength(SpecList);
            IOscillatorEffect[] List = Generator.List = New(ref SynthParams.freelists.IOscillatorEffectFreeList, count); // zeroed
            if (unchecked((uint)count > (uint)List.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }

            int MaxPreOrigin = 0;

            /* build list of thingers */
            int j = 0;
            for (int i = 0; j < count; i++)
            {
                /* see if effect is enabled */
                if (!IsEffectFromEffectSpecListEnabled(SpecList, i))
                {
                    continue;
                }

                /* fill in fields */
                EffectTypes Type = GetEffectSpecListElementType(SpecList, i);
                switch (Type)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case EffectTypes.eDelayEffect:
                        List[j] = DelayUnifiedRec.NewOscUnifiedDelayLineProcessor(
                            GetDelayEffectFromEffectSpecList(SpecList, i),
                            ref Accents,
                            HurryUp,
                            InitialFrequency,
                            FreqForMultisampling,
                            out OnePreOrigin,
                            TrackInfo,
                            SynthParams);
                        if (OnePreOrigin > MaxPreOrigin)
                        {
                            MaxPreOrigin = OnePreOrigin;
                        }
                        break;
                    case EffectTypes.eNLProcEffect:
                        List[j] = NLProcUnifiedRec.NewOscNLProcProcessor(
                            GetNLProcEffectFromEffectSpecList(SpecList, i),
                            ref Accents,
                            HurryUp,
                            InitialFrequency,
                            FreqForMultisampling,
                            out OnePreOrigin,
                            TrackInfo,
                            SynthParams);
                        if (OnePreOrigin > MaxPreOrigin)
                        {
                            MaxPreOrigin = OnePreOrigin;
                        }
                        break;
                    case EffectTypes.eFilterEffect:
                        List[j] = FilterArrayRec.NewOscFilterArrayProcessor(
                            GetFilterEffectFromEffectSpecList(SpecList, i),
                            ref Accents,
                            HurryUp,
                            InitialFrequency,
                            FreqForMultisampling,
                            out OnePreOrigin,
                            TrackInfo,
                            SynthParams);
                        if (OnePreOrigin > MaxPreOrigin)
                        {
                            MaxPreOrigin = OnePreOrigin;
                        }
                        break;
                    case EffectTypes.eAnalyzerEffect:
                        List[j] = AnalyzerRec.NewAnalyzer(
                            GetAnalyzerEffectFromEffectSpecList(SpecList, i),
                            SynthParams);
                        break;
                    case EffectTypes.eHistogramEffect:
                        List[j] = HistogramRec.NewHistogram(
                            GetHistogramEffectFromEffectSpecList(SpecList, i),
                            SynthParams);
                        break;
                    case EffectTypes.eResamplerEffect:
                        List[j] = ResamplerRec.NewResampler(
                            GetResamplerEffectFromEffectSpecList(SpecList, i),
                            SynthParams);
                        break;
                    case EffectTypes.eCompressorEffect:
                        List[j] = CompressorRec.NewOscCompressor(
                            GetCompressorEffectFromEffectSpecList(SpecList, i),
                            ref Accents,
                            HurryUp,
                            InitialFrequency,
                            FreqForMultisampling,
                            out OnePreOrigin,
                            TrackInfo,
                            SynthParams);
                        if (OnePreOrigin > MaxPreOrigin)
                        {
                            MaxPreOrigin = OnePreOrigin;
                        }
                        break;
                    case EffectTypes.eVocoderEffect:
                        List[j] = VocoderRec.NewOscVocoder(
                            GetVocoderEffectFromEffectSpecList(SpecList, i),
                            ref Accents,
                            HurryUp,
                            InitialFrequency,
                            FreqForMultisampling,
                            out OnePreOrigin,
                            TrackInfo,
                            SynthParams);
                        if (OnePreOrigin > MaxPreOrigin)
                        {
                            MaxPreOrigin = OnePreOrigin;
                        }
                        break;
                    case EffectTypes.eIdealLowpassEffect:
                        List[j] = IdealLPRec.NewIdealLP(
                            GetIdealLPEffectFromEffectSpecList(SpecList, i),
                            SynthParams);
                        break;
                    case EffectTypes.eUserEffect:
                        {
                            UserEffectProcRec userEffect;
                            SynthErrorCodes error = UserEffectProcRec.NewOscUserEffectProc(
                                GetUserEffectFromEffectSpecList(SpecList, i),
                                ref Accents,
                                HurryUp,
                                InitialFrequency,
                                FreqForMultisampling,
                                out OnePreOrigin,
                                TrackInfo,
                                SynthParams,
                                out userEffect);
                            if (error != SynthErrorCodes.eSynthDone)
                            {
                                return error;
                            }
                            List[j] = userEffect;
                            if (OnePreOrigin > MaxPreOrigin)
                            {
                                MaxPreOrigin = OnePreOrigin;
                            }
                        }
                        break;
                    case EffectTypes.ePluggableEffect:
                        {
                            PluggableSpec Spec = GetPluggableEffectFromEffectSpecList(SpecList, i);
                            Debug.Assert(Spec is PluggableOscSpec);
                            PluggableOscEffectTemplate template = new PluggableOscEffectTemplate(
                                (PluggableOscSpec)Spec,
                                SynthParams);
                            IOscillatorEffect effect;
                            SynthErrorCodes error = template.Create(
                                ref Accents,
                                HurryUp,
                                InitialFrequency,
                                FreqForMultisampling,
                                out OnePreOrigin,
                                TrackInfo,
                                SynthParams,
                                out effect);
                            if (error != SynthErrorCodes.eSynthDone)
                            {
                                return error;
                            }
                            List[j] = effect;
                            if (OnePreOrigin > MaxPreOrigin)
                            {
                                MaxPreOrigin = OnePreOrigin;
                            }
                        }
                        break;
                }

                j++;
            }
            Debug.Assert(j == count);

            PreOriginTimeOut = MaxPreOrigin;

            GeneratorOut = Generator;
            return SynthErrorCodes.eSynthDone;
        }

        /* fix up pre-origin time for the oscillator effect generator */
        public static void FixUpOscEffectGeneratorPreOrigin(
            OscEffectGenRec Generator,
            int ActualPreOrigin)
        {
            int count = Generator.count;
            IOscillatorEffect[] List = Generator.List;
            if (unchecked((uint)count > (uint)List.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                List[i].OscFixEnvelopeOrigins(
                    ActualPreOrigin);
            }
        }

        /* update envelopes for effects */
        public static SynthErrorCodes OscEffectGeneratorUpdateEnvelopes(
            OscEffectGenRec Generator,
            double OscillatorFrequency,
            SynthParamRec SynthParams)
        {
            int count = Generator.count;
            IOscillatorEffect[] List = Generator.List;
            if (unchecked((uint)count > (uint)List.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                SynthErrorCodes error = List[i].OscUpdateEnvelopes(
                    OscillatorFrequency,
                    SynthParams);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* generate effect cycle.  this is called once per envelope tick to apply */
        /* effects to data generated during this envelope clock cycle. */
        public static SynthErrorCodes ApplyOscEffectGenerator(
            OscEffectGenRec Generator,
            float[] workspace,
            int lOffset,
            int rOffset,
            int nActualFrames,
            SynthParamRec SynthParams)
        {
            int count = Generator.count;
            IOscillatorEffect[] List = Generator.List;
            if (unchecked((uint)count > (uint)List.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                SynthErrorCodes error = List[i].Apply(
                    workspace,
                    lOffset,
                    rOffset,
                    nActualFrames,
                    SynthParams);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* finalize before termination */
        public static void FinalizeOscEffectGenerator(
            OscEffectGenRec Generator,
            SynthParamRec SynthParams,
            bool writeOutputLogs)
        {
            int count = Generator.count;
            IOscillatorEffect[] List = Generator.List;
            if (unchecked((uint)count > (uint)List.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                List[i].Finalize(
                    SynthParams,
                    writeOutputLogs);
            }

            Free(ref SynthParams.freelists.IOscillatorEffectFreeList, ref Generator.List);
            Free(ref SynthParams.freelists.OscEffectGenRecFreeList, ref Generator);
        }

        public static void OscEffectKeyUpSustain1(OscEffectGenRec Generator)
        {
            int count = Generator.count;
            IOscillatorEffect[] List = Generator.List;
            if (unchecked((uint)count > (uint)List.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                List[i].OscKeyUpSustain1();
            }
        }

        public static void OscEffectKeyUpSustain2(OscEffectGenRec Generator)
        {
            int count = Generator.count;
            IOscillatorEffect[] List = Generator.List;
            if (unchecked((uint)count > (uint)List.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                List[i].OscKeyUpSustain2();
            }
        }

        public static void OscEffectKeyUpSustain3(OscEffectGenRec Generator)
        {
            int count = Generator.count;
            IOscillatorEffect[] List = Generator.List;
            if (unchecked((uint)count > (uint)List.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                List[i].OscKeyUpSustain3();
            }
        }

        /* retrigger effect envelopes from the origin point */
        public static void OscEffectGeneratorRetriggerFromOrigin(
            OscEffectGenRec Generator,
            ref AccentRec Accents,
            double NewInitialFrequency,
            double HurryUp,
            bool ActuallyRetrigger,
            SynthParamRec SynthParams)
        {
            int count = Generator.count;
            IOscillatorEffect[] List = Generator.List;
            if (unchecked((uint)count > (uint)List.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                List[i].OscRetriggerEnvelopes(
                    ref Accents,
                    HurryUp,
                    NewInitialFrequency,
                    ActuallyRetrigger,
                    SynthParams);
            }
        }
    }
}
