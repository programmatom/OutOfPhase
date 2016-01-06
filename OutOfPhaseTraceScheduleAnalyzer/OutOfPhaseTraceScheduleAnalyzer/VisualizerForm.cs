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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    public partial class VisualizerForm : Form
    {
        private readonly Top top;
        private readonly string path;

        public VisualizerForm(Top top, string path)
        {
            this.top = top;
            this.path = path;

            InitializeComponent();

            this.Text = String.Format("{0} - {1}", this.Text, Path.GetFileName(path));

            this.scheduleViewEpoch.Top = top;
            this.dataGridViewEpochs.Font = new Font(Font.FontFamily, 7);
            this.dataGridViewDefinitions.Font = new Font(Font.FontFamily, 7);
            this.level2DetailView.Top = top;

            this.definitionBindingSource.DataSource = new BindingSource(top.Definitions, null);

            //this.epochBindingSource.DataSource = new BindingSource(top.Epochs, null);
            this.dataGridViewEpochs.CellValueNeeded += new DataGridViewCellValueEventHandler(dataGridViewEpochs_CellValueNeeded);
            // TODO: even in virtual mode, this still allocates an object[RowCount] which takes up a lot of memory
            this.dataGridViewEpochs.RowCount = top.Epochs.Count;

            this.dataGridViewEpochs.CurrentCellChanged += new EventHandler(dataGridViewEpochs_CurrentCellChanged);
            this.scheduleViewEpoch.ItemClicked += new ScheduleView.ItemClickedEventHandler(scheduleViewEpoch_ItemClicked);
            this.checkBoxFixed.CheckedChanged += new EventHandler(checkBoxFixed_CheckedChanged);
            this.textBoxFixed.Validated += new EventHandler(textBoxFixed_Validated);
            UpdateZoom();
            UpdateOffsetMenuCheck();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            DataRegistry.Release(top);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool result = base.ProcessCmdKey(ref msg, keyData);
            if ((keyData == Keys.Enter) && textBoxFixed.Focused)
            {
                dataGridViewEpochs.Focus();
            }
            return result;
        }

        private void dataGridViewEpochs_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            Epoch epoch = top.Epochs[e.RowIndex];
            switch (dataGridViewEpochs.Columns[e.ColumnIndex].Name)
            {
                default:
                    Debug.Assert(false);
                    e.Value = null;
                    break;
                case "EnvelopeTick":
                    e.Value = epoch.EnvelopeTick;
                    break;
                case "FrameCount":
                    e.Value = epoch.FrameCount;
                    break;
                case "FrameBase":
                    e.Value = epoch.FrameBase;
                    break;
                case "FrameBaseTime":
                    e.Value = epoch.FrameBaseTime;
                    break;
                case "Duration":
                    e.Value = epoch.Duration;
                    break;
                case "Denormals":
                    e.Value = epoch.TotalDenormals;
                    break;
            }
        }

        private void dataGridViewEpochs_CurrentCellChanged(object sender, EventArgs e)
        {
            if ((this.dataGridViewEpochs.CurrentCell != null) && ((uint)this.dataGridViewEpochs.CurrentCell.RowIndex < top.Epochs.Count))
            {
                Epoch epoch = top.Epochs[this.dataGridViewEpochs.CurrentCell.RowIndex];
                this.scheduleViewEpoch.Epoch = epoch;
                this.level2DetailView.Epoch = epoch;
            }
            else
            {
                this.scheduleViewEpoch.Epoch = null;
                this.level2DetailView.Epoch = null;
            }
        }

        private void scheduleViewEpoch_ItemClicked(ScheduleView.ItemClickedEventArgs e)
        {
            this.dataGridViewDefinitions.ClearSelection();
            this.dataGridViewDefinitions.Rows[e.Id].Selected = true;
            this.dataGridViewDefinitions.CurrentCell = this.dataGridViewDefinitions.Rows[e.Id].Cells[0]; // scroll into view
        }

        public static void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Top top = DataRegistry.QueryAddref(dialog.FileName);
                    if (top == null)
                    {
                        top = OutOfPhaseTraceScheduleAnalyzer.Top.Read(
                            dialog.FileName,
                            new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read));
                        DataRegistry.Add(dialog.FileName, top);
                    }
                    new VisualizerForm(top, dialog.FileName).Show();
                }
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void goToEnvelopeTickToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OneFieldDialog dialog = new OneFieldDialog("Go To...", "Envelope Tick", dataGridViewEpochs.CurrentRow != null ? top.Epochs[dataGridViewEpochs.CurrentRow.Index].EnvelopeTick.ToString() : String.Empty))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    long envelopeTick;
                    if (Int64.TryParse(dialog.Value, out envelopeTick))
                    {
                        int i = BinarySearchHelper.BinarySearch(top.Epochs, 0, top.Epochs.Count, envelopeTick, delegate(Epoch l, long r) { return l.EnvelopeTick.CompareTo(r); });
                        if (i < 0)
                        {
                            i = ~i;
                        }
                        this.dataGridViewEpochs.ClearSelection();
                        this.dataGridViewEpochs.Rows[i].Selected = true;
                        this.dataGridViewEpochs.CurrentCell = this.dataGridViewEpochs.Rows[i].Cells[0]; // scroll into view
                    }
                }
            }
        }

        private void goToOffsetSecondsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OneFieldDialog dialog = new OneFieldDialog("Go To...", "Offset (seconds)", dataGridViewEpochs.CurrentRow != null ? top.Epochs[dataGridViewEpochs.CurrentRow.Index].FrameBaseTime.ToString() : String.Empty))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    decimal offset;
                    if (Decimal.TryParse(dialog.Value, out offset))
                    {
                        int i = BinarySearchHelper.BinarySearch(top.Epochs, 0, top.Epochs.Count, offset, delegate(Epoch l, decimal r) { return l.FrameBaseTime.CompareTo(r); });
                        if (i < 0)
                        {
                            i = ~i;
                        }
                        this.dataGridViewEpochs.ClearSelection();
                        this.dataGridViewEpochs.Rows[i].Selected = true;
                        this.dataGridViewEpochs.CurrentCell = this.dataGridViewEpochs.Rows[i].Cells[0]; // scroll into view
                    }
                }
            }
        }

        private void goToOffsetframesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OneFieldDialog dialog = new OneFieldDialog("Go To...", "Offset (frames)", dataGridViewEpochs.CurrentRow != null ? top.Epochs[dataGridViewEpochs.CurrentRow.Index].FrameBase.ToString() : String.Empty))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    long offset;
                    if (Int64.TryParse(dialog.Value, out offset))
                    {
                        int i = BinarySearchHelper.BinarySearch(top.Epochs, 0, top.Epochs.Count, offset, delegate(Epoch l, long r) { return l.FrameBase.CompareTo(r); });
                        if (i < 0)
                        {
                            i = ~i;
                        }
                        this.dataGridViewEpochs.ClearSelection();
                        this.dataGridViewEpochs.Rows[i].Selected = true;
                        this.dataGridViewEpochs.CurrentCell = this.dataGridViewEpochs.Rows[i].Cells[0]; // scroll into view
                    }
                }
            }
        }

        private void UpdateZoom()
        {
            labelZoom.Text = String.Format("Zoom: {0}", scheduleViewEpoch.Zoom);
            textBoxFixed.Text = scheduleViewEpoch.Range.ToString();
            textBoxFixed.Enabled = checkBoxFixed.Checked;
        }

        private void buttonZoomIn_Click(object sender, EventArgs e)
        {
            scheduleViewEpoch.Zoom *= 2;
            UpdateZoom();
        }

        private void buttonZoomOut_Click(object sender, EventArgs e)
        {
            scheduleViewEpoch.Zoom /= 2;
            UpdateZoom();
        }

        private void buttonResetZoom_Click(object sender, EventArgs e)
        {
            scheduleViewEpoch.Zoom = 1;
            UpdateZoom();
        }

        private void checkBoxFixed_CheckedChanged(object sender, EventArgs e)
        {
            scheduleViewEpoch.Fixed = checkBoxFixed.Checked;
            UpdateZoom();
        }

        private void textBoxFixed_Validated(object sender, EventArgs e)
        {
            double range;
            if (Double.TryParse(textBoxFixed.Text, out range))
            {
                scheduleViewEpoch.Range = range;
                UpdateZoom();
            }
        }

        private const string OffsetSecondsColumnName = "FrameBaseTime";
        private const string OffsetFramesColumnName = "FrameBase";
        private const string DenormalsColumnName = "Denormals";

        private void checkBoxFixed_Click(object sender, EventArgs e)
        {
            checkBoxFixed.Checked = !checkBoxFixed.Checked;
        }

        private void UpdateOffsetMenuCheck()
        {
            showOffsetInSecondsToolStripMenuItem.Checked = dataGridViewEpochs.Columns[OffsetSecondsColumnName].Visible;
            showOffsetInFramesToolStripMenuItem.Checked = dataGridViewEpochs.Columns[OffsetFramesColumnName].Visible;
        }

        private void showOffsetInSecondsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridViewEpochs.Columns[OffsetSecondsColumnName].Visible = true;
            dataGridViewEpochs.Columns[OffsetFramesColumnName].Visible = false;
            UpdateOffsetMenuCheck();
        }

        private void showOffsetInFramesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridViewEpochs.Columns[OffsetSecondsColumnName].Visible = false;
            dataGridViewEpochs.Columns[OffsetFramesColumnName].Visible = true;
            UpdateOffsetMenuCheck();
        }

        private void showEventsViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new EventsForm(top, path).Show();
        }

        private void showTotalDenormalsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showTotalDenormalsToolStripMenuItem.Checked = !showTotalDenormalsToolStripMenuItem.Checked;
            dataGridViewEpochs.Columns[DenormalsColumnName].Visible = showTotalDenormalsToolStripMenuItem.Checked;
        }
    }
}
