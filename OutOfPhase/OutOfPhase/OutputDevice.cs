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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace OutOfPhase
{
    // TODO: implement stream switch

    // WASAPI Info
    // WASAPI (Core Audio) is a low level interface and does not provide many niceties:
    // https://msdn.microsoft.com/en-us/library/dd370811%28v=vs.85%29.aspx


#if true // prevents "Add New Data Source..." from working
    public class OutputDeviceDestination
    {
    }

    public class OutputDeviceArguments
    {
        public readonly double BufferSeconds;
        public readonly string DeviceId;

        public OutputDeviceArguments(double bufferSeconds, string deviceId)
        {
            this.BufferSeconds = bufferSeconds;
            this.DeviceId = deviceId;
        }

        public OutputDeviceArguments(double bufferSeconds)
            : this(bufferSeconds, Program.Config.OutputDevice)
        {
        }
    }


    public static class OutputDeviceEnumerator
    {
        public static KeyValuePair<string, string>[] EnumerateAudioOutputDeviceIdentifiers()
        {
            List<KeyValuePair<string, string>> devices = new List<KeyValuePair<string, string>>();
            //devices.Add(new KeyValuePair<string, string>(ERole.eConsole.ToString(), "Default Console Device"));
            //devices.Add(new KeyValuePair<string, string>(ERole.eCommunications.ToString(), "Default Communications Device"));
            devices.Add(new KeyValuePair<string, string>(ERole.eLegacy.ToString(), "Default Legacy Multimedia Device"));
            devices.Add(new KeyValuePair<string, string>(ERole.eMultimedia.ToString(), "Default Multimedia Device"));

            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDeviceCollection deviceCollection;
            deviceEnumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE.DEVICE_STATEMASK_ALL, out deviceCollection);
            int count;
            deviceCollection.GetCount(out count);
            for (int i = 0; i < count; i++)
            {
                IMMDevice device;
                deviceCollection.Item(i, out device);
                string id;
                device.GetId(out id);
                string name;
                IPropertyStore props;
                device.OpenPropertyStore(STGM.STGM_READ, out props);
                DEVPROPKEY DEVPKEY_Device_FriendlyName = new DEVPROPKEY(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 14);
                PropVariant prop = new PropVariant();
                try
                {
                    int hr = props.GetValue(ref DEVPKEY_Device_FriendlyName, out prop);
                    name = (string)prop.Value;
                }
                catch (COMException)
                {
                    name = "(Unavailable)";
                }
                finally
                {
                    prop.Clear();
                }
                devices.Add(new KeyValuePair<string, string>(id, name));
            }

            return devices.ToArray();
        }

        public static bool OutputDeviceGetDestination(out OutputDeviceDestination destination)
        {
            destination = new OutputDeviceDestination();
            return true;
        }

        public static DestinationHandler<OutputDeviceDestination> CreateOutputDeviceDestinationHandler(
            OutputDeviceDestination destination,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            OutputDeviceArguments arguments)
        {
            if (String.Equals(arguments.DeviceId, ERole.eLegacy.ToString()))
            {
                return new OutputDeviceLegacyDestinationHandler(
                    channels,
                    bits,
                    samplingRate,
                    arguments);
            }
            else
            {
                return new OutputDeviceWASAPIDestinationHandler(
                    channels,
                    bits,
                    samplingRate,
                    arguments);
            }
        }
    }


    public class OutputDeviceWASAPIDestinationHandler : DestinationHandler<OutputDeviceDestination>, IBufferLoading
    {
        private readonly NumChannelsType channels;
        private readonly NumBitsType bits;
        private readonly int samplingRate;
        private readonly int pointsPerFrame;
        private readonly int pointsPerFrameDevice;

        private IAudioClient audioClient;
        private IAudioRenderClient renderClient;

        public OutputDeviceWASAPIDestinationHandler(
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            OutputDeviceArguments arguments)
        {
            int hr;

#if DEBUG
            if ((channels != NumChannelsType.eSampleMono)
                && (channels != NumChannelsType.eSampleStereo))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((bits != NumBitsType.eSample8bit)
                && (bits != NumBitsType.eSample16bit)
                && (bits != NumBitsType.eSample24bit))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            this.channels = channels;
            this.bits = bits;
            this.samplingRate = samplingRate;
            this.pointsPerFrame = channels == NumChannelsType.eSampleStereo ? 2 : 1;

            Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new Guid("00000003-0000-0010-8000-00aa00389b71");

            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice device;
            DEVICE_STATE state = DEVICE_STATE.DEVICE_STATE_NOTPRESENT;
            deviceEnumerator.GetDevice(arguments.DeviceId, out device);
            if (device != null)
            {
                device.GetState(out state);
            }
            if ((device == null) || (state != DEVICE_STATE.DEVICE_STATE_ACTIVE))
            {
                try
                {
                    ERole defaultDeviceRole = (ERole)Enum.Parse(typeof(ERole), arguments.DeviceId);
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, defaultDeviceRole, out device);
                }
                catch (Exception)
                {
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
                }
            }
            Guid IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
            object o;
            device.Activate(
                ref IID_IAudioClient,
                (int)CLSCTX.CLSCTX_INPROC_SERVER,
                IntPtr.Zero,
                out o);
            audioClient = (IAudioClient)o;
            int EngineLatencyInMS = (int)(arguments.BufferSeconds * 1000);
#if false
            WAVEFORMATEXTENSIBLE mixFormat = new WAVEFORMATEXTENSIBLE();
            mixFormat.wFormatTag = (short)WAVE_FORMAT.WAVE_FORMAT_EXTENSIBLE;
            mixFormat.nChannels = 2;
            mixFormat.nSamplesPerSec = samplingRate;
            mixFormat.nAvgBytesPerSec = samplingRate * 8;
            mixFormat.nBlockAlign = 4;
            mixFormat.wBitsPerSample = 32;
            mixFormat.cbSize = (short)(Marshal.SizeOf(typeof(WAVEFORMATEXTENSIBLE)) - Marshal.SizeOf(typeof(WAVEFORMATEX)));
            mixFormat.multi = 0;
            mixFormat.dwChannelMask = 3;
            Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new Guid("00000003-0000-0010-8000-00aa00389b71");
            mixFormat.SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
#endif
            const bool Shared = true; // TODO: finish
            if (Shared)
            {
                WAVEFORMATEX mixFormat;
                audioClient.GetMixFormat(out mixFormat);
                // Since Core Audio is a low-level interface, it is fairly unforgiving about automatic conversion
                // of data formats. Requesting a sampling rate other than that configured for shared mode does not work.
                // See: https://msdn.microsoft.com/en-us/library/dd370811%28v=vs.85%29.aspx
                if (mixFormat.nSamplesPerSec != samplingRate)
                {
                    throw new ApplicationException(String.Format("The current sampling rate {0} does not match the default audio device's sampling rate {1}. Either set the current sampling rate to the device's sampling rate or reconfigure the audio device to support the desired sampling rate.", samplingRate, mixFormat.nSamplesPerSec));
                }
                pointsPerFrameDevice = mixFormat.nChannels;
                audioClient.Initialize(
                    AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                    (int)AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_NOPERSIST,
                    EngineLatencyInMS * 10000,
                    0,
                    mixFormat,
                    IntPtr.Zero);
            }
            else
            {
                // TODO: report proper error if getting exclusive on device that is already in use

                // TODO: this code doesn't work
                const int AUDCLNT_E_UNSUPPORTED_FORMAT = unchecked((int)0x88890008);

                WAVEFORMATEX closestMatch1;
                {
                    WAVEFORMATEX format1 = new WAVEFORMATEX();
                    format1.wFormatTag = WAVE_FORMAT.WAVE_FORMAT_IEEE_FLOAT;
                    format1.nChannels = 2;
                    format1.nSamplesPerSec = samplingRate;
                    format1.nAvgBytesPerSec = samplingRate * format1.nChannels * 4;
                    format1.nBlockAlign = (ushort)(format1.nChannels * 4);
                    format1.wBitsPerSample = 32;
                    hr = audioClient.IsFormatSupported(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_EXCLUSIVE, format1, out closestMatch1);
                    if ((hr < 0) && (hr != AUDCLNT_E_UNSUPPORTED_FORMAT))
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                    bool supported = hr == 0;
                }

                WAVEFORMATEX closestMatch2;
                {
                    WAVEFORMATEXTENSIBLE format2 = new WAVEFORMATEXTENSIBLE();
                    format2.wFormatTag = WAVE_FORMAT.WAVE_FORMAT_EXTENSIBLE;
                    format2.nChannels = 2;
                    format2.nSamplesPerSec = samplingRate;
                    format2.nAvgBytesPerSec = samplingRate * format2.nChannels * 4;
                    format2.nBlockAlign = (ushort)(format2.nChannels * 4);
                    format2.wBitsPerSample = 32;
                    format2.multi = 0;
                    format2.dwChannelMask = 3;
                    format2.SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
                    hr = audioClient.IsFormatSupported(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_EXCLUSIVE, format2, out closestMatch2);
                    if ((hr < 0) && (hr != AUDCLNT_E_UNSUPPORTED_FORMAT))
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                    bool supported = hr == 0;
                }
            }
            Guid IID_IAudioRenderClient = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
            audioClient.GetService(ref IID_IAudioRenderClient, out o);
            renderClient = (IAudioRenderClient)o;

            audioClient.GetBufferSize(out maxBufferedFrames);
            startThreshholdFrames = maxBufferedFrames / 2;
            criticalBufferedFrames = samplingRate / 2;
        }

        private readonly int maxBufferedFrames; // frames
        private readonly int criticalBufferedFrames;
        private volatile int lastBufferedFrames;

        // these arrange to start playback only after a full buffer is available to prevent initial glitching
        private readonly int startThreshholdFrames;
        private bool started;

        public override Synthesizer.SynthErrorCodes Post(
            float[] data,
            int offset,
            int points)
        {
        Retry:
            int padding;
            int hr = audioClient.GetCurrentPadding(out padding);
            if (hr >= 0)
            {
                // Slipping a bit after every envelope update cycle produces an unpleasant buzzing glitch. Instead, if
                // buffer runs dry, stop playback and allow buffer to fill before restarting.
                if (started && (padding == 0))
                {
                    audioClient.Stop();
                    started = false;
                }

                lastBufferedFrames = padding; // update current status; consumed by progress UI

                int framesToWrite = points / pointsPerFrame;
                int framesAvailable = maxBufferedFrames - padding;

                // Start (or restart) playback when buffer is sufficiently filled
                if (!started && (padding >= startThreshholdFrames))
                {
                    audioClient.Start();
                    started = true;
                }

                if (framesAvailable < framesToWrite)
                {
                    // If buffer is too full to accomodate current data, ensure audio playback is started and
                    // sleep for a bit before attempting to post current data again.
                    if (!started)
                    {
                        audioClient.Start();
                        started = true;
                    }
                    Thread.Sleep(50);
                    goto Retry;
                }

                if (points != 0)
                {
                    IntPtr pData;
                    hr = renderClient.GetBuffer(framesToWrite, out pData);
                    if (pointsPerFrameDevice == pointsPerFrame)
                    {
                        Marshal.Copy(data, offset, pData, points);
                    }
                    else
                    {
                        // mix format may be stereo even if our data is mono
                        Debug.Assert(pointsPerFrameDevice > pointsPerFrame);
                        for (int i = 0; i < framesToWrite; i++)
                        {
                            Marshal.WriteInt32(pData, (2 * i + 0) * sizeof(float), UnsafeScalarCast.AsInt(data[offset + i]));
                            Marshal.WriteInt32(pData, (2 * i + 1) * sizeof(float), UnsafeScalarCast.AsInt(data[offset + i]));
                        }
                    }
                    renderClient.ReleaseBuffer(framesToWrite, 0);
                }
            }

            return Synthesizer.SynthErrorCodes.eSynthDone;
        }

        public override void Finish(bool abort)
        {
            if (!abort)
            {
                Flush();
                if (!started)
                {
                    audioClient.Start();
                    started = true;
                }

                while (true)
                {
                    int padding;
                    int hr = audioClient.GetCurrentPadding(out padding);
                    if (padding == 0)
                    {
                        break;
                    }
                    Thread.Sleep(50);
                }
            }

            audioClient.Stop();
        }

        public override void Dispose()
        {
            renderClient = null;
            audioClient = null;
        }

        // IBufferLoading
        public bool Available { get { return true; } }

        public float Level
        {
            get
            {
                return (float)lastBufferedFrames / samplingRate;
            }
        }

        public float Maximum
        {
            get
            {
                return (float)maxBufferedFrames / samplingRate;
            }
        }

        public float Critical
        {
            get
            {
                return (float)criticalBufferedFrames / samplingRate;
            }
        }

        private void Flush()
        {
            // for very short audio data - add silence to queue until start callback has been triggered
            float[] silence = new float[256];
            while (true)
            {
                if (started)
                {
                    break;
                }
                Post(silence, 0, silence.Length);
            }
        }
    }
