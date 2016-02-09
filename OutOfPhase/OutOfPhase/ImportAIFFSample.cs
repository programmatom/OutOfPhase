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
using System.Windows.Forms;

namespace OutOfPhase
{
    public static partial class Import
    {
        /* this routine asks for a file and tries to import the contents of that */
        /* file as an AIFF sample.  it reports any errors to the user. */
        public static bool ImportAIFFSample(
            Registration registration,
            MainWindow mainWindow,
            out int index)
        {
            string path;

            index = -1;

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Import AIFF Sample";
                dialog.Filter = "AIFF Audio File (.aif, .aiff)|*.aif;*.aiff|Any File Type (*)|*";
                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return false;
                }
                path = dialog.FileName;
            }

            using (Stream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
            {
                using (BinaryReader reader = new BinaryReader(input))
                {
                    NumBitsType NumBits;
                    NumChannelsType NumChannels;
                    float[] SampleData;
                    int NumFrames;
                    int SamplingRate;

                    if (TryToImportAIFFFile(
                        reader,
                        out NumBits,
                        out NumChannels,
                        out SampleData,
                        out NumFrames,
                        out SamplingRate))
                    {
                        SampleObjectRec ReturnedSampleObject = new SampleObjectRec(
                            mainWindow.Document,
                            SampleData,
                            NumFrames,
                            NumBits,
                            NumChannels,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            SamplingRate,
                            Constants.MIDDLEC);
                        ReturnedSampleObject.Name = Path.GetFileName(path);

                        index = mainWindow.Document.SampleList.Count;
                        mainWindow.Document.SampleList.Add(ReturnedSampleObject);

                        new SampleWindow(registration, ReturnedSampleObject, mainWindow).Show();

                        return true;
                    }
                }
            }

            return false;
        }

