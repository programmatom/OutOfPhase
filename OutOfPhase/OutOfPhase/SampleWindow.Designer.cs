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
    partial class SampleWindow
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
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonTest = new System.Windows.Forms.Button();
            this.textBoxTestPitch = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.textBoxScale = new System.Windows.Forms.TextBox();
            this.textBoxSelectionEnd = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.textBoxSelectionStart = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.sampleControl = new OutOfPhase.SampleControl();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.sampleObjectRecBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.textBoxSamplingRate = new System.Windows.Forms.TextBox();
            this.textBoxOrigin = new System.Windows.Forms.TextBox();
            this.textBoxNaturalFrequency = new System.Windows.Forms.TextBox();
            this.menuStripManager = new OutOfPhase.MenuStripManager();
            this.textBoxFunction = new TextEditor.TextEditControl();
            this.documentBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.stringStorageFactory = new TextEditor.StringStorageFactory();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxLoopStart = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.textBoxLoopEnd = new System.Windows.Forms.TextBox();
            this.comboBoxLoopBidirectional = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.comboBoxNumBits = new System.Windows.Forms.ComboBox();
            this.label16 = new System.Windows.Forms.Label();
            this.comboBoxNumChannels = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonZoomIn = new System.Windows.Forms.Button();
            this.buttonZoomOut = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textEditorWindowHelper = new TextEditor.TextEditorWindowHelper(this.components);
            this.textBoxWindowHelper = new OutOfPhase.TextBoxWindowHelper(this.components);
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sampleObjectRecBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 10;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.00062F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.00062F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.00062F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24.99813F));
            this.tableLayoutPanel2.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.buttonTest, 9, 4);
            this.tableLayoutPanel2.Controls.Add(this.textBoxTestPitch, 7, 4);
            this.tableLayoutPanel2.Controls.Add(this.label17, 6, 4);
            this.tableLayoutPanel2.Controls.Add(this.textBoxScale, 7, 3);
            this.tableLayoutPanel2.Controls.Add(this.textBoxSelectionEnd, 4, 4);
            this.tableLayoutPanel2.Controls.Add(this.label13, 3, 4);
            this.tableLayoutPanel2.Controls.Add(this.textBoxSelectionStart, 4, 3);
            this.tableLayoutPanel2.Controls.Add(this.label12, 3, 3);
            this.tableLayoutPanel2.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.label14, 6, 3);
            this.tableLayoutPanel2.Controls.Add(this.sampleControl, 0, 5);
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.label5, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.textBoxName, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.textBoxSamplingRate, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.textBoxOrigin, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.textBoxNaturalFrequency, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.menuStripManager, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.textBoxFunction, 0, 7);
            this.tableLayoutPanel2.Controls.Add(this.label6, 3, 2);
            this.tableLayoutPanel2.Controls.Add(this.textBoxLoopStart, 4, 2);
            this.tableLayoutPanel2.Controls.Add(this.label9, 6, 2);
            this.tableLayoutPanel2.Controls.Add(this.textBoxLoopEnd, 7, 2);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxLoopBidirectional, 9, 2);
            this.tableLayoutPanel2.Controls.Add(this.label15, 3, 1);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxNumBits, 4, 1);
            this.tableLayoutPanel2.Controls.Add(this.label16, 6, 1);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxNumChannels, 7, 1);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel1, 9, 3);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 6);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 8;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(968, 432);
            this.tableLayoutPanel2.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Name";
            // 
            // buttonTest
            // 
            this.buttonTest.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.buttonTest.Location = new System.Drawing.Point(798, 114);
            this.buttonTest.Name = "buttonTest";
            this.buttonTest.Size = new System.Drawing.Size(55, 21);
            this.buttonTest.TabIndex = 13;
            this.buttonTest.Text = "Test";
            this.buttonTest.UseVisualStyleBackColor = true;
            // 
            // textBoxTestPitch
            // 
            this.textBoxTestPitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxTestPitch.Location = new System.Drawing.Point(617, 114);
            this.textBoxTestPitch.Name = "textBoxTestPitch";
            this.textBoxTestPitch.Size = new System.Drawing.Size(165, 20);
            this.textBoxTestPitch.TabIndex = 12;
            // 
            // label17
            // 
            this.label17.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(553, 118);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(58, 13);
            this.label17.TabIndex = 11;
            this.label17.Text = "Test Pitch:";
            // 
            // textBoxScale
            // 
            this.textBoxScale.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxScale.Location = new System.Drawing.Point(617, 87);
            this.textBoxScale.Name = "textBoxScale";
            this.textBoxScale.Size = new System.Drawing.Size(165, 20);
            this.textBoxScale.TabIndex = 8;
            // 
            // textBoxSelectionEnd
            // 
            this.textBoxSelectionEnd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSelectionEnd.Location = new System.Drawing.Point(372, 114);
            this.textBoxSelectionEnd.Name = "textBoxSelectionEnd";
            this.textBoxSelectionEnd.Size = new System.Drawing.Size(165, 20);
            this.textBoxSelectionEnd.TabIndex = 6;
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(287, 118);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(79, 13);
            this.label13.TabIndex = 5;
            this.label13.Text = "Selection End:";
            // 
            // textBoxSelectionStart
            // 
            this.textBoxSelectionStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSelectionStart.Location = new System.Drawing.Point(372, 87);
            this.textBoxSelectionStart.Name = "textBoxSelectionStart";
            this.textBoxSelectionStart.Size = new System.Drawing.Size(165, 20);
            this.textBoxSelectionStart.TabIndex = 4;
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(287, 91);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(79, 13);
            this.label12.TabIndex = 3;
            this.label12.Text = "Selection Start:";
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 64);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(79, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Sampling Rate:";
            // 
            // label14
            // 
            this.label14.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(553, 91);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(58, 13);
            this.label14.TabIndex = 7;
            this.label14.Text = "Scale:";
            // 
            // sampleControl
            // 
            this.sampleControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel2.SetColumnSpan(this.sampleControl, 10);
            this.sampleControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sampleControl.Location = new System.Drawing.Point(3, 141);
            this.sampleControl.LoopEndLabel = "Loop 1 End";
            this.sampleControl.LoopStartLabel = "Loop 1 Start";
            this.sampleControl.Name = "sampleControl";
            this.sampleControl.SampleObject = null;
            this.sampleControl.Size = new System.Drawing.Size(962, 195);
            this.sampleControl.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 91);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Origin:";
            // 
            // label5
            // 
            this.label5.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 118);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(97, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Natural Frequency:";
            // 
            // textBoxName
            // 
            this.textBoxName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "Name", true));
            this.textBoxName.Location = new System.Drawing.Point(106, 33);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(165, 20);
            this.textBoxName.TabIndex = 4;
            // 
            // sampleObjectRecBindingSource
            // 
            this.sampleObjectRecBindingSource.DataSource = typeof(OutOfPhase.SampleObjectRec);
            // 
            // textBoxSamplingRate
            // 
            this.textBoxSamplingRate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxSamplingRate.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "SamplingRate", true));
            this.textBoxSamplingRate.Location = new System.Drawing.Point(106, 60);
            this.textBoxSamplingRate.Name = "textBoxSamplingRate";
            this.textBoxSamplingRate.Size = new System.Drawing.Size(165, 20);
            this.textBoxSamplingRate.TabIndex = 5;
            // 
            // textBoxOrigin
            // 
            this.textBoxOrigin.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxOrigin.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "Origin", true));
            this.textBoxOrigin.Location = new System.Drawing.Point(106, 87);
            this.textBoxOrigin.Name = "textBoxOrigin";
            this.textBoxOrigin.Size = new System.Drawing.Size(165, 20);
            this.textBoxOrigin.TabIndex = 6;
            // 
            // textBoxNaturalFrequency
            // 
            this.textBoxNaturalFrequency.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxNaturalFrequency.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "NaturalFrequencyAsString", true));
            this.textBoxNaturalFrequency.Location = new System.Drawing.Point(106, 114);
            this.textBoxNaturalFrequency.Name = "textBoxNaturalFrequency";
            this.textBoxNaturalFrequency.Size = new System.Drawing.Size(165, 20);
            this.textBoxNaturalFrequency.TabIndex = 7;
            // 
            // menuStripManager
            // 
            this.menuStripManager.AutoSize = true;
            this.tableLayoutPanel2.SetColumnSpan(this.menuStripManager, 10);
            this.menuStripManager.Location = new System.Drawing.Point(3, 3);
            this.menuStripManager.Name = "menuStripManager";
            this.menuStripManager.Size = new System.Drawing.Size(962, 24);
            this.menuStripManager.TabIndex = 27;
            // 
            // textBoxFunction
            // 
            this.textBoxFunction.AutoScroll = true;
            this.textBoxFunction.AutoScrollMinSize = new System.Drawing.Size(960, 13);
            this.textBoxFunction.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBoxFunction.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel2.SetColumnSpan(this.textBoxFunction, 10);
            this.textBoxFunction.DataBindings.Add(new System.Windows.Forms.Binding("TabSize", this.documentBindingSource, "TabSize", true));
            this.textBoxFunction.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "SampleFormula", true));
            this.textBoxFunction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxFunction.Location = new System.Drawing.Point(3, 355);
            this.textBoxFunction.Name = "textBoxFunction";
            this.textBoxFunction.Size = new System.Drawing.Size(962, 74);
            this.textBoxFunction.TabIndex = 28;
            this.textBoxFunction.TextService = TextEditor.TextService.Simple;
            this.textBoxFunction.TextStorageFactory = this.stringStorageFactory;
            // 
            // documentBindingSource
            // 
            this.documentBindingSource.DataSource = typeof(OutOfPhase.Document);
            // 
            // label6
            // 
            this.label6.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(287, 64);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(59, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Loop Start:";
            // 
            // textBoxLoopStart
            // 
            this.textBoxLoopStart.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxLoopStart.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "LoopStart1", true));
            this.textBoxLoopStart.Location = new System.Drawing.Point(372, 60);
            this.textBoxLoopStart.Name = "textBoxLoopStart";
            this.textBoxLoopStart.Size = new System.Drawing.Size(165, 20);
            this.textBoxLoopStart.TabIndex = 10;
            // 
            // label9
            // 
            this.label9.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(553, 64);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(56, 13);
            this.label9.TabIndex = 17;
            this.label9.Text = "Loop End:";
            // 
            // textBoxLoopEnd
            // 
            this.textBoxLoopEnd.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxLoopEnd.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "LoopEnd1", true));
            this.textBoxLoopEnd.Location = new System.Drawing.Point(617, 60);
            this.textBoxLoopEnd.Name = "textBoxLoopEnd";
            this.textBoxLoopEnd.Size = new System.Drawing.Size(165, 20);
            this.textBoxLoopEnd.TabIndex = 14;
            // 
            // comboBoxLoopBidirectional
            // 
            this.comboBoxLoopBidirectional.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.comboBoxLoopBidirectional.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "Loop1BidirectionalAsString", true));
            this.comboBoxLoopBidirectional.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLoopBidirectional.FormattingEnabled = true;
            this.comboBoxLoopBidirectional.Location = new System.Drawing.Point(798, 60);
            this.comboBoxLoopBidirectional.Name = "comboBoxLoopBidirectional";
            this.comboBoxLoopBidirectional.Size = new System.Drawing.Size(165, 21);
            this.comboBoxLoopBidirectional.TabIndex = 20;
            // 
            // label15
            // 
            this.label15.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(287, 37);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(27, 13);
            this.label15.TabIndex = 23;
            this.label15.Text = "Bits:";
            // 
            // comboBoxNumBits
            // 
            this.comboBoxNumBits.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.comboBoxNumBits.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "NumBitsAsString", true));
            this.comboBoxNumBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNumBits.FormattingEnabled = true;
            this.comboBoxNumBits.Location = new System.Drawing.Point(372, 33);
            this.comboBoxNumBits.Name = "comboBoxNumBits";
            this.comboBoxNumBits.Size = new System.Drawing.Size(165, 21);
            this.comboBoxNumBits.TabIndex = 25;
            // 
            // label16
            // 
            this.label16.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(553, 37);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(54, 13);
            this.label16.TabIndex = 24;
            this.label16.Text = "Channels:";
            // 
            // comboBoxNumChannels
            // 
            this.comboBoxNumChannels.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.comboBoxNumChannels.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sampleObjectRecBindingSource, "NumChannelsAsString", true));
            this.comboBoxNumChannels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNumChannels.FormattingEnabled = true;
            this.comboBoxNumChannels.Location = new System.Drawing.Point(617, 33);
            this.comboBoxNumChannels.Name = "comboBoxNumChannels";
            this.comboBoxNumChannels.Size = new System.Drawing.Size(165, 21);
            this.comboBoxNumChannels.TabIndex = 26;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.buttonZoomIn);
            this.flowLayoutPanel1.Controls.Add(this.buttonZoomOut);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(795, 84);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(80, 27);
            this.flowLayoutPanel1.TabIndex = 29;
            // 
            // buttonZoomIn
            // 
            this.buttonZoomIn.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.buttonZoomIn.Location = new System.Drawing.Point(3, 3);
            this.buttonZoomIn.Name = "buttonZoomIn";
            this.buttonZoomIn.Size = new System.Drawing.Size(34, 21);
            this.buttonZoomIn.TabIndex = 9;
            this.buttonZoomIn.Text = "In";
            this.buttonZoomIn.UseVisualStyleBackColor = true;
            this.buttonZoomIn.Click += new System.EventHandler(this.buttonZoomIn_Click);
            // 
            // buttonZoomOut
            // 
            this.buttonZoomOut.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.buttonZoomOut.Location = new System.Drawing.Point(43, 3);
            this.buttonZoomOut.Name = "buttonZoomOut";
            this.buttonZoomOut.Size = new System.Drawing.Size(34, 21);
            this.buttonZoomOut.TabIndex = 10;
            this.buttonZoomOut.Text = "Out";
            this.buttonZoomOut.UseVisualStyleBackColor = true;
            this.buttonZoomOut.Click += new System.EventHandler(this.buttonZoomOut_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.tableLayoutPanel2.SetColumnSpan(this.label1, 10);
            this.label1.Location = new System.Drawing.Point(3, 339);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(962, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Function:";
            // 
            // textEditorWindowHelper
            // 
            this.textEditorWindowHelper.BalanceToolStripMenuItem = null;
            this.textEditorWindowHelper.ClearToolStripMenuItem = null;
            this.textEditorWindowHelper.ConvertTabsToSpacesToolStripMenuItem = null;
            this.textEditorWindowHelper.CopyToolStripMenuItem = null;
            this.textEditorWindowHelper.CutToolStripMenuItem = null;
            this.textEditorWindowHelper.DelegatedMode = true;
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
            this.textBoxWindowHelper.DelegatedMode = true;
            this.textBoxWindowHelper.PasteToolStripMenuItem = null;
            this.textBoxWindowHelper.SelectAllToolStripMenuItem = null;
            this.textBoxWindowHelper.UndoToolStripMenuItem = null;
            // 
            // SampleWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(968, 432);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Name = "SampleWindow";
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sampleObjectRecBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.TextBox textBoxSamplingRate;
        private System.Windows.Forms.TextBox textBoxOrigin;
        private System.Windows.Forms.TextBox textBoxNaturalFrequency;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxLoopStart;
        private System.Windows.Forms.TextBox textBoxLoopEnd;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox comboBoxLoopBidirectional;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox textBoxSelectionStart;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox textBoxSelectionEnd;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textBoxScale;
        private System.Windows.Forms.Button buttonZoomIn;
        private System.Windows.Forms.Button buttonZoomOut;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.ComboBox comboBoxNumBits;
        private System.Windows.Forms.ComboBox comboBoxNumChannels;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox textBoxTestPitch;
        private System.Windows.Forms.Button buttonTest;
        private SampleControl sampleControl;
        private System.Windows.Forms.BindingSource sampleObjectRecBindingSource;
        private MenuStripManager menuStripManager;
        private TextEditor.TextEditControl textBoxFunction;
        private System.Windows.Forms.BindingSource documentBindingSource;
        private TextEditor.TextEditorWindowHelper textEditorWindowHelper;
        private TextBoxWindowHelper textBoxWindowHelper;
        private TextEditor.StringStorageFactory stringStorageFactory;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    }
}
