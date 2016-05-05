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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class SampleWindow : Form, IMenuStripManagerHandler, IGlobalNameChange, IUndoClient
    {
        private readonly Registration registration;
        private readonly SampleObjectRec sampleObject;
        private readonly IMainWindowServices mainWindow;

        private int currentLoop = 1;

        private readonly UndoHelper undoHelper;

        public SampleWindow(Registration registration, SampleObjectRec sampleObject, IMainWindowServices mainWindow)
        {
            this.registration = registration;
            this.sampleObject = sampleObject;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            this.textBoxFunction.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            this.textBoxFunction.AutoIndent = Program.Config.AutoIndent;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            undoHelper = new UndoHelper(this);

            menuStripManager.SetGlobalHandler(mainWindow);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);
            menuStripManager.HookUpTextBoxWindowHelper(this.textBoxWindowHelper);

            sampleControl.SampleObject = sampleObject;

            foreach (string item in EnumUtility.GetDescriptions(SampleObjectRec.LoopBidirectionalAllowedValues))
            {
                comboBoxLoopBidirectional.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(SampleObjectRec.NumBitsAllowedValues))
            {
                comboBoxNumBits.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(SampleObjectRec.NumChannelsAllowedValues))
            {
                comboBoxNumChannels.Items.Add(item);
            }

            documentBindingSource.Add(mainWindow.Document);
            sampleObjectRecBindingSource.Add(sampleObject);

            textBoxName.TextChanged += new EventHandler(textBoxName_TextChanged);
            GlobalNameChanged();

            comboBoxNumBits.TextChanged += new EventHandler(comboBoxNumBits_TextChanged);
            comboBoxNumChannels.TextChanged += new EventHandler(comboBoxNumChannels_TextChanged);

            buttonTest.MouseDown += new MouseEventHandler(buttonTest_MouseDown);
            buttonTest.MouseUp += new MouseEventHandler(buttonTest_MouseUp);

            sampleControl.LoopStart = sampleObject.LoopStart1;
            sampleControl.LoopEnd = sampleObject.LoopEnd1;

            sampleControl.SelectionStartChanged += new EventHandler(sampleControl_SelectionStartChanged);
            sampleControl.SelectionEndChanged += new EventHandler(sampleControl_SelectionEndChanged);
            sampleControl.OriginChanged += new EventHandler(sampleControl_OriginChanged);
            sampleControl.LoopStartChanged += new EventHandler(sampleControl_LoopStartChanged);
            sampleControl.LoopEndChanged += new EventHandler(sampleControl_LoopEndChanged);
            sampleControl.XScaleChanged += new EventHandler(sampleControl_ScaleChanged);

            textBoxSelectionStart.Validated += new EventHandler(textBoxSelectionStart_Validated);
            textBoxSelectionEnd.Validated += new EventHandler(textBoxSelectionEnd_Validated);
            textBoxOrigin.TextChanged += new EventHandler(textBoxOrigin_TextChanged);
            textBoxLoopStart.TextChanged += new EventHandler(textBoxLoopStart_TextChanged);
            textBoxLoopEnd.TextChanged += new EventHandler(textBoxLoopEnd_TextChanged);
            textBoxScale.Validated += new EventHandler(textBoxScale_Validated);

            registration.Register(sampleObject, this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (state != null)
            {
                state.stopper.Stop();
                state.Dispose();
                state = null;
            }

            undoHelper.Dispose();

            registration.Unregister(sampleObject, this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, sampleObject.SavedWindowXLoc, sampleObject.SavedWindowYLoc, sampleObject.SavedWindowWidth, sampleObject.SavedWindowHeight);
            base.OnShown(e);
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                sampleObject.SavedWindowXLoc = x;
                sampleObject.SavedWindowYLoc = y;
                sampleObject.SavedWindowWidth = width;
                sampleObject.SavedWindowHeight = height;
            }
        }

        protected override void OnMove(EventArgs e)
        {
            ResizeMove();
            base.OnMove(e);
        }

        protected override void OnResize(EventArgs e)
        {
            ResizeMove();
            base.OnResize(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            menuStripManager.SetActiveHandler(this);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            menuStripManager.SetActiveHandler(null);
            base.OnDeactivate(e);
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }


        //

        private int GetCurrentLoopStart()
        {
            int i;
            Int32.TryParse(textBoxLoopStart.Text, out i);
            return i;
        }

        private int GetCurrentLoopEnd()
        {
            int i;
            Int32.TryParse(textBoxLoopEnd.Text, out i);
            return i;
        }

        private LoopBidirectionalType GetCurrentLoopBidirectionality()
        {
            return (LoopBidirectionalType)SampleObjectRec.LoopBidirectionalAllowedValues[comboBoxLoopBidirectional.SelectedIndex];
        }

        private double GetTestPitch()
        {
            double pitch;
            if (!Double.TryParse(textBoxTestPitch.Text, out pitch))
            {
                short Pitch = Constants.CENTERNOTE;
                NoteFlags SharpFlatThing = 0;
                SymbolicPitch.StringToNumericPitch(textBoxTestPitch.Text, ref Pitch, ref SharpFlatThing);
                pitch = Math.Exp(((double)(Pitch - Constants.CENTERNOTE) / 12) * Constants.LOG2) * Constants.MIDDLEC;
            }
            return pitch;
        }


        // Sample View Control binding

        private bool suppressSampleControlEvents;

        private void sampleControl_SelectionStartChanged(object sender, EventArgs e)
        {
            if (!suppressSampleControlEvents)
            {
                textBoxSelectionStart.Text = sampleControl.SelectionStart.ToString();
            }
        }

        private void sampleControl_SelectionEndChanged(object sender, EventArgs e)
        {
            if (!suppressSampleControlEvents)
            {
                textBoxSelectionEnd.Text = sampleControl.SelectionEnd.ToString();
            }
        }

        private void sampleControl_OriginChanged(object sender, EventArgs e)
        {
            if (!suppressSampleControlEvents)
            {
                sampleObject.Origin = sampleControl.Origin;
            }
        }

        private void sampleControl_LoopStartChanged(object sender, EventArgs e)
        {
            if (!suppressSampleControlEvents)
            {
                switch (currentLoop)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 1:
                        sampleObject.LoopStart1 = sampleControl.LoopStart;
                        break;
                    case 2:
                        sampleObject.LoopStart2 = sampleControl.LoopStart;
                        break;
                    case 3:
                        sampleObject.LoopStart3 = sampleControl.LoopStart;
                        break;
                }
            }
        }

        private void sampleControl_LoopEndChanged(object sender, EventArgs e)
        {
            if (!suppressSampleControlEvents)
            {
                switch (currentLoop)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 1:
                        sampleObject.LoopEnd1 = sampleControl.LoopEnd;
                        break;
                    case 2:
                        sampleObject.LoopEnd2 = sampleControl.LoopEnd;
                        break;
                    case 3:
                        sampleObject.LoopEnd3 = sampleControl.LoopEnd;
                        break;
                }
            }
        }

        private void sampleControl_ScaleChanged(object sender, EventArgs e)
        {
            if (!suppressSampleControlEvents)
            {
                textBoxScale.Text = sampleControl.XScale.ToString();
            }
        }

        private void textBoxSelectionStart_Validated(object sender, EventArgs e)
        {
            int i;
            if (Int32.TryParse(textBoxSelectionStart.Text, out i))
            {
                sampleControl.SelectionStart = i;
            }
        }

        private void textBoxSelectionEnd_Validated(object sender, EventArgs e)
        {
            int i;
            if (Int32.TryParse(textBoxSelectionEnd.Text, out i))
            {
                sampleControl.SelectionEnd = i;
            }
        }

        private void textBoxOrigin_TextChanged(object sender, EventArgs e)
        {
            int i;
            if (Int32.TryParse(textBoxOrigin.Text, out i))
            {
                sampleControl.Origin = i;
            }
        }

        private void textBoxLoopStart_TextChanged(object sender, EventArgs e)
        {
            suppressSampleControlEvents = true;
            try
            {
                int i;
                if (Int32.TryParse(textBoxLoopStart.Text, out i))
                {
                    sampleControl.LoopStart = i;
                }
            }
            finally
            {
                suppressSampleControlEvents = false;
            }
        }

        private void textBoxLoopEnd_TextChanged(object sender, EventArgs e)
        {
            suppressSampleControlEvents = true;
            try
            {
                int i;
                if (Int32.TryParse(textBoxLoopEnd.Text, out i))
                {
                    sampleControl.LoopEnd = i;
                }
            }
            finally
            {
                suppressSampleControlEvents = false;
            }
        }

        private void textBoxScale_Validated(object sender, EventArgs e)
        {
            float f;
            if (Single.TryParse(textBoxScale.Text, out f))
            {
                sampleControl.XScale = f;
            }
        }

        private void buttonZoomIn_Click(object sender, EventArgs e)
        {
            sampleControl.XScale /= 2;
        }

        private void buttonZoomOut_Click(object sender, EventArgs e)
        {
            sampleControl.XScale *= 2;
        }


        // Sample editing

        // These UI events must set undo info

        private bool suppressTextChangedEvents;

        private void comboBoxNumChannels_TextChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(comboBoxNumChannels.Text) && !suppressTextChangedEvents)
            {
                NumChannelsType to = (NumChannelsType)EnumUtility.GetValue(NumChannelsType.eSampleMono.GetType(), comboBoxNumChannels.Text);
                if (to != sampleObject.SampleData.NumChannels)
                {
                    undoHelper.SaveUndoInfo(false/*forRedo*/, to == NumChannelsType.eSampleMono ? "Convert to Mono" : "Convert to Stereo");
                    sampleObject.SampleData = sampleObject.SampleData.ChangeChannels(to);
                    sampleControl.Redraw();
                }
            }
        }

        private void comboBoxNumBits_TextChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(comboBoxNumBits.Text) && !suppressTextChangedEvents)
            {
                NumBitsType to = (NumBitsType)EnumUtility.GetValue(NumBitsType.eSample8bit.GetType(), comboBoxNumBits.Text);
                if (to != sampleObject.SampleData.NumBits)
                {
                    undoHelper.SaveUndoInfo(false/*forRedo*/, "Change Bit Depth");
                    float[] vector = (float[])(sampleObject.SampleData.Buffer.Clone());
                    SampConv.QuantizeAndClampVector(vector, to);
                    sampleObject.SampleData = new SampleStorageActualRec(
                        sampleObject.SampleData.NumFrames,
                        to,
                        sampleObject.SampleData.NumChannels,
                        vector);
                    sampleControl.Redraw();
                }
            }
        }

        // These methods comprise larger UI-driven sequences and should not set undo info themselves

        private void Delete()
        {
            int selectionStart = sampleControl.SelectionStart;
            int selectionEnd = sampleControl.SelectionEnd;
            sampleControl.SelectionEnd = selectionStart;
            sampleObject.SampleData = sampleObject.SampleData.Reduce(selectionStart, selectionEnd - selectionStart);
            sampleObject.ShiftPoints(selectionStart, selectionStart - selectionEnd/*negative*/);
            sampleControl.Redraw();
        }

        private void Copy()
        {
            SampleObjectRec copy = new SampleObjectRec(mainWindow.Document, sampleObject);
            int selectionStart = sampleControl.SelectionStart;
            int selectionEnd = sampleControl.SelectionEnd;
            copy.SampleData = copy.SampleData.Sub(selectionStart, selectionEnd - selectionStart);
            SampleClipboard clipboard = new SampleClipboard(copy, mainWindow.Document);
            Clipboard.SetData(SampleClipboard.ClipboardIdentifer, clipboard);
        }

        private void Insert(SampleStorageActualRec insert, int frame)
        {
            insert = insert.ChangeChannels(sampleObject.NumChannels);
            sampleObject.SampleData = sampleObject.SampleData.Insert(insert.Buffer, frame, insert.NumFrames);
            sampleObject.ShiftPoints(frame, insert.NumFrames);
            sampleControl.SetSelection(frame, frame + insert.NumFrames, true/*startValidatePriority*/);
            //sampleControl.Redraw(); implied by .SetSelection()
        }


        // Evaluate function

        private static readonly FunctionParamRec[] StereoArgList =
        {
            new FunctionParamRec("loopstart1", DataTypes.eInteger),
            new FunctionParamRec("loopstart2", DataTypes.eInteger),
            new FunctionParamRec("loopstart3", DataTypes.eInteger),
            new FunctionParamRec("loopend1", DataTypes.eInteger),
            new FunctionParamRec("loopend2", DataTypes.eInteger),
            new FunctionParamRec("loopend3", DataTypes.eInteger),
            new FunctionParamRec("loopbidir1", DataTypes.eBoolean),
            new FunctionParamRec("loopbidir2", DataTypes.eBoolean),
            new FunctionParamRec("loopbidir3", DataTypes.eBoolean),
            new FunctionParamRec("origin", DataTypes.eInteger),
            new FunctionParamRec("samplingrate", DataTypes.eInteger),
            new FunctionParamRec("naturalfrequency", DataTypes.eDouble),
            new FunctionParamRec("selectstart", DataTypes.eInteger),
            new FunctionParamRec("selectend", DataTypes.eInteger),
            new FunctionParamRec("leftdata", DataTypes.eArrayOfFloat),
            new FunctionParamRec("rightdata", DataTypes.eArrayOfFloat),
        };

        private static readonly FunctionParamRec[] MonoArgList =
        {
            new FunctionParamRec("loopstart1", DataTypes.eInteger),
            new FunctionParamRec("loopstart2", DataTypes.eInteger),
            new FunctionParamRec("loopstart3", DataTypes.eInteger),
            new FunctionParamRec("loopend1", DataTypes.eInteger),
            new FunctionParamRec("loopend2", DataTypes.eInteger),
            new FunctionParamRec("loopend3", DataTypes.eInteger),
            new FunctionParamRec("loopbidir1", DataTypes.eBoolean),
            new FunctionParamRec("loopbidir2", DataTypes.eBoolean),
            new FunctionParamRec("loopbidir3", DataTypes.eBoolean),
            new FunctionParamRec("origin", DataTypes.eInteger),
            new FunctionParamRec("samplingrate", DataTypes.eInteger),
            new FunctionParamRec("naturalfrequency", DataTypes.eDouble),
            new FunctionParamRec("selectstart", DataTypes.eInteger),
            new FunctionParamRec("selectend", DataTypes.eInteger),
            new FunctionParamRec("data", DataTypes.eArrayOfFloat),
        };

        private void Evaluate()
        {
            if (!Validate() || !mainWindow.MakeUpToDate())
            {
                return;
            }

            int ErrorLineNumberCompilation;
            DataTypes ReturnType;
            PcodeRec FuncCode;
            Compiler.ASTExpression AST;
            CompileErrors CompileError = Compiler.CompileSpecialFunction(
                mainWindow.Document.CodeCenter,
                sampleObject.NumChannels == NumChannelsType.eSampleStereo ? StereoArgList : MonoArgList,
                out ErrorLineNumberCompilation,
                out ReturnType,
                textBoxFunction.Text,
                false/*suppressCILEmission*/,
                out FuncCode,
                out AST);
            if (CompileError != CompileErrors.eCompileNoError)
            {
                textBoxFunction.Focus();
                textBoxFunction.SetSelectionLine(ErrorLineNumberCompilation - 1);
                textBoxFunction.ScrollToSelection();
                LiteralBuildErrorInfo errorInfo = new LiteralBuildErrorInfo(Compiler.GetCompileErrorString(CompileError), ErrorLineNumberCompilation);
                MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                return;
            }

            using (ParamStackRec ParamList = new ParamStackRec())
            {
                int numFrames = sampleObject.SampleData.NumFrames;
                float[] vectorL = null;
                float[] vectorR = null;
                if (sampleObject.SampleData.NumChannels == NumChannelsType.eSampleStereo)
                {
                    vectorL = new float[numFrames];
                    vectorR = new float[numFrames];
                    for (int i = 0; i < numFrames; i++)
                    {
                        vectorL[i] = sampleObject.SampleData.Buffer[2 * i + 0];
                        vectorR[i] = sampleObject.SampleData.Buffer[2 * i + 1];
                    }
                }
                else
                {
                    vectorL = new float[numFrames];
                    Array.Copy(sampleObject.SampleData.Buffer, 0, vectorL, 0, numFrames);
                }

                ArrayHandleFloat dataHandleLeft = new ArrayHandleFloat(vectorL);
                ArrayHandleFloat dataHandleRight = vectorR != null ? new ArrayHandleFloat(vectorR) : null;

                int initialCapacity =
                    1/*loopstart1*/ + 1/*loopstart2*/ + 1/*loopstart3*/ +
                    1/*loopend1*/ + 1/*loopend2*/ + 1/*loopend3*/ +
                    1/*loopbidir1*/ + 1/*loopbidir2*/ + 1/*loopbidir3*/ +
                    1/*origin*/ + 1/*samplingrate*/ + 1/*naturalfrequency*/ +
                    1/*selectstart*/ + 1/*selectend*/ +
                    (sampleObject.SampleData.NumChannels == NumChannelsType.eSampleStereo ? 2 : 1)/*data or leftdata/rightdata */ +
                    1/*retaddr*/;
                ParamList.EmptyParamStackEnsureCapacity(initialCapacity);

                ParamList.AddIntegerToStack(sampleObject.LoopStart1);
                ParamList.AddIntegerToStack(sampleObject.LoopStart2);
                ParamList.AddIntegerToStack(sampleObject.LoopStart3);
                ParamList.AddIntegerToStack(sampleObject.LoopEnd1);
                ParamList.AddIntegerToStack(sampleObject.LoopEnd2);
                ParamList.AddIntegerToStack(sampleObject.LoopEnd3);
                ParamList.AddIntegerToStack(sampleObject.Loop1Bidirectional == LoopBidirectionalType.Yes ? 1 : 0);
                ParamList.AddIntegerToStack(sampleObject.Loop2Bidirectional == LoopBidirectionalType.Yes ? 1 : 0);
                ParamList.AddIntegerToStack(sampleObject.Loop3Bidirectional == LoopBidirectionalType.Yes ? 1 : 0);
                ParamList.AddIntegerToStack(sampleObject.Origin);
                ParamList.AddIntegerToStack(sampleObject.SamplingRate);
                ParamList.AddDoubleToStack(sampleObject.NaturalFrequency);
                ParamList.AddIntegerToStack(sampleControl.SelectionStart);
                ParamList.AddIntegerToStack(sampleControl.SelectionEnd);
                ParamList.AddArrayToStack(dataHandleLeft);
                if (sampleObject.SampleData.NumChannels == NumChannelsType.eSampleStereo)
                {
                    ParamList.AddArrayToStack(dataHandleRight);
                }
                ParamList.AddIntegerToStack(0); /* return address placeholder */

                CodeCenterRec CodeCenter = mainWindow.Document.CodeCenter;
                EvalErrInfoRec ErrorInfo;
                EvalErrors EvaluationError = PcodeSystem.EvaluatePcodeThread.EvaluatePcode(
                    ParamList,
                    FuncCode,
                    CodeCenter,
                    out ErrorInfo,
                    new PcodeExterns(mainWindow));
                if (EvaluationError != EvalErrors.eEvalNoError)
                {
                    PcodeEvaluationErrorInfo errorInfo = new PcodeEvaluationErrorInfo(
                        EvaluationError,
                        ErrorInfo,
                        FuncCode,
                        CodeCenter);
                    MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                    return;
                }
                Debug.Assert(ParamList.GetStackNumElements() == initialCapacity); // args - retaddr + return value
#if DEBUG
                ParamList.Elements[14].AssertFloatArray();
#endif
                dataHandleLeft = ParamList.Elements[14].reference.arrayHandleFloat;
                if (sampleObject.SampleData.NumChannels == NumChannelsType.eSampleStereo)
                {
#if DEBUG
                    ParamList.Elements[15].AssertFloatArray();
#endif
                    dataHandleRight = ParamList.Elements[15].reference.arrayHandleFloat;
                }

                SampleStorageActualRec sampleDataNew = null;
                if (sampleObject.SampleData.NumChannels == NumChannelsType.eSampleStereo)
                {
                    vectorL = dataHandleLeft.floats;
                    vectorR = dataHandleRight.floats;
                    if (vectorL.Length != vectorR.Length)
                    {
                        PcodeEvaluationErrorInfo errorInfo = new PcodeEvaluationErrorInfo(
                            "<anonymous>",
                            PcodeSystem.GetPcodeErrorMessage(EvalErrors.eEvalArrayWrongDimensions),
                            1);
                        MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                        return;
                    }
                    float[] vector = new float[2 * (vectorL.Length + 1)];
                    for (int i = 0; i < vectorL.Length; i++)
                    {
                        vector[2 * i + 0] = vectorL[i];
                        vector[2 * i + 1] = vectorR[i];
                    }
                    SampConv.QuantizeAndClampVector(vector, sampleObject.NumBits);
                    sampleDataNew = new SampleStorageActualRec(vectorL.Length, sampleObject.NumBits, sampleObject.NumChannels, vector);
                }
                else
                {
                    vectorL = dataHandleLeft.floats;
                    SampConv.QuantizeAndClampVector(vectorL, sampleObject.NumBits);
                    Array.Resize(ref vectorL, vectorL.Length + 1);
                    sampleDataNew = new SampleStorageActualRec(vectorL.Length - 1, sampleObject.NumBits, sampleObject.NumChannels, vectorL);
                }

                undoHelper.SaveUndoInfo(false/*forRedo*/, "Evaluate");
                sampleObject.SampleData = sampleDataNew;
            }
        }


        // MenuStripManager methods

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (textEditorWindowHelper.ProcessCmdKeyDelegate(ref msg, keyData))
            {
                return true;
            }
            if (textBoxWindowHelper.ProcessCmdKeyDelegate(ref msg, keyData))
            {
                return true;
            }

            menuStripManager.ProcessCmdKeyDelegate(ref msg, keyData);

            if (((keyData == Keys.Delete) || (keyData == Keys.Back)) && sampleControl.Focused)
            {
                undoHelper.SaveUndoInfo(false/*forRedo*/, "Delete Sample Data");
                Delete();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        void IMenuStripManagerHandler.EnableMenuItems(MenuStripManager menuStrip)
        {
            bool activeTextBox = textEditorWindowHelper.MenuActivateDelegate();
            activeTextBox = textBoxWindowHelper.MenuActivateDelegate() || activeTextBox;

            menuStrip.exportAIFFSampleToolStripMenuItem.Visible = true;
            menuStrip.exportAIFFSampleToolStripMenuItem.Enabled = true;

            menuStrip.exportWAVSampleToolStripMenuItem.Visible = true;
            menuStrip.exportWAVSampleToolStripMenuItem.Enabled = true;

            menuStrip.evaluateToolStripMenuItem.Enabled = true;

            menuStrip.loop1ToolStripMenuItem.Visible = true;
            menuStrip.loop1ToolStripMenuItem.Enabled = true;
            menuStrip.loop1ToolStripMenuItem.Checked = currentLoop == 1;

            menuStrip.loop2ToolStripMenuItem.Visible = true;
            menuStrip.loop2ToolStripMenuItem.Enabled = true;
            menuStrip.loop2ToolStripMenuItem.Checked = currentLoop == 2;

            menuStrip.loop3ToolStripMenuItem.Visible = true;
            menuStrip.loop3ToolStripMenuItem.Enabled = true;
            menuStrip.loop3ToolStripMenuItem.Checked = currentLoop == 3;

            if (!activeTextBox)
            {
                menuStrip.selectAllToolStripMenuItem.Enabled = true;
                menuStrip.selectAllToolStripMenuItem.Text = "Select Entire Sample";

                if (undoHelper.UndoAvailable)
                {
                    menuStrip.undoToolStripMenuItem.Enabled = true;
                    string undoLabel = undoHelper.UndoLabel;
                    menuStrip.undoToolStripMenuItem.Text = String.Format("Undo {0}", undoLabel != null ? undoLabel : "Sample Edit");
                }
                if (undoHelper.RedoAvailable)
                {
                    menuStrip.redoToolStripMenuItem.Enabled = true;
                    string redoLabel = undoHelper.RedoLabel;
                    menuStrip.redoToolStripMenuItem.Text = String.Format("Redo {0}", redoLabel != null ? redoLabel : "Sample Edit");
                }

                if (sampleControl.SelectionNonEmpty)
                {
                    menuStrip.cutToolStripMenuItem.Enabled = true;
                    menuStrip.cutToolStripMenuItem.Text = "Cut Sample Data";
                    menuStrip.copyToolStripMenuItem.Enabled = true;
                    menuStrip.copyToolStripMenuItem.Text = "Copy Sample Data";
                    menuStrip.clearToolStripMenuItem.Enabled = true;
                    menuStrip.clearToolStripMenuItem.Text = "Delete Sample Data";
                }
                bool enablePaste = false;
                IDataObject dataObject = Clipboard.GetDataObject();
                foreach (string format in dataObject.GetFormats())
                {
                    if (String.Equals(format, SampleClipboard.ClipboardIdentifer))
                    {
                        enablePaste = true;
                        break;
                    }
                }
                if (enablePaste)
                {
                    menuStrip.pasteToolStripMenuItem.Enabled = true;
                    menuStrip.pasteToolStripMenuItem.Text = "Paste Sample Data";
                }
            }

            menuStrip.deleteObjectToolStripMenuItem.Enabled = true;
            menuStrip.deleteObjectToolStripMenuItem.Text = "Delete Sample";
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            if (textEditorWindowHelper.ProcessMenuItemDelegate(menuItem))
            {
                // Be sure to set DelegatedMode on textEditorHelper to true
                return true;
            }
            else if (textBoxWindowHelper.ProcessMenuItemDelegate(menuItem))
            {
                // Be sure to set DelegatedMode on textBoxHelper to true
                return true;
            }
            else if (menuItem == menuStrip.exportAIFFSampleToolStripMenuItem)
            {
                Export.ExportAIFFSample(sampleObject.SampleData, sampleObject.SamplingRate);
                return true;
            }
            else if (menuItem == menuStrip.exportWAVSampleToolStripMenuItem)
            {
                Export.ExportWAVSample(sampleObject.SampleData, sampleObject.SamplingRate);
                return true;
            }
            else if (menuItem == menuStrip.evaluateToolStripMenuItem)
            {
                Evaluate();
                return true;
            }
            else if (menuItem == menuStrip.loop1ToolStripMenuItem)
            {
                if (!Validate()) // commit any edits to current loop box
                {
                    return true;
                }
                try
                {
                    suppressSampleControlEvents = true;
                    currentLoop = 1;
                    this.textBoxLoopStart.DataBindings.Clear();
                    this.textBoxLoopStart.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "LoopStart1", true));
                    this.textBoxLoopEnd.DataBindings.Clear();
                    this.textBoxLoopEnd.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "LoopEnd1", true));
                    this.comboBoxLoopBidirectional.DataBindings.Clear();
                    this.comboBoxLoopBidirectional.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "Loop1BidirectionalAsString", true));
                    this.sampleControl.LoopStartLabel = "Loop 1 Start";
                    this.sampleControl.LoopStart = sampleObject.LoopStart1;
                    this.sampleControl.LoopEndLabel = "Loop 1 End";
                    this.sampleControl.LoopEnd = sampleObject.LoopEnd1;
                }
                finally
                {
                    suppressSampleControlEvents = false;
                }
                return true;
            }
            else if (menuItem == menuStrip.loop2ToolStripMenuItem)
            {
                if (!Validate()) // commit any edits to current loop box
                {
                    return true;
                }
                try
                {
                    suppressSampleControlEvents = true;
                    currentLoop = 2;
                    this.textBoxLoopStart.DataBindings.Clear();
                    this.textBoxLoopStart.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "LoopStart2", true));
                    this.textBoxLoopEnd.DataBindings.Clear();
                    this.textBoxLoopEnd.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "LoopEnd2", true));
                    this.comboBoxLoopBidirectional.DataBindings.Clear();
                    this.comboBoxLoopBidirectional.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "Loop2BidirectionalAsString", true));
                    this.sampleControl.LoopStartLabel = "Loop 2 Start";
                    this.sampleControl.LoopStart = sampleObject.LoopStart2;
                    this.sampleControl.LoopEndLabel = "Loop 2 End";
                    this.sampleControl.LoopEnd = sampleObject.LoopEnd2;
                }
                finally
                {
                    suppressSampleControlEvents = false;
                }
                return true;
            }
            else if (menuItem == menuStrip.loop3ToolStripMenuItem)
            {
                if (!Validate()) // commit any edits to current loop box
                {
                    return true;
                }
                try
                {
                    suppressSampleControlEvents = true;
                    currentLoop = 3;
                    this.textBoxLoopStart.DataBindings.Clear();
                    this.textBoxLoopStart.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "LoopStart3", true));
                    this.textBoxLoopEnd.DataBindings.Clear();
                    this.textBoxLoopEnd.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "LoopEnd3", true));
                    this.comboBoxLoopBidirectional.DataBindings.Clear();
                    this.comboBoxLoopBidirectional.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "Loop3BidirectionalAsString", true));
                    this.sampleControl.LoopStartLabel = "Loop 3 Start";
                    this.sampleControl.LoopStart = sampleObject.LoopStart3;
                    this.sampleControl.LoopEndLabel = "Loop 3 End";
                    this.sampleControl.LoopEnd = sampleObject.LoopEnd3;
                }
                finally
                {
                    suppressSampleControlEvents = false;
                }
                return true;
            }
            else if (menuItem == menuStrip.selectAllToolStripMenuItem)
            {
                sampleControl.SetSelection(0, sampleObject.NumFrames, true/*startValidatePriority*/);
                return true;
            }
            else if (menuItem == menuStrip.undoToolStripMenuItem)
            {
                undoHelper.Undo();
                return true;
            }
            else if (menuItem == menuStrip.redoToolStripMenuItem)
            {
                undoHelper.Redo();
                return true;
            }
            else if (menuItem == menuStrip.cutToolStripMenuItem)
            {
                undoHelper.SaveUndoInfo(false/*forRedo*/, "Delete Sample Data");
                Copy();
                Delete();
                return true;
            }
            else if (menuItem == menuStrip.copyToolStripMenuItem)
            {
                Copy();
                return true;
            }
            else if (menuItem == menuStrip.clearToolStripMenuItem)
            {
                undoHelper.SaveUndoInfo(false/*forRedo*/, "Delete Sample Data");
                Delete();
                return true;
            }
            else if (menuItem == menuStrip.pasteToolStripMenuItem)
            {
                SampleObjectRec sample = null;
                IDataObject dataObject = Clipboard.GetDataObject();
                foreach (string format in dataObject.GetFormats())
                {
                    if (String.Equals(format, SampleClipboard.ClipboardIdentifer))
                    {
                        SampleClipboard clipboard = (SampleClipboard)dataObject.GetData(SampleClipboard.ClipboardIdentifer);
                        object o = clipboard.Reconstitute(mainWindow.Document);
                        sample = (SampleObjectRec)o;
                        break;
                    }
                }
                if (sample != null)
                {
                    undoHelper.SaveUndoInfo(false/*forRedo*/, "Insert Sample Data");
                    Delete();
                    Insert(sample.SampleData, sampleControl.SelectionStart);
                }
                return true;
            }
            else if (menuItem == menuStrip.deleteObjectToolStripMenuItem)
            {
                Close();
                mainWindow.DeleteObject(sampleObject, mainWindow.Document.SampleList);
                return true;
            }
            return false;
        }


        //

        private void textBoxName_TextChanged(object sender, EventArgs e)
        {
            GlobalNameChanged();
        }

        public void GlobalNameChanged()
        {
            this.Text = String.Format("{0} - {1}", mainWindow.DisplayName, this.textBoxName.Text);
        }


        // Undo

        public IUndoUnit CaptureCurrentStateForUndo()
        {
            return new UndoInfo(
                sampleObject,
                sampleControl,
                sampleObject.SampleData);
        }

        private class UndoInfo : IUndoUnit, IDisposable
        {
            public readonly int selectionStart;
            public readonly int selectionEnd;
            public readonly int origin;
            public readonly int loopStart1;
            public readonly int loopStart2;
            public readonly int loopStart3;
            public readonly int loopEnd1;
            public readonly int loopEnd2;
            public readonly int loopEnd3;
            //public readonly SampleStorageActualRec storage;
            private readonly FileStream persistedStorage;

            public UndoInfo(
                SampleObjectRec sampleObject,
                SampleControl sampleControl,
                SampleStorageActualRec storage)
            {
                this.selectionStart = sampleControl.SelectionStart;
                this.selectionEnd = sampleControl.SelectionEnd;
                this.origin = sampleObject.Origin;
                this.loopStart1 = sampleObject.LoopStart1;
                this.loopStart2 = sampleObject.LoopStart2;
                this.loopStart3 = sampleObject.LoopStart3;
                this.loopEnd1 = sampleObject.LoopEnd1;
                this.loopEnd2 = sampleObject.LoopEnd2;
                this.loopEnd3 = sampleObject.LoopEnd3;

                //this.storage = storage;
                if (storage != null)
                {
                    persistedStorage = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, Constants.BufferSize, FileOptions.DeleteOnClose);
                    using (BinaryWriter writer = new BinaryWriter(persistedStorage))
                    {
                        storage.Save(writer);
                    }
                }
            }

            public void Do(IUndoClient client)
            {
                SampleWindow window = (SampleWindow)client;
                window.suppressTextChangedEvents = true;
                try
                {
                    if (this.persistedStorage != null)
                    {
                        SampleStorageActualRec storage;
                        this.persistedStorage.Seek(0, SeekOrigin.Begin);
                        using (BinaryReader reader = new BinaryReader(this.persistedStorage))
                        {
                            storage = SampleStorageActualRec.Load(reader);
                        }
                        window.sampleObject.SampleData = storage;
                    }
                    window.sampleControl.SetSelection(this.selectionStart, this.selectionEnd, true/*startValidatePriority*/);
                    window.sampleObject.Origin = this.origin;
                    window.sampleObject.LoopStart1 = this.loopStart1;
                    window.sampleObject.LoopStart2 = this.loopStart2;
                    window.sampleObject.LoopStart3 = this.loopStart3;
                    window.sampleObject.LoopEnd1 = this.loopEnd1;
                    window.sampleObject.LoopEnd2 = this.loopEnd2;
                    window.sampleObject.LoopEnd3 = this.loopEnd3;
                    // TODO: scroll to selection
                }
                finally
                {
                    window.suppressTextChangedEvents = false;
                }
            }

            public void Dispose()
            {
                persistedStorage.Dispose();
            }
        }


        // Test Play

        private OutputGeneric<OutputDeviceDestination, SampleTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>, OutputDeviceArguments> state;
        private SampleTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments> generatorParams;

        private void buttonTest_MouseDown(object sender, MouseEventArgs e)
        {
            const double BufferDuration = .25f;

            if (state != null)
            {
                state.stopper.Stop();
                state.Dispose();
                state = null;
            }

            IPlayPrefsProvider playPrefsProvider = mainWindow.GetPlayPrefsProvider();

#if true // prevents "Add New Data Source..." from working
            state = SampleTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                mainWindow.DisplayName,
                OutputDeviceEnumerator.OutputDeviceGetDestination,
                OutputDeviceEnumerator.CreateOutputDeviceDestinationHandler,
                new OutputDeviceArguments(BufferDuration),
                SampleTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.MainLoop,
                generatorParams = new SampleTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                    sampleObject.SampleData,
                    playPrefsProvider.SamplingRate,
                    GetCurrentLoopStart(),
                    GetCurrentLoopEnd(),
                    GetCurrentLoopBidirectionality() == LoopBidirectionalType.Yes,
                    (GetTestPitch() / sampleObject.NaturalFrequency) * ((double)sampleObject.SamplingRate / playPrefsProvider.SamplingRate)),
                SampleTestGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Completion,
                mainWindow,
                NumChannelsType.eSampleStereo, // always send stereo to output - resampler handles sample data type
                playPrefsProvider.OutputNumBits,
                playPrefsProvider.SamplingRate,
                1/*oversampling*/,
                false/*showProgressWindow*/,
                false/*modal*/);
