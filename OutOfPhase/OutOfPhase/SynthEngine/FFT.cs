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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        public abstract class FFT : IDisposable
        {
            public const bool UseFFTW = true;

            protected readonly int n;
            protected readonly float scaleFactor;

            public static FFT Create(int n, int concurrency, FFT share)
            {
                if (UseFFTW)
                {
                    return new FFTW(n, concurrency, share);
                }
                else
                {
                    return new FFTSimple(n, share);
                }
            }

            public static FFT Create(int n)
            {
                return Create(n, 0, null);
            }

            public static FFT Create(int n, int concurrency)
            {
                return Create(n, concurrency, null);
            }

            public static FFT Create(int n, FFT share)
            {
                return Create(n, 0, share);
            }

            protected FFT(int n, float scaleFactor)
            {
                this.n = n;
                this.scaleFactor = scaleFactor;
            }

            public int N { get { return n; } }
            public float ScaleFactor { get { return scaleFactor; } }

            public abstract float[] Base { get; }
            public abstract int Offset { get; }
            public abstract void FFTfwd();
            public abstract void FFTinv();

            public abstract void Dispose();
        }

        public class FFTSimple : FFT
        {
            private const int SizeOfFloat = 4;

            private readonly float[] workspace;
            private readonly GCHandle hWorkspace;
            private readonly int offset;
#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

            public FFTSimple(int n, FFT share)
                : base(n, 1)
            {
                if (share != null)
                {
                    if (n > share.N)
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    // share workspace
                    this.workspace = share.Base;
                    this.hWorkspace = GCHandle.Alloc(this.workspace, GCHandleType.Pinned);
                    this.offset = share.Offset;
                }
                else
                {
                    // allocate workspace
                    int count = n + 2;
                    this.workspace = new float[count + SynthParamRec.WORKSPACEALIGN];
                    this.hWorkspace = GCHandle.Alloc(this.workspace, GCHandleType.Pinned);
                    this.offset = 0;
                    SynthParamRec.Align(this.hWorkspace.AddrOfPinnedObject(), ref this.offset, SynthParamRec.WORKSPACEALIGN, SizeOfFloat);
                    Debug.Assert(this.offset + count <= this.workspace.Length);
                }
            }

            public override float[] Base { get { return workspace; } }
            public override int Offset { get { return 0; } }

            public override void FFTfwd()
            {
                FFT_realft(
                    workspace,
                    offset,
                    n,
                    1);

                // undo hermitian packed
                workspace[offset + n] = workspace[offset + 1];
                workspace[offset + n + 1] = 0;
                workspace[offset + 1] = 0;
            }

            public override void FFTinv()
            {
                // redo hermitian packed
                workspace[offset + 1] = workspace[offset + n];

                FFT_realft(
                    workspace,
                    offset,
                    n,
                    -1);
            }

            public override void Dispose()
            {
                if (this.hWorkspace.IsAllocated)
                {
                    this.hWorkspace.Free();
                }

                GC.SuppressFinalize(this);
            }

            ~FFTSimple()
            {
#if DEBUG
                Debug.Assert(false, "FFTSimple finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
                Dispose();
            }

            /* source for this version: */
            /* press, william h., teukolsky, saul a., vetterling, william t., and flannery, brian p. */
            /* Numerical Recipes in C:  The Art of Scientific Computing, Second Edition */
            /* Cambridge University Press */
            /* 40 West 20th St., New York, NY 10011-4211 */
            /* Copyright 1992 */

            /* replaces data[0..2*nn-1] by its discrete Fourier transform, if isign is input as 1; */
            /* or replaces data[0..2*nn-1] by nn times its inverse discrete Fourier transform, if */
            /* isign is input as -1.  data is a complex array of length nn or, equivalently, a */
            /* real array of length 2*nn.  nn MUST be an integer power of 2 (this is not checked */
            /* for!) */
            private static void FFT_four1(
                float[] data,
                int offset,
                int nn,
                int isign)
            {
                // original code indexed data[1..n] due to it's fortran roots. C/C# code is more natural with zero indices
                offset -= 1;

                int n = nn << 1;
                int j = 1;
                int i = 1;
                int m = 0;
                int mmax = 2;

                /* bit reversal */
                while (i < n)
                {
                    if (j > i)
                    {
                        float temp;

                        /* exchange two complex numbers */
                        temp = data[j + offset];
                        data[j + offset] = data[i + offset];
                        data[i + offset] = temp;
                        temp = data[j + 1 + offset];
                        data[j + 1 + offset] = data[i + 1 + offset];
                        data[i + 1 + offset] = temp;
                    }

                    m = n >> 1;
                    while ((m >= 2) && (j > m))
                    {
                        j = j - m;
                        m = m >> 1;
                    }

                    j = j + m;
                    i = i + 2;
                }

                /* danielson-lanczos algorithm */
                while (n > mmax) /* outer loop executed log2(nn) times */
                {
                    int istep = mmax << 1;
                    double theta = isign * Math.PI * 2 / mmax;
                    double wtemp = Math.Sin(theta * .5);
                    double wpr = -2 * wtemp * wtemp;
                    double wpi = Math.Sin(theta);
                    double wr = 1;
                    double wi = 0;

                    m = 1;
                    while (m < mmax)
                    {
                        i = m;
                        while (i <= n)
                        {
                            float tempr;
                            float tempi;

                            j = i + mmax;
                            tempr = (float)(wr * data[j + offset] - wi * data[j + 1 + offset]);
                            tempi = (float)(wr * data[j + 1 + offset] + wi * data[j + offset]);
                            data[j + offset] = data[i + offset] - tempr;
                            data[j + 1 + offset] = data[i + 1 + offset] - tempi;
                            data[i + offset] = data[i + offset] + tempr;
                            data[i + 1 + offset] = data[i + 1 + offset] + tempi;

                            i = i + istep;
                        }

                        wtemp = wr; /* trigonometric recurrence */
                        wr = wr * wpr - wi * wpi + wr;
                        wi = wi * wpr + wtemp * wpi + wi;
                        m = m + 2;
                    }

                    mmax = istep;
                }
            }

            /* calculates the Fourier transform of a set of n real-valued data points.  replaces */
            /* this data (in array data[0..n-1]) by the positive frequency half of its complex */
            /* Fourier transform.  the real-valued first and last components of the complex transform */
            /* are returned as elements data[0] and data[1], respectively.  n must be a power of 2. */
            /* this routine also calculates the inverse transform of a complex data array if it is */
            /* the transform of real data. */
            private static void FFT_realft(
                float[] data,
                int _offset,
                int n,
                int isign)
            {
                // original code indexed data[1..n] due to it's fortran roots. C/C# code is more natural with zero indices
                int offset = _offset - 1;

                double theta = Math.PI / (n >> 1); /* initialize recurrence */
                float c1 = .5f;
                float c2;
                double wtemp;
                double wpr;
                double wpi;
                double wr;
                double wi;
                int np3;
                int i;

                if (isign == 1)
                {
                    c2 = -.5f;
                    FFT_four1(data, _offset, n >> 1, 1); /* forward transform */
                }
                else
                {
                    c2 = .5f;
                    theta = -theta; /* prepare for inverse transform */
                }

                wtemp = Math.Sin(theta * .5);
                wpr = -2 * wtemp * wtemp;
                wpi = Math.Sin(theta);
                wr = 1 + wpr;
                wi = wpi;
                np3 = n + 3;
                i = 2; /* case i=1 is done separately below */
                while (i <= (n >> 2))
                {
                    int i1 = 2 * i - 1;
                    int i2 = 1 + i1;
                    int i3 = np3 - i2;
                    int i4 = 1 + i3;
                    float h1r = c1 * (data[i1 + offset] + data[i3 + offset]); /* separating the 2 transforms out */
                    float h1i = c1 * (data[i2 + offset] - data[i4 + offset]);
                    float h2r = -c2 * (data[i2 + offset] + data[i4 + offset]);
                    float h2i = c2 * (data[i1 + offset] - data[i3 + offset]);

                    data[i1 + offset] = (float)(h1r + wr * h2r - wi * h2i); /* recombining transforms */
                    data[i2 + offset] = (float)(h1i + wr * h2i + wi * h2r);
                    data[i3 + offset] = (float)(h1r - wr * h2r + wi * h2i);
                    data[i4 + offset] = (float)(-h1i + wr * h2i + wi * h2r);

                    wtemp = wr; /* trig recurrence */
                    wr = wr * wpr - wi * wpi + wr;
                    wi = wi * wpr + wtemp * wpi + wi;

                    i = i + 1;
                }

                if (isign == 1)
                {
                    float h1r = data[1 + offset]; /* squeeze first and last together to get them all into original array */

                    data[1 + offset] = h1r + data[2 + offset];
                    data[2 + offset] = h1r - data[2 + offset];
                }
                else
                {
                    float h1r = data[1 + offset]; /* inverse transform */
                    float TwoOverN;

                    data[1 + offset] = c1 * (h1r + data[2 + offset]);
                    data[2 + offset] = c1 * (h1r - data[2 + offset]);
                    FFT_four1(data, _offset, n >> 1, -1);

                    TwoOverN = (float)2 / n;
                    for (i = 1; i <= n; i += 1)
                    {
                        data[i + offset] *= TwoOverN;
                    }
                }
            }
        }

        // wrapper for FFTW library, see: http://www.fftw.org/
        public class FFTW : FFT
        {
            private const int SizeOfFloat = 4;

            private readonly float[] workspace;
            private readonly GCHandle hWorkspace;
            private readonly int offset;
            private readonly IntPtr planf;
            private readonly IntPtr plani;
#if DEBUG
            private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

            // Must be called once at the beginning of the program
            public static void StaticInitialization(string wisdom)
            {
                Debug.Assert(lockObject == null);
                if (!(lockObject == null))
                {
                    throw new InvalidOperationException();
                }
                lockObject = new object();

                lock (lockObject)
                {
                    if (fftwf_init_threads() == 0)
                    {
                        throw new Exception("Failed to initialize threads for FFTW library");
                    }

                    if (!String.IsNullOrEmpty(wisdom))
                    {
                        int result = fftwf_import_wisdom_from_string(wisdom);
                        Debug.Assert(result == 1); // but if error - nothing we can do
                    }
                }
            }

            public static string CollectWisdom()
            {
                StringBuilder sb = new StringBuilder();

                Debug.Assert(lockObject != null);
                lock (lockObject)
                {
                    fftwf_export_wisdom(delegate(char c, IntPtr pv) { sb.Append(c); }, IntPtr.Zero);
                }

                return sb.ToString();
            }

            public FFTW(int n, int concurrency, FFT share)
                : base(n, 1f / n)
            {
                Debug.Assert(lockObject != null);
                lock (lockObject)
                {
                    if (share != null)
                    {
                        if (n > share.N)
                        {
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }
                        // share workspace
                        this.workspace = share.Base;
                        this.hWorkspace = GCHandle.Alloc(this.workspace, GCHandleType.Pinned);
                        this.offset = share.Offset;
                    }
                    else
                    {
                        // allocate workspace
                        int count = n + 2;
                        this.workspace = new float[count + SynthParamRec.WORKSPACEALIGN];
                        this.hWorkspace = GCHandle.Alloc(this.workspace, GCHandleType.Pinned);
                        this.offset = 0;
                        SynthParamRec.Align(this.hWorkspace.AddrOfPinnedObject(), ref this.offset, SynthParamRec.WORKSPACEALIGN, SizeOfFloat);
                        Debug.Assert(this.offset + count <= this.workspace.Length);
                    }

                    // create plan
                    IntPtr baseAddr = new IntPtr(this.hWorkspace.AddrOfPinnedObject().ToInt64() + this.offset * SizeOfFloat);
                    int align = fftwf_alignment_of(baseAddr);
                    if (align != 0)
                    {
                        // we are not aligning properly for SIMD
                        Dispose();
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    fftwf_plan_with_nthreads(Math.Min(concurrency, 1));
                    this.planf = fftwf_plan_dft_r2c_1d(n, baseAddr, baseAddr, FFTW_DESTROY_INPUT);
                    this.plani = fftwf_plan_dft_c2r_1d(n, baseAddr, baseAddr, FFTW_DESTROY_INPUT);
                }
            }

            public override float[] Base { get { return workspace; } }
            public override int Offset { get { return offset; } }

            [MethodImpl(MethodImplOptions.NoInlining)] // prevent inlining so we can see this on profiler
            public override void FFTfwd()
            {
                fftwf_execute(this.planf); // inherently not hermitian packed
            }

            [MethodImpl(MethodImplOptions.NoInlining)] // prevent inlining so we can see this on profiler
            public override void FFTinv()
            {
                fftwf_execute(this.plani); // inherently not hermitian packed
            }

            public override void Dispose()
            {
                Debug.Assert(lockObject != null);
                lock (lockObject)
                {
                    if (this.hWorkspace.IsAllocated)
                    {
                        this.hWorkspace.Free();
                    }
                    fftwf_destroy_plan(this.planf);
                    fftwf_destroy_plan(this.plani);

                    GC.SuppressFinalize(this);
                }
            }

            ~FFTW()
            {
#if DEBUG
                Debug.Assert(false, "FFTW finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
                Dispose();
            }

            // interop

            private static object lockObject;

            [DllImport("libfftw3f-3.dll", EntryPoint = "fftwf_plan_dft_r2c_1d", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr fftwf_plan_dft_r2c_1d(
                int n,
                [In] IntPtr p_f_in,
                [Out] IntPtr p_f_out,
                uint flags);

            [DllImport("libfftw3f-3.dll", EntryPoint = "fftwf_plan_dft_c2r_1d", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr fftwf_plan_dft_c2r_1d(
                int n,
                [In] IntPtr p_f_in,
                [Out] IntPtr p_f_out,
                uint flags);

            [DllImport("libfftw3f-3.dll", EntryPoint = "fftwf_destroy_plan", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            private static extern void fftwf_destroy_plan(
                IntPtr plan);

            // Only this method is thread-safe
            [DllImport("libfftw3f-3.dll", EntryPoint = "fftwf_execute", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            private static extern void fftwf_execute(
                IntPtr plan);

            [DllImport("libfftw3f-3.dll", EntryPoint = "fftwf_alignment_of", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            private static extern int fftwf_alignment_of(
                IntPtr pv);

            [DllImport("libfftw3f-3.dll", EntryPoint = "fftwf_init_threads", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            private static extern int fftwf_init_threads();

            [DllImport("libfftw3f-3.dll", EntryPoint = "fftwf_plan_with_nthreads", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            private static extern void fftwf_plan_with_nthreads(
                int nthreads);

            private delegate void WriteCharMethod(
                [MarshalAs(UnmanagedType.I1)] char c,
                IntPtr pv);
            [DllImport("libfftw3f-3.dll", EntryPoint = "fftwf_export_wisdom", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            private static extern void fftwf_export_wisdom(
                WriteCharMethod write_char,
                IntPtr pv);

            [DllImport("libfftw3f-3.dll", EntryPoint = "fftwf_import_wisdom_from_string", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            private static extern int fftwf_import_wisdom_from_string(
                [In, MarshalAs(UnmanagedType.LPStr)] string input_string);


            /* documented flags */
            private const uint FFTW_MEASURE = 0;
            private const uint FFTW_DESTROY_INPUT = 1 << 0;
            private const uint FFTW_UNALIGNED = 1 << 1;
            private const uint FFTW_CONSERVE_MEMORY = 1 << 2;
            private const uint FFTW_EXHAUSTIVE = 1 << 3; /* NO_EXHAUSTIVE is default */
            private const uint FFTW_PRESERVE_INPUT = 1 << 4; /* cancels FFTW_DESTROY_INPUT */
            private const uint FFTW_PATIENT = 1 << 5; /* IMPATIENT is default */
            private const uint FFTW_ESTIMATE = 1 << 6;
            private const uint FFTW_WISDOM_ONLY = 1 << 21;
        }
    }
}
