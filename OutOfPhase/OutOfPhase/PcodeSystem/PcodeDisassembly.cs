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
using System.Diagnostics;
using System.Text;

namespace OutOfPhase
{
    public partial class Compiler
    {
        public static string DisassemblePcode(PcodeRec pcode)
        {
            StringBuilder result = new StringBuilder();

            if (pcode.cilObject != null)
            {
                result.Append(pcode.cilObject.GetDisassembly());
            }

            OpcodeRec[] OpcodeArray = pcode.GetOpcodeFromPcode();
            int c = pcode.GetNumberOfValidCellsInPcode();
            int i = 0;
            while (i < c)
            {
                /* generate instruction index string */
                result.AppendFormat("{0,8}", i);

                /* generate line number */
                result.AppendFormat("{0,8} ", pcode.GetLineNumberForInstruction(i));

                /* generate opcode string */
                switch (OpcodeArray[i].Opcode)
                {
                    case Pcodes.epFuncCallUnresolved: /* <opcode> ^"<functionname>" ^[paramlist] <returntype> <reserved> <reserved> */
                        result.Append("call_unlinked ");
                        result.Append(pcode.strings[OpcodeArray[i + 1].ImmediateString_Ref]);
                        i += 6;
                        break;
                    case Pcodes.epFuncCallResolved: /* <opcode> ^"<functionname>" ^[paramlist] <returntype> ^<OpcodeRec> <maxstack> */
                        result.Append("call_linked ");
                        result.Append(pcode.strings[OpcodeArray[i + 1].ImmediateString_Ref]);
                        i += 6;
                        break;
                    case Pcodes.epFuncCallExternal: /* <opcode> ^"<methodname>" ^[paramlist] <returntype> */
                        result.Append("callextern ");
                        result.Append(pcode.strings[OpcodeArray[i + 1].ImmediateString_Ref]);
                        i += 4;
                        break;

                    case Pcodes.epOperationBooleanEqual: /* <opcode> */
                        result.Append("eq.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanNotEqual:
                        result.Append("neq.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanAnd:
                        result.Append("and.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanOr:
                        result.Append("or.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanNot:
                        result.Append("not.b");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanToInteger:
                        result.Append("booltoint");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanToFloat:
                        result.Append("booltofloat");
                        i++;
                        break;
                    case Pcodes.epOperationBooleanToDouble:
                        result.Append("booltodouble");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerAdd:
                        result.Append("add.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerSubtract:
                        result.Append("sub.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerNegation:
                        result.Append("neg.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerMultiply:
                        result.Append("mult.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerDivide:
                        result.Append("div.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerModulo:
                        result.Append("mod.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerShiftLeft:
                        result.Append("asl.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerShiftRight:
                        result.Append("asr.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerGreaterThan:
                        result.Append("gr.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerLessThan:
                        result.Append("ls.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerGreaterThanOrEqual:
                        result.Append("greq.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerLessThanOrEqual:
                        result.Append("lseq.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerEqual:
                        result.Append("eq.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerNotEqual:
                        result.Append("neq.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerAbs:
                        result.Append("abs.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerToBoolean:
                        result.Append("inttobool");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerToFloat:
                        result.Append("inttofloat");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerToDouble:
                        result.Append("inttodouble");
                        i++;
                        break;
                    case Pcodes.epOperationFloatAdd:
                        result.Append("add.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatSubtract:
                        result.Append("sub.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatNegation:
                        result.Append("neg.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatMultiply:
                        result.Append("mult.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatDivide:
                        result.Append("div.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatGreaterThan:
                        result.Append("gr.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatLessThan:
                        result.Append("ls.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatGreaterThanOrEqual:
                        result.Append("greq.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatLessThanOrEqual:
                        result.Append("lseq.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatEqual:
                        result.Append("eq.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatNotEqual:
                        result.Append("neq.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatAbs:
                        result.Append("abs.s");
                        i++;
                        break;
                    case Pcodes.epOperationFloatToBoolean:
                        result.Append("floattobool");
                        i++;
                        break;
                    case Pcodes.epOperationFloatToInteger:
                        result.Append("floattoint");
                        i++;
                        break;
                    case Pcodes.epOperationFloatToDouble:
                        result.Append("floattodouble");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAdd:
                        result.Append("add.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSubtract:
                        result.Append("sub.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleNegation:
                        result.Append("neg.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleMultiply:
                        result.Append("mult.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleDivide:
                        result.Append("div.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleGreaterThan:
                        result.Append("gr.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleLessThan:
                        result.Append("ls.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleGreaterThanOrEqual:
                        result.Append("greq.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleLessThanOrEqual:
                        result.Append("lseq.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleEqual:
                        result.Append("eq.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleNotEqual:
                        result.Append("neq.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAbs:
                        result.Append("abs.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleToBoolean:
                        result.Append("doubletobool");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleToInteger:
                        result.Append("doubletoint");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleToFloat:
                        result.Append("doubletofloat");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSinF:
                        result.Append("sin.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSinD:
                        result.Append("sin.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCosF:
                        result.Append("cos.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCosD:
                        result.Append("cos.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleTanF:
                        result.Append("tan.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleTanD:
                        result.Append("tan.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAtanF:
                        result.Append("atan.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAtanD:
                        result.Append("atan.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleLnF:
                        result.Append("ln.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleLnD:
                        result.Append("ln.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleExpF:
                        result.Append("exp.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleExpD:
                        result.Append("exp.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSqrtF:
                        result.Append("sqrt.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSqrtD:
                        result.Append("sqrt.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleFloorF:
                        result.Append("floor.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleFloorD:
                        result.Append("floor.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCeilF:
                        result.Append("ceil.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCeilD:
                        result.Append("ceil.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleRoundF:
                        result.Append("round.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleRoundD:
                        result.Append("round.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCoshF:
                        result.Append("cosh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleCoshD:
                        result.Append("cosh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSinhF:
                        result.Append("sinh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSinhD:
                        result.Append("sinh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleTanhF:
                        result.Append("tanh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleTanhD:
                        result.Append("tanh.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoublePowerF:
                        result.Append("pow.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoublePowerD:
                        result.Append("pow.d");
                        i++;
                        break;
                    case Pcodes.epGetByteArraySize: /* <opcode> */
                        result.Append("arraysize.b");
                        i++;
                        break;
                    case Pcodes.epGetIntegerArraySize:
                        result.Append("arraysize.i");
                        i++;
                        break;
                    case Pcodes.epGetFloatArraySize:
                        result.Append("arraysize.s");
                        i++;
                        break;
                    case Pcodes.epGetDoubleArraySize:
                        result.Append("arraysize.d");
                        i++;
                        break;
                    case Pcodes.epReturnFromSubroutine: /* <opcode> <argcount> */
                        result.Append("return ");
                        result.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadImmediateNILArrayByte: /* <opcode> */
                        result.Append("loadnullarray.b");
                        i++;
                        break;
                    case Pcodes.epLoadImmediateNILArrayInt32: /* <opcode> */
                        result.Append("loadnullarray.i");
                        i++;
                        break;
                    case Pcodes.epLoadImmediateNILArrayFloat: /* <opcode> */
                        result.Append("loadnullarray.s");
                        i++;
                        break;
                    case Pcodes.epLoadImmediateNILArrayDouble: /* <opcode> */
                        result.Append("loadnullarray.d");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerAnd:
                        result.Append("and.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerOr:
                        result.Append("or.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerXor:
                        result.Append("xor.i");
                        i++;
                        break;
                    case Pcodes.epOperationIntegerNot:
                        result.Append("not.i");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAsinF:
                        result.Append("asin.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAsinD:
                        result.Append("asin.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAcosF:
                        result.Append("acos.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleAcosD:
                        result.Append("acos.d");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSqrF:
                        result.Append("sqr.s");
                        i++;
                        break;
                    case Pcodes.epOperationDoubleSqrD:
                        result.Append("sqr.d");
                        i++;
                        break;
                    case Pcodes.epOperationTestIntegerNegative:
                        result.Append("isneg.i");
                        i++;
                        break;
                    case Pcodes.epOperationTestFloatNegative:
                        result.Append("isneg.s");
                        i++;
                        break;
                    case Pcodes.epOperationTestDoubleNegative:
                        result.Append("isneg.d");
                        i++;
                        break;
                    case Pcodes.epOperationGetSignInteger:
                        result.Append("sign.i");
                        i++;
                        break;
                    case Pcodes.epOperationGetSignFloat:
                        result.Append("sign.s");
                        i++;
                        break;
                    case Pcodes.epOperationGetSignDouble:
                        result.Append("sign.d");
                        i++;
                        break;
                    case Pcodes.epCopyArrayByte:
                        result.Append("copyarray.b");
                        i++;
                        break;
                    case Pcodes.epCopyArrayInteger:
                        result.Append("copyarray.i");
                        i++;
                        break;
                    case Pcodes.epCopyArrayFloat:
                        result.Append("copyarray.s");
                        i++;
                        break;
                    case Pcodes.epCopyArrayDouble:
                        result.Append("copyarray.d");
                        i++;
                        break;

                    case Pcodes.epStackPop: /* <opcode> */
                        result.Append("pop");
                        i++;
                        break;
                    case Pcodes.epStackPopMultipleUnder: /* <opcode> <numwords> */
                        result.Append("popmultipleunder ");
                        result.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epDuplicate: /* <opcode> */
                        result.Append("dup");
                        i++;
                        break;

                    case Pcodes.epNop: /* <opcode> */
                        result.Append("nop");
                        i++;
                        break;

                    case Pcodes.epBranchUnconditional: /* <opcode> <branchoffset> */
                        result.Append("bra ");
                        result.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epBranchIfZero:
                        result.Append("brz ");
                        result.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epBranchIfNotZero:
                        result.Append("brnz ");
                        result.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;

                    case Pcodes.epResizeByteArray2: /* <opcode> */
                        result.Append("resize.b");
                        i++;
                        break;
                    case Pcodes.epResizeIntegerArray2:
                        result.Append("resize.i");
                        i++;
                        break;
                    case Pcodes.epResizeFloatArray2:
                        result.Append("resize.s");
                        i++;
                        break;
                    case Pcodes.epResizeDoubleArray2:
                        result.Append("resize.d");
                        i++;
                        break;

                    case Pcodes.epStoreIntegerOnStack: /* <opcode> <stackindex> */
                        AppendStack(result, "store.i", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreFloatOnStack:
                        AppendStack(result, "store.s", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreDoubleOnStack:
                        AppendStack(result, "store.d", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreArrayOfByteOnStack:
                        AppendStack(result, "storea.b", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreArrayOfInt32OnStack:
                        AppendStack(result, "storea.i", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreArrayOfFloatOnStack:
                        AppendStack(result, "storea.f", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epStoreArrayOfDoubleOnStack:
                        AppendStack(result, "storea.d", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadIntegerFromStack:
                        AppendStack(result, "load.i", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadFloatFromStack:
                        AppendStack(result, "load.s", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadDoubleFromStack:
                        AppendStack(result, "load.d", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadArrayFromStack:
                        AppendStack(result, "load.a", OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;

                    case Pcodes.epMakeByteArray: /* <opcode> */
                        result.Append("newarray.b");
                        i++;
                        break;
                    case Pcodes.epMakeIntegerArray:
                        result.Append("newarray.i");
                        i++;
                        break;
                    case Pcodes.epMakeFloatArray:
                        result.Append("newarray.s");
                        i++;
                        break;
                    case Pcodes.epMakeDoubleArray:
                        result.Append("newarray.d");
                        i++;
                        break;

                    case Pcodes.epStoreByteIntoArray2: /* <opcode> */
                        result.Append("store.b Array[]");
                        i++;
                        break;
                    case Pcodes.epStoreIntegerIntoArray2:
                        result.Append("store.i Array[]");
                        i++;
                        break;
                    case Pcodes.epStoreFloatIntoArray2:
                        result.Append("store.s Array[]");
                        i++;
                        break;
                    case Pcodes.epStoreDoubleIntoArray2:
                        result.Append("store.d Array[]");
                        i++;
                        break;
                    case Pcodes.epLoadByteFromArray2: /* <opcode> */
                        result.Append("load.b Array[]");
                        i++;
                        break;
                    case Pcodes.epLoadIntegerFromArray2:
                        result.Append("load.i Array[]");
                        i++;
                        break;
                    case Pcodes.epLoadFloatFromArray2:
                        result.Append("load.s Array[]");
                        i++;
                        break;
                    case Pcodes.epLoadDoubleFromArray2:
                        result.Append("load.d Array[]");
                        i++;
                        break;

                    case Pcodes.epLoadImmediateInteger: /* <opcode> <integer>; also used for boolean & fixed */
                        result.Append("load.i #");
                        result.Append(OpcodeArray[i + 1].ImmediateInteger);
                        i += 2;
                        break;
                    case Pcodes.epLoadImmediateFloat: /* <opcode> ^<float> */
                        result.Append("load.s #");
                        result.Append(OpcodeArray[i + 1].ImmediateFloat);
                        i += 2;
                        break;

                    case Pcodes.epLoadImmediateDouble: /* <opcode> ^<double> */
                        result.Append("load.d #");
                        result.Append(pcode.doubles[OpcodeArray[i + 1].ImmediateDouble_Ref]);
                        i += 2;
                        break;

                    case Pcodes.epMakeByteArrayFromString: /* <opcode> ^"<data>" */
                        result.Append("newarraydata.b ");
                        result.Append("\x22");
                        result.Append(pcode.strings[OpcodeArray[i + 1].ImmediateString_Ref]);
                        result.Append("\x22");
                        i += 2;
                        break;

                    case Pcodes.epMinInt:
                        result.Append("min.i");
                        i++;
                        break;
                    case Pcodes.epMinFloat:
                        result.Append("min.s");
                        i++;
                        break;
                    case Pcodes.epMinDouble:
                        result.Append("min.d");
                        i++;
                        break;
                    case Pcodes.epMaxInt:
                        result.Append("max.i");
                        i++;
                        break;
                    case Pcodes.epMaxFloat:
                        result.Append("max.s");
                        i++;
                        break;
                    case Pcodes.epMaxDouble:
                        result.Append("max.d");
                        i++;
                        break;
                    case Pcodes.epMinMaxInt:
                        result.Append("minmax.i");
                        i++;
                        break;
                    case Pcodes.epMinMaxFloat:
                        result.Append("minmax.s");
                        i++;
                        break;
                    case Pcodes.epMinMaxDouble:
                        result.Append("minmax.d");
                        i++;
                        break;
                    case Pcodes.epAtan2Float:
                        result.Append("atan2.s");
                        i++;
                        break;
                    case Pcodes.epAtan2Double:
                        result.Append("atan2.d");
                        i++;
                        break;

                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        private static void AppendStack(StringBuilder sb, string s, int immediate)
        {
            sb.AppendFormat("{0} [{1}]", s, immediate);
        }
    }
}
