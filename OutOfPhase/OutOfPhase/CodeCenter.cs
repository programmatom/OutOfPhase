/*
 *  Copyright � 1994-2002, 2015-2016 Thomas R. Lawrence
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
    public class CodeCenterRec
    {
        private readonly List<CodeEntryRec> CodeList = new List<CodeEntryRec>();
        private struct CodeEntryRec
        {
            public object Signature;
            public FuncCodeRec Function;

            public CodeEntryRec(object Signature, FuncCodeRec Function)
            {
                this.Signature = Signature;
                this.Function = Function;
            }
        }

        private KeyValuePair<string, FunctionSignature>[] retainedFunctionSignatures = new KeyValuePair<string,FunctionSignature>[0];

#if true // TODO:experimental
        public readonly ManagedFunctionLinkerRec ManagedFunctionLinker = new ManagedFunctionLinkerRec();
#endif


        /* if we have the pcode, but not the function, then find it */
        /* it returns NIL if the function couldn't be found. */
        public FuncCodeRec GetFunctionFromOpcode(OpcodeRec[] Opcode)
        {
            for (int i = 0; i < CodeList.Count; i++)
            {
                if (CodeList[i].Function.GetFunctionPcode().GetOpcodeFromPcode() == Opcode)
                {
                    return CodeList[i].Function;
                }
            }
            return null;
        }

        /* obtain a handle for the named function */
        public FuncCodeRec ObtainFunctionHandle(string FunctionName)
        {
            for (int i = 0; i < CodeList.Count; i++)
            {
                if (String.Equals(CodeList[i].Function.GetFunctionName(), FunctionName))
                {
                    return CodeList[i].Function;
                }
            }
            return null;
        }

        public int Count { get { return CodeList.Count; } }

        public void FlushAllCompiledFunctions()
        {
            Dictionary<object, bool> signatures = new Dictionary<object, bool>();
            foreach (CodeEntryRec codeEntry in CodeList)
            {
                signatures[codeEntry.Signature] = false;
            }
            foreach (object signature in signatures.Keys)
            {
                FlushModulesCompiledFunctions(signature);
            }
            Debug.Assert(CodeList.Count == 0);
        }

        /* delete all object code from a particular code module & delink references */
        public void FlushModulesCompiledFunctions(object Signature)
        {
            int Limit = CodeList.Count;
            int i = 0;
            while (i < Limit)
            {
                if (CodeList[i].Signature == Signature)
                {
                    /* unlink references to this function */
                    for (int j = 0; j < Limit; j++)
                    {
                        CodeList[j].Function.GetFunctionPcode().PcodeUnlink(
                            CodeList[i].Function.GetFunctionName());
                    }
#if true // TODO:experimental
                    // remove managed reference
                    if (CodeList[i].Function.CILObject != null)
                    {
                        ManagedFunctionLinker.UnlinkFunctionName(CodeList[i].Function.GetFunctionName());
                    }
#endif
                    /* delete the function from the array & adjust Limit (local array size) */
                    CodeList.RemoveAt(i);
                    Limit -= 1;
                }
                else
                {
                    i += 1;
                }
            }
        }

        /* get a list of functions owned by a specified code module */
        public List<FuncCodeRec> GetListOfFunctionsForModule(object Signature)
        {
            List<FuncCodeRec> List = new List<FuncCodeRec>();
            for (int i = 0; i < CodeList.Count; i += 1)
            {
                if (CodeList[i].Signature == Signature)
                {
                    List.Add(CodeList[i].Function);
                }
            }
            return List;
        }

        /* find out if a function with the given name exists */
        public bool CodeCenterHaveThisFunction(string FunctionName)
        {
            for (int Scan = 0; Scan < CodeList.Count; Scan += 1)
            {
                if (String.Equals(CodeList[Scan].Function.GetFunctionName(), FunctionName))
                {
                    /* yup, function's been added before */
                    return true;
                }
            }
            return false;
        }

        /* add this function to the code center.  it better not be in there already */
        public void AddFunctionToCodeCenter(FuncCodeRec TheNewFunction, object Signature)
        {
            if (CodeCenterHaveThisFunction(TheNewFunction.GetFunctionName()))
            {
                // function is already in the database
                Debug.Assert(false);
                throw new ArgumentException();
            }
            CodeList.Add(new CodeEntryRec(Signature, TheNewFunction));
#if true // TODO:experimental
            // add managed reference
            if (TheNewFunction.CILObject != null)
            {
                ManagedFunctionLinker.LinkFunctionName(
                    TheNewFunction.GetFunctionName(),
                    TheNewFunction.CILObject.MethodInfo,
                    TheNewFunction.GetFunctionParameterTypeList(),
                    TheNewFunction.GetFunctionReturnType());
            }
#endif
        }

        public KeyValuePair<string, FunctionSignature>[] RetainedFunctionSignatures
        { get { return retainedFunctionSignatures; } set { retainedFunctionSignatures = value; } }
    }

    public class FunctionSignature
    {
        private readonly DataTypes[] argsTypes;
        private readonly DataTypes returnType;

        public FunctionSignature(DataTypes[] argsTypes, DataTypes returnType)
        {
            this.argsTypes = argsTypes;
            this.returnType = returnType;
        }

        public DataTypes[] ArgsTypes { get { return argsTypes; } }
        public DataTypes ReturnType { get { return returnType; } }

        public static bool Equals(FunctionSignature l, FunctionSignature r)
        {
            if (l.returnType != r.returnType)
            {
                return false;
            }
            if (l.argsTypes.Length != r.argsTypes.Length)
            {
                return false;
            }
            for (int i = 0; i < l.argsTypes.Length; i++)
            {
                if (l.argsTypes[i] != r.argsTypes[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            FunctionSignature r = obj as FunctionSignature;
            if (r == null)
            {
                return false;
            }
            return Equals(this, r);
        }

        public override int GetHashCode()
        {
            int h = returnType.GetHashCode();
            for (int i = 0; i < argsTypes.Length; i++)
            {
                h = h + argsTypes[i].GetHashCode();
            }
            return h;
        }
    }

#if true // TODO:experimental
    public class ManagedFunctionLinkerRec
    {
        private readonly Dictionary<string, int> functionNameMapper = new Dictionary<string, int>();

        private IntPtr[] functionPointers = new IntPtr[0];
        private int[] functionSignatures = new int[0];
        private int functionCount = 0;

        private readonly Dictionary<FunctionSignature, int> functionSignaturesMap = new Dictionary<FunctionSignature, int>();

        public int QueryFunctionIndex(string name)
        {
            int functionIndex;
            if (!functionNameMapper.TryGetValue(name, out functionIndex))
            {
                functionIndex = functionCount;

                functionCount++;
                if (functionPointers.Length < functionCount)
                {
                    Array.Resize(ref functionPointers, 2 * functionCount);
                    Array.Resize(ref functionSignatures, 2 * functionCount);
                }
                Debug.Assert(!(functionPointers.Length < functionCount));
                Debug.Assert(FunctionPointers.Length == functionSignatures.Length);

                functionPointers[functionIndex] = IntPtr.Zero;
                functionSignatures[functionIndex] = -1;

                functionNameMapper.Add(name, functionIndex);
            }
            return functionIndex;
        }

        public int QuerySignatureIndex(DataTypes[] argsTypes, DataTypes returnType)
        {
            FunctionSignature signature = new FunctionSignature(argsTypes, returnType);
            int signatureIndex;
            if (!functionSignaturesMap.TryGetValue(signature, out signatureIndex))
            {
                signatureIndex = functionSignaturesMap.Count; // assign unique number to each added (they're never removed)
                functionSignaturesMap.Add(signature, signatureIndex);
            }
            return signatureIndex;
        }

        public IntPtr[] FunctionPointers { get { return functionPointers; } }
        public int[] FunctionSignatures { get { return functionSignatures; } }

        public void LinkFunctionName(string name, MethodInfo methodInfo, DataTypes[] argsTypes, DataTypes returnType)
        {
            int i = QueryFunctionIndex(name);

            functionPointers[i] = methodInfo.MethodHandle.GetFunctionPointer();
            functionSignatures[i] = QuerySignatureIndex(argsTypes, returnType);
        }

        public void UnlinkFunctionName(string name)
        {
            int i = functionNameMapper[name];

            functionPointers[i] = IntPtr.Zero;
            functionSignatures[i] = -1;
        }
    }
#endif
}
