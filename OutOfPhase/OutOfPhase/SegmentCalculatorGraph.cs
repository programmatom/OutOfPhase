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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class SegmentCalculatorGraph : Control
    {
        private double initial, final, duration, newStart, newEnd;
        private bool exponential;

        private double max, min;
        private double left = 0, right = 1;

        public SegmentCalculatorGraph()
        {
            this.DoubleBuffered = true;

            InitializeComponent();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (Brush backBrush = new SolidBrush(BackColor))
            {
                using (Pen forePen = new Pen(ForeColor))
                {
                    using (Pen axisPen = new Pen(TrackViewControl.BlendColor(BackColor, 3, ForeColor, 1)))
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                        left = Math.Min(left, Math.Min(newStart, newEnd));
                        right = Math.Max(right, Math.Max(newStart, newEnd));

                    TryAgain:
                        e.Graphics.FillRectangle(backBrush, ClientRectangle);

                        e.Graphics.DrawLine(axisPen, 0, (float)Math.Round(PxY(0)), ClientSize.Width, (float)Math.Round(PxY(0))); // zero-line
                        e.Graphics.DrawLine(axisPen, 0, 0, 0, ClientSize.Height);

                        if ((left != newStart) || (right != newEnd) || (left != 0) || (right != 1))
                        {
                            int start = (int)Math.Round((newStart - left) / (right - left) * (ClientSize.Width - 1));
                            int end = (int)Math.Round((newEnd - left) / (right - left) * (ClientSize.Width - 1));
                            e.Graphics.DrawRectangle(
                                axisPen,
                                start,
                                0,
                                end - start,
                                ClientSize.Height - 1);
                        }

                        double oldMax = max;
                        double oldMin = min;

                        double y9 = F(0);
                        float pxY9 = PxY(y9);
                        for (int i = 0; i < ClientSize.Width; i++)
                        {
                            double normalizedX = (i + 1d) / ClientSize.Width;
                            double x = (normalizedX - left) / (right - left);
                            double y = F(x);

                            max = Math.Max(y, max);
                            min = Math.Min(y, min);

                            float pxY = PxY(y);

                            e.Graphics.DrawLine(forePen, i, pxY9 + 1, i + 1, pxY + 1);

                            y9 = y;
                            pxY9 = pxY;
                        }

                        if (Double.IsNaN(max) || Double.IsInfinity(max) || Double.IsNaN(min) || double.IsInfinity(min))
                        {
                            max = min = 0;
                        }
                        else if ((oldMax != max) || (oldMin != min))
                        {
                            goto TryAgain;
                        }
                    }
                }
            }
            base.OnPaint(e);
        }

        private double F(double x) // x normalized to [0..1]
        {
            if (!exponential)
            {
                return initial + x * (final - initial);
            }
            else
            {
                double logInitial = Synthesizer.ExpSegEndpointToLog(initial);
                double logFinal = Synthesizer.ExpSegEndpointToLog(final);

                double y = logInitial + x * (logFinal - logInitial);

                return Synthesizer.ExpSegEndpointToLinear(y);
            }
        }

        private float PxY(double y)
        {
            return FixNaNs((float)((y - max) / (min - max) * (ClientSize.Height - 2)));
        }

        private float FixNaNs(float f)
        {
            if (Single.IsNaN(f) || Single.IsInfinity(f))
            {
                return 0;
            }
            return f;
        }

        public void Update(double initial, double final, double duration, double newStart, double newEnd, bool exponential)
        {
            this.initial = initial;
            this.final = final;
            this.duration = duration;
            this.newStart = newStart;
            this.newEnd = newEnd;
            this.exponential = exponential;
            Invalidate();
        }

        public void Reset()
        {
            min = max = 0;
            left = 0;
            right = 1;
            Invalidate();
        }
    }
}
