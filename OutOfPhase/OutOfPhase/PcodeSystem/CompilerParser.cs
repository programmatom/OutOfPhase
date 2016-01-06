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

        /* parse a top-level form, which is either a prototype or a function.  prototypes */
        /* are entered into the symbol table and return null in *FunctionBodyOut but returns */
        /*CompileErrors. eCompileNoError. */
        /*   1:   <form>             ::= <function> ; */
        /*   2:                      ::= <prototype> ; */
        /* FIRST SET: */
        /* <form>             : {func, proto, <function>, <prototype>} */
        /* FOLLOW SET: */
        /* <form>             : {$$$} */
        public static CompileErrors ParseForm(
            out SymbolRec FunctionSymbolTableEntryOut,
            out ASTExpressionRec FunctionBodyOut,
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
            }

#if true // TODO: remove 'proto'
            /* do lookahead on "proto" */
            else if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == KeywordsType.eExprKwrdProto))
            {
                /* push token back */
                Context.Scanner.UngetToken(Token);

                /* parse prototype */
                Error = ParsePrototype(
                    out FunctionSymbolTableEntryOut,
                    Context,
                    out LineNumberOut);
                FunctionBodyOut = null; /* no code body for a prototype */
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
            }
#endif

            /* otherwise, it's an error */
            else
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedFuncOrProto;
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
            out ASTExpressionRec FunctionBodyOut,
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
            switch (Context.SymbolTable.AddSymbolToTable(FunctionSymbolTableEntryOut))
            {
                case AddSymbolType.eAddSymbolNoErr:
                    break;
                case AddSymbolType.eAddSymbolAlreadyExists:
                    LineNumberOut = LineNumberOfIdentifier;
                    return CompileErrors.eCompileMultiplyDefinedIdentifier;
                default:
                    // bad value from AddSymbolToTable
                    Debug.Assert(false);
                    throw new InvalidOperationException();
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




#if true // TODO: remove 'proto'
        /* this parses a prototype of a function and returns a symbol table entry in */
        /* the *PrototypeSymbolTableEntryOut place. */
        /*  21:   <prototype>        ::= proto <identifier> ( <formalparamstart> ) : */
        /*      <type> */
        /* FIRST SET: */
        /* <prototype>        : {proto} */
        /* FOLLOW SET: */
        /*  <prototype>        : {then, else, elseif, while, until, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParsePrototype(
            out SymbolRec PrototypeSymbolTableEntryOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            TokenRec<KeywordsType> FunctionName;
            int LineNumberOfIdentifier;
            CompileErrors Error;
            SymbolListRec FormalArgumentList;
            DataTypes ReturnType;

            PrototypeSymbolTableEntryOut = null;
            LineNumberOut = -1;

            /* swallow "proto" */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdProto))
            {
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
            PrototypeSymbolTableEntryOut = new SymbolRec(FunctionName.GetTokenIdentifierString());
            switch (Context.SymbolTable.AddSymbolToTable(PrototypeSymbolTableEntryOut))
            {
                case AddSymbolType.eAddSymbolNoErr:
                    break;
                case AddSymbolType.eAddSymbolAlreadyExists:
                    LineNumberOut = LineNumberOfIdentifier;
                    return CompileErrors.eCompileMultiplyDefinedIdentifier;
                default:
                    // bad value from AddSymbolToTable
                    Debug.Assert(false);
                    throw new InvalidOperationException();
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
            PrototypeSymbolTableEntryOut.SymbolBecomeFunction(
                FormalArgumentList,
                ReturnType);

            /* pop lexical level */
            Context.SymbolTable.DecrementSymbolTableLevel();

            return CompileErrors.eCompileNoError;
        }
#endif




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
        /*       <string>, bool, int, single, double, fixed, proto, var, not, sin, cos, */
        /*       tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, if, */
        /*       then, else, elseif, while, until, do, resize, to, error, true, */
        /*       false, set, (, ), CLOSEBRACKET, , , :=, ;, -, EQ, <prototype>, <expr>, */
        /*       <formalargtail>, <vartail>, <expr2>, <expr3>, <expr4>, <expr5>, <expr6>, */
        /*       <unary_oper>, <expr7>, <expr8>, <actualtail>, <iftail>, <whileloop>, */
        /*       <loopwhileuntil>, <untilloop>, <exprlisttail>} */
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
                case KeywordsType.eExprKwrdFixed:
                    TypeOut = DataTypes.eFloat;
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
                case KeywordsType.eExprKwrdFixedarray:
                    TypeOut = DataTypes.eArrayOfFloat;
                    break;
            }

            return CompileErrors.eCompileNoError;
        }




        /*   26:   <expr>             ::= <expr2> */
        /*  109:   <expr>             ::= if <ifrest> */
        /*  114:   <expr>             ::= <whileloop> */
        /*  115:                      ::= do <expr> <loopwhileuntil> */
        /*  116:                      ::= <untilloop> */
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
        /*       <string>, bool, int, single, double, fixed, proto, var, not, sin, cos, */
        /*       tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, if, */
        /*       while, until, do, resize, error, true, false, set, (, -, */
        /*       <prototype>, <expr2>, <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, */
        /*       <expr7>, <expr8>, <whileloop>, <untilloop>} */
        /* FOLLOW SET: */
        /*  <expr>             : {then, else, elseif, while, until, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        public static CompileErrors ParseExpr(
            out ASTExpressionRec ExpressionOut,
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

                        /*  115:                      ::= do <expr> <loopwhileuntil> */
                        case KeywordsType.eExprKwrdDo:
                            {
                                ASTExpressionRec BodyExpression;
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
                                return ParseLoopWhileUntil(
                                    out ExpressionOut,
                                    BodyExpression,
                                    Context,
                                    out LineNumberOut,
                                    LineNumberForFirstToken);
                            }

                        /*  116:                      ::= <untilloop> */
                        /* FIRST SET */
                        /*  <untilloop>        : {until} */
                        case KeywordsType.eExprKwrdUntil:
                            Context.Scanner.UngetToken(Token);
                            return ParseUntilLoop(
                                out ExpressionOut,
                                Context,
                                out LineNumberOut);

                        /*  121:   <expr>             ::= set <expr> := <expr> */
                        case KeywordsType.eExprKwrdSet:
                            {
                                ASTExpressionRec LValue;
                                ASTExpressionRec RValue;
                                CompileErrors Error;
                                ASTAssignRec TotalAssignment;

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

                                TotalAssignment = NewAssignment(
                                    LValue,
                                    RValue,
                                    LineNumberForFirstToken);

                                ExpressionOut = NewExprAssignment(
                                    TotalAssignment,
                                    LineNumberForFirstToken);

                                return CompileErrors.eCompileNoError;
                            }

                        /*  125:   <expr>             ::= resize <expr> to <expr> */
                        case KeywordsType.eExprKwrdResize:
                            {
                                ASTExpressionRec ArrayGenerator;
                                ASTExpressionRec NewSizeExpression;
                                CompileErrors Error;
                                ASTBinaryOpRec BinaryOperator;

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

                                BinaryOperator = NewBinaryOperator(
                                    BinaryOpType.eBinaryResizeArray,
                                    ArrayGenerator,
                                    NewSizeExpression,
                                    LineNumberForFirstToken);

                                ExpressionOut = NewExprBinaryOperator(
                                    BinaryOperator,
                                    LineNumberForFirstToken);

                                return CompileErrors.eCompileNoError;
                            }

                        /*  126:                      ::= error <string> [resumable <expr>] */
                        case KeywordsType.eExprKwrdError:
                            {
                                TokenRec<KeywordsType> MessageString;
                                ASTExpressionRec ResumableCondition;
                                CompileErrors Error;
                                ASTErrorFormRec ErrorForm;

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
                                    ResumableCondition = NewExprOperand(
                                        NewBooleanLiteral(
                                            false,
                                            Context.Scanner.GetCurrentLineNumber()),
                                        Context.Scanner.GetCurrentLineNumber());
                                }

                                ErrorForm = NewErrorForm(
                                    ResumableCondition,
                                    MessageString.GetTokenStringValue(),
                                    LineNumberForFirstToken);

                                ExpressionOut = NewExprErrorForm(
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
                                ASTWaveGetterRec WaveGetterThang;
                                WaveGetterOp Op;

                                switch (TokenKeywordTag)
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case KeywordsType.eExprKwrdGetsampleleft:
                                        Op = WaveGetterOp.eWaveGetterSampleLeft;
                                        break;
                                    case KeywordsType.eExprKwrdGetsampleright:
                                        Op = WaveGetterOp.eWaveGetterSampleRight;
                                        break;
                                    case KeywordsType.eExprKwrdGetsample:
                                        Op = WaveGetterOp.eWaveGetterSampleMono;
                                        break;
                                    case KeywordsType.eExprKwrdGetwavenumframes:
                                        Op = WaveGetterOp.eWaveGetterWaveFrames;
                                        break;
                                    case KeywordsType.eExprKwrdGetwavenumtables:
                                        Op = WaveGetterOp.eWaveGetterWaveTables;
                                        break;
                                    case KeywordsType.eExprKwrdGetwavedata:
                                        Op = WaveGetterOp.eWaveGetterWaveArray;
                                        break;
                                }

                                Token = Context.Scanner.GetNextToken();
                                if (Token.GetTokenType() != TokenTypes.eTokenString)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileExpectedStringLiteral;
                                }

                                WaveGetterThang = NewWaveGetter(
                                    Token.GetTokenStringValue(),
                                    Op,
                                    Context.Scanner.GetCurrentLineNumber());

                                ExpressionOut = NewExprWaveGetter(
                                    WaveGetterThang,
                                    Context.Scanner.GetCurrentLineNumber());

                                return CompileErrors.eCompileNoError;
                            }

                        /*  XXX:                      ::= print (<string> | <expr>) */
                        case KeywordsType.eExprKwrdPrint:
                            Token = Context.Scanner.GetNextToken();
                            if (Token.GetTokenType() == TokenTypes.eTokenString)
                            {
                                ASTPrintStringRec PrintString;

                                /* print string literal part */
                                PrintString = NewPrintString(
                                    Token.GetTokenStringValue(),
                                    LineNumberForFirstToken);
                                ExpressionOut = NewExprPrintString(
                                    PrintString,
                                    LineNumberForFirstToken);
                            }
                            else
                            {
                                ASTPrintExprRec PrintExpr;
                                ASTExpressionRec Operand;
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
                                PrintExpr = NewPrintExpr(
                                    Operand,
                                    LineNumberForFirstToken);
                                ExpressionOut = NewExprPrintExpr(
                                    PrintExpr,
                                    LineNumberForFirstToken);
                            }
                            return CompileErrors.eCompileNoError;

                        /*  XXX:                      ::= butterworthbandpass ( <formalparamlist> ) */
                        /*  XXX:                      ::= firstorderlowpass ( <actualstart> )  */
                        /*  XXX:                      ::= vecsqr ( <actualstart> )  */
                        /*  XXX:                      ::= vecsqrt ( <actualstart> )  */
                        case KeywordsType.eExprKwrdButterworthbandpass:
                        case KeywordsType.eExprKwrdFirstorderlowpass:
                        case KeywordsType.eExprKwrdVecsqr:
                        case KeywordsType.eExprKwrdVecsqrt:
                            {
                                ASTExprListRec ListOfParameters;
                                ASTExpressionRec ArrayExpr;
                                ASTExpressionRec StartIndexExpr;
                                ASTExpressionRec EndIndexExpr;
                                ASTExpressionRec SamplingRateExpr = null;
                                ASTExpressionRec CutoffExpr = null;
                                ASTExpressionRec BandwidthExpr = null;
                                ASTFilterRec Filter;
                                ASTExpressionRec TotalFilter;
                                CompileErrors Error;

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

                                /* build arguments for filter AST */

                                /* arg 1 = array */
                                if (ListOfParameters == null)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileWrongNumberOfArgsToFunction;
                                }
                                ArrayExpr = ExprListGetFirstExpr(ListOfParameters);
                                ListOfParameters = ExprListGetRestList(ListOfParameters);

                                /* arg 2 = start index */
                                if (ListOfParameters == null)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileWrongNumberOfArgsToFunction;
                                }
                                StartIndexExpr = ExprListGetFirstExpr(ListOfParameters);
                                ListOfParameters = ExprListGetRestList(ListOfParameters);

                                /* arg 3 = end index */
                                if (ListOfParameters == null)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileWrongNumberOfArgsToFunction;
                                }
                                EndIndexExpr = ExprListGetFirstExpr(ListOfParameters);
                                ListOfParameters = ExprListGetRestList(ListOfParameters);

                                /* arg 4 = sampling rate */
                                if ((TokenKeywordTag == KeywordsType.eExprKwrdButterworthbandpass)
                                    || (TokenKeywordTag == KeywordsType.eExprKwrdFirstorderlowpass))
                                {
                                    if (ListOfParameters == null)
                                    {
                                        LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                        return CompileErrors.eCompileWrongNumberOfArgsToFunction;
                                    }
                                    SamplingRateExpr = ExprListGetFirstExpr(ListOfParameters);
                                    ListOfParameters = ExprListGetRestList(ListOfParameters);
                                }

                                /* arg 5 = cutoff */
                                if ((TokenKeywordTag == KeywordsType.eExprKwrdButterworthbandpass)
                                    || (TokenKeywordTag == KeywordsType.eExprKwrdFirstorderlowpass))
                                {
                                    if (ListOfParameters == null)
                                    {
                                        LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                        return CompileErrors.eCompileWrongNumberOfArgsToFunction;
                                    }
                                    CutoffExpr = ExprListGetFirstExpr(ListOfParameters);
                                    ListOfParameters = ExprListGetRestList(ListOfParameters);
                                }

                                /* arg 6 = bandwidth */
                                if (TokenKeywordTag == KeywordsType.eExprKwrdButterworthbandpass)
                                {
                                    if (ListOfParameters == null)
                                    {
                                        LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                        return CompileErrors.eCompileWrongNumberOfArgsToFunction;
                                    }
                                    BandwidthExpr = ExprListGetFirstExpr(ListOfParameters);
                                    ListOfParameters = ExprListGetRestList(ListOfParameters);
                                }

                                if (ListOfParameters != null)
                                {
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileWrongNumberOfArgsToFunction;
                                }

                                switch (TokenKeywordTag)
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();

                                    case KeywordsType.eExprKwrdButterworthbandpass:
                                        Filter = NewASTFilterButterworthBandpass(
                                            ArrayExpr,
                                            StartIndexExpr,
                                            EndIndexExpr,
                                            SamplingRateExpr,
                                            CutoffExpr,
                                            BandwidthExpr,
                                            Context.Scanner.GetCurrentLineNumber());
                                        break;

                                    case KeywordsType.eExprKwrdFirstorderlowpass:
                                        Filter = NewASTFilterFirstOrderLowpass(
                                            ArrayExpr,
                                            StartIndexExpr,
                                            EndIndexExpr,
                                            SamplingRateExpr,
                                            CutoffExpr,
                                            Context.Scanner.GetCurrentLineNumber());
                                        break;

                                    case KeywordsType.eExprKwrdVecsqr:
                                        Filter = NewASTFilterSquare(
                                            ArrayExpr,
                                            StartIndexExpr,
                                            EndIndexExpr,
                                            Context.Scanner.GetCurrentLineNumber());
                                        break;

                                    case KeywordsType.eExprKwrdVecsqrt:
                                        Filter = NewASTFilterSquareRoot(
                                            ArrayExpr,
                                            StartIndexExpr,
                                            EndIndexExpr,
                                            Context.Scanner.GetCurrentLineNumber());
                                        break;
                                }

                                TotalFilter = NewExprFilterExpr(Filter, Context.Scanner.GetCurrentLineNumber());

                                ExpressionOut = TotalFilter;
                            }
                            return CompileErrors.eCompileNoError;

                        /*  XXX:                      ::= loadsample <string>, <string> */
                        /*  XXX:                      ::= loadsampleleft <string>, <string> */
                        /*  XXX:                      ::= loadsampleright <string>, <string> */
                        case KeywordsType.eExprKwrdLoadsampleleft:
                        case KeywordsType.eExprKwrdLoadsampleright:
                        case KeywordsType.eExprKwrdLoadsample:
                            {
                                ASTSampleLoaderRec SampleLoaderThang;
                                TokenRec<KeywordsType> Token2;
                                SampleLoaderOp Op;

                                switch (TokenKeywordTag)
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case KeywordsType.eExprKwrdLoadsampleleft:
                                        Op = SampleLoaderOp.eSampleLoaderSampleLeft;
                                        break;
                                    case KeywordsType.eExprKwrdLoadsampleright:
                                        Op = SampleLoaderOp.eSampleLoaderSampleRight;
                                        break;
                                    case KeywordsType.eExprKwrdLoadsample:
                                        Op = SampleLoaderOp.eSampleLoaderSampleMono;
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

                                SampleLoaderThang = NewSampleLoader(
                                    Token.GetTokenStringValue(), /* filetype */
                                    Token2.GetTokenStringValue(), /* filename */
                                    Op,
                                    Context.Scanner.GetCurrentLineNumber());

                                ExpressionOut = NewExprSampleLoader(
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
                                ASTExpressionRec InitialValue;
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
                                    Context.SymbolTable.AddSymbolToTable(LoopVariable);

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

                                ASTExpressionRec WhileExpression;
                                Error = ParseExpr(
                                    out WhileExpression,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }

                                ASTAssignRec IncrementExpression;
                                Token = Context.Scanner.GetNextToken();
                                if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                                    && (Token.GetTokenKeywordTag() == KeywordsType.eExprKwrdDo))
                                {
                                    // implicit form

                                    Context.Scanner.UngetToken(Token);

                                    IncrementExpression = NewAssignment(
                                        NewExprOperand(NewSymbolReference(LoopVariable, LoopVariableLineNumber), LoopVariableLineNumber),
                                        NewExprBinaryOperator(
                                            NewBinaryOperator(
                                                BinaryOpType.eBinaryPlus,
                                                NewExprOperand(NewSymbolReference(LoopVariable, LoopVariableLineNumber), LoopVariableLineNumber),
                                                NewExprOperand(
                                                    LoopVariable.GetSymbolVariableDataType() != DataTypes.eFloat
                                                        ? NewIntegerLiteral(1, Context.Scanner.GetCurrentLineNumber())
                                                        : NewSingleLiteral(1, Context.Scanner.GetCurrentLineNumber()),
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

                                        ASTExpressionRec LValue;
                                        ASTExpressionRec RValue;

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

                                        IncrementExpression = NewAssignment(
                                            LValue,
                                            RValue,
                                            LineNumberForFirstToken);
                                    }
                                    else
                                    {
                                        Context.Scanner.UngetToken(Token);

                                        // special form: step <expression>

                                        ASTExpressionRec RValue;
                                        Error = ParseExpr(
                                            out RValue,
                                            Context,
                                            out LineNumberOut);
                                        if (Error != CompileErrors.eCompileNoError)
                                        {
                                            return Error;
                                        }

                                        IncrementExpression = NewAssignment(
                                            NewExprOperand(NewSymbolReference(LoopVariable, LoopVariableLineNumber), LoopVariableLineNumber),
                                            NewExprBinaryOperator(
                                                NewBinaryOperator(
                                                    BinaryOpType.eBinaryPlus,
                                                    NewExprOperand(NewSymbolReference(LoopVariable, LoopVariableLineNumber), LoopVariableLineNumber),
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

                                ASTExpressionRec Body;
                                Error = ParseExpr(
                                    out Body,
                                    Context,
                                    out LineNumberOut);
                                if (Error != CompileErrors.eCompileNoError)
                                {
                                    return Error;
                                }

                                Context.SymbolTable.DecrementSymbolTableLevel();

                                ASTForLoop ForLoop = NewForLoop(
                                    LoopVariable,
                                    InitialValue,
                                    WhileExpression,
                                    IncrementExpression,
                                    Body,
                                    LineNumberForFirstToken);

                                ExpressionOut = NewExprForLoop(
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

            FormalArgListOut = SymbolListRec.SymbolListCons(
                FormalArgOut,
                ListTail);

            return CompileErrors.eCompileNoError;
        }




        /*  117:   <whileloop>        ::= while <expr> do <expr> */
        /* FIRST SET: */
        /*  <whileloop>        : {while} */
        /* FOLLOW SET: */
        /*  <whileloop>        : {then, else, elseif, while, until, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseWhileLoop(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTExpressionRec ConditionalExpr;
            ASTExpressionRec BodyExpr;
            CompileErrors Error;
            ASTLoopRec WhileLoopThing;
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

            WhileLoopThing = NewLoop(
                LoopTypes.eLoopWhileDo,
                ConditionalExpr,
                BodyExpr,
                LineNumberOfWholeForm);

            ExpressionOut = NewExprLoop(
                WhileLoopThing,
                LineNumberOfWholeForm);

            return CompileErrors.eCompileNoError;
        }




        /*  118:   <untilloop>        ::= until <expr> do <expr> */
        /* FIRST SET: */
        /*  <untilloop>        : {until} */
        /* FOLLOW SET: */
        /*  <untilloop>        : {then, else, elseif, while, until, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseUntilLoop(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTExpressionRec ConditionalExpr;
            ASTExpressionRec BodyExpr;
            CompileErrors Error;
            ASTLoopRec UntilLoopThing;
            int LineNumberOfWholeForm;

            ExpressionOut = null;

            LineNumberOfWholeForm = Context.Scanner.GetCurrentLineNumber();

            /* munch until */
            Token = Context.Scanner.GetNextToken();
            if ((Token.GetTokenType() != TokenTypes.eTokenKeyword)
                || (Token.GetTokenKeywordTag() != KeywordsType.eExprKwrdUntil))
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedUntil;
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

            UntilLoopThing = NewLoop(
                LoopTypes.eLoopUntilDo,
                ConditionalExpr,
                BodyExpr,
                LineNumberOfWholeForm);

            ExpressionOut = NewExprLoop(
                UntilLoopThing,
                LineNumberOfWholeForm);

            return CompileErrors.eCompileNoError;
        }




        /*   24:   <vartail>          ::= EQ <expr> */
        /*   25:                      ::= ( <expr> ) */
        /* FIRST SET: */
        /*  <vartail>          : {(, EQ} */
        /* FOLLOW SET: */
        /*  <vartail>          : {then, else, elseif, while, until, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseVarTail(
            out ASTExpressionRec ExpressionOut,
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
                        ASTExpressionRec ArraySizeExpression;
                        CompileErrors Error;
                        ASTArrayDeclRec ArrayConstructor;

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
                        ArrayConstructor = NewArrayConstruction(
                            SymbolTableEntry,
                            ArraySizeExpression,
                            VariableDeclLine);

                        /* build AST node */
                        ExpressionOut = NewExprArrayDecl(
                            ArrayConstructor,
                            VariableDeclLine);
                    }
                    break;

                /* variable construction */
                case TokenTypes.eTokenEqual:
                    {
                        ASTExpressionRec Initializer;
                        CompileErrors Error;
                        ASTVarDeclRec VariableConstructor;

                        Error = ParseExpr(
                            out Initializer,
                            Context,
                            out LineNumberOut);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }

                        /* build variable thing */
                        VariableConstructor = NewVariableDeclaration(
                            SymbolTableEntry,
                            Initializer,
                            VariableDeclLine);

                        /* encapsulate */
                        ExpressionOut = NewExprVariableDeclaration(
                            VariableConstructor,
                            VariableDeclLine);
                    }
                    break;
            }

            /* add the identifier to the symbol table */
            switch (Context.SymbolTable.AddSymbolToTable(SymbolTableEntry))
            {
                case AddSymbolType.eAddSymbolNoErr:
                    break;
                case AddSymbolType.eAddSymbolAlreadyExists:
                    LineNumberOut = VariableDeclLine;
                    return CompileErrors.eCompileMultiplyDefinedIdentifier;
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
            }

            return CompileErrors.eCompileNoError;
        }




        /*  110:   <ifrest>           ::= <expr> then <expr> <iftail> */
        /* FIRST SET: */
        /*  <ifrest>           : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, proto, var, not, sin, */
        /*       cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, length, */
        /*       if, while, until, do, resize, error, true, false, set, (, -, */
        /*       <prototype>, <expr>, <expr2>, <expr3>, <expr4>, <expr5>, <expr6>, */
        /*       <unary_oper>, <expr7>, <expr8>, <whileloop>, <untilloop>} */
        /* FOLLOW SET: */
        /*  <ifrest>           : {then, else, elseif, while, until, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseIfRest(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            CompileErrors Error;
            ASTExpressionRec Predicate;
            ASTExpressionRec Consequent;

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




        /*  119:   <loopwhileuntil>   ::= while <expr> */
        /*  120:                      ::= until <expr> */
        /* FIRST SET: */
        /*  <loopwhileuntil>   : {while, until} */
        /* FOLLOW SET: */
        /*  <loopwhileuntil>   : {then, else, elseif, while, until, do, to, */
        /*       ), CLOSEBRACKET, , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, */
        /*       <exprlisttail>} */
        private static CompileErrors ParseLoopWhileUntil(
            out ASTExpressionRec ExpressionOut,
            ASTExpressionRec LoopBodyExpression,
            ParserContext Context,
            out int LineNumberOut,
            int LineNumberOfLoop)
        {
            TokenRec<KeywordsType> Token;
            LoopTypes LoopKind;
            ASTExpressionRec ConditionalExpression;
            CompileErrors Error;
            ASTLoopRec LoopThang;

            ExpressionOut = null;

            /* see what there is to do */
            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenKeyword)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedWhileOrUntil;
            }

            switch (Token.GetTokenKeywordTag())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedWhileOrUntil;

                case KeywordsType.eExprKwrdWhile:
                    LoopKind = LoopTypes.eLoopDoWhile;
                    break;

                case KeywordsType.eExprKwrdUntil:
                    LoopKind = LoopTypes.eLoopDoUntil;
                    break;
            }

            Error = ParseExpr(
                out ConditionalExpression,
                Context,
                out LineNumberOut);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            LoopThang = NewLoop(
                LoopKind,
                ConditionalExpression,
                LoopBodyExpression,
                LineNumberOfLoop);

            ExpressionOut = NewExprLoop(
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
        /*  <expr2>            : {then, else, elseif, while, until, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr2(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpressionRec LeftHandSide;

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

            switch (Context.SymbolTable.AddSymbolToTable(FormalArgOut))
            {
                case AddSymbolType.eAddSymbolNoErr:
                    break;
                case AddSymbolType.eAddSymbolAlreadyExists:
                    LineNumberOut = LineNumberOfIdentifier;
                    return CompileErrors.eCompileMultiplyDefinedIdentifier;
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
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
        /*  <iftail>           : {then, else, elseif, while, until, do, to, ), CLOSEBRACKET, */
        /*       , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        /* note that 'else' and 'elseif' are in both the first and follow set.  this is */
        /* because if-then-else isn't LL(1).  we handle this by binding else to the deepest */
        /* if statement. */
        private static CompileErrors ParseIfTail(
            out ASTExpressionRec ExpressionOut,
            ASTExpressionRec Predicate,
            ASTExpressionRec Consequent,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTCondRec Conditional;
            ASTExpressionRec Alternative;
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
                    Conditional = NewConditional(
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
                            Conditional = NewConditional(
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
                            Conditional = NewConditional(
                                Predicate,
                                Consequent,
                                Alternative,
                                Context.Scanner.GetCurrentLineNumber());
                            break;
                    }
                    break;
            }

            /* finish building expression node */
            ExpressionOut = NewExprConditional(
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
        /*  <expr3>            : {and, or, xor, then, else, elseif, while, until, do, */
        /*       to, ), CLOSEBRACKET, , , :=, ;, <expr2prime>, <conj_oper>, <actualtail>, */
        /*       <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr3(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpressionRec LeftHandSide;

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
        /*  <expr2prime>       : {then, else, elseif, while, until, do, to, ), */
        /*       CLOSEBRACKET, , , :=, ;, <actualtail>, <iftail>, <loopwhileuntil>, */
        /*       <exprlisttail>} */
        private static CompileErrors ParseExpr2Prime(
            out ASTExpressionRec ExpressionOut,
            ASTExpressionRec LeftHandSide,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            BinaryOpType OperatorType;
            CompileErrors Error;
            ASTExpressionRec RightHandSide;
            ASTBinaryOpRec WholeOperator;
            ASTExpressionRec ThisWholeNode;

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
                            WholeOperator = NewBinaryOperator(
                                OperatorType,
                                LeftHandSide,
                                RightHandSide,
                                Context.Scanner.GetCurrentLineNumber());
                            ThisWholeNode = NewExprBinaryOperator(
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
        /*  <expr4>            : {and, or, xor, then, else, elseif, while, until, do, */
        /*       to, ), CLOSEBRACKET, , , :=, ;, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, */
        /*       <conj_oper>, <expr3prime>, <rel_oper>, <actualtail>, <iftail>, */
        /*       <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr4(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpressionRec LeftHandSide;

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
        /*  <expr3prime>       : {and, or, xor, then, else, elseif, while, until, */
        /*       do, to, ), CLOSEBRACKET, , , :=, ;, <expr2prime>, <conj_oper>, */
        /*       <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr3Prime(
            out ASTExpressionRec ExpressionOut,
            ASTExpressionRec LeftHandSide,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            BinaryOpType OperatorType;
            CompileErrors Error;
            ASTExpressionRec RightHandSide;
            ASTBinaryOpRec WholeOperator;
            ASTExpressionRec ThisWholeNode;

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
                    WholeOperator = NewBinaryOperator(
                        OperatorType,
                        LeftHandSide,
                        RightHandSide,
                        Context.Scanner.GetCurrentLineNumber());
                    ThisWholeNode = NewExprBinaryOperator(
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
            out BinaryOpType OperatorOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            OperatorOut = BinaryOpType.eInvalid;
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
                            OperatorOut = BinaryOpType.eBinaryAnd;
                            return CompileErrors.eCompileNoError;

                        case KeywordsType.eExprKwrdOr:
                            OperatorOut = BinaryOpType.eBinaryOr;
                            return CompileErrors.eCompileNoError;

                        case KeywordsType.eExprKwrdXor:
                            OperatorOut = BinaryOpType.eBinaryXor;
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
        /*  <expr5>            : {and, or, xor, then, else, elseif, while, until, do, */
        /*       to, ), CLOSEBRACKET, , , :=, ;, +, -, EQ, NEQ, LT, LTEQ, GR, GREQ, */
        /*       <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, <expr4prime>, */
        /*       <add_oper>, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr5(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpressionRec LeftHandSide;

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
        /*  <expr4prime>       : {and, or, xor, then, else, elseif, while, until, */
        /*       do, to, ), CLOSEBRACKET, , , :=, ;, EQ, NEQ, LT, LTEQ, GR, GREQ, */
        /*       <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, <actualtail>, */
        /*       <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr4Prime(
            out ASTExpressionRec ExpressionOut,
            ASTExpressionRec LeftHandSide,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            BinaryOpType OperatorType;
            CompileErrors Error;
            ASTExpressionRec RightHandSide;
            ASTBinaryOpRec WholeOperator;
            ASTExpressionRec ThisWholeNode;

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
                    WholeOperator = NewBinaryOperator(
                        OperatorType,
                        LeftHandSide,
                        RightHandSide,
                        Context.Scanner.GetCurrentLineNumber());
                    ThisWholeNode = NewExprBinaryOperator(
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
            out BinaryOpType OperatorOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            OperatorOut = BinaryOpType.eInvalid;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedOperatorOrStatement;

                case TokenTypes.eTokenEqual:
                    OperatorOut = BinaryOpType.eBinaryEqual;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenLessGreater:
                    OperatorOut = BinaryOpType.eBinaryNotEqual;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenLess:
                    OperatorOut = BinaryOpType.eBinaryLessThan;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenLessEqual:
                    OperatorOut = BinaryOpType.eBinaryLessThanOrEqual;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenGreater:
                    OperatorOut = BinaryOpType.eBinaryGreaterThan;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenGreaterEqual:
                    OperatorOut = BinaryOpType.eBinaryGreaterThanOrEqual;
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
        /*       while, until, do, to, ), CLOSEBRACKET, , , :=, ;, *, /, +, -, EQ, NEQ, */
        /*       LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, */
        /*       <expr4prime>, <add_oper>, <expr5prime>, <mult_oper>, <actualtail>, */
        /*       <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr6(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            UnaryOpType UnaryOperatorThing;
            ASTExpressionRec UnaryArgument;
            CompileErrors Error;
            ASTUnaryOpRec UnaryOpNode;

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
                    UnaryOperatorThing = UnaryOpType.eUnaryNegation;
                    break;

                case TokenTypes.eTokenKeyword:
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            goto OtherPoint;

                        case KeywordsType.eExprKwrdNot:
                            UnaryOperatorThing = UnaryOpType.eUnaryNot;
                            break;

                        case KeywordsType.eExprKwrdSin:
                            UnaryOperatorThing = UnaryOpType.eUnarySine;
                            break;

                        case KeywordsType.eExprKwrdCos:
                            UnaryOperatorThing = UnaryOpType.eUnaryCosine;
                            break;

                        case KeywordsType.eExprKwrdTan:
                            UnaryOperatorThing = UnaryOpType.eUnaryTangent;
                            break;

                        case KeywordsType.eExprKwrdAsin:
                            UnaryOperatorThing = UnaryOpType.eUnaryArcSine;
                            break;

                        case KeywordsType.eExprKwrdAcos:
                            UnaryOperatorThing = UnaryOpType.eUnaryArcCosine;
                            break;

                        case KeywordsType.eExprKwrdAtan:
                            UnaryOperatorThing = UnaryOpType.eUnaryArcTangent;
                            break;

                        case KeywordsType.eExprKwrdLn:
                            UnaryOperatorThing = UnaryOpType.eUnaryLogarithm;
                            break;

                        case KeywordsType.eExprKwrdExp:
                            UnaryOperatorThing = UnaryOpType.eUnaryExponentiation;
                            break;

                        case KeywordsType.eExprKwrdBool:
                            UnaryOperatorThing = UnaryOpType.eUnaryCastToBoolean;
                            break;

                        case KeywordsType.eExprKwrdInt:
                            UnaryOperatorThing = UnaryOpType.eUnaryCastToInteger;
                            break;

                        case KeywordsType.eExprKwrdSingle:
                        case KeywordsType.eExprKwrdFloat:
                            UnaryOperatorThing = UnaryOpType.eUnaryCastToSingle;
                            break;

                        case KeywordsType.eExprKwrdDouble:
                            UnaryOperatorThing = UnaryOpType.eUnaryCastToDouble;
                            break;

                        case KeywordsType.eExprKwrdFixed:
                            UnaryOperatorThing = UnaryOpType.eUnaryCastToSingle;
                            break;

                        case KeywordsType.eExprKwrdSqr:
                            UnaryOperatorThing = UnaryOpType.eUnarySquare;
                            break;

                        case KeywordsType.eExprKwrdSqrt:
                            UnaryOperatorThing = UnaryOpType.eUnarySquareRoot;
                            break;

                        case KeywordsType.eExprKwrdAbs:
                            UnaryOperatorThing = UnaryOpType.eUnaryAbsoluteValue;
                            break;

                        case KeywordsType.eExprKwrdNeg:
                            UnaryOperatorThing = UnaryOpType.eUnaryTestNegative;
                            break;

                        case KeywordsType.eExprKwrdSign:
                            UnaryOperatorThing = UnaryOpType.eUnaryGetSign;
                            break;

                        case KeywordsType.eExprKwrdLength:
                            UnaryOperatorThing = UnaryOpType.eUnaryGetArrayLength;
                            break;

                        case KeywordsType.eExprKwrdDup:
                            UnaryOperatorThing = UnaryOpType.eUnaryDuplicateArray;
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
            UnaryOpNode = NewUnaryOperator(
                UnaryOperatorThing,
                UnaryArgument,
                Context.Scanner.GetCurrentLineNumber());

            ExpressionOut = NewExprUnaryOperator(
                UnaryOpNode,
                Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*   48:   <expr5prime>       ::= <mult_oper> <expr6> <expr5prime> */
        /*   49:                      ::=  */
        /* FIRST SET: */
        /*  <expr5prime>       : {div, mod, SHR, SHL, *, /, <mult_oper>} */
        /* FOLLOW SET: */
        /*  <expr5prime>       : {and, or, xor, then, else, elseif, while, until, */
        /*       do, to, ), CLOSEBRACKET, , , :=, ;, +, -, EQ, NEQ, LT, LTEQ, GR, */
        /*       GREQ, <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, <expr4prime>, */
        /*       <add_oper>, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr5Prime(
            out ASTExpressionRec ExpressionOut,
            ASTExpressionRec LeftHandSide,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTExpressionRec RightHandSide;
            BinaryOpType OperatorThing;
            CompileErrors Error;
            ASTBinaryOpRec BinaryOperator;
            ASTExpressionRec WholeThingThing;

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
                    BinaryOperator = NewBinaryOperator(
                        OperatorThing,
                        LeftHandSide,
                        RightHandSide,
                        Context.Scanner.GetCurrentLineNumber());
                    WholeThingThing = NewExprBinaryOperator(
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
            out BinaryOpType OperatorOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            OperatorOut = BinaryOpType.eInvalid;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedOperatorOrStatement;

                case TokenTypes.eTokenPlus:
                    OperatorOut = BinaryOpType.eBinaryPlus;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenMinus:
                    OperatorOut = BinaryOpType.eBinaryMinus;
                    return CompileErrors.eCompileNoError;
            }
        }




        /*   79:   <expr7>            ::= <expr8> <expr7prime> */
        /* FIRST SET: */
        /*  <expr7>            : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, true, false, (, <expr8>} */
        /* FOLLOW SET: */
        /*  <expr7>            : {and, or, xor, div, mod, SHR, SHL, then, else, elseif, */
        /*       while, until, do, to, ), CLOSEBRACKET, , , :=, ;, *, /, +, -, EQ, NEQ, */
        /*       LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, <expr3prime>, <rel_oper>, */
        /*       <expr4prime>, <add_oper>, <expr5prime>, <mult_oper>, <actualtail>, */
        /*       <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr7(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            ASTExpressionRec ResultOfExpr8;
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
            out BinaryOpType OperatorOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;

            OperatorOut = BinaryOpType.eInvalid;
            LineNumberOut = -1;

            Token = Context.Scanner.GetNextToken();

            switch (Token.GetTokenType())
            {
                default:
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompileExpectedOperatorOrStatement;

                case TokenTypes.eTokenStar:
                    OperatorOut = BinaryOpType.eBinaryMultiplication;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenSlash:
                    OperatorOut = BinaryOpType.eBinaryImpreciseDivision;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenLeftLeft:
                    OperatorOut = BinaryOpType.eBinaryShiftLeft;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenRightRight:
                    OperatorOut = BinaryOpType.eBinaryShiftRight;
                    return CompileErrors.eCompileNoError;

                case TokenTypes.eTokenKeyword:
                    switch (Token.GetTokenKeywordTag())
                    {
                        default:
                            LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                            return CompileErrors.eCompileExpectedOperatorOrStatement;

                        case KeywordsType.eExprKwrdDiv:
                            OperatorOut = BinaryOpType.eBinaryIntegerDivision;
                            return CompileErrors.eCompileNoError;

                        case KeywordsType.eExprKwrdMod:
                            OperatorOut = BinaryOpType.eBinaryIntegerRemainder;
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
        /*       while, until, do, to, (, ), OPENBRACKET, CLOSEBRACKET, , , :=, ;, *, */
        /*       /, +, -, ^, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, */
        /*       <expr3prime>, <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, */
        /*       <mult_oper>, <expr7prime>, <arraysubscript>, <funccall>, <exponentiation>, */
        /*       <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr8(
            out ASTExpressionRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            ASTOperandRec TheOperand;
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

                        TheSymbolTableEntry = Context.SymbolTable.GetSymbolFromTable(Token.GetTokenIdentifierString());
                        if (TheSymbolTableEntry == null)
                        {
                            /* LineNumberOut = Context.Scanner.GetCurrentLineNumber(); */
                            /* return CompileErrors.eCompileIdentifierNotDeclared; */
                            TheSymbolTableEntry = new SymbolRec(Token.GetTokenIdentifierString());
                            switch (Context.SymbolTable.AddSymbolToTable(TheSymbolTableEntry))
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case AddSymbolType.eAddSymbolNoErr:
                                    break;
                                case AddSymbolType.eAddSymbolAlreadyExists:
                                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                                    return CompileErrors.eCompileMultiplyDefinedIdentifier;
                            }
                        }
                        TheOperand = NewSymbolReference(
                            TheSymbolTableEntry,
                            Context.Scanner.GetCurrentLineNumber());
                    }
                    break;

                /*   93:                      ::= <integer> */
                case TokenTypes.eTokenInteger:
                    TheOperand = NewIntegerLiteral(
                        Token.GetTokenIntegerValue(),
                        Context.Scanner.GetCurrentLineNumber());
                    break;

                /*   94:                      ::= <single> */
                case TokenTypes.eTokenSingle:
                    TheOperand = NewSingleLiteral(
                        Token.GetTokenSingleValue(),
                        Context.Scanner.GetCurrentLineNumber());
                    break;

                /*   95:                      ::= <double> */
                case TokenTypes.eTokenDouble:
                    TheOperand = NewDoubleLiteral(
                        Token.GetTokenDoubleValue(),
                        Context.Scanner.GetCurrentLineNumber());
                    break;

                /*   96:                      ::= <fixed> */
                /* defunct */

                /*   97:                      ::= <string> */
                case TokenTypes.eTokenString:
                    TheOperand = NewStringLiteral(
                        Token.GetTokenStringValue(),
                        Context.Scanner.GetCurrentLineNumber());
                    break;

                /*  108:                      ::= ( <exprlist> ) */
                case TokenTypes.eTokenOpenParen:
                    {
                        CompileErrors Error;
                        ASTExprListRec ListOfExpressions;

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
                        ExpressionOut = NewExprSequence(
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
                            TheOperand = NewBooleanLiteral(
                                true,
                                Context.Scanner.GetCurrentLineNumber());
                            break;

                        /*   99:                      ::= false */
                        case KeywordsType.eExprKwrdFalse:
                            TheOperand = NewBooleanLiteral(
                                false,
                                Context.Scanner.GetCurrentLineNumber());
                            break;

                        /* this was added later. */
                        case KeywordsType.eExprKwrdPi:
                            TheOperand = NewDoubleLiteral(
                                Math.PI,
                                Context.Scanner.GetCurrentLineNumber());
                            break;
                    }
                    break;
            }

            ExpressionOut = NewExprOperand(TheOperand, Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*   80:   <expr7prime>       ::= <arraysubscript> */
        /*   81:                      ::= <funccall> */
        /*   82:                      ::= <exponentiation> */
        /*   83:                      ::=  */
        /*  <expr7prime>       : {(, OPENBRACKET, ^, <arraysubscript>, <funccall>, */
        /*       <exponentiation>} */
        /*  <expr7prime>       : {and, or, xor, div, mod, SHR, SHL, then, else, */
        /*       elseif, while, until, do, to, ), CLOSEBRACKET, , , :=, ;, *, /, */
        /*       +, -, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, */
        /*       <expr3prime>, <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, */
        /*       <mult_oper>, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExpr7Prime(
            out ASTExpressionRec ExpressionOut,
            ASTExpressionRec TheExpr8Thing,
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
                        ASTExpressionRec RightHandSide;
                        ASTBinaryOpRec TheOperator;
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
                        TheOperator = NewBinaryOperator(
                            BinaryOpType.eBinaryExponentiation,
                            TheExpr8Thing,
                            RightHandSide,
                            Context.Scanner.GetCurrentLineNumber());
                        ExpressionOut = NewExprBinaryOperator(
                            TheOperator,
                            Context.Scanner.GetCurrentLineNumber());
                        return CompileErrors.eCompileNoError;
                    }
            }
        }




        /*   85:   <funccall>         ::= ( <actualstart> ) */
        /*   85:   <funccall>         ::= ( <actualstart> ) : <type> */
        /* FIRST SET: */
        /*  <funccall>         : {(} */
        /* FOLLOW SET: */
        /*  <funccall>         : {and, or, xor, div, mod, SHR, SHL, then, else, elseif, */
        /*       while, until, do, to, ), CLOSEBRACKET, , , :=, ;, *, /, +, -, EQ, */
        /*       NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, <expr3prime>, */
        /*       <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, <mult_oper>, */
        /*       <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseFuncCall(
            out ASTExpressionRec ExpressionOut,
            ASTExpressionRec FunctionGenerator,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTExprListRec ListOfParameters;
            CompileErrors Error;
            ASTFuncCallRec TheFunctionCall;

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

            SymbolRec FunctionNameSymbol = GetSymbolFromOperand(GetOperandOutOfExpression(FunctionGenerator));

            // TODO: remove deprecated code:

            /* see if we should infer a function prototype from here */
            bool colon;
            Token = Context.Scanner.GetNextToken();
            colon = Token.GetTokenType() == TokenTypes.eTokenColon;
            Context.Scanner.UngetToken(Token);
            bool undeclared = false;
            if ((undeclared = (WhatKindOfExpressionIsThis(FunctionGenerator) == ExprTypes.eExprOperand)
                && (OperandWhatIsIt(GetOperandOutOfExpression(FunctionGenerator)) == ASTOperandType.eASTOperandSymbol)
                && (GetSymbolFromOperand(GetOperandOutOfExpression(FunctionGenerator)).WhatIsThisSymbol() == SymbolType.eSymbolUndefined))
                || colon/*deprecated - but if user specifies return type, must parse it*/)
            {
                DataTypes ReturnType;
                SymbolListRec FormalArgumentList;
                ASTExprListRec ReversedExprList;
                ASTExprListRec ExprListScan;

                /* yes we should -- parse the ": <type>" after it */

                /* swallow colon, then parse type */
                Token = Context.Scanner.GetNextToken();
                if (Token.GetTokenType() != TokenTypes.eTokenColon)
                {
                    Context.Scanner.UngetToken(Token);
                    ReturnType = DataTypes.eInteger; /* default return type is integer */
                }
                else
                {
                    /* get the actual return type */
                    Error = ParseType(
                        out ReturnType,
                        Context,
                        out LineNumberOut);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                }

                /* determine the types of the argument list */
                /* reverse the expression list */
                ReversedExprList = null;
                ExprListScan = ListOfParameters;
                while (ExprListScan != null)
                {
                    /* cons first element on so it will be last of the new list */
                    ReversedExprList = ASTExprListCons(
                        ExprListGetFirstExpr(ExprListScan),
                        ReversedExprList,
                        Context.Scanner.GetCurrentLineNumber());
                    /* go to next */
                    ExprListScan = ExprListGetRestList(ExprListScan);
                }
                /* now build arg symbol list from expression list */
                FormalArgumentList = null;
                ExprListScan = ReversedExprList;
                while (ExprListScan != null)
                {
                    SymbolRec NewDummySymbol;
                    DataTypes ArgumentDataType;
                    int DummyLine;

                    /* create symbol with same type as actual argument */
                    NewDummySymbol = new SymbolRec(String.Empty);
                    if (TypeCheckExpression(out ArgumentDataType, ExprListGetFirstExpr(ExprListScan), out DummyLine)
                        != CompileErrors.eCompileNoError)
                    {
                        /* the types don't check -- this will be caught later, so we'll */
                        /* just ignore for our purposes and use a bogus type */
                        ArgumentDataType = DataTypes.eInteger;
                    }
                    NewDummySymbol.SymbolBecomeVariable(
                        ArgumentDataType);
                    /* add symbol to list */
                    FormalArgumentList = SymbolListRec.SymbolListCons(
                        NewDummySymbol,
                        FormalArgumentList);
                    /* go to next */
                    ExprListScan = ExprListGetRestList(ExprListScan);
                }

                /* fix up the function symbol */
                if (undeclared)
                {
                    FunctionNameSymbol.SymbolBecomeFunction(
                        FormalArgumentList,
                        ReturnType);
                }
                // otherwise, disregard deprecated return type specification - with whole program compilation
                // the return types are determined correctly in a later pass.
            }

            // record function reference for later fixup (whole program compilation case)
            List<ParserContext.FunctionSymbolRefInfo> symbols;
            if (!Context.FunctionSymbolList.TryGetValue(FunctionNameSymbol.GetSymbolName(), out symbols))
            {
                symbols = new List<ParserContext.FunctionSymbolRefInfo>();
                Context.FunctionSymbolList.Add(FunctionNameSymbol.GetSymbolName(), symbols);
            }
            symbols.Add(new ParserContext.FunctionSymbolRefInfo(FunctionNameSymbol, FunctionGenerator.LineNumber));

            TheFunctionCall = NewFunctionCall(
                ListOfParameters,
                FunctionGenerator,
                Context.Scanner.GetCurrentLineNumber());

            ExpressionOut = NewExprFunctionCall(
                TheFunctionCall,
                Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*   84:   <arraysubscript>   ::= OPENBRACKET <exprlist> CLOSEBRACKET */
        /* FIRST SET: */
        /*  <arraysubscript>   : {OPENBRACKET} */
        /* FOLLOW SET: */
        /*  <arraysubscript>   : {and, or, xor, div, mod, SHR, SHL, then, else, */
        /*       elseif, while, until, do, to, ), CLOSEBRACKET, , , :=, ;, *, */
        /*       /, +, -, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, */
        /*       <expr3prime>, <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, */
        /*       <mult_oper>, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseArraySubscript(
            out ASTExpressionRec ExpressionOut,
            ASTExpressionRec ArrayGenerator,
            ParserContext Context,
            out int LineNumberOut)
        {
            TokenRec<KeywordsType> Token;
            ASTExpressionRec Subscript;
            CompileErrors Error;
            ASTBinaryOpRec ArraySubsOperation;
            ASTExprListRec SubscriptRaw;

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
            Subscript = NewExprSequence(
                SubscriptRaw,
                Context.Scanner.GetCurrentLineNumber());

            Token = Context.Scanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenCloseBracket)
            {
                LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                return CompileErrors.eCompileExpectedCloseBracket;
            }

            ArraySubsOperation = NewBinaryOperator(
                BinaryOpType.eBinaryArraySubscripting,
                ArrayGenerator,
                Subscript,
                Context.Scanner.GetCurrentLineNumber());
            ExpressionOut = NewExprBinaryOperator(
                ArraySubsOperation,
                Context.Scanner.GetCurrentLineNumber());

            return CompileErrors.eCompileNoError;
        }




        /*  124:   <exprlist>         ::= <exprlistelem> <exprlisttail> */
        /* FIRST SET: */
        /*  <exprlist>         : {<identifier>, <integer>, <single>, <double>, <fixed>, */
        /*       <string>, bool, int, single, double, fixed, proto, var, not, sin, */
        /*       cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, neg, sign, */
        /*       length, if, while, until, do, resize, error, true, false, */
        /*       set, (, -, <prototype>, <expr>, <expr2>, <expr3>, <expr4>, <expr5>, */
        /*       <expr6>, <unary_oper>, <expr7>, <expr8>, <whileloop>, <untilloop>} */
        /* FOLLOW SET: */
        /*  <exprlist>         : {), CLOSEBRACKET, EOF} */
        public static CompileErrors ParseExprList(
            out ASTExprListRec ExpressionOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            CompileErrors Error;
            ASTExpressionRec FirstExpression;
            TokenRec<KeywordsType> Token;
            ASTExprListRec RestOfList;

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

            /* see if we should parse a non-existent element (prototype) which */
            /* generates no code */
            if ((Token.GetTokenType() == TokenTypes.eTokenKeyword)
                && (Token.GetTokenKeywordTag() == KeywordsType.eExprKwrdProto))
            {
                SymbolRec ProtoSymbolOut;

                Context.Scanner.UngetToken(Token);
                Error = ParsePrototype(
                    out ProtoSymbolOut,
                    Context,
                    out LineNumberOut);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                /* this is declarative and generates no code */
                /* as a hack to get this to work, we'll expect a semicolon and */
                /* another expression list */
                Token = Context.Scanner.GetNextToken();
                /* eat up the semicolon */
                if (Token.GetTokenType() != TokenTypes.eTokenSemicolon)
                {
                    LineNumberOut = Context.Scanner.GetCurrentLineNumber();
                    return CompileErrors.eCompilePrototypeCantBeLastThingInExprList;
                }
                /* now just parse the rest of the expression list */
                return ParseExprList(
                    out ExpressionOut,
                    Context,
                    out LineNumberOut);
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

            ExpressionOut = ASTExprListCons(
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
        /*       elseif, while, until, do, to, ), CLOSEBRACKET, , , :=, ;, *, */
        /*       /, +, -, EQ, NEQ, LT, LTEQ, GR, GREQ, <expr2prime>, <conj_oper>, */
        /*       <expr3prime>, <rel_oper>, <expr4prime>, <add_oper>, <expr5prime>, */
        /*       <mult_oper>, <actualtail>, <iftail>, <loopwhileuntil>, <exprlisttail>} */
        private static CompileErrors ParseExponentiation(
            out ASTExpressionRec ExpressionOut,
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
        /*       <fixed>, <string>, bool, int, single, double, fixed, proto, var, */
        /*       not, sin, cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, */
        /*       neg, sign, length, if, while, until, do, resize, error, */
        /*       true, false, set, (, -, <prototype>, <expr>, <expr2>, */
        /*       <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, <expr7>, <expr8>, */
        /*       <actuallist>, <whileloop>, <untilloop>} */
        /* FOLLOW SET: */
        /*  <actualstart>      : {)} */
        private static CompileErrors ParseActualStart(
            out ASTExprListRec ParamListOut,
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
        /*       <fixed>, <string>, bool, int, single, double, fixed, proto, var, */
        /*       not, sin, cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, abs, */
        /*       neg, sign, length, if, while, until, do, resize, error, */
        /*       true, false, set, (, -, <prototype>, <expr>, <expr2>, */
        /*       <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, <expr7>, <expr8>, */
        /*       <whileloop>, <untilloop>} */
        /* FOLLOW SET: */
        /*  <actuallist>       : {)} */
        private static CompileErrors ParseActualList(
            out ASTExprListRec ParamListOut,
            ParserContext Context,
            out int LineNumberOut)
        {
            ASTExpressionRec FirstExpression;
            CompileErrors Error;
            ASTExprListRec RestOfList;

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

            ParamListOut = ASTExprListCons(
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
            out ASTExprListRec ParamListOut,
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
            out ASTExprListRec ListOut,
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
        /*   22:   <exprlistelem>     ::= <prototype> */
        /* FIRST SET: */
        /*  <exprlistelem>     : {<identifier>, <integer>, <single>, <double>, */
        /*       <fixed>, <string>, bool, int, single, double, fixed, proto, */
        /*       var, not, sin, cos, tan, asin, acos, atan, ln, exp, sqr, sqrt, */
        /*       abs, neg, sign, length, if, while, until, do, resize, error, */
        /*       true, false, set, (, -, <prototype>, <expr>, */
        /*       <expr2>, <expr3>, <expr4>, <expr5>, <expr6>, <unary_oper>, <expr7>, */
        /*       <expr8>, <whileloop>, <untilloop>} */
        /* FOLLOW SET: */
        /*  <exprlistelem>     : {), CLOSEBRACKET, ;, <exprlisttail>} */
        private static CompileErrors ParseExprListElem(
            out ASTExpressionRec ExpressionOut,
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
