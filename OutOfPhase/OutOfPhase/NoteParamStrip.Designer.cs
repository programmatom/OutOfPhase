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
    partial class NoteParamStrip
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.textEditControl = new TextEditor.TextEditControl();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // textEditControl
            // 
            this.textEditControl.AcceptsTab = false;
            this.textEditControl.AutoScroll = true;
            this.textEditControl.AutoScrollMinSize = new System.Drawing.Size(1, 13);
            this.textEditControl.AutoSize = true;
            this.textEditControl.BackColor = System.Drawing.SystemColors.Window;
            this.textEditControl.Location = new System.Drawing.Point(0, 0);
            this.textEditControl.Margin = new System.Windows.Forms.Padding(0);
            this.textEditControl.MinimumSize = new System.Drawing.Size(7, 13);
            this.textEditControl.Name = "textEditControl";
            this.textEditControl.Size = new System.Drawing.Size(7, 13);
            this.textEditControl.TabIndex = 0;
            this.textEditControl.Visible = false;
            // 
            // NoteParamStrip
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.textEditControl);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "NoteParamStrip";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TextEditor.TextEditControl textEditControl;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
