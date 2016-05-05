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

namespace OutOfPhase
{
    public enum SymbolKind
    {
        Undefined,
        Variable,
        Function,
    }

    public class SymbolRec
    {
        private readonly string name;

        private SymbolKind kind = SymbolKind.Undefined;

        private DataTypes objectDataType;
        private SymbolListRec functionArgList;
        private int variableStackAddress = -1; // to aid debugging

        public SymbolRec(string name)
        {
            this.name = name;
        }

        public SymbolKind Kind { get { return kind; } }

        public string SymbolName { get { return name; } }

        /* make symbol into a variable symbol */
        public void SymbolBecomeVariable(DataTypes VarType)
        {
            if (kind != SymbolKind.Undefined)
            {
                // symbol has already been defined
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            kind = SymbolKind.Variable;
            objectDataType = VarType;
        }

        public int SymbolVariableStackLocation
        {
            /* find out where on the stack a variable lives. */
            get
            {
                if (variableStackAddress < 0)
                {
                    // location has not been set
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                return variableStackAddress;
            }

            /* make variable know where on the stack it lives */
            set
            {
                if (value < 0)
                {
                    // negative index
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                if (variableStackAddress >= 0)
                {
                    // location has already been set
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                variableStackAddress = value;
            }
        }

        /* get the object type for the variable */
        public DataTypes VariableDataType
        {
            get
            {
                if (kind != SymbolKind.Variable)
                {
                    // not a variable
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                return objectDataType;
            }
        }

        /* make symbol into a function symbol */
        public void SymbolBecomeFunction(SymbolListRec ArgList, DataTypes ReturnType)
        {
            if (kind != SymbolKind.Undefined)
            {
                // already defined
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            kind = SymbolKind.Function;
            objectDataType = ReturnType;
            functionArgList = ArgList;
        }

        // this version permits redeclaration
        public void SymbolBecomeFunction2(SymbolListRec ArgList, DataTypes ReturnType)
        {
            if ((kind != SymbolKind.Undefined) && (kind != SymbolKind.Function))
            {
                // already defined
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            kind = SymbolKind.Function;
            objectDataType = ReturnType;
            functionArgList = ArgList;
        }

        /* get the return type for the function */
        public DataTypes FunctionReturnType
        {
            get
            {
                if (kind != SymbolKind.Function)
                {
                    // not a function
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                return objectDataType;
            }
        }

        /* get the list of symbol table entries from a function call argument list */
        public SymbolListRec FunctionArgList
        {
            get
            {
                if (kind != SymbolKind.Function)
                {
                    // symbol table entry is not a function
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                return functionArgList;
            }
        }

        public DataTypes[] FunctionArgListTypes
        {
            get
            {
                List<DataTypes> list = new List<DataTypes>();
                SymbolListRec iterator = functionArgList;
                while (iterator != null)
                {
                    list.Add(iterator.First.VariableDataType);
                    iterator = iterator.Rest;
                }
                return list.ToArray();
            }
        }

#if DEBUG
        public override string ToString()
        {
            switch (kind)
            {
                default:
                case SymbolKind.Undefined:
                    return String.Format("{0}", name);
                case SymbolKind.Variable:
                    return String.Format("{0}:{1}", name, FunctionSignature.GetKeywordForDataType(VariableDataType));
                case SymbolKind.Function:
                    return String.Format("{0}{1}", name, new FunctionSignature(FunctionArgListTypes, FunctionReturnType));

            }
        }
#endif
    }

    // cons cell for a symbol list (lisp style) - used for function formal argument lists
    public class SymbolListRec
    {
        private readonly SymbolRec first;
        private readonly SymbolListRec rest;

        public SymbolListRec(SymbolRec first, SymbolListRec rest)
        {
            this.first = first;
            this.rest = rest;
        }

        public static SymbolListRec Cons(SymbolRec first, SymbolListRec rest)
        {
            return new SymbolListRec(first, rest);
        }

        public SymbolRec First { get { return first; } }

        public SymbolListRec Rest { get { return rest; } }

        public int Count { get { return GetLength(this); } }

        public static int GetLength(SymbolListRec node) // null ok
        {
            int c = 0;
            while (node != null)
            {
                c++;
                node = node.rest;
            }
            return c;
        }
    }

    public class SymbolTableRec
    {
        private const bool AllowLocalToMaskOuter = true;
        private readonly List<Dictionary<string, SymbolRec>> ScopeHash = new List<Dictionary<string, SymbolRec>>();

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

        // add symbol table entry to the symbol table.
        //   return true if successful
        //   returns false if symbol by that name is already in table
        public bool Add(SymbolRec symbolToAdd)
        {
            // ensure symbol is not already in table
            for (int i = ScopeHash.Count - 1; i >= (AllowLocalToMaskOuter ? ScopeHash.Count - 1 : 0); i--)
            {
                if (ScopeHash[i].ContainsKey(symbolToAdd.SymbolName))
                {
                    return false;
                }
            }

            ScopeHash[ScopeHash.Count - 1].Add(symbolToAdd.SymbolName, symbolToAdd);

            return true;
        }

        // get a symbol from the table
        //   returns null if the entry was not found.
        public SymbolRec Lookup(string name)
        {
            for (int i = ScopeHash.Count - 1; i >= 0; i--)
            {
                SymbolRec symbol;
                if (ScopeHash[i].TryGetValue(name, out symbol))
                {
                    return symbol;
                }
            }
            return null;
        }
    }
}
