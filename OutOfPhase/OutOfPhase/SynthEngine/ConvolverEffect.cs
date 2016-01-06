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
        public class ConvolverRec : ITrackEffect
        {
            public IConvolution ConvStream0;
            public IConvolution ConvStream1;
            public IConvolution ConvStream2;
            public IConvolution ConvStream3;

            public double CurrentDirectGain;
            public double CurrentProcessedGain;
            public ScalarParamEvalRec DirectGain;
            public ScalarParamEvalRec ProcessedGain;

            public ConvolveSrcType Type;


            public delegate string ConvolverSpecGetImpulseResponseMethod(ConvolverSpecRec Spec);
            private struct ConvRuleRec
            {
                public ConvolverSpecGetImpulseResponseMethod ConvolverSpecGetImpulseResponse;

                public ConvRuleRec(ConvolverSpecGetImpulseResponseMethod ConvolverSpecGetImpulseResponse)
                {
                    this.ConvolverSpecGetImpulseResponse = ConvolverSpecGetImpulseResponse;
                }
            }
            private static readonly ConvRuleRec[] ConvMonoRule = new ConvRuleRec[]
	        {
		        new ConvRuleRec(ConvolverSpecGetImpulseResponseMono),
		        new ConvRuleRec(ConvolverSpecGetImpulseResponseMono),
	        };
            private static readonly ConvRuleRec[] ConvStereoRule = new ConvRuleRec[]
	        {
		        new ConvRuleRec(ConvolverSpecGetImpulseResponseStereoLeft),
		        new ConvRuleRec(ConvolverSpecGetImpulseResponseStereoRight),
	        };
            private static readonly ConvRuleRec[] ConvBiStereoRule = new ConvRuleRec[]
	        {
		        new ConvRuleRec(ConvolverSpecGetImpulseResponseBiStereoLeftIntoLeft),
		        new ConvRuleRec(ConvolverSpecGetImpulseResponseBiStereoRightIntoLeft),
		        new ConvRuleRec(ConvolverSpecGetImpulseResponseBiStereoLeftIntoRight),
		        new ConvRuleRec(ConvolverSpecGetImpulseResponseBiStereoRightIntoRight),
	        };

            /* create a new convolver */
            public static SynthErrorCodes NewConvolver(
                ConvolverSpecRec Template,
                SynthParamRec SynthParams,
                out ConvolverRec ConvolverOut)
            {
                ConvolverOut = null;

                ConvolverRec Convolver = new ConvolverRec();

                Convolver.Type = ConvolverSpecGetSourceType(Template);
                ConvRuleRec[] RuleArray;
                switch (Convolver.Type)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case ConvolveSrcType.eConvolveMono:
                        RuleArray = ConvMonoRule;
                        break;

                    case ConvolveSrcType.eConvolveStereo:
                        RuleArray = ConvStereoRule;
                        break;

                    case ConvolveSrcType.eConvolveBiStereo:
                        RuleArray = ConvBiStereoRule;
                        break;
                }

                int? Latency = null;
                if (ConvolverSpecGetLatencySpecified(Template))
                {
                    Latency = (int)(ConvolverSpecGetLatency(Template) * SynthParams.dSamplingRate);
                    if (Latency.Value < 0)
                    {
                        Latency = 0;
                    }
                }

                // validation pass
                int maximumImpulseResponseLength = 0;
                float[][] Data = new float[RuleArray.Length][];
                int[] Frames = new int[RuleArray.Length];
                for (int i = 0; i < RuleArray.Length; i += 1)
                {
                    NumChannelsType NumChannels;
                    int SamplingRate;
                    int unusedInt;
                    double unusedDouble;
                    bool unusedBool;

                    string ImpulseResponseName = RuleArray[i].ConvolverSpecGetImpulseResponse(Template);
                    if (!WaveSampDictGetSampleInfo(
                        SynthParams.Dictionary,
                        ImpulseResponseName,
                        out Data[i],
                        out Frames[i],
                        out NumChannels,
                        out unusedInt,
                        out unusedInt,
                        out unusedInt,
                        out unusedInt,
                        out unusedInt,
                        out unusedInt,
                        out unusedInt,
                        out unusedDouble,
                        out SamplingRate,
                        out unusedBool,
                        out unusedBool,
                        out unusedBool))
                    {
                        // should never be provided with name of non-existent sample data
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }

                    if (SamplingRate * SynthParams.iOversampling != SynthParams.iSamplingRate)
                    {
                        SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExConvolverBadSamplingRate;
                        SynthParams.ErrorInfo.SampleName = ImpulseResponseName;
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                    if (NumChannels != NumChannelsType.eSampleMono)
                    {
                        SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExConvolverBadNumChannels;
                        SynthParams.ErrorInfo.SampleName = ImpulseResponseName;
                        return SynthErrorCodes.eSynthErrorEx;
                    }

                    maximumImpulseResponseLength = Math.Max(maximumImpulseResponseLength, Frames[i]);
                }
                if (!Latency.HasValue)
                {
                    Latency = maximumImpulseResponseLength * SynthParams.iOversampling;
                }

                for (int i = 0; i < RuleArray.Length; i += 1)
                {
                    IConvolution ConvStream;
                    SynthErrorCodes result = ConvolveStreamFactory.NewConvStream(
                        Data[i],
                        Frames[i],
                        ConvolverSpecGetLatencySpecified(Template),
                        Latency.Value,
                        SynthParams.iOversampling,
                        SynthParams.ErrorInfo,
                        out ConvStream);
                    if (result != SynthErrorCodes.eSynthDone)
                    {
                        return result;
                    }
                    switch (i)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case 0:
                            Convolver.ConvStream0 = ConvStream;
                            break;
                        case 1:
                            Convolver.ConvStream1 = ConvStream;
                            break;
                        case 2:
                            Convolver.ConvStream2 = ConvStream;
                            break;
                        case 3:
                            Convolver.ConvStream3 = ConvStream;
                            break;
                    }
                }

                ConvolverSpecGetDirectGain(
                    Template,
                    out Convolver.DirectGain);
                ConvolverSpecGetProcessedGain(
                    Template,
                    out Convolver.ProcessedGain);

                /* done successfully */
                ConvolverOut = Convolver;
                return SynthErrorCodes.eSynthDone;
            }

            /* apply convolver to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                float DirectGain = (float)this.CurrentDirectGain;
                float ProcessedGain = (float)this.CurrentProcessedGain;

                switch (this.Type)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case ConvolveSrcType.eConvolveMono: /* bad perf: stores the same FFT twice (once for each channel) */
                    case ConvolveSrcType.eConvolveStereo:
                        this.ConvStream0.Apply(
                            workspace,
                            lOffset,
                            workspace,
                            lOffset,
                            nActualFrames,
                            DirectGain,
                            ProcessedGain);
                        this.ConvStream1.Apply(
                            workspace,
                            rOffset,
                            workspace,
                            rOffset,
                            nActualFrames,
                            DirectGain,
                            ProcessedGain);
                        break;

                    case ConvolveSrcType.eConvolveBiStereo:
