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
using System.IO;

namespace OutOfPhase
{
    public class WAVReader : AudioFileReader
    {
        private Stream stream;
        private readonly NumBitsType numBits;
        private readonly NumChannelsType numChannels;
        private readonly int numFrames;
        private readonly int samplingRate;
        private readonly bool allowTruncated;
        private int remainingFrames;
        private byte[] buffer = new byte[0];
        private bool truncated;

        public WAVReader(Stream stream, bool allowTruncated)
        {
            this.stream = stream;
            this.allowTruncated = allowTruncated;
            ReadPreamble(stream, out numBits, out numChannels, out numFrames, out samplingRate);
            remainingFrames = NumFrames;
        }

        public WAVReader(Stream stream)
            : this(stream, false/*allowTruncated*/)
        {
        }

        public override NumChannelsType NumChannels { get { return numChannels; } }

        public override NumBitsType NumBits { get { return numBits; } }

        public override int NumFrames { get { return numFrames; } }

        public override int SamplingRate { get { return samplingRate; } }

        /* RIFF file format, with WAVE information */
        /*  'RIFF' */
        /*  4-byte little endian length descriptor (minus 8 bytes for these 2 fields) */
        /*  'WAVE' */
        /*  'fmt ' */
        /*  4-byte little endian length descriptor for the 'fmt ' header block */
        /*      - this should be 16.  if not, then it's some other kind of WAV file */
        /*  2-byte little endian format descriptor.  this is always here. */
        /*      - 1 = PCM */
        /*  2-byte little endian number of channels. */
        /*  4-byte little endian sampling rate integer. */
        /*  4-byte little endian average bytes per second. */
        /*  2-byte little endian block align.  for 8-bit mono, this is 1; for 16-bit */
        /*    stereo, this is 4. */
        /*  2-byte little endian number of bits. */
        /*      - 8 = 8-bit */
        /*      - 16 = 16-bit */
        /*      - 24 = 24-bit */
        /*  'data' */
        /*  4-byte little endian length of sample data descriptor */
        /*  any length data.  8-bit data goes from 0..255, but 16-bit data goes */
        /*    from -32768 to 32767. */
        public static void ReadPreamble(
            Stream stream,
            out NumBitsType numBitsOut,
            out NumChannelsType numChannelsOut,
            out int numSampleFramesOut,
            out int samplingRateOut)
        {
            NumBitsType numBits;
            NumChannelsType numChannels;
            int numSampleFrames;
            int samplingRate;
            long audioDataOffset;

            numBitsOut = (NumBitsType)0;
            numChannelsOut = (NumChannelsType)0;
            numSampleFramesOut = 0;
            samplingRateOut = 0;

            try
            {
                using (BinaryReader File = new BinaryReader(stream))
                {
                    string stringBuffer;

                    /*  'RIFF' */
                    stringBuffer = File.ReadFixedStringASCII(4);
                    if (!String.Equals(stringBuffer, "RIFF"))
                    {
                        throw new AudioFileReaderException(AudioFileReaderErrors.UnrecognizedFileFormat);
                    }

                    /*  4-byte little endian length descriptor (minus 8 bytes for these 2 fields) */
                    int totalFileLength = File.ReadInt32();

                    /*  'WAVE' */
                    stringBuffer = File.ReadFixedStringASCII(4);
                    if (!String.Equals(stringBuffer, "WAVE"))
                    {
                        throw new AudioFileReaderException(AudioFileReaderErrors.UnrecognizedFileFormat);
                    }

                    /*  'fmt ' */
                    stringBuffer = File.ReadFixedStringASCII(4);
                    if (!String.Equals(stringBuffer, "fmt "))
                    {
                        throw new AudioFileReaderException(AudioFileReaderErrors.UnrecognizedFileFormat);
                    }

                    /*  4-byte little endian length descriptor for the 'fmt ' header block */
                    /*      - this should be 16.  if not, then it's some other kind of WAV file */
                    int headerLength = File.ReadInt32();
                    if (headerLength != 16)
                    {
                        throw new AudioFileReaderException(AudioFileReaderErrors.UnrecognizedFileFormat);
                    }

                    /*  2-byte little endian format descriptor.  this is always here. */
                    /*      - 1 = PCM */
                    short dataTypeDescriptor = File.ReadInt16();
                    if (dataTypeDescriptor != 1)
                    {
                        throw new AudioFileReaderException(AudioFileReaderErrors.NotUncompressedPCM);
                    }

                    /*  2-byte little endian number of channels. */
                    short numberOfChannelsRaw = File.ReadInt16();
                    if ((numberOfChannelsRaw != 1) && (numberOfChannelsRaw != 2))
                    {
                        throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedNumberOfChannels);
                    }

                    /*  4-byte little endian sampling rate integer. */
                    samplingRate = File.ReadInt32();

                    /*  4-byte little endian average bytes per second. */
                    int averageBytesPerSecond = File.ReadInt32();

                    /*  2-byte little endian block align.  for 8-bit mono, this is 1; for 16-bit */
                    /*    stereo, this is 4. */
                    short blockAlignment = File.ReadInt16();

                    /*  2-byte little endian number of bits. */
                    /*      - 8 = 8-bit */
                    /*      - 16 = 16-bit */
                    /*      - 24 = 24-bit */
                    short numberOfBitsRaw = File.ReadInt16();
                    switch (numberOfBitsRaw)
                    {
                        default:
                            throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedNumberOfBits);
                        case 8:
                            numBits = NumBitsType.eSample8bit;
                            break;
                        case 16:
                            numBits = NumBitsType.eSample16bit;
                            break;
                        case 24:
                            numBits = NumBitsType.eSample24bit;
                            break;
                    }

                    /*  'data' */
                    stringBuffer = File.ReadFixedStringASCII(4);
                    if (!String.Equals(stringBuffer, "data"))
                    {
                        throw new AudioFileReaderException(AudioFileReaderErrors.UnrecognizedFileFormat);
                    }

                    /*  4-byte little endian length of sample data descriptor */
                    int sampledNumberOfBytes = File.ReadInt32();

                    audioDataOffset = stream.Position;

                    /* calculate number of sample frames */
                    switch (numBits)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case NumBitsType.eSample8bit:
                            switch (numberOfChannelsRaw)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case 1:
                                    numSampleFrames = sampledNumberOfBytes / 1;
                                    break;
                                case 2:
                                    numSampleFrames = sampledNumberOfBytes / 2;
                                    break;
                            }
                            break;
                        case NumBitsType.eSample16bit:
                            switch (numberOfChannelsRaw)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case 1:
                                    numSampleFrames = sampledNumberOfBytes / 2;
                                    break;
                                case 2:
                                    numSampleFrames = sampledNumberOfBytes / 4;
                                    break;
                            }
                            break;
                        case NumBitsType.eSample24bit:
                            switch (numberOfChannelsRaw)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case 1:
                                    numSampleFrames = sampledNumberOfBytes / 3;
                                    break;
                                case 2:
                                    numSampleFrames = sampledNumberOfBytes / 6;
                                    break;
                            }
                            break;
                    }

                    /*  any length data.  8-bit data goes from 0..255, but 16-bit data goes */
                    /*    from -32768 to 32767. */
                    switch (numberOfChannelsRaw)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case 1:
                            numChannels = NumChannelsType.eSampleMono;
                            break;
                        case 2:
                            numChannels = NumChannelsType.eSampleStereo;
                            break;
                    }
                }
            }
            catch (InvalidDataException)
            {
                throw new AudioFileReaderException(AudioFileReaderErrors.InvalidData);
            }

            numBitsOut = numBits;
            numChannelsOut = numChannels;
            numSampleFramesOut = numSampleFrames;
            samplingRateOut = samplingRate;

            stream.Seek(audioDataOffset, SeekOrigin.Begin);
        }

        public override int TotalFrames { get { return numFrames; } }
        public override int CurrentFrame { get { return numFrames - remainingFrames; } }

        public override bool Truncated { get { return truncated; } }

        public override int ReadPoints(float[] data, int offset, int count)
        {
            if (truncated && !allowTruncated)
            {
                throw new InvalidDataException();
            }
            int frames = Math.Min(remainingFrames, count / PointsPerFrame);
            remainingFrames -= frames;
            int points = frames * PointsPerFrame;
            int bytesPerFrame = 0;
            switch (numBits)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case NumBitsType.eSample8bit:
                    bytesPerFrame = 1;
                    break;
                case NumBitsType.eSample16bit:
                    bytesPerFrame = 2;
                    break;
                case NumBitsType.eSample24bit:
                    bytesPerFrame = 3;
                    break;
            }
            bytesPerFrame *= PointsPerFrame;
            if (buffer.Length < bytesPerFrame * frames)
            {
                buffer = new byte[bytesPerFrame * frames];
            }
            int p = 0;
            int c;
            while ((c = stream.Read(buffer, p, bytesPerFrame * frames - p)) != 0)
            {
                p += c;
            }
            if (p != bytesPerFrame * frames)
            {
                truncated = true;
                if (!allowTruncated)
                {
                    throw new AudioFileReaderException(AudioFileReaderErrors.Truncated);
                }
                Array.Clear(buffer, p, bytesPerFrame * frames - p);
            }
            using (Stream dataStream = new MemoryStream(buffer, 0, bytesPerFrame * frames))
            {
                using (BinaryReader dataReader = new BinaryReader(dataStream))
                {
                    switch (numBits)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case NumBitsType.eSample8bit:
                            for (int i = 0; i < points; i++)
                            {
                                data[i + offset] = SampConv.SignedByteToFloat(dataReader.ReadByte());
                            }
                            break;
                        case NumBitsType.eSample16bit:
                            for (int i = 0; i < points; i++)
                            {
                                data[i + offset] = SampConv.SignedShortToFloat(dataReader.ReadInt16());
                            }
                            break;
                        case NumBitsType.eSample24bit:
                            for (int i = 0; i < points; i++)
                            {
                                data[i + offset] = SampConv.SignedTribyteToFloat(dataReader.ReadInt24());
                            }
                            break;
                    }
                }
            }
            return points;
        }

        public override void Close()
        {
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
        }

        public override void Dispose()
        {
            Close();
        }
    }
}
