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
        // TODO: change SynthParamsRec into a pluggable parameter package that is portable and contains only what's needed

        [Flags]
        public enum PluggableRole
        {
            Oscillator = 1 << 0,
            Effect = 1 << 1,
        }

        [Flags]
        public enum PluggableParameterType
        {
            StaticInteger = 1,
            StaticDouble = 2,
            StaticString = 3,
            DynamicDouble = 4,

            TypeMask = 7, // bitfield covering all of the preceding

            // augmentation flags
            Optional = 16, // valid for all
            Enumerated = 32, // for Integer - parser uses names instead of types
            Smoothed = 64, // for dynamic Double only - also provide envelope-smoothed float[] data
            SmoothedOptional = 128, // for dynamic Double only - also provide envelope-smoothed float[] data

            // oscillator-only augmentation flags
            Loudness = 256, // for dynamic Double only - which parameter serves as the loudness envelope (at most one param can have this)
            Pitch = 512, // for dynamic Double only - which parameter serves as the pitch envelope (at most one param can have this)
        }

        public struct PluggableParameter
        {
            public string ParserName;
            public PluggableParameterType Type;

            public PluggableParameter(string parserName, PluggableParameterType type)
            {
                this.ParserName = parserName;
                this.Type = type;
            }
        }

        public interface IPluggableProcessorFactory
        {
            string ParserName { get; }
            Guid UniqueId { get; }

            ConfigInfo[] Configs { get; }

            PluggableRole Roles { get; }

            int MaximumRequired32BitWorkspaceCount { get; }
            int MaximumRequired64BitWorkspaceCount { get; }

            int MaximumSmoothedParameterCount { get; }

            BuildInstrErrors Create(
                KeyValuePair<string, string[]>[] configsTokens,
                PluggableRole role,
                out IPluggableProcessorTemplate template,
                out string errorMessage);
        }

        [Flags]
        public enum ConfigForm
        {
            // ordered
            List = 0,
            KeyValuePairs = 1,
            BaseMask = 1, // bit field covering all of the preceding

            // flags
            Unique = 256,
            ValueAsDouble = 512,
            CommaSeparated = 1024,
            KeyValuePairColonDelimited = 2048,
            KeyValuePairEqualsDelimited = 4096,
            Required = 8192,
        }

        public class ConfigInfo
        {
            public readonly string ParserName;
            public readonly ConfigForm Form;
            public readonly int? MinCount;
            public readonly int? MaxCount;

            public ConfigInfo(string ParserName, ConfigForm Form)
            {
                this.ParserName = ParserName;
                this.Form = Form;
            }

            public ConfigInfo(string ParserName, ConfigForm Form, int? MinCount, int? MaxCount)
            {
                this.ParserName = ParserName;
                this.Form = Form;
                this.MinCount = MinCount;
                this.MaxCount = MaxCount;
            }

            public override bool Equals(object obj)
            {
                ConfigInfo other = obj as ConfigInfo;
                if (other != null)
                {
                    return String.Equals(this.ParserName, other.ParserName);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return ParserName.GetHashCode();
            }
        }

        public interface IPluggableProcessorTemplate
        {
            IPluggableProcessorFactory Factory { get; }

            PluggableParameter[] ParametersDefinition { get; }
            KeyValuePair<string, int>[] GetEnumeratedIntegerTokens(int parameterIndex);

            bool StaticRangeCheck(int parameterIndex, int value, out string errorMessage);
            bool StaticRangeCheck(int parameterIndex, double value, out string errorMessage);
            bool StaticRangeCheck(int parameterIndex, string value, out string errorMessage);

            SynthErrorCodes CheckUnreferencedObjects(
                string[] staticStringParameters,
                CheckUnrefParamRec context);

            int Required32BitWorkspaceCount { get; }
            int Required64BitWorkspaceCount { get; }

            SynthErrorCodes Create(
                int[] staticIntegerParameters,
                double[] staticDoubleParameters,
                string[] staticStringParameters,
                bool dynamicDoubleOptionallySmoothedParametersDegraded,
                SynthParamRec synthParams,
                out IPluggableProcessor processor);
        }

        public interface IPluggableProcessor
        {
            SynthErrorCodes Update(
                double[] dynamicDoubleParameters);

            SynthErrorCodes Apply(
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
                SynthParamRec SynthParams);

            bool Running { get; }

            void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs);
        }

        public interface IPluggableProcessorReleasable : IPluggableProcessor
        {
            void KeyUpSustain1();
            void KeyUpSustain2();
            void KeyUpSustain3();
        }


        //

        // Allocate vectors for containing workspace offsets - done at instrument creation time
        private static void PluggableAllocateWorkspaces(
            IPluggableProcessorTemplate pluggableTemplate,
            int smoothedParameterCount,
            out int[] smoothedWorkspaceBases,
            out int[] scratch32WorkspaceBases,
            out int[] scratch64WorkspaceBases,
            SynthParamRec synthParams)
        {
            if ((pluggableTemplate.Required32BitWorkspaceCount > pluggableTemplate.Factory.MaximumRequired32BitWorkspaceCount)
                || (pluggableTemplate.Required64BitWorkspaceCount > pluggableTemplate.Factory.MaximumRequired64BitWorkspaceCount))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            smoothedWorkspaceBases = new int[smoothedParameterCount];
            scratch32WorkspaceBases = new int[pluggableTemplate.Required32BitWorkspaceCount];
            Debug.Assert(smoothedWorkspaceBases.Length + scratch32WorkspaceBases.Length <= synthParams.AllScratchWorkspaces32.Length);

            scratch64WorkspaceBases = new int[pluggableTemplate.Required64BitWorkspaceCount];
            Debug.Assert(scratch64WorkspaceBases.Length <= synthParams.AllScratchWorkspaces64.Length);
        }

        // Assign workspace offsets into existing workspace basis arrays
        // IMPORTANT: this must be redone *every* cycle, since offsets may change from one thread to the next depending on
        // what padding was required to arrive at proper vector alignment of each workspace area.
        private static void PluggableAssignWorkspaces(
            int[] smoothedWorkspaceBases,
            int[] scratch32WorkspaceBases,
            int[] scratch64WorkspaceBases,
            SynthParamRec synthParams)
        {
            Debug.Assert(smoothedWorkspaceBases.Length + scratch32WorkspaceBases.Length <= synthParams.AllScratchWorkspaces32.Length);
            int j = 0;
            for (int i = 0; i < smoothedWorkspaceBases.Length; i++)
            {
                smoothedWorkspaceBases[i] = synthParams.AllScratchWorkspaces32[j++];
            }
            for (int i = 0; i < scratch32WorkspaceBases.Length; i++)
            {
                scratch32WorkspaceBases[i] = synthParams.AllScratchWorkspaces32[j++];
            }
            Debug.Assert(j <= synthParams.AllScratchWorkspaces32.Length);

            Debug.Assert(scratch64WorkspaceBases.Length <= synthParams.AllScratchWorkspaces64.Length);
            for (int i = 0; i < scratch64WorkspaceBases.Length; i++)
            {
                scratch64WorkspaceBases[i] = synthParams.AllScratchWorkspaces64[i];
            }
        }


        //

        // (parser-name, factory)
        public static readonly KeyValuePair<string, IPluggableProcessorFactory>[] BuiltInPluggableProcessors = new KeyValuePair<string, IPluggableProcessorFactory>[]
        {
            new KeyValuePair<string, IPluggableProcessorFactory>("sine", new PluggableOscSineFactory()),
            new KeyValuePair<string, IPluggableProcessorFactory>("usereffect", new PluggableEffectUserEffectFactory()),
        };

        public static KeyValuePair<string, IPluggableProcessorFactory>[] ExternalPluggableProcessors;

        public static IEnumerable<KeyValuePair<string, IPluggableProcessorFactory>> AllPluggableProcessors
        {
            get
            {
                List<KeyValuePair<string, IPluggableProcessorFactory>> list = new List<KeyValuePair<string, IPluggableProcessorFactory>>(BuiltInPluggableProcessors);
                list.AddRange(ExternalPluggableProcessors);
                return list;
            }
        }


        //

        public class PluggableOscillatorTemplate : IOscillatorTemplate
        {
            public readonly PluggableOscSpec Spec;

            public readonly int[] StaticIntegerParams;
            public readonly string[] StaticStringParams;
            public readonly PluggableOscStaticParam[] StaticDoubleParams;
            public readonly PluggableOscDynamicParam[] DynamicDoubleParams;
            public readonly int PitchParamOffset;
            public readonly int LoudnessParamOffset;
            public readonly int[] SmoothedParamOffsets;

            public readonly double OverallOscillatorLoudness;
            public readonly double TimeDisplacement;
            public readonly double FrequencyMultiplier;
            public readonly double FrequencyAdder;
            public readonly double StereoBias;
            public readonly EffectSpecListRec OscEffectTemplate; // effect specifier, may be null

            public OscillatorTypes Type { get { return OscillatorTypes.eOscillatorPluggable; } }

            public PluggableOscillatorTemplate(
                OscillatorRec Oscillator,
                SynthParamRec SynthParams)
            {
                this.Spec = GetOscillatorPluggableSpec(Oscillator);

                this.OverallOscillatorLoudness = OscillatorGetOutputLoudness(Oscillator);
                this.TimeDisplacement = OscillatorGetTimeDisplacement(Oscillator);
                this.FrequencyMultiplier = OscillatorGetFrequencyMultiplier(Oscillator)
                    / OscillatorGetFrequencyDivisor(Oscillator);
                this.FrequencyAdder = OscillatorGetFrequencyAdder(Oscillator);
                this.StereoBias = OscillatorGetStereoBias(Oscillator);

                this.PitchParamOffset = Spec.PitchParamOffset;
                this.LoudnessParamOffset = Spec.LoudnessParamOffset;

                this.StaticIntegerParams = new int[Spec.StaticIntegerParamCount];
                for (int i = 0; i < this.StaticIntegerParams.Length; i++)
                {
                    this.StaticIntegerParams[i] = Spec.GetStaticIntegerParameter(i);
                }

                this.StaticStringParams = new string[Spec.StaticStringParamCount];
                for (int i = 0; i < this.StaticStringParams.Length; i++)
                {
                    this.StaticStringParams[i] = Spec.GetStaticStringParameter(i);
                }

                this.StaticDoubleParams = new PluggableOscStaticParam[Spec.StaticDoubleParamCount];
                for (int i = 0; i < this.StaticDoubleParams.Length; i++)
                {
                    PluggableEvaluableParam param = Spec.GetStaticDoubleParameter(i);
                    this.StaticDoubleParams[i] = new PluggableOscStaticParam(
                        param.constValue,
                        param.accent,
                        param.formula);
                }

                this.DynamicDoubleParams = new PluggableOscDynamicParam[Spec.DynamicDoubleParamCount];
                for (int i = 0; i < this.DynamicDoubleParams.Length; i++)
                {
                    PluggableEvaluableOscParam param = Spec.GetDynamicParameter(i);
                    this.DynamicDoubleParams[i] = new PluggableOscDynamicParam(
                        param.ParamEnvelope,
                        param.ParamLFO);
                }

                List<int> smoothedOffsets = new List<int>();
                for (int i = 0; i < Spec.ParameterCount; i++)
                {
                    if (Spec.GetParameterBaseType(i) == PluggableParameterType.DynamicDouble)
                    {
                        if (Spec.IsDynamicParameterEnvelopeSmoothed(i))
                        {
                            smoothedOffsets.Add(i);
                        }
                    }
                }
                this.SmoothedParamOffsets = smoothedOffsets.ToArray();
                // ensure that plug-in designer provides consistent information across interfaces
                if (this.SmoothedParamOffsets.Length > this.Spec.PluggableTemplate.Factory.MaximumSmoothedParameterCount)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }

                this.OscEffectTemplate = GetOscillatorEffectList(Oscillator);
                if (GetEffectSpecListLength(this.OscEffectTemplate) == 0)
                {
                    this.OscEffectTemplate = null;
                }
            }

            public SynthErrorCodes NewState(
                double FreqForMultisampling,
                ref AccentRec Accents,
                double Loudness,
                double HurryUp,
                out int PreOriginTimeOut,
                double StereoPosition,
                double InitialFrequency,
                double PitchDisplacementDepthLimit,
                double PitchDisplacementRateLimit,
                int PitchDisplacementStartPoint,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams,
                out IOscillator StateOut)
            {
                PluggableOscillatorDriver driver;
                SynthErrorCodes result = PluggableOscillatorDriver.Create(
                    this,
                    FreqForMultisampling,
                    ref Accents,
                    Loudness,
                    HurryUp,
                    out PreOriginTimeOut,
                    StereoPosition,
                    InitialFrequency,
                    PitchDisplacementDepthLimit,
                    PitchDisplacementRateLimit,
                    PitchDisplacementStartPoint,
                    TrackInfo,
                    SynthParams,
                    out driver);
                StateOut = driver;
                return result;
            }
        }

        public struct PluggableOscStaticParam
        {
            public readonly double Value;
            public /*readonly but passed by ref*/ AccentRec Accents;
            public readonly PcodeRec Function;// null == constant, signature for EnvelopeInitParamEval()

            public PluggableOscStaticParam(double Value, AccentRec Accents, PcodeRec Function)
            {
                this.Value = Value;
                this.Accents = Accents;
                this.Function = Function;
            }
        }

        public struct PluggableOscDynamicParam
        {
            public readonly EnvelopeRec EnvelopeTemplate;
            public readonly LFOListSpecRec LFOTemplate;

            public PluggableOscDynamicParam(EnvelopeRec EnvelopeTemplate, LFOListSpecRec LFOTemplate)
            {
                this.EnvelopeTemplate = EnvelopeTemplate;
                this.LFOTemplate = LFOTemplate;
            }
        }

        public class PluggableOscillatorDriver : IOscillator
        {
            public readonly PluggableOscillatorTemplate Template;

            public readonly IPluggableProcessor Processor;

            public readonly PluggableOscDynamicEvalParam[] DynamicDoubleParams;
            public readonly double[] DynamicDoubleParamCurrent;
            public readonly int PitchParameterOffset;
            public readonly int LoudnessParameterOffset;

            // envelope tick countdown for pre-start time
            public int PreStartCountdown;
            // pitch lfo startup counter; negative = expired
            public int PitchLFOStartCountdown;

            // this field contains the overall volume scaling for everything so that we
            // can treat the envelopes as always going between 0 and 1.
            public double NoteLoudnessScaling;

            // postprocessing for this oscillator; may be null
            public readonly OscEffectGenRec OscEffectGenerator;

            public readonly bool[] smoothedNonConst;
            public readonly int[] smoothedWorkspaceBases;
            public readonly int[] scratch32WorkspaceBases;
            public readonly int[] scratch64WorkspaceBases;

            public float Panning;


            private PluggableOscillatorDriver(
                PluggableOscillatorTemplate Template,
                IPluggableProcessor Processor,
                PluggableOscDynamicEvalParam[] DynamicDoubleParams,
                double[] DynamicDoubleParamCurrent,
                double NoteLoudnessScaling,
                int PreStartCountdown,
                int PitchLFOStartCountdown,
                OscEffectGenRec OscEffectGenerator,
                float panning,
                SynthParamRec synthParams)
            {
                this.Template = Template;
                this.Processor = Processor;
                this.DynamicDoubleParams = DynamicDoubleParams;
                this.DynamicDoubleParamCurrent = DynamicDoubleParamCurrent;
                this.PitchParameterOffset = Template.PitchParamOffset;
                this.LoudnessParameterOffset = Template.LoudnessParamOffset;
                this.NoteLoudnessScaling = NoteLoudnessScaling;
                this.PreStartCountdown = PreStartCountdown;
                this.PitchLFOStartCountdown = PitchLFOStartCountdown;
                this.OscEffectGenerator = OscEffectGenerator;
                this.Panning = panning;

                this.smoothedNonConst = new bool[Template.SmoothedParamOffsets.Length];
                PluggableAllocateWorkspaces(
                    Template.Spec.PluggableTemplate,
                    Template.SmoothedParamOffsets.Length,
                    out this.smoothedWorkspaceBases,
                    out this.scratch32WorkspaceBases,
                    out this.scratch64WorkspaceBases,
                    synthParams);
            }

            public static SynthErrorCodes Create(
                PluggableOscillatorTemplate Template,
                double FreqForMultisampling,
                ref AccentRec Accents,
                double Loudness,
                double HurryUp,
                out int PreOriginTimeOut,
                double StereoPosition,
                double InitialFrequency,
                double PitchDisplacementDepthLimit,
                double PitchDisplacementRateLimit,
                int PitchDisplacementStartPoint,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec synthParams,
                out PluggableOscillatorDriver driver)
            {
                SynthErrorCodes error;

                PreOriginTimeOut = 0;
                driver = null;

                int maxPreOrigin = 0;

                double noteLoudnessScaling = Loudness * Template.OverallOscillatorLoudness;

                int preStartCountdown = (int)Math.Round(Template.TimeDisplacement * synthParams.dEnvelopeRate);
                maxPreOrigin = Math.Max(maxPreOrigin, -preStartCountdown);

                int pitchLFOStartCountdown = PitchDisplacementStartPoint;

                StereoPosition += Template.StereoBias;
                StereoPosition = Math.Min(Math.Max(StereoPosition, -1), 1);

                // TODO: optimize to remove this unless values actually referenced
                AccentRec LiveAccents;
                AccentRec LiveTrackAccents;
                _PlayTrackParamGetter(
                    TrackInfo,
                    out LiveAccents,
                    out LiveTrackAccents);
                double[] staticDoubleParams = new double[Template.StaticDoubleParams.Length];
                for (int i = 0; i < staticDoubleParams.Length; i++)
                {
                    staticDoubleParams[i] = Template.StaticDoubleParams[i].Value
                        * AccentProduct(ref Accents, ref Template.StaticDoubleParams[i].Accents);
                    if (Template.StaticDoubleParams[i].Function != null)
                    {
                        double temp;
                        error = EnvelopeInitParamEval(
                            Template.StaticDoubleParams[i].Function,
                            ref Accents,
                            ref LiveTrackAccents,
                            synthParams,
                            out temp);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                        staticDoubleParams[i] *= temp;
                    }

                    string errorMessage;
                    if (!Template.Spec.PluggableTemplate.StaticRangeCheck(
                        Template.Spec.DynamicDoubleIndices[i],
                        staticDoubleParams[i],
                        out errorMessage))
                    {
                        synthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExPluggableParameterOutOfRange;
                        synthParams.ErrorInfo.CustomError = errorMessage;
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                }

                PluggableOscDynamicEvalParam[] dynamicDoubleParams = new PluggableOscDynamicEvalParam[Template.DynamicDoubleParams.Length];
                double[] dynamicDoubleParamsCurrent = new double[Template.DynamicDoubleParams.Length];
                for (int i = 0; i < dynamicDoubleParams.Length; i++)
                {
                    int OnePreOrigin;
                    dynamicDoubleParams[i].Envelope = NewEnvelopeStateRecord(
                        Template.DynamicDoubleParams[i].EnvelopeTemplate,
                        ref Accents,
                        InitialFrequency,
                        1,
                        HurryUp,
                        out OnePreOrigin,
                        _PlayTrackParamGetter,
                        TrackInfo,
                        synthParams);
                    maxPreOrigin = Math.Max(maxPreOrigin, OnePreOrigin);

                    dynamicDoubleParams[i].LFOGenerator = NewLFOGenerator(
                        Template.DynamicDoubleParams[i].LFOTemplate,
                        out OnePreOrigin,
                        ref Accents,
                        InitialFrequency,
                        HurryUp,
                        1,
                        1,
                        FreqForMultisampling,
                        _PlayTrackParamGetter,
                        TrackInfo,
                        synthParams);
                    maxPreOrigin = Math.Max(maxPreOrigin, OnePreOrigin);

                    // initial value for envelope smoothing
                    dynamicDoubleParamsCurrent[i] = LFOGenInitialValue(
                        dynamicDoubleParams[i].LFOGenerator,
                        EnvelopeInitialValue(
                           dynamicDoubleParams[i].Envelope));
                }

                OscEffectGenRec oscEffectGenerator = null;
                if (Template.OscEffectTemplate != null)
                {
                    int OnePreOrigin;
                    SynthErrorCodes Result = NewOscEffectGenerator(
                       Template.OscEffectTemplate,
                       ref Accents,
                       HurryUp,
                       InitialFrequency,
                       FreqForMultisampling,
                       out OnePreOrigin,
                       TrackInfo,
                       synthParams,
                       out oscEffectGenerator);
                    if (Result != SynthErrorCodes.eSynthDone)
                    {
                        return Result;
                    }
                    maxPreOrigin = Math.Max(maxPreOrigin, OnePreOrigin);
                }

                IPluggableProcessor processor;
                error = Template.Spec.PluggableTemplate.Create(
                    Template.StaticIntegerParams,
                    staticDoubleParams,
                    Template.StaticStringParams,
                    false/*dynamicDoubleOptionallySmoothedParametersDegraded*/,
                    synthParams,
                    out processor);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                driver = new PluggableOscillatorDriver(
                    Template,
                    processor,
                    dynamicDoubleParams,
                    dynamicDoubleParamsCurrent,
                    noteLoudnessScaling,
                    preStartCountdown,
                    pitchLFOStartCountdown,
                    oscEffectGenerator,
                    (float)StereoPosition,
                    synthParams);

                // TODO: initialize Current to initial envelope value (for smoothing)

                PreOriginTimeOut = maxPreOrigin;

                return SynthErrorCodes.eSynthDone;
            }

            public SynthErrorCodes UpdateEnvelopes(
                double NewFrequencyHertz,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;

                // this is for the benefit of processing -- envelope generators do their own pre-origin sequencing
                if (PreStartCountdown > 0)
                {
                    PreStartCountdown--;
                }

                // frequency computation first
                NewFrequencyHertz = NewFrequencyHertz * Template.FrequencyMultiplier + Template.FrequencyAdder;
                DynamicDoubleParams[PitchParameterOffset].Previous = DynamicDoubleParamCurrent[PitchParameterOffset];
                if (PitchLFOStartCountdown > 0)
                {
                    PitchLFOStartCountdown -= 1;
                    DynamicDoubleParamCurrent[PitchParameterOffset] = NewFrequencyHertz;
                }
                else
                {
                    error = SynthErrorCodes.eSynthDone;
                    DynamicDoubleParamCurrent[PitchParameterOffset] =
                        LFOGenUpdateCycle(
                            DynamicDoubleParams[PitchParameterOffset].LFOGenerator,
                            NewFrequencyHertz * EnvelopeUpdate(
                                DynamicDoubleParams[PitchParameterOffset].Envelope,
                                NewFrequencyHertz,
                                SynthParams,
                                ref error),
                            NewFrequencyHertz,
                            SynthParams,
                            ref error);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    if (i == PitchParameterOffset)
                    {
                        continue; // already did it
                    }

                    double scaling = i == LoudnessParameterOffset ? NoteLoudnessScaling : 1;

                    error = SynthErrorCodes.eSynthDone;
                    DynamicDoubleParams[i].Previous = DynamicDoubleParamCurrent[i];
                    DynamicDoubleParamCurrent[i] = scaling *
                        LFOGenUpdateCycle(
                            DynamicDoubleParams[i].LFOGenerator,
                            EnvelopeUpdate(
                                DynamicDoubleParams[i].Envelope,
                                NewFrequencyHertz,
                                SynthParams,
                                ref error),
                            NewFrequencyHertz,
                            SynthParams,
                            ref error);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                if (OscEffectGenerator != null)
                {
                    error = OscEffectGeneratorUpdateEnvelopes(
                        OscEffectGenerator,
                        NewFrequencyHertz,
                        SynthParams);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                error = Processor.Update(DynamicDoubleParamCurrent);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                return SynthErrorCodes.eSynthDone;
            }

            public void FixUpPreOrigin(int ActualPreOrigin)
            {
                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeStateFixUpInitialDelay(
                        DynamicDoubleParams[i].Envelope,
                        ActualPreOrigin);
                    LFOGeneratorFixEnvelopeOrigins(
                        DynamicDoubleParams[i].LFOGenerator,
                        ActualPreOrigin);
                }

                if (OscEffectGenerator != null)
                {
                    FixUpOscEffectGeneratorPreOrigin(
                        OscEffectGenerator,
                        ActualPreOrigin);
                }

                PreStartCountdown += ActualPreOrigin;
            }

            public void KeyUpSustain1()
            {
                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeKeyUpSustain1(
                        DynamicDoubleParams[i].Envelope);
                    LFOGeneratorKeyUpSustain1(
                        DynamicDoubleParams[i].LFOGenerator);
                }

                if (OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain1(
                        OscEffectGenerator);
                }

                IPluggableProcessorReleasable processorRelease = Processor as IPluggableProcessorReleasable;
                if (processorRelease != null)
                {
                    processorRelease.KeyUpSustain1();
                }
            }

            public void KeyUpSustain2()
            {
                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeKeyUpSustain2(
                        DynamicDoubleParams[i].Envelope);
                    LFOGeneratorKeyUpSustain2(
                        DynamicDoubleParams[i].LFOGenerator);
                }

                if (OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain2(
                        OscEffectGenerator);
                }

                IPluggableProcessorReleasable processorRelease = Processor as IPluggableProcessorReleasable;
                if (processorRelease != null)
                {
                    processorRelease.KeyUpSustain2();
                }
            }

            public void KeyUpSustain3()
            {
                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeKeyUpSustain3(
                        DynamicDoubleParams[i].Envelope);
                    LFOGeneratorKeyUpSustain3(
                        DynamicDoubleParams[i].LFOGenerator);
                }

                if (OscEffectGenerator != null)
                {
                    OscEffectKeyUpSustain3(
                        OscEffectGenerator);
                }

                IPluggableProcessorReleasable processorRelease = Processor as IPluggableProcessorReleasable;
                if (processorRelease != null)
                {
                    processorRelease.KeyUpSustain3();
                }
            }

            public void Restart(
                ref AccentRec NewAccents,
                double NewLoudness,
                double NewHurryUp,
                bool RetriggerEnvelopes,
                double NewStereoPosition,
                double NewInitialFrequency,
                double PitchDisplacementDepthLimit,
                double PitchDisplacementRateLimit,
                SynthParamRec SynthParams)
            {
                NoteLoudnessScaling = NewLoudness * Template.OverallOscillatorLoudness;

                NewStereoPosition += Template.StereoBias;
                NewStereoPosition = Math.Min(Math.Max(NewStereoPosition, -1), 1);

                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeRetriggerFromOrigin(
                        DynamicDoubleParams[i].Envelope,
                        ref NewAccents,
                        NewInitialFrequency,
                        1,
                        NewHurryUp,
                        RetriggerEnvelopes,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        DynamicDoubleParams[i].LFOGenerator,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        i != PitchParameterOffset ? 1 : PitchDisplacementDepthLimit,
                        i != PitchParameterOffset ? 1 : PitchDisplacementRateLimit,
                        RetriggerEnvelopes,
                        SynthParams);
                }

                if (OscEffectGenerator != null)
                {
                    OscEffectGeneratorRetriggerFromOrigin(
                        OscEffectGenerator,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        RetriggerEnvelopes,
                        SynthParams);
                }
                // do not reset PitchLFOStartCountdown since we can't give it a proper value
                // to do the expected thing, and we'll be interrupting the phase of the LFO
                // wave generator
            }

            public SynthErrorCodes Generate(
                int nActualFrames,
                float[] workspace,
                int RawBufferLOffset,
                int RawBufferROffset,
                int PrivateWorkspaceLOffset,
                int PrivateWorkspaceROffset,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error = SynthErrorCodes.eSynthDone;

                PluggableAssignWorkspaces(
                    this.smoothedWorkspaceBases,
                    this.scratch32WorkspaceBases,
                    this.scratch64WorkspaceBases,
                    SynthParams);

                for (int i = 0; i < Template.SmoothedParamOffsets.Length; i++)
                {
                    int index = Template.SmoothedParamOffsets[i];
                    float localCurrentValue = (float)DynamicDoubleParamCurrent[index];
                    float localPreviousValue = (float)DynamicDoubleParams[index].Previous;

                    // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                    if (IsLFOSampleAndHold(DynamicDoubleParams[i].LFOGenerator))
                    {
                        localPreviousValue = localCurrentValue;
                    }

                    smoothedNonConst[i] = localPreviousValue != localCurrentValue;

                    if (!EnvelopeCurrentSegmentExponential(DynamicDoubleParams[i].Envelope))
                    {
                        FloatVectorAdditiveRecurrence(
                            SynthParams.workspace,
                            smoothedWorkspaceBases[i],
                            localPreviousValue,
                            localCurrentValue,
                            nActualFrames);
                    }
                    else
                    {
                        FloatVectorMultiplicativeRecurrence(
                            SynthParams.workspace,
                            smoothedWorkspaceBases[i],
                            localPreviousValue,
                            localCurrentValue,
                            nActualFrames);
                    }
                }

                if (PreStartCountdown <= 0)
                {
                    if (OscEffectGenerator == null)
                    {
                        // normal case
                        error = Processor.Apply(
                            .5f - .5f * Panning,
                            .5f + .5f * Panning,
                            workspace,
                            RawBufferLOffset,
                            RawBufferROffset,
                            smoothedWorkspaceBases,
                            smoothedNonConst,
                            scratch32WorkspaceBases,
                            UnsafeArrayCastLong.AsDoubles(SynthParams.ScratchWorkspace3),
                            scratch64WorkspaceBases,
                            nActualFrames,
                            SynthParams);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            goto Error;
                        }
                    }
                    else
                    {
                        // effect postprocessing case

                        // initialize private storage
                        FloatVectorZero(
                            workspace,
                            PrivateWorkspaceLOffset,
                            nActualFrames);
                        FloatVectorZero(
                            workspace,
                            PrivateWorkspaceROffset,
                            nActualFrames);

                        // generate waveform
                        error = Processor.Apply(
                            .5f - .5f * Panning,
                            .5f + .5f * Panning,
                            workspace,
                            RawBufferLOffset,
                            RawBufferROffset,
                            smoothedWorkspaceBases,
                            smoothedNonConst,
                            scratch32WorkspaceBases,
                            UnsafeArrayCastLong.AsDoubles(SynthParams.ScratchWorkspace3),
                            scratch64WorkspaceBases,
                            nActualFrames,
                            SynthParams);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            goto Error;
                        }

                        /* apply processor to it */
                        error = ApplyOscEffectGenerator(
                            OscEffectGenerator,
                            workspace,
                            PrivateWorkspaceLOffset,
                            PrivateWorkspaceROffset,
                            nActualFrames,
                            SynthParams);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            goto Error;
                        }

                        /* copy out data */
                        FloatVectorAcc(
                            workspace,
                            PrivateWorkspaceLOffset,
                            workspace,
                            RawBufferLOffset,
                            nActualFrames);
                        FloatVectorAcc(
                            workspace,
                            PrivateWorkspaceROffset,
                            workspace,
                            RawBufferROffset,
                            nActualFrames);
                    }
                }


            Error:

                return error;
            }

            public bool IsItFinished()
            {
                if (Processor.Running)
                {
                    return false;
                }
                return IsEnvelopeAtEnd(DynamicDoubleParams[LoudnessParameterOffset].Envelope);
            }

            public void Finalize(SynthParamRec SynthParams, bool writeOutputLogs)
            {
                Processor.Finalize(SynthParams, writeOutputLogs);
                if (OscEffectGenerator != null)
                {
                    FinalizeOscEffectGenerator(OscEffectGenerator, SynthParams, writeOutputLogs);
                }
            }
        }

        public struct PluggableOscDynamicEvalParam
        {
            //public double Current; -- stored separately
            public double Previous;
            public EvalEnvelopeRec Envelope;
            public LFOGenRec LFOGenerator;
        }


        //

        public class PluggableOscEffectTemplate
        {
            public readonly PluggableOscSpec Spec;

            public readonly int[] StaticIntegerParams;
            public readonly string[] StaticStringParams;
            public readonly PluggableOscStaticParam[] StaticDoubleParams;
            public readonly PluggableOscDynamicParam[] DynamicDoubleParams;
            public readonly int[] SmoothedParamOffsets;

            public PluggableOscEffectTemplate(
                PluggableOscSpec Effect,
                SynthParamRec SynthParams)
            {
                this.Spec = Effect;

                this.StaticIntegerParams = new int[Spec.StaticIntegerParamCount];
                for (int i = 0; i < this.StaticIntegerParams.Length; i++)
                {
                    this.StaticIntegerParams[i] = Spec.GetStaticIntegerParameter(i);
                }

                this.StaticStringParams = new string[Spec.StaticStringParamCount];
                for (int i = 0; i < this.StaticStringParams.Length; i++)
                {
                    this.StaticStringParams[i] = Spec.GetStaticStringParameter(i);
                }

                this.StaticDoubleParams = new PluggableOscStaticParam[Spec.StaticDoubleParamCount];
                for (int i = 0; i < this.StaticDoubleParams.Length; i++)
                {
                    PluggableEvaluableParam param = Spec.GetStaticDoubleParameter(i);
                    this.StaticDoubleParams[i] = new PluggableOscStaticParam(
                        param.constValue,
                        param.accent,
                        param.formula);
                }

                this.DynamicDoubleParams = new PluggableOscDynamicParam[Spec.DynamicDoubleParamCount];
                for (int i = 0; i < this.DynamicDoubleParams.Length; i++)
                {
                    PluggableEvaluableOscParam param = Spec.GetDynamicParameter(i);
                    this.DynamicDoubleParams[i] = new PluggableOscDynamicParam(
                        param.ParamEnvelope,
                        param.ParamLFO);
                }

                List<int> smoothedOffsets = new List<int>();
                for (int i = 0; i < Spec.ParameterCount; i++)
                {
                    if (Spec.GetParameterBaseType(i) == PluggableParameterType.DynamicDouble)
                    {
                        if (Spec.IsDynamicParameterEnvelopeSmoothed(i))
                        {
                            smoothedOffsets.Add(i);
                        }
                    }
                }
                this.SmoothedParamOffsets = smoothedOffsets.ToArray();
                // ensure that plug-in designer provides consistent information across interfaces
                if (this.SmoothedParamOffsets.Length > this.Spec.PluggableTemplate.Factory.MaximumSmoothedParameterCount)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
            }

            public SynthErrorCodes Create(
                ref AccentRec Accents,
                double HurryUp,
                double InitialFrequency,
                double FreqForMultisampling,
                out int PreOriginTimeOut,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams,
                out IOscillatorEffect effectOut)
            {
                PluggableOscEffectDriver driver;
                SynthErrorCodes result = PluggableOscEffectDriver.Create(
                    this,
                    ref Accents,
                    HurryUp,
                    InitialFrequency,
                    FreqForMultisampling,
                    out PreOriginTimeOut,
                    TrackInfo,
                    SynthParams,
                    out driver);
                effectOut = driver;
                return result;
            }
        }

        public class PluggableOscEffectDriver : IOscillatorEffect
        {
            public readonly PluggableOscEffectTemplate Template;

            public readonly IPluggableProcessor Processor;

            public readonly PluggableOscDynamicEvalParam[] DynamicDoubleParams;
            public readonly double[] DynamicDoubleParamCurrent;

            public readonly bool[] smoothedNonConst;
            public readonly int[] smoothedWorkspaceBases;
            public readonly int[] scratch32WorkspaceBases;
            public readonly int[] scratch64WorkspaceBases;

            public PluggableOscEffectDriver(
                PluggableOscEffectTemplate Template,
                IPluggableProcessor Processor,
                PluggableOscDynamicEvalParam[] DynamicDoubleParams,
                double[] DynamicDoubleParamsCurrent,
                SynthParamRec synthParams)
            {
                this.Template = Template;
                this.Processor = Processor;
                this.DynamicDoubleParams = DynamicDoubleParams;
                this.DynamicDoubleParamCurrent = DynamicDoubleParamsCurrent;

                this.smoothedNonConst = new bool[Template.SmoothedParamOffsets.Length];
                PluggableAllocateWorkspaces(
                    Template.Spec.PluggableTemplate,
                    Template.SmoothedParamOffsets.Length,
                    out this.smoothedWorkspaceBases,
                    out this.scratch32WorkspaceBases,
                    out this.scratch64WorkspaceBases,
                    synthParams);
            }

            public static SynthErrorCodes Create(
                PluggableOscEffectTemplate Template,
                ref AccentRec Accents,
                double HurryUp,
                double InitialFrequency,
                double FreqForMultisampling,
                out int PreOriginTimeOut,
                PlayTrackInfoRec TrackInfo,
                SynthParamRec SynthParams,
                out PluggableOscEffectDriver driver)
            {
                SynthErrorCodes error;

                driver = null;
                PreOriginTimeOut = 0;

                int maxPreOrigin = 0;

                // TODO: optimize to remove this unless values actually referenced
                AccentRec LiveAccents;
                AccentRec LiveTrackAccents;
                _PlayTrackParamGetter(
                    TrackInfo,
                    out LiveAccents,
                    out LiveTrackAccents);
                double[] staticDoubleParams = new double[Template.StaticDoubleParams.Length];
                for (int i = 0; i < staticDoubleParams.Length; i++)
                {
                    staticDoubleParams[i] = Template.StaticDoubleParams[i].Value
                        * AccentProduct(ref Accents, ref Template.StaticDoubleParams[i].Accents);
                    if (Template.StaticDoubleParams[i].Function != null)
                    {
                        double temp;
                        error = EnvelopeInitParamEval(
                            Template.StaticDoubleParams[i].Function,
                            ref Accents,
                            ref LiveTrackAccents,
                            SynthParams,
                            out temp);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                        staticDoubleParams[i] *= temp;
                    }

                    string errorMessage;
                    if (!Template.Spec.PluggableTemplate.StaticRangeCheck(
                        Template.Spec.DynamicDoubleIndices[i],
                        staticDoubleParams[i],
                        out errorMessage))
                    {
                        SynthParams.ErrorInfo.ErrorEx = SynthErrorSubCodes.eSynthErrorExPluggableParameterOutOfRange;
                        SynthParams.ErrorInfo.CustomError = errorMessage;
                        return SynthErrorCodes.eSynthErrorEx;
                    }
                }

                PluggableOscDynamicEvalParam[] dynamicDoubleParams = new PluggableOscDynamicEvalParam[Template.DynamicDoubleParams.Length];
                double[] dynamicDoubleParamsCurrent = new double[Template.DynamicDoubleParams.Length];
                for (int i = 0; i < dynamicDoubleParams.Length; i++)
                {
                    int OnePreOrigin;
                    dynamicDoubleParams[i].Envelope = NewEnvelopeStateRecord(
                        Template.DynamicDoubleParams[i].EnvelopeTemplate,
                        ref Accents,
                        InitialFrequency,
                        1,
                        HurryUp,
                        out OnePreOrigin,
                        _PlayTrackParamGetter,
                        TrackInfo,
                        SynthParams);
                    maxPreOrigin = Math.Max(maxPreOrigin, OnePreOrigin);

                    dynamicDoubleParams[i].LFOGenerator = NewLFOGenerator(
                        Template.DynamicDoubleParams[i].LFOTemplate,
                        out OnePreOrigin,
                        ref Accents,
                        InitialFrequency,
                        HurryUp,
                        1,
                        1,
                        FreqForMultisampling,
                        _PlayTrackParamGetter,
                        TrackInfo,
                        SynthParams);
                    maxPreOrigin = Math.Max(maxPreOrigin, OnePreOrigin);

                    // initial value for envelope smoothing
                    dynamicDoubleParamsCurrent[i] = LFOGenInitialValue(
                        dynamicDoubleParams[i].LFOGenerator,
                        EnvelopeInitialValue(
                           dynamicDoubleParams[i].Envelope));
                }

                IPluggableProcessor processor;
                error = Template.Spec.PluggableTemplate.Create(
                    Template.StaticIntegerParams,
                    staticDoubleParams,
                    Template.StaticStringParams,
                    false/*dynamicDoubleOptionallySmoothedParametersDegraded*/,
                    SynthParams,
                    out processor);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                driver = new PluggableOscEffectDriver(
                    Template,
                    processor,
                    dynamicDoubleParams,
                    dynamicDoubleParamsCurrent,
                    SynthParams);
                PreOriginTimeOut = maxPreOrigin;
                return SynthErrorCodes.eSynthDone;
            }

            public void OscFixEnvelopeOrigins(int ActualPreOrigin)
            {
                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeStateFixUpInitialDelay(
                        DynamicDoubleParams[i].Envelope,
                        ActualPreOrigin);
                    LFOGeneratorFixEnvelopeOrigins(
                        DynamicDoubleParams[i].LFOGenerator,
                        ActualPreOrigin);
                }
            }

            public SynthErrorCodes OscUpdateEnvelopes(
                double OscillatorFrequency,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;

                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    error = SynthErrorCodes.eSynthDone;
                    DynamicDoubleParams[i].Previous = DynamicDoubleParamCurrent[i];
                    DynamicDoubleParamCurrent[i] = LFOGenUpdateCycle(
                        DynamicDoubleParams[i].LFOGenerator,
                        EnvelopeUpdate(
                            DynamicDoubleParams[i].Envelope,
                            OscillatorFrequency,
                            SynthParams,
                            ref error),
                        OscillatorFrequency,
                        SynthParams,
                        ref error);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                error = Processor.Update(DynamicDoubleParamCurrent);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                return SynthErrorCodes.eSynthDone;
            }

            public void OscKeyUpSustain1()
            {
                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeKeyUpSustain1(
                        DynamicDoubleParams[i].Envelope);
                    LFOGeneratorKeyUpSustain1(
                        DynamicDoubleParams[i].LFOGenerator);
                }

                IPluggableProcessorReleasable processorRelease = Processor as IPluggableProcessorReleasable;
                if (processorRelease != null)
                {
                    processorRelease.KeyUpSustain1();
                }
            }

            public void OscKeyUpSustain2()
            {
                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeKeyUpSustain2(
                        DynamicDoubleParams[i].Envelope);
                    LFOGeneratorKeyUpSustain2(
                        DynamicDoubleParams[i].LFOGenerator);
                }

                IPluggableProcessorReleasable processorRelease = Processor as IPluggableProcessorReleasable;
                if (processorRelease != null)
                {
                    processorRelease.KeyUpSustain2();
                }
            }

            public void OscKeyUpSustain3()
            {
                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeKeyUpSustain3(
                        DynamicDoubleParams[i].Envelope);
                    LFOGeneratorKeyUpSustain3(
                        DynamicDoubleParams[i].LFOGenerator);
                }

                IPluggableProcessorReleasable processorRelease = Processor as IPluggableProcessorReleasable;
                if (processorRelease != null)
                {
                    processorRelease.KeyUpSustain3();
                }
            }

            public void OscRetriggerEnvelopes(
                ref AccentRec NewAccents,
                double NewHurryUp,
                double NewInitialFrequency,
                bool ActuallyRetrigger,
                SynthParamRec SynthParams)
            {
                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    EnvelopeRetriggerFromOrigin(
                        DynamicDoubleParams[i].Envelope,
                        ref NewAccents,
                        NewInitialFrequency,
                        1,
                        NewHurryUp,
                        ActuallyRetrigger,
                        SynthParams);
                    LFOGeneratorRetriggerFromOrigin(
                        DynamicDoubleParams[i].LFOGenerator,
                        ref NewAccents,
                        NewInitialFrequency,
                        NewHurryUp,
                        1,
                        1,
                        ActuallyRetrigger,
                        SynthParams);
                }
            }

            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                PluggableAssignWorkspaces(
                    this.smoothedWorkspaceBases,
                    this.scratch32WorkspaceBases,
                    this.scratch64WorkspaceBases,
                    SynthParams);

                for (int i = 0; i < Template.SmoothedParamOffsets.Length; i++)
                {
                    int index = Template.SmoothedParamOffsets[i];
                    float localCurrentValue = (float)DynamicDoubleParamCurrent[index];
                    float localPreviousValue = (float)DynamicDoubleParams[index].Previous;

                    // intentional discretization by means of sample-and-hold lfo should not be smoothed.
                    if (IsLFOSampleAndHold(DynamicDoubleParams[i].LFOGenerator))
                    {
                        localPreviousValue = localCurrentValue;
                    }

                    smoothedNonConst[i] = localPreviousValue != localCurrentValue;

                    if (!EnvelopeCurrentSegmentExponential(DynamicDoubleParams[i].Envelope))
                    {
                        FloatVectorAdditiveRecurrence(
                            SynthParams.workspace,
                            smoothedWorkspaceBases[i],
                            localPreviousValue,
                            localCurrentValue,
                            nActualFrames);
                    }
                    else
                    {
                        FloatVectorMultiplicativeRecurrence(
                            SynthParams.workspace,
                            smoothedWorkspaceBases[i],
                            localPreviousValue,
                            localCurrentValue,
                            nActualFrames);
                    }
                }

                return Processor.Apply(
                    1,
                    1,
                    workspace,
                    lOffset,
                    rOffset,
                    smoothedWorkspaceBases,
                    smoothedNonConst,
                    scratch32WorkspaceBases,
                    UnsafeArrayCastLong.AsDoubles(SynthParams.ScratchWorkspace3),
                    scratch64WorkspaceBases,
                    nActualFrames,
                    SynthParams);
            }

            public void Finalize(
                SynthParamRec SynthParams,
                bool writeOutputLogs)
            {
                Processor.Finalize(SynthParams, writeOutputLogs);
            }
        }


        //

        public class PluggableTrackEffectTemplate
        {
            public readonly PluggableTrackSpec Spec;

            public readonly int[] StaticIntegerParams;
            public readonly string[] StaticStringParams;
            public readonly PluggableTrackStaticParam[] StaticDoubleParams;
            public readonly PluggableTrackDynamicParam[] DynamicDoubleParams;
            public readonly int[] SmoothedParamOffsets;
            public readonly bool OptionallySmoothedParametersDegraded;

            public PluggableTrackEffectTemplate(
                PluggableTrackSpec Effect,
                SynthParamRec SynthParams)
            {
                this.Spec = Effect;

                this.StaticIntegerParams = new int[Spec.StaticIntegerParamCount];
                for (int i = 0; i < this.StaticIntegerParams.Length; i++)
                {
                    this.StaticIntegerParams[i] = Spec.GetStaticIntegerParameter(i);
                }

                this.StaticStringParams = new string[Spec.StaticStringParamCount];
                for (int i = 0; i < this.StaticStringParams.Length; i++)
                {
                    this.StaticStringParams[i] = Spec.GetStaticStringParameter(i);
                }

                this.StaticDoubleParams = new PluggableTrackStaticParam[Spec.StaticDoubleParamCount];
                for (int i = 0; i < this.StaticDoubleParams.Length; i++)
                {
                    PluggableEvaluableParam param = Spec.GetStaticDoubleParameter(i);
                    this.StaticDoubleParams[i] = new PluggableTrackStaticParam(
                        param.constValue,
                        param.formula);
                }

                this.DynamicDoubleParams = new PluggableTrackDynamicParam[Spec.DynamicDoubleParamCount];
                for (int i = 0; i < this.DynamicDoubleParams.Length; i++)
                {
                    PluggableEvaluableParam param = Spec.GetDynamicParameter(i);
                    this.DynamicDoubleParams[i] = new PluggableTrackDynamicParam(
                        param.constValue,
                        param.accent,
                        param.formula);

                }

                List<int> smoothedOffsets = new List<int>();
                for (int i = 0; i < Spec.ParameterCount; i++)
                {
                    if (Spec.GetParameterBaseType(i) == PluggableParameterType.DynamicDouble)
                    {
                        if (Spec.IsDynamicParameterEnvelopeSmoothed(i))
                        {
                            if (!Spec.IsDynamicParameterEnvelopeSmoothedOptional(i))
                            {
                                smoothedOffsets.Add(i);
                            }
                            else
                            {
                                OptionallySmoothedParametersDegraded = true;
                            }
                        }
                    }
                }
                this.SmoothedParamOffsets = smoothedOffsets.ToArray();
                // ensure that plug-in designer provides consistent information across interfaces
                if (this.SmoothedParamOffsets.Length > this.Spec.PluggableTemplate.Factory.MaximumSmoothedParameterCount)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
            }

            public SynthErrorCodes Create(
                SynthParamRec SynthParams,
                out ITrackEffect effect)
            {
                PluggableTrackEffectDriver driver;
                SynthErrorCodes result = PluggableTrackEffectDriver.Create(
                    this,
                    SynthParams,
                    out driver);
                effect = driver;
                return result;
            }
        }

        public struct PluggableTrackStaticParam
        {
            public readonly double Value;
            public readonly PcodeRec Formula;

            public PluggableTrackStaticParam(double Value, PcodeRec Formula)
            {
                this.Value = Value;
                this.Formula = Formula;
            }
        }

        public struct PluggableTrackDynamicParam
        {
            public readonly double Value;
            public /*readonly but passed by ref*/ AccentRec Accent;
            public readonly PcodeRec Formula;

            public PluggableTrackDynamicParam(double Value, AccentRec Accent, PcodeRec Formula)
            {
                this.Value = Value;
                this.Accent = Accent;
                this.Formula = Formula;
            }
        }

        public class PluggableTrackEffectDriver : ITrackEffect
        {
            public readonly PluggableTrackEffectTemplate Template;

            public readonly IPluggableProcessor Processor;

            public readonly PluggableTrackDynamicEvalParam[] DynamicDoubleParams;
            public readonly double[] DynamicDoubleParamsCurrent;

            public readonly bool[] smoothedNonConst;
            public readonly int[] smoothedWorkspaceBases;
            public readonly int[] scratch32WorkspaceBases;
            public readonly int[] scratch64WorkspaceBases;

            public PluggableTrackEffectDriver(
                PluggableTrackEffectTemplate Template,
                IPluggableProcessor Processor,
                PluggableTrackDynamicEvalParam[] DynamicDoubleParams,
                double[] DynamicDoubleParamsCurrent,
                SynthParamRec synthParams)
            {
                this.Template = Template;
                this.Processor = Processor;
                this.DynamicDoubleParams = DynamicDoubleParams;
                this.DynamicDoubleParamsCurrent = DynamicDoubleParamsCurrent;

                this.smoothedNonConst = new bool[Template.SmoothedParamOffsets.Length];
                PluggableAllocateWorkspaces(
                    Template.Spec.PluggableTemplate,
                    Template.SmoothedParamOffsets.Length,
                    out this.smoothedWorkspaceBases,
                    out this.scratch32WorkspaceBases,
                    out this.scratch64WorkspaceBases,
                    synthParams);
            }

            public static SynthErrorCodes Create(
                PluggableTrackEffectTemplate Template,
                SynthParamRec SynthParams,
                out PluggableTrackEffectDriver driver)
            {
                SynthErrorCodes error;
                driver = null;

                double[] staticDoubleParams = new double[Template.StaticDoubleParams.Length];
                for (int i = 0; i < staticDoubleParams.Length; i++)
                {
                    if (Template.StaticDoubleParams[i].Formula == null)
                    {
                        staticDoubleParams[i] = (float)Template.StaticDoubleParams[i].Value;
                    }
                    else
                    {
                        // This parameter function feature is mostly provided so that max delay time can be parameterized on
                        // 'bpm', to facilitate easy moving of delay effects from one document to another. Not all functionality
                        // is supported right now, such as accent parameterization. In any case, this parameter is fixed at the
                        // creation of the delay line processor, so accent changes or changes to the 'bpm' parameter can't be
                        // handled anyway. The convenience is still worth having it.
                        AccentRec zero = new AccentRec();
                        double temp;
                        error = StaticEval(
                            0,
                            Template.StaticDoubleParams[i].Formula,
                            ref zero, // TODO: pass track effect accents in to here
                            SynthParams,
                            out temp);
                        if (error != SynthErrorCodes.eSynthDone)
                        {
                            return error;
                        }
                        staticDoubleParams[i] = (float)temp;
                    }
                }

                PluggableTrackDynamicEvalParam[] dynamicDoubleParams = new PluggableTrackDynamicEvalParam[Template.DynamicDoubleParams.Length];
                double[] dynamicDoubleParamsCurrent = new double[Template.DynamicDoubleParams.Length];
                for (int i = 0; i < dynamicDoubleParams.Length; i++)
                {
                    InitScalarParamEval(
                        Template.DynamicDoubleParams[i].Value,
                        ref Template.DynamicDoubleParams[i].Accent,
                        Template.DynamicDoubleParams[i].Formula,
                        out dynamicDoubleParams[i].eval);
                }

                IPluggableProcessor processor;
                error = Template.Spec.PluggableTemplate.Create(
                    Template.StaticIntegerParams,
                    staticDoubleParams,
                    Template.StaticStringParams,
                    Template.OptionallySmoothedParametersDegraded,
                    SynthParams,
                    out processor);
                if (error != SynthErrorCodes.eSynthDone)
                {
                    return error;
                }

                // TODO: initialize Current to initial value (for smoothing)

                driver = new PluggableTrackEffectDriver(
                    Template,
                    processor,
                    dynamicDoubleParams,
                    dynamicDoubleParamsCurrent,
                    SynthParams);
                return SynthErrorCodes.eSynthDone;
            }

            public SynthErrorCodes TrackUpdateState(
                ref AccentRec Accents,
                SynthParamRec SynthParams)
            {
                SynthErrorCodes error;

                for (int i = 0; i < DynamicDoubleParams.Length; i++)
                {
                    DynamicDoubleParams[i].Previous = DynamicDoubleParamsCurrent[i];
                    error = ScalarParamEval(
                        DynamicDoubleParams[i].eval,
                        ref Accents,
                        SynthParams,
                        out DynamicDoubleParamsCurrent[i]);
                    if (error != SynthErrorCodes.eSynthDone)
                    {
                        return error;
                    }
                }

                error = Processor.Update(
                    DynamicDoubleParamsCurrent);

                return SynthErrorCodes.eSynthDone;
            }

            public SynthErrorCodes Apply(
                float[] workspace,
                int lOffset,
                int rOffset,
                int nActualFrames,
                SynthParamRec SynthParams)
            {
                PluggableAssignWorkspaces(
                    this.smoothedWorkspaceBases,
                    this.scratch32WorkspaceBases,
                    this.scratch64WorkspaceBases,
                    SynthParams);

                for (int i = 0; i < Template.SmoothedParamOffsets.Length; i++)
                {
                    int index = Template.SmoothedParamOffsets[i];
                    float localCurrentValue = (float)DynamicDoubleParamsCurrent[index];
                    float localPreviousValue = (float)DynamicDoubleParams[index].Previous;

                    smoothedNonConst[i] = localPreviousValue != localCurrentValue;

                    FloatVectorAdditiveRecurrence(
                        SynthParams.workspace,
                        smoothedWorkspaceBases[i],
                        localPreviousValue,
                        localCurrentValue,
                        nActualFrames);
                }

                return Processor.Apply(
                    1,
                    1,
                    workspace,
                    lOffset,
                    rOffset,
                    smoothedWorkspaceBases,
                    smoothedNonConst,
                    scratch32WorkspaceBases,
                    UnsafeArrayCastLong.AsDoubles(SynthParams.ScratchWorkspace3),
                    scratch64WorkspaceBases,
                    nActualFrames,
                    SynthParams);
            }

            public void Finalize(SynthParamRec SynthParams, bool writeOutputLogs)
            {
                Processor.Finalize(SynthParams, writeOutputLogs);
            }
        }

        public struct PluggableTrackDynamicEvalParam
        {
            //public double Current; -- stored separately
            public double Previous;
            public ScalarParamEvalRec eval;
        }
    }
}
