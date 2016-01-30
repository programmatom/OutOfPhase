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
    public static partial class Synthesizer
    {
        // nomenclature around parameter indexing:
        // 'index' refers to the position in single overall parameter definition array
        // 'offset' refers to the position within a type class (e.g. the i-th 'static double' parameter)

        public abstract class PluggableSpec
        {
            protected readonly IPluggableProcessorTemplate pluggableTemplate;
            protected readonly PluggableParameter[] parameters;
            protected readonly bool[] parameterSpecified;

            protected readonly int[] staticIntegerParams;
            protected readonly string[] staticStringParams;
            protected readonly PluggableEvaluableParam[] staticDoubleParams; // accent/formulas evaluated once-only at instance creation time

            protected readonly int dynamicDoubleParamCount;

            protected bool enabled;

            protected readonly int[] staticIntegerIndices; // convert offset within type class back to global parameter index
            protected readonly int[] staticStringIndices;
            protected readonly int[] staticDoubleIndices;
            protected readonly int[] dynamicDoubleIndices;


            protected PluggableSpec(IPluggableProcessorTemplate pluggableTemplate)
            {
                this.pluggableTemplate = pluggableTemplate;
                this.parameters = pluggableTemplate.ParametersDefinition;

                this.parameterSpecified = new bool[this.parameters.Length];

                int staticIntegerParamCount = 0;
                int staticStringParamCount = 0;
                int staticDoubleParamCount = 0;
                dynamicDoubleParamCount = 0;
                for (int i = 0; i < this.parameters.Length; i++)
                {
                    // while counting, also debug-validate the modifier flags on the parameter type
                    switch (this.parameters[i].Type & PluggableParameterType.TypeMask)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case PluggableParameterType.StaticInteger:
                            if ((this.parameters[i].Type & (PluggableParameterType.Smoothed | PluggableParameterType.SmoothedOptional)) != 0)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
                            staticIntegerParamCount++;
                            break;
                        case PluggableParameterType.StaticString:
                            if ((this.parameters[i].Type & (PluggableParameterType.Smoothed | PluggableParameterType.SmoothedOptional
                                | PluggableParameterType.Enumerated)) != 0)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
                            staticStringParamCount++;
                            break;
                        case PluggableParameterType.StaticDouble:
                            if ((this.parameters[i].Type & (PluggableParameterType.Smoothed | PluggableParameterType.SmoothedOptional
                                | PluggableParameterType.Enumerated)) != 0)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
                            staticDoubleParamCount++;
                            break;
                        case PluggableParameterType.DynamicDouble:
                            if ((this.parameters[i].Type & (PluggableParameterType.Enumerated)) != 0)
                            {
                                Debug.Assert(false);
                                throw new ArgumentException();
                            }
                            dynamicDoubleParamCount++;
                            break;
                    }
                }

                this.staticIntegerParams = new int[staticIntegerParamCount];
                this.staticStringParams = new string[staticStringParamCount];
                this.staticDoubleParams = new PluggableEvaluableParam[staticDoubleParamCount];

                this.staticIntegerIndices = new int[staticIntegerParamCount];
                this.staticStringIndices = new int[staticStringParamCount];
                this.staticDoubleIndices = new int[staticDoubleParamCount];
                this.dynamicDoubleIndices = new int[dynamicDoubleParamCount];
                int staticIntegerOffset = 0, staticStringOffset = 0, staticDoubleOffset = 0, dynamicDoubleOffset = 0;
                for (int i = 0; i < this.parameters.Length; i++)
                {
                    switch (this.parameters[i].Type & PluggableParameterType.TypeMask)
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case PluggableParameterType.StaticInteger:
                            this.staticIntegerIndices[staticIntegerOffset++] = i;
                            break;
                        case PluggableParameterType.StaticString:
                            this.staticStringIndices[staticStringOffset++] = i;
                            break;
                        case PluggableParameterType.StaticDouble:
                            this.staticDoubleIndices[staticDoubleOffset++] = i;
                            break;
                        case PluggableParameterType.DynamicDouble:
                            this.dynamicDoubleIndices[dynamicDoubleOffset++] = i;
                            break;
                    }
                }
                Debug.Assert(staticIntegerParamCount == staticIntegerOffset);
                Debug.Assert(staticStringParamCount == staticStringOffset);
                Debug.Assert(staticDoubleParamCount == staticDoubleOffset);
                Debug.Assert(dynamicDoubleParamCount == dynamicDoubleOffset);
            }

            public IPluggableProcessorTemplate PluggableTemplate { get { return pluggableTemplate; } }

            public int ParameterCount { get { return parameters.Length; } }

            public int StaticIntegerParamCount { get { return staticIntegerParams.Length; } }
            public int StaticStringParamCount { get { return staticStringParams.Length; } }
            public int StaticDoubleParamCount { get { return staticDoubleParams.Length; } }
            public int DynamicDoubleParamCount { get { return dynamicDoubleParamCount; } }

            public int[] StaticIntegerIndices { get { return staticIntegerIndices; } }
            public int[] StaticStringIndices { get { return staticStringIndices; } }
            public int[] StaticDoubleIndices { get { return staticDoubleIndices; } }
            public int[] DynamicDoubleIndices { get { return dynamicDoubleIndices; } }


            public int FindParameter(string parserName) // returns -1 if not found
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (String.Equals(parserName, parameters[i].ParserName))
                    {
                        return i;
                    }
                }
                return -1;
            }


            public PluggableParameterType GetParameterBaseType(int index)
            {
                return parameters[index].Type & PluggableParameterType.TypeMask;
            }

            public bool IsIntegerParameterEnumerated(int index)
            {
                Debug.Assert((parameters[index].Type & PluggableParameterType.TypeMask) == PluggableParameterType.StaticInteger);
                return (parameters[index].Type & PluggableParameterType.Enumerated) != 0;
            }

            public bool IsParameterOptional(int index)
            {
                return (parameters[index].Type & PluggableParameterType.Optional) != 0;
            }

            public bool IsDynamicParameterEnvelopeSmoothed(int index)
            {
                Debug.Assert((parameters[index].Type & PluggableParameterType.TypeMask) == PluggableParameterType.DynamicDouble);
                return (parameters[index].Type & PluggableParameterType.Smoothed) != 0;
            }

            public bool IsDynamicParameterEnvelopeSmoothedOptional(int index)
            {
                Debug.Assert((parameters[index].Type & PluggableParameterType.TypeMask) == PluggableParameterType.DynamicDouble);
                return (parameters[index].Type & PluggableParameterType.SmoothedOptional) != 0;
            }


            public int GetParameterIndexInClass(int index)
            {
                int offset = 0;
                for (int i = 0; i < index; i++)
                {
                    if ((parameters[i].Type & PluggableParameterType.TypeMask) == (parameters[index].Type & PluggableParameterType.TypeMask))
                    {
                        offset++;
                    }
                }
#if DEBUG
                switch (parameters[index].Type & PluggableParameterType.TypeMask)
                {
                    default:
                        Debug.Assert(false);
                        break;
                    case PluggableParameterType.StaticInteger:
                        Debug.Assert(unchecked((uint)offset < (uint)staticIntegerParams.Length));
                        break;
                    case PluggableParameterType.StaticString:
                        Debug.Assert(unchecked((uint)offset < (uint)staticStringParams.Length));
                        break;
                    case PluggableParameterType.StaticDouble:
                        Debug.Assert(unchecked((uint)offset < (uint)staticDoubleParams.Length));
                        break;
                    case PluggableParameterType.DynamicDouble:
                        Debug.Assert(unchecked((uint)offset < (uint)dynamicDoubleParamCount));
                        break;
                }
#endif
                return offset;
            }

            public bool IsParameterSpecified(int index)
            {
                return parameterSpecified[index];
            }

            public void SetStaticIntegerParameter(int index, int value)
            {
                Debug.Assert(!parameterSpecified[index]);
                int offset = GetParameterIndexInClass(index);
                staticIntegerParams[offset] = value;
                parameterSpecified[index] = true;
            }

            public int GetStaticIntegerParameter(int offset)
            {
                return staticIntegerParams[offset];
            }

            public void SetStaticStringParameter(int index, string value)
            {
                Debug.Assert(!parameterSpecified[index]);
                int offset = GetParameterIndexInClass(index);
                staticStringParams[offset] = value;
                parameterSpecified[index] = true;
            }

            public string GetStaticStringParameter(int offset)
            {
                return staticStringParams[offset];
            }

            public void SetStaticDoubleParameter(int index, PluggableEvaluableParam value)
            {
                Debug.Assert(!parameterSpecified[index]);
                int offset = GetParameterIndexInClass(index);
                staticDoubleParams[offset] = value;
                parameterSpecified[index] = true;
            }

            public PluggableEvaluableParam GetStaticDoubleParameter(int offset)
            {
                return staticDoubleParams[offset];
            }

            public bool Enabled { get { return enabled; } set { enabled = value; } }


            public string[] GetStaticStrings()
            {
                return staticStringParams;
            }
        }

        public class PluggableEvaluableParam
        {
            public readonly double constValue;
            public readonly AccentRec accent;
            public readonly PcodeRec formula;

            public PluggableEvaluableParam(double constValue, AccentRec accent, PcodeRec formula)
            {
                this.constValue = constValue;
                this.accent = accent;
                this.formula = formula;
            }
        }


        // pluggable spec (AST) for Oscillator and OscEffect

        public class PluggableOscSpec : PluggableSpec
        {
            private readonly PluggableEvaluableOscParam[] dynamicParams;
            private readonly int pitchParamOffset = -1;
            private readonly int loudnessParamOffset = -1;

            public PluggableOscSpec(IPluggableProcessorTemplate pluggableTemplate)
                : base(pluggableTemplate)
            {
                dynamicParams = new PluggableEvaluableOscParam[dynamicDoubleParamCount];

                PluggableParameter[] parameters = pluggableTemplate.ParametersDefinition;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if ((parameters[i].Type & PluggableParameterType.Pitch) != 0)
                    {
                        if (pitchParamOffset >= 0)
                        {
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }
                        pitchParamOffset = i;
                    }
                    if ((parameters[i].Type & PluggableParameterType.Loudness) != 0)
                    {
                        if (loudnessParamOffset >= 0)
                        {
                            Debug.Assert(false);
                            throw new ArgumentException();
                        }
                        loudnessParamOffset = i;
                    }
                }
            }

            public void SetDynamicDoubleParameter(int index, PluggableEvaluableOscParam value)
            {
                Debug.Assert(!parameterSpecified[index]);
                int offset = GetParameterIndexInClass(index);
                dynamicParams[offset] = value;
                parameterSpecified[index] = true;
            }

            public PluggableEvaluableOscParam GetDynamicParameter(int offset)
            {
                return dynamicParams[offset];
            }

            public int PitchParamOffset { get { return pitchParamOffset; } }
            public int LoudnessParamOffset { get { return loudnessParamOffset; } }
        }

        public class PluggableEvaluableOscParam
        {
            public EnvelopeRec ParamEnvelope;
            public LFOListSpecRec ParamLFO;

            public PluggableEvaluableOscParam(EnvelopeRec envelope, LFOListSpecRec lfo)
            {
                this.ParamEnvelope = envelope;
                this.ParamLFO = lfo;
            }
        }


        // pluggable spec (AST) for TrackEffect

        public class PluggableTrackSpec : PluggableSpec
        {
            private readonly PluggableEvaluableParam[] dynamicParams;

            public PluggableTrackSpec(IPluggableProcessorTemplate pluggableTemplate)
                : base(pluggableTemplate)
            {
                dynamicParams = new PluggableEvaluableParam[dynamicDoubleParamCount];
            }

            public void SetDynamicDoubleParameter(int index, PluggableEvaluableParam value)
            {
                Debug.Assert(!parameterSpecified[index]);
                int offset = GetParameterIndexInClass(index);
                dynamicParams[offset] = value;
                parameterSpecified[index] = true;
            }

            public PluggableEvaluableParam GetDynamicParameter(int offset)
            {
                return dynamicParams[offset];
            }
        }
    }
}
