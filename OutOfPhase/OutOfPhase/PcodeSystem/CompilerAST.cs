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
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace OutOfPhase
{
    public static partial class Compiler
    {
        public class ILGenContext
        {
            public readonly ILGenerator ilGenerator;
            public readonly Dictionary<string, int> argumentTable;
            public readonly Dictionary<SymbolRec, LocalBuilder> variableTable;
            public readonly ManagedFunctionLinkerRec managedFunctionLinker;
            public readonly bool argsByRef;
            public readonly Dictionary<string, LocalBuilder> localArgMap;

            public ILGenContext(
                ILGenerator ilGenerator,
                Dictionary<string, int> argumentTable,
                Dictionary<SymbolRec, LocalBuilder> variableTable,
                ManagedFunctionLinkerRec managedFunctionLinker,
                bool argsByRef,
                Dictionary<string, LocalBuilder> localArgMap)
            {
                this.ilGenerator = ilGenerator;
                this.argumentTable = argumentTable;
                this.variableTable = variableTable;
                this.managedFunctionLinker = managedFunctionLinker;
                this.argsByRef = argsByRef;
                this.localArgMap = localArgMap;
            }
        }




        public abstract class ASTBase
        {
            public abstract int LineNumber { get; }

            public abstract DataTypes ResultType { get; } // valid only after TypeCheck()

            public abstract CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber);

            public abstract void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth);

            public abstract void ILGen(
                CILObject cilObject,
                ILGenContext context);

            // if constant folding succeeds, possibly replace original generic expression with ReplacementExpr.
            // ReplacementExpr==null means do not replace.
            public abstract void FoldConst(
                out bool DidSomething,
                out ASTExpression ReplacementExpr);
        }




        public class ASTArrayDeclaration : ASTBase
        {
            private SymbolRec symbol;
            private ASTExpression sizeExpression;
            private readonly int lineNumber;

            private DataTypes resultType;

            public SymbolRec Symbol { get { return symbol; } }
            public ASTExpression SizeExpression { get { return sizeExpression; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            /* create a new array variable constructor node. this should ONLY be used for */
            /* creating arrays. variables that are initialized with an array that results from */
            /* an expression should use ASTVariableDeclaration.  */
            public ASTArrayDeclaration(
                SymbolRec symbol,
                ASTExpression sizeExpression,
                int lineNumber)
            {
                if ((symbol.VariableDataType != DataTypes.eArrayOfBoolean)
                    && (symbol.VariableDataType != DataTypes.eArrayOfByte)
                    && (symbol.VariableDataType != DataTypes.eArrayOfInteger)
                    && (symbol.VariableDataType != DataTypes.eArrayOfFloat)
                    && (symbol.VariableDataType != DataTypes.eArrayOfDouble))
                {
                    // variable type is NOT an array
                    Debug.Assert(false);
                    throw new ArgumentException();
                }

                this.lineNumber = lineNumber;
                this.symbol = symbol;
                this.sizeExpression = sizeExpression;
            }

            /* type check the array variable constructor node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                if (this.Symbol.Kind != SymbolKind.Variable)
                {
                    ErrorLineNumber = this.LineNumber;
                    return CompileErrors.eCompileExpectedVariable;
                }

                DataTypes TheVariableType = this.Symbol.VariableDataType;
                switch (TheVariableType)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                    case DataTypes.eInteger:
                    case DataTypes.eFloat:
                    case DataTypes.eDouble:
                        ErrorLineNumber = this.LineNumber;
                        return CompileErrors.eCompileExpectedArrayType;
                    case DataTypes.eArrayOfBoolean:
                    case DataTypes.eArrayOfByte:
                    case DataTypes.eArrayOfInteger:
                    case DataTypes.eArrayOfFloat:
                    case DataTypes.eArrayOfDouble:
                        break;
                }

                DataTypes SizeSpecifierType;
                CompileErrors Error = this.SizeExpression.TypeCheck(
                    out SizeSpecifierType,
                    out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (SizeSpecifierType != DataTypes.eInteger)
                {
                    ErrorLineNumber = this.LineNumber;
                    return CompileErrors.eCompileArraySizeSpecMustBeInteger;
                }

                ResultingDataType = this.resultType = TheVariableType;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                Pcodes OpcodeToGenerate;

                int StackDepth = StackDepthParam;

                /* evaluate size expression, leaving result on stack */
                this.SizeExpression.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 1)
                {
                    // CodeGenExpression made stack depth error
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* construct array operation.  this pops size, but pushes new array reference */
                switch (this.Symbol.VariableDataType)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eArrayOfBoolean:
                    case DataTypes.eArrayOfByte:
                        OpcodeToGenerate = Pcodes.epMakeByteArray;
                        break;
                    case DataTypes.eArrayOfInteger:
                        OpcodeToGenerate = Pcodes.epMakeIntegerArray;
                        break;
                    case DataTypes.eArrayOfFloat:
                        OpcodeToGenerate = Pcodes.epMakeFloatArray;
                        break;
                    case DataTypes.eArrayOfDouble:
                        OpcodeToGenerate = Pcodes.epMakeDoubleArray;
                        break;
                }
                int unused;
                FuncCode.AddPcodeInstruction(OpcodeToGenerate, out unused, this.LineNumber);

                /* now make the symbol table entry remember where on the stack it is. */
                this.Symbol.SymbolVariableStackLocation = StackDepth;

                /* duplicate the value for something to return */
                FuncCode.AddPcodeInstruction(Pcodes.epDuplicate, out unused, this.LineNumber);
                StackDepth++;
                MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                if (StackDepth != StackDepthParam + 2)
                {
                    // stack depth error after duplicating value for return
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                Debug.Assert(!context.variableTable.ContainsKey(this.Symbol));
                LocalBuilder localVariable = context.ilGenerator.DeclareLocal(
                    PcodeMarshal.GetManagedType(this.Symbol.VariableDataType));
                context.variableTable.Add(this.Symbol, localVariable);

                /* evaluate size expression, leaving result on stack */
                this.SizeExpression.ILGen(
                    cilObject,
                    context);

                /* construct array operation.  this pops size, but pushes new array reference */
                switch (this.Symbol.VariableDataType)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eArrayOfBoolean:
                    case DataTypes.eArrayOfByte:
                        context.ilGenerator.Emit(OpCodes.Newarr, typeof(byte));
                        context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleByte).GetConstructor(new Type[] { typeof(byte[]) }));
                        break;
                    case DataTypes.eArrayOfInteger:
                        context.ilGenerator.Emit(OpCodes.Newarr, typeof(int));
                        context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleInt32).GetConstructor(new Type[] { typeof(int[]) }));
                        break;
                    case DataTypes.eArrayOfFloat:
                        context.ilGenerator.Emit(OpCodes.Newarr, typeof(float));
                        context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleFloat).GetConstructor(new Type[] { typeof(float[]) }));
                        break;
                    case DataTypes.eArrayOfDouble:
                        context.ilGenerator.Emit(OpCodes.Newarr, typeof(double));
                        context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleDouble).GetConstructor(new Type[] { typeof(double[]) }));
                        break;
                }

                context.ilGenerator.Emit(OpCodes.Dup);
                context.ilGenerator.Emit(OpCodes.Stloc, localVariable);
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                ASTExpression sizeExpressionReplacement;
                this.SizeExpression.FoldConst(out DidSomething, out sizeExpressionReplacement);
                if (sizeExpressionReplacement != null)
                {
                    this.sizeExpression = sizeExpressionReplacement;
                }

                replacementExpr = null;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("var {0}({1})", symbol, sizeExpression);
            }
