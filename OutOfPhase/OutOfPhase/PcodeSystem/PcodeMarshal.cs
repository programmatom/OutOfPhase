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

namespace OutOfPhase
{
    public static class PcodeMarshal
    {
        // pcode-managed interop helpers

        public static EvalErrors TypeCheckValue(object value, DataTypes type)
        {
            switch (type)
            {
                default:
                    Debug.Assert(false); // an unknown data type is a bug on our caller's part
                    return EvalErrors.eEvalFunctionSignatureMismatch;
                case DataTypes.eBoolean:
                    if (!(value is Boolean))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eInteger:
                    if (!(value is Int32))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eFloat:
                    if (!(value is Single))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eDouble:
                    if (!(value is Double))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                    if ((value != null) && !(value is ArrayHandleByte))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eArrayOfInteger:
                    if ((value != null) && !(value is ArrayHandleInt32))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eArrayOfFloat:
                    if ((value != null) && !(value is ArrayHandleFloat))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
                case DataTypes.eArrayOfDouble:
                    if ((value != null) && !(value is ArrayHandleDouble))
                    {
                        return EvalErrors.eEvalFunctionSignatureMismatch;
                    }
                    break;
            }
            return EvalErrors.eEvalNoError;
        }

        private static EvalErrors TypeCheckSignature(object[] args, DataTypes[] argsTypes)
        {
            int argsLength = args != null ? args.Length : 0;
            if (argsLength != argsTypes.Length)
            {
                return EvalErrors.eEvalFunctionSignatureMismatch;
            }
            for (int i = 0; i < argsLength; i++)
            {
                EvalErrors error = TypeCheckValue(args[i], argsTypes[i]);
                if (error != EvalErrors.eEvalNoError)
                {
                    return error;
                }
            }
            return EvalErrors.eEvalNoError;
        }

        public static void MarshalToManaged(ref StackElement value, DataTypes type, out object managed)
        {
            switch (type)
            {
                default:
                    Debug.Assert(false); // an unknown data type is a bug on our caller's part
                    throw new ArgumentException();
                case DataTypes.eBoolean:
                    managed = value.Data.Integer != 0;
                    break;
                case DataTypes.eInteger:
                    managed = value.Data.Integer;
                    break;
                case DataTypes.eFloat:
                    managed = value.Data.Float;
                    break;
                case DataTypes.eDouble:
                    managed = value.Data.Double;
                    break;
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                    managed = value.reference.arrayHandleByte;
                    break;
                case DataTypes.eArrayOfInteger:
                    managed = value.reference.arrayHandleInt32;
                    break;
                case DataTypes.eArrayOfFloat:
                    managed = value.reference.arrayHandleFloat;
                    break;
                case DataTypes.eArrayOfDouble:
                    managed = value.reference.arrayHandleDouble;
                    break;
            }
        }

        public static void MarshalToPcode(object value, ref StackElement pcode, DataTypes type)
        {
#if DEBUG
            pcode.AssertClear();
#endif
            switch (type)
            {
                default:
                    Debug.Assert(false); // an unknown data type is a bug on our caller's part
                    throw new ArgumentException();
                case DataTypes.eBoolean:
                    pcode.Data.Integer = (bool)value ? 1 : 0;
                    break;
                case DataTypes.eInteger:
                    pcode.Data.Integer = (int)value;
                    break;
                case DataTypes.eFloat:
                    pcode.Data.Float = (float)value;
                    break;
                case DataTypes.eDouble:
                    pcode.Data.Double = (double)value;
                    break;
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                    pcode.reference.arrayHandleByte = (ArrayHandleByte)value;
                    break;
                case DataTypes.eArrayOfInteger:
                    pcode.reference.arrayHandleInt32 = (ArrayHandleInt32)value;
                    break;
                case DataTypes.eArrayOfFloat:
                    pcode.reference.arrayHandleFloat = (ArrayHandleFloat)value;
                    break;
                case DataTypes.eArrayOfDouble:
                    pcode.reference.arrayHandleDouble = (ArrayHandleDouble)value;
                    break;
            }
        }

        public static Type GetManagedType(DataTypes type)
        {
            switch (type)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case DataTypes.eBoolean:
                    return typeof(Boolean);
                case DataTypes.eInteger:
                    return typeof(Int32);
                case DataTypes.eFloat:
                    return typeof(Single);
                case DataTypes.eDouble:
                    return typeof(Double);
                case DataTypes.eArrayOfBoolean:
                case DataTypes.eArrayOfByte:
                    return typeof(ArrayHandleByte);
                case DataTypes.eArrayOfInteger:
                    return typeof(ArrayHandleInt32);
                case DataTypes.eArrayOfFloat:
                    return typeof(ArrayHandleFloat);
                case DataTypes.eArrayOfDouble:
                    return typeof(ArrayHandleDouble);
            }
        }

        public static void GetManagedFunctionSignature(
            DataTypes[] argsTypes,
            DataTypes returnType,
            out Type[] managedArgsTypes,
            out Type managedReturnType)
        {
            managedArgsTypes = new Type[argsTypes.Length];
            for (int i = 0; i < managedArgsTypes.Length; i++)
            {
                managedArgsTypes[i] = GetManagedType(argsTypes[i]);
            }
            managedReturnType = GetManagedType(returnType);
        }
    }
}
