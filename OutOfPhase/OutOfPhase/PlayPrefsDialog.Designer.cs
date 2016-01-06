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
    partial class PlayPrefsDialog
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
            this.includedTracksBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.sourceBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonSelectAllTracks = new System.Windows.Forms.Button();
            this.buttonUnselectAllTracks = new System.Windows.Forms.Button();
            this.labelBottomButtonSeparator = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonDone = new System.Windows.Forms.Button();
            this.buttonPlayToAudio = new System.Windows.Forms.Button();
            this.buttonPlayToDisk = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.checkBoxClipWarning = new System.Windows.Forms.CheckBox();
            this.comboBoxNumChannels = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.comboBoxBitDepth = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.textBoxOversampling = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxBufferSeconds = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxScanningGapSeconds = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxInverseVolume = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxBeatsPerMinute = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxEnvelopeRate = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxSamplingRate = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.listBoxIncludedTracks = new OutOfPhase.MyListBox();
            this.checkBoxShowSummary = new System.Windows.Forms.CheckBox();
            this.checkBoxDeterministic = new System.Windows.Forms.CheckBox();
            this.textBoxSeed = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.includedTracksBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sourceBindingSource)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // includedTracksBindingSource
            // 
            this.includedTracksBindingSource.DataMember = "IncludedTracks";
            this.includedTracksBindingSource.DataSource = this.sourceBindingSource;
            // 
            // sourceBindingSource
            // 
            this.sourceBindingSource.DataSource = typeof(OutOfPhase.PlayPrefsDialog.Source);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 5);
            this.flowLayoutPanel1.Controls.Add(this.buttonSelectAllTracks);
            this.flowLayoutPanel1.Controls.Add(this.buttonUnselectAllTracks);
            this.flowLayoutPanel1.Controls.Add(this.labelBottomButtonSeparator);
            this.flowLayoutPanel1.Controls.Add(this.buttonCancel);
            this.flowLayoutPanel1.Controls.Add(this.buttonDone);
            this.flowLayoutPanel1.Controls.Add(this.buttonPlayToAudio);
            this.flowLayoutPanel1.Controls.Add(this.buttonPlayToDisk);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(23, 348);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(614, 29);
            this.flowLayoutPanel1.TabIndex = 1;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // buttonSelectAllTracks
            // 
            this.buttonSelectAllTracks.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectAllTracks.Location = new System.Drawing.Point(3, 3);
            this.buttonSelectAllTracks.Name = "buttonSelectAllTracks";
            this.buttonSelectAllTracks.Size = new System.Drawing.Size(90, 23);
            this.buttonSelectAllTracks.TabIndex = 0;
            this.buttonSelectAllTracks.Text = "Include All";
            this.buttonSelectAllTracks.UseVisualStyleBackColor = true;
            this.buttonSelectAllTracks.Click += new System.EventHandler(this.buttonSelectAllTracks_Click);
            // 
            // buttonUnselectAllTracks
            // 
            this.buttonUnselectAllTracks.Location = new System.Drawing.Point(99, 3);
            this.buttonUnselectAllTracks.Name = "buttonUnselectAllTracks";
            this.buttonUnselectAllTracks.Size = new System.Drawing.Size(90, 23);
            this.buttonUnselectAllTracks.TabIndex = 1;
            this.buttonUnselectAllTracks.Text = "Include None";
            this.buttonUnselectAllTracks.UseVisualStyleBackColor = true;
            this.buttonUnselectAllTracks.Click += new System.EventHandler(this.buttonUnselectAllTracks_Click);
            // 
            // labelBottomButtonSeparator
            // 
            this.labelBottomButtonSeparator.Location = new System.Drawing.Point(195, 0);
            this.labelBottomButtonSeparator.Name = "labelBottomButtonSeparator";
            this.labelBottomButtonSeparator.Size = new System.Drawing.Size(32, 23);
            this.labelBottomButtonSeparator.TabIndex = 4;
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(233, 3);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(90, 23);
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonDone
            // 
            this.buttonDone.Location = new System.Drawing.Point(329, 3);
            this.buttonDone.Name = "buttonDone";
            this.buttonDone.Size = new System.Drawing.Size(90, 23);
            this.buttonDone.TabIndex = 1;
            this.buttonDone.Text = "Done";
            this.buttonDone.UseVisualStyleBackColor = true;
            this.buttonDone.Click += new System.EventHandler(this.buttonDone_Click);
            // 
            // buttonPlayToAudio
            // 
            this.buttonPlayToAudio.Location = new System.Drawing.Point(425, 3);
            this.buttonPlayToAudio.Name = "buttonPlayToAudio";
            this.buttonPlayToAudio.Size = new System.Drawing.Size(90, 23);
            this.buttonPlayToAudio.TabIndex = 2;
            this.buttonPlayToAudio.Text = "Play To Audio";
            this.buttonPlayToAudio.UseVisualStyleBackColor = true;
            this.buttonPlayToAudio.Click += new System.EventHandler(this.buttonPlayToAudio_Click);
            // 
            // buttonPlayToDisk
            // 
            this.buttonPlayToDisk.Location = new System.Drawing.Point(521, 3);
            this.buttonPlayToDisk.Name = "buttonPlayToDisk";
            this.buttonPlayToDisk.Size = new System.Drawing.Size(90, 23);
            this.buttonPlayToDisk.TabIndex = 3;
            this.buttonPlayToDisk.Text = "Play To Disk...";
            this.buttonPlayToDisk.UseVisualStyleBackColor = true;
            this.buttonPlayToDisk.Click += new System.EventHandler(this.buttonPlayToDisk_Click);
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.label11, 3);
            this.label11.Location = new System.Drawing.Point(372, 6);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(276, 13);
            this.label11.TabIndex = 23;
            this.label11.Text = "Output control parameters:";
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(13, 6);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(341, 13);
            this.label10.TabIndex = 22;
            this.label10.Text = "Tracks to include in playback:";
            // 
            // checkBoxClipWarning
            // 
            this.checkBoxClipWarning.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxClipWarning.AutoSize = true;
            this.checkBoxClipWarning.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.sourceBindingSource, "ClipWarning", true));
            this.checkBoxClipWarning.Location = new System.Drawing.Point(519, 266);
            this.checkBoxClipWarning.Name = "checkBoxClipWarning";
            this.checkBoxClipWarning.Size = new System.Drawing.Size(129, 17);
            this.checkBoxClipWarning.TabIndex = 21;
            this.checkBoxClipWarning.Text = "Warn On Clipping";
            this.checkBoxClipWarning.UseVisualStyleBackColor = true;
            // 
            // comboBoxNumChannels
            // 
            this.comboBoxNumChannels.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxNumChannels.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "NumChannelsAsString", true));
            this.comboBoxNumChannels.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNumChannels.FormattingEnabled = true;
            this.comboBoxNumChannels.Location = new System.Drawing.Point(519, 238);
            this.comboBoxNumChannels.Name = "comboBoxNumChannels";
            this.comboBoxNumChannels.Size = new System.Drawing.Size(129, 21);
            this.comboBoxNumChannels.TabIndex = 20;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(372, 242);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(129, 13);
            this.label9.TabIndex = 19;
            this.label9.Text = "Channels:";
            // 
            // comboBoxBitDepth
            // 
            this.comboBoxBitDepth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxBitDepth.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "OutputNumBitsAsString", true));
            this.comboBoxBitDepth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBitDepth.FormattingEnabled = true;
            this.comboBoxBitDepth.Location = new System.Drawing.Point(519, 211);
            this.comboBoxBitDepth.Name = "comboBoxBitDepth";
            this.comboBoxBitDepth.Size = new System.Drawing.Size(129, 21);
            this.comboBoxBitDepth.TabIndex = 18;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(372, 215);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(129, 13);
            this.label8.TabIndex = 17;
            this.label8.Text = "Bit Depth:";
            // 
            // textBoxOversampling
            // 
            this.textBoxOversampling.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOversampling.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "Oversampling", true));
            this.textBoxOversampling.Location = new System.Drawing.Point(519, 185);
            this.textBoxOversampling.Name = "textBoxOversampling";
            this.textBoxOversampling.Size = new System.Drawing.Size(129, 20);
            this.textBoxOversampling.TabIndex = 16;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(372, 188);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(129, 13);
            this.label7.TabIndex = 15;
            this.label7.Text = "Oversampling:";
            // 
            // textBoxBufferSeconds
            // 
            this.textBoxBufferSeconds.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxBufferSeconds.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "BufferDuration", true));
            this.textBoxBufferSeconds.Location = new System.Drawing.Point(519, 159);
            this.textBoxBufferSeconds.Name = "textBoxBufferSeconds";
            this.textBoxBufferSeconds.Size = new System.Drawing.Size(129, 20);
            this.textBoxBufferSeconds.TabIndex = 14;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(372, 162);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(129, 13);
            this.label6.TabIndex = 13;
            this.label6.Text = "Buffer Seconds:";
            // 
            // textBoxScanningGapSeconds
            // 
            this.textBoxScanningGapSeconds.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxScanningGapSeconds.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "ScanningGap", true));
            this.textBoxScanningGapSeconds.Location = new System.Drawing.Point(519, 133);
            this.textBoxScanningGapSeconds.Name = "textBoxScanningGapSeconds";
            this.textBoxScanningGapSeconds.Size = new System.Drawing.Size(129, 20);
            this.textBoxScanningGapSeconds.TabIndex = 12;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(372, 136);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(129, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Scanning Gap:";
            // 
            // textBoxInverseVolume
            // 
            this.textBoxInverseVolume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInverseVolume.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "OverallVolumeScalingFactor", true));
            this.textBoxInverseVolume.Location = new System.Drawing.Point(519, 107);
            this.textBoxInverseVolume.Name = "textBoxInverseVolume";
            this.textBoxInverseVolume.Size = new System.Drawing.Size(129, 20);
            this.textBoxInverseVolume.TabIndex = 10;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(372, 110);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(129, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Inverse Volume:";
            // 
            // textBoxBeatsPerMinute
            // 
            this.textBoxBeatsPerMinute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxBeatsPerMinute.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "DefaultBeatsPerMinute", true));
            this.textBoxBeatsPerMinute.Location = new System.Drawing.Point(519, 81);
            this.textBoxBeatsPerMinute.Name = "textBoxBeatsPerMinute";
            this.textBoxBeatsPerMinute.Size = new System.Drawing.Size(129, 20);
            this.textBoxBeatsPerMinute.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(372, 84);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(129, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Beats Per Minute:";
            // 
            // textBoxEnvelopeRate
            // 
            this.textBoxEnvelopeRate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxEnvelopeRate.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "EnvelopeUpdateRate", true));
            this.textBoxEnvelopeRate.Location = new System.Drawing.Point(519, 55);
            this.textBoxEnvelopeRate.Name = "textBoxEnvelopeRate";
            this.textBoxEnvelopeRate.Size = new System.Drawing.Size(129, 20);
            this.textBoxEnvelopeRate.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(372, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(129, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Envelope Rate:";
            // 
            // textBoxSamplingRate
            // 
            this.textBoxSamplingRate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSamplingRate.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "SamplingRate", true));
            this.textBoxSamplingRate.Location = new System.Drawing.Point(519, 29);
            this.textBoxSamplingRate.Name = "textBoxSamplingRate";
            this.textBoxSamplingRate.Size = new System.Drawing.Size(129, 20);
            this.textBoxSamplingRate.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(372, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(129, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Sampling Rate:";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 7;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 56.20438F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 12F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 21.89781F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 12F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 21.89781F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 12F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxSamplingRate, 5, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.textBoxEnvelopeRate, 5, 2);
            this.tableLayoutPanel1.Controls.Add(this.label3, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.textBoxBeatsPerMinute, 5, 3);
            this.tableLayoutPanel1.Controls.Add(this.label4, 3, 4);
            this.tableLayoutPanel1.Controls.Add(this.textBoxInverseVolume, 5, 4);
            this.tableLayoutPanel1.Controls.Add(this.label5, 3, 5);
            this.tableLayoutPanel1.Controls.Add(this.textBoxScanningGapSeconds, 5, 5);
            this.tableLayoutPanel1.Controls.Add(this.label6, 3, 6);
            this.tableLayoutPanel1.Controls.Add(this.textBoxBufferSeconds, 5, 6);
            this.tableLayoutPanel1.Controls.Add(this.label7, 3, 7);
            this.tableLayoutPanel1.Controls.Add(this.textBoxOversampling, 5, 7);
            this.tableLayoutPanel1.Controls.Add(this.label8, 3, 8);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxBitDepth, 5, 8);
            this.tableLayoutPanel1.Controls.Add(this.label9, 3, 9);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxNumChannels, 5, 9);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxClipWarning, 5, 10);
            this.tableLayoutPanel1.Controls.Add(this.label10, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label11, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 14);
            this.tableLayoutPanel1.Controls.Add(this.listBoxIncludedTracks, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxShowSummary, 3, 10);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxDeterministic, 3, 11);
            this.tableLayoutPanel1.Controls.Add(this.textBoxSeed, 5, 12);
            this.tableLayoutPanel1.Controls.Add(this.label12, 3, 12);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 16;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 5F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(665, 385);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // listBoxIncludedTracks
            // 
            this.listBoxIncludedTracks.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.listBoxIncludedTracks.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listBoxIncludedTracks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxIncludedTracks.Location = new System.Drawing.Point(13, 29);
            this.listBoxIncludedTracks.Multiselect = true;
            this.listBoxIncludedTracks.Name = "listBoxIncludedTracks";
            this.tableLayoutPanel1.SetRowSpan(this.listBoxIncludedTracks, 13);
            this.listBoxIncludedTracks.Size = new System.Drawing.Size(341, 313);
            this.listBoxIncludedTracks.TabIndex = 26;
            // 
            // checkBoxShowSummary
            // 
            this.checkBoxShowSummary.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxShowSummary.AutoSize = true;
            this.checkBoxShowSummary.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.sourceBindingSource, "ShowSummary", true));
            this.checkBoxShowSummary.Location = new System.Drawing.Point(372, 266);
            this.checkBoxShowSummary.Name = "checkBoxShowSummary";
            this.checkBoxShowSummary.Size = new System.Drawing.Size(129, 17);
            this.checkBoxShowSummary.TabIndex = 28;
            this.checkBoxShowSummary.Text = "Show Summary";
            this.checkBoxShowSummary.UseVisualStyleBackColor = true;
            // 
            // checkBoxDeterministic
            // 
            this.checkBoxDeterministic.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxDeterministic.AutoSize = true;
            this.checkBoxDeterministic.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.sourceBindingSource, "Deterministic", true));
            this.checkBoxDeterministic.Location = new System.Drawing.Point(372, 292);
            this.checkBoxDeterministic.Name = "checkBoxDeterministic";
            this.checkBoxDeterministic.Size = new System.Drawing.Size(129, 17);
            this.checkBoxDeterministic.TabIndex = 29;
            this.checkBoxDeterministic.Text = "Deterministic";
            this.checkBoxDeterministic.UseVisualStyleBackColor = true;
            // 
            // textBoxSeed
            // 
            this.textBoxSeed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSeed.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.sourceBindingSource, "Seed", true));
            this.textBoxSeed.Enabled = false;
            this.textBoxSeed.Location = new System.Drawing.Point(519, 317);
            this.textBoxSeed.Name = "textBoxSeed";
            this.textBoxSeed.Size = new System.Drawing.Size(129, 20);
            this.textBoxSeed.TabIndex = 30;
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(372, 320);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(129, 13);
            this.label12.TabIndex = 31;
            this.label12.Text = "Seed:";
            // 
            // PlayPrefsDialog
            // 
            this.AcceptButton = this.buttonDone;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(665, 385);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "PlayPrefsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)(this.includedTracksBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sourceBindingSource)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.BindingSource sourceBindingSource;
        private System.Windows.Forms.BindingSource includedTracksBindingSource;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxSamplingRate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxEnvelopeRate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxBeatsPerMinute;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxInverseVolume;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxScanningGapSeconds;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxBufferSeconds;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBoxOversampling;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox comboBoxBitDepth;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox comboBoxNumChannels;
        private System.Windows.Forms.CheckBox checkBoxClipWarning;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button buttonSelectAllTracks;
        private System.Windows.Forms.Button buttonUnselectAllTracks;
        private System.Windows.Forms.Label labelBottomButtonSeparator;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonDone;
        private System.Windows.Forms.Button buttonPlayToAudio;
        private System.Windows.Forms.Button buttonPlayToDisk;
        private MyListBox listBoxIncludedTracks;
        private System.Windows.Forms.CheckBox checkBoxShowSummary;
        private System.Windows.Forms.CheckBox checkBoxDeterministic;
        private System.Windows.Forms.TextBox textBoxSeed;
        private System.Windows.Forms.Label label12;
    }
}