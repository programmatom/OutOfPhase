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
    public enum DataTypes
    {
        eBoolean,
        eInteger,
        eFloat,
        eDouble,
        eArrayOfBoolean,
        eArrayOfByte,
        eArrayOfInteger,
        eArrayOfFloat,
        eArrayOfDouble,

        eInvalidDataType,
    }

    /* Stack indices are non-positive:  0 is the word on the */
    /* top of the stack and descending values (-1, -2, -3, ...) are words further */
    /* from the top of stack. */
    public enum Pcodes
    {
        epInvalid = 0,

        /* Unlinked function call.  The function's name is known from compile time, but */
        /* the address has not been linked.  The first parameter is the name of the */
        /* function (a string).  The second parameter is the list of types of the */
        /* parameters for the function in question.  The third parameter */
        /* is the return type of the function.  The return address is pushed */
        /* onto the stack */
        epFuncCallUnresolved,
        /* <opcode> ^"<functionname>" ^[paramlist] <returntype> <reserved> <reserved> */

        /* Linked function call.  The opcode epFuncCallUnresolved is converted to this */
        /* when first encountered and successfully linked.  The first parameter is not */
        /* changed.  The second parameter is released and converted to a pointer to */
        /* the appropriate function's PcodeRec.  The third parameter */
        /* is the return type of the function.  The return address is pushed */
        /* onto the stack */
        epFuncCallResolved,
        /* <opcode> ^"<functionname>" ^[paramlist] <returntype> ^<OpcodeRec> <maxstackdepth> */

        /* invoke external environment method. semantics are same as epFuncCallUnresolved. */
        epFuncCallExternal,
        /* <opcode> ^"<methodname>" ^[paramlist] <returntype> */

        /* Branch instructions.  The parameter is the absolute address within the */
        /* current block of code.  Conditional branch instructions test and pop */
        /* the top of stack */
        epBranchUnconditional,  /* <opcode> <branchoffset> */
        epBranchIfZero,
        epBranchIfNotZero,

        /* calling conventions:  first, push a word on to reserve a place for the */
        /* return value.  next, push the parameters on.  then call the function (which */
        /* pushes the return address on).  the function returns and pops the return */
        /* address off.  then the function pops the parameters off from under return addr. */
        /* Return from subroutine.  This instruction marks the end of code.  The */
        /* address to jump to is contained in the top of the stack.  This address is */
        /* popped off the stack and execution resumes at the new address. */
        epReturnFromSubroutine,  /* <opcode> <argcount> */

        /* Intrinsic operators.  for binary operators, the right hand operand is on */
        /* top of the stack, and one word is popped.  for unary operators, no words */
        /* are popped */
        epOperationBooleanEqual,
        epOperationBooleanNotEqual,
        epOperationBooleanAnd,
        epOperationBooleanOr,
        epOperationBooleanNot,
        epOperationBooleanToInteger,
        epOperationBooleanToFloat,
        epOperationBooleanToDouble,
        epOperationIntegerAdd,
        epOperationIntegerSubtract,
        epOperationIntegerNegation,
        epOperationIntegerMultiply,
        epOperationIntegerDivide,
        epOperationIntegerModulo,
        epOperationIntegerShiftLeft,
        epOperationIntegerShiftRight,
        epOperationIntegerGreaterThan,
        epOperationIntegerLessThan,
        epOperationIntegerGreaterThanOrEqual,
        epOperationIntegerLessThanOrEqual,
        epOperationIntegerEqual,
        epOperationIntegerNotEqual,
        epOperationIntegerAbs,
        epOperationIntegerToBoolean,
        epOperationIntegerToFloat,
        epOperationIntegerToDouble,
        epOperationIntegerAnd,
        epOperationIntegerOr,
        epOperationIntegerXor,
        epOperationIntegerNot,
        epOperationTestIntegerNegative,
        epOperationGetSignInteger,
        epOperationFloatAdd,
        epOperationFloatSubtract,
        epOperationFloatNegation,
        epOperationFloatMultiply,
        epOperationFloatDivide,
        epOperationFloatGreaterThan,
        epOperationFloatLessThan,
        epOperationFloatGreaterThanOrEqual,
        epOperationFloatLessThanOrEqual,
        epOperationFloatEqual,
        epOperationFloatNotEqual,
        epOperationFloatAbs,
        epOperationFloatToBoolean,
        epOperationFloatToInteger,
        epOperationFloatToDouble,
        epOperationTestFloatNegative,
        epOperationGetSignFloat,
        epOperationDoubleAdd,
        epOperationDoubleSubtract,
        epOperationDoubleNegation,
        epOperationDoubleMultiply,
        epOperationDoubleDivide,
        epOperationDoubleGreaterThan,
        epOperationDoubleLessThan,
        epOperationDoubleGreaterThanOrEqual,
        epOperationDoubleLessThanOrEqual,
        epOperationDoubleEqual,
        epOperationDoubleNotEqual,
        epOperationDoubleAbs,
        epOperationDoubleToBoolean,
        epOperationDoubleToInteger,
        epOperationDoubleToFloat,
        epOperationDoubleSin,
        epOperationDoubleCos,
        epOperationDoubleTan,
        epOperationDoubleAsin,
        epOperationDoubleAcos,
        epOperationDoubleAtan,
        epOperationDoubleLn,
        epOperationDoubleExp,
        epOperationDoubleSqrt,
        epOperationDoubleSqr,
        epOperationDoublePower,
        epOperationTestDoubleNegative,
        epOperationGetSignDouble,

        /* these obtain the size of the array on top of stack, replacing the array */
        /* with the size value. */
        epGetByteArraySize,  /* <opcode> */
        epGetIntegerArraySize,
        epGetFloatArraySize,
        epGetDoubleArraySize,

        /* array is pushed on stack, then size is pushed on stack.  this resizes */
        /* the array, and pops the new size word. */
        epResizeByteArray2,  /* <opcode> */
        epResizeIntegerArray2,
        epResizeFloatArray2,
        epResizeDoubleArray2,

        /* store values on stack.  the value on top is stored BUT NOT POPPED */
        epStoreIntegerOnStack,  /* <opcode> <stackindex> */
        epStoreFloatOnStack,
        epStoreDoubleOnStack,
        epStoreArrayOfByteOnStack,
        epStoreArrayOfInt32OnStack,
        epStoreArrayOfFloatOnStack,
        epStoreArrayOfDoubleOnStack,
        /* load values.  the value is loaded and pushed onto the stack */
        epLoadIntegerFromStack,
        epLoadFloatFromStack,
        epLoadDoubleFromStack,
        epLoadArrayFromStack,

        /* deallocate one cell from the stack */
        epStackPop,  /* <opcode> */

        /* deallocate several cells from the stack */
        epStackPopMultiple,  /* <opcode> <numstackwords> */

        /* deallocate some cells from underneath the top cell */
        epStackDeallocateUnder,  /* <opcode> <numwords> */

        /* duplicate word on top of stack */
        epDuplicate,  /* <opcode> */

        /* no op */
        epNop,  /* <opcode> */

        /* allocate array on stack; size is on stack to start out with */
        epMakeByteArray,  /* <opcode> */
        epMakeIntegerArray,
        epMakeFloatArray,
        epMakeDoubleArray,

        /* store value into array.  value is pushed on stack, then array ref, then */
        /* array subscript.  ref and subscript are popped; value remains */
        epStoreByteIntoArray2,  /* <opcode> */
        epStoreIntegerIntoArray2,
        epStoreFloatIntoArray2,
        epStoreDoubleIntoArray2,

        /* load from array.  array is on stack, then subscript is on top of that.  both */
        /* are popped and the value is pushed on the stack. */
        epLoadByteFromArray2,  /* <opcode> */
        epLoadIntegerFromArray2,
        epLoadFloatFromArray2,
        epLoadDoubleFromArray2,

        /* load an immediate value on to the stack */
        epLoadImmediateInteger, /* <opcode> <integer>; also used for boolean */
        epLoadImmediateFloat, /* <opcode> ^<float> */
        epLoadImmediateDouble, /* <opcode> ^<double> */
        epLoadImmediateNILArray, /* <opcode> */

        /* new byte array from string */
        epMakeByteArrayFromString,  /* <opcode> ^"<data>" */

        /* duplicate the array on top of stack, pushes the duplicate */
        /* copies raw data, so it works for all data types */
        epCopyArrayByte, /* <opcode> */
        epCopyArrayInteger, /* <opcode> */
        epCopyArrayFloat, /* <opcode> */
        epCopyArrayDouble, /* <opcode> */
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct OpcodeRec
    {
        // inline values
        [FieldOffset(0)]
        public Pcodes Opcode;
        [FieldOffset(0)]
        public float ImmediateFloat;
        [FieldOffset(0)]
        public int ImmediateInteger;
        [FieldOffset(0)]
        public DataTypes DataType;

        // reference to const data
        // (In C, value and reference types in unions could be mixed because the programmer managed
        // disposal of everything. In C#, they can't be mixed because the garbage collector doesn't
        // know whether raw memory is a value (e.g. float) or a pointer. Therefore, long/ref types
        // need to be stored in an auxilliary array and referenced by index so everything is a
        // value type here.)
        [FieldOffset(0)]
        public int ImmediateDouble_Ref;
        [FieldOffset(0)]
        public int ImmediateString_Ref;
        [FieldOffset(0)]
        public int DataTypeArray_Ref;
        [FieldOffset(0)]
        public int FunctionPcodeRecPtr_Ref; // external reference for linked code
    }

    public class PcodeRec
    {
        // either one or the other of these is set, but not both
        private OpcodeRec[] OpcodeArray = null;
        private List<OpcodeRec> OpcodeList = new List<OpcodeRec>();

        private int maxStackDepth = -1;

        // auxilliary constant data (can't inline in OpcodeRec[] since reference and value types can't
        // be overlaid in .NET)
        public double[] doubles = new double[0];
        public string[] strings = new string[0];
        public DataTypes[][] dataTypesArrays = new DataTypes[0][];

        public PcodeRec[] externRefs = new PcodeRec[0];
        private int externRefsLast;

        private List<int> LineNumberArray = new List<int>();

#if true // TODO:experimental
        public CILObject cilObject;
#endif


        // set-once property

        public int MaxStackDepth
        {
            get
            {
                Debug.Assert(maxStackDepth != -1);
                return maxStackDepth;
            }
            set
            {
                Debug.Assert(maxStackDepth == -1);
                Debug.Assert(value >= 0);
                maxStackDepth = value;
            }
        }



        // These appenders are not the most efficient during compilation, but reduce the
        // complexity of code by avoiding the need to convert lists to arrays at some
        // point as preparation for execution.

        private int AppendDoubleRef(double d)
        {
            int i = Array.IndexOf(doubles, d);
            if (i >= 0)
            {
                return i;
            }
            i = doubles.Length;
            Array.Resize(ref doubles, i + 1);
            doubles[i] = d;
            return i;
        }

        private int AppendStringRef(string s)
        {
            int i = Array.IndexOf(strings, s);
            if (i >= 0)
            {
                return i;
            }
            i = strings.Length;
            Array.Resize(ref strings, i + 1);
            strings[i] = s;
            return i;
        }

        private int AppendDataTypesArrayRef(DataTypes[] a)
        {
            for (int j = 0; j < dataTypesArrays.Length; j++)
            {
                bool equal = a.Length == dataTypesArrays[j].Length;
                for (int k = 0; equal && (k < a.Length); k++)
                {
                    equal = a[k] == dataTypesArrays[j][k];
                }
                if (equal)
                {
                    return j;
                }
            }
            int i = dataTypesArrays.Length;
            Array.Resize(ref dataTypesArrays, i + 1);
            dataTypesArrays[i] = a;
            return i;
        }

        // The methods AppendExternRef() and ClearExternRef() can be called from multiple threads during
        // synthesis, so must be single-access.

        public int AppendExternRef(PcodeRec p)
        {
            lock (this)
            {
                while (externRefsLast < externRefs.Length)
                {
                    if (externRefs[externRefsLast] == null)
                    {
                        externRefs[externRefsLast] = p;
                        return externRefsLast++;
                    }
                }
                Array.Resize(ref externRefs, externRefs.Length * 2 + 1);
                externRefs[externRefsLast] = p;
                return externRefsLast++;
            }
        }

        public void ClearExternRef(int i)
        {
            lock (this)
            {
                externRefs[i] = null;
                if (externRefsLast > i)
                {
                    externRefsLast = i;
                }
            }
        }


        // convert to array - the array form is used during execution for speed
        private void ToArray()
        {
            if ((OpcodeArray == null) == (OpcodeList == null))
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if (OpcodeArray == null)
            {
                OpcodeArray = OpcodeList.ToArray();
                OpcodeList = null;
            }
        }

        // convert to List<> - the List<> form is used during compilation and linking for ease of manipulation
        private void ToList()
        {
            if ((OpcodeArray == null) == (OpcodeList == null))
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if (OpcodeList == null)
            {
                OpcodeList = new List<OpcodeRec>(OpcodeArray);
                OpcodeArray = null;
            }
        }

        /* find out what the address of the next instruction will be */
        public int PcodeGetNextAddress()
        {
            ToList();
            return OpcodeList.Count;
        }

        // The method GetOpcodeFromPcode() can be called from multiple threads during
        // synthesis, so must be single-access.

        public OpcodeRec[] GetOpcodeFromPcode()
        {
            lock (this)
            {
                ToArray();
                return OpcodeArray;
            }
        }

        /* get the line number array from a pcode */
        public List<int> GetLineNumberArrayFromPcode()
        {
            return LineNumberArray;
        }

        /* get the number of cells in the array */
        public int GetNumberOfValidCellsInPcode()
        {
            return OpcodeArray != null ? OpcodeArray.Length : OpcodeList.Count;
        }

        /* Add a pcode instruction.  *Index returns the index of the instruction so that */
        /* branches can be patched up.  if Index is NIL, then it won't bother returning */
        /* anything. */
        public void AddPcodeInstruction(Pcodes Opcode, out int Index, int LineNumber)
        {
            ToList();

            Index = OpcodeList.Count;

            OpcodeRec opcodeRec = new OpcodeRec();
            opcodeRec.Opcode = Opcode;
            OpcodeList.Add(opcodeRec);

            LineNumberArray.Add(LineNumber);

            Debug.Assert(OpcodeList.Count == LineNumberArray.Count);
        }

        // add a line number that is a duplicate of the line number of the preceding instruction
        private void ExtendLineNumber()
        {
            // this should never be called for the leading word of an instruction - only for subsequent arguments
            if (LineNumberArray.Count == 0)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            LineNumberArray.Add(LineNumberArray[LineNumberArray.Count - 1]);
        }

        /* add immediate integer operand */
        public void AddPcodeOperandInteger(int ImmediateData)
        {
            ToList();

            OpcodeRec opcodeRec = new OpcodeRec();
            opcodeRec.ImmediateInteger = ImmediateData;
            OpcodeList.Add(opcodeRec);

            ExtendLineNumber();
        }

        /* add immediate float operand */
        public void AddPcodeOperandFloat(float ImmediateData)
        {
            ToList();

            OpcodeRec opcodeRec = new OpcodeRec();
            opcodeRec.ImmediateFloat = ImmediateData;
            OpcodeList.Add(opcodeRec);

            ExtendLineNumber();
        }

        /* add immediate double operand */
        public void AddPcodeOperandDouble(double ImmediateData)
        {
            ToList();

            int NewDouble = AppendDoubleRef(ImmediateData);

            OpcodeRec opcodeRec = new OpcodeRec();
            opcodeRec.ImmediateDouble_Ref = NewDouble;
            OpcodeList.Add(opcodeRec);

            ExtendLineNumber();
        }

        /* add immediate string operand.  string is copied */
        public void AddPcodeOperandString(string String)
        {
            ToList();

            int NewString = AppendStringRef(String);

            OpcodeRec opcodeRec = new OpcodeRec();
            opcodeRec.ImmediateString_Ref = NewString;
            OpcodeList.Add(opcodeRec);

            ExtendLineNumber();
        }

        /* add data type array operand, used for type checking function calls */
        public void AddPcodeOperandDataTypeArray(DataTypes[] DataTypeArray)
        {
            ToList();

            int NewArray = AppendDataTypesArrayRef(DataTypeArray);

            OpcodeRec opcodeRec = new OpcodeRec();
            opcodeRec.DataTypeArray_Ref = NewArray;
            OpcodeList.Add(opcodeRec);

            ExtendLineNumber();
        }

        /* resolve a pcode branch whose destination was not known earlier but now is */
        public void ResolvePcodeBranch(int Where, int Destination)
        {
            ToList();

            if ((OpcodeList[Where].Opcode != Pcodes.epBranchUnconditional)
                && (OpcodeList[Where].Opcode != Pcodes.epBranchIfZero)
                && (OpcodeList[Where].Opcode != Pcodes.epBranchIfNotZero))
            {
                // patching a non-branch instruction
                Debug.Assert(false);
                throw new ArgumentException();
            }

            OpcodeRec opcode = OpcodeList[Where + 1];
            opcode.ImmediateInteger = Destination;
            OpcodeList[Where + 1] = opcode;
        }

        /* unlink references to a function which is soon going to disappear. */
        // Note: the way this is used in the code is potentially prone to growing unboundedly
        // if the application is very long-running and a given opcode array is repeatedly patched for
        // unlinking and relinking without regenerating.
        // [An attempt is made to mitigate by reusing nulled-out slots the next time a link is created.]
        public void PcodeUnlink(string DeadFuncName)
        {
            ToList();

            int i = 0;
            while (i < OpcodeList.Count)
            {
                /* first, see if we have to delink it */
                if (OpcodeList[i].Opcode == Pcodes.epFuncCallResolved)
                {
                    /* this is the one we have to delink. */
                    /*    0              1              2            3            4    */
                    /* <opcode> ^"<functionname>" ^[paramlist] <returntype> ^<OpcodeRec> */
                    /* will be converted to */
                    /* <opcode> ^"<functionname>" ^[paramlist] <returntype> <reserved> */
                    if (String.Equals(strings[OpcodeList[i + 1].ImmediateString_Ref], DeadFuncName))
                    {
                        OpcodeRec opcode;

                        /* if the name is the same, then delink it */

                        opcode = OpcodeList[i];
                        opcode.Opcode = Pcodes.epFuncCallUnresolved;
                        OpcodeList[i] = opcode;

                        // remove opcode reference
                        opcode = OpcodeList[i + 4];
                        ClearExternRef(opcode.FunctionPcodeRecPtr_Ref); // remove reference so GC can reclaim -- CAN'T SHARE REFS!
                        opcode.FunctionPcodeRecPtr_Ref = -1;
                        OpcodeList[i + 4] = opcode;

                        // reset max stack depth
                        opcode = OpcodeList[i + 5];
                        opcode.ImmediateInteger = -1;
                        OpcodeList[i + 5] = opcode;
                    }
                }
                /* now, advance the program counter the right amount */
                i += GetInstructionLength(OpcodeList[i].Opcode);
            }
        }

        /* get the number of words for the specified instruction opcode */
        public static int GetInstructionLength(Pcodes OpcodeWord)
        {
            switch (OpcodeWord)
            {
                case Pcodes.epFuncCallUnresolved: /* <opcode> ^"<functionname>" ^[paramlist] <returntype> <reserved> <reserved> */
                case Pcodes.epFuncCallResolved: /* <opcode> ^"<functionname>" ^[paramlist] <returntype> ^<OpcodeRec> <maxstackdepth> */
                    return 6;
                case Pcodes.epFuncCallExternal: /* <opcode> ^"<methodname>" ^[paramlist] <returntype> */
                    return 4;
                case Pcodes.epOperationBooleanEqual: /* <opcode> */
                case Pcodes.epOperationBooleanNotEqual:
                case Pcodes.epOperationBooleanAnd:
                case Pcodes.epOperationBooleanOr:
                case Pcodes.epOperationBooleanNot:
                case Pcodes.epOperationBooleanToInteger:
                case Pcodes.epOperationBooleanToFloat:
                case Pcodes.epOperationBooleanToDouble:
                case Pcodes.epOperationIntegerAdd:
                case Pcodes.epOperationIntegerSubtract:
                case Pcodes.epOperationIntegerNegation:
                case Pcodes.epOperationIntegerMultiply:
                case Pcodes.epOperationIntegerDivide:
                case Pcodes.epOperationIntegerModulo:
                case Pcodes.epOperationIntegerShiftLeft:
                case Pcodes.epOperationIntegerShiftRight:
                case Pcodes.epOperationIntegerGreaterThan:
                case Pcodes.epOperationIntegerLessThan:
                case Pcodes.epOperationIntegerGreaterThanOrEqual:
                case Pcodes.epOperationIntegerLessThanOrEqual:
                case Pcodes.epOperationIntegerEqual:
                case Pcodes.epOperationIntegerNotEqual:
                case Pcodes.epOperationIntegerAbs:
                case Pcodes.epOperationIntegerToBoolean:
                case Pcodes.epOperationIntegerToFloat:
                case Pcodes.epOperationIntegerToDouble:
                case Pcodes.epOperationFloatAdd:
                case Pcodes.epOperationFloatSubtract:
                case Pcodes.epOperationFloatNegation:
                case Pcodes.epOperationFloatMultiply:
                case Pcodes.epOperationFloatDivide:
                case Pcodes.epOperationFloatGreaterThan:
                case Pcodes.epOperationFloatLessThan:
                case Pcodes.epOperationFloatGreaterThanOrEqual:
                case Pcodes.epOperationFloatLessThanOrEqual:
                case Pcodes.epOperationFloatEqual:
                case Pcodes.epOperationFloatNotEqual:
                case Pcodes.epOperationFloatAbs:
                case Pcodes.epOperationFloatToBoolean:
                case Pcodes.epOperationFloatToInteger:
                case Pcodes.epOperationFloatToDouble:
                case Pcodes.epOperationDoubleAdd:
                case Pcodes.epOperationDoubleSubtract:
                case Pcodes.epOperationDoubleNegation:
                case Pcodes.epOperationDoubleMultiply:
                case Pcodes.epOperationDoubleDivide:
                case Pcodes.epOperationDoubleGreaterThan:
                case Pcodes.epOperationDoubleLessThan:
                case Pcodes.epOperationDoubleGreaterThanOrEqual:
                case Pcodes.epOperationDoubleLessThanOrEqual:
                case Pcodes.epOperationDoubleEqual:
                case Pcodes.epOperationDoubleNotEqual:
                case Pcodes.epOperationDoubleAbs:
                case Pcodes.epOperationDoubleToBoolean:
                case Pcodes.epOperationDoubleToInteger:
                case Pcodes.epOperationDoubleToFloat:
                case Pcodes.epOperationDoubleSin:
                case Pcodes.epOperationDoubleCos:
                case Pcodes.epOperationDoubleTan:
                case Pcodes.epOperationDoubleAtan:
                case Pcodes.epOperationDoubleLn:
                case Pcodes.epOperationDoubleExp:
                case Pcodes.epOperationDoubleSqrt:
                case Pcodes.epOperationDoublePower:
                case Pcodes.epGetByteArraySize: /* <opcode> */
                case Pcodes.epGetIntegerArraySize:
                case Pcodes.epGetFloatArraySize:
                case Pcodes.epGetDoubleArraySize:
                case Pcodes.epLoadImmediateNILArray: /* <opcode> */
                case Pcodes.epMakeByteArray: /* <opcode> */
                case Pcodes.epMakeIntegerArray:
                case Pcodes.epMakeFloatArray:
                case Pcodes.epMakeDoubleArray:
                case Pcodes.epStackPop: /* <opcode> */
                case Pcodes.epDuplicate: /* <opcode> */
                case Pcodes.epNop: /* <opcode> */
                case Pcodes.epResizeByteArray2: /* <opcode> */
                case Pcodes.epResizeIntegerArray2:
                case Pcodes.epResizeFloatArray2:
                case Pcodes.epResizeDoubleArray2:
                case Pcodes.epStoreByteIntoArray2: /* <opcode> */
                case Pcodes.epStoreIntegerIntoArray2:
                case Pcodes.epStoreFloatIntoArray2:
                case Pcodes.epStoreDoubleIntoArray2:
                case Pcodes.epLoadByteFromArray2: /* <opcode> */
                case Pcodes.epLoadIntegerFromArray2:
                case Pcodes.epLoadFloatFromArray2:
                case Pcodes.epLoadDoubleFromArray2:
                case Pcodes.epOperationIntegerAnd:
                case Pcodes.epOperationIntegerOr:
                case Pcodes.epOperationIntegerXor:
                case Pcodes.epOperationIntegerNot:
                case Pcodes.epOperationDoubleAsin:
                case Pcodes.epOperationDoubleAcos:
                case Pcodes.epOperationDoubleSqr:
                case Pcodes.epOperationTestIntegerNegative:
                case Pcodes.epOperationTestFloatNegative:
                case Pcodes.epOperationTestDoubleNegative:
                case Pcodes.epOperationGetSignInteger:
                case Pcodes.epOperationGetSignFloat:
                case Pcodes.epOperationGetSignDouble:
                case Pcodes.epCopyArrayByte: /* <opcode> */
                case Pcodes.epCopyArrayInteger:
                case Pcodes.epCopyArrayFloat:
                case Pcodes.epCopyArrayDouble:
                    return 1;
                case Pcodes.epReturnFromSubroutine: /* <opcode> <argcount> */
                case Pcodes.epStackPopMultiple: /* <opcode> <numwords> */
                case Pcodes.epStackDeallocateUnder: /* <opcode> <numwords> */
                case Pcodes.epBranchUnconditional: /* <opcode> <branchoffset> */
                case Pcodes.epBranchIfZero:
                case Pcodes.epBranchIfNotZero:
                case Pcodes.epStoreIntegerOnStack: /* <opcode> <stackindex> */
                case Pcodes.epStoreFloatOnStack:
                case Pcodes.epStoreDoubleOnStack:
                case Pcodes.epStoreArrayOfByteOnStack:
                case Pcodes.epStoreArrayOfInt32OnStack:
                case Pcodes.epStoreArrayOfFloatOnStack:
                case Pcodes.epStoreArrayOfDoubleOnStack:
                case Pcodes.epLoadIntegerFromStack:
                case Pcodes.epLoadFloatFromStack:
                case Pcodes.epLoadDoubleFromStack:
                case Pcodes.epLoadArrayFromStack:
                case Pcodes.epLoadImmediateInteger: /* <opcode> <integer>; also used for boolean & fixed */
                case Pcodes.epLoadImmediateFloat: /* <opcode> ^<float> */
                case Pcodes.epLoadImmediateDouble: /* <opcode> ^<double> */
                case Pcodes.epMakeByteArrayFromString: /* <opcode> ^"<data>" */
                    return 2;
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
            }
        }

        /* convert instruction index into line number */
        public int GetLineNumberForInstruction(int Index)
        {
            return LineNumberArray[Index];
        }

        public void OptimizePcode()
        {
            ToList();
            PcodePeepholeOptimizer.OptimizePcode(OpcodeList, LineNumberArray);
        }
    }
}
