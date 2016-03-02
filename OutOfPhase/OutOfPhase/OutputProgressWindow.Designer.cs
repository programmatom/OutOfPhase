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
    partial class OutputProgressWindow
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
            this.buttonStop = new System.Windows.Forms.Button();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.labelElapsedSongTime = new OutOfPhase.MyLabel();
            this.textElapsedAudioSeconds = new OutOfPhase.SimpleTextDisplayControl();
            this.labelTotalFrames = new OutOfPhase.MyLabel();
            this.textTotalFrames = new OutOfPhase.SimpleTextDisplayControl();
            this.labelTotalClippedPoints = new OutOfPhase.MyLabel();
            this.textTotalClippedPoints = new OutOfPhase.SimpleTextDisplayControl();
            this.labelBufferLoading = new OutOfPhase.MyLabel();
            this.myProgressBarBufferLoading = new OutOfPhase.MyProgressBar();
            this.labelBufferSeconds = new OutOfPhase.MyLabel();
            this.timerUpdate = new System.Windows.Forms.Timer(this.components);
            this.dpiChangeHelper = new OutOfPhase.DpiChangeHelper(this.components);
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonStop
            // 
            this.buttonStop.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonStop.Location = new System.Drawing.Point(184, 65);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 0;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.tableLayoutPanel2.Controls.Add(this.buttonStop, 1, 5);
            this.tableLayoutPanel2.Controls.Add(this.labelElapsedSongTime, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.textElapsedAudioSeconds, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.labelTotalFrames, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.textTotalFrames, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.labelTotalClippedPoints, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.textTotalClippedPoints, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.labelBufferLoading, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.myProgressBarBufferLoading, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.labelBufferSeconds, 2, 3);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.Padding = new System.Windows.Forms.Padding(5);
            this.tableLayoutPanel2.RowCount = 6;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 13F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 13F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 13F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 13F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 5F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(386, 98);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // labelElapsedSongTime
            // 
            this.labelElapsedSongTime.AutoSize = true;
            this.labelElapsedSongTime.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelElapsedSongTime.Location = new System.Drawing.Point(8, 5);
            this.labelElapsedSongTime.Name = "labelElapsedSongTime";
            this.labelElapsedSongTime.Size = new System.Drawing.Size(170, 13);
            this.labelElapsedSongTime.TabIndex = 0;
            this.labelElapsedSongTime.Text = "Elapsed Audio Time (Seconds):";
            // 
            // textElapsedAudioSeconds
            // 
            this.tableLayoutPanel2.SetColumnSpan(this.textElapsedAudioSeconds, 2);
            this.textElapsedAudioSeconds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textElapsedAudioSeconds.Location = new System.Drawing.Point(184, 5);
            this.textElapsedAudioSeconds.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.textElapsedAudioSeconds.Name = "textElapsedAudioSeconds";
            this.textElapsedAudioSeconds.Size = new System.Drawing.Size(194, 13);
            this.textElapsedAudioSeconds.TabIndex = 1;
            this.textElapsedAudioSeconds.Text = null;
            // 
            // labelTotalFrames
            // 
            this.labelTotalFrames.AutoSize = true;
            this.labelTotalFrames.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelTotalFrames.Location = new System.Drawing.Point(8, 18);
            this.labelTotalFrames.Name = "labelTotalFrames";
            this.labelTotalFrames.Size = new System.Drawing.Size(170, 13);
            this.labelTotalFrames.TabIndex = 2;
            this.labelTotalFrames.Text = "Total Generated Frames:";
            // 
            // textTotalFrames
            // 
            this.tableLayoutPanel2.SetColumnSpan(this.textTotalFrames, 2);
            this.textTotalFrames.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textTotalFrames.Location = new System.Drawing.Point(184, 18);
            this.textTotalFrames.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.textTotalFrames.Name = "textTotalFrames";
            this.textTotalFrames.Size = new System.Drawing.Size(194, 13);
            this.textTotalFrames.TabIndex = 3;
            this.textTotalFrames.Text = null;
            // 
            // labelTotalClippedPoints
            // 
            this.labelTotalClippedPoints.AutoSize = true;
            this.labelTotalClippedPoints.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelTotalClippedPoints.Location = new System.Drawing.Point(8, 31);
            this.labelTotalClippedPoints.Name = "labelTotalClippedPoints";
            this.labelTotalClippedPoints.Size = new System.Drawing.Size(170, 13);
            this.labelTotalClippedPoints.TabIndex = 4;
            this.labelTotalClippedPoints.Text = "Total Clipped Sample Points:";
            // 
            // textTotalClippedPoints
            // 
            this.tableLayoutPanel2.SetColumnSpan(this.textTotalClippedPoints, 2);
            this.textTotalClippedPoints.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textTotalClippedPoints.Location = new System.Drawing.Point(184, 31);
            this.textTotalClippedPoints.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.textTotalClippedPoints.Name = "textTotalClippedPoints";
            this.textTotalClippedPoints.Size = new System.Drawing.Size(194, 13);
            this.textTotalClippedPoints.TabIndex = 5;
            this.textTotalClippedPoints.Text = null;
            // 
            // labelBufferLoading
            // 
            this.labelBufferLoading.AutoSize = true;
            this.labelBufferLoading.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelBufferLoading.Location = new System.Drawing.Point(8, 44);
            this.labelBufferLoading.Name = "labelBufferLoading";
            this.labelBufferLoading.Size = new System.Drawing.Size(170, 13);
            this.labelBufferLoading.TabIndex = 6;
            this.labelBufferLoading.Text = "Buffer Loading:";
            // 
            // myProgressBarBufferLoading
            // 
            this.myProgressBarBufferLoading.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myProgressBarBufferLoading.CritThreshhold = 1F;
            this.myProgressBarBufferLoading.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myProgressBarBufferLoading.GoodColor = System.Drawing.Color.LimeGreen;
            this.myProgressBarBufferLoading.Level = 0F;
            this.myProgressBarBufferLoading.Location = new System.Drawing.Point(184, 47);
            this.myProgressBarBufferLoading.Maximum = 0F;
            this.myProgressBarBufferLoading.Name = "myProgressBarBufferLoading";
            this.myProgressBarBufferLoading.Size = new System.Drawing.Size(138, 7);
            this.myProgressBarBufferLoading.TabIndex = 7;
            // 
            // labelBufferSeconds
            // 
            this.labelBufferSeconds.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelBufferSeconds.AutoSize = true;
            this.labelBufferSeconds.Location = new System.Drawing.Point(328, 44);
            this.labelBufferSeconds.Name = "labelBufferSeconds";
            this.labelBufferSeconds.Size = new System.Drawing.Size(50, 13);
            this.labelBufferSeconds.TabIndex = 8;
            this.labelBufferSeconds.Text = "0.0 sec";
            // 
            // timerUpdate
            // 
            this.timerUpdate.Enabled = true;
            this.timerUpdate.Interval = 1000;
            this.timerUpdate.Tick += new System.EventHandler(this.timerUpdate_Tick);
            // 
            // dpiChangeHelper
            // 
            this.dpiChangeHelper.Form = this;
            // 
            // OutputProgressWindow
            // 
            this.AcceptButton = this.buttonStop;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.buttonStop;
            this.ClientSize = new System.Drawing.Size(386, 98);
            this.Controls.Add(this.tableLayoutPanel2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "OutputProgressWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Synthesizing";
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private MyLabel labelElapsedSongTime;
        private SimpleTextDisplayControl textElapsedAudioSeconds;
        private MyLabel labelTotalFrames;
        private SimpleTextDisplayControl textTotalFrames;
        private MyLabel labelTotalClippedPoints;
        private SimpleTextDisplayControl textTotalClippedPoints;
        private System.Windows.Forms.Timer timerUpdate;
        private MyLabel labelBufferLoading;
        private MyProgressBar myProgressBarBufferLoading;
        private MyLabel labelBufferSeconds;
        private DpiChangeHelper dpiChangeHelper;
    }
}
