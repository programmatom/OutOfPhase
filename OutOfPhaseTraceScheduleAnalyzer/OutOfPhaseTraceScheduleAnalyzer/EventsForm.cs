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
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    public partial class EventsForm : Form
    {
        private readonly Top top;
        private readonly string path;

        public EventsForm(Top top, string path)
        {
            this.top = top;
            this.path = path;

            InitializeComponent();

            this.Text = String.Format("{0} - {1}", this.Text, Path.GetFileName(path));

            this.eventsView.LogPath = path;
            this.eventsView.Top = top;
        }

        private void toolStripButtonZoomIn_Click(object sender, EventArgs e)
        {
            eventsView.Zoom *= 2;
            toolStripLabelZoom.Text = String.Format("Zoom: {0}", eventsView.Zoom);
        }

        private void toolStripButtonZoomOut_Click(object sender, EventArgs e)
        {
            eventsView.Zoom /= 2;
            toolStripLabelZoom.Text = String.Format("Zoom: {0}", eventsView.Zoom);
        }

        private void toolStripButtonZoomReset_Click(object sender, EventArgs e)
        {
            eventsView.Zoom = 1;
            toolStripLabelZoom.Text = String.Format("Zoom: {0}", eventsView.Zoom);
        }

        private void goToEpochToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OneFieldDialog dialog = new OneFieldDialog("Go To...", "Epoch", String.Empty))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    int epoch;
                    if (Int32.TryParse(dialog.Value, out epoch))
                    {
                        eventsView.ShowEpoch(epoch);
                    }
                }
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