#endif

    #region WASAPI Interop
    public enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count,
    }

    public enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count,

        eLegacy = 127, // our addition
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceEnumerator
    {
        [return: MarshalAs(UnmanagedType.Error)]
        int EnumAudioEndpoints(
            [In] EDataFlow dataFlow,
            [In] DEVICE_STATE dwStateMask,
            out IMMDeviceCollection ppDevices);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetDefaultAudioEndpoint(
            [In] EDataFlow dataFlow,
            [In] ERole role,
            out IMMDevice ppEndpoint);

        //[return: MarshalAs(UnmanagedType.Error)]
        [PreserveSig]
        int GetDevice(
            [In] string pwstrId,
            out IMMDevice ppDevice);

        [return: MarshalAs(UnmanagedType.Error)]
        int RegisterEndpointNotificationCallback(
            [In] IMMNotificationClient pClient);

        [return: MarshalAs(UnmanagedType.Error)]
        int UnregisterEndpointNotificationCallback(
            [In] IMMNotificationClient pClient);
    }

    [Flags]
    public enum DEVICE_STATE : int
    {
        DEVICE_STATE_ACTIVE = 0x00000001,
        DEVICE_STATE_DISABLED = 0x00000002,
        DEVICE_STATE_NOTPRESENT = 0x00000004,
        DEVICE_STATE_UNPLUGGED = 0x00000008,

        DEVICE_STATEMASK_ALL = 0x0000000f,
    }

    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceCollection
    {
        [return: MarshalAs(UnmanagedType.Error)]
        int GetCount(
            out int pcDevices);

        [return: MarshalAs(UnmanagedType.Error)]
        int Item(
            [In] int nDevice,
            out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDevice
    {
        [return: MarshalAs(UnmanagedType.Error)]
        int Activate(
            [In] ref Guid iid,
            [In] int dwClsCtx,
            /*[In, Optional] ref Guid pActivationParams*/[In, Optional] IntPtr pActivationParams,
            [Out, MarshalAs(UnmanagedType.Interface)] out object ppInterface);

        [return: MarshalAs(UnmanagedType.Error)]
        int OpenPropertyStore(
            [In] STGM stgmAccess,
            out IPropertyStore ppProperties);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetId(
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetState(
            out DEVICE_STATE pdwState);
    }

    public enum STGM : int
    {
        STGM_READ = 0x00000000,
        STGM_WRITE = 0x00000001,
        STGM_READWRITE = 0x00000002,
    }

    [ComImport, Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMNotificationClient
    {
        [return: MarshalAs(UnmanagedType.Error)]
        int OnDeviceStateChanged(
            [In] string pwstrDeviceId,
            [In] int dwNewState);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnDeviceAdded(
             [In] string pwstrDeviceId);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnDeviceRemoved(
             [In] string pwstrDeviceId);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnDefaultDeviceChanged(
             [In] EDataFlow flow,
             [In] ERole role,
             [In] string pwstrDefaultDeviceId);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnPropertyValueChanged(
             [In] string pwstrDeviceId,
             [In] ref Guid key);
    }

    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore
    {
        [return: MarshalAs(UnmanagedType.Error)]
        int GetCount(
            out int cProps);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetAt(
            [In] int iProp,
            out DEVPROPKEY pkey);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetValue(
            [In] ref DEVPROPKEY key,
            out PropVariant pv);

        [return: MarshalAs(UnmanagedType.Error)]
        int SetValue(
            [In] ref DEVPROPKEY key,
            [In] PropVariant propvar);

        [return: MarshalAs(UnmanagedType.Error)]
        int Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DEVPROPKEY
    {
        public readonly uint a;
        public readonly ushort b;
        public readonly ushort c;
        public readonly byte d;
        public readonly byte e;
        public readonly byte f;
        public readonly byte g;
        public readonly byte h;
        public readonly byte i;
        public readonly byte j;
        public readonly byte k;
        public readonly uint pid;

        public DEVPROPKEY(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k, uint pid)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.e = e;
            this.f = f;
            this.g = g;
            this.h = h;
            this.i = i;
            this.j = j;
            this.k = k;
            this.pid = pid;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PropVariant
    {
        [FieldOffset(0)]
        private short vt;

        [FieldOffset(8)]
        private sbyte cVal;
        [FieldOffset(8)]
        private byte bVal;
        [FieldOffset(8)]
        private short iVal;
        [FieldOffset(8)]
        private ushort uiVal;
        [FieldOffset(8)]
        private int lVal;
        [FieldOffset(8)]
        private uint ulVal;
        [FieldOffset(8)]
        private int intVal;
        [FieldOffset(8)]
        private uint uintVal;
        [FieldOffset(8)]
        private long hVal;
        [FieldOffset(8)]
        private long uhVal;
        [FieldOffset(8)]
        private float fltVal;
        [FieldOffset(8)]
        private double dblVal;
        [FieldOffset(8)]
        private bool boolVal;
        [FieldOffset(8)]
        private int scode;
        [FieldOffset(8)]
        private DateTime date;
        [FieldOffset(8)]
        private System.Runtime.InteropServices.ComTypes.FILETIME filetime;
        [FieldOffset(8)]
        private BLOB blobVal;
        [FieldOffset(8)]
        private IntPtr pwszVal; //LPWSTR 

        private struct BLOB
        {
            public int cb;
            public IntPtr data;
        }

        [DllImport("ole32.dll", PreserveSig = false)]
        private extern static void PropVariantClear(ref PropVariant pvar);

        public void Clear()
        {
            // TODO: find out why this started throwing exceptions on .NET 4.61 (worked fine on .NET 2.0)
            try
            {
                PropVariantClear(ref this);
            }
            catch (OverflowException)
            {
            }
        }

        public object Value
        {
            get
            {
                switch ((VarEnum)vt)
                {
                    default:
                        Debug.Assert(false);
                        throw new NotImplementedException();
                    case VarEnum.VT_I1:
                        return bVal;
                    case VarEnum.VT_I2:
                        return iVal;
                    case VarEnum.VT_I4:
                        return lVal;
                    case VarEnum.VT_I8:
                        return hVal;
                    case VarEnum.VT_INT:
                        return iVal;
                    case VarEnum.VT_UI4:
                        return ulVal;
                    case VarEnum.VT_LPWSTR:
                        return Marshal.PtrToStringUni(pwszVal);
                    case VarEnum.VT_BLOB:
                        byte[] b = new byte[blobVal.cb];
                        Marshal.Copy(blobVal.data, b, 0, b.Length);
                        return b;
                }
            }
        }
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    public class MMDeviceEnumerator
    {
    }

    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioClient
    {
        [return: MarshalAs(UnmanagedType.Error)]
        int Initialize(
            [In] AUDCLNT_SHAREMODE ShareMode,
            [In] int StreamFlags,
            [In] long hnsBufferDuration, // REFERENCE_TIME 
            [In] long hnsPeriodicity, // REFERENCE_TIME 
            [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OutOfPhase.WAVEFORMATEX_marshaler))] WAVEFORMATEX pFormat,
            /*[In, Optional] ref Guid AudioSessionGuid*/[In, Optional] IntPtr AudioSessionGuid);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetBufferSize(
            out int pNumBufferFrames);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetStreamLatency(
            out long phnsLatency); // REFERENCE_TIME 

        //[return: MarshalAs(UnmanagedType.Error)]
        [PreserveSig]
        int GetCurrentPadding(
            out int pNumPaddingFrames);

        //[return: MarshalAs(UnmanagedType.Error)]
        [PreserveSig]
        int IsFormatSupported(
            [In] AUDCLNT_SHAREMODE ShareMode,
            [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OutOfPhase.WAVEFORMATEX_marshaler))] WAVEFORMATEX pFormat,
            [Out, Optional, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OutOfPhase.WAVEFORMATEX_marshaler))] out WAVEFORMATEX ppClosestMatch);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetMixFormat(
            [Out, Optional, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OutOfPhase.WAVEFORMATEX_marshaler))] out WAVEFORMATEX ppDeviceFormat);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetDevicePeriod(
            out long phnsDefaultDevicePeriod, // REFERENCE_TIME 
            out long phnsMinimumDevicePeriod); // REFERENCE_TIME 

        [return: MarshalAs(UnmanagedType.Error)]
        int Start();

        [return: MarshalAs(UnmanagedType.Error)]
        int Stop();

        [return: MarshalAs(UnmanagedType.Error)]
        int Reset();

        [return: MarshalAs(UnmanagedType.Error)]
        int SetEventHandle(
            [In] IntPtr eventHandle);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetService(
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out object ppv);
    }

    [Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioRenderClient
    {
        //[return: MarshalAs(UnmanagedType.Error)]
        [PreserveSig]
        int GetBuffer(
            [In] int NumFramesRequested,
            [Out] out IntPtr ppData); // BYTE*

        [return: MarshalAs(UnmanagedType.Error)]
        int ReleaseBuffer(
            [In] int NumFramesWritten,
            [In] int dwFlags);
    }

    public enum CLSCTX : int
    {
        CLSCTX_INPROC_SERVER = 0x1,
    }

    [Flags]
    public enum WAVE_FORMAT : ushort
    {
        WAVE_FORMAT_PCM = 1,
        WAVE_FORMAT_IEEE_FLOAT = 0x0003,
        WAVE_FORMAT_EXTENSIBLE = 0xFFFE,
    }

    [Flags]
    public enum AUDCLNT_STREAMFLAGS : int
    {
        AUDCLNT_STREAMFLAGS_CROSSPROCESS = 0x00010000,
        AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000,
        AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 0x00040000,
        AUDCLNT_STREAMFLAGS_NOPERSIST = 0x00080000,
        AUDCLNT_STREAMFLAGS_RATEADJUST = 0x00100000,
        AUDCLNT_SESSIONFLAGS_EXPIREWHENUNOWNED = 0x10000000,
        AUDCLNT_SESSIONFLAGS_DISPLAY_HIDE = 0x20000000,
        AUDCLNT_SESSIONFLAGS_DISPLAY_HIDEWHENEXPIRED = 0x40000000,
    }

    public enum AUDCLNT_SHAREMODE
    {
        AUDCLNT_SHAREMODE_SHARED,
        AUDCLNT_SHAREMODE_EXCLUSIVE,
    }

    public enum AudioSessionState
    {
        AudioSessionStateInactive = 0,
        AudioSessionStateActive = 1,
        AudioSessionStateExpired = 2,
    }

    public enum AudioSessionDisconnectReason
    {
        DisconnectReasonDeviceRemoval = 0,
        DisconnectReasonServerShutdown = (DisconnectReasonDeviceRemoval + 1),
        DisconnectReasonFormatChanged = (DisconnectReasonServerShutdown + 1),
        DisconnectReasonSessionLogoff = (DisconnectReasonFormatChanged + 1),
        DisconnectReasonSessionDisconnected = (DisconnectReasonSessionLogoff + 1),
        DisconnectReasonExclusiveModeOverride = (DisconnectReasonSessionDisconnected + 1)
    }

    [ComImport, Guid("24918ACC-64B3-37C1-8CA9-74A66E9957A8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioSessionEvents
    {
        [return: MarshalAs(UnmanagedType.Error)]
        int OnDisplayNameChanged(
            [In] string NewDisplayName,
            [In] ref Guid EventContext);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnIconPathChanged(
            [In] string NewIconPath,
            [In] ref Guid EventContext);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnSimpleVolumeChanged(
            [In] float NewVolume,
            [In]bool NewMute,
            [In] ref Guid EventContext);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnChannelVolumeChanged(
            [In] int ChannelCount,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] float[] NewChannelVolumeArray,
            [In] int ChangedChannel,
            [In] ref Guid EventContext);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnGroupingParamChanged(
            [In] ref Guid NewGroupingParam,
            [In] ref Guid EventContext);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnStateChanged(
            [In] AudioSessionState NewState);

        [return: MarshalAs(UnmanagedType.Error)]
        int OnSessionDisconnected(
            [In] AudioSessionDisconnectReason DisconnectReason);
    }

    [Flags]
    public enum AUDCLNT_BUFFERFLAGS
    {
        AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY = 0x1,
        AUDCLNT_BUFFERFLAGS_SILENT = 0x2,
        AUDCLNT_BUFFERFLAGS_TIMESTAMP_ERROR = 0x4,
    }

    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioSessionControl
    {
        [return: MarshalAs(UnmanagedType.Error)]
        int GetState(
            out AudioSessionState pRetVal);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetDisplayName(
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [return: MarshalAs(UnmanagedType.Error)]
        int SetDisplayName(
            [In] string Value,
            [In] ref Guid EventContext);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetIconPath(
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [return: MarshalAs(UnmanagedType.Error)]
        int SetIconPath(
            [In] string Value,
            [In] ref Guid EventContext);

        [return: MarshalAs(UnmanagedType.Error)]
        int GetGroupingParam(
            out Guid pRetVal);

        [return: MarshalAs(UnmanagedType.Error)]
        int SetGroupingParam(
            [In] ref Guid Override,
            [In] ref Guid EventContext);

        [return: MarshalAs(UnmanagedType.Error)]
        int RegisterAudioSessionNotification(
            [In] IAudioSessionEvents NewNotifications);

        [return: MarshalAs(UnmanagedType.Error)]
        int UnregisterAudioSessionNotification(
            [In] IAudioSessionEvents NewNotifications);
    }

    public class WAVEFORMATEX
    {
        public WAVE_FORMAT wFormatTag; /* format type */
        public ushort nChannels; /* number of channels (i.e. mono, stereo...) */
        public int nSamplesPerSec; /* sample rate */
        public int nAvgBytesPerSec; /* for buffer estimation */
        public ushort nBlockAlign; /* block size of data */
        public ushort wBitsPerSample; /* Number of bits per sample of mono data */
        //public ushort cbSize; /* The count in bytes of the size of extra information (after cbSize) */
    }

    public class WAVEFORMATEXTENSIBLE : WAVEFORMATEX
    {
        public ushort multi; // one of:
        //WORD wValidBitsPerSample; /* bits of precision  */
        //WORD wSamplesPerBlock; /* valid if wBitsPerSample==0 */
        //WORD wReserved; /* If neither applies, set to zero. */
        public uint dwChannelMask; /* which channels are present in stream  */
        public Guid SubFormat;
    }

    public class WAVEFORMATEX_marshaler : ICustomMarshaler
    {
        public static ICustomMarshaler GetInstance(string pstrCookie)
        {
            return new WAVEFORMATEX_marshaler();
        }

        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.FreeCoTaskMem(pNativeData);
        }

        public int GetNativeDataSize()
        {
            Debugger.Break();
            throw new Exception("The method or operation is not implemented.");
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            WAVEFORMATEX f = (WAVEFORMATEX)ManagedObj;
            IntPtr p;
            if (ManagedObj is WAVEFORMATEXTENSIBLE)
            {
                WAVEFORMATEXTENSIBLE ff = (WAVEFORMATEXTENSIBLE)ManagedObj;
                p = Marshal.AllocCoTaskMem(40);
                Marshal.WriteInt16(p, 16, 22); // cbSize
                Marshal.WriteInt16(p, 18, unchecked((short)ff.multi));
                Marshal.WriteInt32(p, 20, unchecked((int)ff.dwChannelMask));
                Marshal.Copy(ff.SubFormat.ToByteArray(), 0, new IntPtr(p.ToInt64() + 24), 16);
            }
            else
            {
                p = Marshal.AllocCoTaskMem(18);
                Marshal.WriteInt16(p, 16, 0); // cbSize
            }
            // common prefix
            Marshal.WriteInt16(p, 0, unchecked((short)f.wFormatTag));
            Marshal.WriteInt16(p, 2, unchecked((short)f.nChannels));
            Marshal.WriteInt32(p, 4, f.nSamplesPerSec);
            Marshal.WriteInt32(p, 8, f.nAvgBytesPerSec);
            Marshal.WriteInt16(p, 12, unchecked((short)f.nBlockAlign));
            Marshal.WriteInt16(p, 14, unchecked((short)f.wBitsPerSample));
            return p;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            WAVE_FORMAT wFormatTag = (WAVE_FORMAT)Marshal.ReadInt16(pNativeData, 0);
            WAVEFORMATEX f;
            switch (wFormatTag)
            {
                default:
                    f = new WAVEFORMATEX();
                    Debugger.Break(); // do we need to do any work for this type?
                    break;
                case WAVE_FORMAT.WAVE_FORMAT_PCM:
                case WAVE_FORMAT.WAVE_FORMAT_IEEE_FLOAT:
                    f = new WAVEFORMATEX();
                    break;
                case WAVE_FORMAT.WAVE_FORMAT_EXTENSIBLE:
                    WAVEFORMATEXTENSIBLE ff = new WAVEFORMATEXTENSIBLE();
                    f = ff;
                    ff.multi = (ushort)Marshal.ReadInt16(pNativeData, 18);
                    ff.dwChannelMask = (uint)Marshal.ReadInt32(pNativeData, 20);
                    byte[] guid = new byte[16];
                    Marshal.Copy(new IntPtr(pNativeData.ToInt64() + 24), guid, 0, guid.Length);
                    ff.SubFormat = new Guid(guid);
                    break;
            }
            // common prefix
            f.wFormatTag = wFormatTag;
            f.nChannels = (ushort)Marshal.ReadInt16(pNativeData, 2);
            f.nSamplesPerSec = Marshal.ReadInt32(pNativeData, 4);
            f.nAvgBytesPerSec = Marshal.ReadInt32(pNativeData, 8);
            f.nBlockAlign = (ushort)Marshal.ReadInt16(pNativeData, 12);
            f.wBitsPerSample = (ushort)Marshal.ReadInt16(pNativeData, 14);
            //f.cbSize = (ushort)Marshal.ReadInt16(pNativeData, 16);
            return f;
        }
    }
    #endregion

#if true // prevents "Add New Data Source..." from working
    public class OutputDeviceLegacyDestinationHandler : DestinationHandler<OutputDeviceDestination>, IBufferLoading
    {
        private readonly NumChannelsType channels;
        private readonly NumBitsType bits;
        private readonly int samplingRate;
        private readonly int pointsPerFrame;

        private readonly IntPtr hWaveOut;

        private readonly int bufferCount;
        private readonly int pointsPerBuffer;
        private volatile int bufferedPoints;
        private volatile Buffer freeList;
        private volatile Buffer current;
        private volatile int enqueuedBuffers;
        private readonly ManualResetEvent avail;
        private readonly Buffer[] bufferMap;

        private readonly WaveOut.waveOutProc callbackRef; // keep-alive

        private bool paused;
        private bool terminated;

        private class Buffer : IDisposable
        {
            public Buffer next;

            public WaveOut.WAVEHDR header;
            public float[] points; // data
            public GCHandle hPoints;
            public GCHandle hHeader;

            public int level;

            public Buffer(int capacity)
            {
                points = new float[capacity];
                hPoints = GCHandle.Alloc(points, GCHandleType.Pinned);

                header = new WaveOut.WAVEHDR();
                hHeader = GCHandle.Alloc(header, GCHandleType.Pinned);
                header.lpData = hPoints.AddrOfPinnedObject();
            }

            public void Dispose()
            {
                hPoints.Free();
                hHeader.Free();
                GC.SuppressFinalize(this);
            }

            ~Buffer()
            {
#if DEBUG
                Debug.Assert(false, "Buffer finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
                Dispose();
            }
#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif
        }

        public OutputDeviceLegacyDestinationHandler(
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            OutputDeviceArguments arguments)
        {
#if DEBUG
            if ((channels != NumChannelsType.eSampleMono)
                && (channels != NumChannelsType.eSampleStereo))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((bits != NumBitsType.eSample8bit)
                && (bits != NumBitsType.eSample16bit)
                && (bits != NumBitsType.eSample24bit))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            this.channels = channels;
            this.bits = bits;
            this.samplingRate = samplingRate;
            this.pointsPerFrame = channels == NumChannelsType.eSampleStereo ? 2 : 1;
            this.pointsPerBuffer = (pointsPerFrame * samplingRate) / 8;
            this.avail = new ManualResetEvent(true);

            this.bufferCount = Math.Max(2, (int)Math.Ceiling(arguments.BufferSeconds * pointsPerFrame * samplingRate / pointsPerBuffer));
            this.bufferMap = new Buffer[bufferCount];
            for (int i = 0; i < bufferCount; i++)
            {
                Buffer buffer = new Buffer(pointsPerBuffer);
                this.bufferMap[i] = buffer;
                buffer.header.dwUser = new IntPtr(i);
                buffer.next = freeList;
                this.freeList = buffer;
            }

            WAVEFORMATEX format = new WAVEFORMATEX();
            format.wFormatTag = WAVE_FORMAT.WAVE_FORMAT_IEEE_FLOAT;
            format.nChannels = (ushort)pointsPerFrame;
            format.nSamplesPerSec = samplingRate;
            format.nAvgBytesPerSec = samplingRate * sizeof(float) * format.nChannels;
            format.nBlockAlign = (ushort)(sizeof(float) * format.nChannels);
            format.wBitsPerSample = 32;

            int error = WaveOut.waveOutOpen(
                out hWaveOut,
                new IntPtr(WaveOut.WAVE_MAPPER),
                format,
                callbackRef = Callback,
                IntPtr.Zero,
                WaveOut.CALLBACK_FUNCTION /*| WaveOut.WAVE_ALLOWSYNC*/);
            if (error != WaveOut.MMSYSERR_NOERROR)
            {
                throw new MultimediaException("waveOutOpen", error);
            }

            error = WaveOut.waveOutPause(
                hWaveOut);
            if (error != WaveOut.MMSYSERR_NOERROR)
            {
                throw new MultimediaException("waveOutPause", error);
            }
            paused = true;
        }

        public override void Finish(bool abort)
        {
            int error;

            terminated = true;

            if (!abort)
            {
                if (paused)
                {
                    error = WaveOut.waveOutRestart(
                        hWaveOut);
                    if (error != WaveOut.MMSYSERR_NOERROR)
                    {
                        throw new MultimediaException("waveOutRestart", error);
                    }
                    paused = false;
                }

                if (current != null)
                {
                    Flush();
                }

                while (bufferedPoints != 0) // TODO: cancel? review for correctness and hackiness
                {
                    Thread.Sleep(50);
                }
            }
            else
            {
                avail.Set(); // break any deadlocks
            }

            error = WaveOut.waveOutReset(hWaveOut);
            if (error != WaveOut.MMSYSERR_NOERROR)
            {
                throw new MultimediaException("waveOutReset", error);
            }
        }

        ~OutputDeviceLegacyDestinationHandler()
        {
#if DEBUG
            Debug.Assert(false, "OutputDeviceLegacyDestinationHandler finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            Dispose();
        }
#if DEBUG
        private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

        public override void Dispose()
        {
            int error;

            error = WaveOut.waveOutClose(hWaveOut);
            if (error != WaveOut.MMSYSERR_NOERROR)
            {
                throw new MultimediaException("waveOutClose", error);
            }

            for (int i = 0; i < bufferMap.Length; i++)
            {
                //Debug.Assert(((bufferMap[i].header.dwFlags & WaveOut.WHDR_PREPARED) != 0)
                //    == ((bufferMap[i].header.dwFlags & WaveOut.WHDR_DONE) != 0));
                bufferMap[i].Dispose();
            }

            avail.Close();

            GC.SuppressFinalize(this);
        }

        // IBufferLoading
        public bool Available { get { return true; } }
        public float Level { get { return (float)(bufferedPoints / pointsPerFrame) / samplingRate; } } // seconds
        public float Maximum { get { return (float)((bufferCount * pointsPerBuffer) / pointsPerFrame) / samplingRate; } } // seconds
        public float Critical { get { return .5f; } }

        public override Synthesizer.SynthErrorCodes Post(
            float[] data,
            int offset,
            int pointCount)
        {
            if (pointCount != 0)
            {
            Restart:
                try
                {
                    while (!avail.WaitOne(1000))
                    {
                    }
                }
                catch (Exception exception)
                {
                    Debugger.Break();
                    // event object was deleted - indicating playback was cancelled while we were waiting for buffer to empty
                    terminated = true;
                }
                if (terminated)
                {
                    return Synthesizer.SynthErrorCodes.eSynthUserCancelled;
                }

                if ((current != null) && (current.level + pointCount > pointsPerBuffer))
                {
                    Flush();
                }

                if (!paused && (enqueuedBuffers == 0))
                {
                    int error = WaveOut.waveOutPause(
                        hWaveOut);
                    if (error != WaveOut.MMSYSERR_NOERROR)
                    {
                        throw new MultimediaException("waveOutPause", error);
                    }
                    paused = true;
                }

                if (current == null)
                {
                    lock (this)
                    {
                        if (freeList == null)
                        {
                            avail.Reset();
                            goto Restart;
                        }

                        current = freeList;
                        freeList = freeList.next;
                        current.next = null;
                        current.level = 0;
                    }
                }
                Array.Copy(data, offset, current.points, current.level, pointCount);
                current.level += pointCount;
                Interlocked.Add(ref bufferedPoints, pointCount);
            }

            return Synthesizer.SynthErrorCodes.eSynthDone;
        }

        private void Flush()
        {
            int error;

            if ((current.header.dwFlags & WaveOut.WHDR_PREPARED) != 0)
            {
                error = WaveOut.waveOutUnprepareHeader(
                    hWaveOut,
                    current.header,
                    Marshal.SizeOf(current.header));
                if (error != WaveOut.MMSYSERR_NOERROR)
                {
                    throw new MultimediaException("waveOutUnprepareHeader", error);
                }
            }

            current.header.dwBufferLength = sizeof(float) * current.level;
            current.header.dwFlags = 0;
            error = WaveOut.waveOutPrepareHeader(
                hWaveOut,
                current.header,
                Marshal.SizeOf(current.header));
            if (error != WaveOut.MMSYSERR_NOERROR)
            {
                throw new MultimediaException("waveOutPrepareHeader", error);
            }
            error = WaveOut.waveOutWrite(
                hWaveOut,
                current.header,
                Marshal.SizeOf(current.header));
            if (error != WaveOut.MMSYSERR_NOERROR)
            {
                throw new MultimediaException("waveOutWrite", error);
            }
            Interlocked.Increment(ref enqueuedBuffers);

            if (paused && (enqueuedBuffers >= bufferCount / 2))
            {
                error = WaveOut.waveOutRestart(
                    hWaveOut);
                if (error != WaveOut.MMSYSERR_NOERROR)
                {
                    throw new MultimediaException("waveOutRestart", error);
                }
                paused = false;
            }

            current = null;
        }

        public void Callback(
            IntPtr/*HWAVEOUT*/ hwo,
            uint uMsg,
            IntPtr dwInstance,
            WaveOut.WAVEHDR dwParam1,
            IntPtr dwParam2)
        {
            switch (uMsg)
            {
                case WaveOut.WOM_OPEN:
                    break;

                case WaveOut.WOM_CLOSE:
                    break;

                case WaveOut.WOM_DONE:
                    Buffer buffer = bufferMap[dwParam1.dwUser.ToInt32()];
#if false
                    int error = WaveOut.waveOutUnprepareHeader(
                        hWaveOut,
                        buffer.header,
                        Marshal.SizeOf(buffer.header));
#endif
                    Interlocked.Add(ref bufferedPoints, -buffer.level);
                    Interlocked.Decrement(ref enqueuedBuffers);
                    lock (this)
                    {
                        buffer.next = freeList;
                        freeList = buffer;
                        avail.Set();
                    }
                    break;
            }
        }

        public class MultimediaException : ApplicationException
        {
            public MultimediaException()
                : base()
            {
            }

            public MultimediaException(string function, int error)
                : base(String.Format("{0} error {1}", function, error))
            {
            }
        }
    }
#endif

    #region Legacy Interop
    public static class WaveOut
    {
        public delegate void waveOutProc(
            IntPtr/*HWAVEOUT*/ hwo,
            uint uMsg,
            IntPtr dwInstance,
            [In] WaveOut.WAVEHDR dwParam1,
            IntPtr dwParam2);

        [DllImport("winmm.dll", PreserveSig = true)]
        public static extern int/*MMRESULT*/ waveOutOpen(
            out IntPtr/*LPHWAVEOUT*/ phwo,
            IntPtr uDeviceID,
            [In, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OutOfPhase.WAVEFORMATEX_marshaler))] WAVEFORMATEX pwfx,
            waveOutProc dwCallback,
            IntPtr dwCallbackInstance,
            uint fdwOpen);

        [DllImport("winmm.dll", PreserveSig = true)]
        public static extern int/*MMRESULT*/ waveOutPause(
            IntPtr/*HWAVEOUT*/ hwo);

        [DllImport("winmm.dll", PreserveSig = true)]
        public static extern int/*MMRESULT*/ waveOutRestart(
            IntPtr/*HWAVEOUT*/ hwo);

        [DllImport("winmm.dll", PreserveSig = true)]
        public static extern int/*MMRESULT*/ waveOutReset(
            IntPtr/*HWAVEOUT*/ hwo);

        [DllImport("winmm.dll", PreserveSig = true)]
        public static extern int/*MMRESULT*/ waveOutClose(
            IntPtr/*HWAVEOUT*/ hwo);

        [DllImport("winmm.dll", PreserveSig = true)]
        public static extern int/*MMRESULT*/ waveOutPrepareHeader(
            IntPtr/*HWAVEOUT*/ hwo,
            [In, Out] WaveOut.WAVEHDR pwh,
            int cbwh);

        [DllImport("winmm.dll", PreserveSig = true)]
        public static extern int/*MMRESULT*/ waveOutUnprepareHeader(
            IntPtr/*HWAVEOUT*/ hwo,
            [In, Out] WaveOut.WAVEHDR pwh,
            int cbwh);

        [DllImport("winmm.dll", PreserveSig = true)]
        public static extern int/*MMRESULT*/ waveOutWrite(
            IntPtr/*HWAVEOUT*/ hwo,
            [In, Out] WaveOut.WAVEHDR pwh,
            int cbwh);

        // MMSystem.h

        /* device ID for wave device mapper */
        public const int WAVE_MAPPER = -1;

        public const int CALLBACK_TYPEMASK = 0x00070000; /* callback type mask */
        public const int CALLBACK_NULL = 0x00000000; /* no callback */
        public const int CALLBACK_WINDOW = 0x00010000; /* dwCallback is a HWND */
        public const int CALLBACK_TASK = 0x00020000; /* dwCallback is a HTASK */
        public const int CALLBACK_FUNCTION = 0x00030000; /* dwCallback is a FARPROC */
        public const int CALLBACK_THREAD = CALLBACK_TASK; /* thread ID replaces 16 bit task */
        public const int CALLBACK_EVENT = 0x00050000; /* dwCallback is an EVENT Handle */

        /* flags for dwFlags parameter in waveOutOpen() and waveInOpen() */
        public const int WAVE_FORMAT_QUERY = 0x0001;
        public const int WAVE_ALLOWSYNC = 0x0002;
        public const int WAVE_MAPPED = 0x0004;
        public const int WAVE_FORMAT_DIRECT = 0x0008;
        public const int WAVE_FORMAT_DIRECT_QUERY = (WAVE_FORMAT_QUERY | WAVE_FORMAT_DIRECT);
        public const int WAVE_MAPPED_DEFAULT_COMMUNICATION_DEVICE = 0x0010;

        public const int WOM_OPEN = 0x3BB; /* waveform output */
        public const int WOM_CLOSE = 0x3BC;
        public const int WOM_DONE = 0x3BD;

        /* flags for dwFlags field of WAVEHDR */
        public const int WHDR_DONE = 0x00000001; /* done bit */
        public const int WHDR_PREPARED = 0x00000002; /* set if this header has been prepared */
        public const int WHDR_BEGINLOOP = 0x00000004; /* loop start block */
        public const int WHDR_ENDLOOP = 0x00000008; /* loop end block */
        public const int WHDR_INQUEUE = 0x00000010; /* reserved for driver */

        /* wave data block header */
        [StructLayout(LayoutKind.Sequential)]
        public class WAVEHDR
        {
            public IntPtr lpData; /* pointer to locked data buffer */
            public int dwBufferLength; /* length of data buffer */
            public int dwBytesRecorded; /* used for input only */
            public IntPtr dwUser; /* for client's use */
            public int dwFlags; /* assorted flags (see defines) */
            public int dwLoops; /* loop control counter */
            public IntPtr lpNext; /* reserved for driver */
            public IntPtr reserved; /* reserved for driver */
        }

        /* general error return values */
        private const int MMSYSERR_BASE = 0;
        public const int MMSYSERR_NOERROR = 0; /* no error */
        public const int MMSYSERR_ERROR = MMSYSERR_BASE + 1; /* unspecified error */
        public const int MMSYSERR_BADDEVICEID = MMSYSERR_BASE + 2; /* device ID out of range */
        public const int MMSYSERR_NOTENABLED = MMSYSERR_BASE + 3; /* driver failed enable */
        public const int MMSYSERR_ALLOCATED = MMSYSERR_BASE + 4; /* device already allocated */
        public const int MMSYSERR_INVALHANDLE = MMSYSERR_BASE + 5; /* device handle is invalid */
        public const int MMSYSERR_NODRIVER = MMSYSERR_BASE + 6; /* no device driver present */
        public const int MMSYSERR_NOMEM = MMSYSERR_BASE + 7; /* memory allocation error */
        public const int MMSYSERR_NOTSUPPORTED = MMSYSERR_BASE + 8; /* function isn't supported */
        public const int MMSYSERR_BADERRNUM = MMSYSERR_BASE + 9; /* error value out of range */
        public const int MMSYSERR_INVALFLAG = MMSYSERR_BASE + 10; /* invalid flag passed */
        public const int MMSYSERR_INVALPARAM = MMSYSERR_BASE + 11; /* invalid parameter passed */
        public const int MMSYSERR_HANDLEBUSY = MMSYSERR_BASE + 12; /* handle being used simultaneously on another thread =eg callback) */
        public const int MMSYSERR_INVALIDALIAS = MMSYSERR_BASE + 13; /* specified alias not found */
        public const int MMSYSERR_BADDB = MMSYSERR_BASE + 14; /* bad registry database */
        public const int MMSYSERR_KEYNOTFOUND = MMSYSERR_BASE + 15; /* registry key not found */
        public const int MMSYSERR_READERROR = MMSYSERR_BASE + 16; /* registry read error */
        public const int MMSYSERR_WRITEERROR = MMSYSERR_BASE + 17; /* registry write error */
        public const int MMSYSERR_DELETEERROR = MMSYSERR_BASE + 18; /* registry delete error */
        public const int MMSYSERR_VALNOTFOUND = MMSYSERR_BASE + 19; /* registry value not found */
        public const int MMSYSERR_NODRIVERCB = MMSYSERR_BASE + 20; /* driver does not call DriverCallback */
        public const int MMSYSERR_MOREDATA = MMSYSERR_BASE + 21; /* more data to be returned */
        public const int MMSYSERR_LASTERROR = MMSYSERR_BASE + 21; /* last error in range */

        /* waveform audio error return values */
        private const int WAVERR_BASE = 32;
        public const int WAVERR_BADFORMAT = WAVERR_BASE + 0; /* unsupported wave format */
        public const int WAVERR_STILLPLAYING = WAVERR_BASE + 1; /* still something playing */
        public const int WAVERR_UNPREPARED = WAVERR_BASE + 2; /* header not prepared */
        public const int WAVERR_SYNC = WAVERR_BASE + 3; /* device is synchronous */
        public const int WAVERR_LASTERROR = WAVERR_BASE + 3; /* last error in range */
    }
    #endregion
}
