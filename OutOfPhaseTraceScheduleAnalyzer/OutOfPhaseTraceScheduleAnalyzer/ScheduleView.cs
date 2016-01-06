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
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    [DefaultBindingProperty("Epoch")]
    public partial class ScheduleView : UserControl
    {
        private Top top;
        private Epoch epoch;
        private int margin = 40;
        private readonly DisposeList<Region> clickables = new DisposeList<Region>();
        private int lastToolTipId = -1;

        private long min, max;

        private bool fixedScale;
        private float scale = 1;
        private const double RangeDefault = .001;
        private double range = RangeDefault;

        public ScheduleView()
        {
            InitializeComponent();

            this.Disposed += new EventHandler(ScheduleView_Disposed);
        }

        private void ScheduleView_Disposed(object sender, EventArgs e)
        {
            clickables.Dispose();
        }

        [Browsable(true), Category("Appearance"), DefaultValue(1)]
        public float Zoom { get { return scale; } set { scale = value; UpdateZoom(); } }

        private void UpdateZoom()
        {
            if (!fixedScale)
            {
                AutoScrollMinSize = new Size((int)Math.Ceiling((ClientSize.Width - 2 * margin) * scale + 2 * margin), 0);
            }
            else
            {
                RecalcEpochChanged();
            }
            Invalidate();
        }

        [Browsable(true), Category("Appearance"), DefaultValue(false)]
        public bool Fixed { get { return fixedScale; } set { fixedScale = value; UpdateZoom(); } }
        [Browsable(true), Category("Appearance"), DefaultValue(RangeDefault)]
        public double Range { get { return range; } set { range = value; UpdateZoom(); } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Top Top { get { return top; } set { top = value; } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Epoch Epoch
        {
            get
            {
                return epoch;
            }
            set
            {
                epoch = value;
                RecalcEpochChanged();
            }
        }

        private void RecalcEpochChanged()
        {
            RecalcMinMax();

            if (fixedScale && (epoch != null))
            {
                double width = (double)(max - min) / top.TimerBasis;
                double factor = Math.Max(width, range) / range;
                AutoScrollMinSize = new Size((int)Math.Ceiling((ClientSize.Width - 2 * margin) * scale * factor + 2 * margin), 0);
            }

            Invalidate();
        }

        private void RecalcMinMax()
        {
            min = Int64.MaxValue;
            max = Int64.MinValue;
            if (epoch != null)
            {
                min = Math.Min(min, epoch.Phase0Start);
                max = Math.Max(max, epoch.Phase0Start);

                min = Math.Min(min, epoch.Phase1Start);
                max = Math.Max(max, epoch.Phase1Start);

                min = Math.Min(min, epoch.Phase2Start);
                max = Math.Max(max, epoch.Phase2Start);

                min = Math.Min(min, epoch.Phase3Start);
                max = Math.Max(max, epoch.Phase3Start);

                for (int i = 0; i < epoch.Data.Length; i++)
                {
                    min = Math.Min(min, epoch.Data[i].Start);
                    max = Math.Max(max, epoch.Data[i].Start);
                    min = Math.Min(min, epoch.Data[i].End);
                    max = Math.Max(max, epoch.Data[i].End);
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (DesignMode)
            {
                base.OnPaintBackground(e);
            }
            // control draws entire surface in OnPaint, so no need to erase background
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            clickables.Clear();

            if (!DesignMode)
            {
                if (epoch == null)
                {
                    e.Graphics.FillRectangle(Brushes.LightGray, ClientRectangle);
                }
                else
                {
                    e.Graphics.FillRectangle(Brushes.White, ClientRectangle);

                    float p0x = X(epoch.Phase0Start);
                    float p1x = X(epoch.Phase1Start);
                    float p2x = X(epoch.Phase2Start);
                    float p3x = X(epoch.Phase3Start);
                    e.Graphics.FillRectangle(Brushes.Chartreuse, p0x, 0, p1x - p0x, ClientSize.Height);
                    e.Graphics.FillRectangle(Brushes.Wheat, p1x, 0, p2x - p1x, ClientSize.Height);
                    e.Graphics.FillRectangle(Brushes.YellowGreen, p2x, 0, p3x - p2x, ClientSize.Height);
                    TextRenderer.DrawText(e.Graphics, "Leading", Font, new Rectangle((int)p0x, 0, (int)(p1x - p0x), FontHeight), Color.Black, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                    TextRenderer.DrawText(e.Graphics, "Concurrent", Font, new Rectangle((int)p1x, 0, (int)(p2x - p1x), FontHeight), Color.Black, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                    TextRenderer.DrawText(e.Graphics, "Trailing", Font, new Rectangle((int)p2x, 0, (int)(p3x - p2x), FontHeight), Color.Black, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

                    decimal duration = (decimal)(max - min) / top.TimerBasis;
                    decimal divisor = 1;
                    while (X((long)(min + top.TimerBasis * divisor)) - X(min) > 200)
                    {
                        divisor /= 10;
                    }
                    bool flip = false;
                    while (X((long)(min + top.TimerBasis * divisor)) - X(min) < 50)
                    {
                        divisor *= flip ? 2 : 5;
                        flip = !flip;
                    }
                    int ti = 0;
                    while (true)
                    {
                        decimal offset = ti * divisor;
                        decimal td = min + offset * top.TimerBasis;
                        long t = (long)td;
                        float x = X(t);
                        string label;
                        if (divisor >= 1m)
                        {
                            label = ((int)Math.Round(offset)).ToString() + "s";
                        }
                        else if (divisor >= 0.001m)
                        {
                            label = ((int)Math.Round(1000 * offset)).ToString() + "ms";
                        }
                        else if (divisor >= 0.000001m)
                        {
                            label = ((int)Math.Round(1000000 * offset)).ToString() + "\u03bcs";
                        }
                        else //if (divisor >= 0.000000001m)
                        {
                            label = ((int)Math.Round(1000000000 * offset)).ToString() + "ns";
                        }
                        float labelWidth = TextRenderer.MeasureText(label, Font).Width;
                        if (x > ClientSize.Width + labelWidth)
                        {
                            break;
                        }
                        e.Graphics.DrawLine(Pens.Black, x, FontHeight * 2, x, FontHeight * 3 - 1);
                        e.Graphics.DrawLine(Pens.Black, x, FontHeight * 3, x, ClientSize.Height);
                        TextRenderer.DrawText(
                            e.Graphics,
                            label,
                            Font,
                            new Rectangle(
                                (int)Math.Ceiling(x - labelWidth / 2),
                                FontHeight,
                                (int)Math.Ceiling(labelWidth),
                                FontHeight),
                            Color.Black,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                        ti++;
                    }
                    e.Graphics.DrawLine(Pens.Black, 0, FontHeight * 2.5f, ClientSize.Width, FontHeight * 2.5f);

                    float vTop = 3 * FontHeight + FontHeight / 2;
                    float vBottom = ClientSize.Height - FontHeight / 2;
                    float sep = FontHeight / 2;
                    for (int p = 0; p < top.Concurrency; p++)
                    {
                        List<int> ids = new List<int>();
                        for (int i = 0; i < epoch.Data.Length; i++)
                        {
                            if (p == epoch.Data[i].Processor)
                            {
                                ids.Add(i);
                            }
                        }
                        ids.Sort(delegate(int l, int r) { return epoch.Data[l].Start.CompareTo(epoch.Data[r].Start); });
                        float vT = p * (vBottom - vTop) / top.Concurrency + vTop;
                        float vB = (p + 1) * (vBottom - vTop) / top.Concurrency - sep + vTop;
                        e.Graphics.FillRectangle(Brushes.WhiteSmoke, 0, vT, ClientSize.Width, vB - vT);
                        e.Graphics.DrawRectangle(Pens.DarkGray, -1, vT, ClientSize.Width + 2, vB - vT);
                        TextRenderer.DrawText(
                            e.Graphics,
                            p.ToString(),
                            Font,
                            new Rectangle(
                                (int)Math.Ceiling(X(min) - margin),
                                (int)Math.Ceiling(vT),
                                margin,
                                (int)Math.Floor(vB - vT)),
                            Color.Black,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                        foreach (int i in ids)
                        {
                            while (i >= clickables.Count)
                            {
                                clickables.Add(null);
                            }
                            float l = X(epoch.Data[i].Start);
                            float r = X(epoch.Data[i].End);
                            Rectangle rect = new Rectangle(
                                (int)Math.Round(l),
                                (int)Math.Round(vT + sep),
                                (int)Math.Round(r - l),
                                (int)Math.Round(vB - vT - 2 * sep));
                            e.Graphics.FillRectangle(
                                top.Definitions[epoch.Data[i].Id].Kind == Kind.Effect
                                    ? Brushes.Salmon
                                    : Brushes.SandyBrown,
                                rect);
                            e.Graphics.DrawRectangle(
                                Pens.Black,
                                rect);
                            clickables[i] = new Region(rect);
                        }
                        int y0 = (int)Math.Ceiling(vT + sep) + 1;
                        int y = y0;
                        foreach (int i in ids)
                        {
                            string label = epoch.Data[i].Id.ToString();
                            float l = X(epoch.Data[i].Start);
                            float r = X(epoch.Data[i].End);
                            int width = TextRenderer.MeasureText(label, Font).Width;
                            Rectangle rect = new Rectangle((int)Math.Ceiling(l), y, width, FontHeight);
                            TextRenderer.DrawText(
                                e.Graphics,
                                label,
                                Font,
                                rect,
                                Color.Black,
                                top.Definitions[epoch.Data[i].Id].Kind == Kind.Effect
                                    ? Color.Salmon
                                    : Color.SandyBrown,
                                TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
                            clickables[i].Union(rect);
                            y += FontHeight;
                            if (y + FontHeight > vB - sep)
                            {
                                y = y0;
                            }
                        }
                    }
                }
            }
            base.OnPaint(e);
        }

        private float X(long v)
        {
            double d;
            if (!fixedScale)
            {
                d = Math.Max(max - min, 1);
            }
            else
            {
                d = range * top.TimerBasis;
            }
            return (float)((double)(v - min) * scale / d * (ClientSize.Width - margin * 2)) + margin + AutoScrollPosition.X;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            for (int i = clickables.Count - 1; i >= 0; i--)
            {
                if (clickables[i] != null)
                {
                    if (clickables[i].IsVisible(e.Location))
                    {
                        if (ItemClicked != null)
                        {
                            ItemClicked.Invoke(new ItemClickedEventArgs(i));
                            return;
                        }
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            for (int i = clickables.Count - 1; i >= 0; i--)
            {
                if (clickables[i] != null)
                {
                    if (clickables[i].IsVisible(e.Location))
                    {
                        if (lastToolTipId != i)
                        {
                            lastToolTipId = i;
                            toolTip.Show(
                                String.Format("{0} - {1} ({2})", i, top.Definitions[i].Name, top.Definitions[i].KindAsString),
                                this,
                                e.X + FontHeight,
                                e.Y + FontHeight,
                                30000);
                        }
                        return;
                    }
                }
            }

            lastToolTipId = -1;
            toolTip.Hide(this);
        }

        public class ItemClickedEventArgs : EventArgs
        {
            private readonly int id;

            public ItemClickedEventArgs(int id)
            {
                this.id = id;
            }

            public int Id { get { return id; } }
        }

        public delegate void ItemClickedEventHandler(ItemClickedEventArgs e);

        public event ItemClickedEventHandler ItemClicked;
    }
}