#endif
        }




        public class ASTAssignment : ASTBase
        {
            private ASTExpression lValueGenerator;
            private ASTExpression objectValue;
            private readonly int lineNumber;

            private DataTypes resultType;

            public ASTExpression LValueGenerator { get { return lValueGenerator; } }
            public ASTExpression ObjectValue { get { return objectValue; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            public ASTAssignment(
                ASTExpression LeftValue,
                ASTExpression RightValue,
                int LineNumber)
            {
                this.lineNumber = LineNumber;
                this.lValueGenerator = LeftValue;
                this.objectValue = RightValue;
            }

            /* type check the assignment node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                DataTypes RValueType;
                Error = this.ObjectValue.TypeCheck(out RValueType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                DataTypes LValueType;
                Error = this.LValueGenerator.TypeCheck(out LValueType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                if (!CanRightBeMadeToMatchLeft(LValueType, RValueType))
                {
                    ErrorLineNumber = this.LineNumber;
                    return CompileErrors.eCompileTypeMismatch;
                }

                if (MustRightBePromotedToLeft(LValueType, RValueType))
                {
                    /* insert promotion operator above right hand side */
                    ASTExpression ReplacementRValue = PromoteTheExpression(RValueType, LValueType,
                        this.ObjectValue, this.LineNumber);
                    this.objectValue = ReplacementRValue;
                    /* sanity check */
                    Error = this.ObjectValue.TypeCheck(out RValueType, out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        // type promotion caused failure"));
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (RValueType != LValueType)
                    {
                        // after type promotion, types are no longer the same
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }

                /* make sure it's a valid lvalue */
                if (!this.LValueGenerator.IsValidLValue)
                {
                    ErrorLineNumber = this.LineNumber;
                    return CompileErrors.eCompileInvalidLValue;
                }

                ResultingDataType = this.resultType = LValueType;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int StackDepth = StackDepthParam;

                /* find out what kind of assignment operation to perform */
                /*  - for single variables:  it stores the value from top of stack into the */
                /*    appropriate index, but does not pop the value. */
                /*  - for array variables:  the array index is computed and pushed on the */
                /*    stack.  then the array index is popped, and the element value is stored. */
                /*    the element value is NOT popped. */
                switch (this.LValueGenerator.Kind)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case ExprKind.eExprOperand:
                        {
                            /* evaluate the value expression.  this leaves the result on the stack */
                            this.ObjectValue.PcodeGen(
                                FuncCode,
                                ref StackDepth,
                                ref MaxStackDepth);
                            Debug.Assert(StackDepth <= MaxStackDepth);
                            if (StackDepth != StackDepthParam + 1)
                            {
                                // CodeGenExpression made stack depth bad
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }

                            /* get the variable being assigned to */
                            ASTOperand TheOperand = this.LValueGenerator.InnerOperand;
                            SymbolRec TheVariable = TheOperand.Symbol;

                            /* generate the assignment opcode word */
                            Pcodes TheAssignmentOpcode;
                            switch (TheVariable.VariableDataType)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case DataTypes.eBoolean:
                                case DataTypes.eInteger:
                                    TheAssignmentOpcode = Pcodes.epStoreIntegerOnStack;
                                    break;
                                case DataTypes.eFloat:
                                    TheAssignmentOpcode = Pcodes.epStoreFloatOnStack;
                                    break;
                                case DataTypes.eDouble:
                                    TheAssignmentOpcode = Pcodes.epStoreDoubleOnStack;
                                    break;
                                case DataTypes.eArrayOfBoolean:
                                case DataTypes.eArrayOfByte:
                                    TheAssignmentOpcode = Pcodes.epStoreArrayOfByteOnStack;
                                    break;
                                case DataTypes.eArrayOfInteger:
                                    TheAssignmentOpcode = Pcodes.epStoreArrayOfInt32OnStack;
                                    break;
                                case DataTypes.eArrayOfFloat:
                                    TheAssignmentOpcode = Pcodes.epStoreArrayOfFloatOnStack;
                                    break;
                                case DataTypes.eArrayOfDouble:
                                    TheAssignmentOpcode = Pcodes.epStoreArrayOfDoubleOnStack;
                                    break;
                            }
                            int unused;
                            FuncCode.AddPcodeInstruction(TheAssignmentOpcode, out unused, this.LineNumber);

                            /* generate the assignment operand (destination variable index) */
                            /* stack offsets are negative. */
                            FuncCode.AddPcodeOperandInteger(TheVariable.SymbolVariableStackLocation - StackDepth);
                        }
                        break;

                    case ExprKind.eExprBinaryOperator:
                        {
                            ASTBinaryOperation TheBinaryOperator = this.LValueGenerator.InnerBinaryOperator;
                            ASTExpression LeftOperand = TheBinaryOperator.LeftArg;
                            ASTExpression RightOperand = TheBinaryOperator.RightArg;
                            /* the left operand is the array reference generator */
                            /* the right operand is the array index generator. */

                            /* 1. evaluate the expression that's going to be assigned, leave on stack */
                            /* 2. evaluate array reference, leaving it on the stack */
                            /* 3. evaluate array subscript, leaving it on the stack */
                            /* 4. do assignment which */
                            /*     - pops subscript */
                            /*     - stores into array thing */
                            /*     - value is left on stack */

                            /* 1.  evaluate the value expression */
                            this.ObjectValue.PcodeGen(
                                FuncCode,
                                ref StackDepth,
                                ref MaxStackDepth);
                            Debug.Assert(StackDepth <= MaxStackDepth);
                            if (StackDepth != StackDepthParam + 1)
                            {
                                // eval array element new value messed up stack
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }

                            /* 2.  evaluate array reference */
                            LeftOperand.PcodeGen(
                                FuncCode,
                                ref StackDepth,
                                ref MaxStackDepth);
                            Debug.Assert(StackDepth <= MaxStackDepth);
                            if (StackDepth != StackDepthParam + 2)
                            {
                                // eval array reference messed up stack
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }

                            /* 3.  evaluate array subscript */
                            RightOperand.PcodeGen(
                                FuncCode,
                                ref StackDepth,
                                ref MaxStackDepth);
                            Debug.Assert(StackDepth <= MaxStackDepth);
                            if (StackDepth != StackDepthParam + 3)
                            {
                                // eval array subscript messed up stack
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }

                            /* 4.  generate the opcode for storing into an array */
                            Pcodes TheAssignmentOpcode;
                            switch (LeftOperand.ResultType)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case DataTypes.eArrayOfBoolean:
                                case DataTypes.eArrayOfByte:
                                    TheAssignmentOpcode = Pcodes.epStoreByteIntoArray2;
                                    break;
                                case DataTypes.eArrayOfInteger:
                                    TheAssignmentOpcode = Pcodes.epStoreIntegerIntoArray2;
                                    break;
                                case DataTypes.eArrayOfFloat:
                                    TheAssignmentOpcode = Pcodes.epStoreFloatIntoArray2;
                                    break;
                                case DataTypes.eArrayOfDouble:
                                    TheAssignmentOpcode = Pcodes.epStoreDoubleIntoArray2;
                                    break;
                            }
                            int unused;
                            FuncCode.AddPcodeInstruction(TheAssignmentOpcode, out unused, this.LineNumber);
                            StackDepth -= 2; /* popping the array subscript */
                            if (StackDepth != StackDepthParam + 1)
                            {
                                // eval store into array internal stack messing
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                        }
                        break;
                }
                if (StackDepth != StackDepthParam + 1)
                {
                    // after assignment, stack state is bad
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                /* find out what kind of assignment operation to perform */
                /*  - for single variables:  it stores the value from top of stack into the */
                /*    appropriate index, but does not pop the value. */
                /*  - for array variables:  the array index is computed and pushed on the */
                /*    stack.  then the array index is popped, and the element value is stored. */
                /*    the element value is NOT popped. */
                switch (this.LValueGenerator.Kind)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case ExprKind.eExprOperand:
                        {
                            /* evaluate the value expression.  this leaves the result on the stack */
                            this.ObjectValue.ILGen(
                                cilObject,
                                context);

                            /* get the variable being assigned to */
                            ASTOperand TheOperand = this.LValueGenerator.InnerOperand;
                            SymbolRec TheVariable = TheOperand.Symbol;
                            switch (TheVariable.VariableDataType)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();

                                case DataTypes.eBoolean:
                                case DataTypes.eInteger:
                                case DataTypes.eFloat:
                                case DataTypes.eDouble:

                                case DataTypes.eArrayOfBoolean:
                                case DataTypes.eArrayOfByte:
                                case DataTypes.eArrayOfInteger:
                                case DataTypes.eArrayOfFloat:
                                case DataTypes.eArrayOfDouble:

                                    if (context.variableTable.ContainsKey(TheVariable)) // local variables can mask arguments
                                    {
                                        LocalBuilder localVariable = context.variableTable[TheVariable];
                                        context.ilGenerator.Emit(OpCodes.Dup);
                                        context.ilGenerator.Emit(OpCodes.Stloc, localVariable);
                                    }
                                    else if (context.argumentTable.ContainsKey(TheVariable.SymbolName))
                                    {
                                        Debug.Assert(!context.argsByRef);
                                        context.ilGenerator.Emit(OpCodes.Dup);
                                        int argIndex = context.argumentTable[TheVariable.SymbolName];
                                        context.ilGenerator.Emit(OpCodes.Starg, argIndex);
                                    }
                                    else if (context.localArgMap.ContainsKey(TheVariable.SymbolName))
                                    {
                                        Debug.Assert(context.argsByRef);
                                        context.ilGenerator.Emit(OpCodes.Dup);
                                        context.ilGenerator.Emit(OpCodes.Stloc, context.localArgMap[TheVariable.SymbolName]);
                                    }
                                    else
                                    {
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    }
                                    break;
                            }
                        }
                        break;

                    case ExprKind.eExprBinaryOperator:
                        {
                            ASTBinaryOperation TheBinaryOperator = this.LValueGenerator.InnerBinaryOperator;
                            ASTExpression LeftOperand = TheBinaryOperator.LeftArg;
                            ASTExpression RightOperand = TheBinaryOperator.RightArg;
                            /* the left operand is the array reference generator */
                            /* the right operand is the array index generator. */

                            // .NET CIL array semantics are different than the pcode scheme, so the order of
                            // clause evaluation here is different than that in CodeGenAssignment(). The language is
                            // sufficiently restrictive that code written to depend on evaluation order of array
                            // reference is unlikely, and performance is prioritized.

                            /* 1 [was 2].  evaluate array reference */
                            LeftOperand.ILGen(
                                cilObject,
                                context);
                            // dereference the array handle
                            switch (LeftOperand.ResultType)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case DataTypes.eArrayOfBoolean:
                                case DataTypes.eArrayOfByte:
                                    context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleByte).GetField("bytes"));
                                    break;
                                case DataTypes.eArrayOfInteger:
                                    context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleInt32).GetField("ints"));
                                    break;
                                case DataTypes.eArrayOfFloat:
                                    context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleFloat).GetField("floats"));
                                    break;
                                case DataTypes.eArrayOfDouble:
                                    context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleDouble).GetField("doubles"));
                                    break;
                            }

                            /* 2 [was 3].  evaluate array subscript */
                            RightOperand.ILGen(
                                cilObject,
                                context);

                            /* 3 [was 1].  evaluate the value expression */
                            this.ObjectValue.ILGen(
                                cilObject,
                                context);

                            // because we leave rval on stack (and stelem doesn't), need to save a copy in
                            // a scratch variable
                            LocalBuilder scratchVariable = context.ilGenerator.DeclareLocal(
                                PcodeMarshal.GetManagedType(this.ObjectValue.ResultType));
                            context.ilGenerator.Emit(OpCodes.Dup, scratchVariable);
                            context.ilGenerator.Emit(OpCodes.Stloc, scratchVariable);

                            // store to array
                            switch (LeftOperand.ResultType)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case DataTypes.eArrayOfBoolean:
                                case DataTypes.eArrayOfByte:
                                    context.ilGenerator.Emit(OpCodes.Stelem_I1);
                                    break;
                                case DataTypes.eArrayOfInteger:
                                    context.ilGenerator.Emit(OpCodes.Stelem_I4);
                                    break;
                                case DataTypes.eArrayOfFloat:
                                    context.ilGenerator.Emit(OpCodes.Stelem_R4);
                                    break;
                                case DataTypes.eArrayOfDouble:
                                    context.ilGenerator.Emit(OpCodes.Stelem_R8);
                                    break;
                            }

                            // reload the value for return
                            context.ilGenerator.Emit(OpCodes.Ldloc, scratchVariable);
                        }
                        break;
                }
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression ReplacementExpr)
            {
                bool WeDidSomething = false;
                bool TheyDidSomething;

                ASTExpression lValueReplacement;
                this.LValueGenerator.FoldConst(out TheyDidSomething, out lValueReplacement);
                if (lValueReplacement != null)
                {
                    this.lValueGenerator = lValueReplacement;
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                ASTExpression rValueReplacement;
                this.ObjectValue.FoldConst(out TheyDidSomething, out rValueReplacement);
                if (rValueReplacement != null)
                {
                    this.objectValue = rValueReplacement;
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                DidSomething = WeDidSomething;
                ReplacementExpr = null;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("set {0} := {1}", lValueGenerator, objectValue);
            }
#endif
        }




        /* binary operator operations */
        public enum BinaryOperatorKind
        {
            eInvalid,

            eBinaryAnd,
            eBinaryOr,
            eBinaryXor,
            eBinaryLessThan,
            eBinaryLessThanOrEqual,
            eBinaryGreaterThan,
            eBinaryGreaterThanOrEqual,
            eBinaryEqual,
            eBinaryNotEqual,
            eBinaryPlus,
            eBinaryMinus,
            eBinaryMultiplication,
            eBinaryImpreciseDivision,
            eBinaryIntegerDivision,
            eBinaryIntegerRemainder,
            eBinaryShiftLeft,
            eBinaryShiftRight,
            eBinaryArraySubscripting,
            eBinaryExponentiation,
            eBinaryResizeArray,
        }

        public class ASTBinaryOperation : ASTBase
        {
            private readonly BinaryOperatorKind kind;
            private ASTExpression leftArg;
            private ASTExpression rightArg;
            private readonly int lineNumber;

            private DataTypes resultType;

            public BinaryOperatorKind Kind { get { return kind; } }
            public ASTExpression LeftArg { get { return leftArg; } }
            public ASTExpression RightArg { get { return rightArg; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            public ASTBinaryOperation(
                BinaryOperatorKind Operation,
                ASTExpression LeftArgument,
                ASTExpression RightArgument,
                int LineNumber)
            {
                this.lineNumber = LineNumber;
                this.kind = Operation;
                this.leftArg = LeftArgument;
                this.rightArg = RightArgument;
            }

            /* type check the binary operator node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

            Restart:
                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                DataTypes LeftOperandType;
                Error = this.LeftArg.TypeCheck(out LeftOperandType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                DataTypes RightOperandType;
                Error = this.RightArg.TypeCheck(out RightOperandType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                /* do type checking and promotion.  return type determination is deferred */
                switch (this.Kind)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    /* operators capable of boolean, integer, single, double, and fixed args, */
                    /* which return a boolean result */
                    case BinaryOperatorKind.eBinaryEqual:
                    case BinaryOperatorKind.eBinaryNotEqual:
                        if (!CanRightBeMadeToMatchLeft(LeftOperandType, RightOperandType)
                            && !CanRightBeMadeToMatchLeft(RightOperandType, LeftOperandType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileTypeMismatch;
                        }
                        if ((IsScalarType(LeftOperandType) && !IsScalarType(RightOperandType))
                            || (!IsScalarType(LeftOperandType) && IsScalarType(RightOperandType)))
                        {
                            // IsItAScalarType error
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (!IsScalarType(LeftOperandType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileOperandsMustBeScalar;
                        }
                        /* do type promotion */
                        Error = PromoteTypeHelper(ref LeftOperandType, ref RightOperandType, out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }
                        break;

                    /* operators capable of boolean, integer, and fixed, */
                    /* which return the same type as the arguments */
                    case BinaryOperatorKind.eBinaryAnd:
                    case BinaryOperatorKind.eBinaryOr:
                    case BinaryOperatorKind.eBinaryXor:
                        if ((LeftOperandType != DataTypes.eBoolean) && (LeftOperandType != DataTypes.eInteger))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileTypeMismatch;
                        }
                        if (LeftOperandType != RightOperandType)
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileTypeMismatch;
                        }
                        break;

                    /* operators capable of integer, single, double, and fixed args, */
                    /* where the return is the same type as args */
                    case BinaryOperatorKind.eBinaryPlus:
                    case BinaryOperatorKind.eBinaryMinus:
                    case BinaryOperatorKind.eBinaryMultiplication:
                    /* FALL THROUGH! */

                    /* operators capable of integer, single, double, and fixed args, */
                    /* which return a boolean */
                    case BinaryOperatorKind.eBinaryLessThan:
                    case BinaryOperatorKind.eBinaryLessThanOrEqual:
                    case BinaryOperatorKind.eBinaryGreaterThan:
                    case BinaryOperatorKind.eBinaryGreaterThanOrEqual:
                        if (!CanRightBeMadeToMatchLeft(LeftOperandType, RightOperandType)
                            && !CanRightBeMadeToMatchLeft(RightOperandType, LeftOperandType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileTypeMismatch;
                        }
                        if ((IsScalarType(LeftOperandType) && !IsScalarType(RightOperandType))
                            || (!IsScalarType(LeftOperandType) && IsScalarType(RightOperandType)))
                        {
                            // IsItAScalarType error
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (!IsSequencedScalarType(LeftOperandType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileOperandsMustBeSequencedScalar;
                        }
                        /* do type promotion */
                        Error = PromoteTypeHelper(ref LeftOperandType, ref RightOperandType, out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }
                        break;

                    /* operators capable of single, double, and fixed args, */
                    /* where the return is a single, double, or fixed */
                    case BinaryOperatorKind.eBinaryImpreciseDivision:
                        if (LeftOperandType == DataTypes.eInteger)
                        {
                            this.leftArg = PromoteTheExpression(
                                LeftOperandType,
                                DataTypes.eDouble,
                                this.LeftArg,
                                this.LeftArg.LineNumber);
                            goto Restart;
                        }
                        if (RightOperandType == DataTypes.eInteger)
                        {
                            this.rightArg = PromoteTheExpression(
                                RightOperandType,
                                DataTypes.eDouble,
                                this.RightArg,
                                this.RightArg.LineNumber);
                            goto Restart;
                        }
                        if (!CanRightBeMadeToMatchLeft(LeftOperandType, RightOperandType)
                            && !CanRightBeMadeToMatchLeft(RightOperandType, LeftOperandType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileTypeMismatch;
                        }
                        if ((IsScalarType(LeftOperandType) && !IsScalarType(RightOperandType))
                            || (!IsScalarType(LeftOperandType) && IsScalarType(RightOperandType)))
                        {
                            // IsItAScalarType error
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (!IsSequencedScalarType(LeftOperandType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileOperandsMustBeSequencedScalar;
                        }
                        /* do type promotion */
                        Error = PromoteTypeHelper(ref LeftOperandType, ref RightOperandType, out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }
                        break;

                    /* operators capable of integers, returning integer results */
                    case BinaryOperatorKind.eBinaryIntegerDivision:
                    case BinaryOperatorKind.eBinaryIntegerRemainder:
                    case BinaryOperatorKind.eBinaryShiftLeft:
                    case BinaryOperatorKind.eBinaryShiftRight:
                        if ((LeftOperandType != DataTypes.eInteger) || (RightOperandType != DataTypes.eInteger))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileOperandsMustBeIntegers;
                        }
                        /* no type promotion is necessary */
                        break;

                    /* operators where the left argument must be an array and the right */
                    /* argument must be an integer, and the array's element type is returned */
                    case BinaryOperatorKind.eBinaryArraySubscripting:
                        if (RightOperandType != DataTypes.eInteger)
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileArraySubscriptMustBeInteger;
                        }
                        if (!IsIndexedType(LeftOperandType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileArrayRequiredForSubscription;
                        }
                        break;

                    /* operators where the arguments are double, */
                    /* and which return a double result */
                    case BinaryOperatorKind.eBinaryExponentiation:
                        DataTypes targetType;
                        if (CanRightBeMadeToMatchLeft(DataTypes.eFloat, LeftOperandType)
                            && CanRightBeMadeToMatchLeft(DataTypes.eFloat, RightOperandType))
                        {
                            targetType = DataTypes.eFloat;
                        }
                        else if (CanRightBeMadeToMatchLeft(DataTypes.eDouble, LeftOperandType)
                           && CanRightBeMadeToMatchLeft(DataTypes.eDouble, RightOperandType))
                        {
                            targetType = DataTypes.eDouble;
                        }
                        else
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileFloatOrDoubleRequiredForExponentiation;
                        }
                        /* force the promotion, if necessary */
                        if (LeftOperandType != targetType)
                        {
                            /* promote right operand to double, so left operand is a fake double */
                            DataTypes FakePromotionForcer = targetType;
                            Error = PromoteTypeHelper(ref LeftOperandType, ref FakePromotionForcer, out ErrorLineNumber);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                return Error;
                            }
                            if ((FakePromotionForcer != targetType) || (LeftOperandType != targetType))
                            {
                                // exponent convert to double promotion failed"));
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                        }
                        if (RightOperandType != targetType)
                        {
                            /* promote left operand to double, so right operand is a fake double */
                            DataTypes FakePromotionForcer = targetType;
                            Error = PromoteTypeHelper(ref FakePromotionForcer, ref RightOperandType, out ErrorLineNumber);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                return Error;
                            }
                            if ((FakePromotionForcer != targetType) || (RightOperandType != targetType))
                            {
                                // exponent conver to double promotion failed"));
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                        }
                        break;

                    /* operators where left must be an array type and right must be an integer */
                    /* and an array of the same type is returned */
                    case BinaryOperatorKind.eBinaryResizeArray:
                        if (!IsIndexedType(LeftOperandType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileArrayRequiredForResize;
                        }
                        if (RightOperandType != DataTypes.eInteger)
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileIntegerRequiredForResize;
                        }
                        break;
                }

                /* now, figure out what the return type should be */
                switch (this.Kind)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    /* operators capable of boolean, integer, single, double, and fixed args, */
                    /* which return a boolean result */
                    case BinaryOperatorKind.eBinaryEqual:
                    case BinaryOperatorKind.eBinaryNotEqual:
                        if (LeftOperandType != RightOperandType)
                        {
                            // operand types are not equivalent but should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (!IsScalarType(LeftOperandType))
                        {
                            // operand types are not scalar but should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eBoolean;
                        break;

                    /* operators capable of boolean, integer, single, double, and fixed args, */
                    /* which return the same type as the arguments */
                    case BinaryOperatorKind.eBinaryAnd:
                    case BinaryOperatorKind.eBinaryOr:
                    case BinaryOperatorKind.eBinaryXor:
                        if (LeftOperandType != RightOperandType)
                        {
                            // operand types are not equivalent but should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if ((LeftOperandType != DataTypes.eBoolean) && (LeftOperandType != DataTypes.eInteger))
                        {
                            // operand types are not what they should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = LeftOperandType;
                        break;

                    /* operators capable of integer, single, double, and fixed args, */
                    /* where the return is the same type as args */
                    case BinaryOperatorKind.eBinaryPlus:
                    case BinaryOperatorKind.eBinaryMinus:
                    case BinaryOperatorKind.eBinaryMultiplication:
                        if (LeftOperandType != RightOperandType)
                        {
                            // operand types are not equivalent but should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (!IsSequencedScalarType(LeftOperandType))
                        {
                            // operand types are not seq scalar but should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = LeftOperandType;
                        break;

                    /* operators capable of integer, single, double, and fixed args, */
                    /* where the return is a single, double, or fixed */
                    case BinaryOperatorKind.eBinaryImpreciseDivision:
                        if (LeftOperandType != RightOperandType)
                        {
                            //operand types are not equivalent but should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (!IsSequencedScalarType(LeftOperandType))
                        {
                            // operand types are not seq scalar but should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (LeftOperandType != DataTypes.eInteger)
                        {
                            ResultingDataType = LeftOperandType;
                        }
                        else
                        {
                            ResultingDataType = DataTypes.eDouble;
                        }
                        break;

                    /* operators capable of integer, single, double, and fixed args, */
                    /* which return a boolean */
                    case BinaryOperatorKind.eBinaryLessThan:
                    case BinaryOperatorKind.eBinaryLessThanOrEqual:
                    case BinaryOperatorKind.eBinaryGreaterThan:
                    case BinaryOperatorKind.eBinaryGreaterThanOrEqual:
                        if (LeftOperandType != RightOperandType)
                        {
                            // operand types are not equivalent but should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (!IsScalarType(LeftOperandType))
                        {
                            // operand types are not scalar but should be
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eBoolean;
                        break;

                    /* operators capable of integers, returning integer results */
                    case BinaryOperatorKind.eBinaryIntegerDivision:
                    case BinaryOperatorKind.eBinaryIntegerRemainder:
                        if ((LeftOperandType != DataTypes.eInteger) || (RightOperandType != DataTypes.eInteger))
                        {
                            // operands should be integers
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eInteger;
                        break;

                    /* operators where the left argument must be integer, single, double, fixed */
                    /* and the right argument must be integer, and it returns the same type */
                    /* as the left argument */
                    case BinaryOperatorKind.eBinaryShiftLeft:
                    case BinaryOperatorKind.eBinaryShiftRight:
                        if (RightOperandType != DataTypes.eInteger)
                        {
                            // right operand should be integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (!IsSequencedScalarType(LeftOperandType))
                        {
                            // left operand should be seq scalar
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = LeftOperandType;
                        break;

                    /* operators where the left argument must be an array and the right */
                    /* argument must be an integer, and the array's element type is returned */
                    case BinaryOperatorKind.eBinaryArraySubscripting:
                        if (RightOperandType != DataTypes.eInteger)
                        {
                            // right operand should be integer
                        }
                        switch (LeftOperandType)
                        {
                            default:
                                // spurious type occurred after array subscript typecheck filter
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eArrayOfBoolean:
                                ResultingDataType = DataTypes.eBoolean;
                                break;
                            case DataTypes.eArrayOfByte:
                            case DataTypes.eArrayOfInteger:
                                ResultingDataType = DataTypes.eInteger;
                                break;
                            case DataTypes.eArrayOfFloat:
                                ResultingDataType = DataTypes.eFloat;
                                break;
                            case DataTypes.eArrayOfDouble:
                                ResultingDataType = DataTypes.eDouble;
                                break;
                        }
                        break;

                    /* operators where the arguments are double, */
                    /* and which return a double result */
                    case BinaryOperatorKind.eBinaryExponentiation:
                        if ((LeftOperandType == DataTypes.eFloat) && (RightOperandType == DataTypes.eFloat))
                        {
                            ResultingDataType = DataTypes.eFloat;
                        }
                        else if ((LeftOperandType == DataTypes.eDouble) && (RightOperandType == DataTypes.eDouble))
                        {
                            ResultingDataType = DataTypes.eDouble;
                        }
                        else
                        {
                            // operands should be double
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        break;

                    /* operators where left must be an array type and right must be an integer */
                    /* and an array of the same type is returned */
                    case BinaryOperatorKind.eBinaryResizeArray:
                        if (!IsIndexedType(LeftOperandType))
                        {
                            // operand should be array
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (RightOperandType != DataTypes.eInteger)
                        {
                            // operand should be integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = LeftOperandType;
                        break;
                }

                resultType = ResultingDataType;
                return CompileErrors.eCompileNoError;
            }

            private CompileErrors PromoteTypeHelper(
                ref DataTypes LeftOperandType,
                ref DataTypes RightOperandType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

                ErrorLineNumber = -1;

                if (CanRightBeMadeToMatchLeft(LeftOperandType, RightOperandType)
                    && MustRightBePromotedToLeft(LeftOperandType, RightOperandType))
                {
                    /* we must promote the right operand to become the left operand type */
                    ASTExpression PromotedRightOperand = PromoteTheExpression(
                        RightOperandType/*orig*/,
                        LeftOperandType/*desired*/,
                        this.RightArg,
                        this.LineNumber);
                    this.rightArg = PromotedRightOperand;
                    /* sanity check */
                    Error = this.RightArg.TypeCheck(
                        out RightOperandType/*obtain new right type*/,
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        // type promotion caused failure
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (RightOperandType != LeftOperandType)
                    {
                        // after type promotion, types are no longer the same
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
                else if (CanRightBeMadeToMatchLeft(RightOperandType, LeftOperandType)
                    && MustRightBePromotedToLeft(RightOperandType, LeftOperandType))
                {
                    /* we must promote the left operand to become the right operand type */
                    ASTExpression PromotedLeftOperand = PromoteTheExpression(
                        LeftOperandType/*orig*/,
                        RightOperandType/*desired*/,
                        this.LeftArg,
                        this.LineNumber);
                    this.leftArg = PromotedLeftOperand;
                    /* sanity check */
                    Error = this.LeftArg.TypeCheck(
                        out LeftOperandType/*obtain new left type*/,
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        // type promotion caused failure
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (RightOperandType != LeftOperandType)
                    {
                        // after type promotion, types are no longer the same
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int StackDepth = StackDepthParam;

                /* generate code for left operand */
                this.LeftArg.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack depth error on left operand
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* generate code for the right operand */
                this.RightArg.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 2)
                {
                    // stack depth error on right operand
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* generate the opcode for performing the computation */
                Pcodes Opcode;
                switch (this.Kind)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case BinaryOperatorKind.eBinaryAnd:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryAnd]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epOperationBooleanAnd;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerAnd;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryOr:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryOr]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryOr]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epOperationBooleanOr;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerOr;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryXor:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryXor]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryXor]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epOperationBooleanNotEqual;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerXor;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryLessThan:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryLessThan]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryLessThan]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerLessThan;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatLessThan;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleLessThan;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryLessThanOrEqual:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryLessThanOrEqual]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryLessThanOrEqual]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerLessThanOrEqual;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatLessThanOrEqual;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleLessThanOrEqual;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryGreaterThan:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryGreaterThan]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryGreaterThan]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerGreaterThan;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatGreaterThan;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleGreaterThan;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryGreaterThanOrEqual:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryGreaterThanOrEqual]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryGreaterThanOrEqual]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerGreaterThanOrEqual;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatGreaterThanOrEqual;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleGreaterThanOrEqual;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryEqual:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryEqual]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryEqual]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epOperationBooleanEqual;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerEqual;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatEqual;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleEqual;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryNotEqual:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryNotEqual]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryNotEqual]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epOperationBooleanNotEqual;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerNotEqual;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatNotEqual;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleNotEqual;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryPlus:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryPlus]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryPlus]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerAdd;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatAdd;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleAdd;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryMinus:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryMinus]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryMinus]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerSubtract;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatSubtract;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleSubtract;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryMultiplication:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryMultiplication]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryMultiplication]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerMultiply;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatMultiply;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleMultiply;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryImpreciseDivision:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryImpreciseDivision]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryImpreciseDivision]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatDivide;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleDivide;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryIntegerDivision:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryIntegerDivision]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryIntegerDivision]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerDivide;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryIntegerRemainder:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryIntegerRemainder]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryIntegerRemainder]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerModulo;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryShiftLeft:
                        if (this.RightArg.ResultType != DataTypes.eInteger)
                        {
                            // CodeGenBinaryOperator[eBinaryShiftLeft]:  type check failure -- right operand isn't an integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryShiftLeft]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerShiftLeft;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryShiftRight:
                        if (this.RightArg.ResultType != DataTypes.eInteger)
                        {
                            // CodeGenBinaryOperator[eBinaryShiftRight]:  type check failure -- right operand isn't an integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                //CodeGenBinaryOperator[eBinaryShiftRight]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerShiftRight;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryArraySubscripting:
                        if (this.RightArg.ResultType != DataTypes.eInteger)
                        {
                            // CodeGenBinaryOperator[eBinaryArraySubscripting]:  type check failure -- right operand isn't an integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryArraySubscripting]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                                Opcode = Pcodes.epLoadByteFromArray2;
                                break;
                            case DataTypes.eArrayOfInteger:
                                Opcode = Pcodes.epLoadIntegerFromArray2;
                                break;
                            case DataTypes.eArrayOfFloat:
                                Opcode = Pcodes.epLoadFloatFromArray2;
                                break;
                            case DataTypes.eArrayOfDouble:
                                Opcode = Pcodes.epLoadDoubleFromArray2;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryExponentiation:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryExponentiation]:  type check failure -- args don't have type parity
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryExponentiation]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoublePowerF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoublePowerD;
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryResizeArray:
                        if (this.RightArg.ResultType != DataTypes.eInteger)
                        {
                            // CodeGenBinaryOperator[eBinaryResizeArray]:  type check failure -- right operand isn't an integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryResizeArray]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                                Opcode = Pcodes.epResizeByteArray2;
                                break;
                            case DataTypes.eArrayOfInteger:
                                Opcode = Pcodes.epResizeIntegerArray2;
                                break;
                            case DataTypes.eArrayOfFloat:
                                Opcode = Pcodes.epResizeFloatArray2;
                                break;
                            case DataTypes.eArrayOfDouble:
                                Opcode = Pcodes.epResizeDoubleArray2;
                                break;
                        }
                        break;
                }
                int unused;
                FuncCode.AddPcodeInstruction(Opcode, out unused, this.LineNumber);
                StackDepth--;
                if (StackDepth != StackDepthParam + 1)
                {
                    // post operator stack size is screwed up
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                /* generate code for left operand */
                this.LeftArg.ILGen(
                    cilObject,
                    context);

                /* generate code for the right operand */
                this.RightArg.ILGen(
                    cilObject,
                    context);

                /* generate the opcode for performing the computation */
                switch (this.Kind)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case BinaryOperatorKind.eBinaryAnd:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryAnd]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.And);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryOr:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryOr]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryOr]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Or);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryXor:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryXor]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryXor]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Xor);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryLessThan:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryLessThan]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryLessThan]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Clt);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryLessThanOrEqual:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryLessThanOrEqual]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryLessThanOrEqual]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Cgt);
                                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryGreaterThan:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryGreaterThan]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryGreaterThan]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Cgt);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryGreaterThanOrEqual:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryGreaterThanOrEqual]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryGreaterThanOrEqual]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Clt);
                                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryEqual:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryEqual]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryEqual]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryNotEqual:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryNotEqual]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryNotEqual]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryPlus:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryPlus]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryPlus]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Add);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryMinus:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryMinus]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryMinus]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Sub);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryMultiplication:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryMultiplication]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryMultiplication]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Mul);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryImpreciseDivision:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryImpreciseDivision]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryImpreciseDivision]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Div);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryIntegerDivision:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryIntegerDivision]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryIntegerDivision]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Div);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryIntegerRemainder:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryIntegerRemainder]:  type check failure -- operands are not the same type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryIntegerRemainder]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Rem);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryShiftLeft:
                        if (this.RightArg.ResultType != DataTypes.eInteger)
                        {
                            // CodeGenBinaryOperator[eBinaryShiftLeft]:  type check failure -- right operand isn't an integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryShiftLeft]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Shl);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryShiftRight:
                        if (this.RightArg.ResultType != DataTypes.eInteger)
                        {
                            // CodeGenBinaryOperator[eBinaryShiftRight]:  type check failure -- right operand isn't an integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                //CodeGenBinaryOperator[eBinaryShiftRight]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Shr);
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryArraySubscripting:
                        if (this.RightArg.ResultType != DataTypes.eInteger)
                        {
                            // CodeGenBinaryOperator[eBinaryArraySubscripting]:  type check failure -- right operand isn't an integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        {
                            // save subscript to dereference array handle
                            LocalBuilder scratchVariable = context.ilGenerator.DeclareLocal(typeof(int));
                            context.ilGenerator.Emit(OpCodes.Stloc, scratchVariable);
                            switch (this.LeftArg.ResultType)
                            {
                                default:
                                    // CodeGenBinaryOperator[eBinaryArraySubscripting]:  bad operand types
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case DataTypes.eArrayOfBoolean:
                                case DataTypes.eArrayOfByte:
                                    context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleByte).GetField("bytes"));
                                    break;
                                case DataTypes.eArrayOfInteger:
                                    context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleInt32).GetField("ints"));
                                    break;
                                case DataTypes.eArrayOfFloat:
                                    context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleFloat).GetField("floats"));
                                    break;
                                case DataTypes.eArrayOfDouble:
                                    context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleDouble).GetField("doubles"));
                                    break;
                            }
                            context.ilGenerator.Emit(OpCodes.Ldloc, scratchVariable);
                            switch (this.LeftArg.ResultType)
                            {
                                default:
                                    // CodeGenBinaryOperator[eBinaryArraySubscripting]:  bad operand types
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case DataTypes.eArrayOfBoolean:
                                case DataTypes.eArrayOfByte:
                                    context.ilGenerator.Emit(OpCodes.Ldelem_I1);
                                    break;
                                case DataTypes.eArrayOfInteger:
                                    context.ilGenerator.Emit(OpCodes.Ldelem_I4);
                                    break;
                                case DataTypes.eArrayOfFloat:
                                    context.ilGenerator.Emit(OpCodes.Ldelem_R4);
                                    break;
                                case DataTypes.eArrayOfDouble:
                                    context.ilGenerator.Emit(OpCodes.Ldelem_R8);
                                    break;
                            }
                        }
                        break;

                    case BinaryOperatorKind.eBinaryExponentiation:
                        if (this.RightArg.ResultType != this.LeftArg.ResultType)
                        {
                            // CodeGenBinaryOperator[eBinaryExponentiation]:  type check failure -- args don't have type parity
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        switch (this.LeftArg.ResultType)
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryExponentiation]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                // no need to coerce arguments from float to double -- semantics of CIL provide automatic up-conversion
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Pow", new Type[] { typeof(double), typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Pow", new Type[] { typeof(double), typeof(double) }));
                                break;
                        }
                        break;

                    case BinaryOperatorKind.eBinaryResizeArray:
                        Debug.Assert((this.LeftArg.ResultType == DataTypes.eArrayOfBoolean)
                            || (this.LeftArg.ResultType == DataTypes.eArrayOfByte)
                            || (this.LeftArg.ResultType == DataTypes.eArrayOfInteger)
                            || (this.LeftArg.ResultType == DataTypes.eArrayOfFloat)
                            || (this.LeftArg.ResultType == DataTypes.eArrayOfDouble));
                        if (this.RightArg.ResultType != DataTypes.eInteger)
                        {
                            // CodeGenBinaryOperator[eBinaryResizeArray]:  type check failure -- right operand isn't an integer
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        {
                            LocalBuilder newLength = context.ilGenerator.DeclareLocal(typeof(int));
                            context.ilGenerator.Emit(OpCodes.Stloc, newLength); // save new size to get at array handle
                            context.ilGenerator.Emit(OpCodes.Dup); // duplicate array handle for return value
                            context.ilGenerator.Emit(OpCodes.Ldloc, newLength);
                            context.ilGenerator.Emit(OpCodes.Callvirt, typeof(ArrayHandle).GetMethod("Resize"));
                        }
                        break;
                }
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                DataTypes LocalResultType;

                bool WeDidSomething = false;
                bool TheyDidSomething;
                ASTOperand NewOperand = null;

                replacementExpr = null;
                LocalResultType = this.resultType;

                ASTExpression leftArgReplacement;
                this.LeftArg.FoldConst(out TheyDidSomething, out leftArgReplacement);
                if (leftArgReplacement != null)
                {
                    this.leftArg = leftArgReplacement;
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                ASTExpression rightArgReplacement;
                this.RightArg.FoldConst(out TheyDidSomething, out rightArgReplacement);
                if (rightArgReplacement != null)
                {
                    this.rightArg = rightArgReplacement;
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                if ((this.LeftArg.Kind == ExprKind.eExprOperand)
                    && (this.RightArg.Kind == ExprKind.eExprOperand))
                {
                    ASTOperandKind LeftOperandType;
                    ASTOperandKind RightOperandType;

                    LeftOperandType = this.LeftArg.InnerOperand.Kind;
                    RightOperandType = this.RightArg.InnerOperand.Kind;
                    if (
                        ((LeftOperandType == ASTOperandKind.eASTOperandIntegerLiteral)
                        || (LeftOperandType == ASTOperandKind.eASTOperandBooleanLiteral)
                        || (LeftOperandType == ASTOperandKind.eASTOperandSingleLiteral)
                        || (LeftOperandType == ASTOperandKind.eASTOperandDoubleLiteral))
                        &&
                        ((RightOperandType == ASTOperandKind.eASTOperandIntegerLiteral)
                        || (RightOperandType == ASTOperandKind.eASTOperandBooleanLiteral)
                        || (RightOperandType == ASTOperandKind.eASTOperandSingleLiteral)
                        || (RightOperandType == ASTOperandKind.eASTOperandDoubleLiteral))
                        )
                    {
                        switch (this.Kind)
                        {
                            default:
                                // FoldConstBinaryOperator: unknown operator
                                Debug.Assert(false);
                                throw new InvalidOperationException();

                            case BinaryOperatorKind.eBinaryArraySubscripting:
                            case BinaryOperatorKind.eBinaryResizeArray:
                                break;

                            case BinaryOperatorKind.eBinaryAnd:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eBoolean:
                                        {
                                            bool LeftValue;
                                            bool RightValue;
                                            bool NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.BooleanLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.BooleanLiteralValue;
                                            NewValue = LeftValue && RightValue;
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue & RightValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryOr:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eBoolean:
                                        {
                                            bool LeftValue;
                                            bool RightValue;
                                            bool NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.BooleanLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.BooleanLiteralValue;
                                            NewValue = LeftValue || RightValue;
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue | RightValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryXor:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eBoolean:
                                        {
                                            bool LeftValue;
                                            bool RightValue;
                                            bool NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.BooleanLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.BooleanLiteralValue;
                                            NewValue = (!LeftValue != !RightValue);
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue ^ RightValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryLessThan:
                                {
                                    bool NewValue;

                                    switch (this.LeftArg.ResultType)
                                    {
                                        default:
                                            // FoldConstBinaryOperator: illegal operand type
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        case DataTypes.eInteger:
                                            {
                                                int LeftValue;
                                                int RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                                NewValue = LeftValue < RightValue;
                                            }
                                            break;
                                        case DataTypes.eFloat:
                                            {
                                                float LeftValue;
                                                float RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                                NewValue = LeftValue < RightValue;
                                            }
                                            break;
                                        case DataTypes.eDouble:
                                            {
                                                double LeftValue;
                                                double RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                                NewValue = LeftValue < RightValue;
                                            }
                                            break;
                                    }

                                    LocalResultType = DataTypes.eBoolean;

                                    NewOperand = new ASTOperand(
                                        NewValue,
                                        this.LineNumber);
                                }
                                break;

                            case BinaryOperatorKind.eBinaryLessThanOrEqual:
                                {
                                    bool NewValue;

                                    switch (this.LeftArg.ResultType)
                                    {
                                        default:
                                            // FoldConstBinaryOperator: illegal operand type
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        case DataTypes.eInteger:
                                            {
                                                int LeftValue;
                                                int RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                                NewValue = LeftValue <= RightValue;
                                            }
                                            break;
                                        case DataTypes.eFloat:
                                            {
                                                float LeftValue;
                                                float RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                                NewValue = LeftValue <= RightValue;
                                            }
                                            break;
                                        case DataTypes.eDouble:
                                            {
                                                double LeftValue;
                                                double RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                                NewValue = LeftValue <= RightValue;
                                            }
                                            break;
                                    }

                                    LocalResultType = DataTypes.eBoolean;

                                    NewOperand = new ASTOperand(
                                        NewValue,
                                        this.LineNumber);
                                }
                                break;

                            case BinaryOperatorKind.eBinaryGreaterThan:
                                {
                                    bool NewValue;

                                    switch (this.LeftArg.ResultType)
                                    {
                                        default:
                                            // FoldConstBinaryOperator: illegal operand type
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        case DataTypes.eInteger:
                                            {
                                                int LeftValue;
                                                int RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                                NewValue = LeftValue > RightValue;
                                            }
                                            break;
                                        case DataTypes.eFloat:
                                            {
                                                float LeftValue;
                                                float RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                                NewValue = LeftValue > RightValue;
                                            }
                                            break;
                                        case DataTypes.eDouble:
                                            {
                                                double LeftValue;
                                                double RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                                NewValue = LeftValue > RightValue;
                                            }
                                            break;
                                    }

                                    LocalResultType = DataTypes.eBoolean;

                                    NewOperand = new ASTOperand(
                                        NewValue,
                                        this.LineNumber);
                                }
                                break;

                            case BinaryOperatorKind.eBinaryGreaterThanOrEqual:
                                {
                                    bool NewValue;

                                    switch (this.LeftArg.ResultType)
                                    {
                                        default:
                                            // FoldConstBinaryOperator: illegal operand type
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        case DataTypes.eInteger:
                                            {
                                                int LeftValue;
                                                int RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                                NewValue = LeftValue >= RightValue;
                                            }
                                            break;
                                        case DataTypes.eFloat:
                                            {
                                                float LeftValue;
                                                float RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                                NewValue = LeftValue >= RightValue;
                                            }
                                            break;
                                        case DataTypes.eDouble:
                                            {
                                                double LeftValue;
                                                double RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                                NewValue = LeftValue >= RightValue;
                                            }
                                            break;
                                    }

                                    LocalResultType = DataTypes.eBoolean;

                                    NewOperand = new ASTOperand(
                                        NewValue,
                                        this.LineNumber);
                                }
                                break;

                            case BinaryOperatorKind.eBinaryEqual:
                                {
                                    bool NewValue;

                                    switch (this.LeftArg.ResultType)
                                    {
                                        default:
                                            // FoldConstBinaryOperator: illegal operand type
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        case DataTypes.eBoolean:
                                            {
                                                bool LeftValue;
                                                bool RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.BooleanLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.BooleanLiteralValue;
                                                NewValue = (!LeftValue == !RightValue);
                                            }
                                            break;
                                        case DataTypes.eInteger:
                                            {
                                                int LeftValue;
                                                int RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                                NewValue = LeftValue == RightValue;
                                            }
                                            break;
                                        case DataTypes.eFloat:
                                            {
                                                float LeftValue;
                                                float RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                                NewValue = LeftValue == RightValue;
                                            }
                                            break;
                                        case DataTypes.eDouble:
                                            {
                                                double LeftValue;
                                                double RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                                NewValue = LeftValue == RightValue;
                                            }
                                            break;
                                    }

                                    LocalResultType = DataTypes.eBoolean;

                                    NewOperand = new ASTOperand(
                                        NewValue,
                                        this.LineNumber);
                                }
                                break;

                            case BinaryOperatorKind.eBinaryNotEqual:
                                {
                                    bool NewValue;

                                    switch (this.LeftArg.ResultType)
                                    {
                                        default:
                                            // FoldConstBinaryOperator: illegal operand type
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        case DataTypes.eBoolean:
                                            {
                                                bool LeftValue;
                                                bool RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.BooleanLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.BooleanLiteralValue;
                                                NewValue = (!LeftValue != !RightValue);
                                            }
                                            break;
                                        case DataTypes.eInteger:
                                            {
                                                int LeftValue;
                                                int RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                                NewValue = LeftValue != RightValue;
                                            }
                                            break;
                                        case DataTypes.eFloat:
                                            {
                                                float LeftValue;
                                                float RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                                NewValue = LeftValue != RightValue;
                                            }
                                            break;
                                        case DataTypes.eDouble:
                                            {
                                                double LeftValue;
                                                double RightValue;

                                                LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                                RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                                NewValue = LeftValue != RightValue;
                                            }
                                            break;
                                    }

                                    LocalResultType = DataTypes.eBoolean;

                                    NewOperand = new ASTOperand(
                                        NewValue,
                                        this.LineNumber);
                                }
                                break;

                            case BinaryOperatorKind.eBinaryPlus:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue + RightValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;
                                            float NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                            NewValue = LeftValue + RightValue;
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;
                                            double NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                            NewValue = LeftValue + RightValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryMinus:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue - RightValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;
                                            float NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                            NewValue = LeftValue - RightValue;
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;
                                            double NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                            NewValue = LeftValue - RightValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryMultiplication:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue * RightValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;
                                            float NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                            NewValue = LeftValue * RightValue;
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;
                                            double NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                            NewValue = LeftValue * RightValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryImpreciseDivision:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            double NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = (double)LeftValue / (double)RightValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;
                                            float NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                            NewValue = LeftValue / RightValue;
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;
                                            double NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                            NewValue = LeftValue / RightValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryIntegerDivision:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type"));
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            /* if divisor is zero, don't optimize out, so that it */
                                            /* will generate an exception during execution. */
                                            if (RightValue != 0)
                                            {
                                                NewValue = LeftValue / RightValue;
                                                LocalResultType = DataTypes.eInteger;

                                                NewOperand = new ASTOperand(
                                                    NewValue,
                                                    this.LineNumber);
                                            }
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryIntegerRemainder:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            /* if divisor is zero, don't optimize out, so that it */
                                            /* will generate an exception during execution. */
                                            if (RightValue != 0)
                                            {
                                                NewValue = LeftValue % RightValue;
                                                LocalResultType = DataTypes.eInteger;

                                                NewOperand = new ASTOperand(
                                                    NewValue,
                                                    this.LineNumber);
                                            }
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryShiftLeft:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue << RightValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            int RightValue;
                                            float NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = (float)(LeftValue * Math.Pow(2, RightValue));
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            int RightValue;
                                            double NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue * Math.Pow(2, RightValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryShiftRight:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;
                                            int NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.IntegerLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue / (1 << RightValue);
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            int RightValue;
                                            float NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = (float)(LeftValue * Math.Pow(2, -RightValue));
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            int RightValue;
                                            double NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.IntegerLiteralValue;
                                            NewValue = LeftValue * Math.Pow(2, -RightValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;

                            case BinaryOperatorKind.eBinaryExponentiation:
                                switch (this.LeftArg.ResultType)
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;
                                            float NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.SingleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.SingleLiteralValue;
                                            NewValue = (float)Math.Pow(LeftValue, RightValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;
                                            double NewValue;

                                            LeftValue = this.LeftArg.InnerOperand.DoubleLiteralValue;
                                            RightValue = this.RightArg.InnerOperand.DoubleLiteralValue;
                                            NewValue = Math.Pow(LeftValue, RightValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }
                                break;
                        }
                    }
                }

                Debug.Assert(LocalResultType == this.resultType); // constant folding shouldn't change type
                if (NewOperand != null)
                {
                    replacementExpr = new ASTExpression(
                        NewOperand,
                        this.LineNumber);
                }
                else
                {
                    replacementExpr = new ASTExpression(
                        this,
                        this.LineNumber);
                }

                DidSomething = WeDidSomething;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("{0}({1},{2})", kind, LeftArg, rightArg);
            }
#endif
        }




        public class ASTConditional : ASTBase
        {
            private ASTExpression conditional;
            private ASTExpression consequent;
            private ASTExpression alternate; // may be null
            private readonly int lineNumber;

            private DataTypes resultType;

            public ASTExpression Conditional { get { return conditional; } }
            public ASTExpression Consequent { get { return consequent; } }
            public ASTExpression Alternate { get { return alternate; } } // may be null
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            public ASTConditional(
                ASTExpression Conditional,
                ASTExpression Consequent,
                ASTExpression Alternate,
                int LineNumber)
            {
                this.lineNumber = LineNumber;
                this.conditional = Conditional;
                this.consequent = Consequent;
                this.alternate = Alternate;
            }

            /* type check the conditional node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                DataTypes ConditionalReturnType;
                Error = this.Conditional.TypeCheck(out ConditionalReturnType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (ConditionalReturnType != DataTypes.eBoolean)
                {
                    ErrorLineNumber = Conditional.LineNumber;
                    return CompileErrors.eCompileConditionalMustBeBoolean;
                }

                DataTypes ConsequentReturnType;
                Error = this.Consequent.TypeCheck(out ConsequentReturnType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                if (this.Alternate == null)
                {
                    /* no else clause */
                    ResultingDataType = this.resultType = ConsequentReturnType;
                    return CompileErrors.eCompileNoError;
                }
                else
                {
                    /* there is an else clause */
                    DataTypes AlternateReturnType;
                    Error = this.Alternate.TypeCheck(out AlternateReturnType, out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    /* make sure the types can be promoted to each other */
                    if (CanRightBeMadeToMatchLeft(ConsequentReturnType, AlternateReturnType))
                    {
                        if (MustRightBePromotedToLeft(ConsequentReturnType, AlternateReturnType))
                        {
                            /* alternate must be promoted to be same as consequent */
                            ASTExpression PromotedThing = PromoteTheExpression(
                                AlternateReturnType/*orig*/,
                                ConsequentReturnType/*desired*/,
                                this.Alternate,
                                this.LineNumber);
                            this.alternate = PromotedThing;
                            /* sanity check */
                            Error = this.Alternate.TypeCheck(
                                out AlternateReturnType/*obtain new type*/,
                                out ErrorLineNumber);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                // type promotion caused failure
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                            if (ConsequentReturnType != AlternateReturnType)
                            {
                                // after type promotion, types are no longer the same
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                        }
                    }
                    else if (CanRightBeMadeToMatchLeft(AlternateReturnType, ConsequentReturnType))
                    {
                        if (MustRightBePromotedToLeft(AlternateReturnType, ConsequentReturnType))
                        {
                            /* consequent must be promoted to be same as alternate */
                            ASTExpression PromotedThing = PromoteTheExpression(
                                ConsequentReturnType/*orig*/,
                                AlternateReturnType/*desired*/,
                                this.Consequent,
                                this.LineNumber);
                            this.consequent = PromotedThing;
                            /* sanity check */
                            Error = this.Consequent.TypeCheck(
                                out ConsequentReturnType/*obtain new type*/,
                                out ErrorLineNumber);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                // type promotion caused failure
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                            if (ConsequentReturnType != AlternateReturnType)
                            {
                                // after type promotion, types are no longer the same
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                        }
                    }
                    else
                    {
                        /* can't promote */
                        ErrorLineNumber = this.LineNumber;
                        return CompileErrors.eCompileTypeMismatchBetweenThenAndElse;
                    }
                    if (ConsequentReturnType != AlternateReturnType)
                    {
                        // Consequent and Alternate return types differ
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = this.resultType = ConsequentReturnType;
                    return CompileErrors.eCompileNoError;
                }
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int StackDepth = StackDepthParam;

                /* evaluate the condition */
                this.Conditional.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack bad after evaluating conditional
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* perform branch to bad guy */
                int PatchLocationForConditionalBranch;
                FuncCode.AddPcodeInstruction(Pcodes.epBranchIfZero, out PatchLocationForConditionalBranch, this.LineNumber);
                FuncCode.AddPcodeOperandInteger(-1/*not known yet*/);
                StackDepth--;
                if (StackDepth != StackDepthParam)
                {
                    // stack bad after performing conditional branch
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* evaluate the true branch */
                this.Consequent.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack bad after evaluating true branch
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                int PatchForConditionalEnd;
                FuncCode.AddPcodeInstruction(Pcodes.epBranchUnconditional, out PatchForConditionalEnd, this.LineNumber);
                FuncCode.AddPcodeOperandInteger(-1/*not known yet*/);

                StackDepth--;

                /* patch the conditional branch */
                FuncCode.ResolvePcodeBranch(PatchLocationForConditionalBranch, FuncCode.PcodeGetNextAddress());

                /* evaluate the false branch */
                if (this.Alternate != null)
                {
                    /* there is a real live alternate */
                    this.Alternate.PcodeGen(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                }
                else
                {
                    /* there is no alternate, so push zero or nil */
                    int unused;
                    switch (this.Consequent.ResultType)
                    {
                        default:
                            // CodeGenConditional:  bad type for 0
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eBoolean:
                        case DataTypes.eInteger:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, this.LineNumber);
                            FuncCode.AddPcodeOperandInteger(0);
                            break;
                        case DataTypes.eFloat:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateFloat, out unused, this.LineNumber);
                            FuncCode.AddPcodeOperandFloat(0);
                            break;
                        case DataTypes.eDouble:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateDouble, out unused, this.LineNumber);
                            FuncCode.AddPcodeOperandDouble(0);
                            break;
                        case DataTypes.eArrayOfBoolean:
                        case DataTypes.eArrayOfByte:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayByte, out unused, this.LineNumber);
                            break;
                        case DataTypes.eArrayOfInteger:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayInt32, out unused, this.LineNumber);
                            break;
                        case DataTypes.eArrayOfFloat:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayFloat, out unused, this.LineNumber);
                            break;
                        case DataTypes.eArrayOfDouble:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayDouble, out unused, this.LineNumber);
                            break;
                    }
                    StackDepth++;
                    MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                }
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack depth bad after alternate
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* resolve the then-skipover-else branch */
                FuncCode.ResolvePcodeBranch(PatchForConditionalEnd, FuncCode.PcodeGetNextAddress());

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                /* evaluate the condition */
                this.Conditional.ILGen(
                    cilObject,
                    context);

                Label endLabel = context.ilGenerator.DefineLabel();
                Label altLabel = context.ilGenerator.DefineLabel();

                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                context.ilGenerator.Emit(OpCodes.Beq, altLabel);

                /* evaluate the true branch */
                this.Consequent.ILGen(
                    cilObject,
                    context);
                context.ilGenerator.Emit(OpCodes.Br, endLabel);

                context.ilGenerator.MarkLabel(altLabel);

                /* evaluate the false branch */
                if (this.Alternate != null)
                {
                    /* there is a real live alternate */
                    this.Alternate.ILGen(
                        cilObject,
                        context);
                }
                else
                {
                    /* there is no alternate, so push zero or nil */
                    switch (this.Consequent.ResultType)
                    {
                        default:
                            // CodeGenConditional:  bad type for 0
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eBoolean:
                        case DataTypes.eInteger:
                            context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                            break;
                        case DataTypes.eFloat:
                            context.ilGenerator.Emit(OpCodes.Ldc_R4, (float)0);
                            break;
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Ldc_R8, (double)0);
                            break;
                        case DataTypes.eArrayOfBoolean:
                        case DataTypes.eArrayOfByte:
                            context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleByte).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                            break;
                        case DataTypes.eArrayOfInteger:
                            context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleInt32).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                            break;
                        case DataTypes.eArrayOfFloat:
                            context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleFloat).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                            break;
                        case DataTypes.eArrayOfDouble:
                            context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleDouble).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                            break;
                    }
                }

                context.ilGenerator.MarkLabel(endLabel);
            }

            /* fold constants in the AST. returns True in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                bool WeDidSomething = false;
                bool TheyDidSomething;

                replacementExpr = null;

                DataTypes LocalResultType = this.Consequent.ResultType;

                ASTExpression conditionalReplacement;
                this.Conditional.FoldConst(out TheyDidSomething, out conditionalReplacement);
                if (conditionalReplacement != null)
                {
                    this.conditional = conditionalReplacement;
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                ASTExpression consequentReplacement;
                this.Consequent.FoldConst(out TheyDidSomething, out consequentReplacement);
                if (consequentReplacement != null)
                {
                    this.consequent = consequentReplacement;
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                if (this.Alternate != null)
                {
                    ASTExpression alternateReplacement;
                    this.Alternate.FoldConst(out TheyDidSomething, out alternateReplacement);
                    if (alternateReplacement != null)
                    {
                        this.alternate = alternateReplacement;
                    }
                    WeDidSomething = WeDidSomething || TheyDidSomething;
                }

                /* can we eliminate the conditional? */
                bool ConditionalWasRemoved = false;
                if ((this.Conditional.Kind == ExprKind.eExprOperand)
                    && (this.Conditional.InnerOperand.Kind == ASTOperandKind.eASTOperandBooleanLiteral))
                {
                    /* constant conditional -- reduce to one branch or the other */
                    if (this.Conditional.InnerOperand.BooleanLiteralValue)
                    {
                        /* then clause taken */
                        replacementExpr = this.Consequent;
                        WeDidSomething = true;
                        ConditionalWasRemoved = true;
                    }
                    else
                    {
                        /* else clause taken */
                        if (this.Alternate != null)
                        {
                            replacementExpr = this.Alternate;
                            WeDidSomething = true;
                            ConditionalWasRemoved = true;
                        }
                        else
                        {

                            /* no alternate -- synthesize the zero value */
                            ASTOperand NewOperand;
                            switch (this.Consequent.ResultType)
                            {
                                default:
                                    // bad data type
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case DataTypes.eArrayOfBoolean:
                                case DataTypes.eArrayOfByte:
                                case DataTypes.eArrayOfInteger:
                                case DataTypes.eArrayOfFloat:
                                case DataTypes.eArrayOfDouble:
                                    NewOperand = null;
                                    break;
                                case DataTypes.eBoolean:
                                    NewOperand = new ASTOperand(
                                        false,
                                        this.LineNumber);
                                    break;
                                case DataTypes.eInteger:
                                    NewOperand = new ASTOperand(
                                        0,
                                        this.LineNumber);
                                    break;
                                case DataTypes.eFloat:
                                    NewOperand = new ASTOperand(
                                        0,
                                        this.LineNumber);
                                    break;
                                case DataTypes.eDouble:
                                    NewOperand = new ASTOperand(
                                        0,
                                        this.LineNumber);
                                    break;
                            }

                            if (NewOperand != null)
                            {
                                replacementExpr = new ASTExpression(
                                    NewOperand,
                                    this.LineNumber);
                                WeDidSomething = true;
                                ConditionalWasRemoved = true;
                            }
                        }
                    }
                }
                if (!ConditionalWasRemoved)
                {
                    /* couldn't eliminate conditional */
                    replacementExpr = new ASTExpression(
                        this,
                        this.LineNumber);
                }

                Debug.Assert(LocalResultType == this.resultType);
                DidSomething = WeDidSomething;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("if {0} then{3}{1}{3}else{3}{2}", conditional, ASTExpressionList.IndentAll(consequent.ToString()), ASTExpressionList.IndentAll(alternate != null ? alternate.ToString() : "()"), Environment.NewLine);
            }
