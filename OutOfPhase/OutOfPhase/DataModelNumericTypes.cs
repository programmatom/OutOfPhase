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
using System.Text;

namespace OutOfPhase
{
    public static class SampConv
    {
        public const float FLOATFACTOR8BIT = (float)0x00000080;
        public const float FLOATFACTOR16BIT = (float)0x00008000;
        public const float FLOATFACTOR24BIT = (float)0x00800000;

        public static float SignedByteToFloat(byte b) // byte is -128..127
        {
            int i = (sbyte)b;
            return (float)i * (1f / FLOATFACTOR8BIT);
        }

        public static float UnsignedByteToFloat(byte b) // byte is 0..255 *(i.e. biased)
        {
            int i = (sbyte)b;
            i -= 128; // remove bias
            return (float)i * (1f / FLOATFACTOR8BIT);
        }

        public static float SignedShortToFloat(short s)
        {
            return (float)s * (1f / FLOATFACTOR16BIT);
        }

        public static float SignedTribyteToFloat(int i)
        {
            return (float)i * (1f / FLOATFACTOR24BIT);
        }

        public static byte FloatToSignedByte(float f)
        {
            int i = Math.Min(Math.Max((int)Math.Round(f * FLOATFACTOR8BIT), -0x00000080), 0x0000007f);
            return unchecked((byte)i);
        }

        public static short FloatToSignedShort(float f)
        {
            int i = Math.Min(Math.Max((int)Math.Round(f * FLOATFACTOR16BIT), -0x00008000), 0x00007fff);
            return unchecked((short)i);
        }

        public static int FloatToSignedTribyte(float f)
        {
            int i = Math.Min(Math.Max((int)Math.Round(f * FLOATFACTOR24BIT), -0x00800000), 0x007fffff);
            return unchecked((int)i);
        }

