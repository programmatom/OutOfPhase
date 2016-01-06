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
        /* this number is the number of duration update pulses that occur in every whole note. */
        public const int DURATIONUPDATECLOCKRESOLUTION = 64 * 2 * 3 * 5 * 7; // should be equal to Constants.Denominator

        // epsilon used to prevent filter frequency parameters from hitting exclusive limits
        public const double FILTER_FREQ_EPSILON = 0.01;

        // transcendental constants
        public const double TWOPI = 2 * Math.PI;
        public const double NEGTWOPI = -2 * Math.PI;
        public const double HALFPI = Math.PI / 2;
        public const double SQRT2 = 1.41421356237309505;
    }
}
