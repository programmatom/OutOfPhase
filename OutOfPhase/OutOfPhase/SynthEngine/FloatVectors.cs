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
#if VECTOR
using System.Numerics;
#endif
using System.Runtime.InteropServices;
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
#if VECTOR
        public const bool EnableVector = true; // enable use of .NET SIMD "Vector" class - also must #define VECTOR
#endif


        /* 2nd order IIR in direct I form coefficients and state */
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
        public static void AssertVectorAligned(
            float[] vector,
            int offset)
        {
#if VECTOR
            GCHandle hVector = GCHandle.Alloc(vector, GCHandleType.Pinned);
            try
            {
                int vectorLength = Vector<float>.Count;
                Debug.Assert((hVector.AddrOfPinnedObject().ToInt64() + offset * sizeof(float)) % (vectorLength * sizeof(float)) == 0);
            }
            finally
            {
                hVector.Free();
            }
#endif
        }

        public static void AssertVectorAligned(
            Fixed64[] vector,
            int offset)
        {
#if VECTOR
            GCHandle hVector = GCHandle.Alloc(vector, GCHandleType.Pinned);
            try
            {
                int vectorLength = Vector<long>.Count;
                Debug.Assert((hVector.AddrOfPinnedObject().ToInt64() + offset * sizeof(long)) % (vectorLength * sizeof(long)) == 0);
            }
            finally
            {
                hVector.Free();
            }
#endif
        }

        public static void AssertVectorAligned(
            double[] vector,
            int offset)
        {
#if VECTOR
            GCHandle hVector = GCHandle.Alloc(vector, GCHandleType.Pinned);
            try
            {
                int vectorLength = Vector<double>.Count;
                Debug.Assert((hVector.AddrOfPinnedObject().ToInt64() + offset * sizeof(double)) % (vectorLength * sizeof(double)) == 0);
            }
            finally
            {
                hVector.Free();
            }
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
                    vectorValue.CopyTo(target, i + offset);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    target[i + offset + 0] = value;
                    target[i + offset + 1] = value;
                    target[i + offset + 2] = value;
                    target[i + offset + 3] = value;
                }
            }

            for (; i < count; i++)
            {
                target[i + offset] = value;
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
                    Vector<float> s = new Vector<float>(source, i + sourceOffset) * vectorSourceScale
                        + new Vector<float>(target, i + targetOffset);
                    s.CopyTo(target, i + targetOffset);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source0 = source[i + sourceOffset + 0];
                    float Source1 = source[i + sourceOffset + 1];
                    float Source2 = source[i + sourceOffset + 2];
                    float Source3 = source[i + sourceOffset + 3];
                    float Target0 = target[i + targetOffset + 0];
                    float Target1 = target[i + targetOffset + 1];
                    float Target2 = target[i + targetOffset + 2];
                    float Target3 = target[i + targetOffset + 3];
                    target[i + targetOffset + 0] = Target0 + sourceScale * Source0;
                    target[i + targetOffset + 1] = Target1 + sourceScale * Source1;
                    target[i + targetOffset + 2] = Target2 + sourceScale * Source2;
                    target[i + targetOffset + 3] = Target3 + sourceScale * Source3;
                }
            }

            for (; i < count; i++)
            {
                target[i + targetOffset] = target[i + targetOffset] + sourceScale * source[i + sourceOffset];
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
                    Vector<float> s = new Vector<float>(source, i + sourceOffset) * vectorFactor;
                    s.CopyTo(target, i + targetOffset);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source0 = source[i + sourceOffset + 0];
                    float Source1 = source[i + sourceOffset + 1];
                    float Source2 = source[i + sourceOffset + 2];
                    float Source3 = source[i + sourceOffset + 3];
                    target[i + targetOffset + 0] = factor * Source0;
                    target[i + targetOffset + 1] = factor * Source1;
                    target[i + targetOffset + 2] = factor * Source2;
                    target[i + targetOffset + 3] = factor * Source3;
                }
            }

            for (; i < count; i++)
            {
                target[i + targetOffset] = factor * source[i + sourceOffset];
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
                    Vector<float> s = new Vector<float>(source, i + sourceOffset);
                    s.CopyTo(target, i + targetOffset);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source0 = source[i + 0 + sourceOffset];
                    float Source1 = source[i + 1 + sourceOffset];
                    float Source2 = source[i + 2 + sourceOffset];
                    float Source3 = source[i + 3 + sourceOffset];
                    target[i + 0 + targetOffset] = Source0;
                    target[i + 1 + targetOffset] = Source1;
                    target[i + 2 + targetOffset] = Source2;
                    target[i + 3 + targetOffset] = Source3;
                }
            }

            for (; i < count; i++)
            {
                target[i + targetOffset] = source[i + sourceOffset];
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
                    Vector<float> s = new Vector<float>(source, i + sourceOffset);
                    s.CopyTo(target, i + targetOffset);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source0 = source[i + 0 + sourceOffset];
                    float Source1 = source[i + 1 + sourceOffset];
                    float Source2 = source[i + 2 + sourceOffset];
                    float Source3 = source[i + 3 + sourceOffset];
                    target[i + 0 + targetOffset] = Source0;
                    target[i + 1 + targetOffset] = Source1;
                    target[i + 2 + targetOffset] = Source2;
                    target[i + 3 + targetOffset] = Source3;
                }
            }

            for (; i < count; i++)
            {
                target[i + targetOffset] = source[i + sourceOffset];
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
                    Vector<float> s = new Vector<float>(source1, i + source1Offset)
                        * new Vector<float>(source2, i + source2Offset)
                        + new Vector<float>(target, i + targetOffset);
                    s.CopyTo(target, i + targetOffset);
                }
            }
            else
