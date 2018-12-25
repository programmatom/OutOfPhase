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
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using TreeLib;

namespace OutOfPhase
{
    // T: destination object (e.g. file path of audio file to write)
    // U: generator parameters (e.g. configuration of synth engine)
    // W: destination handler arguments (e.g. audio device configuration)



    public delegate bool GetDestinationMethod<T>(out T destination);

    public delegate DestinationHandler<T> CreateDestinationHandlerMethod<T, W>(
        T destination,
        NumChannelsType channels,
        NumBitsType bits,
        int samplingRate,
        W arguments);

    public abstract class DestinationHandler<T> : IDisposable
    {
        public abstract Synthesizer.SynthErrorCodes Post(
            float[] data,
            int offset,
            int points);

        public abstract void Finish(bool abort);

        public abstract void Dispose();
    }



    public delegate void GeneratorMainLoopMethod<T, U, W>(
        U generatorParams,
        Synthesizer.DataOutCallbackMethod<OutputGeneric<T, U, W>> dataCallback,
        OutputGeneric<T, U, W> dataCallbackState,
        Synthesizer.StopTask Stopper);

    public delegate void GeneratorCompletionMethod<U>(
        U generatorParams,
        ref ClipInfo clipInfo);



    [StructLayout(LayoutKind.Auto)]
    public struct ClipInfo
    {
        public int totalSampleCount;
        public int clippedSampleCount;
        public float maxClipExtent;

        public ClipInfo(
            int totalSampleCount,
            int clippedSampleCount,
            float maxClipExtent)
        {
            this.totalSampleCount = totalSampleCount;
            this.clippedSampleCount = clippedSampleCount;
            this.maxClipExtent = maxClipExtent;
        }
    }

    public class OutputGeneric<T, U, W> : IProgressInfo, IBufferLoading, IDisposable
    {
        public NumChannelsType channels;
        public NumBitsType bits;
        public int samplingRate;
        public int oversamplingFactor;

        public int totalSampleCount;
        public int clippedSampleCount;
        public float maxClipExtent;
        public int oversamplingSkipCarryover;

        public Synthesizer.StopTask stopper;
        public OutputProgressWindow progressWindow;

        public Thread thread;
        public WaitFinishedHelper waitFinishedHelper;

        // HPTRI was used as the default in the old application
        // the old C code awkwardly used #if statements to compile in one or the other
        public const bool UseHPTRI = true;
        public Synthesizer.StereoDither ditherState;

        public double ElapsedAudioSeconds { get { return (double)totalSampleCount / samplingRate; } }
        public int TotalFrames { get { return totalSampleCount; } }
        public int TotalClippedPoints { get { return clippedSampleCount; } }

        public AVLTreeMap<float, int> ranks;
        public int delayIndex;
        public float[] delayLine;
        public delegate void MeteringCallback(float shortMax, float longMax, out bool mute);
        public MeteringCallback meteringCallback;

        public T destination;
        public W destinationHandlerParams;
        public CreateDestinationHandlerMethod<T, W> createDestinationHandler;
        public DestinationHandler<T> destinationHandler;

        public GeneratorMainLoopMethod<T, U, W> generatorMainLoop;
        public U generatorParams;
        public GeneratorCompletionMethod<U> generatorCompletion;


