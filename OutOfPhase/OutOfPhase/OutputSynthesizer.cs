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
using System.Threading;
using System.Windows.Forms;

namespace OutOfPhase
{
#if true // prevents "Add New Data Source..." from working
    public class SynthesizerGeneratorParams<T, W>
    {
        public IMainWindowServices mainWindow;
        public Document document;
        public List<TrackObjectRec> listOfTracks;
        public TrackObjectRec keyTrack;
        public int frameToStartAt;
        public int samplingRate;
        public int envelopeRate;
        public NumChannelsType channels;
        public LargeBCDType defaultBeatsPerMinute;
        public double overallVolumeScalingReciprocal;
        public LargeBCDType scanningGap;
        public NumBitsType bits;
        public bool clipWarn;
        public int oversamplingFactor;
        public bool showSummary;
        public bool deterministic; // now ignored - control by setting randomSeed to null or int
        public int? randomSeed;
        public Synthesizer.AutomationSettings automationSettings;

        public Synthesizer.SynthErrorCodes result;
        public Synthesizer.SynthErrorInfoRec errorInfo;
        public Exception exception;
        public StringWriter interactionLog = new StringWriter();

        private bool completed;

        public bool Completed { get { return completed; } }

        public SynthesizerGeneratorParams(
            IMainWindowServices mainWindow,
            Document document,
            List<TrackObjectRec> listOfTracks,
            TrackObjectRec keyTrack,
            int frameToStartAt,
            int samplingRate,
            int envelopeRate,
            NumChannelsType channels,
            LargeBCDType defaultBeatsPerMinute,
            double overallVolumeScalingReciprocal,
            LargeBCDType scanningGap,
            NumBitsType bits,
            bool clipWarn,
            int oversamplingFactor,
            bool showSummary,
            bool deterministic,// now ignored - control by setting randomSeed to null or int
            int? randomSeed,
            Synthesizer.AutomationSettings automationSettings)
        {
            this.mainWindow = mainWindow;
            this.document = document;
            this.listOfTracks = listOfTracks;
            this.keyTrack = keyTrack;
            this.frameToStartAt = frameToStartAt;
            this.samplingRate = samplingRate;
            this.envelopeRate = envelopeRate;
            this.channels = channels;
            this.defaultBeatsPerMinute = defaultBeatsPerMinute;
            this.overallVolumeScalingReciprocal = overallVolumeScalingReciprocal;
            this.scanningGap = scanningGap;
            this.bits = bits;
            this.clipWarn = clipWarn;
            this.oversamplingFactor = oversamplingFactor;
            this.showSummary = showSummary;
            this.deterministic = deterministic;
            this.randomSeed = randomSeed;
            this.automationSettings = automationSettings;
        }

        public static OutputGeneric<T, SynthesizerGeneratorParams<T, W>, W> Do(
            string baseName,
            GetDestinationMethod<T> getDestination,
            CreateDestinationHandlerMethod<T, W> createDestinationHandler,
            W destinationHandlerArguments,
            GeneratorMainLoopMethod<T, SynthesizerGeneratorParams<T, W>, W> generatorMainLoop,
            SynthesizerGeneratorParams<T, W> generatorParams,
            GeneratorCompletionMethod<SynthesizerGeneratorParams<T, W>> generatorCompletion,
            IMainWindowServices mainWindow,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            int oversamplingFactor,
            bool showProgressWindow,
            bool modal)
        {
            // prerequisites 

            /* force an auto-save since play may take a long time */
            MainWindow.DoAutosaveGlobally();

            /* make sure all objects are up to date */
            if (!mainWindow.MakeUpToDate())
            {
                /* couldn't compile score elements */
                return null;
            }

            return OutputGeneric<T, SynthesizerGeneratorParams<T, W>, W>.Do(
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

        public static void SynthesizerMainLoop<U>(
            SynthesizerGeneratorParams<T, W> generatorParams,
            Synthesizer.DataOutCallbackMethod<OutputGeneric<T, U, W>> dataCallback,
            OutputGeneric<T, U, W> dataCallbackState,
            Synthesizer.StopTask stopper)
        {
            try
            {
                generatorParams.result = Synthesizer.DoSynthesizer(
                    generatorParams.document,
                    dataCallback,
                    dataCallbackState,
                    generatorParams.listOfTracks,
                    generatorParams.keyTrack,
                    generatorParams.frameToStartAt,
                    generatorParams.samplingRate * generatorParams.oversamplingFactor,
                    generatorParams.oversamplingFactor,
                    generatorParams.envelopeRate,
                    generatorParams.defaultBeatsPerMinute,
                    generatorParams.overallVolumeScalingReciprocal,
                    generatorParams.scanningGap,
                    stopper,
                    generatorParams.showSummary,
                    out generatorParams.errorInfo,
                    generatorParams.interactionLog,
                    generatorParams.deterministic,
                    generatorParams.randomSeed,
                    generatorParams.automationSettings);
            }
            catch (Exception exception)
            {
                Program.WriteLog("SynthesizerMainLoop", exception.ToString());
                generatorParams.exception = exception;
                stopper.Stop();
            }
            finally
            {
                generatorParams.completed = true;
            }
        }

        public static void SynthesizerCompletion(
            SynthesizerGeneratorParams<T, W> generatorParams,
            ref ClipInfo clipInfo)
        {
            string interactionLogFinal = generatorParams.interactionLog.ToString();
            if (interactionLogFinal.Length != 0)
            {
                InteractionWindow interaction = generatorParams.mainWindow.GetInteractionWindow();
                interaction.Append(interactionLogFinal);
            }

            string message = null;
            if (generatorParams.exception != null)
            {
                message = generatorParams.exception.ToString();
            }
            else if (generatorParams.result != Synthesizer.SynthErrorCodes.eSynthDone)
            {
                message = Synthesizer.GetErrorMessage(generatorParams.result, generatorParams.errorInfo);
            }
            if (message != null)
            {
                MessageBox.Show(message, "Synthesis Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

            if ((message == null) && generatorParams.clipWarn && (clipInfo.clippedSampleCount != 0))
            {
                string clippingMessage = String.Format(
                    "{0} out of {1} samples were clipped, with a maximum overextent of {2:0.000000}. Set the inverse volume to be greater than {3:0.000000} to eliminate the clipping.",
                    clipInfo.clippedSampleCount,
                    clipInfo.totalSampleCount,
                    clipInfo.maxClipExtent,
                    clipInfo.maxClipExtent * generatorParams.overallVolumeScalingReciprocal);
                MessageBox.Show(clippingMessage, "Clipping Ocurred", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
#endif
}
