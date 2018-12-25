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
    partial class PresetDefinitionNewSchoolWindow
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.myGridPresets = new OutOfPhase.MyGrid();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonNewPreset = new System.Windows.Forms.Button();
            this.buttonDeletePreset = new System.Windows.Forms.Button();
            this.myLabel1 = new OutOfPhase.MyLabel();
            this.myLabel2 = new OutOfPhase.MyLabel();
            this.myGridTargets = new OutOfPhase.MyGrid();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonNewTarget = new System.Windows.Forms.Button();
            this.buttonDeleteTarget = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.myGridPresets, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.myLabel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.myLabel2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.myGridTargets, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel2, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(627, 406);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // myGridPresets
            // 
            this.myGridPresets.AutoScroll = true;
            this.myGridPresets.AutoScrollMinSize = new System.Drawing.Size(1, 0);
            this.myGridPresets.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myGridPresets.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myGridPresets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myGridPresets.Location = new System.Drawing.Point(3, 16);
            this.myGridPresets.Name = "myGridPresets";
            this.myGridPresets.PastEndColor = System.Drawing.SystemColors.Control;
            this.myGridPresets.Size = new System.Drawing.Size(307, 352);
            this.myGridPresets.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.buttonNewPreset);
            this.flowLayoutPanel1.Controls.Add(this.buttonDeletePreset);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(75, 374);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(162, 29);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // buttonNewPreset
            // 
            this.buttonNewPreset.Location = new System.Drawing.Point(3, 3);
            this.buttonNewPreset.Name = "buttonNewPreset";
            this.buttonNewPreset.Size = new System.Drawing.Size(75, 23);
            this.buttonNewPreset.TabIndex = 0;
            this.buttonNewPreset.Text = "New Preset";
            this.buttonNewPreset.UseVisualStyleBackColor = true;
            this.buttonNewPreset.Click += new System.EventHandler(this.buttonNewPreset_Click);
            // 
            // buttonDeletePreset
            // 
            this.buttonDeletePreset.Location = new System.Drawing.Point(84, 3);
            this.buttonDeletePreset.Name = "buttonDeletePreset";
            this.buttonDeletePreset.Size = new System.Drawing.Size(75, 23);
            this.buttonDeletePreset.TabIndex = 1;
            this.buttonDeletePreset.Text = "Delete Preset";
            this.buttonDeletePreset.UseVisualStyleBackColor = true;
            this.buttonDeletePreset.Click += new System.EventHandler(this.buttonDeletePreset_Click);
            // 
            // myLabel1
            // 
            this.myLabel1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel1.AutoSize = true;
            this.myLabel1.Location = new System.Drawing.Point(3, 0);
            this.myLabel1.Name = "myLabel1";
            this.myLabel1.Size = new System.Drawing.Size(45, 13);
            this.myLabel1.TabIndex = 2;
            this.myLabel1.Text = "Presets:";
            // 
            // myLabel2
            // 
            this.myLabel2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel2.AutoSize = true;
            this.myLabel2.Location = new System.Drawing.Point(316, 0);
            this.myLabel2.Name = "myLabel2";
            this.myLabel2.Size = new System.Drawing.Size(53, 13);
            this.myLabel2.TabIndex = 3;
            this.myLabel2.Text = "Members:";
            // 
            // myGridTargets
            // 
            this.myGridTargets.AutoScroll = true;
            this.myGridTargets.AutoScrollMinSize = new System.Drawing.Size(1, 0);
            this.myGridTargets.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myGridTargets.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myGridTargets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myGridTargets.Location = new System.Drawing.Point(316, 16);
            this.myGridTargets.Name = "myGridTargets";
            this.myGridTargets.PastEndColor = System.Drawing.SystemColors.Control;
            this.myGridTargets.Size = new System.Drawing.Size(308, 352);
            this.myGridTargets.TabIndex = 4;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.Controls.Add(this.buttonNewTarget);
            this.flowLayoutPanel2.Controls.Add(this.buttonDeleteTarget);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(389, 374);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(162, 29);
            this.flowLayoutPanel2.TabIndex = 5;
            // 
            // buttonNewTarget
            // 
            this.buttonNewTarget.Location = new System.Drawing.Point(3, 3);
            this.buttonNewTarget.Name = "buttonNewTarget";
            this.buttonNewTarget.Size = new System.Drawing.Size(75, 23);
            this.buttonNewTarget.TabIndex = 1;
            this.buttonNewTarget.Text = "New Target";
            this.buttonNewTarget.UseVisualStyleBackColor = true;
            this.buttonNewTarget.Click += new System.EventHandler(this.buttonNewTarget_Click);
            // 
            // buttonDeleteTarget
            // 
            this.buttonDeleteTarget.Location = new System.Drawing.Point(84, 3);
            this.buttonDeleteTarget.Name = "buttonDeleteTarget";
            this.buttonDeleteTarget.Size = new System.Drawing.Size(75, 23);
            this.buttonDeleteTarget.TabIndex = 0;
            this.buttonDeleteTarget.Text = "Delete Target";
            this.buttonDeleteTarget.UseVisualStyleBackColor = true;
            this.buttonDeleteTarget.Click += new System.EventHandler(this.buttonDeleteTarget_Click);
            // 
            // PresetDefinitionNewSchoolWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(627, 406);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "PresetDefinitionNewSchoolWindow";
            this.Text = "PresetSequenceNewSchoolWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private MyGrid myGridPresets;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button buttonNewPreset;
        private System.Windows.Forms.Button buttonDeletePreset;
        private MyLabel myLabel1;
        private MyLabel myLabel2;
        private MyGrid myGridTargets;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button buttonNewTarget;
        private System.Windows.Forms.Button buttonDeleteTarget;
    }
}