#endif
        }




        public class ASTErrorForm : ASTBase
        {
            private ASTExpression resumeCondition;
            private readonly string messageString;
            private readonly int lineNumber;

            public ASTExpression ResumeCondition { get { return resumeCondition; } }
            public string MessageString { get { return messageString; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return DataTypes.eBoolean; } }


            public ASTErrorForm(
                ASTExpression resumeCondition,
                string messageString,
                int lineNumber)
            {
                this.resumeCondition = resumeCondition;
                this.messageString = messageString;
                this.lineNumber = lineNumber;
            }

            /* type check the error message node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                DataTypes ResumeConditionType;
                Error = this.ResumeCondition.TypeCheck(out ResumeConditionType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                if (ResumeConditionType != DataTypes.eBoolean)
                {
                    ErrorLineNumber = this.LineNumber;
                    return CompileErrors.eCompileErrorNeedsBooleanArg;
                }

                Debug.Assert(DataTypes.eBoolean == this.ResultType);
                ResultingDataType = DataTypes.eBoolean;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                ASTExpression messageStringExpression = new ASTExpression(this.MessageString, this.LineNumber);

                PcodeGenExternCall(
                    FuncCode,
                    ref StackDepthParam,
                    ref MaxStackDepth,
                    this.LineNumber,
                    "ErrorTrap",
                    new ASTExpression[2]
                    {
                        messageStringExpression,
                        this.ResumeCondition,
                    },
                    new DataTypes[2]
                    {
                        DataTypes.eArrayOfByte/*0: name*/,
                        DataTypes.eBoolean/*1: resume condition*/,
                    },
                    DataTypes.eBoolean);
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                ASTExpression messageStringExpression = new ASTExpression(this.MessageString, this.LineNumber);

                ILGenExternCall(
                    cilObject,
                    context,
                    this.LineNumber,
                    "ErrorTrap",
                    new ASTExpression[2]
                    {
                        messageStringExpression,
                        this.ResumeCondition,
                    },
                    new DataTypes[2]
                    {
                        DataTypes.eArrayOfByte/*0: name*/,
                        DataTypes.eBoolean/*1: resume condition*/,
                    },
                    DataTypes.eBoolean);
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpression)
            {
                ASTExpression resumeConditionReplacement;
                this.ResumeCondition.FoldConst(out DidSomething, out resumeConditionReplacement);
                if (resumeConditionReplacement != null)
                {
                    this.resumeCondition = resumeConditionReplacement;
                }

                replacementExpression = null;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("error \"{0}\" resumable {1}", messageString, resumeCondition);
            }
