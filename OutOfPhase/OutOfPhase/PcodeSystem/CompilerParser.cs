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
    public static partial class Compiler
    {
        public class ParserContext
        {
            public readonly ScannerRec<KeywordsType> Scanner;
            public readonly SymbolTableRec SymbolTable;
            public readonly Dictionary<string, List<FunctionSymbolRefInfo>> FunctionSymbolList;

            public class FunctionSymbolRefInfo
            {
                public readonly SymbolRec symbol;
                public readonly int lineNumber;
                public int module;

                public FunctionSymbolRefInfo(SymbolRec symbol, int lineNumber)
                {
                    this.symbol = symbol;
                    this.lineNumber = lineNumber;
                    this.module = -1;
                }
            }

            public ParserContext(
                ScannerRec<KeywordsType> Scanner,
                SymbolTableRec SymbolTable)
            {
                this.Scanner = Scanner;
                this.SymbolTable = SymbolTable;
                this.FunctionSymbolList = new Dictionary<string, List<FunctionSymbolRefInfo>>();
            }

            public ParserContext(
                ScannerRec<KeywordsType> Scanner,
                SymbolTableRec SymbolTable,
                Dictionary<string, List<FunctionSymbolRefInfo>> FunctionSymbolList)
            {
                this.Scanner = Scanner;
                this.SymbolTable = SymbolTable;
                this.FunctionSymbolList = FunctionSymbolList;
            }
        }

        /* parse a top-level form, which is... a function. */
        /*   1:   <form>             ::= <function> ; */
        /* FIRST SET: */
        /* <form>             : {func, <function>} */
        /* FOLLOW SET: */
        /* <form>             : {$$$} */
        public static CompileErrors ParseForm(
            out SymbolRec FunctionSymbolTableEntryOut,
            out ASTExpression FunctionBodyOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            CompileErrors Error;

            FunctionSymbolTableEntryOut = null;
            FunctionBodyOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            /* do lookahead on "func" */
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == KeywordsType.eExprKwrdFunc))
            {
                /* push token back */
                Context.Scanner.UngetToken(Token);

                /* parse function definition */
                Error = ParseFunction(
                    out FunctionSymbolTableEntryOut,
                    out FunctionBodyOut,
                    Context,
                    out LineNumberOut);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                int start, count;
                if (ASTFunctionCall.TryFindBuiltInFunctionRange(FunctionSymbolTableEntryOut.SymbolName, out start, out count))
                {
                    LineNumberOut = FunctionBodyOut.LineNumber;
                    return CompileErrors.eCompileFunctionNameConflictsWithBuiltIn;
                }
            }
            else
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedFunc;
            }


            /* swallow the semicolon */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedSemicolon;
            }

            return CompileErrors.eCompileNoError;
        }




        /* this parses a function declaration, returning the symbol table entry for the */
        /* function in *FunctionSymbolTableEntryOut and the expression for the function */
        /* in *FunctionBodyOut. */
        /*  14:   <function>         ::= func <identifier> ( <formalparamstart> ) : */
        /*      <type> <expr> */
        /* FIRST SET: */
        /* <function>         : {func} */
        /* FOLLOW SET: */
        /* <function>         : {;} */
        private static CompileErrors ParseFunction(
            out SymbolRec FunctionSymbolTableEntryOut,
            out ASTExpression FunctionBodyOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            TokenRec<KeywordsType> FunctionName;
            SymbolListRec FormalArgumentList;
            CompileErrors Error;
            DataTypes ReturnType;
            int LineNumberOfIdentifier;

            FunctionSymbolTableEntryOut = null;
            FunctionBodyOut = null;
            LineNumberOut = -1;

            /* swallow "func" */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdFunc))
            {
                /* this is impossible -- we should be able to do some error checking here, */
                /* but it seems uncertain since we don't (formally) know whose calling us. */
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedFunc;
            }

            /* get the identifier */
            FunctionName = Context.Scanner.GetNextToken();
            if (FunctionName.GetTokenType() != TokenTypes.eTokenIdentifier)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedIdentifier;
            }
            LineNumberOfIdentifier = Context.Scanner.GetCurrentLineNumber();

            /* add the identifier to the symbol table */
            FunctionSymbolTableEntryOut = new SymbolRec(FunctionName.GetTokenIdentifierString());
            if (!Context.SymbolTable.Add(FunctionSymbolTableEntryOut))
            {
                LineNumberOut = LineNumberOfIdentifier;
                return CompileErrors.eCompileMultiplyDefinedIdentifier;
            }

            /* create a new lexical level */
            Context.SymbolTable.IncrementSymbolTableLevel();

            /* swallow the open parenthesis */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedOpenParen;
            }

            /* parse <formalparamstart> */
            Error = ParseFormalParamStart(
                out FormalArgumentList,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* swallow the close parenthesis */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedCloseParen;
            }

            /* swallow the colon */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenColon)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedColon;
            }

            /* parse the return type of the function */
            Error = ParseType(
                out ReturnType,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* store the interesting information into the symbol table entry */
            FunctionSymbolTableEntryOut.SymbolBecomeFunction(
                FormalArgumentList,
                ReturnType);

            /* parse the body of the function */
            Error = ParseExpr(
                out FunctionBodyOut,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* pop lexical level */
            Context.SymbolTable.DecrementSymbolTableLevel();

            return CompileErrors.eCompileNoError;
        }




        /* this parses an argument list.  the argument list may be empty, in which case */
        /* the empty list (null) is returned in *FormalArgListOut. */
        /*  15:   <formalparamstart> ::= <formalparamlist> */
        /*  16:                      ::=  */
        /* FIRST SET: */
        /* <formalparamstart> : {<identifier>, <formalparamlist>, <formalarg>} */
        /* FOLLOW SET: */
        /* <formalparamstart> : {)} */
        private static CompileErrors ParseFormalParamStart(
            out SymbolListRec FormalArgListOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            LineNumberOut = -1;

            /* get a token so we can lookahead */
            Token = Context.Scanner.GetNextToken();

            /* if it's a paren, then we abort */
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                /* stuff it back */
                Context.Scanner.UngetToken(Token);

                /* we want to return the empty list since argument list is empty */
                FormalArgListOut = null;

                return CompileErrors.eCompileNoError;
            }

            /* stuff it back */
            Context.Scanner.UngetToken(Token);

            /* it really is something, so parse it */
            return ParseFormalParamList(
                out FormalArgListOut,
                Context,
                out LineNumberOut);
        }




        /* this function parses a type and returns the corresponding enumeration value */
        /* in *TypeOut. */
        /*   3:   <type>             ::= void */
        /*   4:                      ::= bool */
        /*   5:                      ::= int */
        /*   6:                      ::= single */
        /*   7:                      ::= double */
        /*   8:                      ::= fixed */
        /*   9:                      ::= boolarray */
        /*  10:                      ::= intarray */
        /*  11:                      ::= singlearray */
        /*  12:                      ::= doublearray */
        /*  13:                      ::= fixedarray */
        /* FIRST SET: */
        /* <type>             : {void, bool, int, single, double, fixed, boolarray, */
        /*      intarray, singlearray, doublearray, fixedarray} */
        /* FOLLOW SET: */
        /*  <type>             : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, var, not, sin, cos, */
        /*       tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, if, */
        /*       then, else, elseif, while, do, resize, to, error, true, */
        /*       false, set, (, ), CLOSEBRACKET, , , :=, ;, -, EQ, <expr>, */
        /*       <formalargtail>, <vartail>, <expr2>, <expr3>, <expr4>, <expr5>, <expr6>, */
        /*       <unary_oper>, <expr7>, <expr8>, <actualtail>, <iftail>, <whileloop>, */
        /*       <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseType(
            out DataTypes TypeOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            TypeOut = DataTypes.eInvalidDataType;
            LineNumberOut = -1;

            /* get the word */
            Token = Context.Scanner.GetNextToken();

            /* make sure it's a keyword */
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedTypeSpecifier;
            }

            /* do the decoding */
            switch (Token.GetTokenKeywordTag())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedTypeSpecifier;
                case KeywordsType.eExprKwrdVoid:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileVoidExpressionIsNotAllowed;
                case KeywordsType.eExprKwrdBool:
                    TypeOut = DataTypes.eBoolean;
                    break;
                case KeywordsType.eExprKwrdInt:
                    TypeOut = DataTypes.eInteger;
                    break;
                case KeywordsType.eExprKwrdSingle:
                case KeywordsType.eExprKwrdFloat:
                    TypeOut = DataTypes.eFloat;
                    break;
                case KeywordsType.eExprKwrdDouble:
                    TypeOut = DataTypes.eDouble;
                    break;
                case KeywordsType.eExprKwrdBoolarray:
                    TypeOut = DataTypes.eArrayOfBoolean;
                    break;
                case KeywordsType.eExprKwrdBytearray:
                    TypeOut = DataTypes.eArrayOfByte;
                    break;
                case KeywordsType.eExprKwrdIntarray:
                    TypeOut = DataTypes.eArrayOfInteger;
                    break;
                case KeywordsType.eExprKwrdSinglearray:
                case KeywordsType.eExprKwrdFloatarray:
                    TypeOut = DataTypes.eArrayOfFloat;
                    break;
                case KeywordsType.eExprKwrdDoublearray:
                    TypeOut = DataTypes.eArrayOfDouble;
                    break;
            }

            return CompileErrors.eCompileNoError;
        }




        /*   26:   <expr>             ::= <expr2> */
        /*  109:   <expr>             ::= if <ifrest> */
        /*  114:   <expr>             ::= <whileloop> */
        /*  115:                      ::= do <expr> <loopwhile> */
        /*  121:   <expr>             ::= set <expr> := <expr> */
        /*  125:   <expr>             ::= resize <expr> to <expr> */
        /*  126:                      ::= error <string> [resumable <expr>] */
        /*  XXX:                      ::= getsampleleft <string> */
        /*  XXX:                      ::= getsampleright <string> */
        /*  XXX:                      ::= getsample <string> */
        /*  XXX:                      ::= getwavenumframes <string> */
        /*  XXX:                      ::= getwavenumtables <string> */
        /*  XXX:                      ::= getwavedata <string> */
        /*  XXX:                      ::= print (<string> | <expr>) */
        /*  XXX:                      ::= butterworthbandpass ( <actualstart> )  */
        /*  XXX:                      ::= firstorderlowpass ( <actualstart> )  */
        /*  XXX:                      ::= vecsqr ( <actualstart> )  */
        /*  XXX:                      ::= vecsqrt ( <actualstart> )  */
        /* FIRST SET: */
        /*  <expr>             : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, var, not, sin, cos, */
        /*       tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, if, */
        /*       while, do, resize, error, true, false, set, (, -, */
        /*       <expr2>, <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, */
        /*       <expr7>, <expr8>, <whileloop> } */
        /* FOLLOW SET: */
        /*  <expr>             : {then, else, elseif, while, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        public static CompileErrors ParseExpr(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            int LineNumberForFirstToken;
            KeywordsType TokenKeywordTag;

            ExpressionOut = null;
            LineNumberOut = -1;

            /* get the first token to be gotten */
            Token = Context.Scanner.GetNextToken();
            LineNumberForFirstToken = Context.Scanner.GetCurrentLineNumber();

            /* see what action should be taken */
            switch (Token.GetTokenType())
            {
                default:
                DefaultAction:
                    Context.Scanner.UngetToken(Token);
                    return ParseExpr2(
                        out ExpressionOut,
                        Context,
                        out LineNumberOut);

                case TokenTypes.eTokenKeyword:
                    TokenKeywordTag = Token.GetTokenKeywordTag();
                    switch (TokenKeywordTag)
                    {
                        default:
                            Context.Scanner.UngetToken(Token);
                            return ParseExpr2(
                                out ExpressionOut,
                                Context,
                                out LineNumberOut);

                        /*  109:   <expr>             ::= if <ifrest> */
                        case KeywordsType.eExprKwrdIf:
                            return ParseIfRest(
                                out ExpressionOut,
                                Context,
                                out LineNumberOut);

                        /*  114:   <expr>             ::= <whileloop> */
                        /* FIRST SET: */
                        /*  <whileloop>        : {while} */
                        case KeywordsType.eExprKwrdWhile:
                            Context.Scanner.UngetToken(Token);
                            return ParseWhileLoop(
                                out ExpressionOut,
                                Context,
                                out LineNumberOut);

                        /*  115:                      ::= do <expr> <loopwhile> */
                        case KeywordsType.eExprKwrdDo:
                            {
                                ASTExpression BodyExpression;
                                CompileErrors Error;

                                Error = ParseExpr(
                                    out BodyExpression,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }

                                /* parse the rest of it */
                                return ParseLoopWhile(
                                    out ExpressionOut,
                                    BodyExpression,
                                    Context,
                                    out LineNumberOut,
                                    LineNumberForFirstToken);
                            }

                        /*  121:   <expr>             ::= set <expr> := <expr> */
                        case KeywordsType.eExprKwrdSet:
                            {
                                ASTExpression LValue;
                                ASTExpression RValue;
                                CompileErrors Error;
                                ASTAssignment TotalAssignment;

                                Error = ParseExpr(
                                    out LValue,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }

                                /* swallow the colon-equals */
                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() != TokenTypes.eTokenColonEqual)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileExpectedColonEqual;
                                }

                                Error = ParseExpr(
                                    out RValue,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }

                                TotalAssignment = new ASTAssignment(
                                    LValue,
                                    RValue,
                                    LineNumberForFirstToken);

                                ExpressionOut = new ASTExpression(
                                    TotalAssignment,
                                    LineNumberForFirstToken);

                                return CompileErrors.eCompileNoError;
                            }

                        /*  125:   <expr>             ::= resize <expr> to <expr> */
                        case KeywordsType.eExprKwrdResize:
                            {
                                ASTExpression ArrayGenerator;
                                ASTExpression NewSizeExpression;
                                CompileErrors Error;
                                ASTBinaryOperation BinaryOperator;

                                Error = ParseExpr(
                                    out ArrayGenerator,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }

                                /* swallow the to */
                                Token = Context.Scanner.GetNextToken();
                                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                    || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdTo))
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileExpectedTo;
                                }

                                Error = ParseExpr(
                                    out NewSizeExpression,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }

                                BinaryOperator = new ASTBinaryOperation(
                                    BinaryOperatorKind.eBinaryResizeArray,
                                    ArrayGenerator,
                                    NewSizeExpression,
                                    LineNumberForFirstToken);

                                ExpressionOut = new ASTExpression(
                                    BinaryOperator,
                                    LineNumberForFirstToken);

                                return CompileErrors.eCompileNoError;
                            }

                        /*  126:                      ::= error <string> [resumable <expr>] */
                        case KeywordsType.eExprKwrdError:
                            {
                                TokenRec<KeywordsType> MessageString;
                                ASTExpression ResumableCondition;
                                CompileErrors Error;
                                ASTErrorForm ErrorForm;

                                MessageString = Context.Scanner.GetNextToken();
                                if (MessageString.GetTokenType() != TokenTypes.eTokenString)
                                {
                                    LineNumberOut = LineNumberForFirstToken;
                                    return CompileErrors.eCompileExpectedStringLiteral;
                                }

                                /* swallow the resumable */
                                Token = Context.Scanner.GetNextToken();
                                if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                    && (Token.GetTokenKeywordTag() == KeywordsType.eExprKwrdResumable))
                                {
                                    Error = ParseExpr(
                                        out ResumableCondition,
                                        Context,
                                        out LineNumberOut);
                                    if (Error != CompileErrors.eCompileNoError)
                                    {
                                        return Error;
                                    }
                                }
                                else
                                {
                                    // ought to check follow set here, but parser has been modified by hand so unreliable.
                                    Context.Scanner.UngetToken(Token);
                                    ResumableCondition = new ASTExpression(
                                        new ASTOperand(
                                            false,
                                            Context.Scanner.GetCurrentLineNumber()),
                                        Context.Scanner.GetCurrentLineNumber());
                                }

                                ErrorForm = new ASTErrorForm(
                                    ResumableCondition,
                                    MessageString.GetTokenStringValue(),
                                    LineNumberForFirstToken);

                                ExpressionOut = new ASTExpression(
                                    ErrorForm,
                                    LineNumberForFirstToken);

                                return CompileErrors.eCompileNoError;
                            }

                        /*  XXX:                      ::= getsampleleft <string> */
                        /*  XXX:                      ::= getsampleright <string> */
                        /*  XXX:                      ::= getsample <string> */
                        /*  XXX:                      ::= getwavenumframes <string> */
                        /*  XXX:                      ::= getwavenumtables <string> */
                        /*  XXX:                      ::= getwavedata <string> */
                        case KeywordsType.eExprKwrdGetwavedata:
                        case KeywordsType.eExprKwrdGetwavenumtables:
                        case KeywordsType.eExprKwrdGetwavenumframes:
                        case KeywordsType.eExprKwrdGetsample:
                        case KeywordsType.eExprKwrdGetsampleright:
                        case KeywordsType.eExprKwrdGetsampleleft:
                            {
                                ASTWaveGetter WaveGetterThang;
                                WaveGetterKind Op;

                                switch (TokenKeywordTag)
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case KeywordsType.eExprKwrdGetsampleleft:
                                        Op = WaveGetterKind.eWaveGetterSampleLeft;
                                        break;
                                    case KeywordsType.eExprKwrdGetsampleright:
                                        Op = WaveGetterKind.eWaveGetterSampleRight;
                                        break;
                                    case KeywordsType.eExprKwrdGetsample:
                                        Op = WaveGetterKind.eWaveGetterSampleMono;
                                        break;
                                    case KeywordsType.eExprKwrdGetwavenumframes:
                                        Op = WaveGetterKind.eWaveGetterWaveFrames;
                                        break;
                                    case KeywordsType.eExprKwrdGetwavenumtables:
                                        Op = WaveGetterKind.eWaveGetterWaveTables;
                                        break;
                                    case KeywordsType.eExprKwrdGetwavedata:
                                        Op = WaveGetterKind.eWaveGetterWaveArray;
                                        break;
                                }

                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() != TokenTypes.eTokenString)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileExpectedStringLiteral;
                                }

                                WaveGetterThang = new ASTWaveGetter(
                                    Token.GetTokenStringValue(),
                                    Op,
                                    Context.Scanner.GetCurrentLineNumber());

                                ExpressionOut = new ASTExpression(
                                    WaveGetterThang,
                                    Context.Scanner.GetCurrentLineNumber());

                                return CompileErrors.eCompileNoError;
                            }

                        /*  XXX:                      ::= print (<string> | <expr>) */
                        case KeywordsType.eExprKwrdPrint:
                            Token = Context.Scanner.GetNextToken();
                            if (Token.GetTokenType() == TokenTypes.eTokenString)
                            {
                                ASTPrintString PrintString;

                                /* print string literal part */
                                PrintString = new ASTPrintString(
                                    Token.GetTokenStringValue(),
                                    LineNumberForFirstToken);
                                ExpressionOut = new ASTExpression(
                                    PrintString,
                                    LineNumberForFirstToken);
                            }
                            else
                            {
                                ASTPrintExpression PrintExpr;
                                ASTExpression Operand;
                                CompileErrors Error;

                                /* print expression part */
                                Context.Scanner.UngetToken(Token);
                                Error = ParseExpr(
                                    out Operand,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }
                                PrintExpr = new ASTPrintExpression(
                                    Operand,
                                    LineNumberForFirstToken);
                                ExpressionOut = new ASTExpression(
                                    PrintExpr,
                                    LineNumberForFirstToken);
                            }
                            return CompileErrors.eCompileNoError;

                        /*  XXX:                      ::= loadsample <string>, <string> */
                        /*  XXX:                      ::= loadsampleleft <string>, <string> */
                        /*  XXX:                      ::= loadsampleright <string>, <string> */
                        case KeywordsType.eExprKwrdLoadsampleleft:
                        case KeywordsType.eExprKwrdLoadsampleright:
                        case KeywordsType.eExprKwrdLoadsample:
                            {
                                ASTSampleLoader SampleLoaderThang;
                                TokenRec<KeywordsType> Token2;
                                SampleLoaderKind Op;

                                switch (TokenKeywordTag)
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case KeywordsType.eExprKwrdLoadsampleleft:
                                        Op = SampleLoaderKind.eSampleLoaderSampleLeft;
                                        break;
                                    case KeywordsType.eExprKwrdLoadsampleright:
                                        Op = SampleLoaderKind.eSampleLoaderSampleRight;
                                        break;
                                    case KeywordsType.eExprKwrdLoadsample:
                                        Op = SampleLoaderKind.eSampleLoaderSampleMono;
                                        break;
                                }

                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() != TokenTypes.eTokenString)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileExpectedStringLiteral;
                                }

                                Token2 = Context.Scanner.GetNextToken();
                                if (Token2.GetTokenType() != TokenTypes.eTokenComma)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileExpectedComma;
                                }

                                Token2 = Context.Scanner.GetNextToken();
                                if (Token2.GetTokenType() != TokenTypes.eTokenString)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileExpectedStringLiteral;
                                }

                                SampleLoaderThang = new ASTSampleLoader(
                                    Token.GetTokenStringValue(), /* filetype */
                                    Token2.GetTokenStringValue(), /* filename */
                                    Op,
                                    Context.Scanner.GetCurrentLineNumber());

                                ExpressionOut = new ASTExpression(
                                    SampleLoaderThang,
                                    Context.Scanner.GetCurrentLineNumber());

                                return CompileErrors.eCompileNoError;
                            }

                        case KeywordsType.eExprKwrdFor:
                            {
                                CompileErrors Error;

                                Context.SymbolTable.IncrementSymbolTableLevel();

                                SymbolRec LoopVariable;
                                int LoopVariableLineNumber;
                                ASTExpression InitialValue;
                                {
                                    TokenRec<KeywordsType> LoopVariableName = Context.Scanner.GetNextToken();
                                    if (LoopVariableName.GetTokenType() != TokenTypes.eTokenIdentifier)
                                    {
                                        LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                        return CompileErrors.eCompileExpectedIdentifier;
                                    }
                                    LoopVariableLineNumber = Context.Scanner.GetCurrentLineNumber();

                                    /* get the colon */
                                    Token = Context.Scanner.GetNextToken();
                                    if (Token.GetTokenType() != TokenTypes.eTokenColon)
                                    {
                                        LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                        return CompileErrors.eCompileExpectedColon;
                                    }

                                    /* get the variable's type */
                                    DataTypes VariableType;
                                    Error = ParseType(
                                        out VariableType,
                                        Context,
                                        out LineNumberOut);
                                    if (Error != CompileErrors.eCompileNoError)
                                    {
                                        return Error;
                                    }

                                    /* create symbol table entry */
                                    LoopVariable = new SymbolRec(LoopVariableName.GetTokenIdentifierString());
                                    LoopVariable.SymbolBecomeVariable(VariableType);
                                    bool f = Context.SymbolTable.Add(LoopVariable);
                                    Debug.Assert(f); // should never fail with duplicate since we pushed a lexical scope just for this stmt

                                    /* get the = */
                                    Token = Context.Scanner.GetNextToken();
                                    if (Token.GetTokenType() != TokenTypes.eTokenEqual)
                                    {
                                        LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                        return CompileErrors.eCompileExpectedEqual;
                                    }

                                    Error = ParseExpr(
                                        out InitialValue,
                                        Context,
                                        out LineNumberOut);
                                    if (Error != CompileErrors.eCompileNoError)
                                    {
                                        return Error;
                                    }
                                }

                                // eat "while"
                                Token = Context.Scanner.GetNextToken();
                                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                    || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdWhile))
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileExpectedWhile;
                                }

                                ASTExpression WhileExpression;
                                Error = ParseExpr(
                                    out WhileExpression,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }

                                ASTAssignment IncrementExpression;
                                Token = Context.Scanner.GetNextToken();
                                if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                    && (Token.GetTokenKeywordTag() == KeywordsType.eExprKwrdDo))
                                {
                                    // implicit form

                                    Context.Scanner.UngetToken(Token);

                                    IncrementExpression = new ASTAssignment(
                                        new ASTExpression(new ASTOperand(LoopVariable, LoopVariableLineNumber), LoopVariableLineNumber),
                                        new ASTExpression(
                                            new ASTBinaryOperation(
                                                BinaryOperatorKind.eBinaryPlus,
                                                new ASTExpression(new ASTOperand(LoopVariable, LoopVariableLineNumber), LoopVariableLineNumber),
                                                new ASTExpression(
                                                    LoopVariable.VariableDataType != DataTypes.eFloat
                                                        ? new ASTOperand(1, Context.Scanner.GetCurrentLineNumber())
                                                        : new ASTOperand(1, Context.Scanner.GetCurrentLineNumber()),
                                                    Context.Scanner.GetCurrentLineNumber()),
                                                Context.Scanner.GetCurrentLineNumber()),
                                            Context.Scanner.GetCurrentLineNumber()),
                                        Context.Scanner.GetCurrentLineNumber());
                                }
                                else
                                {
                                    Token = Context.Scanner.GetNextToken();
                                    if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                        && (Token.GetTokenKeywordTag() == KeywordsType.eExprKwrdSet))
                                    {
                                        // general form: step set <lvalue> := <rvalue>

                                        ASTExpression LValue;
                                        ASTExpression RValue;

                                        Error = ParseExpr(
                                            out LValue,
                                            Context,
                                            out LineNumberOut);
                                        if (Error != CompileErrors.eCompileNoError)
                                        {
                                            return Error;
                                        }

                                        /* swallow the colon-equals */
                                        Token = Context.Scanner.GetNextToken();
                                        if (Token.GetTokenType() != TokenTypes.eTokenColonEqual)
                                        {
                                            LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                            return CompileErrors.eCompileExpectedColonEqual;
                                        }

                                        Error = ParseExpr(
                                            out RValue,
                                            Context,
                                            out LineNumberOut);
                                        if (Error != CompileErrors.eCompileNoError)
                                        {
                                            return Error;
                                        }

                                        IncrementExpression = new ASTAssignment(
                                            LValue,
                                            RValue,
                                            LineNumberForFirstToken);
                                    }
                                    else
                                    {
                                        Context.Scanner.UngetToken(Token);

                                        // special form: step <expression>

                                        ASTExpression RValue;
                                        Error = ParseExpr(
                                            out RValue,
                                            Context,
                                            out LineNumberOut);
                                        if (Error != CompileErrors.eCompileNoError)
                                        {
                                            return Error;
                                        }

                                        IncrementExpression = new ASTAssignment(
                                            new ASTExpression(new ASTOperand(LoopVariable, LoopVariableLineNumber), LoopVariableLineNumber),
                                            new ASTExpression(
                                                new ASTBinaryOperation(
                                                    BinaryOperatorKind.eBinaryPlus,
                                                    new ASTExpression(new ASTOperand(LoopVariable, LoopVariableLineNumber), LoopVariableLineNumber),
                                                    RValue,
                                                    RValue.LineNumber),
                                                RValue.LineNumber),
                                            RValue.LineNumber);
                                    }
                                }

                                // eat "do"
                                Token = Context.Scanner.GetNextToken();
                                if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                                    || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdDo))
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileExpectedDo;
                                }

                                ASTExpression Body;
                                Error = ParseExpr(
                                    out Body,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }

                                Context.SymbolTable.DecrementSymbolTableLevel();

                                ASTForLoop ForLoop = new ASTForLoop(
                                    LoopVariable,
                                    InitialValue,
                                    WhileExpression,
                                    IncrementExpression,
                                    Body,
                                    LineNumberForFirstToken);

                                ExpressionOut = new ASTExpression(
                                    ForLoop,
                                    LineNumberForFirstToken);

                                return CompileErrors.eCompileNoError;
                            }
                    }
            }
        }




        /*   17:   <formalparamlist>  ::= <formalarg> <formalargtail>  */
        /* FIRST SET: */
        /*  <formalparamlist>  : {<identifier>, <formalarg>} */
        /* FOLLOW SET: */
        /*  <formalparamlist>  : {)} */
        private static CompileErrors ParseFormalParamList(
            out SymbolListRec FormalArgListOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            SymbolRec FormalArgOut;
            SymbolListRec ListTail;

            FormalArgListOut = null;

            Error = ParseFormalArg(
                out FormalArgOut,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            Error = ParseFormalArgTail(
                out ListTail,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            FormalArgListOut = SymbolListRec.Cons(
                FormalArgOut,
                ListTail);

            return CompileErrors.eCompileNoError;
        }




        /*  117:   <whileloop>        ::= while <expr> do <expr> */
        /* FIRST SET: */
        /*  <whileloop>        : {while} */
        /* FOLLOW SET: */
        /*  <whileloop>        : {then, else, elseif, while, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseWhileLoop(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTExpression ConditionalExpr;
            ASTExpression BodyExpr;
            CompileErrors Error;
            ASTLoop WhileLoopThing;
            int LineNumberOfWholeForm;

            ExpressionOut = null;

            LineNumberOfWholeForm = Context.Scanner.GetCurrentLineNumber();

            /* munch while */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdWhile))
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedWhile;
            }

            Error = ParseExpr(
                out ConditionalExpr,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* munch do */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdDo))
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedDo;
            }

            Error = ParseExpr(
                out BodyExpr,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            WhileLoopThing = new ASTLoop(
                LoopKind.eLoopWhileDo,
                ConditionalExpr,
                BodyExpr,
                LineNumberOfWholeForm);

            ExpressionOut = new ASTExpression(
                WhileLoopThing,
                LineNumberOfWholeForm);

            return CompileErrors.eCompileNoError;
        }




        /*   24:   <vartail>          ::= EQ <expr> */
        /*   25:                      ::= ( <expr> ) */
        /* FIRST SET: */
        /*  <vartail>          : {(, EQ} */
        /* FOLLOW SET: */
        /*  <vartail>          : {then, else, elseif, while, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseVarTail(
            out ASTExpression ExpressionOut,
            TokenRec<KeywordsType> VariableName,
            int VariableDeclLine,
            DataTypes VariableType,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            SymbolRec SymbolTableEntry;

            ExpressionOut = null;

            Token = Context.Scanner.GetNextToken();

            /* create symbol table entry */
            SymbolTableEntry = new SymbolRec(VariableName.GetTokenIdentifierString());
            SymbolTableEntry.SymbolBecomeVariable(VariableType);

            /* see what to do */
            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedOpenParenOrEqual;

                /* array declaration */
                case TokenTypes.eTokenOpenParen:
                    {
                        ASTExpression ArraySizeExpression;
                        CompileErrors Error;
                        ASTArrayDeclaration ArrayConstructor;

                        if ((VariableType != DataTypes.eArrayOfBoolean)
                            && (VariableType != DataTypes.eArrayOfByte)
                            && (VariableType != DataTypes.eArrayOfInteger)
                            && (VariableType != DataTypes.eArrayOfFloat)
                            && (VariableType != DataTypes.eArrayOfDouble))
                        {
                            LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                            return CompileErrors.eCompileArrayConstructionOnScalarType;
                        }

                        Error = ParseExpr(
                            out ArraySizeExpression,
                            Context,
                            out LineNumberOut);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }

                        /* swallow the close paren */
                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
                        {
                            LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                            return CompileErrors.eCompileExpectedCloseParen;
                        }

                        /* build the array constructor node */
                        ArrayConstructor = new ASTArrayDeclaration(
                            SymbolTableEntry,
                            ArraySizeExpression,
                            VariableDeclLine);

                        /* build AST node */
                        ExpressionOut = new ASTExpression(
                            ArrayConstructor,
                            VariableDeclLine);
                    }
                    break;

                /* variable construction */
                case TokenTypes.eTokenEqual:
                    {
                        ASTExpression Initializer;
                        CompileErrors Error;
                        ASTVariableDeclaration VariableConstructor;

                        Error = ParseExpr(
                            out Initializer,
                            Context,
                            out LineNumberOut);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }

                        /* build variable thing */
                        VariableConstructor = new ASTVariableDeclaration(
                            SymbolTableEntry,
                            Initializer,
                            VariableDeclLine);

                        /* encapsulate */
                        ExpressionOut = new ASTExpression(
                            VariableConstructor,
                            VariableDeclLine);
                    }
                    break;
            }

            /* add the identifier to the symbol table */
            if (!Context.SymbolTable.Add(SymbolTableEntry))
            {
                LineNumberOut = VariableDeclLine;
                return CompileErrors.eCompileMultiplyDefinedIdentifier;
            }

            return CompileErrors.eCompileNoError;
        }




        /*  110:   <ifrest>           ::= <expr> then <expr> <iftail> */
        /* FIRST SET: */
        /*  <ifrest>           : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, var, not, sin, */
        /*       cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, */
        /*       if, while, do, resize, error, true, false, set, (, -, */
        /*       <expr>, <expr2>, <expr3>, <expr4>, <expr5>, <expr6>, */
        /*       <unary_oper>, <expr7>, <expr8>, <whileloop>} */
        /* FOLLOW SET: */
        /*  <ifrest>           : {then, else, elseif, while, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseIfRest(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            CompileErrors Error;
            ASTExpression Predicate;
            ASTExpression Consequent;

            ExpressionOut = null;

            Error = ParseExpr(
                out Predicate,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* eat the "then" */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdThen))
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedThen;
            }

            Error = ParseExpr(
                out Consequent,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            return ParseIfTail(
                out ExpressionOut,
                Predicate,
                Consequent,
                Context,
                out LineNumberOut);
        }




        /*  119:   <loopwhile> ::= while <expr> */
        /* FIRST SET: */
        /*  <loopwhile>        : {while} */
        /* FOLLOW SET: */
        /*  <loopwhile>        : {then, else, elseif, while, do, to, */
        /*       ), CLOSEBRACKET, , , :=, ;, <actualtail>, <iftail>, <loopwhile>, */
        /*       <exprlisttail>} */
        private static CompileErrors ParseLoopWhile(
            out ASTExpression ExpressionOut,
            ASTExpression LoopBodyExpression,
            ParserContext Context,
            out int LineNumberOut,
            int LineNumberOfLoop)
        {
            TokenRec<KeywordsType> Token;
            LoopKind LoopKind;
            ASTExpression ConditionalExpression;
            CompileErrors Error;
            ASTLoop LoopThang;

            ExpressionOut = null;

            /* see what there is to do */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdWhile))
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedWhile;
            }

            LoopKind = LoopKind.eLoopDoWhile;

            Error = ParseExpr(
                out ConditionalExpression,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            LoopThang = new ASTLoop(
                LoopKind,
                ConditionalExpression,
                LoopBodyExpression,
                LineNumberOfLoop);

            ExpressionOut = new ASTExpression(
                LoopThang,
                LineNumberOfLoop);

            return CompileErrors.eCompileNoError;
        }




        /*   27:   <expr2>            ::= <expr3> <expr2prime> */
        /* FIRST SET: */
        /*  <expr2>            : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, not, sin, cos, tan, asin, */
        /*       acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, true, */
        /*       false, (, -, <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, <expr7>, */
        /*       <expr8>} */
        /* FOLLOW SET: */
        /*  <expr2>            : {then, else, elseif, while, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr2(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpression LeftHandSide;

            ExpressionOut = null;

            Error = ParseExpr3(
                out LeftHandSide,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            return ParseExpr2Prime(
                out ExpressionOut,
                LeftHandSide,
                Context,
                out LineNumberOut);
        }




        /*   18:   <formalarg>        ::= <identifier> : <type> */
        /* FIRST SET: */
        /*  <formalarg>        : {<identifier>} */
        /* FOLLOW SET: */
        /*  <formalarg>        : {), , , <formalargtail>} */
        private static CompileErrors ParseFormalArg(
            out SymbolRec FormalArgOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> IdentifierName;
            TokenRec<KeywordsType> Token;
            DataTypes Type;
            CompileErrors Error;
            int LineNumberOfIdentifier;

            FormalArgOut = null;

            LineNumberOfIdentifier = Context.Scanner.GetCurrentLineNumber();

            IdentifierName = Context.Scanner.GetNextToken();
            if (IdentifierName.GetTokenType() != TokenTypes.eTokenIdentifier)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedIdentifier;
            }

            /* swallow the colon */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenColon)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedColon;
            }

            Error = ParseType(
                out Type,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            FormalArgOut = new SymbolRec(IdentifierName.GetTokenIdentifierString());
            FormalArgOut.SymbolBecomeVariable(Type);

            if (!Context.SymbolTable.Add(FormalArgOut))
            {
                LineNumberOut = LineNumberOfIdentifier;
                return CompileErrors.eCompileMultiplyDefinedIdentifier;
            }

            return CompileErrors.eCompileNoError;
        }




        /*   19:   <formalargtail>    ::= , <formalparamlist> */
        /*   20:                      ::=  */
        /* FIRST SET: */
        /*  <formalargtail>    : {, } */
        /* FOLLOW SET: */
        /*  <formalargtail>    : {)} */
        private static CompileErrors ParseFormalArgTail(
            out SymbolListRec ArgListTailOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            ArgListTailOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedCommaOrCloseParen;

                case TokenTypes.eTokenComma:
                    return ParseFormalParamList(
                        out ArgListTailOut,
                        Context,
                        out LineNumberOut);

                case TokenTypes.eTokenCloseParen:
                    Context.Scanner.UngetToken(Token);
                    ArgListTailOut = null; /* end of list */
                    return CompileErrors.eCompileNoError;
            }
        }




        /*  111:   <iftail>           ::= else <expr> */
        /*  112:                      ::= elseif <ifrest> */
        /*  113:                      ::=  */
        /* FIRST SET: */
        /*  <iftail>           : {else, elseif} */
        /* FOLLOW SET: */
        /*  <iftail>           : {then, else, elseif, while, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        /* note that 'else' and 'elseif' are in both the first and follow set.  this is */
        /* because if-then-else isn't LL(1).  we handle this by binding else to the deepest */
        /* if statement. */
        private static CompileErrors ParseIfTail(
            out ASTExpression ExpressionOut,
            ASTExpression Predicate,
            ASTExpression Consequent,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTConditional Conditional;
            ASTExpression Alternative;
            CompileErrors Error;

            ExpressionOut = null;
            LineNumberOut = -1;

            /* see what the token is */
            Token = Context.Scanner.GetNextToken();

            /* do the operation */
            switch (Token.GetTokenType())
            {
                /*  113:                      ::=  */
                default:
                NullificationPoint:
                    Context.Scanner.UngetToken(Token);
                    Conditional = new ASTConditional(
                        Predicate,
                        Consequent,
                        null,
                        Context.Scanner.GetCurrentLineNumber());
                    break;

                case TokenTypes.eTokenKeyword:
                    switch (Token.GetTokenKeywordTag())
                    {
                        /*  113:                      ::=  */
                        default:
                            goto NullificationPoint;

                        /*  111:   <iftail>           ::= else <expr> */
                        case KeywordsType.eExprKwrdElse:
                            Error = ParseExpr(
                                out Alternative,
                                Context,
                                out LineNumberOut);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                return Error;
                            }
                            Conditional = new ASTConditional(
                                Predicate,
                                Consequent,
                                Alternative,
                                Context.Scanner.GetCurrentLineNumber());
                            break;

                        /*  112:                      ::= elseif <ifrest> */
                        case KeywordsType.eExprKwrdElseif:
                            Error = ParseIfRest(
                                out Alternative,
                                Context,
                                out LineNumberOut);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                return Error;
                            }
                            Conditional = new ASTConditional(
                                Predicate,
                                Consequent,
                                Alternative,
                                Context.Scanner.GetCurrentLineNumber());
                            break;
                    }
                    break;
            }

            /* finish building expression node */
            ExpressionOut = new ASTExpression(
                Conditional,
                Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*   33:   <expr3>            ::= <expr4> <expr3prime> */
        /* FIRST SET: */
        /*  <expr3>            : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, not, sin, cos, tan, asin, */
        /*       acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, true, */
        /*       false, (, -, <expr4>, <expr5>, <expr6>, <unary_oper>, <expr7>, <expr8>} */
        /* FOLLOW SET: */
        /*  <expr3>            : {and, or, xor, then, else, elseif, while, do, */
        /*       to, ), CLOSEBRACKET, , , :=, ;, <expr2prime>, <conj_oper>, <actualtail>, */
        /*       <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr3(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpression LeftHandSide;

            ExpressionOut = null;

            Error = ParseExpr4(
                out LeftHandSide,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            return ParseExpr3Prime(
                out ExpressionOut,
                LeftHandSide,
                Context,
                out LineNumberOut);
        }




        /*   28:   <expr2prime>       ::= <conj_oper> <expr3> <expr2prime> */
        /*   29:                      ::=  */
        /* FIRST SET: */
        /*  <expr2prime>       : {and, or, xor, <conj_oper>} */
        /* FOLLOW SET: */
        /*  <expr2prime>       : {then, else, elseif, while, do, to, ), */
        /*       CLOSEBRACKET, , , :=, ;, <actualtail>, <iftail>, <loopwhile>, */
        /*       <exprlisttail>} */
        private static CompileErrors ParseExpr2Prime(
            out ASTExpression ExpressionOut,
            ASTExpression LeftHandSide,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            BinaryOperatorKind OperatorType;
            CompileErrors Error;
            ASTExpression RightHandSide;
            ASTBinaryOperation WholeOperator;
            ASTExpression ThisWholeNode;

            ExpressionOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                NullifyPoint:
                    Context.Scanner.UngetToken(Token);
                    ExpressionOut = LeftHandSide;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenKeyword:
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            goto NullifyPoint;

                        case KeywordsType.eExprKwrdAnd:
                        case KeywordsType.eExprKwrdOr:
                        case KeywordsType.eExprKwrdXor:
                            /* actually do the thing */
                            Context.Scanner.UngetToken(Token);
                            Error = ParseConjOper(
                                out OperatorType,
                                Context,
                                out LineNumberOut);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                return Error;
                            }
                            Error = ParseExpr3(
                                out RightHandSide,
                                Context,
                                out LineNumberOut);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                return Error;
                            }
                            /* build the operator node */
                            WholeOperator = new ASTBinaryOperation(
                                OperatorType,
                                LeftHandSide,
                                RightHandSide,
                                Context.Scanner.GetCurrentLineNumber());
                            ThisWholeNode = new ASTExpression(
                                WholeOperator,
                                Context.Scanner.GetCurrentLineNumber());
                            return ParseExpr2Prime(
                                out ExpressionOut,
                                ThisWholeNode,
                                Context,
                                out LineNumberOut);
                    }
            }
        }




        /*   42:   <expr4>            ::= <expr5> <expr4prime> */
        /* FIRST SET: */
        /*  <expr4>            : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, not, sin, cos, tan, asin, */
        /*       acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, true, */
        /*       false, (, -, <expr5>, <expr6>, <unary_oper>, <expr7>, <expr8>} */
        /* FOLLOW SET: */
        /*  <expr4>            : {and, or, xor, then, else, elseif, while, do, */
        /*       to, ), CLOSEBRACKET, , , :=, ;, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, */
        /*       <conj_oper>, <expr3prime>, <rel_oper>, <actualtail>, <iftail>, */
        /*       <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr4(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpression LeftHandSide;

            ExpressionOut = null;

            Error = ParseExpr5(
                out LeftHandSide,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            return ParseExpr4Prime(
                out ExpressionOut,
                LeftHandSide,
                Context,
                out LineNumberOut);
        }




        /*   34:   <expr3prime>       ::= <rel_oper> <expr4> <expr3prime> */
        /*   35:                      ::=  */
        /* FIRST SET: */
        /*  <expr3prime>       : {EQ, NEQ, LT, LTEQ, GR, GREQ, <rel_oper>} */
        /* FOLLOW SET: */
        /*  <expr3prime>       : {and, or, xor, then, else, elseif, while, */
        /*       do, to, ), CLOSEBRACKET, , , :=, ;, <expr2prime>, <conj_oper>, */
        /*       <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr3Prime(
            out ASTExpression ExpressionOut,
            ASTExpression LeftHandSide,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            BinaryOperatorKind OperatorType;
            CompileErrors Error;
            ASTExpression RightHandSide;
            ASTBinaryOperation WholeOperator;
            ASTExpression ThisWholeNode;

            ExpressionOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    Context.Scanner.UngetToken(Token);
                    ExpressionOut = LeftHandSide;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenLess:
                case TokenTypes.eTokenLessEqual:
                case TokenTypes.eTokenGreater:
                case TokenTypes.eTokenGreaterEqual:
                case TokenTypes.eTokenEqual:
                case TokenTypes.eTokenLessGreater:
                    /* actually do the thing */
                    Context.Scanner.UngetToken(Token);
                    Error = ParseRelOper(
                        out OperatorType,
                        Context,
                        out LineNumberOut);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    Error = ParseExpr4(
                        out RightHandSide,
                        Context,
                        out LineNumberOut);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    /* build the operator node */
                    WholeOperator = new ASTBinaryOperation(
                        OperatorType,
                        LeftHandSide,
                        RightHandSide,
                        Context.Scanner.GetCurrentLineNumber());
                    ThisWholeNode = new ASTExpression(
                        WholeOperator,
                        Context.Scanner.GetCurrentLineNumber());
                    return ParseExpr3Prime(
                        out ExpressionOut,
                        ThisWholeNode,
                        Context,
                        out LineNumberOut);
            }
        }




        /*   30:   <conj_oper>        ::= and */
        /*   31:                      ::= or */
        /*   32:                      ::= xor */
        /* FIRST SET: */
        /*  <conj_oper>        : {and, or, xor} */
        /* FOLLOW SET: */
        /*  <conj_oper>        : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, not, sin, cos, tan, */
        /*       asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, */
        /*       true, false, (, -, <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, */
        /*       <expr7>, <expr8>} */
        private static CompileErrors ParseConjOper(
            out BinaryOperatorKind OperatorOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            OperatorOut = BinaryOperatorKind.eInvalid;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedOperatorOrStatement;

                case TokenTypes.eTokenKeyword:
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                            return CompileErrors.eCompileExpectedOperatorOrStatement;

                        case KeywordsType.eExprKwrdAnd:
                            OperatorOut = BinaryOperatorKind.eBinaryAnd;
                            return CompileErrors.eCompileNoError;

                        case KeywordsType.eExprKwrdOr:
                            OperatorOut = BinaryOperatorKind.eBinaryOr;
                            return CompileErrors.eCompileNoError;

                        case KeywordsType.eExprKwrdXor:
                            OperatorOut = BinaryOperatorKind.eBinaryXor;
                            return CompileErrors.eCompileNoError;
                    }
            }
        }




        /*   47:   <expr5>            ::= <expr6> <expr5prime> */
        /* FIRST SET: */
        /*  <expr5>            : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, not, sin, cos, tan, asin, */
        /*       acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, true, */
        /*       false, (, -, <expr6>, <unary_oper>, <expr7>, <expr8>} */
        /* FOLLOW SET: */
        /*  <expr5>            : {and, or, xor, then, else, elseif, while, do, */
        /*       to, ), CLOSEBRACKET, , , :=, ;, +, -, EQ, NEQ, LT, LTEQ, GR, GREQ, */
        /*       <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, <expr4prime>, */
        /*       <add_oper>, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr5(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpression LeftHandSide;

            ExpressionOut = null;

            Error = ParseExpr6(
                out LeftHandSide,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            return ParseExpr5Prime(
                out ExpressionOut,
                LeftHandSide,
                Context,
                out LineNumberOut);
        }




        /*   43:   <expr4prime>       ::= <add_oper> <expr5> <expr4prime> */
        /*   44:                      ::=  */
        /* FIRST SET: */
        /*  <expr4prime>       : {+, -, <add_oper>} */
        /* FOLLOW SET: */
        /*  <expr4prime>       : {and, or, xor, then, else, elseif, while, */
        /*       do, to, ), CLOSEBRACKET, , , :=, ;, EQ, NEQ, LT, LTEQ, GR, GREQ, */
        /*       <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, <actualtail>, */
        /*       <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr4Prime(
            out ASTExpression ExpressionOut,
            ASTExpression LeftHandSide,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            BinaryOperatorKind OperatorType;
            CompileErrors Error;
            ASTExpression RightHandSide;
            ASTBinaryOperation WholeOperator;
            ASTExpression ThisWholeNode;

            ExpressionOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    Context.Scanner.UngetToken(Token);
                    ExpressionOut = LeftHandSide;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenPlus:
                case TokenTypes.eTokenMinus:
                    Context.Scanner.UngetToken(Token);
                    Error = ParseAddOper(
                        out OperatorType,
                        Context,
                        out LineNumberOut);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    Error = ParseExpr5(
                        out RightHandSide,
                        Context,
                        out LineNumberOut);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    /* create the node */
                    WholeOperator = new ASTBinaryOperation(
                        OperatorType,
                        LeftHandSide,
                        RightHandSide,
                        Context.Scanner.GetCurrentLineNumber());
                    ThisWholeNode = new ASTExpression(
                        WholeOperator,
                        Context.Scanner.GetCurrentLineNumber());
                    return ParseExpr4Prime(
                        out ExpressionOut,
                        ThisWholeNode,
                        Context,
                        out LineNumberOut);
            }
        }




        /*   36:   <rel_oper>         ::= LT */
        /*   37:                      ::= LTEQ */
        /*   38:                      ::= GR */
        /*   39:                      ::= GREQ */
        /*   40:                      ::= EQ */
        /*   41:                      ::= NEQ */
        /* FIRST SET: */
        /*  <rel_oper>         : {EQ, NEQ, LT, LTEQ, GR, GREQ} */
        /* FOLLOW SET: */
        /*  <rel_oper>         : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, not, sin, cos, tan, */
        /*       asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, */
        /*       true, false, (, -, <expr4>, <expr5>, <expr6>, <unary_oper>, <expr7>, */
        /*       <expr8>} */
        private static CompileErrors ParseRelOper(
            out BinaryOperatorKind OperatorOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            OperatorOut = BinaryOperatorKind.eInvalid;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedOperatorOrStatement;

                case TokenTypes.eTokenEqual:
                    OperatorOut = BinaryOperatorKind.eBinaryEqual;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenLessGreater:
                    OperatorOut = BinaryOperatorKind.eBinaryNotEqual;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenLess:
                    OperatorOut = BinaryOperatorKind.eBinaryLessThan;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenLessEqual:
                    OperatorOut = BinaryOperatorKind.eBinaryLessThanOrEqual;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenGreater:
                    OperatorOut = BinaryOperatorKind.eBinaryGreaterThan;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenGreaterEqual:
                    OperatorOut = BinaryOperatorKind.eBinaryGreaterThanOrEqual;
                    return CompileErrors.eCompileNoError;
            }
        }




        /*   56:   <expr6>            ::= <unary_oper> <expr6> */
        /*   57:                      ::= <expr7> */
        /* FIRST SET: */
        /*  <expr6>            : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, not, sin, cos, tan, asin, */
        /*       acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, true, */
        /*       false, (, -, <unary_oper>, <expr7>, <expr8>} */
        /* FOLLOW SET: */
        /*  <expr6>            : {and, or, xor, div, mod, SHR, SHL, then, else, elseif, */
        /*       while, do, to, ), CLOSEBRACKET, , , :=, ;, *, /, +, -, EQ, NEQ, */
        /*       LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, */
        /*       <expr4prime>, <add_oper>, <expr5prime>, <mult_oper>, <actualtail>, */
        /*       <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr6(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            UnaryOpKind UnaryOperatorThing;
            bool ExplicitCast = false;
            ASTExpression UnaryArgument;
            CompileErrors Error;
            ASTUnaryOperation UnaryOpNode;

            ExpressionOut = null;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                OtherPoint:
                    Context.Scanner.UngetToken(Token);
                    return ParseExpr7(
                        out ExpressionOut,
                        Context,
                        out LineNumberOut);

                case TokenTypes.eTokenMinus:
                    UnaryOperatorThing = UnaryOpKind.eUnaryNegation;
                    break;

                case TokenTypes.eTokenKeyword:
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            goto OtherPoint;

                        case KeywordsType.eExprKwrdNot:
                            UnaryOperatorThing = UnaryOpKind.eUnaryNot;
                            break;

                        case KeywordsType.eExprKwrdSin:
                            UnaryOperatorThing = UnaryOpKind.eUnarySine;
                            break;

                        case KeywordsType.eExprKwrdCos:
                            UnaryOperatorThing = UnaryOpKind.eUnaryCosine;
                            break;

                        case KeywordsType.eExprKwrdTan:
                            UnaryOperatorThing = UnaryOpKind.eUnaryTangent;
                            break;

                        case KeywordsType.eExprKwrdAsin:
                            UnaryOperatorThing = UnaryOpKind.eUnaryArcSine;
                            break;

                        case KeywordsType.eExprKwrdAcos:
                            UnaryOperatorThing = UnaryOpKind.eUnaryArcCosine;
                            break;

                        case KeywordsType.eExprKwrdAtan:
                            UnaryOperatorThing = UnaryOpKind.eUnaryArcTangent;
                            break;

                        case KeywordsType.eExprKwrdLn:
                            UnaryOperatorThing = UnaryOpKind.eUnaryLogarithm;
                            break;

                        case KeywordsType.eExprKwrdExp:
                            UnaryOperatorThing = UnaryOpKind.eUnaryExponentiation;
                            break;

                        case KeywordsType.eExprKwrdBool:
                            UnaryOperatorThing = UnaryOpKind.eUnaryCastToBoolean;
                            ExplicitCast = true;
                            break;

                        case KeywordsType.eExprKwrdInt:
                            UnaryOperatorThing = UnaryOpKind.eUnaryCastToInteger;
                            ExplicitCast = true;
                            break;

                        case KeywordsType.eExprKwrdSingle:
                        case KeywordsType.eExprKwrdFloat:
                            UnaryOperatorThing = UnaryOpKind.eUnaryCastToSingle;
                            ExplicitCast = true;
                            break;

                        case KeywordsType.eExprKwrdDouble:
                            UnaryOperatorThing = UnaryOpKind.eUnaryCastToDouble;
                            ExplicitCast = true;
                            break;

                        case KeywordsType.eExprKwrdSqr:
                            UnaryOperatorThing = UnaryOpKind.eUnarySquare;
                            break;

                        case KeywordsType.eExprKwrdSqrt:
                            UnaryOperatorThing = UnaryOpKind.eUnarySquareRoot;
                            break;

                        case KeywordsType.eExprKwrdAbs:
                            UnaryOperatorThing = UnaryOpKind.eUnaryAbsoluteValue;
                            break;

                        case KeywordsType.eExprKwrdNeg:
                            UnaryOperatorThing = UnaryOpKind.eUnaryTestNegative;
                            break;

                        case KeywordsType.eExprKwrdSign:
                            UnaryOperatorThing = UnaryOpKind.eUnaryGetSign;
                            break;

                        case KeywordsType.eExprKwrdLength:
                            UnaryOperatorThing = UnaryOpKind.eUnaryGetArrayLength;
                            break;

                        case KeywordsType.eExprKwrdDup:
                            UnaryOperatorThing = UnaryOpKind.eUnaryDuplicateArray;
                            break;

                        case KeywordsType.eExprKwrdFloor:
                            UnaryOperatorThing = UnaryOpKind.eUnaryFloor;
                            break;

                        case KeywordsType.eExprKwrdCeil:
                            UnaryOperatorThing = UnaryOpKind.eUnaryCeil;
                            break;

                        case KeywordsType.eExprKwrdRound:
                            UnaryOperatorThing = UnaryOpKind.eUnaryRound;
                            break;

                        case KeywordsType.eExprKwrdCosh:
                            UnaryOperatorThing = UnaryOpKind.eUnaryCosh;
                            break;

                        case KeywordsType.eExprKwrdSinh:
                            UnaryOperatorThing = UnaryOpKind.eUnarySinh;
                            break;

                        case KeywordsType.eExprKwrdTanh:
                            UnaryOperatorThing = UnaryOpKind.eUnaryTanh;
                            break;
                    }
                    break;
            }

            /* build argument */
            Error = ParseExpr6(
                out UnaryArgument,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* build node */
            UnaryOpNode = new ASTUnaryOperation(
                UnaryOperatorThing,
                UnaryArgument,
                Context.Scanner.GetCurrentLineNumber(),
                ExplicitCast);

            ExpressionOut = new ASTExpression(
                UnaryOpNode,
                Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*   48:   <expr5prime>       ::= <mult_oper> <expr6> <expr5prime> */
        /*   49:                      ::=  */
        /* FIRST SET: */
        /*  <expr5prime>       : {div, mod, SHR, SHL, *, /, <mult_oper>} */
        /* FOLLOW SET: */
        /*  <expr5prime>       : {and, or, xor, then, else, elseif, while, */
        /*       do, to, ), CLOSEBRACKET, , , :=, ;, +, -, EQ, NEQ, LT, LTEQ, GR, */
        /*       GREQ, <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, <expr4prime>, */
        /*       <add_oper>, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr5Prime(
            out ASTExpression ExpressionOut,
            ASTExpression LeftHandSide,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTExpression RightHandSide;
            BinaryOperatorKind OperatorThing;
            CompileErrors Error;
            ASTBinaryOperation BinaryOperator;
            ASTExpression WholeThingThing;

            ExpressionOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            int action;

            switch (Token.GetTokenType())
            {
                default:
                    action = 0;
                    break;
                case TokenTypes.eTokenKeyword:
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            action = 0;
                            break;
                        case KeywordsType.eExprKwrdDiv:
                        case KeywordsType.eExprKwrdMod:
                            action = 1;
                            break;
                    }
                    break;
                case TokenTypes.eTokenLeftLeft:
                case TokenTypes.eTokenRightRight:
                case TokenTypes.eTokenStar:
                case TokenTypes.eTokenSlash:
                    action = 1;
                    break;
            }

            switch (action)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    ExpressionOut = LeftHandSide;
                    Context.Scanner.UngetToken(Token);
                    return CompileErrors.eCompileNoError;
                case 1:
                    Context.Scanner.UngetToken(Token);
                    Error = ParseMultOper(
                        out OperatorThing,
                        Context,
                        out LineNumberOut);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    Error = ParseExpr6(
                        out RightHandSide,
                        Context,
                        out LineNumberOut);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    /* create the node */
                    BinaryOperator = new ASTBinaryOperation(
                        OperatorThing,
                        LeftHandSide,
                        RightHandSide,
                        Context.Scanner.GetCurrentLineNumber());
                    WholeThingThing = new ASTExpression(
                        BinaryOperator,
                        Context.Scanner.GetCurrentLineNumber());
                    return ParseExpr5Prime(
                        out ExpressionOut,
                        WholeThingThing,
                        Context,
                        out LineNumberOut);
            }
        }




        /*   45:   <add_oper>         ::= + */
        /*   46:                      ::= - */
        /*  <add_oper>         : {+, -} */
        /*  <add_oper>         : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, not, sin, cos, tan, */
        /*       asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, */
        /*       true, false, (, -, <expr5>, <expr6>, <unary_oper>, <expr7>, <expr8>} */
        private static CompileErrors ParseAddOper(
            out BinaryOperatorKind OperatorOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            OperatorOut = BinaryOperatorKind.eInvalid;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedOperatorOrStatement;

                case TokenTypes.eTokenPlus:
                    OperatorOut = BinaryOperatorKind.eBinaryPlus;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenMinus:
                    OperatorOut = BinaryOperatorKind.eBinaryMinus;
                    return CompileErrors.eCompileNoError;
            }
        }




        /*   79:   <expr7>            ::= <expr8> <expr7prime> */
        /* FIRST SET: */
        /*  <expr7>            : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, true, false, (, <expr8>} */
        /* FOLLOW SET: */
        /*  <expr7>            : {and, or, xor, div, mod, SHR, SHL, then, else, elseif, */
        /*       while, do, to, ), CLOSEBRACKET, , , :=, ;, *, /, +, -, EQ, NEQ, */
        /*       LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, */
        /*       <expr4prime>, <add_oper>, <expr5prime>, <mult_oper>, <actualtail>, */
        /*       <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr7(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            ASTExpression ResultOfExpr8;
            CompileErrors Error;

            ExpressionOut = null;

            Error = ParseExpr8(
                out ResultOfExpr8,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            return ParseExpr7Prime(
                out ExpressionOut,
                ResultOfExpr8,
                Context,
                out LineNumberOut);
        }




        /*   50:   <mult_oper>        ::= * */
        /*   51:                      ::= / */
        /*   52:                      ::= div */
        /*   53:                      ::= mod */
        /*   54:                      ::= SHL */
        /*   55:                      ::= SHR */
        /*  <mult_oper>        : {div, mod, SHR, SHL, *, /} */
        /*  <mult_oper>        : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, not, sin, cos, tan, */
        /*       asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, */
        /*       true, false, (, -, <expr6>, <unary_oper>, <expr7>, <expr8>} */
        private static CompileErrors ParseMultOper(
            out BinaryOperatorKind OperatorOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            OperatorOut = BinaryOperatorKind.eInvalid;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedOperatorOrStatement;

                case TokenTypes.eTokenStar:
                    OperatorOut = BinaryOperatorKind.eBinaryMultiplication;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenSlash:
                    OperatorOut = BinaryOperatorKind.eBinaryImpreciseDivision;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenLeftLeft:
                    OperatorOut = BinaryOperatorKind.eBinaryShiftLeft;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenRightRight:
                    OperatorOut = BinaryOperatorKind.eBinaryShiftRight;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenKeyword:
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                            return CompileErrors.eCompileExpectedOperatorOrStatement;

                        case KeywordsType.eExprKwrdDiv:
                            OperatorOut = BinaryOperatorKind.eBinaryIntegerDivision;
                            return CompileErrors.eCompileNoError;

                        case KeywordsType.eExprKwrdMod:
                            OperatorOut = BinaryOperatorKind.eBinaryIntegerRemainder;
                            return CompileErrors.eCompileNoError;
                    }
            }
        }




        /*   92:   <expr8>            ::= <identifier> */
        /*   93:                      ::= <integer> */
        /*   94:                      ::= <single> */
        /*   95:                      ::= <double> */
        /*   96:                      ::= <fixed> */
        /*   97:                      ::= <string> */
        /*   98:                      ::= true */
        /*   99:                      ::= false */
        /*  108:                      ::= ( <exprlist> ) */
        /*  <expr8>            : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, true, false, (} */
        /*  <expr8>            : {and, or, xor, div, mod, SHR, SHL, then, else, elseif, */
        /*       while, do, to, (, ), OPENBRACKET, CLOSEBRACKET, , , :=, ;, *, */
        /*       /, +, -, ^, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, */
        /*       <expr3prime>, <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, */
        /*       <mult_oper>, <expr7prime>, <arraysubscript>, <funccall>, <exponentiation>, */
        /*       <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr8(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            ASTOperand TheOperand;
            TokenRec<KeywordsType> Token;

            ExpressionOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedOperand;

                /*   92:   <expr8>            ::= <identifier> */
                case TokenTypes.eTokenIdentifier:
                    {
                        SymbolRec TheSymbolTableEntry;

                        TheSymbolTableEntry = Context.SymbolTable.Lookup(Token.GetTokenIdentifierString());
                        if (TheSymbolTableEntry == null)
                        {
                            /* LineNumberOut = Context.Scanner.GetCurrentLineNumber(); */
                            /* return CompileErrors.eCompileIdentifierNotDeclared; */
                            TheSymbolTableEntry = new SymbolRec(Token.GetTokenIdentifierString());
                            if (!Context.SymbolTable.Add(TheSymbolTableEntry))
                            {
                                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                return CompileErrors.eCompileMultiplyDefinedIdentifier;
                            }
                        }
                        TheOperand = new ASTOperand(
                            TheSymbolTableEntry,
                            Context.Scanner.GetCurrentLineNumber());
                    }
                    break;

                /*   93:                      ::= <integer> */
                case TokenTypes.eTokenInteger:
                    TheOperand = new ASTOperand(
                        Token.GetTokenIntegerValue(),
                        Context.Scanner.GetCurrentLineNumber());
                    break;

                /*   94:                      ::= <single> */
                case TokenTypes.eTokenSingle:
                    TheOperand = new ASTOperand(
                        Token.GetTokenSingleValue(),
                        Context.Scanner.GetCurrentLineNumber());
                    break;

                /*   95:                      ::= <double> */
                case TokenTypes.eTokenDouble:
                    TheOperand = new ASTOperand(
                        Token.GetTokenDoubleValue(),
                        Context.Scanner.GetCurrentLineNumber());
                    break;

                /*   96:                      ::= <fixed> */
                /* defunct */

                /*   97:                      ::= <string> */
                case TokenTypes.eTokenString:
                    TheOperand = new ASTOperand(
                        Token.GetTokenStringValue(),
                        Context.Scanner.GetCurrentLineNumber());
                    break;

                /*  108:                      ::= ( <exprlist> ) */
                case TokenTypes.eTokenOpenParen:
                    {
                        CompileErrors Error;
                        ASTExpressionList ListOfExpressions;

                        /* open a new scope */
                        Context.SymbolTable.IncrementSymbolTableLevel();

                        /* parse the expression sequence */
                        Error = ParseExprList(
                            out ListOfExpressions,
                            Context,
                            out LineNumberOut);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }

                        /* dispose of the current scope */
                        Context.SymbolTable.DecrementSymbolTableLevel();

                        /* build the thing */
                        ExpressionOut = new ASTExpression(
                            ListOfExpressions,
                            Context.Scanner.GetCurrentLineNumber());

                        /* clean up by getting rid of the close paren */
                        Token = Context.Scanner.GetNextToken();
                        if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
                        {
                            LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                            return CompileErrors.eCompileExpectedCloseParen;
                        }
                    }
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenKeyword:
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                            return CompileErrors.eCompileExpectedOperand;

                        /*   98:                      ::= true */
                        case KeywordsType.eExprKwrdTrue:
                            TheOperand = new ASTOperand(
                                true,
                                Context.Scanner.GetCurrentLineNumber());
                            break;

                        /*   99:                      ::= false */
                        case KeywordsType.eExprKwrdFalse:
                            TheOperand = new ASTOperand(
                                false,
                                Context.Scanner.GetCurrentLineNumber());
                            break;

                        /* this was added later. */
                        case KeywordsType.eExprKwrdPi:
                            TheOperand = new ASTOperand(
                                Math.PI,
                                Context.Scanner.GetCurrentLineNumber());
                            break;
                    }
                    break;
            }

            ExpressionOut = new ASTExpression(TheOperand, Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*   80:   <expr7prime>       ::= <arraysubscript> */
        /*   81:                      ::= <funccall> */
        /*   82:                      ::= <exponentiation> */
        /*   83:                      ::=  */
        /*  <expr7prime>       : {(, OPENBRACKET, ^, <arraysubscript>, <funccall>, */
        /*       <exponentiation>} */
        /*  <expr7prime>       : {and, or, xor, div, mod, SHR, SHL, then, else, */
        /*       elseif, while, do, to, ), CLOSEBRACKET, , , :=, ;, *, /, */
        /*       +, -, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, */
        /*       <expr3prime>, <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, */
        /*       <mult_oper>, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExpr7Prime(
            out ASTExpression ExpressionOut,
            ASTExpression TheExpr8Thing,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            ExpressionOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    Context.Scanner.UngetToken(Token);
                    ExpressionOut = TheExpr8Thing;
                    return CompileErrors.eCompileNoError;

                /*   81:                      ::= <funccall> */
                case TokenTypes.eTokenOpenParen:
                    Context.Scanner.UngetToken(Token);
                    return ParseFuncCall(
                        out ExpressionOut,
                        TheExpr8Thing,
                        Context,
                        out LineNumberOut);

                /*   80:   <expr7prime>       ::= <arraysubscript> */
                case TokenTypes.eTokenOpenBracket:
                    Context.Scanner.UngetToken(Token);
                    return ParseArraySubscript(
                        out ExpressionOut,
                        TheExpr8Thing,
                        Context,
                        out LineNumberOut);

                /*   82:                      ::= <exponentiation> */
                case TokenTypes.eTokenCircumflex:
                    {
                        ASTExpression RightHandSide;
                        ASTBinaryOperation TheOperator;
                        CompileErrors Error;

                        Context.Scanner.UngetToken(Token);
                        Error = ParseExponentiation(
                            out RightHandSide,
                            Context,
                            out LineNumberOut);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }
                        TheOperator = new ASTBinaryOperation(
                            BinaryOperatorKind.eBinaryExponentiation,
                            TheExpr8Thing,
                            RightHandSide,
                            Context.Scanner.GetCurrentLineNumber());
                        ExpressionOut = new ASTExpression(
                            TheOperator,
                            Context.Scanner.GetCurrentLineNumber());
                        return CompileErrors.eCompileNoError;
                    }
            }
        }




        /*   85:   <funccall>         ::= ( <actualstart> ) */
        /* FIRST SET: */
        /*  <funccall>         : {(} */
        /* FOLLOW SET: */
        /*  <funccall>         : {and, or, xor, div, mod, SHR, SHL, then, else, elseif, */
        /*       while, do, to, ), CLOSEBRACKET, , , :=, ;, *, /, +, -, EQ, */
        /*       NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, <expr3prime>, */
        /*       <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, <mult_oper>, */
        /*       <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseFuncCall(
            out ASTExpression ExpressionOut,
            ASTExpression FunctionGenerator,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTExpressionList ListOfParameters;
            CompileErrors Error;
            ASTFunctionCall TheFunctionCall;

            ExpressionOut = null;

            /* swallow open parenthesis */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenParen)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedOpenParen;
            }

            /* parse the argument list */
            Error = ParseActualStart(
                out ListOfParameters,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* swallow close parenthesis */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseParen)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedCommaOrCloseParen;
            }

            SymbolRec FunctionNameSymbol = FunctionGenerator.InnerOperand.Symbol;

            /* see if we should infer a function prototype from here */
            bool undeclared = false;
            if ((undeclared = (FunctionGenerator.Kind == ExprKind.eExprOperand)
                && (FunctionGenerator.InnerOperand.Kind == ASTOperandKind.eASTOperandSymbol)
                && (FunctionGenerator.InnerOperand.Symbol.Kind == SymbolKind.Undefined)))
            {
                // Create a function entry symbol - arguments and return type do not matter at this time because of whole-program
                // compilation: type checking is deferred until all functions are parsed and signatures known.
                FunctionNameSymbol.SymbolBecomeFunction(
                    null/*FormalArgumentList*/,
                    DataTypes.eInvalidDataType/*ReturnType*/);
            }

            // record function in global function table for later fixup (whole program compilation case)
            int start, count;
            if (ASTFunctionCall.TryFindBuiltInFunctionRange(FunctionNameSymbol.SymbolName, out start, out count))
            {
                // do not add built-in functions to the global function table
            }
            else
            {
                List<ParserContext.FunctionSymbolRefInfo> symbols;
                if (!Context.FunctionSymbolList.TryGetValue(FunctionNameSymbol.SymbolName, out symbols))
                {
                    symbols = new List<ParserContext.FunctionSymbolRefInfo>();
                    Context.FunctionSymbolList.Add(FunctionNameSymbol.SymbolName, symbols);
                }
                symbols.Add(new ParserContext.FunctionSymbolRefInfo(FunctionNameSymbol, FunctionGenerator.LineNumber));
            }

            TheFunctionCall = new ASTFunctionCall(
                new ASTFunctionArgumentsExpressionList(
                    ListOfParameters,
                    Context.Scanner.GetCurrentLineNumber()),
                FunctionGenerator,
                Context.Scanner.GetCurrentLineNumber());

            ExpressionOut = new ASTExpression(
                TheFunctionCall,
                Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*   84:   <arraysubscript>   ::= OPENBRACKET <exprlist> CLOSEBRACKET */
        /* FIRST SET: */
        /*  <arraysubscript>   : {OPENBRACKET} */
        /* FOLLOW SET: */
        /*  <arraysubscript>   : {and, or, xor, div, mod, SHR, SHL, then, else, */
        /*       elseif, while, do, to, ), CLOSEBRACKET, , , :=, ;, *, */
        /*       /, +, -, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, */
        /*       <expr3prime>, <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, */
        /*       <mult_oper>, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseArraySubscript(
            out ASTExpression ExpressionOut,
            ASTExpression ArrayGenerator,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTExpression Subscript;
            CompileErrors Error;
            ASTBinaryOperation ArraySubsOperation;
            ASTExpressionList SubscriptRaw;

            ExpressionOut = null;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenOpenBracket)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedOpenBracket;
            }

            Error = ParseExprList(
                out SubscriptRaw,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            Subscript = new ASTExpression(
                SubscriptRaw,
                Context.Scanner.GetCurrentLineNumber());

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseBracket)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedCloseBracket;
            }

            ArraySubsOperation = new ASTBinaryOperation(
                BinaryOperatorKind.eBinaryArraySubscripting,
                ArrayGenerator,
                Subscript,
                Context.Scanner.GetCurrentLineNumber());
            ExpressionOut = new ASTExpression(
                ArraySubsOperation,
                Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*  124:   <exprlist>         ::= <exprlistelem> <exprlisttail> */
        /* FIRST SET: */
        /*  <exprlist>         : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, var, not, sin, */
        /*       cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, */
        /*       length, if, while, do, resize, error, true, false, */
        /*       set, (, -, <expr>, <expr2>, <expr3>, <expr4>, <expr5>, */
        /*       <expr6>, <unary_oper>, <expr7>, <expr8>, <whileloop>} */
        /* FOLLOW SET: */
        /*  <exprlist>         : {), CLOSEBRACKET, EOF} */
        public static CompileErrors ParseExprList(
            out ASTExpressionList ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpression FirstExpression;
            TokenRec<KeywordsType> Token;
            ASTExpressionList RestOfList;

            ExpressionOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            if ((Token.GetTokenType() == TokenTypes.eTokenCloseParen)
                || (Token.GetTokenType() == TokenTypes.eTokenCloseBracket)
                || (Token.GetTokenType() == TokenTypes.eTokenEndOfInput))
            {
                Context.Scanner.UngetToken(Token);
                ExpressionOut = null; /* empty list */
                return CompileErrors.eCompileNoError;
            }

            /* get first part of list */
            Context.Scanner.UngetToken(Token);
            Error = ParseExprListElem(
                out FirstExpression,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* get the rest of the list */
            Error = ParseExprListTail(
                out RestOfList,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            ExpressionOut = new ASTExpressionList(
                FirstExpression,
                RestOfList,
                Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*   91:   <exponentiation>   ::= ^ <expr7> */
        /* FIRST SET: */
        /*  <exponentiation>   : {^} */
        /* FOLLOW SET: */
        /*  <exponentiation>   : {and, or, xor, div, mod, SHR, SHL, then, else, */
        /*       elseif, while, do, to, ), CLOSEBRACKET, , , :=, ;, *, */
        /*       /, +, -, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, */
        /*       <expr3prime>, <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, */
        /*       <mult_oper>, <actualtail>, <iftail>, <loopwhile>, <exprlisttail>} */
        private static CompileErrors ParseExponentiation(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            ExpressionOut = null;

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCircumflex)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedRightAssociativeOperator;
            }

            return ParseExpr7(
                out ExpressionOut,
                Context,
                out LineNumberOut);
        }




        /*   86:   <actualstart>      ::= <actuallist> */
        /*   87:                      ::=  */
        /* FIRST SET: */
        /*  <actualstart>      : {<identifier>, <integer>, <single>, <double>, */
        /*       <fixed>, <string>, bool, int, single, double, fixed, var, */
        /*       not, sin, cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, */
        /*       neg, sign, length, if, while, do, resize, error, */
        /*       true, false, set, (, -, <expr>, <expr2>, */
        /*       <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, <expr7>, <expr8>, */
        /*       <actuallist>, <whileloop>} */
        /* FOLLOW SET: */
        /*  <actualstart>      : {)} */
        private static CompileErrors ParseActualStart(
            out ASTExpressionList ParamListOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            /* handle the nullification */
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                ParamListOut = null; /* empty list is null */
                return CompileErrors.eCompileNoError;
            }

            Context.Scanner.UngetToken(Token);
            return ParseActualList(
                out ParamListOut,
                Context,
                out LineNumberOut);
        }




        /*   88:   <actuallist>       ::= <expr> <actualtail> */
        /* FIRST SET: */
        /*  <actuallist>       : {<identifier>, <integer>, <single>, <double>, */
        /*       <fixed>, <string>, bool, int, single, double, fixed, var, */
        /*       not, sin, cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, */
        /*       neg, sign, length, if, while, do, resize, error, */
        /*       true, false, set, (, -, <expr>, <expr2>, */
        /*       <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, <expr7>, <expr8>, */
        /*       <whileloop>} */
        /* FOLLOW SET: */
        /*  <actuallist>       : {)} */
        private static CompileErrors ParseActualList(
            out ASTExpressionList ParamListOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            ASTExpression FirstExpression;
            CompileErrors Error;
            ASTExpressionList RestOfList;

            ParamListOut = null;

            Error = ParseExpr(
                out FirstExpression,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            Error = ParseActualTail(
                out RestOfList,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            ParamListOut = new ASTExpressionList(
                FirstExpression,
                RestOfList,
                Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*   89:   <actualtail>       ::= , <actuallist> */
        /*   90:                      ::=  */
        /* FIRST SET: */
        /*  <actualtail>       : {, } */
        /* FOLLOW SET: */
        /*  <actualtail>       : {)} */
        private static CompileErrors ParseActualTail(
            out ASTExpressionList ParamListOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            ParamListOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenComma)
                && (Token.GetTokenType() != TokenTypes.eTokenCloseParen))
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedCommaOrCloseParen;
            }

            /* handle nullification */
            if (Token.GetTokenType() == TokenTypes.eTokenCloseParen)
            {
                Context.Scanner.UngetToken(Token);
                ParamListOut = null; /* empty list */
                return CompileErrors.eCompileNoError;
            }

            return ParseActualList(
                out ParamListOut,
                Context,
                out LineNumberOut);
        }




        /*  125:   <exprlisttail>     ::= ; <exprlist> */
        /*  126:                      ::=  */
        /* FIRST SET: */
        /*  <exprlisttail>     : {;} */
        /* FOLLOW SET: */
        /*  <exprlisttail>     : {), CLOSEBRACKET, EOF} */
        private static CompileErrors ParseExprListTail(
            out ASTExpressionList ListOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            ListOut = null;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            /* find out if we should continue or not */
            if ((Token.GetTokenType() == TokenTypes.eTokenCloseParen)
                || (Token.GetTokenType() == TokenTypes.eTokenCloseBracket)
                || (Token.GetTokenType() == TokenTypes.eTokenEndOfInput))
            {
                Context.Scanner.UngetToken(Token);
                ListOut = null; /* empty list */
                return CompileErrors.eCompileNoError;
            }

            if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedSemicolon;
            }

            return ParseExprList(
                out ListOut,
                Context,
                out LineNumberOut);
        }




        /*  124:   <exprlistelem>     ::= <expr> */
        /*  125:                      ::= var <identifier> : <type> <vartail> */
        /* FIRST SET: */
        /*  <exprlistelem>     : {<identifier>, <integer>, <single>, <double>, */
        /*       <fixed>, <string>, bool, int, single, double, fixed, */
        /*       var, not, sin, cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, */
        /*       abs, neg, sign, length, if, while, do, resize, error, */
        /*       true, false, set, (, -, <expr>, */
        /*       <expr2>, <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, <expr7>, */
        /*       <expr8>, <whileloop>} */
        /* FOLLOW SET: */
        /*  <exprlistelem>     : {), CLOSEBRACKET, ;, <exprlisttail>} */
        private static CompileErrors ParseExprListElem(
            out ASTExpression ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            ExpressionOut = null;

            /* lookahead to see what to do */
            Token = Context.Scanner.GetNextToken();

            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == KeywordsType.eExprKwrdVar))
            {
                TokenRec<KeywordsType> VariableName;
                DataTypes ReturnType;
                CompileErrors Error;
                int LineNumberForFirstToken;

                /* get the identifier */
                VariableName = Context.Scanner.GetNextToken();
                if (VariableName.GetTokenType() != TokenTypes.eTokenIdentifier)
                {
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedIdentifier;
                }
                LineNumberForFirstToken = Context.Scanner.GetCurrentLineNumber();

                /* get the colon */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenColon)
                {
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedColon;
                }

                /* get the variable's type */
                Error = ParseType(
                    out ReturnType,
                    Context,
                    out LineNumberOut);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                /* finish up the declaration of the variable */
                return ParseVarTail(
                    out ExpressionOut,
                    VariableName,
                    LineNumberForFirstToken,
                    ReturnType,
                    Context,
                    out LineNumberOut);
            }
            else
            {
                /* do the other thing */
                Context.Scanner.UngetToken(Token);
                return ParseExpr(
                    out ExpressionOut,
                    Context,
                    out LineNumberOut);
            }
        }
    }
}
