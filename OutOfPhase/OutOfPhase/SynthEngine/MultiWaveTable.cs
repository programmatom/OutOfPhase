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
        /* structure containing all the information */
        public class OneWaveTableRec
        {
            /* this is the lowest frequency that the wave table should be played at */
            public double MinFrequency;
            /* this is the frequency just above the highest frequency that this */
            /* wave table should be played at. */
            public double MaxFrequency;

            /* raw wave table data array */
            public float[][] WaveTableMatrix; /* array of wave points */
            /* number of wavetables in the array */
            public int NumberOfTables;
            /* number of frames in each table */
            public int FramesPerTable;
        }

        public class MultiWaveTableRec
        {
            public OneWaveTableRec[] WaveTableInfoArray;
        }

        /* create a new multisampling object based on a list of wave tables */
        public static MultiWaveTableRec NewMultiWaveTable(
            SampleSelectorRec Selector,
            WaveSampDictRec Dictionary)
        {
            MultiWaveTableRec MultiWaveTable = new MultiWaveTableRec();

            int Count = GetSampleSelectorListLength(Selector);
            MultiWaveTable.WaveTableInfoArray = new OneWaveTableRec[Count];

            int Index = 0;
            for (int i = 0; i < Count; i += 1)
            {
                OneWaveTableRec Entry = MultiWaveTable.WaveTableInfoArray[Index] = new OneWaveTableRec();

                /* get interval for which this one is valid */
                Entry.MinFrequency = GetSampleListEntryLowFreqBound(Selector, i);
                Entry.MaxFrequency = GetSampleListEntryHighFreqBound(Selector, i);

                /* get name of wave table for this interval */
                string WaveTableName = GetSampleListEntryName(Selector, i);
                /* see if we can get info about this one */
                if (!WaveSampDictGetWaveTableInfo(
                    Dictionary,
                    WaveTableName,
                    out Entry.WaveTableMatrix,
                    out Entry.FramesPerTable,
                    out Entry.NumberOfTables))
                {
                    /* couldn't find that wave table, so just skip it */
                    continue;
                }

                /* this entry is valid */
                Index += 1;
            }
            if (Index < Count)
            {
                Array.Resize(ref MultiWaveTable.WaveTableInfoArray, Index);
            }

            return MultiWaveTable;
        }

        /* obtain a data reference & info for a sample.  returns False if there is no */
        /* sample corresponding to the supplied frequency. */
        public static bool GetMultiWaveTableReference(
            MultiWaveTableRec MultiWaveTable,
            double FrequencyHertz,
            out float[][] TwoDimensionalVecOut,
            out int NumFramesOut,
            out int NumTablesOut)
        {
            TwoDimensionalVecOut = null;
            NumFramesOut = 0;
            NumTablesOut = 0;

            for (int i = 0; i < MultiWaveTable.WaveTableInfoArray.Length; i += 1)
            {
                OneWaveTableRec Entry = MultiWaveTable.WaveTableInfoArray[i];

                if ((FrequencyHertz >= Entry.MinFrequency) && (FrequencyHertz < Entry.MaxFrequency))
                {
                    TwoDimensionalVecOut = Entry.WaveTableMatrix;
                    NumFramesOut = Entry.FramesPerTable;
                    NumTablesOut = Entry.NumberOfTables;
                    return (Entry.NumberOfTables > 0);
                }
            }
            return false;
        }
    }
}
