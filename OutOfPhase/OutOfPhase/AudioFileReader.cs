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
    public abstract class AudioFileReader : IDisposable
    {
        public abstract NumChannelsType NumChannels { get; }
        public abstract NumBitsType NumBits { get; }
        public abstract int NumFrames { get; }
        public int PointsPerFrame { get { return NumChannels == NumChannelsType.eSampleStereo ? 2 : 1; } }
        public abstract int SamplingRate { get; }

        public abstract int TotalFrames { get; }
        public abstract int CurrentFrame { get; }

        public abstract int ReadPoints(float[] data, int offset, int count);

        public abstract void Close();
        public abstract void Dispose();
    }
}
