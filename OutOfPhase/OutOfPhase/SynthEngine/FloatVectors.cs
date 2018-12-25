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
#if VECTOR
using System.Numerics;
#endif
using System.Runtime.InteropServices;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        // perf work in this file is awaiting features from .NET:
        // https://github.com/dotnet/corefx/issues/5474 Provide a generic API to read from and write to a pointer
        // https://github.com/dotnet/corefx/issues/5106 Add overload for Vector.CopyTo() that supports pointers
        // https://github.com/dotnet/corefx/issues/3741 Change System.Numerics.Vector<T>'s unsafe constructors' access level to public
        // https://github.com/dotnet/corefx/issues/5106 Add overload for Vector.CopyTo() that supports pointers
        //
        // https://github.com/dotnet/coreclr/issues/3210 Looking for a way to elide bounds checks
        //
        // from there, the plan is to use fixed() to get pointers instead of arrays (to suppress bounds check emission)
        // and use the above features to get Vector<T> to be able to access them.

#if VECTOR
        public const bool EnableVector = true; // enable use of .NET SIMD "Vector" class - also must #define VECTOR
#endif


        // Most loops in this file were tested by rewriting as pointer arithmetic using "unsafe" and "fixed" to avoid
        // array bounds checking. Measurements on a Pentium N3520 and AMD A10-8700P showed no measureable difference
        // in program performance. The checked versions were retained for maintainability and robustness.


        /* 2nd order IIR in direct I form coefficients and state */
        [StructLayout(LayoutKind.Auto)]
        public struct IIR2DirectIRec
        {
            /* filter state.  client should initialize these to zero at beginning of run */
            public float X9; /* saved X[-1] */
            public float X8; /* saved X[-2] */
            public float Y9; /* saved Y[-1] */
            public float Y8; /* saved Y[-2] */

            /* filter parameters.  client should initialize these using design formula */
            /* for desired filter. */
            public float A0; /* X[0] coeff */
            public float A1; /* X[-1] coeff */
            public float A2; /* X[-2] coeff */
            public float B1; /* Y[-1] coeff */
            public float B2; /* Y[-2] coeff */
        }

        /* 2nd order IIR in direct II form coefficients and state */
        [StructLayout(LayoutKind.Auto)]
        public struct IIR2DirectIIRec
        {
            /* filter state.  client should initialize these to zero at beginning of run */
            public float Y9; /* saved Y[-1] */
            public float Y8; /* saved Y[-2] */

            /* filter parameters.  client should initialize these using design formula */
            /* for desired filter. */
            public float A0; /* X[0] coeff */
            public float A1; /* X[-1] coeff */
            public float A2; /* X[-2] coeff */
            public float B1; /* Y[-1] coeff */
            public float B2; /* Y[-2] coeff */
        }

        /* 1st order IIR all-pole coefficients and state */
        [StructLayout(LayoutKind.Auto)]
        public struct IIR1AllPoleRec
        {
            /* filter state.  client should initialize these to zero at beginning of run */
            public float Y9; /* saved Y[-1] */

            /* filter parameters.  client should initialize these using design formula */
            /* for desired filter. */
            public float A; /* X[0] coeff */
            public float B; /* Y[-1] coeff */
        }


#if DEBUG
        public unsafe static void AssertVectorAligned(
            float[] vector,
            int offset)
        {
#if VECTOR
            fixed (float* pVector0 = &(vector[0]))
            {
                IntPtr iVector0 = new IntPtr(pVector0);
                int vectorLength = Vector<float>.Count;
                Debug.Assert((iVector0.ToInt64() + offset * sizeof(float)) % (vectorLength * sizeof(float)) == 0);
            }
#endif
        }

        public unsafe static void AssertVectorAligned(
            Fixed64[] vector,
            int offset)
        {
#if VECTOR
            if (Environment.Is64BitProcess)
            {
                fixed (Fixed64* pVector0 = &(vector[0]))
                {
                    IntPtr iVector0 = new IntPtr(pVector0);
                    int vectorLength = Vector<long>.Count;
                    Debug.Assert((iVector0.ToInt64() + offset * sizeof(long)) % (vectorLength * sizeof(long)) == 0);
                }
            }
            // else: compensating for unaligned allocation isn't feasible for 64-bit types in 32-bit mode - see SynthParamRec for explanation
#endif
        }

        public unsafe static void AssertVectorAligned(
            double[] vector,
            int offset)
        {
#if VECTOR
            if (Environment.Is64BitProcess)
            {
                fixed (double* pVector0 = &(vector[0]))
                {
                    IntPtr iVector0 = new IntPtr(pVector0);
                    int vectorLength = Vector<double>.Count;
                    Debug.Assert((iVector0.ToInt64() + offset * sizeof(double)) % (vectorLength * sizeof(double)) == 0);
                }
            }
            // else: compensating for unaligned allocation isn't feasible for 64-bit types in 32-bit mode - see SynthParamRec for explanation
#endif
        }
