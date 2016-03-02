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
    /* syntactical errors that can occur while compiling */
    public enum CompileErrors
    {
        eCompileNoError,
        eCompileOutOfMemory,
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
        eCompileExpectedOpenParenOrEqual,
        eCompileExpectedThen,
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
        eCompileFloatOrDoubleRequiredForExponentiation,
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
        eCompileOperandMustBeFloatOrDouble,
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
        eCompileFunctionNameConflictsWithBuiltIn,
    }

    public enum KeywordsType
    {
        eExprKwrdFunc,
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
        eExprKwrdDup,
        eExprKwrdLoadsampleleft,
        eExprKwrdLoadsampleright,
        eExprKwrdLoadsample,
        eExprKwrdFor,
        eExprKwrdStep,
        eExprKwrdFloor,
        eExprKwrdCeil,
        eExprKwrdRound,
        eExprKwrdCosh,
        eExprKwrdSinh,
        eExprKwrdTanh,
        eExprKwrdClass,
        eExprKwrdStruct,
        eExprKwrdForeach,
        eExprKwrdVector,
        eExprKwrdLong,
        eExprKwrdLongarray,
    }

    /* this function is used for specifying information about a parameter */
    [StructLayout(LayoutKind.Auto)]
    public struct FunctionParamRec
    {
        public readonly string ParameterName;
        public readonly DataTypes ParameterType;

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
            new KeywordRec<KeywordsType>("bytearray", KeywordsType.eExprKwrdBytearray),
            new KeywordRec<KeywordsType>("ceil", KeywordsType.eExprKwrdCeil),
            new KeywordRec<KeywordsType>("class", KeywordsType.eExprKwrdClass),
            new KeywordRec<KeywordsType>("cos", KeywordsType.eExprKwrdCos),
            new KeywordRec<KeywordsType>("cosh", KeywordsType.eExprKwrdCosh),
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
            new KeywordRec<KeywordsType>("fixed", KeywordsType.eExprKwrdFixed),
            new KeywordRec<KeywordsType>("fixedarray", KeywordsType.eExprKwrdFixedarray),
            new KeywordRec<KeywordsType>("float", KeywordsType.eExprKwrdFloat),
            new KeywordRec<KeywordsType>("floatarray", KeywordsType.eExprKwrdFloatarray),
            new KeywordRec<KeywordsType>("floor", KeywordsType.eExprKwrdFloor),
            new KeywordRec<KeywordsType>("for", KeywordsType.eExprKwrdFor),
            new KeywordRec<KeywordsType>("foreach", KeywordsType.eExprKwrdForeach),
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
            new KeywordRec<KeywordsType>("long", KeywordsType.eExprKwrdLong),
            new KeywordRec<KeywordsType>("longarray", KeywordsType.eExprKwrdLongarray),
            new KeywordRec<KeywordsType>("mod", KeywordsType.eExprKwrdMod),
            new KeywordRec<KeywordsType>("neg", KeywordsType.eExprKwrdNeg),
            new KeywordRec<KeywordsType>("not", KeywordsType.eExprKwrdNot),
            new KeywordRec<KeywordsType>("or", KeywordsType.eExprKwrdOr),
            new KeywordRec<KeywordsType>("pi", KeywordsType.eExprKwrdPi),
            new KeywordRec<KeywordsType>("print", KeywordsType.eExprKwrdPrint),
            new KeywordRec<KeywordsType>("resize", KeywordsType.eExprKwrdResize),
            new KeywordRec<KeywordsType>("resumable", KeywordsType.eExprKwrdResumable),
            new KeywordRec<KeywordsType>("round", KeywordsType.eExprKwrdRound),
            new KeywordRec<KeywordsType>("set", KeywordsType.eExprKwrdSet),
            new KeywordRec<KeywordsType>("sign", KeywordsType.eExprKwrdSign),
            new KeywordRec<KeywordsType>("sin", KeywordsType.eExprKwrdSin),
            new KeywordRec<KeywordsType>("single", KeywordsType.eExprKwrdSingle),
            new KeywordRec<KeywordsType>("singlearray", KeywordsType.eExprKwrdSinglearray),
            new KeywordRec<KeywordsType>("sinh", KeywordsType.eExprKwrdSinh),
            new KeywordRec<KeywordsType>("sqr", KeywordsType.eExprKwrdSqr),
            new KeywordRec<KeywordsType>("sqrt", KeywordsType.eExprKwrdSqrt),
            new KeywordRec<KeywordsType>("step", KeywordsType.eExprKwrdStep),
            new KeywordRec<KeywordsType>("struct", KeywordsType.eExprKwrdStruct),
            new KeywordRec<KeywordsType>("tan", KeywordsType.eExprKwrdTan),
            new KeywordRec<KeywordsType>("tanh", KeywordsType.eExprKwrdTanh),
            new KeywordRec<KeywordsType>("then", KeywordsType.eExprKwrdThen),
            new KeywordRec<KeywordsType>("to", KeywordsType.eExprKwrdTo),
            new KeywordRec<KeywordsType>("true", KeywordsType.eExprKwrdTrue),
            new KeywordRec<KeywordsType>("var", KeywordsType.eExprKwrdVar),
            new KeywordRec<KeywordsType>("vector", KeywordsType.eExprKwrdVector),
            new KeywordRec<KeywordsType>("void", KeywordsType.eExprKwrdVoid),
            new KeywordRec<KeywordsType>("while", KeywordsType.eExprKwrdWhile),
            new KeywordRec<KeywordsType>("xor", KeywordsType.eExprKwrdXor),
        };

        [StructLayout(LayoutKind.Auto)]
        private struct CompileErrorRec
        {
            public readonly CompileErrors ErrorCode;
            public readonly string Message;

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
            new CompileErrorRec(CompileErrors.eCompileExpectedOpenParenOrEqual,
                "Expected '(' or '='"),
            new CompileErrorRec(CompileErrors.eCompileExpectedThen,
                "Expected 'then'"),
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
            new CompileErrorRec(CompileErrors.eCompileFloatOrDoubleRequiredForExponentiation,
                "Operands for exponentiation must be floats or doubles"),
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
            new CompileErrorRec(CompileErrors.eCompileOperandMustBeFloatOrDouble,
                "Operand must be float or double"),
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
            new CompileErrorRec(CompileErrors.eCompileFunctionNameConflictsWithBuiltIn,
                "Declared function name conflicts with built-in function name"),
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
            SymbolListRec argNode = functionSymbol.FunctionArgList;
            while (argNode != null)
            {
                SymbolRec arg = argNode.First;
                argsTypes.Add(arg.VariableDataType);
                argsNames.Add(arg.SymbolName);
                argNode = argNode.Rest;
            }
            argsTypesOut = argsTypes.ToArray();
            argsNamesOut = argsNames.ToArray();
        }

        /* apply optimizations to the AST.  returns False if it runs out of memory. */
        private static void OptimizeAST(ref ASTExpression Expr)
        {
            bool DidSomething = true;
            while (DidSomething)
            {
                bool OneDidSomething;

                DidSomething = false;

                ASTExpression exprReplacement;
                Expr.FoldConst(out OneDidSomething, out exprReplacement);
                if (exprReplacement != null)
                {
                    Expr = exprReplacement;
                }
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
            List<ASTExpression> FunctionBodyRecords = new List<ASTExpression>();
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
                    ASTExpression FunctionBodyRecord;

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

                    Debug.Assert(FunctionBodyRecord != null);
                    ModuleIndices.Add(module);
                    SymbolTableEntriesForForm.Add(SymbolTableEntryForForm);
                    FunctionBodyRecords.Add(FunctionBodyRecord);
                    InitialLineNumbersOfForm.Add(InitialLineNumberOfForm);
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
                if (functionNamesUsed.ContainsKey(FunctionDeclarationSymbol.SymbolName))
                {
                    ErrorLineNumber = FunctionBodyRecords[i].LineNumber;
                    return CompileErrors.eCompileMultiplyDeclaredFunction;
                }
                functionNamesUsed.Add(FunctionDeclarationSymbol.SymbolName, false);

                List<ParserContext.FunctionSymbolRefInfo> symbols;
                if (FunctionRefSymbolList.TryGetValue(FunctionDeclarationSymbol.SymbolName, out symbols))
                {
                    foreach (ParserContext.FunctionSymbolRefInfo functionRef in symbols)
                    {
                        functionRef.symbol.SymbolBecomeFunction2(
                            FunctionDeclarationSymbol.FunctionArgList,
                            FunctionDeclarationSymbol.FunctionReturnType);
                    }
                    FunctionRefSymbolList.Remove(FunctionDeclarationSymbol.SymbolName);
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
                ASTExpression FunctionBodyRecord = FunctionBodyRecords[i];
                int InitialLineNumberOfForm = InitialLineNumbersOfForm[i];

                ErrorModuleIndex = module;

                /* SymbolTableEntryForForm will be the symbol table entry that */
                /* was added to the symbol table.  FunctionBodyRecord is either */
                /* an expression for a function or NIL if it was a prototype */

                Debug.Assert(!CodeCenter.CodeCenterHaveThisFunction(SymbolTableEntryForForm.SymbolName));

                /* step 1:  do type checking */
                DataTypes ResultingType;
                CompileErrors Error = FunctionBodyRecord.TypeCheck(
                    out ResultingType,
                    out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                /* check to see that resulting type matches declared type */
                if (!CanRightBeMadeToMatchLeft(SymbolTableEntryForForm.FunctionReturnType, ResultingType))
                {
                    ErrorLineNumber = InitialLineNumberOfForm;
                    return CompileErrors.eCompileTypeMismatch;
                }
                /* if it has to be promoted, then promote it */
                if (MustRightBePromotedToLeft(SymbolTableEntryForForm.FunctionReturnType, ResultingType))
                {
                    /* insert promotion operator above expression */
                    ASTExpression ReplacementExpr = PromoteTheExpression(
                        ResultingType,
                        SymbolTableEntryForForm.FunctionReturnType,
                        FunctionBodyRecord,
                        InitialLineNumberOfForm);
                    FunctionBodyRecord = ReplacementExpr;
                    /* sanity check */
                    Error = FunctionBodyRecord.TypeCheck(
                        out ResultingType,
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        // type promotion caused failure
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (ResultingType != SymbolTableEntryForForm.FunctionReturnType)
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
                ASTExpression FunctionBodyRecord = FunctionBodyRecords[i];
                int InitialLineNumberOfForm = InitialLineNumbersOfForm[i];

                string Filename = Filenames[module];
                object Signature = Signatures[module];

                ErrorModuleIndex = module;

                Debug.Assert(!CodeCenter.CodeCenterHaveThisFunction(SymbolTableEntryForForm.SymbolName));

                /* step 1.5:  optimize the AST */
                OptimizeAST(ref FunctionBodyRecord);

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
                SymbolListRec FormalArgumentListScan = SymbolTableEntryForForm.FunctionArgList;
                while (FormalArgumentListScan != null)
                {
                    SymbolRec TheFormalArg = FormalArgumentListScan.First;
                    StackDepth++; /* allocate first */
                    MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                    TheFormalArg.SymbolVariableStackLocation = StackDepth; /* remember */
                    ArgumentIndex++;
                    FormalArgumentListScan = FormalArgumentListScan.Rest;
                }
                /* reserve return address spot */
                StackDepth++;
                MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                /* allocate the function code */
                PcodeRec TheFunctionCode = new PcodeRec();
                FunctionBodyRecord.PcodeGen(
                    TheFunctionCode,
                    ref StackDepth,
                    ref MaxStackDepth);
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
                    SymbolTableEntryForForm.SymbolName,
                    SymbolTableEntryForForm.FunctionArgList,
                    TheFunctionCode,
                    SymbolTableEntryForForm.FunctionReturnType,
                    Filename);

                TheWholeFunctionThings[i] = TheWholeFunctionThing;

                if (CILObject.EnableCIL)
                {
                    DataTypes[] argsTypes;
                    string[] argsNames;
                    SymbolicArgListToType(SymbolTableEntryForForm, out argsTypes, out argsNames);
                    CILObject cilObject = new CILObject(
                        CodeCenter.ManagedFunctionLinker,
                        argsTypes,
                        argsNames,
                        SymbolTableEntryForForm.FunctionReturnType,
                        FunctionBodyRecord,
                        cilAssembly,
                        false/*argsByRef*/);
                    TheWholeFunctionThing.CILObject = cilObject;
                }
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
                    SymbolTableEntriesForForm[i].SymbolName,
                    new FunctionSignature(
                        argsTypes,
                        SymbolTableEntriesForForm[i].FunctionReturnType));
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
            out ASTExpression ASTOut)
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
                bool f = TheSymbolTable.Add(functionSignature);
                Debug.Assert(f); // should never fail (due to duplicate) since CodeCenter.RetainedFunctionSignatures is unique-keyed
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
                TheParameter.SymbolVariableStackLocation = StackDepth;
                if (!TheSymbolTable.Add(TheParameter)) // our own code should never pass in a formal arg list with duplicates
                {
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

            ASTExpressionList ListOfExpressions;
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
            ASTExpression TheExpressionThang = new ASTExpression(
                ListOfExpressions,
                TheScanner.GetCurrentLineNumber());

            /* make sure there is nothing after it */
            TokenRec<KeywordsType> Token = TheScanner.GetNextToken();
            if (Token.GetTokenType() != TokenTypes.eTokenEndOfInput)
            {
                ErrorLineNumber = TheScanner.GetCurrentLineNumber();
                return CompileErrors.eCompileInputBeyondEndOfFunction;
            }

            DataTypes ResultingType;
            Error = TheExpressionThang.TypeCheck(out ResultingType, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            OptimizeAST(ref TheExpressionThang);

            PcodeRec TheFunctionCode = new PcodeRec();

            TheExpressionThang.PcodeGen(
                TheFunctionCode,
                ref StackDepth,
                ref MaxStackDepth);
            Debug.Assert(StackDepth <= MaxStackDepth);

            ReturnTypeOut = TheExpressionThang.ResultType;


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
            // special function returns without popping args -- so that args can be have in/out behavior
            TheFunctionCode.AddPcodeOperandInteger(0);
            StackDepth -= 1; /* pop retaddr */
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != 1 + FuncArray.Length)
            {
                // stack depth is wrong at end of function
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            TheFunctionCode.MaxStackDepth = MaxStackDepth;

            /* optimize stupid things away */
            TheFunctionCode.OptimizePcode();


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
                    TheExpressionThang.ResultType,
                    TheExpressionThang,
                    cilAssembly,
                    true/*argsByRef*/); // args by ref true for special functions to permit multiple return values
                TheFunctionCode.cilObject = cilObject;
                cilAssembly.Finish();
            }


            /* it worked, so return the dang thing */
            FunctionOut = TheFunctionCode;
            ASTOut = TheExpressionThang;
            return CompileErrors.eCompileNoError;
        }
    }
}
