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
    partial class GroupDefinitionsNewSchoolWindow
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
            this.myGridGroups = new OutOfPhase.MyGrid();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonNewGroup = new System.Windows.Forms.Button();
            this.buttonDeleteGroup = new System.Windows.Forms.Button();
            this.myLabel1 = new OutOfPhase.MyLabel();
            this.myLabel2 = new OutOfPhase.MyLabel();
            this.myGridTracks = new OutOfPhase.MyGrid();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.myGridGroups, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.myLabel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.myLabel2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.myGridTracks, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(646, 469);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // myGridGroups
            // 
            this.myGridGroups.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myGridGroups.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myGridGroups.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myGridGroups.Location = new System.Drawing.Point(3, 16);
            this.myGridGroups.Name = "myGridGroups";
            this.myGridGroups.PastEndColor = System.Drawing.SystemColors.Control;
            this.myGridGroups.Size = new System.Drawing.Size(317, 415);
            this.myGridGroups.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.buttonNewGroup);
            this.flowLayoutPanel1.Controls.Add(this.buttonDeleteGroup);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(80, 437);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(162, 29);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // buttonNewGroup
            // 
            this.buttonNewGroup.Location = new System.Drawing.Point(3, 3);
            this.buttonNewGroup.Name = "buttonNewGroup";
            this.buttonNewGroup.Size = new System.Drawing.Size(75, 23);
            this.buttonNewGroup.TabIndex = 0;
            this.buttonNewGroup.Text = "New Group";
            this.buttonNewGroup.UseVisualStyleBackColor = true;
            this.buttonNewGroup.Click += new System.EventHandler(this.buttonNewGroup_Click);
            // 
            // buttonDeleteGroup
            // 
            this.buttonDeleteGroup.Location = new System.Drawing.Point(84, 3);
            this.buttonDeleteGroup.Name = "buttonDeleteGroup";
            this.buttonDeleteGroup.Size = new System.Drawing.Size(75, 23);
            this.buttonDeleteGroup.TabIndex = 1;
            this.buttonDeleteGroup.Text = "Delete Group";
            this.buttonDeleteGroup.UseVisualStyleBackColor = true;
            this.buttonDeleteGroup.Click += new System.EventHandler(this.buttonDeleteGroup_Click);
            // 
            // myLabel1
            // 
            this.myLabel1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel1.AutoSize = true;
            this.myLabel1.Location = new System.Drawing.Point(3, 0);
            this.myLabel1.Name = "myLabel1";
            this.myLabel1.Size = new System.Drawing.Size(44, 13);
            this.myLabel1.TabIndex = 2;
            this.myLabel1.Text = "Groups:";
            // 
            // myLabel2
            // 
            this.myLabel2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.myLabel2.AutoSize = true;
            this.myLabel2.Location = new System.Drawing.Point(326, 0);
            this.myLabel2.Name = "myLabel2";
            this.myLabel2.Size = new System.Drawing.Size(53, 13);
            this.myLabel2.TabIndex = 3;
            this.myLabel2.Text = "Members:";
            // 
            // myGridTracks
            // 
            this.myGridTracks.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myGridTracks.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myGridTracks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myGridTracks.Location = new System.Drawing.Point(326, 16);
            this.myGridTracks.Name = "myGridTracks";
            this.myGridTracks.PastEndColor = System.Drawing.SystemColors.Control;
            this.tableLayoutPanel1.SetRowSpan(this.myGridTracks, 2);
            this.myGridTracks.Size = new System.Drawing.Size(317, 450);
            this.myGridTracks.TabIndex = 4;
            // 
            // GroupDefinitionsNewSchoolWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(646, 469);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "GroupDefinitionsNewSchoolWindow";
            this.Text = "GroupDefinitionsNewSchoolWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private MyGrid myGridGroups;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button buttonNewGroup;
        private System.Windows.Forms.Button buttonDeleteGroup;
        private MyLabel myLabel1;
        private MyLabel myLabel2;
        private MyGrid myGridTracks;
    }
}