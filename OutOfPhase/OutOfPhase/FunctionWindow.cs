/*
 *  Copyright � 1994-2002, 2015-2016 Thomas R. Lawrence
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
    public partial class FunctionWindow : Form, IHighlightLine, IMenuStripManagerHandler, IGlobalNameChange
    {
        private readonly Registration registration;
        private readonly FunctionObjectRec functionObject;
        private readonly Document document;
        private readonly IMainWindowServices mainWindow;

        public FunctionWindow(Registration registration, FunctionObjectRec functionObject, Document document, IMainWindowServices mainWindow)
        {
            this.registration = registration;
            this.functionObject = functionObject;
            this.document = document;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            this.textBoxFunctionBody.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            this.textBoxFunctionBody.AutoIndent = Program.Config.AutoIndent;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            menuStripManager.SetGlobalHandler(mainWindow);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);
            menuStripManager.HookUpTextBoxWindowHelper(this.textBoxWindowHelper);

            documentBindingSource.Add(mainWindow.Document);
            functionObjectRecBindingSource.Add(functionObject);

            textBoxFunctionName.TextChanged += new EventHandler(textBoxFunctionName_TextChanged);
            GlobalNameChanged();

            registration.Register(functionObject, this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            registration.Unregister(functionObject, this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, functionObject.SavedWindowXLoc, functionObject.SavedWindowYLoc, functionObject.SavedWindowWidth, functionObject.SavedWindowHeight);
            base.OnShown(e);
            textBoxFunctionBody.SetInsertionPoint(0, 0);
            textBoxFunctionBody.Focus();
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                functionObject.SavedWindowXLoc = x;
                functionObject.SavedWindowYLoc = y;
                functionObject.SavedWindowWidth = width;
                functionObject.SavedWindowHeight = height;
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

        public enum BuildResult
        {
            Succeeded,
            SyntaxCheckedOnly,
            Failed,
        }

        public BuildResult BuildThis(bool force)
        {
            if (!Validate()) // ensure all controls commit data to store
            {
                return BuildResult.Failed;
            }

            if (functionObject.EnsureBuilt(
                force,
                new PcodeExterns(mainWindow),
                delegate (object source, BuildErrorInfo errorInfo) { }))
            {
                // Prefer proper full build - if it succeeds, since that populates the real CodeCenter object and also ensures
                // function signatures match function call argument lists. Disassembly will be available in this case.
                return BuildResult.Succeeded;
            }
            else
            {
                // If there are compile errors, perform a partial compilation that does not enforce linkability. Disassembly
                // may not be available in this case if conflicting signagures have been inferred for a function.
                if (functionObject.TestBuild(
                    force,
                    new PcodeExterns(mainWindow),
                    delegate (object source, BuildErrorInfo errorInfo)
                    {
                        if (source == functionObject)
                        {
                            HighlightLine(errorInfo.LineNumber);
                            MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                        }
                        else
                        {
                            mainWindow.DefaultBuildFailedCallback(source, errorInfo);
                        }
                    },
                    new CodeCenterRec()/*throw-away*/))
                {
                    return BuildResult.SyntaxCheckedOnly;
                }
                else
                {
                    return BuildResult.Failed;
                }
            }
        }


        //

        public void HighlightLine(int line)
        {
            textBoxFunctionBody.Focus();
            textBoxFunctionBody.SetSelectionLine(line - 1);
            textBoxFunctionBody.ScrollToSelection();
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

            menuStrip.disassembleToolStripMenuItem.Visible = true;
            menuStrip.disassembleToolStripMenuItem.Enabled = true;

            menuStrip.deleteObjectToolStripMenuItem.Enabled = true;
            menuStrip.deleteObjectToolStripMenuItem.Text = "Delete Function Module";
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
                switch (BuildThis(false/*force*/))
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();

                    case BuildResult.Succeeded:
                        StringBuilder sb = new StringBuilder();
                        foreach (FuncCodeRec TheFunction in document.CodeCenter.GetListOfFunctionsForModule(functionObject))
                        {
                            sb.AppendLine();
                            sb.AppendLine();
                            sb.AppendLine(TheFunction.GetFunctionName());
                            sb.AppendLine();

                            sb.Append(Compiler.DisassemblePcode(TheFunction.GetFunctionPcode()));
                        }
                        sb.AppendLine();
                        new DisassemblyWindow(sb.ToString(), mainWindow, textBoxFunctionName.Text).Show();
                        break;

                    case BuildResult.Failed:
                        break;

                    case BuildResult.SyntaxCheckedOnly:
                        MessageBox.Show("Compile errors in other modules prevented the disassembly for this module from being generated.", "Out Of Phase");
                        break;
                }
                return true;
            }
            else if (menuItem == menuStrip.deleteObjectToolStripMenuItem)
            {
                Close();
                mainWindow.DeleteObject(functionObject, mainWindow.Document.FunctionList);
                return true;
            }
            return false;
        }


        //

        private void textBoxFunctionName_TextChanged(object sender, EventArgs e)
        {
            GlobalNameChanged();
        }

        public void GlobalNameChanged()
        {
            this.Text = String.Format("{0} - {1}", mainWindow.DisplayName, this.textBoxFunctionName.Text);
        }
    }
}
