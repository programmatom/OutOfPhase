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
    public class AIFFReader : AudioFileReader
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

        public AIFFReader(Stream stream, bool allowTruncated)
        {
            this.stream = stream;
            this.allowTruncated = allowTruncated;
            ReadPreamble(stream, out numBits, out numChannels, out numFrames, out samplingRate);
            remainingFrames = NumFrames;
        }

        public AIFFReader(Stream stream)
            : this(stream, false/*allowTruncated*/)
        {
        }

        public override NumChannelsType NumChannels { get { return numChannels; } }

        public override NumBitsType NumBits { get { return numBits; } }

        public override int NumFrames { get { return numFrames; } }

        public override int SamplingRate { get { return samplingRate; } }

        /* AIFF/AIFF-C File Format: */
        /*     "FORM" */
        /*     4-byte big endian form chunk length descriptor (minus 8 for "FORM" & this) */
        /*     4-byte type */
        /*        "AIFF" = AIFF format file */
        /*        "AIFC" = AIFF-C format file */
        /* in any order, these chunks can occur: */
        /*   Version Chunk (this only occurs in AIFF-C files) */
        /*     "FVER" */
        /*     4-byte big endian length, which should always be the value 4 (four) */
        /*     4-byte date code.  this is probably 0xA2805140 (stored big endian), but it */
        /*          probably doesn't matter. */
        /*   Common Chunk for AIFF files */
        /*     "COMM" */
        /*     4-byte big endian length. */
        /*        always 18 for AIFF files */
        /*     2-byte big endian number of channels */
        /*     4-byte big endian number of sample frames */
        /*     2-byte big endian number of bits per sample */
        /*        a value in the domain 1..32 */
        /*     10-byte extended precision number of frames per second */
        /*   Common Chunk for AIFF-C files */
        /*     "COMM" */
        /*     4-byte big endian length. */
        /*        22 + compression method string length for AIFF-C files */
        /*     2-byte big endian number of channels */
        /*     4-byte big endian number of sample frames */
        /*     2-byte big endian number of bits per sample */
        /*        a value in the domain 1..32 */
        /*     10-byte extended precision number of frames per second */
        /*     4-byte character code ID for the compression method */
        /*        "NONE" means there is no compression method used */
        /*     some characters in a string identifying the compression method */
        /*        this must be padded to an even number of bytes, but the pad is */
        /*        NOT included in the length descriptor for the chunk. */
        /*        for uncompressed data, the string should be */
        /*        "\x0enot compressed\x00", including the null, for 16 bytes. */
        /*        the total chunk length is thus 38 bytes. */
        /*   Sound Data Chunk */
        /*     "SSND" */
        /*     4-byte big endian number of bytes in sample data array */
        /*     4-byte big endian offset to the first byte of sample data in the array */
        /*     4-byte big endian number of bytes to which the sound data is aligned. */
        /*     any length vector of raw sound data. */
        /*        this must be padded to an even number of bytes, but the pad is */
        /*        NOT included in the length descriptor for the chunk. */
        /*        Samples are stored in an integral number of bytes, the smallest that */
        /*        is required for the specified number of bits.  If this is not an even */
        /*        multiple of 8, then the data is shifted left and the low bits are zeroed */
        /*        Multichannel sound is interleaved with the left channel first. */
        private static void ReadPreamble(
            Stream stream,
            out NumBitsType numBitsOut,
            out NumChannelsType numChannelsOut,
            out int numSampleFramesOut,
            out int samplingRateOut)
        {
            NumBitsType numBits = (NumBitsType)(-1);
            bool numBitsIsValid = false;
            NumChannelsType numChannels = (NumChannelsType)(-1);
            bool numChannelsIsValid = false;
            int numSampleFrames = -1;
            bool numSampleFramesIsValid = false;
            int samplingRate = -1;
            bool samplingRateIsValid = false;
            long audioDataOffset = 0;
            bool rawDataFromFileIsValid = false;

            numBitsOut = (NumBitsType)0;
            numChannelsOut = (NumChannelsType)0;
            numSampleFramesOut = 0;
            samplingRateOut = 0;

            try
            {
                using (BinaryReader File = new BinaryReader(stream))
                {
                    string charBuff;

                    /*     "FORM" */
                    charBuff = File.ReadFixedStringASCII(4);
                    if (!String.Equals(charBuff, "FORM"))
                    {
                        throw new AudioFileReaderException(AudioFileReaderErrors.UnrecognizedFileFormat);
                    }

                    /*     4-byte big endian form chunk length descriptor (minus 8 for "FORM" & this) */
                    int formChunkLength = File.ReadInt32BigEndian();

                    /*     4-byte type */
                    /*        "AIFF" = AIFF format file */
                    /*        "AIFC" = AIFF-C format file */
                    charBuff = File.ReadFixedStringASCII(4);
                    bool isAIFFC;
                    if (String.Equals(charBuff, "AIFF"))
                    {
                        isAIFFC = false;
                    }
                    else if (String.Equals(charBuff, "AIFC"))
                    {
                        isAIFFC = true;
                    }
                    else
                    {
                        throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedVariant);
                    }
                    formChunkLength -= 4;

                    /* now, read in chunks until we die */
                    while (formChunkLength > 0)
                    {
                        /* get the chunk type */
                        charBuff = File.ReadFixedStringASCII(4);
                        /* get the chunk length */
                        int localChunkLength = File.ReadInt32BigEndian();
                        formChunkLength -= 8;
                        /* adjust for even alignment */
                        if ((localChunkLength % 2) != 0)
                        {
                            localChunkLength += 1;
                        }
                        formChunkLength -= localChunkLength;

                        /* decode the chunk */
                        if (String.Equals(charBuff, "COMM"))
                        {
                            uint exponent;
                            uint mantissa;
                            byte[] extended = new byte[10];

                            if (!isAIFFC)
                            {
                                short s;

                                /*   Common Chunk for AIFF files */
                                /*     "COMM" */
                                /*     4-byte big endian length. */
                                /*        always 18 for AIFF files */
                                if (localChunkLength != 18)
                                {
                                    throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedVariant);
                                }

                                /*     2-byte big endian number of channels */
                                s = File.ReadInt16BigEndian();
                                switch (s)
                                {
                                    default:
                                        throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedNumberOfChannels);
                                    case 1:
                                        numChannels = NumChannelsType.eSampleMono;
                                        numChannelsIsValid = true;
                                        break;
                                    case 2:
                                        numChannels = NumChannelsType.eSampleStereo;
                                        numChannelsIsValid = true;
                                        break;
                                }

                                /*     4-byte big endian number of sample frames */
                                numSampleFrames = File.ReadInt32BigEndian();
                                numSampleFramesIsValid = true;

                                /*     2-byte big endian number of bits per sample */
                                /*        a value in the domain 1..32 */
                                s = File.ReadInt16BigEndian();
                                switch (s)
                                {
                                    default:
                                        throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedNumberOfBits);
                                    case 1:
                                    case 2:
                                    case 3:
                                    case 4:
                                    case 5:
                                    case 6:
                                    case 7:
                                    case 8:
                                        numBits = NumBitsType.eSample8bit;
                                        numBitsIsValid = true;
                                        break;
                                    case 9:
                                    case 10:
                                    case 11:
                                    case 12:
                                    case 13:
                                    case 14:
                                    case 15:
                                    case 16:
                                        numBits = NumBitsType.eSample16bit;
                                        numBitsIsValid = true;
                                        break;
                                    case 17:
                                    case 18:
                                    case 19:
                                    case 20:
                                    case 21:
                                    case 22:
                                    case 23:
                                    case 24:
                                        numBits = NumBitsType.eSample24bit;
                                        numBitsIsValid = true;
                                        break;
                                }

                                /*     10-byte extended precision number of frames per second */
                                File.ReadRaw(extended, 0, 10);
                            }
                            else
                            {
                                short s;

                                /*   Common Chunk for AIFF-C files */
                                /*     "COMM" */
                                /*     4-byte big endian length. */
                                /*        always 18 for AIFF files */
                                if (localChunkLength < 22)
                                {
                                    throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedVariant);
                                }

                                /*     2-byte big endian number of channels */
                                s = File.ReadInt16BigEndian();
                                switch (s)
                                {
                                    default:
                                        throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedNumberOfChannels);
                                    case 1:
                                        numChannels = NumChannelsType.eSampleMono;
                                        numChannelsIsValid = true;
                                        break;
                                    case 2:
                                        numChannels = NumChannelsType.eSampleStereo;
                                        numChannelsIsValid = true;
                                        break;
                                }

                                /*     4-byte big endian number of sample frames */
                                numSampleFrames = File.ReadInt32BigEndian();
                                numSampleFramesIsValid = true;

                                /*     2-byte big endian number of bits per sample */
                                /*        a value in the domain 1..32 */
                                s = File.ReadInt16BigEndian();
                                switch (s)
                                {
                                    default:
                                        throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedNumberOfBits);
                                    case 1:
                                    case 2:
                                    case 3:
                                    case 4:
                                    case 5:
                                    case 6:
                                    case 7:
                                    case 8:
                                        numBits = NumBitsType.eSample8bit;
                                        numBitsIsValid = true;
                                        break;
                                    case 9:
                                    case 10:
                                    case 11:
                                    case 12:
                                    case 13:
                                    case 14:
                                    case 15:
                                    case 16:
                                        numBits = NumBitsType.eSample16bit;
                                        numBitsIsValid = true;
                                        break;
                                    case 17:
                                    case 18:
                                    case 19:
                                    case 20:
                                    case 21:
                                    case 22:
                                    case 23:
                                    case 24:
                                        numBits = NumBitsType.eSample24bit;
                                        numBitsIsValid = true;
                                        break;
                                }

                                /*     10-byte extended precision number of frames per second */
                                File.ReadRaw(extended, 0, 10);

                                /*     4-byte character code ID for the compression method */
                                /*        "NONE" means there is no compression method used */
                                charBuff = File.ReadFixedStringASCII(4);
                                if (!String.Equals(charBuff, "NONE"))
                                {
                                    throw new AudioFileReaderException(AudioFileReaderErrors.NotUncompressedPCM);
                                }

                                /*     some characters in a string identifying the compression method */
                                /*        this must be padded to an even number of bytes, but the pad is */
                                /*        NOT included in the length descriptor for the chunk. */
                                /*        for uncompressed data, the string should be */
                                /*        "\x0enot compressed\x00", including the null, for 16 bytes. */
                                /*        the total chunk length is thus 38 bytes. */
                                for (int i = 0; i < localChunkLength - 22; i++)
                                {
                                    File.ReadByte();
                                }
                            }

                            /* extended 22050 = 400D AC44000000000000 */
                            /* extended 22051 = 400D AC46000000000000 */
                            /* extended 44100 = 400E AC44000000000000 */
                            /* extended 44101 = 400E AC45000000000000 */
                            exponent = (((uint)extended[0] & 0xff) << 8)
                                | ((uint)extended[1] & 0xff);
                            mantissa = (((uint)extended[2] & 0xff) << 24)
                                | (((uint)extended[3] & 0xff) << 16)
                                | (((uint)extended[4] & 0xff) << 8)
                                | ((uint)extended[5] & 0xff);
                            samplingRate = (int)(mantissa >> (0x401e - (int)exponent));
                            if (samplingRate < Constants.MINSAMPLINGRATE)
                            {
                                samplingRate = Constants.MINSAMPLINGRATE;
                            }
                            if (samplingRate > Constants.MAXSAMPLINGRATE)
                            {
                                samplingRate = Constants.MAXSAMPLINGRATE;
                            }
                            samplingRateIsValid = true;
                        }
                        else if (String.Equals(charBuff, "SSND"))
                        {
                            /*   Sound Data Chunk */
                            /*     "SSND" */
                            /*     4-byte big endian number of bytes in sample data array */

                            /* only one of these is allowed */
                            if (rawDataFromFileIsValid)
                            {
                                throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedVariant);
                            }

                            /*     4-byte big endian offset to the first byte of sample data in the array */
                            int offsetToFirstByte = File.ReadInt32BigEndian();
                            if (offsetToFirstByte != 0)
                            {
                                throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedVariant);
                            }

                            /*     4-byte big endian number of bytes to which the sound data is aligned. */
                            int alignmentFactor = File.ReadInt32BigEndian();
                            if (alignmentFactor != 0)
                            {
                                throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedVariant);
                            }

                            /*     any length vector of raw sound data. */
                            /*        this must be padded to an even number of bytes, but the pad is */
                            /*        NOT included in the length descriptor for the chunk. */
                            /*        Samples are stored in an integral number of bytes, the smallest that */
                            /*        is required for the specified number of bits.  If this is not an even */
                            /*        multiple of 8, then the data is shifted left and the low bits are zeroed */
                            /*        Multichannel sound is interleaved with the left channel first. */
                            if (stream.Length - stream.Position < localChunkLength - 8)
                            {
                                throw new AudioFileReaderException(AudioFileReaderErrors.Truncated);
                            }
                            rawDataFromFileIsValid = true;
                            audioDataOffset = stream.Position;

                            stream.Seek(localChunkLength - 8, SeekOrigin.Current);
                        }
                        else
                        {
                            /* just read the data & get rid of it */
                            while (localChunkLength > 0)
                            {
                                File.ReadByte();
                                localChunkLength -= 1;
                            }
                        }
                    }
                }
            }
            catch (InvalidDataException)
            {
                throw new AudioFileReaderException(AudioFileReaderErrors.InvalidData);
            }

            if (!rawDataFromFileIsValid || !numBitsIsValid || !numChannelsIsValid || !samplingRateIsValid || !numSampleFramesIsValid)
            {
                throw new AudioFileReaderException(AudioFileReaderErrors.UnsupportedVariant);
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
                                data[i + offset] = SampConv.SignedShortToFloat(dataReader.ReadInt16BigEndian());
                            }
                            break;
                        case NumBitsType.eSample24bit:
                            for (int i = 0; i < points; i++)
                            {
                                data[i + offset] = SampConv.SignedTribyteToFloat(dataReader.ReadInt24BigEndian());
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
