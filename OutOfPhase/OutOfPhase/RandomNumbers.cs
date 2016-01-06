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
    public struct ParkAndMiller
    {
        public const int Minimum = 1;
        public const int Maximum = 2147483646;

        private int seed;

        // Seed must be in range - else ArgumentException is thrown
        public ParkAndMiller(int seed)
        {
#if DEBUG
            if ((seed < Minimum) || (seed > Maximum))
            {
                // seed is out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            this.seed = seed;
        }

        // Seed must be in range - else ArgumentException is thrown
        public void SetSeed(int seed)
        {
#if DEBUG
            if ((seed < Minimum) || (seed > Maximum))
            {
                // seed is out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            this.seed = seed;
        }

        // Constains seed to be in range - always succeeds
        public void ConstrainedSetSeed(int seed)
        {
            this.seed = ConstrainSeed(seed);
        }

#if DEBUG
        private const bool CheckParkAndMiller_DBG = true;
#endif

        private const int A = 16807;
        private const int M = 2147483647;
        private const int Q = 127773;
        private const int R = 2836;

        /* this implements the Park and Miller (Communications of the ACM, 1988) Minimal */
        /* Standard random number generator. it returns a number in the range [1..2147483646] */
        /* IMPORTANT: this is a linear congruential generator, a good one, but still has */
        /* all the flaws associated with them, including: */
        /*  - lack of randomness in the low order bits */
        /*  - clustering around planes in higher dimensions */
        /*  - relatively low (2^31-2) period */
        /*  - correlation between successive numbers, especially when they are small */
#if DEBUG
        private static bool Checked_DBG;
#endif
        public int Random()
        {
            int S;
            int lo;
            int hi;

#if DEBUG
            if (CheckParkAndMiller_DBG)
            {
                if (!Checked_DBG)
                {
                    Checked_DBG = true;

                    ParkAndMiller State_DBG = new ParkAndMiller(1);
                    int Value_DBG = -1;
                    for (int Counter_DBG = 1; Counter_DBG <= 10000; Counter_DBG++)
                    {
                        Value_DBG = State_DBG.Random();
                    }
                    if (Value_DBG != 1043618065)
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
            }
#endif

            S = this.seed;

#if DEBUG
            if ((S < Minimum) || (S > Maximum))
            {
                // seed is out of range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif
            hi = S / Q;
            lo = S % Q;
            S = A * lo - R * hi;
            if (S <= 0)
            {
                S += M;
            }

#if DEBUG
            if ((S < Minimum) || (S > Maximum))
            {
                // seed exceeded range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif

            this.seed = S;
            return S;
        }

        /* convert random number to 0 through and including 1 */
        public static double Double0Through1(int rnd)
        {
#if DEBUG
            if ((rnd < Minimum) || (rnd > Maximum))
            {
                // that doesn't look like a park and miller number
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif
            return (rnd - Minimum) / (double)(Maximum - Minimum);
        }

        /* convert random number to 0 up to but excluding 1 */
        public static double Double0ToExcluding1(int rnd)
        {
#if DEBUG
            if ((rnd < Minimum) || (rnd > Maximum))
            {
                // that doesn't look like a park and miller number
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif
            return (rnd - Minimum) / (double)(Maximum - Minimum + 1);
        }

        /* constrain initial PM seed into range */
        public static int ConstrainSeed(int seed)
        {
            return unchecked((int)(((uint)(seed - Minimum) % (Maximum - Minimum + 1)) + Minimum));
        }
    }


    public struct LEcuyer
    {
        public const int LECUYERMINIMUM = 1;
        public const int LECUYERMAXIMUM = 2147483562;
        public const int LECUYERSEED1MIN = LECUYERMINIMUM;
        public const int LECUYERSEED1MAX = LECUYERMAXIMUM;
        public const int LECUYERSEED2MIN = LECUYERMINIMUM;
        public const int LECUYERSEED2MAX = 2147483398;

        private int seed1;
        private int seed2;

        /* this implements the L'Ecuyer (Communications of the ACM, 1988) hybrid 32-bit */
        /* random number generator.  it returns a value in the range [1..2147483562] */
        /* WARNING: no test data or algorithm was supplied in the article, so the */
        /* correctness of this implementation can not be guaranteed. */
        /* the first seed must be in the range [1..2147483562] */
        /* and the second seed in the range [1..2147483398]. */
        /* IMPORTANT: this is an enhancement of the linear congruential generator */
        /* by combining two sequences of different period.  in this case, the period */
        /* is approx. 2.3e18. */
        public int Random()
        {
            int Z;
            int K;
            int seed1;
            int seed2;

            seed1 = this.seed1;
            seed2 = this.seed2;

#if DEBUG
            if ((seed1 < 1) || (seed1 > 2147483562))
            {
                // first seed is out of range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if ((seed2 < 1) || (seed2 > 2147483398))
            {
                // second seed is out of range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif

            K = seed1 / 53668;
            seed1 = 40014 * (seed1 - K * 53668) - K * 12211;
            if (seed1 < 0)
            {
                seed1 = seed1 + 2147483563;
            }
            K = seed2 / 52774;
            seed2 = 40692 * (seed2 - K * 52774) - K * 3791;
            if (seed2 < 0)
            {
                seed2 = seed2 + 2147483399;
            }
            Z = seed1 - seed2;
            if (Z < 1)
            {
                Z = Z + 2147483562;
            }

#if DEBUG
            if ((seed1 < 1) || (seed1 > 2147483562))
            {
                // first seed is out of range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if ((seed2 < 1) || (seed2 > 2147483398))
            {
                // second seed is out of range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if ((LECUYERMINIMUM != 1) || (LECUYERMAXIMUM != 2147483562))
            {
                // limit macros are bad
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if ((Z < LECUYERMINIMUM) || (Z > LECUYERMAXIMUM))
            {
                // return value is outside of limit macro range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif

            this.seed1 = seed1;
            this.seed2 = seed2;
            return Z;
        }
    }
}
