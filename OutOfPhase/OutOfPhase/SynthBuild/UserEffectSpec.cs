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
    public static partial class Synthesizer
    {
        public class ItemRec
        {
            /* these controls only apply to the track effect, not the oscillator effect */
            public double Param;
            public AccentRec ParamAccent;
            public PcodeRec ParamFormula;

            /* these controls apply only to the oscillator effect, not the track effect */
            public EnvelopeRec ParamEnvelope;
            public LFOListSpecRec ParamLFO;
        }

        public class UserEffectSpecRec
        {
            public ItemRec[] Items;

            public string InitFuncName; /* NIL = none */
            public string ArgUpdateFuncName; /* NIL = none */
            public string ProcessDataFuncName;
        }

        /* validate type signature of pcode things */
        public static bool UserEffectValidateTypeInit(FuncCodeRec FuncCode)
        {
            DataTypes[] Args = FuncCode.GetFunctionParameterTypeList();

            /* signature of init method is */
            /* void init(doublearray dLeftState, doublearray dRightState, floatarray fLeftState, floatarray fRightState, */
            /*     double t, double bpm, double SamplingRate) */

            if (Args.Length != 7)
            {
                return false;
            }

            if (Args[0] != DataTypes.eArrayOfDouble)
            {
                return false;
            }
            if (Args[1] != DataTypes.eArrayOfDouble)
            {
                return false;
            }
            if (Args[2] != DataTypes.eArrayOfFloat)
            {
                return false;
            }
            if (Args[3] != DataTypes.eArrayOfFloat)
            {
                return false;
            }
            if (Args[4] != DataTypes.eDouble)
            {
                return false;
            }
            if (Args[5] != DataTypes.eDouble)
            {
                return false;
            }
            if (Args[6] != DataTypes.eDouble)
            {
                return false;
            }

            return true;
        }

        /* validate type signature of pcode things */
        public static bool UserEffectValidateTypeUpdate(
            FuncCodeRec FuncCode,
            int ArgCount)
        {
            DataTypes[] Args = FuncCode.GetFunctionParameterTypeList();

            /* signature of update method is */
            /* void update(doublearray LeftState, doublearray RightState, floatarray fLeftState, floatarray fRightState, */
            /*   double t, double bpm, double SamplingRate [, double param, ...]) */

            if (Args.Length != 7 + ArgCount)
            {
                return false;
            }

            if (Args[0] != DataTypes.eArrayOfDouble)
            {
                return false;
            }
            if (Args[1] != DataTypes.eArrayOfDouble)
            {
                return false;
            }
            if (Args[2] != DataTypes.eArrayOfFloat)
            {
                return false;
            }
            if (Args[3] != DataTypes.eArrayOfFloat)
            {
                return false;
            }
            // t, bpm, SamplingRate, and all user params checked here (all must be 'double')
            for (int i = 4; i < Args.Length; i++)
            {
                if (Args[i] != DataTypes.eDouble)
                {
                    return false;
                }
            }

            return true;
        }

        /* validate type signature of pcode things */
        public static bool UserEffectValidateTypeData(FuncCodeRec FuncCode)
        {
            DataTypes[] Args = FuncCode.GetFunctionParameterTypeList();

            /* signature of init method is */
            /* void processdata(doublearray LeftState, doublearray RightState, floatarray fLeftState, floatarray fRightState, */
            /*   floatarray LeftData, floatarray RightData, int c, double SamplingRate) */

            if (Args.Length != 8)
            {
                return false;
            }

            if (Args[0] != DataTypes.eArrayOfDouble)
            {
                return false;
            }
            if (Args[1] != DataTypes.eArrayOfDouble)
            {
                return false;
            }
            if (Args[2] != DataTypes.eArrayOfFloat)
            {
                return false;
            }
            if (Args[3] != DataTypes.eArrayOfFloat)
            {
                return false;
            }
            if (Args[4] != DataTypes.eArrayOfFloat)
            {
                return false;
            }
            if (Args[5] != DataTypes.eArrayOfFloat)
            {
                return false;
            }
            if (Args[6] != DataTypes.eInteger)
            {
                return false;
            }
            if (Args[7] != DataTypes.eDouble)
            {
                return false;
            }

            return true;
        }

        /* create a new user effect processor specifier.  name block is deleted */
        public static UserEffectSpecRec NewUserEffectSpec(
            string InitFuncName,
            string ArgUpdateFuncName,
            string ProcessDataFuncName)
        {
            UserEffectSpecRec Spec = new UserEffectSpecRec();

            Spec.Items = new ItemRec[0];

            Spec.InitFuncName = InitFuncName; // can be null
            Spec.ArgUpdateFuncName = ArgUpdateFuncName; // can be null
            Spec.ProcessDataFuncName = ProcessDataFuncName; // can be null

            return Spec;
        }

        /* create new param */
        public static void AddUserEffectSpecParam(
            UserEffectSpecRec Spec,
            out int Index)
        {
            ItemRec Item = new ItemRec();

            Index = Spec.Items.Length;
            Array.Resize(ref Spec.Items, Spec.Items.Length + 1);
            Spec.Items[Index] = Item;

            Item.ParamEnvelope = NewEnvelope();
            Item.ParamLFO = NewLFOListSpecifier();
        }

        /* get count of parameters */
        public static int GetUserEffectSpecParamCount(UserEffectSpecRec Spec)
        {
            return Spec.Items.Length;
        }

        /* get function symbols (NIL = not specified) */
        public static string GetUserEffectSpecInitFuncName(UserEffectSpecRec Spec)
        {
            return Spec.InitFuncName;
        }

        /* get function symbols (NIL = not specified) */
        public static string GetUserEffectSpecArgUpdateFuncName(UserEffectSpecRec Spec)
        {
            return Spec.ArgUpdateFuncName;
        }

        /* get function symbols (NIL = not specified) */
        public static string GetUserEffectSpecProcessDataFuncName(UserEffectSpecRec Spec)
        {
            return Spec.ProcessDataFuncName;
        }

        /* set various attributes */
        public static void PutUserEffectSpecParam(
            UserEffectSpecRec Spec,
            int Index,
            double Param,
            PcodeRec ParamFormula)
        {
#if DEBUG
            if (Spec.Items[Index].ParamFormula != null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            Spec.Items[Index].ParamFormula = ParamFormula;
            Spec.Items[Index].Param = Param;
        }

        /* set various attributes */
        public static void PutUserEffectSpecParamAccent(
            UserEffectSpecRec Spec,
            int Index,
            double Value,
            int AccentNum)
        {
            SetAccentMemberValue(ref Spec.Items[Index].ParamAccent, AccentNum, Value);
        }

        /* retrieve various attributes */
        public static void GetUserEffectSpecParamAgg(
            UserEffectSpecRec Spec,
            int Index,
            out ScalarParamEvalRec ParamsOut)
        {
            InitScalarParamEval(
                Spec.Items[Index].Param,
                ref Spec.Items[Index].ParamAccent,
                Spec.Items[Index].ParamFormula,
                out ParamsOut);
        }

        /* get enveloping things */
        public static EnvelopeRec GetUserEffectSpecParamEnvelope(
            UserEffectSpecRec Spec,
            int Index)
        {
            return Spec.Items[Index].ParamEnvelope;
        }

        /* get enveloping things */
        public static LFOListSpecRec GetUserEffectSpecParamLFO(
            UserEffectSpecRec Spec,
            int Index)
        {
            return Spec.Items[Index].ParamLFO;
        }
    }
}
