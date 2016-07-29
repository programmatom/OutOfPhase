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
    partial class PlayAudioFileWithEffects
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
            this.label1 = new OutOfPhase.MyLabel();
            this.textBoxAudioFilePath = new System.Windows.Forms.TextBox();
            this.buttonSelectAudioFile = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.label2 = new OutOfPhase.MyLabel();
            this.buttonClose = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.textBoxEffectBody = new TextEditor.TextEditControl();
            this.textEditorWindowHelper1 = new TextEditor.TextEditorWindowHelper(this.components);
            this.dpiChangeHelper = new OutOfPhase.DpiChangeHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.textBoxAudioFilePath, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonSelectAudioFile, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(649, 348);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Audio File To Play:";
            // 
            // textBoxAudioFilePath
            // 
            this.textBoxAudioFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxAudioFilePath.Location = new System.Drawing.Point(119, 4);
            this.textBoxAudioFilePath.Name = "textBoxAudioFilePath";
            this.textBoxAudioFilePath.Size = new System.Drawing.Size(474, 20);
            this.textBoxAudioFilePath.TabIndex = 1;
            // 
            // buttonSelectAudioFile
            // 
            this.buttonSelectAudioFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectAudioFile.AutoSize = true;
            this.buttonSelectAudioFile.Location = new System.Drawing.Point(599, 3);
            this.buttonSelectAudioFile.Name = "buttonSelectAudioFile";
            this.buttonSelectAudioFile.Size = new System.Drawing.Size(47, 23);
            this.buttonSelectAudioFile.TabIndex = 2;
            this.buttonSelectAudioFile.Text = "Select";
            this.buttonSelectAudioFile.UseVisualStyleBackColor = true;
            this.buttonSelectAudioFile.Click += new System.EventHandler(this.buttonSelectAudioFile_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.flowLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 4);
            this.flowLayoutPanel1.Controls.Add(this.buttonPlay);
            this.flowLayoutPanel1.Controls.Add(this.label2);
            this.flowLayoutPanel1.Controls.Add(this.buttonClose);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(368, 316);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(278, 29);
            this.flowLayoutPanel1.TabIndex = 7;
            // 
            // buttonPlay
            // 
            this.buttonPlay.Location = new System.Drawing.Point(3, 3);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(125, 23);
            this.buttonPlay.TabIndex = 0;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(134, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(10, 23);
            this.label2.TabIndex = 3;
            // 
            // buttonClose
            // 
            this.buttonClose.Location = new System.Drawing.Point(150, 3);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(125, 23);
            this.buttonClose.TabIndex = 2;
            this.buttonClose.Text = "Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel1.SetColumnSpan(this.panel1, 4);
            this.panel1.Controls.Add(this.textBoxEffectBody);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 32);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(643, 278);
            this.panel1.TabIndex = 8;
            // 
            // textBoxEffectBody
            // 
            this.textBoxEffectBody.AutoScroll = true;
            this.textBoxEffectBody.AutoScrollMinSize = new System.Drawing.Size(641, 13);
            this.textBoxEffectBody.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxEffectBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxEffectBody.Location = new System.Drawing.Point(0, 0);
            this.textBoxEffectBody.Name = "textBoxEffectBody";
            this.textBoxEffectBody.SimpleNavigation = true;
            this.textBoxEffectBody.Size = new System.Drawing.Size(641, 276);
            this.textBoxEffectBody.TabIndex = 6;
            this.textBoxEffectBody.TextService = TextEditor.TextService.Simple;
            // 
            // textEditorWindowHelper1
            // 
            this.textEditorWindowHelper1.BalanceToolStripMenuItem = null;
            this.textEditorWindowHelper1.ClearToolStripMenuItem = null;
            this.textEditorWindowHelper1.ConvertTabsToSpacesToolStripMenuItem = null;
            this.textEditorWindowHelper1.CopyToolStripMenuItem = null;
            this.textEditorWindowHelper1.CutToolStripMenuItem = null;
            this.textEditorWindowHelper1.EnterSelectionToolStripMenuItem = null;
            this.textEditorWindowHelper1.FindAgainToolStripMenuItem = null;
            this.textEditorWindowHelper1.FindToolStripMenuItem = null;
            this.textEditorWindowHelper1.GoToLineToolStripMenuItem = null;
            this.textEditorWindowHelper1.PasteToolStripMenuItem = null;
            this.textEditorWindowHelper1.RedoToolStripMenuItem = null;
            this.textEditorWindowHelper1.ReplaceAndFindAgainToolStripMenuItem = null;
            this.textEditorWindowHelper1.SelectAllToolStripMenuItem = null;
            this.textEditorWindowHelper1.ShiftLeftToolStripMenuItem = null;
            this.textEditorWindowHelper1.ShiftRightToolStripMenuItem = null;
            this.textEditorWindowHelper1.TextEditControl = null;
            this.textEditorWindowHelper1.TrimTrailingSpacesToolStripMenuItem = null;
            this.textEditorWindowHelper1.UndoToolStripMenuItem = null;
            // 
            // dpiChangeHelper
            // 
            this.dpiChangeHelper.Form = this;
            // 
            // PlayAudioFileWithEffects
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(649, 348);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "PlayAudioFileWithEffects";
            this.Text = "Play Audio File With Effects";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private MyLabel label1;
        private System.Windows.Forms.TextBox textBoxAudioFilePath;
        private System.Windows.Forms.Button buttonSelectAudioFile;
        private TextEditor.TextEditorWindowHelper textEditorWindowHelper1;
        private TextEditor.TextEditControl textBoxEffectBody;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button buttonPlay;
        private MyLabel label2;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.Panel panel1;
        private DpiChangeHelper dpiChangeHelper;
    }
}