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
using System.Text;
using System.Windows.Forms;

using TextEditor;

namespace OutOfPhase
{
    public partial class CalculatorWindow : Form, IMenuStripManagerHandler, IGlobalNameChange
    {
        private Document document;
        private MainWindow mainWindow;

        private int LastLine;

        public CalculatorWindow(Document document, MainWindow mainWindow)
        {
            this.document = document;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            this.textBox.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            this.textBox.AutoIndent = Program.Config.AutoIndent;

            menuStripManager.SetGlobalHandler(mainWindow);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);

            documentBindingSource.Add(mainWindow.Document);

            mainWindow.AddMiscForm(this);

            GlobalNameChanged();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            mainWindow.RemoveMiscForm(this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.textBox.Focus();
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

        private void SetUpSelection()
        {
            if (!textBox.SelectionNonEmpty)
            {
                LastLine = Math.Min(LastLine, textBox.Count);
                textBox.SetSelectionLine(LastLine);
                textBox.ScrollToSelection();
            }
        }

        private bool Compile(out PcodeRec FuncCode, out DataTypes ReturnType)
        {
            int LineNumber;
            Compiler.ASTExpression AST;
            CompileErrors Error = Compiler.CompileSpecialFunction(
                document.CodeCenter,
                new FunctionParamRec[0]/*no arguments*/,
                out LineNumber,
                out ReturnType,
                textBox.SelectedTextStorage.GetText(Environment.NewLine),
                false/*suppressCILEmission*/,
                out FuncCode,
                out AST);
            if (Error != CompileErrors.eCompileNoError)
            {
                textBox.Focus();
                textBox.SetSelectionLine(textBox.SelectionStartLine + LineNumber - 1);
                textBox.ScrollToSelection();
                BuildErrorInfo error = new LiteralBuildErrorInfo(
                    Compiler.GetCompileErrorString(Error),
                    LineNumber);
                MessageBox.Show(error.CompositeErrorMessage, "Compile Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }
            return true;
        }

        private void Disassemble()
        {
            SetUpSelection();

            PcodeRec FuncCode;
            DataTypes ReturnType;
            if (!Compile(out FuncCode, out ReturnType))
            {
                return;
            }

            string DisassemblyText = Compiler.DisassemblePcode(FuncCode, Environment.NewLine);
            new DisassemblyWindow(DisassemblyText, mainWindow, "Calculator").Show();
        }

        private void DoCalculation()
        {
            if (!mainWindow.MakeUpToDate())
            {
                return;
            }

            SetUpSelection();

            PcodeRec FuncCode;
            DataTypes ReturnType;
            if (!Compile(out FuncCode, out ReturnType))
            {
                return;
            }

            /* try to evaluate the code */

            StringBuilder Output = new StringBuilder();
            using (ParamStackRec ParamList = new ParamStackRec())
            {
                /* return address placeholder */
                ParamList.AddIntegerToStack(0);

                /* executing the actual code */
                EvalErrInfoRec ErrorInfo;
                EvalErrors EvaluationError = PcodeSystem.EvaluatePcodeThread.EvaluatePcode(
                    ParamList,
                    FuncCode,
                    document.CodeCenter,
                    out ErrorInfo,
                    new PcodeExterns(mainWindow));
                if (EvaluationError != EvalErrors.eEvalNoError)
                {
                    PcodeEvaluationErrorInfo error = new PcodeEvaluationErrorInfo(
                        EvaluationError,
                        ErrorInfo,
                        FuncCode,
                        document.CodeCenter);
                    MessageBox.Show(error.CompositeErrorMessage, "Evaluation Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                Debug.Assert(ParamList.GetStackNumElements() == 1); // return value

                /* add new data to window */
                Output.AppendLine();
                switch (ReturnType)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case DataTypes.eBoolean:
                        Output.AppendLine(
                            ParamList.GetStackInteger(0) != 0
                                ? "returns boolean:  true"
                                : "returns boolean:  false");
                        break;
                    case DataTypes.eInteger:
                        Output.AppendFormat("returns integer:  {0}", ParamList.GetStackInteger(0));
                        Output.AppendLine();
                        break;
                    case DataTypes.eFloat:
                        Output.AppendFormat("returns float:  {0}", ParamList.GetStackFloat(0));
                        Output.AppendLine();
                        break;
                    case DataTypes.eDouble:
                        Output.AppendFormat("returns double:  {0}", ParamList.GetStackDouble(0));
                        Output.AppendLine();
                        break;
                    case DataTypes.eArrayOfBoolean:
                        Output.AppendFormat("returns array of booleans:");
                        Output.AppendLine();
                        goto ArrayMakePoint;
                    case DataTypes.eArrayOfByte:
                        Output.AppendFormat("returns array of bytes:");
                        Output.AppendLine();
                        goto ArrayMakePoint;
                    case DataTypes.eArrayOfInteger:
                        Output.AppendFormat("returns array of integers:");
                        Output.AppendLine();
                        goto ArrayMakePoint;
                    case DataTypes.eArrayOfFloat:
                        Output.AppendFormat("returns array of floats:");
                        Output.AppendLine();
                        goto ArrayMakePoint;
                    case DataTypes.eArrayOfDouble:
                        Output.AppendFormat("returns array of doubles:");
                        Output.AppendLine();
                    ArrayMakePoint:
                        if (ParamList.GetStackArray(0) == null)
                        {
                            Output.AppendFormat("NIL");
                            Output.AppendLine();
                        }
                        else
                        {
                            switch (ReturnType)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case DataTypes.eArrayOfBoolean:
                                    {
                                        byte[] a = (byte[])ParamList.GetStackArray(0);
                                        for (int i = 0; i < a.Length; i++)
                                        {
                                            int value = a[i];
                                            Output.AppendFormat("{0,8}:  {1}", i, value != 0 ? "true" : "false");
                                            Output.AppendLine();
                                        }
                                    }
                                    break;
                                case DataTypes.eArrayOfByte:
                                    {
                                        byte[] a = (byte[])ParamList.GetStackArray(0);
                                        for (int i = 0; i < a.Length; i++)
                                        {
                                            int value = a[i];
                                            Output.AppendFormat("{0,8}:{1,3}", i, value);
                                            Output.AppendLine();
                                        }
                                    }
                                    break;
                                case DataTypes.eArrayOfInteger:
                                    {
                                        int[] a = (int[])ParamList.GetStackArray(0);
                                        for (int i = 0; i < a.Length; i++)
                                        {
                                            int value = a[i];
                                            Output.AppendFormat("{0,8}:{1,10}", i, value);
                                            Output.AppendLine();
                                        }
                                    }
                                    break;
                                case DataTypes.eArrayOfFloat:
                                    {
                                        float[] a = (float[])ParamList.GetStackArray(0);
                                        for (int i = 0; i < a.Length; i++)
                                        {
                                            float value = a[i];
                                            Output.AppendFormat("{0,8}:{1,12}", i, value);
                                            Output.AppendLine();
                                        }
                                    }
                                    break;
                                case DataTypes.eArrayOfDouble:
                                    {
                                        double[] a = (double[])ParamList.GetStackArray(0);
                                        for (int i = 0; i < a.Length; i++)
                                        {
                                            double value = a[i];
                                            Output.AppendFormat("{0,8}:{1,18}", i, value);
                                            Output.AppendLine();
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }

            Output.AppendLine();

            textBox.ReplaceRangeAndSelect(
                textBox.End,
                textBox.End,
                Output.ToString(),
                1);
            LastLine = textBox.Count - 1;
        }


        // MenuStripManager methods

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            menuStripManager.ProcessCmdKeyDelegate(ref msg, keyData);

            if (textEditorWindowHelper.ProcessCmdKeyDelegate(ref msg, keyData))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        void IMenuStripManagerHandler.EnableMenuItems(MenuStripManager menuStrip)
        {
            textEditorWindowHelper.MenuActivateDelegate();

            menuStrip.evaluateToolStripMenuItem.Enabled = true;

            menuStrip.disassembleToolStripMenuItem.Visible = true;
            menuStrip.disassembleToolStripMenuItem.Enabled = true;
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            if (menuItem == menuStrip.evaluateToolStripMenuItem)
            {
                DoCalculation();
                return true;
            }
            else if (menuItem == menuStrip.disassembleToolStripMenuItem)
            {
                Disassemble();
                return true;
            }
            return false;
        }


        //

        public void GlobalNameChanged()
        {
            this.Text = String.Format("{0} - {1}", mainWindow.DisplayName, "Calculator");
        }
    }
}
