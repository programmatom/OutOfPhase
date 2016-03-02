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
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class DisassemblyWindow : Form, IMenuStripManagerHandler, IGlobalNameChange
    {
        private MainWindow mainWindow;
        private string functionModuleName;

        public DisassemblyWindow(string text, MainWindow mainWindow, string functionModuleName)
        {
            this.mainWindow = mainWindow;
            this.functionModuleName = functionModuleName;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            menuStripManager.SetGlobalHandler(mainWindow);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);

            mainWindow.AddMiscForm(this);

            textBoxDisassembly.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            textBoxDisassembly.Text = text;
            textBoxDisassembly.SetInsertionPoint(0, 0);

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom, delegate () { return new Control[] { textBoxDisassembly }; });

            // By default make disassembly window tkae up most of the vertical screen height.
            const int Margin = 20;
            Size = new Size(Size.Width, Screen.PrimaryScreen.WorkingArea.Height - Margin);

            GlobalNameChanged();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            mainWindow.RemoveMiscForm(this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            // Ensure bottom of form isn't under task bar
            Location = new Point(Location.X, Location.Y - Math.Max(0, Location.Y + Size.Height - Screen.PrimaryScreen.WorkingArea.Height));

            base.OnShown(e);
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
            dpiChangeHelper.WndProcDelegate(ref m, delegate () { return new Control[] { textBoxDisassembly }; });
            base.WndProc(ref m);
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
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            return false;
        }


        //

        public void GlobalNameChanged()
        {
            this.Text = String.Format("{0} - {1} - {2}", mainWindow.DisplayName, functionModuleName, "Disassembly");
        }
    }
}
