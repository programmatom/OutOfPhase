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
        public class LFOListSpecRec
        {
            public int NumLFOSpecs;
            public LFOSpecRec[] Array;
        }

        /* allocate a new LFO spec list */
        public static LFOListSpecRec NewLFOListSpecifier()
        {
            LFOListSpecRec LFOListSpec = new LFOListSpecRec();

            LFOListSpec.Array = new LFOSpecRec[0];
            //LFOListSpec.NumLFOSpecs = 0;

            return LFOListSpec;
        }

        /* get a LFOSpecRec out of the list */
        public static LFOSpecRec LFOListSpecGetLFOSpec(LFOListSpecRec LFOListSpec, int Index)
        {
#if DEBUG
            if ((Index < 0) || (Index >= LFOListSpec.NumLFOSpecs))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            return LFOListSpec.Array[Index];
        }

        /* create a new LFO spec entry in the list */
        public static void LFOListSpecAppendNewEntry(LFOListSpecRec LFOListSpec, LFOSpecRec NewEntry)
        {
            LFOListSpec.NumLFOSpecs++;
            Array.Resize(ref LFOListSpec.Array, LFOListSpec.NumLFOSpecs);
            LFOListSpec.Array[LFOListSpec.NumLFOSpecs - 1] = NewEntry;
        }

        /* get the number of elements in the list */
        public static int LFOListSpecGetNumElements(LFOListSpecRec LFOListSpec)
        {
            return LFOListSpec.NumLFOSpecs;
        }
    }
}
