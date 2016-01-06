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
    partial class MainWindow
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
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.myListBoxTracks = new OutOfPhase.MyListBox();
            this.myListBoxWaveTables = new OutOfPhase.MyListBox();
            this.myListBoxSamples = new OutOfPhase.MyListBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.myListBoxInstruments = new OutOfPhase.MyListBox();
            this.myListBoxAlgoWaveTables = new OutOfPhase.MyListBox();
            this.myListBoxAlgoSamples = new OutOfPhase.MyListBox();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.myListBoxFunctions = new OutOfPhase.MyListBox();
            this.textEditComment = new TextEditor.TextEditControl();
            this.documentBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.stringStorageFactory = new TextEditor.StringStorageFactory();
            this.menuStripManager = new OutOfPhase.MenuStripManager();
            this.trackListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.waveTableListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.sampleListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.instrumentListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.algoWaveTableListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.algoSampListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.functionListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.textEditorWindowHelper = new TextEditor.TextEditorWindowHelper(this.components);
            this.timerAutosave = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackListBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.waveTableListBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleListBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.instrumentListBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.algoWaveTableListBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.algoSampListBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.functionListBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel5, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.menuStripManager, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33332F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(673, 419);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label2, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label3, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.myListBoxTracks, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.myListBoxWaveTables, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.myListBoxSamples, 2, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 33);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(667, 123);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(216, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Tracks:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(225, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(216, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Wave Tables:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(447, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(217, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Samples:";
            // 
            // myListBoxTracks
            // 
            this.myListBoxTracks.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myListBoxTracks.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myListBoxTracks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myListBoxTracks.Location = new System.Drawing.Point(3, 16);
            this.myListBoxTracks.Name = "myListBoxTracks";
            this.myListBoxTracks.Size = new System.Drawing.Size(216, 104);
            this.myListBoxTracks.TabIndex = 1;
            // 
            // myListBoxWaveTables
            // 
            this.myListBoxWaveTables.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myListBoxWaveTables.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myListBoxWaveTables.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myListBoxWaveTables.Location = new System.Drawing.Point(225, 16);
            this.myListBoxWaveTables.Name = "myListBoxWaveTables";
            this.myListBoxWaveTables.Size = new System.Drawing.Size(216, 104);
            this.myListBoxWaveTables.TabIndex = 2;
            // 
            // myListBoxSamples
            // 
            this.myListBoxSamples.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myListBoxSamples.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myListBoxSamples.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myListBoxSamples.Location = new System.Drawing.Point(447, 16);
            this.myListBoxSamples.Name = "myListBoxSamples";
            this.myListBoxSamples.Size = new System.Drawing.Size(217, 104);
            this.myListBoxSamples.TabIndex = 3;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.Controls.Add(this.label4, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.label5, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label6, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.myListBoxInstruments, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.myListBoxAlgoWaveTables, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.myListBoxAlgoSamples, 2, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 162);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(667, 123);
            this.tableLayoutPanel3.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(216, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Instruments:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(225, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(216, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Algorithmic Wave Tables:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(447, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(217, 13);
            this.label6.TabIndex = 2;
            this.label6.Text = "Algorithmic Samples:";
            // 
            // myListBoxInstruments
            // 
            this.myListBoxInstruments.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myListBoxInstruments.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myListBoxInstruments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myListBoxInstruments.Location = new System.Drawing.Point(3, 16);
            this.myListBoxInstruments.Name = "myListBoxInstruments";
            this.myListBoxInstruments.Size = new System.Drawing.Size(216, 104);
            this.myListBoxInstruments.TabIndex = 4;
            // 
            // myListBoxAlgoWaveTables
            // 
            this.myListBoxAlgoWaveTables.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myListBoxAlgoWaveTables.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myListBoxAlgoWaveTables.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myListBoxAlgoWaveTables.Location = new System.Drawing.Point(225, 16);
            this.myListBoxAlgoWaveTables.Name = "myListBoxAlgoWaveTables";
            this.myListBoxAlgoWaveTables.Size = new System.Drawing.Size(216, 104);
            this.myListBoxAlgoWaveTables.TabIndex = 5;
            // 
            // myListBoxAlgoSamples
            // 
            this.myListBoxAlgoSamples.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myListBoxAlgoSamples.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myListBoxAlgoSamples.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myListBoxAlgoSamples.Location = new System.Drawing.Point(447, 16);
            this.myListBoxAlgoSamples.Name = "myListBoxAlgoSamples";
            this.myListBoxAlgoSamples.Size = new System.Drawing.Size(217, 104);
            this.myListBoxAlgoSamples.TabIndex = 6;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 2;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.67F));
            this.tableLayoutPanel5.Controls.Add(this.label10, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.label11, 1, 0);
            this.tableLayoutPanel5.Controls.Add(this.myListBoxFunctions, 0, 1);
            this.tableLayoutPanel5.Controls.Add(this.textEditComment, 1, 1);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 291);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 2;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(667, 125);
            this.tableLayoutPanel5.TabIndex = 3;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label10.Location = new System.Drawing.Point(3, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(216, 13);
            this.label10.TabIndex = 0;
            this.label10.Text = "Functions:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label11.Location = new System.Drawing.Point(225, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(439, 13);
            this.label11.TabIndex = 1;
            this.label11.Text = "Comments:";
            // 
            // myListBoxFunctions
            // 
            this.myListBoxFunctions.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.myListBoxFunctions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.myListBoxFunctions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.myListBoxFunctions.Location = new System.Drawing.Point(3, 16);
            this.myListBoxFunctions.Name = "myListBoxFunctions";
            this.myListBoxFunctions.Size = new System.Drawing.Size(216, 106);
            this.myListBoxFunctions.TabIndex = 7;
            // 
            // textEditComment
            // 
            this.textEditComment.AutoScroll = true;
            this.textEditComment.AutoScrollMinSize = new System.Drawing.Size(437, 13);
            this.textEditComment.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textEditComment.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textEditComment.DataBindings.Add(new System.Windows.Forms.Binding("TabSize", this.documentBindingSource, "TabSize", true));
            this.textEditComment.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.documentBindingSource, "CommentInfo", true));
            this.textEditComment.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textEditComment.Location = new System.Drawing.Point(225, 16);
            this.textEditComment.Name = "textEditComment";
            this.textEditComment.Size = new System.Drawing.Size(439, 106);
            this.textEditComment.TabIndex = 8;
            this.textEditComment.TextService = TextEditor.TextService.Uniscribe;
            this.textEditComment.TextStorageFactory = this.stringStorageFactory;
            // 
            // documentBindingSource
            // 
            this.documentBindingSource.DataSource = typeof(OutOfPhase.Document);
            // 
            // menuStripManager
            // 
            this.menuStripManager.AutoSize = true;
            this.menuStripManager.Location = new System.Drawing.Point(3, 3);
            this.menuStripManager.Name = "menuStripManager";
            this.menuStripManager.Size = new System.Drawing.Size(667, 24);
            this.menuStripManager.TabIndex = 4;
            // 
            // trackListBindingSource
            // 
            this.trackListBindingSource.DataMember = "TrackList";
            this.trackListBindingSource.DataSource = this.documentBindingSource;
            // 
            // waveTableListBindingSource
            // 
            this.waveTableListBindingSource.DataMember = "WaveTableList";
            this.waveTableListBindingSource.DataSource = this.documentBindingSource;
            // 
            // sampleListBindingSource
            // 
            this.sampleListBindingSource.DataMember = "SampleList";
            this.sampleListBindingSource.DataSource = this.documentBindingSource;
            // 
            // instrumentListBindingSource
            // 
            this.instrumentListBindingSource.DataMember = "InstrumentList";
            this.instrumentListBindingSource.DataSource = this.documentBindingSource;
            // 
            // algoWaveTableListBindingSource
            // 
            this.algoWaveTableListBindingSource.DataMember = "AlgoWaveTableList";
            this.algoWaveTableListBindingSource.DataSource = this.documentBindingSource;
            // 
            // algoSampListBindingSource
            // 
            this.algoSampListBindingSource.DataMember = "AlgoSampList";
            this.algoSampListBindingSource.DataSource = this.documentBindingSource;
            // 
            // functionListBindingSource
            // 
            this.functionListBindingSource.DataMember = "FunctionList";
            this.functionListBindingSource.DataSource = this.documentBindingSource;
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
            this.textEditorWindowHelper.TextEditControl = this.textEditComment;
            this.textEditorWindowHelper.TrimTrailingSpacesToolStripMenuItem = null;
            this.textEditorWindowHelper.UndoToolStripMenuItem = null;
            // 
            // timerAutosave
            // 
            this.timerAutosave.Enabled = true;
            this.timerAutosave.Interval = 5000;
            this.timerAutosave.Tick += new System.EventHandler(this.timerAutosave_Tick);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(673, 419);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "MainWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.documentBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackListBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.waveTableListBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleListBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.instrumentListBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.algoWaveTableListBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.algoSampListBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.functionListBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.BindingSource documentBindingSource;
        private System.Windows.Forms.BindingSource sampleListBindingSource;
        private System.Windows.Forms.BindingSource functionListBindingSource;
        private System.Windows.Forms.BindingSource trackListBindingSource;
        private System.Windows.Forms.BindingSource waveTableListBindingSource;
        private System.Windows.Forms.BindingSource algoWaveTableListBindingSource;
        private System.Windows.Forms.BindingSource algoSampListBindingSource;
        private System.Windows.Forms.BindingSource instrumentListBindingSource;
        private MyListBox myListBoxFunctions;
        private MyListBox myListBoxTracks;
        private MyListBox myListBoxWaveTables;
        private MyListBox myListBoxSamples;
        private MyListBox myListBoxInstruments;
        private MyListBox myListBoxAlgoWaveTables;
        private MyListBox myListBoxAlgoSamples;
        private MenuStripManager menuStripManager;
        private TextEditor.TextEditControl textEditComment;
        private TextEditor.TextEditorWindowHelper textEditorWindowHelper;
        private TextEditor.StringStorageFactory stringStorageFactory;
        private System.Windows.Forms.Timer timerAutosave;
    }
}
