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
        public const double MINBPM = .00001;

        public class TempoControlRec
        {
            public double DefaultBeatsPerMinute;
            public double CurrentBeatsPerMinute;
            public LinearTransRec BeatsPerMinuteChange;
            public int BeatsPerMinuteChangeCountdown;
        }

        /* create a new tempo control record */
        public static TempoControlRec NewTempoControl(LargeBCDType DefaultBeatsPerMinute)
        {
#if DEBUG
            Debug.Assert(DURATIONUPDATECLOCKRESOLUTION == Constants.Denominator);
#endif

            TempoControlRec Tempo = new TempoControlRec();

            Tempo.DefaultBeatsPerMinute = (double)DefaultBeatsPerMinute;
            Tempo.CurrentBeatsPerMinute = Tempo.DefaultBeatsPerMinute;
            ResetLinearTransition(ref Tempo.BeatsPerMinuteChange, 0, 0, 1);
            Tempo.BeatsPerMinuteChangeCountdown = 0;

            return Tempo;
        }

        /* update the tempo control & return the new value */
        public static double TempoControlUpdate(
            TempoControlRec Tempo,
            int NumTicks)
        {
            int RunoutTicks = NumTicks;
            if (RunoutTicks > Tempo.BeatsPerMinuteChangeCountdown)
            {
                RunoutTicks = Tempo.BeatsPerMinuteChangeCountdown;
            }

            if (RunoutTicks > 0)
            {
                Tempo.CurrentBeatsPerMinute = LinearTransitionUpdateMultiple(
                    ref Tempo.BeatsPerMinuteChange,
                    RunoutTicks);
                Tempo.BeatsPerMinuteChangeCountdown -= RunoutTicks;
            }

            return Tempo.CurrentBeatsPerMinute;
        }

        /* reset tempo to default value */
        public static void TempoControlRestoreDefault(TempoControlRec Tempo)
        {
            Tempo.CurrentBeatsPerMinute = Tempo.DefaultBeatsPerMinute;
            Tempo.BeatsPerMinuteChangeCountdown = 0;
        }

        /* set the tempo to the specified number of beats per minute */
        public static void TempoControlSetBeatsPerMinute(
            TempoControlRec Tempo,
            LargeBCDType NewBeatsPerMinute)
        {
            Tempo.CurrentBeatsPerMinute = (double)NewBeatsPerMinute;
            if (Tempo.CurrentBeatsPerMinute < MINBPM)
            {
                Tempo.CurrentBeatsPerMinute = MINBPM;
            }
            Tempo.BeatsPerMinuteChangeCountdown = 0;
        }

        /* adjust the tempo by adding the specified value to it */
        public static void TempoControlAdjustBeatsPerMinute(
            TempoControlRec Tempo,
            LargeBCDType IncrementBeatsPerMinute)
        {
            Tempo.CurrentBeatsPerMinute += (double)IncrementBeatsPerMinute;
            if (Tempo.CurrentBeatsPerMinute < MINBPM)
            {
                Tempo.CurrentBeatsPerMinute = MINBPM;
            }
            Tempo.BeatsPerMinuteChangeCountdown = 0;
        }

        /* helper for sweeping to new double-precision value */
        private static void TempoControlSweepHelper(
            TempoControlRec Tempo,
            double dNewBPM,
            SmallExtBCDType NumBeatsToReach)
        {
            if (dNewBPM < MINBPM)
            {
                dNewBPM = MINBPM;
            }
            double LocalNumBeatsToReach = (double)NumBeatsToReach;
            if (LocalNumBeatsToReach < 0)
            {
                LocalNumBeatsToReach = 0;
            }
            Tempo.BeatsPerMinuteChangeCountdown = (int)(LocalNumBeatsToReach * (DURATIONUPDATECLOCKRESOLUTION / 4));
            ResetLinearTransition(
                ref Tempo.BeatsPerMinuteChange,
                Tempo.CurrentBeatsPerMinute,
                dNewBPM,
                Tempo.BeatsPerMinuteChangeCountdown);
        }

        /* sweep the tempo to a new value */
        public static void TempoControlSweepToNewValue(
            TempoControlRec Tempo,
            LargeBCDType NewBPM,
            SmallExtBCDType NumBeatsToReach)
        {
            TempoControlSweepHelper(
                Tempo,
                (double)NewBPM,
                NumBeatsToReach);
        }

        /* sweep the tempo to a new value relative to the current value */
        public static void TempoControlSweepToAdjustedValue(
            TempoControlRec Tempo,
            LargeBCDType AdjustBPM,
            SmallExtBCDType NumBeatsToReach)
        {
            TempoControlSweepHelper(
                Tempo,
                Tempo.CurrentBeatsPerMinute + (double)AdjustBPM,
                NumBeatsToReach);
        }
    }
}
