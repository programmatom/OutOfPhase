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
        public class HistogramRec : ITrackEffect, IOscillatorEffect
        {
            public string HistogramString;

            public int NumBins;
            public int[] BinArray;
            public double Min;
            public double Max;
            public double LnMin;
            public double LnMax;
            public int Unders;
            public int Overs;

            public FirstOrderLowpassRec LeftLowpass;
            public FirstOrderLowpassRec RightLowpass;

            public bool IgnoreUnders;
            public bool Logarithmic;
            public HistogramChannelType ChannelSelect;
            public HistogramPowerEstType FilterMethod;
            public int ChartWidth;

#if DEBUG
            public bool Finalized_DBG;
#endif


            /* create a new histogram processor */
            public static HistogramRec NewHistogram(
                HistogramSpecRec Template,
                SynthParamRec SynthParams)
            {
                double Cutoff;

                HistogramRec Histogram = new HistogramRec();

                Histogram.HistogramString = GetHistogramSpecLabel(Template);

                Histogram.NumBins = GetHistogramSpecNumBins(Template);
                Histogram.BinArray = new int[Histogram.NumBins];

                Histogram.LeftLowpass = new FirstOrderLowpassRec();
                Histogram.RightLowpass = new FirstOrderLowpassRec();

                Cutoff = GetHistogramSpecPowerEstimatorCutoff(Template);
                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                    Histogram.LeftLowpass,
                    Cutoff,
                    SynthParams.dSamplingRate);
                FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                    Histogram.RightLowpass,
                    Cutoff,
                    SynthParams.dSamplingRate);

                Histogram.Min = GetHistogramSpecBottom(Template);
                Histogram.Max = GetHistogramSpecTop(Template);
                Histogram.Unders = 0;
                Histogram.Overs = 0;

                Histogram.IgnoreUnders = GetHistogramSpecDiscardUnders(Template);
                Histogram.Logarithmic = GetHistogramSpecBinDistribution(Template);
                Histogram.ChannelSelect = GetHistogramSpecChannelSelector(Template);
                Histogram.FilterMethod = GetHistogramSpecPowerEstimatorMethod(Template);
                Histogram.ChartWidth = GetHistogramSpecBarChartWidth(Template);

                Histogram.IgnoreUnders = Histogram.IgnoreUnders || (Histogram.Min == 0);
                if (Histogram.Logarithmic)
                {
                    Histogram.LnMin = Math.Log(Histogram.Min);
                    Histogram.LnMax = Math.Log(Histogram.Max);
                }

                return Histogram;
            }

            public void TrackUpdateState(ref AccentRec Accents, SynthParamRec SynthParams)
            {
            }

            public void OscFixEnvelopeOrigins(int ActualPreOriginTime)
            {
            }

            public void OscUpdateEnvelopes(double OscillatorFrequency, SynthParamRec SynthParams)
            {
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

            /* helper for padding */
            private static void Padding(
                StringBuilder StringThing,
                int DesiredColumn)
            {
                StringThing.Append(new String(' ', Math.Max(0, DesiredColumn - StringThing.Length)));
            }

            /* helper for writing out the chart */
            private static void WriteLine(
                double Start,
                double End,
                int Count,
                int CumulativeCount,
                int Frames,
                int MaxBin,
                int ChartWidth,
                TextWriter Output)
            {
                /* format: */
                /*   [llllllllll, hhhhhhhhhh) cccccccccc rrrrrrrrrr mmmmmmmmmm  | *****  */
                /* l = low, h = high, c = count, r = ratio, m = cumulative ratio */

                Output.Write(
                    "  [{0,10:0.0000}, {1,10:0.0000}) {2,10} {3,10:0.0000} {4,10:0.0000}",
                    Start,
                    End,
                    Count,
                    100 * (double)Count / (double)Frames,
                    100 * (double)CumulativeCount / (double)Frames);
                if (ChartWidth > 0)
                {
                    double d;
                    string s;

                    d = (ChartWidth + 1) * (double)Count / (double)(MaxBin + 1);
                    if ((d < 1) && (d > 0))
                    {
                        s = ".";
                    }
                    else
                    {
                        s = new String('*', Math.Max(0, (int)d));
                    }

                    Output.WriteLine(
                        " | {0}",
                        s);
                }
                else
                {
                    Output.WriteLine();
                }
            }

            /* helper function to generate filter */
            private static void HistogramFilter(
                HistogramRec Histogram,
                float[] Workspace,
                int WorkspaceOffset,
                int Length,
                FirstOrderLowpassRec Lowpass)
            {
                switch (Histogram.FilterMethod)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case HistogramPowerEstType.eHistogramAbsVal:
                    case HistogramPowerEstType.eHistogramSmoothedAbsVal:
                        FloatVectorAbsVal(
                            Workspace,
                            WorkspaceOffset,
                            Workspace,
                            WorkspaceOffset,
                            Length);
                        break;
                    case HistogramPowerEstType.eHistogramSmoothedRMS:
                        FloatVectorSquare(
                            Workspace,
                            WorkspaceOffset,
                            Workspace,
                            WorkspaceOffset,
                            Length);
                        break;
                }

                switch (Histogram.FilterMethod)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case HistogramPowerEstType.eHistogramAbsVal:
                        break;
                    case HistogramPowerEstType.eHistogramSmoothedAbsVal:
                    case HistogramPowerEstType.eHistogramSmoothedRMS:
                        FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                            Lowpass,
                            Workspace,
                            WorkspaceOffset,
                            Length);
                        break;
                }

                switch (Histogram.FilterMethod)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case HistogramPowerEstType.eHistogramAbsVal:
                    case HistogramPowerEstType.eHistogramSmoothedAbsVal:
                        break;
                    case HistogramPowerEstType.eHistogramSmoothedRMS:
                        FloatVectorSquareRoot(
                            Workspace,
                            WorkspaceOffset,
                            Length);
                        break;
                }
            }

            /* apply histogram to some stuff */
            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec synthParams)
            {
                int[] BinArray;
                bool Logarithmic;
                double NumBins;
                double OneOverlMaxMinuslMin;
                double lMin;
                double Min;
                double Max;
                int Unders;
                int Overs;
                bool IgnoreUnders;

#if DEBUG
                Debug.Assert(!synthParams.ScratchWorkspace1InUse);
                synthParams.ScratchWorkspace1InUse = true;
#endif
                int WorkspaceOffset = synthParams.ScratchWorkspace1LOffset;
                int Workspace2Offset = synthParams.ScratchWorkspace1ROffset;

                /* load local copies of params */
                BinArray = this.BinArray;
                Logarithmic = this.Logarithmic;
                NumBins = this.NumBins;
                Min = this.Min;
                Max = this.Max;
                IgnoreUnders = this.IgnoreUnders;
                if (Logarithmic)
                {
                    lMin = this.LnMin;
                    OneOverlMaxMinuslMin = 1d / (this.LnMax - this.LnMin);
                }
                else
                {
                    lMin = this.Min;
                    OneOverlMaxMinuslMin = 1d / (this.Max - this.Min);
                }

                /* load local state */
                Unders = this.Unders;
                Overs = this.Overs;

                /* do power analysis */
                switch (this.ChannelSelect)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case HistogramChannelType.eHistogramLeft:
                        FloatVectorCopy(
                            workspace,
                            lOffset,
                            workspace,
                            WorkspaceOffset,
                            nActualFrames);
                        HistogramFilter(
                            this,
                            workspace,
                            WorkspaceOffset,
                            nActualFrames,
                            this.LeftLowpass);
                        break;
                    case HistogramChannelType.eHistogramRight:
                        FloatVectorCopy(
                            workspace,
                            rOffset,
                            workspace,
                            WorkspaceOffset,
                            nActualFrames);
                        HistogramFilter(
                            this,
                            workspace,
                            WorkspaceOffset,
                            nActualFrames,
                            this.LeftLowpass);
                        break;
                    case HistogramChannelType.eHistogramAverageBeforeFilter:
                        FloatVectorAverage(
                            workspace/* source a */,
                            lOffset,
                            workspace/* source b */,
                            rOffset,
                            workspace/* target */,
                            WorkspaceOffset,
                            nActualFrames);
                        HistogramFilter(
                            this,
                            workspace,
                            WorkspaceOffset,
                            nActualFrames,
                            this.LeftLowpass);
                        break;
                    case HistogramChannelType.eHistogramAverageAfterFilter:
                        FloatVectorCopy(
                            workspace,
                            lOffset,
                            workspace,
                            WorkspaceOffset,
                            nActualFrames);
                        FloatVectorCopy(
                            workspace,
                            rOffset,
                            workspace,
                            Workspace2Offset,
                            nActualFrames);
                        /* filter each channel separately */
                        HistogramFilter(
                            this,
                            workspace,
                            WorkspaceOffset,
                            nActualFrames,
                            this.LeftLowpass);
                        HistogramFilter(
                            this,
                            workspace,
                            Workspace2Offset,
                            nActualFrames,
                            this.RightLowpass);
                        /* compute average */
                        FloatVectorAverage(
                            workspace/* source a */,
                            WorkspaceOffset,
                            workspace/* source b */,
                            Workspace2Offset,
                            workspace/* target */,
                            WorkspaceOffset,
                            nActualFrames);
                        break;
                    case HistogramChannelType.eHistogramMaxAfterFilter:
                        FloatVectorCopy(
                            workspace,
                            lOffset,
                            workspace,
                            WorkspaceOffset,
                            nActualFrames);
                        FloatVectorCopy(
                            workspace,
                            rOffset,
                            workspace,
                            Workspace2Offset,
                            nActualFrames);
                        /* filter each channel separately */
                        HistogramFilter(
                            this,
                            workspace,
                            WorkspaceOffset,
                            nActualFrames,
                            this.LeftLowpass);
                        HistogramFilter(
                            this,
                            workspace,
                            Workspace2Offset,
                            nActualFrames,
                            this.RightLowpass);
                        /* compute average */
                        FloatVectorMax(
                            workspace/* source a */,
                            WorkspaceOffset,
                            workspace/* source b */,
                            Workspace2Offset,
                            workspace/* target */,
                            WorkspaceOffset,
                            nActualFrames);
                        break;
                }

                /* update bins */
                for (int i = 0; i < nActualFrames; i += 1)
                {
                    double Temp;

                    Temp = workspace[i + WorkspaceOffset];
                    if (Temp < Min)
                    {
                        if (!IgnoreUnders)
                        {
                            Unders += 1;
                        }
                    }
                    else if (Temp >= Max)
                    {
                        Overs += 1;
                    }
                    else
                    {
                        int Index;

                        if (Logarithmic)
                        {
                            Temp = Math.Log(Temp);
                        }
                        Index = (int)(NumBins * (Temp - lMin) * OneOverlMaxMinuslMin);
                        BinArray[Index] += 1;
                    }
                }

                /* save local state */
                this.Unders = Unders;
                this.Overs = Overs;

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
                int NumFrames;
                int CumulativeFrames;
                int MaxBinCount;
                float LastEnd;

