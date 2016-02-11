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
    public partial class AlgoWaveTableWindow : Form, IHighlightLine, IMenuStripManagerHandler, IGlobalNameChange
    {
        private readonly Registration registration;
        private readonly AlgoWaveTableObjectRec algoWaveTableObject;
        private readonly MainWindow mainWindow;

        public AlgoWaveTableWindow(Registration registration, AlgoWaveTableObjectRec algoWaveTableObject, MainWindow mainWindow)
        {
            this.registration = registration;
            this.algoWaveTableObject = algoWaveTableObject;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            this.textBoxFunction.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            this.textBoxFunction.AutoIndent = Program.Config.AutoIndent;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            menuStripManager.SetGlobalHandler(mainWindow);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);
            menuStripManager.HookUpTextBoxWindowHelper(this.textBoxWindowHelper);

            foreach (int i in AlgoWaveTableObjectRec.NumFramesAllowedValues)
            {
                comboBoxNumFrames.Items.Add(i.ToString());
            }

            documentBindingSource.Add(mainWindow.Document);
            algoWaveTableObjectRecBindingSource.Add(algoWaveTableObject);

            textBoxName.TextChanged += new EventHandler(textBoxName_TextChanged);
            GlobalNameChanged();

            registration.Register(algoWaveTableObject, this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            registration.Unregister(algoWaveTableObject, this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, algoWaveTableObject.SavedWindowXLoc, algoWaveTableObject.SavedWindowYLoc, algoWaveTableObject.SavedWindowWidth, algoWaveTableObject.SavedWindowHeight);
            base.OnShown(e);
            textBoxFunction.SetInsertionPoint(0, 0);
            textBoxFunction.Focus();
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                algoWaveTableObject.SavedWindowXLoc = x;
                algoWaveTableObject.SavedWindowYLoc = y;
                algoWaveTableObject.SavedWindowWidth = width;
                algoWaveTableObject.SavedWindowHeight = height;
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

            return algoWaveTableObject.EnsureBuilt(
                force,
                new PcodeExterns(mainWindow),
                delegate (object source, BuildErrorInfo errorInfo)
                {
                    Debug.Assert(source == algoWaveTableObject);
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
            if (!algoWaveTableObject.BuildCode(mainWindow.DefaultBuildFailedCallback, out FuncCode))
            {
                return false;
            }
            string DisassemblyText = Compiler.DisassemblePcode(FuncCode, Environment.NewLine);
            new DisassemblyWindow(DisassemblyText, mainWindow, String.Format("{0} - Disassembly", algoWaveTableObject.Name)).Show();

            return true;
        }


        //

        public void HighlightLine(int line)
        {
            textBoxFunction.Focus();
            textBoxFunction.SetSelectionLine(line - 1);
            textBoxFunction.ScrollToSelection();
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

            menuStrip.openAsWaveTableToolStripMenuItem.Visible = true;
            menuStrip.openAsWaveTableToolStripMenuItem.Enabled = true;

            menuStrip.disassembleToolStripMenuItem.Enabled = true;
            menuStrip.disassembleToolStripMenuItem.Visible = true;

            menuStrip.deleteObjectToolStripMenuItem.Enabled = true;
            menuStrip.deleteObjectToolStripMenuItem.Text = "Delete Algorithmic Wave Table";
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
            else if (menuItem == menuStrip.openAsWaveTableToolStripMenuItem)
            {
                if (BuildThis(false/*force*/))
                {
                    float[] raw = algoWaveTableObject.WaveTableData.GetRawCopy();
                    SampConv.QuantizeAndClampVector(raw, algoWaveTableObject.WaveTableData.NumBits); // ensure target bit depth is honored
                    WaveTableObjectRec waveTableObject = new WaveTableObjectRec(
                        mainWindow.Document,
                        new WaveTableStorageRec(
                            algoWaveTableObject.WaveTableData.NumTables,
                            algoWaveTableObject.WaveTableData.NumFrames,
                            algoWaveTableObject.WaveTableData.NumBits,
                            raw));
                    waveTableObject.Name = String.Format("Copy of {0}", algoWaveTableObject.Name);
                    mainWindow.Document.WaveTableList.Add(waveTableObject);
                    new WaveTableWindow(registration, waveTableObject, mainWindow).Show();
                }
                return true;
            }
            else if (menuItem == menuStrip.deleteObjectToolStripMenuItem)
            {
                Close();
                mainWindow.DeleteObject(algoWaveTableObject, mainWindow.Document.AlgoWaveTableList);
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
