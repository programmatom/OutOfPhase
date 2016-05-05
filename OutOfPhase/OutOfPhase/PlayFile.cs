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
    public class PlayFile
    {
        private OutputGeneric<OutputDeviceDestination, AudioFilePlayGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>, OutputDeviceArguments> state;
        private AudioFilePlayGeneratorParams<OutputDeviceDestination, OutputDeviceArguments> generatorParams;

        public void Play(string path, IMainWindowServices mainWindow)
        {
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize);
            AudioFileReader reader = null;
            try
            {
                reader = new WAVReader(stream);
            }
            catch (FormatException)
            {
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    reader = new AIFFReader(stream);
                }
                catch (FormatException)
                {
                    MessageBox.Show("File is not a recognized AIFF or WAV file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            const double BufferDuration = 2;

#if true // prevents "Add New Data Source..." from working
            state = AudioFilePlayGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                mainWindow.DisplayName,
                OutputDeviceEnumerator.OutputDeviceGetDestination,
                OutputDeviceEnumerator.CreateOutputDeviceDestinationHandler,
                new OutputDeviceArguments(BufferDuration),
                AudioFilePlayGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.MainLoop,
                generatorParams = new AudioFilePlayGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                    reader),
                AudioFilePlayGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Completion,
                mainWindow,
                reader.NumChannels,
                reader.NumBits,
                reader.SamplingRate,
                1/*oversampling*/,
                true/*showProgressWindow*/,
                true/*modal*/);
#endif
        }
    }

#if true // prevents "Add New Data Source..." from working
    public class AudioFilePlayGeneratorParams<T, W>
    {
        public AudioFileReader reader;

        public Exception exception;

        public AudioFilePlayGeneratorParams(
            AudioFileReader reader)
        {
            this.reader = reader;
        }

        public static OutputGeneric<T, AudioFilePlayGeneratorParams<T, W>, W> Do(
            string baseName,
            GetDestinationMethod<T> getDestination,
            CreateDestinationHandlerMethod<T, W> createDestinationHandler,
            W destinationHandlerArguments,
            GeneratorMainLoopMethod<T, AudioFilePlayGeneratorParams<T, W>, W> generatorMainLoop,
            AudioFilePlayGeneratorParams<T, W> generatorParams,
            GeneratorCompletionMethod<AudioFilePlayGeneratorParams<T, W>> generatorCompletion,
            IMainWindowServices mainWindow,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            int oversamplingFactor,
            bool showProgressWindow,
            bool modal)
        {
            // prerequisites 

            return OutputGeneric<T, AudioFilePlayGeneratorParams<T, W>, W>.Do(
                baseName,
                getDestination,
                createDestinationHandler,
                destinationHandlerArguments,
                generatorMainLoop,
                generatorParams,
                generatorCompletion,
                channels,
                bits,
                samplingRate,
                oversamplingFactor,
                showProgressWindow,
                modal);
        }

        public static void MainLoop<U>(
            AudioFilePlayGeneratorParams<T, W> generatorParams,
            Synthesizer.DataOutCallbackMethod<OutputGeneric<T, U, W>> dataCallback,
            OutputGeneric<T, U, W> dataCallbackState,
            Synthesizer.StopTask stopper)
        {
            try
            {
                const int NUMPOINTS = 256;
                float[] buffer = new float[NUMPOINTS];
                float[] buffer2 = new float[NUMPOINTS * 2];
                int c;
                while ((c = generatorParams.reader.ReadPoints(buffer, 0, buffer.Length)) != 0)
                {
                    if (stopper.Stopped)
                    {
                        break;
                    }
                    if (generatorParams.reader.NumChannels == NumChannelsType.eSampleStereo)
                    {
                        dataCallback(
                            dataCallbackState,
                            buffer,
                            0,
                            c / 2);
                    }
                    else
                    {
                        for (int i = 0; i < c; i++)
                        {
                            buffer2[2 * i + 0] = buffer[i];
                            buffer2[2 * i + 1] = buffer[i];
                        }
                        dataCallback(
                            dataCallbackState,
                            buffer2,
                            0,
                            c);
                    }
                }

            }
            catch (Exception exception)
            {
                generatorParams.exception = exception;
                stopper.Stop();
            }
        }

        public static void Completion(
            AudioFilePlayGeneratorParams<T, W> generatorParams,
            ref ClipInfo clipInfo)
        {
            generatorParams.reader.Close();
            generatorParams.reader = null;
        }
    }
#endif
}
