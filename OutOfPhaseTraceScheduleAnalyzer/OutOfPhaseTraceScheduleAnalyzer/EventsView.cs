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
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    public partial class EventsView : UserControl
    {
        private Top top;
        private string logPath;
        private float scale = 1;
#if false
        private BitVector evtf;
#endif
        private EventBar[][] bars;
        private int[][] lines;
        private int[] linesCounts;
        private const int LeftMargin = 200;
        private const int RightMargin = 50;
        private const int MinSpace = 20;
        private const int StandardWidth = 10;
        private const int InnerSpacing = 2;

        private readonly DisposeList<Region> trackRegions = new DisposeList<Region>();
        private int lastToolTipId = -1;

        private class EventBar
        {
            public string tag;
            public int start;
            public int? frameIndex;
            public int? noteIndex;
            public int end;
            public int seq;
            public Tie[] ties = new Tie[0];

            public struct Tie
            {
                public int when;
                public int frameIndex;
                public int noteIndex;
            }

            public override string ToString()
            {
                return String.Format("{2} - [{0}..{1}]", start, end, seq);
            }
        }

        public EventsView()
        {
            InitializeComponent();

            this.Disposed += new EventHandler(EventsView_Disposed);
        }

        private void EventsView_Disposed(object sender, EventArgs e)
        {
            trackRegions.Dispose();
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LogPath { get { return logPath; } set { logPath = value; } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Top Top
        {
            get
            {
                return top;
            }
            set
            {
                top = value;
                Reset();
            }
        }

        [Browsable(true), Category("Appearance"), DefaultValue(1)]
        public float Zoom
        {
            get
            {
                return scale;
            }
            set
            {
                float oldScale = scale;
                scale = value;

                Invalidate();
                if (!DesignMode && (top != null))
                {
                    float totalWidth = LeftMargin + (int)Math.Ceiling(StandardWidth * oldScale * top.Epochs.Count);
                    float oldOffset = -AutoScrollPosition.X / totalWidth;
                    float oldOffset2 = (ClientSize.Width - AutoScrollPosition.X) / totalWidth;
                    float center = (oldOffset + oldOffset2) / 2;

                    totalWidth = LeftMargin + (int)Math.Ceiling(StandardWidth * scale * top.Epochs.Count);
                    oldOffset = center * totalWidth - ClientSize.Width / 2;
                    oldOffset2 *= totalWidth;

                    AutoScrollMinSize = new Size(
                        LeftMargin + (int)Math.Ceiling(StandardWidth * scale * top.Epochs.Count) + RightMargin,
                        AutoScrollMinSize.Height);
                    AutoScrollPosition = new Point(
                        (int)oldOffset,
                        -AutoScrollPosition.Y);
                }
            }
        }

        public void ShowEpoch(int epoch)
        {
            float center = (float)epoch / top.Epochs.Count;

            float totalWidth = LeftMargin + (int)Math.Ceiling(StandardWidth * scale * top.Epochs.Count);
            float offset = center * totalWidth - ClientSize.Width / 2;

            AutoScrollPosition = new Point(
                (int)offset,
                -AutoScrollPosition.Y);
        }

        private void Reset()
        {
            bars = null;
            lines = null;
#if false
            evtf = null;
#endif
            Invalidate();
        }

        private void Generate()
        {
            if (bars != null)
            {
                return;
            }
            if (!top.Level2)
            {
                return;
            }

#if false
            string acceleratorPath = Accelerators.Events.QueryAcceleratorPath(logPath, top.LogStream, Top.AcceleratorVersion);
            if (acceleratorPath == null)
            {
                evtf = new BitVector(top.Epochs.Count);
            }
            else
            {
                using (Stream stream = new FileStream(acceleratorPath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
                {
                    evtf = BitVector.Load(stream);
                }
            }
#endif

            List<EventBar>[] bars2 = new List<EventBar>[top.Definitions.Count];
            Dictionary<int, EventBar>[] bars3 = new Dictionary<int, EventBar>[top.Definitions.Count];
            for (int j = 0; j < top.Definitions.Count; j++)
            {
                bars2[j] = new List<EventBar>();
                bars3[j] = new Dictionary<int, EventBar>();
            }
            for (int i = 0; i < top.Epochs.Count; i++)
            {
#if false
                if (acceleratorPath != null)
                {
                    if (!evtf[i])
                    {
                        continue;
                    }
                }

                bool found = false;
#endif

                Epoch epoch = top.Epochs[i];
                if (!epoch.Level2)
                {
                    continue;
                }
                for (int j = 0; j < top.Definitions.Count; j++)
                {
                    DatumEvent[] events = epoch.Data[j].Events;
                    foreach (DatumEvent e in events)
                    {
#if false
                        found = true;
#endif
                        switch (e.Tag)
                        {
                            default:
                                Debug.Assert(false);
                                break;
                            case "Start":
                                {
                                    EventBar bar = new EventBar();
                                    bar.start = i;
                                    bar.seq = e.Seq;
                                    bar.frameIndex = ((DatumEvent2)e).frameIndex;
                                    bar.noteIndex = ((DatumEvent2)e).noteIndex;
                                    bars2[j].Add(bar);
                                    bars3[j].Add(e.Seq, bar);
                                }
                                break;
                            case "Stop":
                                {
                                    EventBar bar = bars3[j][e.Seq];
                                    bar.end = i;
                                }
                                break;
                            case "Restart":
                                {
                                    EventBar bar = bars3[j][e.Seq];
                                    EventBar.Tie tie = new EventBar.Tie();
                                    tie.when = i;
                                    tie.frameIndex = ((DatumEvent2)e).frameIndex;
                                    tie.noteIndex = ((DatumEvent2)e).noteIndex;
                                    int p = bar.ties.Length;
                                    Array.Resize(ref bar.ties, p + 1);
                                    bar.ties[p] = tie;
                                }
                                break;
                            case "SkipEnter":
                            case "SkipLeave":
                                {
                                    EventBar bar = new EventBar();
                                    bar.tag = e.Tag;
                                    bar.start = i;
                                    bar.end = i;
                                    bar.seq = e.Seq;
                                    bars2[j].Add(bar);
                                    bars3[j].Add(e.Seq, bar);
                                }
                                break;
                        }
                    }
                }

#if false
                if ((acceleratorPath == null) && found)
                {
                    evtf[i] = true;
                }
#endif
            }

#if false
            if (acceleratorPath == null)
            {
                acceleratorPath = System.IO.Path.GetTempFileName();
                using (Stream stream = new FileStream(acceleratorPath, FileMode.Create, FileAccess.Write, FileShare.None, Constants.BufferSize))
                {
                    evtf.Save(stream);
                }
                Accelerators.Events.RecordAcceleratorPath(logPath, top.LogStream, acceleratorPath, Top.AcceleratorVersion);
            }
#endif

            bars = new EventBar[top.Definitions.Count][];
            for (int j = 0; j < top.Definitions.Count; j++)
            {
                bars[j] = bars2[j].ToArray();
            }
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if ((e.KeyData == Keys.Left) || (e.KeyData == Keys.Right) || (e.KeyData == Keys.Down) || (e.KeyData == Keys.Up))
            {
                e.IsInputKey = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyData)
            {
                case Keys.Left:
                    AutoScrollPosition = new Point(-AutoScrollPosition.X - ClientSize.Width / 8, -AutoScrollPosition.Y);
                    break;
                case Keys.Right:
                    AutoScrollPosition = new Point(-AutoScrollPosition.X + ClientSize.Width / 8, -AutoScrollPosition.Y);
                    break;
                case Keys.Up:
                    AutoScrollPosition = new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y - ClientSize.Height / 8);
                    break;
                case Keys.Down:
                    AutoScrollPosition = new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y + ClientSize.Height / 8);
                    break;
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
            trackRegions.Clear();

            if (!DesignMode)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillRectangle(Brushes.White, ClientRectangle);
                if (!top.Level2)
                {
                    TextRenderer.DrawText(e.Graphics, "Log does not contain event data.", Font, ClientRectangle, Color.Black);
                    return;
                }

                Generate();
                if (lines == null)
                {
                    lines = new int[top.Definitions.Count][];
                    linesCounts = new int[top.Definitions.Count];
                    for (int j = 0; j < top.Definitions.Count; j++)
                    {
                        lines[j] = new int[bars[j].Length];
                        List<int> lineEnds = new List<int>();
                        for (int i = 0; i < bars[j].Length; i++)
                        {
                            // allocate from existing lines
                            bool done = false;
                            for (int k = 0; k < lineEnds.Count; k++)
                            {
                                if (lineEnds[k] + MinSpace <= bars[j][i].start)
                                {
                                    lines[j][i] = k;
                                    lineEnds[k] = bars[j][i].end;
                                    done = true;
                                    break;
                                }
                            }
                            // create new line
                            if (!done)
                            {
                                lines[j][i] = lineEnds.Count;
                                lineEnds.Add(bars[j][i].end);
                            }
                        }
                        linesCounts[j] = lineEnds.Count;
                    }
                }

                int leftEpoch = Math.Max((int)Math.Floor((-AutoScrollPosition.X - LeftMargin)
                    / (StandardWidth * scale)), 0);
                int rightEpoch = Math.Max((int)Math.Ceiling((-AutoScrollPosition.X - LeftMargin + ClientSize.Width + StandardWidth)
                    / (StandardWidth * scale)), 0);

                int c = 1;
                int cc = 1;
                bool f = false;
                while (c * StandardWidth * scale < 80)
                {
                    cc = c;
                    c = c * (!f ? 5 : 2);
                    f = !f;
                }
                for (int i = leftEpoch / c * c; i < rightEpoch + c - 1; i += cc)
                {
                    int x = AutoScrollPosition.X + LeftMargin + (int)Math.Floor(i * StandardWidth * scale);
                    e.Graphics.DrawLine(
                        i % c == 0 ? Pens.Gray : Pens.LightGray,
                        new Point(x, 0),
                        new Point(x, ClientSize.Height));
                    if (i % c == 0)
                    {
                        string label = i.ToString();
                        int w = TextRenderer.MeasureText(label, Font).Width;
                        TextRenderer.DrawText(
                            e.Graphics,
                            label,
                            Font,
                            new Rectangle(x - w / 2, AutoScrollPosition.Y, w, FontHeight),
                            Color.Black,
                            Color.Transparent,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                    }
                }

                int y = 2 * FontHeight;
                for (int j = 0; j < top.Definitions.Count; j++)
                {
                    e.Graphics.DrawLine(Pens.Gray, 0, y + AutoScrollPosition.Y, ClientSize.Width, y + AutoScrollPosition.Y);
                    y++;

                    bool colorSync = (j % 2 == 0);

                    Rectangle bounds = new Rectangle(
                        AutoScrollPosition.X,
                        y + AutoScrollPosition.Y,
                        LeftMargin + (int)Math.Ceiling(StandardWidth * scale * top.Epochs.Count),
                        1 + (FontHeight + 2 * InnerSpacing) * Math.Max(1, linesCounts[j]));
                    e.Graphics.FillRectangle(colorSync ? Brushes.OldLace : Brushes.WhiteSmoke, bounds);
                    trackRegions.Add(new Region(bounds));
                    TextRenderer.DrawText(
                        e.Graphics,
                        String.Format("{0}: {1}", j, top.Definitions[j].Name),
                        Font,
                        new Rectangle(bounds.Location, new Size(LeftMargin, bounds.Height)),
                        Color.Black,
                        Color.Transparent,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

                    for (int i = 0; i < bars[j].Length; i++)
                    {
                        EventBar bar = bars[j][i];
                        if ((bar.end < leftEpoch) || (bar.start > rightEpoch))
                        {
                            continue; // not visible
                        }

                        Rectangle barBounds;
                        if (bar.start < bar.end)
                        {
                            barBounds = new Rectangle(
                                LeftMargin + (int)Math.Floor(StandardWidth * scale * bar.start) + AutoScrollPosition.X,
                                1 + y + lines[j][i] * (FontHeight + 2 * InnerSpacing) + AutoScrollPosition.Y,
                                (int)Math.Ceiling(StandardWidth * scale * (bar.end - bar.start)),
                                FontHeight + 2);
                            e.Graphics.FillRectangle(colorSync ? Brushes.NavajoWhite : Brushes.PaleTurquoise, barBounds);
                            e.Graphics.DrawRectangle(Pens.Black, barBounds);
                        }
                        else
                        {
                            barBounds = new Rectangle(
                                LeftMargin + (int)Math.Floor(StandardWidth * scale * bar.start) + AutoScrollPosition.X,
                                1 + y + lines[j][i] * (FontHeight + 2 * InnerSpacing) + AutoScrollPosition.Y,
                                Int16.MaxValue,
                                FontHeight + 2);
                            Point[] curve = new Point[]
                            {
                                new Point(barBounds.X + FontHeight / 2, barBounds.Y),
                                new Point(barBounds.X - FontHeight / 2, barBounds.Y + FontHeight + 2),
                                new Point(barBounds.X, barBounds.Y + FontHeight + 2),
                                new Point(barBounds.X, barBounds.Y),
                            };
                            e.Graphics.FillPolygon(colorSync ? Brushes.NavajoWhite : Brushes.PaleTurquoise, curve);
                            e.Graphics.DrawPolygon(Pens.Black, curve);
                        }
                        string l1 = bar.seq.ToString() + (!String.IsNullOrEmpty(bar.tag) ? String.Concat(":", bar.tag) : String.Empty);
                        int l1w = TextRenderer.MeasureText(l1, Font, new Size(Int16.MaxValue, FontHeight), TextFormatFlags.NoPadding | TextFormatFlags.Left).Width;
                        TextRenderer.DrawText(
                            e.Graphics,
                            l1,
                            Font,
                            barBounds,
                            Color.Black,
                            Color.Transparent,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        if (bar.frameIndex.HasValue || bar.noteIndex.HasValue)
                        {
                            TextRenderer.DrawText(
                                e.Graphics,
                                String.Format("{0}.{1}", bar.frameIndex, bar.noteIndex),
                                Font,
                                new Rectangle(barBounds.X + l1w, barBounds.Y, barBounds.Width - l1w, barBounds.Height),
                                Color.Gray,
                                Color.Transparent,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        }
                        for (int t = 0; t < bar.ties.Length; t++)
                        {
                            Rectangle barBounds2 = new Rectangle(
                                LeftMargin + (int)Math.Floor(StandardWidth * scale * bar.ties[t].when) + AutoScrollPosition.X,
                                1 + y + lines[j][i] * (FontHeight + 2 * InnerSpacing) + AutoScrollPosition.Y,
                                (int)Math.Ceiling(StandardWidth * scale * (bar.end - bar.ties[t].when)),
                                FontHeight + 2);
                            e.Graphics.DrawLine(colorSync ? Pens.DarkSalmon : Pens.MediumSlateBlue, barBounds2.X, barBounds2.Y + 1, barBounds2.X, barBounds2.Y + barBounds2.Height - 1);
                            TextRenderer.DrawText(
                                e.Graphics,
                                String.Format("{0}.{1}", bar.ties[t].frameIndex, bar.ties[t].noteIndex),
                                Font,
                                barBounds2,
                                Color.Gray,
                                Color.Transparent,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                        }
                    }

                    y += bounds.Height;
                }

                e.Graphics.DrawLine(Pens.Gray, 0, y + AutoScrollPosition.Y, ClientSize.Width, y + AutoScrollPosition.Y);
                y++;

                y += FontHeight;

                AutoScrollMinSize = new Size(
                    LeftMargin + (int)Math.Ceiling(StandardWidth * scale * top.Epochs.Count) + RightMargin,
                    y);
                HorizontalScroll.SmallChange = ClientSize.Width / 8;
            }
            base.OnPaint(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            for (int i = trackRegions.Count - 1; i >= 0; i--)
            {
                if (trackRegions[i].IsVisible(e.Location))
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

            lastToolTipId = -1;
            toolTip.Hide(this);
        }
    }
}
