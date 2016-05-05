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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace OutOfPhase
{
    public static partial class Import
    {
        public enum SignModeType
        {
            [Description("Signed")]
            eSignSigned,
            [Description("Unsigned")]
            eSignUnsigned,
            [Description("Sign-bit")]
            eSignSignBit,
        }

        public enum EndianType
        {
            [Description("Little Endian")]
            eEndianLittle,
            [Description("Big Endian")]
            eEndianBig,
        }

        public enum ImportBits
        {
            [Description("8 bit")]
            eSample8bit,
            [Description("16 bit")]
            eSample16bit,
            [Description("24 bit")]
            eSample24bit,
        }

        public class ImportRawSettings : HierarchicalBindingRoot
        {
            private string _Name = String.Empty;
            public const string Name_PropertyName = "Name";
            [Bindable(true)]
            public string Name { get { return _Name; } set { Patch(value, ref _Name, Name_PropertyName); } }

            private ImportBits _NumBits = ImportBits.eSample16bit;
            public const string NumBits_PropertyName = "NumBits";
            public static Enum[] NumBitsAllowedValues { get { return EnumUtility.GetValues(ImportBits.eSample8bit.GetType()); } }
            [Bindable(true)]
            public ImportBits NumBits { get { return _NumBits; } set { PatchObject(value, ref _NumBits, NumBits_PropertyName); } }
            [Bindable(true)]
            public string NumBitsAsString { get { return EnumUtility.GetDescription(_NumBits); } set { string old = EnumUtility.GetDescription(_NumBits); _NumBits = (ImportBits)EnumUtility.GetValue(ImportBits.eSample8bit.GetType(), value); PatchObject(value, ref old, NumBits_PropertyName); } }

            private NumChannelsType _NumChannels = NumChannelsType.eSampleStereo;
            public const string NumChannels_PropertyName = "NumChannels";
            public static Enum[] NumChannelsAllowedValues { get { return EnumUtility.GetValues(NumChannelsType.eSampleMono.GetType()); } }
            [Bindable(true)]
            public NumChannelsType NumChannels { get { return _NumChannels; } set { PatchObject(value, ref _NumChannels, NumChannels_PropertyName); } }
            [Bindable(true)]
            public string NumChannelsAsString { get { return EnumUtility.GetDescription(_NumChannels); } set { string old = EnumUtility.GetDescription(_NumChannels); _NumChannels = (NumChannelsType)EnumUtility.GetValue(NumChannelsType.eSampleMono.GetType(), value); PatchObject(value, ref old, NumChannels_PropertyName); } }

            private SignModeType _SignMode = SignModeType.eSignSigned;
            public const string SignMode_PropertyName = "SignMode";
            public static Enum[] SignModeAllowedValues { get { return EnumUtility.GetValues(SignModeType.eSignSignBit.GetType()); } }
            [Bindable(true)]
            public SignModeType SignMode { get { return _SignMode; } set { PatchObject(value, ref _SignMode, SignMode_PropertyName); } }
            [Bindable(true)]
            public string SignModeAsString { get { return EnumUtility.GetDescription(_SignMode); } set { string old = EnumUtility.GetDescription(_SignMode); _SignMode = (SignModeType)EnumUtility.GetValue(SignModeType.eSignSigned.GetType(), value); PatchObject(value, ref old, SignMode_PropertyName); } }

            private EndianType _Endianness = EndianType.eEndianLittle;
            public const string Endianness_PropertyName = "Endianness";
            public static Enum[] EndiannessAllowedValues { get { return EnumUtility.GetValues(EndianType.eEndianLittle.GetType()); } }
            [Bindable(true)]
            public EndianType Endianness { get { return _Endianness; } set { PatchObject(value, ref _Endianness, Endianness_PropertyName); } }
            [Bindable(true)]
            public string EndiannessAsString { get { return EnumUtility.GetDescription(_Endianness); } set { string old = EnumUtility.GetDescription(_Endianness); _Endianness = (EndianType)EnumUtility.GetValue(EndianType.eEndianLittle.GetType(), value); PatchObject(value, ref old, Endianness_PropertyName); } }

            private int _InitialSkip;
            public const string InitialSkip_PropertyName = "InitialSkip";
            [Bindable(true)]
            public int InitialSkip { get { return _InitialSkip; } set { Patch(value, ref _InitialSkip, InitialSkip_PropertyName); } }

            private int _FramePadding;
            public const string FramePadding_PropertyName = "FramePadding";
            [Bindable(true)]
            public int FramePadding { get { return _FramePadding; } set { Patch(value, ref _FramePadding, FramePadding_PropertyName); } }
        }

        // nonpersisted (for now), but reused within session
        private static ImportRawSettings SessionSettings = new ImportRawSettings();

        public static void TryImportRaw(
            string path,
            int? truncate,
            int initialSkip,
            int framePadding,
            NumChannelsType channels,
            ImportBits bits,
            SignModeType signing,
            EndianType endianness,
            out float[] data,
            out int NumFrames,
            out bool endedOnFrameBoundary)
        {
            data = null;

            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
            {
                int channelCount = (channels == NumChannelsType.eSampleStereo ? 2 : 1);
                int bytesPerPoint;
                switch (bits)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case ImportBits.eSample8bit:
                        bytesPerPoint = 1;
                        break;
                    case ImportBits.eSample16bit:
                        bytesPerPoint = 2;
                        break;
                    case ImportBits.eSample24bit:
                        bytesPerPoint = 3;
                        break;
                }
                int frameLength = channelCount * bytesPerPoint + framePadding;

                {
                    long framesL = ((stream.Length - initialSkip) + (frameLength - 1)) / frameLength;
                    if (framesL > Int32.MaxValue)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    NumFrames = (int)framesL;
                }
                endedOnFrameBoundary = NumFrames * frameLength + initialSkip == stream.Length;
                if (truncate.HasValue)
                {
                    NumFrames = Math.Min(NumFrames, truncate.Value);
                }

                data = new float[(NumFrames + 1) * channelCount];

                stream.Position = initialSkip;

                TranslateMethod translate;
                switch (bits)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case ImportBits.eSample8bit:
                        translate = Translate_8bit;
                        break;
                    case ImportBits.eSample16bit:
                        switch (endianness)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case EndianType.eEndianBig:
                                translate = Translate_16bit_big;
                                break;
                            case EndianType.eEndianLittle:
                                translate = Translate_16bit_little;
                                break;
                        }
                        break;
                    case ImportBits.eSample24bit:
                        switch (endianness)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case EndianType.eEndianBig:
                                translate = Translate_24bit_big;
                                break;
                            case EndianType.eEndianLittle:
                                translate = Translate_24bit_little;
                                break;
                        }
                        break;
                }

                ResignMethod resign;
                switch (signing)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case SignModeType.eSignSigned:
                        resign = Resign_Signed;
                        break;
                    case SignModeType.eSignUnsigned:
                        resign = Resign_Unsigned;
                        break;
                    case SignModeType.eSignSignBit:
                        resign = Resign_SignBit;
                        break;
                }

                bool terminate = false;
                for (int iFrame = 0; !terminate && iFrame < NumFrames; iFrame++)
                {
                    byte[] frame = new byte[frameLength]; // zeroed each time

                    int c = stream.Read(frame, 0, frameLength);
                    if (c != frameLength)
                    {
                        terminate = true;
                        Debug.Assert(!endedOnFrameBoundary);
                    }

                    switch (channels)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case NumChannelsType.eSampleMono:
                            data[iFrame] = Convert(resign(translate(frame, 0)));
                            break;
                        case NumChannelsType.eSampleStereo:
                            data[2 * iFrame + 0] = Convert(resign(translate(frame, 0)));
                            data[2 * iFrame + 1] = Convert(resign(translate(frame, bytesPerPoint)));
                            break;
                    }
                }
            }
        }

        private delegate uint TranslateMethod(byte[] frame, int offset);

        private delegate int ResignMethod(uint u);

        private static float Convert(int v)
        {
            return ((float)v / (float)0x0080000000);
        }

        private static int Resign_Signed(uint u)
        {
            return (int)u;
        }

        private static int Resign_Unsigned(uint u)
        {
            return unchecked((int)u - (int)0x80000000);
        }

        private static int Resign_SignBit(uint u)
        {
            return ((int)u & 0x7fffffff) * ((u & 0x80000000) != 0 ? -1 : 1);
        }

        private static uint Translate_8bit(byte[] frame, int offset)
        {
            return (uint)frame[offset] << 24;
        }

        private static uint Translate_16bit_big(byte[] frame, int offset)
        {
            return ((uint)frame[offset + 1] | ((uint)frame[offset + 0] << 8)) << 16;
        }

        private static uint Translate_16bit_little(byte[] frame, int offset)
        {
            return ((uint)frame[offset + 0] | ((uint)frame[offset + 1] << 8)) << 16;
        }

        private static uint Translate_24bit_big(byte[] frame, int offset)
        {
            return ((uint)frame[offset + 2] | ((uint)frame[offset + 1] << 8) | ((uint)frame[offset + 0] << 16)) << 8;
        }

        private static uint Translate_24bit_little(byte[] frame, int offset)
        {
            return ((uint)frame[offset + 0] | ((uint)frame[offset + 1] << 8) | ((uint)frame[offset + 2] << 16)) << 8;
        }

        public static void ImportRawSample(
            Document document,
            string path,
            int? truncate,
            ImportRawSettings settings,
            out SampleObjectRec ReturnedSampleObject)
        {
            float[] data;
            int frames;
            bool endedOnFrameBoundary;

            TryImportRaw(
                path,
                truncate,
                settings.InitialSkip,
                settings.FramePadding,
                settings.NumChannels,
                settings.NumBits,
                settings.SignMode,
                settings.Endianness,
                out data,
                out frames,
                out endedOnFrameBoundary);

            NumBitsType numBits;
            switch (settings.NumBits)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case ImportBits.eSample8bit:
                    numBits = NumBitsType.eSample8bit;
                    break;
                case ImportBits.eSample16bit:
                    numBits = NumBitsType.eSample16bit;
                    break;
                case ImportBits.eSample24bit:
                    numBits = NumBitsType.eSample24bit;
                    break;
            }

            const int DEFAULTSAMPLINGRATE = 44100;

            ReturnedSampleObject = new SampleObjectRec(
                document,
                data,
                frames,
                numBits,
                settings.NumChannels,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                DEFAULTSAMPLINGRATE,
                Constants.MIDDLEC);
            ReturnedSampleObject.Name = Path.GetFileName(path);
        }

        public static bool ImportRawSample(
            Registration registration,
            IMainWindowServices mainWindow,
            out int index)
        {
            index = -1;

            string path;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return false;
                }
                path = dialog.FileName;
            }

            SessionSettings.Name = Path.GetFileName(path);

            using (ImportRawSampleDialog dialog = new ImportRawSampleDialog(SessionSettings, path))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SampleObjectRec ReturnedSampleObject;
                    ImportRawSample(
                        mainWindow.Document,
                        path,
                        null/*truncate*/,
                        SessionSettings,
                        out ReturnedSampleObject);
                    index = mainWindow.Document.SampleList.Count;
                    mainWindow.Document.SampleList.Add(ReturnedSampleObject);

                    new SampleWindow(registration, ReturnedSampleObject, mainWindow).Show();

                    return true;
                }
            }

            return false;
        }
    }
}
