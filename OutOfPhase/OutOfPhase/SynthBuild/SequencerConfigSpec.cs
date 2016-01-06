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
        public class OneTrackRec
        {
            public string TrackName;
            public string TrackGroupName; /* tracks controllable as a unit */
        }

        public class SequencerConfigSpecRec
        {
            public OneTrackRec[] List;
        }

        /* create a new one */
        public static SequencerConfigSpecRec NewSequencerConfigSpec()
        {
            SequencerConfigSpecRec Spec = new SequencerConfigSpecRec();

            Spec.List = new OneTrackRec[0];

            return Spec;
        }

        /* add a track to it (copies the strings, but takes ownership of track object) */
        public static void AddTrackToSequencerConfigSpec(
            SequencerConfigSpecRec Spec,
            string TrackName,
            string TrackGroupName)
        {
            OneTrackRec OneTrack = new OneTrackRec();

            OneTrack.TrackGroupName = TrackGroupName;
            OneTrack.TrackName = TrackName;

            Array.Resize(ref Spec.List, Spec.List.Length + 1);
            Spec.List[Spec.List.Length - 1] = OneTrack;
        }

        /* get number of thingies in it */
        public static int GetSequencerConfigLength(SequencerConfigSpecRec Spec)
        {
            return Spec.List.Length;
        }

        public static string SequencerConfigGetTrackName(
            SequencerConfigSpecRec Spec,
            int Index)
        {
            return Spec.List[Index].TrackName;
        }

        public static string SequencerConfigGetGroupName(
            SequencerConfigSpecRec Spec,
            int Index)
        {
            return Spec.List[Index].TrackGroupName;
        }
    }
}
