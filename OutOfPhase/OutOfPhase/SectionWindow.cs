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
    public partial class SectionWindow : Form, IHighlightLine, IMenuStripManagerHandler, IGlobalNameChange
    {
        private readonly Registration registration;
        private readonly SectionObjectRec section;
        private readonly MainWindow mainWindow;

        public SectionWindow(Registration registration, SectionObjectRec section, MainWindow mainWindow)
        {
            this.registration = registration;
            this.section = section;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            this.textBoxSectionText.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            this.textBoxSectionText.AutoIndent = Program.Config.AutoIndent;

            menuStripManager.SetGlobalHandler(mainWindow);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);
            menuStripManager.HookUpTextBoxWindowHelper(this.textBoxWindowHelper);

            documentBindingSource.Add(mainWindow.Document);
            sectionObjectRecBindingSource.Add(section);

            textBoxSectionName.TextChanged += new EventHandler(textBoxSectionName_TextChanged);
            GlobalNameChanged();

            registration.Register(section, this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            registration.Unregister(section, this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, section.SavedWindowXLoc, section.SavedWindowYLoc, section.SavedWindowWidth, section.SavedWindowHeight);
            base.OnShown(e);
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                section.SavedWindowXLoc = x;
                section.SavedWindowYLoc = y;
                section.SavedWindowWidth = width;
                section.SavedWindowHeight = height;
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

            return section.EnsureBuilt(
                force,
                new PcodeExterns(mainWindow),
                delegate(object source, BuildErrorInfo errorInfo)
                {
                    Debug.Assert(source == section);
                    HighlightLine(errorInfo.LineNumber);
                    MessageBox.Show(errorInfo.CompositeErrorMessage, "Error", MessageBoxButtons.OK);
                });
        }


        //

        public void HighlightLine(int line)
        {
            line--;
            textBoxSectionText.Focus();
            textBoxSectionText.SetSelectionLine(line);
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
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            if (menuItem == menuStrip.buildObjectToolStripMenuItem)
            {
                BuildThis(true/*force*/);
                return true;
            }
            return false;
        }


        //

        private void textBoxSectionName_TextChanged(object sender, EventArgs e)
        {
            GlobalNameChanged();
        }

        public void GlobalNameChanged()
        {
            this.Text = String.Format("{0} - {1}", mainWindow.DisplayName, this.textBoxSectionName.Text);
        }
    }
}