        public static OutputGeneric<T, U, W> Do(
            string baseName,
            GetDestinationMethod<T> getDestination,
            CreateDestinationHandlerMethod<T, W> createDestinationHandler,
            W destinationHandlerParams,
            GeneratorMainLoopMethod<T, U, W> generatorMainLoop,
            U generatorParams,
            GeneratorCompletionMethod<U> generatorCompletion,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            int oversamplingFactor,
            bool showProgressWindow,
            bool modal,
            (float, MeteringCallback)? metering) // modal is only meaningful when showProgressWindow==true
        {
            // set up processing record

            OutputGeneric<T, U, W> state = new OutputGeneric<T, U, W>();

            state.channels = channels;
            state.bits = bits;
            state.samplingRate = samplingRate;
            state.oversamplingFactor = oversamplingFactor;

            state.totalSampleCount = 0;
            state.clippedSampleCount = 0;
            state.maxClipExtent = 0;
            state.oversamplingSkipCarryover = 0;

            if (metering.HasValue)
            {
                float delayLengthSeconds = metering.Value.Item1;
                int delayLength = (int)(samplingRate * delayLengthSeconds);
                state.delayLine = new float[delayLength];
                state.ranks = new AVLTreeMap<float, int>((uint)delayLength, AllocationMode.PreallocatedFixed);
                state.ranks.Add(0, delayLength);
                state.meteringCallback = metering.Value.Item2;
            }

            if ((state.bits == NumBitsType.eSample8bit) || (state.bits == NumBitsType.eSample16bit))
            {
                if (OutputGeneric<T, U, W>.UseHPTRI) // TODO: add dither selector to global settings
                {
                    state.ditherState = new Synthesizer.StereoDither_HPTRI(bits);
                }
                else
                {
                    state.ditherState = new Synthesizer.StereoDither_TPDF(bits);
                }
            }

            if (!getDestination(out state.destination))
            {
                return null;
            }

            state.createDestinationHandler = createDestinationHandler;

            state.generatorMainLoop = generatorMainLoop;
            state.generatorParams = generatorParams;
            state.generatorCompletion = generatorCompletion;

            state.destinationHandlerParams = destinationHandlerParams;

            state.stopper = new Synthesizer.StopTask();

            state.waitFinishedHelper = new WaitFinishedHelper();


            // All synth parameter initialization must be done by this point to avoid race conditions!!!
            state.thread = new Thread(ThreadMain);
            state.thread.Start(state);


            if (showProgressWindow)
            {
                state.progressWindow = new OutputProgressWindow(
                    baseName,
                    true/*show clipping*/,
                    state,
                    state,
                    state.stopper,
                    state.waitFinishedHelper,
                    state.waitFinishedHelper,
                    delegate ()
                    {
                        ClipInfo clipInfo = new ClipInfo(
                            state.totalSampleCount,
                            state.clippedSampleCount,
                            state.maxClipExtent);

                        state.generatorCompletion(
                            generatorParams,
                            ref clipInfo);
                    });
                if (modal)
                {
                    state.progressWindow.ShowDialog();
                    state.Dispose(); // suppress finalize
                    state = null;
                }
                else
                {
                    state.progressWindow.Show();
                    // client required to dispose after completion
                }
            }

            // After this, the dialog prevents UI interaction with the application on the main thread
            // and the rendering thread does it's thing until finished.
            // The dialog and the rendering thread talk to each other about stopping and completion.

            return state;
        }

        public void Dispose()
        {
            if (progressWindow != null)
            {
                progressWindow.Dispose();
                progressWindow = null;
            }
            GC.SuppressFinalize(this);
        }

