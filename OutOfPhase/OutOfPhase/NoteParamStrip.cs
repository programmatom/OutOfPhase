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
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class NoteParamStrip : UserControl, IValueInfoOwner
    {
        private InlineParamVis inlineParamVis = InlineParamVis.None;
        private readonly List<Info> info = new List<Info>();
        private readonly List<InlineParamVis> lines = new List<InlineParamVis>();
        private Brush backBrush;
        private bool allInvalid;
        private int lastToolTipLine;
        private Color highlightBackColor = SystemColors.Window;
        private Control focusReturnsTo;
        private UndoHelper undoHelper;
        private int currentIndex = -1, currentLine = -1;
        private int maxInternalFieldWidth;
        private TrackViewControl trackView;

        public NoteParamStrip()
        {
            InitializeComponent();

            textEditControl.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;

            textEditControl.TextChanged += TextEditControl_TextChanged;
            textEditControl.LostFocus += TextEditControl_LostFocus;
            textEditControl.BackColor = highlightBackColor;
            textEditControl.ForeColor = this.ForeColor;

            this.Disposed += NoteParamStrip_Disposed;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
        }

        private void NoteParamStrip_Disposed(object sender, EventArgs e)
        {
            if (backBrush != null)
            {
                backBrush.Dispose();
            }
        }

        private class Info
        {
            public int x;
            public int width;
            public readonly NoteNoteObjectRec note;

            public Info(int x, int width, NoteNoteObjectRec note)
            {
                this.x = x;
                this.width = width;
                this.note = note;
            }
        }


        //

        [Category("Appearance"), DefaultValue(typeof(SystemColors), "Window")]
        public Color HighlightBackColor { get { return highlightBackColor; } set { highlightBackColor = value; } }

        [Category("Behavior")]
        public Control FocusReturnsTo { get { return focusReturnsTo; } set { focusReturnsTo = value; } }

        [Category("Misc")]
        public TrackViewControl TrackView { get { return trackView; } set { trackView = value; } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UndoHelper UndoHelper { get { return undoHelper; } set { undoHelper = value; } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int MaxFieldWidth
        {
            get
            {
                return MaxInternalFieldWidth + FontHeight / 2;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int MaxInternalFieldWidth
        {
            get
            {
                if (maxInternalFieldWidth == 0) // compute once
                {
                    using (Graphics graphics = CreateGraphics())
                    {
                        maxInternalFieldWidth = MyTextRenderer.MeasureText(graphics, "-00.000", Font, new Size(Int16.MaxValue, FontHeight), Flags).Width;
                    }
                }
                return maxInternalFieldWidth;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public InlineParamVis InlineParamVis
        {
            get
            {
                return inlineParamVis;
            }
            set
            {
                inlineParamVis = value;
                CommitFieldEdit();
                lines.Clear();
                for (int i = 0; i <= (int)InlineParamVis.MaximumExponent; i++)
                {
                    InlineParamVis one = (InlineParamVis)(1U << i);
                    if ((inlineParamVis & one) != 0)
                    {
                        lines.Add(one);
                    }
                }
                Size = MinimumSize = new Size(MinimumSize.Width, FontHeight * lines.Count);
            }
        }

        public event EventHandler ValueChanged;

        public void Clear()
        {
            info.Clear();
            Invalidate2();
        }

        // returns width
        // requiring Graphics passed in is a bit hacky
        public int Add(int x, NoteNoteObjectRec note, Graphics graphics)
        {
            Info one;
            info.Add(one = new Info(x, MeasureWidth(graphics, note), note));
            Invalidate2();
            return one.width;
        }

        public void Shift(int shift)
        {
            for (int i = 0; i < info.Count; i++)
            {
                info[i].x += shift;
            }
            bool textEditVisible;
            if (textEditVisible = textEditControl.Visible)
            {
                // temporarily hide floating edit box during bitblt to prevent artifacts
                textEditControl.Visible = false;
                textEditControl.Location = new Point(textEditControl.Location.X + shift, textEditControl.Location.Y);
            }
            //Invalidate2(); -- redraw immediately for more scrolling responsiveness
            if (!allInvalid)
            {
                using (Graphics graphics = CreateGraphics())
                {
                    if (shift < 0)
                    {
                        // blit left
                        graphics.CopyFromScreen(
                            PointToScreen(new Point(-shift, 0)),
                            new Point(0, 0),
                            ClientSize);
                        graphics.SetClip(
                            new Rectangle(
                                new Point(ClientSize.Width - -shift, 0),
                                new Size(-shift, ClientSize.Height)));
                        Redraw(graphics);
                    }
                    else
                    {
                        // blit right
                        graphics.CopyFromScreen(
                            PointToScreen(new Point(0, 0)),
                            new Point(shift, 0),
                            ClientSize);
                        graphics.SetClip(
                            new Rectangle(
                                new Point(0, 0),
                                new Size(shift, ClientSize.Height)));
                        Redraw(graphics);
                    }
                }
            }
            if (textEditVisible)
            {
                textEditControl.Visible = true;
            }
        }

        private int MeasureWidth(Graphics graphics, NoteNoteObjectRec note)
        {
            int width = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                int width1 = MyTextRenderer.MeasureText(
                    graphics,
                    GetValue(note, lines[i]),
                    Font,
                    new Size(Int16.MaxValue, FontHeight),
                    Flags).Width;
                width = Math.Max(width, width1);
            }
            return width;
        }


        //

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // full owner draw - no action needed
        }

        public const TextFormatFlags Flags = TextFormatFlags.Left | TextFormatFlags.NoClipping | TextFormatFlags.NoPadding
            | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.SingleLine;

        private void Invalidate2()
        {
            if (!allInvalid)
            {
                allInvalid = true;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Redraw(e.Graphics);
            base.OnPaint(e);
        }

        protected void Redraw(Graphics graphics)
        {
            allInvalid = false;

            if (backBrush == null)
            {
                backBrush = new SolidBrush(BackColor);
            }

            graphics.FillRectangle(backBrush, ClientRectangle);

            for (int i = 0; i < info.Count; i++)
            {
                if (graphics.IsVisible(new Rectangle(info[i].x, 0, info[i].width, FontHeight * lines.Count)))
                {
                    for (int l = 0; l < lines.Count; l++)
                    {
                        bool highlight = (i == highlightedIndex) && (l == highlightedLine);
                        string s = GetValue(info[i].note, lines[l]);
                        MyTextRenderer.DrawText(
                            graphics,
                            s,
                            Font,
                            new Rectangle(
                                info[i].x,
                                l * FontHeight,
                                info[i].width,
                                FontHeight),
                            !highlight ? ForeColor : BackColor,
                            !highlight ? BackColor : ForeColor,
                            Flags);
                    }
                }
            }
        }

        private string GetValue(NoteNoteObjectRec note, InlineParamVis param)
        {
            ValueInfo valueInfo = ValueInfo.FindInlineParamVis(param);
            return valueInfo.GetValue(note);
        }

        private void SetValue(NoteNoteObjectRec note, InlineParamVis param, string value)
        {
            ValueInfo valueInfo = ValueInfo.FindInlineParamVis(param);
            valueInfo.SetValue(note, value);
            if (ValueChanged != null)
            {
                ValueChanged.Invoke(this, EventArgs.Empty);
            }
        }


        //

        private Rectangle GetNoteBounds(int index)
        {
            return new Rectangle(
                info[index].x,
                0,
                info[index].width,
                lines.Count * FontHeight);
        }

        private Rectangle GetFieldBounds(int i, int l)
        {
            return new Rectangle(info[i].x, l * FontHeight, info[i].width, FontHeight);
        }

        private bool HitTest(Point location, out int index, out int line)
        {
            index = -1;
            line = -1;
            for (int i = 0; i < info.Count; i++)
            {
                if (GetNoteBounds(i).Contains(location))
                {
                    index = i;
                    for (int l = 0; l < lines.Count; l++)
                    {
                        if (GetFieldBounds(i, l).Contains(location))
                        {
                            line = l;
                            return true;
                        }
                    }
                    Debug.Assert(false); // impossible
                }
            }
            return false;
        }

        private int mouseTrackingIndex = -1;
        private int mouseTrackingLine = -1;
        private int highlightedIndex = -1;
        private int highlightedLine = -1;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (textEditControl.Visible)
            {
                CommitFieldEdit();
            }

            HitTest(e.Location, out mouseTrackingIndex, out mouseTrackingLine);
            highlightedIndex = mouseTrackingIndex;
            highlightedLine = mouseTrackingLine;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (mouseTrackingIndex >= 0)
            {
                int i = mouseTrackingIndex;
                int l = mouseTrackingLine;
                mouseTrackingIndex = -1;
                mouseTrackingLine = -1;

                if (highlightedIndex >= 0)
                {
                    //BeginFieldEdit(i, l, GetValue(info[i].note, lines[l]));
                    ValueInfo valueInfo = ValueInfo.FindInlineParamVis(lines[l]);
                    valueInfo.DoClick(info[i].note, i, this);
                }

                highlightedIndex = -1;
                highlightedLine = -1;
            }
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (mouseTrackingIndex >= 0)
            {
                int oldHighlightIndex = highlightedIndex;
                int oldHighlightLine = highlightedLine;
                if (GetFieldBounds(mouseTrackingIndex, mouseTrackingLine).Contains(e.Location))
                {
                    highlightedIndex = mouseTrackingIndex;
                    highlightedLine = mouseTrackingLine;
                }
                else
                {
                    highlightedIndex = -1;
                    highlightedLine = -1;
                }
                if ((oldHighlightIndex != highlightedIndex) || (oldHighlightLine != highlightedLine))
                {
                    Invalidate();
                }
            }
            else
            {
                for (int l = 0; l < lines.Count; l++)
                {
                    Rectangle rect = new Rectangle(0, l * FontHeight, ClientSize.Width, FontHeight);
                    if (rect.Contains(e.Location))
                    {
                        if (lastToolTipLine != l)
                        {
                            lastToolTipLine = l;
                            toolTip.Show(
                                EnumUtility.GetDescription(lines[l], null),
                                this,
                                e.X + FontHeight,
                                e.Y + FontHeight,
                                30000);
                        }
                        return;
                    }
                }
            }

            lastToolTipLine = -1;
            toolTip.Hide(this);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            lastToolTipLine = -1;
            toolTip.Hide(this);
        }


        //

        public void SaveUndoInfo(string tag)
        {
            undoHelper.SaveUndoInfo(false/*forRedo*/, tag);
        }

        public void BeginFieldEdit(ValueInfo valueInfo, int noteIndex, string initialText)
        {
            int line = lines.IndexOf(valueInfo.InlineParam);
            Debug.Assert(line >= 0); // shouldn't happen
            if (line >= 0)
            {
                BeginFieldEdit(noteIndex, line, initialText);
            }
        }

        private void BeginFieldEdit(int index, int line, string initialText)
        {
            if (textEditControl.Visible)
            {
                CommitFieldEdit();
            }

            if (trackView != null)
            {
                trackView.TrackViewShowNote(info[index].note);
            }

            textEditControl.Location = new Point(info[index].x, line * FontHeight);
            textEditControl.Size = textEditControl.MinimumSize = new Size(Math.Max(info[index].width, maxInternalFieldWidth), FontHeight);
            textEditControl.Text = initialText;
            textEditControl.SelectAll();
            textEditControl.Visible = true;
            textEditControl.Focus();

            currentIndex = index;
            currentLine = line;

            Invalidate();
        }

        private void CommitFieldEdit()
        {
            if (currentIndex >= 0)
            {
                if (!String.Equals(textEditControl.Text, GetValue(info[currentIndex].note, lines[currentLine])))
                {
                    undoHelper.SaveUndoInfo(false/*forRedo*/, "Change Note Property");
                    SetValue(info[currentIndex].note, lines[currentLine], textEditControl.Text);
                }
            }

            textEditControl.Visible = false;
            currentIndex = -1;
            currentLine = -1;
            Invalidate();
        }

        private void CancelFieldEdit()
        {
            textEditControl.Visible = false;
            currentIndex = -1;
            currentLine = -1;
            Invalidate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!textEditControl.Visible)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            // inline edit is operative

            if ((keyData & Keys.KeyCode) == Keys.Escape)
            {
                CancelFieldEdit();
                if (focusReturnsTo != null)
                {
                    focusReturnsTo.Focus();
                }
            }
            else if ((keyData == (Keys.Enter | Keys.Alt)) || (keyData == (Keys.Enter | Keys.Shift)))
            {
                CommitFieldEdit();
                if (focusReturnsTo != null)
                {
                    focusReturnsTo.Focus();
                }
            }
            else if (keyData == Keys.Enter)
            {
                int lastIndex = currentIndex;
                int lastLine = currentLine;
                CommitFieldEdit();
                if (lastIndex >= 0)
                {
                    if (lastLine + 1 < lines.Count)
                    {
                        BeginFieldEdit(lastIndex, lastLine + 1, GetValue(info[lastIndex].note, lines[lastLine + 1]));
                    }
                    else if (lastIndex + 1 < info.Count)
                    {
                        BeginFieldEdit(lastIndex + 1, 0, GetValue(info[lastIndex + 1].note, lines[0]));
                    }
                    else
                    {
                        focusReturnsTo.Focus();
                    }
                }
            }
            else if (keyData == (Keys.Enter | Keys.Control))
            {
                int lastIndex = currentIndex;
                int lastLine = currentLine;
                CommitFieldEdit();
                if (lastIndex >= 0)
                {
                    if (lastLine - 1 >= 0)
                    {
                        BeginFieldEdit(lastIndex, lastLine - 1, GetValue(info[lastIndex].note, lines[lastLine - 1]));
                    }
                    else if (lastIndex - 1 >= 0)
                    {
                        BeginFieldEdit(lastIndex - 1, lines.Count - 1, GetValue(info[lastIndex - 1].note, lines[lines.Count - 1]));
                    }
                    else
                    {
                        focusReturnsTo.Focus();
                    }
                }
            }
            // editing shortcut keys
            else if (keyData == (Keys.A | Keys.Control))
            {
                textEditControl.SelectAll();
            }
            else if (keyData == (Keys.V | Keys.Control))
            {
                textEditControl.Paste();
            }
            else if (keyData == (Keys.X | Keys.Control))
            {
                textEditControl.Cut();
            }
            else if (keyData == (Keys.C | Keys.Control))
            {
                textEditControl.Copy();
            }
            else if (keyData == Keys.Tab)
            {
                int nextIndex = currentIndex + 1;
                int nextLine = currentLine;
                if (nextIndex < info.Count)
                {
                    CommitFieldEdit();
                    BeginFieldEdit(nextIndex, nextLine, GetValue(info[nextIndex].note, lines[nextLine]));
                }
            }
            else if (keyData == (Keys.Tab | Keys.Shift))
            {
                int previousIndex = currentIndex - 1;
                int previousLine = currentLine;
                if (previousIndex >= 0)
                {
                    CommitFieldEdit();
                    BeginFieldEdit(previousIndex, previousLine, GetValue(info[previousIndex].note, lines[previousLine]));
                }
            }
            else if (Array.IndexOf(NavKeys, keyData & Keys.KeyCode) >= 0)
            {
                return base.ProcessCmdKey(ref msg, keyData); // nav keys get routed normally
            }
            // default cases
            else if ((keyData & (Keys.Control | Keys.Alt)) == 0)
            {
                return base.ProcessCmdKey(ref msg, keyData); // unmodified keys get routed normally
            }
            return true; // eat all modified key commands - prevent going to other panels
        }
        private static readonly Keys[] NavKeys = new Keys[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown, Keys.F4 };

        private void TextEditControl_TextChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void TextEditControl_LostFocus(object sender, EventArgs e)
        {
            if (textEditControl.Visible)
            {
                CommitFieldEdit();
            }
        }
    }
}
