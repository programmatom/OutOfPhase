/*
 *  Copyright © 1994-2002, 2015-2017 Thomas R. Lawrence
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
    partial class PlayPrefsDialogNewSchool
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
            this.textBoxSamplingRate = new System.Windows.Forms.TextBox();
            this.newSchoolDocumentBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.textBoxEnvelopeRate = new System.Windows.Forms.TextBox();
            this.myLabel1 = new OutOfPhase.MyLabel();
            this.myLabel2 = new OutOfPhase.MyLabel();
            this.myLabel3 = new OutOfPhase.MyLabel();
            this.textBoxOversampling = new System.Windows.Forms.TextBox();
            this.textBoxScanningGap = new System.Windows.Forms.TextBox();
            this.myLabel4 = new OutOfPhase.MyLabel();
            this.myLabel5 = new OutOfPhase.MyLabel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.radioButton16Bit = new System.Windows.Forms.RadioButton();
            this.radioButton24Bit = new System.Windows.Forms.RadioButton();
            this.textBoxBufferDuration = new System.Windows.Forms.TextBox();
            this.myLabel6 = new OutOfPhase.MyLabel();
            this.myLabel7 = new OutOfPhase.MyLabel();
            this.textBoxRandomSeed = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonOK = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.newSchoolDocumentBindingSource)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 41.71429F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58.28571F));
            this.tableLayoutPanel1.Controls.Add(this.textBoxSamplingRate, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxEnvelopeRate, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.myLabel1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.myLabel2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.myLabel3, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.textBoxOversampling, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.textBoxScanningGap, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.myLabel4, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.myLabel5, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.textBoxBufferDuration, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.myLabel6, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.myLabel7, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.textBoxRandomSeed, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel2, 0, 9);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 10;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(350, 232);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // textBoxSamplingRate
            // 
            this.textBoxSamplingRate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxSamplingRate.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.newSchoolDocumentBindingSource, "SamplingRate", true));
            this.textBoxSamplingRate.Location = new System.Drawing.Point(149, 3);
            this.textBoxSamplingRate.Name = "textBoxSamplingRate";
            this.textBoxSamplingRate.Size = new System.Drawing.Size(150, 20);
            this.textBoxSamplingRate.TabIndex = 1;
            // 
            // newSchoolDocumentBindingSource
            // 
            this.newSchoolDocumentBindingSource.DataSource = typeof(OutOfPhase.NewSchoolDocument);
            // 
            // textBoxEnvelopeRate
            // 
            this.textBoxEnvelopeRate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxEnvelopeRate.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.newSchoolDocumentBindingSource, "EnvelopeUpdateRate", true));
            this.textBoxEnvelopeRate.Location = new System.Drawing.Point(149, 29);
            this.textBoxEnvelopeRate.Name = "textBoxEnvelopeRate";
            this.textBoxEnvelopeRate.Size = new System.Drawing.Size(150, 20);
            this.textBoxEnvelopeRate.TabIndex = 3;
            // 
            // myLabel1
            // 
            this.myLabel1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel1.AutoSize = true;
            this.myLabel1.Location = new System.Drawing.Point(3, 6);
            this.myLabel1.Name = "myLabel1";
            this.myLabel1.Size = new System.Drawing.Size(79, 13);
            this.myLabel1.TabIndex = 4;
            this.myLabel1.Text = "Sampling Rate:";
            // 
            // myLabel2
            // 
            this.myLabel2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel2.AutoSize = true;
            this.myLabel2.Location = new System.Drawing.Point(3, 32);
            this.myLabel2.Name = "myLabel2";
            this.myLabel2.Size = new System.Drawing.Size(81, 13);
            this.myLabel2.TabIndex = 5;
            this.myLabel2.Text = "Envelope Rate:";
            // 
            // myLabel3
            // 
            this.myLabel3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel3.AutoSize = true;
            this.myLabel3.Location = new System.Drawing.Point(3, 58);
            this.myLabel3.Name = "myLabel3";
            this.myLabel3.Size = new System.Drawing.Size(74, 13);
            this.myLabel3.TabIndex = 6;
            this.myLabel3.Text = "Oversampling:";
            // 
            // textBoxOversampling
            // 
            this.textBoxOversampling.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxOversampling.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.newSchoolDocumentBindingSource, "Oversampling", true));
            this.textBoxOversampling.Location = new System.Drawing.Point(149, 55);
            this.textBoxOversampling.Name = "textBoxOversampling";
            this.textBoxOversampling.Size = new System.Drawing.Size(100, 20);
            this.textBoxOversampling.TabIndex = 7;
            // 
            // textBoxScanningGap
            // 
            this.textBoxScanningGap.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxScanningGap.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.newSchoolDocumentBindingSource, "ScanningGap", true));
            this.textBoxScanningGap.Location = new System.Drawing.Point(149, 81);
            this.textBoxScanningGap.Name = "textBoxScanningGap";
            this.textBoxScanningGap.Size = new System.Drawing.Size(100, 20);
            this.textBoxScanningGap.TabIndex = 8;
            // 
            // myLabel4
            // 
            this.myLabel4.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel4.AutoSize = true;
            this.myLabel4.Location = new System.Drawing.Point(3, 84);
            this.myLabel4.Name = "myLabel4";
            this.myLabel4.Size = new System.Drawing.Size(78, 13);
            this.myLabel4.TabIndex = 9;
            this.myLabel4.Text = "Scanning Gap:";
            // 
            // myLabel5
            // 
            this.myLabel5.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel5.AutoSize = true;
            this.myLabel5.Location = new System.Drawing.Point(3, 112);
            this.myLabel5.Name = "myLabel5";
            this.myLabel5.Size = new System.Drawing.Size(54, 13);
            this.myLabel5.TabIndex = 10;
            this.myLabel5.Text = "Bit Depth:";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.radioButton16Bit);
            this.flowLayoutPanel1.Controls.Add(this.radioButton24Bit);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(149, 107);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(114, 23);
            this.flowLayoutPanel1.TabIndex = 11;
            // 
            // radioButton16Bit
            // 
            this.radioButton16Bit.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.radioButton16Bit.AutoSize = true;
            this.radioButton16Bit.Location = new System.Drawing.Point(3, 3);
            this.radioButton16Bit.Name = "radioButton16Bit";
            this.radioButton16Bit.Size = new System.Drawing.Size(51, 17);
            this.radioButton16Bit.TabIndex = 0;
            this.radioButton16Bit.TabStop = true;
            this.radioButton16Bit.Text = "16-bit";
            this.radioButton16Bit.UseVisualStyleBackColor = true;
            // 
            // radioButton24Bit
            // 
            this.radioButton24Bit.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.radioButton24Bit.AutoSize = true;
            this.radioButton24Bit.Location = new System.Drawing.Point(60, 3);
            this.radioButton24Bit.Name = "radioButton24Bit";
            this.radioButton24Bit.Size = new System.Drawing.Size(51, 17);
            this.radioButton24Bit.TabIndex = 1;
            this.radioButton24Bit.TabStop = true;
            this.radioButton24Bit.Text = "24-bit";
            this.radioButton24Bit.UseVisualStyleBackColor = true;
            // 
            // textBoxBufferDuration
            // 
            this.textBoxBufferDuration.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxBufferDuration.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.newSchoolDocumentBindingSource, "BufferDuration", true));
            this.textBoxBufferDuration.Location = new System.Drawing.Point(149, 136);
            this.textBoxBufferDuration.Name = "textBoxBufferDuration";
            this.textBoxBufferDuration.Size = new System.Drawing.Size(100, 20);
            this.textBoxBufferDuration.TabIndex = 12;
            // 
            // myLabel6
            // 
            this.myLabel6.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel6.AutoSize = true;
            this.myLabel6.Location = new System.Drawing.Point(3, 139);
            this.myLabel6.Name = "myLabel6";
            this.myLabel6.Size = new System.Drawing.Size(81, 13);
            this.myLabel6.TabIndex = 13;
            this.myLabel6.Text = "Buffer Duration:";
            // 
            // myLabel7
            // 
            this.myLabel7.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel7.AutoSize = true;
            this.myLabel7.Location = new System.Drawing.Point(3, 165);
            this.myLabel7.Name = "myLabel7";
            this.myLabel7.Size = new System.Drawing.Size(78, 13);
            this.myLabel7.TabIndex = 14;
            this.myLabel7.Text = "Random Seed:";
            // 
            // textBoxRandomSeed
            // 
            this.textBoxRandomSeed.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxRandomSeed.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.newSchoolDocumentBindingSource, "Seed", true));
            this.textBoxRandomSeed.Location = new System.Drawing.Point(149, 162);
            this.textBoxRandomSeed.Name = "textBoxRandomSeed";
            this.textBoxRandomSeed.Size = new System.Drawing.Size(150, 20);
            this.textBoxRandomSeed.TabIndex = 15;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.flowLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel2, 2);
            this.flowLayoutPanel2.Controls.Add(this.buttonOK);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(109, 200);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(131, 29);
            this.flowLayoutPanel2.TabIndex = 16;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(3, 3);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(125, 23);
            this.buttonOK.TabIndex = 0;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // PlayPrefsDialogNewSchool
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 232);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "PlayPrefsDialogNewSchool";
            this.Text = "PlayPrefsNewSchool";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.newSchoolDocumentBindingSource)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox textBoxSamplingRate;
        private System.Windows.Forms.TextBox textBoxEnvelopeRate;
        private MyLabel myLabel1;
        private MyLabel myLabel2;
        private System.Windows.Forms.BindingSource newSchoolDocumentBindingSource;
        private MyLabel myLabel3;
        private System.Windows.Forms.TextBox textBoxOversampling;
        private System.Windows.Forms.TextBox textBoxScanningGap;
        private MyLabel myLabel4;
        private MyLabel myLabel5;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.RadioButton radioButton16Bit;
        private System.Windows.Forms.RadioButton radioButton24Bit;
        private System.Windows.Forms.TextBox textBoxBufferDuration;
        private MyLabel myLabel6;
        private MyLabel myLabel7;
        private System.Windows.Forms.TextBox textBoxRandomSeed;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button buttonOK;
    }
}