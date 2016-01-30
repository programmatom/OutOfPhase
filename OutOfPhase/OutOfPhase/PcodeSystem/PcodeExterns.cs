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
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public class PcodeExterns : PcodeSystem.IEvaluationContext
    {
        private readonly MainWindow mainWindow;
        private bool interaction;

        public PcodeExterns(MainWindow mainWindow)
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
                InteractionWindow interaction = mainWindow.GetInteractionWindow();
                interaction.Append(text);
            }
            else
            {
                // UI interaction must be marshaled to the main UI thread
                // (see https://msdn.microsoft.com/en-us/library/ms171728%28v=vs.80%29.aspx)
                interaction = true;
                MainWindow.GetInteractionWindowDelegate d
                    = new MainWindow.GetInteractionWindowDelegate(mainWindow.GetInteractionWindow);
                object o = mainWindow.Invoke(d, new object[] { });
                InteractionWindow interactionWindow = (InteractionWindow)o;
                InteractionWindow.AppendDelegate e
                    = new InteractionWindow.AppendDelegate(interactionWindow.Append);
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


        private ArrayHandleFloat VectorSquare(
            ArrayHandleFloat vector,
            int start,
            int length)
        {
            for (int i = 0; i < length; i++)
            {
                vector.floats[i + start] = vector.floats[i + start] * vector.floats[i + start];
            }
            return vector;
        }

        private ArrayHandleFloat VectorSquareRoot(
            ArrayHandleFloat vector,
            int start,
            int length)
        {
            for (int i = 0; i < length; i++)
            {
                vector.floats[i + start] = (float)Math.Sqrt(vector.floats[i + start]);
            }
            return vector;
        }


        private ArrayHandleFloat FirstOrderLowpass(
            ArrayHandleFloat vector,
            int start,
            int length,
            double samplingRate,
            double cutoff)
        {
            Synthesizer.FirstOrderLowpassRec Filter = new Synthesizer.FirstOrderLowpassRec();

            Synthesizer.FirstOrderLowpassRec.SetFirstOrderLowpassCoefficients(
                Filter,
                cutoff,
                samplingRate);

            // this is not a performance-critical code path but the filter function requires vector alignment.
            using (Synthesizer.AlignedWorkspace aligned = new Synthesizer.AlignedWorkspace(length))
            {
                Synthesizer.FloatVectorCopyUnaligned(vector.floats, start, aligned.Base, aligned.Offset, length);
                Synthesizer.FirstOrderLowpassRec.ApplyFirstOrderLowpassVectorModify(
                    Filter,
                    aligned.Base,
                    aligned.Offset,
                    length);
                Synthesizer.FloatVectorCopyUnaligned(aligned.Base, aligned.Offset, vector.floats, start, length);
            }

            return vector;
        }

        private ArrayHandleFloat ButterworthBandpass(
            ArrayHandleFloat vector,
            int start,
            int length,
            double samplingRate,
            double cutoff,
            double bandwidth)
        {
            Synthesizer.ButterworthBandpassRec Filter = new Synthesizer.ButterworthBandpassRec();

            Synthesizer.ButterworthBandpassRec.SetButterworthBandpassCoefficients(
                Filter,
                cutoff,
                bandwidth,
                samplingRate);

            // this is not a performance-critical code path but the filter function requires vector alignment.
            using (Synthesizer.AlignedWorkspace aligned = new Synthesizer.AlignedWorkspace(length))
            {
                Synthesizer.FloatVectorCopyUnaligned(vector.floats, start, aligned.Base, aligned.Offset, length);
                Synthesizer.ButterworthBandpassRec.ApplyButterworthBandpassVectorModify(
                    Filter,
                    aligned.Base,
                    aligned.Offset,
                    length);
                Synthesizer.FloatVectorCopyUnaligned(aligned.Base, aligned.Offset, vector.floats, start, length);
            }

            return vector;
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
                if (SecurePathCombine(Program.ConfigDirectory, name, out path2)
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


        // pcode-managed interop helpers

        public static EvalErrors TypeCheckValue(object value, DataTypes type)
        {
            switch (type)
            {
                default:
                    Debug.Assert(false); // an unknown data type is a bug on our caller's part
                    return EvalErrors.eEvalFunctionSignatureMismatch;
                case DataTypes.eBoolean:
                    if (!(value is Boolean))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eInteger:
                    if (!(value is Int32))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eFloat:
                    if (!(value is Single))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eDouble:
                    if (!(value is Double))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                    if ((value != null) && !(value is ArrayHandleByte))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eArrayOfInteger:
                    if ((value != null) && !(value is ArrayHandleInt32))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eArrayOfFloat:
                    if ((value != null) && !(value is ArrayHandleFloat))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eArrayOfDouble:
                    if ((value != null) && !(value is ArrayHandleDouble))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
            }
            return EvalErrors.eEvalNoError;
        }

        private static EvalErrors TypeCheckSignature(object[] args, DataTypes[] argsTypes)
        {
            int argsLength = args != null ? args.Length : 0;
            if (argsLength != argsTypes.Length)
            {
                return EvalErrors.eEvalFunctionSignatureMismatch;
            }
            for (int i = 0; i < argsLength; i++)
            {
                EvalErrors error = TypeCheckValue(args[i], argsTypes[i]);
                if (error != EvalErrors.eEvalNoError)
                {
                    return error;
                }
            }
            return EvalErrors.eEvalNoError;
        }

        public static void MarshalToManaged(ref StackElement value, DataTypes type, out object managed)
        {
            switch (type)
            {
                default:
                    Debug.Assert(false); // an unknown data type is a bug on our caller's part
                    throw new ArgumentException();
                case DataTypes.eBoolean:
                    managed = value.Data.Integer != 0;
                    break;
                case DataTypes.eInteger:
                    managed = value.Data.Integer;
                    break;
                case DataTypes.eFloat:
                    managed = value.Data.Float;
                    break;
                case DataTypes.eDouble:
                    managed = value.Data.Double;
                    break;
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                    managed = value.reference.arrayHandleByte;
                    break;
                case DataTypes.eArrayOfInteger:
                    managed = value.reference.arrayHandleInt32;
                    break;
                case DataTypes.eArrayOfFloat:
                    managed = value.reference.arrayHandleFloat;
                    break;
                case DataTypes.eArrayOfDouble:
                    managed = value.reference.arrayHandleDouble;
                    break;
            }
        }

        public static void MarshalToPcode(object value, ref StackElement pcode, DataTypes type)
        {
#if DEBUG
            pcode.AssertClear();
#endif
            switch (type)
            {
                default:
                    Debug.Assert(false); // an unknown data type is a bug on our caller's part
                    throw new ArgumentException();
                case DataTypes.eBoolean:
                    pcode.Data.Integer = (bool)value ? 1 : 0;
                    break;
                case DataTypes.eInteger:
                    pcode.Data.Integer = (int)value;
                    break;
                case DataTypes.eFloat:
                    pcode.Data.Float = (float)value;
                    break;
                case DataTypes.eDouble:
                    pcode.Data.Double = (double)value;
                    break;
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                    pcode.reference.arrayHandleByte = (ArrayHandleByte)value;
                    break;
                case DataTypes.eArrayOfInteger:
                    pcode.reference.arrayHandleInt32 = (ArrayHandleInt32)value;
                    break;
                case DataTypes.eArrayOfFloat:
                    pcode.reference.arrayHandleFloat = (ArrayHandleFloat)value;
                    break;
                case DataTypes.eArrayOfDouble:
                    pcode.reference.arrayHandleDouble = (ArrayHandleDouble)value;
                    break;
            }
        }
    }
}
