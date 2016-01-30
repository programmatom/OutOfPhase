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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class WaveTableControl : UserControl
    {
        private WaveTableObjectRec waveTableObject;
        private int tableIndex;

        private Color axisColor = SystemColors.GrayText;

        private Brush backBrush;
        private Brush foreBrush;
        private Pen axisPen;

        public WaveTableControl()
        {
            ResizeRedraw = true;
            DoubleBuffered = true;

            InitializeComponent();

            Disposed += new EventHandler(WaveTableControl_Disposed);
        }

        [Category("Appearance"), DefaultValue(typeof(Color), "GrayText")]
        public Color AxisColor { get { return axisColor; } set { axisColor = value; } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WaveTableObjectRec WaveTableObject
        {
            get
            {
                return waveTableObject;
            }
            set
            {
                if (waveTableObject != null)
                {
                    waveTableObject.WaveTableDataChanged -= new WaveTableObjectRec.WaveTableStorageEventHandler(OnWaveTableDataChanged);
                }

                waveTableObject = value;

                if (waveTableObject != null)
                {
                    waveTableObject.WaveTableDataChanged += new WaveTableObjectRec.WaveTableStorageEventHandler(OnWaveTableDataChanged);
                }

                Invalidate();
            }
        }

        private void WaveTableControl_Disposed(object sender, EventArgs e)
        {
            if (waveTableObject != null)
            {
                waveTableObject.WaveTableDataChanged -= new WaveTableObjectRec.WaveTableStorageEventHandler(OnWaveTableDataChanged);
            }

            if (backBrush != null)
            {
                backBrush.Dispose();
            }
            if (foreBrush != null)
            {
                foreBrush.Dispose();
            }
            if (axisPen != null)
            {
                axisPen.Dispose();
            }
        }

        private void EnsureGraphicsObjects()
        {
            if (backBrush == null)
            {
                backBrush = new SolidBrush(BackColor);
            }
            if (foreBrush == null)
            {
                foreBrush = new SolidBrush(ForeColor);
            }
            if (axisPen == null)
            {
                axisPen = new Pen(axisColor);
            }
        }

        public void OnWaveTableDataChanged(object sender, WaveTableObjectRec.WaveTableStorageEventArgs e)
        {
            Invalidate();
        }

        public void OnIndexChanged(object sender, ScrollEventArgs e)
        {
            if (tableIndex != e.NewValue)
            {
                tableIndex = e.NewValue;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            EnsureGraphicsObjects();

            // custom paint code
            pe.Graphics.FillRectangle(backBrush, ClientRectangle);

            if (waveTableObject != null)
            {
                pe.Graphics.DrawLine(axisPen, 0, .5f * (Height - 1), Width, .5f * (Height - 1));

                int ti = Math.Max(Math.Min(tableIndex, waveTableObject.WaveTableData.NumTables - 1), 0);
                if ((ti >= 0) && (ti < waveTableObject.WaveTableData.NumTables))
                {
                    int numFrames = waveTableObject.WaveTableData.NumFrames;
                    WaveTableStorageRec.Table table = waveTableObject.WaveTableData.ListOfTables[ti];
                    for (int i = 0; i < numFrames; i++)
                    {
                        float x = ((float)i * Width) / numFrames;
                        float y = ((-table[i] + 1) / 2 * (Height - 1));
                        pe.Graphics.FillRectangle(foreBrush, x, y, 1, 1);
                    }
                }
            }

            // Calling the base class OnPaint
            base.OnPaint(pe);
        }
    }
}
