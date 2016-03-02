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
using System.Runtime.InteropServices;
using System.Text;

namespace OutOfPhase
{
    public enum TokenTypes
    {
        eTokenError,

        eTokenIdentifier,
        eTokenString,
        eTokenInteger,
        eTokenSingle,
        eTokenDouble,

        eTokenKeyword,

        eTokenBeginUnqualified,
        //
        eTokenOpenParen, /* ( */
        eTokenCloseParen, /* ) */
        eTokenOpenBracket, /* [ */
        eTokenCloseBracket, /* ] */
        eTokenOpenBrace, /* { */
        eTokenCloseBrace, /* } */
        eTokenColon, /* : */
        eTokenSemicolon, /* ; */
        eTokenComma, /* , */
        eTokenColonEqual, /* := */
        eTokenPlus, /* + */
        eTokenPlusPlus, /* ++ */
        eTokenPlusEqual, /* += */
        eTokenMinus, /* - */
        eTokenMinusMinus, /* -- */
        eTokenMinusEqual, /* -= */
        eTokenMinusGreater, /* -> */
        eTokenStar, /* * */
        eTokenSlash, /* / */
        eTokenEqual, /* = */
        eTokenEqualEqual, /* = */
        eTokenLessGreater, /* <> */
        eTokenLess, /* < */
        eTokenLessEqual, /* <= */
        eTokenGreater, /* > */
        eTokenGreaterEqual, /* >= */
        eTokenLeftLeft, /* << */
        eTokenRightRight, /* >> */
        eTokenCircumflex, /* ^ */
        eTokenBang, /* ! */
        eTokenBangEqual, /* != */
        eTokenAmpersand, /* & */
        eTokenAmpersandAmpersand, /* && */
        eTokenPipe, /* | */
        eTokenPipePipe, /* || */
        eTokenTilde, /* ~ */
        eTokenDollar, /* $ */
        eTokenAt, /* @ */
        eTokenPercent, /* % */
        eTokenBackslash, /* \ */
        eTokenQuestion, /* ? */
        eTokenDot, /* . */
        //
        eTokenEndOfInput,
        //
        eTokenEndUnqualified,
    }

    public enum ScannerErrors
    {
        eNone,
        eScannerMalformedFloat,
        eScannerMalformedInteger,
        eScannerUnknownCharacter,
    }

    [StructLayout(LayoutKind.Auto)]
    public struct KeywordRec<T> where T : struct
    {
        public string KeywordName;
        public T TagValue;

        public KeywordRec(string Keyword, T Tag)
        {
            this.KeywordName = Keyword;
            this.TagValue = Tag;
        }
    }

