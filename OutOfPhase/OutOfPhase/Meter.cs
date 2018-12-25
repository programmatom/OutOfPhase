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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class Meter : UserControl
    {
        private Color markColor = SystemColors.ControlDark;
        private Color safeColor = Color.LimeGreen;
        private Color warningColor = Color.Gold;
        private Color overloadColor = Color.Red;

        private float lowLevel = -30;
        private float highLevel = 6;
        private float warningLevel = -6;
        private float overloadLevel = 0;

        private float shortValue, longValue;

        private readonly List<SliderCore.Mark> marks = new List<SliderCore.Mark>();

        public Meter()
        {
            InitializeComponent();
        }

        [Category("Appearance"), DefaultValue(typeof(Color), "ControlDark")]
        public Color MarkColor { get { return markColor; } set { markColor = value; Redraw(); } }
        [Category("Appearance"), DefaultValue(typeof(Color), "LimeGreen")]
        public Color SafeColor { get { return safeColor; } set { safeColor = value; Redraw(); } }
        [Category("Appearance"), DefaultValue(typeof(Color), "Gold")]
        public Color WarningColor { get { return warningColor; } set { warningColor = value; Redraw(); } }
        [Category("Appearance"), DefaultValue(typeof(Color), "Red")]
        public Color OverloadColor { get { return overloadColor; } set { overloadColor = value; Redraw(); } }

        [Category("Appearance"), DefaultValue(-30f)]
        public float LowLevel { get { return lowLevel; } set { lowLevel = value; ResetMetrics(); Redraw(); } }
        [Category("Appearance"), DefaultValue(6f)]
        public float HighLevel { get { return highLevel; } set { highLevel = value; ResetMetrics(); Redraw(); } }
        [Category("Appearance"), DefaultValue(-6f)]
        public float WarningLevel { get { return warningLevel; } set { warningLevel = value; Redraw(); } }
        [Category("Appearance"), DefaultValue(0f)]
        public float OverloadLevel { get { return overloadLevel; } set { overloadLevel = value; Redraw(); } }

        [Category("Appearance"), DefaultValue(0f)]
        public float ShortValue { get { return shortValue; } set { shortValue = value; Redraw(); } }
        [Category("Appearance"), DefaultValue(0f)]
        public float LongValue { get { return longValue; } set { longValue = value; Redraw(); } }

        public void SetValues(float shortValue, float longValue)
        {
            this.shortValue = shortValue;
            this.longValue = longValue;
            Redraw();
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            ResetMetrics();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Redraw(e.Graphics, ClientRectangle);
        }

        private void Redraw()
        {
            using (Graphics graphics = CreateGraphics())
            {
                Redraw(graphics, ClientRectangle);
            }
        }

        private void Redraw(Graphics graphics, Rectangle bounds)
        {
            if (!bounds.IsEmpty)
            {
                using (Image image = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                {
                    using (Graphics offscreen = Graphics.FromImage(image))
                    {
                        SliderState state = new SliderState(lowLevel, highLevel);
                        float effectiveShortValue = Math.Min(shortValue, highLevel);
                        float effectiveLongValue = Math.Min(LongValue, highLevel);

                        using (Brush backBrush = new SolidBrush(BackColor))
                        {
                            offscreen.FillRectangle(backBrush, 0, 0, bounds.Width, bounds.Height);
                        }

                        const int BarHeight = 11;

                        if (effectiveShortValue > lowLevel)
                        {
                            using (Brush brush = new SolidBrush(safeColor))
                            {
                                offscreen.FillRectangle(brush, 0, 0, SliderCore.ValueToPoint(effectiveShortValue, state, bounds.Width), BarHeight);
                            }
                        }
                        if (effectiveShortValue > warningLevel)
                        {
                            using (Brush brush = new SolidBrush(warningColor))
                            {
                                int x = SliderCore.ValueToPoint(warningLevel, state, bounds.Width);
                                offscreen.FillRectangle(brush, x, 0, SliderCore.ValueToPoint(effectiveShortValue, state, bounds.Width) - x, BarHeight);
                            }
                        }
                        if (effectiveShortValue > overloadLevel)
                        {
                            using (Brush brush = new SolidBrush(overloadColor))
                            {
                                int x = SliderCore.ValueToPoint(overloadLevel, state, bounds.Width);
                                offscreen.FillRectangle(brush, x, 0, SliderCore.ValueToPoint(effectiveShortValue, state, bounds.Width) - x, BarHeight);
                            }
                        }

                        Color longColor = effectiveLongValue > overloadLevel ? overloadColor : effectiveLongValue > warningLevel ? warningColor : safeColor;
                        using (Brush brush = new SolidBrush(longColor))
                        {
                            offscreen.FillRectangle(brush, SliderCore.ValueToPoint(effectiveLongValue, state, bounds.Width) - 1, 0, 3, BarHeight);
                        }

                        SliderCore.DrawMetrics(offscreen, bounds.Width, BarHeight, ForeColor, BackColor, markColor, Font, marks, true/*showLabels*/, state);
                    }

                    graphics.DrawImage(image, bounds);
                }
            }
        }

        private void ResetMetrics()
        {
            if (Width != 0)
            {
                using (Graphics graphics = CreateGraphics())
                {
                    SliderCore.ResetMetrics(graphics, new SliderState(lowLevel, highLevel), Width, marks, Font);
                }
            }
        }
    }
}
