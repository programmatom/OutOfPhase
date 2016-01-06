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
    public class FuncCodeRec
    {
        private string FunctionName;
        private string Filename;
        private int NumParameters;
        private DataTypes[] ParameterTypeList;
        private PcodeRec Code;
        private DataTypes ReturnType;

        /* get the actual code block for a function. */
        public PcodeRec GetFunctionPcode()
        {
            return Code;
        }

        /* get the name of a function (actual, not copy).  it's a pointer into the heap */
        public string GetFunctionName()
        {
            return FunctionName;
        }

        /* get the file name of a function (actual, not copy).  it's a pointer into the heap */
        public string GetFunctionFilename()
        {
            return Filename;
        }

        /* get the list of parameters for a function.  it returns a heap block array */
        /* of data types with fields from left parameter to right parameter. */
        public DataTypes[] GetFunctionParameterTypeList()
        {
            return ParameterTypeList;
        }

        /* get the data type a function returns.  could be undefined. */
        public DataTypes GetFunctionReturnType()
        {
            return ReturnType;
        }

        public FuncCodeRec(
            string FuncName,
            SymbolListRec Parameters,
            PcodeRec PcodeThing,
            DataTypes ReturnType,
            string Filename)
        {
            this.FunctionName = FuncName;

            this.NumParameters = SymbolListRec.GetSymbolListLength(Parameters);
            this.ParameterTypeList = new DataTypes[this.NumParameters];

            this.Filename = Filename;

            SymbolListRec FormalParameterScanner = Parameters;
            int ParamIndex = 0;
            while (FormalParameterScanner != null)
            {
                this.ParameterTypeList[ParamIndex] = FormalParameterScanner.GetFirstFromSymbolList().GetSymbolVariableDataType();
                FormalParameterScanner = FormalParameterScanner.GetRestListFromSymbolList();
                ParamIndex++;
            }

            this.Code = PcodeThing;

            this.ReturnType = ReturnType;
        }

#if true // TODO:experimental
        public CILObject CILObject
        {
            get
            {
                return Code.cilObject;
            }
            set
            {
                Code.cilObject = value;
            }
        }
#endif
    }
}
