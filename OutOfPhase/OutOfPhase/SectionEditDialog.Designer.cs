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
    partial class SectionEditDialog
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
            this.myListBoxSections = new OutOfPhase.MyListBox();
            this.label1 = new OutOfPhase.MyLabel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonChangeSection = new System.Windows.Forms.Button();
            this.buttonNew = new System.Windows.Forms.Button();
            this.buttonDeleteSection = new System.Windows.Forms.Button();
            this.buttonEditSection = new System.Windows.Forms.Button();
            this.buttonClose = new System.Windows.Forms.Button();
            this.dpiChangeHelper = new OutOfPhase.DpiChangeHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.myListBoxSections, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonClose, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(473, 333);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // myListBoxSections
            // 
            this.myListBoxSections.BackColor = System.Drawing.SystemColors.Window;
            this.myListBoxSections.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myListBoxSections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myListBoxSections.Location = new System.Drawing.Point(3, 26);
            this.myListBoxSections.Name = "myListBoxSections";
            this.tableLayoutPanel1.SetRowSpan(this.myListBoxSections, 2);
            this.myListBoxSections.Size = new System.Drawing.Size(355, 304);
            this.myListBoxSections.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(355, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Sections:";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.buttonChangeSection);
            this.flowLayoutPanel1.Controls.Add(this.buttonNew);
            this.flowLayoutPanel1.Controls.Add(this.buttonDeleteSection);
            this.flowLayoutPanel1.Controls.Add(this.buttonEditSection);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(364, 26);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(106, 116);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // buttonChangeSection
            // 
            this.buttonChangeSection.Location = new System.Drawing.Point(3, 3);
            this.buttonChangeSection.Name = "buttonChangeSection";
            this.buttonChangeSection.Size = new System.Drawing.Size(100, 23);
            this.buttonChangeSection.TabIndex = 0;
            this.buttonChangeSection.Text = "Set Section";
            this.buttonChangeSection.UseVisualStyleBackColor = true;
            this.buttonChangeSection.Click += new System.EventHandler(this.buttonChangeSection_Click);
            // 
            // buttonNew
            // 
            this.buttonNew.Location = new System.Drawing.Point(3, 32);
            this.buttonNew.Name = "buttonNew";
            this.buttonNew.Size = new System.Drawing.Size(100, 23);
            this.buttonNew.TabIndex = 1;
            this.buttonNew.Text = "New Section";
            this.buttonNew.UseVisualStyleBackColor = true;
            this.buttonNew.Click += new System.EventHandler(this.buttonNew_Click);
            // 
            // buttonDeleteSection
            // 
            this.buttonDeleteSection.Location = new System.Drawing.Point(3, 61);
            this.buttonDeleteSection.Name = "buttonDeleteSection";
            this.buttonDeleteSection.Size = new System.Drawing.Size(100, 23);
            this.buttonDeleteSection.TabIndex = 2;
            this.buttonDeleteSection.Text = "Delete Section";
            this.buttonDeleteSection.UseVisualStyleBackColor = true;
            this.buttonDeleteSection.Click += new System.EventHandler(this.buttonDeleteSection_Click);
            // 
            // buttonEditSection
            // 
            this.buttonEditSection.Location = new System.Drawing.Point(3, 90);
            this.buttonEditSection.Name = "buttonEditSection";
            this.buttonEditSection.Size = new System.Drawing.Size(100, 23);
            this.buttonEditSection.TabIndex = 3;
            this.buttonEditSection.Text = "Edit Section";
            this.buttonEditSection.UseVisualStyleBackColor = true;
            this.buttonEditSection.Click += new System.EventHandler(this.buttonEditSection_Click);
            // 
            // buttonClose
            // 
            this.buttonClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonClose.Location = new System.Drawing.Point(367, 307);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(100, 23);
            this.buttonClose.TabIndex = 4;
            this.buttonClose.Text = "Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // dpiChangeHelper
            // 
            this.dpiChangeHelper.Form = this;
            // 
            // SectionEditDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(473, 333);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SectionEditDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Section Assignments";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private MyListBox myListBoxSections;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button buttonChangeSection;
        private System.Windows.Forms.Button buttonNew;
        private System.Windows.Forms.Button buttonDeleteSection;
        private System.Windows.Forms.Button buttonEditSection;
        private System.Windows.Forms.Button buttonClose;
        private MyLabel label1;
        private DpiChangeHelper dpiChangeHelper;
    }
}