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
namespace OutOfPhase
{
    partial class CalculatorWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.menuStripManager = new OutOfPhase.MenuStripManager();
            this.panel1 = new System.Windows.Forms.Panel();
            this.textBox = new TextEditor.TextEditControl();
            this.documentBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.stringStorageFactory = new TextEditor.StringStorageFactory();
            this.textEditorWindowHelper = new TextEditor.TextEditorWindowHelper(this.components);
            this.dpiChangeHelper = new OutOfPhase.DpiChangeHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.menuStripManager, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(600, 401);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // menuStripManager
            // 
            this.menuStripManager.AutoSize = true;
            this.menuStripManager.Location = new System.Drawing.Point(0, 0);
            this.menuStripManager.Margin = new System.Windows.Forms.Padding(0);
            this.menuStripManager.Name = "menuStripManager";
            this.menuStripManager.Size = new System.Drawing.Size(600, 24);
            this.menuStripManager.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.textBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 27);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(594, 371);
            this.panel1.TabIndex = 4;
            // 
            // textBox
            // 
            this.textBox.AutoScroll = true;
            this.textBox.AutoScrollMinSize = new System.Drawing.Size(594, 13);
            this.textBox.BackColor = System.Drawing.SystemColors.Window;
            this.textBox.DataBindings.Add(new System.Windows.Forms.Binding("TabSize", this.documentBindingSource, "TabSize", true));
            this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox.Location = new System.Drawing.Point(0, 0);
            this.textBox.Name = "textBox";
            this.textBox.SimpleNavigation = true;
            this.textBox.Size = new System.Drawing.Size(592, 369);
            this.textBox.TabIndex = 2;
            this.textBox.TextService = TextEditor.TextService.Simple;
            this.textBox.TextStorageFactory = this.stringStorageFactory;
            // 
            // documentBindingSource
            // 
            this.documentBindingSource.DataSource = typeof(OutOfPhase.Document);
            // 
            // textEditorWindowHelper
            // 
            this.textEditorWindowHelper.BalanceToolStripMenuItem = null;
            this.textEditorWindowHelper.ClearToolStripMenuItem = null;
            this.textEditorWindowHelper.ConvertTabsToSpacesToolStripMenuItem = null;
            this.textEditorWindowHelper.CopyToolStripMenuItem = null;
            this.textEditorWindowHelper.CutToolStripMenuItem = null;
            this.textEditorWindowHelper.EnterSelectionToolStripMenuItem = null;
            this.textEditorWindowHelper.FindAgainToolStripMenuItem = null;
            this.textEditorWindowHelper.FindToolStripMenuItem = null;
            this.textEditorWindowHelper.GoToLineToolStripMenuItem = null;
            this.textEditorWindowHelper.PasteToolStripMenuItem = null;
            this.textEditorWindowHelper.RedoToolStripMenuItem = null;
            this.textEditorWindowHelper.ReplaceAndFindAgainToolStripMenuItem = null;
            this.textEditorWindowHelper.SelectAllToolStripMenuItem = null;
            this.textEditorWindowHelper.ShiftLeftToolStripMenuItem = null;
            this.textEditorWindowHelper.ShiftRightToolStripMenuItem = null;
            this.textEditorWindowHelper.TextEditControl = this.textBox;
            this.textEditorWindowHelper.TrimTrailingSpacesToolStripMenuItem = null;
            this.textEditorWindowHelper.UndoToolStripMenuItem = null;
            // 
            // dpiChangeHelper
            // 
            this.dpiChangeHelper.Form = this;
            // 
            // CalculatorWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 401);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "CalculatorWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private TextEditor.TextEditControl textBox;
        private System.Windows.Forms.BindingSource documentBindingSource;
        private TextEditor.TextEditorWindowHelper textEditorWindowHelper;
        private MenuStripManager menuStripManager;
        private TextEditor.StringStorageFactory stringStorageFactory;
        private System.Windows.Forms.Panel panel1;
        private DpiChangeHelper dpiChangeHelper;
    }
}