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
    partial class AlgoSampWindow
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
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxLoop1Start = new System.Windows.Forms.TextBox();
            this.algoSampObjectRecBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.textBoxLoop1End = new System.Windows.Forms.TextBox();
            this.textBoxLoop2Start = new System.Windows.Forms.TextBox();
            this.textBoxLoop2End = new System.Windows.Forms.TextBox();
            this.textBoxLoop3Start = new System.Windows.Forms.TextBox();
            this.textBoxLoop3End = new System.Windows.Forms.TextBox();
            this.comboBoxLoop1Bidirectional = new System.Windows.Forms.ComboBox();
            this.comboBoxLoop2Bidirectional = new System.Windows.Forms.ComboBox();
            this.comboBoxLoop3Bidirectional = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.label8 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.textBoxOrigin = new System.Windows.Forms.TextBox();
            this.comboBoxChannels = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.textBoxSamplingRate = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.textBoxNaturalFrequency = new System.Windows.Forms.TextBox();
            this.menuStripManager = new OutOfPhase.MenuStripManager();
            this.textBoxFormula = new TextEditor.TextEditControl();
            this.documentBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.stringStorageFactory = new TextEditor.StringStorageFactory();
            this.textEditorWindowHelper = new TextEditor.TextEditorWindowHelper(this.components);
            this.textBoxWindowHelper = new OutOfPhase.TextBoxWindowHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.algoSampObjectRecBindingSource)).BeginInit();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.menuStripManager, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.textBoxFormula, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 195F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(579, 373);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 225);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(573, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Waveform Generating Function:";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel4, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 33);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(573, 189);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 4;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.Controls.Add(this.label2, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label3, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.label4, 3, 0);
            this.tableLayoutPanel3.Controls.Add(this.label5, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.label6, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.label7, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.textBoxLoop1Start, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.textBoxLoop1End, 1, 2);
            this.tableLayoutPanel3.Controls.Add(this.textBoxLoop2Start, 2, 1);
            this.tableLayoutPanel3.Controls.Add(this.textBoxLoop2End, 2, 2);
            this.tableLayoutPanel3.Controls.Add(this.textBoxLoop3Start, 3, 1);
            this.tableLayoutPanel3.Controls.Add(this.textBoxLoop3End, 3, 2);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxLoop1Bidirectional, 1, 3);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxLoop2Bidirectional, 2, 3);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxLoop3Bidirectional, 3, 3);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 93);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 4;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.Size = new System.Drawing.Size(567, 93);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(76, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(158, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Loop 1:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(240, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(158, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Loop 2:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(404, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(160, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Loop 3:";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 19);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(67, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Start:";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 45);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(67, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "End:";
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 72);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(67, 13);
            this.label7.TabIndex = 5;
            this.label7.Text = "Bidirectional:";
            // 
            // textBoxLoop1Start
            // 
            this.textBoxLoop1Start.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLoop1Start.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "LoopStart1", true));
            this.textBoxLoop1Start.Location = new System.Drawing.Point(76, 16);
            this.textBoxLoop1Start.Name = "textBoxLoop1Start";
            this.textBoxLoop1Start.Size = new System.Drawing.Size(158, 20);
            this.textBoxLoop1Start.TabIndex = 6;
            // 
            // algoSampObjectRecBindingSource
            // 
            this.algoSampObjectRecBindingSource.DataSource = typeof(OutOfPhase.AlgoSampObjectRec);
            // 
            // textBoxLoop1End
            // 
            this.textBoxLoop1End.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLoop1End.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "LoopEnd1", true));
            this.textBoxLoop1End.Location = new System.Drawing.Point(76, 42);
            this.textBoxLoop1End.Name = "textBoxLoop1End";
            this.textBoxLoop1End.Size = new System.Drawing.Size(158, 20);
            this.textBoxLoop1End.TabIndex = 7;
            // 
            // textBoxLoop2Start
            // 
            this.textBoxLoop2Start.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLoop2Start.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "LoopStart2", true));
            this.textBoxLoop2Start.Location = new System.Drawing.Point(240, 16);
            this.textBoxLoop2Start.Name = "textBoxLoop2Start";
            this.textBoxLoop2Start.Size = new System.Drawing.Size(158, 20);
            this.textBoxLoop2Start.TabIndex = 8;
            // 
            // textBoxLoop2End
            // 
            this.textBoxLoop2End.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLoop2End.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "LoopEnd2", true));
            this.textBoxLoop2End.Location = new System.Drawing.Point(240, 42);
            this.textBoxLoop2End.Name = "textBoxLoop2End";
            this.textBoxLoop2End.Size = new System.Drawing.Size(158, 20);
            this.textBoxLoop2End.TabIndex = 9;
            // 
            // textBoxLoop3Start
            // 
            this.textBoxLoop3Start.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLoop3Start.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "LoopStart3", true));
            this.textBoxLoop3Start.Location = new System.Drawing.Point(404, 16);
            this.textBoxLoop3Start.Name = "textBoxLoop3Start";
            this.textBoxLoop3Start.Size = new System.Drawing.Size(160, 20);
            this.textBoxLoop3Start.TabIndex = 10;
            // 
            // textBoxLoop3End
            // 
            this.textBoxLoop3End.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLoop3End.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "LoopEnd3", true));
            this.textBoxLoop3End.Location = new System.Drawing.Point(404, 42);
            this.textBoxLoop3End.Name = "textBoxLoop3End";
            this.textBoxLoop3End.Size = new System.Drawing.Size(160, 20);
            this.textBoxLoop3End.TabIndex = 11;
            // 
            // comboBoxLoop1Bidirectional
            // 
            this.comboBoxLoop1Bidirectional.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxLoop1Bidirectional.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "Loop1BidirectionalAsString", true));
            this.comboBoxLoop1Bidirectional.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLoop1Bidirectional.FormattingEnabled = true;
            this.comboBoxLoop1Bidirectional.Location = new System.Drawing.Point(76, 68);
            this.comboBoxLoop1Bidirectional.Name = "comboBoxLoop1Bidirectional";
            this.comboBoxLoop1Bidirectional.Size = new System.Drawing.Size(158, 21);
            this.comboBoxLoop1Bidirectional.TabIndex = 12;
            // 
            // comboBoxLoop2Bidirectional
            // 
            this.comboBoxLoop2Bidirectional.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxLoop2Bidirectional.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "Loop2BidirectionalAsString", true));
            this.comboBoxLoop2Bidirectional.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLoop2Bidirectional.FormattingEnabled = true;
            this.comboBoxLoop2Bidirectional.Location = new System.Drawing.Point(240, 68);
            this.comboBoxLoop2Bidirectional.Name = "comboBoxLoop2Bidirectional";
            this.comboBoxLoop2Bidirectional.Size = new System.Drawing.Size(158, 21);
            this.comboBoxLoop2Bidirectional.TabIndex = 13;
            // 
            // comboBoxLoop3Bidirectional
            // 
            this.comboBoxLoop3Bidirectional.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxLoop3Bidirectional.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "Loop3BidirectionalAsString", true));
            this.comboBoxLoop3Bidirectional.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLoop3Bidirectional.FormattingEnabled = true;
            this.comboBoxLoop3Bidirectional.Location = new System.Drawing.Point(404, 68);
            this.comboBoxLoop3Bidirectional.Name = "comboBoxLoop3Bidirectional";
            this.comboBoxLoop3Bidirectional.Size = new System.Drawing.Size(160, 21);
            this.comboBoxLoop3Bidirectional.TabIndex = 14;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 5;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Controls.Add(this.label8, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.label11, 3, 0);
            this.tableLayoutPanel4.Controls.Add(this.textBoxName, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.textBoxOrigin, 4, 0);
            this.tableLayoutPanel4.Controls.Add(this.comboBoxChannels, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.label10, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.label12, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.textBoxSamplingRate, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this.label13, 3, 1);
            this.tableLayoutPanel4.Controls.Add(this.textBoxNaturalFrequency, 4, 1);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 3;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.Size = new System.Drawing.Size(567, 84);
            this.tableLayoutPanel4.TabIndex = 1;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 6);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(79, 13);
            this.label8.TabIndex = 0;
            this.label8.Text = "Name:";
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(287, 6);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(97, 13);
            this.label11.TabIndex = 3;
            this.label11.Text = "Origin:";
            // 
            // textBoxName
            // 
            this.textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "Name", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxName.Location = new System.Drawing.Point(88, 3);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(173, 20);
            this.textBoxName.TabIndex = 6;
            // 
            // textBoxOrigin
            // 
            this.textBoxOrigin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOrigin.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "Origin", true));
            this.textBoxOrigin.Location = new System.Drawing.Point(390, 3);
            this.textBoxOrigin.Name = "textBoxOrigin";
            this.textBoxOrigin.Size = new System.Drawing.Size(174, 20);
            this.textBoxOrigin.TabIndex = 9;
            // 
            // comboBoxChannels
            // 
            this.comboBoxChannels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxChannels.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "NumChannelsAsString", true));
            this.comboBoxChannels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxChannels.FormattingEnabled = true;
            this.comboBoxChannels.Location = new System.Drawing.Point(88, 29);
            this.comboBoxChannels.Name = "comboBoxChannels";
            this.comboBoxChannels.Size = new System.Drawing.Size(173, 21);
            this.comboBoxChannels.TabIndex = 8;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(3, 33);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(79, 13);
            this.label10.TabIndex = 2;
            this.label10.Text = "Channels:";
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(3, 62);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(79, 13);
            this.label12.TabIndex = 4;
            this.label12.Text = "Sampling Rate:";
            // 
            // textBoxSamplingRate
            // 
            this.textBoxSamplingRate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSamplingRate.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "SamplingRate", true));
            this.textBoxSamplingRate.Location = new System.Drawing.Point(88, 58);
            this.textBoxSamplingRate.Name = "textBoxSamplingRate";
            this.textBoxSamplingRate.Size = new System.Drawing.Size(173, 20);
            this.textBoxSamplingRate.TabIndex = 10;
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(287, 33);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(97, 13);
            this.label13.TabIndex = 5;
            this.label13.Text = "Natural Frequency:";
            // 
            // textBoxNaturalFrequency
            // 
            this.textBoxNaturalFrequency.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxNaturalFrequency.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "NaturalFrequencyAsString", true));
            this.textBoxNaturalFrequency.Location = new System.Drawing.Point(390, 29);
            this.textBoxNaturalFrequency.Name = "textBoxNaturalFrequency";
            this.textBoxNaturalFrequency.Size = new System.Drawing.Size(174, 20);
            this.textBoxNaturalFrequency.TabIndex = 11;
            // 
            // menuStripManager
            // 
            this.menuStripManager.AutoSize = true;
            this.menuStripManager.Location = new System.Drawing.Point(3, 3);
            this.menuStripManager.Name = "menuStripManager";
            this.menuStripManager.Size = new System.Drawing.Size(573, 24);
            this.menuStripManager.TabIndex = 4;
            // 
            // textBoxFormula
            // 
            this.textBoxFormula.AutoScroll = true;
            this.textBoxFormula.AutoScrollMinSize = new System.Drawing.Size(571, 13);
            this.textBoxFormula.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBoxFormula.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxFormula.DataBindings.Add(new System.Windows.Forms.Binding("TabSize", this.documentBindingSource, "TabSize", true));
            this.textBoxFormula.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.algoSampObjectRecBindingSource, "AlgoSampFormula", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxFormula.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxFormula.Location = new System.Drawing.Point(3, 241);
            this.textBoxFormula.Name = "textBoxFormula";
            this.textBoxFormula.Size = new System.Drawing.Size(573, 129);
            this.textBoxFormula.TabIndex = 5;
            this.textBoxFormula.TextService = TextEditor.TextService.Simple;
            this.textBoxFormula.TextStorageFactory = this.stringStorageFactory;
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
            this.textEditorWindowHelper.TextEditControl = this.textBoxFormula;
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
            // AlgoSampWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(579, 373);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "AlgoSampWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.algoSampObjectRecBindingSource)).EndInit();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBoxLoop1Start;
        private System.Windows.Forms.TextBox textBoxLoop1End;
        private System.Windows.Forms.TextBox textBoxLoop2Start;
        private System.Windows.Forms.TextBox textBoxLoop2End;
        private System.Windows.Forms.TextBox textBoxLoop3Start;
        private System.Windows.Forms.TextBox textBoxLoop3End;
        private System.Windows.Forms.ComboBox comboBoxLoop1Bidirectional;
        private System.Windows.Forms.ComboBox comboBoxLoop2Bidirectional;
        private System.Windows.Forms.ComboBox comboBoxLoop3Bidirectional;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.ComboBox comboBoxChannels;
        private System.Windows.Forms.TextBox textBoxOrigin;
        private System.Windows.Forms.TextBox textBoxSamplingRate;
        private System.Windows.Forms.TextBox textBoxNaturalFrequency;
        private System.Windows.Forms.BindingSource algoSampObjectRecBindingSource;
        private MenuStripManager menuStripManager;
        private TextEditor.TextEditControl textBoxFormula;
        private System.Windows.Forms.BindingSource documentBindingSource;
        private TextEditor.TextEditorWindowHelper textEditorWindowHelper;
        private TextBoxWindowHelper textBoxWindowHelper;
        private TextEditor.StringStorageFactory stringStorageFactory;
    }
}