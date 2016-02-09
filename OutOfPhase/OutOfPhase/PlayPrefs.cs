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
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace OutOfPhase
{
    // Encapsulates all playback parameters as can be configured in PlayPrefsDialog
    public interface IPlayPrefsProvider
    {
        int SamplingRate { get; }
        int EnvelopeUpdateRate { get; }
        int Oversampling { get; }
        double DefaultBeatsPerMinute { get; }
        double OverallVolumeScalingFactor { get; }
        NumBitsType OutputNumBits { get; }
        double ScanningGap { get; }
        double BufferDuration { get; }
        bool ClipWarning { get; }
        NumChannelsType NumChannels { get; }
        bool ShowSummary { get; }
        bool Deterministic { get; }
        int Seed { get; }
        TrackObjectRec[] IncludedTracks { get; }
    }

    // Delegates to the document object - used to supply parameters to clients when PlayPrefsDialog is not open
    public class PlayPrefsDocumentDelegator : IPlayPrefsProvider
    {
        private readonly Document document;

        public PlayPrefsDocumentDelegator(Document document)
        {
            this.document = document;
        }

        public int SamplingRate { get { return document.SamplingRate; } }
        public int EnvelopeUpdateRate { get { return document.EnvelopeUpdateRate; } }
        public int Oversampling { get { return document.Oversampling; } }
        public double DefaultBeatsPerMinute { get { return document.DefaultBeatsPerMinute; } }
        public double OverallVolumeScalingFactor { get { return document.OverallVolumeScalingFactor; } }
        public NumBitsType OutputNumBits { get { return document.OutputNumBits; } }
        public double ScanningGap { get { return document.ScanningGap; } }
        public double BufferDuration { get { return document.BufferDuration; } }
        public bool ClipWarning { get { return document.ClipWarning; } }
        public NumChannelsType NumChannels { get { return document.NumChannels; } }
        public bool ShowSummary { get { return document.ShowSummary; } }
        public bool Deterministic { get { return document.Deterministic; } }
        public int Seed { get { return document.Seed; } }

        public TrackObjectRec[] IncludedTracks
        {
            get
            {
                List<TrackObjectRec> included = new List<TrackObjectRec>(document.TrackList.Count);
                foreach (TrackObjectRec track in document.TrackList)
                {
                    if (track.IncludeThisTrackInFinalPlayback)
                    {
                        included.Add(track);
                    }
                }
                return included.ToArray();
            }
        }
    }

    // The PlayPrefsDialog delegator is implemented in the contained class Source
}
