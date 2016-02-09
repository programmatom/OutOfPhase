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
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace OutOfPhase
{
    public partial class MyListBox : UserControl
    {
        private bool multiselect;
        private IBindingList underlying;
        private readonly Dictionary<object, bool> selected = new Dictionary<object, bool>();
        private GetDisplayNameMethod getDisplayNameMethod;
        private int cursor = -1;
        private bool showBlankItemsDifferently;

        private Brush backBrush;
        private Brush foreBrush;
        private Brush selectBackBrush;
        private Brush selectForeBrush;
        private Brush selectBackInactiveBrush;
        private Brush selectForeInactiveBrush;
        private Color selectedBackColor = SystemColors.Highlight;
        private Color selectedForeColor = SystemColors.HighlightText;
        private Color selectedBackInactiveColor = SystemColors.GradientInactiveCaption;
        private Color selectedForeInactiveColor = SystemColors.ControlText;
        private Brush cursorBrush;
        private Pen cursorPen;
        private Image offscreenStrip;

        public MyListBox()
        {
            InitializeComponent();

            Disposed += new EventHandler(MyListBox_Disposed);
        }

        void MyListBox_Disposed(object sender, EventArgs e)
        {
            ClearDrawingResources();

            if (underlying != null)
            {
                underlying.ListChanged -= new ListChangedEventHandler(underlying_ListChanged);
            }
            underlying = null;
            getDisplayNameMethod = null;

            selected.Clear();
        }

        public delegate string GetDisplayNameMethod(object obj);

        public void SetUnderlying(IBindingList list, GetDisplayNameMethod getDisplayName)
        {
            if (underlying != null)
            {
                underlying.ListChanged -= new ListChangedEventHandler(underlying_ListChanged);
            }
            underlying = list;
            if (underlying != null)
            {
                underlying.ListChanged += new ListChangedEventHandler(underlying_ListChanged);
            }

            getDisplayNameMethod = getDisplayName;

            Rebuild();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            VerticalScroll.SmallChange = FontHeight;
        }

        [Category("Behavior"), DefaultValue(false)]
        public bool Multiselect
        {
            get
            {
                return multiselect;
            }
            set
            {
                multiselect = value;
                if (!multiselect && (selected.Count > 1))
                {
                    SelectNone();
                }
            }
        }

        [Category("Appearance"), DefaultValue(typeof(Color), "Highlight")]
        public Color SelectedBackColor { get { return selectedBackColor; } set { selectedBackColor = value; } }
        [Category("Appearance"), DefaultValue(typeof(Color), "HighlightText")]
        public Color SelectedForeColor { get { return selectedForeColor; } set { selectedForeColor = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "GradientInactiveCaption")]
        public Color SelectedBackColorInactive { get { return selectedBackInactiveColor; } set { selectedBackInactiveColor = value; } }
        [Category("Appearance"), DefaultValue(typeof(Color), "ControlText")]
        public Color SelectedForeColorInactive { get { return selectedForeInactiveColor; } set { selectedForeInactiveColor = value; } }

        [Category("Appearance"), DefaultValue(false)]
        public bool ShowBlankItemsDifferently { get { return showBlankItemsDifferently; } set { showBlankItemsDifferently = value; } }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ClearDrawingResources(); // recreate offscreen strip
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            ClearDrawingResources(); // recreate offscreen strip
            Invalidate();
            VerticalScroll.SmallChange = FontHeight;
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
            if (underlying != null)
            {
                Redraw(pe.Graphics);
            }
            else
            {
                // draw X to indicate control is operating but has no content (vs. failing to start)
                EnsureDrawingResources();
                pe.Graphics.FillRectangle(backBrush, ClientRectangle);
                const int T = 3;
                pe.Graphics.FillPolygon(foreBrush, new Point[] { new Point(0 - T, 0 + T), new Point(Width - T, Height + T), new Point(Width + T, Height - T), new Point(0 + T, 0 - T) });
                pe.Graphics.FillPolygon(foreBrush, new Point[] { new Point(Width - T, 0 - T), new Point(0 - T, Height - T), new Point(0 + T, Height + T), new Point(Width + T, 0 + T) });
            }

            // Calling the base class OnPaint
            base.OnPaint(pe);
        }

        private void ClearDrawingResources()
        {
            if (backBrush != null)
            {
                backBrush.Dispose();
                backBrush = null;
            }
            if (foreBrush != null)
            {
                foreBrush.Dispose();
                foreBrush = null;
            }
            if (selectBackBrush != null)
            {
                selectBackBrush.Dispose();
                selectBackBrush = null;
            }
            if (selectBackInactiveBrush != null)
            {
                selectBackInactiveBrush.Dispose();
                selectBackInactiveBrush = null;
            }
            if (foreBrush != null)
            {
                foreBrush.Dispose();
                foreBrush = null;
            }
            if (selectForeBrush != null)
            {
                selectForeBrush.Dispose();
                selectForeBrush = null;
            }
            if (selectForeInactiveBrush != null)
            {
                selectForeInactiveBrush.Dispose();
                selectForeInactiveBrush = null;
            }
            if (cursorBrush != null)
            {
                cursorBrush.Dispose();
                cursorBrush = null;
            }
            if (cursorPen != null)
            {
                cursorPen.Dispose();
                cursorPen = null;
            }
            if (offscreenStrip != null)
            {
                offscreenStrip.Dispose();
                offscreenStrip = null;
            }
        }

        private void EnsureDrawingResources()
        {
            if (backBrush == null)
            {
                backBrush = new SolidBrush(this.BackColor);
            }
            if (foreBrush == null)
            {
                foreBrush = new SolidBrush(this.ForeColor);
            }
            if (selectBackBrush == null)
            {
                selectBackBrush = new SolidBrush(selectedBackColor);
            }
            if (selectForeBrush == null)
            {
                selectForeBrush = new SolidBrush(selectedForeColor);
            }
            if (selectBackInactiveBrush == null)
            {
                selectBackInactiveBrush = new SolidBrush(selectedBackInactiveColor);
            }
            if (selectForeInactiveBrush == null)
            {
                selectForeInactiveBrush = new SolidBrush(selectedForeInactiveColor);
            }
            if (cursorBrush == null)
            {
                cursorBrush = new HatchBrush(HatchStyle.Percent50, selectedForeColor, selectedBackColor);
            }
            if (cursorPen == null)
            {
                cursorPen = new Pen(cursorBrush);
            }
            if (offscreenStrip == null)
            {
                offscreenStrip = new Bitmap(Width, FontHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }
        }

        private void DrawOnePrimitive(Graphics graphics, int i)
        {
            object item = i < underlying.Count ? underlying[i] : null;
            Rectangle rect = new Rectangle(0, i * FontHeight, Width, FontHeight);
            if (graphics.IsVisible(rect))
            {
                Brush foreground, background;
                Color foregroundColor, backgroundColor;
                if ((i >= underlying.Count) || !selected.ContainsKey(item))
                {
                    foreground = foreBrush;
                    background = backBrush;
                    foregroundColor = ForeColor;
                    backgroundColor = BackColor;
                }
                else
                {
                    foreground = Focused ? selectForeBrush : selectForeInactiveBrush;
                    background = Focused ? selectBackBrush : selectBackInactiveBrush;
                    foregroundColor = Focused ? selectedForeColor : selectedForeInactiveColor;
                    backgroundColor = Focused ? selectedBackColor : selectedBackInactiveColor;
                }
                using (Graphics graphics2 = Graphics.FromImage(offscreenStrip))
                {
                    Rectangle rect2 = rect;
                    rect2.Offset(-rect.X, -rect.Y);

                    graphics2.FillRectangle(background, rect2);
                    if (i < underlying.Count)
                    {
                        string text = getDisplayNameMethod(item);
                        if (showBlankItemsDifferently && String.IsNullOrEmpty(text))
                        {
                            text = "(no name)";
                        }
                        MyTextRenderer.DrawText(
                            graphics2,
                            text,
                            Font,
                            rect2.Location,
                            foregroundColor,
                            backgroundColor);
                    }

                    if ((cursor == i) && Focused)
                    {
                        graphics2.DrawRectangle(cursorPen, rect2.X, rect2.Y, rect2.Width - 1, rect2.Height - 1);
                    }
                }

                graphics.DrawImage(offscreenStrip, rect);
            }
        }

        private void DrawOne(int i)
        {
            using (Graphics graphics = CreateGraphics())
            {
                EnsureDrawingResources();

                GraphicsState state = graphics.Save();
                graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

                DrawOnePrimitive(graphics, i);

                graphics.Restore(state);
            }
        }

        private void Redraw(Graphics graphics)
        {
            graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

            int firstVisible = -AutoScrollPosition.Y / FontHeight;
            int lastVisible = (-AutoScrollPosition.Y + Height + FontHeight - 1) / FontHeight;
            for (int i = firstVisible; i < lastVisible; i++)
            {
                DrawOnePrimitive(graphics, i);
            }
        }

        private void Redraw()
        {
            using (Graphics graphics = CreateGraphics())
            {
                Redraw(graphics);
            }
        }

        public void ClearCursor()
        {
            if (cursor >= 0)
            {
                int index = cursor;
                cursor = -1;
                DrawOne(index);
            }
        }

        private bool SelectNoneInternal()
        {
            bool notify = false;
            foreach (object obj in new List<object>(selected.Keys))
            {
                notify = true;
                selected.Remove(obj);
                int index = underlying.IndexOf(obj);
                if (index >= 0)
                {
                    DrawOne(index);
                }
            }
            return notify;
        }

        public void SelectNone()
        {
            if (SelectNoneInternal())
            {
                if (SelectionChanged != null)
                {
                    SelectionChanged(this, new EventArgs());
                }
            }
        }

        public void SelectAll()
        {
            bool notify = false;
            for (int i = 0; i < underlying.Count; i++)
            {
                object obj = underlying[i];
                if (!selected.ContainsKey(obj))
                {
                    notify = true;
                    selected.Add(obj, false);
                    DrawOne(i);
                }
            }
            if (notify && (SelectionChanged != null))
            {
                SelectionChanged(this, new EventArgs());
            }
        }

        public void Rebuild()
        {
            AutoScrollMinSize = new Size(0, (underlying != null ? underlying.Count : 0) * FontHeight);

            foreach (object obj in new List<object>(selected.Keys))
            {
                selected[obj] = true;
            }
            foreach (object obj in underlying)
            {
                if (selected.ContainsKey(obj))
                {
                    selected[obj] = false;
                }
            }
            List<object> remove = new List<object>();
            foreach (KeyValuePair<object, bool> k in selected)
            {
                if (k.Value)
                {
                    remove.Add(k.Key);
                }
            }
            foreach (object obj in remove)
            {
                selected.Remove(obj);
            }
            if ((remove.Count > 0) && (SelectionChanged != null))
            {
                SelectionChanged(this, new EventArgs());
            }

            if (cursor > underlying.Count - 1)
            {
                cursor = underlying.Count - 1; // -1 ok
            }

            Invalidate();
        }

        private void underlying_ListChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                case ListChangedType.ItemDeleted:
                    Rebuild();
                    break;
                case ListChangedType.ItemChanged:
                    DrawOne(e.OldIndex);
                    break;
                case ListChangedType.ItemMoved:
                    Invalidate();
                    break;
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            int y = e.Y - AutoScrollPosition.Y;
            int index = y / FontHeight;
            if ((index < 0) || (index >= underlying.Count))
            {
                if (!multiselect)
                {
                    SelectNone();
                }
            }
            else
            {
                object obj = underlying[index];
                if (multiselect && selected.ContainsKey(obj))
                {
                    selected.Remove(obj);
                    ClearCursor();
                    cursor = index;
                    DrawOne(index);
                    if (SelectionChanged != null)
                    {
                        SelectionChanged(this, new EventArgs());
                    }
                }
                else
                {
                    if (!multiselect)
                    {
                        SelectNone();
                    }
                    selected.Add(obj, false);
                    ClearCursor();
                    cursor = index;
                    DrawOne(index);
                    if (SelectionChanged != null)
                    {
                        SelectionChanged(this, new EventArgs());
                    }
                    if (!multiselect && (e.Clicks > 1))
                    {
                        if (DoubleClick2 != null)
                        {
                            DoubleClick2(this, new DoubleClick2EventArgs(index, obj));
                        }
                    }
                }
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                case Keys.Space:
                case Keys.Enter:
                    return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            bool checkChange = false;

            switch (e.KeyCode)
            {
                case Keys.Up:
                    MoveCursorTo(cursor - 1);
                    checkChange = true;
                    e.Handled = true;
                    break;
                case Keys.Down:
                    MoveCursorTo(cursor + 1);
                    checkChange = true;
                    e.Handled = true;
                    break;
                case Keys.PageUp:
                    MoveCursorTo(cursor - (Height / FontHeight - 2));
                    checkChange = true;
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                    MoveCursorTo(cursor + (Height / FontHeight - 2));
                    checkChange = true;
                    e.Handled = true;
                    break;
                case Keys.Home:
                    MoveCursorTo(0);
                    checkChange = true;
                    e.Handled = true;
                    break;
                case Keys.End:
                    MoveCursorTo(Int32.MaxValue);
                    checkChange = true;
                    e.Handled = true;
                    break;
                case Keys.Space:
                    if ((underlying != null) && (underlying.Count > 0))
                    {
                        if (cursor >= 0)
                        {
                            object obj = underlying[cursor];
                            if (selected.ContainsKey(obj))
                            {
                                selected.Remove(obj);
                                DrawOne(cursor);
                                if (SelectionChanged != null)
                                {
                                    SelectionChanged(this, new EventArgs());
                                }
                            }
                            else
                            {
                                if (!multiselect)
                                {
                                    SelectNone();
                                }
                                selected.Add(obj, false);
                                DrawOne(cursor);
                                if (SelectionChanged != null)
                                {
                                    SelectionChanged(this, new EventArgs());
                                }
                            }
                        }
                        DrawOne(0);
                    }
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    if (!multiselect)
                    {
                        if ((cursor >= 0) && (cursor != SelectedIndex))
                        {
                            SelectItem(cursor, true/*clearOtherSelections*/);
                        }
                        int index = SelectedIndex;
                        if (index >= 0)
                        {
                            if (DoubleClick2 != null)
                            {
                                DoubleClick2(this, new DoubleClick2EventArgs(index, underlying[index]));
                            }
                        }
                        e.Handled = true;
                    }
                    break;
            }

            if (checkChange && !multiselect)
            {
                cursor = Math.Min(cursor, this.Count - 1);
                if (cursor >= 0)
                {
                    SelectItem(cursor, true/*clearOtherSelections*/);
                }
            }
        }

        private void MoveCursorTo(int newCursor)
        {
            if ((underlying != null) && (underlying.Count > 0))
            {
                ClearCursor();
                cursor = Math.Min(Math.Max(newCursor, 0), underlying.Count - 1);
                DrawOne(cursor);
                ScrollToCursor();
            }
        }

        public void ScrollToCursor()
        {
            if (cursor >= 0)
            {
                int y = cursor * FontHeight;
                if (y < -AutoScrollPosition.Y)
                {
                    AutoScrollPosition = new Point(0, y);
                }
                else if (y - (Height / FontHeight - 2) * FontHeight > -AutoScrollPosition.Y)
                {
                    AutoScrollPosition = new Point(0, y - (Height / FontHeight - 2) * FontHeight);
                }
            }
        }

        public int Count
        {
            get
            {
                return underlying.Count;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedItem
        {
            get
            {
                if (multiselect)
                {
                    throw new InvalidOperationException();
                }

                foreach (object obj in selected.Keys)
                {
                    return obj;
                }
                return null;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get
            {
                object selectedItem = SelectedItem;
                if (selectedItem == null)
                {
                    return -1;
                }
                return underlying.IndexOf(selectedItem);
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object[] SelectedItems
        {
            get
            {
                return new List<object>(selected.Keys).ToArray();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int[] SelectedIndices
        {
            get
            {
                object[] selectedItems = SelectedItems;
                int[] indices = new int[selectedItems.Length];
                for (int i = 0; i < selectedItems.Length; i++)
                {
                    indices[i] = underlying.IndexOf(selectedItems[i]);
                    if (indices[i] < 0)
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
                return indices;
            }
        }

        public void UnselectItem(int index)
        {
            object obj = underlying[index];
            if (selected.ContainsKey(obj))
            {
                selected.Remove(obj);
                DrawOne(index);
                if (SelectionChanged != null)
                {
                    SelectionChanged(this, new EventArgs());
                }
            }
        }

        public void SelectItem(int index, bool clearOtherSelections)
        {
            object obj = underlying[index];
            if (!selected.ContainsKey(obj))
            {
                if (!multiselect || clearOtherSelections)
                {
                    SelectNoneInternal();
                }
                selected.Add(obj, false);
                ClearCursor();
                cursor = index;
                DrawOne(index);
                if (SelectionChanged != null)
                {
                    SelectionChanged(this, new EventArgs());
                }
            }
        }

        public void SelectItem(object obj, bool clearOtherSelections)
        {
            int index = underlying.IndexOf(obj);
            if (index < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            SelectItem(index, clearOtherSelections);
        }

        public event EventHandler SelectionChanged;

        public class DoubleClick2EventArgs : EventArgs
        {
            public readonly int Index;
            public readonly object Item;

            public DoubleClick2EventArgs(int index, object item)
            {
                this.Index = index;
                this.Item = item;
            }
        }

        public delegate void DoubleClick2EventHandler(object sender, DoubleClick2EventArgs e);

        public event DoubleClick2EventHandler DoubleClick2;
    }
}
