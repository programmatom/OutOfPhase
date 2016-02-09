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
    partial class GlobalPrefsDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GlobalPrefsDialog));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label1 = new OutOfPhase.MyLabel();
            this.label2 = new OutOfPhase.MyLabel();
            this.label3 = new OutOfPhase.MyLabel();
            this.textBoxTabSize = new System.Windows.Forms.TextBox();
            this.globalPrefsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.checkBoxAutosaveEnabled = new System.Windows.Forms.CheckBox();
            this.textBoxAutosaveInterval = new System.Windows.Forms.TextBox();
            this.label4 = new OutOfPhase.MyLabel();
            this.comboBoxOutputDevice = new System.Windows.Forms.ComboBox();
            this.label5 = new OutOfPhase.MyLabel();
            this.comboBoxConcurrency = new System.Windows.Forms.ComboBox();
            this.textBoxConcurrency = new System.Windows.Forms.TextBox();
            this.label6 = new OutOfPhase.MyLabel();
            this.checkBoxAutoIndent = new System.Windows.Forms.CheckBox();
            this.label7 = new OutOfPhase.MyLabel();
            this.label8 = new OutOfPhase.MyLabel();
            this.label9 = new OutOfPhase.MyLabel();
            this.comboBoxPriority = new System.Windows.Forms.ComboBox();
            this.dpiChangeHelper = new OutOfPhase.DpiChangeHelper(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.globalPrefsBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 11);
            this.tableLayoutPanel1.Controls.Add(this.label1, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label3, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.textBoxTabSize, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxAutosaveEnabled, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.textBoxAutosaveInterval, 2, 4);
            this.tableLayoutPanel1.Controls.Add(this.label4, 1, 8);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxOutputDevice, 2, 8);
            this.tableLayoutPanel1.Controls.Add(this.label5, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxConcurrency, 2, 7);
            this.tableLayoutPanel1.Controls.Add(this.textBoxConcurrency, 3, 7);
            this.tableLayoutPanel1.Controls.Add(this.label6, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxAutoIndent, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label8, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.label9, 1, 9);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxPriority, 2, 9);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 12;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(378, 292);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 3);
            this.flowLayoutPanel1.Controls.Add(this.buttonOK);
            this.flowLayoutPanel1.Controls.Add(this.buttonCancel);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(85, 260);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(290, 29);
            this.flowLayoutPanel1.TabIndex = 0;
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
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(134, 3);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(125, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Tab Size:";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(23, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(144, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Autosave Enabled:";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 101);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(144, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Autosave Interval (Seconds):";
            // 
            // textBoxTabSize
            // 
            this.textBoxTabSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxTabSize.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.globalPrefsBindingSource, "TabSize", true));
            this.textBoxTabSize.Location = new System.Drawing.Point(173, 26);
            this.textBoxTabSize.Name = "textBoxTabSize";
            this.textBoxTabSize.Size = new System.Drawing.Size(136, 20);
            this.textBoxTabSize.TabIndex = 4;
            // 
            // globalPrefsBindingSource
            // 
            this.globalPrefsBindingSource.DataSource = typeof(OutOfPhase.GlobalPrefs);
            // 
            // checkBoxAutosaveEnabled
            // 
            this.checkBoxAutosaveEnabled.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxAutosaveEnabled.AutoSize = true;
            this.checkBoxAutosaveEnabled.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.globalPrefsBindingSource, "AutosaveEnabled", true));
            this.checkBoxAutosaveEnabled.Location = new System.Drawing.Point(173, 76);
            this.checkBoxAutosaveEnabled.Name = "checkBoxAutosaveEnabled";
            this.checkBoxAutosaveEnabled.Size = new System.Drawing.Size(136, 14);
            this.checkBoxAutosaveEnabled.TabIndex = 5;
            this.checkBoxAutosaveEnabled.UseVisualStyleBackColor = true;
            // 
            // textBoxAutosaveInterval
            // 
            this.textBoxAutosaveInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxAutosaveInterval.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.globalPrefsBindingSource, "AutosaveIntervalSeconds", true));
            this.textBoxAutosaveInterval.Location = new System.Drawing.Point(173, 98);
            this.textBoxAutosaveInterval.Name = "textBoxAutosaveInterval";
            this.textBoxAutosaveInterval.Size = new System.Drawing.Size(136, 20);
            this.textBoxAutosaveInterval.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 193);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(144, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Output Device:";
            // 
            // comboBoxOutputDevice
            // 
            this.comboBoxOutputDevice.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.comboBoxOutputDevice, 2);
            this.comboBoxOutputDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOutputDevice.FormattingEnabled = true;
            this.comboBoxOutputDevice.Location = new System.Drawing.Point(173, 189);
            this.comboBoxOutputDevice.Name = "comboBoxOutputDevice";
            this.comboBoxOutputDevice.Size = new System.Drawing.Size(202, 21);
            this.comboBoxOutputDevice.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(23, 166);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(144, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Concurrency:";
            // 
            // comboBoxConcurrency
            // 
            this.comboBoxConcurrency.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxConcurrency.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxConcurrency.Items.AddRange(new object[] {
            "Default",
            "Single Processor",
            "Multiple Processors",
            "Reserve Processors"});
            this.comboBoxConcurrency.Location = new System.Drawing.Point(173, 162);
            this.comboBoxConcurrency.Name = "comboBoxConcurrency";
            this.comboBoxConcurrency.Size = new System.Drawing.Size(136, 21);
            this.comboBoxConcurrency.TabIndex = 10;
            this.comboBoxConcurrency.SelectedIndexChanged += new System.EventHandler(this.comboBoxConcurrency_SelectedIndexChanged);
            // 
            // textBoxConcurrency
            // 
            this.textBoxConcurrency.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxConcurrency.Location = new System.Drawing.Point(315, 162);
            this.textBoxConcurrency.MinimumSize = new System.Drawing.Size(60, 4);
            this.textBoxConcurrency.Name = "textBoxConcurrency";
            this.textBoxConcurrency.Size = new System.Drawing.Size(60, 20);
            this.textBoxConcurrency.TabIndex = 11;
            this.textBoxConcurrency.TextChanged += new System.EventHandler(this.textBoxConcurrency_TextChanged);
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(23, 54);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(144, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Auto-Indent:";
            // 
            // checkBoxAutoIndent
            // 
            this.checkBoxAutoIndent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxAutoIndent.AutoSize = true;
            this.checkBoxAutoIndent.DataBindings.Add(new System.Windows.Forms.Binding("CheckState", this.globalPrefsBindingSource, "AutoIndent", true));
            this.checkBoxAutoIndent.Location = new System.Drawing.Point(173, 53);
            this.checkBoxAutoIndent.Name = "checkBoxAutoIndent";
            this.checkBoxAutoIndent.Size = new System.Drawing.Size(136, 14);
            this.checkBoxAutoIndent.TabIndex = 13;
            this.checkBoxAutoIndent.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.label7, 4);
            this.label7.Location = new System.Drawing.Point(3, 5);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(372, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Editing:";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.label8, 4);
            this.label8.Location = new System.Drawing.Point(3, 141);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(372, 13);
            this.label8.TabIndex = 15;
            this.label8.Text = "Synthesis Engine:";
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(23, 220);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(144, 13);
            this.label9.TabIndex = 16;
            this.label9.Text = "Priority:";
            // 
            // comboBoxPriority
            // 
            this.comboBoxPriority.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPriority.FormattingEnabled = true;
            this.comboBoxPriority.Location = new System.Drawing.Point(173, 216);
            this.comboBoxPriority.Name = "comboBoxPriority";
            this.comboBoxPriority.Size = new System.Drawing.Size(136, 21);
            this.comboBoxPriority.TabIndex = 17;
            // 
            // dpiChangeHelper
            // 
            this.dpiChangeHelper.Form = this;
            // 
            // GlobalPrefsDialog
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(378, 292);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GlobalPrefsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Preferences";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.globalPrefsBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private MyLabel label1;
        private MyLabel label2;
        private MyLabel label3;
        private System.Windows.Forms.TextBox textBoxTabSize;
        private System.Windows.Forms.CheckBox checkBoxAutosaveEnabled;
        private System.Windows.Forms.TextBox textBoxAutosaveInterval;
        private MyLabel label4;
        private System.Windows.Forms.ComboBox comboBoxOutputDevice;
        private MyLabel label5;
        private System.Windows.Forms.ComboBox comboBoxConcurrency;
        private System.Windows.Forms.TextBox textBoxConcurrency;
        private MyLabel label6;
        private System.Windows.Forms.CheckBox checkBoxAutoIndent;
        private DpiChangeHelper dpiChangeHelper;
        private System.Windows.Forms.BindingSource globalPrefsBindingSource;
        private System.Windows.Forms.ComboBox comboBoxPriority;
        private MyLabel label7;
        private MyLabel label8;
        private MyLabel label9;
    }
}