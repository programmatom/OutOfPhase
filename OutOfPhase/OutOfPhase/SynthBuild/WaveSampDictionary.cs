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
        /* record for one sample object */
        public class SampleRec
        {
            /* heap block containing name of object */
            public string Name;

            /* information for object */
            public float[] SampleDataReference;
            public NumChannelsType NumChannels;
            public int NumFrames;
            public int Loop1Start;
            public int Loop1End;
            public int Loop2Start;
            public int Loop2End;
            public int Loop3Start;
            public int Loop3End;
            public bool Loop1Bidirectional;
            public bool Loop2Bidirectional;
            public bool Loop3Bidirectional;
            public int Origin;
            public double NaturalFreq;
            public int SamplingRate;
        }

        /* record for one wave table object */
        public class WaveTableRec
        {
            /* heap block containing name of object */
            public string Name;

            /* information for object */
            public float[][] WaveVectorReference; /* array of references to wave tables */
            public int NumTables;
            public int FramesPerTable;
        }

        /* list of objects */
        public class WaveSampDictRec
        {
            public Dictionary<string, SampleRec> Samples = new Dictionary<string, SampleRec>();
            public Dictionary<string, WaveTableRec> WaveTables = new Dictionary<string, WaveTableRec>();
        }

        /* create new wave table and sample dictionary */
        public static WaveSampDictRec NewWaveSampDictionary(
            IEnumerable<SampleObjectRec> SampleList,
            IEnumerable<AlgoSampObjectRec> AlgoSampList,
            IEnumerable<WaveTableObjectRec> WaveTableList,
            IEnumerable<AlgoWaveTableObjectRec> AlgoWaveTableList)
        {
            WaveSampDictRec Dict = new WaveSampDictRec();

            foreach (SampleObjectRec Sample in SampleList)
            {
                SampleRec NewNode = new SampleRec();

                NewNode.Name = Sample.Name;
                Dict.Samples.Add(NewNode.Name, NewNode);

                NewNode.SampleDataReference = Sample.SampleData.Buffer;
                NewNode.NumChannels = Sample.SampleData.NumChannels;
                Debug.Assert(Sample.NumChannels == Sample.SampleData.NumChannels);
                NewNode.NumFrames = Sample.SampleData.NumFrames;
                Debug.Assert(Sample.NumFrames == Sample.SampleData.NumFrames);
                NewNode.Loop1Start = Sample.LoopStart1;
                NewNode.Loop1End = Sample.LoopEnd1;
                NewNode.Loop2Start = Sample.LoopStart2;
                NewNode.Loop2End = Sample.LoopEnd2;
                NewNode.Loop3Start = Sample.LoopStart3;
                NewNode.Loop3End = Sample.LoopEnd3;
                NewNode.Loop1Bidirectional = Sample.Loop1Bidirectional == LoopBidirectionalType.Yes;
                NewNode.Loop2Bidirectional = Sample.Loop2Bidirectional == LoopBidirectionalType.Yes;
                NewNode.Loop3Bidirectional = Sample.Loop3Bidirectional == LoopBidirectionalType.Yes;
                NewNode.Origin = Sample.Origin;
                NewNode.NaturalFreq = Sample.NaturalFrequency;
                NewNode.SamplingRate = Sample.SamplingRate;
            }

            foreach (AlgoSampObjectRec AlgoSamp in AlgoSampList)
            {
                SampleRec NewNode = new SampleRec();

                NewNode.Name = AlgoSamp.Name;
                Dict.Samples.Add(NewNode.Name, NewNode);

                NewNode.SampleDataReference = AlgoSamp.SampleData.Buffer;
                NewNode.NumChannels = AlgoSamp.NumChannels;
                Debug.Assert(AlgoSamp.NumChannels == AlgoSamp.SampleData.NumChannels);
                NewNode.NumFrames = AlgoSamp.SampleData.NumFrames;
                NewNode.Loop1Start = AlgoSamp.LoopStart1;
                NewNode.Loop1End = AlgoSamp.LoopEnd1;
                NewNode.Loop2Start = AlgoSamp.LoopStart2;
                NewNode.Loop2End = AlgoSamp.LoopEnd2;
                NewNode.Loop3Start = AlgoSamp.LoopStart3;
                NewNode.Loop3End = AlgoSamp.LoopEnd3;
                NewNode.Loop1Bidirectional = AlgoSamp.Loop1Bidirectional == LoopBidirectionalType.Yes;
                NewNode.Loop2Bidirectional = AlgoSamp.Loop2Bidirectional == LoopBidirectionalType.Yes;
                NewNode.Loop3Bidirectional = AlgoSamp.Loop3Bidirectional == LoopBidirectionalType.Yes;
                NewNode.Origin = AlgoSamp.Origin;
                NewNode.NaturalFreq = AlgoSamp.NaturalFrequency;
                NewNode.SamplingRate = AlgoSamp.SamplingRate;
            }

            foreach (WaveTableObjectRec WaveTable in WaveTableList)
            {
                WaveTableRec NewNode = new WaveTableRec();

                NewNode.Name = WaveTable.Name;
                Dict.WaveTables.Add(NewNode.Name, NewNode);

                NewNode.NumTables = WaveTable.NumTables;
                NewNode.FramesPerTable = WaveTable.NumFrames;

                NewNode.WaveVectorReference = new float[NewNode.FramesPerTable][];
                for (int i = 0; i < NewNode.NumTables; i++)
                {
                    NewNode.WaveVectorReference[i] = WaveTable.WaveTableData.ListOfTables[i].frames;
                    Debug.Assert(NewNode.WaveVectorReference[i].Length == NewNode.FramesPerTable + 1);
                }
            }

            foreach (AlgoWaveTableObjectRec AlgoWaveTable in AlgoWaveTableList)
            {
                WaveTableRec NewNode = new WaveTableRec();

                NewNode.Name = AlgoWaveTable.Name;
                Dict.WaveTables.Add(NewNode.Name, NewNode);

                NewNode.NumTables = AlgoWaveTable.NumTables;
                NewNode.FramesPerTable = AlgoWaveTable.NumFrames;

                NewNode.WaveVectorReference = new float[NewNode.FramesPerTable][];
                for (int i = 0; i < NewNode.NumTables; i++)
                {
                    NewNode.WaveVectorReference[i] = AlgoWaveTable.WaveTableData.ListOfTables[i].frames;
                    Debug.Assert(NewNode.WaveVectorReference[i].Length == NewNode.FramesPerTable + 1);
                }
            }
            return Dict;
        }

        /* get information for a sample object.  returns False if not found. */
        public static bool WaveSampDictGetSampleInfo(
            WaveSampDictRec Dict,
            string Name,
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
            DataOut = null;
            NumFramesOut = 0;
            NumChannelsOut = 0;
            Loop1StartOut = 0;
            Loop1EndOut = 0;
            Loop2StartOut = 0;
            Loop2EndOut = 0;
            Loop3StartOut = 0;
            Loop3EndOut = 0;
            OriginOut = 0;
            NaturalFreqOut = 0;
            SamplingRateOut = 0;
            Loop1Bidirectional = false;
            Loop2Bidirectional = false;
            Loop3Bidirectional = false;

            SampleRec Sample;

            if (!Dict.Samples.TryGetValue(Name, out Sample))
            {
                return false;
            }

            DataOut = Sample.SampleDataReference;
            NumFramesOut = Sample.NumFrames;
            NumChannelsOut = Sample.NumChannels;
            Loop1StartOut = Sample.Loop1Start;
            Loop1EndOut = Sample.Loop1End;
            Loop2StartOut = Sample.Loop2Start;
            Loop2EndOut = Sample.Loop2End;
            Loop3StartOut = Sample.Loop3Start;
            Loop3EndOut = Sample.Loop3End;
            OriginOut = Sample.Origin;
            NaturalFreqOut = Sample.NaturalFreq;
            SamplingRateOut = Sample.SamplingRate;
            Loop1Bidirectional = Sample.Loop1Bidirectional;
            Loop2Bidirectional = Sample.Loop2Bidirectional;
            Loop3Bidirectional = Sample.Loop3Bidirectional;

            return true;
        }

        /* get information for a wave table object.  returns False if not found */
        public static bool WaveSampDictGetWaveTableInfo(
            WaveSampDictRec Dict,
            string Name,
            out float[][] TwoDimensionalVecOut,
            out int NumFramesOut,
            out int NumTablesOut)
        {
            TwoDimensionalVecOut = null;
            NumFramesOut = 0;
            NumTablesOut = 0;

            WaveTableRec WaveTable;

            if (!Dict.WaveTables.TryGetValue(Name, out WaveTable))
            {
                return false;
            }

            TwoDimensionalVecOut = WaveTable.WaveVectorReference;
            NumFramesOut = WaveTable.FramesPerTable;
            NumTablesOut = WaveTable.NumTables;

            return true;
        }

        /* find out if named wave table exists.  returns True if it does */
        public static bool WaveSampDictDoesWaveTableExist(
            WaveSampDictRec Dict,
            string Name)
        {
            return Dict.WaveTables.ContainsKey(Name);
        }

        /* find out if named sample exists.  returns True if it does */
        public static bool WaveSampDictDoesSampleExist(
            WaveSampDictRec Dict,
            string Name)
        {
            return Dict.Samples.ContainsKey(Name);
        }
    }
}
