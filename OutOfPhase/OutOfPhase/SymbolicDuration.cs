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
using System.Diagnostics;

namespace OutOfPhase
{
    public static class SymbolicDuration
    {
        /* convert the duration flags into a heap allocated string */
        public static string NumericDurationToString(
            NoteFlags Duration,
            bool DotFlag,
            NoteFlags Division)
        {
            string RootString;
            string DotPrefix;
            string DivisionSuffix;

            if (Division == NoteFlags.eDiv1Modifier)
            {
                /* normal notes */
                switch (Duration)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NoteFlags.e64thNote:
                        RootString = "64th";
                        break;
                    case NoteFlags.e32ndNote:
                        RootString = "32nd";
                        break;
                    case NoteFlags.e16thNote:
                        RootString = "16th";
                        break;
                    case NoteFlags.e8thNote:
                        RootString = "8th";
                        break;
                    case NoteFlags.e4thNote:
                        RootString = "quarter";
                        break;
                    case NoteFlags.e2ndNote:
                        RootString = "half";
                        break;
                    case NoteFlags.eWholeNote:
                        RootString = "whole";
                        break;
                    case NoteFlags.eDoubleNote:
                        RootString = "double";
                        break;
                    case NoteFlags.eQuadNote:
                        RootString = "quad";
                        break;
                }
            }
            else
            {
                /* fractional notes */
                switch (Duration)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NoteFlags.e64thNote:
                        RootString = "128th";
                        break;
                    case NoteFlags.e32ndNote:
                        RootString = "64th";
                        break;
                    case NoteFlags.e16thNote:
                        RootString = "32nd";
                        break;
                    case NoteFlags.e8thNote:
                        RootString = "16th";
                        break;
                    case NoteFlags.e4thNote:
                        RootString = "8th";
                        break;
                    case NoteFlags.e2ndNote:
                        RootString = "quarter";
                        break;
                    case NoteFlags.eWholeNote:
                        RootString = "half";
                        break;
                    case NoteFlags.eDoubleNote:
                        RootString = "whole";
                        break;
                    case NoteFlags.eQuadNote:
                        RootString = "double";
                        break;
                }
            }
            if (DotFlag)
            {
                DotPrefix = "dotted ";
            }
            else
            {
                DotPrefix = "";
            }
            switch (Division)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case NoteFlags.eDiv1Modifier:
                    DivisionSuffix = "";
                    break;
                case NoteFlags.eDiv3Modifier:
                    DivisionSuffix = " triplet";
                    break;
                case NoteFlags.eDiv5Modifier:
                    DivisionSuffix = " div 5";
                    break;
                case NoteFlags.eDiv7Modifier:
                    DivisionSuffix = " div 7";
                    break;
            }

            return String.Concat(DotPrefix, RootString, DivisionSuffix);
        }


        /* obtain duration attributes from the string passed in.  if the duration attribute */
        /* can't be determined, then Duration will remain unchanged */
        /* this routine is kinda yucky. */
        public static void StringToNumericDuration(
            string String,
            ref NoteFlags Duration,
            out bool DotFlag,
            out NoteFlags Division)
        {
            /* look for hint of division */
            if (String.Contains("div 3") || String.Contains("div3") || String.Contains("trip"))
            {
                Division = NoteFlags.eDiv3Modifier;
            }
            else if (String.Contains("div 5") || String.Contains("div5") || String.Contains("quint"))
            {
                Division = NoteFlags.eDiv5Modifier;
            }
            else if (String.Contains("div 7") || String.Contains("div7") || String.Contains("sept"))
            {
                Division = NoteFlags.eDiv7Modifier;
            }
            else
            {
                Division = NoteFlags.eDiv1Modifier;
            }

            /* look for hint of duration */
            if (Division == NoteFlags.eDiv1Modifier)
            {
                /* normal notes */
                if (String.Contains("64") || String.Contains("sixty"))
                {
                    Duration = NoteFlags.e64thNote;
                }
                else if (String.Contains("32") || String.Contains("thirty"))
                {
                    Duration = NoteFlags.e32ndNote;
                }
                else if (String.Contains("16") || String.Contains("sixteen"))
                {
                    Duration = NoteFlags.e16thNote;
                }
                else if (String.Contains("8") || String.Contains("eight"))
                {
                    Duration = NoteFlags.e8thNote;
                }
                else if (String.Contains("4") || String.Contains("quarter"))
                {
                    Duration = NoteFlags.e4thNote;
                }
                else if (String.Contains("half"))
                {
                    Duration = NoteFlags.e2ndNote;
                }
                else if (String.Contains("whole"))
                {
                    Duration = NoteFlags.eWholeNote;
                }
                else if (String.Contains("double"))
                {
                    Duration = NoteFlags.eDoubleNote;
                }
                else if (String.Contains("quad"))
                {
                    Duration = NoteFlags.eQuadNote;
                }
                else
                {
                    /* didn't find anything */
                }
            }
            else
            {
                /* handle fractional ones */
                if (String.Contains("128") || String.Contains("twenty"))
                {
                    Duration = NoteFlags.e64thNote;
                }
                else if (String.Contains("64") || String.Contains("sixty"))
                {
                    Duration = NoteFlags.e32ndNote;
                }
                else if (String.Contains("32") || String.Contains("thirty"))
                {
                    Duration = NoteFlags.e16thNote;
                }
                else if (String.Contains("16") || String.Contains("sixteen"))
                {
                    Duration = NoteFlags.e8thNote;
                }
                else if (String.Contains("8") || String.Contains("eight"))
                {
                    Duration = NoteFlags.e4thNote;
                }
                else if (String.Contains("4") || String.Contains("quarter"))
                {
                    Duration = NoteFlags.e2ndNote;
                }
                else if (String.Contains("half"))
                {
                    Duration = NoteFlags.eWholeNote;
                }
                else if (String.Contains("whole"))
                {
                    Duration = NoteFlags.eDoubleNote;
                }
                else if (String.Contains("double"))
                {
                    Duration = NoteFlags.eQuadNote;
                }
                else
                {
                    /* didn't find anything */
                }
            }

            /* look for dot hint */
            if (String.Contains("dot"))
            {
                DotFlag = true;
            }
            else
            {
                DotFlag = false;
            }
        }
    }
}
