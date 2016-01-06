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
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class TextBoxWindowHelper : Component
    {
        private ContainerControl containerControl;

        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem cutToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ToolStripMenuItem clearToolStripMenuItem;
        private ToolStripMenuItem selectAllToolStripMenuItem;

        private bool delegatedMode; // false: fire events using MenuItem.Click() event; true: caller invokes via ProcessMenuItemDelegate(MenuItem)

        public TextBoxWindowHelper()
        {
            InitializeComponent();
        }

        public TextBoxWindowHelper(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }


        [Browsable(true), Category("Container Control")]
        public ContainerControl ContainerControl
        {
            get { return containerControl; }
            set
            {
                containerControl = value;
            }
        }


        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem UndoToolStripMenuItem
        {
            get { return undoToolStripMenuItem; }
            set
            {
                if (undoToolStripMenuItem != null)
                {
                    undoToolStripMenuItem.Click -= new EventHandler(undoToolStripMenuItem_Click);
                }

                undoToolStripMenuItem = value;

                if ((undoToolStripMenuItem != null) && !delegatedMode)
                {
                    undoToolStripMenuItem.Click += new EventHandler(undoToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem CutToolStripMenuItem
        {
            get { return cutToolStripMenuItem; }
            set
            {
                if (cutToolStripMenuItem != null)
                {
                    cutToolStripMenuItem.Click -= new EventHandler(cutToolStripMenuItem_Click);
                }

                cutToolStripMenuItem = value;

                if ((cutToolStripMenuItem != null) && !delegatedMode)
                {
                    cutToolStripMenuItem.Click += new EventHandler(cutToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem CopyToolStripMenuItem
        {
            get { return copyToolStripMenuItem; }
            set
            {
                if (copyToolStripMenuItem != null)
                {
                    copyToolStripMenuItem.Click -= new EventHandler(copyToolStripMenuItem_Click);
                }

                copyToolStripMenuItem = value;

                if ((copyToolStripMenuItem != null) && !delegatedMode)
                {
                    copyToolStripMenuItem.Click += new EventHandler(copyToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem PasteToolStripMenuItem
        {
            get { return pasteToolStripMenuItem; }
            set
            {
                if (pasteToolStripMenuItem != null)
                {
                    pasteToolStripMenuItem.Click -= new EventHandler(pasteToolStripMenuItem_Click);
                }

                pasteToolStripMenuItem = value;

                if ((pasteToolStripMenuItem != null) && !delegatedMode)
                {
                    pasteToolStripMenuItem.Click += new EventHandler(pasteToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem ClearToolStripMenuItem
        {
            get { return clearToolStripMenuItem; }
            set
            {
                if (clearToolStripMenuItem != null)
                {
                    clearToolStripMenuItem.Click -= new EventHandler(clearToolStripMenuItem_Click);
                }

                clearToolStripMenuItem = value;

                if ((clearToolStripMenuItem != null) && !delegatedMode)
                {
                    clearToolStripMenuItem.Click += new EventHandler(clearToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem SelectAllToolStripMenuItem
        {
            get { return selectAllToolStripMenuItem; }
            set
            {
                if (selectAllToolStripMenuItem != null)
                {
                    selectAllToolStripMenuItem.Click -= new EventHandler(selectAllToolStripMenuItem_Click);
                }

                selectAllToolStripMenuItem = value;

                if ((selectAllToolStripMenuItem != null) && !delegatedMode)
                {
                    selectAllToolStripMenuItem.Click += new EventHandler(selectAllToolStripMenuItem_Click);
                }
            }
        }


        [Browsable(true), Category("Behavior"), DefaultValue(false)]
        public bool DelegatedMode
        {
            get
            {
                return delegatedMode;
            }
            set
            {
                delegatedMode = value;
            }
        }


        private TextBox ActiveTextBox
        {
            get
            {
                if (containerControl.ActiveControl is TextBox)
                {
                    return (TextBox)containerControl.ActiveControl;
                }
                return null;
            }
        }

        public bool MenuActivateDelegate()
        {
            TextBox activeTextBox = ActiveTextBox;
            if (activeTextBox != null)
            {
                if ((undoToolStripMenuItem != null))
                {
                    undoToolStripMenuItem.Enabled = activeTextBox.CanUndo;
                }
                if ((cutToolStripMenuItem != null))
                {
                    cutToolStripMenuItem.Enabled = activeTextBox.SelectionLength != 0;
                }
                if ((copyToolStripMenuItem != null))
                {
                    copyToolStripMenuItem.Enabled = activeTextBox.SelectionLength != 0;
                }
                if ((pasteToolStripMenuItem != null))
                {
                    pasteToolStripMenuItem.Enabled = Clipboard.ContainsText();
                }
                if ((clearToolStripMenuItem != null))
                {
                    clearToolStripMenuItem.Enabled = activeTextBox.SelectionLength != 0;
                }
                if ((selectAllToolStripMenuItem != null))
                {
                    selectAllToolStripMenuItem.Enabled = true;
                }
                return true;
            }
            return false;
        }

        public bool ProcessCmdKeyDelegate(ref Message msg, Keys keyData)
        {
            bool result = false;

            if ((keyData & Keys.Control) != 0)
            {
                MenuActivateDelegate();
            }

            return result;
        }

        public bool ProcessMenuItemDelegate(ToolStripMenuItem menuItem)
        {
            if ((ActiveTextBox == null) || !ActiveTextBox.Focused)
            {
                return false;
            }

            if (menuItem == undoToolStripMenuItem)
            {
                undoToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == cutToolStripMenuItem)
            {
                cutToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == copyToolStripMenuItem)
            {
                copyToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == pasteToolStripMenuItem)
            {
                pasteToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == clearToolStripMenuItem)
            {
                clearToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == selectAllToolStripMenuItem)
            {
                selectAllToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveTextBox != null)
            {
                ActiveTextBox.Undo();
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveTextBox != null)
            {
                ActiveTextBox.Cut();
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveTextBox != null)
            {
                ActiveTextBox.Copy();
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveTextBox != null)
            {
                ActiveTextBox.Paste();
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveTextBox != null)
            {
                ActiveTextBox.Clear();
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveTextBox != null)
            {
                ActiveTextBox.SelectAll();
            }
        }
    }
}
