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
    partial class SectionWindow
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxSectionName = new System.Windows.Forms.TextBox();
            this.sectionObjectRecBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.textBoxSectionText = new TextEditor.TextEditControl();
            this.documentBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.stringStorageFactory = new TextEditor.StringStorageFactory();
            this.menuStripManager = new OutOfPhase.MenuStripManager();
            this.textEditorWindowHelper = new TextEditor.TextEditorWindowHelper(this.components);
            this.textBoxWindowHelper = new OutOfPhase.TextBoxWindowHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sectionObjectRecBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxSectionName, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxSectionText, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.menuStripManager, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(627, 364);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Section Name:";
            // 
            // textBoxSectionName
            // 
            this.textBoxSectionName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSectionName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sectionObjectRecBindingSource, "Name", true));
            this.textBoxSectionName.Location = new System.Drawing.Point(106, 33);
            this.textBoxSectionName.Name = "textBoxSectionName";
            this.textBoxSectionName.Size = new System.Drawing.Size(518, 20);
            this.textBoxSectionName.TabIndex = 3;
            // 
            // sectionObjectRecBindingSource
            // 
            this.sectionObjectRecBindingSource.DataSource = typeof(OutOfPhase.SectionObjectRec);
            // 
            // textBoxSectionText
            // 
            this.textBoxSectionText.AutoScroll = true;
            this.textBoxSectionText.AutoScrollMinSize = new System.Drawing.Size(621, 13);
            this.textBoxSectionText.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBoxSectionText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel1.SetColumnSpan(this.textBoxSectionText, 3);
            this.textBoxSectionText.DataBindings.Add(new System.Windows.Forms.Binding("TabSize", this.documentBindingSource, "TabSize", true));
            this.textBoxSectionText.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sectionObjectRecBindingSource, "Source", true));
            this.textBoxSectionText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxSectionText.Location = new System.Drawing.Point(3, 59);
            this.textBoxSectionText.Name = "textBoxSectionText";
            this.textBoxSectionText.Size = new System.Drawing.Size(621, 302);
            this.textBoxSectionText.TabIndex = 4;
            this.textBoxSectionText.TextService = TextEditor.TextService.Simple;
            this.textBoxSectionText.TextStorageFactory = this.stringStorageFactory;
            // 
            // documentBindingSource
            // 
            this.documentBindingSource.DataSource = typeof(OutOfPhase.Document);
            // 
            // menuStripManager
            // 
            this.menuStripManager.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.menuStripManager, 3);
            this.menuStripManager.Location = new System.Drawing.Point(3, 3);
            this.menuStripManager.Name = "menuStripManager";
            this.menuStripManager.Size = new System.Drawing.Size(621, 24);
            this.menuStripManager.TabIndex = 5;
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
            this.textEditorWindowHelper.TextEditControl = this.textBoxSectionText;
            this.textEditorWindowHelper.TrimTrailingSpacesToolStripMenuItem = null;
            this.textEditorWindowHelper.UndoToolStripMenuItem = null;
            // 
            // textBoxWindowHelper
            // 
            this.textBoxWindowHelper.ClearToolStripMenuItem = null;
            this.textBoxWindowHelper.ContainerControl = this;
            this.textBoxWindowHelper.CopyToolStripMenuItem = null;
            this.textBoxWindowHelper.CutToolStripMenuItem = null;
            this.textBoxWindowHelper.PasteToolStripMenuItem = null;
            this.textBoxWindowHelper.SelectAllToolStripMenuItem = null;
            this.textBoxWindowHelper.UndoToolStripMenuItem = null;
            // 
            // SectionWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(627, 364);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "SectionWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sectionObjectRecBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.BindingSource sectionObjectRecBindingSource;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxSectionName;
        private TextEditor.TextEditControl textBoxSectionText;
        private System.Windows.Forms.BindingSource documentBindingSource;
        private MenuStripManager menuStripManager;
        private TextEditor.TextEditorWindowHelper textEditorWindowHelper;
        private TextBoxWindowHelper textBoxWindowHelper;
        private TextEditor.StringStorageFactory stringStorageFactory;
    }
}