#endif
        }




        // helper function
        public static void PcodeGenExternCall(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            int RuntimeLineNumber,
            string ExternFuncName,
            ASTExpression[] Arguments,
            DataTypes[] ArgumentsTypes,
            DataTypes ReturnType)
        {
            int unused;

            int StackDepth = StackDepthParam;

            // eval arguments
            int startStackDepth = StackDepth;
            for (int i = 0; i < Arguments.Length; i++)
            {
                Debug.Assert(ArgumentsTypes[i] == Arguments[i].ResultType);
                Arguments[i].PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
            }
            Debug.Assert(startStackDepth + Arguments.Length == StackDepth);

            /* function call */
            FuncCode.AddPcodeInstruction(
                Pcodes.epFuncCallExternal,
                out unused,
                RuntimeLineNumber);
            /* push function name */
            FuncCode.AddPcodeOperandString(
                ExternFuncName);
            /* push a parameter list record for runtime checking */
            FuncCode.AddPcodeOperandDataTypeArray(
                ArgumentsTypes);
            /* save the return type */
            FuncCode.AddPcodeOperandInteger(
                (int)ReturnType);
            StackDepth++;
            MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
            // account for argument poppage
            StackDepth -= ArgumentsTypes.Length;

            if (StackDepth != StackDepthParam + 1)
            {
                // stack depth error after pushing return value
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            StackDepthParam = StackDepth;
        }

        // helper function
        public static void ILGenExternCall(
            CILObject cilObject,
            ILGenContext context,
            int RuntimeLineNumber, // TODO:
            string ExternFuncName,
            ASTExpression[] Arguments,
            DataTypes[] ArgumentsTypes,
            DataTypes ReturnType)
        {
            // begin push for ".Invoke(string, object[], out object)" sequence

            // 'this' (context from thread local store)
            context.ilGenerator.Emit(OpCodes.Call, typeof(CILThreadLocalStorage).GetMethod("get_CurrentEvaluationContext", BindingFlags.Public | BindingFlags.Static));

            // string
            context.ilGenerator.Emit(OpCodes.Ldstr, ExternFuncName);

            // object[]
            // eval function arguments
            context.ilGenerator.Emit(OpCodes.Ldc_I4, Arguments.Length);
            context.ilGenerator.Emit(OpCodes.Newarr, typeof(object));
            // assign argument slots
            Debug.Assert(Arguments.Length == ArgumentsTypes.Length);
            for (int i = 0; i < Arguments.Length; i++)
            {
                Debug.Assert(ArgumentsTypes[i] == Arguments[i].ResultType);
                context.ilGenerator.Emit(OpCodes.Dup); // arrayref
                context.ilGenerator.Emit(OpCodes.Ldc_I4, i);
                Arguments[i].ILGen(
                    cilObject,
                    context);
                Type managedType;
                switch (ArgumentsTypes[i])
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                        managedType = typeof(bool);
                        break;
                    case DataTypes.eInteger:
                        managedType = typeof(int);
                        break;
                    case DataTypes.eFloat:
                        managedType = typeof(float);
                        break;
                    case DataTypes.eDouble:
                        managedType = typeof(double);
                        break;
                    case DataTypes.eArrayOfBoolean:
                    case DataTypes.eArrayOfByte:
                        managedType = typeof(ArrayHandleByte);
                        break;
                    case DataTypes.eArrayOfInteger:
                        managedType = typeof(ArrayHandleInt32);
                        break;
                    case DataTypes.eArrayOfFloat:
                        managedType = typeof(ArrayHandleFloat);
                        break;
                    case DataTypes.eArrayOfDouble:
                        managedType = typeof(ArrayHandleDouble);
                        break;
                }
                context.ilGenerator.Emit(OpCodes.Box, managedType);
                context.ilGenerator.Emit(OpCodes.Stelem, typeof(object));
            }

            // out object
            LocalBuilder localReturnValue = context.ilGenerator.DeclareLocal(typeof(object));
            context.ilGenerator.Emit(OpCodes.Ldloca, localReturnValue);

            context.ilGenerator.Emit(OpCodes.Callvirt, typeof(PcodeSystem.IEvaluationContext).GetMethod("Invoke"));
            LocalBuilder localErrorCode = context.ilGenerator.DeclareLocal(typeof(EvalErrors));
            context.ilGenerator.Emit(OpCodes.Stloc, localErrorCode);

            // check result
            context.ilGenerator.Emit(OpCodes.Ldloc, localErrorCode);
            context.ilGenerator.Emit(OpCodes.Ldc_I4, (int)EvalErrors.eEvalNoError);
            Label labelGood = context.ilGenerator.DefineLabel();
            context.ilGenerator.Emit(OpCodes.Beq, labelGood);
            context.ilGenerator.Emit(OpCodes.Ldloc, localErrorCode);
            context.ilGenerator.Emit(OpCodes.Newobj, typeof(PcodeExterns.EvalErrorException).GetConstructor(new Type[] { typeof(EvalErrors) }));
            context.ilGenerator.Emit(OpCodes.Throw);
            context.ilGenerator.MarkLabel(labelGood);
            context.ilGenerator.Emit(OpCodes.Ldloc, localReturnValue);
        }




        public enum ExprKind
        {
            eInvalid,

            eExprArrayDeclaration,
            eExprAssignment,
            eExprBinaryOperator,
            eExprConditional,
            eExprExpressionSequence,
            eExprFunctionArguments,
            eExprFunctionCall,
            eExprLoop,
            eExprOperand,
            eExprUnaryOperator,
            eExprVariableDeclaration,
            eExprErrorForm,
            eExprWaveGetter,
            eExprPrintString,
            eExprPrintExpr,
            eExprSampleLoader,
            eExprForLoop,
        }

        // The ASTExpressionRec is a generic (variant) wrapper around specific types of expressions. Since expressions now
        // inherit from the abstract base class ASTBase, this is just a vestigate of the old C code when subclassing was not
        // available. However, keeping the extra level of indirection does provide the ability to update the expression type
        // in place which simplifies the implementation of constant folding.

        public class ASTExpression : ASTBase
        {
            private ExprKind kind;
            private ASTBase u;
            private readonly int lineNumber;

            public ExprKind Kind { get { return kind; } }
            public override int LineNumber { get { return lineNumber; } }
            public ASTBase U { get { return u; } }
            public override DataTypes ResultType { get { return u.ResultType; } } // valid only after TypeCheck()


            public ASTExpression(
                ASTArrayDeclaration TheArrayDeclaration,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprArrayDeclaration;
                this.u = TheArrayDeclaration;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTAssignment TheAssignment,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprAssignment;
                this.u = TheAssignment;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTBinaryOperation TheBinaryOperator,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprBinaryOperator;
                this.u = TheBinaryOperator;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTConditional TheConditional,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprConditional;
                this.u = TheConditional;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTExpressionList TheExpressionList,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprExpressionSequence;
                this.u = TheExpressionList;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTFunctionArgumentsExpressionList functionArguments,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprFunctionArguments;
                this.u = functionArguments;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTFunctionCall TheFunctionCall,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprFunctionCall;
                this.u = TheFunctionCall;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTLoop TheLoop,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprLoop;
                this.u = TheLoop;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTOperand TheOperand,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprOperand;
                this.u = TheOperand;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTUnaryOperation TheUnaryOperator,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprUnaryOperator;
                this.u = TheUnaryOperator;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTVariableDeclaration TheVariableDecl,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprVariableDeclaration;
                this.u = TheVariableDecl;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTErrorForm TheErrorForm,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprErrorForm;
                this.u = TheErrorForm;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTWaveGetter TheWaveGetter,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprWaveGetter;
                this.u = TheWaveGetter;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTSampleLoader TheSampleLoader,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprSampleLoader;
                this.u = TheSampleLoader;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTPrintString ThePrintString,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprPrintString;
                this.u = ThePrintString;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTPrintExpression ThePrintExpr,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprPrintExpr;
                this.u = ThePrintExpr;
                this.lineNumber = TheLineNumber;
            }

            public ASTExpression(
                ASTForLoop ForLoop,
                int TheLineNumber)
            {
                this.kind = ExprKind.eExprForLoop;
                this.u = ForLoop;
                this.lineNumber = TheLineNumber;
            }

            // special case
            public ASTExpression(
                string stringLiteral,
                int TheLineNumber)
            {
                ASTOperand literalOperand = new ASTOperand(stringLiteral, TheLineNumber);

                this.kind = ExprKind.eExprOperand;
                this.u = literalOperand;
                this.lineNumber = TheLineNumber;
            }


            public ASTOperand InnerOperand
            {
                get
                {
                    if ((this.Kind != ExprKind.eExprOperand) || !(this.U is ASTOperand))
                    {
                        // expression is not an operand
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (ASTOperand)this.U;
                }
            }

            public ASTExpressionList InnerExpressionList
            {
                get
                {
                    if ((this.Kind != ExprKind.eExprExpressionSequence) || !(this.U is ASTExpressionList))
                    {
                        // expression is not a sequence
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (ASTExpressionList)this.U;
                }
            }

            public ASTFunctionArgumentsExpressionList InnerFunctionArguments
            {
                get
                {
                    if ((this.Kind != ExprKind.eExprFunctionArguments) || !(this.U is ASTFunctionArgumentsExpressionList))
                    {
                        // expression is not a function arguments container
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (ASTFunctionArgumentsExpressionList)this.U;
                }
            }

            public ASTBinaryOperation InnerBinaryOperator
            {
                get
                {
                    if ((this.Kind != ExprKind.eExprBinaryOperator) || !(this.U is ASTBinaryOperation))
                    {
                        // expression isn't a binary operator
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (ASTBinaryOperation)this.U;
                }
            }

            /* type check an expression. returns eCompileNoError and the resulting value type if it checks correctly. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultTypeOut,
                out int ErrorLineNumber)
            {
                ErrorLineNumber = -1;
                ResultTypeOut = DataTypes.eInvalidDataType;

                // special case: empty expression sequences are not permitted
                if (this.Kind == ExprKind.eExprExpressionSequence)
                {
                    if (this.u == null)
                    {
                        ErrorLineNumber = this.LineNumber;
                        return CompileErrors.eCompileVoidExpressionIsNotAllowed;
                    }
                }

                CompileErrors ReturnValue = this.u.TypeCheck(
                    out ResultTypeOut,
                    out ErrorLineNumber);

                return ReturnValue;
            }

            public CompileErrors GetInnerFunctionCallSymbol(
                out SymbolRec SymbolOut)
            {
                SymbolOut = null;

                if (this.Kind != ExprKind.eExprOperand)
                {
                    return CompileErrors.eCompileFunctionIdentifierRequired;
                }
                if (!((ASTOperand)this.U).IsSymbol)
                {
                    return CompileErrors.eCompileFunctionIdentifierRequired;
                }
                SymbolRec FunctionThing = ((ASTOperand)this.U).Symbol;
                if (FunctionThing.Kind != SymbolKind.Function)
                {
                    return CompileErrors.eCompileFunctionIdentifierRequired;
                }
                SymbolOut = FunctionThing;
                return CompileErrors.eCompileNoError;
            }

            public bool IsValidLValue
            {
                get
                {
                    /* to be a valid lvalue, the expression must be one of */
                    /*  - variable */
                    /*  - array subscription operation */
                    switch (this.Kind)
                    {
                        default:
                            return false;

                        case ExprKind.eExprBinaryOperator:
                            Debug.Assert(this.U is ASTBinaryOperation);
                            return ((ASTBinaryOperation)this.U).Kind == BinaryOperatorKind.eBinaryArraySubscripting;

                        case ExprKind.eExprOperand:
                            Debug.Assert(this.U is ASTOperand);
                            if (((ASTOperand)this.U).IsSymbol)
                            {
                                SymbolRec TheOperandThing = ((ASTOperand)this.U).Symbol;
                                return TheOperandThing.Kind == SymbolKind.Variable;
                            }
                            else
                            {
                                return false;
                            }
                    }
                }
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int StackDepth = StackDepthParam;

                this.u.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);


                if ((((this.Kind != ExprKind.eExprVariableDeclaration)
                        && (this.Kind != ExprKind.eExprArrayDeclaration))
                        && (StackDepth != StackDepthParam + 1))
                    ||
                    (((this.Kind == ExprKind.eExprVariableDeclaration)
                        || (this.Kind == ExprKind.eExprArrayDeclaration))
                        && (StackDepth != StackDepthParam + 2)))
                {
                    // stack depth error
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                this.u.ILGen(
                    cilObject,
                    context);
            }

            /* fold constants in the AST. returns True in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpression)
            {
                bool OneDidSomething = false;

                DataTypes initialResultType = this.ResultType;

                replacementExpression = null;
                switch (this.Kind)
                {
                    default:
                        // FoldConstExpression: unknown expression type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case ExprKind.eExprSampleLoader:
                        Debug.Assert(this.u is ASTSampleLoader);
                        ((ASTSampleLoader)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprErrorForm:
                        Debug.Assert(this.u is ASTErrorForm);
                        ((ASTErrorForm)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprWaveGetter:
                        Debug.Assert(this.u is ASTWaveGetter);
                        ((ASTWaveGetter)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprPrintString:
                        Debug.Assert(this.u is ASTPrintString);
                        ((ASTPrintString)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprPrintExpr:
                        Debug.Assert(this.u is ASTPrintExpression);
                        ((ASTPrintExpression)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprArrayDeclaration:
                        Debug.Assert(this.u is ASTArrayDeclaration);
                        ((ASTArrayDeclaration)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprAssignment:
                        Debug.Assert(this.u is ASTAssignment);
                        ((ASTAssignment)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprBinaryOperator:
                        Debug.Assert(this.u is ASTBinaryOperation);
                        ((ASTBinaryOperation)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprConditional:
                        Debug.Assert(this.u is ASTConditional);
                        ((ASTConditional)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprFunctionCall:
                        Debug.Assert(this.u is ASTFunctionCall);
                        ((ASTFunctionCall)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        Debug.Assert(replacementExpression == null); // TODO: move built-in folding into function object
                        if (((ASTFunctionCall)this.u).BuiltIn != null)
                        {
                            object value;
                            if (((ASTFunctionCall)this.u).BuiltIn.emitter.FoldConst((ASTFunctionCall)this.u, out value))
                            {
                                Debug.Assert(((ASTFunctionCall)this.u).BuiltIn.returnType == this.ResultType);
                                switch (((ASTFunctionCall)this.u).BuiltIn.returnType)
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new ArgumentException();
                                    case DataTypes.eBoolean:
                                        this.u = new ASTOperand((bool)value, this.LineNumber);
                                        break;
                                    case DataTypes.eInteger:
                                        this.u = new ASTOperand((int)value, this.LineNumber);
                                        break;
                                    case DataTypes.eFloat:
                                        this.u = new ASTOperand((float)value, this.LineNumber);
                                        break;
                                    case DataTypes.eDouble:
                                        this.u = new ASTOperand((double)value, this.LineNumber);
                                        break;
                                }
                                this.kind = ExprKind.eExprOperand;
                            }
                        }
                        break;
                    case ExprKind.eExprLoop:
                        Debug.Assert(this.u is ASTLoop);
                        ((ASTLoop)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprOperand:
                        Debug.Assert(this.u is ASTOperand);
                        ((ASTOperand)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprVariableDeclaration:
                        Debug.Assert(this.u is ASTVariableDeclaration);
                        ((ASTVariableDeclaration)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprExpressionSequence:
                        Debug.Assert(this.u is ASTExpressionList);
                        ((ASTExpressionList)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprUnaryOperator:
                        Debug.Assert(this.u is ASTUnaryOperation);
                        ((ASTUnaryOperation)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                    case ExprKind.eExprForLoop:
                        Debug.Assert(this.u is ASTForLoop);
                        ((ASTForLoop)this.u).FoldConst(out OneDidSomething, out replacementExpression);
                        break;
                }

                Debug.Assert(this.ResultType == initialResultType); // constant folding should not change type

                /* absorb the result into us */
                if (replacementExpression != null)
                {
                    Debug.Assert(this.ResultType == replacementExpression.ResultType);
                    this.kind = replacementExpression.Kind;
                    this.u = replacementExpression.u;
                }

                DidSomething = OneDidSomething;
            }

#if DEBUG
            public override string ToString()
            {
                return u.ToString();
            }
#endif
        }




        public class ASTExpressionList : ASTBase
        {
            // lisp-style linked list
            private ASTExpression first;
            private ASTExpressionList rest;
            private readonly int lineNumber;

            public ASTExpression First { get { return first; } set { first = value; } }
            public ASTExpressionList Rest { get { return rest; } }
            public override int LineNumber { get { return lineNumber; } }

            public override DataTypes ResultType
            {
                get
                {
                    DataTypes resultType = DataTypes.eInvalidDataType;
                    ASTExpressionList iterator = this;
                    while (iterator != null)
                    {
                        resultType = iterator.First.ResultType;
                        iterator = iterator.Rest;
                    }
                    return resultType;
                }
            }


            public ASTExpressionList(
                ASTExpression first,
                ASTExpressionList rest,
                int lineNumber)
            {
                this.first = first;
                this.rest = rest;
                this.lineNumber = lineNumber;
            }

            /* type check a list of expressions.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                ASTExpressionList iterator = this;
                while (iterator != null)
                {
                    CompileErrors Error = iterator.First.TypeCheck(
                        out ResultingDataType,
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }

                    iterator = iterator.Rest;
                }
                Debug.Assert(ResultingDataType != DataTypes.eInvalidDataType);
                // ResultingDataType contains the type of the last list item

                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int StackDepth = StackDepthParam;

                /* generate code for all of the expressions */
                this.PcodeGenSequenceHelper(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);

                /* if there are any more than 1 additional value on the stack, then we */
                /* must pop all the other values off, since they are local variables */
                if (StackDepth - StackDepthParam > 1)
                {
                    int unused;
                    FuncCode.AddPcodeInstruction(Pcodes.epStackDeallocateUnder, out unused, this.LineNumber);
                    FuncCode.AddPcodeOperandInteger(StackDepth - StackDepthParam - 1);
                    StackDepth = StackDepthParam + 1;
                    Debug.Assert(StackDepth <= MaxStackDepth);
                }
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack messed up after recurrance call
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                StackDepthParam = StackDepth;
            }

            private void PcodeGenSequenceHelper(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int StackDepth = StackDepthParam;

                ASTExpressionList iterator = this;
                while (iterator != null)
                {
                    iterator.First.PcodeGen(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth);
                    Debug.Assert(StackDepth <= MaxStackDepth);

                    if (iterator.Rest != null)
                    {
                        /* if there is another expression, then pop the value we just */
                        /* calcuated & evaluate the next one */
                        int unused;
                        FuncCode.AddPcodeInstruction(Pcodes.epStackPop, out unused, this.LineNumber);
                        StackDepth--;
                    }
                    iterator = iterator.Rest;
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                ASTExpressionList iterator = this;
                while (iterator != null)
                {
                    iterator.First.ILGen(
                        cilObject,
                        context);

                    if (iterator.Rest != null)
                    {
                        /* if there is another expression, then pop the value we just */
                        /* calcuated & evaluate the next one */
                        context.ilGenerator.Emit(OpCodes.Pop);
                    }
                    iterator = iterator.Rest;
                }
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                FoldConstInternal(
                    this,
                    out DidSomething,
                    out replacementExpr,
                    false/*isFunctionArgumentList*/);
            }

            public static void FoldConstInternal(
                ASTExpressionList exprList,
                out bool DidSomething,
                out ASTExpression replacementExpr,
                bool isFunctionArgumentList)
            {
                bool WeDidSomething = false;

                DataTypes LocalResultType = DataTypes.eInvalidDataType;

                /* optimize each expression */
                ASTExpressionList ExprScan = exprList;
                while (ExprScan != null)
                {
                    bool TheyDidSomething;

                    /* fold the member */
                    ASTExpression replacement;
                    ExprScan.First.FoldConst(out TheyDidSomething, out replacement);
                    if (replacement != null)
                    {
                        ExprScan.First = replacement;
                    }
                    WeDidSomething = WeDidSomething || TheyDidSomething;

                    /* return the result type for the last one. */
                    LocalResultType = ExprScan.First.ResultType;

                    /* try the next */
                    ExprScan = ExprScan.Rest;
                }

                /* if we're not an argument list, then we can discard anything with */
                /* no side effects */
                if (!isFunctionArgumentList)
                {
                    ExprScan = exprList;
                    ASTExpressionList ExprScanTrail = null;
                    while (ExprScan != null)
                    {
                        /* must save the last one, so only do this for previous ones */
                        if ((ExprScan.Rest != null)
                            && (ExprScan.First.Kind == ExprKind.eExprOperand))
                        {
                            /* operand is the only one that has no side effect */
                            /* splice us out */
                            if (ExprScanTrail != null)
                            {
                                ExprScanTrail.rest = ExprScan.Rest;
                            }
                            else
                            {
                                exprList = ExprScan.Rest;
                            }
                            ExprScan = ExprScan.Rest;
                            WeDidSomething = true;
                        }
                        else
                        {
                            ExprScanTrail = ExprScan;
                            ExprScan = ExprScan.Rest;
                        }
                    }
                }

                /* create the return thing */
                if (!isFunctionArgumentList
                    && (exprList.Rest == null)
                    && (exprList.First.Kind != ExprKind.eExprArrayDeclaration)
                    && (exprList.First.Kind != ExprKind.eExprVariableDeclaration))
                {
                    /* if we have one member and we're not an arg list, and we aren't something */
                    /* that allocates local storage on the stack, then we are */
                    /* no longer a list, so make it more obvious to those above us */
                    replacementExpr = exprList.First;
                    WeDidSomething = true;
                }
                else
                {
                    replacementExpr = null;
                }

                DidSomething = WeDidSomething;
            }

#if DEBUG
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("(");
                ASTExpressionList scan = this;
                while (scan != null)
                {
                    sb.Append(IndentAll(scan.first.ToString()));
                    sb.AppendLine(";");
                    scan = scan.rest;
                }
                sb.Append(")");
                return sb.ToString();
            }

            public static string IndentAll(string s)
            {
                StringBuilder sb = new StringBuilder();
                string[] lines = s.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    sb.Append("\t");
                    sb.Append(lines[i]);
                    if (i < lines.Length - 1)
                    {
                        sb.AppendLine();
                    }
                }
                return sb.ToString();
            }
#endif
        }




        public class ASTFunctionArgumentsExpressionList : ASTBase
        {
            private ASTExpressionList argumentList; // null == no arguments
            private readonly int lineNumber;

            public ASTExpressionList List { get { return argumentList; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { throw new InvalidOperationException(); } }


            public ASTFunctionArgumentsExpressionList(
                ASTExpressionList argumentList,
                int lineNumber)
            {
                this.argumentList = argumentList;
                this.lineNumber = lineNumber;
            }

            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                if (argumentList != null)
                {
                    DataTypes notused;
                    return argumentList.TypeCheck(
                        out notused,
                        out ErrorLineNumber);
                }

                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int StackDepth = StackDepthParam;

                ASTExpressionList iterator = this.argumentList;
                while (iterator != null)
                {
                    int previousStackDepth = StackDepth;
                    iterator.First.PcodeGen(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    if (StackDepth != previousStackDepth + 1)
                    {
                        // stack messed up
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }

                    iterator = iterator.Rest;
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                ASTExpressionList iterator = this.argumentList;
                while (iterator != null)
                {
                    iterator.First.ILGen(
                        cilObject,
                        context);

                    iterator = iterator.Rest;
                }
            }

            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                ASTExpressionList.FoldConstInternal(
                    this.argumentList,
                    out DidSomething,
                    out replacementExpr,
                    true/*isFunctionArgumentList*/);
                Debug.Assert(replacementExpr == null);
            }

#if DEBUG
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("(");
                ASTExpressionList scan = argumentList;
                bool first = true;
                while (scan != null)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    first = false;
                    sb.Append(scan.First);
                    scan = scan.Rest;
                }
                sb.Append(")");
                return sb.ToString();
            }
