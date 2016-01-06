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
        public class IntervalRec
        {
            public IntervalRec Next;
            public double dStart; /* envelope ticks */
            public double dDuration; /* duration ticks */
        }

        public class SkipSegmentsRec
        {
            public double dElapsedEnvelopeTicks;
            public IntervalRec List;
        }

#if DEBUG
        private static void ValidateSkipSegments(SkipSegmentsRec Segments)
        {
            IntervalRec Interval = Segments.List;
            while (Interval != null)
            {
                if (Interval.dDuration <= 0)
                {
                    // found expired interval
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                Interval = Interval.Next;
            }
        }
#endif

        /* create a new skip segments list */
        public static SkipSegmentsRec NewSkipSegments()
        {
            SkipSegmentsRec Segments = new SkipSegmentsRec();
            return Segments;
        }

        /* union a segment into the set */
        public static void SkipSegmentsAdd(
            SkipSegmentsRec Segments,
            double dStart,
            double dDuration)
        {
#if DEBUG
            if (dDuration < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            if (dDuration <= 0)
            {
                return;
            }

            IntervalRec Interval = new IntervalRec();

            Interval.Next = Segments.List;
            Segments.List = Interval;

            Interval.dStart = dStart;
            Interval.dDuration = dDuration;

#if DEBUG
            ValidateSkipSegments(Segments);
#endif
        }

        /* increment the position counter.  returns True if current cycle should */
        /* be skipped (according to the segment list) */
        public static bool SkipSegmentUpdateOneCycle(
            SkipSegmentsRec Segments,
            double dEnvelopeTicks,
            double dDurationTicks)
        {
            bool CurrentlyInSegment = false;

#if DEBUG
            ValidateSkipSegments(Segments);
#endif

            IntervalRec IntervalTrailer = null;
            IntervalRec Interval = Segments.List;
            while (Interval != null)
            {
                /* decrement current intervals to expiration */
                if (Interval.dStart <= Segments.dElapsedEnvelopeTicks)
                {
                    CurrentlyInSegment = true;

                    Interval.dDuration -= dDurationTicks;
                    if (Interval.dDuration <= 0)
                    {
                        if (IntervalTrailer == null)
                        {
                            Segments.List = Interval.Next;
                        }
                        else
                        {
                            IntervalTrailer.Next = Interval.Next;
                        }
                        Interval = Interval.Next;

                        continue;
                    }
                }

                IntervalTrailer = Interval;
                Interval = Interval.Next;
            }

            Segments.dElapsedEnvelopeTicks += dEnvelopeTicks;

#if DEBUG
            ValidateSkipSegments(Segments);
#endif
            return CurrentlyInSegment;
        }
    }
}
