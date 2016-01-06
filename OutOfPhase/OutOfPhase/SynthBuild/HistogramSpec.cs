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
        /* types of filters */
        public enum HistogramPowerEstType
        {
            eHistogramAbsVal,
            eHistogramSmoothedAbsVal,
            eHistogramSmoothedRMS,
        }

        /* types of channel combination */
        public enum HistogramChannelType
        {
            eHistogramLeft,
            eHistogramRight,
            eHistogramAverageBeforeFilter,
            eHistogramAverageAfterFilter,
            eHistogramMaxAfterFilter,
        }

        public class HistogramSpecRec
        {
            public double Bottom;
            public double Top;
            public double PowerEstimatorCutoff;
            public string Label;
            public int NumPointsInChart;
            public int NumBins;
            public bool DiscardUnders;
            public bool LogBinDistribution;
            public HistogramPowerEstType PowerEstimatorMethod;
            public HistogramChannelType ChannelSelector;
        }

        /* create a new histogram spec */
        public static HistogramSpecRec NewHistogramSpec(string Identifier)
        {
            HistogramSpecRec Histogram = new HistogramSpecRec();

            Histogram.Label = Identifier;
            Histogram.PowerEstimatorCutoff = 10;
            Histogram.PowerEstimatorMethod = HistogramPowerEstType.eHistogramAbsVal;
            //Histogram.DiscardUnders = false;
            //Histogram.NumPointsInChart = 0;
            //Histogram.Bottom = 0;
            Histogram.Top = 1;
            Histogram.NumBins = 10;
            //Histogram.LogBinDistribution = false;
            Histogram.ChannelSelector = HistogramChannelType.eHistogramAverageBeforeFilter;

            return Histogram;
        }

        /* get actual heap block histogram identifier Label */
        public static string GetHistogramSpecLabel(HistogramSpecRec Histogram)
        {
            return Histogram.Label;
        }

        /* set power estimation mode */
        public static void SetHistogramSpecPowerEstimatorMode(
            HistogramSpecRec Histogram,
            HistogramPowerEstType Method)
        {
            Histogram.PowerEstimatorMethod = Method;
        }

        /* set power estimator filter cutoff frequency */
        public static void SetHistogramSpecPowerEstimatorFilter(
            HistogramSpecRec Histogram,
            double FilterCutoff)
        {
            Histogram.PowerEstimatorCutoff = FilterCutoff;
        }

        /* get cutoff frequency for power estimator */
        public static double GetHistogramSpecPowerEstimatorCutoff(HistogramSpecRec Histogram)
        {
            return Histogram.PowerEstimatorCutoff;
        }

        /* get power estimator method */
        public static HistogramPowerEstType GetHistogramSpecPowerEstimatorMethod(HistogramSpecRec Histogram)
        {
            return Histogram.PowerEstimatorMethod;
        }

        /* set discard-unders flag */
        public static void SetHistogramSpecDiscardUnders(
            HistogramSpecRec Histogram,
            bool Discard)
        {
            Histogram.DiscardUnders = Discard;
        }

        /* get discard-unders flag */
        public static bool GetHistogramSpecDiscardUnders(HistogramSpecRec Histogram)
        {
            return Histogram.DiscardUnders;
        }

        /* set number of points in bar chart (0 = don't print chart) */
        public static void SetHistogramSpecBarChartWidth(
            HistogramSpecRec Histogram,
            int NumPoints)
        {
            Histogram.NumPointsInChart = NumPoints;
        }

        /* get number of points in bar chart (0 = don't print chart) */
        public static int GetHistogramSpecBarChartWidth(HistogramSpecRec Histogram)
        {
            return Histogram.NumPointsInChart;
        }

        /* set bottom level of range */
        public static void SetHistogramSpecBottom(
            HistogramSpecRec Histogram,
            double Bottom)
        {
            Histogram.Bottom = Bottom;
        }

        /* get bottom level of range */
        public static double GetHistogramSpecBottom(HistogramSpecRec Histogram)
        {
            return Histogram.Bottom;
        }

        /* set top level of range */
        public static void SetHistogramSpecTop(
            HistogramSpecRec Histogram,
            double Top)
        {
            Histogram.Top = Top;
        }

        /* get top level of range */
        public static double GetHistogramSpecTop(HistogramSpecRec Histogram)
        {
            return Histogram.Top;
        }

        /* set number of bins */
        public static void SetHistogramSpecNumBins(
            HistogramSpecRec Histogram,
            int NumBins)
        {
            Histogram.NumBins = NumBins;
        }

        /* get number of bins */
        public static int GetHistogramSpecNumBins(HistogramSpecRec Histogram)
        {
            return Histogram.NumBins;
        }

        /* set logarithmic/linear binning (false = linear, true = log) */
        public static void SetHistogramSpecBinDistribution(
            HistogramSpecRec Histogram,
            bool Logarithmic)
        {
            Histogram.LogBinDistribution = Logarithmic;
        }

        /* get logarithmic/linear binning (false = linear, true = log) */
        public static bool GetHistogramSpecBinDistribution(HistogramSpecRec Histogram)
        {
            return Histogram.LogBinDistribution;
        }

        /* set histogram channel selector */
        public static void SetHistogramSpecChannelSelector(
            HistogramSpecRec Histogram,
            HistogramChannelType Selector)
        {
            Histogram.ChannelSelector = Selector;
        }

        /* get histogram channel selector */
        public static HistogramChannelType GetHistogramSpecChannelSelector(HistogramSpecRec Histogram)
        {
            return Histogram.ChannelSelector;
        }
    }
}
