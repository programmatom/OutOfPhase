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
        public class OscOneEffectRec
        {
            public OscOneEffectRec Next;
            public IOscillatorEffect u;
        }

        public class OscEffectGenRec
        {
            /* list of effects records */
            public OscOneEffectRec List;
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

            OscEffectGenRec Generator = new OscEffectGenRec();

            int MaxPreOrigin = 0;

            /* build list of thingers */
            OscOneEffectRec Appender = null;
            int List = GetEffectSpecListLength(SpecList);
            for (int Scan = 0; Scan < List; Scan += 1)
            {
                /* see if effect is enabled */
                if (IsEffectFromEffectSpecListEnabled(SpecList, Scan))
                {
                    OscOneEffectRec Effect = new OscOneEffectRec();

                    /* append */
                    Effect.Next = null;
                    if (Appender == null)
                    {
                        Generator.List = Effect;
                    }
                    else
                    {
                        Appender.Next = Effect;
                    }
                    Appender = Effect;

                    /* fill in fields */
                    EffectTypes Type = GetEffectSpecListElementType(SpecList, Scan);
                    switch (Type)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case EffectTypes.eDelayEffect:
                            Effect.u = DelayUnifiedRec.NewOscUnifiedDelayLineProcessor(
                                GetDelayEffectFromEffectSpecList(SpecList, Scan),
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
                            Effect.u = NLProcUnifiedRec.NewOscNLProcProcessor(
                                GetNLProcEffectFromEffectSpecList(SpecList, Scan),
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
                            Effect.u = FilterArrayRec.NewOscFilterArrayProcessor(
                                GetFilterEffectFromEffectSpecList(SpecList, Scan),
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
                            Effect.u = AnalyzerRec.NewAnalyzer(
                                GetAnalyzerEffectFromEffectSpecList(SpecList, Scan),
                                SynthParams);
                            break;
                        case EffectTypes.eHistogramEffect:
                            Effect.u = HistogramRec.NewHistogram(
                                GetHistogramEffectFromEffectSpecList(SpecList, Scan),
                                SynthParams);
                            break;
                        case EffectTypes.eResamplerEffect:
                            Effect.u = ResamplerRec.NewResampler(
                                GetResamplerEffectFromEffectSpecList(SpecList, Scan),
                                SynthParams);
                            break;
                        case EffectTypes.eCompressorEffect:
                            Effect.u = CompressorRec.NewOscCompressor(
                                GetCompressorEffectFromEffectSpecList(SpecList, Scan),
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
                            Effect.u = VocoderRec.NewOscVocoder(
                                GetVocoderEffectFromEffectSpecList(SpecList, Scan),
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
                            Effect.u = IdealLPRec.NewIdealLP(
                                GetIdealLPEffectFromEffectSpecList(SpecList, Scan),
                                SynthParams);
                            break;
                        case EffectTypes.eUserEffect:
                            {
                                UserEffectProcRec userEffect;
                                SynthErrorCodes error = UserEffectProcRec.NewOscUserEffectProc(
                                    GetUserEffectFromEffectSpecList(SpecList, Scan),
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
                                Effect.u = userEffect;
                                if (OnePreOrigin > MaxPreOrigin)
                                {
                                    MaxPreOrigin = OnePreOrigin;
                                }
                            }
                            break;
                        case EffectTypes.ePluggableEffect:
                            {
                                PluggableSpec Spec = GetPluggableEffectFromEffectSpecList(SpecList, Scan);
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
                                Effect.u = effect;
                                if (OnePreOrigin > MaxPreOrigin)
                                {
                                    MaxPreOrigin = OnePreOrigin;
                                }
                            }
                            break;
                    }
                }
            }

            PreOriginTimeOut = MaxPreOrigin;

            GeneratorOut = Generator;
            return SynthErrorCodes.eSynthDone;
        }

        /* fix up pre-origin time for the oscillator effect generator */
        public static void FixUpOscEffectGeneratorPreOrigin(
            OscEffectGenRec Generator,
            int ActualPreOrigin)
        {
            OscOneEffectRec Scan;

            Scan = Generator.List;
            while (Scan != null)
            {
                Scan.u.OscFixEnvelopeOrigins(
                    ActualPreOrigin);
                Scan = Scan.Next;
            }
        }

        /* update envelopes for effects */
        public static SynthErrorCodes OscEffectGeneratorUpdateEnvelopes(
            OscEffectGenRec Generator,
            double OscillatorFrequency,
            SynthParamRec SynthParams)
        {
            OscOneEffectRec Scan;

            Scan = Generator.List;
            while (Scan != null)
            {
                SynthErrorCodes error = Scan.u.OscUpdateEnvelopes(
                    OscillatorFrequency,
                    SynthParams);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }
                Scan = Scan.Next;
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
            OscOneEffectRec Scan;

            Scan = Generator.List;
            while (Scan != null)
            {
                SynthErrorCodes error = Scan.u.Apply(
                    workspace,
                    lOffset,
                    rOffset,
                    nActualFrames,
                    SynthParams);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }
                Scan = Scan.Next;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* finalize before termination */
        public static void FinalizeOscEffectGenerator(
            OscEffectGenRec Generator,
            SynthParamRec SynthParams,
            bool writeOutputLogs)
        {
            OscOneEffectRec Scan;

            Scan = Generator.List;
            while (Scan != null)
            {
                Scan.u.Finalize(
                    SynthParams,
                    writeOutputLogs);
                Scan = Scan.Next;
            }
        }

        public static void OscEffectKeyUpSustain1(OscEffectGenRec Generator)
        {
            OscOneEffectRec Scan;

            Scan = Generator.List;
            while (Scan != null)
            {
                Scan.u.OscKeyUpSustain1();
                Scan = Scan.Next;
            }
        }

        public static void OscEffectKeyUpSustain2(OscEffectGenRec Generator)
        {
            OscOneEffectRec Scan;

            Scan = Generator.List;
            while (Scan != null)
            {
                Scan.u.OscKeyUpSustain2();
                Scan = Scan.Next;
            }
        }

        public static void OscEffectKeyUpSustain3(OscEffectGenRec Generator)
        {
            OscOneEffectRec Scan;

            Scan = Generator.List;
            while (Scan != null)
            {
                Scan.u.OscKeyUpSustain3();
                Scan = Scan.Next;
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
            OscOneEffectRec Scan;

            Scan = Generator.List;
            while (Scan != null)
            {
                Scan.u.OscRetriggerEnvelopes(
                    ref Accents,
                    HurryUp,
                    NewInitialFrequency,
                    ActuallyRetrigger,
                    SynthParams);
                Scan = Scan.Next;
            }
        }
    }
}