#endif
        }

        private void buttonTest_MouseUp(object sender, MouseEventArgs e)
        {
            // don't stop on mouse-up, just release loop
            generatorParams.loopReleaseSignaled = true;
        }
    }

#if true // prevents "Add New Data Source..." from working
    public class SampleTestGeneratorParams<T, W>
    {
        public SampleStorageActualRec data;
        public int samplingRate;
        public int loopStart;
        public int loopEnd;
        public bool bidirectionalLoop;
        public double pitchScaling;

        public bool loopReleaseSignaled;

        public Exception exception;

        public SampleTestGeneratorParams(
            SampleStorageActualRec data,
            int samplingRate,
            int loopStart,
            int loopEnd,
            bool bidirectionalLoop,
            double pitchScaling)
        {
            this.data = data;
            this.samplingRate = samplingRate;
            this.loopStart = loopStart;
            this.loopEnd = loopEnd;
            this.bidirectionalLoop = bidirectionalLoop;
            this.pitchScaling = pitchScaling;
        }

        public static OutputGeneric<T, SampleTestGeneratorParams<T, W>, W> Do(
            string baseName,
            GetDestinationMethod<T> getDestination,
            CreateDestinationHandlerMethod<T, W> createDestinationHandler,
            W destinationHandlerArguments,
            GeneratorMainLoopMethod<T, SampleTestGeneratorParams<T, W>, W> generatorMainLoop,
            SampleTestGeneratorParams<T, W> generatorParams,
            GeneratorCompletionMethod<SampleTestGeneratorParams<T, W>> generatorCompletion,
            IMainWindowServices mainWindow,
            NumChannelsType channels,
            NumBitsType bits,
            int samplingRate,
            int oversamplingFactor,
            bool showProgressWindow,
            bool modal)
        {
            // prerequisites 

            return OutputGeneric<T, SampleTestGeneratorParams<T, W>, W>.Do(
                baseName,
                getDestination,
                createDestinationHandler,
                destinationHandlerArguments,
                generatorMainLoop,
                generatorParams,
                generatorCompletion,
                channels,
                bits,
                samplingRate,
                oversamplingFactor,
                showProgressWindow,
                modal);
        }

        public static void MainLoop<U>(
            SampleTestGeneratorParams<T, W> generatorParams,
            Synthesizer.DataOutCallbackMethod<OutputGeneric<T, U, W>> dataCallback,
            OutputGeneric<T, U, W> dataCallbackState,
            Synthesizer.StopTask stopper)
        {
            try
            {
                // A bit hacky (ought to expose an API) - patch up a sample oscillator so we can reuse the playback
                // code from the synth engine
                Synthesizer.SampleStateRec state = new Synthesizer.SampleStateRec();
                state.Panning = 0;
                state.Loudness = 1;
                state.PreviousLoudness = 1;
                state.Data = generatorParams.data.Buffer;
                state.NumFrames = generatorParams.data.NumFrames;
                state.SamplePositionDifferential = new Synthesizer.Fixed64(generatorParams.pitchScaling);
                state.CurrentLoopStart = Math.Min(Math.Max(generatorParams.loopStart, 0), state.NumFrames);
                state.CurrentLoopEnd = Math.Min(Math.Max(generatorParams.loopEnd, 0), state.NumFrames);
                if (state.CurrentLoopEnd < state.CurrentLoopStart)
                {
                    state.CurrentLoopEnd = state.CurrentLoopStart;
                }
                state.CurrentLoopBidirectionality = generatorParams.bidirectionalLoop;
                if (generatorParams.bidirectionalLoop)
                {
                    if (state.CurrentLoopEnd - state.CurrentLoopStart < 2)
                    {
                        state.CurrentLoopEnd = state.CurrentLoopStart;
                    }
                }
                state.CurrentLoopLength = state.CurrentLoopEnd - state.CurrentLoopStart;
                state.LoopState = state.EffectiveLoopState = Synthesizer.LoopType.eRepeatingLoop1;
                if (state.CurrentLoopLength == 0)
                {
                    state.CurrentLoopStart = 0;
                    state.CurrentLoopEnd = state.NumFrames;
                    state.EffectiveLoopState = Synthesizer.LoopType.eNoLoop;
                }

                Synthesizer.SampleGenSamplesMethod generate = generatorParams.data.NumChannels == NumChannelsType.eSampleStereo
                    ? (Synthesizer.SampleGenSamplesMethod)Synthesizer.SampleStateRec.Sample_StereoOut_StereoSamp_Bidir
                    : (Synthesizer.SampleGenSamplesMethod)Synthesizer.SampleStateRec.Sample_StereoOut_MonoSamp_Bidir;

                const int SOUNDBUFFERLENGTHFRAMES = 256;
                // 2 channels, and 1) one private workspace used by playback overlaid with 2) 2 units for interleaved output
                using (Synthesizer.AlignedWorkspace workspace = new Synthesizer.AlignedWorkspace(4 * SOUNDBUFFERLENGTHFRAMES))
                {
                    bool PlaybackInProgress = true;
                    while (PlaybackInProgress && !stopper.Stopped)
                    {
                        if (generatorParams.loopReleaseSignaled)
                        {
                            state.EffectiveLoopState = state.LoopState = Synthesizer.LoopType.eNoLoop;
                        }

                        Synthesizer.FloatVectorZero(
                            workspace.Base,
                            workspace.Offset,
                            2 * SOUNDBUFFERLENGTHFRAMES);
                        generate(
                            state,
                            SOUNDBUFFERLENGTHFRAMES,
                            workspace.Base,
                            workspace.Offset,
                            workspace.Offset + SOUNDBUFFERLENGTHFRAMES,
                            workspace.Offset + 2 * SOUNDBUFFERLENGTHFRAMES);

                        if (state.EffectiveLoopState == Synthesizer.LoopType.eSampleFinished)
                        {
                            PlaybackInProgress = false;
                        }

                        Synthesizer.FloatVectorMakeInterleaved(
                            workspace.Base,
                            workspace.Offset,
                            workspace.Base,
                            workspace.Offset + SOUNDBUFFERLENGTHFRAMES,
                            SOUNDBUFFERLENGTHFRAMES,
                            workspace.Base,
                            workspace.Offset + 2 * SOUNDBUFFERLENGTHFRAMES);
                        dataCallback(
                            dataCallbackState,
                            workspace.Base,
                            workspace.Offset + 2 * SOUNDBUFFERLENGTHFRAMES,
                            SOUNDBUFFERLENGTHFRAMES);
                    }
                }
            }
            catch (Exception exception)
            {
                generatorParams.exception = exception;
                stopper.Stop();
            }
        }

        public static void Completion(
            SampleTestGeneratorParams<T, W> generatorParams,
            ref ClipInfo clipInfo)
        {
        }
    }
#endif
}
