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
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using TextEditor;

namespace OutOfPhase
{
    public static class Program
    {
        private static GlobalPrefs config = new GlobalPrefs();
        public static GlobalPrefs Config { get { return config; } }

        private const string SettingsFileName = "Settings.xml";
        private const string LocalApplicationDirectoryName = "OutOfPhase";

        public static string GetSettingsDirectory(bool create, bool roaming)
        {
            string root = Environment.GetFolderPath(
                roaming ? Environment.SpecialFolder.ApplicationData : Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.None);
            string dir = Path.Combine(root, LocalApplicationDirectoryName);
            if (create)
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        private static string GetSettingsPath(bool create, bool roaming)
        {
            string dir = GetSettingsDirectory(create, roaming);
            string path = Path.Combine(dir, SettingsFileName);
            return path;
        }

        public static void LoadSettings()
        {
            string pathRoaming = GetSettingsPath(false/*create*/, true/*roaming*/);
            string pathLocal = GetSettingsPath(false/*create*/, false/*roaming*/);
            if (File.Exists(pathRoaming) || File.Exists(pathLocal))
            {
                try
                {
                    config = new GlobalPrefs(pathRoaming, pathLocal);
                }
                catch (Exception exception)
                {
                    //Debug.Assert(false);
                    MessageBox.Show(String.Format("Exception reading settings file: {0}", exception));
                }
            }
        }

        public static void SaveSettings()
        {
            Config.Save(
                GetSettingsPath(true/*create*/, true/*roaming*/),
                GetSettingsPath(true/*create*/, false/*roaming*/));
        }

        public static void SaveFFTWWisdomIfNeeded()
        {
            string wisdom = Synthesizer.FFTW.CollectWisdom();
            if (!String.Equals(wisdom, Program.Config.FFTWWisdom))
            {
                Program.Config.FFTWWisdom = wisdom;
                Program.SaveSettings();
            }
        }

        public static void ReferenceRecentDocument(string path)
        {
            Program.Config.ReferenceRecentDocument(path);
            Program.SaveSettings();
        }

        private static TextWriter log;

        public static void WriteLog(string prefix, TextReader reader)
        {
            if (log != null)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    log.WriteLine("    {0}: {1}", prefix, line);
                }
                log.Flush();
            }
        }

        public static void WriteLog(string prefix, string text)
        {
            if (log != null)
            {
                WriteLog(prefix, new StringReader(text));
            }
        }

        private static void ShiftArgs(int used, ref string[] args)
        {
            Array.Copy(args, used, args, 0, args.Length - used);
            Array.Resize(ref args, args.Length - used);
        }

