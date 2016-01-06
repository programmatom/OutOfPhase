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
    /* syntactical errors that can occur while compiling */
    public enum CompileErrors
    {
        eCompileNoError,
        eCompileOutOfMemory,
        eCompileExpectedFuncOrProto,
        eCompileExpectedFunc,
        eCompileExpectedIdentifier,
        eCompileExpectedOpenParen,
        eCompileExpectedCloseParen,
        eCompileExpectedColon,
        eCompileExpectedSemicolon,
        eCompileMultiplyDefinedIdentifier,
        eCompileExpectedTypeSpecifier,
        eCompileExpectedExpressionForm,
        eCompileExpectedColonEqual,
        eCompileExpectedTo,
        eCompileExpectedStringLiteral,
        eCompileExpectedResumable,
        eCompileExpectedWhile,
        eCompileExpectedDo,
        eCompileExpectedUntil,
        eCompileExpectedOpenParenOrEqual,
        eCompileExpectedThen,
        eCompileExpectedWhileOrUntil,
        eCompileExpectedComma,
        eCompileExpectedCommaOrCloseParen,
        eCompileExpectedElseOrElseIf,
        eCompileExpectedOperatorOrStatement,
        eCompileExpectedOperand,
        eCompileIdentifierNotDeclared,
        eCompileExpectedRightAssociativeOperator,
        eCompileExpectedOpenBracket,
        eCompileExpectedCloseBracket,
        eCompileExpectedVariable,
        eCompileExpectedArrayType,
        eCompileArraySizeSpecMustBeInteger,
        eCompileTypeMismatch,
        eCompileInvalidLValue,
        eCompileOperandsMustBeScalar,
        eCompileOperandsMustBeSequencedScalar,
        eCompileOperandsMustBeIntegers,
        eCompileRightOperandMustBeInteger,
        eCompileArraySubscriptMustBeInteger,
        eCompileArrayRequiredForSubscription,
        eCompileDoubleRequiredForExponentiation,
        eCompileArrayRequiredForResize,
        eCompileIntegerRequiredForResize,
        eCompileConditionalMustBeBoolean,
        eCompileTypeMismatchBetweenThenAndElse,
        eCompileErrorNeedsBooleanArg,
        eCompileFunctionIdentifierRequired,
        eCompileArgumentTypeConflict,
        eCompileWrongNumberOfArgsToFunction,
        eCompileMustBeAVariableIdentifier,
        eCompileOperandMustBeBooleanOrInteger,
        eCompileOperandMustBeDouble,
        eCompileArrayRequiredForGetLength,
        eCompileTypeMismatchInAssignment,
        eCompileVoidExpressionIsNotAllowed,
        eCompileMultiplyDeclaredFunction,
        eCompilePrototypeCantBeLastThingInExprList,
        eCompileInputBeyondEndOfFunction,
        eCompileArrayConstructionOnScalarType,
        eCompileOperandMustBeInteger,
        eCompileOperandMustBeArrayOfFloat,
        eCompileExpectedStep,
        eCompileExpectedEqual,
    }

    public enum KeywordsType
    {
        eExprKwrdFunc,
        eExprKwrdProto,
        eExprKwrdVoid,
        eExprKwrdBool,
        eExprKwrdInt,
        eExprKwrdSingle,
        eExprKwrdFloat,
        eExprKwrdDouble,
        eExprKwrdFixed,
        eExprKwrdBoolarray,
        eExprKwrdBytearray,
        eExprKwrdIntarray,
        eExprKwrdSinglearray,
        eExprKwrdFloatarray,
        eExprKwrdDoublearray,
        eExprKwrdFixedarray,
        eExprKwrdVar,
        eExprKwrdIf,
        eExprKwrdWhile,
        eExprKwrdDo,
        eExprKwrdUntil,
        eExprKwrdSet,
        eExprKwrdResize,
        eExprKwrdTo,
        eExprKwrdError,
        eExprKwrdResumable,
        eExprKwrdNot,
        eExprKwrdSin,
        eExprKwrdCos,
        eExprKwrdTan,
        eExprKwrdAsin,
        eExprKwrdAcos,
        eExprKwrdAtan,
        eExprKwrdLn,
        eExprKwrdExp,
        eExprKwrdSqr,
        eExprKwrdSqrt,
        eExprKwrdAbs,
        eExprKwrdNeg,
        eExprKwrdSign,
        eExprKwrdLength,
        eExprKwrdPi,
        eExprKwrdTrue,
        eExprKwrdFalse,
        eExprKwrdThen,
        eExprKwrdElse,
        eExprKwrdElseif,
        eExprKwrdAnd,
        eExprKwrdOr,
        eExprKwrdXor,
        eExprKwrdDiv,
        eExprKwrdMod,
        eExprKwrdGetsampleleft,
        eExprKwrdGetsampleright,
        eExprKwrdGetsample,
        eExprKwrdGetwavenumframes,
        eExprKwrdGetwavenumtables,
        eExprKwrdGetwavedata,
        eExprKwrdPrint,
        eExprKwrdButterworthbandpass,
        eExprKwrdFirstorderlowpass,
        eExprKwrdVecsqr,
        eExprKwrdVecsqrt,
        eExprKwrdDup,
        eExprKwrdLoadsampleleft,
        eExprKwrdLoadsampleright,
        eExprKwrdLoadsample,
        eExprKwrdFor,
        eExprKwrdStep,
    }

    /* this function is used for specifying information about a parameter */
    public struct FunctionParamRec
    {
        public string ParameterName;
        public DataTypes ParameterType;

        public FunctionParamRec(string ParameterName, DataTypes ParameterType)
        {
            this.ParameterName = ParameterName;
            this.ParameterType = ParameterType;
        }
    }

    public static partial class Compiler
    {
        private static readonly KeywordRec<KeywordsType>[] KeywordTable = new KeywordRec<KeywordsType>[]
    	{
		    new KeywordRec<KeywordsType>("abs", KeywordsType.eExprKwrdAbs),
		    new KeywordRec<KeywordsType>("acos", KeywordsType.eExprKwrdAcos),
		    new KeywordRec<KeywordsType>("and", KeywordsType.eExprKwrdAnd),
		    new KeywordRec<KeywordsType>("asin", KeywordsType.eExprKwrdAsin),
		    new KeywordRec<KeywordsType>("atan", KeywordsType.eExprKwrdAtan),
		    new KeywordRec<KeywordsType>("bool", KeywordsType.eExprKwrdBool),
		    new KeywordRec<KeywordsType>("boolarray", KeywordsType.eExprKwrdBoolarray),
		    new KeywordRec<KeywordsType>("butterworthbandpass", KeywordsType.eExprKwrdButterworthbandpass),
		    new KeywordRec<KeywordsType>("bytearray", KeywordsType.eExprKwrdBytearray),
		    new KeywordRec<KeywordsType>("cos", KeywordsType.eExprKwrdCos),
		    new KeywordRec<KeywordsType>("div", KeywordsType.eExprKwrdDiv),
		    new KeywordRec<KeywordsType>("do", KeywordsType.eExprKwrdDo),
		    new KeywordRec<KeywordsType>("double", KeywordsType.eExprKwrdDouble),
		    new KeywordRec<KeywordsType>("doublearray", KeywordsType.eExprKwrdDoublearray),
		    new KeywordRec<KeywordsType>("dup", KeywordsType.eExprKwrdDup),
		    new KeywordRec<KeywordsType>("else", KeywordsType.eExprKwrdElse),
		    new KeywordRec<KeywordsType>("elseif", KeywordsType.eExprKwrdElseif),
		    new KeywordRec<KeywordsType>("error", KeywordsType.eExprKwrdError),
		    new KeywordRec<KeywordsType>("exp", KeywordsType.eExprKwrdExp),
		    new KeywordRec<KeywordsType>("false", KeywordsType.eExprKwrdFalse),
		    new KeywordRec<KeywordsType>("firstorderlowpass", KeywordsType.eExprKwrdFirstorderlowpass),
		    new KeywordRec<KeywordsType>("fixed", KeywordsType.eExprKwrdFixed),
		    new KeywordRec<KeywordsType>("fixedarray", KeywordsType.eExprKwrdFixedarray),
		    new KeywordRec<KeywordsType>("float", KeywordsType.eExprKwrdFloat),
		    new KeywordRec<KeywordsType>("floatarray", KeywordsType.eExprKwrdFloatarray),
		    new KeywordRec<KeywordsType>("for", KeywordsType.eExprKwrdFor),
		    new KeywordRec<KeywordsType>("func", KeywordsType.eExprKwrdFunc),
		    new KeywordRec<KeywordsType>("getsample", KeywordsType.eExprKwrdGetsample),
		    new KeywordRec<KeywordsType>("getsampleleft", KeywordsType.eExprKwrdGetsampleleft),
		    new KeywordRec<KeywordsType>("getsampleright", KeywordsType.eExprKwrdGetsampleright),
		    new KeywordRec<KeywordsType>("getwavedata", KeywordsType.eExprKwrdGetwavedata),
		    new KeywordRec<KeywordsType>("getwavenumframes", KeywordsType.eExprKwrdGetwavenumframes),
		    new KeywordRec<KeywordsType>("getwavenumtables", KeywordsType.eExprKwrdGetwavenumtables),
		    new KeywordRec<KeywordsType>("if", KeywordsType.eExprKwrdIf),
		    new KeywordRec<KeywordsType>("int", KeywordsType.eExprKwrdInt),
		    new KeywordRec<KeywordsType>("intarray", KeywordsType.eExprKwrdIntarray),
		    new KeywordRec<KeywordsType>("length", KeywordsType.eExprKwrdLength),
		    new KeywordRec<KeywordsType>("ln", KeywordsType.eExprKwrdLn),
		    new KeywordRec<KeywordsType>("loadsample", KeywordsType.eExprKwrdLoadsample),
		    new KeywordRec<KeywordsType>("loadsampleleft", KeywordsType.eExprKwrdLoadsampleleft),
		    new KeywordRec<KeywordsType>("loadsampleright", KeywordsType.eExprKwrdLoadsampleright),
		    new KeywordRec<KeywordsType>("mod", KeywordsType.eExprKwrdMod),
		    new KeywordRec<KeywordsType>("neg", KeywordsType.eExprKwrdNeg),
		    new KeywordRec<KeywordsType>("not", KeywordsType.eExprKwrdNot),
		    new KeywordRec<KeywordsType>("or", KeywordsType.eExprKwrdOr),
		    new KeywordRec<KeywordsType>("pi", KeywordsType.eExprKwrdPi),
		    new KeywordRec<KeywordsType>("print", KeywordsType.eExprKwrdPrint),
		    new KeywordRec<KeywordsType>("proto", KeywordsType.eExprKwrdProto),
		    new KeywordRec<KeywordsType>("resize", KeywordsType.eExprKwrdResize),
		    new KeywordRec<KeywordsType>("resumable", KeywordsType.eExprKwrdResumable),
		    new KeywordRec<KeywordsType>("set", KeywordsType.eExprKwrdSet),
		    new KeywordRec<KeywordsType>("sign", KeywordsType.eExprKwrdSign),
		    new KeywordRec<KeywordsType>("sin", KeywordsType.eExprKwrdSin),
		    new KeywordRec<KeywordsType>("single", KeywordsType.eExprKwrdSingle),
		    new KeywordRec<KeywordsType>("singlearray", KeywordsType.eExprKwrdSinglearray),
		    new KeywordRec<KeywordsType>("sqr", KeywordsType.eExprKwrdSqr),
		    new KeywordRec<KeywordsType>("sqrt", KeywordsType.eExprKwrdSqrt),
		    new KeywordRec<KeywordsType>("step", KeywordsType.eExprKwrdStep),
		    new KeywordRec<KeywordsType>("tan", KeywordsType.eExprKwrdTan),
		    new KeywordRec<KeywordsType>("then", KeywordsType.eExprKwrdThen),
		    new KeywordRec<KeywordsType>("to", KeywordsType.eExprKwrdTo),
		    new KeywordRec<KeywordsType>("true", KeywordsType.eExprKwrdTrue),
		    new KeywordRec<KeywordsType>("until", KeywordsType.eExprKwrdUntil),
		    new KeywordRec<KeywordsType>("var", KeywordsType.eExprKwrdVar),
		    new KeywordRec<KeywordsType>("vecsqr", KeywordsType.eExprKwrdVecsqr),
		    new KeywordRec<KeywordsType>("vecsqrt", KeywordsType.eExprKwrdVecsqrt),
		    new KeywordRec<KeywordsType>("void", KeywordsType.eExprKwrdVoid),
		    new KeywordRec<KeywordsType>("while", KeywordsType.eExprKwrdWhile),
		    new KeywordRec<KeywordsType>("xor", KeywordsType.eExprKwrdXor),
	    };

        private struct CompileErrorRec
        {
            public CompileErrors ErrorCode;
            public string Message;

            public CompileErrorRec(CompileErrors ErrorCode, string Message)
            {
                this.ErrorCode = ErrorCode;
                this.Message = Message;
            }
        }

        private static readonly CompileErrorRec[] Errors = new CompileErrorRec[]
	    {
		    new CompileErrorRec(CompileErrors.eCompileNoError,
			    "No error"),
		    new CompileErrorRec(CompileErrors.eCompileOutOfMemory,
			    "Out of memory"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedFuncOrProto,
			    "Expected 'func' or 'proto'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedFunc,
			    "Expected 'func'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedIdentifier,
			    "Expected identifier"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedOpenParen,
			    "Expected '('"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedCloseParen,
			    "Expected ')'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedColon,
			    "Expected ':'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedSemicolon,
			    "Expected ';'"),
		    new CompileErrorRec(CompileErrors.eCompileMultiplyDefinedIdentifier,
			    "Identifier has already been used"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedTypeSpecifier,
			    "Expected type specification"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedExpressionForm,
			    "Expected expression"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedColonEqual,
			    "Expected ':='"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedTo,
			    "Expected 'to'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedStringLiteral,
			    "Expected string literal"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedResumable,
			    "Expected 'resumable'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedWhile,
			    "Expected 'while'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedDo,
			    "Expected 'do'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedUntil,
			    "Expected 'until'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedOpenParenOrEqual,
			    "Expected '(' or '='"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedThen,
			    "Expected 'then'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedWhileOrUntil,
			    "Expected 'while' or 'until'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedComma,
			    "Expected ', '"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedCommaOrCloseParen,
			    "Expected ', ' or ')'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedElseOrElseIf,
			    "Expected 'else' or 'elseif'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedOperatorOrStatement,
			    "Expected operator or statement"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedOperand,
			    "Expected operand"),
		    new CompileErrorRec(CompileErrors.eCompileIdentifierNotDeclared,
			    "Identifier hasn't been declared"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedRightAssociativeOperator,
			    "Expected right associative operator"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedOpenBracket,
			    "Expected '['"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedCloseBracket,
			    "Expected ']'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedVariable,
			    "Expected variable"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedArrayType,
			    "Expected array type"),
		    new CompileErrorRec(CompileErrors.eCompileArraySizeSpecMustBeInteger,
			    "Array size specifier must be integer"),
		    new CompileErrorRec(CompileErrors.eCompileTypeMismatch,
			    "Type conflict"),
		    new CompileErrorRec(CompileErrors.eCompileInvalidLValue,
			    "Invalid l-value"),
		    new CompileErrorRec(CompileErrors.eCompileOperandsMustBeScalar,
			    "Operands must be scalar"),
		    new CompileErrorRec(CompileErrors.eCompileOperandsMustBeSequencedScalar,
			    "Operands must be sequenced scalar"),
		    new CompileErrorRec(CompileErrors.eCompileOperandsMustBeIntegers,
			    "Operands must be an integer"),
		    new CompileErrorRec(CompileErrors.eCompileRightOperandMustBeInteger,
			    "Right operand must be an integer"),
		    new CompileErrorRec(CompileErrors.eCompileArraySubscriptMustBeInteger,
			    "Array subscript must be an integer"),
		    new CompileErrorRec(CompileErrors.eCompileArrayRequiredForSubscription,
			    "Array required for subscription"),
		    new CompileErrorRec(CompileErrors.eCompileDoubleRequiredForExponentiation,
			    "Operands for exponentiation must be doubles"),
		    new CompileErrorRec(CompileErrors.eCompileArrayRequiredForResize,
			    "Array required for resize"),
		    new CompileErrorRec(CompileErrors.eCompileIntegerRequiredForResize,
			    "Integer required resize array size specifier"),
		    new CompileErrorRec(CompileErrors.eCompileConditionalMustBeBoolean,
			    "Conditional expression must boolean"),
		    new CompileErrorRec(CompileErrors.eCompileTypeMismatchBetweenThenAndElse,
			    "Type conflict between consequent and alternate arms of conditional"),
		    new CompileErrorRec(CompileErrors.eCompileErrorNeedsBooleanArg,
			    "Error directive needs boolean argument"),
		    new CompileErrorRec(CompileErrors.eCompileFunctionIdentifierRequired,
			    "Function identifier required"),
		    new CompileErrorRec(CompileErrors.eCompileArgumentTypeConflict,
			    "Type conflict between formal and actual arguments"),
		    new CompileErrorRec(CompileErrors.eCompileWrongNumberOfArgsToFunction,
			    "Wrong number of arguments to function"),
		    new CompileErrorRec(CompileErrors.eCompileMustBeAVariableIdentifier,
			    "Variable identifier required"),
		    new CompileErrorRec(CompileErrors.eCompileOperandMustBeBooleanOrInteger,
			    "Operands must be boolean or integer"),
		    new CompileErrorRec(CompileErrors.eCompileOperandMustBeDouble,
			    "Operand must be double"),
		    new CompileErrorRec(CompileErrors.eCompileArrayRequiredForGetLength,
			    "Array required for length operator"),
		    new CompileErrorRec(CompileErrors.eCompileTypeMismatchInAssignment,
			    "Type conflict between l-value and argument"),
		    new CompileErrorRec(CompileErrors.eCompileVoidExpressionIsNotAllowed,
			    "Void expression is not allowed"),
		    new CompileErrorRec(CompileErrors.eCompileMultiplyDeclaredFunction,
			    "Function name has already been used"),
		    new CompileErrorRec(CompileErrors.eCompilePrototypeCantBeLastThingInExprList,
			    "Prototype can't be the last expression in an expression sequence"),
		    new CompileErrorRec(CompileErrors.eCompileInputBeyondEndOfFunction,
			    "Input beyond end of expression"),
		    new CompileErrorRec(CompileErrors.eCompileArrayConstructionOnScalarType,
			    "Array constructor applied to scalar variable"),
		    new CompileErrorRec(CompileErrors.eCompileOperandMustBeInteger,
			    "Operand must be integer"),
		    new CompileErrorRec(CompileErrors.eCompileOperandMustBeArrayOfFloat,
			    "Operand must be array of single"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedStep,
			    "Expected 'step'"),
		    new CompileErrorRec(CompileErrors.eCompileExpectedEqual,
			    "Expected '='"),
        };


        private static SymbolRec ArgListTypesToSymbol(
            string functionName,
            DataTypes[] argsTypes,
            DataTypes returnType)
        {
            SymbolListRec head = null;
            for (int i = argsTypes.Length - 1; i >= 0; i--)
            {
                SymbolRec symbolArg = new SymbolRec(null);
                symbolArg.SymbolBecomeVariable(argsTypes[i]);
                SymbolListRec node = new SymbolListRec(symbolArg, head);
                head = node;
            }

            SymbolRec symbol = new SymbolRec(functionName);
            symbol.SymbolBecomeFunction(head, returnType);
            return symbol;
        }

        private static void SymbolicArgListToType(
            SymbolRec functionSymbol,
            out DataTypes[] argsTypesOut,
            out string[] argsNamesOut)
        {
            List<DataTypes> argsTypes = new List<DataTypes>();
            List<string> argsNames = new List<string>();
            SymbolListRec argNode = functionSymbol.GetSymbolFunctionArgList();
            while (argNode != null)
            {
                SymbolRec arg = argNode.GetFirstFromSymbolList();
                argsTypes.Add(arg.GetSymbolVariableDataType());
                argsNames.Add(arg.GetSymbolName());
                argNode = argNode.GetRestListFromSymbolList();
            }
            argsTypesOut = argsTypes.ToArray();
            argsNamesOut = argsNames.ToArray();
        }

        /* apply optimizations to the AST.  returns False if it runs out of memory. */
        private static void OptimizeAST(ASTExpressionRec Expr)
        {
            bool DidSomething = true;
            while (DidSomething)
            {
                bool OneDidSomething;

                DidSomething = false;

                FoldConstExpression(Expr, out OneDidSomething);
                DidSomething = DidSomething || OneDidSomething;
            }
        }

        /* return a null terminated static string describing the error. */
        public static string GetCompileErrorString(CompileErrors Error)
        {
#if DEBUG
            /* verify structure */
            for (int i = 0; i < Errors.Length; i += 1)
            {
                if (Errors[i].ErrorCode != i + CompileErrors.eCompileNoError)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
            }
#endif

            /* direct index error string */
            return Errors[Error - CompileErrors.eCompileNoError].Message;
        }

#if false // TODO: remove
        /* compile a module.  a module is a text block with a series of function definitions. */
        /* if compilation succeeds, the functions are added to the CodeCenter object. */
        /* the text data is NOT altered. */
        public static CompileErrors CompileModule(
            out int ErrorLineNumber,
            string TextData,
            object Signature,
            CodeCenterRec CodeCenter,
            string Filename)
        {
            CompileErrors FinalErrorThing = CompileErrors.eCompileNoError;
            ErrorLineNumber = -1;

            ScannerRec<KeywordsType> TheScanner = new ScannerRec<KeywordsType>(TextData, KeywordTable);
            SymbolTableRec TheSymbolTable = new SymbolTableRec();

            /* loop until there are no more things to parse */
            bool LoopFlag = true;
            while (LoopFlag)
            {
                TokenRec<KeywordsType> Token;
                int InitialLineNumberOfForm;

                Token = TheScanner.GetNextToken();
                InitialLineNumberOfForm = TheScanner.GetCurrentLineNumber();
                if (Token.GetTokenType() == TokenTypes.eTokenEndOfInput)
                {
                    /* no more functions to parse, so stop */
                    LoopFlag = false;
                }
                else
                {
                    SymbolRec SymbolTableEntryForForm;
                    ASTExpressionRec FunctionBodyRecord;

                    /* parse the function */
                    TheScanner.UngetToken(Token);
                    CompileErrors Error = ParseForm(
                        out SymbolTableEntryForForm,
                        out FunctionBodyRecord,
                        new ParserContext(
                            TheScanner,
                            TheSymbolTable),
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        FinalErrorThing = Error;
                        goto ExitErrorPoint;
                    }
                    /* SymbolTableEntryForForm will be the symbol table entry that */
                    /* was added to the symbol table.  FunctionBodyRecord is either */
                    /* an expression for a function or NIL if it was a prototype */

                    /* if no error occurred, then generate code */
                    if (FunctionBodyRecord != null)
                    {
                        /* only generate code for real functions */

                        /* step 0:  make sure it hasn't been created yet */
                        if (CodeCenter.CodeCenterHaveThisFunction(SymbolTableEntryForForm.GetSymbolName()))
                        {
                            ErrorLineNumber = TheScanner.GetCurrentLineNumber();
                            FinalErrorThing = CompileErrors.eCompileMultiplyDeclaredFunction;
                            goto ExitErrorPoint;
                        }

                        /* step 1:  do type checking */
                        DataTypes ResultingType;
                        Error = TypeCheckExpression(
                            out ResultingType,
                            FunctionBodyRecord,
                            out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            FinalErrorThing = Error;
                            goto ExitErrorPoint;
                        }
                        /* check to see that resulting type matches declared type */
                        if (!CanRightBeMadeToMatchLeft(SymbolTableEntryForForm.GetSymbolFunctionReturnType(), ResultingType))
                        {
                            ErrorLineNumber = InitialLineNumberOfForm;
                            FinalErrorThing = CompileErrors.eCompileTypeMismatch;
                            goto ExitErrorPoint;
                        }
                        /* if it has to be promoted, then promote it */
                        if (MustRightBePromotedToLeft(SymbolTableEntryForForm.GetSymbolFunctionReturnType(), ResultingType))
                        {
                            /* insert promotion operator above expression */
                            ASTExpressionRec ReplacementExpr = PromoteTheExpression(
                                ResultingType,
                                SymbolTableEntryForForm.GetSymbolFunctionReturnType(),
                                FunctionBodyRecord,
                                InitialLineNumberOfForm);
                            FunctionBodyRecord = ReplacementExpr;
                            /* sanity check */
                            Error = TypeCheckExpression(
                                out ResultingType,
                                FunctionBodyRecord,
                                out ErrorLineNumber);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                // type promotion caused failure
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                            if (ResultingType != SymbolTableEntryForForm.GetSymbolFunctionReturnType())
                            {
                                // after type promotion, types are no longer the same
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                        }

                        /* step 1.5:  optimize the AST */
                        OptimizeAST(FunctionBodyRecord);

                        /* step 2:  do code generation */
                        /* calling conventions:  */
                        /*  - push the arguments */
                        /*  - funccall pushes the return address */
                        /* thus, upon entry, Stack[0] will be the return address */
                        /* and Stack[-1] will be the rightmost argument */
                        /* on return, args and retaddr are popped and retval replaces them */
                        int StackDepth = 0;
                        int MaxStackDepth = 0;
                        int ReturnValueLocation = StackDepth; /* remember return value location */
                        int ArgumentIndex = 0;
                        SymbolListRec FormalArgumentListScan = SymbolTableEntryForForm.GetSymbolFunctionArgList();
                        while (FormalArgumentListScan != null)
                        {
                            SymbolRec TheFormalArg = FormalArgumentListScan.GetFirstFromSymbolList();
                            StackDepth++; /* allocate first */
                            MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                            TheFormalArg.SetSymbolVariableStackLocation(StackDepth); /* remember */
                            ArgumentIndex++;
                            FormalArgumentListScan = FormalArgumentListScan.GetRestListFromSymbolList();
                        }
                        /* reserve return address spot */
                        StackDepth++;
                        MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                        /* allocate the function code */
                        PcodeRec TheFunctionCode = new PcodeRec();
                        CodeGenExpression(
                            TheFunctionCode,
                            ref StackDepth,
                            ref MaxStackDepth,
                            FunctionBodyRecord);
                        Debug.Assert(StackDepth <= MaxStackDepth);
                        /* 2 extra words for retaddr and resultofexpr */
                        if (StackDepth != ArgumentIndex + 1 + 1)
                        {
                            // stack depth error after evaluating function
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        /* now put the return instruction (pops retaddr and args, leaving retval) */
                        int ignored;
                        TheFunctionCode.AddPcodeInstruction(Pcodes.epReturnFromSubroutine, out ignored, InitialLineNumberOfForm);
                        TheFunctionCode.AddPcodeOperandInteger(ArgumentIndex);
                        StackDepth = StackDepth - (1 + ArgumentIndex);
                        Debug.Assert(StackDepth <= MaxStackDepth);
                        if (StackDepth != 1)
                        {
                            // stack depth is wrong at end of function
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }

                        TheFunctionCode.MaxStackDepth = MaxStackDepth;

                        /* step 2.5:  optimize the code */
                        TheFunctionCode.OptimizePcode();

                        /* step 3:  create the function and save it away */
                        FuncCodeRec TheWholeFunctionThing = new FuncCodeRec(
                            SymbolTableEntryForForm.GetSymbolName(),
                            SymbolTableEntryForForm.GetSymbolFunctionArgList(),
                            TheFunctionCode,
                            SymbolTableEntryForForm.GetSymbolFunctionReturnType(),
                            Filename);

#if true // TODO:experimental
                        if (CILObject.EnableCIL)
                        {
                            CILAssembly cilAssembly = new CILAssembly();

                            DataTypes[] argsTypes;
                            string[] argsNames;
                            SymbolicArgListToType(SymbolTableEntryForForm, out argsTypes, out argsNames);
                            CILObject cilObject = new CILObject(
                                CodeCenter.ManagedFunctionLinker,
                                argsTypes,
                                argsNames,
                                SymbolTableEntryForForm.GetSymbolFunctionReturnType(),
                                FunctionBodyRecord,
                                cilAssembly);
                            TheWholeFunctionThing.CILObject = cilObject;

                            if (CILObject.EnableCIL)
                            {
                                cilAssembly.Finish();
                            }
                        }
#endif

                        /* add it to the code center */
                        CodeCenter.AddFunctionToCodeCenter(TheWholeFunctionThing, Signature);

                        /* wow, all done!  on to the next one */
                    }
                }
            }


        ExitErrorPoint:

            if (FinalErrorThing != CompileErrors.eCompileNoError)
            {
                /* since we're aborting, dump any functions we've already created */
                CodeCenter.FlushModulesCompiledFunctions(Signature);
            }

            return FinalErrorThing;
        }
#endif

        // Compile multiple modules. (eliminates the need to do prototyping or function signature inference.)
        // CodeCenter is cleared, and if compilation succeeds, the functions are added to CodeCenter.
        public static CompileErrors CompileWholeProgram(
            out int ErrorLineNumber,
            out int ErrorModuleIndex,
            string[] TextDatas,
            object[] Signatures,
            CodeCenterRec CodeCenter,
            string[] Filenames)
        {
            Debug.Assert(TextDatas.Length == Signatures.Length);
            Debug.Assert(TextDatas.Length == Filenames.Length);

            ErrorLineNumber = -1;
            ErrorModuleIndex = -1;

            CodeCenter.FlushAllCompiledFunctions();
            CodeCenter.RetainedFunctionSignatures = new KeyValuePair<string, FunctionSignature>[0];

            // parse

            List<SymbolRec> SymbolTableEntriesForForm = new List<SymbolRec>();
            List<ASTExpressionRec> FunctionBodyRecords = new List<ASTExpressionRec>();
            List<int> ModuleIndices = new List<int>();
            Dictionary<string, List<ParserContext.FunctionSymbolRefInfo>> FunctionRefSymbolList = new Dictionary<string, List<ParserContext.FunctionSymbolRefInfo>>();
            List<int> InitialLineNumbersOfForm = new List<int>();
            for (int module = 0; module < TextDatas.Length; module++)
            {
                string TextData = TextDatas[module];

                ErrorModuleIndex = module;

                ScannerRec<KeywordsType> TheScanner = new ScannerRec<KeywordsType>(TextData, KeywordTable);
                SymbolTableRec TheSymbolTable = new SymbolTableRec();

                /* loop until there are no more things to parse */
                while (true)
                {
                    TokenRec<KeywordsType> Token = TheScanner.GetNextToken();
                    int InitialLineNumberOfForm = TheScanner.GetCurrentLineNumber();
                    if (Token.GetTokenType() == TokenTypes.eTokenEndOfInput)
                    {
                        /* no more functions to parse, so stop */
                        break;
                    }

                    SymbolRec SymbolTableEntryForForm;
                    ASTExpressionRec FunctionBodyRecord;

                    /* parse the function */
                    TheScanner.UngetToken(Token);
                    CompileErrors Error = ParseForm(
                        out SymbolTableEntryForForm,
                        out FunctionBodyRecord,
                        new ParserContext(
                            TheScanner,
                            TheSymbolTable,
                            FunctionRefSymbolList),
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }

#if true // TODO:remove -- null means 'proto'
                    if (FunctionBodyRecord != null)
#endif
                    {
                        ModuleIndices.Add(module);
                        SymbolTableEntriesForForm.Add(SymbolTableEntryForForm);
                        FunctionBodyRecords.Add(FunctionBodyRecord);
                        InitialLineNumbersOfForm.Add(InitialLineNumberOfForm);
                    }
                }

                foreach (KeyValuePair<string, List<ParserContext.FunctionSymbolRefInfo>> name in FunctionRefSymbolList)
                {
                    foreach (ParserContext.FunctionSymbolRefInfo funcRef in name.Value)
                    {
                        funcRef.module = module;
                    }
                }
            }

            // push function type signatures into function call refs

            Dictionary<string, bool> functionNamesUsed = new Dictionary<string, bool>();
            for (int i = 0; i < SymbolTableEntriesForForm.Count; i++)
            {
                ErrorModuleIndex = ModuleIndices[i];

                SymbolRec FunctionDeclarationSymbol = SymbolTableEntriesForForm[i];
                if (functionNamesUsed.ContainsKey(FunctionDeclarationSymbol.GetSymbolName()))
                {
                    ErrorLineNumber = FunctionBodyRecords[i].LineNumber;
                    return CompileErrors.eCompileMultiplyDeclaredFunction;
                }
                functionNamesUsed.Add(FunctionDeclarationSymbol.GetSymbolName(), false);

                List<ParserContext.FunctionSymbolRefInfo> symbols;
                if (FunctionRefSymbolList.TryGetValue(FunctionDeclarationSymbol.GetSymbolName(), out symbols))
                {
                    foreach (ParserContext.FunctionSymbolRefInfo functionRef in symbols)
                    {
                        functionRef.symbol.SymbolBecomeFunction2(
                            FunctionDeclarationSymbol.GetSymbolFunctionArgList(),
                            FunctionDeclarationSymbol.GetSymbolFunctionReturnType());
                    }
                    FunctionRefSymbolList.Remove(FunctionDeclarationSymbol.GetSymbolName());
                }
            }

            foreach (KeyValuePair<string, List<ParserContext.FunctionSymbolRefInfo>> name in FunctionRefSymbolList)
            {
                foreach (ParserContext.FunctionSymbolRefInfo funcRef in name.Value)
                {
                    ErrorModuleIndex = funcRef.module;
                    ErrorLineNumber = funcRef.lineNumber;
                    return CompileErrors.eCompileMultiplyDeclaredFunction;
                }
            }

            // type check and type inference

            for (int i = 0; i < FunctionBodyRecords.Count; i++)
            {
                int module = ModuleIndices[i];
                SymbolRec SymbolTableEntryForForm = SymbolTableEntriesForForm[i];
                ASTExpressionRec FunctionBodyRecord = FunctionBodyRecords[i];
                int InitialLineNumberOfForm = InitialLineNumbersOfForm[i];

                ErrorModuleIndex = module;

                /* SymbolTableEntryForForm will be the symbol table entry that */
                /* was added to the symbol table.  FunctionBodyRecord is either */
                /* an expression for a function or NIL if it was a prototype */

                Debug.Assert(!CodeCenter.CodeCenterHaveThisFunction(SymbolTableEntryForForm.GetSymbolName()));

                /* step 1:  do type checking */
                DataTypes ResultingType;
                CompileErrors Error = TypeCheckExpression(
                    out ResultingType,
                    FunctionBodyRecord,
                    out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                /* check to see that resulting type matches declared type */
                if (!CanRightBeMadeToMatchLeft(SymbolTableEntryForForm.GetSymbolFunctionReturnType(), ResultingType))
                {
                    ErrorLineNumber = InitialLineNumberOfForm;
                    return CompileErrors.eCompileTypeMismatch;
                }
                /* if it has to be promoted, then promote it */
                if (MustRightBePromotedToLeft(SymbolTableEntryForForm.GetSymbolFunctionReturnType(), ResultingType))
                {
                    /* insert promotion operator above expression */
                    ASTExpressionRec ReplacementExpr = PromoteTheExpression(
                        ResultingType,
                        SymbolTableEntryForForm.GetSymbolFunctionReturnType(),
                        FunctionBodyRecord,
                        InitialLineNumberOfForm);
                    FunctionBodyRecord = ReplacementExpr;
                    /* sanity check */
                    Error = TypeCheckExpression(
                        out ResultingType,
                        FunctionBodyRecord,
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        // type promotion caused failure
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (ResultingType != SymbolTableEntryForForm.GetSymbolFunctionReturnType())
                    {
                        // after type promotion, types are no longer the same
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
            }

            // code generation

            CILAssembly cilAssembly = null;
            if (CILObject.EnableCIL)
            {
                cilAssembly = new CILAssembly();
            }

            FuncCodeRec[] TheWholeFunctionThings = new FuncCodeRec[FunctionBodyRecords.Count];
            for (int i = 0; i < FunctionBodyRecords.Count; i++)
            {
                int module = ModuleIndices[i];
                SymbolRec SymbolTableEntryForForm = SymbolTableEntriesForForm[i];
                ASTExpressionRec FunctionBodyRecord = FunctionBodyRecords[i];
                int InitialLineNumberOfForm = InitialLineNumbersOfForm[i];

                string Filename = Filenames[module];
                object Signature = Signatures[module];

                ErrorModuleIndex = module;

                Debug.Assert(!CodeCenter.CodeCenterHaveThisFunction(SymbolTableEntryForForm.GetSymbolName()));

                /* step 1.5:  optimize the AST */
                OptimizeAST(FunctionBodyRecord);

                /* step 2:  do code generation */
                /* calling conventions:  */
                /*  - push the arguments */
                /*  - funccall pushes the return address */
                /* thus, upon entry, Stack[0] will be the return address */
                /* and Stack[-1] will be the rightmost argument */
                /* on return, args and retaddr are popped and retval replaces them */
                int StackDepth = 0;
                int MaxStackDepth = 0;
                int ReturnValueLocation = StackDepth; /* remember return value location */
                int ArgumentIndex = 0;
                SymbolListRec FormalArgumentListScan = SymbolTableEntryForForm.GetSymbolFunctionArgList();
                while (FormalArgumentListScan != null)
                {
                    SymbolRec TheFormalArg = FormalArgumentListScan.GetFirstFromSymbolList();
                    StackDepth++; /* allocate first */
                    MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                    TheFormalArg.SetSymbolVariableStackLocation(StackDepth); /* remember */
                    ArgumentIndex++;
                    FormalArgumentListScan = FormalArgumentListScan.GetRestListFromSymbolList();
                }
                /* reserve return address spot */
                StackDepth++;
                MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                /* allocate the function code */
                PcodeRec TheFunctionCode = new PcodeRec();
                CodeGenExpression(
                    TheFunctionCode,
                    ref StackDepth,
                    ref MaxStackDepth,
                    FunctionBodyRecord);
                Debug.Assert(StackDepth <= MaxStackDepth);
                /* 2 extra words for retaddr and resultofexpr */
                if (StackDepth != ArgumentIndex + 1 + 1)
                {
                    // stack depth error after evaluating function
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                /* now put the return instruction (pops retaddr and args, leaving retval) */
                int ignored;
                TheFunctionCode.AddPcodeInstruction(Pcodes.epReturnFromSubroutine, out ignored, InitialLineNumberOfForm);
                TheFunctionCode.AddPcodeOperandInteger(ArgumentIndex);
                StackDepth = StackDepth - (1 + ArgumentIndex);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != 1)
                {
                    // stack depth is wrong at end of function
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                TheFunctionCode.MaxStackDepth = MaxStackDepth;

                /* step 2.5:  optimize the code */
                TheFunctionCode.OptimizePcode();

                /* step 3:  create the function and save it away */
                FuncCodeRec TheWholeFunctionThing = new FuncCodeRec(
                    SymbolTableEntryForForm.GetSymbolName(),
                    SymbolTableEntryForForm.GetSymbolFunctionArgList(),
                    TheFunctionCode,
                    SymbolTableEntryForForm.GetSymbolFunctionReturnType(),
                    Filename);

                TheWholeFunctionThings[i] = TheWholeFunctionThing;

#if true // TODO:experimental
                if (CILObject.EnableCIL)
                {
                    DataTypes[] argsTypes;
                    string[] argsNames;
                    SymbolicArgListToType(SymbolTableEntryForForm, out argsTypes, out argsNames);
                    CILObject cilObject = new CILObject(
                        CodeCenter.ManagedFunctionLinker,
                        argsTypes,
                        argsNames,
                        SymbolTableEntryForForm.GetSymbolFunctionReturnType(),
                        FunctionBodyRecord,
                        cilAssembly);
                    TheWholeFunctionThing.CILObject = cilObject;
                }
#endif
            }

            // register after entire assembly is emitted
            if (CILObject.EnableCIL)
            {
                cilAssembly.Finish();
            }

            for (int i = 0; i < TheWholeFunctionThings.Length; i++)
            {
                FuncCodeRec TheWholeFunctionThing = TheWholeFunctionThings[i];
                object Signature = Signatures[ModuleIndices[i]];

                CodeCenter.AddFunctionToCodeCenter(TheWholeFunctionThing, Signature);
            }

            // retain signatures for compilation of special functions

            CodeCenter.RetainedFunctionSignatures = new KeyValuePair<string, FunctionSignature>[SymbolTableEntriesForForm.Count];
            for (int i = 0; i < CodeCenter.RetainedFunctionSignatures.Length; i++)
            {
                DataTypes[] argsTypes;
                string[] argsNames;
                SymbolicArgListToType(SymbolTableEntriesForForm[i], out argsTypes, out argsNames);
                CodeCenter.RetainedFunctionSignatures[i] = new KeyValuePair<string, FunctionSignature>(
                    SymbolTableEntriesForForm[i].GetSymbolName(),
                    new FunctionSignature(
                        argsTypes,
                        SymbolTableEntriesForForm[i].GetSymbolFunctionReturnType()));
            }

            return CompileErrors.eCompileNoError;
        }

        /* compile a special function.  a special function has no function header, but is */
        /* simply some code to be executed.  the parameters the code is expecting are provided */
        /* in the FuncArray[] and NumParams.  the first parameter is deepest beneath the */
        /* top of stack.  the TextData is NOT altered.  if an error occurrs, *FunctionOut */
        /* will NOT contain a valid object */
        public static CompileErrors CompileSpecialFunction(
            CodeCenterRec CodeCenter,
            FunctionParamRec[] FuncArray,
            out int ErrorLineNumber,
            out DataTypes ReturnTypeOut,
            string TextData,
            bool suppressCILEmission,
            out PcodeRec FunctionOut,
            out ASTExpressionRec ASTOut)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;
            ASTOut = null;
            FunctionOut = null;
            ReturnTypeOut = DataTypes.eInvalidDataType;

            ScannerRec<KeywordsType> TheScanner = new ScannerRec<KeywordsType>(TextData, KeywordTable);
            SymbolTableRec TheSymbolTable = new SymbolTableRec();

            // reconstitute function prototypes
            for (int i = 0; i < CodeCenter.RetainedFunctionSignatures.Length; i++)
            {
                SymbolRec functionSignature = ArgListTypesToSymbol(
                    CodeCenter.RetainedFunctionSignatures[i].Key,
                    CodeCenter.RetainedFunctionSignatures[i].Value.ArgsTypes,
                    CodeCenter.RetainedFunctionSignatures[i].Value.ReturnType);
                TheSymbolTable.AddSymbolToTable(functionSignature);
            }

            /* build parameters into symbol table */
            int StackDepth = 0;
            int MaxStackDepth = 0;
            int ReturnAddressIndex = StackDepth;
            for (int i = 0; i < FuncArray.Length; i += 1)
            {
                SymbolRec TheParameter = new SymbolRec(FuncArray[i].ParameterName);
                TheParameter.SymbolBecomeVariable(FuncArray[i].ParameterType);
                /* allocate stack slot */
                StackDepth++;
                MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                TheParameter.SetSymbolVariableStackLocation(StackDepth);
                switch (TheSymbolTable.AddSymbolToTable(TheParameter))
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case AddSymbolType.eAddSymbolNoErr:
                        break;
                    case AddSymbolType.eAddSymbolAlreadyExists:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                }
            }
            /* fence them off */
            TheSymbolTable.IncrementSymbolTableLevel();

            /* reserve spot for fake return address (so we have uniform calling convention everywhere) */
            StackDepth++;
            MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
            if (StackDepth != FuncArray.Length + 1)
            {
                // stack depth error before evaluating function
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            ASTExprListRec ListOfExpressions;
            Error = ParseExprList(
                out ListOfExpressions,
                new ParserContext(
                    TheScanner,
                    TheSymbolTable),
                out ErrorLineNumber);
            /* compile the thing */
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            ASTExpressionRec TheExpressionThang = NewExprSequence(ListOfExpressions, TheScanner.GetCurrentLineNumber());

            /* make sure there is nothing after it */
            TokenRec<KeywordsType> Token = TheScanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenEndOfInput)
            {
                ErrorLineNumber = TheScanner.GetCurrentLineNumber();
                return CompileErrors.eCompileInputBeyondEndOfFunction;
            }

            DataTypes ResultingType;
            Error = TypeCheckExpression(out ResultingType, TheExpressionThang, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            OptimizeAST(TheExpressionThang);

            PcodeRec TheFunctionCode = new PcodeRec();

            CodeGenExpression(
                TheFunctionCode,
                ref StackDepth,
                ref MaxStackDepth,
                TheExpressionThang);
            Debug.Assert(StackDepth <= MaxStackDepth);

            ReturnTypeOut = GetExpressionsResultantType(TheExpressionThang);


            /* 2 extra words for retaddr, resultofexpr */
            if (StackDepth != FuncArray.Length + 1/*retaddr*/ + 1/*result*/)
            {
                // stack depth error after evaluating function
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            /* now put the return instruction */
            int unused;
            TheFunctionCode.AddPcodeInstruction(Pcodes.epReturnFromSubroutine, out unused, TheScanner.GetCurrentLineNumber());
            TheFunctionCode.AddPcodeOperandInteger(FuncArray.Length);
            StackDepth = StackDepth - (FuncArray.Length + 1); /* also pops retaddr */
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != 1)
            {
                // stack depth is wrong at end of function
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            TheFunctionCode.MaxStackDepth = MaxStackDepth;

            /* optimize stupid things away */
            TheFunctionCode.OptimizePcode();


#if true // TODO:experimental
            if (CILObject.EnableCIL && !suppressCILEmission)
            {
                DataTypes[] argsTypes = new DataTypes[FuncArray.Length];
                string[] argsNames = new string[FuncArray.Length];
                for (int i = 0; i < argsTypes.Length; i++)
                {
                    argsTypes[i] = FuncArray[i].ParameterType;
                    argsNames[i] = FuncArray[i].ParameterName;
                }
                CILAssembly cilAssembly = new CILAssembly();
                CILObject cilObject = new CILObject(
                    CodeCenter.ManagedFunctionLinker,
                    argsTypes,
                    argsNames,
                    GetExpressionsResultantType(TheExpressionThang),
                    TheExpressionThang,
                    cilAssembly);
                TheFunctionCode.cilObject = cilObject;
                cilAssembly.Finish();
            }
#endif


            /* it worked, so return the dang thing */
            FunctionOut = TheFunctionCode;
            ASTOut = TheExpressionThang;
            return CompileErrors.eCompileNoError;
        }
    }
}
