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
    partial class WaveTableWindow
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
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.waveTableObjectRecBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxNumTables = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.comboBoxNumFrames = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxNumBits = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.textBoxTestAttackDuration = new System.Windows.Forms.TextBox();
            this.textBoxTestDecayDuration = new System.Windows.Forms.TextBox();
            this.textBoxTestFrequency = new System.Windows.Forms.TextBox();
            this.textBoxTestSamplingRate = new System.Windows.Forms.TextBox();
            this.buttonTest = new System.Windows.Forms.Button();
            this.labelScale = new System.Windows.Forms.Label();
            this.comboBoxScale = new System.Windows.Forms.ComboBox();
            this.tabControlWave = new System.Windows.Forms.TabControl();
            this.tabPageWaveVisual = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.hScrollBarWaveTable = new System.Windows.Forms.HScrollBar();
            this.panelWaveTable = new System.Windows.Forms.Panel();
            this.waveTableControl = new OutOfPhase.WaveTableControl();
            this.tabPageWaveGrid = new System.Windows.Forms.TabPage();
            this.dataGridViewWave = new System.Windows.Forms.DataGridView();
            this.menuStripManager = new OutOfPhase.MenuStripManager();
            this.textBoxFormula = new TextEditor.TextEditControl();
            this.documentBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.stringStorageFactory = new TextEditor.StringStorageFactory();
            this.textEditorWindowHelper = new TextEditor.TextEditorWindowHelper(this.components);
            this.textBoxWindowHelper = new OutOfPhase.TextBoxWindowHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.waveTableObjectRecBindingSource)).BeginInit();
            this.tabControlWave.SuspendLayout();
            this.tabPageWaveVisual.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.panelWaveTable.SuspendLayout();
            this.tabPageWaveGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewWave)).BeginInit();
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
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(701, 388);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 275);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(695, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Wave Table Function:";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 350F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.tabControlWave, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 33);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(695, 239);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.AutoSize = true;
            this.tableLayoutPanel3.ColumnCount = 5;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.textBoxName, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label3, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.textBoxNumTables, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.label4, 3, 0);
            this.tableLayoutPanel3.Controls.Add(this.label5, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxNumFrames, 1, 2);
            this.tableLayoutPanel3.Controls.Add(this.label6, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxNumBits, 1, 3);
            this.tableLayoutPanel3.Controls.Add(this.label7, 3, 1);
            this.tableLayoutPanel3.Controls.Add(this.label8, 3, 2);
            this.tableLayoutPanel3.Controls.Add(this.label9, 3, 3);
            this.tableLayoutPanel3.Controls.Add(this.label10, 3, 4);
            this.tableLayoutPanel3.Controls.Add(this.textBoxTestAttackDuration, 4, 1);
            this.tableLayoutPanel3.Controls.Add(this.textBoxTestDecayDuration, 4, 2);
            this.tableLayoutPanel3.Controls.Add(this.textBoxTestFrequency, 4, 3);
            this.tableLayoutPanel3.Controls.Add(this.textBoxTestSamplingRate, 4, 4);
            this.tableLayoutPanel3.Controls.Add(this.buttonTest, 4, 5);
            this.tableLayoutPanel3.Controls.Add(this.labelScale, 0, 5);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxScale, 1, 5);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(348, 3);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 6;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.Size = new System.Drawing.Size(344, 161);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Name:";
            // 
            // textBoxName
            // 
            this.textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.waveTableObjectRecBindingSource, "Name", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxName.Location = new System.Drawing.Point(53, 3);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(86, 20);
            this.textBoxName.TabIndex = 1;
            // 
            // waveTableObjectRecBindingSource
            // 
            this.waveTableObjectRecBindingSource.DataSource = typeof(OutOfPhase.WaveTableObjectRec);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Tables:";
            // 
            // textBoxNumTables
            // 
            this.textBoxNumTables.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxNumTables.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.waveTableObjectRecBindingSource, "NumTables", true));
            this.textBoxNumTables.Location = new System.Drawing.Point(53, 29);
            this.textBoxNumTables.Name = "textBoxNumTables";
            this.textBoxNumTables.Size = new System.Drawing.Size(86, 20);
            this.textBoxNumTables.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(165, 6);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(84, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Testing:";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 59);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(44, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "Frames:";
            // 
            // comboBoxNumFrames
            // 
            this.comboBoxNumFrames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxNumFrames.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.waveTableObjectRecBindingSource, "NumFrames", true));
            this.comboBoxNumFrames.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNumFrames.FormattingEnabled = true;
            this.comboBoxNumFrames.Location = new System.Drawing.Point(53, 55);
            this.comboBoxNumFrames.Name = "comboBoxNumFrames";
            this.comboBoxNumFrames.Size = new System.Drawing.Size(86, 21);
            this.comboBoxNumFrames.TabIndex = 6;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 86);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(44, 13);
            this.label6.TabIndex = 7;
            this.label6.Text = "Bits:";
            // 
            // comboBoxNumBits
            // 
            this.comboBoxNumBits.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxNumBits.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.waveTableObjectRecBindingSource, "NumBitsAsString", true));
            this.comboBoxNumBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNumBits.FormattingEnabled = true;
            this.comboBoxNumBits.Location = new System.Drawing.Point(53, 82);
            this.comboBoxNumBits.Name = "comboBoxNumBits";
            this.comboBoxNumBits.Size = new System.Drawing.Size(86, 21);
            this.comboBoxNumBits.TabIndex = 8;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(165, 32);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(84, 13);
            this.label7.TabIndex = 9;
            this.label7.Text = "Attack Duration:";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(165, 59);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(84, 13);
            this.label8.TabIndex = 10;
            this.label8.Text = "Decay Duration:";
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(165, 86);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(84, 13);
            this.label9.TabIndex = 11;
            this.label9.Text = "Frequency:";
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(165, 112);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(84, 13);
            this.label10.TabIndex = 12;
            this.label10.Text = "Sampling Rate:";
            // 
            // textBoxTestAttackDuration
            // 
            this.textBoxTestAttackDuration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxTestAttackDuration.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.waveTableObjectRecBindingSource, "TestAttackDuration", true));
            this.textBoxTestAttackDuration.Location = new System.Drawing.Point(255, 29);
            this.textBoxTestAttackDuration.Name = "textBoxTestAttackDuration";
            this.textBoxTestAttackDuration.Size = new System.Drawing.Size(86, 20);
            this.textBoxTestAttackDuration.TabIndex = 13;
            // 
            // textBoxTestDecayDuration
            // 
            this.textBoxTestDecayDuration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxTestDecayDuration.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.waveTableObjectRecBindingSource, "TestDecayDuration", true));
            this.textBoxTestDecayDuration.Location = new System.Drawing.Point(255, 55);
            this.textBoxTestDecayDuration.Name = "textBoxTestDecayDuration";
            this.textBoxTestDecayDuration.Size = new System.Drawing.Size(86, 20);
            this.textBoxTestDecayDuration.TabIndex = 14;
            // 
            // textBoxTestFrequency
            // 
            this.textBoxTestFrequency.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxTestFrequency.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.waveTableObjectRecBindingSource, "TestFrequencyAsString", true));
            this.textBoxTestFrequency.Location = new System.Drawing.Point(255, 82);
            this.textBoxTestFrequency.Name = "textBoxTestFrequency";
            this.textBoxTestFrequency.Size = new System.Drawing.Size(86, 20);
            this.textBoxTestFrequency.TabIndex = 15;
            // 
            // textBoxTestSamplingRate
            // 
            this.textBoxTestSamplingRate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxTestSamplingRate.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.waveTableObjectRecBindingSource, "TestSamplingRate", true));
            this.textBoxTestSamplingRate.Location = new System.Drawing.Point(255, 109);
            this.textBoxTestSamplingRate.Name = "textBoxTestSamplingRate";
            this.textBoxTestSamplingRate.Size = new System.Drawing.Size(86, 20);
            this.textBoxTestSamplingRate.TabIndex = 16;
            // 
            // buttonTest
            // 
            this.buttonTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonTest.Location = new System.Drawing.Point(255, 135);
            this.buttonTest.Name = "buttonTest";
            this.buttonTest.Size = new System.Drawing.Size(86, 23);
            this.buttonTest.TabIndex = 17;
            this.buttonTest.Text = "Test";
            this.buttonTest.UseVisualStyleBackColor = true;
            // 
            // labelScale
            // 
            this.labelScale.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelScale.AutoSize = true;
            this.labelScale.Location = new System.Drawing.Point(3, 140);
            this.labelScale.Name = "labelScale";
            this.labelScale.Size = new System.Drawing.Size(44, 13);
            this.labelScale.TabIndex = 18;
            this.labelScale.Text = "Scale:";
            // 
            // comboBoxScale
            // 
            this.comboBoxScale.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxScale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxScale.FormattingEnabled = true;
            this.comboBoxScale.Items.AddRange(new object[] {
            "Unity",
            "8-Bit",
            "16-Bit",
            "24-Bit"});
            this.comboBoxScale.Location = new System.Drawing.Point(53, 136);
            this.comboBoxScale.Name = "comboBoxScale";
            this.comboBoxScale.Size = new System.Drawing.Size(86, 21);
            this.comboBoxScale.TabIndex = 19;
            this.comboBoxScale.SelectedIndexChanged += new System.EventHandler(this.comboBoxScale_SelectedIndexChanged);
            // 
            // tabControlWave
            // 
            this.tabControlWave.Controls.Add(this.tabPageWaveVisual);
            this.tabControlWave.Controls.Add(this.tabPageWaveGrid);
            this.tabControlWave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlWave.Location = new System.Drawing.Point(0, 0);
            this.tabControlWave.Margin = new System.Windows.Forms.Padding(0);
            this.tabControlWave.Name = "tabControlWave";
            this.tabControlWave.SelectedIndex = 0;
            this.tabControlWave.Size = new System.Drawing.Size(345, 239);
            this.tabControlWave.TabIndex = 1;
            // 
            // tabPageWaveVisual
            // 
            this.tabPageWaveVisual.Controls.Add(this.tableLayoutPanel4);
            this.tabPageWaveVisual.Location = new System.Drawing.Point(4, 22);
            this.tabPageWaveVisual.Margin = new System.Windows.Forms.Padding(0);
            this.tabPageWaveVisual.Name = "tabPageWaveVisual";
            this.tabPageWaveVisual.Size = new System.Drawing.Size(337, 213);
            this.tabPageWaveVisual.TabIndex = 0;
            this.tabPageWaveVisual.Text = "Wave";
            this.tabPageWaveVisual.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.hScrollBarWaveTable, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.panelWaveTable, 0, 0);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.Size = new System.Drawing.Size(337, 213);
            this.tableLayoutPanel4.TabIndex = 1;
            // 
            // hScrollBarWaveTable
            // 
            this.hScrollBarWaveTable.DataBindings.Add(new System.Windows.Forms.Binding("Maximum", this.waveTableObjectRecBindingSource, "NumTables", true));
            this.hScrollBarWaveTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hScrollBarWaveTable.LargeChange = 1;
            this.hScrollBarWaveTable.Location = new System.Drawing.Point(0, 196);
            this.hScrollBarWaveTable.Name = "hScrollBarWaveTable";
            this.hScrollBarWaveTable.Size = new System.Drawing.Size(337, 17);
            this.hScrollBarWaveTable.TabIndex = 0;
            // 
            // panelWaveTable
            // 
            this.panelWaveTable.Controls.Add(this.waveTableControl);
            this.panelWaveTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelWaveTable.Location = new System.Drawing.Point(0, 0);
            this.panelWaveTable.Margin = new System.Windows.Forms.Padding(0);
            this.panelWaveTable.Name = "panelWaveTable";
            this.panelWaveTable.Size = new System.Drawing.Size(337, 196);
            this.panelWaveTable.TabIndex = 1;
            // 
            // waveTableControl
            // 
            this.waveTableControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waveTableControl.Location = new System.Drawing.Point(0, 0);
            this.waveTableControl.Name = "waveTableControl";
            this.waveTableControl.Size = new System.Drawing.Size(337, 196);
            this.waveTableControl.TabIndex = 0;
            this.waveTableControl.WaveTableObject = null;
            // 
            // tabPageWaveGrid
            // 
            this.tabPageWaveGrid.Controls.Add(this.dataGridViewWave);
            this.tabPageWaveGrid.Location = new System.Drawing.Point(4, 22);
            this.tabPageWaveGrid.Name = "tabPageWaveGrid";
            this.tabPageWaveGrid.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageWaveGrid.Size = new System.Drawing.Size(337, 213);
            this.tabPageWaveGrid.TabIndex = 1;
            this.tabPageWaveGrid.Text = "Grid";
            this.tabPageWaveGrid.UseVisualStyleBackColor = true;
            // 
            // dataGridViewWave
            // 
            this.dataGridViewWave.AllowUserToAddRows = false;
            this.dataGridViewWave.AllowUserToDeleteRows = false;
            this.dataGridViewWave.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dataGridViewWave.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewWave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewWave.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewWave.Name = "dataGridViewWave";
            this.dataGridViewWave.Size = new System.Drawing.Size(331, 207);
            this.dataGridViewWave.TabIndex = 0;
            this.dataGridViewWave.VirtualMode = true;
            // 
            // menuStripManager
            // 
            this.menuStripManager.AutoSize = true;
            this.menuStripManager.Location = new System.Drawing.Point(3, 3);
            this.menuStripManager.Name = "menuStripManager";
            this.menuStripManager.Size = new System.Drawing.Size(695, 24);
            this.menuStripManager.TabIndex = 4;
            // 
            // textBoxFormula
            // 
            this.textBoxFormula.AutoScroll = true;
            this.textBoxFormula.AutoScrollMinSize = new System.Drawing.Size(693, 13);
            this.textBoxFormula.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBoxFormula.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxFormula.DataBindings.Add(new System.Windows.Forms.Binding("TabSize", this.documentBindingSource, "TabSize", true));
            this.textBoxFormula.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.waveTableObjectRecBindingSource, "WaveTableFormula", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.textBoxFormula.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxFormula.Location = new System.Drawing.Point(3, 291);
            this.textBoxFormula.Name = "textBoxFormula";
            this.textBoxFormula.Size = new System.Drawing.Size(695, 94);
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
            // WaveTableWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(701, 388);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "WaveTableWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.waveTableObjectRecBindingSource)).EndInit();
            this.tabControlWave.ResumeLayout(false);
            this.tabPageWaveVisual.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.panelWaveTable.ResumeLayout(false);
            this.tabPageWaveGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewWave)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.BindingSource waveTableObjectRecBindingSource;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxNumTables;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox comboBoxNumFrames;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxNumBits;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBoxTestAttackDuration;
        private System.Windows.Forms.TextBox textBoxTestDecayDuration;
        private System.Windows.Forms.TextBox textBoxTestFrequency;
        private System.Windows.Forms.TextBox textBoxTestSamplingRate;
        private System.Windows.Forms.Button buttonTest;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.HScrollBar hScrollBarWaveTable;
        private System.Windows.Forms.Panel panelWaveTable;
        private WaveTableControl waveTableControl;
        private MenuStripManager menuStripManager;
        private TextEditor.TextEditControl textBoxFormula;
        private System.Windows.Forms.BindingSource documentBindingSource;
        private TextEditor.TextEditorWindowHelper textEditorWindowHelper;
        private TextBoxWindowHelper textBoxWindowHelper;
        private TextEditor.StringStorageFactory stringStorageFactory;
        private System.Windows.Forms.TabControl tabControlWave;
        private System.Windows.Forms.TabPage tabPageWaveGrid;
        private System.Windows.Forms.TabPage tabPageWaveVisual;
        private System.Windows.Forms.DataGridView dataGridViewWave;
        private System.Windows.Forms.Label labelScale;
        private System.Windows.Forms.ComboBox comboBoxScale;
    }
}
