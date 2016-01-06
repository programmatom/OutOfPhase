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
    public class AIFFReader : AudioFileReader
    {
        private Stream stream;
        private readonly NumBitsType numBits;
        private readonly NumChannelsType numChannels;
        private readonly int numFrames;
        private readonly int samplingRate;
        private int remainingFrames;
        private byte[] buffer = new byte[0];

        public AIFFReader(Stream stream)
        {
            this.stream = stream;
            ReadPreamble(stream, out numBits, out  numChannels, out numFrames, out samplingRate);
            remainingFrames = NumFrames;
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
            out NumBitsType NumBitsOut,
            out NumChannelsType NumChannelsOut,
            out int NumSampleFramesOut,
            out int SamplingRateOut)
        {
            const string ErrorUnrecognized = "The file is not an AIFF or AIFF-C file.";
            const string ErrorUnsupportedVariant = "The file is not a supported variant of AIFF or AIFF-C.";
            const string ErrorTooShort = "The file appears to be incomplete.";

            string CharBuff;
            bool IsAnAIFFCFile;
            int FormChunkLength;
            bool RawDataFromFileIsValid = false;
            NumBitsType NumBits = (NumBitsType)(-1);
            bool NumBitsIsValid = false;
            NumChannelsType NumChannels = (NumChannelsType)(-1);
            bool NumChannelsIsValid = false;
            int SamplingRate = -1;
            bool SamplingRateIsValid = false;
            int NumSampleFrames = -1;
            bool NumSampleFramesIsValid = false;
            long audioDataOffset = 0;

            NumBitsOut = (NumBitsType)0;
            NumChannelsOut = (NumChannelsType)0;
            NumSampleFramesOut = 0;
            SamplingRateOut = 0;

            try
            {
                using (BinaryReader File = new BinaryReader(stream))
                {
                    /*     "FORM" */
                    CharBuff = File.ReadFixedStringASCII(4);
                    if (!String.Equals(CharBuff, "FORM"))
                    {
                        throw new FormatException(ErrorUnrecognized);
                    }

                    /*     4-byte big endian form chunk length descriptor (minus 8 for "FORM" & this) */
                    FormChunkLength = File.ReadInt32BigEndian();

                    /*     4-byte type */
                    /*        "AIFF" = AIFF format file */
                    /*        "AIFC" = AIFF-C format file */
                    CharBuff = File.ReadFixedStringASCII(4);
                    if (String.Equals(CharBuff, "AIFF"))
                    {
                        IsAnAIFFCFile = false;
                    }
                    else if (String.Equals(CharBuff, "AIFC"))
                    {
                        IsAnAIFFCFile = true;
                    }
                    else
                    {
                        throw new FormatException(ErrorUnsupportedVariant);
                    }
                    FormChunkLength -= 4;

                    /* now, read in chunks until we die */
                    while (FormChunkLength > 0)
                    {
                        int LocalChunkLength;

                        /* get the chunk type */
                        CharBuff = File.ReadFixedStringASCII(4);
                        /* get the chunk length */
                        LocalChunkLength = File.ReadInt32BigEndian();
                        FormChunkLength -= 8;
                        /* adjust for even alignment */
                        if ((LocalChunkLength % 2) != 0)
                        {
                            LocalChunkLength += 1;
                        }
                        FormChunkLength -= LocalChunkLength;

                        /* decode the chunk */
                        if (String.Equals(CharBuff, "COMM"))
                        {
                            uint Exponent;
                            uint Mantissa;
                            byte[] StupidExtendedThang = new byte[10];

                            if (!IsAnAIFFCFile)
                            {
                                short ShortInteger;

                                /*   Common Chunk for AIFF files */
                                /*     "COMM" */
                                /*     4-byte big endian length. */
                                /*        always 18 for AIFF files */
                                if (LocalChunkLength != 18)
                                {
                                    throw new FormatException(ErrorUnsupportedVariant);
                                }

                                /*     2-byte big endian number of channels */
                                ShortInteger = File.ReadInt16BigEndian();
                                switch (ShortInteger)
                                {
                                    default:
                                        throw new FormatException(ErrorUnsupportedVariant);
                                    case 1:
                                        NumChannels = NumChannelsType.eSampleMono;
                                        NumChannelsIsValid = true;
                                        break;
                                    case 2:
                                        NumChannels = NumChannelsType.eSampleStereo;
                                        NumChannelsIsValid = true;
                                        break;
                                }

                                /*     4-byte big endian number of sample frames */
                                NumSampleFrames = File.ReadInt32BigEndian();
                                NumSampleFramesIsValid = true;

                                /*     2-byte big endian number of bits per sample */
                                /*        a value in the domain 1..32 */
                                ShortInteger = File.ReadInt16BigEndian();
                                switch (ShortInteger)
                                {
                                    default:
                                        throw new FormatException(ErrorUnsupportedVariant);
                                    case 1:
                                    case 2:
                                    case 3:
                                    case 4:
                                    case 5:
                                    case 6:
                                    case 7:
                                    case 8:
                                        NumBits = NumBitsType.eSample8bit;
                                        NumBitsIsValid = true;
                                        break;
                                    case 9:
                                    case 10:
                                    case 11:
                                    case 12:
                                    case 13:
                                    case 14:
                                    case 15:
                                    case 16:
                                        NumBits = NumBitsType.eSample16bit;
                                        NumBitsIsValid = true;
                                        break;
                                    case 17:
                                    case 18:
                                    case 19:
                                    case 20:
                                    case 21:
                                    case 22:
                                    case 23:
                                    case 24:
                                        NumBits = NumBitsType.eSample24bit;
                                        NumBitsIsValid = true;
                                        break;
                                }

                                /*     10-byte extended precision number of frames per second */
                                File.ReadRaw(StupidExtendedThang, 0, 10);
                            }
                            else
                            {
                                short ShortInteger;

                                /*   Common Chunk for AIFF-C files */
                                /*     "COMM" */
                                /*     4-byte big endian length. */
                                /*        always 18 for AIFF files */
                                if (LocalChunkLength < 22)
                                {
                                    throw new FormatException(ErrorUnsupportedVariant);
                                }

                                /*     2-byte big endian number of channels */
                                ShortInteger = File.ReadInt16BigEndian();
                                switch (ShortInteger)
                                {
                                    default:
                                        throw new FormatException(ErrorUnsupportedVariant);
                                    case 1:
                                        NumChannels = NumChannelsType.eSampleMono;
                                        NumChannelsIsValid = true;
                                        break;
                                    case 2:
                                        NumChannels = NumChannelsType.eSampleStereo;
                                        NumChannelsIsValid = true;
                                        break;
                                }

                                /*     4-byte big endian number of sample frames */
                                NumSampleFrames = File.ReadInt32BigEndian();
                                NumSampleFramesIsValid = true;

                                /*     2-byte big endian number of bits per sample */
                                /*        a value in the domain 1..32 */
                                ShortInteger = File.ReadInt16BigEndian();
                                switch (ShortInteger)
                                {
                                    default:
                                        throw new FormatException(ErrorUnsupportedVariant);
                                    case 1:
                                    case 2:
                                    case 3:
                                    case 4:
                                    case 5:
                                    case 6:
                                    case 7:
                                    case 8:
                                        NumBits = NumBitsType.eSample8bit;
                                        NumBitsIsValid = true;
                                        break;
                                    case 9:
                                    case 10:
                                    case 11:
                                    case 12:
                                    case 13:
                                    case 14:
                                    case 15:
                                    case 16:
                                        NumBits = NumBitsType.eSample16bit;
                                        NumBitsIsValid = true;
                                        break;
                                    case 17:
                                    case 18:
                                    case 19:
                                    case 20:
                                    case 21:
                                    case 22:
                                    case 23:
                                    case 24:
                                        NumBits = NumBitsType.eSample24bit;
                                        NumBitsIsValid = true;
                                        break;
                                }

                                /*     10-byte extended precision number of frames per second */
                                File.ReadRaw(StupidExtendedThang, 0, 10);

                                /*     4-byte character code ID for the compression method */
                                /*        "NONE" means there is no compression method used */
                                CharBuff = File.ReadFixedStringASCII(4);
                                if (!String.Equals(CharBuff, "NONE"))
                                {
                                    throw new FormatException(ErrorUnsupportedVariant);
                                }

                                /*     some characters in a string identifying the compression method */
                                /*        this must be padded to an even number of bytes, but the pad is */
                                /*        NOT included in the length descriptor for the chunk. */
                                /*        for uncompressed data, the string should be */
                                /*        "\x0enot compressed\x00", including the null, for 16 bytes. */
                                /*        the total chunk length is thus 38 bytes. */
                                for (int Dumper = 0; Dumper < LocalChunkLength - 22; Dumper += 1)
                                {
                                    File.ReadByte();
                                }
                            }

                            /* extended 22050 = 400D AC44000000000000 */
                            /* extended 22051 = 400D AC46000000000000 */
                            /* extended 44100 = 400E AC44000000000000 */
                            /* extended 44101 = 400E AC45000000000000 */
                            Exponent = (((uint)StupidExtendedThang[0] & 0xff) << 8)
                                | ((uint)StupidExtendedThang[1] & 0xff);
                            Mantissa = (((uint)StupidExtendedThang[2] & 0xff) << 24)
                                | (((uint)StupidExtendedThang[3] & 0xff) << 16)
                                | (((uint)StupidExtendedThang[4] & 0xff) << 8)
                                | ((uint)StupidExtendedThang[5] & 0xff);
                            SamplingRate = (int)(Mantissa >> (0x401e - (int)Exponent));
                            if (SamplingRate < Constants.MINSAMPLINGRATE)
                            {
                                SamplingRate = Constants.MINSAMPLINGRATE;
                            }
                            if (SamplingRate > Constants.MAXSAMPLINGRATE)
                            {
                                SamplingRate = Constants.MAXSAMPLINGRATE;
                            }
                            SamplingRateIsValid = true;
                        }
                        else if (String.Equals(CharBuff, "SSND"))
                        {
                            int AlignmentFactor;
                            int OffsetToFirstByte;

                            /*   Sound Data Chunk */
                            /*     "SSND" */
                            /*     4-byte big endian number of bytes in sample data array */

                            /* only one of these is allowed */
                            if (RawDataFromFileIsValid)
                            {
                                throw new FormatException(ErrorUnsupportedVariant);
                            }

                            /*     4-byte big endian offset to the first byte of sample data in the array */
                            OffsetToFirstByte = File.ReadInt32BigEndian();
                            if (OffsetToFirstByte != 0)
                            {
                                throw new FormatException(ErrorUnsupportedVariant);
                            }

                            /*     4-byte big endian number of bytes to which the sound data is aligned. */
                            AlignmentFactor = File.ReadInt32BigEndian();
                            if (AlignmentFactor != 0)
                            {
                                throw new FormatException(ErrorUnsupportedVariant);
                            }

                            /*     any length vector of raw sound data. */
                            /*        this must be padded to an even number of bytes, but the pad is */
                            /*        NOT included in the length descriptor for the chunk. */
                            /*        Samples are stored in an integral number of bytes, the smallest that */
                            /*        is required for the specified number of bits.  If this is not an even */
                            /*        multiple of 8, then the data is shifted left and the low bits are zeroed */
                            /*        Multichannel sound is interleaved with the left channel first. */
                            if (stream.Length - stream.Position < LocalChunkLength - 8)
                            {
                                throw new FormatException(ErrorTooShort);
                            }
                            RawDataFromFileIsValid = true;
                            audioDataOffset = stream.Position;

                            stream.Seek(LocalChunkLength - 8, SeekOrigin.Current);
                        }
                        else
                        {
                            /* just read the data & get rid of it */
                            while (LocalChunkLength > 0)
                            {
                                File.ReadByte();
                                LocalChunkLength -= 1;
                            }
                        }
                    }
                }
            }
            catch (InvalidDataException exception)
            {
                throw new FormatException(exception.Message);
            }

            if (!RawDataFromFileIsValid || !NumBitsIsValid || !NumChannelsIsValid || !SamplingRateIsValid || !NumSampleFramesIsValid)
            {
                throw new FormatException(ErrorUnsupportedVariant);
            }

            NumBitsOut = NumBits;
            NumChannelsOut = NumChannels;
            NumSampleFramesOut = NumSampleFrames;
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