#endif
            {
                for (; i <= count - 4; i += 4)
                {
                    float Source10 = source1[i + source1Offset + 0];
                    float Source11 = source1[i + source1Offset + 1];
                    float Source12 = source1[i + source1Offset + 2];
                    float Source13 = source1[i + source1Offset + 3];
                    float Source20 = source2[i + source2Offset + 0];
                    float Source21 = source2[i + source2Offset + 1];
                    float Source22 = source2[i + source2Offset + 2];
                    float Source23 = source2[i + source2Offset + 3];
                    float Target0 = target[i + targetOffset + 0];
                    float Target1 = target[i + targetOffset + 1];
                    float Target2 = target[i + targetOffset + 2];
                    float Target3 = target[i + targetOffset + 3];
                    target[i + targetOffset + 0] = Target0 + Source10 * Source20;
                    target[i + targetOffset + 1] = Target1 + Source11 * Source21;
                    target[i + targetOffset + 2] = Target2 + Source12 * Source22;
                    target[i + targetOffset + 3] = Target3 + Source13 * Source23;
                }
            }

            for (; i < count; i++)
            {
                target[i + targetOffset] = target[i + targetOffset] + source1[i + source1Offset] * source2[i + source2Offset];
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
                interleavedTarget[2 * i + 0 + interleavedTargetOffset] = leftSource[i + leftSourceOffset];
                interleavedTarget[2 * i + 1 + interleavedTargetOffset] = rightSource[i + rightSourceOffset];
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
                leftTarget[i + leftTargetOffset] = interleavedSource[2 * i + 0 + interleavedSourceOffset];
                rightTarget[i + rightTargetOffset] = interleavedSource[2 * i + 1 + interleavedSourceOffset];
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
                    Vector<float> s = Vector.Abs(new Vector<float>(source, i + sourceOffset));
                    s.CopyTo(target, i + targetOffset);
                }
            }
#endif

            for (; i < count; i++)
            {
                target[i + targetOffset] = Math.Abs(source[i + sourceOffset]);
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
                    Vector<float> s = Vector.Abs(new Vector<float>(source, i + sourceOffset));
                    s = s * s;
                    s.CopyTo(target, i + targetOffset);
                }
            }
#endif

            for (; i < count; i++)
            {
                float t = source[i + sourceOffset];
                target[i + targetOffset] = t * t;
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
                    Vector<float> s = Vector.SquareRoot(new Vector<float>(vector, i + offset));
                    s.CopyTo(vector, i + offset);
                }
            }
