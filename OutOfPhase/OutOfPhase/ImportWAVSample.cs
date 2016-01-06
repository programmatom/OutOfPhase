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
        private const string ErrorTitle = "Import Error";

        public enum WAVImportErrors
        {
            eWAVImportNoError,
            eWAVImportUnrecognizedFileFormat,
            eWAVImportBadNumberOfChannels,
            eWAVImportNotAPCMFile,
            eWAVImportBadNumberOfBits,
            eWAVImportDiskError,
            eWAVImportOutOfMemory,
        }

        /* this routine asks for a file and tries to import the contents of that */
        /* file as a WAV sample.  it reports any errors to the user. */
        public static bool ImportWAVSample(
            Registration registration,
            MainWindow mainWindow,
            out int index)
        {
            string path;

            index = -1;

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Import WAV Sample";
                dialog.Filter = "WAV Audio File (.wav)|*.wav|Any File Type (*)|*";
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

                    WAVImportErrors Error = TryToImportWAVFile(
                        reader,
                        out NumBits,
                        out NumChannels,
                        out SampleData,
                        out NumFrames,
                        out SamplingRate);
                    if (Error == WAVImportErrors.eWAVImportNoError)
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
                    else
                    {
                        MessageBox.Show(GetWAVImportErrorString(Error), ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return false;
                    }
                }
            }
        }

        // TODO: switch to WAVReader, except that the latter doesn't support zero-filling truncated files

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
        public static WAVImportErrors TryToImportWAVFile(
            BinaryReader File,
            out NumBitsType NumBitsOut,
            out NumChannelsType NumChannelsOut,
            out float[] RawDataPtrOut,
            out int NumSampleFramesOut,
            out int SamplingRateOut)
        {
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

            NumBitsOut = (NumBitsType)0;
            NumChannelsOut = (NumChannelsType)0;
            RawDataPtrOut = null;
            NumSampleFramesOut = 0;
            SamplingRateOut = 0;

            try
            {
                /*  'RIFF' */
                StringBuffer = File.ReadFixedStringASCII(4);
                if (!String.Equals(StringBuffer, "RIFF"))
                {
                    return WAVImportErrors.eWAVImportUnrecognizedFileFormat;
                }

                /*  4-byte little endian length descriptor (minus 8 bytes for these 2 fields) */
                TotalFileLength = File.ReadInt32();

                /*  'WAVE' */
                StringBuffer = File.ReadFixedStringASCII(4);
                if (!String.Equals(StringBuffer, "WAVE"))
                {
                    return WAVImportErrors.eWAVImportUnrecognizedFileFormat;
                }

                /*  'fmt ' */
                StringBuffer = File.ReadFixedStringASCII(4);
                if (!String.Equals(StringBuffer, "fmt "))
                {
                    return WAVImportErrors.eWAVImportUnrecognizedFileFormat;
                }

                /*  4-byte little endian length descriptor for the 'fmt ' header block */
                /*      - this should be 16.  if not, then it's some other kind of WAV file */
                HeaderLength = File.ReadInt32();
                if (HeaderLength != 16)
                {
                    return WAVImportErrors.eWAVImportUnrecognizedFileFormat;
                }

                /*  2-byte little endian format descriptor.  this is always here. */
                /*      - 1 = PCM */
                DataTypeDescriptor = File.ReadInt16();
                if (DataTypeDescriptor != 1)
                {
                    return WAVImportErrors.eWAVImportNotAPCMFile;
                }

                /*  2-byte little endian number of channels. */
                NumberOfChannels = File.ReadInt16();
                if ((NumberOfChannels != 1) && (NumberOfChannels != 2))
                {
                    return WAVImportErrors.eWAVImportBadNumberOfChannels;
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
                        return WAVImportErrors.eWAVImportBadNumberOfBits;
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
                    return WAVImportErrors.eWAVImportUnrecognizedFileFormat;
                }

                /*  4-byte little endian length of sample data descriptor */
                SampledNumberOfBytes = File.ReadInt32();

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
                try
                {
                    float[] Buffer;
                    switch (NumBits)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case NumBitsType.eSample8bit:
                            switch (NumChannels)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case NumChannelsType.eSampleMono:
                                    Buffer = RawDataPtrOut = new float[NumberOfSampleFrames + 1];
                                    for (int Scan = 0; Scan < NumberOfSampleFrames; Scan += 1)
                                    {
                                        Buffer[Scan] = SampConv.UnsignedByteToFloat(File.ReadByte());
                                    }
                                    break;

                                case NumChannelsType.eSampleStereo:
                                    Buffer = RawDataPtrOut = new float[(NumberOfSampleFrames + 1) * 2];
                                    for (int Scan = 0; Scan < NumberOfSampleFrames * 2; Scan += 1)
                                    {
                                        /* alternates frames */
                                        Buffer[Scan] = SampConv.UnsignedByteToFloat(File.ReadByte());
                                    }
                                    break;
                            }
                            break;

                        case NumBitsType.eSample16bit:
                            switch (NumChannels)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case NumChannelsType.eSampleMono:
                                    Buffer = RawDataPtrOut = new float[NumberOfSampleFrames + 1];
                                    for (int Scan = 0; Scan < NumberOfSampleFrames; Scan += 1)
                                    {
                                        Buffer[Scan] = SampConv.SignedShortToFloat(File.ReadInt16());
                                    }
                                    break;

                                case NumChannelsType.eSampleStereo:
                                    Buffer = RawDataPtrOut = new float[(NumberOfSampleFrames + 1) * 2];
                                    for (int Scan = 0; Scan < NumberOfSampleFrames * 2; Scan += 1)
                                    {
                                        /* alternates frames */
                                        Buffer[Scan] = SampConv.SignedShortToFloat(File.ReadInt16());
                                    }
                                    break;
                            }
                            break;

                        case NumBitsType.eSample24bit:
                            switch (NumChannels)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case NumChannelsType.eSampleMono:
                                    Buffer = RawDataPtrOut = new float[NumberOfSampleFrames + 1];
                                    for (int Scan = 0; Scan < NumberOfSampleFrames; Scan += 1)
                                    {
                                        Buffer[Scan] = SampConv.SignedTribyteToFloat(File.ReadInt24());
                                    }
                                    break;

                                case NumChannelsType.eSampleStereo:
                                    Buffer = RawDataPtrOut = new float[(NumberOfSampleFrames + 1) * 2];
                                    for (int Scan = 0; Scan < NumberOfSampleFrames * 2; Scan += 1)
                                    {
                                        /* alternates frames */
                                        Buffer[Scan] = SampConv.SignedTribyteToFloat(File.ReadInt24());
                                    }
                                    break;
                            }
                            break;
                    }
                }
                catch (InvalidDataException)
                {
                    const string ErrorTooShort = "The file appears to be incomplete.  Appending silence to end of sample data.";
                    MessageBox.Show(ErrorTooShort, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (InvalidDataException)
            {
                return WAVImportErrors.eWAVImportDiskError;
            }
            catch (OutOfMemoryException)
            {
                return WAVImportErrors.eWAVImportOutOfMemory;
            }

            /* output parameters */
            NumBitsOut = NumBits;
            NumChannelsOut = NumChannels;
            //RawDataPtrOut assigned earlier
            NumSampleFramesOut = NumberOfSampleFrames;
            SamplingRateOut = SamplingRate;

            return WAVImportErrors.eWAVImportNoError;
        }


        /* get constant null terminated error string */
        public static string GetWAVImportErrorString(WAVImportErrors Error)
        {
            switch (Error)
            {
                case WAVImportErrors.eWAVImportNoError:
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case WAVImportErrors.eWAVImportUnrecognizedFileFormat:
                    return "The file does not appear to be a WAV file.  Try importing it as a RAW file.";
                case WAVImportErrors.eWAVImportBadNumberOfChannels:
                    return "Only files with 1 or 2 channels can be imported.";
                case WAVImportErrors.eWAVImportNotAPCMFile:
                    return "The file is not a PCM file.";
                case WAVImportErrors.eWAVImportBadNumberOfBits:
                    return "Only 8, 16, or 24 bit files can be imported.";
                case WAVImportErrors.eWAVImportDiskError:
                    return "Unable to read data from the file, or the file is corrupt.";
                case WAVImportErrors.eWAVImportOutOfMemory:
                    return "There is not enough memory available to import the sample.";
            }
        }
    }
}