#endif

        /* set a floating point vector to a constant */
        public static void FloatVectorSet(
            float[] target,
            int offset,
            int count,
            float value)
        {
#if false // TODO: enable if this helps avoid bounds checks -- see https://github.com/dotnet/coreclr/issues/3210
            if ((count < 0) || unchecked((uint)(offset + count) > (uint)target.Length))
            {
                Debug.Assert(false);
                throw new IndexOutOfRangeException();
            }
#endif

            int i = 0;

#if DEBUG
            AssertVectorAligned(target, offset);
#endif
#if VECTOR
            if (EnableVector)
            {
                Vector<float> vectorValue = new Vector<float>(value);
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    vectorValue.CopyTo(target, offset + i);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    target[offset + i + 0] = value;
                    target[offset + i + 1] = value;
                    target[offset + i + 2] = value;
                    target[offset + i + 3] = value;
                }
            }

            for (; i < count; i++)
            {
                target[offset + i] = value;
            }
        }

        /* zero a floating point vector */
        public static void FloatVectorZero(
            float[] target,
            int offset,
            int count)
        {
            FloatVectorSet(target, offset, count, 0);
        }

        /* multiply-accumulate one vector into another */
        public static void FloatVectorMAcc(
            float[] source,
            int sourceOffset,
            float[] target,
            int targetOffset,
            int count,
            float sourceScale)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(source, sourceOffset);
            AssertVectorAligned(target, targetOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                Vector<float> vectorSourceScale = new Vector<float>(sourceScale);
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = new Vector<float>(source, sourceOffset + i) * vectorSourceScale
                        + new Vector<float>(target, targetOffset + i);
                    s.CopyTo(target, targetOffset + i);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source0 = source[sourceOffset + i + 0];
                    float Source1 = source[sourceOffset + i + 1];
                    float Source2 = source[sourceOffset + i + 2];
                    float Source3 = source[sourceOffset + i + 3];
                    float Target0 = target[targetOffset + i + 0];
                    float Target1 = target[targetOffset + i + 1];
                    float Target2 = target[targetOffset + i + 2];
                    float Target3 = target[targetOffset + i + 3];
                    target[targetOffset + i + 0] = Target0 + sourceScale * Source0;
                    target[targetOffset + i + 1] = Target1 + sourceScale * Source1;
                    target[targetOffset + i + 2] = Target2 + sourceScale * Source2;
                    target[targetOffset + i + 3] = Target3 + sourceScale * Source3;
                }
            }

            for (; i < count; i++)
            {
                target[targetOffset + i] = target[targetOffset + i] + sourceScale * source[sourceOffset + i];
            }
        }

        /* accumulate one vector into another */
        public static void FloatVectorAcc(
            float[] source,
            int sourceOffset,
            float[] target,
            int targetOffset,
            int count)
        {
            FloatVectorMAcc(source, sourceOffset, target, targetOffset, count, 1f);
        }

        /* scale a float vector */
        public static void FloatVectorScale(
            float[] source,
            int sourceOffset,
            float[] target,
            int targetOffset,
            int count,
            float factor)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(source, sourceOffset);
            AssertVectorAligned(target, targetOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                Vector<float> vectorFactor = new Vector<float>(factor);
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = new Vector<float>(source, sourceOffset + i) * vectorFactor;
                    s.CopyTo(target, targetOffset + i);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source0 = source[sourceOffset + i + 0];
                    float Source1 = source[sourceOffset + i + 1];
                    float Source2 = source[sourceOffset + i + 2];
                    float Source3 = source[sourceOffset + i + 3];
                    target[targetOffset + i + 0] = factor * Source0;
                    target[targetOffset + i + 1] = factor * Source1;
                    target[targetOffset + i + 2] = factor * Source2;
                    target[targetOffset + i + 3] = factor * Source3;
                }
            }

            for (; i < count; i++)
            {
                target[targetOffset + i] = factor * source[sourceOffset + i];
            }
        }

        /* copy floating point vector.  must not be overlapping */
        public static void FloatVectorCopy(
            float[] source,
            int sourceOffset,
            float[] target,
            int targetOffset,
            int count)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(source, sourceOffset);
            AssertVectorAligned(target, targetOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = new Vector<float>(source, sourceOffset + i);
                    s.CopyTo(target, targetOffset + i);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source0 = source[sourceOffset + i + 0];
                    float Source1 = source[sourceOffset + i + 1];
                    float Source2 = source[sourceOffset + i + 2];
                    float Source3 = source[sourceOffset + i + 3];
                    target[targetOffset + i + 0] = Source0;
                    target[targetOffset + i + 1] = Source1;
                    target[targetOffset + i + 2] = Source2;
                    target[targetOffset + i + 3] = Source3;
                }
            }

            for (; i < count; i++)
            {
                target[targetOffset + i] = source[sourceOffset + i];
            }
        }

        public static void FloatVectorCopyUnaligned(
            float[] source,
            int sourceOffset,
            float[] target,
            int targetOffset,
            int count)
        {
            int i = 0;

#if VECTOR
            if (EnableVector)
            {
                // .NET uses movups instruction, so vector has some perf gain even when unaligned
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = new Vector<float>(source, sourceOffset + i);
                    s.CopyTo(target, targetOffset + i);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source0 = source[sourceOffset + i + 0];
                    float Source1 = source[sourceOffset + i + 1];
                    float Source2 = source[sourceOffset + i + 2];
                    float Source3 = source[sourceOffset + i + 3];
                    target[targetOffset + i + 0] = Source0;
                    target[targetOffset + i + 1] = Source1;
                    target[targetOffset + i + 2] = Source2;
                    target[targetOffset + i + 3] = Source3;
                }
            }

            for (; i < count; i++)
            {
                target[targetOffset + i] = source[sourceOffset + i];
            }
        }

        // target[i] += source1[i] * source2[i]
        public static void FloatVectorProductAccumulate(
            float[] source1,
            int source1Offset,
            float[] source2,
            int source2Offset,
            float[] target,
            int targetOffset,
            int count)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(source1, source1Offset);
            AssertVectorAligned(source2, source2Offset);
            AssertVectorAligned(target, targetOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = new Vector<float>(source1, source1Offset + i)
                        * new Vector<float>(source2, source2Offset + i)
                        + new Vector<float>(target, targetOffset + i);
                    s.CopyTo(target, targetOffset + i);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source10 = source1[source1Offset + i + 0];
                    float Source11 = source1[source1Offset + i + 1];
                    float Source12 = source1[source1Offset + i + 2];
                    float Source13 = source1[source1Offset + i + 3];
                    float Source20 = source2[source2Offset + i + 0];
                    float Source21 = source2[source2Offset + i + 1];
                    float Source22 = source2[source2Offset + i + 2];
                    float Source23 = source2[source2Offset + i + 3];
                    float Target0 = target[targetOffset + i + 0];
                    float Target1 = target[targetOffset + i + 1];
                    float Target2 = target[targetOffset + i + 2];
                    float Target3 = target[targetOffset + i + 3];
                    target[targetOffset + i + 0] = Target0 + Source10 * Source20;
                    target[targetOffset + i + 1] = Target1 + Source11 * Source21;
                    target[targetOffset + i + 2] = Target2 + Source12 * Source22;
                    target[targetOffset + i + 3] = Target3 + Source13 * Source23;
                }
            }

            for (; i < count; i++)
            {
                target[targetOffset + i] = target[targetOffset + i] + source1[source1Offset + i] * source2[source2Offset + i];
            }
        }

        /* interleave two split channels */
        public static void FloatVectorMakeInterleaved(
            float[] leftSource,
            int leftSourceOffset,
            float[] rightSource,
            int rightSourceOffset,
            int frameCount,
            float[] interleavedTarget,
            int interleavedTargetOffset)
        {
#if DEBUG
            AssertVectorAligned(leftSource, leftSourceOffset);
            AssertVectorAligned(rightSource, rightSourceOffset);
            AssertVectorAligned(interleavedTarget, interleavedTargetOffset);
#endif
            for (int i = 0; i < frameCount; i++)
            {
                interleavedTarget[interleavedTargetOffset + 2 * i + 0] = leftSource[leftSourceOffset + i];
                interleavedTarget[interleavedTargetOffset + 2 * i + 1] = rightSource[rightSourceOffset + i];
            }
        }

        /* split two interleaved channels */
        public static void FloatVectorMakeUninterleaved(
            float[] interleavedSource,
            int interleavedSourceOffset,
            float[] leftTarget,
            int leftTargetOffset,
            float[] rightTarget,
            int rightTargetOffset,
            int frameCount)
        {
#if DEBUG
            AssertVectorAligned(interleavedSource, interleavedSourceOffset);
            AssertVectorAligned(leftTarget, leftTargetOffset);
            AssertVectorAligned(rightTarget, rightTargetOffset);
#endif
            for (int i = 0; i < frameCount; i++)
            {
                leftTarget[leftTargetOffset + i] = interleavedSource[interleavedSourceOffset + 2 * i + 0];
                rightTarget[rightTargetOffset + i] = interleavedSource[interleavedSourceOffset + 2 * i + 1];
            }
        }

        /* absolute value over vector */
        public static void FloatVectorAbsVal(
            float[] source,
            int sourceOffset,
            float[] target,
            int targetOffset,
            int count)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(source, sourceOffset);
            AssertVectorAligned(target, targetOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = Vector.Abs(new Vector<float>(source, sourceOffset + i));
                    s.CopyTo(target, targetOffset + i);
                }
            }
#endif

            for (; i < count; i++)
            {
                target[targetOffset + i] = Math.Abs(source[sourceOffset + i]);
            }
        }

        public static void FloatVectorSquare(
            float[] source,
            int sourceOffset,
            float[] target,
            int targetOffset,
            int count)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(source, sourceOffset);
            AssertVectorAligned(target, targetOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = Vector.Abs(new Vector<float>(source, sourceOffset + i));
                    s = s * s;
                    s.CopyTo(target, targetOffset + i);
                }
            }
#endif

            for (; i < count; i++)
            {
                float t = source[sourceOffset + i];
                target[targetOffset + i] = t * t;
            }
        }

        public static void FloatVectorSquareRoot(
            float[] vector,
            int offset,
            int count)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(vector, offset);
#endif
#if VECTOR
            if (EnableVector)
            {
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = Vector.SquareRoot(new Vector<float>(vector, offset + i));
                    s.CopyTo(vector, offset + i);
                }
            }
#endif

            for (; i < count; i++)
            {
                float t = vector[offset + i];
                vector[offset + i] = (float)Math.Sqrt(t);
            }
        }

        /* average two vectors. target may be one of sources */
        public static void FloatVectorAverage(
            float[] sourceA,
            int sourceAOffset,
            float[] sourceB,
            int sourceBOffset,
            float[] target,
            int targetOffset,
            int count)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(sourceA, sourceAOffset);
            AssertVectorAligned(sourceB, sourceBOffset);
            AssertVectorAligned(target, targetOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                Vector<float> vectorHalf = new Vector<float>(.5f);
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = vectorHalf * (new Vector<float>(sourceA, sourceAOffset + i)
                        + new Vector<float>(sourceB, sourceBOffset + i));
                    s.CopyTo(target, targetOffset + i);
                }
            }
#endif

            for (; i < count; i++)
            {
                float A = sourceA[sourceAOffset + i];
                float B = sourceB[sourceBOffset + i];
                target[targetOffset + i] = .5f * (A + B);
            }
        }

        /* pairwise max of two vectors. target may be one of sources */
        public static void FloatVectorMax(
            float[] sourceA,
            int sourceAOffset,
            float[] sourceB,
            int sourceBOffset,
            float[] target,
            int targetOffset,
            int count)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(sourceA, sourceAOffset);
            AssertVectorAligned(sourceB, sourceBOffset);
            AssertVectorAligned(target, targetOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = Vector.Max(new Vector<float>(sourceA, sourceAOffset + i),
                        new Vector<float>(sourceB, sourceBOffset + i));
                    s.CopyTo(target, targetOffset + i);
                }
            }
#endif

            for (; i < count; i++)
            {
                float A = sourceA[sourceAOffset + i];
                float B = sourceB[sourceBOffset + i];
                if (A < B)
                {
                    A = B;
                }
                target[targetOffset + i] = A;
            }
        }

        /* min and max reduction */
        public static void FloatVectorReductionMinMax(
            ref float oldMin,
            ref float oldMax,
            float[] source,
            int sourceOffset,
            int count)
        {
            float min = oldMin;
            float max = oldMax;

            int i = 0;

#if DEBUG
            AssertVectorAligned(source, sourceOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                if (count >= Vector<float>.Count)
                {
                    Vector<float> vectorMin = new Vector<float>(min);
                    Vector<float> vectorMax = new Vector<float>(max);
                    for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                    {
                        vectorMin = Vector.Min(vectorMin, new Vector<float>(source, sourceOffset + i));
                        vectorMax = Vector.Max(vectorMin, new Vector<float>(source, sourceOffset + i));
                    }
                    for (int j = 0; j < Vector<float>.Count; j++)
                    {
                        min = Math.Min(min, vectorMin[j]);
                        max = Math.Max(max, vectorMax[j]);
                    }
                }
            }
#endif

            for (; i < count; i++)
            {
                float t = source[sourceOffset + i];
                if (max < t)
                {
                    max = t;
                }
                if (min > t)
                {
                    min = t;
                }
            }

            oldMin = min;
            oldMax = max;
        }

        /* max magnitude reduction */
        public static void FloatVectorReductionMaxMagnitude(
            ref float oldMaxMag,
            float[] source,
            int sourceOffset,
            int count)
        {
            float maxMag = oldMaxMag;

            int i = 0;

#if DEBUG
            AssertVectorAligned(source, sourceOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                if (count >= Vector<float>.Count)
                {
                    Vector<float> vectorMaxMag = new Vector<float>(maxMag);
                    for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                    {
                        vectorMaxMag = Vector.Max(vectorMaxMag, Vector.Abs(new Vector<float>(source, sourceOffset + i)));
                    }
                    for (int j = 0; j < Vector<float>.Count; j++)
                    {
                        maxMag = Math.Max(maxMag, vectorMaxMag[j]);
                    }
                }
            }
#endif

            for (; i < count; i++)
            {
                float t = source[sourceOffset + i];
                t = Math.Abs(t);
                if (maxMag < t)
                {
                    maxMag = t;
                }
            }

            oldMaxMag = maxMag;
        }

        public static bool FloatVectorReductionMagnitudeExceeded(
            float maxMag,
            float[] source,
            int sourceOffset,
            int count)
        {
            int i = 0;

#if DEBUG
            AssertVectorAligned(source, sourceOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                Vector<float> vectorMaxMag = new Vector<float>(maxMag);
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    if (Vector.LessThanAny(vectorMaxMag, Vector.Abs(new Vector<float>(source, sourceOffset + i))))
                    {
                        return true;
                    }
                }
            }