#endif
        }




        public class ASTFunctionCall : ASTBase
        {
            private ASTFunctionArgumentsExpressionList argumentList;
            private ASTExpression functionGenerator;
            private readonly int lineNumber;
            private BuiltInFunction builtIn; // null == normal function; annotated during typecheck pass

            private DataTypes resultType;

            public ASTFunctionArgumentsExpressionList ArgumentList { get { return argumentList; } }
            public ASTExpression FunctionGenerator { get { return functionGenerator; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }
            public BuiltInFunction BuiltIn { get { return builtIn; } } // null == normal function; annotated during typecheck pass


            /* create a new function call node. the argument list can be null if there are no arguments. */
            public ASTFunctionCall(
                ASTFunctionArgumentsExpressionList argumentList,
                ASTExpression functionGeneratorExpression,
                int lineNumber)
            {
                this.lineNumber = lineNumber;
                this.argumentList = argumentList;
                this.functionGenerator = functionGeneratorExpression;
            }

            public static readonly BuiltInFunction[] BuiltInFunctions = CreateBuiltInFunctions();

            private static BuiltInFunction[] CreateBuiltInFunctions()
            {
                BuiltInFunction[] functions = new BuiltInFunction[]
                {
                    new BuiltInFunction("atan2", new DataTypes[] { DataTypes.eFloat, DataTypes.eFloat }, DataTypes.eFloat, Pcodes.epAtan2Float,
                        new EmitBuiltInFunctionStatic(typeof(Math), "Atan2")),
                    new BuiltInFunction("atan2", new DataTypes[] { DataTypes.eDouble, DataTypes.eDouble }, DataTypes.eDouble, Pcodes.epAtan2Double,
                        new EmitBuiltInFunctionStatic(typeof(Math), "Atan2")),
                    new BuiltInFunction("min", new DataTypes[] { DataTypes.eInteger, DataTypes.eInteger }, DataTypes.eInteger, Pcodes.epMinInt,
                        new EmitBuiltInFunctionStatic(typeof(Math), "Min")),
                    new BuiltInFunction("min", new DataTypes[] { DataTypes.eFloat, DataTypes.eFloat }, DataTypes.eFloat, Pcodes.epMinFloat,
                        new EmitBuiltInFunctionStatic(typeof(Math), "Min")),
                    new BuiltInFunction("min", new DataTypes[] { DataTypes.eDouble, DataTypes.eDouble }, DataTypes.eDouble, Pcodes.epMinDouble,
                        new EmitBuiltInFunctionStatic(typeof(Math), "Min")),
                    new BuiltInFunction("max", new DataTypes[] { DataTypes.eInteger, DataTypes.eInteger }, DataTypes.eInteger, Pcodes.epMaxInt,
                        new EmitBuiltInFunctionStatic(typeof(Math), "Max")),
                    new BuiltInFunction("max", new DataTypes[] { DataTypes.eFloat, DataTypes.eFloat }, DataTypes.eFloat, Pcodes.epMaxFloat,
                        new EmitBuiltInFunctionStatic(typeof(Math), "Max")),
                    new BuiltInFunction("max", new DataTypes[] { DataTypes.eDouble, DataTypes.eDouble }, DataTypes.eDouble, Pcodes.epMaxDouble,
                        new EmitBuiltInFunctionStatic(typeof(Math), "Max")),
                    new BuiltInFunction("minmax", new DataTypes[] { DataTypes.eInteger, DataTypes.eInteger, DataTypes.eInteger }, DataTypes.eInteger, Pcodes.epMinMaxInt,
                        new EmitBuiltInFunctionLeftCascade (new CascadeFunc1[]
                        {
                            new CascadeFunc1(typeof(Math), "Min", 2, DataTypes.eInteger),
                            new CascadeFunc1(typeof(Math), "Max", 2, DataTypes.eInteger),
                        })),
                    new BuiltInFunction("minmax", new DataTypes[] { DataTypes.eFloat, DataTypes.eFloat, DataTypes.eFloat }, DataTypes.eFloat, Pcodes.epMinMaxFloat,
                        new EmitBuiltInFunctionLeftCascade (new CascadeFunc1[]
                        {
                            new CascadeFunc1(typeof(Math), "Min", 2, DataTypes.eFloat),
                            new CascadeFunc1(typeof(Math), "Max", 2, DataTypes.eFloat),
                        })),
                    new BuiltInFunction("minmax", new DataTypes[] { DataTypes.eDouble, DataTypes.eDouble, DataTypes.eDouble }, DataTypes.eDouble, Pcodes.epMinMaxDouble,
                        new EmitBuiltInFunctionLeftCascade (new CascadeFunc1[]
                        {
                            new CascadeFunc1(typeof(Math), "Min", 2, DataTypes.eDouble),
                            new CascadeFunc1(typeof(Math), "Max", 2, DataTypes.eDouble),
                        })),
                };
                Array.Sort(functions, BuiltInFunctionComparerTotal.Default);
                return functions;
            }

            public static bool TryFindBuiltInFunctionRange(string name, out int start, out int count)
            {
                start = -1;
                count = Int32.MinValue;

                int index = Array.BinarySearch(BuiltInFunctions, new BuiltInFunction(name), BuiltInFunctionComparerName.Default);
                if (index >= 0)
                {
                    // find entire extent
                    start = index;
                    count = 1;
                    while ((start > 0) && String.Equals(name, BuiltInFunctions[start - 1].name))
                    {
                        start--;
                        count++;
                    }
                    while ((start + count < BuiltInFunctions.Length) && String.Equals(name, BuiltInFunctions[start + count].name))
                    {
                        count++;
                    }
                    return true;
                }
                return false;
            }

            public class BuiltInFunctionComparerName : IComparer<BuiltInFunction>
            {
                public static readonly BuiltInFunctionComparerName Default = new BuiltInFunctionComparerName();

                public int Compare(BuiltInFunction x, BuiltInFunction y)
                {
                    return String.Compare(x.name, y.name);
                }
            }

            public class BuiltInFunctionComparerTotal : IComparer<BuiltInFunction>
            {
                public static readonly BuiltInFunctionComparerTotal Default = new BuiltInFunctionComparerTotal();

                public int Compare(BuiltInFunction x, BuiltInFunction y)
                {
                    int c = String.Compare(x.name, y.name);
                    if (c != 0)
                    {
                        return c;
                    }
                    Debug.Assert(DataTypeScore[(int)x.returnType].Key == x.returnType);
                    Debug.Assert(DataTypeScore[(int)y.returnType].Key == y.returnType);
                    c = (DataTypeScore[(int)x.returnType].Value).CompareTo(DataTypeScore[(int)y.returnType].Value);
                    if (c != 0)
                    {
                        return c;
                    }
                    c = x.argsTypes.Length.CompareTo(y.argsTypes.Length);
                    if (c != 0)
                    {
                        return c;
                    }
                    for (int i = 0; i < x.argsTypes.Length; i++)
                    {
                        Debug.Assert(DataTypeScore[(int)x.argsTypes[i]].Key == x.argsTypes[i]);
                        Debug.Assert(DataTypeScore[(int)y.argsTypes[i]].Key == y.argsTypes[i]);
                        c = (DataTypeScore[(int)x.argsTypes[i]].Value).CompareTo(DataTypeScore[(int)y.argsTypes[i]].Value);
                        if (c != 0)
                        {
                            return c;
                        }
                    }
                    return c;
                }

                private static readonly KeyValuePair<DataTypes, int>[] DataTypeScore = new KeyValuePair<DataTypes, int>[]
                {
                    new KeyValuePair<DataTypes, int>(DataTypes.eInvalidDataType, 0),
                    new KeyValuePair<DataTypes, int>(DataTypes.eBoolean, 1),
                    new KeyValuePair<DataTypes, int>(DataTypes.eInteger, 2),
                    new KeyValuePair<DataTypes, int>(DataTypes.eFloat, 3),
                    new KeyValuePair<DataTypes, int>(DataTypes.eDouble, 4),
                    new KeyValuePair<DataTypes, int>(DataTypes.eArrayOfBoolean, 5),
                    new KeyValuePair<DataTypes, int>(DataTypes.eArrayOfByte, 6),
                    new KeyValuePair<DataTypes, int>(DataTypes.eArrayOfInteger, 7),
                    new KeyValuePair<DataTypes, int>(DataTypes.eArrayOfFloat, 8),
                    new KeyValuePair<DataTypes, int>(DataTypes.eArrayOfDouble, 9),
                };
            }

            public class BuiltInFunction
            {
                public readonly string name;
                public readonly DataTypes[] argsTypes;
                public readonly DataTypes returnType;

                public readonly Pcodes pcode;
                public readonly EmitBuiltInFunction emitter;

                public BuiltInFunction(
                    string name,
                    DataTypes[] argsTypes,
                    DataTypes returnType,
                    Pcodes pcode,
                    EmitBuiltInFunction emitter)
                {
                    this.name = name;
                    this.argsTypes = argsTypes;
                    this.returnType = returnType;
                    this.pcode = pcode;
                    this.emitter = emitter;
                }

                public BuiltInFunction(string name)
                {
                    this.name = name;
                }

#if DEBUG
                public override string ToString()
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(name);
                    sb.Append("(");
                    for (int i = 0; i < argsTypes.Length; i++)
                    {
                        if (i != 0)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(argsTypes[i].ToString());
                    }
                    sb.Append("):");
                    sb.Append(returnType.ToString());
                    return sb.ToString();
                }
#endif
            }

            public abstract class EmitBuiltInFunction
            {
                public abstract void Emit(
                    CILObject cilObject,
                    ILGenContext context,
                    ASTFunctionCall FunctionCall);

                public abstract bool FoldConst(
                    ASTFunctionCall FunctionCall,
                    out object value);
            }

            public class CascadeFunc1
            {
                public readonly Type container;
                public readonly string name;
                public readonly int argsCount;
                public readonly DataTypes resultType;

                public CascadeFunc1(
                    Type container,
                    string name,
                    int argsCount,
                    DataTypes resultType)
                {
                    this.container = container;
                    this.name = name;
                    this.argsCount = argsCount;
                    this.resultType = resultType;
                }
            }

            // emits cascade with first eval leftmost, e.g., One(Two(arg1, arg2), arg3)
            public class EmitBuiltInFunctionLeftCascade : EmitBuiltInFunction
            {
                private readonly CascadeFunc1[] cascade;

                public EmitBuiltInFunctionLeftCascade(CascadeFunc1[] cascade)
                {
                    this.cascade = cascade;
                }

                public override void Emit(
                    CILObject cilObject,
                    ILGenContext context,
                    ASTFunctionCall FunctionCall)
                {
                    List<ASTExpression> args = new List<ASTExpression>();
                    List<DataTypes> argsTypes = new List<DataTypes>();
                    ASTExpressionList arg = FunctionCall.ArgumentList.List;
                    while (arg != null)
                    {
                        args.Add(arg.First);
                        argsTypes.Add(arg.First.ResultType);
                        arg = arg.Rest;
                    }

                    int c = 0;
                    DataTypes resultType = DataTypes.eInvalidDataType;
                    bool first = true;
                    for (int i = cascade.Length - 1; i >= 0; i--)
                    {
                        int argStart = c;
                        int additionalArgCount = cascade[i].argsCount;
                        List<DataTypes> inputTypes = new List<DataTypes>();
                        if (!first)
                        {
                            additionalArgCount--;
                            inputTypes.Add(resultType);
                        }
                        first = false;
                        resultType = cascade[i].resultType;
                        for (int j = 0; j < additionalArgCount; j++)
                        {
                            inputTypes.Add(
                                argsTypes[c]);
                            args[c].ILGen(
                                cilObject,
                                context);
                            c++;
                        }

                        Type[] managedArgsTypes;
                        Type managedReturnType;
                        PcodeMarshal.GetManagedFunctionSignature(
                            inputTypes.ToArray(),
                            resultType,
                            out managedArgsTypes,
                            out managedReturnType);

                        MethodInfo mi = cascade[i].container.GetMethod(cascade[i].name, managedArgsTypes);
                        Debug.Assert(mi.ReturnType.Equals(managedReturnType));

                        context.ilGenerator.Emit(OpCodes.Call, mi);
                    }
                    Debug.Assert(c == args.Count);
                }

                public override bool FoldConst(
                    ASTFunctionCall FunctionCall,
                    out object value)
                {
                    value = null;

                    List<object> args = new List<object>();
                    List<Type> argsTypes = new List<Type>();
                    ASTExpressionList arg = FunctionCall.ArgumentList.List;
                    while (arg != null)
                    {
                        ASTExpression argExpr = arg.First;
                        bool did = true;
                        while (did)
                        {
                            ASTExpression replacement;
                            argExpr.FoldConst(out did, out replacement);
                            if (replacement != null)
                            {
                                argExpr = replacement;
                            }
                        }
                        if (argExpr.Kind != Compiler.ExprKind.eExprOperand)
                        {
                            return false;
                        }
                        ASTOperand operand = argExpr.InnerOperand;
                        object argValue;
                        switch (operand.Kind)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case ASTOperandKind.eASTOperandSymbol:
                                return false;
                            case ASTOperandKind.eASTOperandIntegerLiteral:
                                argValue = operand.IntegerLiteralValue;
                                break;
                            case ASTOperandKind.eASTOperandBooleanLiteral:
                                argValue = operand.BooleanLiteralValue;
                                break;
                            case ASTOperandKind.eASTOperandSingleLiteral:
                                argValue = operand.SingleLiteralValue;
                                break;
                            case ASTOperandKind.eASTOperandDoubleLiteral:
                                argValue = operand.DoubleLiteralValue;
                                break;
                            case ASTOperandKind.eASTOperandStringLiteral:
                                argValue = operand.StringLiteralValue;
                                break;
                        }
                        args.Add(argValue);
                        argsTypes.Add(argValue.GetType());

                        arg = arg.Rest;
                    }

                    int c = 0;
                    Type resultType = null;
                    object resultValue = null;
                    bool first = true;
                    for (int i = cascade.Length - 1; i >= 0; i--)
                    {
                        int argStart = c;
                        int additionalArgCount = cascade[i].argsCount;
                        List<Type> inputTypes = new List<Type>();
                        List<object> inputValues = new List<object>();
                        if (!first)
                        {
                            additionalArgCount--;
                            inputTypes.Add(resultType);
                            inputValues.Add(resultValue);
                        }
                        first = false;
                        for (int j = 0; j < additionalArgCount; j++)
                        {
                            inputTypes.Add(argsTypes[c]);
                            inputValues.Add(args[c]);
                            c++;
                        }

                        MethodInfo mi = cascade[i].container.GetMethod(cascade[i].name, inputTypes.ToArray());
                        Debug.Assert(mi != null);
                        resultType = mi.ReturnType;
                        resultValue = mi.Invoke(null, inputValues.ToArray());
                    }

                    value = resultValue;
                    return true;
                }
            }

            public class EmitBuiltInFunctionStatic : EmitBuiltInFunction
            {
                private readonly Type container;
                private readonly string name;

                public EmitBuiltInFunctionStatic(
                    Type container,
                    string name)
                {
                    this.container = container;
                    this.name = name;
                }

                public override void Emit(
                    CILObject cilObject,
                    ILGenContext context,
                    ASTFunctionCall FunctionCall)
                {
                    // eval args onto stack, leftmost first/deepest
                    FunctionCall.ArgumentList.ILGen(
                        cilObject,
                        context);

                    List<DataTypes> argsTypes = new List<DataTypes>();
                    ASTExpressionList arg = FunctionCall.ArgumentList.List;
                    while (arg != null)
                    {
                        argsTypes.Add(arg.First.ResultType);
                        arg = arg.Rest;
                    }
                    DataTypes returnType = FunctionCall.BuiltIn.returnType;

                    Type[] managedArgsTypes;
                    Type managedReturnType;
                    PcodeMarshal.GetManagedFunctionSignature(
                        argsTypes.ToArray(),
                        returnType,
                        out managedArgsTypes,
                        out managedReturnType);

                    MethodInfo mi = container.GetMethod(name, managedArgsTypes);
                    Debug.Assert(mi.ReturnType.Equals(managedReturnType));

                    context.ilGenerator.Emit(OpCodes.Call, mi);
                }

                public override bool FoldConst(
                    ASTFunctionCall FunctionCall,
                    out object value)
                {
                    value = null;

                    List<object> args = new List<object>();
                    List<Type> argsTypes = new List<Type>();
                    ASTExpressionList arg = FunctionCall.ArgumentList.List;
                    while (arg != null)
                    {
                        ASTExpression argExpr = arg.First;
                        bool did = true;
                        while (did)
                        {
                            ASTExpression replacement;
                            argExpr.FoldConst(out did, out replacement);
                            if (replacement != null)
                            {
                                argExpr = replacement;
                            }
                        }
                        if (argExpr.Kind != Compiler.ExprKind.eExprOperand)
                        {
                            return false;
                        }
                        ASTOperand operand = argExpr.InnerOperand;
                        object argValue;
                        switch (operand.Kind)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case ASTOperandKind.eASTOperandSymbol:
                                return false;
                            case ASTOperandKind.eASTOperandIntegerLiteral:
                                argValue = operand.IntegerLiteralValue;
                                break;
                            case ASTOperandKind.eASTOperandBooleanLiteral:
                                argValue = operand.BooleanLiteralValue;
                                break;
                            case ASTOperandKind.eASTOperandSingleLiteral:
                                argValue = operand.SingleLiteralValue;
                                break;
                            case ASTOperandKind.eASTOperandDoubleLiteral:
                                argValue = operand.DoubleLiteralValue;
                                break;
                            case ASTOperandKind.eASTOperandStringLiteral:
                                argValue = operand.StringLiteralValue;
                                break;
                        }
                        args.Add(argValue);
                        argsTypes.Add(argValue.GetType());

                        arg = arg.Rest;
                    }

                    MethodInfo mi = container.GetMethod(name, argsTypes.ToArray());
                    Debug.Assert(mi != null);
                    value = mi.Invoke(null, args.ToArray());

                    return true;
                }
            }

            /* type check the function call node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;
                SymbolRec FunctionSymbol;
                SymbolListRec FunctionArgumentStepper;
                ASTExpressionList ExpressionListStepper;

                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                /* get the symbol for the function */
                Error = this.FunctionGenerator.GetInnerFunctionCallSymbol(out FunctionSymbol);
                if (Error != CompileErrors.eCompileNoError)
                {
                    ErrorLineNumber = this.LineNumber;
                    return Error;
                }

                // handle built-in functions
                {
                    int start, count;
                    if (!ASTFunctionCall.TryFindBuiltInFunctionRange(FunctionSymbol.SymbolName, out start, out count))
                    {
                        goto NotBuiltIn;
                    }

                    int argsCount = 0;
                    ExpressionListStepper = this.ArgumentList.List;
                    while (ExpressionListStepper != null)
                    {
                        argsCount++;
                        ExpressionListStepper = ExpressionListStepper.Rest;
                    }

                    List<BuiltInFunction> candidates = new List<BuiltInFunction>(count);
                    for (int i = 0; i < count; i++)
                    {
                        BuiltInFunction candidate = BuiltInFunctions[i + start];
                        Debug.Assert(String.Equals(FunctionSymbol.SymbolName, candidate.name));
                        if (argsCount == candidate.argsTypes.Length)
                        {
                            candidates.Add(candidate);
                        }
                    }
                    if (candidates.Count == 0)
                    {
                        goto NotBuiltIn;
                    }

                    int[] candidateTypePromotionScore = new int[candidates.Count];

                    int argIndex = 0;
                    ExpressionListStepper = this.ArgumentList.List;
                    while (ExpressionListStepper != null)
                    {
                        for (int i = 0; i < candidates.Count; i++)
                        {
                            DataTypes FormalType = candidates[i].argsTypes[argIndex];
                            DataTypes ActualType;
                            Error = ExpressionListStepper.First.TypeCheck(
                                out ActualType,
                                out ErrorLineNumber);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                return Error;
                            }
                            if (!CanRightBeMadeToMatchLeft(FormalType, ActualType))
                            {
                                candidateTypePromotionScore[i] = Int32.MinValue; // ineligible
                                continue;
                            }
                            if (MustRightBePromotedToLeft(FormalType, ActualType))
                            {
                                candidateTypePromotionScore[i]++;
                                continue;
                            }
                        }

                        argIndex++;
                        ExpressionListStepper = ExpressionListStepper.Rest;
                    }

                    // select first function in class of lowest needed count of type promotions
                    int minScore = Int32.MaxValue;
                    int minIndex = -1;
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        if (candidateTypePromotionScore[i] < 0)
                        {
                            continue; // skip ineligible
                        }
                        if (minScore > candidateTypePromotionScore[i])
                        {
                            minScore = candidateTypePromotionScore[i];
                            minIndex = i;
                        }
                    }
                    if (minIndex < 0)
                    {
                        // no eligible items
                        ErrorLineNumber = this.LineNumber;
                        return CompileErrors.eCompileArgumentTypeConflict;
                    }

                    // annotate AST for code gen pass
                    this.builtIn = candidates[minIndex];

                    // modify AST for required type promotions
                    argIndex = 0;
                    ExpressionListStepper = this.ArgumentList.List;
                    while (ExpressionListStepper != null)
                    {
                        DataTypes FormalType = candidates[minIndex].argsTypes[argIndex];
                        DataTypes ActualType;
                        Error = ExpressionListStepper.First.TypeCheck(
                            out ActualType,
                            out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }
                        if (!CanRightBeMadeToMatchLeft(FormalType, ActualType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileArgumentTypeConflict;
                        }
                        if (MustRightBePromotedToLeft(FormalType, ActualType))
                        {
                            /* consequent must be promoted to be same as alternate */
                            ASTExpression PromotedThing = PromoteTheExpression(
                                ActualType/*orig*/,
                                FormalType/*desired*/,
                                ExpressionListStepper.First,
                                this.LineNumber);
                            ExpressionListStepper.First = PromotedThing;
                            /* sanity check */
                            Error = ExpressionListStepper.First.TypeCheck(
                                out ActualType/*obtain new type*/,
                                out ErrorLineNumber);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                // type promotion caused failure
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                            if (ActualType != FormalType)
                            {
                                // after type promotion, types are no longer the same
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                        }

                        argIndex++;
                        ExpressionListStepper = ExpressionListStepper.Rest;
                    }

                    ResultingDataType = this.resultType = this.BuiltIn.returnType;
                    return CompileErrors.eCompileNoError;
                }

            NotBuiltIn:
                FunctionArgumentStepper = FunctionSymbol.FunctionArgList;
                ExpressionListStepper = this.ArgumentList.List;
                while ((FunctionArgumentStepper != null) && (ExpressionListStepper != null))
                {
                    DataTypes FormalType = FunctionArgumentStepper.First.VariableDataType;
                    DataTypes ActualType;
                    Error = ExpressionListStepper.First.TypeCheck(
                        out ActualType,
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    if (!CanRightBeMadeToMatchLeft(FormalType, ActualType))
                    {
                        ErrorLineNumber = this.LineNumber;
                        return CompileErrors.eCompileArgumentTypeConflict;
                    }
                    if (MustRightBePromotedToLeft(FormalType, ActualType))
                    {
                        /* consequent must be promoted to be same as alternate */
                        ASTExpression PromotedThing = PromoteTheExpression(
                            ActualType/*orig*/,
                            FormalType/*desired*/,
                            ExpressionListStepper.First,
                            this.LineNumber);
                        ExpressionListStepper.First = PromotedThing;
                        /* sanity check */
                        Error = ExpressionListStepper.First.TypeCheck(
                            out ActualType/*obtain new type*/,
                            out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            // type promotion caused failure
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (ActualType != FormalType)
                        {
                            // after type promotion, types are no longer the same
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                    }

                    /* advance to the next list thing */
                    ExpressionListStepper = ExpressionListStepper.Rest;
                    FunctionArgumentStepper = FunctionArgumentStepper.Rest;
                }
                if ((ExpressionListStepper != null) || (FunctionArgumentStepper != null))
                {
                    ErrorLineNumber = this.LineNumber;
                    return CompileErrors.eCompileWrongNumberOfArgsToFunction;
                }

                ResultingDataType = this.resultType = FunctionSymbol.FunctionReturnType;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int unused;

                int StackDepth = StackDepthParam;

                /* push each argument on stack; from left to right */
                this.ArgumentList.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);

                /* figure out how to call the function */
                ASTOperand FunctionContainer = this.FunctionGenerator.InnerOperand;
                SymbolRec FunctionSymbol = FunctionContainer.Symbol;
                if (this.BuiltIn != null)
                {
                    FuncCode.AddPcodeInstruction(this.BuiltIn.pcode, out unused, this.LineNumber);
                }
                else
                {
                    FuncCode.AddPcodeInstruction(Pcodes.epFuncCallUnresolved, out unused, this.LineNumber);
                    /* push function name */
                    FuncCode.AddPcodeOperandString(FunctionSymbol.SymbolName);
                    /* push a parameter list record for runtime checking */
                    SymbolListRec FormalArgumentList = FunctionSymbol.FunctionArgList;
                    /* now that we have the list, let's just check the stack for consistency */
                    if (StackDepth != StackDepthParam + SymbolListRec.GetLength(FormalArgumentList))
                    {
                        // stack depth error after pushing args
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    int NumberOfArguments = SymbolListRec.GetLength(FormalArgumentList);
                    DataTypes[] DataTypeArray = new DataTypes[NumberOfArguments];
                    int Index = 0;
                    while (FormalArgumentList != null)
                    {
                        DataTypeArray[Index] = FormalArgumentList.First.VariableDataType;
                        Index++;
                        FormalArgumentList = FormalArgumentList.Rest;
                    }
                    FuncCode.AddPcodeOperandDataTypeArray(DataTypeArray);
                    /* save the return type */
                    FuncCode.AddPcodeOperandInteger((int)FunctionSymbol.FunctionReturnType);
                    /* make an instruction operand for the reserved pcode link reference */
                    FuncCode.AddPcodeOperandInteger(-1);
                    // make operand for reserved maxstackdepth */
                    FuncCode.AddPcodeOperandInteger(-1);
                }
                /* increment stack pointer since there will be 1 returned value */
                StackDepthParam++;
                MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                ASTOperand FunctionContainer = this.FunctionGenerator.InnerOperand;
                SymbolRec FunctionSymbol = FunctionContainer.Symbol;

                if (this.BuiltIn != null)
                {
                    this.BuiltIn.emitter.Emit(cilObject, context, this);
                }
                else
                {
                    List<DataTypes> argsTypes = new List<DataTypes>();
                    SymbolListRec argsList = FunctionSymbol.FunctionArgList;
                    while (argsList != null)
                    {
                        argsTypes.Add(argsList.First.VariableDataType);
                        argsList = argsList.Rest;
                    }
                    DataTypes returnType = FunctionSymbol.FunctionReturnType;

                    int methodIndex = context.managedFunctionLinker.QueryFunctionIndex(FunctionSymbol.SymbolName);

                    // check function signature
                    int signatureIndex = context.managedFunctionLinker.QuerySignatureIndex(argsTypes.ToArray(), returnType);
                    context.ilGenerator.Emit(OpCodes.Call, typeof(CILThreadLocalStorage).GetMethod("get_CurrentFunctionSignatures", BindingFlags.Public | BindingFlags.Static));
                    context.ilGenerator.Emit(OpCodes.Ldc_I4, methodIndex);
                    context.ilGenerator.Emit(OpCodes.Ldelem, typeof(int));
                    context.ilGenerator.Emit(OpCodes.Ldc_I4, signatureIndex);
                    Label signatureGood = context.ilGenerator.DefineLabel();
                    context.ilGenerator.Emit(OpCodes.Beq, signatureGood);
                    context.ilGenerator.Emit(OpCodes.Newobj, typeof(CILObject.SignatureMismatchException).GetConstructor(new Type[] { }));
                    context.ilGenerator.Emit(OpCodes.Throw);
                    context.ilGenerator.MarkLabel(signatureGood);

                    /* push each argument on stack; from left to right */
                    this.ArgumentList.ILGen(
                        cilObject,
                        context);

                    // call the function

                    Type[] managedArgsTypes;
                    Type managedReturnType;
                    PcodeMarshal.GetManagedFunctionSignature(
                        argsTypes.ToArray(),
                        returnType,
                        out managedArgsTypes,
                        out managedReturnType);

                    context.ilGenerator.Emit(OpCodes.Call, typeof(CILThreadLocalStorage).GetMethod("get_CurrentFunctionPointers", BindingFlags.Public | BindingFlags.Static));
                    context.ilGenerator.Emit(OpCodes.Ldc_I4, methodIndex);
                    context.ilGenerator.Emit(OpCodes.Ldelem, typeof(IntPtr));
                    context.ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, managedReturnType, managedArgsTypes, null);
                }
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                replacementExpr = null;

                bool WeDidSomething = false;
                bool TheyDidSomething;

                /* optimize arglist */
                ASTExpression replacementArgsList;
                this.ArgumentList.FoldConst(
                    out TheyDidSomething,
                    out replacementArgsList);
                if (replacementArgsList != null)
                {
                    // args list is never reducible
                    Debug.Assert(false);
                    throw new InvalidCastException();
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                ASTExpression replacementFunctionGenerator;
                this.FunctionGenerator.FoldConst(
                    out TheyDidSomething,
                    out replacementFunctionGenerator);
                if (replacementFunctionGenerator != null)
                {
                    this.functionGenerator = replacementFunctionGenerator;
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                DidSomething = WeDidSomething;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("{0}{1}", functionGenerator, argumentList);
            }
#endif
        }




        public enum LoopKind
        {
            eInvalid,

            eLoopWhileDo,
            eLoopDoWhile,
        }

        public class ASTLoop : ASTBase
        {
            private readonly LoopKind kind;
            private ASTExpression controlExpression;
            private ASTExpression bodyExpression;
            private readonly int lineNumber;

            private DataTypes resultType;

            public LoopKind Kind { get { return kind; } }
            public ASTExpression ControlExpression { get { return controlExpression; } }
            public ASTExpression BodyExpression { get { return bodyExpression; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            public ASTLoop(
                LoopKind kind,
                ASTExpression ControlExpr,
                ASTExpression BodyExpr,
                int LineNumber)
            {
                this.lineNumber = LineNumber;
                this.kind = kind;
                this.controlExpression = ControlExpr;
                this.bodyExpression = BodyExpr;
            }

            /* type check the loop node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                DataTypes ConditionalType;
                Error = this.ControlExpression.TypeCheck(out ConditionalType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (ConditionalType != DataTypes.eBoolean)
                {
                    ErrorLineNumber = this.LineNumber;
                    return CompileErrors.eCompileConditionalMustBeBoolean;
                }

                DataTypes BodyType;
                Error = this.BodyExpression.TypeCheck(out BodyType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                ResultingDataType = this.resultType = BodyType;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int unused;

                int StackDepth = StackDepthParam;

                /* this is the loopie thing.  the only real difference between a while and */
                /* a repeat loop is that the while has an extra branch to the test. */
                /* repeat loop: */
                /*  - push default return value */
                /*  - pop previous value */
                /*  - evaluate body */
                /*  - perform test */
                /*  - branch if we keep looping to the pop previous value point */
                /* while loop: */
                /*  - push default return value */
                /*  - jump to the test */
                /*  - pop previous value */
                /*  - evaluate body */
                /*  - perform test */
                /*  - branch if we keep looping to the pop previous value point */

                /* push the default value -- same type as the body expression has */
                switch (this.BodyExpression.ResultType)
                {
                    default:
                        // bad type of body expression
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                    case DataTypes.eInteger:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, this.LineNumber);
                        FuncCode.AddPcodeOperandInteger(0);
                        break;
                    case DataTypes.eFloat:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateFloat, out unused, this.LineNumber);
                        FuncCode.AddPcodeOperandFloat(0);
                        break;
                    case DataTypes.eDouble:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateDouble, out unused, this.LineNumber);
                        FuncCode.AddPcodeOperandDouble(0);
                        break;
                    case DataTypes.eArrayOfBoolean:
                    case DataTypes.eArrayOfByte:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayByte, out unused, this.LineNumber);
                        break;
                    case DataTypes.eArrayOfInteger:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayInt32, out unused, this.LineNumber);
                        break;
                    case DataTypes.eArrayOfFloat:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayFloat, out unused, this.LineNumber);
                        break;
                    case DataTypes.eArrayOfDouble:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayDouble, out unused, this.LineNumber);
                        break;
                }
                StackDepth++;
                MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack depth error after pushing default value
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* if loop is a head-tested loop (while loop) then insert extra branch */
                int WhileBranchPatchupLocation = -1;
                if (this.Kind == LoopKind.eLoopWhileDo)
                {
                    FuncCode.AddPcodeInstruction(Pcodes.epBranchUnconditional, out WhileBranchPatchupLocation, this.LineNumber);
                    FuncCode.AddPcodeOperandInteger(-1/*target unknown*/);
                }

                /* remember this address! */
                int LoopBackAgainLocation = FuncCode.PcodeGetNextAddress();

                /* pop previous result */
                FuncCode.AddPcodeInstruction(Pcodes.epStackPop, out unused, this.LineNumber);
                StackDepth--;
                if (StackDepth != StackDepthParam)
                {
                    // stack depth error after popping previous
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* evaluate the body */
                this.BodyExpression.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack depth error after evaluating body
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* patch up the while branch */
                if (this.Kind == LoopKind.eLoopWhileDo)
                {
                    FuncCode.ResolvePcodeBranch(WhileBranchPatchupLocation, FuncCode.PcodeGetNextAddress());
                }

                /* evaluate the test */
                this.ControlExpression.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 2)
                {
                    // stack depth error after evaluating control
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* "while do" and "do while" need a branch if true conditional */
                FuncCode.AddPcodeInstruction(Pcodes.epBranchIfNotZero, out unused, this.LineNumber);
                FuncCode.AddPcodeOperandInteger(LoopBackAgainLocation);

                StackDepth--;
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack depth error after control branch
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                /* this is the loopie thing.  the only real difference between a while and */
                /* a repeat loop is that the while has an extra branch to the test. */
                /* repeat loop: */
                /*  - push default return value */
                /*  - pop previous value */
                /*  - evaluate body */
                /*  - perform test */
                /*  - branch if we keep looping to the pop previous value point */
                /* while loop: */
                /*  - push default return value */
                /*  - jump to the test */
                /*  - pop previous value */
                /*  - evaluate body */
                /*  - perform test */
                /*  - branch if we keep looping to the pop previous value point */

                /* push the default value -- same type as the body expression has */
                switch (this.BodyExpression.ResultType)
                {
                    default:
                        // bad type of body expression
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                    case DataTypes.eInteger:
                        context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case DataTypes.eFloat:
                        context.ilGenerator.Emit(OpCodes.Ldc_R4, (float)0);
                        break;
                    case DataTypes.eDouble:
                        context.ilGenerator.Emit(OpCodes.Ldc_R8, (double)0);
                        break;
                    case DataTypes.eArrayOfBoolean:
                    case DataTypes.eArrayOfByte:
                        context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleByte).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                        break;
                    case DataTypes.eArrayOfInteger:
                        context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleInt32).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                        break;
                    case DataTypes.eArrayOfFloat:
                        context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleFloat).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                        break;
                    case DataTypes.eArrayOfDouble:
                        context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleDouble).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                        break;
                }

                /* if loop is a head-tested loop (while loop) then insert extra branch */
                Label WhileBranchPatchupLocation = new Label();
                if (this.Kind == LoopKind.eLoopWhileDo)
                {
                    WhileBranchPatchupLocation = context.ilGenerator.DefineLabel();
                    context.ilGenerator.Emit(OpCodes.Br, WhileBranchPatchupLocation);
                }

                /* remember this address! */
                Label LoopBackAgainLocation = context.ilGenerator.DefineLabel();
                context.ilGenerator.MarkLabel(LoopBackAgainLocation);

                /* pop previous result */
                context.ilGenerator.Emit(OpCodes.Pop);

                /* evaluate the body */
                this.BodyExpression.ILGen(
                    cilObject,
                    context);

                /* patch up the while branch */
                if (this.Kind == LoopKind.eLoopWhileDo)
                {
                    context.ilGenerator.MarkLabel(WhileBranchPatchupLocation);
                }

                /* evaluate the test */
                this.ControlExpression.ILGen(
                    cilObject,
                    context);

                /* "while do" and "do while" need a branch if true conditional */
                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                context.ilGenerator.Emit(OpCodes.Bne_Un, LoopBackAgainLocation);
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression ResultantExpr)
            {
                bool WeDidSomething = false;
                bool TheyDidSomething;

                ResultantExpr = null;
                DataTypes LocalResultType = this.BodyExpression.ResultType;

                ASTExpression controlExpressionReplacement;
                this.ControlExpression.FoldConst(out TheyDidSomething, out controlExpressionReplacement);
                if (controlExpressionReplacement != null)
                {
                    this.controlExpression = controlExpressionReplacement;
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                ASTExpression bodyExpressionReplacement;
                this.BodyExpression.FoldConst(out TheyDidSomething, out bodyExpressionReplacement);
                if (bodyExpressionReplacement != null)
                {
                    this.bodyExpression = bodyExpressionReplacement;
                }
                WeDidSomething = WeDidSomething || TheyDidSomething;

                /* can we eliminate the iterations? */
                bool LoopWasRemoved = false;
                if ((this.ControlExpression.Kind == ExprKind.eExprOperand)
                    && (this.ControlExpression.InnerOperand.Kind == ASTOperandKind.eASTOperandBooleanLiteral))
                {
                    switch (this.Kind)
                    {
                        default:
                            // invalid loop type
                            Debug.Assert(false);
                            throw new InvalidOperationException();

                        case LoopKind.eLoopWhileDo:
                            if (!this.ControlExpression.InnerOperand.BooleanLiteralValue)
                            {
                                ASTOperand NewOperand = null;

                                /* if cond is false then we can return zero since nothing happens */
                                switch (this.BodyExpression.ResultType)
                                {
                                    default:
                                        // bad data type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eArrayOfBoolean:
                                    case DataTypes.eArrayOfByte:
                                    case DataTypes.eArrayOfInteger:
                                    case DataTypes.eArrayOfFloat:
                                    case DataTypes.eArrayOfDouble:
                                        break;
                                    case DataTypes.eBoolean:
                                        NewOperand = new ASTOperand(
                                            false,
                                            this.LineNumber);
                                        break;
                                    case DataTypes.eInteger:
                                        NewOperand = new ASTOperand(
                                            0,
                                            this.LineNumber);
                                        break;
                                    case DataTypes.eFloat:
                                        NewOperand = new ASTOperand(
                                            0,
                                            this.LineNumber);
                                        break;
                                    case DataTypes.eDouble:
                                        NewOperand = new ASTOperand(
                                            0,
                                            this.LineNumber);
                                        break;
                                }

                                if (NewOperand != null)
                                {
                                    ResultantExpr = new ASTExpression(
                                        NewOperand,
                                        this.LineNumber);
                                    WeDidSomething = true;
                                    LoopWasRemoved = true;
                                }
                            }
                            break;

                        case LoopKind.eLoopDoWhile:
                            if (!this.ControlExpression.InnerOperand.BooleanLiteralValue)
                            {
                                /* if cond is false then we can substitute the loop body */
                                ResultantExpr = this.BodyExpression;
                                LoopWasRemoved = true;
                            }
                            break;
                    }
                }
                if (!LoopWasRemoved)
                {
                    /* couldn't eliminate the loop */
                    ResultantExpr = new ASTExpression(
                        this,
                        this.LineNumber);
                }

                DidSomething = WeDidSomething;
            }

#if DEBUG
            public override string ToString()
            {
                switch (kind)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case LoopKind.eLoopDoWhile:
                        return String.Format("while {0} do {1}", controlExpression, bodyExpression);
                    case LoopKind.eLoopWhileDo:
                        return String.Format("do {1} while {0}", controlExpression, bodyExpression);
                }
            }
