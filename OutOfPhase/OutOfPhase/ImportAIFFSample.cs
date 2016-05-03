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
using System.IO;
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
            index = -1;

            string path;
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
                try
                {
                    using (AudioFileReader reader = new AIFFReader(input, true/*allowTruncated*/))
                    {
                        NumBitsType numBits = reader.NumBits;
                        NumChannelsType numChannels = reader.NumChannels;
                        int numFrames = reader.NumFrames;
                        int samplingRate = reader.SamplingRate;

                        float[] sampleData;
                        try
                        {
                            sampleData = new float[(numFrames + 1) * reader.PointsPerFrame];
                        }
                        catch (OutOfMemoryException)
                        {
                            throw new AudioFileReaderException(AudioFileReaderErrors.OutOfMemory);
                        }

                        reader.ReadPoints(sampleData, 0, numFrames * reader.PointsPerFrame);

                        if (reader.Truncated)
                        {
                            const string ErrorTooShort = "The file appears to be incomplete. Appending silence to end of sample data.";
                            MessageBox.Show(ErrorTooShort, "Import Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        SampleObjectRec ReturnedSampleObject = new SampleObjectRec(
                            mainWindow.Document,
                            sampleData,
                            numFrames,
                            numBits,
                            numChannels,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            samplingRate,
                            Constants.MIDDLEC);
                        ReturnedSampleObject.Name = Path.GetFileName(path);

                        index = mainWindow.Document.SampleList.Count;
                        mainWindow.Document.SampleList.Add(ReturnedSampleObject);

                        new SampleWindow(registration, ReturnedSampleObject, mainWindow).Show();

                        return true;
                    }
                }
                catch (AudioFileReaderException exception)
                {
                    MessageBox.Show(AudioFileReaderException.MessageFromError(exception.Error), "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            return false;
        }
    }
}
