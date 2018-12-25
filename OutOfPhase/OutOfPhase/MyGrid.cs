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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class MyGrid : UserControl, INotifyPropertyChangeCapture
    {
        private Column[] columns = new Column[0];
        private readonly List<Row> rows = new List<Row>();

        private int selectedRowIndex = -1, selectedColumnIndex = -1;

        private int fontHeight;

        private Action<MyGrid, int> createRow;

        private Color selectedBackColor = SystemColors.Highlight;
        private Color selectedForeColor = SystemColors.HighlightText;
        private Color selectedBackInactiveColor = SystemColors.GradientInactiveCaption;
        private Color selectedForeInactiveColor = SystemColors.ControlText;
        private Color pastEndColor = SystemColors.ControlDark;

        private Image offscreenStrip;

        private Cell cellMouseCapture;
        private Rectangle cellMouseCaptureRect;
        private int cellMouseCaptureRowIndex;
        private int cellMouseCaptureColumnIndex;

        private Control editControl;
        private Cell cellEdit;
        private Rectangle cellEditRect;
        private int cellEditRowIndex;
        private int cellEditColumnIndex;

        private int deferredUpdates;
        private int firstRow;
        private int lastRow;


        public event PropertyChangeCaptureHandler PropertyChangeCapture;

        private void BeginCapture(Cell cell)
        {
            PropertyChangeCapture?.Invoke(cell, new PropertyChangeCaptureEventArgs(true));
        }

        private void EndCapture(Cell cell)
        {
            PropertyChangeCapture?.Invoke(cell, new PropertyChangeCaptureEventArgs(false));
        }


        private class Column
        {
            private readonly int width;
            private readonly int offset;

            public int Width { get { return width; } }
            public int Offset { get { return offset; } }

            public Column(int width, int offset)
            {
                this.width = width;
                this.offset = offset;
            }
        }

        private class Row
        {
            private readonly Cell[] cells;

            public IReadOnlyList<Cell> Cells { get { return cells; } }

            public Row()
            {
                cells = new Cell[0];
            }

            public Row(IList<Cell> cells)
            {
                this.cells = new Cell[cells.Count];
                cells.CopyTo(this.cells, 0);
            }
        }

        private class HeadingRow : Row
        {
            public HeadingRow(string text)
                : base(new Cell[] { new LabelCell(text) })
            {
            }
        }

        public struct DrawContext
        {
            private readonly MyGrid Grid; // TODO: remove
            private readonly int RowIndex; // TODO: remove
            private readonly int ColumnIndex; // TODO: remove
            public readonly Graphics Graphics;
            public readonly Font Font;
            public readonly Color BackColor;
            public readonly Color ForeColor;
            public readonly Color ExtraColor;
            public readonly Rectangle Rect;

            public bool RequestingRedraw;
            public bool RequestingBeginEdit;
            public bool RequestingBeginEditSelectAll;

            public DrawContext(MyGrid grid, int rowIndex, int columnIndex, Graphics graphics, Font font, (Color, Color, Color) backForeExtraColors, Rectangle rect)
            {
                this.Grid = grid;
                this.RowIndex = rowIndex;
                this.ColumnIndex = columnIndex;
                this.Graphics = graphics;
                this.Font = font;
                (this.BackColor, this.ForeColor, this.ExtraColor) = backForeExtraColors;
                this.Rect = rect;

                this.RequestingRedraw = false;
                this.RequestingBeginEdit = false;
                this.RequestingBeginEditSelectAll = false;
            }
        }

        public class Cell : INotifyPropertyChanged, IDisposable
        {
            public virtual void Init(ref DrawContext context) { }

            public virtual object GetValue() { return null; }
            public virtual bool SetValue(object o) { return false; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
            }

            public virtual bool HandlesInput { get { return false; } }

            public virtual void GetColors(bool selected, bool focused, Color unselectedBackColor, Color unselectedForeColor, Color selectedBackColor, Color selectedForeColor, Color selectedInactiveBackColor, Color selectedInactiveForeColor, out Color effectiveBackColor, out Color effectiveForeColor, out Color effectiveExtraColor)
            {
                if (!selected)
                {
                    effectiveBackColor = unselectedBackColor;
                    effectiveForeColor = unselectedForeColor;
                }
                else
                {
                    if (focused)
                    {
                        effectiveBackColor = selectedBackColor;
                        effectiveForeColor = selectedForeColor;
                    }
                    else
                    {
                        effectiveBackColor = selectedInactiveBackColor;
                        effectiveForeColor = selectedInactiveForeColor;
                    }
                }
                effectiveExtraColor = effectiveForeColor;
            }

            public virtual void DrawInit(ref DrawContext context) { }
            public virtual void Draw(ref DrawContext context) { }

            public virtual void OnMouseClick(MouseEventArgs e, ref DrawContext context) { }
            public virtual void OnMouseDoubleClick(MouseEventArgs e, ref DrawContext context) { }
            public virtual void OnMouseDown(MouseEventArgs e, ref DrawContext context) { }
            public virtual void OnMouseMove(MouseEventArgs e, ref DrawContext context) { }
            public virtual void OnMouseUp(MouseEventArgs e, ref DrawContext context) { }
            //public virtual void OnMouseEnter(EventArgs e, ref DrawContext context) { }
            //public virtual void OnMouseLeave(EventArgs e, ref DrawContext context) { }

            //public virtual void OnPreviewKeyDown(PreviewKeyDownEventArgs e, ref DrawContext context) { }
            //public virtual bool ProcessCmdKey(ref Message msg, Keys keyData, ref DrawContext context) { return false; }
            //public virtual bool ProcessKeyPreview(ref Message m, ref DrawContext context) { return false; }
            public virtual bool IsInputKey(Keys keyData) { return false; }
            public virtual void OnKeyDown(KeyEventArgs e, ref DrawContext context) { }
            //public virtual void OnKeyPress(KeyPressEventArgs e, ref DrawContext context) { }
            //public virtual void OnKeyUp(KeyEventArgs e, ref DrawContext context) { }

            public virtual Control BeginEdit(ref DrawContext context, Action<bool> endEdit, bool selectAll) { return null; }
            public virtual void EndEdit(Control editControl, ref DrawContext context, bool commit) { }
            public virtual void CommitWithoutEndEdit(Control editControl, ref DrawContext context) { }

            public virtual void Dispose() { }
        }

        public class LabelCell : Cell
        {
            private string text;

            public LabelCell()
            {
            }

            public LabelCell(string text)
            {
                this.text = text;
            }

            public override object GetValue()
            {
                return text;
            }

            public override bool SetValue(object o)
            {
                string s = (string)o;
                if (!String.Equals(s, this.text))
                {
                    this.text = s;
                    return true;
                }
                return false;
            }

            public override void Draw(ref DrawContext context)
            {
                MyTextRenderer.DrawText(
                    context.Graphics,
                    text,
                    context.Font,
                    context.Rect,
                    context.ForeColor,
                    context.BackColor);
            }
        }

        public class TextEditCell : Cell
        {
            private string text;

            public TextEditCell()
            {
            }

            public TextEditCell(string text)
            {
                this.text = text;
            }

            public override object GetValue()
            {
                return text;
            }

            public override bool SetValue(object o)
            {
                string s = (string)o;
                if (!String.Equals(s, this.text))
                {
                    this.text = s;
                    OnPropertyChanged("Value");
                    return true;
                }
                return false;
            }

            public override void Draw(ref DrawContext context)
            {
                MyTextRenderer.DrawText(
                    context.Graphics,
                    text,
                    context.Font,
                    context.Rect,
                    context.ForeColor,
                    context.BackColor);
            }

            public override void OnMouseDown(MouseEventArgs e, ref DrawContext context)
            {
                context.RequestingBeginEdit = true;
            }

            public override void OnKeyDown(KeyEventArgs e, ref DrawContext context)
            {
                if ((e.KeyCode == Keys.F2) || (e.KeyCode == Keys.Return))
                {
                    context.RequestingRedraw = true;
                    context.RequestingBeginEdit = true;
                    e.Handled = true;
                }
            }

            public override Control BeginEdit(ref DrawContext context, Action<bool> endEdit, bool selectAll)
            {
                TextBox editControl = new TextBox();
                editControl.Bounds = context.Rect;
                editControl.BorderStyle = BorderStyle.None;
                editControl.Text = this.text;
                if (selectAll)
                {
                    editControl.SelectAll();
                }
                editControl.KeyDown += delegate (object sender, KeyEventArgs e)
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        endEdit(false/*commit*/);
                    }
                    else if (e.KeyCode == Keys.Return)
                    {
                        endEdit(true/*commit*/);
                    }
                };
                return editControl;
            }

            public override void EndEdit(Control editControl, ref DrawContext context, bool commit)
            {
                if (commit)
                {
                    CommitWithoutEndEdit(editControl, ref context);
                }
                editControl.Dispose();
            }

            public override void CommitWithoutEndEdit(Control editControl, ref DrawContext context)
            {
                context.RequestingRedraw = SetValue(((TextBox)editControl).Text);
            }
        }

        public class BoolOptionCell : Cell
        {
            private bool value;
            private readonly string falseText;
            private readonly string trueText;

            public BoolOptionCell(string falseText, string trueText)
            {
                this.falseText = falseText;
                this.trueText = trueText;
            }

            public BoolOptionCell(string falseText, string trueText, bool value)
                : this(falseText, trueText)
            {
                this.value = value;
            }

            public override object GetValue()
            {
                return value;
            }

            public override bool SetValue(object o)
            {
                bool b = (bool)o;
                if (this.value != b)
                {
                    this.value = b;
                    OnPropertyChanged("Value");
                    return true;
                }
                return false;
            }

            private Size GetFalseTextSize(ref DrawContext context)
            {
                return MyTextRenderer.MeasureText(context.Graphics, falseText, context.Font);
            }

            private Size GetTrueTextSize(ref DrawContext context)
            {
                return MyTextRenderer.MeasureText(context.Graphics, trueText, context.Font);
            }

            private Rectangle GetFalseTextBounds(ref DrawContext context)
            {
                Rectangle rect = new Rectangle(Point.Empty, GetFalseTextSize(ref context));
                rect.Offset(context.Rect.Location);
                return rect;
            }

            private Rectangle GetTrueTextBounds(ref DrawContext context)
            {
                Rectangle rect = new Rectangle(Point.Empty, GetTrueTextSize(ref context));
                rect.Offset(context.Rect.Location);
                rect.X += context.Font.Height + GetFalseTextSize(ref context).Width;
                return rect;
            }

            public override void GetColors(bool selected, bool focused, Color unselectedBackColor, Color unselectedForeColor, Color selectedBackColor, Color selectedForeColor, Color selectedInactiveBackColor, Color selectedInactiveForeColor, out Color effectiveBackColor, out Color effectiveForeColor, out Color effectiveExtraColor)
            {
                base.GetColors(selected, focused, unselectedBackColor, unselectedForeColor, selectedBackColor, selectedForeColor, selectedInactiveBackColor, selectedInactiveForeColor, out effectiveBackColor, out effectiveForeColor, out effectiveExtraColor);
                if (selected && focused)
                {
                    effectiveBackColor = unselectedBackColor;
                    effectiveForeColor = selectedBackColor;
                }
            }

            public override void Draw(ref DrawContext context)
            {
                MyTextRenderer.DrawText(
                    context.Graphics,
                    falseText,
                    context.Font,
                    GetFalseTextBounds(ref context),
                    !value ? context.BackColor : context.ForeColor,
                    !value ? context.ForeColor : context.BackColor);
                MyTextRenderer.DrawText(
                    context.Graphics,
                    trueText,
                    context.Font,
                    GetTrueTextBounds(ref context),
                    value ? context.BackColor : context.ForeColor,
                    value ? context.ForeColor : context.BackColor);
            }

            public override void OnMouseDown(MouseEventArgs e, ref DrawContext context)
            {
                if (GetFalseTextBounds(ref context).Contains(e.Location))
                {
                    context.RequestingRedraw = SetValue(false);
                }
                else if (GetTrueTextBounds(ref context).Contains(e.Location))
                {
                    context.RequestingRedraw = SetValue(true);
                }
            }

            public override void OnKeyDown(KeyEventArgs e, ref DrawContext context)
            {
                if ((e.KeyCode == Keys.Space) || (e.KeyCode == Keys.Return))
                {
                    SetValue(!value);
                    context.RequestingRedraw = true;
                    e.Handled = true;
                }
            }
        }

        public class BoolToggleCell : Cell
        {
            private bool? value;
            private readonly string text;

            public BoolToggleCell(string text)
            {
                this.text = text;
            }

            public BoolToggleCell(string text, bool? value)
                : this(text)
            {
                this.value = value;
            }

            public override object GetValue()
            {
                return value;
            }

            public override bool SetValue(object o)
            {
                bool? b = (bool?)o;
                if ((this.value.HasValue != b.HasValue) || (this.value.HasValue && (this.value.Value != b.Value)))
                {
                    this.value = b;
                    OnPropertyChanged("Value");
                    return true;
                }
                return false;
            }

            private Size GetTextSize(ref DrawContext context)
            {
                return MyTextRenderer.MeasureText(context.Graphics, text, context.Font);
            }

            private Rectangle GetTextBounds(ref DrawContext context)
            {
                Rectangle rect = new Rectangle(Point.Empty, GetTextSize(ref context));
                rect.Offset(context.Rect.Location);
                return rect;
            }

            public override void GetColors(bool selected, bool focused, Color unselectedBackColor, Color unselectedForeColor, Color selectedBackColor, Color selectedForeColor, Color selectedInactiveBackColor, Color selectedInactiveForeColor, out Color effectiveBackColor, out Color effectiveForeColor, out Color effectiveExtraColor)
            {
                base.GetColors(selected, focused, unselectedBackColor, unselectedForeColor, selectedBackColor, selectedForeColor, selectedInactiveBackColor, selectedInactiveForeColor, out effectiveBackColor, out effectiveForeColor, out effectiveExtraColor);
                if (selected && focused)
                {
                    effectiveBackColor = unselectedBackColor;
                    effectiveForeColor = selectedBackColor;
                }
            }

            public override void Draw(ref DrawContext context)
            {
                Rectangle textRect = GetTextBounds(ref context);
                bool notFlipColors = !value.HasValue || !value.Value;
                MyTextRenderer.DrawText(
                    context.Graphics,
                    text,
                    context.Font,
                    textRect,
                    notFlipColors ? context.ForeColor : context.BackColor,
                    notFlipColors ? context.BackColor : context.ForeColor);
                if (!value.HasValue)
                {
                    using (Pen forePen = new Pen(context.ForeColor))
                    {
                        context.Graphics.DrawRectangle(
                            forePen,
                            textRect);
                    }
                }
            }

            public override void OnMouseDown(MouseEventArgs e, ref DrawContext context)
            {
                if (GetTextBounds(ref context).Contains(e.Location))
                {
                    SetValue(!value);
                    context.RequestingRedraw = true;
                }
            }

            public override void OnKeyDown(KeyEventArgs e, ref DrawContext context)
            {
                if ((e.KeyCode == Keys.Space) || (e.KeyCode == Keys.Return))
                {
                    SetValue(!value);
                    context.RequestingRedraw = true;
                    e.Handled = true;
                }
            }
        }

        public class OptionCell<T> : Cell where T : struct
        {
            private int value;
            private readonly (string, T)[] values;

            public OptionCell((string, T)[] values)
            {
                this.values = values;
            }

            public OptionCell((string, T)[] values, T value)
                : this(values)
            {
                this.value = Array.FindIndex(values, kvp => EqualityComparer<T>.Default.Equals(kvp.Item2, value));
                if (this.value < 0)
                {
                    throw new ArgumentException();
                }
            }

            public override object GetValue()
            {
                return values[value].Item2;
            }

            private bool SetValueIndex(int index)
            {
                if (unchecked((uint)index >= (uint)values.Length))
                {
                    throw new ArgumentException();
                }
                if (this.value != index)
                {
                    this.value = index;
                    OnPropertyChanged("Value");
                    return true;
                }
                return false;
            }

            public override bool SetValue(object o)
            {
                T t = (T)o;
                return SetValueIndex(Array.FindIndex(values, kvp => EqualityComparer<T>.Default.Equals(kvp.Item2, t)));
            }

            private Size GetTextSize(ref DrawContext context, int i)
            {
                return MyTextRenderer.MeasureText(context.Graphics, values[i].Item1, context.Font);
            }

            private Rectangle[] GetTextBounds(ref DrawContext context)
            {
                Rectangle[] rects = new Rectangle[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    rects[i] = new Rectangle(Point.Empty, GetTextSize(ref context, i));
                    if (i > 0)
                    {
                        rects[i].Offset(rects[i - 1].Right + context.Font.Height, 0);
                    }
                }
                for (int i = 0; i < rects.Length; i++)
                {
                    rects[i].Offset(context.Rect.Location);
                }
                return rects;
            }

            public override void GetColors(bool selected, bool focused, Color unselectedBackColor, Color unselectedForeColor, Color selectedBackColor, Color selectedForeColor, Color selectedInactiveBackColor, Color selectedInactiveForeColor, out Color effectiveBackColor, out Color effectiveForeColor, out Color effectiveExtraColor)
            {
                base.GetColors(selected, focused, unselectedBackColor, unselectedForeColor, selectedBackColor, selectedForeColor, selectedInactiveBackColor, selectedInactiveForeColor, out effectiveBackColor, out effectiveForeColor, out effectiveExtraColor);
                if (selected && focused)
                {
                    effectiveBackColor = unselectedBackColor;
                    effectiveForeColor = selectedBackColor;
                }
            }

            public override void Draw(ref DrawContext context)
            {
                Rectangle[] rects = GetTextBounds(ref context);
                for (int i = 0; i < values.Length; i++)
                {
                    MyTextRenderer.DrawText(
                        context.Graphics,
                        values[i].Item1,
                        context.Font,
                        rects[i],
                        value == i ? context.BackColor : context.ForeColor,
                        value == i ? context.ForeColor : context.BackColor);
                }
            }

            public override void OnMouseDown(MouseEventArgs e, ref DrawContext context)
            {
                Rectangle[] rects = GetTextBounds(ref context);
                for (int i = 0; i < rects.Length; i++)
                {
                    if (rects[i].Contains(e.Location))
                    {
                        context.RequestingRedraw = SetValueIndex(i);
                        break;
                    }
                }
            }

            public override void OnKeyDown(KeyEventArgs e, ref DrawContext context)
            {
                if ((e.KeyCode >= Keys.D1) && (e.KeyCode <= Keys.D9))
                {
                    int i = e.KeyCode - Keys.D1;
                    if (i < values.Length)
                    {
                        context.RequestingRedraw = SetValueIndex(i);
                        e.Handled = true;
                    }
                }
                else if (e.KeyCode == Keys.Space)
                {
                    context.RequestingRedraw = SetValueIndex((value + 1) % values.Length);
                    e.Handled = true;
                }
            }
        }

        public class SliderCell : Cell
        {
            private SliderCore core;

            public SliderCell()
            {
                core = new SliderCore();
            }

            public SliderCell(SliderState state)
            {
                core = new SliderCore(state);
            }

            public override object GetValue()
            {
                return core.Value;
            }

            public override bool SetValue(object o)
            {
                double d = (double)o;
                if (core.Value != d)
                {
                    core.Value = d;
                    OnPropertyChanged("Value");
                    return true;
                }
                return false;
            }

            public override void GetColors(bool selected, bool focused, Color unselectedBackColor, Color unselectedForeColor, Color selectedBackColor, Color selectedForeColor, Color selectedInactiveBackColor, Color selectedInactiveForeColor, out Color effectiveBackColor, out Color effectiveForeColor, out Color effectiveExtraColor)
            {
                base.GetColors(selected, focused, unselectedBackColor, unselectedForeColor, selectedBackColor, selectedForeColor, selectedInactiveBackColor, selectedInactiveForeColor, out effectiveBackColor, out effectiveForeColor, out effectiveExtraColor);
                if (selected && focused)
                {
                    effectiveBackColor = unselectedBackColor;
                    effectiveForeColor = selectedBackColor;
                }
            }

            public override void DrawInit(ref DrawContext context)
            {
                core.ResetMetrics(context.Graphics, context.Rect.Width, context.Rect.Height, context.Font);
            }

            public override void Draw(ref DrawContext context)
            {
                core.Draw(context.Graphics, context.Rect, context.BackColor, context.ForeColor, SystemColors.ControlDark, context.Font);
            }

            public override void OnMouseDown(MouseEventArgs e, ref DrawContext context)
            {
                double lastValue = core.Value;
                core.OnMouseDown(e, context.Rect);
                if (lastValue != core.Value)
                {
                    context.RequestingRedraw = true;
                    OnPropertyChanged("Value");
                }
            }

            public override void OnMouseUp(MouseEventArgs e, ref DrawContext context)
            {
                double lastValue = core.Value;
                core.OnMouseUp(e, context.Rect);
                if (lastValue != core.Value)
                {
                    context.RequestingRedraw = true;
                    OnPropertyChanged("Value");
                }
            }

            public override void OnMouseMove(MouseEventArgs e, ref DrawContext context)
            {
                double lastValue = core.Value;
                core.OnMouseMove(e, context.Rect);
                if (lastValue != core.Value)
                {
                    context.RequestingRedraw = true;
                    OnPropertyChanged("Value");
                }
            }
        }


        public MyGrid()
        {
            InitializeComponent();

            fontHeight = Font.Height;

            Disposed += MyGrid_Disposed;
        }

        private void MyGrid_Disposed(object sender, EventArgs e)
        {
            ClearDrawingResources();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            VerticalScroll.SmallChange = fontHeight;
        }

        [Category("Appearance"), DefaultValue(typeof(Color), "ControlDark")]
        public Color PastEndColor { get { return pastEndColor; } set { pastEndColor = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "Highlight")]
        public Color SelectedBackColor { get { return selectedBackColor; } set { selectedBackColor = value; } }
        [Category("Appearance"), DefaultValue(typeof(Color), "HighlightText")]
        public Color SelectedForeColor { get { return selectedForeColor; } set { selectedForeColor = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "GradientInactiveCaption")]
        public Color SelectedBackColorInactive { get { return selectedBackInactiveColor; } set { selectedBackInactiveColor = value; } }
        [Category("Appearance"), DefaultValue(typeof(Color), "ControlText")]
        public Color SelectedForeColorInactive { get { return selectedForeInactiveColor; } set { selectedForeInactiveColor = value; } }

        public event Action<object, EventArgs> SelectionChanged;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<MyGrid, int> CreateRow { get { return this.createRow; } set { this.createRow = value; RefreshAfterLayoutChange(); } }


        public void DeferUpdates()
        {
            if (deferredUpdates == 0)
            {
                firstRow = Int32.MaxValue;
                lastRow = Int32.MinValue;
            }
            deferredUpdates++;
        }

        public void UndeferUpdates()
        {
            deferredUpdates--;
            if (deferredUpdates == 0)
            {
                ResetAutoscrollSize();
                DrawRange(firstRow, Math.Max(lastRow + 1, Int32.MaxValue));
            }
        }

        private void RefreshAfterLayoutChange()
        {
            ClearDrawingResources(); // recreate offscreen strip
            ResetAutoscrollSize();
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RefreshAfterLayoutChange();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            RefreshAfterLayoutChange();

            fontHeight = FontHeight;
            if (!Program.Config.EnableDirectWrite)
            {
                fontHeight = TextEditor.FontHeightHack.GetActualFontHeight(Font, fontHeight);
            }

            VerticalScroll.SmallChange = fontHeight;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // we repaint entire surface - so suppress background fill to reduce flicker
            if (!DesignMode)
            {
                return;
            }

            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            EnsureDrawingResources();

            // custom paint code here
            if (!DesignMode)
            {
                Draw(pe.Graphics);
            }
            else
            {
                // draw X to indicate control is operating but has no content (vs. failing to start)
                EnsureDrawingResources();
                using (Brush backBrush = new SolidBrush(BackColor))
                {
                    pe.Graphics.FillRectangle(backBrush, ClientRectangle);
                }
                using (Brush foreBrush = new SolidBrush(ForeColor))
                {
                    const int T = 3;
                    pe.Graphics.FillPolygon(foreBrush, new Point[] { new Point(0 - T, 0 + T), new Point(Width - T, Height + T), new Point(Width + T, Height - T), new Point(0 + T, 0 - T) });
                    pe.Graphics.FillPolygon(foreBrush, new Point[] { new Point(Width - T, 0 - T), new Point(0 - T, Height - T), new Point(0 + T, Height + T), new Point(Width + T, 0 + T) });
                }
            }

            // Calling the base class OnPaint
            base.OnPaint(pe);
        }

        private void ClearDrawingResources()
        {
            if (offscreenStrip != null)
            {
                offscreenStrip.Dispose();
                offscreenStrip = null;
            }
        }

        private void EnsureDrawingResources()
        {
            if ((offscreenStrip == null) || (offscreenStrip.Width != Width) || (offscreenStrip.Height != FontHeight))
            {
                if (offscreenStrip != null)
                {
                    offscreenStrip.Dispose();
                }
                offscreenStrip = new Bitmap(Width, fontHeight, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            }
        }

        private bool IsSelected(int row, int column)
        {
            if ((selectedRowIndex < 0) || (selectedColumnIndex < 0))
            {
                return false;
            }
            return (row == selectedRowIndex) && (column == selectedColumnIndex);
        }

        private (Color, Color, Color) GetEffectiveColors(int rowIndex, int columnIndex)
        {
            Color effectiveBackColor, effectiveForeColor, effectiveExtraColor;
            rows[rowIndex].Cells[columnIndex].GetColors(
                IsSelected(rowIndex, columnIndex),
                Focused,
                BackColor,
                ForeColor,
                selectedBackColor,
                selectedForeColor,
                selectedBackInactiveColor,
                selectedForeInactiveColor,
                out effectiveBackColor,
                out effectiveForeColor,
                out effectiveExtraColor);
            return (effectiveBackColor, effectiveForeColor, effectiveExtraColor);
        }

        private void DrawOnePrimitive(Graphics graphics, int rowIndex)
        {
            EnsureDrawingResources();

            Rectangle rect = new Rectangle(0, rowIndex * fontHeight, TotalWidth, fontHeight);
            rect.Offset(AutoScrollPosition);
            if (graphics.IsVisible(rect))
            {
                using (Graphics graphics2 = Graphics.FromImage(offscreenStrip))
                {
                    if ((rowIndex == rows.Count) && (createRow != null))
                    {
                        using (Brush ghostBrush = new HatchBrush(HatchStyle.Percent50, pastEndColor, BackColor))
                        {
                            graphics2.FillRectangle(ghostBrush, new Rectangle(Point.Empty, offscreenStrip.Size));
                        }
                    }
                    else if ((rowIndex >= 0) && (rowIndex < rows.Count))
                    {
                        using (Brush backBrush = new SolidBrush(BackColor))
                        {
                            graphics2.FillRectangle(backBrush, new Rectangle(Point.Empty, offscreenStrip.Size));
                        }

                        Row row = rows[rowIndex];

                        int xOffset = 0;
                        for (int columnIndex = 0; columnIndex < row.Cells.Count; columnIndex++)
                        {
                            Cell cell = row.Cells[columnIndex];

                            int columnWidth = row is HeadingRow ? TotalWidth : columns[columnIndex].Width;
                            Rectangle cellRect = new Rectangle(rect.X + xOffset, 0, columnWidth, fontHeight);
                            xOffset += columnWidth;
                            if (cellRect.IntersectsWith(new Rectangle(Point.Empty, offscreenStrip.Size)))
                            {
                                DrawContext context = new DrawContext(
                                    this,
                                    rowIndex,
                                    columnIndex,
                                    graphics2,
                                    Font,
                                    GetEffectiveColors(rowIndex, columnIndex),
                                    cellRect);
                                graphics2.SetClip(cellRect);
                                using (Brush backBrush = new SolidBrush(context.BackColor))
                                {
                                    graphics2.FillRectangle(backBrush, cellRect);
                                }
                                cell.Draw(ref context);
                            }
                        }
                    }
                    else
                    {
                        using (Brush pastEndBrush = new SolidBrush(pastEndColor))
                        {
                            graphics2.FillRectangle(pastEndBrush, new Rectangle(Point.Empty, offscreenStrip.Size));
                        }
                    }
                }

                graphics.DrawImage(offscreenStrip, new Rectangle(new Point(0, rect.Y), new Size(Width, fontHeight)));
            }
        }

        private void DrawOne(int rowIndex)
        {
            using (Graphics graphics = CreateGraphics())
            {
                DrawOnePrimitive(graphics, rowIndex);
            }
        }

        private void DrawRange(int startRowIndex, int endRowIndex)
        {
            using (Graphics graphics = CreateGraphics())
            {
                startRowIndex = Math.Max(0, Math.Max(FirstVisibleRow, startRowIndex));
                endRowIndex = (int)Math.Min((uint)LastVisibleRow, (uint)endRowIndex + 1);
                for (int rowIndex = startRowIndex; rowIndex < endRowIndex; rowIndex++)
                {
                    DrawOnePrimitive(graphics, rowIndex);
                }
            }
        }

        private int FirstVisibleRow { get { return -AutoScrollPosition.Y / fontHeight; } }
        private int LastVisibleRow { get { return (-AutoScrollPosition.Y + Height + fontHeight - 1) / fontHeight; } }

        private void Draw(Graphics graphics)
        {
            int firstVisible = FirstVisibleRow;
            int lastVisible = LastVisibleRow;
            for (int rowIndex = firstVisible; rowIndex < lastVisible; rowIndex++)
            {
                DrawOnePrimitive(graphics, rowIndex);
            }
        }

        private void Draw()
        {
            using (Graphics graphics = CreateGraphics())
            {
                Draw(graphics);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate(); // selection color and decoration changes
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate(); // selection color and decoration changes
            base.OnLostFocus(e);
        }

        private Rectangle GetCellRectangle(int rowIndex, int columnIndex)
        {
            return new Rectangle(columns[columnIndex].Offset, rowIndex * FontHeight, columns[columnIndex].Width, fontHeight);
        }

        private enum Hit { None, Cell, Ghost };
        private Hit HitTest(Point e, Graphics graphics, out int rowIndexOut, out int columnIndexOut, out Rectangle boundsOut, out Cell cellOut, out DrawContext contextOut)
        {
            contextOut = new DrawContext();
            rowIndexOut = 0;
            columnIndexOut = 0;
            boundsOut = new Rectangle();
            cellOut = null;

            int x = e.X - AutoScrollPosition.X;
            int y = e.Y - AutoScrollPosition.Y;
            int rowIndex = y / fontHeight;
            if (rowIndex < 0)
            {
                return Hit.None;
            }

            if (rowIndex >= rows.Count)
            {
                rowIndex = rows.Count;
            }

            Row row = rowIndex < rows.Count ? rows[rowIndex] : null;
            if (!(row is HeadingRow))
            {
                for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                {
                    Rectangle cellRect = GetCellRectangle(rowIndex, columnIndex);
                    if (cellRect.Contains(x, y))
                    {
                        cellOut = rowIndex < rows.Count ? row.Cells[columnIndex] : null;
                        boundsOut = cellRect;
                        rowIndexOut = rowIndex;
                        columnIndexOut = columnIndex;
                        contextOut = new DrawContext(
                            this,
                            rowIndex,
                            columnIndex,
                            graphics,
                            Font,
                            rowIndex < rows.Count ? GetEffectiveColors(rowIndex, columnIndex) : (ForeColor, BackColor, BackColor),
                            cellRect);
                        return rowIndex < rows.Count ? Hit.Cell : Hit.Ghost;
                    }
                }
            }
            return Hit.None;
        }

        private MouseEventArgs ShiftLocationMouseEventArgs(MouseEventArgs e)
        {
            return new MouseEventArgs(e.Button, e.Clicks, e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y, e.Delta);
        }

        public delegate void OnMouseEventHandler(object sender, MouseEventArgs e, int rowIndex, int columnIndex, out bool handled);
        public event OnMouseEventHandler OnMouseDownEvent;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            using (Graphics graphics = CreateGraphics())
            {
                DrawContext context;
                Hit hit = HitTest(e.Location, graphics, out cellMouseCaptureRowIndex, out cellMouseCaptureColumnIndex, out cellMouseCaptureRect, out cellMouseCapture, out context);
                if ((hit == Hit.Ghost) && (createRow != null))
                {
                    Debug.Assert(cellMouseCapture == null);
                    createRow(this, cellMouseCaptureRowIndex);
                    SetSelection(cellMouseCaptureRowIndex, cellMouseCaptureColumnIndex);
                    BeginEdit(cellMouseCaptureRowIndex, cellMouseCaptureColumnIndex, false/*selectAll*/);
                }
                else if (hit == Hit.Cell)
                {
                    Debug.Assert(cellMouseCapture != null);
                    BeginCapture(cellMouseCapture);

                    if ((cellEdit != null) && (cellMouseCapture != cellEdit))
                    {
                        EndEdit(true/*commit*/);
                    }

                    MouseEventArgs eShifted = ShiftLocationMouseEventArgs(e);

                    bool handled = false;
                    OnMouseDownEvent?.Invoke(this, eShifted, cellMouseCaptureRowIndex, cellMouseCaptureColumnIndex, out handled);
                    if (handled)
                    {
                        return;
                    }

                    SetSelection(cellMouseCaptureRowIndex, cellMouseCaptureColumnIndex);
                    cellMouseCapture.OnMouseDown(eShifted, ref context);
                    if (context.RequestingRedraw)
                    {
                        DrawOne(cellMouseCaptureRowIndex);
                    }
                    if (context.RequestingBeginEdit)
                    {
                        BeginEdit(cellMouseCaptureRowIndex, cellMouseCaptureColumnIndex, context.RequestingBeginEditSelectAll);
                    }
                }
            }
        }

        public event OnMouseEventHandler OnMouseMoveEvent;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (cellMouseCapture != null)
            {
                int x = e.X - AutoScrollPosition.X;
                int y = e.Y - AutoScrollPosition.Y;
                using (Graphics graphics = CreateGraphics())
                {
                    MouseEventArgs eShifted = ShiftLocationMouseEventArgs(e);

                    bool handled = false;
                    OnMouseMoveEvent?.Invoke(this, eShifted, cellMouseCaptureRowIndex, cellMouseCaptureColumnIndex, out handled);
                    if (handled)
                    {
                        return;
                    }

                    DrawContext context = new DrawContext(
                        this,
                        cellMouseCaptureRowIndex,
                        cellMouseCaptureColumnIndex,
                        graphics,
                        Font,
                        GetEffectiveColors(cellMouseCaptureRowIndex, cellMouseCaptureColumnIndex),
                        cellMouseCaptureRect);
                    cellMouseCapture.OnMouseMove(eShifted, ref context);
                    if (context.RequestingRedraw)
                    {
                        DrawOne(cellMouseCaptureRowIndex);
                    }
                }
            }
        }

        public event OnMouseEventHandler OnMouseUpEvent;

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (cellMouseCapture != null)
            {
                try
                {
                    int x = e.X - AutoScrollPosition.X;
                    int y = e.Y - AutoScrollPosition.Y;
                    using (Graphics graphics = CreateGraphics())
                    {
                        MouseEventArgs eShifted = ShiftLocationMouseEventArgs(e);

                        bool handled = false;
                        OnMouseUpEvent?.Invoke(this, eShifted, cellMouseCaptureRowIndex, cellMouseCaptureColumnIndex, out handled);
                        if (handled)
                        {
                            return;
                        }

                        DrawContext context = new DrawContext(
                            this,
                            cellMouseCaptureRowIndex,
                            cellMouseCaptureColumnIndex,
                            graphics,
                            Font,
                            GetEffectiveColors(cellMouseCaptureRowIndex, cellMouseCaptureColumnIndex),
                            cellMouseCaptureRect);
                        cellMouseCapture.OnMouseUp(ShiftLocationMouseEventArgs(e), ref context);
                        if (context.RequestingRedraw)
                        {
                            DrawOne(cellMouseCaptureRowIndex);
                        }
                    }
                }
                finally
                {
                    EndCapture(cellMouseCapture);
                    cellMouseCapture = null;
                }
            }
        }

        public event OnMouseEventHandler OnMouseClickEvent;

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            using (Graphics graphics = CreateGraphics())
            {
                DrawContext context;
                int cellRowIndex, cellColumnIndex;
                Rectangle cellRect;
                Cell cell;
                if (Hit.Cell == HitTest(e.Location, graphics, out cellRowIndex, out cellColumnIndex, out cellRect, out cell, out context))
                {
                    MouseEventArgs eShifted = ShiftLocationMouseEventArgs(e);

                    bool handled = false;
                    OnMouseClickEvent?.Invoke(this, eShifted, cellRowIndex, cellColumnIndex, out handled);
                    if (handled)
                    {
                        return;
                    }

                    cell.OnMouseClick(eShifted, ref context);
                    if (context.RequestingRedraw)
                    {
                        DrawOne(cellRowIndex);
                    }
                    if (context.RequestingBeginEdit)
                    {
                        BeginEdit(cellRowIndex, cellColumnIndex, context.RequestingBeginEditSelectAll);
                    }
                }
            }
        }

        public event OnMouseEventHandler OnMouseDoubleClickEvent;

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            using (Graphics graphics = CreateGraphics())
            {
                DrawContext context;
                int cellRowIndex, cellColumnIndex;
                Rectangle cellRect;
                Cell cell;
                if (Hit.Cell == HitTest(e.Location, graphics, out cellRowIndex, out cellColumnIndex, out cellRect, out cell, out context))
                {
                    MouseEventArgs eShifted = ShiftLocationMouseEventArgs(e);

                    bool handled = false;
                    OnMouseDoubleClickEvent?.Invoke(this, eShifted, cellRowIndex, cellColumnIndex, out handled);
                    if (handled)
                    {
                        return;
                    }

                    cell.OnMouseDoubleClick(eShifted, ref context);
                    if (context.RequestingRedraw)
                    {
                        DrawOne(cellRowIndex);
                    }
                    if (context.RequestingBeginEdit)
                    {
                        BeginEdit(cellRowIndex, cellColumnIndex, context.RequestingBeginEditSelectAll);
                    }
                }
            }
        }

        public void BeginEdit(bool selectAll)
        {
            BeginEdit(selectedRowIndex, SelectedColumn, selectAll);
        }

        public void BeginEdit(int rowIndex, int columnIndex, bool selectAll)
        {
            if (unchecked(((uint)rowIndex >= (uint)rows.Count) || ((uint)columnIndex >= (uint)columns.Length)))
            {
                throw new ArgumentException();
            }

            if (cellEdit != null)
            {
                if ((rowIndex == cellEditRowIndex) && (columnIndex == cellEditColumnIndex))
                {
                    return;
                }
                EndEdit(true/*commit*/);
            }

            cellEdit = rows[rowIndex].Cells[columnIndex];
            cellEditRect = GetCellRectangle(rowIndex, columnIndex);
            cellEditRowIndex = rowIndex;
            cellEditColumnIndex = columnIndex;
            using (Graphics graphics = CreateGraphics())
            {
                DrawContext context = new DrawContext(
                    this,
                    cellEditRowIndex,
                    cellEditColumnIndex,
                    graphics,
                    Font,
                    GetEffectiveColors(cellEditRowIndex, cellEditColumnIndex),
                    cellEditRect);
                editControl = cellEdit.BeginEdit(ref context, this.EndEdit, selectAll);
                Rectangle bounds = editControl.Bounds;
                bounds.Offset(AutoScrollPosition);
                editControl.Bounds = bounds;
                Controls.Add(editControl);
                editControl.Select();
            }
        }

        public void EndEdit(bool commit)
        {
            if (cellEdit != null)
            {
                using (Graphics graphics = CreateGraphics())
                {
                    DrawContext context = new DrawContext(
                        this,
                        cellEditRowIndex,
                        cellEditColumnIndex,
                        graphics,
                        Font,
                        GetEffectiveColors(cellEditRowIndex, cellEditColumnIndex),
                        cellEditRect);
                    cellEdit.EndEdit(editControl, ref context, commit);
                    Controls.Remove(editControl);
                    cellEdit = null;
                }
            }
        }

        public void CommitWithoutEndEdit()
        {
            if (cellEdit != null)
            {
                using (Graphics graphics = CreateGraphics())
                {
                    DrawContext context = new DrawContext(
                        this,
                        cellEditRowIndex,
                        cellEditColumnIndex,
                        graphics,
                        Font,
                        GetEffectiveColors(cellEditRowIndex, cellEditColumnIndex),
                        cellEditRect);
                    cellEdit.CommitWithoutEndEdit(editControl, ref context);
                }
            }
        }

        public event Func<Keys, bool> IsInputKeyEvent;

        protected override bool IsInputKey(Keys keyData)
        {
            if ((IsInputKeyEvent != null) && IsInputKeyEvent(keyData))
            {
                return true;
            }

            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    return true;
            }

            Cell selectedCell = this.SelectedCell;
            if ((selectedCell != null) && selectedCell.IsInputKey(keyData))
            {
                return true;
            }

            return base.IsInputKey(keyData);
        }

        public event Action<KeyEventArgs> OnKeyDownEvent;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (OnKeyDownEvent != null)
            {
                OnKeyDownEvent(e);
                if (e.Handled)
                {
                    return;
                }
            }

            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (selectedRowIndex > 0)
                    {
                        int old = selectedRowIndex--;
                        DrawRange(selectedRowIndex, old);
                        ShowSelection();
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                    }
                    e.Handled = true;
                    break;
                case Keys.Down:
                    if ((selectedRowIndex >= 0) && (selectedRowIndex < rows.Count - 1))
                    {
                        int old = selectedRowIndex++;
                        DrawRange(old, selectedRowIndex);
                        ShowSelection();
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                    }
                    e.Handled = true;
                    break;
                case Keys.Left:
                    if (selectedColumnIndex > 0)
                    {
                        int old = selectedColumnIndex--;
                        DrawOne(selectedRowIndex);
                        ShowSelection();
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                    }
                    e.Handled = true;
                    break;
                case Keys.Right:
                    if ((selectedRowIndex >= 0) && (selectedColumnIndex < rows[selectedRowIndex].Cells.Count - 1))
                    {
                        int old = selectedColumnIndex++;
                        DrawOne(selectedRowIndex);
                        ShowSelection();
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                    }
                    e.Handled = true;
                    break;
            }

            Cell selectedCell = this.SelectedCell;
            if (selectedCell != null)
            {
                using (Graphics graphics = CreateGraphics())
                {
                    DrawContext context = new DrawContext(
                        this,
                        selectedRowIndex,
                        selectedColumnIndex,
                        graphics,
                        Font,
                        GetEffectiveColors(selectedRowIndex, selectedColumnIndex),
                        GetCellRectangle(selectedRowIndex, selectedColumnIndex));
                    selectedCell.OnKeyDown(e, ref context);
                    if (context.RequestingRedraw)
                    {
                        DrawOne(selectedRowIndex);
                    }
                    if (context.RequestingBeginEdit)
                    {
                        BeginEdit(selectedRowIndex, selectedColumnIndex, context.RequestingBeginEditSelectAll);
                    }
                }
            }
        }

        public void DefineColumns(int[] width)
        {
            if ((columns.Length != 0) && (rows.Count != 0))
            {
                throw new InvalidOperationException();
            }

            columns = new Column[width.Length];
            for (int columnIndex = 0, cumulative = 0; columnIndex < width.Length; cumulative += width[columnIndex], columnIndex++)
            {
                columns[columnIndex] = new Column(width[columnIndex], cumulative);
            }
        }

        private int TotalWidth { get { return columns.Length != 0 ? columns[columns.Length - 1].Offset + columns[columns.Length - 1].Width : 0; } }

        private void ResetAutoscrollSize()
        {
            this.AutoScrollMinSize = new Size(TotalWidth + 1, rows.Count * fontHeight);
        }

        public void InsertHeading(int rowIndex, string text)
        {
            rows.Insert(rowIndex, new HeadingRow(text));
            if (selectedRowIndex >= rowIndex)
            {
                selectedRowIndex++;
            }
            if (deferredUpdates == 0)
            {
                DrawRange(rowIndex, Int32.MaxValue);
                ResetAutoscrollSize();
            }
            else
            {
                firstRow = Math.Min(firstRow, rowIndex);
                lastRow = Int32.MaxValue;
            }
        }

        private void InsertRowInternal(int rowIndex, Cell[] cells, bool updateDisplay)
        {
            if (columns.Length != cells.Length)
            {
                throw new ArgumentException();
            }

            Row row = new Row(Array.ConvertAll(cells, cell => cell != null ? cell : new Cell()));
            using (Graphics graphics = CreateGraphics())
            {
                for (int columnIndex = 0; columnIndex < row.Cells.Count; columnIndex++)
                {
                    DrawContext context = new DrawContext(
                        this,
                        rowIndex,
                        columnIndex,
                        graphics,
                        Font,
                        (Color.Black, Color.Black, Color.Black),
                        GetCellRectangle(rowIndex, columnIndex));
                    row.Cells[columnIndex].DrawInit(ref context);
                }
            }

            rows.Insert(rowIndex, row);
            if (selectedRowIndex >= rowIndex)
            {
                selectedRowIndex++;
            }
            if (updateDisplay)
            {
                if (deferredUpdates == 0)
                {
                    DrawRange(rowIndex, Int32.MaxValue);
                    ResetAutoscrollSize();
                }
                else
                {
                    firstRow = Math.Min(firstRow, rowIndex);
                    lastRow = Int32.MaxValue;
                }
            }
        }

        public void InsertRow(int rowIndex, Cell[] cells)
        {
            InsertRowInternal(rowIndex, cells, true/*updateDisplay*/);
        }

        public void InsertRange(int rowIndex, Cell[][] rowsCells)
        {
            for (int i = 0; i < rowsCells.Length; i++)
            {
                if (columns.Length != rowsCells[i].Length)
                {
                    throw new ArgumentException();
                }

                InsertRowInternal(rowIndex + i, Array.ConvertAll(rowsCells[i], cell => cell != null ? cell : new Cell()), false/*updateDisplay*/);
                if (selectedRowIndex >= rowIndex)

                {
                    selectedRowIndex++;
                }
            }

            if (deferredUpdates == 0)
            {
                Draw();
                ResetAutoscrollSize();
            }
            else
            {
                firstRow = Math.Min(firstRow, rowIndex);
                lastRow = Int32.MaxValue;
            }
        }

        private void DeleteRowInternal(int rowIndex, ref bool selectionChanged)
        {
            if ((rowIndex < 0) || (rowIndex >= rows.Count))
            {
                throw new ArgumentException();
            }

            if (rowIndex == cellEditRowIndex)
            {
                EndEdit(true/*commit*/);
            }

            Row row = rows[rowIndex];
            for (int i = 0; i < row.Cells.Count; i++)
            {
                row.Cells[i].Dispose();
            }
            rows.RemoveAt(rowIndex);

            if (selectedRowIndex == rowIndex)
            {
                selectedRowIndex = -1;
                selectedColumnIndex = -1;
                selectionChanged = true;
            }
            else if (selectedRowIndex > rowIndex)
            {
                selectedRowIndex--;
            }
        }

        public void DeleteRow(int rowIndex)
        {
            bool selectionChanged = false;

            DeleteRowInternal(rowIndex, ref selectionChanged);

            if (deferredUpdates == 0)
            {
                DrawRange(rowIndex, Int32.MaxValue);
                ResetAutoscrollSize();
            }
            else
            {
                firstRow = Math.Min(firstRow, rowIndex);
                lastRow = Int32.MaxValue;
            }
            if (selectionChanged)
            {
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void DeleteRange(int rowStartIndex, int count)
        {
            if ((rowStartIndex < 0) || (rowStartIndex + count > rows.Count))
            {
                throw new ArgumentException();
            }

            bool selectionChanged = false;

            for (int i = count - 1; i >= 0; i--)
            {
                DeleteRowInternal(rowStartIndex + i, ref selectionChanged);
            }

            if (deferredUpdates == 0)
            {
                Draw();
                ResetAutoscrollSize();
            }
            else
            {
                firstRow = Math.Min(firstRow, rowStartIndex);
                lastRow = Int32.MaxValue;
            }
            if (selectionChanged)
            {
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void BatchDeleteInsert(IList<int> deletes, IList<KeyValuePair<int, Cell[]>> inserts)
        {
            bool selectionChanged = false;

            if (deletes != null)
            {
                int[] deletes2 = new int[deletes.Count];
                deletes.CopyTo(deletes2, 0);
                Array.Sort(deletes2);

                for (int i = deletes2.Length - 1; i >= 0; i--)
                {
                    DeleteRowInternal(deletes2[i], ref selectionChanged);
                }
            }

            if (inserts != null)
            {
                foreach (KeyValuePair<int, Cell[]> insert in inserts)
                {
                    InsertRowInternal(insert.Key, insert.Value, false/*updateDisplay*/);
                }
            }

            if (deferredUpdates == 0)
            {
                ResetAutoscrollSize();
                Draw();
            }
            else
            {
                firstRow = 0;
                lastRow = Int32.MaxValue;
            }
            if (selectionChanged)
            {
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool ClearSelection()
        {
            if (selectedRowIndex >= 0)
            {
                EndEdit(true/*commit*/);
                int savedRowIndex = selectedRowIndex;
                selectedRowIndex = -1;
                selectedColumnIndex = -1;
                if (deferredUpdates == 0)
                {
                    DrawOne(savedRowIndex);
                }
                else
                {
                    firstRow = Math.Min(firstRow, savedRowIndex);
                    lastRow = Math.Max(lastRow, savedRowIndex);
                }
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public void SetSelection(int rowIndex, int columnIndex)
        {
            if ((rowIndex == selectedRowIndex) && (columnIndex == selectedColumnIndex))
            {
                return;
            }

            ClearSelection();
            selectedRowIndex = rowIndex;
            selectedColumnIndex = columnIndex;
            if (deferredUpdates == 0)
            {
                DrawOne(rowIndex);
            }
            else
            {
                firstRow = Math.Min(firstRow, rowIndex);
                lastRow = Math.Max(lastRow, rowIndex);
            }
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasSelection { get { return selectedRowIndex >= 0; } }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Cell SelectedCell { get { return selectedRowIndex >= 0 ? rows[selectedRowIndex]?.Cells[selectedColumnIndex] : null; } }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedRow { get { return selectedRowIndex; } }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedColumn { get { return selectedColumnIndex; } }

        public void ShowSelection()
        {
            if (selectedRowIndex >= 0)
            {
                int y = selectedRowIndex * fontHeight;
                if (y < -AutoScrollPosition.Y)
                {
                    AutoScrollPosition = new Point(-AutoScrollPosition.X, y);
                }
                else if (y - (Height / fontHeight - 2) * fontHeight > -AutoScrollPosition.Y)
                {
                    AutoScrollPosition = new Point(-AutoScrollPosition.X, y - (Height / fontHeight - 2) * fontHeight);
                }

                int x = columns[selectedColumnIndex].Offset;
                int xr = columns[selectedColumnIndex].Offset + columns[selectedColumnIndex].Width;
                if (x < -AutoScrollPosition.X)
                {
                    AutoScrollPosition = new Point(x, -AutoScrollPosition.Y);
                }
                else if (xr - Width > -AutoScrollPosition.X)
                {
                    AutoScrollPosition = new Point(xr - Width, -AutoScrollPosition.Y);
                }
            }
        }

        public void Clear()
        {
            ClearSelection();
            rows.Clear();
            ResetAutoscrollSize();
            Invalidate();
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int RowCount { get { return rows.Count; } }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ColumnCount { get { return columns.Length; } }

        public void SetValue(Cell cell, object value)
        {
            // TODO: slow - fix this
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < rows[rowIndex].Cells.Count; columnIndex++)
                {
                    if (rows[rowIndex].Cells[columnIndex] == cell)
                    {
                        SetValue(rowIndex, columnIndex, value);
                        return;
                    }
                }
            }
        }

        public void SetValue(int rowIndex, int columnIndex, object value)
        {
            using (Graphics graphics = CreateGraphics())
            {
                DrawContext context = new DrawContext(
                    this,
                    rowIndex,
                    columnIndex,
                    graphics,
                    Font,
                    GetEffectiveColors(rowIndex, columnIndex),
                    GetCellRectangle(rowIndex, columnIndex));
                if (rows[rowIndex].Cells[columnIndex].SetValue(value))
                {
                    if (deferredUpdates == 0)
                    {
                        DrawOne(rowIndex);
                    }
                    else
                    {
                        firstRow = Math.Min(firstRow, rowIndex);
                        lastRow = Math.Max(lastRow, rowIndex);
                    }
                }
            }
        }

        // Hacky - had to expose to MyGridBinder to suppress cyclical updates
        public Cell GetCell(int rowIndex, int columnIndex)
        {
            return rows[rowIndex].Cells[columnIndex];
        }

        private class AllCellsEnumerable : IEnumerable<Cell>
        {
            private readonly MyGrid grid;

            public AllCellsEnumerable(MyGrid grid)
            {
                this.grid = grid;
            }

            public IEnumerator<Cell> GetEnumerator()
            {
                List<Cell> cells = new List<Cell>(grid.rows.Count * grid.columns.Length);
                for (int rowIndex = 0; rowIndex < grid.rows.Count; rowIndex++)
                {
                    for (int columnIndex = 0; columnIndex < grid.rows[rowIndex].Cells.Count; columnIndex++)
                    {
                        cells.Add(grid.rows[rowIndex].Cells[columnIndex]);
                    }
                }
                return cells.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable AllCells { get { return new AllCellsEnumerable(this); } }
    }


    public class PropertyChangeCaptureEventArgs : EventArgs
    {
        private readonly bool capture;

        public PropertyChangeCaptureEventArgs(bool capture)
        {
            this.capture = capture;
        }

        public bool Capture { get { return capture; } }
    }

    public delegate void PropertyChangeCaptureHandler(object sender, PropertyChangeCaptureEventArgs e);

    public interface INotifyPropertyChangeCapture
    {
        event PropertyChangeCaptureHandler PropertyChangeCapture;
    }


    public class MyGridBinder<T> : IDisposable
    {
        private readonly MyGrid grid;
        private BindingList<T> list;
        private BindingList<T> emptyList; // create as needed
        private readonly CellBindingBase[] columns;
        private readonly Dictionary<object, bool> sendingViewObjects = new Dictionary<object, bool>();

        public struct OneBinding
        {
            public readonly Func<T, bool> CanBind;
            public readonly Func<T, MyGrid.Cell> CreateCell;
            public readonly Func<T, object> ReadUnderlying;
            public readonly Action<T, object> WriteUnderlying;

            /// <summary>
            /// Create a binding methods record.
            /// </summary>
            /// <param name="canBind">Method to determine if the underlying list item instance can be bound to by this set of methods</param>
            /// <param name="createCell">Method that creates the appropriate cell for displaying or editing the underlying value</param>
            /// <param name="readUnderlying">Method that reads the current value from the underlying list item</param>
            /// <param name="writeUnderlying">Method that writes a value to the underlying list item</param>
            public OneBinding(Func<T, bool> canBind, Func<T, MyGrid.Cell> createCell, Func<T, object> readUnderlying, Action<T, object> writeUnderlying)
            {
                this.CanBind = canBind;
                this.CreateCell = createCell;
                this.ReadUnderlying = readUnderlying;
                this.WriteUnderlying = writeUnderlying;
            }

            /// <summary>
            /// Create a binding methods record.
            /// </summary>
            /// <param name="createCell">Method that creates the appropriate cell for displaying or editing the underlying value</param>
            /// <param name="readUnderlying">Method that reads the current value from the underlying list item</param>
            /// <param name="writeUnderlying">Method that writes a value to the underlying list item</param>
            public OneBinding(Func<T, MyGrid.Cell> createCell, Func<T, object> readUnderlying, Action<T, object> writeUnderlying)
                : this(null/*canBind*/, createCell, readUnderlying, writeUnderlying)
            {
            }
        }

        public abstract class CellBindingBase
        {
            protected readonly string propertyName;

            protected CellBindingBase(string propertyName)
            {
                this.propertyName = propertyName;
            }

            /// <summary>
            /// Name of property, corresponding to that from INotifyPropertyChanged.PropertyChanged
            /// </summary>
            public string PropertyName { get { return propertyName; } }

            public abstract MyGrid.Cell CreateCell(T item);
            public abstract object ReadUnderlying(T item);
            public abstract void WriteUnderlying(T item, object value);
        }

        public class CellBinding : CellBindingBase
        {
            private readonly OneBinding binding;

            /// <summary>
            /// Create a simple cell binding
            /// </summary>
            /// <param name="propertyName">Name of property, corresponding to that from INotifyPropertyChanged.PropertyChanged</param>
            /// <param name="createCell">Method that creates the appropriate cell for displaying or editing the underlying value</param>
            /// <param name="readUnderlying">Method that reads the current value from the underlying list item</param>
            /// <param name="writeUnderlying">Method that writes a value to the underlying list item</param>
            public CellBinding(string propertyName, Func<T, MyGrid.Cell> createCell, Func<T, object> readUnderlying, Action<T, object> writeUnderlying)
                : this(propertyName, new OneBinding(createCell, readUnderlying, writeUnderlying))
            {
            }

            /// <summary>
            /// Create a simple cell binding
            /// </summary>
            /// <param name="propertyName">Name of property, corresponding to that from INotifyPropertyChanged.PropertyChanged</param>
            /// <param name="binding">Binding methods record</param>
            public CellBinding(string propertyName, OneBinding binding)
                : base(propertyName)
            {
                Debug.Assert(binding.CanBind == null);
                this.binding = binding;
            }


            public override MyGrid.Cell CreateCell(T item)
            {
                return binding.CreateCell(item);
            }

            public override object ReadUnderlying(T item)
            {
                return binding.ReadUnderlying(item);
            }

            public override void WriteUnderlying(T item, object value)
            {
                binding.WriteUnderlying(item, value);
            }
        }

        public class VariantCellBinding : CellBindingBase
        {
            private readonly OneBinding[] bindings;
            private bool throwOnNoMatch;

            /// <summary>
            /// Create a variant binding record, which can bind using different UI elements corresponding to the provided
            /// OneBinding records, specifically for the first item in bindings for which CanBind(T) returns true.
            /// </summary>
            /// <param name="propertyName">Name of property, corresponding to that from INotifyPropertyChanged.PropertyChanged</param>
            /// <param name="bindings">List of binding method units. The first for which CanBind(T) == true will be used.</param>
            /// <param name="throwOnNoMatch">True: if none of the bindings can handle the list item, throw an exception. False: if none of
            /// the bindings can handle the list item, display an empty read-only cell.</param>
            public VariantCellBinding(string propertyName, OneBinding[] bindings, bool throwOnNoMatch)
                : base(propertyName)
            {
                if (bindings == null)
                {
                    throw new ArgumentException();
                }
                this.bindings = bindings;
                this.throwOnNoMatch = throwOnNoMatch;
            }

            /// <summary>
            /// Create a variant binding record, which can bind using different UI elements corresponding to the provided
            /// OneBinding records, specifically for the first item in bindings[] for which CanBind(T) returns true.
            /// If no bindings match, and exception will be thrown.
            /// </summary>
            /// <param name="propertyName">Name of property, corresponding to that from INotifyPropertyChanged.PropertyChanged</param>
            /// <param name="bindings">List of binding method units. The first for which CanBind(T) == true will be used.</param>
            public VariantCellBinding(string propertyName, OneBinding[] bindings)
                : this(propertyName, bindings, true/*throwOnNoMatch*/)
            {
            }

            public override MyGrid.Cell CreateCell(T item)
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (bindings[i].CanBind(item))
                    {
                        return bindings[i].CreateCell(item);
                    }
                }
                if (throwOnNoMatch)
                {
                    throw new NotSupportedException();
                }
                return new MyGrid.Cell();
            }

            public override object ReadUnderlying(T item)
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (bindings[i].CanBind(item))
                    {
                        return bindings[i].ReadUnderlying(item);
                    }
                }
                if (throwOnNoMatch)
                {
                    throw new NotSupportedException();
                }
                return null;
            }

            public override void WriteUnderlying(T item, object value)
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (bindings[i].CanBind(item))
                    {
                        bindings[i].WriteUnderlying(item, value);
                        return;
                    }
                }
                if (throwOnNoMatch)
                {
                    throw new NotSupportedException();
                }
            }
        }

        public MyGridBinder(MyGrid grid, BindingList<T> list, CellBindingBase[] columns)
        {
            this.grid = grid;
            this.columns = columns;

            this.grid.PropertyChangeCapture += ViewSenderCaptureChanged;
            this.list = ListOrEmpty(list);
            this.list.ListChanged += List_ListChanged;
            Reset();
        }

        private BindingList<T> ListOrEmpty(BindingList<T> list)
        {
            if (list == null)
            {
                if (this.emptyList == null)
                {
                    this.emptyList = new BindingList<T>();
                    this.emptyList.AllowEdit = false;
                }
                list = emptyList;
            }
            return list;
        }

        private MyGrid.Cell[] MakeRow(int insertedAt)
        {
            T listItem = list[insertedAt];
            MyGrid.Cell[] row = new MyGrid.Cell[columns.Length];
            for (int i = 0; i < row.Length; i++)
            {
                CellBindingBase cellBinding = columns[i];
                MyGrid.Cell cell = row[i] = cellBinding.CreateCell(listItem);
                cell.SetValue(cellBinding.ReadUnderlying(listItem));
                cell.PropertyChanged += (s, e) => CellPropertyChanged(listItem, cell, cellBinding, s, e);
            }
            return row;
        }

        private void ViewSenderCaptureChanged(object sender, PropertyChangeCaptureEventArgs e)
        {
            if (e.Capture)
            {
                // This can happen when debugging: break occurs while mouse is captured - in using the debugger, mouse is
                // released, but code still thinks it is depressed when it resumes because it never got the up event. Clicking
                // again on that control will capture again and throw on sender already in collection.
                if (sendingViewObjects.ContainsKey(sender) && Debugger.IsAttached)
                {
                    sendingViewObjects.Remove(sender);
                }

                Debug.Assert(!sendingViewObjects.ContainsKey(sender));
                sendingViewObjects.Add(sender, true); // captured
            }
            else
            {
                Debug.Assert(sendingViewObjects.ContainsKey(sender) && sendingViewObjects[sender]);
                sendingViewObjects.Remove(sender);
            }
        }

        private void CellPropertyChanged(T listItem, MyGrid.Cell cell, CellBindingBase cellBinding, object sender, PropertyChangedEventArgs e)
        {
            Debug.Assert(!sendingViewObjects.ContainsKey(cell) || sendingViewObjects[cell]); // only inactive or captured makes sense
            bool capture;
            sendingViewObjects.TryGetValue(cell, out capture);
            if (!capture)
            {
                sendingViewObjects.Add(cell, false); // active, not captured
            }

            try
            {
                cellBinding.WriteUnderlying(listItem, cell.GetValue());
            }
            finally
            {
                if (!capture)
                {
                    Debug.Assert(!sendingViewObjects[cell]); // shouldn't have been changedd
                    sendingViewObjects.Remove(cell);
                }
            }
        }

        public void ForcePropertyChanged(int index, string propertyName)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                if (String.Equals(propertyName, columns[i].PropertyName))
                {
                    object identity = grid.GetCell(index, i);
                    if (!sendingViewObjects.ContainsKey(identity))
                    {
                        grid.SetValue(index, i, columns[i].ReadUnderlying(list[index]));
                    }
                }
            }
        }

        private void List_ListChanged(object sender, ListChangedEventArgs e)
        {
            // if UI was dropped without telling us, unhook listener now
            if ((this.list == null) || grid.IsDisposed)
            {
                this.Dispose();
                return;
            }

            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    grid.InsertRow(e.NewIndex, MakeRow(e.NewIndex));
                    break;

                case ListChangedType.ItemChanged:
                    ForcePropertyChanged(e.NewIndex, e.PropertyDescriptor.Name);
                    break;

                case ListChangedType.ItemDeleted:
                    grid.DeleteRow(e.NewIndex);
                    break;

                case ListChangedType.Reset:
                    Reset();
                    break;

                default:
                case ListChangedType.ItemMoved:
                case ListChangedType.PropertyDescriptorAdded:
                case ListChangedType.PropertyDescriptorChanged:
                case ListChangedType.PropertyDescriptorDeleted:
                    throw new NotImplementedException();
            }
        }

        private void Reset()
        {
            grid.Clear();
            List<KeyValuePair<int, MyGrid.Cell[]>> insertions = new List<KeyValuePair<int, MyGrid.Cell[]>>();
            for (int i = 0; i < list.Count; i++)
            {
                insertions.Add(new KeyValuePair<int, MyGrid.Cell[]>(i, MakeRow(i)));
            }
            grid.BatchDeleteInsert(null, insertions);
        }

        public BindingList<T> List { get { return list != emptyList ? list : null; } }

        /// <summary>
        /// Change the data source underlying the grid. If the current data source is passed in as
        /// an argument, nothing happens. A null list rebinds to an empty data source.
        /// </summary>
        /// <param name="list">The data source to rebind to. May be null.</param>
        public void SetDataSource(BindingList<T> list)
        {
            grid.EndEdit(true/*commit*/);

            list = ListOrEmpty(list);
            if (this.list != list)
            {
                this.list.ListChanged -= List_ListChanged;
                this.list = list;
                this.list.ListChanged += List_ListChanged;

                Reset();
            }
        }

        public void Dispose()
        {
            if (this.list != null)
            {
                this.list.ListChanged -= List_ListChanged;
                this.list = null;
            }
        }
    }

    // TODO: keep active selected row fixed on screen if insert/delete occurs above it
}
