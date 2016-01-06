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
    public static partial class Synthesizer
    {
        public struct LinearTransRec
        {
            public double RightValue;
            public double InverseDifferential;
            public double Countdown; /* double to eliminate conversions */
        }

        /* refill a linear transition with new state information */
        public static void ResetLinearTransition(
            ref LinearTransRec TransRec,
            double Start,
            double Destination,
            int TicksToReach)
        {
            double LocalTicksToReach = TicksToReach;

            TransRec.RightValue = Destination;
            TransRec.InverseDifferential = (Start - Destination) / LocalTicksToReach;
            TransRec.Countdown = LocalTicksToReach;
        }

        /* execute one cycle and return the value */
        public static double LinearTransitionUpdate(ref LinearTransRec TransRec)
        {
            double LocalCountdown = TransRec.Countdown - 1;
            double ReturnValue = TransRec.RightValue + TransRec.InverseDifferential * LocalCountdown;
            TransRec.Countdown = LocalCountdown;

#if DEBUG
            if (Math.Floor(TransRec.Countdown) != TransRec.Countdown)
            {
                // Countdown not integer
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif

            return ReturnValue;
        }

        /* execute multiple cycles and return the value */
        public static double LinearTransitionUpdateMultiple(
            ref LinearTransRec TransRec,
            int NumCycles)
        {
            double LocalCountdown = TransRec.Countdown - NumCycles;
            double ReturnValue = TransRec.RightValue + TransRec.InverseDifferential * LocalCountdown;
            TransRec.Countdown = LocalCountdown;

#if DEBUG
            if (Math.Floor(TransRec.Countdown) != TransRec.Countdown)
            {
                // Countdown not integer
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif

            return ReturnValue;
        }
    }
}
