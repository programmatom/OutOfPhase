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
        /* this checks all of the objects to make sure that names are unique */
        /* *Result is True if they are, or false and presents a warning dialog */
        /* if they aren't unique.  it returns True if test succeeded or false if */
        /* it ran out of memory. */
        public static SynthErrorCodes CheckNameUniqueness(
            Document Document,
            SynthErrorInfoRec ErrorInfo)
        {
            bool NotUnique;

            SampleCheck(
                Document.SampleList,
                Document.AlgoSampList,
                out NotUnique);
            if (NotUnique)
            {
                ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExSomeSamplesHaveSameName;
                return SynthErrorCodes.eSynthErrorEx;
            }

            WaveTableCheck(
                Document.WaveTableList,
                Document.AlgoWaveTableList,
                out NotUnique);
            if (NotUnique)
            {
                ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExSomeWaveTablesHaveSameName;
                return SynthErrorCodes.eSynthErrorEx;
            }

            InstrCheck(
                Document.InstrumentList,
                out NotUnique);
            if (NotUnique)
            {
                ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExSomeInstrumentsHaveSameName;
                return SynthErrorCodes.eSynthErrorEx;
            }

            return SynthErrorCodes.eSynthDone;
        }

        /* test samples */
        private static void SampleCheck(
            IEnumerable<SampleObjectRec> SampleList,
            IEnumerable<AlgoSampObjectRec> AlgoSampList,
            out bool AnyDuplicates)
        {
            AnyDuplicates = false;

            Dictionary<string, bool> dict = new Dictionary<string, bool>();

            foreach (SampleObjectRec obj in SampleList)
            {
                if (dict.ContainsKey(obj.Name))
                {
                    AnyDuplicates = true;
                    return;
                }
                dict.Add(obj.Name, false);
            }

            foreach (AlgoSampObjectRec obj in AlgoSampList)
            {
                if (dict.ContainsKey(obj.Name))
                {
                    AnyDuplicates = true;
                    return;
                }
                dict.Add(obj.Name, false);
            }
        }

        /* test wave tables */
        private static void WaveTableCheck(
            IEnumerable<WaveTableObjectRec> WaveTableList,
            IEnumerable<AlgoWaveTableObjectRec> AlgoWaveTableList,
            out bool AnyDuplicates)
        {
            AnyDuplicates = false;

            Dictionary<string, bool> dict = new Dictionary<string, bool>();

            foreach (WaveTableObjectRec obj in WaveTableList)
            {
                if (dict.ContainsKey(obj.Name))
                {
                    AnyDuplicates = true;
                    return;
                }
                dict.Add(obj.Name, false);
            }

            foreach (AlgoWaveTableObjectRec obj in AlgoWaveTableList)
            {
                if (dict.ContainsKey(obj.Name))
                {
                    AnyDuplicates = true;
                    return;
                }
                dict.Add(obj.Name, false);
            }
        }

        /* test wave tables */
        private static void InstrCheck(
            IEnumerable<InstrObjectRec> InstrList,
            out bool AnyDuplicates)
        {
            AnyDuplicates = false;

            Dictionary<string, bool> dict = new Dictionary<string, bool>();

            foreach (InstrObjectRec obj in InstrList)
            {
                if (dict.ContainsKey(obj.Name))
                {
                    AnyDuplicates = true;
                    return;
                }
                dict.Add(obj.Name, false);
            }
        }
    }
}