        public static void QuantizeAndClampVector(float[] vector, NumBitsType bits)
        {
            switch (bits)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case NumBitsType.eSample8bit:
                    for (int i = 0; i < vector.Length; i++)
                    {
                        float v = vector[i];
                        float v2 = SignedByteToFloat(FloatToSignedByte(vector[i]));
                        Debug.Assert(v2 == SignedByteToFloat(FloatToSignedByte(vector[i]))); // verify conversion idempotency (i.e. stability)
                        vector[i] = v2;
                    }
                    break;
                case NumBitsType.eSample16bit:
                    for (int i = 0; i < vector.Length; i++)
                    {
                        float v = vector[i];
                        float v2 = SignedShortToFloat(FloatToSignedShort(vector[i]));
                        Debug.Assert(v2 == SignedShortToFloat(FloatToSignedShort(vector[i]))); // verify conversion idempotency (i.e. stability)
                        vector[i] = v2;
                    }
                    break;
                case NumBitsType.eSample24bit:
                    for (int i = 0; i < vector.Length; i++)
                    {
                        float v = vector[i];
                        float v2 = SignedTribyteToFloat(FloatToSignedTribyte(vector[i]));
                        Debug.Assert(v2 == SignedTribyteToFloat(FloatToSignedTribyte(vector[i]))); // verify conversion idempotency (i.e. stability)
                        vector[i] = v2;
                    }
                    break;
            }
        }
    }


    /* small BCD format is 4 digits of bcd: [+-]xX.XXX, represented as an integer value */
    /* that is 1000 (one thousand) times the decimal value (milli-unit precision). */
    /* It's range is -29.999 to 29.999 */
    public struct SmallBCDType : IEquatable<SmallBCDType>
    {
        public short rawInt16;
        public const int SMALLBCDPRECISION = 1000;
        public const short MINVALRAW = -29999;
        public const short MAXVALRAW = 29999;

        private SmallBCDType(short rawInt16)
        {
            this.rawInt16 = rawInt16;
        }

        public static SmallBCDType FromRawInt16(short rawInt16)
        {
            return new SmallBCDType(rawInt16);
        }

        public static explicit operator double(SmallBCDType sbcd)
        {
            return (double)sbcd.rawInt16 / (double)SMALLBCDPRECISION;
        }

        public static explicit operator float(SmallBCDType sbcd)
        {
            return (float)sbcd.rawInt16 / (float)SMALLBCDPRECISION;
        }

        public static explicit operator decimal(SmallBCDType sbcd)
        {
            return new decimal(Math.Abs(sbcd.rawInt16), 0, 0, sbcd.rawInt16 < 0, 3);
        }

        public static explicit operator SmallBCDType(double d)
        {
            double c = (d * SMALLBCDPRECISION) + ((d >= 0) ? 0.5 : -0.5);
            c = Math.Min(Math.Max(c, MINVALRAW), MAXVALRAW);
            return new SmallBCDType((short)c);
        }

        public override string ToString()
        {
            return ((decimal)this).ToString("0.###");
        }

        public bool Equals(SmallBCDType other)
        {
            return this.rawInt16 == other.rawInt16;
        }
    }


    /* large BCD format is 9 digits of bcd: [+-]xXXX.XXXXXX, represented as an integer */
    /* value that is 1 000 000 (one million) times the decimal value (micro-unit precision) */
    /* It's range is -1999.999999 to 1999.999999 */
    public struct LargeBCDType : IEquatable<LargeBCDType>
    {
        public int rawInt32;
        public const int LARGEBCDPRECISION = 1000000;
        public const int MINVALRAW = -2000000000;
        public const int MAXVALRAW = 2000000000;

        private LargeBCDType(int rawInt32)
        {
            this.rawInt32 = rawInt32;
        }

        public static LargeBCDType FromRawInt32(int rawInt32)
        {
            return new LargeBCDType(rawInt32);
        }

        public static explicit operator double(LargeBCDType lbcd)
        {
            return (double)lbcd.rawInt32 / (double)LARGEBCDPRECISION;
        }

        public static explicit operator float(LargeBCDType lbcd)
        {
            return (float)lbcd.rawInt32 / (float)LARGEBCDPRECISION;
        }

        public static explicit operator decimal(LargeBCDType lbcd)
        {
            return new decimal(Math.Abs(lbcd.rawInt32), 0, 0, lbcd.rawInt32 < 0, 6);
        }

        public static explicit operator LargeBCDType(double d)
        {
            double c = (d * LARGEBCDPRECISION) + ((d >= 0) ? 0.5 : -0.5);
            c = Math.Min(Math.Max(c, MINVALRAW), MAXVALRAW);
            return new LargeBCDType((int)c);
        }

        public static explicit operator LargeBCDType(SmallExtBCDType sxbcd)
        {
            return new LargeBCDType(sxbcd.rawInt32 * (LargeBCDType.LARGEBCDPRECISION / SmallExtBCDType.SMALLEXTBCDPRECISION));
        }

        public override string ToString()
        {
            return ((decimal)this).ToString("0.######");
        }

        public bool Equals(LargeBCDType other)
        {
            return this.rawInt32 == other.rawInt32;
        }
    }


    /* small extended BCD format is 8 digits: [+-]xXXXXXX.XXX, represented as an integer */
    /* value that is 1000 (one thousand) times the decimal value. */
    /* It's range is -1999999.999 to 1999999.999 */
    public struct SmallExtBCDType : IEquatable<SmallExtBCDType>
    {
        public int rawInt32;
        public const int SMALLEXTBCDPRECISION = 1000;
        public const int MINVALRAW = -2000000000;
        public const int MAXVALRAW = 2000000000;

        private SmallExtBCDType(int rawInt32)
        {
            this.rawInt32 = rawInt32;
        }

        public static SmallExtBCDType FromRawInt32(int rawInt32)
        {
            return new SmallExtBCDType(rawInt32);
        }

        public static explicit operator double(SmallExtBCDType sxbcd)
        {
            return (double)sxbcd.rawInt32 / (double)SMALLEXTBCDPRECISION;
        }

        public static explicit operator float(SmallExtBCDType sxbcd)
        {
            return (float)sxbcd.rawInt32 / (float)SMALLEXTBCDPRECISION;
        }

        public static explicit operator decimal(SmallExtBCDType sxbcd)
        {
            return new decimal(Math.Abs(sxbcd.rawInt32), 0, 0, sxbcd.rawInt32 < 0, 3);
        }

        public static explicit operator SmallExtBCDType(double d)
        {
            double c = (d * SMALLEXTBCDPRECISION) + ((d >= 0) ? 0.5 : -0.5);
            c = Math.Min(Math.Max(c, MINVALRAW), MAXVALRAW);
            return new SmallExtBCDType((int)c);
        }

        public override string ToString()
        {
            return ((decimal)this).ToString("0.###");
        }

        public bool Equals(SmallExtBCDType other)
        {
            return this.rawInt32 == other.rawInt32;
        }
    }


    /* note: this module does NOT deal with negative numbers!!! */

    public struct FractionRec
    {
        public uint Integer;
        public uint Fraction;
        public uint Denominator;

        public FractionRec(uint Integer, uint Fraction, uint Denominator)
        {
            this.Integer = Integer;
            this.Fraction = Fraction;
            this.Denominator = Denominator;
        }

        /* make fraction.  arguments do not have to be normalized */
        public static void MakeFraction(out FractionRec Fraction, int Integer, int Numerator, int Denominator)
        {
            if (Denominator == 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            if (Denominator < 0)
            {
                Denominator = -Denominator;
                Numerator = -Numerator;
            }
            int Overflow;
            if (Numerator < 0)
            {
                /* round down, even for negative numbers */
                Overflow = (Numerator - (Denominator - 1)) / Denominator;
            }
            else
            {
                Overflow = Numerator / Denominator;
            }
            Integer += Overflow;
            Numerator -= Denominator * Overflow;

            if (Integer < 0)
            {
                // fraction is negative
                Debug.Assert(false);
                throw new ArgumentException();
            }
            Fraction = new FractionRec((uint)Integer, (uint)Numerator, (uint)Denominator);

            ReduceFraction(ref Fraction);
        }

        /* convert a decimal number to a fraction with the specified denominator limit */
        public static void Double2Fraction(double Value, uint Denominator, out FractionRec Fraction)
        {
            if (Value < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            /* add half so that truncation results in value rounded to nearest denominator */
            Value = Value + (double)0.5 / Denominator;

            uint Integer = (uint)Value;
            Fraction = new FractionRec(Integer, (uint)((Value - Integer) * Denominator), Denominator);

            ReduceFraction(ref Fraction);
        }

        /* convert fraction to a double */
        public static double Fraction2Double(FractionRec Fraction)
        {
            if (Fraction.Fraction >= Fraction.Denominator)
            {
                // numerator is larger than denominator
                Debug.Assert(false);
                throw new ArgumentException();
            }

            return Fraction.Integer + ((double)Fraction.Fraction / Fraction.Denominator);
        }

        /* add fractions.  Destination fraction can be one of the source fractions */
        public static void AddFractions(FractionRec Left, FractionRec Right, out FractionRec Dest)
        {
            if ((Left.Fraction >= Left.Denominator) || (Right.Fraction >= Right.Denominator))
            {
                // numerator is larger than denominator
                Debug.Assert(false);
                throw new ArgumentException();
            }

            /* add fractional parts */
            uint FractionTemp;
            uint DenominatorTemp;
            if (Left.Denominator == Right.Denominator)
            {
                /* if the denominators are the same, then adding is really easy */
                DenominatorTemp = Left.Denominator;
                FractionTemp = Left.Fraction + Right.Fraction;
            }
            else
            {
                uint GCF;

                /* if the denominators are not the same, then we need to multiply each */
                /* side by some number so that they will be the same.  finding the greatest */
                /* common factor helps us find the smallest number to multiply by. */
                /* Left->Denominator / GCF = the factors that left has which right needs. */
                /* Right->Denominator / GCF = the factors that right has which left needs. */
                GCF = FindCommonFactors(Left.Denominator, Right.Denominator);
                /* by multiplying the denominators together, then dividing out the extra */
                /* set of common factors, we find the smallest common denominator.  The */
                /* division is performed inside to prevent overflow */
                DenominatorTemp = (Left.Denominator / GCF) * Right.Denominator;
                /* the left and right sides should yield the same denominator */
                if (DenominatorTemp != (Right.Denominator / GCF) * Left.Denominator)
                {
                    // couldn't factor denominators
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                /* since we are multiplying each fraction by N/N, we need to multiply */
                /* the numerators by the same thing we multiplied the denominators by. */
                FractionTemp = Left.Fraction * (Right.Denominator / GCF)
                    + Right.Fraction * (Left.Denominator / GCF);
            }
            /* add the integer components */
            uint IntegerTemp = Left.Integer + Right.Integer;
            /* if there was an overflow in the fractional part, carry it to the integer */
            if (FractionTemp >= DenominatorTemp)
            {
                /* since we are adding, the amount of carry should never be more than 1 */
                FractionTemp -= DenominatorTemp;
                IntegerTemp += 1;
            }
            if (FractionTemp >= DenominatorTemp)
            {
                // numerator is larger than denominator after reduction
                Debug.Assert(false);
                throw new ArgumentException();
            }
            /* store result */
            Dest = new FractionRec(IntegerTemp, FractionTemp, DenominatorTemp);
        }

        /* test to see if the left is greater than the right */
        public static bool FracGreaterThan(FractionRec Left, FractionRec Right)
        {
            if ((Left.Fraction >= Left.Denominator) || (Right.Fraction >= Right.Denominator))
            {
                //numerator is larger than denominator
                Debug.Assert(false);
                throw new ArgumentException();
            }

            if (Left.Integer > Right.Integer)
            {
                /* if the integer portion is bigger, then there's no contest */
                return true;
            }
            else if (Left.Integer < Right.Integer)
            {
                /* same as above */
                return false;
            }
            else
            {
                /* if the integer portions are the same, then we have to compare the */
                /* fractional portions */
                if (Left.Denominator == Right.Denominator)
                {
                    /* if the denominators are the same, then comparison is easy */
                    return Left.Fraction > Right.Fraction;
                }
                else
                {
                    uint GCF;

                    /* if the denominators are not the same, then they have to be */
                    /* made the same.  as before, the GCF is the factors that are */
                    /* common to both sides.  Left->Denominator / GCF is the portion of */
                    /* the left that right needs and Right->Denominator / GCF is the portion */
                    /* of the right that left needs.  We don't care about the new */
                    /* denominator, but we will compare the new numerators. */
                    GCF = FindCommonFactors(Left.Denominator, Right.Denominator);
                    return Left.Fraction * (Right.Denominator / GCF)
                        > Right.Fraction * (Left.Denominator / GCF);
                }
            }
        }

        /* test to see if the left is greater than or equal to the right */
        public static bool FracGreaterEqual(FractionRec Left, FractionRec Right)
        {
            if ((Left.Fraction >= Left.Denominator) || (Right.Fraction >= Right.Denominator))
            {
                // numerator is larger than denominator
                Debug.Assert(false);
                throw new ArgumentException();
            }

            if (Left.Integer > Right.Integer)
            {
                /* if the integer portion is bigger, then there's no contest */
                return true;
            }
            else if (Left.Integer < Right.Integer)
            {
                /* same as above */
                return false;
            }
            else
            {
                /* if the integer portions are the same, then we have to compare the */
                /* fractional portions */
                if (Left.Denominator == Right.Denominator)
                {
                    /* if the denominators are the same, then comparison is easy */
                    return Left.Fraction >= Right.Fraction;
                }
                else
                {
                    uint GCF;

                    /* if the denominators are not the same, then they have to be */
                    /* made the same.  as before, the GCF is the factors that are */
                    /* common to both sides.  Left->Denominator / GCF is the portion of */
                    /* the left that right needs and Right->Denominator / GCF is the portion */
                    /* of the right that left needs.  We don't care about the new */
                    /* denominator, but we will compare the new numerators. */
                    GCF = FindCommonFactors(Left.Denominator, Right.Denominator);
                    return Left.Fraction * (Right.Denominator / GCF)
                        >= Right.Fraction * (Left.Denominator / GCF);
                }
            }
        }

        /* test fractions for equality */
        public static bool FractionsEqual(FractionRec Left, FractionRec Right)
        {
            if ((Left.Fraction >= Left.Denominator) || (Right.Fraction >= Right.Denominator))
            {
                // numerator is larger than denominator
                Debug.Assert(false);
                throw new ArgumentException();
            }

            if (Left.Integer != Right.Integer)
            {
                /* if the integers aren't equal, then it's easy */
                return false;
            }
            else
            {
                /* if the integer portions are the same, then we have to compare the */
                /* fractional portions */
                if (Left.Denominator == Right.Denominator)
                {
                    /* if the denominators are the same, then comparison is easy */
                    return Left.Fraction == Right.Fraction;
                }
                else
                {
                    uint GCF;

                    /* if the denominators are not the same, then they have to be */
                    /* made the same.  as before, the GCF is the factors that are */
                    /* common to both sides.  Left->Denominator / GCF is the portion of */
                    /* the left that right needs and Right->Denominator / GCF is the portion */
                    /* of the right that left needs.  We don't care about the new */
                    /* denominator, but we will compare the new numerators. */
                    GCF = FindCommonFactors(Left.Denominator, Right.Denominator);
                    return Left.Fraction * (Right.Denominator / GCF)
                        == Right.Fraction * (Left.Denominator / GCF);
                }
            }
        }

        /* reduce fraction */
        public static void ReduceFraction(ref FractionRec Frac)
        {
            if (Frac.Fraction >= Frac.Denominator)
            {
                // numerator is larger than denominator
                Debug.Assert(false);
                throw new ArgumentException();
            }

            uint GCF = FindCommonFactors(Frac.Fraction, Frac.Denominator);
            Frac.Fraction = Frac.Fraction / GCF;
            Frac.Denominator = Frac.Denominator / GCF;
        }

        /* multiply fractions.  destination can be one of the sources */
        /* this function will fail on numbers considerably smaller than the */
        /* range of representable fractions. */
        public static void MultFractions(FractionRec Left, FractionRec Right, out FractionRec Dest)
        {
            if ((Left.Fraction >= Left.Denominator) || (Right.Fraction >= Right.Denominator))
            {
                // numerator is larger than denominator
                Debug.Assert(false);
                throw new ArgumentException();
            }

            ReduceFraction(ref Left);
            ReduceFraction(ref Right);
            /* the product of two fractions: A/B * C/D == AC/BD */
            /* here we multiply the denominators */
            uint Denominator = Left.Denominator * Right.Denominator;
            /* here we multiply the numerators */
            uint Numerator = (Left.Integer * Left.Denominator + Left.Fraction)
                * (Right.Integer * Right.Denominator + Right.Fraction);
            /* division gives us the integer part back and the remainder is the numerator */
            Dest = new FractionRec(Numerator / Denominator, Numerator % Denominator, Denominator);
            /* keep denominators under control */
            ReduceFraction(ref Dest);
        }

        /* subtract second fraction from first.  Destination can be one of the sources */
        public static void SubFractions(FractionRec Left, FractionRec Right, out FractionRec Dest)
        {
            if ((Left.Fraction >= Left.Denominator) || (Right.Fraction >= Right.Denominator))
            {
                // numerator is larger than denominator
                Debug.Assert(false);
                throw new ArgumentException();
            }

            /* add fractional parts */
            int FractionTemp;
            int DenominatorTemp;
            if (Left.Denominator == Right.Denominator)
            {
                /* if the denominators are the same, then adding is really easy */
                DenominatorTemp = (int)Left.Denominator;
                FractionTemp = (int)Left.Fraction - (int)Right.Fraction;
            }
            else
            {
                uint GCF;

                /* if the denominators are not the same, then we need to multiply each */
                /* side by some number so that they will be the same.  finding the greatest */
                /* common factor helps us find the smallest number to multiply by. */
                /* Left->Denominator / GCF = the factors that left has which right needs. */
                /* Right->Denominator / GCF = the factors that right has which left needs. */
                GCF = FindCommonFactors(Left.Denominator, Right.Denominator);
                /* by multiplying the denominators together, then dividing out the extra */
                /* set of common factors, we find the smallest common denominator.  The */
                /* division is performed inside to prevent overflow */
                DenominatorTemp = (int)((Left.Denominator / GCF) * Right.Denominator);
                /* the left and right sides should yield the same denominator */
                if (DenominatorTemp != (Right.Denominator / GCF) * Left.Denominator)
                {
                    // couldn't factor denominators
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                /* since we are multiplying each fraction by N/N, we need to multiply */
                /* the numerators by the same thing we multiplied the denominators by. */
                FractionTemp = (int)(Left.Fraction * (Right.Denominator / GCF))
                    - (int)(Right.Fraction * (Left.Denominator / GCF));
            }
            /* add the integer components */
            int IntegerTemp = (int)Left.Integer - (int)Right.Integer;
            /* if there was an overflow in the fractional part, carry it to the integer */
            if (FractionTemp >= DenominatorTemp)
            {
                // overflow occurred when it shouldn't
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (FractionTemp < 0)
            {
                /* since we are adding, the amount of carry should never be more than 1 */
                FractionTemp += DenominatorTemp;
                IntegerTemp -= 1;
            }
            if (FractionTemp < 0)
            {
                // numerator is way too small
                Debug.Assert(false);
                throw new ArgumentException();
            }
            /* store result */
            Dest = new FractionRec((uint)IntegerTemp, (uint)FractionTemp, (uint)DenominatorTemp);
        }

        /* find the common factors of two numbers, using Euclid's algorithm.  the product */
        /* of the common factors is returned (i.e. greatest common factor). */
        public static uint FindCommonFactors(uint Left, uint Right)
        {
            uint A;
            uint Am1;

            /* normalize:  gcd(a, b) = gcd(a, -b); gcd(a, b) = gcd(b, a) */
            if (unchecked((int)Left) < 0)
            {
                Left = unchecked((uint)-(int)Left);
            }
            if (unchecked((int)Right) < 0)
            {
                Right = unchecked((uint)-(int)Right);
            }
            if (Right == 0)
            {
                Right = Left;
                Left = 0;
            }
            if (Right == 0)
            {
                // both factors are zero
                Debug.Assert(false);
                throw new ArgumentException();
            }

            /* initialize recurrence variables */
            Am1 = Left;
            A = Right;

            /* loop until a is divisible by previous a */
            while ((Am1 % A) != 0) /* not A divides Am1 */
            {
                uint T;

                T = A;
                A = Am1 % T;
                Am1 = T;
            }

            return A;
        }
    }
}
