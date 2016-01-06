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
        /* types of power estimation methods */
        public enum AnalyzerPowerEstType
        {
            eAnalyzerPowerAbsVal,
            eAnalyzerPowerRMS,
        }

        public class AnalyzerSpecRec
        {
            /* string to print out when we're done */
            public string String;

            /* cutoff for power estimator filter */
            public double PowerEstimatorCutoff;

            /* flag indicating power estimator should be used */
            public bool PowerEstimatorEnable;

            /* method of estimating power */
            public AnalyzerPowerEstType PowerEstimatorMethod;
        }

        /* create a new analyzer spec */
        public static AnalyzerSpecRec NewAnalyzerSpec(string Identifier)
        {
            AnalyzerSpecRec Analyzer = new AnalyzerSpecRec();

            Analyzer.String = Identifier;
            Analyzer.PowerEstimatorCutoff = 10;
            //Analyzer.PowerEstimatorEnable = false;

            return Analyzer;
        }

        /* get actual heap block analyzer identifier string */
        public static string GetAnalyzerSpecString(AnalyzerSpecRec Analyzer)
        {
            return Analyzer.String;
        }

        /* enable power estimation and set frequency */
        public static void AnalyzerSpecEnablePowerEstimator(
            AnalyzerSpecRec Analyzer,
            double FilterCutoff,
            AnalyzerPowerEstType Method)
        {
#if DEBUG
            if ((Method != AnalyzerPowerEstType.eAnalyzerPowerAbsVal) && (Method != AnalyzerPowerEstType.eAnalyzerPowerRMS))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Analyzer.PowerEstimatorEnable = true;
            Analyzer.PowerEstimatorCutoff = FilterCutoff;
            Analyzer.PowerEstimatorMethod = Method;
        }

        /* find out if power estimator is enabled */
        public static bool IsAnalyzerSpecPowerEstimatorEnabled(AnalyzerSpecRec Analyzer)
        {
            return Analyzer.PowerEstimatorEnable;
        }

        /* get cutoff frequency for power estimator */
        public static double GetAnalyzerSpecPowerEstimatorCutoff(AnalyzerSpecRec Analyzer)
        {
#if DEBUG
            if (!Analyzer.PowerEstimatorEnable)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Analyzer.PowerEstimatorCutoff;
        }

        /* get power estimator method */
        public static AnalyzerPowerEstType GetAnalyzerSpecPowerEstimatorMethod(AnalyzerSpecRec Analyzer)
        {
#if DEBUG
            if (!Analyzer.PowerEstimatorEnable)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return Analyzer.PowerEstimatorMethod;
        }
    }
}
