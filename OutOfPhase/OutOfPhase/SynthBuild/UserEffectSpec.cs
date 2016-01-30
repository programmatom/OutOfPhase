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

            public bool Smoothed;
        }

        public class UserEffectSpecRec
        {
            public ItemRec[] Items;

            public DataTypes[] workspaces = new DataTypes[0];
            public string InitFuncName; /* NIL = none */
            public string[] ProcessDataFuncNames;

            public bool NoOversampling;
        }

        public static void UserEffectGetInitSignature(
            UserEffectSpecRec UserEffect,
            out DataTypes[] argsTypesOut,
            out DataTypes returnTypeOut)
        {
            List<DataTypes> argsTypes = new List<DataTypes>();

            argsTypes.Add(DataTypes.eDouble); // 't'
            argsTypes.Add(DataTypes.eDouble); // 'bpm'
            argsTypes.Add(DataTypes.eDouble); // 'samplingRate'
            argsTypes.Add(DataTypes.eInteger); // 'maxSampleCount'
            argsTypes.AddRange(UserEffect.workspaces); // user-specified workspace arrays

            argsTypesOut = argsTypes.ToArray();
            returnTypeOut = DataTypes.eBoolean;
        }

        public static void UserEffectGetDataSignature(
            UserEffectSpecRec UserEffect,
            out DataTypes[] argsTypesOut,
            out DataTypes returnTypeOut)
        {
            List<DataTypes> argsTypes = new List<DataTypes>();

            argsTypes.Add(DataTypes.eDouble); // 't'
            argsTypes.Add(DataTypes.eDouble); // 'bpm'
            argsTypes.Add(DataTypes.eDouble); // 'samplingRate'
            argsTypes.Add(DataTypes.eArrayOfFloat); // 'leftData'
            argsTypes.Add(DataTypes.eArrayOfFloat); // 'rightData'
            argsTypes.Add(DataTypes.eInteger); // 'sampleCount'
            argsTypes.AddRange(UserEffect.workspaces); // user-specified workspace arrays
            for (int i = 0; i < UserEffect.Items.Length; i++)
            {
                argsTypes.Add(UserEffect.Items[i].Smoothed ? DataTypes.eArrayOfFloat : DataTypes.eDouble); // user-specified control param
            }

            argsTypesOut = argsTypes.ToArray();
            returnTypeOut = DataTypes.eBoolean;
        }

        /* create a new user effect processor specifier.  name block is deleted */
        public static UserEffectSpecRec NewUserEffectSpec(
            string InitFuncName,
            string[] ProcessDataFuncNames,
            DataTypes[] workspaces)
        {
            UserEffectSpecRec Spec = new UserEffectSpecRec();

            Spec.Items = new ItemRec[0];

            Spec.workspaces = workspaces;
            Spec.InitFuncName = InitFuncName; // can be null
            Spec.ProcessDataFuncNames = ProcessDataFuncNames; // can be null

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
        public static string[] GetUserEffectSpecProcessDataFuncNames(UserEffectSpecRec Spec)
        {
            return Spec.ProcessDataFuncNames;
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

        public static bool GetUserEffectSpecParamSmoothed(
            UserEffectSpecRec Spec,
            int Index)
        {
            return Spec.Items[Index].Smoothed;
        }

        public static void SetUserEffectSpecParamSmoothed(
            UserEffectSpecRec Spec,
            int Index,
            bool smoothed)
        {
            Spec.Items[Index].Smoothed = smoothed;
        }

        public static void SetUserEffectSpecNoOversampling(
            UserEffectSpecRec Spec,
            bool NoOversampling)
        {
            Spec.NoOversampling = NoOversampling;
        }

        public static bool GetUserEffectSpecNoOversampling(
            UserEffectSpecRec Spec)
        {
            return Spec.NoOversampling;
        }

        public static DataTypes[] UserEffectGetWorkspaceTypes(
            UserEffectSpecRec Spec)
        {
            return Spec.workspaces;
        }
    }
}
