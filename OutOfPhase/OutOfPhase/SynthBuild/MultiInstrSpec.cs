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
        /* parse errors */
        public enum MultiInstrParseError
        {
            eMultiInstrParseOK,
            eMultiInstrParseExpectedInstrNameString,
            eMultiInstrParseExpectedPitchLetter,
            eMultiInstrParseExpectedFlatOrSharp,
            eMultiInstrParseExpectedFlatOrSharpOrOctave,
            eMultiInstrParseBasePitchOutOfRange,
            eMultiInstrParseTopPitchOutOfRange,
            eMultiInstrParseExpectedTo,
            eMultiInstrParseExpectedSemicolon,
            eMultiInstrParseOverlapExistingRange,
            eMultiInstrParseInvertedRange,
            eMultiInstrParseExpectedAs,
        }

        public struct ZoneRec
        {
            /* name of the instrument for this zone */
            public string InstrName;

            /* inclusive pitch range covered by this instrument */
            public short BasePitch;
            public short TopPitch;
            public short EffectiveBasePitch;
        }

        public class MultiInstrSpecRec
        {
            /* array of zone records */
            public ZoneRec[] Array;
        }

        /* create a new multi-instrument spec */
        public static MultiInstrSpecRec NewMultiInstrSpec()
        {
            MultiInstrSpecRec Spec = new MultiInstrSpecRec();

            Spec.Array = new ZoneRec[0];

            return Spec;
        }

        /* add pitch zone to multi-instrument spec.  returns False if it runs out of memory. */
        /* the specified pitch range is inclusive. */
        public static void MultiInstrSpecAddZone(
            MultiInstrSpecRec Spec,
            string InstrName,
            int BasePitch,
            int TopPitch,
            int EffectiveBasePitch)
        {
#if DEBUG
            if ((BasePitch < 0) || (BasePitch > Constants.NUMNOTES - 1))
            {
                // BasePitch out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((TopPitch < 0) || (TopPitch > Constants.NUMNOTES - 1))
            {
                // TopPitch out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (TopPitch < BasePitch)
            {
                // Pitch range is inverted
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((EffectiveBasePitch < 0) || (EffectiveBasePitch > Constants.NUMNOTES - 1))
            {
                // EffectiveBasePitch out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (!MultiInstrSpecTestOverlap(Spec, BasePitch, TopPitch))
            {
                // Pitch range overlaps existing range
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif

            ZoneRec Zone = new ZoneRec();

            Zone.InstrName = InstrName;
            Zone.BasePitch = (short)BasePitch;
            Zone.TopPitch = (short)TopPitch;
            Zone.EffectiveBasePitch = (short)EffectiveBasePitch;

            Array.Resize(ref Spec.Array, Spec.Array.Length + 1);
            Spec.Array[Spec.Array.Length - 1] = Zone;
        }

        /* test for zone overlap.  the specified pitch range is inclusive.  returns True */
        /* if there is no overlap. */
        public static bool MultiInstrSpecTestOverlap(
            MultiInstrSpecRec Spec,
            int BasePitch,
            int TopPitch)
        {
#if DEBUG
            if ((BasePitch < 0) || (BasePitch > Constants.NUMNOTES - 1))
            {
                // BasePitch out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((TopPitch < 0) || (TopPitch > Constants.NUMNOTES - 1))
            {
                // TopPitch out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (TopPitch < BasePitch)
            {
                // Pitch range is inverted
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            for (int i = 0; i < Spec.Array.Length; i++)
            {
                if (((Spec.Array[i].BasePitch >= BasePitch) && (Spec.Array[i].BasePitch <= TopPitch))
                    || ((Spec.Array[i].TopPitch >= BasePitch) && (Spec.Array[i].TopPitch <= TopPitch)))
                {
                    return false;
                }
            }
            return true;
        }

        /* add maximum zone */
        public static void MultiInstrSpecAddMaximumDefaultZone(
            MultiInstrSpecRec Spec,
            string InstrName)
        {
            MultiInstrSpecAddZone(Spec, InstrName, 0, Constants.NUMNOTES - 1, 0);
        }

        /* get number of instruments */
        public static int MultiInstrSpecGetLength(MultiInstrSpecRec Spec)
        {
            return Spec.Array.Length;
        }

        /* get actual instrument name for the specified zone */
        public static string MultiInstrSpecGetIndexedActualInstrName(
            MultiInstrSpecRec Spec,
            int Index)
        {
            return Spec.Array[Index].InstrName;
        }

        /* get base pitch of the zone's range */
        public static short MultiInstrSpecGetIndexedBasePitch(
            MultiInstrSpecRec Spec,
            int Index)
        {
            return Spec.Array[Index].BasePitch;
        }

        /* get top pitch of the zone's range */
        public static short MultiInstrSpecGetIndexedTopPitch(
            MultiInstrSpecRec Spec,
            int Index)
        {
            return Spec.Array[Index].TopPitch;
        }

        /* get effective base pitch of the zone's range */
        public static short MultiInstrSpecGetIndexedEffectiveBasePitch(
            MultiInstrSpecRec Spec,
            int Index)
        {
            return Spec.Array[Index].EffectiveBasePitch;
        }

        public enum MultiInstrKeywords
        {
            eMIK_A,
            eMIK_B,
            eMIK_C,
            eMIK_D,
            eMIK_E,
            eMIK_F,
            eMIK_G,
            eMIK_flat,
            eMIK_sharp,
            eMIK_to,
            eMIK_as,
        }

        public static readonly KeywordRec<MultiInstrKeywords>[] MultiInstrKeywordTable = new KeywordRec<MultiInstrKeywords>[]
	    {
		    new KeywordRec<MultiInstrKeywords>("a", MultiInstrKeywords.eMIK_A),
		    new KeywordRec<MultiInstrKeywords>("A", MultiInstrKeywords.eMIK_A),
		    new KeywordRec<MultiInstrKeywords>("as", MultiInstrKeywords.eMIK_as),
		    new KeywordRec<MultiInstrKeywords>("b", MultiInstrKeywords.eMIK_B),
		    new KeywordRec<MultiInstrKeywords>("B", MultiInstrKeywords.eMIK_B),
		    new KeywordRec<MultiInstrKeywords>("c", MultiInstrKeywords.eMIK_C),
		    new KeywordRec<MultiInstrKeywords>("C", MultiInstrKeywords.eMIK_C),
		    new KeywordRec<MultiInstrKeywords>("d", MultiInstrKeywords.eMIK_D),
		    new KeywordRec<MultiInstrKeywords>("D", MultiInstrKeywords.eMIK_D),
		    new KeywordRec<MultiInstrKeywords>("e", MultiInstrKeywords.eMIK_E),
		    new KeywordRec<MultiInstrKeywords>("E", MultiInstrKeywords.eMIK_E),
		    new KeywordRec<MultiInstrKeywords>("f", MultiInstrKeywords.eMIK_F),
		    new KeywordRec<MultiInstrKeywords>("F", MultiInstrKeywords.eMIK_F),
		    new KeywordRec<MultiInstrKeywords>("flat", MultiInstrKeywords.eMIK_flat),
		    new KeywordRec<MultiInstrKeywords>("g", MultiInstrKeywords.eMIK_G),
		    new KeywordRec<MultiInstrKeywords>("G", MultiInstrKeywords.eMIK_G),
		    new KeywordRec<MultiInstrKeywords>("sharp", MultiInstrKeywords.eMIK_sharp),
		    new KeywordRec<MultiInstrKeywords>("to", MultiInstrKeywords.eMIK_to),
	    };

        /* create multi-instrument spec from string */
        public static MultiInstrParseError MultiInstrParse(
            out MultiInstrSpecRec Spec,
            string String)
        {
            Spec = NewMultiInstrSpec();

            ScannerRec<MultiInstrKeywords> Scanner = new ScannerRec<MultiInstrKeywords>(String, MultiInstrKeywordTable);

            /* parse all the thingies */
            while (true)
            {
                TokenRec<MultiInstrKeywords> Token;
                string InstrName;
                int BasePitch;
                int TopPitch;
                int EffectiveBasePitch;
                int Sign;

                /* end of input, or "instrname" */
                Token = Scanner.GetNextToken();
                if (Token.GetTokenType() == TokenTypes.eTokenEndOfInput)
                {
                    return MultiInstrParseError.eMultiInstrParseOK;
                }
                if (Token.GetTokenType() != TokenTypes.eTokenString)
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedInstrNameString;
                }
                InstrName = Token.GetTokenStringValue();

                /* base pitch */

                /* letter */
                Token = Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedPitchLetter;
                }
                switch (Token.GetTokenKeywordTag())
                {
                    default:
                        return MultiInstrParseError.eMultiInstrParseExpectedPitchLetter;
                    case MultiInstrKeywords.eMIK_C:
                        BasePitch = Constants.CENTERNOTE;
                        break;
                    case MultiInstrKeywords.eMIK_D:
                        BasePitch = Constants.CENTERNOTE + 2;
                        break;
                    case MultiInstrKeywords.eMIK_E:
                        BasePitch = Constants.CENTERNOTE + 4;
                        break;
                    case MultiInstrKeywords.eMIK_F:
                        BasePitch = Constants.CENTERNOTE + 5;
                        break;
                    case MultiInstrKeywords.eMIK_G:
                        BasePitch = Constants.CENTERNOTE + 7;
                        break;
                    case MultiInstrKeywords.eMIK_A:
                        BasePitch = Constants.CENTERNOTE + 9;
                        break;
                    case MultiInstrKeywords.eMIK_B:
                        BasePitch = Constants.CENTERNOTE + 11;
                        break;
                }

                Token = Scanner.GetNextToken();

                /* optional "flat" or "sharp" */
                if (Token.GetTokenType() == TokenTypes.eTokenKeyword)
                {
                    if (Token.GetTokenKeywordTag() == MultiInstrKeywords.eMIK_flat)
                    {
                        BasePitch -= 1;
                    }
                    else if (Token.GetTokenKeywordTag() == MultiInstrKeywords.eMIK_sharp)
                    {
                        BasePitch += 1;
                    }
                    else
                    {
                        return MultiInstrParseError.eMultiInstrParseExpectedFlatOrSharp;
                    }

                    Token = Scanner.GetNextToken();
                }

                /* octave number */
                Sign = 1;
                if (Token.GetTokenType() == TokenTypes.eTokenMinus)
                {
                    Sign = -1;
                    Token = Scanner.GetNextToken();
                }
                if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedFlatOrSharpOrOctave;
                }
                BasePitch += 12 * Sign * Token.GetTokenIntegerValue();
                if ((BasePitch < 0) || (BasePitch > Constants.NUMNOTES - 1))
                {
                    return MultiInstrParseError.eMultiInstrParseBasePitchOutOfRange;
                }

                Token = Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != MultiInstrKeywords.eMIK_to))
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedTo;
                }

                /* top pitch */

                /* letter */
                Token = Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedPitchLetter;
                }
                switch (Token.GetTokenKeywordTag())
                {
                    default:
                        return MultiInstrParseError.eMultiInstrParseExpectedPitchLetter;
                    case MultiInstrKeywords.eMIK_C:
                        TopPitch = Constants.CENTERNOTE;
                        break;
                    case MultiInstrKeywords.eMIK_D:
                        TopPitch = Constants.CENTERNOTE + 2;
                        break;
                    case MultiInstrKeywords.eMIK_E:
                        TopPitch = Constants.CENTERNOTE + 4;
                        break;
                    case MultiInstrKeywords.eMIK_F:
                        TopPitch = Constants.CENTERNOTE + 5;
                        break;
                    case MultiInstrKeywords.eMIK_G:
                        TopPitch = Constants.CENTERNOTE + 7;
                        break;
                    case MultiInstrKeywords.eMIK_A:
                        TopPitch = Constants.CENTERNOTE + 9;
                        break;
                    case MultiInstrKeywords.eMIK_B:
                        TopPitch = Constants.CENTERNOTE + 11;
                        break;
                }

                Token = Scanner.GetNextToken();

                /* optional "flat" or "sharp" */
                if (Token.GetTokenType() == TokenTypes.eTokenKeyword)
                {
                    if (Token.GetTokenKeywordTag() == MultiInstrKeywords.eMIK_flat)
                    {
                        TopPitch -= 1;
                    }
                    else if (Token.GetTokenKeywordTag() == MultiInstrKeywords.eMIK_sharp)
                    {
                        TopPitch += 1;
                    }
                    else
                    {
                        return MultiInstrParseError.eMultiInstrParseExpectedFlatOrSharp;
                    }

                    Token = Scanner.GetNextToken();
                }

                /* octave number */
                Sign = 1;
                if (Token.GetTokenType() == TokenTypes.eTokenMinus)
                {
                    Sign = -1;
                    Token = Scanner.GetNextToken();
                }
                if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedFlatOrSharpOrOctave;
                }
                TopPitch += 12 * Sign * Token.GetTokenIntegerValue();
                if ((TopPitch < 0) || (TopPitch > Constants.NUMNOTES - 1))
                {
                    return MultiInstrParseError.eMultiInstrParseTopPitchOutOfRange;
                }

                Token = Scanner.GetNextToken();
                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                    || (Token.GetTokenKeywordTag() != MultiInstrKeywords.eMIK_as))
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedAs;
                }

                /* effective base pitch */

                /* letter */
                Token = Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedPitchLetter;
                }
                switch (Token.GetTokenKeywordTag())
                {
                    default:
                        return MultiInstrParseError.eMultiInstrParseExpectedPitchLetter;
                    case MultiInstrKeywords.eMIK_C:
                        EffectiveBasePitch = Constants.CENTERNOTE;
                        break;
                    case MultiInstrKeywords.eMIK_D:
                        EffectiveBasePitch = Constants.CENTERNOTE + 2;
                        break;
                    case MultiInstrKeywords.eMIK_E:
                        EffectiveBasePitch = Constants.CENTERNOTE + 4;
                        break;
                    case MultiInstrKeywords.eMIK_F:
                        EffectiveBasePitch = Constants.CENTERNOTE + 5;
                        break;
                    case MultiInstrKeywords.eMIK_G:
                        EffectiveBasePitch = Constants.CENTERNOTE + 7;
                        break;
                    case MultiInstrKeywords.eMIK_A:
                        EffectiveBasePitch = Constants.CENTERNOTE + 9;
                        break;
                    case MultiInstrKeywords.eMIK_B:
                        EffectiveBasePitch = Constants.CENTERNOTE + 11;
                        break;
                }

                Token = Scanner.GetNextToken();

                /* optional "flat" or "sharp" */
                if (Token.GetTokenType() == TokenTypes.eTokenKeyword)
                {
                    if (Token.GetTokenKeywordTag() == MultiInstrKeywords.eMIK_flat)
                    {
                        EffectiveBasePitch -= 1;
                    }
                    else if (Token.GetTokenKeywordTag() == MultiInstrKeywords.eMIK_sharp)
                    {
                        EffectiveBasePitch += 1;
                    }
                    else
                    {
                        return MultiInstrParseError.eMultiInstrParseExpectedFlatOrSharp;
                    }

                    Token = Scanner.GetNextToken();
                }

                /* octave number */
                Sign = 1;
                if (Token.GetTokenType() == TokenTypes.eTokenMinus)
                {
                    Sign = -1;
                    Token = Scanner.GetNextToken();
                }
                if (Token.GetTokenType() != TokenTypes.eTokenInteger)
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedFlatOrSharpOrOctave;
                }
                EffectiveBasePitch += 12 * Sign * Token.GetTokenIntegerValue();
                if ((EffectiveBasePitch < 0) || (EffectiveBasePitch > Constants.NUMNOTES - 1))
                {
                    return MultiInstrParseError.eMultiInstrParseTopPitchOutOfRange;
                }

                Token = Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    return MultiInstrParseError.eMultiInstrParseExpectedSemicolon;
                }

                /* put value */
                if (BasePitch > TopPitch)
                {
                    return MultiInstrParseError.eMultiInstrParseInvertedRange;
                }
                if (!MultiInstrSpecTestOverlap(
                    Spec,
                    BasePitch,
                    TopPitch))
                {
                    return MultiInstrParseError.eMultiInstrParseOverlapExistingRange;
                }
                MultiInstrSpecAddZone(
                    Spec,
                    InstrName,
                    BasePitch,
                    TopPitch,
                    EffectiveBasePitch);
            }
        }

        /* get error code string */
        public static string MultiInstrGetErrorString(MultiInstrParseError Error)
        {
            switch (Error)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case MultiInstrParseError.eMultiInstrParseExpectedInstrNameString:
                    return "Expected string containing instrument name.";
                case MultiInstrParseError.eMultiInstrParseExpectedPitchLetter:
                    return "Expected pitch letter.";
                case MultiInstrParseError.eMultiInstrParseExpectedFlatOrSharp:
                    return "Expected 'flat' or 'sharp'.";
                case MultiInstrParseError.eMultiInstrParseExpectedFlatOrSharpOrOctave:
                    return "Expected 'flat', 'sharp', or octave integer.";
                case MultiInstrParseError.eMultiInstrParseBasePitchOutOfRange:
                    return "Base pitch is out of range.";
                case MultiInstrParseError.eMultiInstrParseTopPitchOutOfRange:
                    return "Top pitch is out of range.";
                case MultiInstrParseError.eMultiInstrParseExpectedTo:
                    return "Expected 'to'.";
                case MultiInstrParseError.eMultiInstrParseExpectedSemicolon:
                    return "Expected ';'.";
                case MultiInstrParseError.eMultiInstrParseOverlapExistingRange:
                    return "Range overlaps a previous range.";
                case MultiInstrParseError.eMultiInstrParseInvertedRange:
                    return "Top pitch must not be lower than base pitch.";
                case MultiInstrParseError.eMultiInstrParseExpectedAs:
                    return "Expected 'as'.";
            }
        }
    }
}
