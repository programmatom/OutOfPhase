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
    public partial class Compiler
    {
        /* disassemble pcode and return a string block containing all the data */
        public static string DisassemblePcode(
            PcodeRec Pcode,
            string CarriageReturn)
        {
            StringBuilder String = new StringBuilder();
            OpcodeRec[] OpcodeArray;

            OpcodeArray = Pcode.GetOpcodeFromPcode();
            int c = Pcode.GetNumberOfValidCellsInPcode();
            int i = 0;
            while (i < c)
            {
                /* generate instruction index string */
                String.AppendFormat("{0,8}", i);

                /* generate line number */
                String.AppendFormat("{0,8} ", Pcode.GetLineNumberForInstruction(i));

                /* generate opcode string */
                switch (OpcodeArray[i].Opcode)
                {
                    case Pcodes.epFuncCallUnresolved: /* <opcode> ^"<functionname>" ^[paramlist] <returntype> <reserved> <reserved> */
                        String.Append("call_unlinked ");
                        String.Append(Pcode.strings[OpcodeArray[i + 1].ImmediateString_Ref]);
                        i += 6;
                        break;
                    case Pcodes.epFuncCallResolved: /* <opcode> ^"<functionname>" ^[paramlist] <returntype> ^<OpcodeRec> <maxstack> */
                        String.Append("call_linked ");
                        String.Append(Pcode.strings[OpcodeArray[i + 1].ImmediateString_Ref]);
                        i += 6;
                        break;
                    case Pcodes.epFuncCallExternal: /* <opcode> ^"<methodname>" ^[paramlist] <returntype> */
                        String.Append("callextern ");
                        String.Append(Pcode.strings[OpcodeArray[i + 1].ImmediateString_Ref]);
                        i += 4;
                        break;

                    case Pcodes.epOperationBooleanEqual: /* <opcode> */
                        String.Append("eq.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanNotEqual:
                        String.Append("neq.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanAnd:
                        String.Append("and.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanOr:
                        String.Append("or.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanNot:
                        String.Append("not.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanToInteger:
                        String.Append("booltoint");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanToFloat:
                        String.Append("booltofloat");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanToDouble:
                        String.Append("booltodouble");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerAdd:
                        String.Append("add.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerSubtract:
                        String.Append("sub.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerNegation:
                        String.Append("neg.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerMultiply:
                        String.Append("mult.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerDivide:
                        String.Append("div.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerModulo:
                        String.Append("mod.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerShiftLeft:
                        String.Append("asl.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerShiftRight:
                        String.Append("asr.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerGreaterThan:
                        String.Append("gr.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerLessThan:
                        String.Append("ls.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerGreaterThanOrEqual:
                        String.Append("greq.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerLessThanOrEqual:
                        String.Append("lseq.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerEqual:
                        String.Append("eq.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerNotEqual:
                        String.Append("neq.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerAbs:
                        String.Append("abs.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerToBoolean:
                        String.Append("inttobool");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerToFloat:
                        String.Append("inttofloat");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerToDouble:
                        String.Append("inttodouble");
                        i++;
                        break;
                    case Pcodes.epOperationFloatAdd:
                        String.Append("add.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatSubtract:
                        String.Append("sub.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatNegation:
                        String.Append("neg.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatMultiply:
                        String.Append("mult.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatDivide:
                        String.Append("div.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatGreaterThan:
                        String.Append("gr.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatLessThan:
                        String.Append("ls.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatGreaterThanOrEqual:
                        String.Append("greq.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatLessThanOrEqual:
                        String.Append("lseq.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatEqual:
                        String.Append("eq.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatNotEqual:
                        String.Append("neq.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatAbs:
                        String.Append("abs.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatToBoolean:
                        String.Append("floattobool");
                        i++;
                        break;
                    case Pcodes.epOperationFloatToInteger:
                        String.Append("floattoint");
                        i++;
                        break;
                    case Pcodes.epOperationFloatToDouble:
                        String.Append("floattodouble");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAdd:
                        String.Append("add.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSubtract:
                        String.Append("sub.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleNegation:
                        String.Append("neg.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleMultiply:
                        String.Append("mult.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleDivide:
                        String.Append("div.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleGreaterThan:
                        String.Append("gr.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleLessThan:
                        String.Append("ls.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleGreaterThanOrEqual:
                        String.Append("greq.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleLessThanOrEqual:
                        String.Append("lseq.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleEqual:
                        String.Append("eq.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleNotEqual:
                        String.Append("neq.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAbs:
                        String.Append("abs.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleToBoolean:
                        String.Append("doubletobool");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleToInteger:
                        String.Append("doubletoint");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleToFloat:
                        String.Append("doubletofloat");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSinF:
                        String.Append("sin.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSinD:
                        String.Append("sin.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCosF:
                        String.Append("cos.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCosD:
                        String.Append("cos.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleTanF:
                        String.Append("tan.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleTanD:
                        String.Append("tan.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAtanF:
                        String.Append("atan.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAtanD:
                        String.Append("atan.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleLnF:
                        String.Append("ln.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleLnD:
                        String.Append("ln.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleExpF:
                        String.Append("exp.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleExpD:
                        String.Append("exp.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSqrtF:
                        String.Append("sqrt.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSqrtD:
                        String.Append("sqrt.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleFloorF:
                        String.Append("floor.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleFloorD:
                        String.Append("floor.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCeilF:
                        String.Append("ceil.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCeilD:
                        String.Append("ceil.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleRoundF:
                        String.Append("round.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleRoundD:
                        String.Append("round.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCoshF:
                        String.Append("cosh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCoshD:
                        String.Append("cosh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSinhF:
                        String.Append("sinh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSinhD:
                        String.Append("sinh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleTanhF:
                        String.Append("tanh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleTanhD:
                        String.Append("tanh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoublePowerF:
                        String.Append("pow.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoublePowerD:
                        String.Append("pow.d");
                        i++;
                        break;
                    case Pcodes.epGetByteArraySize: /* <opcode> */
                        String.Append("arraysize.b");
                        i++;
                        break;
                    case Pcodes.epGetIntegerArraySize:
                        String.Append("arraysize.i");
                        i++;
                        break;
                    case Pcodes.epGetFloatArraySize:
                        String.Append("arraysize.s");
                        i++;
                        break;
                    case Pcodes.epGetDoubleArraySize:
                        String.Append("arraysize.d");
                        i++;
                        break;
                    case Pcodes.epReturnFromSubroutine: /* <opcode> <argcount> */
                        String.Append("return ");
                        String.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadImmediateNILArray: /* <opcode> */
                        String.Append("loadnull");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerAnd:
                        String.Append("and.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerOr:
                        String.Append("or.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerXor:
                        String.Append("xor.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerNot:
                        String.Append("not.i");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAsinF:
                        String.Append("asin.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAsinD:
                        String.Append("asin.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAcosF:
                        String.Append("acos.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAcosD:
                        String.Append("acos.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSqrF:
                        String.Append("sqr.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSqrD:
                        String.Append("sqr.d");
                        i++;
                        break;
                    case Pcodes.epOperationTestIntegerNegative:
                        String.Append("isneg.i");
                        i++;
                        break;
                    case Pcodes.epOperationTestFloatNegative:
                        String.Append("isneg.s");
                        i++;
                        break;
                    case Pcodes.epOperationTestDoubleNegative:
                        String.Append("isneg.d");
                        i++;
                        break;
                    case Pcodes.epOperationGetSignInteger:
                        String.Append("sign.i");
                        i++;
                        break;
                    case Pcodes.epOperationGetSignFloat:
                        String.Append("sign.s");
                        i++;
                        break;
                    case Pcodes.epOperationGetSignDouble:
                        String.Append("sign.d");
                        i++;
                        break;
                    case Pcodes.epCopyArrayByte:
                        String.Append("copyarray.b");
                        i++;
                        break;
                    case Pcodes.epCopyArrayInteger:
                        String.Append("copyarray.i");
                        i++;
                        break;
                    case Pcodes.epCopyArrayFloat:
                        String.Append("copyarray.s");
                        i++;
                        break;
                    case Pcodes.epCopyArrayDouble:
                        String.Append("copyarray.d");
                        i++;
                        break;

                    case Pcodes.epStackPop: /* <opcode> */
                        String.Append("pop");
                        i++;
                        break;
                    case Pcodes.epStackDeallocateUnder: /* <opcode> <numwords> */
                        String.Append("popmultipleunder ");
                        String.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epDuplicate: /* <opcode> */
                        String.Append("dup");
                        i++;
                        break;
                    case Pcodes.epStackPopMultiple: /* <opcode> <numwords> */
                        String.Append("popmultiple ");
                        String.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;

                    case Pcodes.epNop: /* <opcode> */
                        String.Append("nop");
                        i++;
                        break;

                    case Pcodes.epBranchUnconditional: /* <opcode> <branchoffset> */
                        String.Append("bra ");
                        String.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epBranchIfZero:
                        String.Append("brz ");
                        String.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epBranchIfNotZero:
                        String.Append("brnz ");
                        String.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;

                    case Pcodes.epResizeByteArray2: /* <opcode> */
                        String.Append("resize.b");
                        i++;
                        break;
                    case Pcodes.epResizeIntegerArray2:
                        String.Append("resize.i");
                        i++;
                        break;
                    case Pcodes.epResizeFloatArray2:
                        String.Append("resize.s");
                        i++;
                        break;
                    case Pcodes.epResizeDoubleArray2:
                        String.Append("resize.d");
                        i++;
                        break;

                    case Pcodes.epStoreIntegerOnStack: /* <opcode> <stackindex> */
                        AppendStack(String, "store.i", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreFloatOnStack:
                        AppendStack(String, "store.s", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreDoubleOnStack:
                        AppendStack(String, "store.d", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreArrayOfByteOnStack:
                        AppendStack(String, "storea.b", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreArrayOfInt32OnStack:
                        AppendStack(String, "storea.i", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreArrayOfFloatOnStack:
                        AppendStack(String, "storea.f", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreArrayOfDoubleOnStack:
                        AppendStack(String, "storea.d", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadIntegerFromStack:
                        AppendStack(String, "load.i", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadFloatFromStack:
                        AppendStack(String, "load.s", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadDoubleFromStack:
                        AppendStack(String, "load.d", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadArrayFromStack:
                        AppendStack(String, "load.a", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;

                    case Pcodes.epMakeByteArray: /* <opcode> */
                        String.Append("newarray.b");
                        i++;
                        break;
                    case Pcodes.epMakeIntegerArray:
                        String.Append("newarray.i");
                        i++;
                        break;
                    case Pcodes.epMakeFloatArray:
                        String.Append("newarray.s");
                        i++;
                        break;
                    case Pcodes.epMakeDoubleArray:
                        String.Append("newarray.d");
                        i++;
                        break;

                    case Pcodes.epStoreByteIntoArray2: /* <opcode> */
                        String.Append("store.b Array[]");
                        i++;
                        break;
                    case Pcodes.epStoreIntegerIntoArray2:
                        String.Append("store.i Array[]");
                        i++;
                        break;
                    case Pcodes.epStoreFloatIntoArray2:
                        String.Append("store.s Array[]");
                        i++;
                        break;
                    case Pcodes.epStoreDoubleIntoArray2:
                        String.Append("store.d Array[]");
                        i++;
                        break;
                    case Pcodes.epLoadByteFromArray2: /* <opcode> */
                        String.Append("load.b Array[]");
                        i++;
                        break;
                    case Pcodes.epLoadIntegerFromArray2:
                        String.Append("load.i Array[]");
                        i++;
                        break;
                    case Pcodes.epLoadFloatFromArray2:
                        String.Append("load.s Array[]");
                        i++;
                        break;
                    case Pcodes.epLoadDoubleFromArray2:
                        String.Append("load.d Array[]");
                        i++;
                        break;

                    case Pcodes.epLoadImmediateInteger: /* <opcode> <integer>; also used for boolean & fixed */
                        String.Append("load.i #");
                        String.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadImmediateFloat: /* <opcode> ^<float> */
                        String.Append("load.s #");
                        String.Append(OpcodeArray[i + 1].ImmediateFloat);
                        i += 2;
                        break;

                    case Pcodes.epLoadImmediateDouble: /* <opcode> ^<double> */
                        String.Append("load.d #");
                        String.Append(Pcode.doubles[OpcodeArray[i + 1].ImmediateDouble_Ref]);
                        i += 2;
                        break;

                    case Pcodes.epMakeByteArrayFromString: /* <opcode> ^"<data>" */
                        String.Append("newarraydata.b ");
                        String.Append("\x22");
                        String.Append(Pcode.strings[OpcodeArray[i + 1].ImmediateString_Ref]);
                        String.Append("\x22");
                        i += 2;
                        break;

                    case Pcodes.epMinInt:
                        String.Append("min.i");
                        i++;
                        break;
                    case Pcodes.epMinFloat:
                        String.Append("min.s");
                        i++;
                        break;
                    case Pcodes.epMinDouble:
                        String.Append("min.d");
                        i++;
                        break;
                    case Pcodes.epMaxInt:
                        String.Append("max.i");
                        i++;
                        break;
                    case Pcodes.epMaxFloat:
                        String.Append("max.s");
                        i++;
                        break;
                    case Pcodes.epMaxDouble:
                        String.Append("max.d");
                        i++;
                        break;
                    case Pcodes.epMinMaxInt:
                        String.Append("minmax.i");
                        i++;
                        break;
                    case Pcodes.epMinMaxFloat:
                        String.Append("minmax.s");
                        i++;
                        break;
                    case Pcodes.epMinMaxDouble:
                        String.Append("minmax.d");
                        i++;
                        break;
                    case Pcodes.epAtan2Float:
                        String.Append("atan2.s");
                        i++;
                        break;
                    case Pcodes.epAtan2Double:
                        String.Append("atan2.d");
                        i++;
                        break;

                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                }

                /* end of line */
                String.Append(CarriageReturn);
            }

            return String.ToString();
        }

        private static void AppendStack(StringBuilder sb, string s, int immediate)
        {
            sb.AppendFormat("{0} [{1}]", s, immediate);
        }
    }
}