    public abstract class TokenRec<T> where T : struct
    {
        public virtual TokenTypes GetTokenType()
        {
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

        public virtual string GetTokenIdentifierString()
        {
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

        public virtual string GetTokenStringValue()
        {
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

        public virtual int GetTokenIntegerValue()
        {
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

        public virtual float GetTokenSingleValue()
        {
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

        public virtual double GetTokenDoubleValue()
        {
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

        public virtual T GetTokenKeywordTag()
        {
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

        public virtual ScannerErrors GetTokenErrorCode()
        {
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

#if DEBUG
        public override string ToString()
        {
            return base.ToString();
        }
#endif
    }

    public class UnqualifiedTokenRec<T> : TokenRec<T> where T : struct
    {
        private TokenTypes type;

        public UnqualifiedTokenRec(TokenTypes type)
        {
            if ((type < TokenTypes.eTokenBeginUnqualified) || (type > TokenTypes.eTokenEndUnqualified))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            this.type = type;
        }

        public override TokenTypes GetTokenType()
        {
            return type;
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("{0}", type);
        }
#endif
    }

    public class StringTokenRec<T> : TokenRec<T> where T : struct
    {
        private string literal;

        public StringTokenRec(string literal)
        {
            this.literal = literal;
        }

        public override TokenTypes GetTokenType()
        {
            return TokenTypes.eTokenString;
        }

        public override string GetTokenStringValue()
        {
            return literal;
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("\"{0}\"", literal);
        }
#endif
    }

    public class IdentifierTokenRec<T> : TokenRec<T> where T : struct
    {
        private string identifier;

        public IdentifierTokenRec(string identifier)
        {
            this.identifier = identifier;
        }

        public override TokenTypes GetTokenType()
        {
            return TokenTypes.eTokenIdentifier;
        }

        public override string GetTokenIdentifierString()
        {
            return identifier;
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("identifier: {0}", identifier);
        }
#endif
    }

    public class IntegerTokenRec<T> : TokenRec<T> where T : struct
    {
        private int number;

        public IntegerTokenRec(int number)
        {
            this.number = number;
        }

        public override TokenTypes GetTokenType()
        {
            return TokenTypes.eTokenInteger;
        }

        public override int GetTokenIntegerValue()
        {
            return number;
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("{0}", number);
        }
#endif
    }

    public class SingleTokenRec<T> : TokenRec<T> where T : struct
    {
        private float number;

        public SingleTokenRec(float number)
        {
            this.number = number;
        }

        public override TokenTypes GetTokenType()
        {
            return TokenTypes.eTokenSingle;
        }

        public override float GetTokenSingleValue()
        {
            return number;
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("{0}f", number);
        }
#endif
    }

    public class DoubleTokenRec<T> : TokenRec<T> where T : struct
    {
        private double number;

        public DoubleTokenRec(double number)
        {
            this.number = number;
        }

        public override TokenTypes GetTokenType()
        {
            return TokenTypes.eTokenDouble;
        }

        public override double GetTokenDoubleValue()
        {
            return number;
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("{0}d", number);
        }
#endif
    }

    public class KeywordTokenRec<T> : TokenRec<T> where T : struct
    {
        private T keywordTag;

        public KeywordTokenRec(T keywordTag)
        {
            this.keywordTag = keywordTag;
        }

        public override TokenTypes GetTokenType()
        {
            return TokenTypes.eTokenKeyword;
        }

        public override T GetTokenKeywordTag()
        {
            return keywordTag;
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("keyword: {0}", keywordTag);
        }
#endif
    }

    public class ErrorTokenRec<T> : TokenRec<T> where T : struct
    {
        private ScannerErrors error;

        public ErrorTokenRec(ScannerErrors error)
        {
            this.error = error;
        }

        public override TokenTypes GetTokenType()
        {
            return TokenTypes.eTokenError;
        }

        public override ScannerErrors GetTokenErrorCode()
        {
            return error;
        }

#if DEBUG
        public override string ToString()
        {
            return String.Format("error: {0}", error);
        }
#endif
    }

    public class ScannerRec<T> where T : struct
    {
        private string Block;
        private int Index;
        private int LineNumber = 1;
        private TokenRec<T> PushedBackToken;
        private KeywordRec<T>[] KeywordList;

        /* this type is used when parsing floating point numbers to remember what */
        /* part we're on */
        private enum NumStateType
        {
            eInvalid,

            eIntegerPart,
            eFractionalPart,
            eExponentialPart,
            eExponNumberPart,
            eNumberFinished,
        }

        /* this type is used for explicitly specifying the type of a number */
        private enum NumFormType
        {
            eTypeNotSpecified,
            eTypeSingle,
            eTypeDouble,
            eTypeInteger,
        }

        public ScannerRec(string s, KeywordRec<T>[] keywords)
        {
            AssertKeywordTableSorted(keywords);

            this.Block = s;
            this.LineNumber = 1;
            this.KeywordList = keywords;
        }

        /* install a keyword table */
        private static void AssertKeywordTableSorted(KeywordRec<T>[] Table)
        {
#if DEBUG
            /* ensure sorted */
            for (int i = 1; i < Table.Length; i++)
            {
                if (!(String.Compare(Table[i - 1].KeywordName, Table[i].KeywordName) < 0))
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
            }
#endif
        }

        /* get the line number (starting at 1) that the scanner is on right now */
        public int GetCurrentLineNumber()
        {
            return LineNumber;
        }

        private const int ENDOFTEXT = -1;

        private int GetCharacter()
        {
            char Value;

            if (Index >= Block.Length)
            {
                Index++; /* increment so that we can unget end of text */
                return ENDOFTEXT;
            }
            Value = Block[Index];
            Index++;
            return Value;
        }

        private void UngetCharacter()
        {
            if (Index == 0)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            Index--;
        }

        private delegate bool Method();
        private static bool Eval(Method method)
        {
            return method();
        }

        public TokenRec<T> GetNextToken()
        {
            int C;
            TokenRec<T> Token;

            /* check for pushback */
            if (PushedBackToken != null)
            {
                TokenRec<T> Temp;

                Temp = PushedBackToken;
                PushedBackToken = null;
                return Temp;
            }

            /* get a character */
            C = GetCharacter();

            /* strip while space */
            bool cr = false;
            while (((C >= 0) && (C <= 32)) || (C == '#'))
            {
                if ((C == 13) || (C == 10))
                {
                    bool crPrev = cr;
                    cr = (C == 13);
                    if (!crPrev)
                    {
                        LineNumber++;
                    }
                }
                if (C == '#')
                {
                    /* comment */
                    while ((C != 13) && (C != 10) && (C != ENDOFTEXT))
                    {
                        C = GetCharacter();
                    }
                }
                else
                {
                    C = GetCharacter();
                }
            }

        RestartParse:
            /* handle the end of text character */
            if (C == ENDOFTEXT)
            {
                Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenEndOfInput);
            }

            /* handle a string literal */
            else if (C == '\x22')
            {
                StringBuilder String = new StringBuilder();

                cr = false;
                C = GetCharacter();
                while (C != '\x22')
                {
                    if (C == ENDOFTEXT)
                    {
                        goto BreakStringReadPoint;
                    }
                    if (C == '\\')
                    {
                        C = GetCharacter();
                        if (C == 'n')
                        {
                            String.Append(Environment.NewLine); // originally was '\n'
                            goto DoAnotherCharPoint;
                        }
                        else if ((C == '\x22') || (C == '\\') || (C == 10) || (C == 13))
                        {
                            /* keep these */
                        }
                        else
                        {
                            /* others become strange character */
                            C = '.';
                        }
                    }
                    String.Append((char)C);
                    if ((C == 10) || (C == 13))
                    {
                        bool crPrev = cr;
                        cr = (C == 13);
                        if (!crPrev)
                        {
                            LineNumber++;
                        }
                    }
                DoAnotherCharPoint:
                    C = GetCharacter();
                }
            BreakStringReadPoint:
                ;

                Token = new StringTokenRec<T>(String.ToString());
            }

            /* handle an identifier:  [a-zA-Z_][a-zA-Z0-9_]*  */
            else if (((C >= 'a') && (C <= 'z')) || ((C >= 'A') && (C <= 'Z')) || (C == '_'))
            {
                StringBuilder String = new StringBuilder();
                int KeywordIndex = -1; /* -1 == not a keyword */
                string StringCopy;

                /* read the entire token */
                while (((C >= 'a') && (C <= 'z')) || ((C >= 'A') && (C <= 'Z')) || (C == '_') || ((C >= '0') && (C <= '9')))
                {
                    String.Append((char)C);
                    C = GetCharacter();
                }
                /* unget the character that made us stop */
                UngetCharacter();
                /* get the string out of the line buffer */
                StringCopy = String.ToString();

                /* figure out if it is a keyword (binary search) */
                int LowBound = 0;
                int HighBoundPlusOne = KeywordList.Length;
                bool ContinueLoopingFlag = true;
                while (ContinueLoopingFlag)
                {
                    int MidPoint;
                    int CompareResult;

                    if (LowBound > HighBoundPlusOne)
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }

                    MidPoint = (LowBound + HighBoundPlusOne) / 2;

                    CompareResult = StringCopy.CompareTo(KeywordList[MidPoint].KeywordName);
                    /* CompareResult == 0  -->  found the target */
                    /* CompareResult < 0  --> in the first half of the list */
                    /* CompareResult > 0  --> in the second half of the list */

                    if (CompareResult == 0)
                    {
                        /* found the one */
                        KeywordIndex = MidPoint;
                        ContinueLoopingFlag = false;
                    }
                    else
                    {
                        if (CompareResult < 0)
                        {
                            /* select first half of list */
                            HighBoundPlusOne = MidPoint;
                        }
                        else /* if (CompareResult > 0) */
                        {
                            /* select second half of list */
                            LowBound = MidPoint + 1;
                        }
                        /* termination condition:  if range in array collapses to an */
                        /* empty array, then there is no entry in the array */
                        if (LowBound == HighBoundPlusOne)
                        {
                            KeywordIndex = -1; /* indicate there is no keyword */
                            ContinueLoopingFlag = false;
                        }
                    }
                }

                /* create the token */
                if (KeywordIndex == -1)
                {
                    /* no keyword; make a string containing token */
                    Token = new IdentifierTokenRec<T>(StringCopy);
                }
                else
                {
                    Token = new KeywordTokenRec<T>(KeywordList[KeywordIndex].TagValue);
                }
            }

            /* integer or floating?  [0-9]+  [0-9]+"."[0-9]+([Ee][+-]?[0-9]+)?[sdf]?  */
            else if (((C >= '0') && (C <= '9'))
                // TODO: C# 2.0 hack - convert to elegant lambda evaluation after upgrade
                || ((C == '.') && Eval(delegate () { int CC = GetCharacter(); UngetCharacter(); return (CC >= '0') || (CC <= '9'); })))
            {
                NumFormType SpecifiedNumType = NumFormType.eTypeNotSpecified;
                NumStateType NumberState = NumStateType.eIntegerPart;
                StringBuilder String = new StringBuilder();
                string StringData;

                Token = null;

                while (((C >= '0') && (C <= '9')) || (C == '.') || (C == '+') || (C == '-')
                    || (C == 's') || (C == 'd') || (C == 'f') || (C == 'e') || (C == 'E'))
                {
                    /* do some state changes */
                    if (C == '.')
                    {
                        if (NumberState != NumStateType.eIntegerPart)
                        {
                            Token = new ErrorTokenRec<T>(ScannerErrors.eScannerMalformedFloat);
                            goto AbortNumberErrorPoint;
                        }
                        else
                        {
                            NumberState = NumStateType.eFractionalPart;
                        }
                    }
                    else if ((C == 'e') || (C == 'E'))
                    {
                        if ((NumberState != NumStateType.eIntegerPart) && (NumberState != NumStateType.eFractionalPart))
                        {
                            Token = new ErrorTokenRec<T>(ScannerErrors.eScannerMalformedFloat);
                            goto AbortNumberErrorPoint;
                        }
                        else
                        {
                            NumberState = NumStateType.eExponentialPart;
                        }
                    }
                    else if ((C == '+') || (C == '-'))
                    {
                        if (NumberState != NumStateType.eExponentialPart)
                        {
                            /* this is not an error, since it could be a unary operator */
                            /* coming later, so we stop, but don't abort */
                            goto FinishNumberPoint; /* character ungot at target */
                        }
                        else
                        {
                            NumberState = NumStateType.eExponNumberPart;
                        }
                    }
                    else if ((C == 's') || (C == 'f'))
                    {
                        if (NumberState == NumStateType.eNumberFinished)
                        {
                            Token = new ErrorTokenRec<T>(ScannerErrors.eScannerMalformedFloat);
                            goto AbortNumberErrorPoint;
                        }
                        else
                        {
                            NumberState = NumStateType.eNumberFinished;
                            SpecifiedNumType = NumFormType.eTypeSingle;
                            C = (char)32; /* so adding it to the string doesn't do damage */
                        }
                    }
                    else if (C == 'd')
                    {
                        if (NumberState == NumStateType.eNumberFinished)
                        {
                            Token = new ErrorTokenRec<T>(ScannerErrors.eScannerMalformedFloat);
                            goto AbortNumberErrorPoint;
                        }
                        else
                        {
                            NumberState = NumStateType.eNumberFinished;
                            SpecifiedNumType = NumFormType.eTypeDouble;
                            C = (char)32;
                        }
                    }

                    /* actually save the character */
                    String.Append((char)C);

                    C = GetCharacter();
                }
            FinishNumberPoint:
                UngetCharacter();

                StringData = String.ToString();

                /* if the token type is not specified, then see what we can guess */
                if (SpecifiedNumType == NumFormType.eTypeNotSpecified)
                {
                    if (NumberState == NumStateType.eIntegerPart)
                    {
                        /* if we only got as far as the integer part, then it's an int */
                        SpecifiedNumType = NumFormType.eTypeInteger;
                    }
                    else
                    {
                        /* otherwise, assume the highest precision type */
                        SpecifiedNumType = NumFormType.eTypeDouble;
                    }
                }

                /* create the token */
                switch (SpecifiedNumType)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case NumFormType.eTypeSingle:
                        {
                            float v;
                            if (!Single.TryParse(StringData, out v))
                            {
                                // Reasons it could fail:
                                // 1: our scanner is more permissive than they are - accepting things like "."
                                // 2: number could be syntactically valid but out of range for the type.
                                Token = new ErrorTokenRec<T>(ScannerErrors.eScannerMalformedFloat);
                                goto AbortNumberErrorPoint;
                            }
                            Token = new SingleTokenRec<T>(v);
                        }
                        break;
                    case NumFormType.eTypeDouble:
                        {
                            double v;
                            if (!Double.TryParse(StringData, out v))
                            {
                                // Reasons it could fail:
                                // 1: our scanner is more permissive than they are - accepting things like "."
                                // 2: number could be syntactically valid but out of range for the type.
                                Token = new ErrorTokenRec<T>(ScannerErrors.eScannerMalformedFloat);
                                goto AbortNumberErrorPoint;
                            }
                            Token = new DoubleTokenRec<T>(v);
                        }
                        break;
                    case NumFormType.eTypeInteger:
                        {
                            int v;
                            if (!Int32.TryParse(StringData, out v))
                            {
                                // Reasons it could fail:
                                // 1: our scanner is more permissive than they are - accepting things like "."
                                // 2: number could be syntactically valid but out of range for the type.
                                Token = new ErrorTokenRec<T>(ScannerErrors.eScannerMalformedInteger);
                                goto AbortNumberErrorPoint;
                            }
                            Token = new IntegerTokenRec<T>(v);
                        }
                        break;
                }

            /* this is the escape point for when a bad character is encountered. */
            AbortNumberErrorPoint:
                ;
            }

            /* handle a symbol */
            else
            {
                Token = null;

                switch (C)
                {
                    default:
                        Token = new ErrorTokenRec<T>(ScannerErrors.eScannerUnknownCharacter);
                        break;
                    case '(':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenOpenParen);
                        break;
                    case ')':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenCloseParen);
                        break;
                    case '[':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenOpenBracket);
                        break;
                    case ']':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenCloseBracket);
                        break;
                    case '{':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenOpenBrace);
                        break;
                    case '}':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenCloseBrace);
                        break;
                    case ':':
                        C = GetCharacter();
                        if (C == '=')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenColonEqual);
                        }
                        else
                        {
                            /* push the character back */
                            UngetCharacter();
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenColon);
                        }
                        break;
                    case ';':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenSemicolon);
                        break;
                    case ',':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenComma);
                        break;
                    case '+':
                        C = GetCharacter();
                        if (C == '=')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenPlusEqual);
                        }
                        else if (C == '+')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenPlusPlus);
                        }
                        else
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenPlus);
                            UngetCharacter();
                        }
                        break;
                    case '-':
                        C = GetCharacter();
                        if (C == '=')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenMinusEqual);
                        }
                        else if (C == '-')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenMinusMinus);
                        }
                        else if (C == '>')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenMinusGreater);
                        }
                        else
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenMinus);
                            UngetCharacter();
                        }
                        break;
                    case '*':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenStar);
                        break;
                    case '/':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenSlash);
                        break;
                    case '=':
                        C = GetCharacter();
                        if (C == '=')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenEqualEqual);
                        }
                        else
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenEqual);
                            UngetCharacter();
                        }
                        break;
                    case '<':
                        C = GetCharacter();
                        if (C == '>')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenLessGreater);
                        }
                        else if (C == '=')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenLessEqual);
                        }
                        else if (C == '<')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenLeftLeft);
                        }
                        else
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenLess);
                            UngetCharacter();
                        }
                        break;
                    case '>':
                        C = GetCharacter();
                        if (C == '=')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenGreaterEqual);
                        }
                        else if (C == '>')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenRightRight);
                        }
                        else
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenGreater);
                            UngetCharacter();
                        }
                        break;
                    case '^':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenCircumflex);
                        break;
                    case '!':
                        C = GetCharacter();
                        if (C == '=')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenBangEqual);
                        }
                        else
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenBang);
                            UngetCharacter();
                        }
                        break;
                    case '&':
                        C = GetCharacter();
                        if (C == '&')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenAmpersandAmpersand);
                        }
                        else
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenAmpersand);
                            UngetCharacter();
                        }
                        break;
                    case '|':
                        C = GetCharacter();
                        if (C == '|')
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenPipePipe);
                        }
                        else
                        {
                            Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenPipe);
                            UngetCharacter();
                        }
                        break;
                    case '~':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenTilde);
                        break;
                    case '$':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenDollar);
                        break;
                    case '@':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenAt);
                        break;
                    case '%':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenPercent);
                        break;
                    case '\\':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenBackslash);
                        break;
                    case '?':
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenQuestion);
                        break;
                    case '.':
                        {
                            int CC = GetCharacter();
                            if ((CC >= '0') && (CC <= '9'))
                            {
                                UngetCharacter();
                                goto RestartParse; // parse number
                            }
                        }
                        Token = new UnqualifiedTokenRec<T>(TokenTypes.eTokenDot);
                        break;
                }
            }

            return Token;
        }

        public void UngetToken(TokenRec<T> Token)
        {
            if (PushedBackToken != null)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            PushedBackToken = Token;
        }
    }
}
