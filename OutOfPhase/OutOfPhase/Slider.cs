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
    public partial class Slider : UserControl, INotifyPropertyChanged
    {
        private SliderCore core = new SliderCore();
        private Color markColor = SystemColors.ControlDark;
        private bool mouseCapture;

        public Slider()
        {
            InitializeComponent();
        }

        [Category("Appearance"), DefaultValue(typeof(Color), "ControlDark")]
        public Color MarkColor { get { return markColor; } set { markColor = value; Redraw(); } }

        [Category("State"), DefaultValue(0d)]
        public double Low { get { return core.State.Low; } set { core.State.Low = value; ResetMetrics(); } }

        [Category("State"), DefaultValue(1d)]
        public double High { get { return core.State.High; } set { core.State.High = value; ResetMetrics(); } }

        [Category("State"), DefaultValue(SliderScale.Linear)]
        public SliderScale SliderScale { get { return core.State.Scale; } set { core.State.Scale = value; ResetMetrics(); } }

        [Category("State"), DefaultValue(1000)]
        public int Grain { get { return core.State.Grain; } set { core.State.Grain = value; } }

        [Category("State"), DefaultValue(false)]
        public bool ShowLabels { get { return core.ShowLabels; } set { core.ShowLabels = value; Redraw(); } }

        [Category("State"), DefaultValue(0d), Bindable(true)]
        public double Value
        {
            get { return core.Value; }
            set
            {
                core.Value = value;
                Redraw();
                OnValueChangedIfNeeded();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SliderState State
        {
            get { return new SliderState(core.State); }
            set
            {
                using (Graphics graphics = CreateGraphics())
                {
                    core.SetSliderState(value, graphics, Width, Height, Font);
                }
                core.ValueChanged = true;
                Redraw();
                OnValueChangedIfNeeded();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ValueChanged { get { return core.ValueChanged; } set { core.ValueChanged = value; } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            ResetMetrics();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Redraw(e.Graphics);
        }

        private void Redraw()
        {
            using (Graphics graphics = CreateGraphics())
            {
                Redraw(graphics);
            }
        }

        private void Redraw(Graphics graphics)
        {
            core.Draw(graphics, ClientRectangle, BackColor, ForeColor, markColor, Font);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            mouseCapture = true;
            core.OnMouseDown(e, ClientRectangle);
            Redraw();
            OnValueChangedIfNeeded();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (mouseCapture)
            {
                mouseCapture = false;
                core.OnMouseUp(e, ClientRectangle);
                Redraw();
                OnValueChangedIfNeeded();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (mouseCapture)
            {
                core.OnMouseMove(e, ClientRectangle);
                Redraw();
                OnValueChangedIfNeeded();
            }
        }

        private void ResetMetrics()
        {
            using (Graphics graphics = CreateGraphics())
            {
                core.ResetMetrics(graphics, Width, Height, Font);
            }
            Invalidate();
        }

        private void OnValueChangedIfNeeded()
        {
            if (core.ValueChanged)
            {
                core.ValueChanged = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
            }
        }
    }

    public class SliderCore
    {
        private SliderState state = new SliderState();
        private readonly List<Mark> marks = new List<Mark>();
        private bool showLabels;
        private bool valueChanged;

        public SliderScale SliderScale { get { return state.Scale; } }
        public void SetSliderScale(SliderScale scale, Graphics graphics, int width, int height, Font font)
        {
            state.Scale = scale;
            ResetMetrics(graphics, width, height, font);
        }

        public double Value { get { return state.Value; } set { valueChanged = state.Value != value; state.Value = value; } }
        public SliderState State { get { return new SliderState(state); } }
        public void SetSliderState(SliderState state, Graphics graphics, int width, int height, Font font)
        {
            this.state = new SliderState(state);
            ResetMetrics(graphics, width, height, font);
        }

        public bool ValueChanged { get { return valueChanged; } set { valueChanged = value; } }

        public bool ShowLabels { get { return showLabels; } set { showLabels = value; } }


        public SliderCore()
        {
        }

        public SliderCore(SliderState state)
        {
            this.state = state;
        }

        public void Draw(Graphics graphics, Rectangle bounds, Color backColor, Color foreColor, Color markColor, Font font)
        {
            if (!bounds.IsEmpty)
            {
                using (Image image = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                {
                    using (Graphics offscreen = Graphics.FromImage(image))
                    {
                        Draw(offscreen, state, marks, showLabels, backColor, foreColor, markColor, bounds.Width, bounds.Height, font);
                    }
                    graphics.DrawImage(image, bounds);
                }
            }
        }

        private static void Draw(Graphics graphics, SliderState state, List<Mark> marks, bool showLabels, Color backColor, Color foreColor, Color markColor, int width, int height, Font font)
        {
            using (Brush backBrush = new SolidBrush(backColor))
            {
                graphics.FillRectangle(backBrush, 0, 0, width, height);
            }
            if ((state.Scale != SliderScale.Invalid) && (width != 0) && (height != 0))
            {
                using (Pen forePen = new Pen(foreColor))
                {
                    DrawMetrics(graphics, width, 10, foreColor, backColor, markColor, font, marks, showLabels, state);

                    int xx = ValueToPoint(state.Value, state, width);
                    using (Brush foreBrush = new SolidBrush(foreColor))
                    {
                        graphics.FillRectangle(foreBrush, xx - 3, 0, 7, 3);
                        graphics.FillRectangle(foreBrush, xx - 3, 10 - 3 + 1, 7, 3);
                    }
                    graphics.DrawLine(forePen, xx, 0, xx, 10);
                }
            }
        }

        public static void DrawMetrics(Graphics graphics, int width, int verticalTextOffset, Color foreColor, Color backColor, Color markColor, Font font, List<Mark> marks, bool showLabels, SliderState state)
        {
            using (Pen forePen = new Pen(foreColor))
            {
                using (Pen markPen = new Pen(markColor))
                {
                    graphics.DrawLine(markPen, 0, 5, width, 5);
                    for (int i = 0; i < marks.Count; i++)
                    {
                        int x = ValueToPoint(marks[i].Value, state, width);
                        if (showLabels && (marks[i].TextWidth != 0))
                        {
                            int textX = x - marks[i].TextWidth / 2;
                            textX = Math.Max(Math.Min(textX, width - marks[i].TextWidth), 0);
                            MyTextRenderer.DrawText(
                                graphics,
                                marks[i].Value.ToString(),
                                font,
                                new Point(textX, verticalTextOffset),
                                markColor,
                                backColor);
                        }
                        graphics.DrawLine(i != 0 ? markPen : forePen, x, 5 - marks[i].Weight, x, 5 + marks[i].Weight);
                    }
                }
            }
        }

        public void OnMouseDown(MouseEventArgs e, Rectangle bounds)
        {
            this.Value = ValueFromMouseMove(state, e.X - bounds.X, bounds.Width);
            this.valueChanged = true;
        }

        public void OnMouseUp(MouseEventArgs e, Rectangle bounds)
        {
            this.Value = ValueFromMouseMove(state, e.X - bounds.X, bounds.Width);
            this.valueChanged = true;
        }

        public void OnMouseMove(MouseEventArgs e, Rectangle bounds)
        {
            this.Value = ValueFromMouseMove(state, e.X - bounds.X, bounds.Width);
            this.valueChanged = true;
        }

        public static double ValueFromMouseMove(SliderState state, int x, int width)
        {
            return PointToValue(Math.Max(0, Math.Min(width - 1, x)), state, width);
        }

        public void ResetMetrics(Graphics graphics, int width, int height, Font font)
        {
            ResetMetrics(graphics, state, width, marks, font);
        }

        public struct Mark
        {
            public readonly double Value;
            public readonly int TextWidth;
            public readonly int Weight;

            public Mark(double value, int textWidth, int weight)
            {
                this.Value = value;
                this.TextWidth = textWidth;
                this.Weight = weight;
            }
        }

        public static void ResetMetrics(Graphics graphics, SliderState state, int width, List<Mark> marks, Font font)
        {
            marks.Clear();

            double effectiveLow = state.EffectiveLow;
            double effectiveHigh = state.EffectiveHigh;

            const int WeightStep = 1;
            const int WeightTiers = 3;
            int weight = WeightStep * WeightTiers;

            int fontHeight = font.Height;
            Dictionary<double, bool> marked = new Dictionary<double, bool>();

            // prioritize client-requested labels
            for (int i = 0; i < state.LabelsCount; i++)
            {
                double label = state.GetLabelValue(i);
                marks.Add(new Mark(label, 0, weight));
                marked.Add(label, false);
            }

            if ((state.Scale == SliderScale.Linear)
                && (effectiveLow <= 0) && (effectiveHigh >= 0)
                && !marked.ContainsKey(0))
            {
                marks.Add(new Mark(0, 0, weight));
                marked.Add(0, false);
            }
            else if (((state.Scale == SliderScale.Log) || (state.Scale == SliderScale.LogWithZero))
                && (effectiveLow <= 1) && (effectiveHigh >= 1)
                && !marked.ContainsKey(1))
            {
                marks.Add(new Mark(1, 0, weight));
                marked.Add(1, false);
            }

            if (!marked.ContainsKey(effectiveLow))
            {
                marks.Add(new Mark(effectiveLow, 0, weight));
                marked.Add(effectiveLow, false);
            }
            if (!marked.ContainsKey(effectiveHigh))
            {
                marks.Add(new Mark(effectiveHigh, 0, weight));
                marked.Add(effectiveHigh, false);
            }

            int intervalNumerator = 1, intervalDenominator = 1;
            {
                int order = (int)Math.Round(Math.Log10(effectiveHigh - effectiveLow));
                if (order > 0)
                {
                    intervalNumerator = (int)Math.Pow(10, order);
                }
                else
                {
                    intervalDenominator = (int)Math.Pow(10, -order);
                }
            }

            intervalDenominator *= 5;
            int c = 1;

            int targetMarksCount = width / (fontHeight / 2);
            while (marks.Count < targetMarksCount)
            {
                int index = (int)Math.Floor(effectiveLow * intervalDenominator / intervalNumerator);
                double x;
                while ((x = (double)index * intervalNumerator / intervalDenominator) < effectiveHigh)
                {
                    if ((x > effectiveLow) && !marked.ContainsKey(x))
                    {
                        marks.Add(new Mark(x, 0, weight));
                        marked.Add(x, false);
                    }
                    index++;
                }
                weight -= WeightStep;
                if (weight <= 0)
                {
                    break;
                }
                intervalDenominator *= c == 0 ? 5 : 2;
                c = c ^ 1;
            }

            using (Region region = new Region(Rectangle.Empty))
            {
                for (int i = 0; i < marks.Count; i++)
                {
                    int xPos = ValueToPoint(marks[i].Value, state, width);
                    int textWidth = MyTextRenderer.MeasureText(graphics, marks[i].Value.ToString(), font).Width;
                    int textX = xPos - textWidth / 2;
                    textX = Math.Max(Math.Min(textX, width - textWidth), 0);
                    int edgePadding = fontHeight / 4;
                    Rectangle textRect = new Rectangle(textX - edgePadding, 0, textWidth + 2 * edgePadding, 1);
                    if (!region.IsVisible(textRect))
                    {
                        region.Union(textRect);
                        marks[i] = new Mark(marks[i].Value, textWidth, marks[i].Weight);
                    }
                }
            }
        }

        public static double PointToValue(int x, SliderState state, int width)
        {
            int offset = 0;
            if (state.Scale == SliderScale.LogWithZero)
            {
                offset = 1;
                if (x == 0)
                {
                    return 0;
                }
            }

            double xLow = state.Low;
            double xHigh = state.High;
            if ((state.Scale == SliderScale.Log) || (state.Scale == SliderScale.LogWithZero))
            {
                xLow = Math.Log(xLow);
                xHigh = Math.Log(xHigh);
            }

            double v = ((double)(x - (state.Low < state.High ? offset : 0)) / (width - offset - 1)) * (xHigh - xLow) + xLow;

            if ((state.Scale == SliderScale.Log) || (state.Scale == SliderScale.LogWithZero))
            {
                v = Math.Exp(v);
            }

            v = Constrain(v, state);

            return v;
        }

        public static int ValueToPoint(double v, SliderState state, int width)
        {
            int offset = 0;
            if (state.Scale == SliderScale.LogWithZero)
            {
                offset = 1;
                if (v == 0)
                {
                    return 0;
                }
            }

            double xLow = state.Low;
            double xHigh = state.High;
            double xV = v;
            if ((state.Scale == SliderScale.Log) || (state.Scale == SliderScale.LogWithZero))
            {
                xLow = Math.Log(xLow);
                xHigh = Math.Log(xHigh);
                xV = Math.Log(xV);
            }

            double final = Math.Round((xV - xLow) / (xHigh - xLow) * (width - offset - 1)) + (state.Low < state.High ? offset : 0);
            if (Double.IsNaN(final) || Double.IsInfinity(final))
            {
                final = 0;
            }
            return (int)final;
        }

        public static double Constrain(double v, SliderState state)
        {
            if (state.Low < state.High)
            {
                v = Math.Max(state.Low, Math.Min(state.High, Math.Round(v * state.Grain) / state.Grain));
            }
            else
            {
                v = Math.Max(state.High, Math.Min(state.Low, Math.Round(v * state.Grain) / state.Grain));
            }
            return v;
        }
    }

    public enum SliderScale { Invalid, Linear, Log, LogWithZero };
    public class SliderState
    {
        public const int DefaultGrain = 1000;

        private double value = 0;
        private double low = 0;
        private double high = 1;
        private SliderScale scale = SliderScale.Linear;
        private int grain = DefaultGrain;
        private double[] labels;

        public SliderState()
        {
        }

        public SliderState(double value, double low, double high, SliderScale scale, int grain, double[] labels)
        {
            this.value = value;
            this.low = low;
            this.high = high;
            this.scale = scale;
            this.grain = grain;
            this.labels = labels;
        }

        public SliderState(double value, double low, double high, SliderScale scale, int grain)
            : this(value, low, high, scale, grain, null/*labels*/)
        {
        }

        public SliderState(double value, double low, double high, SliderScale scale)
            : this(value, low, high, scale, DefaultGrain, null/*labels*/)
        {
        }

        public SliderState(double low, double high, SliderScale scale, int grain, double[] labels)
            : this(Math.Max(low, Math.Min(high, 0)), low, high, scale, grain, labels)
        {
        }

        public SliderState(double low, double high, SliderScale scale, int grain)
            : this(Math.Max(low, Math.Min(high, 0)), low, high, scale, grain)
        {
        }

        public SliderState(double low, double high, SliderScale scale)
            : this(Math.Max(low, Math.Min(high, 0)), low, high, scale)
        {
        }

        public SliderState(double low, double high, int grain)
            : this(low, high, SliderScale.Linear, grain)
        {
        }

        public SliderState(double low, double high)
            : this(low, high, SliderScale.Linear)
        {
        }

        public SliderState(SliderScale scale)
        {
            this.scale = scale;
        }

        public SliderState(SliderState original)
        {
            this.value = original.value;
            this.low = original.low;
            this.high = original.high;
            this.scale = original.scale;
            this.grain = original.grain;
            this.labels = original.labels != null ? (double[])original.labels.Clone() : null;
        }

        public SliderState WithLabels(double[] labels)
        {
            return new SliderState(value, low, high, scale, grain, labels);
        }

        public double Value { get { return this.value; } set { this.value = value; } }
        public double Low { get { return this.low; } set { this.low = value; } }
        public double High { get { return this.high; } set { this.high = value; } }
        public SliderScale Scale { get { return this.scale; } set { this.scale = value; } }
        public int Grain { get { return this.grain; } set { this.grain = value; } }

        public double EffectiveLow { get { return Math.Min(this.low, this.high); } }
        public double EffectiveHigh { get { return Math.Max(this.low, this.high); } }

        public int LabelsCount { get { return labels != null ? labels.Length : 0; } }
        public double GetLabelValue(int index) { return labels[index]; }
    }
}
