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
    partial class ImportRawSampleDialog
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
            this.label2 = new OutOfPhase.MyLabel();
            this.label3 = new OutOfPhase.MyLabel();
            this.label4 = new OutOfPhase.MyLabel();
            this.label5 = new OutOfPhase.MyLabel();
            this.label6 = new OutOfPhase.MyLabel();
            this.textBoxInitialSkip = new System.Windows.Forms.TextBox();
            this.importRawSettingsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.textBoxFramePadding = new System.Windows.Forms.TextBox();
            this.comboBoxBits = new System.Windows.Forms.ComboBox();
            this.comboBoxChannels = new System.Windows.Forms.ComboBox();
            this.comboBoxSignMode = new System.Windows.Forms.ComboBox();
            this.comboBoxEndianness = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label7 = new OutOfPhase.MyLabel();
            this.buttonImport = new System.Windows.Forms.Button();
            this.label8 = new OutOfPhase.MyLabel();
            this.labelFileName = new OutOfPhase.MyLabel();
            this.sampleView = new OutOfPhase.SampleControl();
            this.label9 = new OutOfPhase.MyLabel();
            this.dpiChangeHelper = new OutOfPhase.DpiChangeHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.importRawSettingsBindingSource)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 8;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label3, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.label4, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.label5, 6, 1);
            this.tableLayoutPanel1.Controls.Add(this.label6, 6, 2);
            this.tableLayoutPanel1.Controls.Add(this.textBoxInitialSkip, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxFramePadding, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxBits, 4, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxChannels, 4, 2);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxSignMode, 7, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxEndianness, 7, 2);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 4, 3);
            this.tableLayoutPanel1.Controls.Add(this.label8, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelFileName, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.sampleView, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.label9, 0, 4);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 6;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(658, 358);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Initial Skip:";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Extra Frame Padding:";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(243, 30);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Bits/Sample:";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(243, 57);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Channels:";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(463, 30);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Encoding:";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(463, 57);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "Endianness:";
            // 
            // textBoxInitialSkip
            // 
            this.textBoxInitialSkip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInitialSkip.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.importRawSettingsBindingSource, "InitialSkip", true));
            this.textBoxInitialSkip.Location = new System.Drawing.Point(117, 26);
            this.textBoxInitialSkip.Name = "textBoxInitialSkip";
            this.textBoxInitialSkip.Size = new System.Drawing.Size(100, 20);
            this.textBoxInitialSkip.TabIndex = 6;
            // 
            // importRawSettingsBindingSource
            // 
            this.importRawSettingsBindingSource.DataSource = typeof(OutOfPhase.Import.ImportRawSettings);
            // 
            // textBoxFramePadding
            // 
            this.textBoxFramePadding.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFramePadding.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.importRawSettingsBindingSource, "FramePadding", true));
            this.textBoxFramePadding.Location = new System.Drawing.Point(117, 53);
            this.textBoxFramePadding.Name = "textBoxFramePadding";
            this.textBoxFramePadding.Size = new System.Drawing.Size(100, 20);
            this.textBoxFramePadding.TabIndex = 7;
            // 
            // comboBoxBits
            // 
            this.comboBoxBits.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxBits.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.importRawSettingsBindingSource, "NumBitsAsString", true));
            this.comboBoxBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBits.FormattingEnabled = true;
            this.comboBoxBits.Location = new System.Drawing.Point(316, 26);
            this.comboBoxBits.Name = "comboBoxBits";
            this.comboBoxBits.Size = new System.Drawing.Size(121, 21);
            this.comboBoxBits.TabIndex = 8;
            // 
            // comboBoxChannels
            // 
            this.comboBoxChannels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxChannels.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.importRawSettingsBindingSource, "NumChannelsAsString", true));
            this.comboBoxChannels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxChannels.FormattingEnabled = true;
            this.comboBoxChannels.Location = new System.Drawing.Point(316, 53);
            this.comboBoxChannels.Name = "comboBoxChannels";
            this.comboBoxChannels.Size = new System.Drawing.Size(121, 21);
            this.comboBoxChannels.TabIndex = 9;
            // 
            // comboBoxSignMode
            // 
            this.comboBoxSignMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxSignMode.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.importRawSettingsBindingSource, "SignModeAsString", true));
            this.comboBoxSignMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSignMode.FormattingEnabled = true;
            this.comboBoxSignMode.Location = new System.Drawing.Point(534, 26);
            this.comboBoxSignMode.Name = "comboBoxSignMode";
            this.comboBoxSignMode.Size = new System.Drawing.Size(121, 21);
            this.comboBoxSignMode.TabIndex = 10;
            // 
            // comboBoxEndianness
            // 
            this.comboBoxEndianness.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxEndianness.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.importRawSettingsBindingSource, "EndiannessAsString", true));
            this.comboBoxEndianness.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxEndianness.FormattingEnabled = true;
            this.comboBoxEndianness.Location = new System.Drawing.Point(534, 53);
            this.comboBoxEndianness.Name = "comboBoxEndianness";
            this.comboBoxEndianness.Size = new System.Drawing.Size(121, 21);
            this.comboBoxEndianness.TabIndex = 11;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.flowLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 4);
            this.flowLayoutPanel1.Controls.Add(this.buttonCancel);
            this.flowLayoutPanel1.Controls.Add(this.label7);
            this.flowLayoutPanel1.Controls.Add(this.buttonImport);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(346, 80);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(278, 29);
            this.flowLayoutPanel1.TabIndex = 12;
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(3, 3);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(100, 23);
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(109, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(60, 23);
            this.label7.TabIndex = 1;
            // 
            // buttonImport
            // 
            this.buttonImport.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonImport.Location = new System.Drawing.Point(175, 3);
            this.buttonImport.Name = "buttonImport";
            this.buttonImport.Size = new System.Drawing.Size(100, 23);
            this.buttonImport.TabIndex = 2;
            this.buttonImport.Text = "Import";
            this.buttonImport.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 5);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(108, 13);
            this.label8.TabIndex = 13;
            this.label8.Text = "File:";
            // 
            // labelFileName
            // 
            this.labelFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFileName.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.labelFileName, 7);
            this.labelFileName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.importRawSettingsBindingSource, "Name", true));
            this.labelFileName.Location = new System.Drawing.Point(117, 5);
            this.labelFileName.Name = "labelFileName";
            this.labelFileName.Size = new System.Drawing.Size(538, 13);
            this.labelFileName.TabIndex = 14;
            this.labelFileName.Text = "label9";
            // 
            // sampleView
            // 
            this.sampleView.BackColor = System.Drawing.SystemColors.Window;
            this.tableLayoutPanel1.SetColumnSpan(this.sampleView, 8);
            this.sampleView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sampleView.Location = new System.Drawing.Point(3, 135);
            this.sampleView.LoopEndLabel = null;
            this.sampleView.LoopStartLabel = null;
            this.sampleView.Name = "sampleView";
            this.sampleView.Size = new System.Drawing.Size(652, 220);
            this.sampleView.TabIndex = 15;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 115);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(108, 13);
            this.label9.TabIndex = 16;
            this.label9.Text = "Preview:";
            // 
            // dpiChangeHelper
            // 
            this.dpiChangeHelper.Form = this;
            // 
            // ImportRawSampleDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(658, 358);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ImportRawSampleDialog";
            this.Text = "Import Raw Sample";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.importRawSettingsBindingSource)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private MyLabel label1;
        private MyLabel label2;
        private MyLabel label3;
        private MyLabel label4;
        private MyLabel label5;
        private MyLabel label6;
        private System.Windows.Forms.TextBox textBoxInitialSkip;
        private System.Windows.Forms.TextBox textBoxFramePadding;
        private System.Windows.Forms.ComboBox comboBoxBits;
        private System.Windows.Forms.ComboBox comboBoxChannels;
        private System.Windows.Forms.ComboBox comboBoxSignMode;
        private System.Windows.Forms.ComboBox comboBoxEndianness;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button buttonCancel;
        private MyLabel label7;
        private System.Windows.Forms.Button buttonImport;
        private MyLabel label8;
        private MyLabel labelFileName;
        private System.Windows.Forms.BindingSource importRawSettingsBindingSource;
        private SampleControl sampleView;
        private MyLabel label9;
        private DpiChangeHelper dpiChangeHelper;
    }
}