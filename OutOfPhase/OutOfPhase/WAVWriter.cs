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
    public class WAVWriter : AudioFileWriter, IDisposable
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

        public WAVWriter(
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

            SetUpWAVHeader(
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
                            // for WAV 8-bit ONLY, use bias representation (0..255)
                            TempVal = unchecked((int)((byte)(TempVal + 0x80)));
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
                            output[2 * i + 0] = (byte)(TempVal & 0xff);
                            output[2 * i + 1] = (byte)((TempVal >> 8) & 0xff);
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
                            output[3 * i + 0] = (byte)(TempVal & 0xff);
                            output[3 * i + 1] = (byte)((TempVal >> 8) & 0xff);
                            output[3 * i + 2] = (byte)((TempVal >> 16) & 0xff);
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
            ResolveWAVHeader(
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

        /* RIFF file format, with WAVE information */
        /*  'RIFF' */
        /*  4-byte little endian length descriptor (minus 8 bytes for these 2 fields) */
        /*  'WAVE' */
        /*  'fmt ' */
        /*  4-byte little endian length descriptor for the 'fmt ' header block */
        /*      - this should be 16.  if not, then it's some other kind of WAV file */
        /*  2-byte little endian format descriptor.  this is always here. */
        /*      - 1 = PCM */
        /*    for future use: */
        /*      - 3 = IEEE floats */
        /*    and just in case: */
        /*      - 6 = 8-bit ITU-T G.711 A-law */
        /*      - 7 = 8-bit ITU-T G.711 mu-law */
        /*      - 0xFFFE = extensible (requires SubFormat field) */
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
        private static void SetUpWAVHeader(
            Stream outputStream,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate)
        {
            using (BinaryWriter writer = new BinaryWriter(outputStream))
            {
                int bytesPerSecond;
                short blockAlignment;

                /*  'RIFF' */
                writer.WriteFixedStringASCII(4, "RIFF");

                /* figure out how long chunk will be */
                switch (bits)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NumBitsType.eSample8bit:
                        switch (channels)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case NumChannelsType.eSampleMono:
                                bytesPerSecond = samplingRate * 1;
                                blockAlignment = 1;
                                break;
                            case NumChannelsType.eSampleStereo:
                                bytesPerSecond = samplingRate * 2;
                                blockAlignment = 2;
                                break;
                        }
                        break;
                    case NumBitsType.eSample16bit:
                        switch (channels)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case NumChannelsType.eSampleMono:
                                bytesPerSecond = samplingRate * 2;
                                blockAlignment = 2;
                                break;
                            case NumChannelsType.eSampleStereo:
                                bytesPerSecond = samplingRate * 4;
                                blockAlignment = 4;
                                break;
                        }
                        break;
                    case NumBitsType.eSample24bit:
                        switch (channels)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case NumChannelsType.eSampleMono:
                                bytesPerSecond = samplingRate * 3;
                                blockAlignment = 3;
                                break;
                            case NumChannelsType.eSampleStereo:
                                bytesPerSecond = samplingRate * 6;
                                blockAlignment = 6;
                                break;
                        }
                        break;
                }

                /*  4-byte little endian length descriptor (minus 8 bytes for these 2 fields) */
                writer.WriteInt32(0); // placeholder - corrected later

                /*  'WAVE' */
                writer.WriteFixedStringASCII(4, "WAVE");
                /*  'fmt ' */
                writer.WriteFixedStringASCII(4, "fmt ");

                /*  4-byte little endian length descriptor for the 'fmt ' header block */
                /*      - this should be 16.  if not, then it's some other kind of WAV file */
                writer.WriteInt32(16);

                /*  2-byte little endian format descriptor.  this is always here. */
                /*      - 1 = PCM */
                writer.WriteInt16(1);

                /*  2-byte little endian number of channels. */
                switch (channels)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NumChannelsType.eSampleMono:
                        writer.WriteInt16(1);
                        break;
                    case NumChannelsType.eSampleStereo:
                        writer.WriteInt16(2);
                        break;
                }

                /*  4-byte little endian sampling rate integer. */
                writer.WriteInt32(samplingRate);

                /*  4-byte little endian average bytes per second. */
                writer.WriteInt32(bytesPerSecond);

                /*  2-byte little endian block align.  for 8-bit mono, this is 1; for 16-bit */
                /*    stereo, this is 4. */
                writer.WriteInt16(blockAlignment);

                /*  2-byte little endian number of bits. */
                /*      - 8 = 8-bit */
                /*      - 16 = 16-bit */
                /*      - 24 = 24-bit */
                switch (bits)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NumBitsType.eSample8bit:
                        writer.WriteInt16(8);
                        break;
                    case NumBitsType.eSample16bit:
                        writer.WriteInt16(16);
                        break;
                    case NumBitsType.eSample24bit:
                        writer.WriteInt16(24);
                        break;
                }

                /*  'data' */
                writer.WriteFixedStringASCII(4, "data");

                /*  4-byte little endian length of sample data descriptor */
                writer.WriteInt32(0); // placeholder - corrected later

                /*  any length data.  8-bit data goes from 0..255, but 16-bit data goes */
                /*    from -32768 to 32767. */
            }
        }

        /* update various size fields in the file */
        private static void ResolveWAVHeader(
            Stream output,
            long start,
            int totalFrameCount,
            NumChannelsType channels,
            NumBitsType bits)
        {
            byte[] buffer = new byte[4];

            int totalBytesOfSamples = totalFrameCount;
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
            /*  4-byte little endian length descriptor (minus 8 bytes for these 2 fields) */
            output.Position = start + 4;
            buffer[0] = (byte)((totalBytesOfSamples + 36) & 0xff);
            buffer[1] = (byte)(((totalBytesOfSamples + 36) >> 8) & 0xff);
            buffer[2] = (byte)(((totalBytesOfSamples + 36) >> 16) & 0xff);
            buffer[3] = (byte)(((totalBytesOfSamples + 36) >> 24) & 0xff);
            output.Write(buffer, 0, 4);

            /* 40..43 */
            /*  4-byte little endian length of sample data descriptor */
            output.Position = start + 40;
            buffer[0] = (byte)(totalBytesOfSamples & 0xff);
            buffer[1] = (byte)((totalBytesOfSamples >> 8) & 0xff);
            buffer[2] = (byte)((totalBytesOfSamples >> 16) & 0xff);
            buffer[3] = (byte)((totalBytesOfSamples >> 24) & 0xff);
            output.Write(buffer, 0, 4);

            output.Position = output.Length;
        }
    }
}
