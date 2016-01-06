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
        public struct AccentRec
        {
            /* accent values */
            public double Accent0;
            public double Accent1;
            public double Accent2;
            public double Accent3;
            public double Accent4;
            public double Accent5;
            public double Accent6;
            public double Accent7;

            /* this should be True if all values are 0 or False if any value isn't 0 */
            public bool AnyNonZero;
        }

        /* create new zeroed accent structure */
        public static void InitializeAccentZero(out AccentRec Accent)
        {
            Accent = new AccentRec();
        }

        /* set contained values for accent structure */
        public static void InitializeAccent(
            ref AccentRec Accent,
            double Accent1,
            double Accent2,
            double Accent3,
            double Accent4,
            double Accent5,
            double Accent6,
            double Accent7,
            double Accent8)
        {
            Accent.Accent0 = Accent1;
            Accent.Accent1 = Accent2;
            Accent.Accent2 = Accent3;
            Accent.Accent3 = Accent4;
            Accent.Accent4 = Accent5;
            Accent.Accent5 = Accent6;
            Accent.Accent6 = Accent7;
            Accent.Accent7 = Accent8;
            Accent.AnyNonZero = (Accent1 != 0) || (Accent2 != 0) || (Accent3 != 0) || (Accent4 != 0)
                || (Accent5 != 0) || (Accent6 != 0) || (Accent7 != 0) || (Accent8 != 0);
        }

        /* set accent member to value */
        public static void SetAccentMemberValue(
            ref AccentRec Accent,
            int Index,
            double Value)
        {
            switch (Index)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case 1:
                    Accent.Accent0 = Value;
                    break;
                case 2:
                    Accent.Accent1 = Value;
                    break;
                case 3:
                    Accent.Accent2 = Value;
                    break;
                case 4:
                    Accent.Accent3 = Value;
                    break;
                case 5:
                    Accent.Accent4 = Value;
                    break;
                case 6:
                    Accent.Accent5 = Value;
                    break;
                case 7:
                    Accent.Accent6 = Value;
                    break;
                case 8:
                    Accent.Accent7 = Value;
                    break;
            }
            if (Value != 0)
            {
                Accent.AnyNonZero = true;
            }
        }

        /* compute accent product */
        public static double AccentProduct(
            ref AccentRec Left,
            ref AccentRec Right)
        {
#if DEBUG
            // zero flag violation
            if (!Right.AnyNonZero && ((Right.Accent0 != 0) || (Right.Accent1 != 0)
                || (Right.Accent2 != 0) || (Right.Accent3 != 0) || (Right.Accent4 != 0)
                || (Right.Accent5 != 0) || (Right.Accent6 != 0) || (Right.Accent7 != 0)))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (!Left.AnyNonZero && ((Left.Accent0 != 0) || (Left.Accent1 != 0)
                || (Left.Accent2 != 0) || (Left.Accent3 != 0) || (Left.Accent4 != 0)
                || (Left.Accent5 != 0) || (Left.Accent6 != 0) || (Left.Accent7 != 0)))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            if (!Right.AnyNonZero && !Left.AnyNonZero)
            {
                return 0;
            }
            else
            {
                return Left.Accent0 * Right.Accent0
                    + Left.Accent1 * Right.Accent1
                    + Left.Accent2 * Right.Accent2
                    + Left.Accent3 * Right.Accent3
                    + Left.Accent4 * Right.Accent4
                    + Left.Accent5 * Right.Accent5
                    + Left.Accent6 * Right.Accent6
                    + Left.Accent7 * Right.Accent7;
            }
        }

        /* compute accent sum */
        public static double AccentProductAdd(
            double AddMe,
            ref AccentRec Left,
            ref AccentRec Right)
        {
#if DEBUG
            // zero flag violation
            if (!Right.AnyNonZero && ((Right.Accent0 != 0) || (Right.Accent1 != 0)
                || (Right.Accent2 != 0) || (Right.Accent3 != 0) || (Right.Accent4 != 0)
                || (Right.Accent5 != 0) || (Right.Accent6 != 0) || (Right.Accent7 != 0)))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (!Left.AnyNonZero && ((Left.Accent0 != 0) || (Left.Accent1 != 0)
                || (Left.Accent2 != 0) || (Left.Accent3 != 0) || (Left.Accent4 != 0)
                || (Left.Accent5 != 0) || (Left.Accent6 != 0) || (Left.Accent7 != 0)))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            if (!Right.AnyNonZero && !Left.AnyNonZero)
            {
                return AddMe;
            }
            else
            {
                return AddMe
                    + Left.Accent0 * Right.Accent0
                    + Left.Accent1 * Right.Accent1
                    + Left.Accent2 * Right.Accent2
                    + Left.Accent3 * Right.Accent3
                    + Left.Accent4 * Right.Accent4
                    + Left.Accent5 * Right.Accent5
                    + Left.Accent6 * Right.Accent6
                    + Left.Accent7 * Right.Accent7;
            }
        }

        /* scale one accent and add another to it.  source and target may be the same. */
        public static void AccentAdd(
            double LeftScale,
            ref AccentRec Left,
            ref AccentRec Right,
            ref AccentRec Target)
        {
            double Left0 = Left.Accent0;
            double Left1 = Left.Accent1;
            double Left2 = Left.Accent2;
            double Left3 = Left.Accent3;
            double Left4 = Left.Accent4;
            double Left5 = Left.Accent5;
            double Left6 = Left.Accent6;
            double Left7 = Left.Accent7;

            double Right0 = Right.Accent0;
            double Right1 = Right.Accent1;
            double Right2 = Right.Accent2;
            double Right3 = Right.Accent3;
            double Right4 = Right.Accent4;
            double Right5 = Right.Accent5;
            double Right6 = Right.Accent6;
            double Right7 = Right.Accent7;

            Target.Accent0 = LeftScale * Left0 + Right0;
            Target.Accent1 = LeftScale * Left1 + Right1;
            Target.Accent2 = LeftScale * Left2 + Right2;
            Target.Accent3 = LeftScale * Left3 + Right3;
            Target.Accent4 = LeftScale * Left4 + Right4;
            Target.Accent5 = LeftScale * Left5 + Right5;
            Target.Accent6 = LeftScale * Left6 + Right6;
            Target.Accent7 = LeftScale * Left7 + Right7;
        }
    }
}
