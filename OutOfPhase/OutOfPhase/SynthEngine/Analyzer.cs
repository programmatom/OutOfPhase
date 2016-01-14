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
using System.IO;
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        public class AnalyzerRec : ITrackEffect, IOscillatorEffect
        {
            public float LeftMinimum;
            public float LeftMaximum;
            public float RightMinimum;
            public float RightMaximum;
            public int FrameCount;

            public FirstOrderLowpassRec LeftLowpass;
            public FirstOrderLowpassRec RightLowpass;
            public float LeftPowerMaximum;
            public float RightPowerMaximum;
            public AnalyzerPowerEstType PowerMethod;
            public bool PowerEnabled;

            public string AnalyzerString;

#if DEBUG
            public bool Finalized_DBG;
#endif


            /* create a new analyzer processor */
            public static AnalyzerRec NewAnalyzer(
                AnalyzerSpecRec Template,
                SynthParamRec SynthParams)
            {
                AnalyzerRec Analyzer = new AnalyzerRec();

                Analyzer.AnalyzerString = GetAnalyzerSpecString(Template);

                Analyzer.LeftMinimum = Int32.MaxValue;
                Analyzer.LeftMaximum = Int32.MinValue;
                Analyzer.RightMinimum = Int32.MaxValue;
                Analyzer.RightMaximum = Int32.MinValue;
                Analyzer.FrameCount = 0;
                Analyzer.LeftPowerMaximum = Int32.MinValue;
                Analyzer.RightPowerMaximum = Int32.MinValue;

                Analyzer.PowerEnabled = IsAnalyzerSpecPowerEstimatorEnabled(Template);
                if (Analyzer.PowerEnabled)
                {
                    Analyzer.LeftLowpass = new FirstOrderLowpassRec();
                    FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                        Analyzer.LeftLowpass,
                        GetAnalyzerSpecPowerEstimatorCutoff(Template),
                        SynthParams.dSamplingRate);

                    Analyzer.RightLowpass = new FirstOrderLowpassRec();
                    FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                        Analyzer.RightLowpass,
                        GetAnalyzerSpecPowerEstimatorCutoff(Template),
                        SynthParams.dSamplingRate);

                    Analyzer.PowerMethod = GetAnalyzerSpecPowerEstimatorMethod(Template);
#if DEBUG
                    if ((Analyzer.PowerMethod != AnalyzerPowerEstType.eAnalyzerPowerAbsVal)
                        && (Analyzer.PowerMethod != AnalyzerPowerEstType.eAnalyzerPowerRMS))
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
#endif
                }

                return Analyzer;
            }

            public SynthErrorCodes TrackUpdateState(ref AccentRec Accents, SynthParamRec SynthParams)
            {
                return SynthErrorCodes.eSynthDone;
            }

            public void OscFixEnvelopeOrigins(int ActualPreOriginTime)
            {
            }

            public SynthErrorCodes OscUpdateEnvelopes(double OscillatorFrequency, SynthParamRec SynthParams)
            {
                return SynthErrorCodes.eSynthDone;
            }

            public void OscKeyUpSustain1()
            {
            }

            public void OscKeyUpSustain2()
            {
            }

            public void OscKeyUpSustain3()
            {
            }

            public void OscRetriggerEnvelopes(ref AccentRec NewAccents, double NewHurryUp, double NewInitialFrequency, bool ActuallyRetrigger, SynthParamRec SynthParams)
            {
            }

            /* apply analyzer to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec synthParams)
            {
                this.FrameCount += nActualFrames;

#if DEBUG
                Debug.Assert(!synthParams.ScratchWorkspace1InUse);
                synthParams.ScratchWorkspace1InUse = true;
#endif
                int LeftBufferOffset = synthParams.ScratchWorkspace1LOffset;
                int RightBufferOffset = synthParams.ScratchWorkspace1ROffset;

                /* build buffers */
                FloatVectorCopy(
                    workspace,
                    lOffset,
                    workspace,
                    LeftBufferOffset,
                    nActualFrames);
                FloatVectorCopy(
                    workspace,
                    rOffset,
                    workspace,
                    RightBufferOffset,
                    nActualFrames);

                FloatVectorReductionMinMax(
                    ref this.LeftMinimum,
                    ref this.LeftMaximum,
                    workspace,
                    LeftBufferOffset,
                    nActualFrames);
                FloatVectorReductionMinMax(
                    ref this.RightMinimum,
                    ref this.RightMaximum,
                    workspace,
                    RightBufferOffset,
                    nActualFrames);

                /* compute power analysis */
                if (this.PowerEnabled)
                {
                    switch (this.PowerMethod)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();

                        case AnalyzerPowerEstType.eAnalyzerPowerAbsVal:
                            {
                                float Min = 0;

                                /* absval the vector */
                                FloatVectorAbsVal(
                                    workspace,
                                    LeftBufferOffset,
                                    workspace,
                                    LeftBufferOffset,
                                    nActualFrames);
                                FloatVectorAbsVal(
                                    workspace,
                                    RightBufferOffset,
                                    workspace,
                                    RightBufferOffset,
                                    nActualFrames);

                                /* apply filter */
                                FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                                    this.LeftLowpass,
                                    workspace,
                                    LeftBufferOffset,
                                    nActualFrames);
                                FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                                    this.RightLowpass,
                                    workspace,
                                    RightBufferOffset,
                                    nActualFrames);

                                /* compute maximum */
                                FloatVectorReductionMinMax(
                                    ref Min,
                                    ref this.LeftPowerMaximum,
                                    workspace,
                                    LeftBufferOffset,
                                    nActualFrames);
                                FloatVectorReductionMinMax(
                                    ref Min,
                                    ref this.RightPowerMaximum,
                                    workspace,
                                    RightBufferOffset,
                                    nActualFrames);
                            }
                            break;

                        case AnalyzerPowerEstType.eAnalyzerPowerRMS:
                            {
                                float Min = 0;

                                /* square the vector */
                                FloatVectorSquare(
                                    workspace,
                                    LeftBufferOffset,
                                    workspace,
                                    LeftBufferOffset,
                                    nActualFrames);
                                FloatVectorSquare(
                                    workspace,
                                    RightBufferOffset,
                                    workspace,
                                    RightBufferOffset,
                                    nActualFrames);

                                /* apply filter */
                                FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                                    this.LeftLowpass,
                                    workspace,
                                    LeftBufferOffset,
                                    nActualFrames);
                                FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                                    this.RightLowpass,
                                    workspace,
                                    RightBufferOffset,
                                    nActualFrames);

                                /* compute square root */
                                FloatVectorSquareRoot(
                                    workspace,
                                    LeftBufferOffset,
                                    nActualFrames);
                                FloatVectorSquareRoot(
                                    workspace,
                                    RightBufferOffset,
                                    nActualFrames);

                                /* compute maximum */
                                FloatVectorReductionMinMax(
                                    ref Min,
                                    ref this.LeftPowerMaximum,
                                    workspace,
                                    LeftBufferOffset,
                                    nActualFrames);
                                FloatVectorReductionMinMax(
                                    ref Min,
                                    ref this.RightPowerMaximum,
                                    workspace,
                                    RightBufferOffset,
                                    nActualFrames);
                            }
                            break;
                    }
                }

