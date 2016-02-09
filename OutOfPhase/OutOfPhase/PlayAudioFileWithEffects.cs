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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    // NOTE: this implementation is a bit of a hack and has multiple problems

    public partial class PlayAudioFileWithEffects : Form
    {
        // TODO: Static is wrong storage class - This window should be one-only at scope of MainWindow object.
        // It's OK for now, since the window is used as a modal dialog.

        private static string effectBody = "# Score Effects" + Environment.NewLine;
        private static string filePath;
        private static short savedX, savedY, savedWidth, savedHeight;

        private MainWindow mainWindow;

        public PlayAudioFileWithEffects(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            this.textBoxEffectBody.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            this.textBoxEffectBody.AutoIndent = Program.Config.AutoIndent;

            textBoxAudioFilePath.Text = filePath;

            textBoxEffectBody.TabSize = Program.Config.TabSize;
            textBoxEffectBody.Text = effectBody;
        }

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, savedX, savedY, savedWidth, savedHeight);
            base.OnShown(e);
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                savedX = x;
                savedY = y;
                savedWidth = width;
                savedHeight = height;
            }
        }

        protected override void OnMove(EventArgs e)
        {
            ResizeMove();
            base.OnMove(e);
        }

        protected override void OnResize(EventArgs e)
        {
            ResizeMove();
            base.OnResize(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            filePath = textBoxAudioFilePath.Text;
            effectBody = textBoxEffectBody.Text;
#if false
            if (state != null)
            {
                state.stopper.Stop();
            }
#endif
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        private bool BuildThis(out Synthesizer.EffectSpecListRec effectSpec)
        {
            effectSpec = null;

            if (!Validate()) // ensure all controls commit data to store
            {
                return false;
            }

            int ErrorLine;
            string ErrorExtraMessage;
            Synthesizer.BuildInstrErrors Error = Synthesizer.BuildScoreEffectList(
                textBoxEffectBody.Text,
                mainWindow.Document.CodeCenter,
                out ErrorLine,
                out ErrorExtraMessage,
                out effectSpec);
            if (Error != Synthesizer.BuildInstrErrors.eBuildInstrNoError)
            {
                BuildErrorInfo errorInfo = new LiteralBuildErrorInfo(Synthesizer.BuildInstrGetErrorMessageText(Error, ErrorExtraMessage), ErrorLine);
                textBoxEffectBody.Focus();
                textBoxEffectBody.SetSelectionLine(ErrorLine - 1);
                textBoxEffectBody.ScrollToSelection();
                MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                return false;
            }

            return true;
        }

#if false
        private OutputGeneric<OutputDeviceDestination, PlayFileWithEffectsGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>, OutputDeviceArguments> state;
        private PlayFileWithEffectsGeneratorParams<OutputDeviceDestination, OutputDeviceArguments> generatorParams;
#endif

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            MainWindow.DoAutosaveGlobally();

            if (!mainWindow.MakeUpToDate())
            {
                return;
            }

            Synthesizer.EffectSpecListRec effectSpec;
            if (!BuildThis(out effectSpec))
            {
                return;
            }

            const double BufferDuration = 2f;

#if false
            if (state != null)
            {
                state.stopper.Stop();
            }
            state = null;
#endif

            Stream stream = new FileStream(textBoxAudioFilePath.Text, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize);
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

#if true // prevents "Add New Data Source..." from working
#if false
            state =
#endif
            PlayFileWithEffectsGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                Path.GetFileNameWithoutExtension(textBoxAudioFilePath.Text),
                OutputDeviceEnumerator.OutputDeviceGetDestination,
                OutputDeviceEnumerator.CreateOutputDeviceDestinationHandler,
                new OutputDeviceArguments(BufferDuration),
                PlayFileWithEffectsGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.MainLoop,
#if false
                generatorParams =
#endif
 new PlayFileWithEffectsGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                    stream,
                    reader,
                    effectSpec,
                    mainWindow),
                PlayFileWithEffectsGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Completion,
                mainWindow,
                reader.NumChannels,
                reader.NumBits,
                reader.SamplingRate,
                1/*oversampling*/,
                true/*showProgressWindow*/,
                true/*modal*/);
