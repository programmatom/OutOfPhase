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
    public partial class TrackWindow : Form, IMenuStripManagerHandler, IGlobalNameChange
    {
        private readonly Registration registration;
        private readonly TrackObjectRec trackObject;
        private readonly MainWindow mainWindow;
        private readonly ToolStripMenuItem backgroundToolStripMenuItem;
        private readonly ToolStripMenuItem inlineEditToolStripMenuItem;

        public TrackWindow(Registration registration, TrackObjectRec trackObject, MainWindow mainWindow)
        {
            this.registration = registration;
            this.trackObject = trackObject;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            // By default make new tracks take up most of the horizontal screen width.
            SetDesktopBounds(DesktopBounds.X, DesktopBounds.Y, Screen.PrimaryScreen.Bounds.Width - DesktopBounds.X - 100, DesktopBounds.Height);

            menuStripManager.SetGlobalHandler(mainWindow);

            GlobalNameChanged();

            registration.Register(trackObject, this);

            backgroundToolStripMenuItem = new ToolStripMenuItem("Background", null, new EventHandler(backgroundMenuItem_Click));
            menuStripManager.ContainedMenuStrip.Items.Add(backgroundToolStripMenuItem);
            inlineEditToolStripMenuItem = new ToolStripMenuItem("Inline", null, new EventHandler(inlineEditMenuItem_Click));
            menuStripManager.ContainedMenuStrip.Items.Add(inlineEditToolStripMenuItem);

            trackEditControl.Init(trackObject, mainWindow, menuStripManager, backgroundToolStripMenuItem, inlineEditToolStripMenuItem);

            trackObject.PropertyChanged += TrackObject_PropertyChanged;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            trackObject.PropertyChanged -= TrackObject_PropertyChanged;
            registration.Unregister(trackObject, this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, trackObject.SavedWindowXLoc, trackObject.SavedWindowYLoc, trackObject.SavedWindowWidth, trackObject.SavedWindowHeight);

            base.OnShown(e);

            // deferred until OnShown because that's when trackViewControl.Height is finally valid
            trackEditControl.SetDefaultScrollPosition();

            trackEditControl.FocusToView();
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                trackObject.SavedWindowXLoc = x;
                trackObject.SavedWindowYLoc = y;
                trackObject.SavedWindowWidth = width;
                trackObject.SavedWindowHeight = height;
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

        public TrackEditControl Edit { get { return trackEditControl; } }


        // MenuStripManager methods

        private void backgroundMenuItem_Click(object sender, EventArgs e)
        {
            trackEditControl.backgroundMenuItem_Click(sender, e);
        }

        private void inlineEditMenuItem_Click(object sender, EventArgs e)
        {
            trackEditControl.inlineEditingMenuItem_Click(sender, e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            menuStripManager.ProcessCmdKeyDelegate(ref msg, keyData);

            return base.ProcessCmdKey(ref msg, keyData);
        }

        void IMenuStripManagerHandler.EnableMenuItems(MenuStripManager menuStrip)
        {
            menuStrip.deleteObjectToolStripMenuItem.Enabled = true;
            menuStrip.deleteObjectToolStripMenuItem.Text = "Delete Track";

            ((IMenuStripManagerHandler)trackEditControl).EnableMenuItems(menuStrip);
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            if (menuItem == menuStrip.closeDocumentToolStripMenuItem)
            {
                Close();
                return true;
            }
            else if (menuItem == menuStrip.deleteObjectToolStripMenuItem)
            {
                Close();
                mainWindow.DeleteObject(trackObject, mainWindow.Document.TrackList);
                return true;
            }
            else if (((IMenuStripManagerHandler)trackEditControl).ExecuteMenuItem(menuStrip, menuItem))
            {
                return true;
            }

            return false;
        }


        //

        private void TrackObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == TrackObjectRec.Name_PropertyName)
            {
                GlobalNameChanged();
            }
        }

        public void GlobalNameChanged()
        {
            this.Text = String.Format("{0} - {1}", mainWindow.DisplayName, trackObject.Name);
        }
    }
}
