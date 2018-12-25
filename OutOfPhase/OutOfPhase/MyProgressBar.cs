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
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class MyProgressBar : UserControl
    {
        private float level;
        private float maximum;
        private Color goodColor = Color.LimeGreen;
        private Color criticalColor = Color.Firebrick;
        private float criticalThreshhold;

        public MyProgressBar()
        {
            InitializeComponent();
        }

        [Category("Appearance"), DefaultValue(typeof(Color), "Firebrick")]
        public Color CritColor { get { return criticalColor; } set { criticalColor = value; } }
        [Category("Appearance"), DefaultValue(typeof(Color), "LimeGreen")]
        public Color GoodColor { get { return goodColor; } set { goodColor = value; } }

        [Category("Appearance"), DefaultValue(0f)]
        public float CritThreshhold { get { return criticalThreshhold; } set { criticalThreshhold = value; } }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Redraw(pe.Graphics);
            base.OnPaint(pe);
        }

        private void Redraw(Graphics graphics)
        {
            float cutoff = Math.Min(Math.Max((float)level / maximum, 0), 1);
            if (!Single.IsNaN(cutoff))
            {
                Color fore = criticalThreshhold >= 0
                    ? (level >= criticalThreshhold ? goodColor : criticalColor)
                    : (level <= -criticalThreshhold ? goodColor : criticalColor);
                using (Brush brush = new SolidBrush(fore))
                {
                    graphics.FillRectangle(brush, 0, 0, Width * cutoff, Height);
                }
                using (Brush brush = new SolidBrush(BackColor))
                {
                    graphics.FillRectangle(brush, Width * cutoff, 0, Width * (1 - cutoff), Height);
                }
            }
            else
            {
                using (Brush brush = new HatchBrush(HatchStyle.WideDownwardDiagonal, criticalColor, BackColor))
                {
                    graphics.FillRectangle(brush, 0, 0, Width, Height);
                }
            }
        }

        public void SetAll(float maximum, float level, float critical)
        {
            this.maximum = maximum;
            this.level = level;
            this.criticalThreshhold = critical;
            using (Graphics graphics = CreateGraphics())
            {
                Redraw(graphics);
            }
        }

        [Category("Appearance"), DefaultValue(0f)]
        public float Maximum { get { return maximum; } set { SetAll(value, level, criticalThreshhold); } }

        [Category("Appearance"), DefaultValue(0f)]
        public float Level { get { return level; } set { SetAll(maximum, value, criticalThreshhold); } }
    }
}
