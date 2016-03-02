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
    // On the old (pre-OSX) Macintosh, it was customary to handle menus by not updating them
    // as view state changes (as .NET protocols would prescribe), but just update all of them
    // in one pass right before the menu is shown or a shortcut key is pressed. It was faster
    // and less error-prone, back in the days when that sort of thing mattered.
    // There was also a distinction between menu item handling targeted to the active window
    // and "global" items (always available) - after all, the Macintosh was essentially an MDI
    // interface without the MDI container window.
    // This control tries to replicate those old schemes in .NET Forms.
    public partial class MenuStripManager : UserControl
    {
        private IMenuStripManagerHandler globalHandler;
        private IMenuStripManagerHandler activeHandler;

        private readonly Dictionary<ToolStripMenuItem, string> initialNames = new Dictionary<ToolStripMenuItem, string>();

        private ToolStripMenuItem[] initiallyInvisible = new ToolStripMenuItem[0];

        public MenuStripManager()
        {
            Dock = DockStyle.Top;

            InitializeComponent();

            menuStrip.MenuActivate += new EventHandler(delegate (object sender, EventArgs e) { EnableMenuItems(); });
            foreach (ToolStripMenuItem menuItem in menuStrip.Items)
            {
                AttachHandlerMenuItemsRecursive(menuItem);
                CollectInitialNamesRecursive(menuItem);
            }

            initiallyInvisible = new ToolStripMenuItem[]
            {
                exitToolStripMenuItem,
                editNotePropertiesToolStripMenuItem,
                editTrackPropertiesToolStripMenuItem,
                disassembleToolStripMenuItem,
                envelopeSegmentCalculatorToolStripMenuItem,
                openAsSampleToolStripMenuItem,
                openAsWaveTableToolStripMenuItem,
                exportAIFFSampleToolStripMenuItem,
                exportWAVSampleToolStripMenuItem,
                playTrackFromHereToolStripMenuItem,
                playAllFromHereToolStripMenuItem,
                playTrackFromHereToDiskToolStripMenuItem,
                playAllFromHereToDiskToolStripMenuItem,
                loop1ToolStripMenuItem,
                loop2ToolStripMenuItem,
                loop3ToolStripMenuItem,
                disassembleApplicationMethodToolStripMenuItem,
            };
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ToolStripMenuItem[] InitiallyInvisible
        {
            get
            {
                return initiallyInvisible;
            }
            set
            {
                if (value == null)
                {
                    value = new ToolStripMenuItem[0];
                }
                initiallyInvisible = value;
            }
        }

        public void HookUpTextEditorWindowHelper(TextEditorWindowHelper textEditorWindowHelper)
        {
            textEditorWindowHelper.UndoToolStripMenuItem = this.undoToolStripMenuItem;
            textEditorWindowHelper.RedoToolStripMenuItem = this.redoToolStripMenuItem;
            textEditorWindowHelper.CutToolStripMenuItem = this.cutToolStripMenuItem;
            textEditorWindowHelper.CopyToolStripMenuItem = this.copyToolStripMenuItem;
            textEditorWindowHelper.PasteToolStripMenuItem = this.pasteToolStripMenuItem;
            textEditorWindowHelper.ClearToolStripMenuItem = this.clearToolStripMenuItem;
            textEditorWindowHelper.SelectAllToolStripMenuItem = this.selectAllToolStripMenuItem;
            textEditorWindowHelper.ShiftLeftToolStripMenuItem = this.shiftLeftToolStripMenuItem;
            textEditorWindowHelper.ShiftRightToolStripMenuItem = this.shiftRightToolStripMenuItem;
            textEditorWindowHelper.BalanceToolStripMenuItem = this.balanceToolStripMenuItem;
            textEditorWindowHelper.FindToolStripMenuItem = this.findToolStripMenuItem;
            textEditorWindowHelper.FindAgainToolStripMenuItem = this.findNextToolStripMenuItem;
            textEditorWindowHelper.EnterSelectionToolStripMenuItem = this.enterSelectionToolStripMenuItem;
            textEditorWindowHelper.ReplaceAndFindAgainToolStripMenuItem = this.replaceAndFindNextToolStripMenuItem;
            textEditorWindowHelper.GoToLineToolStripMenuItem = this.goToLineToolStripMenuItem;
        }

        public void HookUpTextBoxWindowHelper(TextBoxWindowHelper textBoxWindowHelper)
        {
            textBoxWindowHelper.UndoToolStripMenuItem = this.undoToolStripMenuItem;
            textBoxWindowHelper.CutToolStripMenuItem = this.cutToolStripMenuItem;
            textBoxWindowHelper.CopyToolStripMenuItem = this.copyToolStripMenuItem;
            textBoxWindowHelper.PasteToolStripMenuItem = this.pasteToolStripMenuItem;
            textBoxWindowHelper.ClearToolStripMenuItem = this.clearToolStripMenuItem;
            textBoxWindowHelper.SelectAllToolStripMenuItem = this.selectAllToolStripMenuItem;
        }

        private void CollectInitialNamesRecursive(ToolStripMenuItem root)
        {
            if (root.DropDownItems.Count == 0)
            {
                initialNames[root] = root.Text;
            }
            else
            {
                foreach (object obj in root.DropDownItems)
                {
                    ToolStripMenuItem child = obj as ToolStripMenuItem;
                    if (child != null)
                    {
                        CollectInitialNamesRecursive(child);
                    }
                }
            }
        }

        private void EnableMenuItems()
        {
            foreach (ToolStripMenuItem menuItem in initiallyInvisible)
            {
                menuItem.Available = false;
            }
            foreach (ToolStripMenuItem menuItem in menuStrip.Items)
            {
                DisableAndResetMenuItemsRecursive(menuItem);
            }

            if (activeHandler != null)
            {
                activeHandler.EnableMenuItems(this);
            }
            if (globalHandler != null)
            {
                globalHandler.EnableMenuItems(this);
            }

            foreach (ToolStripMenuItem menuItem in menuStrip.Items)
            {
                menuItem.Enabled = true; // always enable top-level menus - less confusing ux
                PropagateEnableMenuItemsRecursive(menuItem);
                HideRedundantSeparatorsRecursive(menuItem);
            }
        }

        private void AttachHandlerMenuItemsRecursive(ToolStripMenuItem root)
        {
            if (root.DropDownItems.Count == 0)
            {
                root.Click += new EventHandler(menuItem_Click);
            }
            else
            {
                foreach (object obj in root.DropDownItems)
                {
                    ToolStripMenuItem child = obj as ToolStripMenuItem;
                    if (child != null)
                    {
                        AttachHandlerMenuItemsRecursive(child);
                    }
                }
            }
        }

        private void menuItem_Click(object sender, EventArgs e)
        {
            if (activeHandler != null)
            {
                if (activeHandler.ExecuteMenuItem(this, (ToolStripMenuItem)sender))
                {
                    return;
                }
            }
            if (globalHandler != null)
            {
                if (globalHandler.ExecuteMenuItem(this, (ToolStripMenuItem)sender))
                {
                    return;
                }
            }
        }

        private void DisableAndResetMenuItemsRecursive(ToolStripMenuItem root)
        {
            foreach (object obj in root.DropDownItems)
            {
                ToolStripMenuItem child = obj as ToolStripMenuItem;
                if (child != null)
                {
                    DisableAndResetMenuItemsRecursive(child);
                    child.Enabled = false;
                }
            }
            root.Enabled = false;

            string text;
            if (initialNames.TryGetValue(root, out text))
            {
                root.Text = text;
            }
        }

        private static bool PropagateEnableMenuItemsRecursive(ToolStripMenuItem root)
        {
            if (root.DropDownItems.Count == 0)
            {
                return root.Enabled;
            }
            else
            {
                foreach (object obj in root.DropDownItems)
                {
                    ToolStripMenuItem child = obj as ToolStripMenuItem;
                    if (child != null)
                    {
                        if (PropagateEnableMenuItemsRecursive(child))
                        {
                            root.Enabled = true;
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private static void HideRedundantSeparatorsRecursive(ToolStripMenuItem root)
        {
            ToolStripSeparator lastSeparator = null;
            for (int i = 0; i < root.DropDownItems.Count; i++)
            {
                ToolStripItem item = root.DropDownItems[i];
                if (item is ToolStripSeparator)
                {
                    if (lastSeparator != null)
                    {
                        item.Available = false;
                    }
                    else
                    {
                        lastSeparator = (ToolStripSeparator)item;
                    }
                }
                else if (item is ToolStripMenuItem)
                {
                    if (item.Available)
                    {
                        lastSeparator = null;
                        HideRedundantSeparatorsRecursive((ToolStripMenuItem)item);
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            if (lastSeparator != null)
            {
                lastSeparator.Available = false;
            }
        }


        // 

        public void ProcessCmdKeyDelegate(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.Control) != 0)
            {
                EnableMenuItems();
            }
        }


        //

        public MenuStrip ContainedMenuStrip { get { return menuStrip; } }

        public void SetGlobalHandler(IMenuStripManagerHandler globalHandler)
        {
            if (this.globalHandler != null)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            this.globalHandler = globalHandler;
        }

        public void SetActiveHandler(IMenuStripManagerHandler activeHandler)
        {
            if (activeHandler == null)
            {
                this.activeHandler = null;
            }
            else
            {
                if (this.activeHandler != null)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                this.activeHandler = activeHandler;
            }
        }

        [DefaultValue(DockStyle.Top)]
        public override DockStyle Dock
        {
            get
            {
                return base.Dock;
            }
            set
            {
                base.Dock = value;
            }
        }
    }

    public interface IMenuStripManagerHandler
    {
        void EnableMenuItems(MenuStripManager menuStrip);
        bool ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem);
    }
}