#if DEBUG
                        Debug.Assert(!SynthParams.ScratchWorkspace1InUse);
                        SynthParams.ScratchWorkspace1InUse = true;
#endif
                        int LeftOffset = SynthParams.ScratchWorkspace1LOffset;
                        int RightOffset = SynthParams.ScratchWorkspace1ROffset;

                        FloatVectorCopy(
                            workspace,
                            lOffset,
                            workspace,
                            LeftOffset,
                            nActualFrames);
                        FloatVectorCopy(
                            workspace,
                            rOffset,
                            workspace,
                            RightOffset,
                            nActualFrames);

                        FloatVectorZero(
                            workspace,
                            lOffset,
                            nActualFrames);
                        FloatVectorZero(
                            workspace,
                            rOffset,
                            nActualFrames);

                        this.ConvStream0.Apply( /* left into left */
                            workspace,
                            LeftOffset,
                            workspace,
                            lOffset,
                            nActualFrames,
                            DirectGain,
                            ProcessedGain);
                        this.ConvStream1.Apply( /* right into left */
                            workspace,
                            RightOffset,
                            workspace,
                            lOffset,
                            nActualFrames,
                            0/*DirectGain*/,
                            ProcessedGain);
                        this.ConvStream2.Apply( /* left into right */
                            workspace,
                            LeftOffset,
                            workspace,
                            rOffset,
                            nActualFrames,
                            0/*DirectGain*/,
                            ProcessedGain);
                        this.ConvStream3.Apply( /* right into right */
                            workspace,
                            RightOffset,
                            workspace,
                            rOffset,
                            nActualFrames,
                            DirectGain,
                            ProcessedGain);

#if DEBUG
                        SynthParams.ScratchWorkspace1InUse = false;
#endif
                        break;
                }

                return SynthErrorCodes.eSynthDone;
            }

            /* update convolver state with accent information */
            public void TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams)
            {
                ScalarParamEval(
                    this.DirectGain,
                    ref Accents,
                    SynthParams,
                    out this.CurrentDirectGain);
                ScalarParamEval(
                    this.ProcessedGain,
                    ref Accents,
                    SynthParams,
                    out this.CurrentProcessedGain);
            }

            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                if (ConvStream0 != null)
                {
                    ConvStream0.Dispose();
                }
                if (ConvStream1 != null)
                {
                    ConvStream1.Dispose();
                }
                if (ConvStream2 != null)
                {
                    ConvStream2.Dispose();
                }
                if (ConvStream3 != null)
                {
                    ConvStream3.Dispose();
                }
            }
        }
    }
}