#if DEBUG
                if (this.Finalized_DBG)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                this.Finalized_DBG = true;
#endif

                NumFrames = this.Unders + this.Overs;
                MaxBinCount = this.Unders;
                if (MaxBinCount < this.Overs)
                {
                    MaxBinCount = this.Overs;
                }
                for (int i = 0; i < this.NumBins; i += 1)
                {
                    NumFrames += this.BinArray[i];
                    if (MaxBinCount < this.BinArray[i])
                    {
                        MaxBinCount = this.BinArray[i];
                    }
                }
                CumulativeFrames = 0;


                // piece together locally and then write all at once to SynthParams.InteractionLog to
                // provide coherent output in a multithreaded scenario
                StringBuilder sb = new StringBuilder();
                using (TextWriter writer = new StringWriter(sb))
                {
                    /* header */
                    writer.WriteLine();
                    writer.WriteLine("Results for Histogram \"{0}\":", this.HistogramString);
                    writer.WriteLine("  Frames Processed: {0}", NumFrames);

                    CumulativeFrames += this.Unders;
                    if (!this.IgnoreUnders)
                    {
                        WriteLine(
                            0,
                            this.Min,
                            this.Unders,
                            CumulativeFrames,
                            NumFrames,
                            MaxBinCount,
                            this.ChartWidth,
                            writer);
                    }
                    LastEnd = (float)this.Min;
                    for (int i = 0; i < this.NumBins; i += 1)
                    {
                        double Start;
                        double End;

                        Start = LastEnd;
                        if (this.Logarithmic)
                        {
                            End = Math.Exp(this.LnMin + (((double)(i + 1) / this.NumBins)
                                * (this.LnMax - this.LnMin)));
                        }
                        else
                        {
                            End = this.Min + (((double)(i + 1) / this.NumBins)
                                * (this.Max - this.Min));
                        }

                        CumulativeFrames += this.BinArray[i];
                        WriteLine(
                            Start,
                            End,
                            this.BinArray[i],
                            CumulativeFrames,
                            NumFrames,
                            MaxBinCount,
                            this.ChartWidth,
                            writer);

                        LastEnd = (float)End;
                    }

                    CumulativeFrames += this.Overs;
                    WriteLine(
                        LastEnd,
                        Double.PositiveInfinity,
                        this.Overs,
                        CumulativeFrames,
                        NumFrames,
                        MaxBinCount,
                        this.ChartWidth,
                        writer);

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