#endif
        }




        /* operand types */
        public enum ASTOperandKind
        {
            eInvalid,

            eASTOperandIntegerLiteral,
            eASTOperandBooleanLiteral,
            eASTOperandSingleLiteral,
            eASTOperandDoubleLiteral,
            eASTOperandSymbol,
            eASTOperandStringLiteral,
        }

        public class ASTOperand : ASTBase
        {
            private readonly ASTOperandKind kind;
            private readonly int lineNumber;
            private readonly object u;

            public ASTOperandKind Kind { get { return kind; } }
            public object U { get { return u; } }
            public override int LineNumber { get { return lineNumber; } }


            public ASTOperand(
                int IntegerLiteralValue,
                int LineNumber)
            {
                this.kind = ASTOperandKind.eASTOperandIntegerLiteral;
                this.lineNumber = LineNumber;
                this.u = IntegerLiteralValue;
            }

            public ASTOperand(
                bool BooleanLiteralValue,
                int LineNumber)
            {
                this.kind = ASTOperandKind.eASTOperandBooleanLiteral;
                this.lineNumber = LineNumber;
                this.u = BooleanLiteralValue;
            }

            public ASTOperand(
                float SingleLiteralValue,
                int LineNumber)
            {
                this.kind = ASTOperandKind.eASTOperandSingleLiteral;
                this.lineNumber = LineNumber;
                this.u = SingleLiteralValue;
            }

            public ASTOperand(
                double DoubleLiteralValue,
                int LineNumber)
            {
                this.kind = ASTOperandKind.eASTOperandDoubleLiteral;
                this.lineNumber = LineNumber;
                this.u = DoubleLiteralValue;
            }

            public ASTOperand(
                SymbolRec SymbolTableEntry,
                int LineNumber)
            {
                this.kind = ASTOperandKind.eASTOperandSymbol;
                this.lineNumber = LineNumber;
                this.u = SymbolTableEntry;
            }

            public ASTOperand(
                string stringLiteralValue,
                int LineNumber)
            {
                this.kind = ASTOperandKind.eASTOperandStringLiteral;
                this.lineNumber = LineNumber;
                this.u = stringLiteralValue;
            }


            public override DataTypes ResultType
            {
                get
                {
                    switch (kind)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();

                        case ASTOperandKind.eASTOperandIntegerLiteral:
                            return DataTypes.eInteger;
                        case ASTOperandKind.eASTOperandBooleanLiteral:
                            return DataTypes.eBoolean;
                        case ASTOperandKind.eASTOperandSingleLiteral:
                            return DataTypes.eFloat;
                        case ASTOperandKind.eASTOperandDoubleLiteral:
                            return DataTypes.eDouble;
                        case ASTOperandKind.eASTOperandStringLiteral:
                            return DataTypes.eArrayOfByte;

                        case ASTOperandKind.eASTOperandSymbol:
                            switch (this.Symbol.Kind)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case SymbolKind.Function:
                                    return this.Symbol.FunctionReturnType;
                                case SymbolKind.Variable:
                                    return this.Symbol.VariableDataType;
                            }
                    }
                }
            }


            public int IntegerLiteralValue
            {
                get
                {
                    if ((this.Kind != ASTOperandKind.eASTOperandIntegerLiteral) || !(this.U is int))
                    {
                        // not an integer literal
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (int)this.U;
                }
            }

            public bool BooleanLiteralValue
            {
                get
                {
                    if ((this.Kind != ASTOperandKind.eASTOperandBooleanLiteral) || !(this.U is bool))
                    {
                        // not a boolean literal
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (bool)this.U;
                }
            }

            public float SingleLiteralValue
            {
                get
                {
                    if ((this.Kind != ASTOperandKind.eASTOperandSingleLiteral) || !(this.U is float))
                    {
                        // not a single literal
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (float)this.U;
                }
            }

            public double DoubleLiteralValue
            {
                get
                {
                    if ((this.Kind != ASTOperandKind.eASTOperandDoubleLiteral) || !(this.U is double))
                    {
                        // not a double literal
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (double)this.U;
                }
            }

            public string StringLiteralValue
            {
                get
                {
                    if ((this.Kind != ASTOperandKind.eASTOperandStringLiteral) || !(this.U is string))
                    {
                        // not a string literal
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (string)this.U;
                }
            }

            public bool IsSymbol
            {
                get
                {
                    return this.Kind == ASTOperandKind.eASTOperandSymbol;
                }
            }

            public SymbolRec Symbol
            {
                get
                {
                    if (!this.IsSymbol || !(this.U is SymbolRec))
                    {
                        // not a symbolic operand
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    return (SymbolRec)this.U;
                }
            }

            /* type check the operand node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                switch (this.Kind)
                {
                    default:
                        // unknown operand kind
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case ASTOperandKind.eASTOperandIntegerLiteral:
                    case ASTOperandKind.eASTOperandBooleanLiteral:
                    case ASTOperandKind.eASTOperandSingleLiteral:
                    case ASTOperandKind.eASTOperandDoubleLiteral:
                    case ASTOperandKind.eASTOperandStringLiteral:
                        ResultingDataType = this.ResultType;
                        break;

                    case ASTOperandKind.eASTOperandSymbol:
                        if (this.Symbol.Kind != SymbolKind.Variable)
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileMustBeAVariableIdentifier;
                        }
                        ResultingDataType = this.Symbol.VariableDataType;
                        break;
                }

                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int unused;

                int StackDepth = StackDepthParam;

                switch (this.Kind)
                {
                    default:
                        // unknown operand type
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case ASTOperandKind.eASTOperandIntegerLiteral:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, this.LineNumber);
                        FuncCode.AddPcodeOperandInteger(this.IntegerLiteralValue);
                        break;
                    case ASTOperandKind.eASTOperandBooleanLiteral:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, this.LineNumber);
                        FuncCode.AddPcodeOperandInteger(this.BooleanLiteralValue ? 1 : 0);
                        break;
                    case ASTOperandKind.eASTOperandSingleLiteral:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateFloat, out unused, this.LineNumber);
                        FuncCode.AddPcodeOperandFloat(this.SingleLiteralValue);
                        break;
                    case ASTOperandKind.eASTOperandDoubleLiteral:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateDouble, out unused, this.LineNumber);
                        FuncCode.AddPcodeOperandDouble(this.DoubleLiteralValue);
                        break;

                    /* this had better be a variable */
                    case ASTOperandKind.eASTOperandSymbol:
                        switch (this.Symbol.VariableDataType)
                        {
                            default:
                                // bad variable type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                            case DataTypes.eInteger:
                                FuncCode.AddPcodeInstruction(Pcodes.epLoadIntegerFromStack, out unused, this.LineNumber);
                                break;
                            case DataTypes.eFloat:
                                FuncCode.AddPcodeInstruction(Pcodes.epLoadFloatFromStack, out unused, this.LineNumber);
                                break;
                            case DataTypes.eDouble:
                                FuncCode.AddPcodeInstruction(Pcodes.epLoadDoubleFromStack, out unused, this.LineNumber);
                                break;
                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                            case DataTypes.eArrayOfInteger:
                            case DataTypes.eArrayOfFloat:
                            case DataTypes.eArrayOfDouble:
                                FuncCode.AddPcodeInstruction(Pcodes.epLoadArrayFromStack, out unused, this.LineNumber);
                                break;
                        }
                        /* stack offsets are negative */
                        FuncCode.AddPcodeOperandInteger(this.Symbol.SymbolVariableStackLocation - StackDepth);
                        break;

                    case ASTOperandKind.eASTOperandStringLiteral:
                        FuncCode.AddPcodeInstruction(
                            Pcodes.epMakeByteArrayFromString,
                            out unused,
                            this.LineNumber);
                        FuncCode.AddPcodeOperandString(
                            this.StringLiteralValue);
                        break;
                }
                StackDepth++;
                MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack depth error after pushing operand
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                switch (this.Kind)
                {
                    default:
                        // unknown operand type
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case ASTOperandKind.eASTOperandIntegerLiteral:
                        context.ilGenerator.Emit(OpCodes.Ldc_I4, this.IntegerLiteralValue);
                        break;
                    case ASTOperandKind.eASTOperandBooleanLiteral:
                        context.ilGenerator.Emit(this.BooleanLiteralValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        break;
                    case ASTOperandKind.eASTOperandSingleLiteral:
                        context.ilGenerator.Emit(OpCodes.Ldc_R4, this.SingleLiteralValue);
                        break;
                    case ASTOperandKind.eASTOperandDoubleLiteral:
                        context.ilGenerator.Emit(OpCodes.Ldc_R8, this.DoubleLiteralValue);
                        break;

                    /* this had better be a variable */
                    case ASTOperandKind.eASTOperandSymbol:
                        if (context.variableTable.ContainsKey(this.Symbol)) // local variables can mask arguments
                        {
                            // local variable
                            context.ilGenerator.Emit(OpCodes.Ldloc, context.variableTable[this.Symbol]);
                        }
                        else
                        {
                            // function argument
                            if (!context.argsByRef)
                            {
                                context.ilGenerator.Emit(OpCodes.Ldarg, (short)context.argumentTable[this.Symbol.SymbolName]); // operand is 'short' -- see https://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes.ldarg(v=vs.110).aspx
                            }
                            else
                            {
                                context.ilGenerator.Emit(OpCodes.Ldloc, context.localArgMap[this.Symbol.SymbolName]);
                            }
                        }
                        break;

                    case ASTOperandKind.eASTOperandStringLiteral:
                        context.ilGenerator.Emit(OpCodes.Call, typeof(Encoding).GetMethod("get_UTF8"));
                        context.ilGenerator.Emit(OpCodes.Ldstr, this.StringLiteralValue);
                        context.ilGenerator.Emit(OpCodes.Call, typeof(Encoding).GetMethod("GetBytes", new Type[] { typeof(string) }));
                        context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleByte).GetConstructor(new Type[] { typeof(byte[]) }));
                        break;
                }
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                DidSomething = false;
                replacementExpr = null;
            }

#if DEBUG
            public override string ToString()
            {
                switch (kind)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case ASTOperandKind.eASTOperandIntegerLiteral:
                    case ASTOperandKind.eASTOperandSymbol:
                        return String.Format("{0}", u);
                    case ASTOperandKind.eASTOperandBooleanLiteral:
                        return String.Format("{0}", u.ToString().ToLower());
                    case ASTOperandKind.eASTOperandSingleLiteral:
                        return String.Format("{0}f", u);
                    case ASTOperandKind.eASTOperandDoubleLiteral:
                        return String.Format("{0}d", u);
                    case ASTOperandKind.eASTOperandStringLiteral:
                        return String.Format("\"{0}\"", u);
                }
            }
#endif
        }




        public class ASTPrintExpression : ASTBase
        {
            private ASTExpression value;
            private readonly int lineNumber;
            private DataTypes expressionType;

            private DataTypes resultType;

            public ASTExpression Value { get { return value; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            public ASTPrintExpression(
                ASTExpression Expression,
                int LineNumber)
            {
                this.value = Expression;
                this.lineNumber = LineNumber;
            }

            /* type check the expr print node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                Error = this.Value.TypeCheck(out this.expressionType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                switch (this.expressionType)
                {
                    default:
                        // unknown expression type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                        break;
                    case DataTypes.eDouble:
                    case DataTypes.eInteger:
                    case DataTypes.eFloat:
                        if (!CanRightBeMadeToMatchLeft(DataTypes.eDouble, this.expressionType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileOperandMustBeDouble;
                        }
                        if (MustRightBePromotedToLeft(DataTypes.eDouble, this.expressionType))
                        {
                            /* we must promote the right operand to become the left operand type */
                            ASTExpression PromotedOperand = PromoteTheExpression(
                                this.expressionType/*orig*/,
                                DataTypes.eDouble/*desired*/,
                                this.Value,
                                this.LineNumber);
                            this.value = PromotedOperand;
                            /* sanity check */
                            Error = this.Value.TypeCheck(
                                out this.expressionType/*obtain new type*/,
                                out ErrorLineNumber);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                // type promotion caused failure
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                            if (DataTypes.eDouble != this.expressionType)
                            {
                                // after type promotion, types are no longer the same
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                        }
                        break;
                    case DataTypes.eArrayOfBoolean:
                    case DataTypes.eArrayOfByte:
                    case DataTypes.eArrayOfInteger:
                    case DataTypes.eArrayOfFloat:
                    case DataTypes.eArrayOfDouble:
                        ErrorLineNumber = this.LineNumber;
                        return CompileErrors.eCompileOperandsMustBeScalar;
                }

                ResultingDataType = this.resultType = DataTypes.eBoolean;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                string methodName;
                DataTypes argType;

                switch (this.expressionType)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                        methodName = "PrintBool";
                        argType = DataTypes.eBoolean;
                        break;
                    case DataTypes.eDouble:
                        methodName = "PrintDouble";
                        argType = DataTypes.eDouble;
                        break;
                }

                PcodeGenExternCall(
                    FuncCode,
                    ref StackDepthParam,
                    ref MaxStackDepth,
                    this.LineNumber,
                    methodName,
                    new ASTExpression[1]
                    {
                        this.Value,
                    },
                    new DataTypes[1]
                    {
                        argType/*0: value*/,
                    },
                    argType/*return type/value same as arg*/);
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                string methodName;
                DataTypes argType;

                switch (this.expressionType)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                        methodName = "PrintBool";
                        argType = DataTypes.eBoolean;
                        break;
                    case DataTypes.eDouble:
                        methodName = "PrintDouble";
                        argType = DataTypes.eDouble;
                        break;
                }

                ILGenExternCall(
                    cilObject,
                    context,
                    this.LineNumber,
                    methodName,
                    new ASTExpression[1]
                    {
                        this.Value,
                    },
                    new DataTypes[1]
                    {
                        argType/*0: value*/,
                    },
                    argType/*return type/value same as arg*/);
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                ASTExpression valueReplacement;
                this.Value.FoldConst(out DidSomething, out valueReplacement);
                if (valueReplacement != null)
                {
                    this.value = valueReplacement;
                }

                replacementExpr = null;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("print {0}", value);
            }
#endif
        }




        public class ASTPrintString : ASTBase
        {
            private readonly string messageString;
            private readonly int lineNumber;

            private DataTypes resultType;

            public string MessageString { get { return messageString; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            public ASTPrintString(
                string messageString,
                int lineNumber)
            {
                this.messageString = messageString;
                this.lineNumber = lineNumber;
            }

            /* type check the string print node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                ErrorLineNumber = -1;

                ResultingDataType = this.resultType = DataTypes.eBoolean;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                ASTExpression messageStringExpression = new ASTExpression(this.MessageString, this.LineNumber);

                PcodeGenExternCall(
                    FuncCode,
                    ref StackDepthParam,
                    ref MaxStackDepth,
                    this.LineNumber,
                    "PrintString",
                    new ASTExpression[1]
                    {
                        messageStringExpression,
                    },
                    new DataTypes[1]
                    {
                        DataTypes.eArrayOfByte/*0: value*/,
                    },
                    DataTypes.eBoolean);
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                ASTExpression messageStringExpression = new ASTExpression(this.MessageString, this.LineNumber);

                ILGenExternCall(
                    cilObject,
                    context,
                    this.LineNumber,
                    "PrintString",
                    new ASTExpression[1]
                    {
                        messageStringExpression,
                    },
                    new DataTypes[1]
                    {
                        DataTypes.eArrayOfByte/*0: value*/,
                    },
                    DataTypes.eBoolean);
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                DidSomething = false;
                replacementExpr = null;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("print \"{0}\"", messageString);
            }
#endif
        }




        public enum SampleLoaderKind
        {
            eInvalid,

            eSampleLoaderSampleLeft,
            eSampleLoaderSampleRight,
            eSampleLoaderSampleMono,
        }

        public class ASTSampleLoader : ASTBase
        {
            private readonly string fileType;
            private readonly string sampleName;
            private readonly SampleLoaderKind kind;
            private readonly int lineNumber;

            public string FileType { get { return fileType; } }
            public string SampleName { get { return sampleName; } }
            public SampleLoaderKind Kind { get { return kind; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return DataTypes.eArrayOfFloat; } }


            public ASTSampleLoader(
                string FileType,
                string SampleName,
                SampleLoaderKind kind,
                int LineNumber)
            {
                this.fileType = FileType;
                this.sampleName = SampleName;
                this.kind = kind;
                this.lineNumber = LineNumber;
            }

            /* type check the sample loader node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                ErrorLineNumber = -1;

                ResultingDataType = DataTypes.eArrayOfFloat;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                ASTExpression fileTypeStringExpression = new ASTExpression(this.FileType, this.LineNumber);
                ASTExpression fileNameStringExpression = new ASTExpression(this.SampleName, this.LineNumber);

                string WhichMethod;
                switch (this.Kind)
                {
                    default:
                        // bad opcode
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case SampleLoaderKind.eSampleLoaderSampleLeft:
                        WhichMethod = "LoadSampleLeftArray";
                        break;
                    case SampleLoaderKind.eSampleLoaderSampleRight:
                        WhichMethod = "LoadSampleRightArray";
                        break;
                    case SampleLoaderKind.eSampleLoaderSampleMono:
                        WhichMethod = "LoadSampleMonoArray";
                        break;
                }

                PcodeGenExternCall(
                    FuncCode,
                    ref StackDepthParam,
                    ref MaxStackDepth,
                    this.LineNumber,
                    WhichMethod,
                    new ASTExpression[2]
                    {
                        fileTypeStringExpression,
                        fileNameStringExpression,
                    },
                    new DataTypes[]
                    {
                        DataTypes.eArrayOfByte/*0: type*/,
                        DataTypes.eArrayOfByte/*1: name*/,
                    },
                    DataTypes.eArrayOfFloat);
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                ASTExpression fileTypeStringExpression = new ASTExpression(this.FileType, this.LineNumber);
                ASTExpression fileNameStringExpression = new ASTExpression(this.SampleName, this.LineNumber);

                string WhichMethod;
                switch (this.Kind)
                {
                    default:
                        // bad opcode
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case SampleLoaderKind.eSampleLoaderSampleLeft:
                        WhichMethod = "LoadSampleLeftArray";
                        break;
                    case SampleLoaderKind.eSampleLoaderSampleRight:
                        WhichMethod = "LoadSampleRightArray";
                        break;
                    case SampleLoaderKind.eSampleLoaderSampleMono:
                        WhichMethod = "LoadSampleMonoArray";
                        break;
                }

                ILGenExternCall(
                    cilObject,
                    context,
                    this.LineNumber,
                    WhichMethod,
                    new ASTExpression[2]
                    {
                        fileTypeStringExpression,
                        fileNameStringExpression,
                    },
                    new DataTypes[]
                    {
                        DataTypes.eArrayOfByte/*0: type*/,
                        DataTypes.eArrayOfByte/*1: name*/,
                    },
                    DataTypes.eArrayOfFloat);
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                DidSomething = false;
                replacementExpr = null;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("loadsample({0}, \"{1}\", \"{2}\"", kind, sampleName, fileType);
            }
