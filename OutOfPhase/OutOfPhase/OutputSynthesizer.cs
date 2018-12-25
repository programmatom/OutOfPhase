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
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace OutOfPhase
{
    public class SynthesizerGeneratorParams<T, W>
    {
        public readonly IMainWindowServices mainWindow;
        public readonly Document document;
        public readonly List<TrackObjectRec> listOfTracks;
        public readonly TrackObjectRec keyTrack;
        public readonly int frameToStartAt;
        public readonly int samplingRate;
        public readonly int envelopeRate;
        public readonly NumChannelsType channels;
        public readonly LargeBCDType defaultBeatsPerMinute;
        public readonly double overallVolumeScalingReciprocal;
        public readonly LargeBCDType scanningGap;
        public readonly NumBitsType bits;
        public readonly bool clipWarn;
        public readonly int oversamplingFactor;
        public readonly bool showSummary;
        public readonly int? randomSeed;
        public readonly bool stayActiveIfNoFrames;
        public readonly bool robust;
        public readonly Synthesizer.AutomationSettings automationSettings;
        public readonly Synthesizer.SynthCycleClientCallback clientCycleCallback;

        public Synthesizer.SynthErrorCodes result;
        public Synthesizer.SynthErrorInfoRec errorInfo;
        public Exception exception;
        public readonly StringWriter interactionLog = new StringWriter();

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
            int? randomSeed,
            bool stayActiveIfNoFrames,
            bool robust,
            Synthesizer.AutomationSettings automationSettings,
            Synthesizer.SynthCycleClientCallback clientCycleCallback)
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
            this.randomSeed = randomSeed;
            this.stayActiveIfNoFrames = stayActiveIfNoFrames;
            this.robust = robust;
            this.automationSettings = automationSettings;
            this.clientCycleCallback = clientCycleCallback;
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
            bool modal,
            (float, OutputGeneric<T, SynthesizerGeneratorParams<T, W>, W>.MeteringCallback)? metering)
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
                modal,
                metering);
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
                    dataCallback,
                    dataCallbackState,
                    generatorParams.document,
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
                    generatorParams.randomSeed,
                    generatorParams.stayActiveIfNoFrames,
                    generatorParams.robust,
                    generatorParams.automationSettings,
                    generatorParams.clientCycleCallback);
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
                IInteractionWindowService interaction = generatorParams.mainWindow.GetInteractionWindow();
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
}