        ~OutputGeneric()
        {
#if DEBUG
            Debug.Assert(false, "OutputGeneric finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            Dispose();
        }
#if DEBUG
        private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

        private static void ThreadMain(object obj)
        {
            OutputGeneric<T, U, W> state = (OutputGeneric<T, U, W>)obj;
            try
            {
                using (state.destinationHandler = state.createDestinationHandler(
                    state.destination,
                    state.channels,
                    state.bits,
                    state.samplingRate,
                    state.destinationHandlerParams))
                {
                    state.generatorMainLoop(
                        state.generatorParams,
                        DataCallback,
                        state,
                        state.stopper);

                    state.destinationHandler.Finish(state.stopper.Stopped);
                }
            }
            catch (Exception exception)
            {
                // generator method should record and defer exceptions. this one got through
                string message = (exception is ApplicationException) ? exception.Message : exception.ToString();
                Program.WriteLog("OutputGeneric.ThreadMain", message);
                MessageBox.Show(message, "Synthesis Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            finally
            {
                state.waitFinishedHelper.NotifyFinished();
            }
        }

        /* synthesizer calls back and passes data to this routine */
        private static Synthesizer.SynthErrorCodes DataCallback(
            OutputGeneric<T, U, W> state,
            float[] data,
            int offset,
            int frameCount)
        {
#if DEBUG
            if ((state.bits != NumBitsType.eSample8bit)
                && (state.bits != NumBitsType.eSample16bit)
                && (state.bits != NumBitsType.eSample24bit))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            // drop samples for oversampling
            if (state.oversamplingFactor > 1)
            {
                int writeScan = 0;
                int readScan = state.oversamplingSkipCarryover;

                while (readScan < frameCount)
                {
                    data[2 * writeScan + 0 + offset] = data[2 * readScan + 0 + offset];
                    data[2 * writeScan + 1 + offset] = data[2 * readScan + 1 + offset];

                    writeScan += 1;
                    readScan += state.oversamplingFactor;
                }

                state.oversamplingSkipCarryover = readScan - frameCount;
                frameCount = writeScan;
            }

            if (state.ranks != null)
            {
                if (frameCount != 0)
                {
                    float shortMax = 0;
                    UpdatePredicate<float, int> removeItem = Synthesizer.CompressorRec._RemoveItem;
                    UpdatePredicate<float, int> addItem = Synthesizer.CompressorRec._AddItem;
                    for (int i = 0; i < frameCount; i++)
                    {
                        state.ranks.ConditionalSetOrRemove(state.delayLine[state.delayIndex], removeItem);
                        float last = Math.Max(Math.Abs(data[2 * i + 0 + offset]), Math.Abs(data[2 * i + 1 + offset]));
                        if (Single.IsNaN(last) || Single.IsInfinity(last))
                        {
                            last = 100;
                        }
                        state.delayLine[state.delayIndex] = last;
                        if (++state.delayIndex >= state.delayLine.Length)
                        {
                            state.delayIndex = 0;
                        }
                        state.ranks.ConditionalSetOrAdd(last, addItem);
                        shortMax = Math.Max(shortMax, last);
                    }
                    float longMax;
                    state.ranks.Greatest(out longMax);
                    bool mute;
                    state.meteringCallback(shortMax, longMax, out mute);
                    if (mute)
                    {
                        for (int i = 0; i < frameCount; i++)
                        {
                            data[2 * i + 0 + offset] = 0;
                            data[2 * i + 1 + offset] = 0;
                        }
                    }
                }
            }

            if (state.channels == NumChannelsType.eSampleMono)
            {
                for (int i = 0; i < frameCount; i++)
                {
                    data[i + offset] = .5f * (data[2 * i + 0 + offset] + data[2 * i + 1 + offset]);
                }
            }

            int pointCount = frameCount;
            if (state.channels == NumChannelsType.eSampleStereo)
            {
                pointCount *= 2;
            }

            float localMaxClipExtent = state.maxClipExtent;
            int localClippedSampleCount = state.clippedSampleCount;
            for (int i = 0; i < pointCount; i++)
            {
                float value = data[i + offset];

                /* find absolute value, and save sign */
                float sign = 1;
                if (value < 0)
                {
                    value = -value;
                    sign = -sign;
                }

                /* clip value at 1 */
                if (value > 1)
                {
                    localClippedSampleCount++;
                    if (value > localMaxClipExtent)
                    {
                        localMaxClipExtent = value;
                    }
                    value = 1;
                }

                data[i + offset] = sign * value;
            }
            state.maxClipExtent = localMaxClipExtent;
            state.clippedSampleCount = localClippedSampleCount;

            if ((state.bits == NumBitsType.eSample8bit) || (state.bits == NumBitsType.eSample16bit))
            {
                if (state.channels == NumChannelsType.eSampleStereo)
                {
                    state.ditherState.DoStereo(
                        frameCount,
                        data,
                        offset);
                }
                else
                {
                    state.ditherState.DoMono(
                        frameCount,
                        data,
                        offset);
                }
            }

            state.destinationHandler.Post(data, offset, pointCount);

            state.totalSampleCount += frameCount;

            return Synthesizer.SynthErrorCodes.eSynthDone;
        }


        // IBufferLoading proxy

        public bool Available
        {
            get
            {
                return destinationHandler is IBufferLoading;
            }
        }

        public float Level
        {
            get
            {
                IBufferLoading bufferLoading = destinationHandler as IBufferLoading;
                if (bufferLoading != null)
                {
                    return bufferLoading.Level;
                }
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
        }

        public float Maximum
        {
            get
            {
                IBufferLoading bufferLoading = destinationHandler as IBufferLoading;
                if (bufferLoading != null)
                {
                    return bufferLoading.Maximum;
                }
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
        }

        public float Critical
        {
            get
            {
                IBufferLoading bufferLoading = destinationHandler as IBufferLoading;
                if (bufferLoading != null)
                {
                    return bufferLoading.Critical;
                }
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
        }
    }
}
