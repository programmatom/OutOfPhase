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
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        /* record for one sample object */
        public class OneSampRec
        {
            /* lower bound frequency (freq >= lower) */
            public double LowerBound;

            /* upper bound frequency (freq < upper) */
            public double UpperBound;

            /* name of sample or wave table being referenced */
            public string Name;
        }

        /* list of specifications */
        public class SampleSelectorRec
        {
            /* list of OneSampRec objects */
            public OneSampRec[] List;

            /* lower bound to use when adding a new entry */
            public double CurrentLowerBound;
        }

        /* create a new sample selector record */
        public static SampleSelectorRec NewSampleSelectorList(double LowestBound)
        {
            SampleSelectorRec SampList = new SampleSelectorRec();

            SampList.List = new OneSampRec[0];

            /* set the starting threshhold */
            SampList.CurrentLowerBound = LowestBound;

            return SampList;
        }

        /* add a new sample thing, with it's upper bound.  the Name heap block is deleted */
        public static void AppendSampleSelector(
            SampleSelectorRec SampList,
            double UpperBound,
            string Name)
        {
            /* constrain interval */
            if (UpperBound < SampList.CurrentLowerBound)
            {
                UpperBound = SampList.CurrentLowerBound;
            }

            OneSampRec NewRec = new OneSampRec();
            NewRec.LowerBound = SampList.CurrentLowerBound;
            NewRec.UpperBound = UpperBound;
            NewRec.Name = Name;

            /* update our idea of lower bound */
            SampList.CurrentLowerBound = UpperBound;

            Array.Resize(ref SampList.List, SampList.List.Length + 1);
            SampList.List[SampList.List.Length - 1] = NewRec;
        }

        /* get the number of sample ranges specified in the list */
        public static int GetSampleSelectorListLength(SampleSelectorRec SampList)
        {
            return SampList.List.Length;
        }

        /* return the low bound frequency of a particular list entry */
        public static double GetSampleListEntryLowFreqBound(
            SampleSelectorRec SampList,
            int Index)
        {
            return SampList.List[Index].LowerBound;
        }

        /* return the high bound frequency of a particular list entry */
        public static double GetSampleListEntryHighFreqBound(
            SampleSelectorRec SampList,
            int Index)
        {
            return SampList.List[Index].UpperBound;
        }

        /* get the actual ptr to the heap block containing non-null-terminated sample name */
        public static string GetSampleListEntryName(
            SampleSelectorRec SampList,
            int Index)
        {
            return SampList.List[Index].Name;
        }
    }
}
