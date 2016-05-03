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
using System.Diagnostics;

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

        public abstract bool Truncated { get; }

        public abstract void Close();
        public abstract void Dispose();
    }

    public enum AudioFileReaderErrors
    {
        Success = 0,

        UnrecognizedFileFormat,
        UnsupportedVariant,
        UnsupportedNumberOfChannels,
        NotUncompressedPCM,
        UnsupportedNumberOfBits,
        InvalidData,
        Truncated,
        OutOfMemory,
    }

    public class AudioFileReaderException : Exception
    {
        private readonly AudioFileReaderErrors error;

        public AudioFileReaderException(AudioFileReaderErrors error)
        {
            this.error = error;
        }

        public AudioFileReaderErrors Error { get { return error; } }

        public static string MessageFromError(AudioFileReaderErrors error)
        {
            switch (error)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case AudioFileReaderErrors.Success:
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case AudioFileReaderErrors.UnrecognizedFileFormat:
                    return "The format of file content is not recognized.";
                case AudioFileReaderErrors.UnsupportedVariant:
                    return "The file contains valid audio data but uses file format features that are not supported.";
                case AudioFileReaderErrors.UnsupportedNumberOfChannels:
                    return "Only files with 1 or 2 channels can be imported.";
                case AudioFileReaderErrors.NotUncompressedPCM:
                    return "The audio data is in a format other than uncompressed PCM. Only uncompressed PCM audio data is supported.";
                case AudioFileReaderErrors.UnsupportedNumberOfBits:
                    return "Only 8, 16, or 24 bit files can be imported.";
                case AudioFileReaderErrors.InvalidData:
                    return "The file contains invalid data.";
                case AudioFileReaderErrors.Truncated:
                    return "The file is shorter than the header indicates it should be.";
                case AudioFileReaderErrors.OutOfMemory:
                    return "There is not enough memory available to load the file.";
            }
        }
    }
}
