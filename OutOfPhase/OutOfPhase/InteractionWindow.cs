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

using TextEditor;

namespace OutOfPhase
{
    public partial class InteractionWindow : Form, IMenuStripManagerHandler, IGlobalNameChange
    {
        private readonly MainWindow mainWindow;

        public InteractionWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            this.textBox.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            // Place interaction window across bottom of screen.
            const int Margin = 50;
            int width = Screen.PrimaryScreen.Bounds.Width - 2 * Margin;
            Size = new Size(width, Height);
            Location = new Point(Margin, Screen.PrimaryScreen.Bounds.Height - Height - Margin);

            menuStripManager.SetGlobalHandler(mainWindow);
            menuStripManager.HookUpTextEditorWindowHelper(this.textEditorWindowHelper);

            GlobalNameChanged();
        }

        public delegate void AppendDelegate(string text);
        public void Append(string text)
        {
            textBox.ReplaceRangeAndSelect(
                textBox.End,
                textBox.End,
                text,
                1);
            textBox.ScrollToSelectionEndEdge();
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

        // MainWindow does hooking of Disposed event to handle InteractionWindow being closed


        //

        public void GlobalNameChanged()
        {
            this.Text = String.Format("{0} - {1}", mainWindow.DisplayName, "Interaction");
        }


        //

        void IMenuStripManagerHandler.EnableMenuItems(MenuStripManager menuStrip)
        {
            textEditorWindowHelper.MenuActivateDelegate();
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            return false;
        }
    }
}
