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
    partial class AlgoWaveTableWindow
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
            this.label4 = new OutOfPhase.MyLabel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new OutOfPhase.MyLabel();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.algoWaveTableObjectRecBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.label2 = new OutOfPhase.MyLabel();
            this.textBoxNumTables = new System.Windows.Forms.TextBox();
            this.label3 = new OutOfPhase.MyLabel();
            this.comboBoxNumFrames = new System.Windows.Forms.ComboBox();
            this.menuStripManager = new OutOfPhase.MenuStripManager();
            this.panel1 = new System.Windows.Forms.Panel();
            this.textBoxFunction = new TextEditor.TextEditControl();
            this.documentBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.stringStorageFactory = new TextEditor.StringStorageFactory();
            this.textEditorWindowHelper = new TextEditor.TextEditorWindowHelper(this.components);
            this.textBoxWindowHelper = new OutOfPhase.TextBoxWindowHelper(this.components);
            this.dpiChangeHelper = new OutOfPhase.DpiChangeHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.algoWaveTableObjectRecBindingSource)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.menuStripManager, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(575, 382);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 57);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(569, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Waveform Generating Function:";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.label1);
            this.flowLayoutPanel1.Controls.Add(this.textBoxName);
            this.flowLayoutPanel1.Controls.Add(this.label2);
            this.flowLayoutPanel1.Controls.Add(this.textBoxNumTables);
            this.flowLayoutPanel1.Controls.Add(this.label3);
            this.flowLayoutPanel1.Controls.Add(this.comboBoxNumFrames);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 27);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(569, 27);
            this.flowLayoutPanel1.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxName
            // 
            this.textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoWaveTableObjectRecBindingSource, "Name", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxName.Location = new System.Drawing.Point(47, 3);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(160, 20);
            this.textBoxName.TabIndex = 3;
            // 
            // algoWaveTableObjectRecBindingSource
            // 
            this.algoWaveTableObjectRecBindingSource.DataSource = typeof(OutOfPhase.AlgoWaveTableObjectRec);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(213, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Num Tables:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxNumTables
            // 
            this.textBoxNumTables.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxNumTables.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoWaveTableObjectRecBindingSource, "NumTables", true));
            this.textBoxNumTables.Location = new System.Drawing.Point(286, 3);
            this.textBoxNumTables.Name = "textBoxNumTables";
            this.textBoxNumTables.Size = new System.Drawing.Size(50, 20);
            this.textBoxNumTables.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(342, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Num Frames:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // comboBoxNumFrames
            // 
            this.comboBoxNumFrames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxNumFrames.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoWaveTableObjectRecBindingSource, "NumFrames", true));
            this.comboBoxNumFrames.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNumFrames.FormattingEnabled = true;
            this.comboBoxNumFrames.Location = new System.Drawing.Point(417, 3);
            this.comboBoxNumFrames.Name = "comboBoxNumFrames";
            this.comboBoxNumFrames.Size = new System.Drawing.Size(70, 21);
            this.comboBoxNumFrames.TabIndex = 5;
            // 
            // menuStripManager
            // 
            this.menuStripManager.AutoSize = true;
            this.menuStripManager.Location = new System.Drawing.Point(0, 0);
            this.menuStripManager.Margin = new System.Windows.Forms.Padding(0);
            this.menuStripManager.Name = "menuStripManager";
            this.menuStripManager.Size = new System.Drawing.Size(575, 24);
            this.menuStripManager.TabIndex = 5;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.textBoxFunction);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 73);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(569, 306);
            this.panel1.TabIndex = 7;
            // 
            // textBoxFunction
            // 
            this.textBoxFunction.AutoScroll = true;
            this.textBoxFunction.AutoScrollMinSize = new System.Drawing.Size(569, 13);
            this.textBoxFunction.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxFunction.DataBindings.Add(new System.Windows.Forms.Binding("TabSize", this.documentBindingSource, "TabSize", true));
            this.textBoxFunction.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoWaveTableObjectRecBindingSource, "AlgoWaveTableFormula", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxFunction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxFunction.Location = new System.Drawing.Point(0, 0);
            this.textBoxFunction.Name = "textBoxFunction";
            this.textBoxFunction.Size = new System.Drawing.Size(567, 304);
            this.textBoxFunction.TabIndex = 6;
            this.textBoxFunction.TextService = TextEditor.TextService.Simple;
            this.textBoxFunction.SimpleNavigation = true;
            this.textBoxFunction.TextStorageFactory = this.stringStorageFactory;
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
            this.textEditorWindowHelper.TextEditControl = this.textBoxFunction;
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
            // dpiChangeHelper
            // 
            this.dpiChangeHelper.Form = this;
            // 
            // AlgoWaveTableWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(575, 382);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "AlgoWaveTableWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.algoWaveTableObjectRecBindingSource)).EndInit();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private MyLabel label4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private MyLabel label1;
        private System.Windows.Forms.TextBox textBoxName;
        private MyLabel label2;
        private System.Windows.Forms.TextBox textBoxNumTables;
        private MyLabel label3;
        private System.Windows.Forms.ComboBox comboBoxNumFrames;
        private System.Windows.Forms.BindingSource algoWaveTableObjectRecBindingSource;
        private MenuStripManager menuStripManager;
        private TextEditor.TextEditControl textBoxFunction;
        private System.Windows.Forms.BindingSource documentBindingSource;
        private TextEditor.TextEditorWindowHelper textEditorWindowHelper;
        private TextBoxWindowHelper textBoxWindowHelper;
        private TextEditor.StringStorageFactory stringStorageFactory;
        private System.Windows.Forms.Panel panel1;
        private DpiChangeHelper dpiChangeHelper;
    }
}