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
using System.Text;

namespace OutOfPhase
{
    public static partial class Synthesizer
    {
        public class OscillatorListRec
        {
            public int NumOscillators;
            public OscillatorRec[] OscillatorArray;
        }

        /* create a new array of oscillators */
        public static OscillatorListRec NewOscillatorListSpecifier()
        {
            OscillatorListRec OscList = new OscillatorListRec();

            OscList.OscillatorArray = new OscillatorRec[0];
            OscList.NumOscillators = 0;

            return OscList;
        }

        /* append a new oscillator */
        public static void AppendOscillatorToList(
            OscillatorListRec OscList,
            OscillatorRec NewOscillator)
        {
            Array.Resize(ref OscList.OscillatorArray, OscList.OscillatorArray.Length + 1);
            OscList.OscillatorArray[OscList.OscillatorArray.Length - 1] = NewOscillator;
            OscList.NumOscillators++;
        }

        /* get one of the oscillators from the list */
        public static OscillatorRec GetOscillatorFromList(
            OscillatorListRec OscList,
            int Index)
        {
            return OscList.OscillatorArray[Index];
        }

        /* find out how many oscillators there are in the list */
        public static int GetOscillatorListLength(OscillatorListRec OscList)
        {
            return OscList.NumOscillators;
        }
    }
}