#endif

            for (; i < count; i++)
            {
                float t = vector[i + offset];
                vector[i + offset] = (float)Math.Sqrt(t);
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
                    Vector<float> s = vectorHalf * (new Vector<float>(sourceA, i + sourceAOffset)
                        + new Vector<float>(sourceB, i + sourceBOffset));
                    s.CopyTo(target, i + targetOffset);
                }
            }
#endif

            for (; i < count; i++)
            {
                float A = sourceA[i + sourceAOffset];
                float B = sourceB[i + sourceBOffset];
                target[i + targetOffset] = .5f * (A + B);
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
                    Vector<float> s = Vector.Max(new Vector<float>(sourceA, i + sourceAOffset),
                        new Vector<float>(sourceB, i + sourceBOffset));
                    s.CopyTo(target, i + targetOffset);
                }
            }
#endif

            for (; i < count; i++)
            {
                float A = sourceA[i + sourceAOffset];
                float B = sourceB[i + sourceBOffset];
                if (A < B)
                {
                    A = B;
                }
                target[i + targetOffset] = A;
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
                        vectorMin = Vector.Min(vectorMin, new Vector<float>(source, i + sourceOffset));
                        vectorMax = Vector.Max(vectorMin, new Vector<float>(source, i + sourceOffset));
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
                float t = source[i + sourceOffset];
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
                        vectorMaxMag = Vector.Max(vectorMaxMag, Vector.Abs(new Vector<float>(source, i + sourceOffset)));
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
                float t = source[i + sourceOffset];
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
                    if (Vector.LessThanAny(vectorMaxMag, Vector.Abs(new Vector<float>(source, i + sourceOffset))))
                    {
                        return true;
                    }
                }
            }
#endif

            for (; i < count; i++)
            {
                float t = source[i + sourceOffset];
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
            int i = 0;

#if DEBUG
            AssertVectorAligned(vector, offset);
#endif
#if VECTOR
            if (EnableVector)
            {
                for (; i <= count - Vector<float>.Count; i += Vector<float>.Count)
                {
                    Vector<float> s = vectorINeg * new Vector<float>(vector, i + offset);
                    s.CopyTo(vector, i + offset);
                }
            }
#endif

            for (; i < count; i += 2)
            {
                float vR = vector[offset + i + 0];
                float vI = vector[offset + i + 1];
                //vector[offset + i + 0] = vR; -- no-op
                vector[offset + i + 1] = -vI;
            }
        }
#if VECTOR
        private static readonly Vector<float> vectorINeg = CreateVectorINegTemplate();
        private static Vector<float> CreateVectorINegTemplate()
        {
            float[] t = new float[Vector<float>.Count];
            for (int i = 0; i < Vector<float>.Count; i += 2)
            {
                t[0] = 1;
                t[1] = -1;
            }
            return new Vector<float>(t);
        }
#endif

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
                    Vector<float> input = new Vector<float>(source, i + sourceOffset);
                    Vector<int> b = Vector.AsVectorInt32(input);
                    b = b & mask;
                    Vector<int> selector = Vector.Equals(mask, b);
                    Vector<float> s = Vector.ConditionalSelect(selector, Vector<float>.Zero, input);
                    s.CopyTo(destination, i + destinationOffset);
                }
            }
