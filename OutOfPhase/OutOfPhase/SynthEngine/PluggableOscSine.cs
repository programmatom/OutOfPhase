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
        // This is probably the simplest possible example to have of a pluggable processor, and a good place to start
        // if trying to build a new one.

        public class PluggableOscSineFactory : IPluggableProcessorFactory
        {
            public string ParserName { get { return "sine"; } }
            public Guid UniqueId { get { return uniqueId; } }
            private static readonly Guid uniqueId = new Guid("{B8DA5E53-27DF-4CF7-ADC3-3777DA94E0EB}");

            public ConfigInfo[] Configs { get { return null; } }

            public PluggableRole Roles { get { return PluggableRole.Oscillator; } }

            public int MaximumRequired32BitWorkspaceCount { get { return 0; } }
            public int MaximumRequired64BitWorkspaceCount { get { return 0; } }

            public int MaximumSmoothedParameterCount { get { return 2; } }

            public BuildInstrErrors Create(
                KeyValuePair<string, string[]>[] configsTokens,
                PluggableRole role,
                out IPluggableProcessorTemplate template,
                out string errorMessage)
            {
                if (configsTokens.Length != 0) // parser guarrantees to meet requirements specified by Configs property
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                if (role != PluggableRole.Oscillator) // parser gaurrantees to respect constraint of Roles property
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }

                errorMessage = null;
                template = new PluggableOscSineTemplate(this);
                return BuildInstrErrors.eBuildInstrNoError;
            }
        }

        public class PluggableOscSineTemplate : IPluggableProcessorTemplate
        {
            private readonly PluggableOscSineFactory factory;

            public PluggableOscSineTemplate(PluggableOscSineFactory factory)
            {
                this.factory = factory;
            }

            public IPluggableProcessorFactory Factory { get { return factory; } }

            public PluggableParameter[] ParametersDefinition { get { return ParametersDefinitions; } }
            private static readonly PluggableParameter[] ParametersDefinitions = new PluggableParameter[]
            {
                new PluggableParameter("loudness", PluggableParameterType.DynamicDouble | PluggableParameterType.Smoothed | PluggableParameterType.SmoothedOptional | PluggableParameterType.Loudness),
                new PluggableParameter("pitch", PluggableParameterType.DynamicDouble | PluggableParameterType.Pitch),
            };

            public SynthErrorCodes CheckUnreferencedObjects(
                string[] staticStringParameters,
                CheckUnrefParamRec context)
            {
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
                Debug.Assert(staticIntegerParameters.Length == 0);
                Debug.Assert(staticDoubleParameters.Length == 0);
                Debug.Assert(staticStringParameters.Length == 0);

                processor = new PluggableOscSine(!dynamicDoubleOptionallySmoothedParametersDegraded);

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
                Debug.Assert(false);
                throw new NotSupportedException();
            }

            public bool StaticRangeCheck(int parameterIndex, int value, out string errorMessage)
            {
                Debug.Assert(false);
                throw new NotSupportedException();
            }

            public int Required32BitWorkspaceCount { get { return 0; } }
            public int Required64BitWorkspaceCount { get { return 0; } }
        }


        public class PluggableOscSine : IPluggableProcessor
        {
            private double loudness, pitch;
            private double phase;
            private readonly bool smoothed;

            public PluggableOscSine(bool smoothed)
            {
                this.smoothed = smoothed;
            }

            public bool Running { get { return false; } } // allow driver's loudness envelope to determine lifetime

            public SynthErrorCodes Update(double[] dynamicDoubleParameters)
            {
                Debug.Assert(dynamicDoubleParameters.Length == 2);

                loudness = dynamicDoubleParameters[0];
                pitch = dynamicDoubleParameters[1];

                return SynthErrorCodes.eSynthDone;
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
                double delta = TWOPI * pitch / SynthParams.dSamplingRate;
                if (smoothed)
                {
                    for (int i = 0; i < nActualFrames; i++)
                    {
                        float smoothedLoudness = workspace32Base[i + smoothedOffsets[0]];
                        float v = (float)Math.Sin(phase) * smoothedLoudness;
                        workspace32Base[i + lOffset] += v * leftLoudness;
                        workspace32Base[i + rOffset] += v * rightLoudness;
                        phase += delta;
                        while (phase >= TWOPI)
                        {
                            phase -= TWOPI;
                        }
                    }
                }
                else
                {
                    leftLoudness *= (float)loudness;
                    rightLoudness *= (float)loudness;
                    for (int i = 0; i < nActualFrames; i++)
                    {
                        float v = (float)Math.Sin(phase);
                        workspace32Base[i + lOffset] += v * leftLoudness;
                        workspace32Base[i + rOffset] += v * rightLoudness;
                        phase += delta;
                        while (phase >= TWOPI)
                        {
                            phase -= TWOPI;
                        }
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