#endif
        }




        public enum UnaryOpKind
        {
            eInvalid,

            eUnaryNegation,
            eUnaryNot,
            eUnarySine,
            eUnaryCosine,
            eUnaryTangent,
            eUnaryArcSine,
            eUnaryArcCosine,
            eUnaryArcTangent,
            eUnaryLogarithm,
            eUnaryExponentiation,
            eUnaryFloor,
            eUnaryCeil,
            eUnaryRound,
            eUnaryCosh,
            eUnarySinh,
            eUnaryTanh,
            eUnaryCastToBoolean,
            eUnaryCastToInteger,
            eUnaryCastToSingle,
            eUnaryCastToDouble,
            eUnarySquare,
            eUnarySquareRoot,
            eUnaryAbsoluteValue,
            eUnaryTestNegative,
            eUnaryGetSign,
            eUnaryGetArrayLength,
            eUnaryDuplicateArray,
        }

        public class ASTUnaryOperation : ASTBase
        {
            private readonly UnaryOpKind kind;
            private ASTExpression argument;
            private readonly int lineNumber;
            private readonly bool explicitCast; // true: user wrote it, false: automatically inserted

            private DataTypes resultType;

            public UnaryOpKind Kind { get { return kind; } }
            public ASTExpression Argument { get { return argument; } }
            public override int LineNumber { get { return lineNumber; } }
            public bool ExplicitCast { get { return explicitCast; } } // true: user wrote it, false: automatically inserted
            public override DataTypes ResultType { get { return resultType; } }


            public ASTUnaryOperation(
                UnaryOpKind kind,
                ASTExpression Argument,
                int LineNumber)
            {
                this.lineNumber = LineNumber;
                this.kind = kind;
                this.argument = Argument;
            }

            public ASTUnaryOperation(
                UnaryOpKind WhatOperation,
                ASTExpression Argument,
                int LineNumber,
                bool explicitCast)
            {
                this.lineNumber = LineNumber;
                this.kind = WhatOperation;
                this.argument = Argument;
                this.explicitCast = explicitCast;
            }


            /* type check the unary operator node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                /* check the argument and find out what type it is */
                DataTypes ArgumentType;
                Error = this.Argument.TypeCheck(out ArgumentType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                /* do type checking and promotion; return type determination is deferred... */
                switch (this.Kind)
                {
                    default:
                        // unknown opcode
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    /* operators which take an integer, single, double, or fixed and */
                    /* return a boolean */
                    case UnaryOpKind.eUnaryTestNegative:
                    /* FALL THROUGH */

                    /* operators which take an integer, single, double, or fixed and */
                    /* return an integer */
                    case UnaryOpKind.eUnaryGetSign:
                    /* FALL THROUGH */

                    /* operators capable of integer, single, double, fixed arguments */
                    /* which return the same type as the argument */
                    case UnaryOpKind.eUnaryNegation:
                    case UnaryOpKind.eUnaryAbsoluteValue:
                        if (!IsSequencedScalarType(ArgumentType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileOperandsMustBeSequencedScalar;
                        }
                        break;

                    /* operators capable of boolean or integer arguments which */
                    /* return the same type as the argument */
                    case UnaryOpKind.eUnaryNot:
                        if ((ArgumentType != DataTypes.eBoolean) && (ArgumentType != DataTypes.eInteger))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileOperandMustBeBooleanOrInteger;
                        }
                        break;

                    /* operators capable of float/double arguments which returns same */
                    case UnaryOpKind.eUnarySine:
                    case UnaryOpKind.eUnaryCosine:
                    case UnaryOpKind.eUnaryTangent:
                    case UnaryOpKind.eUnaryArcSine:
                    case UnaryOpKind.eUnaryArcCosine:
                    case UnaryOpKind.eUnaryArcTangent:
                    case UnaryOpKind.eUnaryLogarithm:
                    case UnaryOpKind.eUnaryExponentiation:
                    case UnaryOpKind.eUnarySquare:
                    case UnaryOpKind.eUnarySquareRoot:
                    case UnaryOpKind.eUnaryFloor:
                    case UnaryOpKind.eUnaryCeil:
                    case UnaryOpKind.eUnaryRound:
                    case UnaryOpKind.eUnaryCosh:
                    case UnaryOpKind.eUnarySinh:
                    case UnaryOpKind.eUnaryTanh:
                        DataTypes targetType;
                        if (CanRightBeMadeToMatchLeft(DataTypes.eFloat, ArgumentType))
                        {
                            targetType = DataTypes.eFloat;
                        }
                        else if (CanRightBeMadeToMatchLeft(DataTypes.eDouble, ArgumentType))
                        {
                            targetType = DataTypes.eDouble;
                        }
                        else
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileOperandMustBeFloatOrDouble;
                        }
                        if (MustRightBePromotedToLeft(targetType, ArgumentType))
                        {
                            /* we must promote the right operand to become the left operand type */
                            ASTExpression PromotedOperand = PromoteTheExpression(
                                ArgumentType/*orig*/,
                                targetType/*desired*/,
                                this.Argument,
                                this.LineNumber);
                            this.argument = PromotedOperand;
                            /* sanity check */
                            Error = this.Argument.TypeCheck(
                                out ArgumentType/*obtain new right type*/,
                                out ErrorLineNumber);
                            if (Error != CompileErrors.eCompileNoError)
                            {
                                // type promotion caused failure
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                            if (targetType != ArgumentType)
                            {
                                // after type promotion, types are no longer the same
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }
                        }
                        break;

                    /* operands which take a boolean, integer, single, double, or fixed */
                    /* and return an integer */
                    case UnaryOpKind.eUnaryCastToInteger:
                    /* FALL THROUGH */

                    /* operators which take a boolean, integer, single, double, or fixed */
                    /* and return a single */
                    case UnaryOpKind.eUnaryCastToSingle:
                    /* FALL THROUGH */

                    /* operators which take a boolean, integer, single, double, or fixed */
                    /* and return a double */
                    case UnaryOpKind.eUnaryCastToDouble:
                    /* FALL THROUGH */

                    /* operators which take a boolean, integer, single, double, or fixed */
                    /* and return a boolean */
                    case UnaryOpKind.eUnaryCastToBoolean:
                        if (!IsScalarType(ArgumentType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileOperandsMustBeScalar;
                        }
                        break;

                    /* operators which take an array and return an integer */
                    case UnaryOpKind.eUnaryGetArrayLength:
                    /* FALL THROUGH */

                    /* operators which take an array and return an array of the same type */
                    case UnaryOpKind.eUnaryDuplicateArray:
                        if (!IsIndexedType(ArgumentType))
                        {
                            ErrorLineNumber = this.LineNumber;
                            return CompileErrors.eCompileArrayRequiredForGetLength;
                        }
                        break;
                }

                /* figure out the return type */
                switch (this.Kind)
                {
                    default:
                        // unknown opcode
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    /* operators which take an integer, single, double, or fixed and */
                    /* return a boolean */
                    case UnaryOpKind.eUnaryTestNegative:
                        if (!IsSequencedScalarType(ArgumentType))
                        {
                            // arg should be seq scalar but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eBoolean;
                        break;

                    /* operators which take an integer, single, double, or fixed and */
                    /* return an integer */
                    case UnaryOpKind.eUnaryGetSign:
                        if (!IsSequencedScalarType(ArgumentType))
                        {
                            // arg should be seq scalar but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eInteger;
                        break;

                    /* operators capable of integer, single, double, fixed arguments */
                    /* which return the same type as the argument */
                    case UnaryOpKind.eUnaryNegation:
                    case UnaryOpKind.eUnaryAbsoluteValue:
                        if (!IsSequencedScalarType(ArgumentType))
                        {
                            // arg should be seq scalar but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = ArgumentType;
                        break;

                    /* operators capable of boolean or integer arguments which */
                    /* return the same type as the argument */
                    case UnaryOpKind.eUnaryNot:
                        if ((ArgumentType != DataTypes.eBoolean) && (ArgumentType != DataTypes.eInteger))
                        {
                            // arg should be int or bool
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = ArgumentType;
                        break;

                    /* operators capable of float/double arguments which returns same */
                    case UnaryOpKind.eUnarySine:
                    case UnaryOpKind.eUnaryCosine:
                    case UnaryOpKind.eUnaryTangent:
                    case UnaryOpKind.eUnaryArcSine:
                    case UnaryOpKind.eUnaryArcCosine:
                    case UnaryOpKind.eUnaryArcTangent:
                    case UnaryOpKind.eUnaryLogarithm:
                    case UnaryOpKind.eUnaryExponentiation:
                    case UnaryOpKind.eUnarySquare:
                    case UnaryOpKind.eUnarySquareRoot:
                    case UnaryOpKind.eUnaryFloor:
                    case UnaryOpKind.eUnaryCeil:
                    case UnaryOpKind.eUnaryRound:
                    case UnaryOpKind.eUnaryCosh:
                    case UnaryOpKind.eUnarySinh:
                    case UnaryOpKind.eUnaryTanh:
                        if ((ArgumentType != DataTypes.eFloat) && (ArgumentType != DataTypes.eDouble))
                        {
                            // arg should be float or double but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = ArgumentType;
                        break;

                    /* operands which take a boolean, integer, single, double, or fixed */
                    /* and return an integer */
                    case UnaryOpKind.eUnaryCastToInteger:
                        if (!IsScalarType(ArgumentType))
                        {
                            // arg should be scalar but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eInteger;
                        break;

                    /* operators which take a boolean, integer, single, double, or fixed */
                    /* and return a single */
                    case UnaryOpKind.eUnaryCastToSingle:
                        if (!IsScalarType(ArgumentType))
                        {
                            // arg should be scalar but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eFloat;
                        break;

                    /* operators which take a boolean, integer, single, double, or fixed */
                    /* and return a double */
                    case UnaryOpKind.eUnaryCastToDouble:
                        if (!IsScalarType(ArgumentType))
                        {
                            // arg should be scalar but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eDouble;
                        break;

                    /* operators which take a boolean, integer, single, double, or fixed */
                    /* and return a boolean */
                    case UnaryOpKind.eUnaryCastToBoolean:
                        if (!IsScalarType(ArgumentType))
                        {
                            // arg should be scalar but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eBoolean;
                        break;

                    /* operators which take an array and return an integer */
                    case UnaryOpKind.eUnaryGetArrayLength:
                        if (!IsIndexedType(ArgumentType))
                        {
                            // arg should be indexable but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = DataTypes.eInteger;
                        break;

                    /* operators which take an array and return an array of the same type */
                    case UnaryOpKind.eUnaryDuplicateArray:
                        if (!IsIndexedType(ArgumentType))
                        {
                            // arg should be indexable but isn't
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        ResultingDataType = ArgumentType;
                        break;
                }

                this.resultType = ResultingDataType;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                Pcodes Opcode;

                int StackDepth = StackDepthParam;

                /* evaluate the argument */
                this.Argument.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack depth error after evaluating argument
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* generate the operation code */
                switch (this.Kind)
                {
                    default:
                        // bad opcode
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case UnaryOpKind.eUnaryNegation:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryNegation]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerNegation;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatNegation;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleNegation;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryNot:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryNot]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epOperationBooleanNot;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerNot;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnarySine:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnarySine]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleSinF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleSinD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCosine:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCosine]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleCosF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleCosD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryTangent:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryTangent]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleTanF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleTanD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryArcSine:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryArcSine]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleAsinF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleAsinD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryArcCosine:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryArcCosine]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleAcosF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleAcosD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryArcTangent:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryArcTangent]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleAtanF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleAtanD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryLogarithm:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryLogarithm]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleLnF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleLnD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryExponentiation:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryExponentiation]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleExpF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleExpD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnarySquare:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnarySquare]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleSqrF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleSqrD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnarySquareRoot:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnarySquareRoot]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleSqrtF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleSqrtD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryFloor:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleFloorF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleFloorD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCeil:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleCeilF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleCeilD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryRound:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleRoundF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleRoundD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCosh:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleCoshF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleCoshD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnarySinh:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleSinhF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleSinhD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryTanh:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationDoubleTanhF;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleTanhD;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCastToBoolean:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCastToBoolean]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epNop;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerToBoolean;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatToBoolean;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleToBoolean;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCastToInteger:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCastToInteger]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epOperationBooleanToInteger;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epNop;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatToInteger;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleToInteger;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCastToSingle:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCastToSingle]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epOperationBooleanToFloat;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerToFloat;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epNop;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleToFloat;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCastToDouble:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCastToDouble]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                Opcode = Pcodes.epOperationBooleanToDouble;
                                break;
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerToDouble;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatToDouble;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epNop;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryAbsoluteValue:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryAbsoluteValue]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationIntegerAbs;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationFloatAbs;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationDoubleAbs;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryTestNegative:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryTestNegative]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationTestIntegerNegative;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationTestFloatNegative;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationTestDoubleNegative;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryGetSign:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryGetSign]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                Opcode = Pcodes.epOperationGetSignInteger;
                                break;
                            case DataTypes.eFloat:
                                Opcode = Pcodes.epOperationGetSignFloat;
                                break;
                            case DataTypes.eDouble:
                                Opcode = Pcodes.epOperationGetSignDouble;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryGetArrayLength:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryGetArrayLength]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                                Opcode = Pcodes.epGetByteArraySize;
                                break;
                            case DataTypes.eArrayOfInteger:
                                Opcode = Pcodes.epGetIntegerArraySize;
                                break;
                            case DataTypes.eArrayOfFloat:
                                Opcode = Pcodes.epGetFloatArraySize;
                                break;
                            case DataTypes.eArrayOfDouble:
                                Opcode = Pcodes.epGetDoubleArraySize;
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryDuplicateArray:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryDuplicateArray]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                                Opcode = Pcodes.epCopyArrayByte;
                                break;
                            case DataTypes.eArrayOfInteger:
                                Opcode = Pcodes.epCopyArrayInteger;
                                break;
                            case DataTypes.eArrayOfFloat:
                                Opcode = Pcodes.epCopyArrayFloat;
                                break;
                            case DataTypes.eArrayOfDouble:
                                Opcode = Pcodes.epCopyArrayDouble;
                                break;
                        }
                        break;
                }
                if (Opcode != Pcodes.epNop)
                {
                    int unused;
                    FuncCode.AddPcodeInstruction(Opcode, out unused, this.LineNumber);
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                /* evaluate the argument */
                this.Argument.ILGen(
                    cilObject,
                    context);

                /* generate the operation code */
                switch (this.Kind)
                {
                    default:
                        // bad opcode
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case UnaryOpKind.eUnaryNegation:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryNegation]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Neg);
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryNot:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryNot]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                break;
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Not);
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnarySine:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnarySine]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sin", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sin", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCosine:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCosine]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Cos", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Cos", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryTangent:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryTangent]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Tan", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Tan", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryArcSine:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryArcSine]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Asin", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Asin", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryArcCosine:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryArcCosine]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Acos", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Acos", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryArcTangent:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryArcTangent]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Atan", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Atan", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryLogarithm:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryLogarithm]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Log", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Log", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryExponentiation:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryExponentiation]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Exp", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Exp", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnarySquare:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnarySquare]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Dup);
                                context.ilGenerator.Emit(OpCodes.Mul);
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnarySquareRoot:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnarySquareRoot]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sqrt", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sqrt", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryFloor:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Floor", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Floor", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCeil:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Ceiling", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Ceiling", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryRound:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Round", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Round", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnarySinh:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sinh", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sinh", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCosh:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Cosh", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Cosh", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryTanh:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Tanh", new Type[] { typeof(double) }));
                                LocalBuilder force = context.ilGenerator.DeclareLocal(typeof(float));
                                context.ilGenerator.Emit(OpCodes.Stloc, force);
                                context.ilGenerator.Emit(OpCodes.Ldloc, force);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Tanh", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCastToBoolean:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCastToBoolean]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                break;
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                break;
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Ldc_R4, (float)0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Ldc_R8, (double)0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ilGenerator.Emit(OpCodes.Ceq);
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCastToInteger:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCastToInteger]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                                break;
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Conv_I4);
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCastToSingle:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCastToSingle]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                            case DataTypes.eInteger:
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Conv_R4);
                                break;
                            case DataTypes.eFloat:
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryCastToDouble:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryCastToDouble]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Conv_R8);
                                break;
                            case DataTypes.eDouble:
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryAbsoluteValue:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryAbsoluteValue]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Abs", new Type[] { typeof(int) }));
                                break;
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Abs", new Type[] { typeof(float) }));
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Abs", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryTestNegative:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryTestNegative]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                                context.ilGenerator.Emit(OpCodes.Clt);
                                break;
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Ldc_R4, (float)0);
                                context.ilGenerator.Emit(OpCodes.Clt);
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Ldc_R8, (double)0);
                                context.ilGenerator.Emit(OpCodes.Clt);
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryGetSign:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryGetSign]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eInteger:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sign", new Type[] { typeof(int) }));
                                break;
                            case DataTypes.eFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sign", new Type[] { typeof(float) }));
                                break;
                            case DataTypes.eDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sign", new Type[] { typeof(double) }));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryGetArrayLength:
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryGetArrayLength]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(ArrayHandleByte).GetMethod("get_Length"));
                                break;
                            case DataTypes.eArrayOfInteger:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(ArrayHandleInt32).GetMethod("get_Length"));
                                break;
                            case DataTypes.eArrayOfFloat:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(ArrayHandleFloat).GetMethod("get_Length"));
                                break;
                            case DataTypes.eArrayOfDouble:
                                context.ilGenerator.Emit(OpCodes.Call, typeof(ArrayHandleDouble).GetMethod("get_Length"));
                                break;
                        }
                        break;

                    case UnaryOpKind.eUnaryDuplicateArray:
                        context.ilGenerator.Emit(OpCodes.Callvirt, typeof(ArrayHandle).GetMethod("Duplicate"));
                        switch (this.Argument.ResultType)
                        {
                            default:
                                // CodeGenUnaryOperator [eUnaryDuplicateArray]:  bad type
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                                context.ilGenerator.Emit(OpCodes.Castclass, typeof(ArrayHandleByte));
                                break;
                            case DataTypes.eArrayOfInteger:
                                context.ilGenerator.Emit(OpCodes.Castclass, typeof(ArrayHandleInt32));
                                break;
                            case DataTypes.eArrayOfFloat:
                                context.ilGenerator.Emit(OpCodes.Castclass, typeof(ArrayHandleFloat));
                                break;
                            case DataTypes.eArrayOfDouble:
                                context.ilGenerator.Emit(OpCodes.Castclass, typeof(ArrayHandleDouble));
                                break;
                        }
                        break;
                }
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                bool WeDidSomething;

                replacementExpr = null;
                DataTypes LocalResultType = this.resultType;

                /* fold stuff under us */
                ASTExpression argumentReplacement;
                this.Argument.FoldConst(out WeDidSomething, out argumentReplacement);
                if (argumentReplacement != null)
                {
                    this.argument = argumentReplacement;
                }

                /* see if we can do anything with it */
                if (this.Argument.Kind == ExprKind.eExprOperand)
                {
                    /* we might be able to do something with an operand */
                    ASTOperand Operand = this.Argument.InnerOperand;
                    switch (Operand.Kind)
                    {
                        default:
                            break;

                        case ASTOperandKind.eASTOperandIntegerLiteral:
                            {
                                ASTOperand NewOperand = null;
                                int OldValue;

                                OldValue = Operand.IntegerLiteralValue;
                                switch (this.Kind)
                                {
                                    default:
                                        // FoldConstUnaryOperator: for integer literal argument, found illegal operator
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case UnaryOpKind.eUnaryNegation:
                                        {
                                            int NewValue;

                                            NewValue = -OldValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryNot:
                                        {
                                            int NewValue;

                                            NewValue = ~OldValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToBoolean:
                                        {
                                            bool NewValue;

                                            NewValue = (OldValue != 0);
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToInteger:
                                        {
                                            int NewValue;

                                            NewValue = OldValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToSingle:
                                        {
                                            float NewValue;

                                            NewValue = OldValue;
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToDouble:
                                        {
                                            double NewValue;

                                            NewValue = OldValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryAbsoluteValue:
                                        {
                                            int NewValue;

                                            NewValue = (OldValue >= 0 ? OldValue : -OldValue);
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryTestNegative:
                                        {
                                            bool NewValue;

                                            NewValue = (OldValue < 0);
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryGetSign:
                                        {
                                            int NewValue;

                                            if (OldValue < 0)
                                            {
                                                NewValue = -1;
                                            }
                                            else if (OldValue > 0)
                                            {
                                                NewValue = 1;
                                            }
                                            else
                                            {
                                                NewValue = 0;
                                            }
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }

                                if (NewOperand != null)
                                {
                                    /* a conversion occurred */
                                    replacementExpr = new ASTExpression(
                                        NewOperand,
                                        this.LineNumber);
                                    WeDidSomething = true;
                                }
                            }
                            break;

                        case ASTOperandKind.eASTOperandBooleanLiteral:
                            {
                                ASTOperand NewOperand = null;
                                bool OldValue;

                                OldValue = Operand.BooleanLiteralValue;
                                switch (this.Kind)
                                {
                                    default:
                                        // FoldConstUnaryOperator: for boolean literal argument, found illegal operator
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case UnaryOpKind.eUnaryNot:
                                        {
                                            bool NewValue;

                                            NewValue = !OldValue;
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToBoolean:
                                        {
                                            bool NewValue;

                                            NewValue = OldValue;
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToInteger:
                                        {
                                            int NewValue;

                                            NewValue = (OldValue ? 1 : 0);
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToSingle:
                                        {
                                            float NewValue;

                                            NewValue = (OldValue ? 1 : 0);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToDouble:
                                        {
                                            double NewValue;

                                            NewValue = (OldValue ? 1 : 0);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }

                                if (NewOperand != null)
                                {
                                    /* a conversion occurred */
                                    replacementExpr = new ASTExpression(
                                        NewOperand,
                                        this.LineNumber);
                                    WeDidSomething = true;
                                }
                            }
                            break;

                        case ASTOperandKind.eASTOperandSingleLiteral:
                            {
                                ASTOperand NewOperand = null;
                                float OldValue;

                                OldValue = Operand.SingleLiteralValue;
                                switch (this.Kind)
                                {
                                    default:
                                        // FoldConstUnaryOperator: for single literal argument, found illegal operator
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case UnaryOpKind.eUnaryNegation:
                                        {
                                            float NewValue;

                                            NewValue = -OldValue;
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToBoolean:
                                        {
                                            bool NewValue;

                                            NewValue = (OldValue != 0);
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToInteger:
                                        {
                                            int NewValue;

                                            NewValue = (int)OldValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToSingle:
                                        {
                                            float NewValue;

                                            NewValue = OldValue;
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToDouble:
                                        {
                                            double NewValue;

                                            NewValue = OldValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryAbsoluteValue:
                                        {
                                            float NewValue;

                                            NewValue = (OldValue >= 0 ? OldValue : -OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryTestNegative:
                                        {
                                            bool NewValue;

                                            NewValue = (OldValue < 0);
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryGetSign:
                                        {
                                            int NewValue;

                                            if (OldValue < 0)
                                            {
                                                NewValue = -1;
                                            }
                                            else if (OldValue > 0)
                                            {
                                                NewValue = 1;
                                            }
                                            else
                                            {
                                                NewValue = 0;
                                            }
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnarySine:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Sin(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCosine:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Cos(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryTangent:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Tan(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryArcSine:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Asin(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryArcCosine:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Acos(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryArcTangent:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Atan(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryLogarithm:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Log(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryExponentiation:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Exp(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnarySquare:
                                        {
                                            float NewValue;

                                            NewValue = OldValue * OldValue;
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnarySquareRoot:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Sqrt(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryFloor:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Floor(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCeil:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Ceiling(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryRound:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Round(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnarySinh:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Sinh(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCosh:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Cosh(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryTanh:
                                        {
                                            float NewValue;

                                            NewValue = (float)Math.Tanh(OldValue);
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }

                                if (NewOperand != null)
                                {
                                    /* a conversion occurred */
                                    replacementExpr = new ASTExpression(
                                        NewOperand,
                                        this.LineNumber);
                                    WeDidSomething = true;
                                }
                            }
                            break;

                        case ASTOperandKind.eASTOperandDoubleLiteral:
                            {
                                ASTOperand NewOperand = null;
                                double OldValue;

                                OldValue = Operand.DoubleLiteralValue;
                                switch (this.Kind)
                                {
                                    default:
                                        // FoldConstUnaryOperator: for double literal argument, found illegal operator
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case UnaryOpKind.eUnaryNegation:
                                        {
                                            double NewValue;

                                            NewValue = -OldValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnarySine:
                                        {
                                            double NewValue;

                                            NewValue = Math.Sin(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCosine:
                                        {
                                            double NewValue;

                                            NewValue = Math.Cos(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryTangent:
                                        {
                                            double NewValue;

                                            NewValue = Math.Tan(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryArcSine:
                                        {
                                            double NewValue;

                                            NewValue = Math.Asin(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryArcCosine:
                                        {
                                            double NewValue;

                                            NewValue = Math.Acos(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryArcTangent:
                                        {
                                            double NewValue;

                                            NewValue = Math.Atan(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryLogarithm:
                                        {
                                            double NewValue;

                                            NewValue = Math.Log(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryExponentiation:
                                        {
                                            double NewValue;

                                            NewValue = Math.Exp(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToBoolean:
                                        {
                                            bool NewValue;

                                            NewValue = (OldValue != 0);
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToInteger:
                                        {
                                            int NewValue;

                                            NewValue = (int)OldValue;
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToSingle:
                                        {
                                            float NewValue;

                                            NewValue = (float)OldValue;
                                            LocalResultType = DataTypes.eFloat;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCastToDouble:
                                        {
                                            double NewValue;

                                            NewValue = OldValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnarySquare:
                                        {
                                            double NewValue;

                                            NewValue = OldValue * OldValue;
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnarySquareRoot:
                                        {
                                            double NewValue;

                                            NewValue = Math.Sqrt(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryFloor:
                                        {
                                            double NewValue;

                                            NewValue = Math.Floor(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCeil:
                                        {
                                            double NewValue;

                                            NewValue = Math.Ceiling(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryRound:
                                        {
                                            double NewValue;

                                            NewValue = Math.Round(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnarySinh:
                                        {
                                            double NewValue;

                                            NewValue = Math.Sinh(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryCosh:
                                        {
                                            double NewValue;

                                            NewValue = Math.Cosh(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryTanh:
                                        {
                                            double NewValue;

                                            NewValue = Math.Tanh(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryAbsoluteValue:
                                        {
                                            double NewValue;

                                            NewValue = Math.Abs(OldValue);
                                            LocalResultType = DataTypes.eDouble;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryTestNegative:
                                        {
                                            bool NewValue;

                                            NewValue = (OldValue < 0);
                                            LocalResultType = DataTypes.eBoolean;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                    case UnaryOpKind.eUnaryGetSign:
                                        {
                                            int NewValue;

                                            if (OldValue < 0)
                                            {
                                                NewValue = -1;
                                            }
                                            else if (OldValue > 0)
                                            {
                                                NewValue = 1;
                                            }
                                            else
                                            {
                                                NewValue = 0;
                                            }
                                            LocalResultType = DataTypes.eInteger;

                                            NewOperand = new ASTOperand(
                                                NewValue,
                                                this.LineNumber);
                                        }
                                        break;
                                }

                                if (NewOperand != null)
                                {
                                    /* a conversion occurred */
                                    replacementExpr = new ASTExpression(
                                        NewOperand,
                                        this.LineNumber);
                                    WeDidSomething = true;
                                }
                            }
                            break;
                    }
                }

                Debug.Assert(LocalResultType == this.resultType);
                DidSomething = WeDidSomething;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("{0}({1})", kind, argument);
            }
#endif
        }




        public class ASTVariableDeclaration : ASTBase
        {
            private readonly SymbolRec symbol;
            private ASTExpression initializationExpression; // may be null
            private readonly int lineNumber;

            private DataTypes resultType;

            public SymbolRec Symbol { get { return symbol; } }
            public ASTExpression InitializationExpression { get { return initializationExpression; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            /* allocate a new variable declaration.  if the Initializer expression is null, then */
            /* the object is initialized to null or zero when it enters scope. */
            public ASTVariableDeclaration(
                SymbolRec symbol,
                ASTExpression Initializer,
                int LineNumber)
            {
                this.lineNumber = LineNumber;
                this.symbol = symbol;
                this.initializationExpression = Initializer;
            }

            /* type check the variable declaration node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                if (this.InitializationExpression == null)
                {
                    /* no initializer -- default */
                    ResultingDataType = this.Symbol.VariableDataType;
                }
                else
                {
                    /* initializer checking */
                    DataTypes VariableType = this.Symbol.VariableDataType;
                    DataTypes InitializerType;
                    Error = this.InitializationExpression.TypeCheck(
                        out InitializerType,
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    if (!CanRightBeMadeToMatchLeft(VariableType, InitializerType))
                    {
                        ErrorLineNumber = this.LineNumber;
                        return CompileErrors.eCompileTypeMismatchInAssignment;
                    }
                    if (MustRightBePromotedToLeft(VariableType, InitializerType))
                    {
                        /* promote the initializer */
                        ASTExpression PromotedInitializer = PromoteTheExpression(
                            InitializerType/*orig*/,
                            VariableType/*desired*/,
                            this.InitializationExpression,
                            this.LineNumber);
                        this.initializationExpression = PromotedInitializer;
                        /* sanity check */
                        Error = this.InitializationExpression.TypeCheck(
                            out InitializerType/*obtain new right type*/,
                            out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            // type promotion caused failure
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                    }
                    if (VariableType != InitializerType)
                    {
                        // after type promotion, types are no longer the same
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = InitializerType;
                }

                this.resultType = ResultingDataType;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int unused;

                int StackDepth = StackDepthParam;

                /* evaluate the value initializer expression if there is one */
                if (this.InitializationExpression != null)
                {
                    /* there's a true initializer */
                    this.InitializationExpression.PcodeGen(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                }
                else
                {
                    /* generate the default zero value for this type */
                    switch (this.Symbol.VariableDataType)
                    {
                        default:
                            // bad variable type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eBoolean:
                        case DataTypes.eInteger:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, this.LineNumber);
                            FuncCode.AddPcodeOperandInteger(0);
                            break;
                        case DataTypes.eFloat:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateFloat, out unused, this.LineNumber);
                            FuncCode.AddPcodeOperandFloat(0);
                            break;
                        case DataTypes.eDouble:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateDouble, out unused, this.LineNumber);
                            FuncCode.AddPcodeOperandDouble(0);
                            break;
                        case DataTypes.eArrayOfBoolean:
                        case DataTypes.eArrayOfByte:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayByte, out unused, this.LineNumber);
                            break;
                        case DataTypes.eArrayOfInteger:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayInt32, out unused, this.LineNumber);
                            break;
                        case DataTypes.eArrayOfFloat:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayFloat, out unused, this.LineNumber);
                            break;
                        case DataTypes.eArrayOfDouble:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArrayDouble, out unused, this.LineNumber);
                            break;
                    }
                    StackDepth++;
                    MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                }
                if (StackDepth != StackDepthParam + 1)
                {
                    // stack depth error after evaluting initializer
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                /* save the index of the stack into the variable thing yah */
                this.Symbol.SymbolVariableStackLocation = StackDepth;

                /* duplicate the value so there's something to return */
                FuncCode.AddPcodeInstruction(Pcodes.epDuplicate, out unused, this.LineNumber);
                StackDepth++;
                MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
                if (StackDepth != StackDepthParam + 2)
                {
                    // stack depth error after duplicating value for return
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                Debug.Assert(!context.variableTable.ContainsKey(this.Symbol));
                LocalBuilder localVariable = context.ilGenerator.DeclareLocal(
                    PcodeMarshal.GetManagedType(this.Symbol.VariableDataType));
                context.variableTable.Add(this.Symbol, localVariable);

                /* evaluate the value initializer expression if there is one */
                if (this.InitializationExpression != null)
                {
                    /* there's a true initializer */
                    this.InitializationExpression.ILGen(
                        cilObject,
                        context);
                }
                else
                {
                    /* generate the default zero value for this type */
                    switch (this.Symbol.VariableDataType)
                    {
                        default:
                            // bad variable type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eBoolean:
                        case DataTypes.eInteger:
                            context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                            break;
                        case DataTypes.eFloat:
                            context.ilGenerator.Emit(OpCodes.Ldc_R4, (float)0);
                            break;
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Ldc_R8, (double)0);
                            break;
                        case DataTypes.eArrayOfBoolean:
                        case DataTypes.eArrayOfByte:
                            context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleByte).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                            break;
                        case DataTypes.eArrayOfInteger:
                            context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleInt32).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                            break;
                        case DataTypes.eArrayOfFloat:
                            context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleFloat).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                            break;
                        case DataTypes.eArrayOfDouble:
                            context.ilGenerator.Emit(OpCodes.Ldsfld, typeof(ArrayHandleDouble).GetField("Nullish", BindingFlags.Public | BindingFlags.Static));
                            break;
                    }
                }

                context.ilGenerator.Emit(OpCodes.Dup);
                context.ilGenerator.Emit(OpCodes.Stloc, localVariable);
            }

            /* fold constants in the AST. returns True in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                ASTExpression initializationReplacement;
                this.InitializationExpression.FoldConst(out DidSomething, out initializationReplacement);
                if (initializationReplacement != null)
                {
                    this.initializationExpression = initializationReplacement;
                }

                replacementExpr = null;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("var {0} = {1}", symbol, initializationExpression != null ? initializationExpression.ToString() : "NULL");
            }
#endif
        }




        public enum WaveGetterKind
        {
            eInvalid,

            eWaveGetterSampleLeft,
            eWaveGetterSampleRight,
            eWaveGetterSampleMono,
            eWaveGetterWaveFrames,
            eWaveGetterWaveTables,
            eWaveGetterWaveArray,
        }

        public class ASTWaveGetter : ASTBase
        {
            private readonly string sampleName;
            private readonly WaveGetterKind kind;
            private readonly int lineNumber;

            private DataTypes resultType;

            public string SampleName { get { return sampleName; } }
            public WaveGetterKind Kind { get { return kind; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            public ASTWaveGetter(
                string sampleName,
                WaveGetterKind kind,
                int LineNumber)
            {
                this.sampleName = sampleName;
                this.kind = kind;
                this.lineNumber = LineNumber;
            }

            /* type check the wave getter node.  this returns eCompileNoError if */
            /* everything is ok, and the appropriate type in *ResultingDataType. */
            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                ErrorLineNumber = -1;

                switch (this.Kind)
                {
                    default:
                        // bad type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case WaveGetterKind.eWaveGetterSampleLeft:
                    case WaveGetterKind.eWaveGetterSampleRight:
                    case WaveGetterKind.eWaveGetterSampleMono:
                    case WaveGetterKind.eWaveGetterWaveArray:
                        ResultingDataType = DataTypes.eArrayOfFloat;
                        break;
                    case WaveGetterKind.eWaveGetterWaveFrames:
                    case WaveGetterKind.eWaveGetterWaveTables:
                        ResultingDataType = DataTypes.eInteger;
                        break;
                }

                this.resultType = ResultingDataType;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                ASTExpression waveNameExpression = new ASTExpression(this.SampleName, this.LineNumber);

                string WhichMethod;
                DataTypes WhichReturn;
                switch (this.Kind)
                {
                    default:
                        // bad opcode
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case WaveGetterKind.eWaveGetterSampleLeft:
                        WhichMethod = "GetSampleLeftArray";
                        WhichReturn = DataTypes.eArrayOfFloat;
                        break;
                    case WaveGetterKind.eWaveGetterSampleRight:
                        WhichMethod = "GetSampleRightArray";
                        WhichReturn = DataTypes.eArrayOfFloat;
                        break;
                    case WaveGetterKind.eWaveGetterSampleMono:
                        WhichMethod = "GetSampleMonoArray";
                        WhichReturn = DataTypes.eArrayOfFloat;
                        break;
                    case WaveGetterKind.eWaveGetterWaveFrames:
                        WhichMethod = "GetWaveTableFrames";
                        WhichReturn = DataTypes.eInteger;
                        break;
                    case WaveGetterKind.eWaveGetterWaveTables:
                        WhichMethod = "GetWaveTableTables";
                        WhichReturn = DataTypes.eInteger;
                        break;
                    case WaveGetterKind.eWaveGetterWaveArray:
                        WhichMethod = "GetWaveTableArray";
                        WhichReturn = DataTypes.eArrayOfFloat;
                        break;
                }
                PcodeGenExternCall(
                    FuncCode,
                    ref StackDepthParam,
                    ref MaxStackDepth,
                    this.LineNumber,
                    WhichMethod,
                    new ASTExpression[1]
                    {
                        waveNameExpression,
                    },
                    new DataTypes[]
                    {
                        DataTypes.eArrayOfByte/*0: name*/,
                    },
                    WhichReturn);
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                ASTExpression waveNameExpression = new ASTExpression(this.SampleName, this.LineNumber);

                string WhichMethod;
                DataTypes WhichReturn;
                switch (this.Kind)
                {
                    default:
                        // bad opcode
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case WaveGetterKind.eWaveGetterSampleLeft:
                        WhichMethod = "GetSampleLeftArray";
                        WhichReturn = DataTypes.eArrayOfFloat;
                        break;
                    case WaveGetterKind.eWaveGetterSampleRight:
                        WhichMethod = "GetSampleRightArray";
                        WhichReturn = DataTypes.eArrayOfFloat;
                        break;
                    case WaveGetterKind.eWaveGetterSampleMono:
                        WhichMethod = "GetSampleMonoArray";
                        WhichReturn = DataTypes.eArrayOfFloat;
                        break;
                    case WaveGetterKind.eWaveGetterWaveFrames:
                        WhichMethod = "GetWaveTableFrames";
                        WhichReturn = DataTypes.eInteger;
                        break;
                    case WaveGetterKind.eWaveGetterWaveTables:
                        WhichMethod = "GetWaveTableTables";
                        WhichReturn = DataTypes.eInteger;
                        break;
                    case WaveGetterKind.eWaveGetterWaveArray:
                        WhichMethod = "GetWaveTableArray";
                        WhichReturn = DataTypes.eArrayOfFloat;
                        break;
                }
                ILGenExternCall(
                    cilObject,
                    context,
                    this.LineNumber,
                    WhichMethod,
                    new ASTExpression[1]
                    {
                        waveNameExpression,
                    },
                    new DataTypes[]
                    {
                        DataTypes.eArrayOfByte/*0: name*/,
                    },
                    WhichReturn);
            }

            /* fold constants in the AST. returns true in *DidSomething if it was able to make an improvement. */
            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                DidSomething = false;
                replacementExpr = null;
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("getwave({0}, \"{1}\")", kind, sampleName);
            }
#endif
        }




        public class ASTForLoop : ASTBase
        {
            private readonly SymbolRec symbol; // enumeration variable
            private ASTExpression initializationExpression;
            private ASTExpression whileExpression;
            private ASTExpression incrementExpression;
            private ASTExpression bodyExpression;
            private readonly int lineNumber;

            private DataTypes resultType;

            public SymbolRec Symbol { get { return symbol; } } // enumeration variable
            public ASTExpression InitializationExpression { get { return initializationExpression; } }
            public ASTExpression WhileExpression { get { return whileExpression; } }
            public ASTExpression IncrementExpression { get { return incrementExpression; } }
            public ASTExpression BodyExpression { get { return bodyExpression; } }
            public override int LineNumber { get { return lineNumber; } }
            public override DataTypes ResultType { get { return resultType; } }


            public ASTForLoop(
                SymbolRec symbol,
                ASTExpression InitializationExpression,
                ASTExpression WhileExpression,
                ASTExpression IncrementExpression,
                ASTExpression BodyExpression,
                int LineNumber)
            {
                this.symbol = symbol;
                this.initializationExpression = InitializationExpression;
                this.whileExpression = WhileExpression;
                this.incrementExpression = IncrementExpression;
                this.bodyExpression = BodyExpression;
                this.lineNumber = LineNumber;
            }

            public override CompileErrors TypeCheck(
                out DataTypes ResultingDataType,
                out int ErrorLineNumber)
            {
                CompileErrors Error;

                ErrorLineNumber = -1;
                ResultingDataType = DataTypes.eInvalidDataType;

                DataTypes LoopVariableType = this.Symbol.VariableDataType;
                DataTypes InitializationType;
                Error = this.InitializationExpression.TypeCheck(out InitializationType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (!IsSequencedScalarType(InitializationType))
                {
                    ErrorLineNumber = this.InitializationExpression.LineNumber;
                    return CompileErrors.eCompileOperandsMustBeSequencedScalar;
                }
                if (!CanRightBeMadeToMatchLeft(LoopVariableType, InitializationType))
                {
                    ErrorLineNumber = this.InitializationExpression.LineNumber;
                    return CompileErrors.eCompileTypeMismatch;
                }
                if (MustRightBePromotedToLeft(LoopVariableType, InitializationType))
                {
                    /* insert promotion operator above right hand side */
                    ASTExpression ReplacementRValue = PromoteTheExpression(
                        LoopVariableType,
                        InitializationType,
                        this.InitializationExpression,
                        this.InitializationExpression.LineNumber);
                    this.initializationExpression = ReplacementRValue;
                    /* sanity check */
                    Error = this.InitializationExpression.TypeCheck(
                        out InitializationType,
                        out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        // type promotion caused failure
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (LoopVariableType != InitializationType)
                    {
                        // after type promotion, types are no longer the same
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }

                DataTypes WhileType;
                Error = this.WhileExpression.TypeCheck(out WhileType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (WhileType != DataTypes.eBoolean)
                {
                    ErrorLineNumber = this.LineNumber;
                    return CompileErrors.eCompileTypeMismatch;
                }

                DataTypes IncrementType;
                Error = this.IncrementExpression.TypeCheck(out IncrementType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (IncrementType != LoopVariableType)
                {
                    ErrorLineNumber = this.LineNumber;
                    return CompileErrors.eCompileTypeMismatch;
                }

                DataTypes BodyType;
                Error = this.BodyExpression.TypeCheck(out BodyType, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }

                ResultingDataType = this.resultType = LoopVariableType;
                return CompileErrors.eCompileNoError;
            }

            public override void PcodeGen(
                PcodeRec FuncCode,
                ref int StackDepthParam,
                ref int MaxStackDepth)
            {
                int unused;

                int StackDepth = StackDepthParam;

                this.InitializationExpression.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 1)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                this.Symbol.SymbolVariableStackLocation = StackDepth;

                int WhileBranchPatchupLocation = -1;
                FuncCode.AddPcodeInstruction(Pcodes.epBranchUnconditional, out WhileBranchPatchupLocation, this.LineNumber);
                FuncCode.AddPcodeOperandInteger(-1/*target unknown*/);

                int LoopBackAgainLocation = FuncCode.PcodeGetNextAddress();

                this.BodyExpression.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 2)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                FuncCode.AddPcodeInstruction(Pcodes.epStackPop, out unused, this.BodyExpression.LineNumber);
                StackDepth--;
                if (StackDepth != StackDepthParam + 1)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                this.IncrementExpression.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 2)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                FuncCode.AddPcodeInstruction(Pcodes.epStackPop, out unused, this.IncrementExpression.LineNumber);
                StackDepth--;
                if (StackDepth != StackDepthParam + 1)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                FuncCode.ResolvePcodeBranch(WhileBranchPatchupLocation, FuncCode.PcodeGetNextAddress());

                this.WhileExpression.PcodeGen(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth);
                Debug.Assert(StackDepth <= MaxStackDepth);
                if (StackDepth != StackDepthParam + 2)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                FuncCode.AddPcodeInstruction(Pcodes.epBranchIfNotZero, out unused, this.WhileExpression.LineNumber);
                FuncCode.AddPcodeOperandInteger(LoopBackAgainLocation);
                StackDepth--;
                if (StackDepth != StackDepthParam + 1)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                StackDepthParam = StackDepth;
            }

            public override void ILGen(
                CILObject cilObject,
                ILGenContext context)
            {
                LocalBuilder loopVariable;
                switch (this.Symbol.VariableDataType)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidCastException();
                    case DataTypes.eInteger:
                        loopVariable = context.ilGenerator.DeclareLocal(typeof(int));
                        break;
                    case DataTypes.eFloat:
                        loopVariable = context.ilGenerator.DeclareLocal(typeof(float));
                        break;
                    case DataTypes.eDouble:
                        loopVariable = context.ilGenerator.DeclareLocal(typeof(double));
                        break;
                }
                this.InitializationExpression.ILGen(
                    cilObject,
                    context);
                context.ilGenerator.Emit(OpCodes.Stloc, loopVariable);

                context.variableTable.Add(this.Symbol, loopVariable);

                Label whileExpr = context.ilGenerator.DefineLabel();
                context.ilGenerator.Emit(OpCodes.Br, whileExpr);

                Label bodyExpr = context.ilGenerator.DefineLabel();
                context.ilGenerator.MarkLabel(bodyExpr);

                this.BodyExpression.ILGen(
                    cilObject,
                    context);
                context.ilGenerator.Emit(OpCodes.Pop);

                this.IncrementExpression.ILGen(
                    cilObject,
                    context);
                context.ilGenerator.Emit(OpCodes.Pop);

                context.ilGenerator.MarkLabel(whileExpr);

                this.WhileExpression.ILGen(
                    cilObject,
                    context);
                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                context.ilGenerator.Emit(OpCodes.Bne_Un, bodyExpr);

                context.ilGenerator.Emit(OpCodes.Ldloc, loopVariable);
            }

            public override void FoldConst(
                out bool DidSomething,
                out ASTExpression replacementExpr)
            {
                bool DidSomething1;

                replacementExpr = null;
                DidSomething = false;

                ASTExpression initializationReplacement;
                this.InitializationExpression.FoldConst(out DidSomething1, out initializationReplacement);
                if (initializationReplacement != null)
                {
                    this.initializationExpression = initializationReplacement;
                }
                DidSomething = DidSomething || DidSomething1;

                ASTExpression whileReplacement;
                this.WhileExpression.FoldConst(out DidSomething1, out whileReplacement);
                if (whileReplacement != null)
                {
                    this.whileExpression = whileReplacement;
                }
                DidSomething = DidSomething || DidSomething1;

                ASTExpression incrementReplacement;
                this.IncrementExpression.FoldConst(out DidSomething1, out incrementReplacement);
                DidSomething = DidSomething || DidSomething1;
                if (incrementReplacement != null)
                {
                    Debug.Assert(incrementReplacement.Kind == ExprKind.eExprAssignment);
                    this.incrementExpression = incrementReplacement;
                }

                ASTExpression bodyReplacement;
                this.BodyExpression.FoldConst(out DidSomething1, out bodyReplacement);
                if (bodyReplacement != null)
                {
                    this.bodyExpression = bodyReplacement;
                }
                DidSomething = DidSomething || DidSomething1;

                // TODO: try to fold loop iterations
            }

#if DEBUG
            public override string ToString()
            {
                return String.Format("for {1} = {2} while {3} step {4} do{0}{5}", Environment.NewLine, symbol, initializationExpression, whileExpression, incrementExpression, ASTExpressionList.IndentAll(bodyExpression.ToString()));
            }
#endif
        }




        /* this routine sees if the right hand type can be promoted (if necessary) to */
        /* become the left hand type.  True is returned if that is the case. */
        public static bool CanRightBeMadeToMatchLeft(
            DataTypes LeftType,
            DataTypes RightType)
        {
            if (LeftType == DataTypes.eDouble)
            {
                if ((RightType == DataTypes.eInteger) || (RightType == DataTypes.eFloat))
                {
                    return true;
                }
            }
            else if (LeftType == DataTypes.eFloat)
            {
                if (RightType == DataTypes.eInteger)
                {
                    return true;
                }
            }
            return (LeftType == RightType);
        }

        /* this routine sees if the right hand type MUST be promoted to become */
        /* the left hand type. it is not allowed to call with non-compatible types */
        public static bool MustRightBePromotedToLeft(
            DataTypes LeftType,
            DataTypes RightType)
        {
            /* see if we have to promote */
            if (LeftType == RightType)
            {
                return false;
            }

            /* we have to promote, so see if we can */
            if ((LeftType == DataTypes.eDouble)
                && ((RightType == DataTypes.eInteger) || (RightType == DataTypes.eFloat) || (RightType == DataTypes.eDouble)))
            {
                return true;
            }
            if ((LeftType == DataTypes.eFloat)
                && ((RightType == DataTypes.eInteger) || (RightType == DataTypes.eFloat)))
            {
                return true;
            }
            // types are not promotable -- shouldn't have gotten here
            Debug.Assert(false);
            throw new InvalidOperationException();
        }

        /* perform type promotion on an expression */
        public static ASTExpression PromoteTheExpression(
            DataTypes OriginalType,
            DataTypes DesiredType,
            ASTExpression OriginalExpression,
            int LineNumber)
        {
            ASTUnaryOperation UnaryOperator;
            ASTExpression ResultingExpression;

            if ((DesiredType != DataTypes.eDouble) && (DesiredType != DataTypes.eFloat))
            {
                // types are not promotable
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            switch (OriginalType)
            {
                default:
                    // unknown type
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case DataTypes.eBoolean:
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                case DataTypes.eArrayOfInteger:
                case DataTypes.eArrayOfFloat:
                case DataTypes.eArrayOfDouble:
                    // types are not promotable
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case DataTypes.eDouble:
                    // type promotion unnecessary
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case DataTypes.eInteger:
                case DataTypes.eFloat:
                    UnaryOperator = new ASTUnaryOperation(
                        DesiredType == DataTypes.eDouble ? UnaryOpKind.eUnaryCastToDouble : UnaryOpKind.eUnaryCastToSingle,
                        OriginalExpression,
                        LineNumber);
                    ResultingExpression = new ASTExpression(UnaryOperator, LineNumber);
                    return ResultingExpression;
            }
        }

        public static bool IsScalarType(DataTypes type)
        {
            switch (type)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                case DataTypes.eArrayOfInteger:
                case DataTypes.eArrayOfFloat:
                case DataTypes.eArrayOfDouble:
                    return false;
                case DataTypes.eBoolean:
                case DataTypes.eInteger:
                case DataTypes.eFloat:
                case DataTypes.eDouble:
                    return true;
            }
        }

        public static bool IsSequencedScalarType(DataTypes type)
        {
            switch (type)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                case DataTypes.eArrayOfInteger:
                case DataTypes.eArrayOfFloat:
                case DataTypes.eArrayOfDouble:
                case DataTypes.eBoolean:
                    return false;
                case DataTypes.eInteger:
                case DataTypes.eFloat:
                case DataTypes.eDouble:
                    return true;
            }
        }

        public static bool IsIndexedType(DataTypes type)
        {
            switch (type)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case DataTypes.eBoolean:
                case DataTypes.eInteger:
                case DataTypes.eFloat:
                case DataTypes.eDouble:
                    return false;
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                case DataTypes.eArrayOfInteger:
                case DataTypes.eArrayOfFloat:
                case DataTypes.eArrayOfDouble:
                    return true;
            }
        }
    }
}