#if DEBUG
                synthParams.ScratchWorkspace1InUse = false;
#endif

                return SynthErrorCodes.eSynthDone;
            }

            /* finalize before termination */
            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
#if DEBUG
                if (this.Finalized_DBG)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                this.Finalized_DBG = true;
#endif

                // piece together locally and then write all at once to SynthParams.InteractionLog to
                // provide coherent output in a multithreaded scenario
                StringBuilder sb = new StringBuilder();
                using (TextWriter writer = new StringWriter(sb))
                {
                    /* header */
                    writer.WriteLine();
                    writer.WriteLine("Results for analyzer \"{0}\":", this.AnalyzerString);
                    writer.WriteLine("  Frames Processed:         {0}", this.FrameCount);

                    if (this.FrameCount != 0)
                    {
                        /* amplitude */
                        writer.WriteLine("  Left Channel Minimum:     {0,7:0.0000}", this.LeftMinimum);
                        writer.WriteLine("  Left Channel Maximum:     {0,7:0.0000}", this.LeftMaximum);
                        writer.WriteLine("  Right Channel Minimum:    {0,7:0.0000}", this.RightMinimum);
                        writer.WriteLine("  Right Channel Maximum:    {0,7:0.0000}", this.RightMaximum);

                        /* power */
                        if (this.PowerEnabled)
                        {
                            writer.WriteLine("  Left Channel Max Power:   {0,7:0.0000}", this.LeftPowerMaximum);
                            writer.WriteLine("  Right Channel Max Power:  {0,7:0.0000}", this.RightPowerMaximum);
                        }
                    }

                    writer.WriteLine();
                    writer.WriteLine();
                }

                if (writeOutputLogs)
                {
                    SynthParams.InteractionLog.Write(sb.ToString());
                }
            }
        }
    }
}