        private static string DecodePathUnicodeHack(bool enable, string source)
        {
            if (!enable)
            {
                return source;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != '_')
                {
                    sb.Append(source[i]);
                }
                else
                {
                    i++;
                    if (source[i] != 'x')
                    {
                        throw new ArgumentException(String.Format("malformed unicode escape for path hack: {0}", source));
                    }
                    i++;
                    int u;
                    try
                    {
                        u = Int32.Parse(source.Substring(i, 4), System.Globalization.NumberStyles.HexNumber);
                    }
                    catch (Exception)
                    {
                        throw new ArgumentException(String.Format("malformed unicode escape for path hack: {0}", source));
                    }
                    i += 4;
                    if (source[i] != '_')
                    {
                        throw new ArgumentException(String.Format("malformed unicode escape for path hack: {0}", source));
                    }
                    sb.Append((char)u);
                }
            }
            return sb.ToString();
        }

        public static AudioFileReader TryGetAudioReader(Stream stream)
        {
            AudioFileReader reader = null;
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
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
                    return null;
                }
            }
            return reader;
        }

        private static void CompareAudioFiles(
            string leftPath,
            string rightPath,
            int significantBits,
            float? autoAlign,
            int permittedDriftInterval,
            long limit,
            bool histogram,
            TextWriter log,
            out int skipLeftOut,
            out int skipRightOut)
        {
            skipLeftOut = 0;
            skipRightOut = 0;

            if (log == null)
            {
                log = StreamWriter.Null;
            }

            try
            {
                using (Stream streamLeft = new FileStream(leftPath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
                {
                    using (Stream streamRight = new FileStream(rightPath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
                    {
                        AudioFileReader readerLeft = TryGetAudioReader(streamLeft);
                        AudioFileReader readerRight = TryGetAudioReader(streamRight);
                        try
                        {
                            if (readerLeft == null)
                            {
                                log.WriteLine("      \"{0}\" is not a recognized AIFF or WAV file.", leftPath);
                                return;
                            }
                            if (readerRight == null)
                            {
                                log.WriteLine("      \"{0}\" is not a recognized AIFF or WAV file.", rightPath);
                                return;
                            }

                            if (readerLeft.NumChannels != readerRight.NumChannels)
                            {
                                log.WriteLine("      files have DIFFERING numbers of channels");
                                return;
                            }
                            int ch = readerLeft.NumChannels == NumChannelsType.eSampleStereo ? 2 : 1;
                            if (readerLeft.NumBits != readerRight.NumBits)
                            {
                                log.WriteLine("      files have DIFFERING numbers of bits (warning - proceeding anyway)");
                            }
                            if (readerLeft.SamplingRate != readerRight.SamplingRate)
                            {
                                log.WriteLine("      files have DIFFERING sampling rates (warning - proceeding anyway)");
                            }

                            float[] l = new float[2], r = new float[2];

                            if (autoAlign.HasValue)
                            {
                                int windowLength = (int)Math.Ceiling(autoAlign.Value * readerLeft.SamplingRate);
                                if ((windowLength & (windowLength - 1)) != 0)
                                {
                                    windowLength = windowLength | (windowLength >> 1);
                                    windowLength = windowLength | (windowLength >> 2);
                                    windowLength = windowLength | (windowLength >> 4);
                                    windowLength = windowLength | (windowLength >> 8);
                                    windowLength = windowLength | (windowLength >> 16);
                                    windowLength++;
                                }
                                using (Synthesizer.FFT leftComposite = Synthesizer.FFT.Create(windowLength))
                                {
                                    using (Synthesizer.FFT rightComposite = Synthesizer.FFT.Create(windowLength))
                                    {
                                        int skipLeft = 0, skipRight = 0;

                                        float bestObservedSig = 1;

                                        bool success = false;
                                        int cumulativeFrame = 0, iterations = 1;

                                    Again:
                                        int c;
                                        if (readerLeft.NumChannels == NumChannelsType.eSampleStereo)
                                        {
                                            // stereo

                                            float[] workspace = new float[windowLength * 2];

                                            if (iterations == 1)
                                            {
                                                // skip first n

                                                c = readerLeft.ReadPoints(workspace, 0, windowLength * 2);
                                                if (c != windowLength * 2)
                                                {
                                                    goto Done;
                                                }
                                                c = readerRight.ReadPoints(workspace, 0, windowLength * 2);
                                                if (c != windowLength * 2)
                                                {
                                                    goto Done;
                                                }
                                            }

                                            // load second n

                                            c = readerLeft.ReadPoints(workspace, 0, windowLength * 2);
                                            if (c != windowLength * 2)
                                            {
                                                goto Done;
                                            }
                                            for (int i = 0; i < windowLength; i++)
                                            {
                                                leftComposite.Base[i + leftComposite.Offset]
                                                    = workspace[2 * i + 0]
                                                    + workspace[2 * i + 1];
                                            }

                                            c = readerRight.ReadPoints(workspace, 0, windowLength * 2);
                                            if (c != windowLength * 2)
                                            {
                                                goto Done;
                                            }
                                            for (int i = 0; i < windowLength; i++)
                                            {
                                                rightComposite.Base[i + rightComposite.Offset]
                                                    = workspace[2 * i + 0]
                                                    + workspace[2 * i + 1];
                                            }
                                        }
                                        else
                                        {
                                            // mono

                                            if (iterations == 1)
                                            {
                                                // skip first n
                                                c = readerLeft.ReadPoints(leftComposite.Base, leftComposite.Offset, windowLength);
                                                if (c != windowLength * 2)
                                                {
                                                    goto Done;
                                                }
                                                c = readerRight.ReadPoints(rightComposite.Base, rightComposite.Offset, windowLength);
                                                if (c != windowLength * 2)
                                                {
                                                    goto Done;
                                                }
                                            }

                                            // load second n
                                            c = readerLeft.ReadPoints(leftComposite.Base, leftComposite.Offset, windowLength);
                                            if (c != windowLength * 2)
                                            {
                                                goto Done;
                                            }
                                            c = readerRight.ReadPoints(rightComposite.Base, rightComposite.Offset, windowLength);
                                            if (c != windowLength * 2)
                                            {
                                                goto Done;
                                            }
                                        }

                                        // correlation
                                        leftComposite.FFTfwd();
                                        rightComposite.FFTfwd();
                                        Synthesizer.FloatVectorComplexConjugate(
                                            leftComposite.Base,
                                            leftComposite.Offset,
                                            windowLength + 2);
                                        Synthesizer.FloatVectorMultiplyComplex(
                                            leftComposite.Base,
                                            leftComposite.Offset,
                                            rightComposite.Base,
                                            rightComposite.Offset,
                                            leftComposite.Base,
                                            leftComposite.Offset,
                                            windowLength + 2);
                                        leftComposite.FFTinv();

                                        Program.SaveFFTWWisdomIfNeeded();

                                        KeyValuePair<int, float>[] sorted = new KeyValuePair<int, float>[windowLength];
                                        for (int i = 0; i < windowLength; i++)
                                        {
                                            sorted[i] = new KeyValuePair<int, float>(i, leftComposite.Base[i + leftComposite.Offset]);
                                        }
                                        Array.Sort(sorted, delegate (KeyValuePair<int, float> ll, KeyValuePair<int, float> rr) { return -ll.Value.CompareTo(rr.Value); });
                                        int maxIndex = sorted[0].Key;

                                        const float ThreshholdOfSignificance = .99f;
                                        float currentSig = sorted[1].Value / sorted[0].Value;
                                        if (currentSig >= ThreshholdOfSignificance)
                                        {
                                            iterations++;
                                            cumulativeFrame += windowLength;
                                            bestObservedSig = Math.Min(bestObservedSig, currentSig);
                                            goto Again;
                                        }
                                        else
                                        {
                                            log.WriteLine("      auto-align: sufficient distinction found after {0:0.000} seconds of audio ({1} iterations)", (decimal)cumulativeFrame / readerLeft.SamplingRate, iterations);
                                        }

                                        success = true;

                                        // compute skip counts
                                        if (maxIndex == 0)
                                        {
                                            log.WriteLine("      auto-align: skipping 0 frames - files are already aligned");
                                        }
                                        else if (maxIndex < windowLength / 2)
                                        {
                                            skipRight = maxIndex;
                                            log.WriteLine("      auto-align: skipping first {0} frames in right file", skipRight);
                                        }
                                        else
                                        {
                                            skipLeft = windowLength - maxIndex;
                                            log.WriteLine("      auto-align: skipping first {0} frames in left file", skipLeft);
                                        }
                                        skipLeftOut = skipLeft;
                                        skipRightOut = skipRight;

                                    Done:
                                        // reset readers
                                        readerLeft = TryGetAudioReader(streamLeft);
                                        readerRight = TryGetAudioReader(streamRight);

                                        // fast forward
                                        while (skipLeft > 0)
                                        {
                                            c = readerLeft.ReadPoints(l, 0, ch);
                                            if (c != ch)
                                            {
                                                throw new InvalidOperationException();
                                            }
                                            skipLeft--;
                                        }
                                        while (skipRight > 0)
                                        {
                                            c = readerRight.ReadPoints(r, 0, ch);
                                            if (c != ch)
                                            {
                                                throw new InvalidOperationException();
                                            }
                                            skipRight--;
                                        }

                                        if (!success)
                                        {
                                            log.WriteLine("      AUTO-ALIGN NOT DONE: unable to find sufficiently distinct ({0}) offset - either window is too large or data too similar (best observed: {1})", ThreshholdOfSignificance, bestObservedSig);
                                        }
                                    }
                                }
                            }

                            float driftQuantum;
                            switch (readerLeft.NumBits)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case NumBitsType.eSample8bit:
                                    driftQuantum = SampConv.SignedByteToFloat(1);
                                    break;
                                case NumBitsType.eSample16bit:
                                    driftQuantum = SampConv.SignedShortToFloat(1);
                                    break;
                                case NumBitsType.eSample24bit:
                                    driftQuantum = SampConv.SignedTribyteToFloat(1);
                                    break;
                            }

                            float quantum = 1f / (1 << significantBits);

                            long differCount = 0;
                            int[] histo = histogram ? new int[24] : null;
                            int lDriftCounter = 0, rDriftCounter = 0;
                            int lDrift = 0, rDrift = 0;
                            while (true)
                            {
                                {
                                    int cl, cr;
                                    cl = readerLeft.ReadPoints(l, 0, ch);
                                    cr = readerRight.ReadPoints(r, 0, ch);
                                    if ((cl == 0) && (cr == 0))
                                    {
                                        break;
                                    }
                                    if (cl != cr)
                                    {
                                        log.WriteLine(
                                            "      {0} file DIFFERS, terminates early (IDENTICAL until this point); {1} ({2}) remaining",
                                            cl == 0 ? "left" : "right",
                                            cl == 0 ? readerRight.NumFrames - readerRight.CurrentFrame : readerLeft.NumFrames - readerLeft.CurrentFrame,
                                            new TimeSpan((long)(TimeSpan.TicksPerSecond * (cl == 0 ? (double)(readerRight.NumFrames - readerRight.CurrentFrame) / readerRight.SamplingRate : (double)(readerLeft.NumFrames - readerLeft.CurrentFrame) / readerLeft.SamplingRate))));
                                        differCount++;
                                        goto Finished;
                                    }
                                }

                                if (permittedDriftInterval != 0)
                                {
                                    lDriftCounter = Math.Max(lDriftCounter - 1, 0);
                                    rDriftCounter = Math.Max(rDriftCounter - 1, 0);
                                    if ((l[0] > r[0]) && (lDriftCounter == 0))
                                    {
                                        lDrift--;
                                        lDriftCounter = permittedDriftInterval;
                                    }
                                    else if ((l[0] < r[0]) && (lDriftCounter == 0))
                                    {
                                        lDrift++;
                                        lDriftCounter = permittedDriftInterval;
                                    }
                                    if ((l[1] > r[1]) && (rDriftCounter == 0))
                                    {
                                        rDrift--;
                                        rDriftCounter = permittedDriftInterval;
                                    }
                                    else if ((l[1] < r[1]) && (rDriftCounter == 0))
                                    {
                                        rDrift++;
                                        rDriftCounter = permittedDriftInterval;
                                    }
                                }

                                if ((Math.Abs(l[0] - r[0] + driftQuantum * lDrift) > quantum)
                                    || (Math.Abs(l[1] - r[1] + driftQuantum * rDrift) > quantum))
                                {
                                    int q;
                                    int lv0, lv1;
                                    switch (readerLeft.NumBits)
                                    {
                                        default:
                                            Debug.Assert(false);
                                            throw new ArgumentException();
                                        case NumBitsType.eSample8bit:
                                            lv0 = SampConv.FloatToSignedByte(l[0]);
                                            lv1 = SampConv.FloatToSignedByte(l[1]);
                                            q = SampConv.FloatToSignedByte(quantum);
                                            break;
                                        case NumBitsType.eSample16bit:
                                            lv0 = SampConv.FloatToSignedShort(l[0]);
                                            lv1 = SampConv.FloatToSignedShort(l[1]);
                                            q = SampConv.FloatToSignedShort(quantum);
                                            break;
                                        case NumBitsType.eSample24bit:
                                            lv0 = SampConv.FloatToSignedTribyte(l[0]);
                                            lv1 = SampConv.FloatToSignedTribyte(l[1]);
                                            q = SampConv.FloatToSignedTribyte(quantum);
                                            break;
                                    }
                                    int rv0, rv1;
                                    switch (readerRight.NumBits)
                                    {
                                        default:
                                            Debug.Assert(false);
                                            throw new ArgumentException();
                                        case NumBitsType.eSample8bit:
                                            rv0 = SampConv.FloatToSignedByte(r[0]);
                                            rv1 = SampConv.FloatToSignedByte(r[1]);
                                            break;
                                        case NumBitsType.eSample16bit:
                                            rv0 = SampConv.FloatToSignedShort(r[0]);
                                            rv1 = SampConv.FloatToSignedShort(r[1]);
                                            break;
                                        case NumBitsType.eSample24bit:
                                            rv0 = SampConv.FloatToSignedTribyte(r[0]);
                                            rv1 = SampConv.FloatToSignedTribyte(r[1]);
                                            break;
                                    }
                                    if (!histogram || (limit != Int64.MaxValue))
                                    {
                                        log.WriteLine(
                                            "      values DIFFER starting at point left: {0} ({1}), right:  {2} ({3})  [q={4}, l({5}{11},{6}{12}), r({7},{8}), d({9},{10})]",
                                            readerLeft.CurrentFrame,
                                            new TimeSpan(readerLeft.CurrentFrame * TimeSpan.TicksPerSecond / readerLeft.SamplingRate),
                                            readerRight.CurrentFrame,
                                            new TimeSpan(readerRight.CurrentFrame * TimeSpan.TicksPerSecond / readerRight.SamplingRate),
                                            q,
                                            lv0,
                                            lv1,
                                            rv0,
                                            rv1,
                                            Math.Abs(lv0 - rv0 + lDrift),
                                            Math.Abs(lv1 - rv1 + rDrift),
                                            lDrift != 0 ? String.Concat("+", lDrift.ToString()) : null,
                                            rDrift != 0 ? String.Concat("+", rDrift.ToString()) : null);
                                    }

                                    if (histogram)
                                    {
                                        foreach (int v in new int[] { Math.Abs(lv0 - rv0), Math.Abs(lv1 - rv1) })
                                        {
                                            if (!(v > q))
                                            {
                                                continue;
                                            }
                                            int i = v;
                                            int p = 0;
                                            while (i != 0)
                                            {
                                                i = i >> 1;
                                                p++;
                                            }
                                            histo[p]++;
                                        }
                                    }

                                    differCount++;
                                    if (differCount > limit)
                                    {
                                        log.WriteLine("      TOO MANY DIFFERENCES - aborting");
                                        goto Finished;
                                    }
                                }
                            }

                            if (differCount == 0)
                            {
                                log.WriteLine("      files IDENTICAL within specified tolerances; {0}/{1}", readerLeft.CurrentFrame, readerRight.CurrentFrame);
                            }

                        Finished:
                            if (histogram && (differCount > 0))
                            {
                                int histoTop = histo.Length - 1;
                                while ((histoTop > 0) && (histo[histoTop] == 0))
                                {
                                    histoTop--;
                                }
                                bool started = false;
                                for (int i = 0; i <= histoTop; i++)
                                {
                                    if (!started && (histo[i] == 0))
                                    {
                                        continue;
                                    }
                                    started = true;
                                    int bottom = (1 << i) / 2;
                                    int top = Math.Max((bottom << 1) - 1, 0);
                                    log.WriteLine("      histo: [{0,5}..{1,5}] {2,5}", bottom, top, histo[i]);
                                }
                            }
                        }
                        finally
                        {
                            if (readerRight != null)
                            {
                                readerRight.Dispose();
                            }
                            if (readerLeft != null)
                            {
                                readerLeft.Dispose();
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException exception)
            {
                log.WriteLine("      file not found: \"{0}\"", exception.FileName);
            }
        }

        private delegate void DeltaFunction(float[] l, float[] r, float[] y);
        private static void WriteAudioFileDelta(
            string leftPath,
            string rightPath,
            string outputPath,
            int skipLeft,
            int skipRight,
            DeltaFunction delta)
        {
            using (Stream streamOutput = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, Constants.BufferSize))
            {
                try
                {
                    using (Stream streamLeft = new FileStream(leftPath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
                    {
                        using (Stream streamRight = new FileStream(rightPath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
                        {
                            AudioFileReader readerLeft = TryGetAudioReader(streamLeft);
                            AudioFileReader readerRight = TryGetAudioReader(streamRight);
                            AudioFileWriter writerOutput = null;
                            try
                            {
                                if (readerLeft == null)
                                {
                                    return;
                                }
                                if (readerRight == null)
                                {
                                    return;
                                }

                                if (readerLeft.NumChannels != readerRight.NumChannels)
                                {
                                    return;
                                }
                                int ch = readerLeft.NumChannels == NumChannelsType.eSampleStereo ? 2 : 1;

                                writerOutput = new AIFFWriter(streamOutput, readerLeft.NumChannels, readerLeft.NumBits, readerLeft.SamplingRate);

                                float[] l = new float[2], r = new float[2];

                                // fast forward
                                while (skipLeft > 0)
                                {
                                    int c = readerLeft.ReadPoints(l, 0, ch);
                                    if (c != ch)
                                    {
                                        throw new InvalidOperationException();
                                    }
                                    skipLeft--;
                                }
                                while (skipRight > 0)
                                {
                                    int c = readerRight.ReadPoints(r, 0, ch);
                                    if (c != ch)
                                    {
                                        throw new InvalidOperationException();
                                    }
                                    skipRight--;
                                }

                                while (true)
                                {
                                    int cl, cr;
                                    cl = readerLeft.ReadPoints(l, 0, ch);
                                    cr = readerRight.ReadPoints(r, 0, ch);
                                    if ((cl == 0) && (cr == 0))
                                    {
                                        break;
                                    }
                                    if (cl == 0)
                                    {
                                        Array.Clear(l, 0, l.Length);
                                    }
                                    if (cr == 0)
                                    {
                                        Array.Clear(r, 0, r.Length);
                                    }
                                    if (ch == 1)
                                    {
                                        l[1] = l[0];
                                        r[1] = r[0];
                                    }
                                    delta(l, r, l);
                                    writerOutput.WritePoints(l, 0, ch);
                                }
                            }
                            finally
                            {
                                if (writerOutput != null)
                                {
                                    writerOutput.Dispose();
                                }
                                if (readerRight != null)
                                {
                                    readerRight.Dispose();
                                }
                                if (readerLeft != null)
                                {
                                    readerLeft.Dispose();
                                }
                            }
                        }
                    }
                }
                catch (FileNotFoundException exception)
                {
                }
            }
        }

        private static void Delta(float[] l, float[] r, float[] y)
        {
            y[0] = l[0] - r[0];
            y[1] = l[1] - r[1];
        }

        private static void LRAudition(float[] l, float[] r, float[] y)
        {
            float ll = .5f * (l[0] + l[1]);
            float rr = .5f * (r[0] + r[1]);
            y[0] = ll;
            y[1] = rr;
        }

        private class AsyncPlaybackTestTask : IDisposable
        {
#if true // prevents "Add New Data Source..." from working
            private SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments> synthParams;
            private OutputGeneric<OutputSelectableFileDestination, SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>, OutputSelectableFileArguments> state;
#endif
            private MainWindow mainWindow;

            public AsyncPlaybackTestTask(
                string logHeader,
                string source,
                string options,
                string target,
                string traceScheduleLogPath)
            {
                mainWindow = new MainWindow(new Document(source), source);
                mainWindow.Show();
                Start(
                    logHeader,
                    mainWindow,
                    options,
                    target,
                    false/*modal*/,
                    traceScheduleLogPath,
                    this/*asyncObj*/);
            }

            public static void Start(
                string logHeader,
                MainWindow mainWindow,
                string options,
                string target,
                bool modal,
                string traceScheduleLogPath,
                AsyncPlaybackTestTask obj)
            {
                int samplingRate = mainWindow.Document.SamplingRate;
                int envelopeUpdateRate = mainWindow.Document.EnvelopeUpdateRate;
                int oversampling = mainWindow.Document.Oversampling;
                bool showSummary = mainWindow.Document.ShowSummary;
                int? randomSeed = mainWindow.Document.Deterministic ? new Nullable<int>(mainWindow.Document.Seed) : null;
                NumChannelsType numChannels = mainWindow.Document.NumChannels;
                NumBitsType numBits = mainWindow.Document.OutputNumBits;
                bool clipWarning = mainWindow.Document.ClipWarning;
                LargeBCDType defaultBPM = (LargeBCDType)mainWindow.Document.DefaultBeatsPerMinute;
                double overallVolumeScalingFactor = mainWindow.Document.OverallVolumeScalingFactor;
                LargeBCDType scanningGap = (LargeBCDType)mainWindow.Document.ScanningGap;
                bool perfCounters = true;
                int? concurrency = null;
                List<long> breakFrames = new List<long>();
                bool traceSchedule = false;
                Synthesizer.AutomationSettings.TraceFlags traceFlags = Synthesizer.AutomationSettings.TraceFlags.None;
                foreach (string param in options.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = param.Split('=');
                    switch (parts[0].ToLowerInvariant())
                    {
                        default:
                            throw new ArgumentException();
                        case "samplingrate":
                            samplingRate = Int32.Parse(parts[1]);
                            break;
                        case "enveloperate":
                            envelopeUpdateRate = Int32.Parse(parts[1]);
                            break;
                        case "oversampling":
                            oversampling = Int32.Parse(parts[1]);
                            break;
                        case "showsummary":
                            showSummary = Boolean.Parse(parts[1]);
                            break;
                        case "randomseed":
                            if (String.Equals(parts[1], "null", StringComparison.OrdinalIgnoreCase))
                            {
                                randomSeed = null;
                            }
                            else
                            {
                                randomSeed = Int32.Parse(parts[1]);
                            }
                            break;
                        case "clipwarning":
                            clipWarning = Boolean.Parse(parts[1]);
                            break;
                        case "perfcounters":
                            perfCounters = Boolean.Parse(parts[1]);
                            break;
                        case "concurrency":
                            if (String.Equals(parts[1], "null", StringComparison.OrdinalIgnoreCase))
                            {
                                concurrency = null;
                            }
                            else
                            {
                                concurrency = Int32.Parse(parts[1]);
                            }
                            break;
                        case "breakframe":
                            breakFrames.Add(Int64.Parse(parts[1]));
                            break;
                        case "traceschedule":
                            traceSchedule = Boolean.Parse(parts[1]);
                            break;
                        case "trace2":
                            traceFlags = Synthesizer.AutomationSettings.TraceFlags.None;
                            foreach (string e in parts[1].Split(new char[] { ',' }))
                            {
                                traceFlags |= (Synthesizer.AutomationSettings.TraceFlags)Enum.Parse(
                                    typeof(Synthesizer.AutomationSettings.TraceFlags),
                                    e,
                                    true/*ignorecase*/);
                            }
                            break;
                    }
                }

                if (log != null)
                {
                    StringBuilder combined = new StringBuilder();
                    if (logHeader != null)
                    {
                        combined.AppendLine(logHeader);
                    }
                    combined.AppendLine(String.Format("        params:samplingRate={0};envelopeUpdateRate={1},oversampling={2},showSummary={3},randomSeed={5},numChannels={6},numBits={7},clipWarning={8},defaultBPM={9},overallVolumeScalingFactor={10},scanningGap={11},concurrency={12},breakFrames={13}", samplingRate, envelopeUpdateRate, oversampling, showSummary, null/*{4}-unused*/, randomSeed, numChannels, numBits, clipWarning, (double)defaultBPM, overallVolumeScalingFactor, (double)scanningGap, concurrency, breakFrames.Count != 0 ? "yes" : "no"));
                    log.Write(combined.ToString());
                    log.Flush();
                }

                List<TrackObjectRec> included = new List<TrackObjectRec>();
                foreach (TrackObjectRec track in mainWindow.Document.TrackList)
                {
                    if (track.IncludeThisTrackInFinalPlayback)
                    {
                        included.Add(track);
                    }
                }
#if true // prevents "Add New Data Source..." from working
                SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments> synthParams;
                OutputGeneric<OutputSelectableFileDestination, SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>, OutputSelectableFileArguments> state;
                state = SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.Do(
                    mainWindow.DisplayName,
                    delegate (out OutputSelectableFileDestination destination)
                    {
                        destination = new OutputSelectableFileDestination();
                        destination.path = target;
                        return true;
                    },
                    OutputSelectableFileDestinationHandler.CreateOutputSelectableFileDestinationHandler,
                    new OutputSelectableFileArguments(),
                    SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerMainLoop,
                    synthParams = new SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>(
                        mainWindow,
                        mainWindow.Document,
                        included,
                        included[0],
                        0,
                        samplingRate,
                        envelopeUpdateRate,
                        numChannels,
                        defaultBPM,
                        overallVolumeScalingFactor,
                        scanningGap,
                        numBits,
                        clipWarning,
                        oversampling,
                        showSummary,
                        false/*deterministic - no longer used*/,
                        randomSeed,
                        new Synthesizer.AutomationSettings(
                            perfCounters,
                            concurrency,
                            breakFrames.Count != 0 ? breakFrames.ToArray() : null,
                            traceSchedule ? traceScheduleLogPath : null,
                            traceFlags,
                            mainWindow.SavePath != null ? Path.GetFileName(mainWindow.SavePath) : null)),
                    SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerCompletion,
                    mainWindow,
                    numChannels,
                    numBits,
                    samplingRate,
                    oversampling,
                    true/*showProgressWindow*/,
                    modal/*modal*/);
                if (obj != null)
                {
                    obj.synthParams = synthParams;
                    obj.state = state;
                }
                if (modal)
                {
                    if (log != null)
                    {
                        log.WriteLine("      clipped sample points: {0}", state.clippedSampleCount);
                    }
                }
#endif

                // TODO: on completion, the interaction log / summary from synthesis is written to the
                // log file here using WriteLog(). The TextWriter is wrapped thread-safe and the text is
                // written as a single string, but there is no identifying information to relate the
                // summary to the source file it came from, which is confusing when using concurrent
                // execution.
            }

            public void Dispose()
            {
                if (state != null)
                {
                    state.Dispose();
                    state = null;
                }
                if (mainWindow != null)
                {
                    mainWindow.Close();
                    mainWindow = null;
                }
            }

            public bool Completed
            {
                get
                {
                    return synthParams.Completed;
                }
            }

            private const int Delay = 250;

            public static int WaitOne(AsyncPlaybackTestTask[] tasks, TextWriter log)
            {
                if (tasks.Length == 0)
                {
                    throw new ArgumentException();
                }

                while (true)
                {
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        bool dispose = false;
                        if ((tasks[i] == null) || (dispose = tasks[i].Completed))
                        {
                            if (dispose)
                            {
                                if (log != null)
                                {
                                    log.WriteLine("      clipped sample points: {0} [{1}]", tasks[i].state.clippedSampleCount, Path.GetFileName(tasks[i].state.destination.path));
                                }
                                tasks[i].Dispose();
                                tasks[i] = null;
                            }
                            return i;
                        }
                    }

                    // Must pump WinForms messages to keep UI responsive; WaitHandle.WaitAny() is insufficient
                    Application.DoEvents();
                    Thread.Sleep(Delay);
                }
            }

            public static void Drain(AsyncPlaybackTestTask[] tasks, TextWriter log)
            {
                if (tasks.Length == 0)
                {
                    return;
                }
                while (true)
                {
                    bool completed = true;
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        if (tasks[i] != null)
                        {
                            if (!tasks[i].Completed)
                            {
                                completed = false;
                                break;
                            }
                            else
                            {
                                if (log != null)
                                {
                                    log.WriteLine("      clipped sample points: {0} [{1}]", tasks[i].state.clippedSampleCount, Path.GetFileName(tasks[i].state.destination.path));
                                }
                                tasks[i].Dispose();
                                tasks[i] = null;
                            }
                        }
                    }
                    if (completed)
                    {
                        break;
                    }

                    // Must pump WinForms messages to keep UI responsive; WaitHandle.WaitAny() is insufficient
                    Application.DoEvents();
                    Thread.Sleep(Delay);
                }
            }
        }

        private static Assembly[] pluginAssemblies = new Assembly[0];
        public static Assembly[] PluginAssemblies { get { return pluginAssemblies; } }

        private const string PluginsSubdirectory = "plugins";
        private static void LoadPlugins()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), PluginsSubdirectory);
            string[] files;
            try
            {
                files = Directory.GetFiles(path, "*.dll");
            }
            catch (Exception)
            {
                return; // no plugins subdirectory - ok
            }

            List<Assembly> pluginAssembliesList = new List<Assembly>();
            List<KeyValuePair<string, Synthesizer.IPluggableProcessorFactory>> pluggableProcessorList = new List<KeyValuePair<string, Synthesizer.IPluggableProcessorFactory>>();
            foreach (string file in files)
            {
                try
                {
                    // TODO: SECURITY REVIEW
                    Assembly assembly = Assembly.Load(File.ReadAllBytes(file));
                    pluginAssembliesList.Add(assembly);

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (typeof(Synthesizer.IPluggableProcessorFactory).IsAssignableFrom(type))
                        {
                            try
                            {
                                ConstructorInfo constructor = type.GetConstructor(new Type[0]);
                                Synthesizer.IPluggableProcessorFactory factory = (Synthesizer.IPluggableProcessorFactory)constructor.Invoke(new object[0]);
                                pluggableProcessorList.Add(new KeyValuePair<string, Synthesizer.IPluggableProcessorFactory>(factory.ParserName, factory));
                            }
                            catch (Exception)
                            {
                                // TODO: report errors
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // TODO: report errors
                }
            }
            pluginAssemblies = pluginAssembliesList.ToArray();
            Synthesizer.ExternalPluggableProcessors = pluggableProcessorList.ToArray();
        }

        public static IntPtr hFont;
        public static PrivateFontCollection privateFonts = new PrivateFontCollection();
        public static FontFamily bravuraFamily;

        private static void LoadPrivateFonts()
        {
            int cFonts;
            hFont = GDI.AddFontMemResourceEx(
                Properties.Resources.Bravura,
                Properties.Resources.Bravura.Length,
                IntPtr.Zero,
                out cFonts);

            GCHandle pinFont = GCHandle.Alloc(Properties.Resources.Bravura, GCHandleType.Pinned);
            try
            {
                privateFonts.AddMemoryFont(pinFont.AddrOfPinnedObject(), Properties.Resources.Bravura.Length);
                Debug.Assert(privateFonts.Families.Length == 1);
                bravuraFamily = privateFonts.Families[0];
            }
            finally
            {
                pinFont.Free();
            }

            DirectWriteTextRenderer.RegisterPrivateMemoryFont(
                Properties.Resources.Bravura);
        }

        [STAThread]
        public static void Main(string[] args)
        {
            #region Debugger Attach Helper
            {
                bool waitDebugger = false;
                if ((args.Length > 0) && String.Equals(args[0], "-waitdebugger"))
                {
                    waitDebugger = true;
                    Array.Copy(args, 1, args, 0, args.Length - 1);
                    Array.Resize(ref args, args.Length - 1);
                }

                bool debuggerBreak = false;
                if ((args.Length > 0) && String.Equals(args[0], "-break"))
                {
                    debuggerBreak = true;
                    Array.Copy(args, 1, args, 0, args.Length - 1);
                    Array.Resize(ref args, args.Length - 1);
                }

                if (waitDebugger)
                {
                    while (!Debugger.IsAttached)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                if (debuggerBreak)
                {
                    Debugger.Break();
                }
            }
            #endregion

            LoadPrivateFonts();

            LoadSettings();
            Synthesizer.FFTW.StaticInitialization(Config.FFTWWisdom);

            LoadPlugins();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                try
                {
                    int concurrency = 0;
                    AsyncPlaybackTestTask[] tasks = new AsyncPlaybackTestTask[0];
                    Stack<string[]> stack = new Stack<string[]>();
                    List<MainWindow> windows = new List<MainWindow>();
                    Stopwatch timer = new Stopwatch();
                    string globalOptions = String.Empty;
                    bool pathUnicodeHack = false;
                    bool firstTime = true;
                    string traceScheduleLogPath = Environment.ExpandEnvironmentVariables(@"%TEMP%\schedule.log");
                    while (args.Length > 0)
                    {
                        int used;
                        if (firstTime && File.Exists(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[0]))))
                        {
                            string path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[0])));
                            MainWindow.TryOpenFilePath(path);
                            used = 1;
                        }
                        else
                        {
                            firstTime = false;
                            switch (args[0])
                            {
                                default:
                                    throw new ArgumentException(args[0]);
                                case "/pathunicodehack":
                                    // The pathunicodehack allows the caller to pass in escaped unicode characters using the
                                    // format _x****_ where each * is a hexadecimal digit. This is specifically to support
                                    // primitive test utilities (such as CLRProfiler) that don't pass space-containing quoted
                                    // program arguments through.
                                    pathUnicodeHack = Boolean.Parse(args[1]);
                                    used = 2;
                                    break;
                                case "/script":
                                    {
                                        string script = args[1];
                                        used = 2;
                                        ShiftArgs(used, ref args);
                                        stack.Push(args);
                                        List<string> args2 = new List<string>();
                                        foreach (string line2 in File.ReadAllLines(script))
                                        {
                                            string line = String.Concat(line2, " ");
                                            if (line.StartsWith("#"))
                                            {
                                                continue;
                                            }
                                            int i = 0, l = 0;
                                            bool q = false;
                                            while (i < line.Length)
                                            {
                                                if (line[i] == '"')
                                                {
                                                    q = !q;
                                                }
                                                if (!q && Char.IsWhiteSpace(line[i]))
                                                {
                                                    string arg = line.Substring(l, i - l).Trim();
                                                    if (arg.StartsWith("\"") && arg.EndsWith("\""))
                                                    {
                                                        arg = arg.Substring(1, arg.Length - 2);
                                                    }
                                                    if (!String.IsNullOrEmpty(arg))
                                                    {
                                                        args2.Add(arg);
                                                    }
                                                    l = i;
                                                }
                                                i++;
                                            }
                                        }
                                        args = args2.ToArray();
                                    }
                                    used = 0;
                                    break;
                                case "/quit":
                                    Application.Exit(); // exit is deferred until Application.Run() is called
                                    used = 1;
                                    break;
                                case "/log":
                                    if (log != null)
                                    {
                                        log.WriteLine();
                                        log.Close();
                                        log = null;
                                    }
                                    if (!String.Equals(args[1], "null", StringComparison.OrdinalIgnoreCase))
                                    {
                                        log = StreamWriter.Synchronized(new StreamWriter(Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[1]))), true/*append*/, Encoding.UTF8));
                                        log.WriteLine("Log Opened: {0} {1}", DateTime.Now, Process.GetCurrentProcess().MainModule.FileName);
                                        log.Flush();
                                    }
                                    used = 2;
                                    break;
                                case "/open":
                                    {
                                        if (log != null)
                                        {
                                            log.WriteLine("    /open \"{0}\"", args[1]);
                                            log.Flush();
                                        }
                                        string path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[1])));
                                        Document document = new Document(path);
                                        MainWindow window = new MainWindow(document, path);
                                        windows.Add(window);
                                        window.Show();
                                    }
                                    used = 2;
                                    break;
                                case "/new":
                                    {
                                        if (log != null)
                                        {
                                            log.WriteLine("    /new");
                                            log.Flush();
                                        }
                                        Document document = new Document();
                                        MainWindow window = new MainWindow(document, null);
                                        windows.Add(window);
                                        window.Show();
                                    }
                                    used = 1;
                                    break;
                                case "/close":
                                    if (log != null)
                                    {
                                        log.WriteLine("    /close \"{0}\"", windows[windows.Count - 1].SavePath);
                                        log.Flush();
                                    }
                                    windows[windows.Count - 1].Close();
                                    windows.RemoveAt(windows.Count - 1);
                                    used = 1;
                                    break;
                                case "/savecopyas":
                                    {
                                        if (log != null)
                                        {
                                            log.WriteLine("    /savecopyas \"{0}\"", args[1]);
                                            log.Flush();
                                        }
                                        string path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[1])));
                                        windows[windows.Count - 1].SaveCopyAs(path);
                                    }
                                    used = 2;
                                    break;
                                case "/starttimer":
                                    if (log != null)
                                    {
                                        log.WriteLine("    /starttimer");
                                        log.Flush();
                                    }
                                    timer.Reset();
                                    timer.Start();
                                    used = 1;
                                    break;
                                case "/stoptimer":
                                    timer.Stop();
                                    if (log != null)
                                    {
                                        log.WriteLine("  * /stoptimer - Elapsed time {0:0.000} seconds ({1})", timer.ElapsedMilliseconds / 1000d, timer.Elapsed);
                                        log.Flush();
                                    }
                                    used = 1;
                                    break;
                                case "/globaloptions":
                                    globalOptions = args[1];
                                    if (log != null)
                                    {
                                        log.WriteLine("    /globaloptions {0}", globalOptions);
                                        log.Flush();
                                    }
                                    used = 2;
                                    break;
                                case "/traceschedulelogpath":
                                    traceScheduleLogPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[1])));
                                    if (log != null)
                                    {
                                        log.WriteLine("    /traceschedulelogpath \"{0}\"", traceScheduleLogPath);
                                        log.Flush();
                                    }
                                    used = 2;
                                    break;
                                case "/playtofile":
                                    {
                                        string options = args[1];
                                        string target = Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[2])));

                                        if (log != null)
                                        {
                                            log.WriteLine("    /playtofile {0} \"{1}\"", options, target);
                                            log.Flush();
                                        }

                                        MainWindow mainWindow = windows[windows.Count - 1];
                                        AsyncPlaybackTestTask.Start(
                                            null/*logHeader*/,
                                            mainWindow,
                                            String.Concat(options, ";", globalOptions),
                                            target,
                                            true/*modal*/,
                                            traceScheduleLogPath,
                                            null/*asyncObj*/);
                                    }
                                    used = 3;
                                    break;
                                case "/concurrency":
                                    AsyncPlaybackTestTask.Drain(tasks, log);
                                    concurrency = Int32.Parse(Environment.ExpandEnvironmentVariables(args[1]));
                                    if (log != null)
                                    {
                                        log.WriteLine("    /concurrency {0}", concurrency);
                                        log.Flush();
                                    }
                                    tasks = new AsyncPlaybackTestTask[concurrency];
                                    used = 2;
                                    break;
                                case "/drain":
                                    if (log != null)
                                    {
                                        log.WriteLine("    /drain");
                                        log.Flush();
                                    }
                                    AsyncPlaybackTestTask.Drain(tasks, log);
                                    used = 1;
                                    break;
                                case "/generateconcurrent":
                                    {
                                        string source = Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[1])));
                                        string options = args[2];
                                        string target = Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[3])));

                                        string logHeader = String.Format("    /generateconcurrent \"{0}\" {1} \"{2}\"", source, options, target);

                                        int slot = AsyncPlaybackTestTask.WaitOne(tasks, log);

                                        tasks[slot] = new AsyncPlaybackTestTask(
                                            logHeader,
                                            source,
                                            String.Concat(options, ";", globalOptions),
                                            target,
                                            traceScheduleLogPath);
                                    }
                                    used = 4;
                                    break;
                                case "/priority":
                                    if (log != null)
                                    {
                                        log.WriteLine("    /priority {0}", args[1]);
                                        log.Flush();
                                    }
                                    {
                                        // one of: Idle, BelowNormal, Normal, AboveNormal, High, RealTime
                                        ProcessPriorityClass priorityClass = (ProcessPriorityClass)Enum.Parse(typeof(ProcessPriorityClass), args[1]);
                                        Process.GetCurrentProcess().PriorityClass = priorityClass;
                                    }
                                    used = 2;
                                    break;
                                case "/audiocompare":
                                    {
                                        string options = args[1];
                                        string one = Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[2])));
                                        string two = Path.GetFullPath(Environment.ExpandEnvironmentVariables(DecodePathUnicodeHack(pathUnicodeHack, args[3])));

                                        if (log != null)
                                        {
                                            log.WriteLine("    /audiocompare {0} \"{1}\" \"{2}\"", options, one, two);
                                            log.Flush();
                                        }

                                        int sigbits = 16;
                                        float? autoAlign = null;
                                        long limit = 0;
                                        bool histogram = false;
                                        int ignoreDriftUnder = 0;
                                        bool writeDiff = false;
                                        bool writeAudition = false;
                                        foreach (string option in options.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                        {
                                            string[] parts = option.Split('=');
                                            if (parts.Length != 2)
                                            {
                                                throw new ArgumentException();
                                            }
                                            switch (parts[0].ToLowerInvariant())
                                            {
                                                default:
                                                    throw new ArgumentException();
                                                case "sigbits":
                                                    sigbits = Int32.Parse(parts[1]);
                                                    break;
                                                case "autoalign":
                                                    autoAlign = Single.Parse(parts[1]);
                                                    break;
                                                case "limit":
                                                    if (String.Equals(parts[1], "none", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        limit = Int64.MaxValue;
                                                    }
                                                    else
                                                    {
                                                        limit = Int64.Parse(parts[1]);
                                                    }
                                                    break;
                                                case "histo":
                                                case "histogram":
                                                    histogram = Boolean.Parse(parts[1]);
                                                    limit = Int64.MaxValue;
                                                    break;
                                                case "ignoredriftunder":
                                                    ignoreDriftUnder = Int32.Parse(parts[1]);
                                                    break;
                                                case "writediff":
                                                    writeDiff = Boolean.Parse(parts[1]);
                                                    break;
                                                case "writeaudition":
                                                    writeAudition = Boolean.Parse(parts[1]);
                                                    break;
                                            }
                                        }

                                        int skipLeft, skipRight;
                                        CompareAudioFiles(
                                            one,
                                            two,
                                            sigbits,
                                            autoAlign,
                                            ignoreDriftUnder,
                                            limit,
                                            histogram,
                                            log,
                                            out skipLeft,
                                            out skipRight);
                                        if (writeDiff)
                                        {
                                            WriteAudioFileDelta(
                                                one,
                                                two,
                                                Path.Combine(Path.GetDirectoryName(two), Path.GetFileNameWithoutExtension(two) + "-DELTA.AIF"),
                                                skipLeft,
                                                skipRight,
                                                Delta);
                                        }
                                        if (writeAudition)
                                        {
                                            WriteAudioFileDelta(
                                                one,
                                                two,
                                                Path.Combine(Path.GetDirectoryName(two), Path.GetFileNameWithoutExtension(two) + "-LRAUDITION.AIF"),
                                                skipLeft,
                                                skipRight,
                                                LRAudition);
                                        }
                                    }
                                    used = 4;
                                    break;
                            }
                        }
                        ShiftArgs(used, ref args);
                        if ((args.Length == 0) && (stack.Count != 0))
                        {
                            args = stack.Pop();
                        }
                    }
                }
                catch (Exception exception)
                {
                    WriteLog("Main", exception.ToString());
                    throw;
                }
                finally
                {
                    if (log != null)
                    {
                        log.WriteLine("    (command processing ended normally)");
                        log.WriteLine();
                        log.Close();
                    }
                }
            }
            else
            {
                Document document = new Document();
                new MainWindow(document, null, true/*firstWindow*/).Show();
            }

            Application.Idle += new EventHandler(Application_Idle);
            Application.Run();

            MyTextRenderer.FinalizeBeforeShutdown();
        }

        private static void Application_Idle(object sender, EventArgs e)
        {
            if (Application.OpenForms.Count == 0)
            {
                Application.Exit();
            }
        }
    }
}
