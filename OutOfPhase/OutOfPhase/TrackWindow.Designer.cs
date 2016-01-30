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
    partial class TrackWindow
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
            this.menuStripManager = new OutOfPhase.MenuStripManager();
            this.trackEditControl = new OutOfPhase.TrackEditControl();
            this.dpiChangeHelper = new OutOfPhase.DpiChangeHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.menuStripManager, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.trackEditControl, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(918, 373);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // menuStripManager
            // 
            this.menuStripManager.AutoSize = true;
            this.menuStripManager.Location = new System.Drawing.Point(0, 0);
            this.menuStripManager.Margin = new System.Windows.Forms.Padding(0);
            this.menuStripManager.Name = "menuStripManager";
            this.menuStripManager.Size = new System.Drawing.Size(918, 24);
            this.menuStripManager.TabIndex = 7;
            // 
            // trackEditControl
            // 
            this.trackEditControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trackEditControl.Location = new System.Drawing.Point(0, 24);
            this.trackEditControl.Margin = new System.Windows.Forms.Padding(0);
            this.trackEditControl.Name = "trackEditControl";
            this.trackEditControl.Size = new System.Drawing.Size(918, 349);
            this.trackEditControl.TabIndex = 8;
            // 
            // dpiChangeHelper
            // 
            this.dpiChangeHelper.Form = this;
            // 
            // TrackWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(918, 373);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "TrackWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private MenuStripManager menuStripManager;
        private TrackEditControl trackEditControl;
        private DpiChangeHelper dpiChangeHelper;
    }
}
