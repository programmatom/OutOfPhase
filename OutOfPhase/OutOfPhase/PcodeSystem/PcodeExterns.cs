/*
 *  Copyright � 1994-2002, 2015-2016 Thomas R. Lawrence
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
using System.Reflection;

namespace OutOfPhase
{
    public class PcodeExternsNull : PcodeSystem.IEvaluationContext
    {
        public static readonly PcodeExternsNull Default = new PcodeExternsNull();

        public EvalErrors Invoke(string name, object[] args, out object returnValue)
        {
            returnValue = null;
            return EvalErrors.eEvalUndefinedFunction;
        }
    }

    public class PcodeExterns : PcodeSystem.IEvaluationContext
    {
        private readonly IMainWindowServices mainWindow;
        private bool interaction;

        public PcodeExterns(IMainWindowServices mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public class EvalErrorException : Exception
        {
            private readonly EvalErrors error;

            public EvalErrorException(EvalErrors error)
            {
                this.error = error;
            }

            public EvalErrors Error { get { return error; } }
        }

        // SECURITY: TODO: review - invoke is compiler-generated at this point. If it is ever opened up to user-specified
        // names, there needs to be some security checks to prevent invocation of unintended methods on this object.
        public EvalErrors Invoke(string name, object[] args, out object returnValue)
        {
            returnValue = null;

            MethodInfo methodInfo = this.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (methodInfo == null)
            {
                return EvalErrors.eEvalUndefinedFunction;
            }

            try
            {
                returnValue = methodInfo.Invoke(this, args);
            }
            catch (ArgumentException)
            {
                Debug.Assert(false); // code generator shouldn't permit this
                return EvalErrors.eEvalFunctionSignatureMismatch;
            }
            catch (TargetInvocationException exception)
            {
                if (exception.InnerException is EvalErrorException)
                {
                    return ((EvalErrorException)exception.InnerException).Error;
                }
                else if (exception.InnerException is OutOfMemoryException)
                {
                    return EvalErrors.eEvalOutOfMemory;
                }
                else if (exception.InnerException is DivideByZeroException)
                {
                    return EvalErrors.eEvalDivideByZero;
                }
                else if (exception.InnerException is NullReferenceException)
                {
                    return EvalErrors.eEvalArrayDoesntExist;
                }
                else if (exception.InnerException is IndexOutOfRangeException)
                {
                    return EvalErrors.eEvalArraySubscriptOutOfRange;
                }
                else
                {
                    Debug.Assert(false, "unhandled exception from target code", exception.ToString());
                    throw;
                }
            }

            return EvalErrors.eEvalNoError;
        }

        public bool Interaction { get { return interaction; } set { interaction = value; } }


        // methods


        private bool ErrorTrap(
            ArrayHandleByte messageUtf8,
            bool resumable)
        {
            if (!resumable)
            {
                throw new EvalErrorException(EvalErrors.eEvalErrorTrapEncountered);
            }
            if (!mainWindow.InvokeRequired)
            {
                bool resume = mainWindow.PromptResumableError(messageUtf8.strings);
                if (!resume)
                {
                    throw new EvalErrorException(EvalErrors.eEvalErrorTrapEncountered);
                }
            }
            else
            {
                // UI interaction must be marshaled to the main UI thread
                // (see https://msdn.microsoft.com/en-us/library/ms171728%28v=vs.80%29.aspx)
                interaction = true;
                MainWindow.PromptResumableErrorDelegate d
                    = new MainWindow.PromptResumableErrorDelegate(mainWindow.PromptResumableError);
                object o = mainWindow.Invoke(d, new object[] { messageUtf8.strings });
                bool resume = (bool)o;
                if (!resume)
                {
                    throw new EvalErrorException(EvalErrors.eEvalErrorTrapEncountered);
                }
            }
            return true;
        }


        private void PrintStringHelper(
            string text)
        {
            if (!mainWindow.InvokeRequired)
            {
                IInteractionWindowService interaction = mainWindow.GetInteractionWindow();
                interaction.Append(text);
            }
            else
            {
                // UI interaction must be marshaled to the main UI thread
                // (see https://msdn.microsoft.com/en-us/library/ms171728%28v=vs.80%29.aspx)
                interaction = true;
                MainWindowGetInteractionWindowDelegate d = new MainWindowGetInteractionWindowDelegate(mainWindow.GetInteractionWindow);
                object o = mainWindow.Invoke(d, new object[] { });
                IInteractionWindowService interactionWindow = (IInteractionWindowService)o;
                InteractionWindowAppendDelegate e = new InteractionWindowAppendDelegate(interactionWindow.Append);
                interactionWindow.Invoke(e, new object[] { text });
            }
        }

        private bool PrintString(
            ArrayHandleByte text)
        {
            PrintStringHelper(text.strings);
            return true;
        }

        private bool PrintBool(
            bool value)
        {
            PrintStringHelper(value ? "true" : "false");
            return value;
        }

        private double PrintDouble(
            double value)
        {
            PrintStringHelper(value.ToString());
            return value;
        }


        private ArrayHandleFloat GetSampleLeftArray(
            ArrayHandleByte sampleName)
        {
            string name = sampleName.strings;
            SampleObjectRec sample = mainWindow.Document.SampleList.Find(
                delegate (SampleObjectRec candidate) { return String.Equals(name, candidate.Name); });
            if (sample == null)
            {
                throw new EvalErrorException(EvalErrors.eEvalGetSampleNotDefined);
            }
            if (sample.NumChannels != NumChannelsType.eSampleStereo)
            {
                throw new EvalErrorException(EvalErrors.eEvalGetSampleWrongChannelType);
            }
            return new ArrayHandleFloat(sample.SampleData.CopyRawData(ChannelType.eLeftChannel));
        }

        private ArrayHandleFloat GetSampleRightArray(
            ArrayHandleByte sampleName)
        {
            string name = sampleName.strings;
            SampleObjectRec sample = mainWindow.Document.SampleList.Find(
                delegate (SampleObjectRec candidate) { return String.Equals(name, candidate.Name); });
            if (sample == null)
            {
                throw new EvalErrorException(EvalErrors.eEvalGetSampleNotDefined);
            }
            if (sample.NumChannels != NumChannelsType.eSampleStereo)
            {
                throw new EvalErrorException(EvalErrors.eEvalGetSampleWrongChannelType);
            }
            return new ArrayHandleFloat(sample.SampleData.CopyRawData(ChannelType.eRightChannel));
        }

        private ArrayHandleFloat GetSampleMonoArray(
            ArrayHandleByte sampleName)
        {
            string name = sampleName.strings;
            SampleObjectRec sample = mainWindow.Document.SampleList.Find(
                delegate (SampleObjectRec candidate) { return String.Equals(name, candidate.Name); });
            if (sample == null)
            {
                throw new EvalErrorException(EvalErrors.eEvalGetSampleNotDefined);
            }
            if (sample.NumChannels != NumChannelsType.eSampleMono)
            {
                throw new EvalErrorException(EvalErrors.eEvalGetSampleWrongChannelType);
            }
            return new ArrayHandleFloat(sample.SampleData.CopyRawData(ChannelType.eMonoChannel));
        }


        private ArrayHandleFloat GetWaveTableArray(
            ArrayHandleByte waveTableName)
        {
            string name = waveTableName.strings;
            WaveTableObjectRec waveTable = mainWindow.Document.WaveTableList.Find(
                delegate (WaveTableObjectRec candidate) { return String.Equals(name, candidate.Name); });
            if (waveTable == null)
            {
                throw new EvalErrorException(EvalErrors.eEvalGetSampleNotDefined);
            }
            return new ArrayHandleFloat(waveTable.WaveTableData.GetRawCopy());
        }

        private int GetWaveTableTables(
            ArrayHandleByte waveTableName)
        {
            string name = waveTableName.strings;
            WaveTableObjectRec waveTable = mainWindow.Document.WaveTableList.Find(
                delegate (WaveTableObjectRec candidate) { return String.Equals(name, candidate.Name); });
            if (waveTable == null)
            {
                throw new EvalErrorException(EvalErrors.eEvalGetSampleNotDefined);
            }
            return waveTable.NumTables;
        }

        private int GetWaveTableFrames(
            ArrayHandleByte waveTableName)
        {
            string name = waveTableName.strings;
            WaveTableObjectRec waveTable = mainWindow.Document.WaveTableList.Find(
                delegate (WaveTableObjectRec candidate) { return String.Equals(name, candidate.Name); });
            if (waveTable == null)
            {
                throw new EvalErrorException(EvalErrors.eEvalGetSampleNotDefined);
            }
            return waveTable.NumFrames;
        }


        private static bool SecurePathCombine(string prefix, string partial, out string result)
        {
            result = null;
            // treat all filesystem separator characters as equivalent, for document portability
            foreach (string part in partial.Split(new char[] { '\\', '/', ':' }))
            {
                string[] entries = Directory.GetDirectories(prefix);
                foreach (string entry in entries)
                {
                    string entryName = Path.GetFileName(entry);
                    if (String.Equals(entryName, part))
                    {
                        prefix = Path.Combine(prefix, entryName);
                        goto Next;
                    }
                }
                return false;
            Next:
                ;
            }
            result = prefix;
            return true;
        }

        private ArrayHandleFloat LoadSampleHelper(
            ChannelType channel,
            string type,
            string name)
        {
            if (!String.Equals(type, String.Empty)
                && !String.Equals(type, "WAV")
                && !String.Equals(type, "AIFF"))
            {
                throw new EvalErrorException(EvalErrors.eEvalUnableToImportFile);
            }

            // SECURITY: ensure navigation to user-provided path does not escape from the permitted root directories

            string path = null;
            if (mainWindow.SavePath != null)
            {
                string path2;
                if (SecurePathCombine(Path.GetDirectoryName(mainWindow.SavePath), name, out path2)
                    && File.Exists(path2))
                {
                    path = path2;
                }
            }
            if (path == null)
            {
                string path2;
                if (SecurePathCombine(Program.GetSettingsDirectory(false/*create*/, true/*roaming*/), name, out path2)
                    && File.Exists(path2))
                {
                    path = path2;
                }
            }
            if (path == null)
            {
                throw new EvalErrorException(EvalErrors.eEvalUnableToImportFile);
            }

            ArrayHandleFloat array = null;
            try
            {
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
                {
                    using (AudioFileReader reader = Program.TryGetAudioReader(stream))
                    {
                        if (reader == null)
                        {
                            throw new NullReferenceException(); // neither AIFF nor WAV
                        }

                        if ((reader.NumChannels == NumChannelsType.eSampleMono)
                            != (channel == ChannelType.eMonoChannel))
                        {
                            throw new ArgumentException(); // file does not have specified channel
                        }

                        array = new ArrayHandleFloat(new float[reader.NumFrames]);
                        float[] buffer = new float[4096];
                        int c;
                        int p = 0;
                        while ((c = reader.ReadPoints(buffer, 0, buffer.Length)) != 0)
                        {
                            switch (channel)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new ArgumentException();
                                case ChannelType.eLeftChannel:
                                    for (int i = 0; i < c; i += 2)
                                    {
                                        array.floats[p++] = buffer[i + 0];
                                    }
                                    break;
                                case ChannelType.eRightChannel:
                                    for (int i = 0; i < c; i += 2)
                                    {
                                        array.floats[p++] = buffer[i + 1];
                                    }
                                    break;
                                case ChannelType.eMonoChannel:
                                    for (int i = 0; i < c; i++)
                                    {
                                        array.floats[p++] = buffer[i];
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw new EvalErrorException(EvalErrors.eEvalUnableToImportFile);
            }
            return array;
        }

        private ArrayHandleFloat LoadSampleLeftArray(
            ArrayHandleByte type,
            ArrayHandleByte name)
        {
            return LoadSampleHelper(ChannelType.eLeftChannel, type.strings, name.strings);
        }

        private ArrayHandleFloat LoadSampleRightArray(
            ArrayHandleByte type,
            ArrayHandleByte name)
        {
            return LoadSampleHelper(ChannelType.eRightChannel, type.strings, name.strings);
        }

        private ArrayHandleFloat LoadSampleMonoArray(
            ArrayHandleByte type,
            ArrayHandleByte name)
        {
            return LoadSampleHelper(ChannelType.eMonoChannel, type.strings, name.strings);
        }
    }
}
