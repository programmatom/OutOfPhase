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
    public class AIFFWriter : AudioFileWriter, IDisposable
    {
        private Stream outputStream;
        private readonly long start;

        private readonly NumChannelsType channels;
        private readonly NumBitsType bits;
        private readonly int samplingRate;
        private readonly int pointsPerFrame;
        private readonly int bytesPerPoint;
        private readonly float expander;

        private int totalPoints;

        private byte[] output = new byte[0];

        private const float Expander_8 = 0x7f;
        private const float Expander_16 = 0x7fff;
        private const float Expander_24 = 0x7fffff;

        public AIFFWriter(
            Stream outputStream,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate)
        {
            this.outputStream = outputStream;
            this.start = outputStream.Position;

            this.channels = channels;
            this.bits = bits;
            this.samplingRate = samplingRate;

            switch (bits)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case NumBitsType.eSample8bit:
                    expander = Expander_8;
                    bytesPerPoint = 1;
                    break;
                case NumBitsType.eSample16bit:
                    expander = Expander_16;
                    bytesPerPoint = 2;
                    break;
                case NumBitsType.eSample24bit:
                    expander = Expander_24;
                    bytesPerPoint = 3;
                    break;
            }

            switch (channels)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case NumChannelsType.eSampleMono:
                    pointsPerFrame = 1;
                    break;
                case NumChannelsType.eSampleStereo:
                    pointsPerFrame = 2;
                    break;
            }

            SetUpAIFFHeader(
                outputStream,
                channels,
                bits,
                samplingRate);
        }

        // stereo data is interleaved
        public override void WritePoints(float[] data, int offset, int count)
        {
#if !DEBUG
            int blockCount = Constants.BufferSize / bytesPerPoint;
#else
            int blockCount = 9; // ensure multi-iteration mode gets tested in debug
#endif

#if DEBUG
            if (count % pointsPerFrame != 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            while (count > 0)
            {
                int count1 = Math.Min(blockCount, count);

                int bytes;
                switch (bits)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case NumBitsType.eSample8bit:
                        bytes = count1;
                        if (output.Length < bytes)
                        {
                            output = new byte[bytes];
                        }
                        for (int i = 0; i < count1; i++)
                        {
                            float value = data[offset + i];
                            int TempVal = (int)(Math.Sign(value) * (.5f + Math.Abs(value) * expander));
                            output[i] = (byte)TempVal;
                        }
                        break;

                    case NumBitsType.eSample16bit:
                        bytes = count1 * 2;
                        if (output.Length < bytes)
                        {
                            output = new byte[bytes];
                        }
                        for (int i = 0; i < count1; i++)
                        {
                            float value = data[offset + i];
                            int TempVal = (int)(Math.Sign(value) * (.5f + Math.Abs(value) * expander));
                            output[2 * i + 0] = (byte)((TempVal >> 8) & 0xff);
                            output[2 * i + 1] = (byte)(TempVal & 0xff);
                        }
                        break;

                    case NumBitsType.eSample24bit:
                        bytes = count1 * 3;
                        if (output.Length < bytes)
                        {
                            output = new byte[bytes];
                        }
                        for (int i = 0; i < count1; i++)
                        {
                            float value = data[offset + i];
                            int TempVal = (int)(Math.Sign(value) * (.5f + Math.Abs(value) * expander));
                            output[3 * i + 0] = (byte)((TempVal >> 16) & 0xff);
                            output[3 * i + 1] = (byte)((TempVal >> 8) & 0xff);
                            output[3 * i + 2] = (byte)(TempVal & 0xff);
                        }
                        break;
                }

                outputStream.Write(output, 0, bytes);

                totalPoints += count1;
                offset += count1;
                count -= count1;
            }
        }

        public override void Close()
        {
            ResolveAIFFHeader(
                outputStream,
                start,
                totalPoints / pointsPerFrame,
                channels,
                bits);
            outputStream = null;
        }

        public override void Dispose()
        {
            Close();
        }

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
        private static void SetUpAIFFHeader(
            Stream outputStream,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate)
        {
            using (BinaryWriter writer = new BinaryWriter(outputStream))
            {
                /* 0..3 */
                /*     "FORM" */
                writer.WriteFixedStringASCII(4, "FORM");

                /* 4..7 */
                /*     4-byte big endian form chunk length descriptor (minus 8 for "FORM" & this) */
                writer.WriteInt32BigEndian(0); /* RESOLVED LATER */

                /* 8..11 */
                /*     4-byte type */
                /*        "AIFC" = AIFF-C format file */
                writer.WriteFixedStringASCII(4, "AIFF");

                /* 12..15 */
                /*     "COMM" */
                writer.WriteFixedStringASCII(4, "COMM");

                /* 16..19 */
                /*     4-byte big endian length. */
                /*        always 18 for AIFF files */
                writer.WriteInt32BigEndian(18);

                /* 20..21 */
                /*     2-byte big endian number of channels */
                writer.WriteInt16BigEndian(channels == NumChannelsType.eSampleStereo ? (short)2 : (short)1);

                /* 22..25 */
                /*     4-byte big endian number of sample frames */
                writer.WriteInt32BigEndian(0); /* RESOLVED LATER */

                /* 26..27 */
                /*     2-byte big endian number of bits per sample */
                /*        a value in the domain 1..32 */
                switch (bits)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NumBitsType.eSample8bit:
                        writer.WriteInt16BigEndian(8);
                        break;
                    case NumBitsType.eSample16bit:
                        writer.WriteInt16BigEndian(16);
                        break;
                    case NumBitsType.eSample24bit:
                        writer.WriteInt16BigEndian(24);
                        break;
                }

                /* 28..37 */
                /*     10-byte extended precision number of frames per second */
                /* extended 22050 = 400D AC44000000000000 */
                /* extended 22051 = 400D AC46000000000000 */
                /* extended 44100 = 400E AC44000000000000 */
                /* extended 44101 = 400E AC45000000000000 */
                uint exponent = 0x401e;
                uint mantissa = (uint)samplingRate;
                while ((mantissa & 0x80000000) == 0)
                {
                    mantissa = mantissa << 1;
                    exponent -= 1;
                }
                byte[] extended = new byte[10];
                extended[0] = (byte)((exponent >> 8) & 0xff);
                extended[1] = (byte)(exponent & 0xff);
                extended[2] = (byte)((mantissa >> 24) & 0xff);
                extended[3] = (byte)((mantissa >> 16) & 0xff);
                extended[4] = (byte)((mantissa >> 8) & 0xff);
                extended[5] = (byte)(mantissa & 0xff);
                extended[6] = 0;
                extended[7] = 0;
                extended[8] = 0;
                extended[9] = 0;
                writer.WriteRaw(extended, 0, extended.Length);

                /* 38..41 */
                /*     "SSND" */
                writer.WriteFixedStringASCII(4, "SSND");

                /* 42..45 */
                /*     4-byte big endian number of bytes in sample data array */
                writer.WriteInt32BigEndian(0); /* RESOLVED LATER */

                /* 46..49 */
                /*     4-byte big endian offset to the first byte of sample data in the array */
                writer.WriteInt32BigEndian(0);

                /* 50..53 */
                /*     4-byte big endian number of bytes to which the sound data is aligned. */
                writer.WriteInt32BigEndian(0);

                /*     any length vector of raw sound data. */
                /*        this must be padded to an even number of bytes, but the pad is */
                /*        NOT included in the length descriptor for the chunk. */
                /*        Samples are stored in an integral number of bytes, the smallest that */
                /*        is required for the specified number of bits.  If this is not an even */
                /*        multiple of 8, then the data is shifted left and the low bits are zeroed */
                /*        Multichannel sound is interleaved with the left channel first. */
            }
        }

        /* update various size fields in the file */
        private static void ResolveAIFFHeader(
            Stream output,
            long start,
            int totalFrameCount,
            NumChannelsType channels,
            NumBitsType bits)
        {
            int totalBytesOfSamples;
            byte[] buffer = new byte[4];

            totalBytesOfSamples = totalFrameCount;
            if (channels == NumChannelsType.eSampleStereo)
            {
                totalBytesOfSamples *= 2;
            }
            switch (bits)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case NumBitsType.eSample8bit:
                    break;
                case NumBitsType.eSample16bit:
                    totalBytesOfSamples *= 2;
                    break;
                case NumBitsType.eSample24bit:
                    totalBytesOfSamples *= 3;
                    break;
            }

            /* make sure file is an even number of bytes */
            if (((output.Position - start) & 1) != 0)
            {
                output.WriteByte(0);
            }

            /* chop off any crud from the end */
            output.SetLength(output.Position);

            /* 4..7 */
            /*     4-byte big endian form chunk length descriptor (minus 8 for "FORM" & this) */
            output.Position = start + 4;
            buffer[0] = (byte)(((totalBytesOfSamples + 54 - 8) >> 24) & 0xff);
            buffer[1] = (byte)(((totalBytesOfSamples + 54 - 8) >> 16) & 0xff);
            buffer[2] = (byte)(((totalBytesOfSamples + 54 - 8) >> 8) & 0xff);
            buffer[3] = (byte)((totalBytesOfSamples + 54 - 8) & 0xff);
            output.Write(buffer, 0, 4);

            /* 22..25 */
            /*     4-byte big endian number of sample frames */
            output.Position = start + 22;
            buffer[0] = (byte)((totalFrameCount >> 24) & 0xff);
            buffer[1] = (byte)((totalFrameCount >> 16) & 0xff);
            buffer[2] = (byte)((totalFrameCount >> 8) & 0xff);
            buffer[3] = (byte)(totalFrameCount & 0xff);
            output.Write(buffer, 0, 4);

            /* 42..45 */
            /*     4-byte big endian number of bytes in sample data array */
            output.Position = start + 42;
            buffer[0] = (byte)(((totalBytesOfSamples + 8) >> 24) & 0xff);
            buffer[1] = (byte)(((totalBytesOfSamples + 8) >> 16) & 0xff);
            buffer[2] = (byte)(((totalBytesOfSamples + 8) >> 8) & 0xff);
            buffer[3] = (byte)((totalBytesOfSamples + 8) & 0xff);
            output.Write(buffer, 0, 4);

            output.Position = output.Length;
        }
    }
}
