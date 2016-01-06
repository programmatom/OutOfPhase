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
        /* all the info is in here */
        public class OneSampleRec
        {
            /* this is the lowest frequency that the sample should be played at */
            public double MinFrequency;
            /* this is the frequency just above the highest frequency that this */
            /* sample should be played at. */
            public double MaxFrequency;

            /* information about the sample */
            public double NaturalFreq;
            public float[] SampleDataReference;
            public int SamplingRate;
            public int NumFrames;
            public int Origin;
            public int Loop1Start;
            public int Loop1End;
            public int Loop2Start;
            public int Loop2End;
            public int Loop3Start;
            public int Loop3End;
            public bool Loop1Bidirectional;
            public bool Loop2Bidirectional;
            public bool Loop3Bidirectional;
            public NumChannelsType NumChannels;
        }

        public class MultiSampleRec
        {
            public OneSampleRec[] SampleInfoArray;
        }

        /* create a new multisampling object based on a list of samples */
        public static MultiSampleRec NewMultiSample(
            SampleSelectorRec Selector,
            WaveSampDictRec Dictionary)
        {
            MultiSampleRec MultiSample = new MultiSampleRec();

            int Count = GetSampleSelectorListLength(Selector);
            MultiSample.SampleInfoArray = new OneSampleRec[Count];

            /* fill in the entries */
            int Index = 0;
            for (int i = 0; i < Count; i += 1)
            {
                OneSampleRec Entry = MultiSample.SampleInfoArray[Index] = new OneSampleRec();

                /* fill in interval for which this sample is valid */
                Entry.MinFrequency = GetSampleListEntryLowFreqBound(Selector, i);
                Entry.MaxFrequency = GetSampleListEntryHighFreqBound(Selector, i);

                /* get name of sample for this interval */
                string SampleName = GetSampleListEntryName(Selector, i);
                /* see if we can get info about this one */
                if (!WaveSampDictGetSampleInfo(
                    Dictionary,
                    SampleName,
                    out Entry.SampleDataReference,
                    out Entry.NumFrames,
                    out Entry.NumChannels,
                    out Entry.Loop1Start,
                    out Entry.Loop1End,
                    out Entry.Loop2Start,
                    out Entry.Loop2End,
                    out Entry.Loop3Start,
                    out Entry.Loop3End,
                    out Entry.Origin,
                    out Entry.NaturalFreq,
                    out Entry.SamplingRate,
                    out Entry.Loop1Bidirectional,
                    out Entry.Loop2Bidirectional,
                    out Entry.Loop3Bidirectional))
                {
                    /* couldn't find that wave table, so just skip it */
                    continue;
                }

                /* this entry is valid */
                Index += 1;
            }

            if (Index < Count)
            {
                Array.Resize(ref MultiSample.SampleInfoArray, Index);
            }

            return MultiSample;
        }

        /* obtain a data reference & info for a sample.  returns False if there is no */
        /* sample corresponding to the supplied frequency. */
        public static bool GetMultiSampleReference(
            MultiSampleRec MultiSample,
            double FrequencyHertz,
            out float[] DataOut,
            out int NumFramesOut,
            out NumChannelsType NumChannelsOut,
            out int Loop1StartOut,
            out int Loop1EndOut,
            out int Loop2StartOut,
            out int Loop2EndOut,
            out int Loop3StartOut,
            out int Loop3EndOut,
            out int OriginOut,
            out double NaturalFreqOut,
            out int SamplingRateOut,
            out bool Loop1Bidirectional,
            out bool Loop2Bidirectional,
            out bool Loop3Bidirectional)
        {
            for (int i = 0; i < MultiSample.SampleInfoArray.Length; i += 1)
            {
                OneSampleRec Entry = MultiSample.SampleInfoArray[i];

                if ((FrequencyHertz >= Entry.MinFrequency) && (FrequencyHertz < Entry.MaxFrequency))
                {
                    DataOut = Entry.SampleDataReference;
                    NumFramesOut = Entry.NumFrames;
                    NumChannelsOut = Entry.NumChannels;
                    Loop1StartOut = Entry.Loop1Start;
                    Loop1EndOut = Entry.Loop1End;
                    Loop2StartOut = Entry.Loop2Start;
                    Loop2EndOut = Entry.Loop2End;
                    Loop3StartOut = Entry.Loop3Start;
                    Loop3EndOut = Entry.Loop3End;
                    Loop1Bidirectional = Entry.Loop1Bidirectional;
                    Loop2Bidirectional = Entry.Loop2Bidirectional;
                    Loop3Bidirectional = Entry.Loop3Bidirectional;
                    OriginOut = Entry.Origin;
                    NaturalFreqOut = Entry.NaturalFreq;
                    SamplingRateOut = Entry.SamplingRate;
                    return true;
                }
            }

            DataOut = null;
            NumFramesOut = 0;
            NumChannelsOut = (NumChannelsType)0;
            Loop1StartOut = 0;
            Loop1EndOut = 0;
            Loop2StartOut = 0;
            Loop2EndOut = 0;
            Loop3StartOut = 0;
            Loop3EndOut = 0;
            Loop1Bidirectional = false;
            Loop2Bidirectional = false;
            Loop3Bidirectional = false;
            OriginOut = 0;
            NaturalFreqOut = 0;
            SamplingRateOut = 0;
            return false;
        }
    }
}