#endif

            for (; i < count; i++)
            {
                float t = source[sourceOffset + i];
                t = Math.Abs(t);
                if (maxMag < t)
                {
                    return true;
                }
            }

            return false;
        }

        /* pair-wise complex product. Target may be one of A or B */
        public static void FloatVectorMultiplyComplex(
            float[] A,
            int offsetA,
            float[] B,
            int offsetB,
            float[] target,
            int offsetTarget,
            int count)
        {
            Debug.Assert(count % 2 == 0);

            int i = 0;

#if DEBUG
            AssertVectorAligned(A, offsetA);
            AssertVectorAligned(B, offsetB);
            AssertVectorAligned(target, offsetTarget);
#endif
#if VECTOR
            if (EnableVector)
            {
                // TODO: vector (requires 'permute' which is currently missing from System.Numerics)
                // https://github.com/dotnet/corefx/issues/993
                // https://github.com/dotnet/corefx/issues/1168
#if false // proof of concept
                float[] w0 = new float[Vector<float>.Count];
                float[] w1 = new float[Vector<float>.Count];
                float[] w2 = new float[Vector<float>.Count];
                for (int j = 0; j < Vector<float>.Count; j++)
                {
                    w0[j] = (j & 1) == 0 ? -1 : 1;
                }
                Vector<float> vSign = new Vector<float>(w0);
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> vB = new Vector<float>(B, offsetB + i);
                    for (int j = 0; j < Vector<float>.Count; j++)
                    {
                        w0[j] = A[offsetA + i + (j & ~1)];
                        w1[j] = A[offsetA + i + (j | 1)];
                        w2[j] = B[offsetB + i + (j ^ 1)];
                    }
                    // Note: even/odd permutation can be eliminated if the horizontal-add is exposed
                    // see, e.g. http://www.codeproject.com/Articles/874396/Crunching-Numbers-with-AVX-and-AVX
                    Vector<float> vAeven = new Vector<float>(w0);
                    Vector<float> vAodd = new Vector<float>(w1);
                    Vector<float> vBswap = new Vector<float>(w2);

                    Vector<float> r = vB * vAeven + vSign * vBswap * vAodd;
                    r.CopyTo(target, offsetTarget + i);
                }
#endif
            }
#endif

            for (; i < count; i += 2)
            {
                /* multiply ffts to convolve */
                float A0 = A[offsetA + i + 0];
                float A1 = A[offsetA + i + 1];
                float B0 = B[offsetB + i + 0];
                float B1 = B[offsetB + i + 1];
                target[offsetTarget + i + 0] = B0 * A0 - B1 * A1;
                target[offsetTarget + i + 1] = B1 * A0 + B0 * A1;
            }
        }

        // complex conjugate (in place)
        public static void FloatVectorComplexConjugate(
            float[] vector,
            int offset,
            int count)
        {
            Debug.Assert(count % 2 == 0);

            int i = 0;

#if DEBUG
            AssertVectorAligned(vector, offset);
#endif
#if VECTOR
            if (EnableVector)
            {
                Vector<float> vectorINeg = VectorINeg;
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = vectorINeg * new Vector<float>(vector, offset + i);
                    s.CopyTo(vector, offset + i);
                }
            }
#endif

            for (; i < count; i += 2)
            {
                //float vR = vector[offset + i + 0];
                float vI = vector[offset + i + 1];
                //vector[offset + i + 0] = vR; -- no-op
                vector[offset + i + 1] = -vI;
            }
        }
#if VECTOR
        private static readonly Vector<float> VectorINeg = CreateVectorINegTemplate();
        private static Vector<float> CreateVectorINegTemplate()
        {
            float[] t = new float[Vector<float>.Count];
            for (int i = 0; i < Vector<float>.Count; i += 2)
            {
                t[i + 0] = 1;
                t[i + 1] = -1;
            }
            return new Vector<float>(t);
        }
#endif

        public static bool FloatVectorDetectNaNInf(
            float[] source, // permitted to be non-vector-aligned
            int sourceOffset,
            int count)
        {
            int i = 0;

#if VECTOR
            if (EnableVector)
            {
                Vector<int> latch = Vector<int>.Zero;
                Vector<int> mask = new Vector<int>(0x7f800000); // nan/inf
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> input = new Vector<float>(source, sourceOffset + i);
                    Vector<int> b = Vector.AsVectorInt32(input);
                    b = b & mask;
                    latch = latch | Vector.Equals(mask, b);
                }
                if (latch != Vector<int>.Zero)
                {
                    return true;
                }
            }
#endif

            for (; i < count; i++)
            {
                float v = source[sourceOffset + i];

                if (Single.IsNaN(v) || Single.IsInfinity(v))
                {
                    return true;
                }
            }

            return false;
        }

        // remove non-numbers
        public static void FloatVectorCopyReplaceNaNInf(
            float[] source, // permitted to be non-vector-aligned
            int sourceOffset,
            float[] destination,
            int destinationOffset,
            int count)
        {
            int i = 0;

#if DEBUG
            //AssertVectorAligned(source, sourceOffset);
            AssertVectorAligned(destination, destinationOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                Vector<int> mask = new Vector<int>(0x7f800000); // nan/inf
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> input = new Vector<float>(source, sourceOffset + i);
                    Vector<int> b = Vector.AsVectorInt32(input);
                    b = b & mask;
                    Vector<int> selector = Vector.Equals(mask, b);
                    Vector<float> s = Vector.ConditionalSelect(selector, Vector<float>.Zero, input);
                    s.CopyTo(destination, destinationOffset + i);
                }
            }
