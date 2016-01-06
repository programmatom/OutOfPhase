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
    public static class SymbolicPitch
    {
        public static string NumericPitchToString(
            short pitch,
            NoteFlags sharpFlat)
        {
            int octave = (pitch / 12);
            int index = pitch % 12;

            string name;
            switch (index)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case 0: /* B#/C */
                    if ((sharpFlat & NoteFlags.eSharpModifier) != 0)
                    {
                        name = "B sharp ";
                    }
                    else
                    {
                        name = "C ";
                    }
                    break;
                case 1: /* C#/Db */
                    if ((sharpFlat & NoteFlags.eSharpModifier) != 0)
                    {
                        name = "C sharp ";
                    }
                    else
                    {
                        name = "D flat ";
                    }
                    break;
                case 2: /* D */
                    name = "D ";
                    break;
                case 3: /* D#/Eb */
                    if ((sharpFlat & NoteFlags.eSharpModifier) != 0)
                    {
                        name = "D sharp ";
                    }
                    else
                    {
                        name = "E flat ";
                    }
                    break;
                case 4: /* E/Fb */
                    if ((sharpFlat & NoteFlags.eFlatModifier) != 0)
                    {
                        name = "F flat ";
                    }
                    else
                    {
                        name = "E ";
                    }
                    break;
                case 5: /* E#/F */
                    if ((sharpFlat & NoteFlags.eSharpModifier) != 0)
                    {
                        name = "E sharp ";
                    }
                    else
                    {
                        name = "F ";
                    }
                    break;
                case 6: /* F#/Gb */
                    if ((sharpFlat & NoteFlags.eSharpModifier) != 0)
                    {
                        name = "F sharp ";
                    }
                    else
                    {
                        name = "G flat ";
                    }
                    break;
                case 7: /* G */
                    name = "G ";
                    break;
                case 8: /* G#/Ab */
                    if ((sharpFlat & NoteFlags.eSharpModifier) != 0)
                    {
                        name = "G sharp ";
                    }
                    else
                    {
                        name = "A flat ";
                    }
                    break;
                case 9: /* A */
                    name = "A ";
                    break;
                case 10: /* A#/Bb */
                    if ((sharpFlat & NoteFlags.eSharpModifier) != 0)
                    {
                        name = "A sharp ";
                    }
                    else
                    {
                        name = "B flat ";
                    }
                    break;
                case 11: /* B/Cb */
                    if ((sharpFlat & NoteFlags.eFlatModifier) != 0)
                    {
                        name = "C flat ";
                    }
                    else
                    {
                        name = "B ";
                    }
                    break;
            }

            return String.Concat(name, (octave - (Constants.CENTERNOTE / 12)).ToString());
        }

        /* convert the string into a pitch and sharp/flat word. */
        public static void StringToNumericPitch(
            string composite,
            ref short pitch,
            ref NoteFlags sharpFlat)
        {
            int localPitch = pitch % 12; /* get the default */
            int octave = pitch / 12 - (Constants.CENTERNOTE / 12); /* defaults */

            if (String.IsNullOrEmpty(composite))
            {
                /* return without changing anything */
                return;
            }

            /* check the pitch specifier */
            switch (composite[0])
            {
                default:
                    break;
                case 'a':
                case 'A':
                    if (composite.Contains("sharp") || composite.Contains("#"))
                    {
                        /* A# */
                        localPitch = 9 + 1;
                        sharpFlat = NoteFlags.eSharpModifier;
                    }
                    else if (composite.Contains("flat"))
                    {
                        /* Ab */
                        localPitch = 9 - 1;
                        sharpFlat = NoteFlags.eFlatModifier;
                    }
                    else
                    {
                        /* A */
                        localPitch = 9;
                        sharpFlat = 0;
                    }
                    break;
                case 'B':
                case 'b':
                    if (composite.Contains("sharp") || composite.Contains("#"))
                    {
                        /* B# */
                        localPitch = 11 + 1;
                        sharpFlat = NoteFlags.eSharpModifier;
                    }
                    else if (composite.Contains("flat"))
                    {
                        /* Bb */
                        localPitch = 11 - 1;
                        sharpFlat = NoteFlags.eFlatModifier;
                    }
                    else
                    {
                        /* B */
                        localPitch = 11;
                        sharpFlat = 0;
                    }
                    break;
                case 'C':
                case 'c':
                    if (composite.Contains("sharp") || composite.Contains("#"))
                    {
                        /* C# */
                        localPitch = 0 + 1;
                        sharpFlat = NoteFlags.eSharpModifier;
                    }
                    else if (composite.Contains("flat"))
                    {
                        /* Cb */
                        localPitch = 0 - 1 + 12;
                        sharpFlat = NoteFlags.eFlatModifier;
                    }
                    else
                    {
                        /* C */
                        localPitch = 0;
                        sharpFlat = 0;
                    }
                    break;
                case 'D':
                case 'd':
                    if (composite.Contains("sharp") || composite.Contains("#"))
                    {
                        /* D# */
                        localPitch = 2 + 1;
                        sharpFlat = NoteFlags.eSharpModifier;
                    }
                    else if (composite.Contains("flat"))
                    {
                        /* Db */
                        localPitch = 2 - 1;
                        sharpFlat = NoteFlags.eFlatModifier;
                    }
                    else
                    {
                        /* D */
                        localPitch = 2;
                        sharpFlat = 0;
                    }
                    break;
                case 'E':
                case 'e':
                    if (composite.Contains("sharp") || composite.Contains("#"))
                    {
                        /* E# */
                        localPitch = 4 + 1;
                        sharpFlat = NoteFlags.eSharpModifier;
                    }
                    else if (composite.Contains("flat"))
                    {
                        /* Eb */
                        localPitch = 4 - 1;
                        sharpFlat = NoteFlags.eFlatModifier;
                    }
                    else
                    {
                        /* E */
                        localPitch = 4;
                        sharpFlat = 0;
                    }
                    break;
                case 'F':
                case 'f':
                    if (composite.Contains("sharp") || composite.Contains("#"))
                    {
                        /* F# */
                        localPitch = 5 + 1;
                        sharpFlat = NoteFlags.eSharpModifier;
                    }
                    else if (composite.Contains("flat"))
                    {
                        /* Fb */
                        localPitch = 5 - 1;
                        sharpFlat = NoteFlags.eFlatModifier;
                    }
                    else
                    {
                        /* F */
                        localPitch = 5;
                        sharpFlat = 0;
                    }
                    break;
                case 'G':
                case 'g':
                    if (composite.Contains("sharp") || composite.Contains("#"))
                    {
                        /* G# */
                        localPitch = 7 + 1;
                        sharpFlat = NoteFlags.eSharpModifier;
                    }
                    else if (composite.Contains("flat"))
                    {
                        /* Gb */
                        localPitch = 7 - 1;
                        sharpFlat = NoteFlags.eFlatModifier;
                    }
                    else
                    {
                        /* G */
                        localPitch = 7;
                        sharpFlat = 0;
                    }
                    break;
            }

            /* try to figure out what octave they want */
            int i = 0;
            while ((i < composite.Length) && (composite[i] != '-') && ((composite[i] < '0') || (composite[i] > '9')))
            {
                i += 1;
            }
            if (i < composite.Length)
            {
                Int32.TryParse(composite.Substring(i), out octave);
            }
            if ((octave * 12) + Constants.CENTERNOTE < 0)
            {
                octave = -Constants.CENTERNOTE / 12;
            }
            if ((octave * 12) + Constants.CENTERNOTE > Constants.NUMNOTES - 1)
            {
                octave = Constants.CENTERNOTE / 12 - 1;
            }

            localPitch = ((octave + (Constants.CENTERNOTE / 12)) * 12) + localPitch;
            if (localPitch < 0)
            {
                localPitch = 0;
            }
            else if (localPitch > Constants.NUMNOTES - 1)
            {
                localPitch = Constants.NUMNOTES - 1;
            }
            pitch = (short)localPitch;
        }
    }
}
