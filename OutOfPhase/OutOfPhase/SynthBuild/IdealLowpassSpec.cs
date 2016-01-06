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
        public class IdealLPSpecRec
        {
            /* filter order -- must be positive odd number */
            public int Order;

            /* cutoff frequency for the ideal lowpass filter */
            public double Cutoff;
        }

        /* create anew ideal lowpass filter specification */
        public static IdealLPSpecRec NewIdealLowpassSpec(
            double Cutoff,
            int Order)
        {
            IdealLPSpecRec IdealLPSpec = new IdealLPSpecRec();

            IdealLPSpec.Order = Order;
            IdealLPSpec.Cutoff = Cutoff;

            return IdealLPSpec;
        }

        /* get the cutoff frequency for the ideal lowpass filter */
        public static double IdealLowpassSpecGetCutoff(IdealLPSpecRec IdealLPSpec)
        {
            return IdealLPSpec.Cutoff;
        }

        /* get the order for the ideal lowpass filter */
        public static int IdealLowpassSpecGetOrder(IdealLPSpecRec IdealLPSpec)
        {
            return IdealLPSpec.Order;
        }
    }
}