#endif

            for (; i < count; i++)
            {
                float v = source[sourceOffset + i];

                if (Single.IsNaN(v) || Single.IsInfinity(v))
                {
                    v = 0;
                }

                destination[destinationOffset + i] = v;
            }
        }

        public static void FloatVectorCountDenormals(
            float[] source,
            int sourceOffset,
            int count,
            ref int denormalCountArg)
        {
            int denormalCount = 0;

            int i = 0;

#if DEBUG
            AssertVectorAligned(source, sourceOffset);
#endif
#if VECTOR
            if (EnableVector)
            {
                if (count >= Vector<float>.Count)
                {
                    Vector<int> partialSum = Vector<int>.Zero;
                    Vector<int> unsign = new Vector<int>(0x7fffffff);
                    Vector<int> mask = new Vector<int>(0x7f800000);
                    for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                    {
                        Vector<float> v = new Vector<float>(source, sourceOffset + i);
                        Vector<int> vui = Vector.BitwiseAnd(Vector.AsVectorInt32(v), unsign);
                        partialSum += Vector.AndNot(
                            Vector.Equals(Vector.BitwiseAnd(vui, mask), Vector<int>.Zero),
                            Vector.Equals(vui, Vector<int>.Zero));
                    }
                    for (int j = 0; j < Vector<int>.Count; j++)
                    {
                        denormalCount += -partialSum[j];
                    }
                }
            }
#endif

            int[] sourceAsInt = UnsafeArrayCast.AsInts(source);
            for (; i < count; i++)
            {
                int ui = sourceAsInt[sourceOffset + i] & 0x7fffffff;
                if ((ui != 0) && ((ui & 0x7f800000) == 0))
                {
                    denormalCount++;
                }
            }

            denormalCountArg = unchecked(denormalCountArg + denormalCount);
        }

        // Transformations for SIMD in IIR2DirectI:
        //
        // Using basic form:
        //
        //   Y0 = A0 * X0 + A1 * X9 + A2 * X8 - B1 * Y9 - B2 * Y8
        //
        // recurrence is unrolled as follows: 
        //
        //   Y0 = A0* X0 + A1* X9 + A2* X8 - B1* Y9 - B2* Y8
        //   Y1 = A0* X1 + A1* X0 + A2* X9 - B1* Y0 - B2* Y9
        //   Y2 = A0* X2 + A1* X1 + A2* X0 - B1* Y1 - B2* Y0
        //   Y3 = A0* X3 + A1* X2 + A2* X1 - B1* Y2 - B2* Y1
        //
        // intra-loop dependencies are substituted to create closed form and algebraicly reordered:
        //
        //   Y0 = (0) * X3 +
        //        (0) * X2 +
        //        (0) * X1 +
        //        (A0) * X0 +
        //        (A1) * X9 +
        //        (A2) * X8 +
        //        (-B1) * Y9 +
        //        (-B2) * Y8;
        //   Y1 = (0) * X3 +
        //        (0) * X2 +
        //        (A0) * X1 +
        //        (A1 + -B1*A0) * X0 +
        //        (A2 + -B1*A1) * X9 +
        //        (-B1*A2) * X8 +
        //        (-B1*-B1 + -B2) * Y9 +
        //        (-B1*-B2) * Y8;
        //   Y2 = (0) * X3 +
        //        (A0) * X2 +
        //        (A1 + -B1*A0) * X1 +
        //        (A2 + -B1*A1 + -B1*-B1*A0 + -B2*A0) * X0 +
        //        (-B1*A2 + -B1*-B1*A1 + -B2*A1) * X9 +
        //        (-B1*-B1*A2 + -B2*A2) * X8 +
        //        (-B1*-B1*-B1 + -B1*-B2 + -B2*-B1) * Y9 +
        //        (-B1*-B1*-B2 + -B2*-B2) * Y8;
        //   Y3 = (A0) * X3 +
        //        (A1 + -B1*A0) * X2 +
        //        (A2 + -B1*A1 + -B1*-B1*A0 + -B2*A0) * X1 +
        //        (-B1*A2 + -B1*-B1*A1 + -B1*-B1*-B1*A0 + -B1*-B2*A0 + -B2*A1 + -B2*-B1*A0) * X0 +
        //        (-B1*-B1*A2 + -B1*-B1*-B1*A1 + -B1*-B2*A1 + -B2*A2 + -B2*-B1*A1) * X9 +
        //        (-B1*-B1*-B1*A2 + -B1*-B2*A2 + -B2*-B1*A2) * X8 +
        //        (-B1*-B1*-B1*-B1 + -B1*-B1*-B2 + -B1*-B2*-B1 + -B2*-B1*-B1 + -B2*-B2) * Y9 +
        //        (-B1*-B1*-B1*-B2 + -B1*-B2*-B2 + -B2*-B1*-B2) * Y8;
        //
        // this is factored to:
        //
        //   Y0 = (0) * X3 +
        //        (0) * X2 +
        //        (0) * X1 +
        //        (A0) * X0 +
        //        (A1) * X9 +
        //        (A2) * X8 +
        //        (-B1) * Y9 +
        //        (-B2) * Y8;
        //   Y1 = (0) * X3 +
        //        (0) * X2 +
        //        (A0) * X1 +
        //        (A1 + -B1*A0) * X0 +
        //        (A2 + -B1*A1) * X9 +
        //        (-B1*A2) * X8 +
        //        (B1*B1 + -B2) * Y9 +
        //        (B1*B2) * Y8;
        //   Y2 = (0) * X3 +
        //        (A0) * X2 +
        //        (A1 + -B1*A0) * X1 +
        //        (A2 + -B1*A1 + A0*(B1*B1 + -B2)) * X0 +
        //        (-B1*A2 + A1*(B1*B1 + -B2)) * X9 +
        //        (A2*(B1*B1 + -B2)) * X8 +
        //        (-B1*(B1*B1 + -2*B2)) * Y9 +
        //        (-B2*(B1*B1 + -B2)) * Y8;
        //   Y3 = (A0) * X3 +
        //        (A1 + -B1*A0) * X2 +
        //        (A2 + -B1*A1 + A0*(B1*B1 + -B2)) * X1 +
        //        (A1*(B1*B1 + -B2) + B1*(-A0*(B1*B1 + -2*B2) + -A2)) * X0 +
        //        (B1*(B1*A2 + -A1*(B1*B1 + -2*B2)) + -B2*A2) * X9 +
        //        (-B1*A2*(B1*B1 + -2*B2)) * X8 +
        //        (B1*B1*(B1*B1 + -3*B2) + B2*B2) * Y9 +
        //        (B1*B2*(B1*B1 + -2*B2)) * Y8;
        //
        // finally common subexpressions are extracted in the code below.
        //

        /* apply 2nd order IIR filter in direct I form to an array of values, adding */
        /* result to output array */
        public static void IIR2DirectIMAcc(
            ref IIR2DirectIRec filter,
            float[] xinVector,
            int xinVectorOffset,
            float[] youtVector,
            int youtVectorOffset,
            int count,
            float outputScaling)
        {
            float Y9 = filter.Y9;
            float Y8 = filter.Y8;
            float X9 = filter.X9;
            float X8 = filter.X8;
            float A0 = filter.A0;
            float A1 = filter.A1;
            float A2 = filter.A2;
            float B1 = filter.B1;
            float B2 = filter.B2;

            int i = 0;

            float Alpha1;
            float Alpha0;
            float Beta0;

#if DEBUG
            AssertVectorAligned(xinVector, xinVectorOffset);
            AssertVectorAligned(youtVector, youtVectorOffset);
#endif
#if VECTOR
            if (EnableVector && (count >= Vector<float>.Count * 2))
            {
                float B1B1 = B1 * B1;
                float B1B1minus2B2 = B1B1 - 2 * B2;
                float B1B1minusB2 = B1B1 - B2;
                float B1A0 = B1 * A0;
                float A1minusB1A0 = A1 - B1A0;
                float B1B2 = B1 * B2;
                float B1A2 = B1 * A2;
                float B1A1 = B1 * A1;
                float A2minusB1A1 = A2 - B1A1;
                float A1B1B1minusA1B2 = A1 * B1B1minusB2;
                float c = A2minusB1A1 + A0 * B1B1minusB2;
                Vector4 cX8 = new Vector4(
                    A2,
                    -B1A2,
                    A2 * B1B1minusB2,
                    -B1A2 * B1B1minus2B2);
                Vector4 cX9 = new Vector4(
                    A1,
                    A2minusB1A1,
                    A1B1B1minusA1B2 - B1A2,
                    B1 * B1A2 - B1A1 * B1B1minus2B2 - B2 * A2);
                Vector4 cX0 = new Vector4(
                    A0,
                    A1minusB1A0,
                    c,
                    A1B1B1minusA1B2 - B1A0 * B1B1minus2B2 - B1A2);
                Vector4 cX1 = new Vector4(
                    0,
                    A0,
                    A1minusB1A0,
                    c);
                //Vector4 cX2 = new Vector4(0, 0, A0, A1minusB1A0);
                //Vector4 cX3 = new Vector4(0, 0, 0, A0);
                Vector4 cY8 = new Vector4(
                    -B2,
                    B1B2,
                    -B2 * B1B1minusB2,
                    B1B2 * B1B1minus2B2);
                Vector4 cY9 = new Vector4(
                    -B1,
                    B1B1 - B2,
                    -B1 * B1B1minus2B2,
                    B1B1 * (B1B1 - 3 * B2) + B2 * B2);

                for (; i <= count - 4; i += 4)
                {
                    float X0 = xinVector[xinVectorOffset + i + 0];
                    float X1 = xinVector[xinVectorOffset + i + 1];
                    float X2 = xinVector[xinVectorOffset + i + 2];
                    float X3 = xinVector[xinVectorOffset + i + 3];
                    Vector4 Y = cX8 * new Vector4(X8) + cX9 * new Vector4(X9) + cX0 * new Vector4(X0) + cX1 * new Vector4(X1)
                        //+ cX2 * new Vector4(X2) + cX3 * new Vector4(X3)
                        + cY8 * new Vector4(Y8) + cY9 * new Vector4(Y9);
                    float Y0 = Y.X;
                    float Y1 = Y.Y;
                    float Y2 = Y.Z + A0 * X2;
                    float Y3 = Y.W + A1minusB1A0 * X2 + A0 * X3;
                    youtVector[youtVectorOffset + i + 0] += Y0 * outputScaling;
                    youtVector[youtVectorOffset + i + 1] += Y1 * outputScaling;
                    youtVector[youtVectorOffset + i + 2] += Y2 * outputScaling;
                    youtVector[youtVectorOffset + i + 3] += Y3 * outputScaling;
                    X8 = X2;
                    X9 = X3;
                    Y8 = Y2;
                    Y9 = Y3;
                }

                // adjust offsets and compute values for common cleanup loop

                xinVectorOffset += i;
                youtVectorOffset += i;
                count -= i;
                i = 0;

                Alpha1 = A2 * X9 - B2 * Y9;
                Alpha0 = A2 * X8 - B2 * Y8;
                Beta0 = Alpha0 + A1 * X9 - B1 * Y9;

                /* compute input history */
                if (count > 0)
                {
                    X8 = X9;
                    if (count > 1)
                    {
                        X8 = xinVector[xinVectorOffset + count - 2];
                    }

                    X9 = xinVector[xinVectorOffset + count - 1];
                }
            }
            else
#endif
            {
                /* note: this code originally used the recurrence: */
                /* Y0 = A0 * X0 + A1 * X9 + A2 * X8 - B1 * Y9 - B2 * Y8; */
                /* unrolled to 4, but the pipelined form below exposes */
                /* more parallelism to the scheduler. */

                Alpha1 = A2 * X9 - B2 * Y9;
                Alpha0 = A2 * X8 - B2 * Y8;
                Beta0 = Alpha0 + A1 * X9 - B1 * Y9;

                /* compute input history */
                if (count > 0)
                {
                    X8 = X9;
                    if (count > 1)
                    {
                        X8 = xinVector[xinVectorOffset + count - 2];
                    }

                    X9 = xinVector[xinVectorOffset + count - 1];
                }

                /* unrolled to 4 */
                for (; i <= count - 4; i += 4)
                {
                    float X0 = xinVector[xinVectorOffset + i + 0];
                    float Z0 = youtVector[youtVectorOffset + i + 0];
                    float Y0 = Beta0 + A0 * X0;
                    float Alpha2 = A2 * X0 - B2 * Y0;
                    float Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                    float X1 = xinVector[xinVectorOffset + i + 1];
                    float Z1 = youtVector[youtVectorOffset + i + 1];
                    float Y1 = Beta1 + A0 * X1;
                    float Alpha3 = A2 * X1 - B2 * Y1;
                    float Beta2 = Alpha2 + A1 * X1 - B1 * Y1;

                    float X2 = xinVector[xinVectorOffset + i + 2];
                    float Z2 = youtVector[youtVectorOffset + i + 2];
                    float Y2 = Beta2 + A0 * X2;
                    float Alpha4 = A2 * X2 - B2 * Y2;
                    float Beta3 = Alpha3 + A1 * X2 - B1 * Y2;

                    float X3 = xinVector[xinVectorOffset + i + 3];
                    float Z3 = youtVector[youtVectorOffset + i + 3];
                    float Y3 = Beta3 + A0 * X3;
                    float Alpha5 = A2 * X3 - B2 * Y3;
                    float Beta4 = Alpha4 + A1 * X3 - B1 * Y3;

                    Y8 = Y2;
                    Y9 = Y3;
                    Alpha1 = Alpha5;
                    Beta0 = Beta4;

                    youtVector[youtVectorOffset + i + 0] = Z0 + Y0 * outputScaling;
                    youtVector[youtVectorOffset + i + 1] = Z1 + Y1 * outputScaling;
                    youtVector[youtVectorOffset + i + 2] = Z2 + Y2 * outputScaling;
                    youtVector[youtVectorOffset + i + 3] = Z3 + Y3 * outputScaling;
                }
            }

            /* cleanup loop */
            for (; i < count; i++)
            {
                float X0 = xinVector[xinVectorOffset + i];
                float Z0 = youtVector[youtVectorOffset + i];
                float Y0 = Beta0 + A0 * X0;
                float Alpha2 = A2 * X0 - B2 * Y0;
                float Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                Y8 = Y9;
                Y9 = Y0;
                Alpha1 = Alpha2;
                Beta0 = Beta1;

                youtVector[youtVectorOffset + i] = Z0 + Y0 * outputScaling;
            }

            /* save state back */
            filter.Y9 = Y9;
            filter.Y8 = Y8;
            filter.X9 = X9;
            filter.X8 = X8;
        }

        /* apply 2nd order IIR filter in direct I form to an array of values, overwriting */
        /* the output array */
        public static void IIR2DirectI(
            ref IIR2DirectIRec filter,
            float[] xinVector,
            int xinVectorOffset,
            float[] youtVector,
            int youtVectorOffset,
            int count)
        {
            float Y9 = filter.Y9;
            float Y8 = filter.Y8;
            float X9 = filter.X9;
            float X8 = filter.X8;
            float A0 = filter.A0;
            float A1 = filter.A1;
            float A2 = filter.A2;
            float B1 = filter.B1;
            float B2 = filter.B2;

            int i = 0;

            float Alpha1;
            float Alpha0;
            float Beta0;

#if DEBUG
            AssertVectorAligned(xinVector, xinVectorOffset);
            AssertVectorAligned(youtVector, youtVectorOffset);
#endif
#if VECTOR
            if (EnableVector && (count >= Vector<float>.Count * 2))
            {
                float B1B1 = B1 * B1;
                float B1B1minus2B2 = B1B1 - 2 * B2;
                float B1B1minusB2 = B1B1 - B2;
                float B1A0 = B1 * A0;
                float A1minusB1A0 = A1 - B1A0;
                float B1B2 = B1 * B2;
                float B1A2 = B1 * A2;
                float B1A1 = B1 * A1;
                float A2minusB1A1 = A2 - B1A1;
                float A1B1B1minusA1B2 = A1 * B1B1minusB2;
                float c = A2minusB1A1 + A0 * B1B1minusB2;
                Vector4 cX8 = new Vector4(
                    A2,
                    -B1A2,
                    A2 * B1B1minusB2,
                    -B1A2 * B1B1minus2B2);
                Vector4 cX9 = new Vector4(
                    A1,
                    A2minusB1A1,
                    A1B1B1minusA1B2 - B1A2,
                    B1 * B1A2 - B1A1 * B1B1minus2B2 - B2 * A2);
                Vector4 cX0 = new Vector4(
                    A0,
                    A1minusB1A0,
                    c,
                    A1B1B1minusA1B2 - B1A0 * B1B1minus2B2 - B1A2);
                Vector4 cX1 = new Vector4(
                    0,
                    A0,
                    A1minusB1A0,
                    c);
                //Vector4 cX2 = new Vector4(0, 0, A0, A1minusB1A0);
                //Vector4 cX3 = new Vector4(0, 0, 0, A0);
                Vector4 cY8 = new Vector4(
                    -B2,
                    B1B2,
                    -B2 * B1B1minusB2,
                    B1B2 * B1B1minus2B2);
                Vector4 cY9 = new Vector4(
                    -B1,
                    B1B1 - B2,
                    -B1 * B1B1minus2B2,
                    B1B1 * (B1B1 - 3 * B2) + B2 * B2);

                for (; i <= count - 4; i += 4)
                {
                    float X0 = xinVector[xinVectorOffset + i + 0];
                    float X1 = xinVector[xinVectorOffset + i + 1];
                    float X2 = xinVector[xinVectorOffset + i + 2];
                    float X3 = xinVector[xinVectorOffset + i + 3];
                    Vector4 Y = cX8 * new Vector4(X8) + cX9 * new Vector4(X9) + cX0 * new Vector4(X0) + cX1 * new Vector4(X1)
                        //+ cX2 * new Vector4(X2) + cX3 * new Vector4(X3)
                        + cY8 * new Vector4(Y8) + cY9 * new Vector4(Y9);
                    float Y0 = Y.X;
                    float Y1 = Y.Y;
                    float Y2 = Y.Z + A0 * X2;
                    float Y3 = Y.W + A1minusB1A0 * X2 + A0 * X3;
                    youtVector[youtVectorOffset + i + 0] = Y0;
                    youtVector[youtVectorOffset + i + 1] = Y1;
                    youtVector[youtVectorOffset + i + 2] = Y2;
                    youtVector[youtVectorOffset + i + 3] = Y3;
                    X8 = X2;
                    X9 = X3;
                    Y8 = Y2;
                    Y9 = Y3;
                }

                // adjust offsets and compute values for common cleanup loop

                xinVectorOffset += i;
                youtVectorOffset += i;
                count -= i;
                i = 0;

                Alpha1 = A2 * X9 - B2 * Y9;
                Alpha0 = A2 * X8 - B2 * Y8;
                Beta0 = Alpha0 + A1 * X9 - B1 * Y9;

                /* compute input history */
                if (count > 0)
                {
                    X8 = X9;
                    if (count > 1)
                    {
                        X8 = xinVector[xinVectorOffset + count - 2];
                    }

                    X9 = xinVector[xinVectorOffset + count - 1];
                }
            }
            else
#endif
            {
                /* note: this code originally used the recurrence: */
                /* Y0 = A0 * X0 + A1 * X9 + A2 * X8 - B1 * Y9 - B2 * Y8; */
                /* unrolled to 4, but the pipelined form below exposes */
                /* more parallelism to the scheduler. */

                Alpha1 = A2 * X9 - B2 * Y9;
                Alpha0 = A2 * X8 - B2 * Y8;
                Beta0 = Alpha0 + A1 * X9 - B1 * Y9;

                /* compute input history */
                if (count > 0)
                {
                    X8 = X9;
                    if (count > 1)
                    {
                        X8 = xinVector[xinVectorOffset + count - 2];
                    }

                    X9 = xinVector[xinVectorOffset + count - 1];
                }

                /* unrolled to 4 */
                for (; i <= count - 4; i += 4)
                {
                    float X0 = xinVector[xinVectorOffset + i + 0];
                    float Y0 = Beta0 + A0 * X0;
                    float Alpha2 = A2 * X0 - B2 * Y0;
                    float Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                    float X1 = xinVector[xinVectorOffset + i + 1];
                    float Y1 = Beta1 + A0 * X1;
                    float Alpha3 = A2 * X1 - B2 * Y1;
                    float Beta2 = Alpha2 + A1 * X1 - B1 * Y1;

                    float X2 = xinVector[xinVectorOffset + i + 2];
                    float Y2 = Beta2 + A0 * X2;
                    float Alpha4 = A2 * X2 - B2 * Y2;
                    float Beta3 = Alpha3 + A1 * X2 - B1 * Y2;

                    float X3 = xinVector[xinVectorOffset + i + 3];
                    float Y3 = Beta3 + A0 * X3;
                    float Alpha5 = A2 * X3 - B2 * Y3;
                    float Beta4 = Alpha4 + A1 * X3 - B1 * Y3;

                    Y8 = Y2;
                    Y9 = Y3;
                    Alpha1 = Alpha5;
                    Beta0 = Beta4;

                    youtVector[youtVectorOffset + i + 0] = Y0;
                    youtVector[youtVectorOffset + i + 1] = Y1;
                    youtVector[youtVectorOffset + i + 2] = Y2;
                    youtVector[youtVectorOffset + i + 3] = Y3;
                }
            }

            /* cleanup loop */
            for (; i < count; i++)
            {
                float X0 = xinVector[xinVectorOffset + i];
                float Y0 = Beta0 + A0 * X0;
                float Alpha2 = A2 * X0 - B2 * Y0;
                float Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                Y8 = Y9;
                Y9 = Y0;
                Alpha1 = Alpha2;
                Beta0 = Beta1;

                youtVector[youtVectorOffset + i] = Y0;
            }

            /* save state back */
            filter.Y9 = Y9;
            filter.Y8 = Y8;
            filter.X9 = X9;
            filter.X8 = X8;
        }

        /* apply 2nd order IIR filter in direct I form to an array of values, adding */
        /* result to output array */
        public static void IIR2DirectIMAcc_Parallel4(
            ref IIR2DirectIRec filter1,
            ref IIR2DirectIRec filter2,
            ref IIR2DirectIRec filter3,
            ref IIR2DirectIRec filter4,
            float[] xinVector,
            int xinVectorOffset1,
            int xinVectorOffset2,
            int xinVectorOffset3,
            int xinVectorOffset4,
            float[] youtVector,
            int youtVectorOffset,
            int count,
            float outputScaling1,
            float outputScaling2,
            float outputScaling3,
            float outputScaling4)
        {
            Vector4 Y9 = new Vector4(filter1.Y9, filter2.Y9, filter3.Y9, filter4.Y9);
            Vector4 Y8 = new Vector4(filter1.Y8, filter2.Y8, filter3.Y8, filter4.Y8);
            Vector4 X9 = new Vector4(filter1.X9, filter2.X9, filter3.X9, filter4.X9);
            Vector4 X8 = new Vector4(filter1.X8, filter2.X8, filter3.X8, filter4.X8);
            Vector4 A0 = new Vector4(filter1.A0, filter2.A0, filter3.A0, filter4.A0);
            Vector4 A1 = new Vector4(filter1.A1, filter2.A1, filter3.A1, filter4.A1);
            Vector4 A2 = new Vector4(filter1.A2, filter2.A2, filter3.A2, filter4.A2);
            Vector4 B1 = new Vector4(filter1.B1, filter2.B1, filter3.B1, filter4.B1);
            Vector4 B2 = new Vector4(filter1.B2, filter2.B2, filter3.B2, filter4.B2);

            int i = 0;

            Vector4 Alpha1;
            Vector4 Alpha0;
            Vector4 Beta0;

#if DEBUG
            AssertVectorAligned(xinVector, xinVectorOffset1);
            AssertVectorAligned(xinVector, xinVectorOffset2);
            AssertVectorAligned(xinVector, xinVectorOffset3);
            AssertVectorAligned(xinVector, xinVectorOffset4);
            AssertVectorAligned(youtVector, youtVectorOffset);
#endif

            /* note: this code originally used the recurrence: */
            /* Y0 = A0 * X0 + A1 * X9 + A2 * X8 - B1 * Y9 - B2 * Y8; */
            /* unrolled to 4, but the pipelined form below exposes */
            /* more parallelism to the scheduler. */

            Alpha1 = A2 * X9 - B2 * Y9;
            Alpha0 = A2 * X8 - B2 * Y8;
            Beta0 = Alpha0 + A1 * X9 - B1 * Y9;

            /* compute input history */
            if (count > 0)
            {
                X8 = X9;
                if (count > 1)
                {
                    X8 = new Vector4(
                        xinVector[xinVectorOffset1 + count - 2],
                        xinVector[xinVectorOffset2 + count - 2],
                        xinVector[xinVectorOffset3 + count - 2],
                        xinVector[xinVectorOffset4 + count - 2]);
                }

                X9 = new Vector4(
                    xinVector[xinVectorOffset1 + count - 1],
                    xinVector[xinVectorOffset2 + count - 1],
                    xinVector[xinVectorOffset3 + count - 1],
                    xinVector[xinVectorOffset4 + count - 1]);
            }

            Vector4 outputScalingVector = new Vector4(
                outputScaling1,
                outputScaling2,
                outputScaling3,
                outputScaling4);

#if false // TODO: register pressure is pretty high - would unrolling still help?
            /* unrolled to 4 */
            for (; i <= count - 4; i += 4)
            {
                float X0 = xinVector[xinVectorOffset+i + 0];
                float Z0 = youtVector[youtVectorOffset+i + 0 ];
                float Y0 = Beta0 + A0 * X0;
                float Alpha2 = A2 * X0 - B2 * Y0;
                float Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                float X1 = xinVector[xinVectorOffset+i + 1 ];
                float Z1 = youtVector[youtVectorOffset+i + 1 ];
                float Y1 = Beta1 + A0 * X1;
                float Alpha3 = A2 * X1 - B2 * Y1;
                float Beta2 = Alpha2 + A1 * X1 - B1 * Y1;

                float X2 = xinVector[xinVectorOffset+i + 2 ];
                float Z2 = youtVector[youtVectorOffset+i + 2 ];
                float Y2 = Beta2 + A0 * X2;
                float Alpha4 = A2 * X2 - B2 * Y2;
                float Beta3 = Alpha3 + A1 * X2 - B1 * Y2;

                float X3 = xinVector[xinVectorOffset+i + 3 ];
                float Z3 = youtVector[youtVectorOffset+i + 3 ];
                float Y3 = Beta3 + A0 * X3;
                float Alpha5 = A2 * X3 - B2 * Y3;
                float Beta4 = Alpha4 + A1 * X3 - B1 * Y3;

                Y8 = Y2;
                Y9 = Y3;
                Alpha1 = Alpha5;
                Beta0 = Beta4;

                youtVector[youtVectorOffset+i + 0 ] = Z0 + Y0 * outputScaling;
                youtVector[youtVectorOffset+i + 1 ] = Z1 + Y1 * outputScaling;
                youtVector[youtVectorOffset+i + 2 ] = Z2 + Y2 * outputScaling;
                youtVector[youtVectorOffset+i + 3 ] = Z3 + Y3 * outputScaling;
            }
#endif

            /* cleanup loop */
            for (; i < count; i++)
            {
                Vector4 X0 = new Vector4(
                    xinVector[xinVectorOffset1 + i],
                    xinVector[xinVectorOffset2 + i],
                    xinVector[xinVectorOffset3 + i],
                    xinVector[xinVectorOffset4 + i]);
                float Z0 = youtVector[youtVectorOffset + i];
                Vector4 Y0 = Beta0 + A0 * X0;
                Vector4 Alpha2 = A2 * X0 - B2 * Y0;
                Vector4 Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                Y8 = Y9;
                Y9 = Y0;
                Alpha1 = Alpha2;
                Beta0 = Beta1;

                Vector4 Y0s = Y0 * outputScalingVector;
                float Y0t = Y0s.X + Y0s.Y + Y0s.Z + Y0s.W;
                youtVector[youtVectorOffset + i] = Z0 + Y0t;
            }

            /* save state back */
            filter1.Y9 = Y9.X;
            filter2.Y9 = Y9.Y;
            filter3.Y9 = Y9.Z;
            filter4.Y9 = Y9.W;
            filter1.Y8 = Y8.X;
            filter2.Y8 = Y8.Y;
            filter3.Y8 = Y8.Z;
            filter4.Y8 = Y8.W;
            filter1.X9 = X9.X;
            filter2.X9 = X9.Y;
            filter3.X9 = X9.Z;
            filter4.X9 = X9.W;
            filter1.X8 = X8.X;
            filter2.X8 = X8.Y;
            filter3.X8 = X8.Z;
            filter4.X8 = X8.W;
        }

        /* apply 2nd order IIR filter in direct I form to an array of values, overwriting */
        /* the output array */
        public static void IIR2DirectI_Parallel4(
            ref IIR2DirectIRec filter1,
            ref IIR2DirectIRec filter2,
            ref IIR2DirectIRec filter3,
            ref IIR2DirectIRec filter4,
            float[] xinVector,
            int xinVectorOffset1,
            int xinVectorOffset2,
            int xinVectorOffset3,
            int xinVectorOffset4,
            float[] youtVector,
            int youtVectorOffset1,
            int youtVectorOffset2,
            int youtVectorOffset3,
            int youtVectorOffset4,
            int count)
        {
            Vector4 Y9 = new Vector4(filter1.Y9, filter2.Y9, filter3.Y9, filter4.Y9);
            Vector4 Y8 = new Vector4(filter1.Y8, filter2.Y8, filter3.Y8, filter4.Y8);
            Vector4 X9 = new Vector4(filter1.X9, filter2.X9, filter3.X9, filter4.X9);
            Vector4 X8 = new Vector4(filter1.X8, filter2.X8, filter3.X8, filter4.X8);
            Vector4 A0 = new Vector4(filter1.A0, filter2.A0, filter3.A0, filter4.A0);
            Vector4 A1 = new Vector4(filter1.A1, filter2.A1, filter3.A1, filter4.A1);
            Vector4 A2 = new Vector4(filter1.A2, filter2.A2, filter3.A2, filter4.A2);
            Vector4 B1 = new Vector4(filter1.B1, filter2.B1, filter3.B1, filter4.B1);
            Vector4 B2 = new Vector4(filter1.B2, filter2.B2, filter3.B2, filter4.B2);

            int i = 0;

            Vector4 Alpha1;
            Vector4 Alpha0;
            Vector4 Beta0;

#if DEBUG
            AssertVectorAligned(xinVector, xinVectorOffset1);
            AssertVectorAligned(xinVector, xinVectorOffset2);
            AssertVectorAligned(xinVector, xinVectorOffset3);
            AssertVectorAligned(xinVector, xinVectorOffset4);
            AssertVectorAligned(youtVector, youtVectorOffset1);
            AssertVectorAligned(youtVector, youtVectorOffset2);
            AssertVectorAligned(youtVector, youtVectorOffset3);
            AssertVectorAligned(youtVector, youtVectorOffset4);
#endif

            /* note: this code originally used the recurrence: */
            /* Y0 = A0 * X0 + A1 * X9 + A2 * X8 - B1 * Y9 - B2 * Y8; */
            /* unrolled to 4, but the pipelined form below exposes */
            /* more parallelism to the scheduler. */

            Alpha1 = A2 * X9 - B2 * Y9;
            Alpha0 = A2 * X8 - B2 * Y8;
            Beta0 = Alpha0 + A1 * X9 - B1 * Y9;

            /* compute input history */
            if (count > 0)
            {
                X8 = X9;
                if (count > 1)
                {
                    X8 = new Vector4(
                        xinVector[xinVectorOffset1 + count - 2],
                        xinVector[xinVectorOffset2 + count - 2],
                        xinVector[xinVectorOffset3 + count - 2],
                        xinVector[xinVectorOffset4 + count - 2]);
                }

                X9 = new Vector4(
                    xinVector[xinVectorOffset1 + count - 1],
                    xinVector[xinVectorOffset2 + count - 1],
                    xinVector[xinVectorOffset3 + count - 1],
                    xinVector[xinVectorOffset4 + count - 1]);
            }

#if false // TODO: register pressure is pretty high - would unrolling still help?
            /* unrolled to 4 */
            for (; i <= count - 4; i += 4)
            {
                float X0 = xinVector[xinVectorOffset + i + 0];
                float Y0 = Beta0 + A0 * X0;
                float Alpha2 = A2 * X0 - B2 * Y0;
                float Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                float X1 = xinVector[xinVectorOffset + i + 1];
                float Y1 = Beta1 + A0 * X1;
                float Alpha3 = A2 * X1 - B2 * Y1;
                float Beta2 = Alpha2 + A1 * X1 - B1 * Y1;

                float X2 = xinVector[xinVectorOffset + i + 2];
                float Y2 = Beta2 + A0 * X2;
                float Alpha4 = A2 * X2 - B2 * Y2;
                float Beta3 = Alpha3 + A1 * X2 - B1 * Y2;

                float X3 = xinVector[xinVectorOffset + i + 3];
                float Y3 = Beta3 + A0 * X3;
                float Alpha5 = A2 * X3 - B2 * Y3;
                float Beta4 = Alpha4 + A1 * X3 - B1 * Y3;

                Y8 = Y2;
                Y9 = Y3;
                Alpha1 = Alpha5;
                Beta0 = Beta4;

                youtVector[youtVectorOffset + i + 0] = Y0;
                youtVector[youtVectorOffset + i + 1] = Y1;
                youtVector[youtVectorOffset + i + 2] = Y2;
                youtVector[youtVectorOffset + i + 3] = Y3;
            }
#endif

            /* cleanup loop */
            for (; i < count; i++)
            {
                Vector4 X0 = new Vector4(
                    xinVector[xinVectorOffset1 + i],
                    xinVector[xinVectorOffset2 + i],
                    xinVector[xinVectorOffset3 + i],
                    xinVector[xinVectorOffset4 + i]);
                Vector4 Y0 = Beta0 + A0 * X0;
                Vector4 Alpha2 = A2 * X0 - B2 * Y0;
                Vector4 Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                Y8 = Y9;
                Y9 = Y0;
                Alpha1 = Alpha2;
                Beta0 = Beta1;

                youtVector[youtVectorOffset1 + i] = Y0.X;
                youtVector[youtVectorOffset2 + i] = Y0.Y;
                youtVector[youtVectorOffset3 + i] = Y0.Z;
                youtVector[youtVectorOffset4 + i] = Y0.W;
            }

            /* save state back */
            filter1.Y9 = Y9.X;
            filter2.Y9 = Y9.Y;
            filter3.Y9 = Y9.Z;
            filter4.Y9 = Y9.W;
            filter1.Y8 = Y8.X;
            filter2.Y8 = Y8.Y;
            filter3.Y8 = Y8.Z;
            filter4.Y8 = Y8.W;
            filter1.X9 = X9.X;
            filter2.X9 = X9.Y;
            filter3.X9 = X9.Z;
            filter4.X9 = X9.W;
            filter1.X8 = X8.X;
            filter2.X8 = X8.Y;
            filter3.X8 = X8.Z;
            filter4.X8 = X8.W;
        }

        /* apply 2nd order IIR filter in direct II form to an array of values, adding */
        /* result to output array */
        public static void IIR2DirectIIMAcc(
            ref IIR2DirectIIRec filter,
            float[] xinVector,
            int xinVectorOffset,
            float[] youtVector,
            int youtVectorOffset,
            int count,
            float outputScaling)
        {
            float Y9i = filter.Y9;
            float Y8i = filter.Y8;
            float A0 = filter.A0;
            float A1 = filter.A1;
            float A2 = filter.A2;
            float B1 = filter.B1;
            float B2 = filter.B2;

            int i = 0;

#if DEBUG
            AssertVectorAligned(xinVector, xinVectorOffset);
            AssertVectorAligned(youtVector, youtVectorOffset);
#endif

            /* unrolled to 4 */
            for (; i <= count - 4; i += 4)
            {
                float X0 = xinVector[xinVectorOffset + i + 0];
                float Z0 = youtVector[youtVectorOffset + i + 0];
                float Y0i = A0 * X0 - B2 * Y8i - B1 * Y9i;
                float Y0 = Y0i + A2 * Y8i + A1 * Y9i;

                float X1 = xinVector[xinVectorOffset + i + 1];
                float Z1 = youtVector[youtVectorOffset + i + 1];
                float Y1i = A0 * X1 - B2 * Y9i - B1 * Y0i;
                float Y1 = Y1i + A2 * Y9i + A1 * Y0i;

                float X2 = xinVector[xinVectorOffset + i + 2];
                float Z2 = youtVector[youtVectorOffset + i + 2];
                float Y2i = A0 * X2 - B2 * Y0i - B1 * Y1i;
                float Y2 = Y2i + A2 * Y0i + A1 * Y1i;

                float X3 = xinVector[xinVectorOffset + i + 3];
                float Z3 = youtVector[youtVectorOffset + i + 3];
                float Y3i = A0 * X3 - B2 * Y1i - B1 * Y2i;
                float Y3 = Y3i + A2 * Y1i + A1 * Y2i;

                Y8i = Y2i;
                Y9i = Y3i;

                youtVector[youtVectorOffset + i + 0] = Z0 + Y0 * outputScaling;
                youtVector[youtVectorOffset + i + 1] = Z1 + Y1 * outputScaling;
                youtVector[youtVectorOffset + i + 2] = Z2 + Y2 * outputScaling;
                youtVector[youtVectorOffset + i + 3] = Z3 + Y3 * outputScaling;
            }

            for (; i < count; i++)
            {
                float X0 = xinVector[xinVectorOffset + i];
                float Z0 = youtVector[youtVectorOffset + i];
                float Y0i = A0 * X0 - B1 * Y9i - B2 * Y8i;
                float Y0 = Y0i + A1 * Y9i + A2 * Y8i;

                Y8i = Y9i;
                Y9i = Y0i;

                youtVector[youtVectorOffset + i] = Z0 + Y0 * outputScaling;
            }

            /* save state back */
            filter.Y9 = Y9i;
            filter.Y8 = Y8i;
        }

        /* apply 2nd order IIR filter in direct II form to an array of values, overwriting */
        /* the output array */
        public static void IIR2DirectII(
            ref IIR2DirectIIRec filter,
            float[] xinVector,
            int xinVectorOffset,
            float[] youtVector,
            int youtVectorOffset,
            int count)
        {
            float Y9i = filter.Y9;
            float Y8i = filter.Y8;
            float A0 = filter.A0;
            float A1 = filter.A1;
            float A2 = filter.A2;
            float B1 = filter.B1;
            float B2 = filter.B2;

            int i = 0;

#if DEBUG
            AssertVectorAligned(xinVector, xinVectorOffset);
            AssertVectorAligned(youtVector, youtVectorOffset);
#endif

            /* unrolled to 4 */
            for (; i <= count - 4; i += 4)
            {
                float X0 = xinVector[xinVectorOffset + i + 0];
                float Y0i = A0 * X0 - B2 * Y8i - B1 * Y9i;
                float Y0 = Y0i + A2 * Y8i + A1 * Y9i;

                float X1 = xinVector[xinVectorOffset + i + 1];
                float Y1i = A0 * X1 - B2 * Y9i - B1 * Y0i;
                float Y1 = Y1i + A2 * Y9i + A1 * Y0i;

                float X2 = xinVector[xinVectorOffset + i + 2];
                float Y2i = A0 * X2 - B2 * Y0i - B1 * Y1i;
                float Y2 = Y2i + A2 * Y0i + A1 * Y1i;

                float X3 = xinVector[xinVectorOffset + i + 3];
                float Y3i = A0 * X3 - B2 * Y1i - B1 * Y2i;
                float Y3 = Y3i + A2 * Y1i + A1 * Y2i;

                Y8i = Y2i;
                Y9i = Y3i;

                youtVector[youtVectorOffset + i + 0] = Y0;
                youtVector[youtVectorOffset + i + 1] = Y1;
                youtVector[youtVectorOffset + i + 2] = Y2;
                youtVector[youtVectorOffset + i + 3] = Y3;
            }

            /* cleanup loop */
            for (; i < count; i++)
            {
                float X0 = xinVector[xinVectorOffset + i];
                float Y0i = A0 * X0 - B1 * Y9i - B2 * Y8i;
                float Y0 = Y0i + A1 * Y9i + A2 * Y8i;

                Y8i = Y9i;
                Y9i = Y0i;

                youtVector[youtVectorOffset + i] = Y0;
            }

            /* save state back */
            filter.Y9 = Y9i;
            filter.Y8 = Y8i;
        }

        /* apply 1st order IIR all-pole filter to an array of values, adding */
        /* result to output array */
        public static void IIR1AllPoleMAcc(
            ref IIR1AllPoleRec filter,
            float[] xinVector,
            int xinVectorOffset,
            float[] youtVector,
            int youtVectorOffset,
            int count,
            float outputScaling)
        {
            float Y9 = filter.Y9;
            float A = filter.A;
            float B = filter.B;

            int i;

#if DEBUG
            AssertVectorAligned(xinVector, xinVectorOffset);
            AssertVectorAligned(youtVector, youtVectorOffset);
#endif

            for (i = 0; i < count; i++)
            {
                float X0 = xinVector[xinVectorOffset + i];
                float Z0 = youtVector[youtVectorOffset + i];
                float Y0 = A * X0 - B * Y9;

                Y9 = Y0;

                youtVector[youtVectorOffset + i] = Z0 + Y0 * outputScaling;
            }

            /* save state */
            filter.Y9 = Y9;
        }

        /* apply 1st order IIR all-pole filter to an array of values, overwriting */
        /* the output array */
        public static void IIR1AllPole(
            ref IIR1AllPoleRec filter,
            float[] xinVector,
            int xinVectorOffset,
            float[] youtVector,
            int youtVectorOffset,
            int count)
        {
            float Y9 = filter.Y9;
            float A = filter.A;
            float B = filter.B;

            int i;

#if DEBUG
            AssertVectorAligned(xinVector, xinVectorOffset);
            AssertVectorAligned(youtVector, youtVectorOffset);
#endif

            for (i = 0; i < count; i++)
            {
                float X0 = xinVector[xinVectorOffset + i];
                float Y0 = A * X0 - B * Y9;

                Y9 = Y0;

                youtVector[youtVectorOffset + i] = Y0;
            }

            /* save state */
            filter.Y9 = Y9;
        }

        public static void VectorAssignFixed64FromFloat(
            Fixed64[] fixed64Out,
            int fixed64OutOffset,
            float[] floatIn,
            int floatInOffset,
            int len,
            float Factor,
            float Addend)
        {
#if DEBUG
            AssertVectorAligned(fixed64Out, fixed64OutOffset);
            AssertVectorAligned(floatIn, floatInOffset);
#endif
            for (int i = 0; i < len; i++)
            {
                float f = floatIn[floatInOffset + i] * Factor + Addend;
                fixed64Out[fixed64OutOffset + i] = new Fixed64(f);
            }
        }

        public static void VectorWaveIndex(
            double CurrentWaveTableIndex,
            int NumFrames,
            int NumTables,
            float[][] WaveTableMatrix,
            Fixed64[] FrameIndexBufferBase,
            int FrameIndexBufferOffset,
            float[] Data,
            int Offset,
            int Count,
            float OutputScaling)
        {
#if DEBUG
            AssertVectorAligned(FrameIndexBufferBase, FrameIndexBufferOffset);
            AssertVectorAligned(Data, Offset);
#endif

            int FramesMask = NumFrames - 1;
            Debug.Assert((FramesMask & NumFrames) == 0);

            // TODO: vectorize
            // Requires uint==>float conversion (https://github.com/dotnet/corefx/issues/1605)
            // and gather (to collect FracI portions and wave indexing) (https://github.com/dotnet/corefx/issues/1608)

            if ((int)CurrentWaveTableIndex == NumTables - 1)
            {
                /* end interpolating case */

                float[] WaveData0 = WaveTableMatrix[(int)CurrentWaveTableIndex];

                for (int i = 0; i < Count; i++)
                {
                    Fixed64 Int64Phase = FrameIndexBufferBase[FrameIndexBufferOffset + i];
                    float RightWeight = Int64Phase.FracF;
                    int ArraySubscript = Int64Phase.Int & FramesMask;

                    /* L+F(R-L) */
                    float LeftValue = WaveData0[ArraySubscript];
                    float RightValue = WaveData0[ArraySubscript + 1];
                    float Result = LeftValue + (RightWeight * (RightValue - LeftValue));

                    Data[Offset + i] = Result * OutputScaling;
                }
            }
            else
            {
                /* full interpolating case */

                float[] WaveData0 = WaveTableMatrix[(int)CurrentWaveTableIndex];
                float[] WaveData1 = WaveTableMatrix[(int)CurrentWaveTableIndex + 1];

                for (int i = 0; i < Count; i++)
                {
                    float Wave1Weight = (float)(CurrentWaveTableIndex - (int)CurrentWaveTableIndex);

                    Fixed64 Int64Phase = FrameIndexBufferBase[FrameIndexBufferOffset + i];
                    float RightWeight = Int64Phase.FracF;
                    int ArraySubscript = Int64Phase.Int & FramesMask;

                    /* L+F(R-L) -- applied twice */
                    float Left0Value = WaveData0[ArraySubscript];
                    float Right0Value = WaveData0[ArraySubscript + 1];
                    float Left1Value = WaveData1[ArraySubscript];
                    float Right1Value = WaveData1[ArraySubscript + 1];

                    float Wave0Temp = Left0Value + (RightWeight * (Right0Value - Left0Value));
                    float Result = Wave0Temp + (Wave1Weight * (Left1Value + (RightWeight
                        * (Right1Value - Left1Value)) - Wave0Temp));

                    Data[Offset + i] = Result * OutputScaling;
                }
            }
        }

        public static void FloatVectorAdditiveRecurrence(
            float[] target,
            int targetOffset,
            float initial,
            float final,
            int count)
        {
#if DEBUG
            AssertVectorAligned(target, targetOffset);
#endif

            float increment = (final - initial) / count;

            int i = 0;
            float r = initial;

            for (; i < count; i++)
            {
                r += increment;
                target[targetOffset + i] = r;
            }
        }

        public static void FloatVectorMultiplicativeRecurrence(
            float[] target,
            int targetOffset,
            float initial,
            float final,
            int count)
        {
#if DEBUG
            AssertVectorAligned(target, targetOffset);
#endif

            float sign = 1f;
            if (initial < 0)
            {
                sign = -1f;
            }
#if DEBUG
            float sign2 = 1f;
            if (final < 0)
            {
                sign2 = -1f;
            }
            Debug.Assert(sign == sign2);
#endif
            float adjustedInitial = Math.Max(Math.Abs(initial), (float)DECIBELTHRESHHOLD) * sign;
            float adjustedFinal = Math.Max(Math.Abs(final), (float)DECIBELTHRESHHOLD) * sign;
            float differential = (float)Math.Pow(Math.Abs(adjustedFinal / adjustedInitial), 1f / count);
            Debug.Assert(!Single.IsNaN(differential) && !Single.IsInfinity(differential));

            int i = 0;
            float r = adjustedInitial;

            for (; i < count; i++)
            {
                r *= differential;
                target[targetOffset + i] = r;
            }
        }

        public static void FloatVectorAdditiveRecurrenceFixed(
            double[] target,
            int targetOffset,
            double initial,
            double final,
            int count)
        {
#if DEBUG
            AssertVectorAligned(target, targetOffset);
#endif

            double increment = (final - initial) / count;

            int i = 0;
            double r = initial;

            for (; i < count; i++)
            {
                r += increment;
                target[targetOffset + i] = r;
            }
        }

        public static void FloatVectorMultiplicativeRecurrenceFixed(
            double[] target,
            int targetOffset,
            double initial,
            double final,
            int count)
        {
#if DEBUG
            AssertVectorAligned(target, targetOffset);
#endif

            double sign = 1;
            if (initial < 0)
            {
                sign = -1f;
            }
#if DEBUG
            double sign2 = 1;
            if (final < 0)
            {
                sign2 = -1f;
            }
            Debug.Assert(sign == sign2);
#endif
            double adjustedInitial = Math.Max(Math.Abs(initial), (float)DECIBELTHRESHHOLD) * sign;
            double adjustedFinal = Math.Max(Math.Abs(final), (float)DECIBELTHRESHHOLD) * sign;
            double differential = (float)Math.Pow(Math.Abs(adjustedFinal / adjustedInitial), 1f / count);
            Debug.Assert(!Double.IsNaN(differential) && !Double.IsInfinity(differential));

            int i = 0;
            double r = adjustedInitial;

            for (; i < count; i++)
            {
                r *= differential;
                target[targetOffset + i] = r;
            }
        }

        public static void FloatVectorClamp(
            float[] vector,
            int offset,
            float min,
            float max,
            int count)
        {
#if DEBUG
            AssertVectorAligned(vector, offset);
#endif

            int i = 0;

            for (; i < count; i++)
            {
                vector[offset + i] = Math.Min(Math.Max(vector[offset + i], min), max);
            }
        }
    }
}
