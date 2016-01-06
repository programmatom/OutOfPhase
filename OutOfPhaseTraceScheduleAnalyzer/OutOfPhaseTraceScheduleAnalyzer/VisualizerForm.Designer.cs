/*
 *  Copyright © 2015-2016 Thomas R. Lawrence
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
namespace OutOfPhaseTraceScheduleAnalyzer
{
    partial class VisualizerForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VisualizerForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dataGridViewEpochs = new System.Windows.Forms.DataGridView();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.dataGridViewDefinitions = new System.Windows.Forms.DataGridView();
            this.idDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.kindAsStringDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SectionIdAsString = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.InputsAsString = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.definitionBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.level2DetailView = new OutOfPhaseTraceScheduleAnalyzer.Level2DetailView();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.scheduleViewEpoch = new OutOfPhaseTraceScheduleAnalyzer.ScheduleView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.labelZoom = new System.Windows.Forms.ToolStripLabel();
            this.buttonZoomIn = new System.Windows.Forms.ToolStripButton();
            this.buttonZoomOut = new System.Windows.Forms.ToolStripButton();
            this.buttonResetZoom = new System.Windows.Forms.ToolStripButton();
            this.checkBoxFixed = new System.Windows.Forms.ToolStripButton();
            this.textBoxFixed = new System.Windows.Forms.ToolStripTextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToEnvelopeTickToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToOffsetSecondsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToOffsetframesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.showOffsetInSecondsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showOffsetInFramesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.showEventsViewerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.epochBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.showTotalDenormalsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.EnvelopeTick = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FrameCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FrameBaseTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FrameBase = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Duration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Denormals = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewEpochs)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDefinitions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.definitionBindingSource)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.epochBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(3, 27);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dataGridViewEpochs);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(823, 420);
            this.splitContainer1.SplitterDistance = 206;
            this.splitContainer1.TabIndex = 0;
            // 
            // dataGridViewEpochs
            // 
            this.dataGridViewEpochs.AllowUserToAddRows = false;
            this.dataGridViewEpochs.AllowUserToDeleteRows = false;
            this.dataGridViewEpochs.AllowUserToResizeRows = false;
            this.dataGridViewEpochs.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dataGridViewEpochs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewEpochs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.EnvelopeTick,
            this.FrameCount,
            this.FrameBaseTime,
            this.FrameBase,
            this.Duration,
            this.Denormals});
            this.dataGridViewEpochs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewEpochs.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewEpochs.MultiSelect = false;
            this.dataGridViewEpochs.Name = "dataGridViewEpochs";
            this.dataGridViewEpochs.ReadOnly = true;
            this.dataGridViewEpochs.Size = new System.Drawing.Size(206, 420);
            this.dataGridViewEpochs.TabIndex = 0;
            this.dataGridViewEpochs.VirtualMode = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer3);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.tableLayoutPanel2);
            this.splitContainer2.Size = new System.Drawing.Size(613, 420);
            this.splitContainer2.SplitterDistance = 81;
            this.splitContainer2.TabIndex = 0;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.dataGridViewDefinitions);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.level2DetailView);
            this.splitContainer3.Size = new System.Drawing.Size(613, 81);
            this.splitContainer3.SplitterDistance = 460;
            this.splitContainer3.TabIndex = 2;
            // 
            // dataGridViewDefinitions
            // 
            this.dataGridViewDefinitions.AllowUserToAddRows = false;
            this.dataGridViewDefinitions.AllowUserToDeleteRows = false;
            this.dataGridViewDefinitions.AllowUserToResizeRows = false;
            this.dataGridViewDefinitions.AutoGenerateColumns = false;
            this.dataGridViewDefinitions.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dataGridViewDefinitions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewDefinitions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.idDataGridViewTextBoxColumn,
            this.nameDataGridViewTextBoxColumn,
            this.kindAsStringDataGridViewTextBoxColumn,
            this.SectionIdAsString,
            this.InputsAsString});
            this.dataGridViewDefinitions.DataSource = this.definitionBindingSource;
            this.dataGridViewDefinitions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewDefinitions.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewDefinitions.MultiSelect = false;
            this.dataGridViewDefinitions.Name = "dataGridViewDefinitions";
            this.dataGridViewDefinitions.ReadOnly = true;
            this.dataGridViewDefinitions.Size = new System.Drawing.Size(460, 81);
            this.dataGridViewDefinitions.TabIndex = 0;
            // 
            // idDataGridViewTextBoxColumn
            // 
            this.idDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.idDataGridViewTextBoxColumn.DataPropertyName = "Id";
            this.idDataGridViewTextBoxColumn.HeaderText = "Id";
            this.idDataGridViewTextBoxColumn.Name = "idDataGridViewTextBoxColumn";
            this.idDataGridViewTextBoxColumn.ReadOnly = true;
            this.idDataGridViewTextBoxColumn.Width = 41;
            // 
            // nameDataGridViewTextBoxColumn
            // 
            this.nameDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.nameDataGridViewTextBoxColumn.DataPropertyName = "Name";
            this.nameDataGridViewTextBoxColumn.HeaderText = "Name";
            this.nameDataGridViewTextBoxColumn.Name = "nameDataGridViewTextBoxColumn";
            this.nameDataGridViewTextBoxColumn.ReadOnly = true;
            this.nameDataGridViewTextBoxColumn.Width = 60;
            // 
            // kindAsStringDataGridViewTextBoxColumn
            // 
            this.kindAsStringDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.kindAsStringDataGridViewTextBoxColumn.DataPropertyName = "KindAsString";
            this.kindAsStringDataGridViewTextBoxColumn.HeaderText = "Kind";
            this.kindAsStringDataGridViewTextBoxColumn.Name = "kindAsStringDataGridViewTextBoxColumn";
            this.kindAsStringDataGridViewTextBoxColumn.ReadOnly = true;
            this.kindAsStringDataGridViewTextBoxColumn.Width = 53;
            // 
            // SectionIdAsString
            // 
            this.SectionIdAsString.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.SectionIdAsString.DataPropertyName = "SectionIdAsString";
            this.SectionIdAsString.HeaderText = "Section";
            this.SectionIdAsString.Name = "SectionIdAsString";
            this.SectionIdAsString.ReadOnly = true;
            this.SectionIdAsString.Width = 68;
            // 
            // InputsAsString
            // 
            this.InputsAsString.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.InputsAsString.DataPropertyName = "InputsAsString";
            this.InputsAsString.HeaderText = "Inputs";
            this.InputsAsString.Name = "InputsAsString";
            this.InputsAsString.ReadOnly = true;
            this.InputsAsString.Width = 61;
            // 
            // definitionBindingSource
            // 
            this.definitionBindingSource.DataSource = typeof(OutOfPhaseTraceScheduleAnalyzer.Definition);
            // 
            // level2DetailView
            // 
            this.level2DetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.level2DetailView.Location = new System.Drawing.Point(0, 0);
            this.level2DetailView.Name = "level2DetailView";
            this.level2DetailView.Size = new System.Drawing.Size(149, 81);
            this.level2DetailView.TabIndex = 1;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.scheduleViewEpoch, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.toolStrip1, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(613, 335);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // scheduleViewEpoch
            // 
            this.scheduleViewEpoch.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.scheduleViewEpoch.AutoScroll = true;
            this.scheduleViewEpoch.AutoScrollMinSize = new System.Drawing.Size(607, 0);
            this.scheduleViewEpoch.Location = new System.Drawing.Point(3, 28);
            this.scheduleViewEpoch.Name = "scheduleViewEpoch";
            this.scheduleViewEpoch.Size = new System.Drawing.Size(607, 304);
            this.scheduleViewEpoch.TabIndex = 0;
            this.scheduleViewEpoch.Zoom = 1F;
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.labelZoom,
            this.buttonZoomIn,
            this.buttonZoomOut,
            this.buttonResetZoom,
            this.checkBoxFixed,
            this.textBoxFixed});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(613, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // labelZoom
            // 
            this.labelZoom.AutoSize = false;
            this.labelZoom.Name = "labelZoom";
            this.labelZoom.Size = new System.Drawing.Size(75, 22);
            // 
            // buttonZoomIn
            // 
            this.buttonZoomIn.AutoSize = false;
            this.buttonZoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.buttonZoomIn.Image = ((System.Drawing.Image)(resources.GetObject("buttonZoomIn.Image")));
            this.buttonZoomIn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.buttonZoomIn.Name = "buttonZoomIn";
            this.buttonZoomIn.Size = new System.Drawing.Size(35, 22);
            this.buttonZoomIn.Text = "In";
            this.buttonZoomIn.Click += new System.EventHandler(this.buttonZoomIn_Click);
            // 
            // buttonZoomOut
            // 
            this.buttonZoomOut.AutoSize = false;
            this.buttonZoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.buttonZoomOut.Image = ((System.Drawing.Image)(resources.GetObject("buttonZoomOut.Image")));
            this.buttonZoomOut.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.buttonZoomOut.Name = "buttonZoomOut";
            this.buttonZoomOut.Size = new System.Drawing.Size(35, 22);
            this.buttonZoomOut.Text = "Out";
            this.buttonZoomOut.Click += new System.EventHandler(this.buttonZoomOut_Click);
            // 
            // buttonResetZoom
            // 
            this.buttonResetZoom.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.buttonResetZoom.Image = ((System.Drawing.Image)(resources.GetObject("buttonResetZoom.Image")));
            this.buttonResetZoom.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.buttonResetZoom.Name = "buttonResetZoom";
            this.buttonResetZoom.Size = new System.Drawing.Size(39, 22);
            this.buttonResetZoom.Text = "Reset";
            this.buttonResetZoom.Click += new System.EventHandler(this.buttonResetZoom_Click);
            // 
            // checkBoxFixed
            // 
            this.checkBoxFixed.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.checkBoxFixed.Image = ((System.Drawing.Image)(resources.GetObject("checkBoxFixed.Image")));
            this.checkBoxFixed.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.checkBoxFixed.Name = "checkBoxFixed";
            this.checkBoxFixed.Size = new System.Drawing.Size(41, 22);
            this.checkBoxFixed.Text = "Fixed:";
            this.checkBoxFixed.Click += new System.EventHandler(this.checkBoxFixed_Click);
            // 
            // textBoxFixed
            // 
            this.textBoxFixed.Name = "textBoxFixed";
            this.textBoxFixed.Size = new System.Drawing.Size(100, 25);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.splitContainer1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.menuStrip1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(829, 450);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(829, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.closeToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.openToolStripMenuItem.Text = "Open...";
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.goToEnvelopeTickToolStripMenuItem,
            this.goToOffsetSecondsToolStripMenuItem,
            this.goToOffsetframesToolStripMenuItem,
            this.toolStripMenuItem1,
            this.showOffsetInSecondsToolStripMenuItem,
            this.showOffsetInFramesToolStripMenuItem,
            this.showTotalDenormalsToolStripMenuItem,
            this.toolStripMenuItem2,
            this.showEventsViewerToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // goToEnvelopeTickToolStripMenuItem
            // 
            this.goToEnvelopeTickToolStripMenuItem.Name = "goToEnvelopeTickToolStripMenuItem";
            this.goToEnvelopeTickToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
            this.goToEnvelopeTickToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.goToEnvelopeTickToolStripMenuItem.Text = "Go To Envelope Tick...";
            this.goToEnvelopeTickToolStripMenuItem.Click += new System.EventHandler(this.goToEnvelopeTickToolStripMenuItem_Click);
            // 
            // goToOffsetSecondsToolStripMenuItem
            // 
            this.goToOffsetSecondsToolStripMenuItem.Name = "goToOffsetSecondsToolStripMenuItem";
            this.goToOffsetSecondsToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.goToOffsetSecondsToolStripMenuItem.Text = "Go To Offset (seconds)...";
            this.goToOffsetSecondsToolStripMenuItem.Click += new System.EventHandler(this.goToOffsetSecondsToolStripMenuItem_Click);
            // 
            // goToOffsetframesToolStripMenuItem
            // 
            this.goToOffsetframesToolStripMenuItem.Name = "goToOffsetframesToolStripMenuItem";
            this.goToOffsetframesToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.goToOffsetframesToolStripMenuItem.Text = "Go To Offset (frames)...";
            this.goToOffsetframesToolStripMenuItem.Click += new System.EventHandler(this.goToOffsetframesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(229, 6);
            // 
            // showOffsetInSecondsToolStripMenuItem
            // 
            this.showOffsetInSecondsToolStripMenuItem.Name = "showOffsetInSecondsToolStripMenuItem";
            this.showOffsetInSecondsToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.showOffsetInSecondsToolStripMenuItem.Text = "Show Offset in Seconds";
            this.showOffsetInSecondsToolStripMenuItem.Click += new System.EventHandler(this.showOffsetInSecondsToolStripMenuItem_Click);
            // 
            // showOffsetInFramesToolStripMenuItem
            // 
            this.showOffsetInFramesToolStripMenuItem.Name = "showOffsetInFramesToolStripMenuItem";
            this.showOffsetInFramesToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.showOffsetInFramesToolStripMenuItem.Text = "Show Offset in Frames";
            this.showOffsetInFramesToolStripMenuItem.Click += new System.EventHandler(this.showOffsetInFramesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(229, 6);
            // 
            // showEventsViewerToolStripMenuItem
            // 
            this.showEventsViewerToolStripMenuItem.Name = "showEventsViewerToolStripMenuItem";
            this.showEventsViewerToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.showEventsViewerToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.showEventsViewerToolStripMenuItem.Text = "Show Events Viewer";
            this.showEventsViewerToolStripMenuItem.Click += new System.EventHandler(this.showEventsViewerToolStripMenuItem_Click);
            // 
            // epochBindingSource
            // 
            this.epochBindingSource.DataSource = typeof(OutOfPhaseTraceScheduleAnalyzer.Epoch);
            // 
            // showTotalDenormalsToolStripMenuItem
            // 
            this.showTotalDenormalsToolStripMenuItem.Name = "showTotalDenormalsToolStripMenuItem";
            this.showTotalDenormalsToolStripMenuItem.Size = new System.Drawing.Size(232, 22);
            this.showTotalDenormalsToolStripMenuItem.Text = "Show Total Denormals";
            this.showTotalDenormalsToolStripMenuItem.Click += new System.EventHandler(this.showTotalDenormalsToolStripMenuItem_Click);
            // 
            // EnvelopeTick
            // 
            this.EnvelopeTick.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.EnvelopeTick.HeaderText = "Env. Tick";
            this.EnvelopeTick.Name = "EnvelopeTick";
            this.EnvelopeTick.ReadOnly = true;
            this.EnvelopeTick.Width = 78;
            // 
            // FrameCount
            // 
            this.FrameCount.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.FrameCount.HeaderText = "Frames";
            this.FrameCount.Name = "FrameCount";
            this.FrameCount.ReadOnly = true;
            this.FrameCount.Width = 66;
            // 
            // FrameBaseTime
            // 
            this.FrameBaseTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewCellStyle1.Format = "N4";
            dataGridViewCellStyle1.NullValue = null;
            this.FrameBaseTime.DefaultCellStyle = dataGridViewCellStyle1;
            this.FrameBaseTime.HeaderText = "Offset (s)";
            this.FrameBaseTime.Name = "FrameBaseTime";
            this.FrameBaseTime.ReadOnly = true;
            this.FrameBaseTime.Width = 74;
            // 
            // FrameBase
            // 
            this.FrameBase.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.FrameBase.HeaderText = "Offset (fr)";
            this.FrameBase.Name = "FrameBase";
            this.FrameBase.ReadOnly = true;
            this.FrameBase.Visible = false;
            this.FrameBase.Width = 75;
            // 
            // Duration
            // 
            this.Duration.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewCellStyle2.Format = "N3";
            dataGridViewCellStyle2.NullValue = null;
            this.Duration.DefaultCellStyle = dataGridViewCellStyle2;
            this.Duration.HeaderText = "Duration (ms)";
            this.Duration.Name = "Duration";
            this.Duration.ReadOnly = true;
            this.Duration.Width = 94;
            // 
            // Denormals
            // 
            this.Denormals.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.Denormals.HeaderText = "Denormals";
            this.Denormals.Name = "Denormals";
            this.Denormals.ReadOnly = true;
            this.Denormals.Visible = false;
            this.Denormals.Width = 82;
            // 
            // VisualizerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(829, 450);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "VisualizerForm";
            this.Text = "Out Of Phase Concurrency Schedule Visualizer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewEpochs)).EndInit();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDefinitions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.definitionBindingSource)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.epochBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.DataGridView dataGridViewDefinitions;
        private System.Windows.Forms.BindingSource definitionBindingSource;
        private System.Windows.Forms.DataGridView dataGridViewEpochs;
        private System.Windows.Forms.BindingSource epochBindingSource;
        private ScheduleView scheduleViewEpoch;
        private System.Windows.Forms.DataGridViewTextBoxColumn idDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn nameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn kindAsStringDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn SectionIdAsString;
        private System.Windows.Forms.DataGridViewTextBoxColumn InputsAsString;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem goToEnvelopeTickToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel labelZoom;
        private System.Windows.Forms.ToolStripButton buttonZoomIn;
        private System.Windows.Forms.ToolStripButton buttonZoomOut;
        private System.Windows.Forms.ToolStripButton buttonResetZoom;
        private System.Windows.Forms.ToolStripButton checkBoxFixed;
        private System.Windows.Forms.ToolStripTextBox textBoxFixed;
        private System.Windows.Forms.ToolStripMenuItem goToOffsetSecondsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem showOffsetInSecondsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showOffsetInFramesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem goToOffsetframesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem showEventsViewerToolStripMenuItem;
        private Level2DetailView level2DetailView;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.ToolStripMenuItem showTotalDenormalsToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn EnvelopeTick;
        private System.Windows.Forms.DataGridViewTextBoxColumn FrameCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn FrameBaseTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn FrameBase;
        private System.Windows.Forms.DataGridViewTextBoxColumn Duration;
        private System.Windows.Forms.DataGridViewTextBoxColumn Denormals;
    }
}

