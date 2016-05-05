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

namespace OutOfPhase
{
    public partial class AlgoSampWindow : Form, IHighlightLine, IMenuStripManagerHandler, IGlobalNameChange
    {
        private readonly Registration registration;
        private readonly AlgoSampObjectRec algoSampObject;
        private readonly IMainWindowServices mainWindow;

        public AlgoSampWindow(Registration registration, AlgoSampObjectRec algoSampObject, IMainWindowServices mainWindow)
        {
            this.registration = registration;
            this.algoSampObject = algoSampObject;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            this.textBoxFormula.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            this.textBoxFormula.AutoIndent = Program.Config.AutoIndent;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            menuStripManager.SetGlobalHandler(mainWindow);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);
            menuStripManager.HookUpTextBoxWindowHelper(this.textBoxWindowHelper);

            foreach (string item in EnumUtility.GetDescriptions(AlgoSampObjectRec.NumChannelsAllowedValues))
            {
                comboBoxChannels.Items.Add(item);
            }

            foreach (string item in EnumUtility.GetDescriptions(AlgoSampObjectRec.LoopBidirectionalAllowedValues))
            {
                comboBoxLoop1Bidirectional.Items.Add(item);
                comboBoxLoop2Bidirectional.Items.Add(item);
                comboBoxLoop3Bidirectional.Items.Add(item);
            }

            documentBindingSource.Add(mainWindow.Document);
            algoSampObjectRecBindingSource.Add(algoSampObject);

            textBoxName.TextChanged += new EventHandler(textBoxName_TextChanged);
            GlobalNameChanged();

            registration.Register(algoSampObject, this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            registration.Unregister(algoSampObject, this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, algoSampObject.SavedWindowXLoc, algoSampObject.SavedWindowYLoc, algoSampObject.SavedWindowWidth, algoSampObject.SavedWindowHeight);
            base.OnShown(e);
            textBoxFormula.SetInsertionPoint(0, 0);
            textBoxFormula.Focus();
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                algoSampObject.SavedWindowXLoc = x;
                algoSampObject.SavedWindowYLoc = y;
                algoSampObject.SavedWindowWidth = width;
                algoSampObject.SavedWindowHeight = height;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            ResizeMove();
            base.OnResize(e);
        }

        protected override void OnMove(EventArgs e)
        {
            ResizeMove();
            base.OnMove(e);
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

        public bool BuildThis(bool force)
        {
            if (!Validate()) // ensure all controls commit data to store
            {
                return false;
            }

            // ensure [only, at least] function code is built
            if (!mainWindow.MakeUpToDateFunctions())
            {
                return false;
            }

            return algoSampObject.EnsureBuilt(
                force,
                new PcodeExterns(mainWindow),
                delegate (object source, BuildErrorInfo errorInfo)
                {
                    Debug.Assert(source == algoSampObject);
                    HighlightLine(errorInfo.LineNumber);
                    MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                });
        }

        public bool DisassembleThis()
        {
            if (!Validate()) // ensure all controls commit data to store
            {
                return false;
            }

            // ensure [only, at least] function code is built
            if (!mainWindow.MakeUpToDateFunctions())
            {
                return false;
            }

            PcodeRec FuncCode;
            if (!algoSampObject.BuildCode(mainWindow.DefaultBuildFailedCallback, out FuncCode))
            {
                return false;
            }
            string DisassemblyText = Compiler.DisassemblePcode(FuncCode);
            new DisassemblyWindow(DisassemblyText, mainWindow, String.Format("{0} - Disassembly", algoSampObject.Name)).Show();

            return true;
        }


        //

        public void HighlightLine(int line)
        {
            textBoxFormula.Focus();
            textBoxFormula.SetSelectionLine(line - 1);
            textBoxFormula.ScrollToSelection();
        }


        // MenuStripManager methods

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            menuStripManager.ProcessCmdKeyDelegate(ref msg, keyData);

            if (textEditorWindowHelper.ProcessCmdKeyDelegate(ref msg, keyData))
            {
                return true;
            }
            if (textBoxWindowHelper.ProcessCmdKeyDelegate(ref msg, keyData))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        void IMenuStripManagerHandler.EnableMenuItems(MenuStripManager menuStrip)
        {
            textEditorWindowHelper.MenuActivateDelegate();
            textBoxWindowHelper.MenuActivateDelegate();

            menuStrip.buildObjectToolStripMenuItem.Enabled = true;

            menuStrip.openAsSampleToolStripMenuItem.Visible = true;
            menuStrip.openAsSampleToolStripMenuItem.Enabled = true;

            menuStrip.disassembleToolStripMenuItem.Enabled = true;
            menuStrip.disassembleToolStripMenuItem.Visible = true;

            menuStrip.deleteObjectToolStripMenuItem.Enabled = true;
            menuStrip.deleteObjectToolStripMenuItem.Text = "Delete Algorithmic Sample";
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            if (menuItem == menuStrip.buildObjectToolStripMenuItem)
            {
                BuildThis(true/*force*/);
                return true;
            }
            else if (menuItem == menuStrip.disassembleToolStripMenuItem)
            {
                DisassembleThis();
                return true;
            }
            else if (menuItem == menuStrip.openAsSampleToolStripMenuItem)
            {
                if (BuildThis(false/*force*/))
                {
                    NumBitsType numBits = NumBitsType.Max;
                    float[] buffer = (float[])algoSampObject.SampleData.Buffer.Clone();
                    SampConv.QuantizeAndClampVector(buffer, numBits); // ensure target bit depth is honored
                    SampleObjectRec sampleObject = new SampleObjectRec(
                        mainWindow.Document,
                        buffer,
                        algoSampObject.SampleData.NumFrames,
                        numBits,
                        algoSampObject.NumChannels,
                        algoSampObject.Origin,
                        algoSampObject.LoopStart1,
                        algoSampObject.LoopStart2,
                        algoSampObject.LoopStart3,
                        algoSampObject.LoopEnd1,
                        algoSampObject.LoopEnd2,
                        algoSampObject.LoopEnd3,
                        algoSampObject.SamplingRate,
                        algoSampObject.NaturalFrequency);
                    sampleObject.Name = String.Format("Copy of {0}", algoSampObject.Name);
                    mainWindow.Document.SampleList.Add(sampleObject);
                    new SampleWindow(registration, sampleObject, mainWindow).Show();
                }
                return true;
            }
            else if (menuItem == menuStrip.deleteObjectToolStripMenuItem)
            {
                Close();
                mainWindow.DeleteObject(algoSampObject, mainWindow.Document.AlgoSampList);
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
    }
}
