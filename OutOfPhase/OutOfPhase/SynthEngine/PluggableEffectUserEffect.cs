/*
 *  Copyright © 1994-2002, 2015-2017 Thomas R. Lawrence
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
    public static partial class Synthesizer
    {
        // This is relatively complicated example of a pluggable processor, and a good place to look to see how
        // to support complicated things.

        // TODO: add ability to pass static initialization params to initfunc

        public class PluggableEffectUserEffectFactory : IPluggableProcessorFactory
        {
            public string ParserName { get { return "usereffect"; } }

            public Guid UniqueId { get { return uniqueId; } }
            private static readonly Guid uniqueId = new Guid("{8DD20847-F206-4C0F-AF69-81899F1BB42B}");

            public ConfigInfo[] Configs { get { return configs; } }
            private static readonly ConfigInfo[] configs = new ConfigInfo[]
            {
                new ConfigInfo(UserStateToken, ConfigForm.List),
                new ConfigInfo(ParamsToken, ConfigForm.List | ConfigForm.Unique | ConfigForm.Required),
                new ConfigInfo(InitFuncToken, ConfigForm.List, 1, 1),
                new ConfigInfo(DataFuncToken, ConfigForm.List | ConfigForm.Unique | ConfigForm.Required, 1, null),
                new ConfigInfo(LoudnessParamToken, ConfigForm.List, 1, 1),
                new ConfigInfo(PitchParamToken, ConfigForm.List, 1, 1),
                new ConfigInfo(DisableOversamplingToken, ConfigForm.List, 0, 0),
                new ConfigInfo(SmoothedToken, ConfigForm.List | ConfigForm.Unique),
                new ConfigInfo(StaticToken, ConfigForm.KeyValuePairs | ConfigForm.Unique | ConfigForm.KeyValuePairColonDelimited),
            };
            private const string UserStateToken = "workspaces";
            private const string ParamsToken = "params";
            private const string InitFuncToken = "initfunc";
            private const string DataFuncToken = "datafunc";
            private const string LoudnessParamToken = "loudnessparam"; // valid for oscillator only
            private const string PitchParamToken = "pitchparam"; // valid for oscillator only
            private const string DisableOversamplingToken = "oversamplingdisabled";
            private const string SmoothedToken = "smoothed";
            private const string StaticToken = "static";

            public PluggableRole Roles { get { return PluggableRole.Oscillator | PluggableRole.Effect; } }

            private static readonly string[] validUserStateTokens = new string[] { UserStateIntToken, UserStateFloatToken, UserStateDoubleToken };
            private const string UserStateIntToken = "intarray";
            private const string UserStateFloatToken = "floatarray";
            private const string UserStateDoubleToken = "doublearray";

            public int MaximumRequired32BitWorkspaceCount { get { return 0; } }

            public int MaximumRequired64BitWorkspaceCount { get { return 0; } }

            public int MaximumSmoothedParameterCount { get { return Math.Max(DefaultMaximumSmoothedParameterCount, Program.Config.MaximumSmoothedParameterCount); } }
            public const int DefaultMaximumSmoothedParameterCount = 16; // enough for most situations

            public BuildInstrErrors Create(
                KeyValuePair<string, string[]>[] configsTokens,
                PluggableRole role,
                out IPluggableProcessorTemplate template,
                out string errorMessage)
            {
                errorMessage = null;
                template = null;

                string[] userStates = null, parameters = null, initfuncs = null, datafuncs = null, smoothed = null;
                string loudnessParamName = null, pitchParamName = null;
                bool disableOversampling = false;
                KeyValuePair<string, PluggableParameterType>[] staticParams = null;

                Debug.Assert(configsTokens.Length <= configs.Length);
                for (int i = 0; i < configsTokens.Length; i++)
                {
                    // Parser provides the following contract for configsTokens:
                    // - if config category was not specified, it is omitted from the array
                    // - if config was specified but had no items, the .Value property is non-null and has .Length == 0
                    // - otherwise config had items, which are enumerated in the .Value array
                    // property .Value is never null

                    switch (configsTokens[i].Key)
                    {
                        default:
                            Debug.Assert(false); // parser guarrantees no invalid names at config level
                            throw new ArgumentException();

                        case UserStateToken:
                            Debug.Assert(userStates == null); // parser guarrantees no duplicates at config level
                            userStates = configsTokens[i].Value;
                            for (int j = 0; j < userStates.Length; j++)
                            {
                                if (Array.IndexOf(validUserStateTokens, userStates[j]) < 0) // plug-in code must validate individual config tokens
                                {
                                    errorMessage = String.Format("Invalid workspace type '{0}' - must be one of '{1}', '{2}', or '{3}'", userStates[j], UserStateIntToken, UserStateFloatToken, UserStateDoubleToken);
                                    return BuildInstrErrors.eBuildInstrGenericError;
                                }
                            }
                            break;

                        case ParamsToken:
                            Debug.Assert(parameters == null); // parser guarrantees no duplicates at config level
                            parameters = configsTokens[i].Value;
                            Debug.Assert(AllUnique(parameters)); // parser honors ConfigInfo.Form flags
                            break;

                        case InitFuncToken:
                            Debug.Assert(initfuncs == null); // parser guarrantees no duplicates at config level
                            initfuncs = configsTokens[i].Value;
                            Debug.Assert(initfuncs.Length == 1); // parser honors ConfigInfo.Form flags
                            break;

                        case DataFuncToken:
                            Debug.Assert(datafuncs == null); // parser guarrantees no duplicates at config level
                            datafuncs = configsTokens[i].Value;
                            Debug.Assert(AllUnique(datafuncs)); // parser honors ConfigInfo.Form flags
                            break;

                        case LoudnessParamToken:
                            Debug.Assert(loudnessParamName == null); // parser guarrantees no duplicates at config level
                            if (role == PluggableRole.Oscillator) // plug-in code must validate individual config tokens
                            {
                                Debug.Assert(configsTokens[i].Value.Length == 1);  // parser honors ConfigInfo.Form flags
                                loudnessParamName = configsTokens[i].Value[0];
                            }
                            else
                            {
                                if (configsTokens[i].Value.Length != 0) // plug-in code must validate custom business logic
                                {
                                    errorMessage = String.Format("'{0}' can only be specified for oscillators", LoudnessParamToken);
                                    return BuildInstrErrors.eBuildInstrGenericError;
                                }
                            }
                            break;

                        case PitchParamToken:
                            Debug.Assert(pitchParamName == null); // parser guarrantees no duplicates at config level
                            if (role == PluggableRole.Oscillator) // plug-in code must validate individual config tokens
                            {
                                Debug.Assert(configsTokens[i].Value.Length == 1); // parser honors ConfigInfo.Form flags
                                pitchParamName = configsTokens[i].Value[0];
                            }
                            else
                            {
                                if (configsTokens[i].Value.Length != 0) // plug-in code must validate custom business logic
                                {
                                    errorMessage = String.Format("'{0}' can only be specified for oscillators", PitchParamToken);
                                    return BuildInstrErrors.eBuildInstrGenericError;
                                }
                            }
                            break;

                        case DisableOversamplingToken:
                            Debug.Assert(configsTokens[i].Value.Length == 0); // parser honors ConfigInfo.Form flags
                            disableOversampling = true;
                            break;

                        case SmoothedToken:
                            Debug.Assert(smoothed == null); // parser guarrantees no duplicates at config level
                            smoothed = configsTokens[i].Value;
                            Debug.Assert(AllUnique(smoothed)); // parser honors ConfigInfo.Form flags
                            break;

                        case StaticToken:
                            Debug.Assert(staticParams == null); // parser guarrantees no duplicates at config level
                            Debug.Assert(configsTokens[i].Value.Length % 2 == 0); // parser honors ConfigInfo.Form flags
                            staticParams = new KeyValuePair<string, PluggableParameterType>[configsTokens[i].Value.Length / 2];
                            for (int j = 0; j < configsTokens[i].Value.Length; j += 2)
                            {
                                PluggableParameterType type;
                                switch (configsTokens[i].Value[j + 1])
                                {
                                    default: // plug-in code must validate individual config tokens
                                        errorMessage = String.Format("Invalid static type '{0}' - must be 'int' or 'double'", configsTokens[i].Value[j]);
                                        return BuildInstrErrors.eBuildInstrGenericError;
                                    case "int":
                                        type = PluggableParameterType.StaticInteger;
                                        break;
                                    case "double":
                                        type = PluggableParameterType.StaticDouble;
                                        break;
                                }
                                staticParams[j] = new KeyValuePair<string, PluggableParameterType>(configsTokens[i].Value[j], type);
                            }
                            break;
                    }
                }

                Debug.Assert(NullSafeLength(datafuncs) > 0); // parser honors ConfigInfo.Form flags

                for (int i = 0; i < NullSafeLength(smoothed); i++)
                {
                    if (NullSafeIndexOf(parameters, smoothed[i]) < 0)
                    {
                        errorMessage = String.Format("Smoothed item '{0}' does not correspond to any declared parameter", smoothed[i]);
                        return BuildInstrErrors.eBuildInstrGenericError;
                    }
                }

                int loudnessParameterIndex = -1;
                int pitchParameterIndex = -1;
                if (role == PluggableRole.Oscillator)
                {
                    if (loudnessParamName == null) // plug-in code must validate custom business logic
                    {
                        errorMessage = String.Format("'{0}' must be specified for oscillator", LoudnessParamToken);
                        return BuildInstrErrors.eBuildInstrGenericError;
                    }
                    if ((loudnessParameterIndex = NullSafeIndexOf(parameters, loudnessParamName)) < 0) // plug-in code must validate custom business logic
                    {
                        errorMessage = String.Format("'{0}' must specify a valid parameter name (was '{1}')", LoudnessParamToken, loudnessParamName);
                        return BuildInstrErrors.eBuildInstrGenericError;
                    }

                    if (pitchParamName == null) // plug-in code must validate custom business logic
                    {
                        errorMessage = String.Format("'{0}' must be specified for oscillator", PitchParamToken);
                        return BuildInstrErrors.eBuildInstrGenericError;
                    }
                    if ((pitchParameterIndex = NullSafeIndexOf(parameters, pitchParamName)) < 0) // plug-in code must validate custom business logic
                    {
                        errorMessage = String.Format("'{0}' must specify a valid parameter name (was '{1}')", PitchParamToken, pitchParamName);
                        return BuildInstrErrors.eBuildInstrGenericError;
                    }
                }

                DataTypes[] userStateTypes = new DataTypes[userStates != null ? userStates.Length : 0];
                for (int i = 0; i < userStateTypes.Length; i++)
                {
                    switch (userStates[i])
                    {
                        default:
                            Debug.Assert(false); // we should have caught this case above
                            throw new InvalidOperationException();
                        case UserStateIntToken:
                            userStateTypes[i] = DataTypes.eArrayOfInteger;
                            break;
                        case UserStateFloatToken:
                            userStateTypes[i] = DataTypes.eArrayOfFloat;
                            break;
                        case UserStateDoubleToken:
                            userStateTypes[i] = DataTypes.eArrayOfDouble;
                            break;
                    }
                }

                errorMessage = null;
                template = new PluggableEffectUserEffectTemplate(
                    this,
                    userStateTypes,
                    parameters != null ? parameters : new string[0],
                    smoothed != null ? smoothed : new string[0],
                    loudnessParameterIndex,
                    pitchParameterIndex,
                    initfuncs != null ? initfuncs[0] : null,
                    datafuncs,
                    disableOversampling,
                    staticParams != null ? staticParams : new KeyValuePair<string, PluggableParameterType>[0]);
                return BuildInstrErrors.eBuildInstrNoError;
            }
        }

        public static int NullSafeLength<T>(T[] array)
        {
            return array != null ? array.Length : 0;
        }

        public static int NullSafeIndexOf<T>(T[] array, T candidate)
        {
            return array != null ? Array.IndexOf<T>(array, candidate) : -1;
        }

        public static bool AllUnique(IEnumerable<string> items)
        {
            Dictionary<string, bool> unique = new Dictionary<string, bool>();
            foreach (string item in items)
            {
                if (unique.ContainsKey(item))
                {
                    return false;
                }
                unique.Add(item, false);
            }
            return true;
        }

        public class PluggableEffectUserEffectTemplate : IPluggableProcessorTemplate
        {
            private readonly PluggableEffectUserEffectFactory factory;
            private readonly DataTypes[] userStateTypes;
            private readonly PluggableParameter[] parametersDefinitions;
            private readonly int dynamicParamsCount;
            private readonly string initFuncName;
            private readonly string[] dataFuncNames;
            private readonly bool disableOversampling;

            public PluggableEffectUserEffectTemplate(
                PluggableEffectUserEffectFactory factory,
                DataTypes[] userStateTypes,
                string[] userParamsNames,
                string[] smoothedParamsNames,
                int loudnessParameterIndex,
                int pitchParameterIndex,
                string initfunc,
                string[] datafuncs,
                bool disableOversampling,
                KeyValuePair<string, PluggableParameterType>[] staticParams)
            {
                this.factory = factory;
                this.userStateTypes = userStateTypes;
                this.initFuncName = initfunc;
                this.dataFuncNames = datafuncs;
                this.disableOversampling = disableOversampling;

                this.dynamicParamsCount = userParamsNames.Length;
                this.parametersDefinitions = new PluggableParameter[this.dynamicParamsCount + staticParams.Length];
                int j = 0;
                for (int i = 0; i < this.dynamicParamsCount; i++) // dynamic params all come at start of parametersDefinitions[]
                {
                    bool smoothed = Array.IndexOf(smoothedParamsNames, userParamsNames[i]) >= 0;

                    this.parametersDefinitions[j++] = new PluggableParameter(
                        userParamsNames[i],
                        PluggableParameterType.DynamicDouble
                            | (i == loudnessParameterIndex ? PluggableParameterType.Loudness : 0)
                            | (i == pitchParameterIndex ? PluggableParameterType.Pitch : 0)
                            | (smoothed ? PluggableParameterType.Smoothed | PluggableParameterType.SmoothedOptional : 0));
                }
                for (int i = 0; i < staticParams.Length; i++) // static params come after all dynamic params in parametersDefinitions[]
                {
                    this.parametersDefinitions[j++] = new PluggableParameter(staticParams[i].Key, staticParams[i].Value);
                }
                Debug.Assert(j == this.parametersDefinitions.Length);
            }

            public IPluggableProcessorFactory Factory { get { return factory; } }

            public PluggableParameter[] ParametersDefinition { get { return parametersDefinitions; } }

            public SynthErrorCodes CheckUnreferencedObjects(
                string[] staticStringParameters,
                CheckUnrefParamRec context)
            {
                /* init func */
                {
                    string FuncName = initFuncName;
                    if (FuncName != null) /* optional */
                    {
                        FuncCodeRec FuncCode = context.CodeCenter.ObtainFunctionHandle(FuncName);
                        if (FuncCode == null)
                        {
                            context.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedFunction;
                            context.ErrorInfo.FunctionName = FuncName;
                            return SynthErrorCodes.eSynthErrorEx;
                        }

                        DataTypes[] argsTypes;
                        DataTypes returnType;
                        UserEffectGetInitSignature(out argsTypes, out returnType);
                        FunctionSignature expectedSignature = new FunctionSignature(argsTypes, returnType);
                        FunctionSignature actualSignature = new FunctionSignature(
                            FuncCode.GetFunctionParameterTypeList(),
                            FuncCode.GetFunctionReturnType());
                        if (!FunctionSignature.Equals(expectedSignature, actualSignature))
                        {
                            context.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExTypeMismatchFunction;
                            context.ErrorInfo.FunctionName = FuncName;
                            context.ErrorInfo.ExtraInfo = String.Format(
                                "{0}{0}Expected:{0}{1}{0}{0}Actual:{0}{2}",
                                Environment.NewLine,
                                expectedSignature,
                                actualSignature);
                            return SynthErrorCodes.eSynthErrorEx;
                        }
                    }
                }

                /* data func */
                {
                    DataTypes[] argsTypes;
                    DataTypes returnType;
                    UserEffectGetDataSignature(out argsTypes, out returnType);
                    FunctionSignature expectedSignature = new FunctionSignature(argsTypes, returnType);

                    bool matched = false;
                    List<KeyValuePair<string, FunctionSignature>> actualSignatures = new List<KeyValuePair<string, FunctionSignature>>();
                    foreach (string FuncName in dataFuncNames)
                    {
                        FuncCodeRec FuncCode = context.CodeCenter.ObtainFunctionHandle(FuncName);
                        if (FuncCode == null)
                        {
                            context.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUndefinedFunction;
                            context.ErrorInfo.FunctionName = FuncName;
                            return SynthErrorCodes.eSynthErrorEx;
                        }

                        FunctionSignature actualSignature = new FunctionSignature(
                            FuncCode.GetFunctionParameterTypeList(),
                            FuncCode.GetFunctionReturnType());
                        actualSignatures.Add(new KeyValuePair<string, FunctionSignature>(FuncName, actualSignature));
                        if (FunctionSignature.Equals(expectedSignature, actualSignature))
                        {
                            matched = true;
                            break;
                        }
                    }
                    if (!matched)
                    {
                        StringBuilder extraInfo = new StringBuilder();
                        extraInfo.AppendLine();
                        extraInfo.AppendLine();
                        extraInfo.AppendLine(String.Format("Expected - {0}", expectedSignature));
                        foreach (KeyValuePair<string, FunctionSignature> actualSignature in actualSignatures)
                        {
                            extraInfo.AppendLine();
                            extraInfo.AppendLine(String.Format("Actual - {0}{1}", actualSignature.Key, actualSignature.Value));
                        }
                        context.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExTypeMismatchFunctionMultiple;
                        context.ErrorInfo.ExtraInfo = extraInfo.ToString();
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            public SynthErrorCodes Create(
                int[] staticIntegerParameters,
                double[] staticDoubleParameters,
                string[] staticStringParameters,
                bool dynamicDoubleOptionallySmoothedParametersDegraded,
                SynthParamRec synthParams,
                out IPluggableProcessor processor)
            {
                processor = null;

                Debug.Assert(staticIntegerParameters.Length + staticDoubleParameters.Length + dynamicParamsCount == parametersDefinitions.Length);
                Debug.Assert(staticStringParameters.Length == 0);

                PcodeRec initFunc = null;
                {
                    string FuncName = initFuncName;
                    if (FuncName != null)
                    {
                        FuncCodeRec FuncCode = synthParams.perTrack.CodeCenter.ObtainFunctionHandle(FuncName);
                        if (FuncCode == null)
                        {
                            // Function missing; should have been found by CheckUnreferencedThings
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }

                        DataTypes[] argsTypes;
                        DataTypes returnType;
                        UserEffectGetInitSignature(out argsTypes, out returnType);
                        FunctionSignature expectedSignature = new FunctionSignature(argsTypes, returnType);
                        FunctionSignature actualSignature = new FunctionSignature(
                            FuncCode.GetFunctionParameterTypeList(),
                            FuncCode.GetFunctionReturnType());
                        if (!FunctionSignature.Equals(expectedSignature, actualSignature))
                        {
                            // Function type mismatch; should have been found by CheckUnreferencedThings
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }

                        initFunc = FuncCode.GetFunctionPcode();
                    }
                }

                PcodeRec dataFunc = null;
                {
                    DataTypes[] argsTypes;
                    DataTypes returnType;
                    UserEffectGetDataSignature(out argsTypes, out returnType);
                    FunctionSignature expectedSignature = new FunctionSignature(argsTypes, returnType);

                    foreach (string FuncName in dataFuncNames)
                    {
                        FuncCodeRec FuncCode = synthParams.perTrack.CodeCenter.ObtainFunctionHandle(FuncName);
                        if (FuncCode == null)
                        {
                            // Function missing; should have been found by CheckUnreferencedThings
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }

                        FunctionSignature actualSignature = new FunctionSignature(
                            FuncCode.GetFunctionParameterTypeList(),
                            FuncCode.GetFunctionReturnType());
                        if (FunctionSignature.Equals(expectedSignature, actualSignature))
                        {
                            dataFunc = FuncCode.GetFunctionPcode();
                            break;
                        }
                    }
                    if (dataFunc == null)
                    {
                        // None matched -- should have been found by CheckUnreferencedThings
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                }

                double sr = synthParams.dSamplingRate;
                if (!((synthParams.iOversampling == 1) || !disableOversampling))
                {
                    sr /= synthParams.iOversampling;
                }

                // create state objects
                ArrayHandle[] userState = new ArrayHandle[userStateTypes.Length];
                for (int i = 0; i < userState.Length; i++)
                {
                    switch (userStateTypes[i])
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case DataTypes.eArrayOfInteger:
                            userState[i] = new ArrayHandleInt32(new int[0]);
                            break;
                        case DataTypes.eArrayOfFloat:
                            userState[i] = new ArrayHandleFloat(new float[0]);
                            break;
                        case DataTypes.eArrayOfDouble:
                            userState[i] = new ArrayHandleDouble(new double[0]);
                            break;
                    }
                }

                /* initialize user state */
                if (initFunc != null)
                {
                    int argCount = 1/*retval*/
                        + 1/*t*/
                        + 1/*bpm*/
                        + 1/*samplingRate*/
                        + 1/*maxSampleCount*/
                        + userState.Length/*user state arrays*/
                        + (parametersDefinitions.Length - dynamicParamsCount)/*static params*/
                        + 1/*retaddr*/;
                    synthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(argCount);

                    StackElement[] StackBase;
                    int StackNumElements;
                    synthParams.FormulaEvalContext.GetRawStack(out StackBase, out StackNumElements);

                    StackBase[StackNumElements++].Data.Double = synthParams.dElapsedTimeInSeconds; /* t */

                    StackBase[StackNumElements++].Data.Double = synthParams.dCurrentBeatsPerMinute; /* bpm */

                    StackBase[StackNumElements++].Data.Double = sr; /* samplingRate */

                    StackBase[StackNumElements++].Data.Integer = synthParams.nAllocatedPointsOneChannel; /* maxSampleCount */

                    for (int i = 0; i < userState.Length; i++)
                    {
                        StackBase[StackNumElements++].reference.arrayHandleGeneric = userState[i]; // user state
                    }

                    int iStaticInteger = 0, iStaticDouble = 0;
                    for (int i = 0; i < parametersDefinitions.Length; i++)
                    {
                        switch (parametersDefinitions[i].Type & PluggableParameterType.TypeMask)
                        {
                            default:
                            case PluggableParameterType.StaticString:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case PluggableParameterType.StaticInteger:
                                StackBase[StackNumElements++].Data.Integer = staticIntegerParameters[iStaticInteger++]; // user-specified static param
                                break;
                            case PluggableParameterType.StaticDouble:
                                StackBase[StackNumElements++].Data.Double = staticDoubleParameters[iStaticDouble++]; // user-specified static param
                                break;
                            case PluggableParameterType.DynamicDouble:
                                // not for this function
                                break;
                        }
                    }
                    Debug.Assert(iStaticInteger == staticIntegerParameters.Length);
                    Debug.Assert(iStaticDouble == staticDoubleParameters.Length);

                    StackNumElements++; /* return address placeholder */

                    synthParams.FormulaEvalContext.UpdateRawStack(StackBase, StackNumElements);

                    EvalErrInfoRec ErrorInfo;
                    EvalErrors Error = PcodeSystem.EvaluatePcode(
                        synthParams.FormulaEvalContext,
                        initFunc,
                        synthParams.perTrack.CodeCenter,
                        out ErrorInfo,
                        PcodeExternsNull.Default,
                        ref synthParams.pcodeThreadContext);
                    if (Error != EvalErrors.eEvalNoError)
                    {
                        synthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserEffectFunctionEvalError;
                        synthParams.ErrorInfo.UserEvalErrorCode = Error;
                        synthParams.ErrorInfo.UserEvalErrorInfo = ErrorInfo;
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                    Debug.Assert(synthParams.FormulaEvalContext.GetStackNumElements() == 1); // return value

                    synthParams.FormulaEvalContext.Clear();
                }

                processor = new PluggableEffectUserEffect(
                    this,
                    disableOversampling,
                    dataFunc,
                    userState,
                    dynamicParamsCount,
                    synthParams);
                return SynthErrorCodes.eSynthDone;
            }

            public KeyValuePair<string, int>[] GetEnumeratedIntegerTokens(int parameterIndex)
            {
                Debug.Assert(false);
                throw new NotSupportedException();
            }

            public bool StaticRangeCheck(int parameterIndex, string value, out string errorMessage)
            {
                Debug.Assert(false);
                throw new NotSupportedException();
            }

            public bool StaticRangeCheck(int parameterIndex, double value, out string errorMessage)
            {
                errorMessage = null;
                return true;
            }

            public bool StaticRangeCheck(int parameterIndex, int value, out string errorMessage)
            {
                errorMessage = null;
                return true;
            }

            public int Required32BitWorkspaceCount { get { return 0; } }
            public int Required64BitWorkspaceCount { get { return 0; } }

            public void UserEffectGetInitSignature(
                out DataTypes[] argsTypesOut,
                out DataTypes returnTypeOut)
            {
                List<DataTypes> argsTypes = new List<DataTypes>();

                argsTypes.Add(DataTypes.eDouble); // 't'
                argsTypes.Add(DataTypes.eDouble); // 'bpm'
                argsTypes.Add(DataTypes.eDouble); // 'samplingRate'
                argsTypes.Add(DataTypes.eInteger); // 'maxSampleCount'
                argsTypes.AddRange(userStateTypes); // user-specified workspace arrays
                for (int i = 0; i < parametersDefinitions.Length; i++)
                {
                    switch (parametersDefinitions[i].Type & PluggableParameterType.TypeMask)
                    {
                        default:
                        case PluggableParameterType.StaticString:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case PluggableParameterType.StaticInteger:
                            argsTypes.Add(DataTypes.eInteger); // user-specified static param
                            break;
                        case PluggableParameterType.StaticDouble:
                            argsTypes.Add(DataTypes.eDouble); // user-specified static param
                            break;
                        case PluggableParameterType.DynamicDouble:
                            // not for this function
                            break;
                    }
                }

                argsTypesOut = argsTypes.ToArray();
                returnTypeOut = DataTypes.eBoolean;
            }

            public void UserEffectGetDataSignature(
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
                argsTypes.AddRange(userStateTypes); // user-specified workspace arrays
                for (int i = 0; i < parametersDefinitions.Length; i++)
                {
                    if ((parametersDefinitions[i].Type & PluggableParameterType.TypeMask) == PluggableParameterType.DynamicDouble)
                    {
                        argsTypes.Add((parametersDefinitions[i].Type & PluggableParameterType.Smoothed) != 0 ? DataTypes.eArrayOfFloat : DataTypes.eDouble); // user-specified control param
                    }
                }

                argsTypesOut = argsTypes.ToArray();
                returnTypeOut = DataTypes.eBoolean;
            }
        }


        public class PluggableEffectUserEffect : IPluggableProcessor
        {
            // data processing formula.
            private readonly PcodeRec dataFunc;

            // Some user-provided effects might be parametrically unstable and exhibit bad behavior when sampling
            // rates are well out of expected range. This option allows the use of the original parameter range with
            // oversampling for the rest of the score, at a cost of potential aliasing in the audio chain containing
            // this effect processor.
            private readonly bool disableOversampling;
            private float lastLeft, lastRight;

            // reused workspaces for staging sample data in/out during
            private readonly float[] leftWorkspace;
            private readonly ArrayHandleFloat leftWorkspaceHandle;
            private readonly float[] rightWorkspace;
            private readonly ArrayHandleFloat rightWorkspaceHandle;

            // user-request state arrays
            private readonly ArrayHandle[] userState;

            // smoothing workspaces
            private readonly SmoothingEntry[] smoothingBuffers;
            private readonly int[] paramIndexToSmoothingIndex;

            private readonly double[] dynamicParams;

            [StructLayout(LayoutKind.Auto)]
            public struct SmoothingEntry
            {
                public ArrayHandleFloat arrayHandle;
                public float[] vector;
                public bool degraded;
            }

            public PluggableEffectUserEffect(
                PluggableEffectUserEffectTemplate template,
                bool disableOversampling,
                PcodeRec dataFunc,
                ArrayHandle[] userState,
                int dynamicParamsCount,
                SynthParamRec synthParams)
            {
                this.disableOversampling = disableOversampling;
                this.dataFunc = dataFunc;
                this.userState = userState;

                // initialize sample data in/out staging areas
                this.leftWorkspace = new float[synthParams.nAllocatedPointsOneChannel];
                this.leftWorkspaceHandle = new ArrayHandleFloat(null);
                this.rightWorkspace = new float[synthParams.nAllocatedPointsOneChannel];
                this.rightWorkspaceHandle = new ArrayHandleFloat(null);

                this.smoothingBuffers = new SmoothingEntry[dynamicParamsCount];
                this.paramIndexToSmoothingIndex = new int[dynamicParamsCount];
                this.dynamicParams = new double[dynamicParamsCount];
                int j = 0;
                for (int i = 0; i < dynamicParamsCount; i++) // dynamic params come at front of ParametersDefinition[]
                {
                    if ((template.ParametersDefinition[i].Type & PluggableParameterType.Smoothed) != 0)
                    {
                        float[] vector = new float[synthParams.nAllocatedPointsOneChannel];
                        smoothingBuffers[i].vector = vector;
                        smoothingBuffers[i].arrayHandle = new ArrayHandleFloat(vector);

                        paramIndexToSmoothingIndex[j++] = i;
                    }
                }
                Debug.Assert(j <= smoothingBuffers.Length);
                Array.Resize(ref paramIndexToSmoothingIndex, j); // detect bugs
            }

            public bool Running { get { return false; } } // allow driver's loudness envelope to determine lifetime

            public SynthErrorCodes Update(double[] dynamicDoubleParameters)
            {
                Debug.Assert(dynamicParams.Length == dynamicDoubleParameters.Length);
                for (int i = 0; i < dynamicParams.Length; i++)
                {
                    dynamicParams[i] = dynamicDoubleParameters[i];
                }
                return SynthErrorCodes.eSynthDone;
            }

            // linear interpolation upsample (TODO: ought to replace with band-limited upsample)
            // unoptimized loop - optimize when scenarios become available
            private void Upsample(
                float[] source,
                int sourceOffset,
                float[] target,
                int targetOffset,
                int count,
                int rate,
                ref float last)
            {
                float left = last;

                for (int i = 0, n = 0; i < count; i++)
                {
                    Debug.Assert(n == i * rate);

                    float right = source[i + sourceOffset];

                    target[n + targetOffset] = left;
                    n++;
                    for (int c = 1; c < rate; c++, n++)
                    {
                        float rWeight = (float)c / rate;
                        target[n + targetOffset] = left + rWeight * (right - left);
                    }

                    left = right;
                }

                last = left;
            }

            public SynthErrorCodes Apply(
                float leftLoudness,
                float rightLoudness,
                float[] workspace32Base,
                int lOffset,
                int rOffset,
                int[] smoothedOffsets,
                bool[] smoothedNonConst,
                int[] scratchWorkspaces32Offsets,
                double[] scratchWorkspace64Base,
                int[] scratchWorksace64Offsets,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                Debug.Assert(nActualFrames % SynthParams.iOversampling == 0);

                // reinit each cycle because user code can reallocate or remove them
                leftWorkspaceHandle.floats = leftWorkspace;
                rightWorkspaceHandle.floats = rightWorkspace;

                int c = nActualFrames;
                double sr = SynthParams.dSamplingRate;

                if ((SynthParams.iOversampling == 1) || !disableOversampling)
                {
                    FloatVectorCopyUnaligned(
                        workspace32Base,
                        lOffset,
                        leftWorkspaceHandle.floats, // not vector-aligned
                        0,
                        nActualFrames);
                    FloatVectorCopyUnaligned(
                        workspace32Base,
                        rOffset,
                        rightWorkspaceHandle.floats, // not vector-aligned
                        0,
                        nActualFrames);
                }
                else
                {
                    // downsample
                    c /= SynthParams.iOversampling;
                    sr /= SynthParams.iOversampling;
                    for (int i = 0, j = 0; i < nActualFrames; i += SynthParams.iOversampling, j++)
                    {
                        leftWorkspaceHandle.floats[j] = workspace32Base[i + lOffset];
                        rightWorkspaceHandle.floats[j] = workspace32Base[i + rOffset];
                    }
                }

                int argCount = 1/*retval*/
                    + 1/*t*/
                    + 1/*bpm*/
                    + 1/*samplingRate*/
                    + 1/*leftdata*/
                    + 1/*rightdata*/
                    + 1/*count*/
                    + userState.Length/*user state*/
                    + dynamicParams.Length/*user params*/
                    + 1/*retaddr*/;
                SynthParams.FormulaEvalContext.EmptyParamStackEnsureCapacity(argCount);

                int StackNumElements;
                StackElement[] StackBase;
                SynthParams.FormulaEvalContext.GetRawStack(out StackBase, out StackNumElements);

                StackBase[StackNumElements++].Data.Double = SynthParams.dElapsedTimeInSeconds; /* t */

                StackBase[StackNumElements++].Data.Double = SynthParams.dCurrentBeatsPerMinute; /* bpm */

                StackBase[StackNumElements++].Data.Double = sr; /* samplingRate */

                StackBase[StackNumElements++].reference.arrayHandleFloat = leftWorkspaceHandle; // leftdata

                StackBase[StackNumElements++].reference.arrayHandleFloat = rightWorkspaceHandle; // rightdata

                StackBase[StackNumElements++].Data.Integer = c;

                for (int i = 0; i < userState.Length; i++)
                {
                    StackBase[StackNumElements++].reference.arrayHandleGeneric = userState[i]; // user state
                }

                for (int i = 0; i < dynamicParams.Length; i += 1)
                {
                    if (smoothingBuffers[i].arrayHandle == null)
                    {
                        StackBase[StackNumElements++].Data.Double = dynamicParams[i]; // user params
                    }
                    else
                    {
                        // re-initialize handle in case user code cleared it last time
                        smoothingBuffers[i].arrayHandle.floats = smoothingBuffers[paramIndexToSmoothingIndex[i]].vector;

                        // copy smoothed parameter data
                        FloatVectorCopyUnaligned(
                            workspace32Base,
                            smoothedOffsets[paramIndexToSmoothingIndex[i]],
                            smoothingBuffers[i].arrayHandle.floats, // target not aligned
                            0,
                            nActualFrames);

                        StackBase[StackNumElements++].reference.arrayHandleFloat = smoothingBuffers[i].arrayHandle; // user params
                    }
                }

                StackNumElements++; /* return address placeholder */

                SynthParams.FormulaEvalContext.UpdateRawStack(StackBase, StackNumElements);

                EvalErrInfoRec ErrorInfo;
                EvalErrors Error = PcodeSystem.EvaluatePcode(
                    SynthParams.FormulaEvalContext,
                    dataFunc,
                    SynthParams.perTrack.CodeCenter,
                    out ErrorInfo,
                    PcodeExternsNull.Default,
                    ref SynthParams.pcodeThreadContext);
                if (Error != EvalErrors.eEvalNoError)
                {
                    SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExUserEffectFunctionEvalError;
                    SynthParams.ErrorInfo.UserEvalErrorCode = Error;
                    SynthParams.ErrorInfo.UserEvalErrorInfo = ErrorInfo;
                    return SynthErrorCodes.eSynthErrorEx;
                }
                Debug.Assert(SynthParams.FormulaEvalContext.GetStackNumElements() == 1); // return value

                SynthParams.FormulaEvalContext.Clear();

                if ((leftWorkspaceHandle.floats != null) && (leftWorkspaceHandle.floats.Length >= c)
                    && (rightWorkspaceHandle.floats != null) && (rightWorkspaceHandle.floats.Length >= c))
                {
                    if ((SynthParams.iOversampling == 1) || !disableOversampling)
                    {
                        // remove NaN/Infinity - prevent user effect misbehavior from taking rest of system down
                        FloatVectorCopyReplaceNaNInf(
                            leftWorkspaceHandle.floats, // unaligned permitted
                            0,
                            workspace32Base,
                            lOffset,
                            nActualFrames);
                        FloatVectorCopyReplaceNaNInf(
                            rightWorkspaceHandle.floats, // unaligned permitted
                            0,
                            workspace32Base,
                            rOffset,
                            nActualFrames);
                    }
                    else
                    {
                        // upsample
                        Upsample(
                            leftWorkspaceHandle.floats,
                            0,
                            workspace32Base,
                            lOffset,
                            c,
                            SynthParams.iOversampling,
                            ref lastLeft);
                        Upsample(
                            rightWorkspaceHandle.floats,
                            0,
                            workspace32Base,
                            rOffset,
                            c,
                            SynthParams.iOversampling,
                            ref lastRight);
                        // remove NaN/Infinity - prevent user effect misbehavior from taking rest of system down
                        FloatVectorCopyReplaceNaNInf(
                            workspace32Base,
                            lOffset,
                            workspace32Base,
                            lOffset,
                            nActualFrames);
                        FloatVectorCopyReplaceNaNInf(
                            workspace32Base,
                            rOffset,
                            workspace32Base,
                            rOffset,
                            nActualFrames);
                    }
                }

                return SynthErrorCodes.eSynthDone;
            }

            public void Finalize(SynthParamRec SynthParams, bool writeOutputLogs)
            {
            }
        }
    }
}
