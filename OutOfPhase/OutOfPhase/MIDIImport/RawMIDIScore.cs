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
using System.Windows.Forms;

namespace OutOfPhase
{
    public static partial class MIDIImport
    {
        /* MIDI timing mode */
        public enum MIDITimingType
        {
            eMIDIMeteredTime,
            eMIDIRealTime,
        }

        /* get format mode */
        public enum MIDIFileFormatType
        {
            eMIDIFormat0SingleTrack,
            eMIDIFormat1ParallelTracks,
            eMIDIFormat2SequentialTracks,
        }

        public class RawMIDIScoreRec
        {
            /* what kind of timing mode is being used */
            public MIDITimingType TimingMode;
            public int PartsPerQuarterNote; /* for metered time */
            public int FramesPerSecond; /* for real time */
            public int TicksPerFrame; /* for real time */

            /* what kind of file format was this */
            public MIDIFileFormatType FileFormat;

            /* list of tracks */
            public List<RawMIDITrackRec> TrackList;
        }

        /* create a new MIDI score object */
        public static RawMIDIScoreRec NewRawMIDIScore()
        {
            RawMIDIScoreRec RawScore = new RawMIDIScoreRec();
            RawScore.TrackList = new List<RawMIDITrackRec>();
            RawScore.TimingMode = MIDITimingType.eMIDIMeteredTime;
            RawScore.PartsPerQuarterNote = 24;
            RawScore.FramesPerSecond = 25;
            RawScore.TicksPerFrame = 40;
            RawScore.FileFormat = MIDIFileFormatType.eMIDIFormat1ParallelTracks;
            return RawScore;
        }

        /* append a new track to the score object */
        public static void RawMIDIScoreAppendTrack(
            RawMIDIScoreRec RawScore,
            RawMIDITrackRec Track)
        {
            RawScore.TrackList.Add(Track);
        }

        /* get the number of tracks in the score object */
        public static int GetRawMIDIScoreNumTracks(RawMIDIScoreRec RawScore)
        {
            return RawScore.TrackList.Count;
        }

        /* get an indexed track out of the score */
        public static RawMIDITrackRec GetRawMIDIScoreIndexedTrack(
            RawMIDIScoreRec RawScore,
            int Index)
        {
            return RawScore.TrackList[Index];
        }

        /* set MIDI metered timing */
        public static void RawMIDIScoreSetMeteredTime(
            RawMIDIScoreRec RawScore,
            int PartsPerQuarterNote)
        {
            RawScore.TimingMode = MIDITimingType.eMIDIMeteredTime;
            RawScore.PartsPerQuarterNote = PartsPerQuarterNote;
        }

        /* set MIDI real time */
        public static void RawMIDIScoreSetRealTime(
            RawMIDIScoreRec RawScore,
            int FramesPerSecond,
            int TicksPerFrame)
        {
            RawScore.TimingMode = MIDITimingType.eMIDIRealTime;
            RawScore.FramesPerSecond = FramesPerSecond;
            RawScore.TicksPerFrame = TicksPerFrame;
        }

        /* get MIDI timing mode */
        public static MIDITimingType RawMIDIScoreGetTimingMode(RawMIDIScoreRec RawScore)
        {
            return RawScore.TimingMode;
        }

        /* get MIDI parts per quarter note */
        public static int RawMIDIScoreGetPartsPerQuarterNote(RawMIDIScoreRec RawScore)
        {
            Debug.Assert(RawScore.TimingMode == MIDITimingType.eMIDIMeteredTime);
            return RawScore.PartsPerQuarterNote;
        }

        /* get MIDI frames per second */
        public static int RawMIDIScoreGetFramesPerSecond(RawMIDIScoreRec RawScore)
        {
            Debug.Assert(RawScore.TimingMode == MIDITimingType.eMIDIRealTime);
            return RawScore.FramesPerSecond;
        }

        /* get MIDI ticks per frame */
        public static int RawMIDIScoreGetTicksPerFrame(RawMIDIScoreRec RawScore)
        {
            Debug.Assert(RawScore.TimingMode == MIDITimingType.eMIDIRealTime);
            return RawScore.TicksPerFrame;
        }

        /* set the MIDI file type */
        public static void RawMIDIScoreSetFormatType(
            RawMIDIScoreRec RawScore,
            MIDIFileFormatType Format)
        {
            Debug.Assert((Format == MIDIFileFormatType.eMIDIFormat0SingleTrack)
                || (Format == MIDIFileFormatType.eMIDIFormat1ParallelTracks)
                || (Format == MIDIFileFormatType.eMIDIFormat2SequentialTracks));
            RawScore.FileFormat = Format;
        }

        /* get the MIDI file type */
        public static MIDIFileFormatType RawMIDIScoreGetFormatType(RawMIDIScoreRec RawScore)
        {
            return RawScore.FileFormat;
        }
    }
}
