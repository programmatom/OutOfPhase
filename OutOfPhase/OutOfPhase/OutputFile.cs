/*
 *  Copyright © 1994-2002, 2015-2017 Thomas R. Lawrence
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
using System.Windows.Forms;

namespace OutOfPhase
{
    public class OutputSelectableFileDestination
    {
        public string path;
    }

    public class OutputSelectableFileArguments
    {
    }

    public class OutputSelectableFileDestinationHandler : DestinationHandler<OutputSelectableFileDestination>
    {
        private Stream outputStream;
        private AudioFileWriter outputWriter;

        public static bool OutputSelectableFileGetDestination(out OutputSelectableFileDestination destination)
        {
            destination = null;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "AIFF Audio File (.aif)|*.aif|AIFF Audio File (.aiff)|*.aiff|WAV Audio File (.wav)|*.wav";
                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return false;
                }
                destination = new OutputSelectableFileDestination();
                destination.path = dialog.FileName;
                return true;
            }
        }

        public static DestinationHandler<OutputSelectableFileDestination> CreateOutputSelectableFileDestinationHandler(
            OutputSelectableFileDestination destination,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            OutputSelectableFileArguments arguments)
        {
            return new OutputSelectableFileDestinationHandler(
                destination.path,
                channels,
                bits,
                samplingRate,
                arguments);
        }

        public OutputSelectableFileDestinationHandler(
            string destinationPath,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            OutputSelectableFileArguments arguments)
        {
            outputStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                Constants.BufferSize);
            switch (Path.GetExtension(destinationPath).ToLowerInvariant())
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case ".wav":
                    outputWriter = new WAVWriter(
                        outputStream,
                        channels,
                        bits,
                        samplingRate);
                    break;
                case ".aif":
                case ".aiff":
                    outputWriter = new AIFFWriter(
                        outputStream,
                        channels,
                        bits,
                        samplingRate);
                    break;
            }
        }

        public override Synthesizer.SynthErrorCodes Post(
            float[] data,
            int offset,
            int points)
        {
            outputWriter.WritePoints(
                data,
                offset,
                points);

            return Synthesizer.SynthErrorCodes.eSynthDone;
        }

        public override void Finish(bool abort)
        {
            outputWriter.Close();
            outputWriter = null;
        }

        public override void Dispose()
        {
            if (outputWriter != null)
            {
                Finish(true/*abort*/);
            }

            outputStream.Close();
            outputStream = null;
        }
    }
}
