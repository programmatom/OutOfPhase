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
        /* perform wavetable indirection on a wave */
        /* returns a value between 1 and -1 */
        /* Phase is in 0..Frames (if outside, is modulo-reduced to be in range) */
        /* TableIndex is in 0..NumTables */
        /* Frames must be integer power of 2 */
        public static float WaveTableIndexer(
            double Phase,
            double TableIndex,
            int NumTables,
            int Frames,
            float[][] Matrix)
        {
            Fixed64 FrameIndex;
            float[] WaveData0;
            int ArraySubscript;
            float RightWeight;
            float Result;
            float Left0Value;
            float Right0Value;
            int IntegerTableIndex;

#if DEBUG
            if ((TableIndex < 0) || (TableIndex > NumTables))
            {
                // table index out of range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif

            FrameIndex = (Fixed64)Phase;

            IntegerTableIndex = (int)TableIndex;

            WaveData0 = Matrix[IntegerTableIndex];

            RightWeight = FrameIndex.FracF;
            ArraySubscript = FrameIndex.Int & (Frames - 1);

            /* L+F(R-L) */
            Left0Value = WaveData0[ArraySubscript];
            Right0Value = WaveData0[ArraySubscript + 1];
            Result = Left0Value + (RightWeight * (Right0Value - Left0Value));

            if (IntegerTableIndex != NumTables - 1)
            {
                float[] WaveData1;
                float Wave1Weight;
                float Left1Value;
                float Right1Value;
                float Wave0Temp;

                WaveData1 = Matrix[IntegerTableIndex + 1];
                Wave1Weight = (int)(TableIndex - IntegerTableIndex);

                /* L+F(R-L) -- applied twice */
                Left1Value = WaveData1[ArraySubscript];
                Right1Value = WaveData1[ArraySubscript + 1];
                Wave0Temp = Result;
                Result = Wave0Temp + (Wave1Weight * (Left1Value + (RightWeight
                    * (Right1Value - Left1Value)) - Wave0Temp));
            }
            /* else wave table index is at maximum, so no table+1 to interpolate with. */

            return Result;
        }
    }
}