        // TODO: switch to AIFFReader, except that the latter doesn't support zero-filling truncated files

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
        public static bool TryToImportAIFFFile(
            BinaryReader File,
            out NumBitsType NumBitsOut,
            out NumChannelsType NumChannelsOut,
            out float[] RawDataPtrOut,
            out int NumSampleFramesOut,
            out int SamplingRateOut)
        {
            const string ErrorTitle = "Import Error";
            const string ErrorUnrecognized = "The file is not an AIFF or AIFF-C file.";
            const string ErrorUnsupportedVariant = "The file is not a supported variant of AIFF or AIFF-C.";
            const string ErrorUnsupportedVariantOrMalformed = "The file is not a supported variant of AIFF or AIFF-C, or the file is corrupted.";
            const string ErrorTooShort = "The file appears to be incomplete. Appending silence to end of sample data.";
            const string ErrorOutOfMemory = "There is not enough memory available to import the audio data.";

            string CharBuff;
            bool IsAnAIFFCFile;
            int FormChunkLength;
            byte[] RawDataFromFile = null;
            bool RawDataFromFileIsValid = false;
            NumBitsType NumBits = (NumBitsType)(-1);
            bool NumBitsIsValid = false;
            NumChannelsType NumChannels = (NumChannelsType)(-1);
            bool NumChannelsIsValid = false;
            int SamplingRate = -1;
            bool SamplingRateIsValid = false;
            int NumSampleFrames = -1;
            bool NumSampleFramesIsValid = false;
            float[] SampleVector;
            int NumSamplePoints;
            int ActualPoints;

            NumBitsOut = (NumBitsType)0;
            NumChannelsOut = (NumChannelsType)0;
            RawDataPtrOut = null;
            NumSampleFramesOut = 0;
            SamplingRateOut = 0;

            try
            {
                /*     "FORM" */
                CharBuff = File.ReadFixedStringASCII(4);
                if (!String.Equals(CharBuff, "FORM"))
                {
                    MessageBox.Show(ErrorUnrecognized, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return false;
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
                    MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return false;
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
                                MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                return false;
                            }

                            /*     2-byte big endian number of channels */
                            ShortInteger = File.ReadInt16BigEndian();
                            switch (ShortInteger)
                            {
                                default:
                                    MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                    return false;
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
                                    MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                    return false;
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
                                MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                return false;
                            }

                            /*     2-byte big endian number of channels */
                            ShortInteger = File.ReadInt16BigEndian();
                            switch (ShortInteger)
                            {
                                default:
                                    MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                    return false;
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
                                    MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                    return false;
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
                                MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                return false;
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
                            MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            return false;
                        }

                        /*     4-byte big endian offset to the first byte of sample data in the array */
                        OffsetToFirstByte = File.ReadInt32BigEndian();
                        if (OffsetToFirstByte != 0)
                        {
                            MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            return false;
                        }

                        /*     4-byte big endian number of bytes to which the sound data is aligned. */
                        AlignmentFactor = File.ReadInt32BigEndian();
                        if (AlignmentFactor != 0)
                        {
                            MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            return false;
                        }

                        /*     any length vector of raw sound data. */
                        /*        this must be padded to an even number of bytes, but the pad is */
                        /*        NOT included in the length descriptor for the chunk. */
                        /*        Samples are stored in an integral number of bytes, the smallest that */
                        /*        is required for the specified number of bits.  If this is not an even */
                        /*        multiple of 8, then the data is shifted left and the low bits are zeroed */
                        /*        Multichannel sound is interleaved with the left channel first. */
                        RawDataFromFile = new byte[LocalChunkLength - 8];
                        RawDataFromFileIsValid = true;
                        try
                        {
                            File.ReadRaw(RawDataFromFile, 0, LocalChunkLength - 8);
                        }
                        catch (InvalidDataException)
                        {
                            MessageBox.Show(ErrorTooShort, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
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
            catch (InvalidDataException)
            {
                MessageBox.Show(ErrorUnsupportedVariantOrMalformed, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show(ErrorOutOfMemory, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            /* now, deal with the stuff we just got */
            if (!RawDataFromFileIsValid || !NumBitsIsValid || !NumChannelsIsValid || !SamplingRateIsValid || !NumSampleFramesIsValid)
            {
                MessageBox.Show(ErrorUnsupportedVariant, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            NumSamplePoints = NumSampleFrames;
            int pointsPerFrame = (NumChannels == NumChannelsType.eSampleStereo) ? 2 : 1;
            NumSamplePoints *= pointsPerFrame;
            try
            {
                SampleVector = new float[NumSamplePoints + pointsPerFrame]; // must allocate extra frame for
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show(ErrorOutOfMemory, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            using (Stream dataStream = new MemoryStream(RawDataFromFile))
            {
                using (BinaryReader dataReader = new BinaryReader(dataStream))
                {
                    switch (NumBits)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case NumBitsType.eSample8bit:
                            ActualPoints = RawDataFromFile.Length / 1;
                            if (ActualPoints > NumSamplePoints)
                            {
                                ActualPoints = NumSamplePoints;
                            }
                            for (int i = 0; i < ActualPoints; i++)
                            {
                                SampleVector[i] = SampConv.SignedByteToFloat(dataReader.ReadByte());
                            }
                            break;
                        case NumBitsType.eSample16bit:
                            ActualPoints = RawDataFromFile.Length / 2;
                            if (ActualPoints > NumSamplePoints)
                            {
                                ActualPoints = NumSamplePoints;
                            }
                            for (int i = 0; i < ActualPoints; i++)
                            {
                                SampleVector[i] = SampConv.SignedShortToFloat(dataReader.ReadInt16BigEndian());
                            }
                            break;
                        case NumBitsType.eSample24bit:
                            ActualPoints = RawDataFromFile.Length / 3;
                            if (ActualPoints > NumSamplePoints)
                            {
                                ActualPoints = NumSamplePoints;
                            }
                            for (int i = 0; i < ActualPoints; i++)
                            {
                                SampleVector[i] = SampConv.SignedTribyteToFloat(dataReader.ReadInt24BigEndian());
                            }
                            break;
                    }
                }
            }

            RawDataPtrOut = SampleVector;

            NumBitsOut = NumBits;
            NumChannelsOut = NumChannels;
            NumSampleFramesOut = NumSampleFrames;
            SamplingRateOut = SamplingRate;

            return true;
        }
    }
}