#endif
        }

#if false
        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (state != null)
            {
                state.stopper.Stop();
                state = null;
            }

#if false
            if (generatorParams.exception != null)
            {
                MessageBox.Show(generatorParams.exception.ToString());
            }
            state = null;
            generatorParams = null;
#endif
        }
#endif

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonSelectAudioFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Audio File";
                dialog.Filter = "AIFF Audio File (.aif, *.aiff)|*.aif;*.aiff|WAV Audio File (.wav)|*.wav|Any File Type (*)|*";
                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    textBoxAudioFilePath.Text = dialog.FileName;
                }
            }
        }
    }

#if true // prevents "Add New Data Source..." from working
    public class PlayFileWithEffectsGeneratorParams<T, W>
    {
        public Stream stream;
        public AudioFileReader reader;
        public Synthesizer.EffectSpecListRec effectSpec;
        public MainWindow mainWindow;

        public Synthesizer.SynthStateRec synthState;
        public StringWriter interactionLog = new StringWriter();

        public Synthesizer.SynthErrorCodes result;
        public Synthesizer.SynthErrorInfoRec errorInfo;
        public Exception exception;

        public PlayFileWithEffectsGeneratorParams(
            Stream stream,
            AudioFileReader reader,
            Synthesizer.EffectSpecListRec effectSpec,
            MainWindow mainWindow)
        {
            this.stream = stream;
            this.reader = reader;
            this.effectSpec = effectSpec;
            this.mainWindow = mainWindow;
        }

        public static OutputGeneric<T, PlayFileWithEffectsGeneratorParams<T, W>, W> Do(
            string baseName,
            GetDestinationMethod<T> getDestination,
            CreateDestinationHandlerMethod<T, W> createDestinationHandler,
            W destinationHandlerArguments,
            GeneratorMainLoopMethod<T, PlayFileWithEffectsGeneratorParams<T, W>, W> generatorMainLoop,
            PlayFileWithEffectsGeneratorParams<T, W> generatorParams,
            GeneratorCompletionMethod<PlayFileWithEffectsGeneratorParams<T, W>> generatorCompletion,
            MainWindow mainWindow,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            int oversamplingFactor,
            bool showProgressWindow,
            bool modal)
        {
            // prerequisites 

            return OutputGeneric<T, PlayFileWithEffectsGeneratorParams<T, W>, W>.Do(
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
            PlayFileWithEffectsGeneratorParams<T, W> generatorParams,
            Synthesizer.DataOutCallbackMethod<OutputGeneric<T, U, W>> dataCallback,
            OutputGeneric<T, U, W> dataCallbackState,
            Synthesizer.StopTask stopper)
        {
            try
            {
                generatorParams.result = Synthesizer.SynthStateRec.InitializeSynthesizer(
                    out generatorParams.synthState,
                    generatorParams.mainWindow.Document,
                    new List<TrackObjectRec>(),
                    null,
                    0/*FrameToStartAt*/,
                    generatorParams.reader.SamplingRate,
                    1/*Oversampling*/,
                    generatorParams.mainWindow.Document.EnvelopeUpdateRate,
                    (LargeBCDType)generatorParams.mainWindow.Document.DefaultBeatsPerMinute,
                    1/*OverallVolumeScalingReciprocal*/,
                    (LargeBCDType)0d/*ScanningGap*/,
                    out generatorParams.errorInfo,
                    TextWriter.Synchronized(generatorParams.interactionLog),
                    generatorParams.mainWindow.Document.Deterministic,
                    generatorParams.mainWindow.Document.Seed,
                    new Synthesizer.AutomationSettings());
                if (generatorParams.result != Synthesizer.SynthErrorCodes.eSynthDone)
                {
                    return;
                }

                // HACK!
                generatorParams.result = Synthesizer.NewTrackEffectGenerator(
                    generatorParams.effectSpec,
                    generatorParams.synthState.SynthParams0,
                    out generatorParams.synthState.ScoreEffectProcessor);
                if (generatorParams.result != Synthesizer.SynthErrorCodes.eSynthDone)
                {
                    return;
                }

                while (!stopper.Stopped)
                {
                    // TODO: shouldn't ask for nAllocatedPointsOneChannel, that's slightly inaccurate. Should
                    // use the clock logic in SynthGenerateOneCycle -- see e.g. nActualFrames
                    int c;
                    if (generatorParams.reader.NumChannels == NumChannelsType.eSampleMono)
                    {
                        c = generatorParams.reader.ReadPoints(
                            generatorParams.synthState.SynthParams0.workspace,
                            generatorParams.synthState.SynthParams0.ScoreWorkspaceLOffset,
                            generatorParams.synthState.SynthParams0.nAllocatedPointsOneChannel);
                        Synthesizer.FloatVectorCopy(
                            generatorParams.synthState.SynthParams0.workspace,
                            generatorParams.synthState.SynthParams0.ScoreWorkspaceLOffset,
                            generatorParams.synthState.SynthParams0.workspace,
                            generatorParams.synthState.SynthParams0.ScoreWorkspaceROffset,
                            c);
                    }
                    else
                    {
                        c = generatorParams.reader.ReadPoints(
                            generatorParams.synthState.SynthParams0.workspace,
                            generatorParams.synthState.SynthParams0.SectionWorkspaceLOffset,
                            generatorParams.synthState.SynthParams0.nAllocatedPointsOneChannel * 2);
                        c /= 2;
                        Synthesizer.FloatVectorMakeUninterleaved(
                            generatorParams.synthState.SynthParams0.workspace,
                            generatorParams.synthState.SynthParams0.SectionWorkspaceLOffset,
                            generatorParams.synthState.SynthParams0.workspace,
                            generatorParams.synthState.SynthParams0.ScoreWorkspaceLOffset,
                            generatorParams.synthState.SynthParams0.workspace,
                            generatorParams.synthState.SynthParams0.ScoreWorkspaceROffset,
                            c);
                    }
                    if (c == 0)
                    {
                        break;
                    }

                    // HACK!
                    // Should create specialized version of Synthesizer.SynthGenerateOneCycle that does this
                    Synthesizer.UpdateStateTrackEffectGenerator(
                        generatorParams.synthState.ScoreEffectProcessor,
                        generatorParams.synthState.SynthParams0);
                    Synthesizer.ApplyTrackEffectGenerator(
                        generatorParams.synthState.ScoreEffectProcessor,
                        generatorParams.synthState.SynthParams0.workspace,
                        c,
                        generatorParams.synthState.SynthParams0.ScoreWorkspaceLOffset,
                        generatorParams.synthState.SynthParams0.ScoreWorkspaceROffset,
                        generatorParams.synthState.SynthParams0);

                    Synthesizer.FloatVectorMakeInterleaved(
                        generatorParams.synthState.SynthParams0.workspace,
                        generatorParams.synthState.SynthParams0.ScoreWorkspaceLOffset,
                        generatorParams.synthState.SynthParams0.workspace,
                        generatorParams.synthState.SynthParams0.ScoreWorkspaceROffset,
                        c,
                        generatorParams.synthState.SynthParams0.workspace,
                        generatorParams.synthState.SynthParams0.SectionWorkspaceLOffset);
                    dataCallback(
                        dataCallbackState,
                        generatorParams.synthState.SynthParams0.workspace,
                        generatorParams.synthState.SynthParams0.SectionWorkspaceLOffset,
                        c);
                }
            }
            catch (Exception exception)
            {
                generatorParams.exception = exception;
                stopper.Stop();
            }
        }

        public static void Completion(
            PlayFileWithEffectsGeneratorParams<T, W> generatorParams,
            ref ClipInfo clipInfo)
        {
            if (generatorParams.synthState != null)
            {
                Synthesizer.SynthStateRec.FinalizeSynthesizer(
                    generatorParams.synthState,
                    (generatorParams.result == Synthesizer.SynthErrorCodes.eSynthDone) && (generatorParams.exception == null)/*writeOutputLogs*/);
                generatorParams.synthState = null;
            }

            if (generatorParams.reader != null)
            {
                generatorParams.reader.Close();
                generatorParams.reader = null;
            }
            if (generatorParams.stream != null)
            {
                generatorParams.stream.Close();
                generatorParams.stream = null;
            }

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
        }
    }
#endif
}
