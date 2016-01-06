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
    public static partial class Export
    {
        /* this routine saves the data in the provided sample storage object as a WAVE */
        /* formatted file.  it handles any error reporting to the user.  the object is */
        /* NOT disposed, so the caller has to do that. */
        public static void ExportWAVSample(
            SampleStorageActualRec TheSample,
            int SamplingRate)
        {
            string path;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "WAV Audio File (.wav)|*.wav";
                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }
                path = dialog.FileName;
            }

            using (Stream outputStream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                Constants.BufferSize))
            {
                using (WAVWriter outputWriter = new WAVWriter(
                    outputStream,
                    TheSample.NumChannels,
                    TheSample.NumBits,
                    SamplingRate))
                {
                    outputWriter.WritePoints(
                        TheSample.Buffer,
                        0,
                        TheSample.NumPoints);
                }
            }
        }
    }
}