#endif

            for (; i < count; i++)
            {
                float v = source[i + sourceOffset];

                if (Single.IsNaN(v) || Single.IsInfinity(v))
                {
                    v = 0;
                }

                destination[i + destinationOffset] = v;
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
                        Vector<float> v = new Vector<float>(source, i + sourceOffset);
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
                int ui = sourceAsInt[i + sourceOffset] & 0x7fffffff;
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
                    float X0 = xinVector[i + 0 + xinVectorOffset];
                    float X1 = xinVector[i + 1 + xinVectorOffset];
                    float X2 = xinVector[i + 2 + xinVectorOffset];
                    float X3 = xinVector[i + 3 + xinVectorOffset];
                    Vector4 Y = cX8 * new Vector4(X8) + cX9 * new Vector4(X9) + cX0 * new Vector4(X0) + cX1 * new Vector4(X1)
                        //+ cX2 * new Vector4(X2) + cX3 * new Vector4(X3)
                        + cY8 * new Vector4(Y8) + cY9 * new Vector4(Y9);
                    float Y0 = Y.X;
                    float Y1 = Y.Y;
                    float Y2 = Y.Z + A0 * X2;
                    float Y3 = Y.W + A1minusB1A0 * X2 + A0 * X3;
                    youtVector[i + 0 + youtVectorOffset] += Y0 * outputScaling;
                    youtVector[i + 1 + youtVectorOffset] += Y1 * outputScaling;
                    youtVector[i + 2 + youtVectorOffset] += Y2 * outputScaling;
                    youtVector[i + 3 + youtVectorOffset] += Y3 * outputScaling;
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
                        X8 = xinVector[count - 2 + xinVectorOffset];
                    }

                    X9 = xinVector[count - 1 + xinVectorOffset];
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
                        X8 = xinVector[count - 2 + xinVectorOffset];
                    }

                    X9 = xinVector[count - 1 + xinVectorOffset];
                }

                /* unrolled to 4 */
                for (; i < count - 3; i += 4)
                {
                    float X0 = xinVector[i + 0 + xinVectorOffset];
                    float Z0 = youtVector[i + 0 + youtVectorOffset];
                    float Y0 = Beta0 + A0 * X0;
                    float Alpha2 = A2 * X0 - B2 * Y0;
                    float Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                    float X1 = xinVector[i + 1 + xinVectorOffset];
                    float Z1 = youtVector[i + 1 + youtVectorOffset];
                    float Y1 = Beta1 + A0 * X1;
                    float Alpha3 = A2 * X1 - B2 * Y1;
                    float Beta2 = Alpha2 + A1 * X1 - B1 * Y1;

                    float X2 = xinVector[i + 2 + xinVectorOffset];
                    float Z2 = youtVector[i + 2 + youtVectorOffset];
                    float Y2 = Beta2 + A0 * X2;
                    float Alpha4 = A2 * X2 - B2 * Y2;
                    float Beta3 = Alpha3 + A1 * X2 - B1 * Y2;

                    float X3 = xinVector[i + 3 + xinVectorOffset];
                    float Z3 = youtVector[i + 3 + youtVectorOffset];
                    float Y3 = Beta3 + A0 * X3;
                    float Alpha5 = A2 * X3 - B2 * Y3;
                    float Beta4 = Alpha4 + A1 * X3 - B1 * Y3;

                    Y8 = Y2;
                    Y9 = Y3;
                    Alpha1 = Alpha5;
                    Beta0 = Beta4;

                    youtVector[i + 0 + youtVectorOffset] = Z0 + Y0 * outputScaling;
                    youtVector[i + 1 + youtVectorOffset] = Z1 + Y1 * outputScaling;
                    youtVector[i + 2 + youtVectorOffset] = Z2 + Y2 * outputScaling;
                    youtVector[i + 3 + youtVectorOffset] = Z3 + Y3 * outputScaling;
                }
            }

            /* cleanup loop */
            for (; i < count; i++)
            {
                float X0 = xinVector[i + xinVectorOffset];
                float Z0 = youtVector[i + youtVectorOffset];
                float Y0 = Beta0 + A0 * X0;
                float Alpha2 = A2 * X0 - B2 * Y0;
                float Beta1 = Alpha1 + A1 * X0 - B1 * Y0;

                Y8 = Y9;
                Y9 = Y0;
                Alpha1 = Alpha2;
                Beta0 = Beta1;

                youtVector[i + youtVectorOffset] = Z0 + Y0 * outputScaling;
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
                    float X0 = xinVector[i + 0 + xinVectorOffset];
                    float X1 = xinVector[i + 1 + xinVectorOffset];
                    float X2 = xinVector[i + 2 + xinVectorOffset];
                    float X3 = xinVector[i + 3 + xinVectorOffset];
                    Vector4 Y = cX8 * new Vector4(X8) + cX9 * new Vector4(X9) + cX0 * new Vector4(X0) + cX1 * new Vector4(X1)
                        //+ cX2 * new Vector4(X2) + cX3 * new Vector4(X3)
                        + cY8 * new Vector4(Y8) + cY9 * new Vector4(Y9);
                    float Y0 = Y.X;
                    float Y1 = Y.Y;
                    float Y2 = Y.Z + A0 * X2;
                    float Y3 = Y.W + A1minusB1A0 * X2 + A0 * X3;
                    youtVector[i + 0 + youtVectorOffset] = Y0;
                    youtVector[i + 1 + youtVectorOffset] = Y1;
                    youtVector[i + 2 + youtVectorOffset] = Y2;
                    youtVector[i + 3 + youtVectorOffset] = Y3;
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
                        X8 = xinVector[count - 2 + xinVectorOffset];
                    }

                    X9 = xinVector[count - 1 + xinVectorOffset];
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
                for (; i < count - 3; i += 4)
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
            for (; i < count - 3; i += 4)
            {
                float X0 = xinVector[i + xinVectorOffset + 0];
                float Z0 = youtVector[i + youtVectorOffset + 0];
                float Y0i = A0 * X0 - B2 * Y8i - B1 * Y9i;
                float Y0 = Y0i + A2 * Y8i + A1 * Y9i;

                float X1 = xinVector[i + xinVectorOffset + 1];
                float Z1 = youtVector[i + youtVectorOffset + 1];
                float Y1i = A0 * X1 - B2 * Y9i - B1 * Y0i;
                float Y1 = Y1i + A2 * Y9i + A1 * Y0i;

                float X2 = xinVector[i + xinVectorOffset + 2];
                float Z2 = youtVector[i + youtVectorOffset + 2];
                float Y2i = A0 * X2 - B2 * Y0i - B1 * Y1i;
                float Y2 = Y2i + A2 * Y0i + A1 * Y1i;

                float X3 = xinVector[i + xinVectorOffset + 3];
                float Z3 = youtVector[i + youtVectorOffset + 3];
                float Y3i = A0 * X3 - B2 * Y1i - B1 * Y2i;
                float Y3 = Y3i + A2 * Y1i + A1 * Y2i;

                Y8i = Y2i;
                Y9i = Y3i;

                youtVector[i + youtVectorOffset + 0] = Z0 + Y0 * outputScaling;
                youtVector[i + youtVectorOffset + 1] = Z1 + Y1 * outputScaling;
                youtVector[i + youtVectorOffset + 2] = Z2 + Y2 * outputScaling;
                youtVector[i + youtVectorOffset + 3] = Z3 + Y3 * outputScaling;
            }

        /* cleanup loop */
        Cleanup:
            for (; i < count; i++)
            {
                float X0 = xinVector[i + xinVectorOffset];
                float Z0 = youtVector[i + youtVectorOffset];
                float Y0i = A0 * X0 - B1 * Y9i - B2 * Y8i;
                float Y0 = Y0i + A1 * Y9i + A2 * Y8i;

                Y8i = Y9i;
                Y9i = Y0i;

                youtVector[i + youtVectorOffset] = Z0 + Y0 * outputScaling;
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
            for (; i < count - 3; i += 4)
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
                float Z0 = youtVector[i + youtVectorOffset];
                float Y0 = A * X0 - B * Y9;

                Y9 = Y0;

                youtVector[i + youtVectorOffset] = Z0 + Y0 * outputScaling;
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
                float f = floatIn[i + floatInOffset] * Factor + Addend;
                fixed64Out[i + fixed64OutOffset] = new Fixed64(f);
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
                    Fixed64 Int64Phase = FrameIndexBufferBase[i + FrameIndexBufferOffset];
                    float RightWeight = Int64Phase.FracF;
                    int ArraySubscript = Int64Phase.Int & FramesMask;

                    /* L+F(R-L) */
                    float LeftValue = WaveData0[ArraySubscript];
                    float RightValue = WaveData0[ArraySubscript + 1];
                    float Result = LeftValue + (RightWeight * (RightValue - LeftValue));

                    Data[i + Offset] = Result * OutputScaling;
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

                    Fixed64 Int64Phase = FrameIndexBufferBase[i + FrameIndexBufferOffset];
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

                    Data[i + Offset] = Result * OutputScaling;
                }
            }
        }

#if true // TODO:experimental - smoothing
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
#endif

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
                vector[i + offset] = Math.Min(Math.Max(vector[i + offset], min), max);
            }
        }
    }
}
