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

namespace OutOfPhase
{
    public class WAVReader : AudioFileReader
    {
        private Stream stream;
        private readonly NumBitsType numBits;
        private readonly NumChannelsType numChannels;
        private readonly int numFrames;
        private readonly int samplingRate;
        private int remainingFrames;
        private byte[] buffer = new byte[0];

        public WAVReader(Stream stream)
        {
            this.stream = stream;
            ReadPreamble(stream, out numBits, out  numChannels, out numFrames, out samplingRate);
            remainingFrames = NumFrames;
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
            out NumBitsType NumBitsOut,
            out NumChannelsType NumChannelsOut,
            out int NumSampleFramesOut,
            out int SamplingRateOut)
        {
            const string eWAVImportUnrecognizedFileFormat = "The file does not appear to be a WAV file.  Try importing it as a RAW file.";
            const string eWAVImportBadNumberOfChannels = "Only files with 1 or 2 channels can be imported.";
            const string eWAVImportNotAPCMFile = "The file is not a PCM file.";
            const string eWAVImportBadNumberOfBits = "Only 8, 16, or 24 bit files can be imported.";

            string StringBuffer;
            int TotalFileLength;
            int HeaderLength;
            short DataTypeDescriptor;
            short NumberOfChannels;
            int SamplingRate;
            int AverageBytesPerSecond;
            short BlockAlignment;
            short NumberOfBitsRaw;
            NumBitsType NumBits;
            NumChannelsType NumChannels;
            int SampledNumberOfBytes;
            int NumberOfSampleFrames;
            long audioDataOffset;

            NumBitsOut = (NumBitsType)0;
            NumChannelsOut = (NumChannelsType)0;
            NumSampleFramesOut = 0;
            SamplingRateOut = 0;

            try
            {
                using (BinaryReader File = new BinaryReader(stream))
                {
                    /*  'RIFF' */
                    StringBuffer = File.ReadFixedStringASCII(4);
                    if (!String.Equals(StringBuffer, "RIFF"))
                    {
                        throw new FormatException(eWAVImportUnrecognizedFileFormat);
                    }

                    /*  4-byte little endian length descriptor (minus 8 bytes for these 2 fields) */
                    TotalFileLength = File.ReadInt32();

                    /*  'WAVE' */
                    StringBuffer = File.ReadFixedStringASCII(4);
                    if (!String.Equals(StringBuffer, "WAVE"))
                    {
                        throw new FormatException(eWAVImportUnrecognizedFileFormat);
                    }

                    /*  'fmt ' */
                    StringBuffer = File.ReadFixedStringASCII(4);
                    if (!String.Equals(StringBuffer, "fmt "))
                    {
                        throw new FormatException(eWAVImportUnrecognizedFileFormat);
                    }

                    /*  4-byte little endian length descriptor for the 'fmt ' header block */
                    /*      - this should be 16.  if not, then it's some other kind of WAV file */
                    HeaderLength = File.ReadInt32();
                    if (HeaderLength != 16)
                    {
                        throw new FormatException(eWAVImportUnrecognizedFileFormat);
                    }

                    /*  2-byte little endian format descriptor.  this is always here. */
                    /*      - 1 = PCM */
                    DataTypeDescriptor = File.ReadInt16();
                    if (DataTypeDescriptor != 1)
                    {
                        throw new FormatException(eWAVImportNotAPCMFile);
                    }

                    /*  2-byte little endian number of channels. */
                    NumberOfChannels = File.ReadInt16();
                    if ((NumberOfChannels != 1) && (NumberOfChannels != 2))
                    {
                        throw new FormatException(eWAVImportBadNumberOfChannels);
                    }

                    /*  4-byte little endian sampling rate integer. */
                    SamplingRate = File.ReadInt32();

                    /*  4-byte little endian average bytes per second. */
                    AverageBytesPerSecond = File.ReadInt32();

                    /*  2-byte little endian block align.  for 8-bit mono, this is 1; for 16-bit */
                    /*    stereo, this is 4. */
                    BlockAlignment = File.ReadInt16();

                    /*  2-byte little endian number of bits. */
                    /*      - 8 = 8-bit */
                    /*      - 16 = 16-bit */
                    /*      - 24 = 24-bit */
                    NumberOfBitsRaw = File.ReadInt16();
                    switch (NumberOfBitsRaw)
                    {
                        default:
                            throw new FormatException(eWAVImportBadNumberOfBits);
                        case 8:
                            NumBits = NumBitsType.eSample8bit;
                            break;
                        case 16:
                            NumBits = NumBitsType.eSample16bit;
                            break;
                        case 24:
                            NumBits = NumBitsType.eSample24bit;
                            break;
                    }

                    /*  'data' */
                    StringBuffer = File.ReadFixedStringASCII(4);
                    if (!String.Equals(StringBuffer, "data"))
                    {
                        throw new FormatException(eWAVImportUnrecognizedFileFormat);
                    }

                    /*  4-byte little endian length of sample data descriptor */
                    SampledNumberOfBytes = File.ReadInt32();

                    audioDataOffset = stream.Position;

                    /* calculate number of sample frames */
                    switch (NumBits)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case NumBitsType.eSample8bit:
                            switch (NumberOfChannels)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case 1:
                                    NumberOfSampleFrames = SampledNumberOfBytes / 1;
                                    break;
                                case 2:
                                    NumberOfSampleFrames = SampledNumberOfBytes / 2;
                                    break;
                            }
                            break;
                        case NumBitsType.eSample16bit:
                            switch (NumberOfChannels)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case 1:
                                    NumberOfSampleFrames = SampledNumberOfBytes / 2;
                                    break;
                                case 2:
                                    NumberOfSampleFrames = SampledNumberOfBytes / 4;
                                    break;
                            }
                            break;
                        case NumBitsType.eSample24bit:
                            switch (NumberOfChannels)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case 1:
                                    NumberOfSampleFrames = SampledNumberOfBytes / 3;
                                    break;
                                case 2:
                                    NumberOfSampleFrames = SampledNumberOfBytes / 6;
                                    break;
                            }
                            break;
                    }

                    /*  any length data.  8-bit data goes from 0..255, but 16-bit data goes */
                    /*    from -32768 to 32767. */
                    switch (NumberOfChannels)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case 1:
                            NumChannels = NumChannelsType.eSampleMono;
                            break;
                        case 2:
                            NumChannels = NumChannelsType.eSampleStereo;
                            break;
                    }
                }
            }
            catch (InvalidDataException exception)
            {
                throw new FormatException(exception.Message);
            }

            NumBitsOut = NumBits;
            NumChannelsOut = NumChannels;
            NumSampleFramesOut = NumberOfSampleFrames;
            SamplingRateOut = SamplingRate;

            stream.Seek(audioDataOffset, SeekOrigin.Begin);
        }

        public override int TotalFrames { get { return numFrames; } }
        public override int CurrentFrame { get { return numFrames - remainingFrames; } }

        public override int ReadPoints(float[] data, int offset, int count)
        {
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
                throw new InvalidDataException(); // truncated
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
