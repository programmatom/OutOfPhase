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

            public ILGenContext(
                ILGenerator ilGenerator,
                Dictionary<string, int> argumentTable,
                Dictionary<SymbolRec, LocalBuilder> variableTable,
                ManagedFunctionLinkerRec managedFunctionLinker)
            {
                this.ilGenerator = ilGenerator;
                this.argumentTable = argumentTable;
                this.variableTable = variableTable;
                this.managedFunctionLinker = managedFunctionLinker;
            }
        }

        public class ASTArrayDeclRec
        {
            public SymbolRec SymbolTableEntry;
            public ASTExpressionRec SizeExpression;
            public int LineNumber;
        }

        /* create a new array variable constructor node.  this should ONLY be used for */
        /* creating arrays.  variables that are initialized with an array that results from */
        /* an expression should use ASTVariableDeclaration.  */
        public static ASTArrayDeclRec NewArrayConstruction(
            SymbolRec SymbolTableEntry,
            ASTExpressionRec SizeExpression,
            int LineNumber)
        {
            ASTArrayDeclRec ArrayThing = new ASTArrayDeclRec();
            if ((SymbolTableEntry.GetSymbolVariableDataType() != DataTypes.eArrayOfBoolean)
                && (SymbolTableEntry.GetSymbolVariableDataType() != DataTypes.eArrayOfByte)
                && (SymbolTableEntry.GetSymbolVariableDataType() != DataTypes.eArrayOfInteger)
                && (SymbolTableEntry.GetSymbolVariableDataType() != DataTypes.eArrayOfFloat)
                && (SymbolTableEntry.GetSymbolVariableDataType() != DataTypes.eArrayOfDouble))
            {
                // variable type is NOT an array
                Debug.Assert(false);
                throw new ArgumentException();
            }

            ArrayThing.LineNumber = LineNumber;
            ArrayThing.SymbolTableEntry = SymbolTableEntry;
            ArrayThing.SizeExpression = SizeExpression;

            return ArrayThing;
        }

        /* type check the array variable constructor node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckArrayConstruction(
            out DataTypes ResultingDataType,
            ASTArrayDeclRec ArrayConstructor,
            out int ErrorLineNumber)
        {
            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            if (ArrayConstructor.SymbolTableEntry.WhatIsThisSymbol() != SymbolType.eSymbolVariable)
            {
                ErrorLineNumber = ArrayConstructor.LineNumber;
                return CompileErrors.eCompileExpectedVariable;
            }

            DataTypes TheVariableType = ArrayConstructor.SymbolTableEntry.GetSymbolVariableDataType();
            switch (TheVariableType)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case DataTypes.eBoolean:
                case DataTypes.eInteger:
                case DataTypes.eFloat:
                case DataTypes.eDouble:
                    ErrorLineNumber = ArrayConstructor.LineNumber;
                    return CompileErrors.eCompileExpectedArrayType;
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                case DataTypes.eArrayOfInteger:
                case DataTypes.eArrayOfFloat:
                case DataTypes.eArrayOfDouble:
                    break;
            }

            DataTypes SizeSpecifierType;
            CompileErrors Error = TypeCheckExpression(
                out SizeSpecifierType,
                ArrayConstructor.SizeExpression,
                out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            if (SizeSpecifierType != DataTypes.eInteger)
            {
                ErrorLineNumber = ArrayConstructor.LineNumber;
                return CompileErrors.eCompileArraySizeSpecMustBeInteger;
            }

            ResultingDataType = TheVariableType;
            return CompileErrors.eCompileNoError;
        }

        /* generate code for array declaration.  returns True if successful, or False if */
        /* it fails. */
        public static void CodeGenArrayConstruction(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTArrayDeclRec ArrayConstructor)
        {
            Pcodes OpcodeToGenerate;

            int StackDepth = StackDepthParam;

            /* evaluate size expression, leaving result on stack */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                ArrayConstructor.SizeExpression);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 1)
            {
                // CodeGenExpression made stack depth error
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* construct array operation.  this pops size, but pushes new array reference */
            switch (ArrayConstructor.SymbolTableEntry.GetSymbolVariableDataType())
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
            FuncCode.AddPcodeInstruction(OpcodeToGenerate, out unused, ArrayConstructor.LineNumber);

            /* now make the symbol table entry remember where on the stack it is. */
            ArrayConstructor.SymbolTableEntry.SetSymbolVariableStackLocation(StackDepth);

            /* duplicate the value for something to return */
            FuncCode.AddPcodeInstruction(Pcodes.epDuplicate, out unused, ArrayConstructor.LineNumber);
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

#if true // TODO:experimental
        public static void ILGenArrayConstruction(
            CILObject cilObject,
            ILGenContext context,
            ASTArrayDeclRec ArrayConstructor)
        {
            Debug.Assert(!context.variableTable.ContainsKey(ArrayConstructor.SymbolTableEntry));
            LocalBuilder localVariable = context.ilGenerator.DeclareLocal(
                CILObject.GetManagedType(ArrayConstructor.SymbolTableEntry.GetSymbolVariableDataType()));
            context.variableTable.Add(ArrayConstructor.SymbolTableEntry, localVariable);

            /* evaluate size expression, leaving result on stack */
            ILGenExpression(
                cilObject,
                context,
                ArrayConstructor.SizeExpression);

            /* construct array operation.  this pops size, but pushes new array reference */
            switch (ArrayConstructor.SymbolTableEntry.GetSymbolVariableDataType())
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
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstArrayConstruction(
            ASTArrayDeclRec ArrayConstructor,
            out bool DidSomething)
        {
            FoldConstExpression(
                ArrayConstructor.SizeExpression,
                out DidSomething);
        }




        public class ASTAssignRec
        {
            public ASTExpressionRec LValueGenerator;
            public ASTExpressionRec ObjectValue;
            public int LineNumber;
        }

        /* create a new assignment node */
        public static ASTAssignRec NewAssignment(
            ASTExpressionRec LeftValue,
            ASTExpressionRec RightValue,
            int LineNumber)
        {
            ASTAssignRec Assignment = new ASTAssignRec();

            Assignment.LineNumber = LineNumber;
            Assignment.LValueGenerator = LeftValue;
            Assignment.ObjectValue = RightValue;

            return Assignment;
        }

        /* type check the assignment node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckAssignment(
            out DataTypes ResultingDataType,
            ASTAssignRec Assignment,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            DataTypes RValueType;
            Error = TypeCheckExpression(out RValueType, Assignment.ObjectValue, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            DataTypes LValueType;
            Error = TypeCheckExpression(out LValueType, Assignment.LValueGenerator, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            if (!CanRightBeMadeToMatchLeft(LValueType, RValueType))
            {
                ErrorLineNumber = Assignment.LineNumber;
                return CompileErrors.eCompileTypeMismatch;
            }

            if (MustRightBePromotedToLeft(LValueType, RValueType))
            {
                /* insert promotion operator above right hand side */
                ASTExpressionRec ReplacementRValue = PromoteTheExpression(RValueType, LValueType,
                    Assignment.ObjectValue, Assignment.LineNumber);
                Assignment.ObjectValue = ReplacementRValue;
                /* sanity check */
                Error = TypeCheckExpression(out RValueType, Assignment.ObjectValue, out ErrorLineNumber);
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
            if (!IsExpressionValidLValue(Assignment.LValueGenerator))
            {
                ErrorLineNumber = Assignment.LineNumber;
                return CompileErrors.eCompileInvalidLValue;
            }

            ResultingDataType = LValueType;
            return CompileErrors.eCompileNoError;
        }

        /* generate code for an assignment.  returns True if successful, or False if it fails. */
        public static void CodeGenAssignment(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTAssignRec Assignment)
        {
            int StackDepth = StackDepthParam;

            /* find out what kind of assignment operation to perform */
            /*  - for single variables:  it stores the value from top of stack into the */
            /*    appropriate index, but does not pop the value. */
            /*  - for array variables:  the array index is computed and pushed on the */
            /*    stack.  then the array index is popped, and the element value is stored. */
            /*    the element value is NOT popped. */
            switch (WhatKindOfExpressionIsThis(Assignment.LValueGenerator))
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case ExprTypes.eExprOperand:
                    {
                        /* evaluate the value expression.  this leaves the result on the stack */
                        CodeGenExpression(
                            FuncCode,
                            ref StackDepth,
                            ref MaxStackDepth,
                            Assignment.ObjectValue);
                        Debug.Assert(StackDepth <= MaxStackDepth);
                        if (StackDepth != StackDepthParam + 1)
                        {
                            // CodeGenExpression made stack depth bad
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }

                        /* get the variable being assigned to */
                        ASTOperandRec TheOperand = GetOperandOutOfExpression(Assignment.LValueGenerator);
                        SymbolRec TheVariable = GetSymbolFromOperand(TheOperand);

                        /* generate the assignment opcode word */
                        Pcodes TheAssignmentOpcode;
                        switch (TheVariable.GetSymbolVariableDataType())
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
                        FuncCode.AddPcodeInstruction(TheAssignmentOpcode, out unused, Assignment.LineNumber);

                        /* generate the assignment operand (destination variable index) */
                        /* stack offsets are negative. */
                        FuncCode.AddPcodeOperandInteger(TheVariable.GetSymbolVariableStackLocation() - StackDepth);
                    }
                    break;

                case ExprTypes.eExprBinaryOperator:
                    {
                        ASTBinaryOpRec TheBinaryOperator = GetBinaryOperatorOutOfExpression(Assignment.LValueGenerator);
                        ASTExpressionRec LeftOperand = GetLeftOperandForBinaryOperator(TheBinaryOperator);
                        ASTExpressionRec RightOperand = GetRightOperandForBinaryOperator(TheBinaryOperator);
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
                        CodeGenExpression(
                            FuncCode,
                            ref StackDepth,
                            ref MaxStackDepth,
                            Assignment.ObjectValue);
                        Debug.Assert(StackDepth <= MaxStackDepth);
                        if (StackDepth != StackDepthParam + 1)
                        {
                            // eval array element new value messed up stack
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }

                        /* 2.  evaluate array reference */
                        CodeGenExpression(
                            FuncCode,
                            ref StackDepth,
                            ref MaxStackDepth,
                            LeftOperand);
                        Debug.Assert(StackDepth <= MaxStackDepth);
                        if (StackDepth != StackDepthParam + 2)
                        {
                            // eval array reference messed up stack
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }

                        /* 3.  evaluate array subscript */
                        CodeGenExpression(
                            FuncCode,
                            ref StackDepth,
                            ref MaxStackDepth,
                            RightOperand);
                        Debug.Assert(StackDepth <= MaxStackDepth);
                        if (StackDepth != StackDepthParam + 3)
                        {
                            // eval array subscript messed up stack
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }

                        /* 4.  generate the opcode for storing into an array */
                        Pcodes TheAssignmentOpcode;
                        switch (GetExpressionsResultantType(LeftOperand))
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
                        FuncCode.AddPcodeInstruction(TheAssignmentOpcode, out unused, Assignment.LineNumber);
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

#if true // TODO:experimental
        public static void ILGenAssignment(
            CILObject cilObject,
            ILGenContext context,
            ASTAssignRec Assignment)
        {
            /* find out what kind of assignment operation to perform */
            /*  - for single variables:  it stores the value from top of stack into the */
            /*    appropriate index, but does not pop the value. */
            /*  - for array variables:  the array index is computed and pushed on the */
            /*    stack.  then the array index is popped, and the element value is stored. */
            /*    the element value is NOT popped. */
            switch (WhatKindOfExpressionIsThis(Assignment.LValueGenerator))
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case ExprTypes.eExprOperand:
                    {
                        /* evaluate the value expression.  this leaves the result on the stack */
                        ILGenExpression(
                            cilObject,
                            context,
                            Assignment.ObjectValue);

                        /* get the variable being assigned to */
                        ASTOperandRec TheOperand = GetOperandOutOfExpression(Assignment.LValueGenerator);
                        SymbolRec TheVariable = GetSymbolFromOperand(TheOperand);
                        switch (TheVariable.GetSymbolVariableDataType())
                        {
                            default:
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eBoolean:
                            case DataTypes.eInteger:
                            case DataTypes.eFloat:
                            case DataTypes.eDouble:
                                if (context.variableTable.ContainsKey(TheVariable)) // local variables can mask arguments
                                {
                                    LocalBuilder localVariable = context.variableTable[TheVariable];
                                    context.ilGenerator.Emit(OpCodes.Dup);
                                    context.ilGenerator.Emit(OpCodes.Stloc, localVariable);
                                }
                                else if (context.argumentTable.ContainsKey(TheVariable.GetSymbolName()))
                                {
                                    int argIndex = context.argumentTable[TheVariable.GetSymbolName()];
                                    context.ilGenerator.Emit(OpCodes.Dup);
                                    context.ilGenerator.Emit(OpCodes.Starg, argIndex);
                                }
                                else
                                {
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                }
                                break;

                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                            case DataTypes.eArrayOfInteger:
                            case DataTypes.eArrayOfFloat:
                            case DataTypes.eArrayOfDouble:
                                {
                                    // This has unusual semantics: update of existing array transfers the array ref without
                                    // changing the handle. This allows out-arg behavior for assigning an array arg
                                    // that was passed in.
                                    LocalBuilder localVariable = null;
                                    int argIndex = -1;
                                    if (context.variableTable.ContainsKey(TheVariable)) // local variables can mask arguments
                                    {
                                        localVariable = context.variableTable[TheVariable];
                                    }
                                    else if (context.argumentTable.ContainsKey(TheVariable.GetSymbolName()))
                                    {
                                        argIndex = context.argumentTable[TheVariable.GetSymbolName()];
                                    }
                                    else
                                    {
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    }
                                    Label labelEnd = context.ilGenerator.DefineLabel();
                                    Label labelCopy = context.ilGenerator.DefineLabel();
                                    context.ilGenerator.Emit(OpCodes.Dup);
                                    context.ilGenerator.Emit(OpCodes.Brfalse, labelCopy);
                                    if (localVariable != null)
                                    {
                                        context.ilGenerator.Emit(OpCodes.Ldloc, localVariable);
                                    }
                                    else
                                    {
                                        context.ilGenerator.Emit(OpCodes.Ldarg, (short)argIndex); // operand is 'short' -- see https://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes.ldarg(v=vs.110).aspx
                                    }
                                    context.ilGenerator.Emit(OpCodes.Brfalse, labelCopy);
                                    // transfer inner array ref
                                    context.ilGenerator.Emit(OpCodes.Dup);
                                    LocalBuilder scratchRValue;
                                    switch (TheVariable.GetSymbolVariableDataType())
                                    {
                                        default:
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        case DataTypes.eArrayOfBoolean:
                                            scratchRValue = context.ilGenerator.DeclareLocal(typeof(byte[]));
                                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleByte).GetField("bytes"));
                                            break;
                                        case DataTypes.eArrayOfInteger:
                                            scratchRValue = context.ilGenerator.DeclareLocal(typeof(int[]));
                                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleInt32).GetField("ints"));
                                            break;
                                        case DataTypes.eArrayOfFloat:
                                            scratchRValue = context.ilGenerator.DeclareLocal(typeof(float[]));
                                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleFloat).GetField("floats"));
                                            break;
                                        case DataTypes.eArrayOfDouble:
                                            scratchRValue = context.ilGenerator.DeclareLocal(typeof(double[]));
                                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleDouble).GetField("doubles"));
                                            break;
                                    }
                                    context.ilGenerator.Emit(OpCodes.Stloc, scratchRValue);
                                    if (localVariable != null)
                                    {
                                        context.ilGenerator.Emit(OpCodes.Ldloc, localVariable);
                                    }
                                    else
                                    {
                                        context.ilGenerator.Emit(OpCodes.Ldarg, (short)argIndex); // operand is 'short' -- see https://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes.ldarg(v=vs.110).aspx
                                    }
                                    context.ilGenerator.Emit(OpCodes.Ldloc, scratchRValue);
                                    switch (TheVariable.GetSymbolVariableDataType())
                                    {
                                        default:
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        case DataTypes.eArrayOfBoolean:
                                            scratchRValue = context.ilGenerator.DeclareLocal(typeof(byte[]));
                                            context.ilGenerator.Emit(OpCodes.Stfld, typeof(ArrayHandleByte).GetField("bytes"));
                                            break;
                                        case DataTypes.eArrayOfInteger:
                                            scratchRValue = context.ilGenerator.DeclareLocal(typeof(int[]));
                                            context.ilGenerator.Emit(OpCodes.Stfld, typeof(ArrayHandleInt32).GetField("ints"));
                                            break;
                                        case DataTypes.eArrayOfFloat:
                                            scratchRValue = context.ilGenerator.DeclareLocal(typeof(float[]));
                                            context.ilGenerator.Emit(OpCodes.Stfld, typeof(ArrayHandleFloat).GetField("floats"));
                                            break;
                                        case DataTypes.eArrayOfDouble:
                                            scratchRValue = context.ilGenerator.DeclareLocal(typeof(double[]));
                                            context.ilGenerator.Emit(OpCodes.Stfld, typeof(ArrayHandleDouble).GetField("doubles"));
                                            break;
                                    }
                                    context.ilGenerator.Emit(OpCodes.Br, labelEnd);
                                    // simple copy
                                    context.ilGenerator.MarkLabel(labelCopy);
                                    context.ilGenerator.Emit(OpCodes.Dup);
                                    if (localVariable != null)
                                    {
                                        context.ilGenerator.Emit(OpCodes.Stloc, localVariable);
                                    }
                                    else
                                    {
                                        context.ilGenerator.Emit(OpCodes.Starg, argIndex);
                                    }
                                    context.ilGenerator.MarkLabel(labelEnd);
                                }
                                break;
                        }
                    }
                    break;

                case ExprTypes.eExprBinaryOperator:
                    {
                        ASTBinaryOpRec TheBinaryOperator = GetBinaryOperatorOutOfExpression(Assignment.LValueGenerator);
                        ASTExpressionRec LeftOperand = GetLeftOperandForBinaryOperator(TheBinaryOperator);
                        ASTExpressionRec RightOperand = GetRightOperandForBinaryOperator(TheBinaryOperator);
                        /* the left operand is the array reference generator */
                        /* the right operand is the array index generator. */

                        // .NET CIL array semantics are different than the pcode scheme, so the order of
                        // clause evaluation here is different than that in CodeGenAssignment(). The language is
                        // sufficiently restrictive that code written to depend on evaluation order of array
                        // reference is unlikely, and performance is prioritized.

                        /* 1 [was 2].  evaluate array reference */
                        ILGenExpression(
                            cilObject,
                            context,
                            LeftOperand);
                        // dereference the array handle
                        switch (GetExpressionsResultantType(LeftOperand))
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
                        ILGenExpression(
                            cilObject,
                            context,
                            RightOperand);

                        /* 3 [was 1].  evaluate the value expression */
                        ILGenExpression(
                            cilObject,
                            context,
                            Assignment.ObjectValue);

                        // because we leave rval on stack (and stelem doesn't), need to save a copy in
                        // a scratch variable
                        LocalBuilder scratchVariable = context.ilGenerator.DeclareLocal(
                            CILObject.GetManagedType(Assignment.ObjectValue.WhatIsTheExpressionType));
                        context.ilGenerator.Emit(OpCodes.Dup, scratchVariable);
                        context.ilGenerator.Emit(OpCodes.Stloc, scratchVariable);

                        // store to array
                        switch (GetExpressionsResultantType(LeftOperand))
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
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstAssignment(
            ASTAssignRec Assignment,
            out bool DidSomething)
        {
            bool WeDidSomething = false;
            bool TheyDidSomething;

            FoldConstExpression(Assignment.LValueGenerator, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            FoldConstExpression(Assignment.ObjectValue, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            DidSomething = WeDidSomething;
        }




        /* binary operator operations */
        public enum BinaryOpType
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

        public class ASTBinaryOpRec
        {
            public BinaryOpType OperationType;
            public ASTExpressionRec LeftArg;
            public ASTExpressionRec RightArg;
            public int LineNumber;
        }

        public static ASTBinaryOpRec NewBinaryOperator(
            BinaryOpType Operation,
            ASTExpressionRec LeftArgument,
            ASTExpressionRec RightArgument,
            int LineNumber)
        {
            ASTBinaryOpRec BinaryOp = new ASTBinaryOpRec();

            BinaryOp.LineNumber = LineNumber;
            BinaryOp.OperationType = Operation;
            BinaryOp.LeftArg = LeftArgument;
            BinaryOp.RightArg = RightArgument;

            return BinaryOp;
        }

        /* do any needed type promotion */
        private static CompileErrors PromoteTypeHelper(
            ref DataTypes LeftOperandType,
            ref DataTypes RightOperandType,
            ASTBinaryOpRec BinaryOperator,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;

            if (CanRightBeMadeToMatchLeft(LeftOperandType, RightOperandType)
                && MustRightBePromotedToLeft(LeftOperandType, RightOperandType))
            {
                /* we must promote the right operand to become the left operand type */
                ASTExpressionRec PromotedRightOperand = PromoteTheExpression(
                    RightOperandType/*orig*/,
                    LeftOperandType/*desired*/,
                    BinaryOperator.RightArg,
                    BinaryOperator.LineNumber);
                BinaryOperator.RightArg = PromotedRightOperand;
                /* sanity check */
                Error = TypeCheckExpression(
                    out RightOperandType/*obtain new right type*/,
                    BinaryOperator.RightArg,
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
                ASTExpressionRec PromotedLeftOperand = PromoteTheExpression(
                    LeftOperandType/*orig*/,
                    RightOperandType/*desired*/,
                    BinaryOperator.LeftArg,
                    BinaryOperator.LineNumber);
                BinaryOperator.LeftArg = PromotedLeftOperand;
                /* sanity check */
                Error = TypeCheckExpression(
                    out LeftOperandType/*obtain new left type*/,
                    BinaryOperator.LeftArg,
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

        /* type check the binary operator node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckBinaryOperator(
            out DataTypes ResultingDataType,
            ASTBinaryOpRec BinaryOperator,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

        Restart:
            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            DataTypes LeftOperandType;
            Error = TypeCheckExpression(out LeftOperandType, BinaryOperator.LeftArg, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            DataTypes RightOperandType;
            Error = TypeCheckExpression(out RightOperandType, BinaryOperator.RightArg, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* do type checking and promotion.  return type determination is deferred */
            switch (BinaryOperator.OperationType)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                /* operators capable of boolean, integer, single, double, and fixed args, */
                /* which return a boolean result */
                case BinaryOpType.eBinaryEqual:
                case BinaryOpType.eBinaryNotEqual:
                    if (!CanRightBeMadeToMatchLeft(LeftOperandType, RightOperandType)
                        && !CanRightBeMadeToMatchLeft(RightOperandType, LeftOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileTypeMismatch;
                    }
                    if ((IsItAScalarType(LeftOperandType) && !IsItAScalarType(RightOperandType))
                        || (!IsItAScalarType(LeftOperandType) && IsItAScalarType(RightOperandType)))
                    {
                        // IsItAScalarType error
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (!IsItAScalarType(LeftOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileOperandsMustBeScalar;
                    }
                    /* do type promotion */
                    Error = PromoteTypeHelper(ref LeftOperandType, ref RightOperandType, BinaryOperator, out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    break;

                /* operators capable of boolean, integer, and fixed, */
                /* which return the same type as the arguments */
                case BinaryOpType.eBinaryAnd:
                case BinaryOpType.eBinaryOr:
                case BinaryOpType.eBinaryXor:
                    if ((LeftOperandType != DataTypes.eBoolean) && (LeftOperandType != DataTypes.eInteger))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileTypeMismatch;
                    }
                    if (LeftOperandType != RightOperandType)
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileTypeMismatch;
                    }
                    break;

                /* operators capable of integer, single, double, and fixed args, */
                /* where the return is the same type as args */
                case BinaryOpType.eBinaryPlus:
                case BinaryOpType.eBinaryMinus:
                case BinaryOpType.eBinaryMultiplication:
                /* FALL THROUGH! */

                /* operators capable of integer, single, double, and fixed args, */
                /* which return a boolean */
                case BinaryOpType.eBinaryLessThan:
                case BinaryOpType.eBinaryLessThanOrEqual:
                case BinaryOpType.eBinaryGreaterThan:
                case BinaryOpType.eBinaryGreaterThanOrEqual:
                    if (!CanRightBeMadeToMatchLeft(LeftOperandType, RightOperandType)
                        && !CanRightBeMadeToMatchLeft(RightOperandType, LeftOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileTypeMismatch;
                    }
                    if ((IsItAScalarType(LeftOperandType) && !IsItAScalarType(RightOperandType))
                        || (!IsItAScalarType(LeftOperandType) && IsItAScalarType(RightOperandType)))
                    {
                        // IsItAScalarType error
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (!IsItASequencedScalarType(LeftOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileOperandsMustBeSequencedScalar;
                    }
                    /* do type promotion */
                    Error = PromoteTypeHelper(ref LeftOperandType, ref RightOperandType, BinaryOperator, out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    break;

                /* operators capable of single, double, and fixed args, */
                /* where the return is a single, double, or fixed */
                case BinaryOpType.eBinaryImpreciseDivision:
                    if (LeftOperandType == DataTypes.eInteger)
                    {
                        BinaryOperator.LeftArg = PromoteTheExpression(
                            LeftOperandType,
                            DataTypes.eDouble,
                            BinaryOperator.LeftArg,
                            BinaryOperator.LeftArg.LineNumber);
                        goto Restart;
                    }
                    if (RightOperandType == DataTypes.eInteger)
                    {
                        BinaryOperator.RightArg = PromoteTheExpression(
                            RightOperandType,
                            DataTypes.eDouble,
                            BinaryOperator.RightArg,
                            BinaryOperator.RightArg.LineNumber);
                        goto Restart;
                    }
                    if (!CanRightBeMadeToMatchLeft(LeftOperandType, RightOperandType)
                        && !CanRightBeMadeToMatchLeft(RightOperandType, LeftOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileTypeMismatch;
                    }
                    if ((IsItAScalarType(LeftOperandType) && !IsItAScalarType(RightOperandType))
                        || (!IsItAScalarType(LeftOperandType) && IsItAScalarType(RightOperandType)))
                    {
                        // IsItAScalarType error
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (!IsItASequencedScalarType(LeftOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileOperandsMustBeSequencedScalar;
                    }
                    /* do type promotion */
                    Error = PromoteTypeHelper(ref LeftOperandType, ref RightOperandType, BinaryOperator, out ErrorLineNumber);
                    if (Error != CompileErrors.eCompileNoError)
                    {
                        return Error;
                    }
                    break;

                /* operators capable of integers, returning integer results */
                case BinaryOpType.eBinaryIntegerDivision:
                case BinaryOpType.eBinaryIntegerRemainder:
                case BinaryOpType.eBinaryShiftLeft:
                case BinaryOpType.eBinaryShiftRight:
                    if ((LeftOperandType != DataTypes.eInteger) || (RightOperandType != DataTypes.eInteger))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileOperandsMustBeIntegers;
                    }
                    /* no type promotion is necessary */
                    break;

                /* operators where the left argument must be an array and the right */
                /* argument must be an integer, and the array's element type is returned */
                case BinaryOpType.eBinaryArraySubscripting:
                    if (RightOperandType != DataTypes.eInteger)
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileArraySubscriptMustBeInteger;
                    }
                    if (!IsItAnIndexedType(LeftOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileArrayRequiredForSubscription;
                    }
                    break;

                /* operators where the arguments are double, */
                /* and which return a double result */
                case BinaryOpType.eBinaryExponentiation:
                    if (!CanRightBeMadeToMatchLeft(DataTypes.eDouble, LeftOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileDoubleRequiredForExponentiation;
                    }
                    if (!CanRightBeMadeToMatchLeft(DataTypes.eDouble, RightOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileDoubleRequiredForExponentiation;
                    }
                    /* force the promotion, if necessary */
                    if (LeftOperandType != DataTypes.eDouble)
                    {
                        /* promote right operand to double, so left operand is a fake double */
                        DataTypes FakePromotionForcer = DataTypes.eDouble;
                        Error = PromoteTypeHelper(ref LeftOperandType, ref FakePromotionForcer,
                            BinaryOperator, out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }
                        if ((FakePromotionForcer != DataTypes.eDouble) || (LeftOperandType != DataTypes.eDouble))
                        {
                            // exponent convert to double promotion failed"));
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                    }
                    if (RightOperandType != DataTypes.eDouble)
                    {
                        /* promote left operand to double, so right operand is a fake double */
                        DataTypes FakePromotionForcer = DataTypes.eDouble;
                        Error = PromoteTypeHelper(ref FakePromotionForcer, ref RightOperandType,
                            BinaryOperator, out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            return Error;
                        }
                        if ((FakePromotionForcer != DataTypes.eDouble) || (RightOperandType != DataTypes.eDouble))
                        {
                            // exponent conver to double promotion failed"));
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                    }
                    break;

                /* operators where left must be an array type and right must be an integer */
                /* and an array of the same type is returned */
                case BinaryOpType.eBinaryResizeArray:
                    if (!IsItAnIndexedType(LeftOperandType))
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileArrayRequiredForResize;
                    }
                    if (RightOperandType != DataTypes.eInteger)
                    {
                        ErrorLineNumber = BinaryOperator.LineNumber;
                        return CompileErrors.eCompileIntegerRequiredForResize;
                    }
                    break;
            }

            /* now, figure out what the return type should be */
            switch (BinaryOperator.OperationType)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                /* operators capable of boolean, integer, single, double, and fixed args, */
                /* which return a boolean result */
                case BinaryOpType.eBinaryEqual:
                case BinaryOpType.eBinaryNotEqual:
                    if (LeftOperandType != RightOperandType)
                    {
                        // operand types are not equivalent but should be
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (!IsItAScalarType(LeftOperandType))
                    {
                        // operand types are not scalar but should be
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eBoolean;
                    break;

                /* operators capable of boolean, integer, single, double, and fixed args, */
                /* which return the same type as the arguments */
                case BinaryOpType.eBinaryAnd:
                case BinaryOpType.eBinaryOr:
                case BinaryOpType.eBinaryXor:
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
                case BinaryOpType.eBinaryPlus:
                case BinaryOpType.eBinaryMinus:
                case BinaryOpType.eBinaryMultiplication:
                    if (LeftOperandType != RightOperandType)
                    {
                        // operand types are not equivalent but should be
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (!IsItASequencedScalarType(LeftOperandType))
                    {
                        // operand types are not seq scalar but should be
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = LeftOperandType;
                    break;

                /* operators capable of integer, single, double, and fixed args, */
                /* where the return is a single, double, or fixed */
                case BinaryOpType.eBinaryImpreciseDivision:
                    if (LeftOperandType != RightOperandType)
                    {
                        //operand types are not equivalent but should be
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (!IsItASequencedScalarType(LeftOperandType))
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
                case BinaryOpType.eBinaryLessThan:
                case BinaryOpType.eBinaryLessThanOrEqual:
                case BinaryOpType.eBinaryGreaterThan:
                case BinaryOpType.eBinaryGreaterThanOrEqual:
                    if (LeftOperandType != RightOperandType)
                    {
                        // operand types are not equivalent but should be
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (!IsItAScalarType(LeftOperandType))
                    {
                        // operand types are not scalar but should be
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eBoolean;
                    break;

                /* operators capable of integers, returning integer results */
                case BinaryOpType.eBinaryIntegerDivision:
                case BinaryOpType.eBinaryIntegerRemainder:
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
                case BinaryOpType.eBinaryShiftLeft:
                case BinaryOpType.eBinaryShiftRight:
                    if (RightOperandType != DataTypes.eInteger)
                    {
                        // right operand should be integer
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (!IsItASequencedScalarType(LeftOperandType))
                    {
                        // left operand should be seq scalar
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = LeftOperandType;
                    break;

                /* operators where the left argument must be an array and the right */
                /* argument must be an integer, and the array's element type is returned */
                case BinaryOpType.eBinaryArraySubscripting:
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
                case BinaryOpType.eBinaryExponentiation:
                    if ((LeftOperandType != DataTypes.eDouble) || (RightOperandType != DataTypes.eDouble))
                    {
                        // operands should be double
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eDouble;
                    break;

                /* operators where left must be an array type and right must be an integer */
                /* and an array of the same type is returned */
                case BinaryOpType.eBinaryResizeArray:
                    if (!IsItAnIndexedType(LeftOperandType))
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

            return CompileErrors.eCompileNoError;
        }

        /* find out just what kind of binary operation this is */
        public static BinaryOpType BinaryOperatorWhichOne(ASTBinaryOpRec TheBinOp)
        {
            return TheBinOp.OperationType;
        }

        public static ASTExpressionRec GetLeftOperandForBinaryOperator(ASTBinaryOpRec TheBinOp)
        {
            return TheBinOp.LeftArg;
        }

        public static ASTExpressionRec GetRightOperandForBinaryOperator(ASTBinaryOpRec TheBinOp)
        {
            return TheBinOp.RightArg;
        }

        /* generate code for a binary operator.  returns True if successful, or False if it fails. */
        public static void CodeGenBinaryOperator(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTBinaryOpRec BinaryOperator)
        {
            int StackDepth = StackDepthParam;

            /* generate code for left operand */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                BinaryOperator.LeftArg);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 1)
            {
                // stack depth error on left operand
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* generate code for the right operand */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                BinaryOperator.RightArg);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 2)
            {
                // stack depth error on right operand
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* generate the opcode for performing the computation */
            Pcodes Opcode;
            switch (BinaryOperator.OperationType)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case BinaryOpType.eBinaryAnd:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryOr:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryOr]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryXor:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryXor]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryLessThan:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryLessThan]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryLessThanOrEqual:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryLessThanOrEqual]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryGreaterThan:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryGreaterThan]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryGreaterThanOrEqual:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryGreaterThanOrEqual]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryEqual:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryEqual]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryNotEqual:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryNotEqual]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryPlus:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryPlus]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryMinus:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryMinus]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryMultiplication:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryMultiplication]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryImpreciseDivision:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryImpreciseDivision]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryIntegerDivision:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryIntegerDivision]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryIntegerRemainder:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryIntegerRemainder]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryShiftLeft:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eInteger)
                    {
                        // CodeGenBinaryOperator[eBinaryShiftLeft]:  type check failure -- right operand isn't an integer
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryShiftRight:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eInteger)
                    {
                        // CodeGenBinaryOperator[eBinaryShiftRight]:  type check failure -- right operand isn't an integer
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryArraySubscripting:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eInteger)
                    {
                        // CodeGenBinaryOperator[eBinaryArraySubscripting]:  type check failure -- right operand isn't an integer
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryExponentiation:
                    if ((GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eDouble)
                        || (GetExpressionsResultantType(BinaryOperator.LeftArg) != DataTypes.eDouble))
                    {
                        // CodeGenBinaryOperator[eBinaryExponentiation]:  type check failure -- an argument isn't a double
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        default:
                            // CodeGenBinaryOperator[eBinaryExponentiation]:  bad operand types
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoublePower;
                            break;
                    }
                    break;

                case BinaryOpType.eBinaryResizeArray:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eInteger)
                    {
                        // CodeGenBinaryOperator[eBinaryResizeArray]:  type check failure -- right operand isn't an integer
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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
            FuncCode.AddPcodeInstruction(Opcode, out unused, BinaryOperator.LineNumber);
            StackDepth--;
            if (StackDepth != StackDepthParam + 1)
            {
                // post operator stack size is screwed up
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            StackDepthParam = StackDepth;
        }

#if true // TODO:experimental
        public static void ILGenBinaryOperator(
            CILObject cilObject,
            ILGenContext context,
            ASTBinaryOpRec BinaryOperator)
        {
            /* generate code for left operand */
            ILGenExpression(
                cilObject,
                context,
                BinaryOperator.LeftArg);

            /* generate code for the right operand */
            ILGenExpression(
                cilObject,
                context,
                BinaryOperator.RightArg);

            /* generate the opcode for performing the computation */
            switch (BinaryOperator.OperationType)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case BinaryOpType.eBinaryAnd:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryOr:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryOr]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryXor:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryXor]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryLessThan:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryLessThan]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryLessThanOrEqual:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryLessThanOrEqual]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryGreaterThan:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryGreaterThan]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryGreaterThanOrEqual:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryGreaterThanOrEqual]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryEqual:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryEqual]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryNotEqual:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryNotEqual]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryPlus:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryPlus]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryMinus:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryMinus]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryMultiplication:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryMultiplication]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryImpreciseDivision:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryImpreciseDivision]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryIntegerDivision:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryIntegerDivision]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryIntegerRemainder:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg)
                        != GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        // CodeGenBinaryOperator[eBinaryIntegerRemainder]:  type check failure -- operands are not the same type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryShiftLeft:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eInteger)
                    {
                        // CodeGenBinaryOperator[eBinaryShiftLeft]:  type check failure -- right operand isn't an integer
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryShiftRight:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eInteger)
                    {
                        // CodeGenBinaryOperator[eBinaryShiftRight]:  type check failure -- right operand isn't an integer
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryArraySubscripting:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eInteger)
                    {
                        // CodeGenBinaryOperator[eBinaryArraySubscripting]:  type check failure -- right operand isn't an integer
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    {
                        // save subscript to dereference array handle
                        LocalBuilder scratchVariable = context.ilGenerator.DeclareLocal(typeof(int));
                        context.ilGenerator.Emit(OpCodes.Stloc, scratchVariable);
                        switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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
                        switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                case BinaryOpType.eBinaryExponentiation:
                    if ((GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eDouble)
                        || (GetExpressionsResultantType(BinaryOperator.LeftArg) != DataTypes.eDouble))
                    {
                        // CodeGenBinaryOperator[eBinaryExponentiation]:  type check failure -- an argument isn't a double
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                    {
                        default:
                            // CodeGenBinaryOperator[eBinaryExponentiation]:  bad operand types
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Pow", new Type[] { typeof(double), typeof(double) }));
                            break;
                    }
                    break;

                case BinaryOpType.eBinaryResizeArray:
                    if (GetExpressionsResultantType(BinaryOperator.RightArg) != DataTypes.eInteger)
                    {
                        // CodeGenBinaryOperator[eBinaryResizeArray]:  type check failure -- right operand isn't an integer
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    {
                        // save subscript to dereference array handle
                        LocalBuilder scratchVariable = context.ilGenerator.DeclareLocal(typeof(int));
                        context.ilGenerator.Emit(OpCodes.Stloc, scratchVariable);
                        context.ilGenerator.Emit(OpCodes.Dup); // duplicate array handle for return value
                        switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryResizeArray]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                                context.ilGenerator.Emit(OpCodes.Ldflda, typeof(ArrayHandleByte).GetField("bytes"));
                                break;
                            case DataTypes.eArrayOfInteger:
                                context.ilGenerator.Emit(OpCodes.Ldflda, typeof(ArrayHandleInt32).GetField("ints"));
                                break;
                            case DataTypes.eArrayOfFloat:
                                context.ilGenerator.Emit(OpCodes.Ldflda, typeof(ArrayHandleFloat).GetField("floats"));
                                break;
                            case DataTypes.eArrayOfDouble:
                                context.ilGenerator.Emit(OpCodes.Ldflda, typeof(ArrayHandleDouble).GetField("doubles"));
                                break;
                        }
                        context.ilGenerator.Emit(OpCodes.Ldloc, scratchVariable);
                        MethodInfo resizeMethod = null;
                        foreach (MethodInfo methodInfo in typeof(Array).GetMethods())
                        {
                            if (String.Equals(methodInfo.Name, "Resize"))
                            {
                                resizeMethod = methodInfo;
                                break;
                            }
                        }
                        switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                        {
                            default:
                                // CodeGenBinaryOperator[eBinaryResizeArray]:  bad operand types
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            case DataTypes.eArrayOfBoolean:
                            case DataTypes.eArrayOfByte:
                                context.ilGenerator.Emit(OpCodes.Call, resizeMethod.MakeGenericMethod(new Type[] { typeof(byte) }));
                                break;
                            case DataTypes.eArrayOfInteger:
                                context.ilGenerator.Emit(OpCodes.Call, resizeMethod.MakeGenericMethod(new Type[] { typeof(int) }));
                                break;
                            case DataTypes.eArrayOfFloat:
                                context.ilGenerator.Emit(OpCodes.Call, resizeMethod.MakeGenericMethod(new Type[] { typeof(float) }));
                                break;
                            case DataTypes.eArrayOfDouble:
                                context.ilGenerator.Emit(OpCodes.Call, resizeMethod.MakeGenericMethod(new Type[] { typeof(double) }));
                                break;
                        }
                    }
                    break;
            }
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstBinaryOperator(
            ASTBinaryOpRec BinaryOperator,
            DataTypes OriginalResultType,
            out bool DidSomething,
            out ASTExpressionRec ResultantExpr,
            out DataTypes ResultType)
        {
            bool WeDidSomething = false;
            bool TheyDidSomething;
            ASTOperandRec NewOperand = null;

            ResultantExpr = null;
            ResultType = DataTypes.eInvalidDataType;

            FoldConstExpression(BinaryOperator.LeftArg, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            FoldConstExpression(BinaryOperator.RightArg, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            if ((WhatKindOfExpressionIsThis(BinaryOperator.LeftArg) == ExprTypes.eExprOperand)
                && (WhatKindOfExpressionIsThis(BinaryOperator.RightArg) == ExprTypes.eExprOperand))
            {
                ASTOperandType LeftOperandType;
                ASTOperandType RightOperandType;

                LeftOperandType = OperandWhatIsIt(GetOperandFromExpression(BinaryOperator.LeftArg));
                RightOperandType = OperandWhatIsIt(GetOperandFromExpression(BinaryOperator.RightArg));
                if (
                    ((LeftOperandType == ASTOperandType.eASTOperandIntegerLiteral)
                    || (LeftOperandType == ASTOperandType.eASTOperandBooleanLiteral)
                    || (LeftOperandType == ASTOperandType.eASTOperandSingleLiteral)
                    || (LeftOperandType == ASTOperandType.eASTOperandDoubleLiteral))
                    &&
                    ((RightOperandType == ASTOperandType.eASTOperandIntegerLiteral)
                    || (RightOperandType == ASTOperandType.eASTOperandBooleanLiteral)
                    || (RightOperandType == ASTOperandType.eASTOperandSingleLiteral)
                    || (RightOperandType == ASTOperandType.eASTOperandDoubleLiteral))
                    )
                {
                    switch (BinaryOperator.OperationType)
                    {
                        default:
                            // FoldConstBinaryOperator: unknown operator
                            Debug.Assert(false);
                            throw new InvalidOperationException();

                        case BinaryOpType.eBinaryArraySubscripting:
                        case BinaryOpType.eBinaryResizeArray:
                            break;

                        case BinaryOpType.eBinaryAnd:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandBooleanLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandBooleanLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue && RightValue;
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eInteger:
                                    {
                                        int LeftValue;
                                        int RightValue;
                                        int NewValue;

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue & RightValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryOr:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandBooleanLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandBooleanLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue || RightValue;
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eInteger:
                                    {
                                        int LeftValue;
                                        int RightValue;
                                        int NewValue;

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue | RightValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryXor:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandBooleanLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandBooleanLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = (!LeftValue != !RightValue);
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eInteger:
                                    {
                                        int LeftValue;
                                        int RightValue;
                                        int NewValue;

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue ^ RightValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryLessThan:
                            {
                                bool NewValue;

                                switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;

                                            LeftValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue < RightValue;
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;

                                            LeftValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue < RightValue;
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;

                                            LeftValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue < RightValue;
                                        }
                                        break;
                                }

                                ResultType = DataTypes.eBoolean;

                                NewOperand = NewBooleanLiteral(
                                    NewValue,
                                    BinaryOperator.LineNumber);
                            }
                            break;

                        case BinaryOpType.eBinaryLessThanOrEqual:
                            {
                                bool NewValue;

                                switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;

                                            LeftValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue <= RightValue;
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;

                                            LeftValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue <= RightValue;
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;

                                            LeftValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue <= RightValue;
                                        }
                                        break;
                                }

                                ResultType = DataTypes.eBoolean;

                                NewOperand = NewBooleanLiteral(
                                    NewValue,
                                    BinaryOperator.LineNumber);
                            }
                            break;

                        case BinaryOpType.eBinaryGreaterThan:
                            {
                                bool NewValue;

                                switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;

                                            LeftValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue > RightValue;
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;

                                            LeftValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue > RightValue;
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;

                                            LeftValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue > RightValue;
                                        }
                                        break;
                                }

                                ResultType = DataTypes.eBoolean;

                                NewOperand = NewBooleanLiteral(
                                    NewValue,
                                    BinaryOperator.LineNumber);
                            }
                            break;

                        case BinaryOpType.eBinaryGreaterThanOrEqual:
                            {
                                bool NewValue;

                                switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;

                                            LeftValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue >= RightValue;
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;

                                            LeftValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue >= RightValue;
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;

                                            LeftValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue >= RightValue;
                                        }
                                        break;
                                }

                                ResultType = DataTypes.eBoolean;

                                NewOperand = NewBooleanLiteral(
                                    NewValue,
                                    BinaryOperator.LineNumber);
                            }
                            break;

                        case BinaryOpType.eBinaryEqual:
                            {
                                bool NewValue;

                                switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eBoolean:
                                        {
                                            bool LeftValue;
                                            bool RightValue;

                                            LeftValue = GetOperandBooleanLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandBooleanLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = (!LeftValue == !RightValue);
                                        }
                                        break;
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;

                                            LeftValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue == RightValue;
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;

                                            LeftValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue == RightValue;
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;

                                            LeftValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue == RightValue;
                                        }
                                        break;
                                }

                                ResultType = DataTypes.eBoolean;

                                NewOperand = NewBooleanLiteral(
                                    NewValue,
                                    BinaryOperator.LineNumber);
                            }
                            break;

                        case BinaryOpType.eBinaryNotEqual:
                            {
                                bool NewValue;

                                switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                                {
                                    default:
                                        // FoldConstBinaryOperator: illegal operand type
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    case DataTypes.eBoolean:
                                        {
                                            bool LeftValue;
                                            bool RightValue;

                                            LeftValue = GetOperandBooleanLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandBooleanLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = (!LeftValue != !RightValue);
                                        }
                                        break;
                                    case DataTypes.eInteger:
                                        {
                                            int LeftValue;
                                            int RightValue;

                                            LeftValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandIntegerLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue != RightValue;
                                        }
                                        break;
                                    case DataTypes.eFloat:
                                        {
                                            float LeftValue;
                                            float RightValue;

                                            LeftValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandSingleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue != RightValue;
                                        }
                                        break;
                                    case DataTypes.eDouble:
                                        {
                                            double LeftValue;
                                            double RightValue;

                                            LeftValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.LeftArg));
                                            RightValue = GetOperandDoubleLiteral(
                                                GetOperandFromExpression(BinaryOperator.RightArg));
                                            NewValue = LeftValue != RightValue;
                                        }
                                        break;
                                }

                                ResultType = DataTypes.eBoolean;

                                NewOperand = NewBooleanLiteral(
                                    NewValue,
                                    BinaryOperator.LineNumber);
                            }
                            break;

                        case BinaryOpType.eBinaryPlus:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue + RightValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eFloat:
                                    {
                                        float LeftValue;
                                        float RightValue;
                                        float NewValue;

                                        LeftValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue + RightValue;
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eDouble:
                                    {
                                        double LeftValue;
                                        double RightValue;
                                        double NewValue;

                                        LeftValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue + RightValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryMinus:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue - RightValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eFloat:
                                    {
                                        float LeftValue;
                                        float RightValue;
                                        float NewValue;

                                        LeftValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue - RightValue;
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eDouble:
                                    {
                                        double LeftValue;
                                        double RightValue;
                                        double NewValue;

                                        LeftValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue - RightValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryMultiplication:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue * RightValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eFloat:
                                    {
                                        float LeftValue;
                                        float RightValue;
                                        float NewValue;

                                        LeftValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue * RightValue;
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eDouble:
                                    {
                                        double LeftValue;
                                        double RightValue;
                                        double NewValue;

                                        LeftValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue * RightValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryImpreciseDivision:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = (double)LeftValue / (double)RightValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eFloat:
                                    {
                                        float LeftValue;
                                        float RightValue;
                                        float NewValue;

                                        LeftValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue / RightValue;
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eDouble:
                                    {
                                        double LeftValue;
                                        double RightValue;
                                        double NewValue;

                                        LeftValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue / RightValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryIntegerDivision:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        /* if divisor is zero, don't optimize out, so that it */
                                        /* will generate an exception during execution. */
                                        if (RightValue != 0)
                                        {
                                            NewValue = LeftValue / RightValue;
                                            ResultType = DataTypes.eInteger;

                                            NewOperand = NewIntegerLiteral(
                                                NewValue,
                                                BinaryOperator.LineNumber);
                                        }
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryIntegerRemainder:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        /* if divisor is zero, don't optimize out, so that it */
                                        /* will generate an exception during execution. */
                                        if (RightValue != 0)
                                        {
                                            NewValue = LeftValue % RightValue;
                                            ResultType = DataTypes.eInteger;

                                            NewOperand = NewIntegerLiteral(
                                                NewValue,
                                                BinaryOperator.LineNumber);
                                        }
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryShiftLeft:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue << RightValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eFloat:
                                    {
                                        float LeftValue;
                                        int RightValue;
                                        float NewValue;

                                        LeftValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = (float)(LeftValue * Math.Pow(2, RightValue));
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eDouble:
                                    {
                                        double LeftValue;
                                        int RightValue;
                                        double NewValue;

                                        LeftValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue * Math.Pow(2, RightValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryShiftRight:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
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

                                        LeftValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue / (1 << RightValue);
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eFloat:
                                    {
                                        float LeftValue;
                                        int RightValue;
                                        float NewValue;

                                        LeftValue = GetOperandSingleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = (float)(LeftValue * Math.Pow(2, -RightValue));
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                                case DataTypes.eDouble:
                                    {
                                        double LeftValue;
                                        int RightValue;
                                        double NewValue;

                                        LeftValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandIntegerLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = LeftValue * Math.Pow(2, -RightValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;

                        case BinaryOpType.eBinaryExponentiation:
                            switch (GetExpressionsResultantType(BinaryOperator.LeftArg))
                            {
                                default:
                                    // FoldConstBinaryOperator: illegal operand type
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case DataTypes.eDouble:
                                    {
                                        double LeftValue;
                                        double RightValue;
                                        double NewValue;

                                        LeftValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.LeftArg));
                                        RightValue = GetOperandDoubleLiteral(
                                            GetOperandFromExpression(BinaryOperator.RightArg));
                                        NewValue = Math.Pow(LeftValue, RightValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            BinaryOperator.LineNumber);
                                    }
                                    break;
                            }
                            break;
                    }
                }
            }

            if (NewOperand != null)
            {
                ResultantExpr = NewExprOperand(
                    NewOperand,
                    BinaryOperator.LineNumber);
            }
            else
            {
                ResultantExpr = NewExprBinaryOperator(
                    BinaryOperator,
                    BinaryOperator.LineNumber);
                ResultType = OriginalResultType;
            }

            DidSomething = WeDidSomething;
        }




        public class ASTCondRec
        {
            public ASTExpressionRec Conditional;
            public ASTExpressionRec Consequent;
            public ASTExpressionRec Alternate; /* may be NIL */
            public int LineNumber;
        }

        /* create a new if node.  the Alternate can be NIL. */
        public static ASTCondRec NewConditional(
            ASTExpressionRec Conditional,
            ASTExpressionRec Consequent,
            ASTExpressionRec Alternate,
            int LineNumber)
        {
            ASTCondRec MyCond = new ASTCondRec();

            MyCond.LineNumber = LineNumber;
            MyCond.Conditional = Conditional;
            MyCond.Consequent = Consequent;
            MyCond.Alternate = Alternate;

            return MyCond;
        }

        /* type check the conditional node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckConditional(
            out DataTypes ResultingDataType,
            ASTCondRec Conditional,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            DataTypes ConditionalReturnType;
            Error = TypeCheckExpression(out ConditionalReturnType, Conditional.Conditional, out ErrorLineNumber);
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
            Error = TypeCheckExpression(out ConsequentReturnType, Conditional.Consequent, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            if (Conditional.Alternate == null)
            {
                /* no else clause */
                ResultingDataType = ConsequentReturnType;
                return CompileErrors.eCompileNoError;
            }
            else
            {
                /* there is an else clause */
                DataTypes AlternateReturnType;
                Error = TypeCheckExpression(out AlternateReturnType, Conditional.Alternate, out ErrorLineNumber);
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
                        ASTExpressionRec PromotedThing = PromoteTheExpression(
                            AlternateReturnType/*orig*/,
                            ConsequentReturnType/*desired*/,
                            Conditional.Alternate,
                            Conditional.LineNumber);
                        Conditional.Alternate = PromotedThing;
                        /* sanity check */
                        Error = TypeCheckExpression(out AlternateReturnType/*obtain new type*/,
                            Conditional.Alternate, out ErrorLineNumber);
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
                        ASTExpressionRec PromotedThing = PromoteTheExpression(
                            ConsequentReturnType/*orig*/,
                            AlternateReturnType/*desired*/,
                            Conditional.Consequent,
                            Conditional.LineNumber);
                        Conditional.Consequent = PromotedThing;
                        /* sanity check */
                        Error = TypeCheckExpression(out ConsequentReturnType/*obtain new type*/,
                            Conditional.Consequent, out ErrorLineNumber);
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
                    ErrorLineNumber = Conditional.LineNumber;
                    return CompileErrors.eCompileTypeMismatchBetweenThenAndElse;
                }
                if (ConsequentReturnType != AlternateReturnType)
                {
                    // Consequent and Alternate return types differ
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                ResultingDataType = ConsequentReturnType;
                return CompileErrors.eCompileNoError;
            }
        }

        /* generate code for a conditional.  returns True if successful, or False if it fails. */
        public static void CodeGenConditional(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTCondRec Conditional)
        {
            int StackDepth = StackDepthParam;

            /* evaluate the condition */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                Conditional.Conditional);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 1)
            {
                // stack bad after evaluating conditional
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* perform branch to bad guy */
            int PatchLocationForConditionalBranch;
            FuncCode.AddPcodeInstruction(Pcodes.epBranchIfZero, out PatchLocationForConditionalBranch, Conditional.LineNumber);
            FuncCode.AddPcodeOperandInteger(-1/*not known yet*/);
            StackDepth--;
            if (StackDepth != StackDepthParam)
            {
                // stack bad after performing conditional branch
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* evaluate the true branch */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                Conditional.Consequent);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 1)
            {
                // stack bad after evaluating true branch
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            int PatchForConditionalEnd;
            FuncCode.AddPcodeInstruction(Pcodes.epBranchUnconditional, out PatchForConditionalEnd, Conditional.LineNumber);
            FuncCode.AddPcodeOperandInteger(-1/*not known yet*/);

            StackDepth--;

            /* patch the conditional branch */
            FuncCode.ResolvePcodeBranch(PatchLocationForConditionalBranch, FuncCode.PcodeGetNextAddress());

            /* evaluate the false branch */
            if (Conditional.Alternate != null)
            {
                /* there is a real live alternate */
                CodeGenExpression(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth,
                    Conditional.Alternate);
                Debug.Assert(StackDepth <= MaxStackDepth);
            }
            else
            {
                /* there is no alternate, so push zero or nil */
                int unused;
                switch (GetExpressionsResultantType(Conditional.Consequent))
                {
                    default:
                        // CodeGenConditional:  bad type for 0
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                    case DataTypes.eInteger:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, Conditional.LineNumber);
                        FuncCode.AddPcodeOperandInteger(0);
                        break;
                    case DataTypes.eFloat:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateFloat, out unused, Conditional.LineNumber);
                        FuncCode.AddPcodeOperandFloat(0);
                        break;
                    case DataTypes.eDouble:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateDouble, out unused, Conditional.LineNumber);
                        FuncCode.AddPcodeOperandDouble(0);
                        break;
                    case DataTypes.eArrayOfBoolean:
                    case DataTypes.eArrayOfByte:
                    case DataTypes.eArrayOfInteger:
                    case DataTypes.eArrayOfFloat:
                    case DataTypes.eArrayOfDouble:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArray, out unused, Conditional.LineNumber);
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

#if true // TODO:experimental
        public static void ILGenConditional(
            CILObject cilObject,
            ILGenContext context,
            ASTCondRec Conditional)
        {
            /* evaluate the condition */
            ILGenExpression(
                cilObject,
                context,
                Conditional.Conditional);

            Label endLabel = context.ilGenerator.DefineLabel();
            Label altLabel = context.ilGenerator.DefineLabel();

            context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
            context.ilGenerator.Emit(OpCodes.Beq, altLabel);

            /* evaluate the true branch */
            ILGenExpression(
                cilObject,
                context,
                Conditional.Consequent);
            context.ilGenerator.Emit(OpCodes.Br, endLabel);

            context.ilGenerator.MarkLabel(altLabel);

            /* evaluate the false branch */
            if (Conditional.Alternate != null)
            {
                /* there is a real live alternate */
                ILGenExpression(
                    cilObject,
                    context,
                    Conditional.Alternate);
            }
            else
            {
                /* there is no alternate, so push zero or nil */
                switch (GetExpressionsResultantType(Conditional.Consequent))
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
                    case DataTypes.eArrayOfInteger:
                    case DataTypes.eArrayOfFloat:
                    case DataTypes.eArrayOfDouble:
                        context.ilGenerator.Emit(OpCodes.Ldnull);
                        break;
                }
            }

            context.ilGenerator.MarkLabel(endLabel);
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstConditional(
            ASTCondRec Conditional,
            out bool DidSomething,
            out ASTExpressionRec ResultantExpr,
            out DataTypes ResultType)
        {
            bool WeDidSomething = false;
            bool TheyDidSomething;

            ResultantExpr = null;

            ResultType = GetExpressionsResultantType(Conditional.Consequent);

            FoldConstExpression(Conditional.Conditional, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            FoldConstExpression(Conditional.Consequent, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            if (Conditional.Alternate != null)
            {
                FoldConstExpression(Conditional.Alternate, out TheyDidSomething);
                WeDidSomething = WeDidSomething || TheyDidSomething;
            }

            /* can we eliminate the conditional? */
            bool ConditionalWasRemoved = false;
            if ((WhatKindOfExpressionIsThis(Conditional.Conditional) == ExprTypes.eExprOperand)
                && (OperandWhatIsIt(GetOperandFromExpression(Conditional.Conditional)) == ASTOperandType.eASTOperandBooleanLiteral))
            {
                /* constant conditional -- reduce to one branch or the other */
                if (GetOperandBooleanLiteral(GetOperandFromExpression(Conditional.Conditional)))
                {
                    /* then clause taken */
                    ResultantExpr = Conditional.Consequent;
                    WeDidSomething = true;
                    ConditionalWasRemoved = true;
                }
                else
                {
                    /* else clause taken */
                    if (Conditional.Alternate != null)
                    {
                        ResultantExpr = Conditional.Alternate;
                        WeDidSomething = true;
                        ConditionalWasRemoved = true;
                    }
                    else
                    {

                        /* no alternate -- synthesize the zero value */
                        ASTOperandRec NewOperand;
                        switch (GetExpressionsResultantType(Conditional.Consequent))
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
                                NewOperand = NewBooleanLiteral(
                                    false,
                                    Conditional.LineNumber);
                                break;
                            case DataTypes.eInteger:
                                NewOperand = NewIntegerLiteral(
                                    0,
                                    Conditional.LineNumber);
                                break;
                            case DataTypes.eFloat:
                                NewOperand = NewSingleLiteral(
                                    0,
                                    Conditional.LineNumber);
                                break;
                            case DataTypes.eDouble:
                                NewOperand = NewDoubleLiteral(
                                    0,
                                    Conditional.LineNumber);
                                break;
                        }

                        if (NewOperand != null)
                        {
                            ResultantExpr = NewExprOperand(
                                NewOperand,
                                Conditional.LineNumber);
                            WeDidSomething = true;
                            ConditionalWasRemoved = true;
                        }
                    }
                }
            }
            if (!ConditionalWasRemoved)
            {
                /* couldn't eliminate conditional */
                ResultantExpr = NewExprConditional(
                    Conditional,
                    Conditional.LineNumber);
            }

            DidSomething = WeDidSomething;
        }




        public class ASTErrorFormRec
        {
            public ASTExpressionRec ResumeCondition;
            public string MessageString;
            public int LineNumber;
        }

        /* create a new AST error form */
        public static ASTErrorFormRec NewErrorForm(
            ASTExpressionRec Expression,
            string String,
            int LineNumber)
        {
            ASTErrorFormRec ErrorForm = new ASTErrorFormRec();

            ErrorForm.ResumeCondition = Expression;
            ErrorForm.MessageString = String;
            ErrorForm.LineNumber = LineNumber;

            return ErrorForm;
        }

        /* type check the error message node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckErrorForm(
            out DataTypes ResultingDataType,
            ASTErrorFormRec ErrorMessage,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            DataTypes ResumeConditionType;
            Error = TypeCheckExpression(out ResumeConditionType, ErrorMessage.ResumeCondition, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            if (ResumeConditionType != DataTypes.eBoolean)
            {
                ErrorLineNumber = ErrorMessage.LineNumber;
                return CompileErrors.eCompileErrorNeedsBooleanArg;
            }

            ResultingDataType = DataTypes.eBoolean;
            return CompileErrors.eCompileNoError;
        }

        public static void CodeGenExternCall(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExpressionRec OuterExpression,
            string ExternFuncName,
            ASTExpressionRec[] Arguments,
            DataTypes[] ArgumentsTypes,
            DataTypes ReturnType)
        {
            int unused;

            int StackDepth = StackDepthParam;

            // eval arguments
            int startStackDepth = StackDepth;
            for (int i = 0; i < Arguments.Length; i++)
            {
                Debug.Assert(ArgumentsTypes[i] == GetExpressionsResultantType(Arguments[i]));
                CodeGenExpression(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth,
                    Arguments[i]);
                Debug.Assert(StackDepth <= MaxStackDepth);
            }
            Debug.Assert(startStackDepth + Arguments.Length == StackDepth);

            /* function call */
            FuncCode.AddPcodeInstruction(
                Pcodes.epFuncCallExternal,
                out unused,
                OuterExpression.LineNumber);
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

#if true // TODO:experimental
        public static void ILGenExternCall(
            CILObject cilObject,
            ILGenContext context,
            ASTExpressionRec OuterExpression,
            string ExternFuncName,
            ASTExpressionRec[] Arguments,
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
                Debug.Assert(ArgumentsTypes[i] == GetExpressionsResultantType(Arguments[i]));
                context.ilGenerator.Emit(OpCodes.Dup); // arrayref
                context.ilGenerator.Emit(OpCodes.Ldc_I4, i);
                ILGenExpression(
                    cilObject,
                    context,
                    Arguments[i]);
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
#endif

        public static void CodeGenErrorForm(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExpressionRec OuterExpression,
            ASTErrorFormRec ErrorForm)
        {
            ASTExpressionRec messageStringExpression = NewExprOperand(
                NewStringLiteral(
                    ErrorForm.MessageString,
                    ErrorForm.LineNumber),
                ErrorForm.LineNumber);
            messageStringExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            CodeGenExternCall(
                FuncCode,
                ref StackDepthParam,
                ref MaxStackDepth,
                OuterExpression,
                "ErrorTrap",
                new ASTExpressionRec[2]
                {
                    messageStringExpression,
                    ErrorForm.ResumeCondition,
                },
                new DataTypes[2]
                {
                    DataTypes.eArrayOfByte/*0: name*/,
                    DataTypes.eBoolean/*1: resume condition*/,
                },
                DataTypes.eBoolean);
        }

#if true // TODO:experimental
        public static void ILGenErrorForm(
            CILObject cilObject,
            ILGenContext context,
            ASTExpressionRec OuterExpression,
            ASTErrorFormRec ErrorForm)
        {
            ASTExpressionRec messageStringExpression = NewExprOperand(
                NewStringLiteral(
                    ErrorForm.MessageString,
                    ErrorForm.LineNumber),
                ErrorForm.LineNumber);
            messageStringExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            ILGenExternCall(
                cilObject,
                context,
                OuterExpression,
                "ErrorTrap",
                new ASTExpressionRec[2]
                {
                    messageStringExpression,
                    ErrorForm.ResumeCondition,
                },
                new DataTypes[2]
                {
                    DataTypes.eArrayOfByte/*0: name*/,
                    DataTypes.eBoolean/*1: resume condition*/,
                },
                DataTypes.eBoolean);
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstErrorForm(
            ASTErrorFormRec ErrorMessage,
            out bool DidSomething)
        {
            FoldConstExpression(
                ErrorMessage.ResumeCondition,
                out DidSomething);
        }




        public enum ExprTypes
        {
            eInvalid,

            eExprArrayDeclaration,
            eExprAssignment,
            eExprBinaryOperator,
            eExprConditional,
            eExprExpressionList,
            eExprFunctionCall,
            eExprLoop,
            eExprOperand,
            eExprUnaryOperator,
            eExprVariableDeclaration,
            eExprErrorForm,
            eExprWaveGetter,
            eExprPrintString,
            eExprPrintExpr,
            eExprFilter,
            eExprSampleLoader,
            eExprForLoop,
        }

        public class ASTExpressionRec
        {
            public ExprTypes ElementType;
            public DataTypes WhatIsTheExpressionType;
            public int LineNumber;
            public object u;
        };

        /* construct a generic expression around an array declaration */
        public static ASTExpressionRec NewExprArrayDecl(
            ASTArrayDeclRec TheArrayDeclaration,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprArrayDeclaration;
            Expr.u = TheArrayDeclaration;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around an assignment statement */
        public static ASTExpressionRec NewExprAssignment(
            ASTAssignRec TheAssignment,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprAssignment;
            Expr.u = TheAssignment;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around a binary operator */
        public static ASTExpressionRec NewExprBinaryOperator(
            ASTBinaryOpRec TheBinaryOperator,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprBinaryOperator;
            Expr.u = TheBinaryOperator;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around a conditional. */
        public static ASTExpressionRec NewExprConditional(
            ASTCondRec TheConditional,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprConditional;
            Expr.u = TheConditional;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around a list of expressions. */
        public static ASTExpressionRec NewExprSequence(
            ASTExprListRec TheExpressionList,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprExpressionList;
            Expr.u = TheExpressionList;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around a function call */
        public static ASTExpressionRec NewExprFunctionCall(
            ASTFuncCallRec TheFunctionCall,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprFunctionCall;
            Expr.u = TheFunctionCall;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around a loop */
        public static ASTExpressionRec NewExprLoop(
            ASTLoopRec TheLoop,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprLoop;
            Expr.u = TheLoop;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around an operand */
        public static ASTExpressionRec NewExprOperand(
            ASTOperandRec TheOperand,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprOperand;
            Expr.u = TheOperand;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around a unary operator */
        public static ASTExpressionRec NewExprUnaryOperator(
            ASTUnaryOpRec TheUnaryOperator,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprUnaryOperator;
            Expr.u = TheUnaryOperator;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around a variable declaration */
        public static ASTExpressionRec NewExprVariableDeclaration(
            ASTVarDeclRec TheVariableDecl,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprVariableDeclaration;
            Expr.u = TheVariableDecl;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around an error form */
        public static ASTExpressionRec NewExprErrorForm(
            ASTErrorFormRec TheErrorForm,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprErrorForm;
            Expr.u = TheErrorForm;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around a wave getter */
        public static ASTExpressionRec NewExprWaveGetter(
            ASTWaveGetterRec TheWaveGetter,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprWaveGetter;
            Expr.u = TheWaveGetter;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around a sample loader */
        public static ASTExpressionRec NewExprSampleLoader(
            ASTSampleLoaderRec TheSampleLoader,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprSampleLoader;
            Expr.u = TheSampleLoader;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around an error form */
        public static ASTExpressionRec NewExprPrintString(
            ASTPrintStringRec ThePrintString,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprPrintString;
            Expr.u = ThePrintString;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around an error form */
        public static ASTExpressionRec NewExprPrintExpr(
            ASTPrintExprRec ThePrintExpr,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprPrintExpr;
            Expr.u = ThePrintExpr;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* construct a generic expression around filter form */
        public static ASTExpressionRec NewExprFilterExpr(
            ASTFilterRec TheFilterExpr,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprFilter;
            Expr.u = TheFilterExpr;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        public static ASTExpressionRec NewExprForLoop(
            ASTForLoop ForLoop,
            int TheLineNumber)
        {
            ASTExpressionRec Expr = new ASTExpressionRec();

            Expr.ElementType = ExprTypes.eExprForLoop;
            Expr.u = ForLoop;
            Expr.LineNumber = TheLineNumber;

            return Expr;
        }

        /* get operand from expression */
        public static ASTOperandRec GetOperandFromExpression(ASTExpressionRec Expr)
        {
            if ((Expr.ElementType != ExprTypes.eExprOperand) || !(Expr.u is ASTOperandRec))
            {
                // expression is not an operand
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (ASTOperandRec)Expr.u;
        }

        /* get sequence from expression */
        public static ASTExprListRec GetSequenceFromExpression(ASTExpressionRec Expr)
        {
            if ((Expr.ElementType != ExprTypes.eExprExpressionList) || !(Expr.u is ASTExprListRec))
            {
                // expression is not a sequence
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (ASTExprListRec)Expr.u;
        }

        /* get the operand from the generic expression */
        public static ASTOperandRec GetOperandOutOfExpression(ASTExpressionRec TheExpression)
        {
            if ((TheExpression.ElementType != ExprTypes.eExprOperand) || !(TheExpression.u is ASTOperandRec))
            {
                // expression isn't an operand
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (ASTOperandRec)TheExpression.u;
        }


        /* get the binary operator out of the generic expression */
        public static ASTBinaryOpRec GetBinaryOperatorOutOfExpression(ASTExpressionRec TheExpression)
        {
            if ((TheExpression.ElementType != ExprTypes.eExprBinaryOperator) || !(TheExpression.u is ASTBinaryOpRec))
            {
                // expression isn't an operand
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (ASTBinaryOpRec)TheExpression.u;
        }

        /* type check an expression.  returns eCompileNoError and the resulting value */
        /* type if it checks correctly. */
        public static CompileErrors TypeCheckExpression(
            out DataTypes ResultTypeOut,
            ASTExpressionRec TheExpression,
            out int ErrorLineNumber)
        {
            ErrorLineNumber = -1;
            ResultTypeOut = DataTypes.eInvalidDataType;

            CompileErrors ReturnValue;
            switch (TheExpression.ElementType)
            {
                default:
                    // TypeCheckExpression:  unknown AST node type
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case ExprTypes.eExprArrayDeclaration:
                    Debug.Assert(TheExpression.u is ASTArrayDeclRec);
                    ReturnValue = TypeCheckArrayConstruction(
                        out ResultTypeOut,
                        (ASTArrayDeclRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprAssignment:
                    Debug.Assert(TheExpression.u is ASTAssignRec);
                    ReturnValue = TypeCheckAssignment(
                        out ResultTypeOut,
                        (ASTAssignRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprBinaryOperator:
                    Debug.Assert(TheExpression.u is ASTBinaryOpRec);
                    ReturnValue = TypeCheckBinaryOperator(
                        out ResultTypeOut,
                        (ASTBinaryOpRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprConditional:
                    Debug.Assert(TheExpression.u is ASTCondRec);
                    ReturnValue = TypeCheckConditional(
                        out ResultTypeOut,
                        (ASTCondRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprExpressionList:
                    /* this needs to be done specially */
                    if (TheExpression.u == null)
                    {
                        ErrorLineNumber = TheExpression.LineNumber;
                        ReturnValue = CompileErrors.eCompileVoidExpressionIsNotAllowed;
                    }
                    else
                    {
                        Debug.Assert(TheExpression.u is ASTExprListRec);
                        ReturnValue = TypeCheckExprList(
                            out ResultTypeOut,
                            (ASTExprListRec)TheExpression.u,
                            out ErrorLineNumber);
                    }
                    break;
                case ExprTypes.eExprFunctionCall:
                    Debug.Assert(TheExpression.u is ASTFuncCallRec);
                    ReturnValue = TypeCheckFunctionCall(
                        out ResultTypeOut,
                        (ASTFuncCallRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprLoop:
                    Debug.Assert(TheExpression.u is ASTLoopRec);
                    ReturnValue = TypeCheckLoop(
                        out ResultTypeOut,
                        (ASTLoopRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprOperand:
                    Debug.Assert(TheExpression.u is ASTOperandRec);
                    ReturnValue = TypeCheckOperand(
                        out ResultTypeOut,
                        (ASTOperandRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprUnaryOperator:
                    Debug.Assert(TheExpression.u is ASTUnaryOpRec);
                    ReturnValue = TypeCheckUnaryOperator(
                        out ResultTypeOut,
                        (ASTUnaryOpRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprVariableDeclaration:
                    Debug.Assert(TheExpression.u is ASTVarDeclRec);
                    ReturnValue = TypeCheckVariableDeclaration(
                        out ResultTypeOut,
                        (ASTVarDeclRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprErrorForm:
                    Debug.Assert(TheExpression.u is ASTErrorFormRec);
                    ReturnValue = TypeCheckErrorForm(
                        out ResultTypeOut,
                        (ASTErrorFormRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprWaveGetter:
                    Debug.Assert(TheExpression.u is ASTWaveGetterRec);
                    ReturnValue = TypeCheckWaveGetter(
                        out ResultTypeOut,
                        (ASTWaveGetterRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprPrintString:
                    Debug.Assert(TheExpression.u is ASTPrintStringRec);
                    ReturnValue = TypeCheckPrintString(
                        out ResultTypeOut,
                        (ASTPrintStringRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprPrintExpr:
                    Debug.Assert(TheExpression.u is ASTPrintExprRec);
                    ReturnValue = TypeCheckPrintExpr(
                        out ResultTypeOut,
                        (ASTPrintExprRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprFilter:
                    Debug.Assert(TheExpression.u is ASTFilterRec);
                    ReturnValue = TypeCheckASTFilter(
                        out ResultTypeOut,
                        (ASTFilterRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprSampleLoader:
                    Debug.Assert(TheExpression.u is ASTSampleLoaderRec);
                    ReturnValue = TypeCheckSampleLoader(
                        out ResultTypeOut,
                        (ASTSampleLoaderRec)TheExpression.u,
                        out ErrorLineNumber);
                    break;
                case ExprTypes.eExprForLoop:
                    Debug.Assert(TheExpression.u is ASTForLoop);
                    ReturnValue = TypeCheckForLoop(
                        out ResultTypeOut,
                        (ASTForLoop)TheExpression.u,
                        out ErrorLineNumber);
                    break;
            }

            TheExpression.WhatIsTheExpressionType = ResultTypeOut;
            return ReturnValue;
        }

        /* get a symbol table entry out of an expression.  this is used for getting */
        /* function generation stuff. */
        public static CompileErrors ExpressionGetFunctionCallSymbol(
            out SymbolRec SymbolOut,
            ASTExpressionRec TheExpression)
        {
            SymbolOut = null;

            if (TheExpression.ElementType != ExprTypes.eExprOperand)
            {
                return CompileErrors.eCompileFunctionIdentifierRequired;
            }
            if (!IsOperandASymbol((ASTOperandRec)TheExpression.u))
            {
                return CompileErrors.eCompileFunctionIdentifierRequired;
            }
            SymbolRec FunctionThing = GetSymbolFromOperand((ASTOperandRec)TheExpression.u);
            if (FunctionThing.WhatIsThisSymbol() != SymbolType.eSymbolFunction)
            {
                return CompileErrors.eCompileFunctionIdentifierRequired;
            }
            SymbolOut = FunctionThing;
            return CompileErrors.eCompileNoError;
        }

        /* find out if the expression is a valid lvalue */
        public static bool IsExpressionValidLValue(ASTExpressionRec TheExpression)
        {
            /* to be a valid lvalue, the expression must be one of */
            /*  - variable */
            /*  - array subscription operation */
            switch (TheExpression.ElementType)
            {
                default:
                    return false;

                case ExprTypes.eExprBinaryOperator:
                    Debug.Assert(TheExpression.u is ASTBinaryOpRec);
                    return (BinaryOperatorWhichOne((ASTBinaryOpRec)TheExpression.u)
                        == BinaryOpType.eBinaryArraySubscripting);

                case ExprTypes.eExprOperand:
                    Debug.Assert(TheExpression.u is ASTOperandRec);
                    if (IsOperandASymbol((ASTOperandRec)TheExpression.u))
                    {
                        SymbolRec TheOperandThing = GetSymbolFromOperand((ASTOperandRec)TheExpression.u);
                        return TheOperandThing.WhatIsThisSymbol() == SymbolType.eSymbolVariable;
                    }
                    else
                    {
                        return false;
                    }
            }
        }

        public static ExprTypes WhatKindOfExpressionIsThis(ASTExpressionRec TheExpression)
        {
            return TheExpression.ElementType;
        }

        /* get the type of value that is returned by this expression */
        // (cached)
        public static DataTypes GetExpressionsResultantType(ASTExpressionRec TheExpression)
        {
            return TheExpression.WhatIsTheExpressionType;
        }

        /* generate code for an expression.  returns True if successful, or False if it fails. */
        public static void CodeGenExpression(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExpressionRec Expression)
        {
            int StackDepth = StackDepthParam;

            switch (Expression.ElementType)
            {
                default:
                    // CodeGenExpression:  unknown expression type
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case ExprTypes.eExprArrayDeclaration:
                    Debug.Assert(Expression.u is ASTArrayDeclRec);
                    CodeGenArrayConstruction(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTArrayDeclRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprAssignment:
                    Debug.Assert(Expression.u is ASTAssignRec);
                    CodeGenAssignment(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTAssignRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprBinaryOperator:
                    Debug.Assert(Expression.u is ASTBinaryOpRec);
                    CodeGenBinaryOperator(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTBinaryOpRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprConditional:
                    Debug.Assert(Expression.u is ASTCondRec);
                    CodeGenConditional(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTCondRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprExpressionList:
                    Debug.Assert(Expression.u is ASTExprListRec);
                    CodeGenExpressionListSequence(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTExprListRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprFunctionCall:
                    Debug.Assert(Expression.u is ASTFuncCallRec);
                    CodeGenFunctionCall(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTFuncCallRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprLoop:
                    Debug.Assert(Expression.u is ASTLoopRec);
                    CodeGenLoop(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTLoopRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprOperand:
                    Debug.Assert(Expression.u is ASTOperandRec);
                    CodeGenOperand(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTOperandRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprUnaryOperator:
                    Debug.Assert(Expression.u is ASTUnaryOpRec);
                    CodeGenUnaryOperator(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTUnaryOpRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprVariableDeclaration:
                    Debug.Assert(Expression.u is ASTVarDeclRec);
                    CodeGenVarDecl(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTVarDeclRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprErrorForm:
                    Debug.Assert(Expression.u is ASTErrorFormRec);
                    CodeGenErrorForm(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        Expression,
                        (ASTErrorFormRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprWaveGetter:
                    Debug.Assert(Expression.u is ASTWaveGetterRec);
                    CodeGenWaveGetter(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        Expression,
                        (ASTWaveGetterRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprPrintString:
                    Debug.Assert(Expression.u is ASTPrintStringRec);
                    CodeGenPrintString(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        Expression,
                        (ASTPrintStringRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprPrintExpr:
                    Debug.Assert(Expression.u is ASTPrintExprRec);
                    CodeGenPrintExpr(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        Expression,
                        (ASTPrintExprRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprFilter:
                    Debug.Assert(Expression.u is ASTFilterRec);
                    CodeGenASTFilter(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        Expression,
                        (ASTFilterRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprSampleLoader:
                    Debug.Assert(Expression.u is ASTSampleLoaderRec);
                    CodeGenSampleLoader(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        Expression,
                        (ASTSampleLoaderRec)Expression.u);
                    Debug.Assert(StackDepth <= MaxStackDepth);
                    break;
                case ExprTypes.eExprForLoop:
                    Debug.Assert(Expression.u is ASTForLoop);
                    CodeGenForLoop(
                        FuncCode,
                        ref StackDepth,
                        ref MaxStackDepth,
                        (ASTForLoop)Expression.u);
                    break;
            }
            if ((((Expression.ElementType != ExprTypes.eExprVariableDeclaration)
                    && (Expression.ElementType != ExprTypes.eExprArrayDeclaration))
                    && (StackDepth != StackDepthParam + 1))
                ||
                (((Expression.ElementType == ExprTypes.eExprVariableDeclaration)
                    || (Expression.ElementType == ExprTypes.eExprArrayDeclaration))
                    && (StackDepth != StackDepthParam + 2)))
            {
                // stack depth error
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            StackDepthParam = StackDepth;
        }

#if true // TODO:experimental
        public static void ILGenExpression(
            CILObject cilObject,
            ILGenContext context,
            ASTExpressionRec Expression)
        {
            switch (Expression.ElementType)
            {
                default:
                    // CodeGenExpression:  unknown expression type
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case ExprTypes.eExprArrayDeclaration:
                    Debug.Assert(Expression.u is ASTArrayDeclRec);
                    ILGenArrayConstruction(
                        cilObject,
                        context,
                        (ASTArrayDeclRec)Expression.u);
                    break;
                case ExprTypes.eExprAssignment:
                    Debug.Assert(Expression.u is ASTAssignRec);
                    ILGenAssignment(
                        cilObject,
                        context,
                        (ASTAssignRec)Expression.u);
                    break;
                case ExprTypes.eExprBinaryOperator:
                    Debug.Assert(Expression.u is ASTBinaryOpRec);
                    ILGenBinaryOperator(
                        cilObject,
                        context,
                        (ASTBinaryOpRec)Expression.u);
                    break;
                case ExprTypes.eExprConditional:
                    Debug.Assert(Expression.u is ASTCondRec);
                    ILGenConditional(
                        cilObject,
                        context,
                        (ASTCondRec)Expression.u);
                    break;
                case ExprTypes.eExprExpressionList:
                    Debug.Assert(Expression.u is ASTExprListRec);
                    ILGenExpressionListSequence(
                        cilObject,
                        context,
                        (ASTExprListRec)Expression.u);
                    break;
                case ExprTypes.eExprFunctionCall:
                    Debug.Assert(Expression.u is ASTFuncCallRec);
                    ILGenFunctionCall(
                        cilObject,
                        context,
                        (ASTFuncCallRec)Expression.u);
                    break;
                case ExprTypes.eExprLoop:
                    Debug.Assert(Expression.u is ASTLoopRec);
                    ILGenLoop(
                        cilObject,
                        context,
                        (ASTLoopRec)Expression.u);
                    break;
                case ExprTypes.eExprOperand:
                    Debug.Assert(Expression.u is ASTOperandRec);
                    ILGenOperand(
                        cilObject,
                        context,
                        (ASTOperandRec)Expression.u);
                    break;
                case ExprTypes.eExprUnaryOperator:
                    Debug.Assert(Expression.u is ASTUnaryOpRec);
                    ILGenUnaryOperator(
                        cilObject,
                        context,
                        (ASTUnaryOpRec)Expression.u);
                    break;
                case ExprTypes.eExprVariableDeclaration:
                    Debug.Assert(Expression.u is ASTVarDeclRec);
                    ILGenVarDecl(
                        cilObject,
                        context,
                        (ASTVarDeclRec)Expression.u);
                    break;
                case ExprTypes.eExprErrorForm:
                    Debug.Assert(Expression.u is ASTErrorFormRec);
                    ILGenErrorForm(
                        cilObject,
                        context,
                        Expression,
                        (ASTErrorFormRec)Expression.u);
                    break;
                case ExprTypes.eExprWaveGetter:
                    Debug.Assert(Expression.u is ASTWaveGetterRec);
                    ILGenWaveGetter(
                        cilObject,
                        context,
                        Expression,
                        (ASTWaveGetterRec)Expression.u);
                    break;
                case ExprTypes.eExprPrintString:
                    Debug.Assert(Expression.u is ASTPrintStringRec);
                    ILGenPrintString(
                        cilObject,
                        context,
                        Expression,
                        (ASTPrintStringRec)Expression.u);
                    break;
                case ExprTypes.eExprPrintExpr:
                    Debug.Assert(Expression.u is ASTPrintExprRec);
                    ILGenPrintExpr(
                        cilObject,
                        context,
                        Expression,
                        (ASTPrintExprRec)Expression.u);
                    break;
                case ExprTypes.eExprFilter:
                    Debug.Assert(Expression.u is ASTFilterRec);
                    ILGenASTFilter(
                        cilObject,
                        context,
                        Expression,
                        (ASTFilterRec)Expression.u);
                    break;
                case ExprTypes.eExprSampleLoader:
                    Debug.Assert(Expression.u is ASTSampleLoaderRec);
                    ILGenSampleLoader(
                        cilObject,
                        context,
                        Expression,
                        (ASTSampleLoaderRec)Expression.u);
                    break;
                case ExprTypes.eExprForLoop:
                    Debug.Assert(Expression.u is ASTForLoop);
                    ILGenForLoop(
                        cilObject,
                        context,
                        (ASTForLoop)Expression.u);
                    break;
            }
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstExpression(
            ASTExpressionRec Expr,
            out bool DidSomething)
        {
            bool OneDidSomething = false;

            ASTExpressionRec Result = null;
            DataTypes ResultType = DataTypes.eInvalidDataType;
            switch (Expr.ElementType)
            {
                default:
                    // FoldConstExpression: unknown expression type
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case ExprTypes.eExprSampleLoader:
                    Debug.Assert(Expr.u is ASTSampleLoaderRec);
                    FoldConstSampleLoader((ASTSampleLoaderRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprErrorForm:
                    Debug.Assert(Expr.u is ASTErrorFormRec);
                    FoldConstErrorForm((ASTErrorFormRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprWaveGetter:
                    Debug.Assert(Expr.u is ASTWaveGetterRec);
                    FoldConstWaveGetter((ASTWaveGetterRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprPrintString:
                    Debug.Assert(Expr.u is ASTPrintStringRec);
                    FoldConstPrintString((ASTPrintStringRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprPrintExpr:
                    Debug.Assert(Expr.u is ASTPrintExprRec);
                    FoldConstPrintExpr((ASTPrintExprRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprFilter:
                    Debug.Assert(Expr.u is ASTFilterRec);
                    FoldConstASTFilter((ASTFilterRec)Expr.u, out OneDidSomething, out Result, out ResultType);
                    break;
                case ExprTypes.eExprArrayDeclaration:
                    Debug.Assert(Expr.u is ASTArrayDeclRec);
                    FoldConstArrayConstruction((ASTArrayDeclRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprAssignment:
                    Debug.Assert(Expr.u is ASTAssignRec);
                    FoldConstAssignment((ASTAssignRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprBinaryOperator:
                    Debug.Assert(Expr.u is ASTBinaryOpRec);
                    FoldConstBinaryOperator((ASTBinaryOpRec)Expr.u, Expr.WhatIsTheExpressionType, out OneDidSomething, out Result, out ResultType);
                    break;
                case ExprTypes.eExprConditional:
                    Debug.Assert(Expr.u is ASTCondRec);
                    FoldConstConditional((ASTCondRec)Expr.u, out OneDidSomething, out Result, out ResultType);
                    break;
                case ExprTypes.eExprFunctionCall:
                    Debug.Assert(Expr.u is ASTFuncCallRec);
                    FoldConstFunctionCall((ASTFuncCallRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprLoop:
                    Debug.Assert(Expr.u is ASTLoopRec);
                    FoldConstLoop((ASTLoopRec)Expr.u, out OneDidSomething, out Result, out ResultType);
                    break;
                case ExprTypes.eExprOperand:
                    Debug.Assert(Expr.u is ASTOperandRec);
                    FoldConstOperand((ASTOperandRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprVariableDeclaration:
                    Debug.Assert(Expr.u is ASTVarDeclRec);
                    FoldConstVarDecl((ASTVarDeclRec)Expr.u, out OneDidSomething);
                    break;
                case ExprTypes.eExprExpressionList:
                    Debug.Assert(Expr.u is ASTExprListRec);
                    FoldConstExpressionList((ASTExprListRec)Expr.u, out OneDidSomething, out Result, out ResultType, false/*not func arg list */);
                    /* Result is NIL, so we keep the old expression */
                    break;
                case ExprTypes.eExprUnaryOperator:
                    Debug.Assert(Expr.u is ASTUnaryOpRec);
                    FoldConstUnaryOperator((ASTUnaryOpRec)Expr.u, out OneDidSomething, out Result, out ResultType);
                    break;
                case ExprTypes.eExprForLoop:
                    Debug.Assert(Expr.u is ASTForLoop);
                    FoldConstForLoop((ASTForLoop)Expr.u, out OneDidSomething);
                    break;
            }

            /* absorb the result into us */
            if (Result != null)
            {
                if (ResultType != Expr.WhatIsTheExpressionType)
                {
                    // optimizer should not change types
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                Expr.ElementType = Result.ElementType;
                Expr.u = Result.u;
            }

            DidSomething = OneDidSomething;
        }




        public class ASTExprListRec
        {
            public ASTExpressionRec First; // lisp-style linked list
            public ASTExprListRec Rest;
            public int LineNumber;
        }

        /* cons an AST expression onto a list */
        public static ASTExprListRec ASTExprListCons(
            ASTExpressionRec First,
            ASTExprListRec Rest,
            int LineNumber)
        {
            ASTExprListRec NewNode = new ASTExprListRec();

            NewNode.First = First;
            NewNode.Rest = Rest;
            NewNode.LineNumber = LineNumber;

            return NewNode;
        }

        /* type check a list of expressions.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckExprList(
            out DataTypes ResultingDataType,
            ASTExprListRec ExpressionList,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

            /* the result type is filled in now */
            Error = TypeCheckExpression(
                out ResultingDataType,
                ExpressionList.First,
                out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* if there is another one, then do it */
            if (ExpressionList.Rest != null)
            {
                return TypeCheckExprList(out ResultingDataType, ExpressionList.Rest, out ErrorLineNumber);
            }
            else
            {
                return CompileErrors.eCompileNoError;
            }
        }

        /* get the first expression */
        public static ASTExpressionRec ExprListGetFirstExpr(ASTExprListRec ExpressionList)
        {
            return ExpressionList.First;
        }

        /* get the tail expression list */
        public static ASTExprListRec ExprListGetRestList(ASTExprListRec ExpressionList)
        {
            return ExpressionList.Rest;
        }

        /* install a new first in the list */
        public static void ExprListPutNewFirst(
            ASTExprListRec ExpressionList,
            ASTExpressionRec NewFirst)
        {
            ExpressionList.First = NewFirst;
        }

        /* this is a helper function for generating code */
        private static void CodeGenSequenceHelper(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExprListRec ExpressionList)
        {
            int StackDepth = StackDepthParam;

            /* generate code for the first expression */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                ExpressionList.First);
            Debug.Assert(StackDepth <= MaxStackDepth);

            if (ExpressionList.Rest != null)
            {
                /* if there is another expression, then pop the value we just */
                /* calcuated & evaluate the next one */
                int unused;
                FuncCode.AddPcodeInstruction(Pcodes.epStackPop, out unused, ExpressionList.LineNumber);
                StackDepth--;
                CodeGenSequenceHelper(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth,
                    ExpressionList.Rest);
                Debug.Assert(StackDepth <= MaxStackDepth);
            }
            /* else no next one, so just keep it */

            StackDepthParam = StackDepth;
        }

        /* generate code for an expression list that is a series of sequential expressions. */
        /* returns True if successful, or False if it fails. */
        public static void CodeGenExpressionListSequence(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExprListRec ExpressionList)
        {
            int StackDepth = StackDepthParam;

            /* generate code for all of the expressions */
            CodeGenSequenceHelper(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                ExpressionList);
            Debug.Assert(StackDepth <= MaxStackDepth);

            /* if there are any more than 1 additional value on the stack, then we */
            /* must pop all the other values off, since they are local variables */
            if (StackDepth - StackDepthParam > 1)
            {
                int unused;
                FuncCode.AddPcodeInstruction(Pcodes.epStackDeallocateUnder, out unused, ExpressionList.LineNumber);
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

#if true // TODO:experimental
        private static void ILGenSequenceHelper(
            CILObject cilObject,
            ILGenContext context,
            ASTExprListRec ExpressionList)
        {
            /* generate code for the first expression */
            ILGenExpression(
                cilObject,
                context,
                ExpressionList.First);

            if (ExpressionList.Rest != null)
            {
                /* if there is another expression, then pop the value we just */
                /* calcuated & evaluate the next one */
                context.ilGenerator.Emit(OpCodes.Pop);
                ILGenSequenceHelper(
                    cilObject,
                    context,
                    ExpressionList.Rest);
            }
            /* else no next one, so just keep it */
        }

        public static void ILGenExpressionListSequence(
            CILObject cilObject,
            ILGenContext context,
            ASTExprListRec ExpressionList)
        {
            ILGenSequenceHelper(
                cilObject,
                context,
                ExpressionList);
        }
#endif

        /* generate code for an argument list -- all args stay on the stack. */
        /* returns True if successful, or False if it fails. */
        public static void CodeGenExpressionListArguments(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExprListRec ExpressionList)
        {
            /* see if there is even any code to be generated */
            if (ExpressionList == null)
            {
                /* nope */
                return;
            }

            int StackDepth = StackDepthParam;

            /* generate code for the first expression */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                ExpressionList.First);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 1)
            {
                // stack messed up
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* if there's another argument, then do it too */
            if (ExpressionList.Rest != null)
            {
                CodeGenExpressionListArguments(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth,
                    ExpressionList.Rest);
                Debug.Assert(StackDepth <= MaxStackDepth);
            }

            StackDepthParam = StackDepth;
        }

#if true // TODO:experimental
        public static void ILGenExpressionListArguments(
            CILObject cilObject,
            ILGenContext context,
            ASTExprListRec ExpressionList)
        {
            /* see if there is even any code to be generated */
            if (ExpressionList == null)
            {
                /* nope */
                return;
            }

            /* generate code for the first expression */
            ILGenExpression(
                cilObject,
                context,
                ExpressionList.First);

            /* if there's another argument, then do it too */
            if (ExpressionList.Rest != null)
            {
                ILGenExpressionListArguments(
                    cilObject,
                    context,
                    ExpressionList.Rest);
            }
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstExpressionList(
            ASTExprListRec ExpressionList,
            out bool DidSomething,
            out ASTExpressionRec ResultantExpr,
            out DataTypes ResultType,
            bool WeAreAFunctionArgumentList)
        {
            bool WeDidSomething = false;

            ResultType = DataTypes.eInvalidDataType;

            /* optimize each expression */
            ASTExprListRec ExprScan = ExpressionList;
            while (ExprScan != null)
            {
                bool TheyDidSomething;

                /* fold the member */
                FoldConstExpression(ExprScan.First, out TheyDidSomething);
                WeDidSomething = WeDidSomething || TheyDidSomething;

                /* return the result type for the last one. */
                ResultType = GetExpressionsResultantType(ExprScan.First);

                /* try the next */
                ExprScan = ExprScan.Rest;
            }

            /* if we're not an argument list, then we can discard anything with */
            /* no side effects */
            if (!WeAreAFunctionArgumentList)
            {
                ExprScan = ExpressionList;
                ASTExprListRec ExprScanTrail = null;
                while (ExprScan != null)
                {
                    /* must save the last one, so only do this for previous ones */
                    if ((ExprScan.Rest != null)
                        && (WhatKindOfExpressionIsThis(ExprScan.First) == ExprTypes.eExprOperand))
                    {
                        /* operand is the only one that has no side effect */
                        /* splice us out */
                        if (ExprScanTrail != null)
                        {
                            ExprScanTrail.Rest = ExprScan.Rest;
                        }
                        else
                        {
                            ExpressionList = ExprScan.Rest;
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
            if ((ExpressionList.Rest == null)
                && !WeAreAFunctionArgumentList
                && (WhatKindOfExpressionIsThis(ExpressionList.First) != ExprTypes.eExprArrayDeclaration)
                && (WhatKindOfExpressionIsThis(ExpressionList.First) != ExprTypes.eExprVariableDeclaration))
            {
                /* if we have one member and we're not an arg list, and we aren't something */
                /* that allocates local storage on the stack, then we are */
                /* no longer a list, so make it more obvious to those above us */
                ResultantExpr = ExpressionList.First;
                WeDidSomething = true;
            }
            else
            {
                /* we still need to be a list, so create one of those */
                ResultantExpr = NewExprSequence(
                    ExpressionList,
                    ExpressionList.LineNumber);
            }

            DidSomething = WeDidSomething;
        }




        /* types of filters we can have */
        public enum ASTFilterTypes
        {
            eInvalid,

            eASTButterworthBandpass,
            eASTFirstOrderLowpass,
            eASTSquare,
            eASTSquareRoot,
        }

        public class ASTFilterRec
        {
            /* type error information */
            public int LineNumber;

            /* type of filter */
            public ASTFilterTypes TypeTag;
            /* butterworthbandpass:  cutoff and bandwidth */
            /* firstorderlowpass:  cutoff */

            /* parameter expressions for all filters*/
            public ASTExpressionRec ArrayExpr;
            public ASTExpressionRec StartIndexExpr;
            public ASTExpressionRec EndIndexExpr;

            /* conditional parameters */
            public ASTExpressionRec SamplingRateExpr; /* NIL if not used */
            public ASTExpressionRec CutoffExpr; /* NIL if not used */
            public ASTExpressionRec BandwidthExpr; /* NIL if not used */
        }

        /* create new AST filter object, with specified type */
        public static ASTFilterRec NewASTFilterButterworthBandpass(
            ASTExpressionRec ArrayExpr,
            ASTExpressionRec StartIndexExpr,
            ASTExpressionRec EndIndexExpr,
            ASTExpressionRec SamplingRateExpr,
            ASTExpressionRec CutoffExpr,
            ASTExpressionRec BandwidthExpr,
            int LineNumber)
        {
            ASTFilterRec Filter = new ASTFilterRec();
            ASTUnaryOpRec UnaryTemp;

            Filter.TypeTag = ASTFilterTypes.eASTButterworthBandpass;

            Filter.ArrayExpr = ArrayExpr;
            Filter.StartIndexExpr = StartIndexExpr;
            Filter.EndIndexExpr = EndIndexExpr;

            UnaryTemp = NewUnaryOperator(UnaryOpType.eUnaryCastToDouble, SamplingRateExpr, LineNumber);
            Filter.SamplingRateExpr = NewExprUnaryOperator(UnaryTemp, LineNumber);

            UnaryTemp = NewUnaryOperator(UnaryOpType.eUnaryCastToDouble, CutoffExpr, LineNumber);
            Filter.CutoffExpr = NewExprUnaryOperator(UnaryTemp, LineNumber);

            UnaryTemp = NewUnaryOperator(UnaryOpType.eUnaryCastToDouble, BandwidthExpr, LineNumber);
            Filter.BandwidthExpr = NewExprUnaryOperator(UnaryTemp, LineNumber);

            Filter.LineNumber = LineNumber;

            return Filter;
        }

        /* create new AST filter object, with specified type */
        public static ASTFilterRec NewASTFilterFirstOrderLowpass(
            ASTExpressionRec ArrayExpr,
            ASTExpressionRec StartIndexExpr,
            ASTExpressionRec EndIndexExpr,
            ASTExpressionRec SamplingRateExpr,
            ASTExpressionRec CutoffExpr,
            int LineNumber)
        {
            ASTFilterRec Filter = new ASTFilterRec();
            ASTUnaryOpRec UnaryTemp;

            Filter.TypeTag = ASTFilterTypes.eASTFirstOrderLowpass;

            Filter.ArrayExpr = ArrayExpr;
            Filter.StartIndexExpr = StartIndexExpr;
            Filter.EndIndexExpr = EndIndexExpr;

            UnaryTemp = NewUnaryOperator(UnaryOpType.eUnaryCastToDouble, SamplingRateExpr, LineNumber);
            Filter.SamplingRateExpr = NewExprUnaryOperator(UnaryTemp, LineNumber);

            UnaryTemp = NewUnaryOperator(UnaryOpType.eUnaryCastToDouble, CutoffExpr, LineNumber);
            Filter.CutoffExpr = NewExprUnaryOperator(UnaryTemp, LineNumber);

            Filter.BandwidthExpr = null;
            Filter.LineNumber = LineNumber;

            return Filter;
        }

        /* create new AST filter object, with specified type */
        public static ASTFilterRec NewASTFilterSquare(
            ASTExpressionRec ArrayExpr,
            ASTExpressionRec StartIndexExpr,
            ASTExpressionRec EndIndexExpr,
            int LineNumber)
        {
            ASTFilterRec Filter = new ASTFilterRec();

            Filter.TypeTag = ASTFilterTypes.eASTSquare;

            Filter.ArrayExpr = ArrayExpr;
            Filter.StartIndexExpr = StartIndexExpr;
            Filter.EndIndexExpr = EndIndexExpr;
            Filter.SamplingRateExpr = null;
            Filter.CutoffExpr = null;
            Filter.BandwidthExpr = null;
            Filter.LineNumber = LineNumber;

            return Filter;
        }

        /* create new AST filter object, with specified type */
        public static ASTFilterRec NewASTFilterSquareRoot(
            ASTExpressionRec ArrayExpr,
            ASTExpressionRec StartIndexExpr,
            ASTExpressionRec EndIndexExpr,
            int LineNumber)
        {
            ASTFilterRec Filter = new ASTFilterRec();

            Filter.TypeTag = ASTFilterTypes.eASTSquareRoot;

            Filter.ArrayExpr = ArrayExpr;
            Filter.StartIndexExpr = StartIndexExpr;
            Filter.EndIndexExpr = EndIndexExpr;
            Filter.SamplingRateExpr = null;
            Filter.CutoffExpr = null;
            Filter.BandwidthExpr = null;
            Filter.LineNumber = LineNumber;

            return Filter;
        }

        /* type check the filter node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckASTFilter(
            out DataTypes ResultingDataType,
            ASTFilterRec Filter,
            out int ErrorLineNumber)
        {
            DataTypes ArgumentType;
            CompileErrors Error;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            /* check the argument types */

            Error = TypeCheckExpression(out ArgumentType, Filter.ArrayExpr, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            if (ArgumentType != DataTypes.eArrayOfFloat)
            {
                ErrorLineNumber = Filter.LineNumber;
                return CompileErrors.eCompileOperandMustBeArrayOfFloat;
            }

            Error = TypeCheckExpression(out ArgumentType, Filter.StartIndexExpr, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            if (ArgumentType != DataTypes.eInteger)
            {
                ErrorLineNumber = Filter.LineNumber;
                return CompileErrors.eCompileOperandMustBeInteger;
            }

            Error = TypeCheckExpression(out ArgumentType, Filter.EndIndexExpr, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            if (ArgumentType != DataTypes.eInteger)
            {
                ErrorLineNumber = Filter.LineNumber;
                return CompileErrors.eCompileOperandMustBeInteger;
            }

            if (Filter.SamplingRateExpr != null)
            {
                Error = TypeCheckExpression(out ArgumentType, Filter.SamplingRateExpr, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (ArgumentType != DataTypes.eDouble)
                {
                    ErrorLineNumber = Filter.LineNumber;
                    return CompileErrors.eCompileOperandMustBeDouble;
                }
            }

            if (Filter.CutoffExpr != null)
            {
                Error = TypeCheckExpression(out ArgumentType, Filter.CutoffExpr, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (ArgumentType != DataTypes.eDouble)
                {
                    ErrorLineNumber = Filter.LineNumber;
                    return CompileErrors.eCompileOperandMustBeDouble;
                }
            }

            if (Filter.BandwidthExpr != null)
            {
                Error = TypeCheckExpression(out ArgumentType, Filter.BandwidthExpr, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (ArgumentType != DataTypes.eDouble)
                {
                    ErrorLineNumber = Filter.LineNumber;
                    return CompileErrors.eCompileOperandMustBeDouble;
                }
            }

            ResultingDataType = DataTypes.eArrayOfFloat;

            return CompileErrors.eCompileNoError;
        }

        /* generate code for a filter.  returns True if successful, or False if it fails. */
        public static void CodeGenASTFilter(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExpressionRec OuterExpression,
            ASTFilterRec Filter)
        {
            string methodName;
            ASTExpressionRec[] args;
            DataTypes[] argsTypes;
            DataTypes returnType;
            switch (Filter.TypeTag)
            {
                default:
                    // unknown filter type
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case ASTFilterTypes.eASTButterworthBandpass:
                    methodName = "ButterworthBandpass";
                    args = new ASTExpressionRec[]
                    {
                        Filter.ArrayExpr,
                        Filter.StartIndexExpr,
                        Filter.EndIndexExpr, // sic
                        Filter.SamplingRateExpr,
                        Filter.CutoffExpr,
                        Filter.BandwidthExpr,
                    };
                    argsTypes = new DataTypes[]
                    {
                        DataTypes.eArrayOfFloat/*0: data*/,
                        DataTypes.eInteger/*1: start*/,
                        DataTypes.eInteger/*2: length*/,
                        DataTypes.eDouble/*3: samplingrate*/,
                        DataTypes.eDouble/*4: cutoff*/,
                        DataTypes.eDouble/*5: bandwidth*/,
                    };
                    returnType = DataTypes.eArrayOfFloat;
                    break;

                case ASTFilterTypes.eASTFirstOrderLowpass:
                    methodName = "FirstOrderLowpass";
                    args = new ASTExpressionRec[]
                    {
                        Filter.ArrayExpr,
                        Filter.StartIndexExpr,
                        Filter.EndIndexExpr, // sic
                        Filter.SamplingRateExpr,
                        Filter.CutoffExpr,
                    };
                    argsTypes = new DataTypes[]
                    {
                        DataTypes.eArrayOfFloat/*0: data*/,
                        DataTypes.eInteger/*1: start*/,
                        DataTypes.eInteger/*2: length*/,
                        DataTypes.eDouble/*3: samplingrate*/,
                        DataTypes.eDouble/*4: cutoff*/,
                    };
                    returnType = DataTypes.eArrayOfFloat;
                    break;

                case ASTFilterTypes.eASTSquare:
                    methodName = "VectorSquare";
                    args = new ASTExpressionRec[]
                    {
                        Filter.ArrayExpr,
                        Filter.StartIndexExpr,
                        Filter.EndIndexExpr, // sic
                    };
                    argsTypes = new DataTypes[]
                    {
                        DataTypes.eArrayOfFloat/*0: value*/,
                        DataTypes.eInteger/*1: start*/,
                        DataTypes.eInteger/*2: length*/,
                    };
                    returnType = DataTypes.eArrayOfFloat;
                    break;

                case ASTFilterTypes.eASTSquareRoot:
                    methodName = "VectorSquareRoot";
                    args = new ASTExpressionRec[]
                    {
                        Filter.ArrayExpr,
                        Filter.StartIndexExpr,
                        Filter.EndIndexExpr, // sic
                    };
                    argsTypes = new DataTypes[]
                    {
                        DataTypes.eArrayOfFloat/*0: value*/,
                        DataTypes.eInteger/*1: start*/,
                        DataTypes.eInteger/*2: end*/,
                    };
                    returnType = DataTypes.eArrayOfFloat;
                    break;
            }
            Debug.Assert(args.Length == argsTypes.Length);
#if DEBUG
            for (int i = 0; i < args.Length; i++)
            {
                Debug.Assert(args[i] != null);
                Debug.Assert(args[i].WhatIsTheExpressionType == argsTypes[i]);
            }
#endif

            CodeGenExternCall(
                FuncCode,
                ref StackDepthParam,
                ref MaxStackDepth,
                OuterExpression,
                methodName,
                args,
                argsTypes,
                returnType);
        }

#if true // TODO:experimental
        public static void ILGenASTFilter(
            CILObject cilObject,
            ILGenContext context,
            ASTExpressionRec OuterExpression,
            ASTFilterRec Filter)
        {
            string methodName;
            ASTExpressionRec[] args;
            DataTypes[] argsTypes;
            DataTypes returnType;
            switch (Filter.TypeTag)
            {
                default:
                    // unknown filter type
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case ASTFilterTypes.eASTButterworthBandpass:
                    methodName = "ButterworthBandpass";
                    args = new ASTExpressionRec[]
                    {
                        Filter.ArrayExpr,
                        Filter.StartIndexExpr,
                        Filter.EndIndexExpr, // sic
                        Filter.SamplingRateExpr,
                        Filter.CutoffExpr,
                        Filter.BandwidthExpr,
                    };
                    argsTypes = new DataTypes[]
                    {
                        DataTypes.eArrayOfFloat/*0: data*/,
                        DataTypes.eInteger/*1: start*/,
                        DataTypes.eInteger/*2: length*/,
                        DataTypes.eDouble/*3: samplingrate*/,
                        DataTypes.eDouble/*4: cutoff*/,
                        DataTypes.eDouble/*5: bandwidth*/,
                    };
                    returnType = DataTypes.eArrayOfFloat;
                    break;

                case ASTFilterTypes.eASTFirstOrderLowpass:
                    methodName = "FirstOrderLowpass";
                    args = new ASTExpressionRec[]
                    {
                        Filter.ArrayExpr,
                        Filter.StartIndexExpr,
                        Filter.EndIndexExpr, // sic
                        Filter.SamplingRateExpr,
                        Filter.CutoffExpr,
                    };
                    argsTypes = new DataTypes[]
                    {
                        DataTypes.eArrayOfFloat/*0: data*/,
                        DataTypes.eInteger/*1: start*/,
                        DataTypes.eInteger/*2: length*/,
                        DataTypes.eDouble/*3: samplingrate*/,
                        DataTypes.eDouble/*4: cutoff*/,
                    };
                    returnType = DataTypes.eArrayOfFloat;
                    break;

                case ASTFilterTypes.eASTSquare:
                    methodName = "VectorSquare";
                    args = new ASTExpressionRec[]
                    {
                        Filter.ArrayExpr,
                        Filter.StartIndexExpr,
                        Filter.EndIndexExpr, // sic
                    };
                    argsTypes = new DataTypes[]
                    {
                        DataTypes.eArrayOfFloat/*0: value*/,
                        DataTypes.eInteger/*1: start*/,
                        DataTypes.eInteger/*2: length*/,
                    };
                    returnType = DataTypes.eArrayOfFloat;
                    break;

                case ASTFilterTypes.eASTSquareRoot:
                    methodName = "VectorSquareRoot";
                    args = new ASTExpressionRec[]
                    {
                        Filter.ArrayExpr,
                        Filter.StartIndexExpr,
                        Filter.EndIndexExpr, // sic
                    };
                    argsTypes = new DataTypes[]
                    {
                        DataTypes.eArrayOfFloat/*0: value*/,
                        DataTypes.eInteger/*1: start*/,
                        DataTypes.eInteger/*2: end*/,
                    };
                    returnType = DataTypes.eArrayOfFloat;
                    break;
            }
            Debug.Assert(args.Length == argsTypes.Length);
#if DEBUG
            for (int i = 0; i < args.Length; i++)
            {
                Debug.Assert(args[i] != null);
                Debug.Assert(args[i].WhatIsTheExpressionType == argsTypes[i]);
            }
#endif

            ILGenExternCall(
                cilObject,
                context,
                OuterExpression,
                methodName,
                args,
                argsTypes,
                returnType);
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstASTFilter(
            ASTFilterRec Filter,
            out bool DidSomething,
            out ASTExpressionRec ResultantExpr,
                    out DataTypes ResultType)
        {
            bool WeDidSomething = false;
            bool TheyDidSomething;

            FoldConstExpression(Filter.ArrayExpr, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            FoldConstExpression(Filter.StartIndexExpr, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            FoldConstExpression(Filter.EndIndexExpr, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            if (Filter.SamplingRateExpr != null)
            {
                FoldConstExpression(Filter.SamplingRateExpr, out TheyDidSomething);
                WeDidSomething = WeDidSomething || TheyDidSomething;
            }

            if (Filter.CutoffExpr != null)
            {
                FoldConstExpression(Filter.CutoffExpr, out TheyDidSomething);
                WeDidSomething = WeDidSomething || TheyDidSomething;
            }

            if (Filter.BandwidthExpr != null)
            {
                FoldConstExpression(Filter.BandwidthExpr, out TheyDidSomething);
                WeDidSomething = WeDidSomething || TheyDidSomething;
            }

            ResultantExpr = NewExprFilterExpr(Filter, Filter.LineNumber);
            ResultType = DataTypes.eArrayOfFloat;

            DidSomething = WeDidSomething;
        }




        public class ASTFuncCallRec
        {
            public ASTExprListRec ArgumentList;
            public ASTExpressionRec FunctionGenerator;
            public int LineNumber;
        }

        /* create a new function call node.  the argument list can be NIL if there are */
        /* no arguments. */
        public static ASTFuncCallRec NewFunctionCall(
            ASTExprListRec ArgumentList,
            ASTExpressionRec FunctionGeneratorExpression,
            int LineNumber)
        {
            ASTFuncCallRec FuncCall = new ASTFuncCallRec();

            FuncCall.LineNumber = LineNumber;
            FuncCall.ArgumentList = ArgumentList;
            FuncCall.FunctionGenerator = FunctionGeneratorExpression;

            return FuncCall;
        }

        /* type check the function call node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckFunctionCall(
            out DataTypes ResultingDataType,
            ASTFuncCallRec FunctionCall,
            out int ErrorLineNumber)
        {
            CompileErrors Error;
            SymbolRec FunctionSymbol;
            SymbolListRec FunctionArgumentStepper;
            ASTExprListRec ExpressionListStepper;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            /* get the symbol for the function */
            Error = ExpressionGetFunctionCallSymbol(out FunctionSymbol, FunctionCall.FunctionGenerator);
            if (Error != CompileErrors.eCompileNoError)
            {
                ErrorLineNumber = FunctionCall.LineNumber;
                return Error;
            }

            FunctionArgumentStepper = FunctionSymbol.GetSymbolFunctionArgList();
            ExpressionListStepper = FunctionCall.ArgumentList;
            while ((FunctionArgumentStepper != null) && (ExpressionListStepper != null))
            {
                DataTypes FormalType = FunctionArgumentStepper.GetFirstFromSymbolList().GetSymbolVariableDataType();
                DataTypes ActualType;
                Error = TypeCheckExpression(
                    out ActualType,
                    ExprListGetFirstExpr(ExpressionListStepper),
                    out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (!CanRightBeMadeToMatchLeft(FormalType, ActualType))
                {
                    ErrorLineNumber = FunctionCall.LineNumber;
                    return CompileErrors.eCompileArgumentTypeConflict;
                }
                if (MustRightBePromotedToLeft(FormalType, ActualType))
                {
                    /* consequent must be promoted to be same as alternate */
                    ASTExpressionRec PromotedThing = PromoteTheExpression(
                        ActualType/*orig*/,
                        FormalType/*desired*/,
                        ExprListGetFirstExpr(ExpressionListStepper),
                        FunctionCall.LineNumber);
                    ExprListPutNewFirst(ExpressionListStepper, PromotedThing);
                    /* sanity check */
                    Error = TypeCheckExpression(
                        out ActualType/*obtain new type*/,
                        ExprListGetFirstExpr(ExpressionListStepper),
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
                ExpressionListStepper = ExprListGetRestList(ExpressionListStepper);
                FunctionArgumentStepper = FunctionArgumentStepper.GetRestListFromSymbolList();
            }
            if ((ExpressionListStepper != null) || (FunctionArgumentStepper != null))
            {
                ErrorLineNumber = FunctionCall.LineNumber;
                return CompileErrors.eCompileWrongNumberOfArgsToFunction;
            }

            ResultingDataType = FunctionSymbol.GetSymbolFunctionReturnType();
            return CompileErrors.eCompileNoError;
        }

        /* generate code for a function call. returns True if successful, or False if it fails. */
        public static void CodeGenFunctionCall(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTFuncCallRec FunctionCall)
        {
            int unused;

            int StackDepth = StackDepthParam;

            /* push each argument on stack; from left to right */
            CodeGenExpressionListArguments(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                FunctionCall.ArgumentList);
            Debug.Assert(StackDepth <= MaxStackDepth);

            /* figure out how to call the function */
            ASTOperandRec FunctionContainer = GetOperandOutOfExpression(FunctionCall.FunctionGenerator);
            SymbolRec FunctionSymbol = GetSymbolFromOperand(FunctionContainer);
            FuncCode.AddPcodeInstruction(Pcodes.epFuncCallUnresolved, out unused, FunctionCall.LineNumber);
            /* push function name */
            FuncCode.AddPcodeOperandString(FunctionSymbol.GetSymbolName());
            /* push a parameter list record for runtime checking */
            SymbolListRec FormalArgumentList = FunctionSymbol.GetSymbolFunctionArgList();
            /* now that we have the list, let's just check the stack for consistency */
            if (StackDepth != StackDepthParam + SymbolListRec.GetSymbolListLength(FormalArgumentList))
            {
                // stack depth error after pushing args
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            int NumberOfArguments = SymbolListRec.GetSymbolListLength(FormalArgumentList);
            DataTypes[] DataTypeArray = new DataTypes[NumberOfArguments];
            int Index = 0;
            while (FormalArgumentList != null)
            {
                DataTypeArray[Index] = FormalArgumentList.GetFirstFromSymbolList().GetSymbolVariableDataType();
                Index++;
                FormalArgumentList = FormalArgumentList.GetRestListFromSymbolList();
            }
            FuncCode.AddPcodeOperandDataTypeArray(DataTypeArray);
            /* save the return type */
            FuncCode.AddPcodeOperandInteger((int)FunctionSymbol.GetSymbolFunctionReturnType());
            /* make an instruction operand for the reserved pcode link reference */
            FuncCode.AddPcodeOperandInteger(-1);
            // make operand for reserved maxstackdepth */
            FuncCode.AddPcodeOperandInteger(-1);

            /* increment stack pointer since there will be 1 returned value */
            StackDepthParam++;
            MaxStackDepth = Math.Max(MaxStackDepth, StackDepth);
        }

#if true // TODO:experimental
        public static void ILGenFunctionCall(
            CILObject cilObject,
            ILGenContext context,
            ASTFuncCallRec FunctionCall)
        {
            ASTOperandRec FunctionContainer = GetOperandOutOfExpression(FunctionCall.FunctionGenerator);
            SymbolRec FunctionSymbol = GetSymbolFromOperand(FunctionContainer);

            List<DataTypes> argsTypes = new List<DataTypes>();
            SymbolListRec argsList = FunctionSymbol.GetSymbolFunctionArgList();
            while (argsList != null)
            {
                argsTypes.Add(argsList.GetFirstFromSymbolList().GetSymbolVariableDataType());
                argsList = argsList.GetRestListFromSymbolList();
            }
            DataTypes returnType = FunctionSymbol.GetSymbolFunctionReturnType();

            int methodIndex = context.managedFunctionLinker.QueryFunctionIndex(FunctionSymbol.GetSymbolName());

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
            ILGenExpressionListArguments(
                cilObject,
                context,
                FunctionCall.ArgumentList);

            // call the function

            Type[] managedArgsTypes;
            Type managedReturnType;
            CILObject.GetManagedFunctionSignature(
                argsTypes.ToArray(),
                returnType,
                out managedArgsTypes,
                out managedReturnType);

            context.ilGenerator.Emit(OpCodes.Call, typeof(CILThreadLocalStorage).GetMethod("get_CurrentFunctionPointers", BindingFlags.Public | BindingFlags.Static));
            context.ilGenerator.Emit(OpCodes.Ldc_I4, methodIndex);
            context.ilGenerator.Emit(OpCodes.Ldelem, typeof(IntPtr));
            context.ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, managedReturnType, managedArgsTypes, null);
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstFunctionCall(
            ASTFuncCallRec FunctionCall,
            out bool DidSomething)
        {
            bool WeDidSomething = false;
            bool TheyDidSomething;

            /* optimize arglist; don't bother if there are no args to the function */
            if (FunctionCall.ArgumentList != null)
            {
                ASTExpressionRec ExprList;
                DataTypes ExprListType;

                FoldConstExpressionList(
                    FunctionCall.ArgumentList,
                    out TheyDidSomething,
                    out ExprList,
                    out ExprListType,
                    true/*we are func arg list */);
                WeDidSomething = WeDidSomething || TheyDidSomething;
                FunctionCall.ArgumentList = GetSequenceFromExpression(ExprList);
            }

            FoldConstExpression(
                FunctionCall.FunctionGenerator,
                out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            DidSomething = WeDidSomething;
        }




        public enum LoopTypes
        {
            eInvalid,

            eLoopWhileDo,
            eLoopUntilDo,
            eLoopDoWhile,
            eLoopDoUntil,
        }

        public class ASTLoopRec
        {
            public LoopTypes KindOfLoop;
            public ASTExpressionRec ControlExpression;
            public ASTExpressionRec BodyExpression;
            public int LineNumber;
        }

        /* create a new loop node */
        public static ASTLoopRec NewLoop(
            LoopTypes LoopType,
            ASTExpressionRec ControlExpr,
            ASTExpressionRec BodyExpr,
            int LineNumber)
        {
            ASTLoopRec Loop = new ASTLoopRec();

            Loop.LineNumber = LineNumber;
            Loop.KindOfLoop = LoopType;
            Loop.ControlExpression = ControlExpr;
            Loop.BodyExpression = BodyExpr;

            return Loop;
        }

        /* type check the loop node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckLoop(
                                                    out DataTypes ResultingDataType,
                                                    ASTLoopRec Loop,
                                                    out int ErrorLineNumber)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            DataTypes ConditionalType;
            Error = TypeCheckExpression(out ConditionalType, Loop.ControlExpression, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            if (ConditionalType != DataTypes.eBoolean)
            {
                ErrorLineNumber = Loop.LineNumber;
                return CompileErrors.eCompileConditionalMustBeBoolean;
            }

            DataTypes BodyType;
            Error = TypeCheckExpression(out BodyType, Loop.BodyExpression, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            ResultingDataType = BodyType;
            return CompileErrors.eCompileNoError;
        }

        /* generate code for a loop. returns True if successful, or False if it fails. */
        public static void CodeGenLoop(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTLoopRec Loop)
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
            switch (GetExpressionsResultantType(Loop.BodyExpression))
            {
                default:
                    // bad type of body expression
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case DataTypes.eBoolean:
                case DataTypes.eInteger:
                    FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, Loop.LineNumber);
                    FuncCode.AddPcodeOperandInteger(0);
                    break;
                case DataTypes.eFloat:
                    FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateFloat, out unused, Loop.LineNumber);
                    FuncCode.AddPcodeOperandFloat(0);
                    break;
                case DataTypes.eDouble:
                    FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateDouble, out unused, Loop.LineNumber);
                    FuncCode.AddPcodeOperandDouble(0);
                    break;
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                case DataTypes.eArrayOfInteger:
                case DataTypes.eArrayOfFloat:
                case DataTypes.eArrayOfDouble:
                    FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArray, out unused, Loop.LineNumber);
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
            if ((Loop.KindOfLoop == LoopTypes.eLoopWhileDo) || (Loop.KindOfLoop == LoopTypes.eLoopUntilDo))
            {
                FuncCode.AddPcodeInstruction(Pcodes.epBranchUnconditional, out WhileBranchPatchupLocation, Loop.LineNumber);
                FuncCode.AddPcodeOperandInteger(-1/*target unknown*/);
            }

            /* remember this address! */
            int LoopBackAgainLocation = FuncCode.PcodeGetNextAddress();

            /* pop previous result */
            FuncCode.AddPcodeInstruction(Pcodes.epStackPop, out unused, Loop.LineNumber);
            StackDepth--;
            if (StackDepth != StackDepthParam)
            {
                // stack depth error after popping previous
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* evaluate the body */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                Loop.BodyExpression);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 1)
            {
                // stack depth error after evaluating body
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* patch up the while branch */
            if ((Loop.KindOfLoop == LoopTypes.eLoopWhileDo) || (Loop.KindOfLoop == LoopTypes.eLoopUntilDo))
            {
                FuncCode.ResolvePcodeBranch(WhileBranchPatchupLocation, FuncCode.PcodeGetNextAddress());
            }

            /* evaluate the test */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                Loop.ControlExpression);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 2)
            {
                // stack depth error after evaluating control
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* do the appropriate branch */
            if ((Loop.KindOfLoop == LoopTypes.eLoopWhileDo) || (Loop.KindOfLoop == LoopTypes.eLoopDoWhile))
            {
                /* "while do" and "do while" need a branch if true conditional */
                FuncCode.AddPcodeInstruction(Pcodes.epBranchIfNotZero, out unused, Loop.LineNumber);
                FuncCode.AddPcodeOperandInteger(LoopBackAgainLocation);
            }
            else
            {
                /* "until do" and "do until" need a branch if false conditional */
                FuncCode.AddPcodeInstruction(Pcodes.epBranchIfZero, out unused, Loop.LineNumber);
                FuncCode.AddPcodeOperandInteger(LoopBackAgainLocation);
            }
            StackDepth--;
            if (StackDepth != StackDepthParam + 1)
            {
                // stack depth error after control branch
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            StackDepthParam = StackDepth;
        }

#if true // TODO:experimental
        public static void ILGenLoop(
            CILObject cilObject,
            ILGenContext context,
            ASTLoopRec Loop)
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
            switch (GetExpressionsResultantType(Loop.BodyExpression))
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
                case DataTypes.eArrayOfInteger:
                case DataTypes.eArrayOfFloat:
                case DataTypes.eArrayOfDouble:
                    context.ilGenerator.Emit(OpCodes.Ldnull);
                    break;
            }

            /* if loop is a head-tested loop (while loop) then insert extra branch */
            Label WhileBranchPatchupLocation = new Label();
            if ((Loop.KindOfLoop == LoopTypes.eLoopWhileDo) || (Loop.KindOfLoop == LoopTypes.eLoopUntilDo))
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
            ILGenExpression(
                cilObject,
                context,
                Loop.BodyExpression);

            /* patch up the while branch */
            if ((Loop.KindOfLoop == LoopTypes.eLoopWhileDo) || (Loop.KindOfLoop == LoopTypes.eLoopUntilDo))
            {
                context.ilGenerator.MarkLabel(WhileBranchPatchupLocation);
            }

            /* evaluate the test */
            ILGenExpression(
                cilObject,
                context,
                Loop.ControlExpression);

            /* do the appropriate branch */
            if ((Loop.KindOfLoop == LoopTypes.eLoopWhileDo) || (Loop.KindOfLoop == LoopTypes.eLoopDoWhile))
            {
                /* "while do" and "do while" need a branch if true conditional */
                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                context.ilGenerator.Emit(OpCodes.Bne_Un, LoopBackAgainLocation);
            }
            else
            {
                /* "until do" and "do until" need a branch if false conditional */
                context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
                context.ilGenerator.Emit(OpCodes.Beq, LoopBackAgainLocation);
            }
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstLoop(
            ASTLoopRec Loop,
            out bool DidSomething,
            out ASTExpressionRec ResultantExpr,
            out DataTypes ResultType)
        {
            bool WeDidSomething = false;
            bool TheyDidSomething;

            ResultantExpr = null;
            ResultType = GetExpressionsResultantType(Loop.BodyExpression);

            FoldConstExpression(Loop.ControlExpression, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            FoldConstExpression(Loop.BodyExpression, out TheyDidSomething);
            WeDidSomething = WeDidSomething || TheyDidSomething;

            /* can we eliminate the iterations? */
            bool LoopWasRemoved = false;
            if ((WhatKindOfExpressionIsThis(Loop.ControlExpression) == ExprTypes.eExprOperand)
                && (OperandWhatIsIt(GetOperandFromExpression(Loop.ControlExpression)) == ASTOperandType.eASTOperandBooleanLiteral))
            {
                switch (Loop.KindOfLoop)
                {
                    default:
                        // invalid loop type
                        Debug.Assert(false);
                        throw new InvalidOperationException();

                    case LoopTypes.eLoopWhileDo:
                    case LoopTypes.eLoopUntilDo:
                        if (!GetOperandBooleanLiteral(GetOperandFromExpression(Loop.ControlExpression))
                            == !(Loop.KindOfLoop == LoopTypes.eLoopUntilDo))
                        {
                            ASTOperandRec NewOperand = null;

                            /* if cond is true and until do */
                            /* (or if cond is false and while do) */
                            /* then we can return zero since nothing happens */
                            switch (GetExpressionsResultantType(Loop.BodyExpression))
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
                                    NewOperand = NewBooleanLiteral(
                                        false,
                                        Loop.LineNumber);
                                    break;
                                case DataTypes.eInteger:
                                    NewOperand = NewIntegerLiteral(
                                        0,
                                        Loop.LineNumber);
                                    break;
                                case DataTypes.eFloat:
                                    NewOperand = NewSingleLiteral(
                                        0,
                                        Loop.LineNumber);
                                    break;
                                case DataTypes.eDouble:
                                    NewOperand = NewDoubleLiteral(
                                        0,
                                        Loop.LineNumber);
                                    break;
                            }

                            if (NewOperand != null)
                            {
                                ResultantExpr = NewExprOperand(
                                    NewOperand,
                                    Loop.LineNumber);
                                WeDidSomething = true;
                                LoopWasRemoved = true;
                            }
                        }
                        break;

                    case LoopTypes.eLoopDoWhile:
                    case LoopTypes.eLoopDoUntil:
                        if (!GetOperandBooleanLiteral(GetOperandFromExpression(Loop.ControlExpression))
                            == !(Loop.KindOfLoop == LoopTypes.eLoopDoUntil))
                        {
                            /* if cond is true and loop until */
                            /* (or if cond is false and loop while) */
                            /* then we can substitute the loop body */
                            ResultantExpr = Loop.BodyExpression;
                            LoopWasRemoved = true;
                        }
                        break;
                }
            }
            if (!LoopWasRemoved)
            {
                /* couldn't eliminate the loop */
                ResultantExpr = NewExprLoop(
                    Loop,
                    Loop.LineNumber);
            }

            DidSomething = WeDidSomething;
        }




        /* operand types */
        public enum ASTOperandType
        {
            eInvalid,

            eASTOperandIntegerLiteral,
            eASTOperandBooleanLiteral,
            eASTOperandSingleLiteral,
            eASTOperandDoubleLiteral,
            eASTOperandSymbol,
            eASTOperandStringLiteral,
        }

        public class ASTOperandRec
        {
            public ASTOperandType Type;
            public int LineNumberOfSource;
            public object u;
        }

        /* create a new integer literal */
        public static ASTOperandRec NewIntegerLiteral(
            int IntegerLiteralValue,
            int LineNumber)
        {
            ASTOperandRec IntegerLit = new ASTOperandRec();

            IntegerLit.Type = ASTOperandType.eASTOperandIntegerLiteral;
            IntegerLit.LineNumberOfSource = LineNumber;
            IntegerLit.u = IntegerLiteralValue;

            return IntegerLit;
        }

        /* create a new boolean literal */
        public static ASTOperandRec NewBooleanLiteral(
            bool BooleanLiteralValue,
            int LineNumber)
        {
            ASTOperandRec BooleanLit = new ASTOperandRec();

            BooleanLit.Type = ASTOperandType.eASTOperandBooleanLiteral;
            BooleanLit.LineNumberOfSource = LineNumber;
            BooleanLit.u = BooleanLiteralValue;

            return BooleanLit;
        }

        /* create a new single precision literal */
        public static ASTOperandRec NewSingleLiteral(
            float SingleLiteralValue,
            int LineNumber)
        {
            ASTOperandRec SingleLit = new ASTOperandRec();

            SingleLit.Type = ASTOperandType.eASTOperandSingleLiteral;
            SingleLit.LineNumberOfSource = LineNumber;
            SingleLit.u = SingleLiteralValue;

            return SingleLit;
        }

        /* create a new double precision literal */
        public static ASTOperandRec NewDoubleLiteral(
            double DoubleLiteralValue,
            int LineNumber)
        {
            ASTOperandRec DoubleLit = new ASTOperandRec();

            DoubleLit.Type = ASTOperandType.eASTOperandDoubleLiteral;
            DoubleLit.LineNumberOfSource = LineNumber;
            DoubleLit.u = DoubleLiteralValue;

            return DoubleLit;
        }

        /* create a new symbol reference. */
        public static ASTOperandRec NewSymbolReference(
            SymbolRec SymbolTableEntry,
            int LineNumber)
        {
            ASTOperandRec VarRef = new ASTOperandRec();

            VarRef.Type = ASTOperandType.eASTOperandSymbol;
            VarRef.LineNumberOfSource = LineNumber;
            VarRef.u = SymbolTableEntry;

            return VarRef;
        }

        /* create a new string literal */
        public static ASTOperandRec NewStringLiteral(
            string Data,
            int LineNumber)
        {
            ASTOperandRec VarRef = new ASTOperandRec();

            VarRef.Type = ASTOperandType.eASTOperandStringLiteral;
            VarRef.LineNumberOfSource = LineNumber;
            VarRef.u = Data;

            return VarRef;
        }

        public static ASTOperandType OperandWhatIsIt(ASTOperandRec TheOperand)
        {
            return TheOperand.Type;
        }

        public static int GetOperandIntegerLiteral(ASTOperandRec Operand)
        {
            if ((Operand.Type != ASTOperandType.eASTOperandIntegerLiteral) || !(Operand.u is int))
            {
                // not an integer literal
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (int)Operand.u;
        }

        public static bool GetOperandBooleanLiteral(ASTOperandRec Operand)
        {
            if ((Operand.Type != ASTOperandType.eASTOperandBooleanLiteral) || !(Operand.u is bool))
            {
                // not a boolean literal
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (bool)Operand.u;
        }

        public static float GetOperandSingleLiteral(ASTOperandRec Operand)
        {
            if ((Operand.Type != ASTOperandType.eASTOperandSingleLiteral) || !(Operand.u is float))
            {
                // not a single literal
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (float)Operand.u;
        }

        public static double GetOperandDoubleLiteral(ASTOperandRec Operand)
        {
            if ((Operand.Type != ASTOperandType.eASTOperandDoubleLiteral) || !(Operand.u is double))
            {
                // not a double literal
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (double)Operand.u;
        }

        public static string GetOperandStringLiteral(ASTOperandRec Operand)
        {
            if ((Operand.Type != ASTOperandType.eASTOperandStringLiteral) || !(Operand.u is string))
            {
                // not a string literal
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (string)Operand.u;
        }

        public static bool IsOperandASymbol(ASTOperandRec Operand)
        {
            return Operand.Type == ASTOperandType.eASTOperandSymbol;
        }

        public static SymbolRec GetSymbolFromOperand(ASTOperandRec Operand)
        {
            if (!IsOperandASymbol(Operand) || !(Operand.u is SymbolRec))
            {
                // not a symbolic operand
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return (SymbolRec)Operand.u;
        }

        /* type check the operand node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckOperand(
            out DataTypes ResultingDataType,
            ASTOperandRec Operand,
            out int ErrorLineNumber)
        {
            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            switch (Operand.Type)
            {
                default:
                    // unknown operand kind
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case ASTOperandType.eASTOperandIntegerLiteral:
                    ResultingDataType = DataTypes.eInteger;
                    break;
                case ASTOperandType.eASTOperandBooleanLiteral:
                    ResultingDataType = DataTypes.eBoolean;
                    break;
                case ASTOperandType.eASTOperandSingleLiteral:
                    ResultingDataType = DataTypes.eFloat;
                    break;
                case ASTOperandType.eASTOperandDoubleLiteral:
                    ResultingDataType = DataTypes.eDouble;
                    break;
                case ASTOperandType.eASTOperandStringLiteral:
                    ResultingDataType = DataTypes.eArrayOfByte;
                    break;
                case ASTOperandType.eASTOperandSymbol:
                    if (GetSymbolFromOperand(Operand).WhatIsThisSymbol() != SymbolType.eSymbolVariable)
                    {
                        ErrorLineNumber = Operand.LineNumberOfSource;
                        return CompileErrors.eCompileMustBeAVariableIdentifier;
                    }
                    ResultingDataType = GetSymbolFromOperand(Operand).GetSymbolVariableDataType();
                    break;
            }
            return CompileErrors.eCompileNoError;
        }

        /* generate code for an operand. returns True if successful, or False if it fails. */
        public static void CodeGenOperand(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTOperandRec Operand)
        {
            int unused;

            int StackDepth = StackDepthParam;

            switch (Operand.Type)
            {
                default:
                    // unknown operand type
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case ASTOperandType.eASTOperandIntegerLiteral:
                    FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, Operand.LineNumberOfSource);
                    FuncCode.AddPcodeOperandInteger(GetOperandIntegerLiteral(Operand));
                    break;
                case ASTOperandType.eASTOperandBooleanLiteral:
                    FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, Operand.LineNumberOfSource);
                    FuncCode.AddPcodeOperandInteger(GetOperandBooleanLiteral(Operand) ? 1 : 0);
                    break;
                case ASTOperandType.eASTOperandSingleLiteral:
                    FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateFloat, out unused, Operand.LineNumberOfSource);
                    FuncCode.AddPcodeOperandFloat(GetOperandSingleLiteral(Operand));
                    break;
                case ASTOperandType.eASTOperandDoubleLiteral:
                    FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateDouble, out unused, Operand.LineNumberOfSource);
                    FuncCode.AddPcodeOperandDouble(GetOperandDoubleLiteral(Operand));
                    break;

                /* this had better be a variable */
                case ASTOperandType.eASTOperandSymbol:
                    switch (GetSymbolFromOperand(Operand).GetSymbolVariableDataType())
                    {
                        default:
                            // bad variable type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eBoolean:
                        case DataTypes.eInteger:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadIntegerFromStack, out unused, Operand.LineNumberOfSource);
                            break;
                        case DataTypes.eFloat:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadFloatFromStack, out unused, Operand.LineNumberOfSource);
                            break;
                        case DataTypes.eDouble:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadDoubleFromStack, out unused, Operand.LineNumberOfSource);
                            break;
                        case DataTypes.eArrayOfBoolean:
                        case DataTypes.eArrayOfByte:
                        case DataTypes.eArrayOfInteger:
                        case DataTypes.eArrayOfFloat:
                        case DataTypes.eArrayOfDouble:
                            FuncCode.AddPcodeInstruction(Pcodes.epLoadArrayFromStack, out unused, Operand.LineNumberOfSource);
                            break;
                    }
                    /* stack offsets are negative */
                    FuncCode.AddPcodeOperandInteger(GetSymbolFromOperand(Operand).GetSymbolVariableStackLocation() - StackDepth);
                    break;

                case ASTOperandType.eASTOperandStringLiteral:
                    FuncCode.AddPcodeInstruction(
                        Pcodes.epMakeByteArrayFromString,
                        out unused,
                        Operand.LineNumberOfSource);
                    FuncCode.AddPcodeOperandString(
                        GetOperandStringLiteral(Operand));
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

#if true // TODO:experimental
        public static void ILGenOperand(
            CILObject cilObject,
            ILGenContext context,
            ASTOperandRec Operand)
        {
            switch (Operand.Type)
            {
                default:
                    // unknown operand type
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case ASTOperandType.eASTOperandIntegerLiteral:
                    context.ilGenerator.Emit(OpCodes.Ldc_I4, GetOperandIntegerLiteral(Operand));
                    break;
                case ASTOperandType.eASTOperandBooleanLiteral:
                    context.ilGenerator.Emit(GetOperandBooleanLiteral(Operand) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    break;
                case ASTOperandType.eASTOperandSingleLiteral:
                    context.ilGenerator.Emit(OpCodes.Ldc_R4, GetOperandSingleLiteral(Operand));
                    break;
                case ASTOperandType.eASTOperandDoubleLiteral:
                    context.ilGenerator.Emit(OpCodes.Ldc_R8, GetOperandDoubleLiteral(Operand));
                    break;

                /* this had better be a variable */
                case ASTOperandType.eASTOperandSymbol:
                    if (context.variableTable.ContainsKey(GetSymbolFromOperand(Operand))) // local variables can mask arguments
                    {
                        // local variable
                        context.ilGenerator.Emit(OpCodes.Ldloc, context.variableTable[GetSymbolFromOperand(Operand)]);
                    }
                    else
                    {
                        // function argument
                        context.ilGenerator.Emit(OpCodes.Ldarg, (short)context.argumentTable[GetSymbolFromOperand(Operand).GetSymbolName()]); // operand is 'short' -- see https://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes.ldarg(v=vs.110).aspx
                    }
                    break;

                case ASTOperandType.eASTOperandStringLiteral:
                    context.ilGenerator.Emit(OpCodes.Call, typeof(Encoding).GetMethod("get_UTF8"));
                    context.ilGenerator.Emit(OpCodes.Ldstr, GetOperandStringLiteral(Operand));
                    context.ilGenerator.Emit(OpCodes.Call, typeof(Encoding).GetMethod("GetBytes", new Type[] { typeof(string) }));
                    context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleByte).GetConstructor(new Type[] { typeof(byte[]) }));
                    break;
            }
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstOperand(
            ASTOperandRec Operand,
            out bool DidSomething)
        {
            DidSomething = false;
        }




        public class ASTPrintExprRec
        {
            public ASTExpressionRec Value;
            public int LineNumber;
            public DataTypes ExpressionType;
        }

        /* create a new AST expression print */
        public static ASTPrintExprRec NewPrintExpr(
            ASTExpressionRec Expression,
            int LineNumber)
        {
            ASTPrintExprRec PrintExpr = new ASTPrintExprRec();

            PrintExpr.Value = Expression;
            PrintExpr.LineNumber = LineNumber;

            return PrintExpr;
        }

        /* type check the expr print node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckPrintExpr(
            out DataTypes ResultingDataType,
            ASTPrintExprRec PrintExpr,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            Error = TypeCheckExpression(out PrintExpr.ExpressionType, PrintExpr.Value, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            switch (PrintExpr.ExpressionType)
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
                    if (!CanRightBeMadeToMatchLeft(DataTypes.eDouble, PrintExpr.ExpressionType))
                    {
                        ErrorLineNumber = PrintExpr.LineNumber;
                        return CompileErrors.eCompileOperandMustBeDouble;
                    }
                    if (MustRightBePromotedToLeft(DataTypes.eDouble, PrintExpr.ExpressionType))
                    {
                        /* we must promote the right operand to become the left operand type */
                        ASTExpressionRec PromotedOperand = PromoteTheExpression(
                    PrintExpr.ExpressionType/*orig*/,
                     DataTypes.eDouble/*desired*/,
                     PrintExpr.Value,
                     PrintExpr.LineNumber);
                        PrintExpr.Value = PromotedOperand;
                        /* sanity check */
                        Error = TypeCheckExpression(out PrintExpr.ExpressionType/*obtain new type*/,
                            PrintExpr.Value, out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            // type promotion caused failure
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (DataTypes.eDouble != PrintExpr.ExpressionType)
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
                    ErrorLineNumber = PrintExpr.LineNumber;
                    return CompileErrors.eCompileOperandsMustBeScalar;
            }

            ResultingDataType = DataTypes.eBoolean;
            return CompileErrors.eCompileNoError;
        }

        /* generate code for an expr print.  returns True if successful, or False if it fails. */
        public static void CodeGenPrintExpr(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExpressionRec OuterExpression,
            ASTPrintExprRec PrintExpr)
        {
            string methodName;
            DataTypes argType;

            switch (PrintExpr.ExpressionType)
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

            CodeGenExternCall(
                FuncCode,
                ref StackDepthParam,
                ref MaxStackDepth,
                OuterExpression,
                methodName,
                new ASTExpressionRec[1]
                {
                    PrintExpr.Value,
                },
                new DataTypes[1]
                {
                    argType/*0: value*/,
                },
                argType/*return type/value same as arg*/);
        }

#if true // TODO:experimental
        public static void ILGenPrintExpr(
            CILObject cilObject,
            ILGenContext context,
            ASTExpressionRec OuterExpression,
            ASTPrintExprRec PrintExpr)
        {
            string methodName;
            DataTypes argType;

            switch (PrintExpr.ExpressionType)
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
                OuterExpression,
                methodName,
                new ASTExpressionRec[1]
                {
                    PrintExpr.Value,
                },
                new DataTypes[1]
                {
                    argType/*0: value*/,
                },
                argType/*return type/value same as arg*/);
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstPrintExpr(
            ASTPrintExprRec PrintExpr,
            out bool DidSomething)
        {
            FoldConstExpression(PrintExpr.Value, out DidSomething);
        }




        public class ASTPrintStringRec
        {
            public string MessageString;
            public int LineNumber;
        }

        /* create a new AST string print */
        public static ASTPrintStringRec NewPrintString(
            string String,
            int LineNumber)
        {
            ASTPrintStringRec PrintString = new ASTPrintStringRec();

            PrintString.MessageString = String;
            PrintString.LineNumber = LineNumber;

            return PrintString;
        }

        /* type check the string print node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckPrintString(
            out DataTypes ResultingDataType,
            ASTPrintStringRec PrintString,
            out int ErrorLineNumber)
        {
            ErrorLineNumber = -1;

            ResultingDataType = DataTypes.eBoolean;
            return CompileErrors.eCompileNoError;
        }

        /* generate code for a string print.  returns True if successful, or False if it fails. */
        public static void CodeGenPrintString(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExpressionRec OuterExpression,
            ASTPrintStringRec PrintString)
        {
            ASTExpressionRec messageStringExpression = NewExprOperand(
                NewStringLiteral(
                    PrintString.MessageString,
                    PrintString.LineNumber),
                PrintString.LineNumber);
            messageStringExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            CodeGenExternCall(
                FuncCode,
                ref StackDepthParam,
                ref MaxStackDepth,
                OuterExpression,
                "PrintString",
                new ASTExpressionRec[1]
                {
                    messageStringExpression,
                },
                new DataTypes[1]
                {
                    DataTypes.eArrayOfByte/*0: value*/,
                },
                DataTypes.eBoolean);
        }

#if true // TODO:experimental
        public static void ILGenPrintString(
            CILObject cilObject,
            ILGenContext context,
            ASTExpressionRec OuterExpression,
            ASTPrintStringRec PrintString)
        {
            ASTExpressionRec messageStringExpression = NewExprOperand(
                NewStringLiteral(
                    PrintString.MessageString,
                    PrintString.LineNumber),
                PrintString.LineNumber);
            messageStringExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            ILGenExternCall(
                cilObject,
                context,
                OuterExpression,
                "PrintString",
                new ASTExpressionRec[1]
                {
                    messageStringExpression,
                },
                new DataTypes[1]
                {
                    DataTypes.eArrayOfByte/*0: value*/,
                },
                DataTypes.eBoolean);
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstPrintString(
            ASTPrintStringRec PrintString,
            out bool DidSomething)
        {
            DidSomething = false;
        }




        public enum SampleLoaderOp
        {
            eInvalid,

            eSampleLoaderSampleLeft,
            eSampleLoaderSampleRight,
            eSampleLoaderSampleMono,
        }

        public class ASTSampleLoaderRec
        {
            public string FileType;
            public string SampleName;
            public SampleLoaderOp TheOperation;
            public int LineNumber;
        }

        /* create a new AST sample loader form */
        public static ASTSampleLoaderRec NewSampleLoader(
            string FileType,
            string SampleName,
            SampleLoaderOp TheOperation,
            int LineNumber)
        {
            ASTSampleLoaderRec SampleLoader = new ASTSampleLoaderRec();

            SampleLoader.FileType = FileType;
            SampleLoader.SampleName = SampleName;
            SampleLoader.TheOperation = TheOperation;
            SampleLoader.LineNumber = LineNumber;

            return SampleLoader;
        }

        /* type check the sample loader node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckSampleLoader(
            out DataTypes ResultingDataType,
            ASTSampleLoaderRec SampleLoader,
            out int ErrorLineNumber)
        {
            ErrorLineNumber = -1;

            ResultingDataType = DataTypes.eArrayOfFloat;

            return CompileErrors.eCompileNoError;
        }

        /* generate code for a sample loader.  returns True if successful, */
        /* or False if it fails. */
        public static void CodeGenSampleLoader(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExpressionRec OuterExpression,
            ASTSampleLoaderRec SampleLoader)
        {
            ASTExpressionRec fileTypeStringExpression = NewExprOperand(
                NewStringLiteral(
                    SampleLoader.FileType,
                    SampleLoader.LineNumber),
                SampleLoader.LineNumber);
            fileTypeStringExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            ASTExpressionRec fileNameStringExpression = NewExprOperand(
                NewStringLiteral(
                    SampleLoader.SampleName,
                    SampleLoader.LineNumber),
                SampleLoader.LineNumber);
            fileNameStringExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            string WhichMethod;
            switch (SampleLoader.TheOperation)
            {
                default:
                    // bad opcode
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case SampleLoaderOp.eSampleLoaderSampleLeft:
                    WhichMethod = "LoadSampleLeftArray";
                    break;
                case SampleLoaderOp.eSampleLoaderSampleRight:
                    WhichMethod = "LoadSampleRightArray";
                    break;
                case SampleLoaderOp.eSampleLoaderSampleMono:
                    WhichMethod = "LoadSampleMonoArray";
                    break;
            }

            CodeGenExternCall(
                FuncCode,
                ref StackDepthParam,
                ref MaxStackDepth,
                OuterExpression,
                WhichMethod,
                new ASTExpressionRec[2]
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

#if true // TODO:experimental
        public static void ILGenSampleLoader(
            CILObject cilObject,
            ILGenContext context,
            ASTExpressionRec OuterExpression,
            ASTSampleLoaderRec SampleLoader)
        {
            ASTExpressionRec fileTypeStringExpression = NewExprOperand(
                NewStringLiteral(
                    SampleLoader.FileType,
                    SampleLoader.LineNumber),
                SampleLoader.LineNumber);
            fileTypeStringExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            ASTExpressionRec fileNameStringExpression = NewExprOperand(
                NewStringLiteral(
                    SampleLoader.SampleName,
                    SampleLoader.LineNumber),
                SampleLoader.LineNumber);
            fileNameStringExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            string WhichMethod;
            switch (SampleLoader.TheOperation)
            {
                default:
                    // bad opcode
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case SampleLoaderOp.eSampleLoaderSampleLeft:
                    WhichMethod = "LoadSampleLeftArray";
                    break;
                case SampleLoaderOp.eSampleLoaderSampleRight:
                    WhichMethod = "LoadSampleRightArray";
                    break;
                case SampleLoaderOp.eSampleLoaderSampleMono:
                    WhichMethod = "LoadSampleMonoArray";
                    break;
            }

            ILGenExternCall(
                cilObject,
                context,
                OuterExpression,
                WhichMethod,
                new ASTExpressionRec[2]
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
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstSampleLoader(
            ASTSampleLoaderRec SampleLoader,
            out bool DidSomething)
        {
            DidSomething = false;
        }




        public enum UnaryOpType
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

        public class ASTUnaryOpRec
        {
            public UnaryOpType Operation;
            public ASTExpressionRec Argument;
            public int LineNumber;
        }

        /* create a unary operator node */
        public static ASTUnaryOpRec NewUnaryOperator(
                                            UnaryOpType WhatOperation,
                                            ASTExpressionRec Argument,
                                            int LineNumber)
        {
            ASTUnaryOpRec UnaryOp = new ASTUnaryOpRec();

            UnaryOp.LineNumber = LineNumber;
            UnaryOp.Operation = WhatOperation;
            UnaryOp.Argument = Argument;

            return UnaryOp;
        }

        /* type check the unary operator node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckUnaryOperator(
            out DataTypes ResultingDataType,
            ASTUnaryOpRec UnaryOperator,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            /* check the argument and find out what type it is */
            DataTypes ArgumentType;
            Error = TypeCheckExpression(out ArgumentType, UnaryOperator.Argument, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            /* do type checking and promotion; return type determination is deferred... */
            switch (UnaryOperator.Operation)
            {
                default:
                    // unknown opcode
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                /* operators which take an integer, single, double, or fixed and */
                /* return a boolean */
                case UnaryOpType.eUnaryTestNegative:
                /* FALL THROUGH */

                /* operators which take an integer, single, double, or fixed and */
                /* return an integer */
                case UnaryOpType.eUnaryGetSign:
                /* FALL THROUGH */

                /* operators capable of integer, single, double, fixed arguments */
                /* which return the same type as the argument */
                case UnaryOpType.eUnaryNegation:
                case UnaryOpType.eUnaryAbsoluteValue:
                    if (!IsItASequencedScalarType(ArgumentType))
                    {
                        ErrorLineNumber = UnaryOperator.LineNumber;
                        return CompileErrors.eCompileOperandsMustBeSequencedScalar;
                    }
                    break;

                /* operators capable of boolean or integer arguments which */
                /* return the same type as the argument */
                case UnaryOpType.eUnaryNot:
                    if ((ArgumentType != DataTypes.eBoolean) && (ArgumentType != DataTypes.eInteger))
                    {
                        ErrorLineNumber = UnaryOperator.LineNumber;
                        return CompileErrors.eCompileOperandMustBeBooleanOrInteger;
                    }
                    break;

                /* operators capable of double arguments which return doubles */
                case UnaryOpType.eUnarySine:
                case UnaryOpType.eUnaryCosine:
                case UnaryOpType.eUnaryTangent:
                case UnaryOpType.eUnaryArcSine:
                case UnaryOpType.eUnaryArcCosine:
                case UnaryOpType.eUnaryArcTangent:
                case UnaryOpType.eUnaryLogarithm:
                case UnaryOpType.eUnaryExponentiation:
                case UnaryOpType.eUnarySquare:
                case UnaryOpType.eUnarySquareRoot:
                    if (!CanRightBeMadeToMatchLeft(DataTypes.eDouble, ArgumentType))
                    {
                        ErrorLineNumber = UnaryOperator.LineNumber;
                        return CompileErrors.eCompileOperandMustBeDouble;
                    }
                    if (MustRightBePromotedToLeft(DataTypes.eDouble, ArgumentType))
                    {
                        /* we must promote the right operand to become the left operand type */
                        ASTExpressionRec PromotedOperand = PromoteTheExpression(
                            ArgumentType/*orig*/,
                            DataTypes.eDouble/*desired*/,
                            UnaryOperator.Argument,
                            UnaryOperator.LineNumber);
                        UnaryOperator.Argument = PromotedOperand;
                        /* sanity check */
                        Error = TypeCheckExpression(
                            out ArgumentType/*obtain new right type*/,
                            UnaryOperator.Argument,
                            out ErrorLineNumber);
                        if (Error != CompileErrors.eCompileNoError)
                        {
                            // type promotion caused failure
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (DataTypes.eDouble != ArgumentType)
                        {
                            // after type promotion, types are no longer the same
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                    }
                    break;

                /* operands which take a boolean, integer, single, double, or fixed */
                /* and return an integer */
                case UnaryOpType.eUnaryCastToInteger:
                /* FALL THROUGH */

                /* operators which take a boolean, integer, single, double, or fixed */
                /* and return a single */
                case UnaryOpType.eUnaryCastToSingle:
                /* FALL THROUGH */

                /* operators which take a boolean, integer, single, double, or fixed */
                /* and return a double */
                case UnaryOpType.eUnaryCastToDouble:
                /* FALL THROUGH */

                /* operators which take a boolean, integer, single, double, or fixed */
                /* and return a boolean */
                case UnaryOpType.eUnaryCastToBoolean:
                    if (!IsItAScalarType(ArgumentType))
                    {
                        ErrorLineNumber = UnaryOperator.LineNumber;
                        return CompileErrors.eCompileOperandsMustBeScalar;
                    }
                    break;

                /* operators which take an array and return an integer */
                case UnaryOpType.eUnaryGetArrayLength:
                /* FALL THROUGH */

                /* operators which take an array and return an array of the same type */
                case UnaryOpType.eUnaryDuplicateArray:
                    if (!IsItAnIndexedType(ArgumentType))
                    {
                        ErrorLineNumber = UnaryOperator.LineNumber;
                        return CompileErrors.eCompileArrayRequiredForGetLength;
                    }
                    break;
            }

            /* figure out the return type */
            switch (UnaryOperator.Operation)
            {
                default:
                    // unknown opcode
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                /* operators which take an integer, single, double, or fixed and */
                /* return a boolean */
                case UnaryOpType.eUnaryTestNegative:
                    if (!IsItASequencedScalarType(ArgumentType))
                    {
                        // arg should be seq scalar but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eBoolean;
                    break;

                /* operators which take an integer, single, double, or fixed and */
                /* return an integer */
                case UnaryOpType.eUnaryGetSign:
                    if (!IsItASequencedScalarType(ArgumentType))
                    {
                        // arg should be seq scalar but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eInteger;
                    break;

                /* operators capable of integer, single, double, fixed arguments */
                /* which return the same type as the argument */
                case UnaryOpType.eUnaryNegation:
                case UnaryOpType.eUnaryAbsoluteValue:
                    if (!IsItASequencedScalarType(ArgumentType))
                    {
                        // arg should be seq scalar but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = ArgumentType;
                    break;

                /* operators capable of boolean or integer arguments which */
                /* return the same type as the argument */
                case UnaryOpType.eUnaryNot:
                    if ((ArgumentType != DataTypes.eBoolean) && (ArgumentType != DataTypes.eInteger))
                    {
                        // arg should be int or bool
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = ArgumentType;
                    break;

                /* operators capable of double arguments which return doubles */
                case UnaryOpType.eUnarySine:
                case UnaryOpType.eUnaryCosine:
                case UnaryOpType.eUnaryTangent:
                case UnaryOpType.eUnaryArcSine:
                case UnaryOpType.eUnaryArcCosine:
                case UnaryOpType.eUnaryArcTangent:
                case UnaryOpType.eUnaryLogarithm:
                case UnaryOpType.eUnaryExponentiation:
                case UnaryOpType.eUnarySquare:
                case UnaryOpType.eUnarySquareRoot:
                    if (ArgumentType != DataTypes.eDouble)
                    {
                        // arg should be double but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eDouble;
                    break;

                /* operands which take a boolean, integer, single, double, or fixed */
                /* and return an integer */
                case UnaryOpType.eUnaryCastToInteger:
                    if (!IsItAScalarType(ArgumentType))
                    {
                        // arg should be scalar but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eInteger;
                    break;

                /* operators which take a boolean, integer, single, double, or fixed */
                /* and return a single */
                case UnaryOpType.eUnaryCastToSingle:
                    if (!IsItAScalarType(ArgumentType))
                    {
                        // arg should be scalar but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eFloat;
                    break;

                /* operators which take a boolean, integer, single, double, or fixed */
                /* and return a double */
                case UnaryOpType.eUnaryCastToDouble:
                    if (!IsItAScalarType(ArgumentType))
                    {
                        // arg should be scalar but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eDouble;
                    break;

                /* operators which take a boolean, integer, single, double, or fixed */
                /* and return a boolean */
                case UnaryOpType.eUnaryCastToBoolean:
                    if (!IsItAScalarType(ArgumentType))
                    {
                        // arg should be scalar but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eBoolean;
                    break;

                /* operators which take an array and return an integer */
                case UnaryOpType.eUnaryGetArrayLength:
                    if (!IsItAnIndexedType(ArgumentType))
                    {
                        // arg should be indexable but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = DataTypes.eInteger;
                    break;

                /* operators which take an array and return an array of the same type */
                case UnaryOpType.eUnaryDuplicateArray:
                    if (!IsItAnIndexedType(ArgumentType))
                    {
                        // arg should be indexable but isn't
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ResultingDataType = ArgumentType;
                    break;
            }

            return CompileErrors.eCompileNoError;
        }

        /* generate code for a unary operator. returns True if successful, or False if it fails. */
        public static void CodeGenUnaryOperator(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTUnaryOpRec UnaryOperator)
        {
            Pcodes Opcode;

            int StackDepth = StackDepthParam;

            /* evaluate the argument */
            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                UnaryOperator.Argument);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 1)
            {
                // stack depth error after evaluating argument
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* generate the operation code */
            switch (UnaryOperator.Operation)
            {
                default:
                    // bad opcode
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case UnaryOpType.eUnaryNegation:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryNot:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnarySine:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnarySine]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleSin;
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryCosine:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryCosine]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleCos;
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryTangent:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryTangent]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleTan;
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryArcSine:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryArcSine]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleAsin;
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryArcCosine:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryArcCosine]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleAcos;
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryArcTangent:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryArcTangent]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleAtan;
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryLogarithm:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryLogarithm]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleLn;
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryExponentiation:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryExponentiation]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleExp;
                            break;
                    }
                    break;

                case UnaryOpType.eUnarySquare:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnarySquare]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleSqr;
                            break;
                    }
                    break;

                case UnaryOpType.eUnarySquareRoot:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnarySquareRoot]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            Opcode = Pcodes.epOperationDoubleSqrt;
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryCastToBoolean:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryCastToInteger:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryCastToSingle:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryCastToDouble:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryAbsoluteValue:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryTestNegative:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryGetSign:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryGetArrayLength:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryDuplicateArray:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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
                FuncCode.AddPcodeInstruction(Opcode, out unused, UnaryOperator.LineNumber);
            }

            StackDepthParam = StackDepth;
        }

#if true // TODO:experimental
        public static void ILGenUnaryOperator(
            CILObject cilObject,
            ILGenContext context,
            ASTUnaryOpRec UnaryOperator)
        {
            /* evaluate the argument */
            ILGenExpression(
                cilObject,
                context,
                UnaryOperator.Argument);

            /* generate the operation code */
            switch (UnaryOperator.Operation)
            {
                default:
                    // bad opcode
                    Debug.Assert(false);
                    throw new InvalidOperationException();

                case UnaryOpType.eUnaryNegation:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryNot:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnarySine:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnarySine]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sin", new Type[] { typeof(double) }));
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryCosine:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryCosine]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Cos", new Type[] { typeof(double) }));
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryTangent:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryTangent]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Tan", new Type[] { typeof(double) }));
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryArcSine:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryArcSine]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Asin", new Type[] { typeof(double) }));
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryArcCosine:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryArcCosine]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Acos", new Type[] { typeof(double) }));
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryArcTangent:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryArcTangent]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Atan", new Type[] { typeof(double) }));
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryLogarithm:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryLogarithm]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Log", new Type[] { typeof(double) }));
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryExponentiation:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryExponentiation]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Exp", new Type[] { typeof(double) }));
                            break;
                    }
                    break;

                case UnaryOpType.eUnarySquare:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnarySquare]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Dup);
                            context.ilGenerator.Emit(OpCodes.Mul);
                            break;
                    }
                    break;

                case UnaryOpType.eUnarySquareRoot:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnarySquareRoot]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eDouble:
                            context.ilGenerator.Emit(OpCodes.Call, typeof(Math).GetMethod("Sqrt", new Type[] { typeof(double) }));
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryCastToBoolean:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryCastToInteger:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryCastToSingle:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryCastToDouble:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryAbsoluteValue:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryTestNegative:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryGetSign:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
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

                case UnaryOpType.eUnaryGetArrayLength:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryGetArrayLength]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eArrayOfBoolean:
                        case DataTypes.eArrayOfByte:
                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleByte).GetField("bytes"));
                            context.ilGenerator.Emit(OpCodes.Ldlen);
                            break;
                        case DataTypes.eArrayOfInteger:
                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleInt32).GetField("ints"));
                            context.ilGenerator.Emit(OpCodes.Ldlen);
                            break;
                        case DataTypes.eArrayOfFloat:
                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleFloat).GetField("floats"));
                            context.ilGenerator.Emit(OpCodes.Ldlen);
                            break;
                        case DataTypes.eArrayOfDouble:
                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleDouble).GetField("doubles"));
                            context.ilGenerator.Emit(OpCodes.Ldlen);
                            break;
                    }
                    break;

                case UnaryOpType.eUnaryDuplicateArray:
                    switch (GetExpressionsResultantType(UnaryOperator.Argument))
                    {
                        default:
                            // CodeGenUnaryOperator [eUnaryDuplicateArray]:  bad type
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case DataTypes.eArrayOfBoolean:
                        case DataTypes.eArrayOfByte:
                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleByte).GetField("bytes"));
                            context.ilGenerator.Emit(OpCodes.Call, typeof(byte[]).GetMethod("Clone"));
                            context.ilGenerator.Emit(OpCodes.Castclass, typeof(byte[]));
                            context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleByte).GetConstructor(new Type[] { typeof(byte[]) }));
                            break;
                        case DataTypes.eArrayOfInteger:
                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleInt32).GetField("ints"));
                            context.ilGenerator.Emit(OpCodes.Call, typeof(int[]).GetMethod("Clone"));
                            context.ilGenerator.Emit(OpCodes.Castclass, typeof(int[]));
                            context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleInt32).GetConstructor(new Type[] { typeof(int[]) }));
                            break;
                        case DataTypes.eArrayOfFloat:
                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleFloat).GetField("floats"));
                            context.ilGenerator.Emit(OpCodes.Call, typeof(float[]).GetMethod("Clone"));
                            context.ilGenerator.Emit(OpCodes.Castclass, typeof(float[]));
                            context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleFloat).GetConstructor(new Type[] { typeof(float[]) }));
                            break;
                        case DataTypes.eArrayOfDouble:
                            context.ilGenerator.Emit(OpCodes.Ldfld, typeof(ArrayHandleDouble).GetField("doubles"));
                            context.ilGenerator.Emit(OpCodes.Call, typeof(double[]).GetMethod("Clone"));
                            context.ilGenerator.Emit(OpCodes.Castclass, typeof(double[]));
                            context.ilGenerator.Emit(OpCodes.Newobj, typeof(ArrayHandleDouble).GetConstructor(new Type[] { typeof(double[]) }));
                            break;
                    }
                    break;
            }
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstUnaryOperator(
            ASTUnaryOpRec UnaryOperator,
            out bool DidSomething,
            out ASTExpressionRec ResultantExpr,
            out DataTypes ResultType)
        {
            bool WeDidSomething;

            ResultantExpr = null;
            ResultType = DataTypes.eInvalidDataType;

            /* fold stuff under us */
            FoldConstExpression(UnaryOperator.Argument, out WeDidSomething);

            /* see if we can do anything with it */
            if (WhatKindOfExpressionIsThis(UnaryOperator.Argument) == ExprTypes.eExprOperand)
            {
                /* we might be able to do something with an operand */
                ASTOperandRec Operand = GetOperandFromExpression(UnaryOperator.Argument);
                switch (OperandWhatIsIt(Operand))
                {
                    default:
                        break;

                    case ASTOperandType.eASTOperandIntegerLiteral:
                        {
                            ASTOperandRec NewOperand = null;
                            int OldValue;

                            OldValue = GetOperandIntegerLiteral(Operand);
                            switch (UnaryOperator.Operation)
                            {
                                default:
                                    // FoldConstUnaryOperator: for integer literal argument, found illegal operator
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case UnaryOpType.eUnaryNegation:
                                    {
                                        int NewValue;

                                        NewValue = -OldValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryNot:
                                    {
                                        int NewValue;

                                        NewValue = ~OldValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToBoolean:
                                    {
                                        bool NewValue;

                                        NewValue = (OldValue != 0);
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToInteger:
                                    {
                                        int NewValue;

                                        NewValue = OldValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToSingle:
                                    {
                                        float NewValue;

                                        NewValue = OldValue;
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToDouble:
                                    {
                                        double NewValue;

                                        NewValue = OldValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryAbsoluteValue:
                                    {
                                        int NewValue;

                                        NewValue = (OldValue >= 0 ? OldValue : -OldValue);
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryTestNegative:
                                    {
                                        bool NewValue;

                                        NewValue = (OldValue < 0);
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryGetSign:
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
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                            }

                            if (NewOperand != null)
                            {
                                /* a conversion occurred */
                                ResultantExpr = NewExprOperand(
                                    NewOperand,
                                    UnaryOperator.LineNumber);
                                WeDidSomething = true;
                            }
                        }
                        break;

                    case ASTOperandType.eASTOperandBooleanLiteral:
                        {
                            ASTOperandRec NewOperand = null;
                            bool OldValue;

                            OldValue = GetOperandBooleanLiteral(Operand);
                            switch (UnaryOperator.Operation)
                            {
                                default:
                                    // FoldConstUnaryOperator: for boolean literal argument, found illegal operator
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case UnaryOpType.eUnaryNot:
                                    {
                                        bool NewValue;

                                        NewValue = !OldValue;
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToBoolean:
                                    {
                                        bool NewValue;

                                        NewValue = OldValue;
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToInteger:
                                    {
                                        int NewValue;

                                        NewValue = (OldValue ? 1 : 0);
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToSingle:
                                    {
                                        float NewValue;

                                        NewValue = (OldValue ? 1 : 0);
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToDouble:
                                    {
                                        double NewValue;

                                        NewValue = (OldValue ? 1 : 0);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                            }

                            if (NewOperand != null)
                            {
                                /* a conversion occurred */
                                ResultantExpr = NewExprOperand(
                                    NewOperand,
                                    UnaryOperator.LineNumber);
                                WeDidSomething = true;
                            }
                        }
                        break;

                    case ASTOperandType.eASTOperandSingleLiteral:
                        {
                            ASTOperandRec NewOperand = null;
                            float OldValue;

                            OldValue = GetOperandSingleLiteral(Operand);
                            switch (UnaryOperator.Operation)
                            {
                                default:
                                    // FoldConstUnaryOperator: for single literal argument, found illegal operator
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case UnaryOpType.eUnaryNegation:
                                    {
                                        float NewValue;

                                        NewValue = -OldValue;
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToBoolean:
                                    {
                                        bool NewValue;

                                        NewValue = (OldValue != 0);
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToInteger:
                                    {
                                        int NewValue;

                                        NewValue = (int)OldValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToSingle:
                                    {
                                        float NewValue;

                                        NewValue = OldValue;
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToDouble:
                                    {
                                        double NewValue;

                                        NewValue = OldValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryAbsoluteValue:
                                    {
                                        float NewValue;

                                        NewValue = (OldValue >= 0 ? OldValue : -OldValue);
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryTestNegative:
                                    {
                                        bool NewValue;

                                        NewValue = (OldValue < 0);
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryGetSign:
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
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                            }

                            if (NewOperand != null)
                            {
                                /* a conversion occurred */
                                ResultantExpr = NewExprOperand(
                                    NewOperand,
                                    UnaryOperator.LineNumber);
                                WeDidSomething = true;
                            }
                        }
                        break;

                    case ASTOperandType.eASTOperandDoubleLiteral:
                        {
                            ASTOperandRec NewOperand = null;
                            double OldValue;

                            OldValue = GetOperandDoubleLiteral(Operand);
                            switch (UnaryOperator.Operation)
                            {
                                default:
                                    // FoldConstUnaryOperator: for double literal argument, found illegal operator
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case UnaryOpType.eUnaryNegation:
                                    {
                                        double NewValue;

                                        NewValue = -OldValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnarySine:
                                    {
                                        double NewValue;

                                        NewValue = Math.Sin(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCosine:
                                    {
                                        double NewValue;

                                        NewValue = Math.Cos(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryTangent:
                                    {
                                        double NewValue;

                                        NewValue = Math.Tan(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryArcSine:
                                    {
                                        double NewValue;

                                        NewValue = Math.Asin(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryArcCosine:
                                    {
                                        double NewValue;

                                        NewValue = Math.Acos(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryArcTangent:
                                    {
                                        double NewValue;

                                        NewValue = Math.Atan(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryLogarithm:
                                    {
                                        double NewValue;

                                        NewValue = Math.Log(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryExponentiation:
                                    {
                                        double NewValue;

                                        NewValue = Math.Exp(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToBoolean:
                                    {
                                        bool NewValue;

                                        NewValue = (OldValue != 0);
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToInteger:
                                    {
                                        int NewValue;

                                        NewValue = (int)OldValue;
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToSingle:
                                    {
                                        float NewValue;

                                        NewValue = (float)OldValue;
                                        ResultType = DataTypes.eFloat;

                                        NewOperand = NewSingleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryCastToDouble:
                                    {
                                        double NewValue;

                                        NewValue = OldValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnarySquare:
                                    {
                                        double NewValue;

                                        NewValue = OldValue * OldValue;
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnarySquareRoot:
                                    {
                                        double NewValue;

                                        NewValue = Math.Sqrt(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryAbsoluteValue:
                                    {
                                        double NewValue;

                                        NewValue = Math.Abs(OldValue);
                                        ResultType = DataTypes.eDouble;

                                        NewOperand = NewDoubleLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryTestNegative:
                                    {
                                        bool NewValue;

                                        NewValue = (OldValue < 0);
                                        ResultType = DataTypes.eBoolean;

                                        NewOperand = NewBooleanLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                                case UnaryOpType.eUnaryGetSign:
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
                                        ResultType = DataTypes.eInteger;

                                        NewOperand = NewIntegerLiteral(
                                            NewValue,
                                            UnaryOperator.LineNumber);
                                    }
                                    break;
                            }

                            if (NewOperand != null)
                            {
                                /* a conversion occurred */
                                ResultantExpr = NewExprOperand(
                                    NewOperand,
                                    UnaryOperator.LineNumber);
                                WeDidSomething = true;
                            }
                        }
                        break;
                }
            }

            DidSomething = WeDidSomething;
        }




        public class ASTVarDeclRec
        {
            public SymbolRec SymbolTableEntry;
            public ASTExpressionRec InitializationExpression;
            public int LineNumber;
        }

        /* allocate a new variable declaration.  if the Initializer expression is NIL, then */
        /* the object is initialized to NIL or zero when it enters scope. */
        public static ASTVarDeclRec NewVariableDeclaration(
            SymbolRec SymbolTableEntry,
            ASTExpressionRec Initializer,
            int LineNumber)
        {
            ASTVarDeclRec VarDecl = new ASTVarDeclRec();

            VarDecl.LineNumber = LineNumber;
            VarDecl.SymbolTableEntry = SymbolTableEntry;
            VarDecl.InitializationExpression = Initializer;

            return VarDecl;
        }

        /* type check the variable declaration node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckVariableDeclaration(
            out DataTypes ResultingDataType,
            ASTVarDeclRec VariableDeclaration,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            if (VariableDeclaration.InitializationExpression == null)
            {
                /* no initializer -- default */
                ResultingDataType = VariableDeclaration.SymbolTableEntry.GetSymbolVariableDataType();
                return CompileErrors.eCompileNoError;
            }
            else
            {
                /* initializer checking */
                DataTypes VariableType = VariableDeclaration.SymbolTableEntry.GetSymbolVariableDataType();
                DataTypes InitializerType;
                Error = TypeCheckExpression(out InitializerType,
                    VariableDeclaration.InitializationExpression, out ErrorLineNumber);
                if (Error != CompileErrors.eCompileNoError)
                {
                    return Error;
                }
                if (!CanRightBeMadeToMatchLeft(VariableType, InitializerType))
                {
                    ErrorLineNumber = VariableDeclaration.LineNumber;
                    return CompileErrors.eCompileTypeMismatchInAssignment;
                }
                if (MustRightBePromotedToLeft(VariableType, InitializerType))
                {
                    /* promote the initializer */
                    ASTExpressionRec PromotedInitializer = PromoteTheExpression(
                        InitializerType/*orig*/,
                        VariableType/*desired*/,
                        VariableDeclaration.InitializationExpression,
                        VariableDeclaration.LineNumber);
                    VariableDeclaration.InitializationExpression = PromotedInitializer;
                    /* sanity check */
                    Error = TypeCheckExpression(
                        out InitializerType/*obtain new right type*/,
                        VariableDeclaration.InitializationExpression,
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
                return CompileErrors.eCompileNoError;
            }
        }

        /* generate code for a variable declaration. returns True if successful, or */
        /* False if it fails. */
        public static void CodeGenVarDecl(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTVarDeclRec VariableDeclaration)
        {
            int unused;

            int StackDepth = StackDepthParam;

            /* evaluate the value initializer expression if there is one */
            if (VariableDeclaration.InitializationExpression != null)
            {
                /* there's a true initializer */
                CodeGenExpression(
                    FuncCode,
                    ref StackDepth,
                    ref MaxStackDepth,
                    VariableDeclaration.InitializationExpression);
                Debug.Assert(StackDepth <= MaxStackDepth);
            }
            else
            {
                /* generate the default zero value for this type */
                switch (VariableDeclaration.SymbolTableEntry.GetSymbolVariableDataType())
                {
                    default:
                        // bad variable type
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                    case DataTypes.eInteger:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateInteger, out unused, VariableDeclaration.LineNumber);
                        FuncCode.AddPcodeOperandInteger(0);
                        break;
                    case DataTypes.eFloat:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateFloat, out unused, VariableDeclaration.LineNumber);
                        FuncCode.AddPcodeOperandFloat(0);
                        break;
                    case DataTypes.eDouble:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateDouble, out unused, VariableDeclaration.LineNumber);
                        FuncCode.AddPcodeOperandDouble(0);
                        break;
                    case DataTypes.eArrayOfBoolean:
                    case DataTypes.eArrayOfByte:
                    case DataTypes.eArrayOfInteger:
                    case DataTypes.eArrayOfFloat:
                    case DataTypes.eArrayOfDouble:
                        FuncCode.AddPcodeInstruction(Pcodes.epLoadImmediateNILArray, out unused, VariableDeclaration.LineNumber);
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
            VariableDeclaration.SymbolTableEntry.SetSymbolVariableStackLocation(StackDepth);

            /* duplicate the value so there's something to return */
            FuncCode.AddPcodeInstruction(Pcodes.epDuplicate, out unused, VariableDeclaration.LineNumber);
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

#if true // TODO:experimental
        public static void ILGenVarDecl(
            CILObject cilObject,
            ILGenContext context,
            ASTVarDeclRec VariableDeclaration)
        {
            Debug.Assert(!context.variableTable.ContainsKey(VariableDeclaration.SymbolTableEntry));
            LocalBuilder localVariable = context.ilGenerator.DeclareLocal(
                CILObject.GetManagedType(VariableDeclaration.SymbolTableEntry.GetSymbolVariableDataType()));
            context.variableTable.Add(VariableDeclaration.SymbolTableEntry, localVariable);

            /* evaluate the value initializer expression if there is one */
            if (VariableDeclaration.InitializationExpression != null)
            {
                /* there's a true initializer */
                ILGenExpression(
                    cilObject,
                    context,
                    VariableDeclaration.InitializationExpression);
            }
            else
            {
                /* generate the default zero value for this type */
                switch (VariableDeclaration.SymbolTableEntry.GetSymbolVariableDataType())
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
                    case DataTypes.eArrayOfInteger:
                    case DataTypes.eArrayOfFloat:
                    case DataTypes.eArrayOfDouble:
                        context.ilGenerator.Emit(OpCodes.Ldnull);
                        break;
                }
            }

            context.ilGenerator.Emit(OpCodes.Dup);
            context.ilGenerator.Emit(OpCodes.Stloc, localVariable);
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstVarDecl(
            ASTVarDeclRec VariableDeclaration,
            out bool DidSomething)
        {
            FoldConstExpression(
                VariableDeclaration.InitializationExpression,
                out DidSomething);
        }




        public enum WaveGetterOp
        {
            eInvalid,

            eWaveGetterSampleLeft,
            eWaveGetterSampleRight,
            eWaveGetterSampleMono,
            eWaveGetterWaveFrames,
            eWaveGetterWaveTables,
            eWaveGetterWaveArray,
        }

        public class ASTWaveGetterRec
        {
            public string SampleName;
            public WaveGetterOp TheOperation;
            public int LineNumber;
        }

        /* create a new AST wave getter form */
        public static ASTWaveGetterRec NewWaveGetter(
            string SampleName,
            WaveGetterOp TheOperation,
            int LineNumber)
        {
            ASTWaveGetterRec WaveGetter = new ASTWaveGetterRec();

            WaveGetter.SampleName = SampleName;
            WaveGetter.TheOperation = TheOperation;
            WaveGetter.LineNumber = LineNumber;

            return WaveGetter;
        }

        /* type check the wave getter node.  this returns eCompileNoError if */
        /* everything is ok, and the appropriate type in *ResultingDataType. */
        public static CompileErrors TypeCheckWaveGetter(
            out DataTypes ResultingDataType,
            ASTWaveGetterRec WaveGetter,
            out int ErrorLineNumber)
        {
            ErrorLineNumber = -1;

            switch (WaveGetter.TheOperation)
            {
                default:
                    // bad type
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case WaveGetterOp.eWaveGetterSampleLeft:
                case WaveGetterOp.eWaveGetterSampleRight:
                case WaveGetterOp.eWaveGetterSampleMono:
                case WaveGetterOp.eWaveGetterWaveArray:
                    ResultingDataType = DataTypes.eArrayOfFloat;
                    break;
                case WaveGetterOp.eWaveGetterWaveFrames:
                case WaveGetterOp.eWaveGetterWaveTables:
                    ResultingDataType = DataTypes.eInteger;
                    break;
            }

            return CompileErrors.eCompileNoError;
        }

        /* generate code for a wave getter.  returns True if successful, or False if it fails. */
        public static void CodeGenWaveGetter(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTExpressionRec OuterExpression,
            ASTWaveGetterRec WaveGetter)
        {
            ASTExpressionRec waveNameExpression = NewExprOperand(
                NewStringLiteral(
                    WaveGetter.SampleName,
                    WaveGetter.LineNumber),
                WaveGetter.LineNumber);
            waveNameExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            string WhichMethod;
            DataTypes WhichReturn;
            switch (WaveGetter.TheOperation)
            {
                default:
                    // bad opcode
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case WaveGetterOp.eWaveGetterSampleLeft:
                    WhichMethod = "GetSampleLeftArray";
                    WhichReturn = DataTypes.eArrayOfFloat;
                    break;
                case WaveGetterOp.eWaveGetterSampleRight:
                    WhichMethod = "GetSampleRightArray";
                    WhichReturn = DataTypes.eArrayOfFloat;
                    break;
                case WaveGetterOp.eWaveGetterSampleMono:
                    WhichMethod = "GetSampleMonoArray";
                    WhichReturn = DataTypes.eArrayOfFloat;
                    break;
                case WaveGetterOp.eWaveGetterWaveFrames:
                    WhichMethod = "GetWaveTableFrames";
                    WhichReturn = DataTypes.eInteger;
                    break;
                case WaveGetterOp.eWaveGetterWaveTables:
                    WhichMethod = "GetWaveTableTables";
                    WhichReturn = DataTypes.eInteger;
                    break;
                case WaveGetterOp.eWaveGetterWaveArray:
                    WhichMethod = "GetWaveTableArray";
                    WhichReturn = DataTypes.eArrayOfFloat;
                    break;
            }
            CodeGenExternCall(
                FuncCode,
                ref StackDepthParam,
                ref MaxStackDepth,
                OuterExpression,
                WhichMethod,
                new ASTExpressionRec[1]
                {
                    waveNameExpression,
                },
                new DataTypes[]
                {
                    DataTypes.eArrayOfByte/*0: name*/,
                },
                WhichReturn);
        }

#if true // TODO:experimental
        public static void ILGenWaveGetter(
            CILObject cilObject,
            ILGenContext context,
            ASTExpressionRec OuterExpression,
            ASTWaveGetterRec WaveGetter)
        {
            ASTExpressionRec waveNameExpression = NewExprOperand(
                NewStringLiteral(
                    WaveGetter.SampleName,
                    WaveGetter.LineNumber),
                WaveGetter.LineNumber);
            waveNameExpression.WhatIsTheExpressionType = DataTypes.eArrayOfByte;

            string WhichMethod;
            DataTypes WhichReturn;
            switch (WaveGetter.TheOperation)
            {
                default:
                    // bad opcode
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case WaveGetterOp.eWaveGetterSampleLeft:
                    WhichMethod = "GetSampleLeftArray";
                    WhichReturn = DataTypes.eArrayOfFloat;
                    break;
                case WaveGetterOp.eWaveGetterSampleRight:
                    WhichMethod = "GetSampleRightArray";
                    WhichReturn = DataTypes.eArrayOfFloat;
                    break;
                case WaveGetterOp.eWaveGetterSampleMono:
                    WhichMethod = "GetSampleMonoArray";
                    WhichReturn = DataTypes.eArrayOfFloat;
                    break;
                case WaveGetterOp.eWaveGetterWaveFrames:
                    WhichMethod = "GetWaveTableFrames";
                    WhichReturn = DataTypes.eInteger;
                    break;
                case WaveGetterOp.eWaveGetterWaveTables:
                    WhichMethod = "GetWaveTableTables";
                    WhichReturn = DataTypes.eInteger;
                    break;
                case WaveGetterOp.eWaveGetterWaveArray:
                    WhichMethod = "GetWaveTableArray";
                    WhichReturn = DataTypes.eArrayOfFloat;
                    break;
            }
            ILGenExternCall(
                cilObject,
                context,
                OuterExpression,
                WhichMethod,
                new ASTExpressionRec[1]
                {
                    waveNameExpression,
                },
                new DataTypes[]
                {
                    DataTypes.eArrayOfByte/*0: name*/,
                },
                WhichReturn);
        }
#endif

        /* fold constants in the AST.  returns True in *DidSomething if it was able to */
        /* make an improvement.  returns False if it ran out of memory. */
        public static void FoldConstWaveGetter(
            ASTWaveGetterRec WaveGetter,
            out bool DidSomething)
        {
            DidSomething = false;
        }




        public class ASTForLoop
        {
            public SymbolRec SymbolTableEntry; // enumeration variable
            public ASTExpressionRec InitializationExpression;
            public ASTExpressionRec WhileExpression;
            public ASTAssignRec IncrementExpression;
            public ASTExpressionRec BodyExpression;
            public int LineNumber;
        }

        public static ASTForLoop NewForLoop(
            SymbolRec SymbolTableEntry,
            ASTExpressionRec InitializationExpression,
            ASTExpressionRec WhileExpression,
            ASTAssignRec IncrementExpression,
            ASTExpressionRec BodyExpression,
            int LineNumber)
        {
            ASTForLoop ForLoop = new ASTForLoop();
            ForLoop.SymbolTableEntry = SymbolTableEntry;
            ForLoop.InitializationExpression = InitializationExpression;
            ForLoop.WhileExpression = WhileExpression;
            ForLoop.IncrementExpression = IncrementExpression;
            ForLoop.BodyExpression = BodyExpression;
            ForLoop.LineNumber = LineNumber;
            return ForLoop;
        }

        public static CompileErrors TypeCheckForLoop(
            out DataTypes ResultingDataType,
            ASTForLoop ForLoop,
            out int ErrorLineNumber)
        {
            CompileErrors Error;

            ErrorLineNumber = -1;
            ResultingDataType = DataTypes.eInvalidDataType;

            DataTypes LoopVariableType = ForLoop.SymbolTableEntry.GetSymbolVariableDataType();
            DataTypes InitializationType;
            Error = TypeCheckExpression(out InitializationType, ForLoop.InitializationExpression, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            if (!IsItASequencedScalarType(InitializationType))
            {
                ErrorLineNumber = ForLoop.InitializationExpression.LineNumber;
                return CompileErrors.eCompileOperandsMustBeSequencedScalar;
            }
            if (!CanRightBeMadeToMatchLeft(LoopVariableType, InitializationType))
            {
                ErrorLineNumber = ForLoop.InitializationExpression.LineNumber;
                return CompileErrors.eCompileTypeMismatch;
            }
            if (MustRightBePromotedToLeft(LoopVariableType, InitializationType))
            {
                /* insert promotion operator above right hand side */
                ASTExpressionRec ReplacementRValue = PromoteTheExpression(
                    LoopVariableType,
                    InitializationType,
                    ForLoop.InitializationExpression,
                    ForLoop.InitializationExpression.LineNumber);
                ForLoop.InitializationExpression = ReplacementRValue;
                /* sanity check */
                Error = TypeCheckExpression(
                    out InitializationType,
                    ForLoop.InitializationExpression,
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
            Error = TypeCheckExpression(out WhileType, ForLoop.WhileExpression, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            if (WhileType != DataTypes.eBoolean)
            {
                ErrorLineNumber = ForLoop.LineNumber;
                return CompileErrors.eCompileTypeMismatch;
            }

            DataTypes IncrementType;
            Error = TypeCheckAssignment(out IncrementType, ForLoop.IncrementExpression, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }
            if (IncrementType != LoopVariableType)
            {
                ErrorLineNumber = ForLoop.LineNumber;
                return CompileErrors.eCompileTypeMismatch;
            }

            DataTypes BodyType;
            Error = TypeCheckExpression(out BodyType, ForLoop.BodyExpression, out ErrorLineNumber);
            if (Error != CompileErrors.eCompileNoError)
            {
                return Error;
            }

            ResultingDataType = LoopVariableType;
            return CompileErrors.eCompileNoError;
        }

        public static void CodeGenForLoop(
            PcodeRec FuncCode,
            ref int StackDepthParam,
            ref int MaxStackDepth,
            ASTForLoop ForLoop)
        {
            int unused;

            int StackDepth = StackDepthParam;

            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                ForLoop.InitializationExpression);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 1)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            ForLoop.SymbolTableEntry.SetSymbolVariableStackLocation(StackDepth);

            int WhileBranchPatchupLocation = -1;
            FuncCode.AddPcodeInstruction(Pcodes.epBranchUnconditional, out WhileBranchPatchupLocation, ForLoop.LineNumber);
            FuncCode.AddPcodeOperandInteger(-1/*target unknown*/);

            int LoopBackAgainLocation = FuncCode.PcodeGetNextAddress();

            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                ForLoop.BodyExpression);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 2)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            FuncCode.AddPcodeInstruction(Pcodes.epStackPop, out unused, ForLoop.BodyExpression.LineNumber);
            StackDepth--;
            if (StackDepth != StackDepthParam + 1)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            CodeGenAssignment(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                ForLoop.IncrementExpression);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 2)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            FuncCode.AddPcodeInstruction(Pcodes.epStackPop, out unused, ForLoop.IncrementExpression.LineNumber);
            StackDepth--;
            if (StackDepth != StackDepthParam + 1)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            FuncCode.ResolvePcodeBranch(WhileBranchPatchupLocation, FuncCode.PcodeGetNextAddress());

            CodeGenExpression(
                FuncCode,
                ref StackDepth,
                ref MaxStackDepth,
                ForLoop.WhileExpression);
            Debug.Assert(StackDepth <= MaxStackDepth);
            if (StackDepth != StackDepthParam + 2)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            FuncCode.AddPcodeInstruction(Pcodes.epBranchIfNotZero, out unused, ForLoop.WhileExpression.LineNumber);
            FuncCode.AddPcodeOperandInteger(LoopBackAgainLocation);
            StackDepth--;
            if (StackDepth != StackDepthParam + 1)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            StackDepthParam = StackDepth;
        }

#if true // TODO:experimental
        public static void ILGenForLoop(
            CILObject cilObject,
            ILGenContext context,
            ASTForLoop ForLoop)
        {
            LocalBuilder loopVariable;
            switch (ForLoop.SymbolTableEntry.GetSymbolVariableDataType())
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
            ILGenExpression(
                cilObject,
                context,
                ForLoop.InitializationExpression);
            context.ilGenerator.Emit(OpCodes.Stloc, loopVariable);

            context.variableTable.Add(ForLoop.SymbolTableEntry, loopVariable);

            Label whileExpr = context.ilGenerator.DefineLabel();
            context.ilGenerator.Emit(OpCodes.Br, whileExpr);

            Label bodyExpr = context.ilGenerator.DefineLabel();
            context.ilGenerator.MarkLabel(bodyExpr);

            ILGenExpression(
                cilObject,
                context,
                ForLoop.BodyExpression);
            context.ilGenerator.Emit(OpCodes.Pop);

            ILGenAssignment(
                cilObject,
                context,
                ForLoop.IncrementExpression);
            context.ilGenerator.Emit(OpCodes.Pop);

            context.ilGenerator.MarkLabel(whileExpr);

            ILGenExpression(
                cilObject,
                context,
                ForLoop.WhileExpression);
            context.ilGenerator.Emit(OpCodes.Ldc_I4_0);
            context.ilGenerator.Emit(OpCodes.Bne_Un, bodyExpr);

            context.ilGenerator.Emit(OpCodes.Ldloc, loopVariable);
        }
#endif

        public static void FoldConstForLoop(
            ASTForLoop ForLoop,
            out bool DidSomething)
        {
            bool DidSomething1;

            DidSomething = false;

            FoldConstExpression(
                ForLoop.InitializationExpression,
                out DidSomething1);
            DidSomething = DidSomething || DidSomething1;

            FoldConstExpression(
                ForLoop.WhileExpression,
                out DidSomething1);
            DidSomething = DidSomething || DidSomething1;

            FoldConstAssignment(
                ForLoop.IncrementExpression,
                out DidSomething1);
            DidSomething = DidSomething || DidSomething1;

            FoldConstExpression(
                ForLoop.BodyExpression,
                out DidSomething1);
            DidSomething = DidSomething || DidSomething1;
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
            return (LeftType == RightType);
        }

        /* this routine sees if the right hand type MUST be promoted to become */
        /* the left hand type.  it is not allowed to call with non-compatible types */
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
            if (LeftType != DataTypes.eDouble)
            {
                // types are not promotable
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if ((RightType != DataTypes.eInteger) && (RightType != DataTypes.eFloat) && (RightType != DataTypes.eDouble))
            {
                // types are not promotable
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            /* we've filtered out all other possibililities, so they must be promoted */
            return true;
        }

        /* perform a type promotion on an expression */
        public static ASTExpressionRec PromoteTheExpression(
            DataTypes OriginalType,
            DataTypes DesiredType,
            ASTExpressionRec OriginalExpression,
            int LineNumber)
        {
            ASTUnaryOpRec UnaryOperator;
            ASTExpressionRec ResultingExpression;

            if (DesiredType != DataTypes.eDouble)
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
                    UnaryOperator = NewUnaryOperator(UnaryOpType.eUnaryCastToDouble, OriginalExpression, LineNumber);
                    ResultingExpression = NewExprUnaryOperator(UnaryOperator, LineNumber);
                    return ResultingExpression;
            }
        }

        /* make sure the type is scalar */
        public static bool IsItAScalarType(DataTypes TheType)
        {
            switch (TheType)
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

        /* make sure it is some kind of sequenced scalar */
        public static bool IsItASequencedScalarType(DataTypes TheType)
        {
            switch (TheType)
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

        /* make sure it is some kind of indexed type */
        public static bool IsItAnIndexedType(DataTypes TheType)
        {
            switch (TheType)
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
