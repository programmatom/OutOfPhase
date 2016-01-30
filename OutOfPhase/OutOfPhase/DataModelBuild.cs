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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace OutOfPhase
{
    public abstract class HierarchicalBindingBuildable : HierarchicalBindingBase, IBuildable, IDisposable
    {
        // pass-through constructor
        protected HierarchicalBindingBuildable(HierarchicalBindingBase parent, string propertyNameInParent)
            : base(parent, propertyNameInParent)
        {
        }

        // requirement IBuildable

        public abstract bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback);

        public abstract void Unbuild();

        // unbuild on changes

        protected override void Changed(string propertyName, bool dirty)
        {
            Unbuild();
            base.Changed(propertyName, dirty);
        }

        // IDisposable: unbuild on disposal

        public void Dispose()
        {
            Unbuild();
        }
    }


    //

    public abstract class BuildErrorInfo
    {
        public abstract int LineNumber { get; }
        public abstract string CompositeErrorMessage { get; }

        protected BuildErrorInfo()
        {
        }
    }

    public class LiteralBuildErrorInfo : BuildErrorInfo
    {
        public readonly int lineNumber;
        private readonly string errorText;

        public override int LineNumber { get { return lineNumber; } }
        public override string CompositeErrorMessage { get { return String.Format("Compilation error on line {0}:  {1}", lineNumber, errorText); } }

        public LiteralBuildErrorInfo(string errorText, int lineNumber)
        {
            this.lineNumber = lineNumber;
            this.errorText = errorText;
        }
    }

    public class PcodeEvaluationErrorInfo : BuildErrorInfo
    {
        private readonly int lineNumber;
        private readonly string errorText;
        private readonly string module;

        public override int LineNumber { get { return lineNumber; } }
        public override string CompositeErrorMessage { get { return String.Format("Evaluation error in function module '{0}', line {1}:  {2}", module, lineNumber, errorText); } }

        public PcodeEvaluationErrorInfo(string module, string errorText, int LineNumber)
        {
            this.module = module;
            this.errorText = errorText;
            this.lineNumber = LineNumber;
        }

        public PcodeEvaluationErrorInfo(EvalErrors EvaluationError, EvalErrInfoRec ErrorInfo, PcodeRec AnonymousFunction, CodeCenterRec CodeCenter)
        {
            int ErrorLineNumberEvaluation;
            string Name;
            FuncCodeRec ErrorFunction = CodeCenter.GetFunctionFromOpcode(ErrorInfo.OffendingPcode);
            if (ErrorFunction != null)
            {
                Name = ErrorFunction.GetFunctionFilename();
                ErrorLineNumberEvaluation = ErrorFunction.GetFunctionPcode().GetLineNumberForInstruction(ErrorInfo.OffendingInstruction);
            }
            else
            {
                Name = "<anonymous>";
                ErrorLineNumberEvaluation = AnonymousFunction.GetLineNumberForInstruction(ErrorInfo.OffendingInstruction);
            }

            this.module = Name;
            this.errorText = PcodeSystem.GetPcodeErrorMessage(EvaluationError);
            this.lineNumber = ErrorLineNumberEvaluation;
        }
    }

    public delegate void BuildFailedCallback(object sender, BuildErrorInfo errorInfo);
    public interface IBuildable
    {
        bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback);
        void Unbuild();
    }


    //

    public partial class Document : IBuildable
    {
        // class Document should not be buildable in and of itself, but delegate building
        // to all buildable objects that it contains.

        // handle change-->rebuild dependencies
        private void BuildChangeNotify(string propertyName)
        {
            switch (propertyName)
            {
                case FunctionObjectRec.Source_PropertyName:
                    // function module change requires rebuilding all computed data
                    foreach (IBuildable algoWaveTable in AlgoWaveTableList)
                    {
                        algoWaveTable.Unbuild();
                    }
                    foreach (IBuildable algoSamp in AlgoSampList)
                    {
                        algoSamp.Unbuild();
                    }
                    break;
            }
        }

        // DO NOT CALL THIS DIRECTLY! Instead, use MainWindow.MakeUpToDate(), which will ensure
        // that all editors commit their edits to the data storage before building. Calling this
        // directly will fail because Document doesn't know who it's editors are.
        public bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            List<IBuildable> sequence = GetBuildSequence();

            // for global rebuild, unbuild all first
            if (force)
            {
                foreach (IBuildable buildable in sequence)
                {
                    buildable.Unbuild();
                }
            }

            foreach (IBuildable buildable in sequence)
            {
                if (!buildable.EnsureBuilt(force, pcodeEnvironment, failedCallback))
                {
                    return false;
                }
            }

            return true;
        }

        // DO NOT CALL THIS DIRECTLY! Instead, use MainWindow.MakeUpToDateFunctions(), which will ensure
        // that all editors commit their edits to the data storage before building. Calling this
        // directly will fail because Document doesn't know who it's editors are.
        public bool EnsureBuiltFunctions(
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            return functionBuilderProxy.EnsureBuilt(false/*force*/, pcodeEnvironment, failedCallback);
        }

        public void Unbuild()
        {
            foreach (IBuildable buildable in GetBuildSequence())
            {
                buildable.Unbuild();
            }
        }

        private List<IBuildable> GetBuildSequence()
        {
            List<IBuildable> list = new List<IBuildable>();

            list.Add(functionBuilderProxy); // subsumes all of FunctionList

            IEnumerable[] ordering = new IEnumerable[]
            {
                //FunctionList, -- subsumed by functionBuilderProxy
                AlgoWaveTableList,
                AlgoSampList,
                InstrumentList,
                SectionList,
            };

            foreach (IEnumerable enumerable in ordering)
            {
                foreach (object item in enumerable)
                {
                    list.Add((IBuildable)item);
                }
            }

            list.Add(ScoreEffects); // singleton
            list.Add(Sequencer); // singleton

            return list;
        }
    }


    public interface IFunctionBuilderProxyInterface
    {
        bool Built1 { get; set; }
        void Unbuild1();
    }

    public class FunctionBuilderProxy : HierarchicalBindingBuildable, IBuildable
    {
        public const string FunctionBuilderProxy_PropertyName = null; // no bindable properties

        private readonly MyBindingList<FunctionObjectRec> functionList; // copy

        public FunctionBuilderProxy(Document document, MyBindingList<FunctionObjectRec> functionList)
            : base(document, FunctionBuilderProxy_PropertyName)
        {
            this.functionList = functionList;
        }

        public CodeCenterRec CodeCenter { get { return ((Document)Parent).CodeCenter; } }

        [Bindable(true)]
        public bool Built
        {
            get
            {
                foreach (FunctionObjectRec function in functionList)
                {
                    if (!function.Built1)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public override bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            if (!force && Built)
            {
                return true;
            }

            List<string> sources = new List<string>();
            List<string> filenames = new List<string>();
            List<object> signatures = new List<object>();
            for (int i = 0; i < functionList.Count; i++)
            {
                sources.Add(functionList[i].Source);
                filenames.Add(functionList[i].Name);
                signatures.Add(functionList[i]);

                functionList[i].Unbuild1();
            }

            int ErrorLine;
            int ErrorModuleIndex;
            CompileErrors Error = Compiler.CompileWholeProgram(
                out ErrorLine,
                out ErrorModuleIndex,
                sources.ToArray(),
                signatures.ToArray(),
                CodeCenter,
                filenames.ToArray());
            if (Error != CompileErrors.eCompileNoError)
            {
                failedCallback(
                    (FunctionObjectRec)signatures[ErrorModuleIndex],
                    new LiteralBuildErrorInfo(Compiler.GetCompileErrorString(Error), ErrorLine));
                return false;
            }

            for (int i = 0; i < functionList.Count; i++)
            {
                functionList[i].Built1 = true;
            }

            return true;
        }

        public override void Unbuild()
        {
            foreach (FunctionObjectRec function in functionList)
            {
                if (function.Built1)
                {
                    CodeCenter.FlushModulesCompiledFunctions(function);
                    function.Built1 = false;
                }
            }
        }
    }

    public partial class FunctionObjectRec : IBuildable, IFunctionBuilderProxyInterface
    {
        private bool built;

        [Bindable(true)]
        public bool Built { get { return functionBuilderProxy.Built; } }
        public bool Built1 { get { return built; } set { built = value; } }


        /* build the functions if necessary.  return True if successful. */
        public override bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            return functionBuilderProxy.EnsureBuilt(
                force,
                pcodeEnvironment,
                failedCallback);
        }

        /* unconditionally unbuild the functions */
        public override void Unbuild()
        {
            functionBuilderProxy.Unbuild();
        }

        public void Unbuild1()
        {
            if (built)
            {
                codeCenter.FlushModulesCompiledFunctions(this);
                built = false;
            }
        }
    }


    public partial class AlgoSampObjectRec : IBuildable
    {
        // nonpersisted properties

        public SampleStorageActualRec SampleData; // null = needs to be built


        // Exposed separately to support Disassemble menu item
        public bool BuildCode(BuildFailedCallback failedCallback, out PcodeRec FuncCode)
        {
            int ErrorLineNumberCompilation;
            DataTypes ReturnType;
            Compiler.ASTExpressionRec AST;
            CompileErrors CompileError = Compiler.CompileSpecialFunction(
                ((Document)Parent).CodeCenter,
                NumChannels == NumChannelsType.eSampleStereo
                ? new FunctionParamRec[]
                {
                    new FunctionParamRec("loopstart1", DataTypes.eInteger),
                    new FunctionParamRec("loopstart2", DataTypes.eInteger),
                    new FunctionParamRec("loopstart3", DataTypes.eInteger),
                    new FunctionParamRec("loopend1", DataTypes.eInteger),
                    new FunctionParamRec("loopend2", DataTypes.eInteger),
                    new FunctionParamRec("loopend3", DataTypes.eInteger),
                    new FunctionParamRec("origin", DataTypes.eInteger),
                    new FunctionParamRec("samplingrate", DataTypes.eInteger),
                    new FunctionParamRec("naturalfrequency", DataTypes.eDouble),
                    new FunctionParamRec("leftdata", DataTypes.eArrayOfFloat),
                    new FunctionParamRec("rightdata", DataTypes.eArrayOfFloat),
                }
                : new FunctionParamRec[]
                {
                    new FunctionParamRec("loopstart1", DataTypes.eInteger),
                    new FunctionParamRec("loopstart2", DataTypes.eInteger),
                    new FunctionParamRec("loopstart3", DataTypes.eInteger),
                    new FunctionParamRec("loopend1", DataTypes.eInteger),
                    new FunctionParamRec("loopend2", DataTypes.eInteger),
                    new FunctionParamRec("loopend3", DataTypes.eInteger),
                    new FunctionParamRec("origin", DataTypes.eInteger),
                    new FunctionParamRec("samplingrate", DataTypes.eInteger),
                    new FunctionParamRec("naturalfrequency", DataTypes.eDouble),
                    new FunctionParamRec("data", DataTypes.eArrayOfFloat),
                },
                out ErrorLineNumberCompilation,
                out ReturnType,
                AlgoSampFormula,
                false/*suppressCILEmission*/,
                out FuncCode,
                out AST);
            if (CompileError != CompileErrors.eCompileNoError)
            {
                failedCallback(this, new LiteralBuildErrorInfo(Compiler.GetCompileErrorString(CompileError), ErrorLineNumberCompilation));
                return false;
            }

            return true;
        }

        public override bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            if (!force && (SampleData != null))
            {
                return true;
            }
            SampleData = null;

            PcodeRec FuncCode;
            if (!BuildCode(failedCallback, out FuncCode))
            {
                return false;
            }

            using (ParamStackRec ParamList = new ParamStackRec())
            {
                ArrayHandleFloat dataHandleLeft = new ArrayHandleFloat(new float[0]);
                ArrayHandleFloat dataHandleRight = new ArrayHandleFloat(new float[0]);

                ParamList.EmptyParamStackEnsureCapacity(
                    1/*loopstart1*/ + 1/*loopstart2*/ + 1/*loopstart3*/ +
                    1/*loopend1*/ + 1/*loopend2*/ + 1/*loopend3*/ +
                    1/*origin*/ + 1/*samplingrate*/ + 1/*naturalfrequency*/ +
                    (NumChannels == NumChannelsType.eSampleStereo ? 2 : 1)/*data or leftdata/rightdata */ +
                    1/*retaddr*/);

                ParamList.AddIntegerToStack(LoopStart1);
                ParamList.AddIntegerToStack(LoopStart2);
                ParamList.AddIntegerToStack(LoopStart3);
                ParamList.AddIntegerToStack(LoopEnd1);
                ParamList.AddIntegerToStack(LoopEnd2);
                ParamList.AddIntegerToStack(LoopEnd3);
                ParamList.AddIntegerToStack(Origin);
                ParamList.AddIntegerToStack(SamplingRate);
                ParamList.AddDoubleToStack(NaturalFrequency);
                if (NumChannels == NumChannelsType.eSampleStereo)
                {
                    ParamList.AddArrayToStack(dataHandleLeft);
                    ParamList.AddArrayToStack(dataHandleRight);
                }
                else
                {
                    ParamList.AddArrayToStack(dataHandleLeft);
                }
                ParamList.AddIntegerToStack(0); /* return address placeholder */

                CodeCenterRec CodeCenter = ((Document)Parent).CodeCenter;
                EvalErrInfoRec ErrorInfo;
                EvalErrors EvaluationError = PcodeSystem.EvaluatePcodeThread.EvaluatePcode(
                    ParamList,
                    FuncCode,
                    CodeCenter,
                    out ErrorInfo,
                    pcodeEnvironment);
                if (EvaluationError != EvalErrors.eEvalNoError)
                {
                    failedCallback(
                        this,
                        new PcodeEvaluationErrorInfo(
                            EvaluationError,
                            ErrorInfo,
                            FuncCode,
                            CodeCenter));
                    return false;
                }
                Debug.Assert(ParamList.GetStackNumElements() == 1); // return value

                if (NumChannels == NumChannelsType.eSampleStereo)
                {
                    float[] Left = dataHandleLeft.floats;
                    float[] Right = dataHandleRight.floats;
                    if (Left.Length != Right.Length)
                    {
                        failedCallback(
                            this,
                            new PcodeEvaluationErrorInfo(
                                "<anonymous>",
                                "Left and Right algorithmic sample arrays are not the same size.",
                                1));
                        return false;
                    }

                    SampleData = new SampleStorageActualRec(Left.Length, NumBitsType.Max, NumChannels);
                    for (int i = 0; i < Left.Length; i++)
                    {
                        SampleData[2 * i + 0] = Left[i];
                        SampleData[2 * i + 1] = Right[i];
                    }
                }
                else
                {
                    float[] Mono = dataHandleLeft.floats;

                    SampleData = new SampleStorageActualRec(Mono.Length, NumBitsType.Max, NumChannels);
                    for (int i = 0; i < Mono.Length; i++)
                    {
                        SampleData[i] = Mono[i];
                    }
                }
            }

            return true;
        }

        public override void Unbuild()
        {
            SampleData = null;
        }
    }


    public partial class AlgoWaveTableObjectRec : IBuildable
    {
        // nonpersisted properties

        public WaveTableStorageRec WaveTableData; // null = needs to be built


        // Exposed separately to support Disassemble menu item
        public bool BuildCode(BuildFailedCallback failedCallback, out PcodeRec FuncCode)
        {
            int ErrorLineNumberCompilation;
            DataTypes ReturnType;
            Compiler.ASTExpressionRec AST;
            CompileErrors CompileError = Compiler.CompileSpecialFunction(
                ((Document)Parent).CodeCenter,
                new FunctionParamRec[]
                {
                    new FunctionParamRec("frames", DataTypes.eInteger),
                    new FunctionParamRec("tables", DataTypes.eInteger),
                    new FunctionParamRec("data", DataTypes.eArrayOfFloat),
                },
                out ErrorLineNumberCompilation,
                out ReturnType,
                AlgoWaveTableFormula,
                false/*suppressCILEmission*/,
                out FuncCode,
                out AST);
            if (CompileError != CompileErrors.eCompileNoError)
            {
                failedCallback(this, new LiteralBuildErrorInfo(Compiler.GetCompileErrorString(CompileError), ErrorLineNumberCompilation));
                return false;
            }

            return true;
        }

        public override bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            if (!force && (WaveTableData != null))
            {
                return true;
            }
            WaveTableData = null;

            PcodeRec FuncCode;
            if (!BuildCode(failedCallback, out FuncCode))
            {
                return false;
            }

            using (ParamStackRec ParamList = new ParamStackRec())
            {
                ArrayHandleFloat dataHandle = new ArrayHandleFloat(new float[NumFrames * NumTables]);

                ParamList.EmptyParamStackEnsureCapacity(1/*frames*/ + 1/*tables*/ + 1/*data*/ + 1/*retaddr*/);
                ParamList.AddIntegerToStack(NumFrames);
                ParamList.AddIntegerToStack(NumTables);
                ParamList.AddArrayToStack(dataHandle);
                ParamList.AddIntegerToStack(0); /* return address placeholder */

                CodeCenterRec CodeCenter = ((Document)Parent).CodeCenter;
                EvalErrInfoRec ErrorInfo;
                EvalErrors EvaluationError = PcodeSystem.EvaluatePcodeThread.EvaluatePcode(
                    ParamList,
                    FuncCode,
                    CodeCenter,
                    out ErrorInfo,
                    pcodeEnvironment);
                if (EvaluationError != EvalErrors.eEvalNoError)
                {
                    failedCallback(
                        this,
                        new PcodeEvaluationErrorInfo(
                            EvaluationError,
                            ErrorInfo,
                            FuncCode,
                            CodeCenter));
                    return false;
                }
                Debug.Assert(ParamList.GetStackNumElements() == 1); // return value

                WaveTableData = new WaveTableStorageRec(NumTables, NumFrames, NumBitsType.eSample24bit);
                float[] NewData = dataHandle.floats;
                if (NewData.Length != NumTables * NumFrames)
                {
                    failedCallback(
                        this,
                        new PcodeEvaluationErrorInfo(
                            "<anonymous>",
                            PcodeSystem.GetPcodeErrorMessage(EvalErrors.eEvalArrayWrongDimensions),
                            1));
                    return false;
                }
                for (int i = 0; i < NumTables; i++)
                {
                    WaveTableStorageRec.Table table = WaveTableData.ListOfTables[i];
                    for (int j = 0; j < NumFrames; j++)
                    {
                        table[j] = NewData[i * NumFrames + j];
                    }
                }
            }

            return true;
        }

        public override void Unbuild()
        {
            WaveTableData = null;
        }
    }


    public partial class InstrObjectRec : IBuildable
    {
        // nonpersisted properties

        private Synthesizer.InstrumentRec _instrument; /* NIL = not built */

        public Synthesizer.InstrumentRec GetInstrObjectRawData()
        {
            if (_instrument == null)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            return _instrument;
        }

        public override bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            if (!force && (_instrument != null))
            {
                return true;
            }
            _instrument = null;

            Synthesizer.InstrumentRec TheInstrument;

            int ErrorLine;
            string ErrorExtraMessage;
            Synthesizer.BuildInstrErrors Error = Synthesizer.BuildInstrumentFromText(
                InstrDefinition,
                ((Document)Parent).CodeCenter,
                out ErrorLine,
                out ErrorExtraMessage,
                out TheInstrument);
            if (Error != Synthesizer.BuildInstrErrors.eBuildInstrNoError)
            {
                failedCallback(this, new LiteralBuildErrorInfo(Synthesizer.BuildInstrGetErrorMessageText(Error, ErrorExtraMessage), ErrorLine));
                return false;
            }

            _instrument = TheInstrument;
            return true;
        }

        public override void Unbuild()
        {
            _instrument = null;
        }

        public Synthesizer.InstrumentRec BuiltInstrument
        {
            get
            {
                Debug.Assert(_instrument != null);
                return _instrument;
            }
        }
    }


    public partial class SectionObjectRec : IBuildable
    {
        // nonpersisted properties

        private Synthesizer.EffectSpecListRec _effectSpec;


        public override bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            if (!force && (_effectSpec != null))
            {
                return true;
            }
            _effectSpec = null;

            int ErrorLine;
            string ErrorExtraMessage;
            Synthesizer.EffectSpecListRec LocalEffectSpec;
            Synthesizer.BuildInstrErrors Error = Synthesizer.BuildSectionEffectList(
                Source,
                ((Document)Parent).CodeCenter,
                out ErrorLine,
                out ErrorExtraMessage,
                out LocalEffectSpec);
            if (Error != Synthesizer.BuildInstrErrors.eBuildInstrNoError)
            {
                failedCallback(this, new LiteralBuildErrorInfo(Synthesizer.BuildInstrGetErrorMessageText(Error, ErrorExtraMessage), ErrorLine));
                return false;
            }

            _effectSpec = LocalEffectSpec;
            return true;
        }

        public override void Unbuild()
        {
            _effectSpec = null;
        }

        public Synthesizer.EffectSpecListRec EffectSpec
        {
            get
            {
                Debug.Assert(_effectSpec != null);
                return _effectSpec;
            }
        }
    }


    public partial class ScoreEffectsRec : IBuildable
    {
        // nonpersisted properties

        private Synthesizer.EffectSpecListRec _scoreEffectSpec;


        public override bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            if (!force && (_scoreEffectSpec != null))
            {
                return true;
            }
            _scoreEffectSpec = null;

            int ErrorLine;
            string ErrorExtraMessage;
            Synthesizer.EffectSpecListRec LocalEffectSpec;
            Synthesizer.BuildInstrErrors Error = Synthesizer.BuildScoreEffectList(
                this.Source,
                ((Document)Parent).CodeCenter,
                out ErrorLine,
                out ErrorExtraMessage,
                out LocalEffectSpec);
            if (Error != Synthesizer.BuildInstrErrors.eBuildInstrNoError)
            {
                failedCallback(this, new LiteralBuildErrorInfo(Synthesizer.BuildInstrGetErrorMessageText(Error, ErrorExtraMessage), ErrorLine));
                return false;
            }

            _scoreEffectSpec = LocalEffectSpec;
            return true;
        }

        public override void Unbuild()
        {
            _scoreEffectSpec = null;
        }

        public Synthesizer.EffectSpecListRec ScoreEffectSpec
        {
            get
            {
                Debug.Assert(_scoreEffectSpec != null);
                return _scoreEffectSpec;
            }
        }
    }


    public partial class SequencerRec : IBuildable
    {
        // nonpersisted properties

        private Synthesizer.SequencerConfigSpecRec _sequencerSpec;


        public override bool EnsureBuilt(
            bool force,
            PcodeSystem.IEvaluationContext pcodeEnvironment,
            BuildFailedCallback failedCallback)
        {
            if (!force && (_sequencerSpec != null))
            {
                return true;
            }
            _sequencerSpec = null;

            int ErrorLine;
            Synthesizer.SequencerConfigSpecRec LocalSequencerSpec;
            Synthesizer.BuildSeqErrors Error = Synthesizer.BuildSequencerFromText(
                this.Source,
                out ErrorLine,
                out LocalSequencerSpec);
            if (Error != Synthesizer.BuildSeqErrors.eBuildSeqNoError)
            {
                failedCallback(this, new LiteralBuildErrorInfo(Synthesizer.BuildSeqGetErrorMessageText(Error), ErrorLine));
                return false;
            }

            _sequencerSpec = LocalSequencerSpec;
            return true;
        }

        public override void Unbuild()
        {
            _sequencerSpec = null;
        }

        public Synthesizer.SequencerConfigSpecRec SequencerSpec
        {
            get
            {
                Debug.Assert(_sequencerSpec != null);
                return _sequencerSpec;
            }
        }
    }
}
