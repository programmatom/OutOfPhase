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
    public enum SymbolType
    {
        eSymbolUndefined,
        eSymbolVariable,
        eSymbolFunction,
    }

    public class SymbolRec
    {
        private SymbolType Type = SymbolType.eSymbolUndefined;
        private string SymbolName;
        private DataTypes ObjectDataType;
        private SymbolListRec FunctionArgList;
        private int VariableStackAddress = -1; // to aid debugging
        private object Void;

        public SymbolRec(string String)
        {
            this.SymbolName = String;
        }

        public SymbolType WhatIsThisSymbol()
        {
            return Type;
        }

        public string GetSymbolName()
        {
            return SymbolName;
        }

        /* make symbol into a variable symbol */
        public void SymbolBecomeVariable(DataTypes VarType)
        {
            if (Type != SymbolType.eSymbolUndefined)
            {
                // symbol has already been defined
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            Type = SymbolType.eSymbolVariable;
            ObjectDataType = VarType;
        }

        /* make variable know where on the stack it lives */
        public void SetSymbolVariableStackLocation(int StackLoc)
        {
            if (StackLoc < 0)
            {
                // negative index
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (VariableStackAddress >= 0)
            {
                // location has already been set
                Debug.Assert(false);
                throw new ArgumentException();
            }
            VariableStackAddress = StackLoc;
        }

        /* find out where on the stack a variable lives. */
        public int GetSymbolVariableStackLocation()
        {
            if (VariableStackAddress < 0)
            {
                // location has not been set
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return VariableStackAddress;
        }

        /* get the object type for the variable */
        public DataTypes GetSymbolVariableDataType()
        {
            if (Type != SymbolType.eSymbolVariable)
            {
                // not a variable
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return ObjectDataType;
        }

        /* make symbol into a function symbol */
        public void SymbolBecomeFunction(SymbolListRec ArgList, DataTypes ReturnType)
        {
            if (Type != SymbolType.eSymbolUndefined)
            {
                // already defined
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            Type = SymbolType.eSymbolFunction;
            ObjectDataType = ReturnType;
            FunctionArgList = ArgList;
        }

        // permits redeclaration
        public void SymbolBecomeFunction2(SymbolListRec ArgList, DataTypes ReturnType)
        {
            if ((Type != SymbolType.eSymbolUndefined) && (Type != SymbolType.eSymbolFunction))
            {
                // already defined
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            Type = SymbolType.eSymbolFunction;
            ObjectDataType = ReturnType;
            FunctionArgList = ArgList;
        }

        /* get the return type for the function */
        public DataTypes GetSymbolFunctionReturnType()
        {
            if (Type != SymbolType.eSymbolFunction)
            {
                // not a function
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return ObjectDataType;
        }

        /* get the list of symbol table entries from a function call argument list */
        public SymbolListRec GetSymbolFunctionArgList()
        {
            if (Type != SymbolType.eSymbolFunction)
            {
                // symbol table entry is not a function
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return FunctionArgList;
        }

        /* get the symbol's void* value */
        public object GetSymbolVoid()
        {
            return Void;
        }

        /* change the symbol's void* value */
        public void SetSymbolVoid(object o)
        {
            Void = o;
        }
    }

    public class SymbolListRec
    {
        private SymbolRec First;
        private SymbolListRec Rest;

        /* cons operation for a symbol list (lisp style) */
        public SymbolListRec(SymbolRec First, SymbolListRec Rest)
        {
            this.First = First;
            this.Rest = Rest;
        }

        public static SymbolListRec SymbolListCons(SymbolRec First, SymbolListRec Rest)
        {
            return new SymbolListRec(First, Rest);
        }

        public SymbolRec GetFirstFromSymbolList()
        {
            return First;
        }

        public SymbolListRec GetRestListFromSymbolList()
        {
            return Rest;
        }

        public static int GetSymbolListLength(SymbolListRec ListEntry)
        {
            int c = 0;
            while (ListEntry != null)
            {
                c++;
                ListEntry = ListEntry.Rest;
            }
            return c;
        }
    }

    /* result codes from adding a symbol to the symbol table */
    public enum AddSymbolType
    {
        eAddSymbolNoErr,
        eAddSymbolAlreadyExists,
    }

    public class SymbolTableRec
    {
        private const bool AllowLocalToMaskOuter = true;
        private List<Dictionary<string, SymbolRec>> ScopeHash = new List<Dictionary<string, SymbolRec>>();

        public SymbolTableRec()
        {
            IncrementSymbolTableLevel();
        }

        /* create a new symbol table (lexical scope) level */
        public void IncrementSymbolTableLevel()
        {
            ScopeHash.Add(new Dictionary<string, SymbolRec>());
        }

        /* drop the current symbol table lexical level (exit scope) */
        public void DecrementSymbolTableLevel()
        {
            ScopeHash.RemoveAt(ScopeHash.Count - 1);
        }

        /* add symbol table entry to the symbol table.  returns a result code */
        public AddSymbolType AddSymbolToTable(SymbolRec SymbolToAdd)
        {
            // ensure symbol is not already in table
            for (int i = ScopeHash.Count - 1; i >= (AllowLocalToMaskOuter ? ScopeHash.Count - 1 : 0); i--)
            {
                if (ScopeHash[i].ContainsKey(SymbolToAdd.GetSymbolName()))
                {
                    return AddSymbolType.eAddSymbolAlreadyExists;
                }
            }

            ScopeHash[ScopeHash.Count - 1].Add(SymbolToAdd.GetSymbolName(), SymbolToAdd);
            return AddSymbolType.eAddSymbolNoErr;
        }

        /* get a symbol from the symboldflksakdo table */
        /* it returns NIL if the entry was not found. */
        public SymbolRec GetSymbolFromTable(string NameString)
        {
            for (int i = ScopeHash.Count - 1; i >= 0; i--)
            {
                SymbolRec symbol;
                if (ScopeHash[i].TryGetValue(NameString, out symbol))
                {
                    return symbol;
                }
            }
            return null;
        }
    }
}
