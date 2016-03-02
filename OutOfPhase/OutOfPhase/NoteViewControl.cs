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
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class NoteViewControl : UserControl, IValueInfoOwner
    {
        private Font boldFont;
        private int lastToolTipId = -1;
        private readonly Rectangle[] rectsFieldBounds = new Rectangle[ValueInfo.Values.Length];
        private readonly Rectangle[] rectsFieldEditableBounds = new Rectangle[ValueInfo.Values.Length];
        private UndoHelper undoHelper;
        private Color highlightBackColor = SystemColors.Window;
        private Brush backBrush;
        private Pen forePen;
        private Control focusReturnsTo;

        public NoteViewControl()
        {
            DoubleBuffered = true;

            InitializeComponent();

            textEditControl.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;

            textEditControl.TextChanged += TextEditControl_TextChanged;
            textEditControl.LostFocus += TextEditControl_LostFocus;
            textEditControl.BackColor = highlightBackColor;
            textEditControl.ForeColor = this.ForeColor;

            Disposed += NoteViewControl_Disposed;
        }

        private void NoteViewControl_Disposed(object sender, EventArgs e)
        {
            ClearGraphicsObjects();
        }

        protected override Size DefaultMinimumSize
        {
            get
            {
                return new Size(0, NUMLINES * Font.Height);
            }
        }

        private void ClearGraphicsObjects()
        {
            if (boldFont != null)
            {
                boldFont.Dispose();
                boldFont = null;
            }
            if (backBrush != null)
            {
                backBrush.Dispose();
                backBrush = null;
            }
            if (forePen != null)
            {
                forePen.Dispose();
                forePen = null;
            }
        }

        private void EnsureGraphicsObjects()
        {
            if (boldFont == null)
            {
                boldFont = new Font(Font, FontStyle.Bold);
            }
            if (backBrush == null)
            {
                backBrush = new SolidBrush(BackColor);
            }
            if (forePen == null)
            {
                forePen = new Pen(ForeColor);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            EnsureGraphicsObjects();
            textEditControl.Font = boldFont;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            MinimumSize = DefaultMinimumSize; // prevent height glitch on high dpi systems

            ClearGraphicsObjects();
            EnsureGraphicsObjects();
            textEditControl.Font = boldFont;
        }


        //

        [Category("Appearance"), DefaultValue(typeof(SystemColors), "Window")]
        public Color HighlightBackColor { get { return highlightBackColor; } set { highlightBackColor = value; } }

        [Category("Behavior")]
        public Control FocusReturnsTo { get { return focusReturnsTo; } set { focusReturnsTo = value; } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UndoHelper UndoHelper { get { return undoHelper; } set { undoHelper = value; } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextEditor.TextEditControl FloatingTextEdit { get { return textEditControl; } }


        //

        private NoteNoteObjectRec note;
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public NoteNoteObjectRec Note
        {
            get
            {
                return note;
            }
            set
            {
                NoteNoteObjectRec oldNote = note;
                note = value;
                if (oldNote != note)
                {
                    Invalidate();
                }
            }
        }

        private const int NUMLINES = 2;

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
            Redraw(pe.Graphics);
            base.OnPaint(pe);
        }

        private void Redraw(Graphics graphics)
        {
            if (note != null)
            {
                int xWidth = MeasureText(graphics, "x", Font);
                int sepWidth = 2 * xWidth;

                int x = 0, y = 0;

                StartLine(graphics, y);
                for (int i = 0; i < ValueInfo.Values.Length; i++)
                {
                    ValueInfo valueInfo = ValueInfo.Values[i];

                    if (valueInfo == null)
                    {
                        if (y == 0)
                        {
                            FinishLine(graphics, ref x, ref y);
                            StartLine(graphics, y);
                        }
                        continue;
                    }

                    string value = valueInfo.GetValue(Note);
                    string defaultValue = valueInfo.GetDefaultValue();

                    bool editing = currentFieldValueInfo == valueInfo;

                    AddText(
                        graphics,
                        valueInfo.Tag,
                        !editing ? value : textEditControl.Text, // value text
                        editing || !String.Equals(value, defaultValue), // bold
                        !editing && (highlightedItem == i), // highlight
                        ref x,
                        ref y,
                        out rectsFieldBounds[i],
                        out rectsFieldEditableBounds[i]);

                    if (valueInfo.SpacerFollows)
                    {
                        x += sepWidth;
                    }
                }
                FinishLine(graphics, ref x, ref y);

                MinimumSize = new Size(MinimumSize.Width, y);
            }
            else
            {
                graphics.FillRectangle(backBrush, ClientRectangle);

            }
        }

        private void Redraw()
        {
            using (Graphics graphics = CreateGraphics())
            {
                Redraw(graphics);
            }
        }

        private const TextFormatFlags FormatFlags = TextFormatFlags.Left | TextFormatFlags.NoClipping | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

        private int MeasureText(
            Graphics graphics,
            string text,
            Font font)
        {
            return MyTextRenderer.MeasureText(
                graphics,
                text,
                font,
                new Size(ClientSize.Width, FontHeight),
                FormatFlags).Width;
        }

        private void AddText(
            Graphics graphics,
            string label,
            string value,
            bool bold,
            bool highlight,
            ref int x,
            ref int y,
            out Rectangle bounds,
            out Rectangle editableBounds)
        {
            int labelWidth = MeasureText(graphics, label, Font);
            int valueWidth = MeasureText(graphics, value, bold ? boldFont : Font);
            if (x + labelWidth + valueWidth > ClientSize.Width)
            {
                FinishLine(graphics, ref x, ref y);
                StartLine(graphics, y);
            }
            bounds = new Rectangle(x, y, labelWidth + valueWidth, FontHeight);
            editableBounds = new Rectangle(x + labelWidth, y, valueWidth, FontHeight);
            MyTextRenderer.DrawText(
                graphics,
                label,
                Font,
                new Point(
                    x,
                    y),
                ForeColor,
                FormatFlags);
            x += labelWidth;
            MyTextRenderer.DrawText(
                graphics,
                value,
                bold ? boldFont : Font,
                new Point(
                    x,
                    y),
                !highlight ? ForeColor : BackColor,
                !highlight ? Color.Transparent : ForeColor,
                FormatFlags);
            x += valueWidth;
        }

        private void StartLine(
            Graphics graphics,
            int y)
        {
            graphics.FillRectangle(backBrush, 0, y, ClientSize.Width, FontHeight);
        }

        private void FinishLine(
            Graphics graphics,
            ref int x,
            ref int y)
        {
            x = 0;
            y += FontHeight;
        }


        //

        private bool HitTest(Point location, out int index)
        {
            index = -1;
            for (int i = 0; i < rectsFieldBounds.Length; i++)
            {
                if (rectsFieldBounds[i].Contains(location))
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }

        private int mouseTrackingItem = -1;
        private int highlightedItem = -1;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (Note != null)
            {
                HitTest(e.Location, out mouseTrackingItem);
                highlightedItem = mouseTrackingItem;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            highlightedItem = -1;
            Invalidate();
            if (mouseTrackingItem >= 0)
            {
                int i = mouseTrackingItem;
                mouseTrackingItem = -1;
                if (rectsFieldBounds[i].Contains(e.Location))
                {
                    ValueInfo.Values[i].DoClick(Note, -1, this);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (Note != null)
            {
                if (mouseTrackingItem >= 0)
                {
                    int oldHighlight = highlightedItem;
                    if (rectsFieldBounds[mouseTrackingItem].Contains(e.Location))
                    {
                        highlightedItem = mouseTrackingItem;
                    }
                    else
                    {
                        highlightedItem = -1;
                    }
                    if (oldHighlight != highlightedItem)
                    {
                        Invalidate();
                    }
                }
                else
                {
                    int i;
                    if (HitTest(e.Location, out i))
                    {
                        if (lastToolTipId != i)
                        {
                            lastToolTipId = i;
                            toolTip.Show(
                                String.Format("{0}: {1}", ValueInfo.Values[i].Description, ValueInfo.Values[i].GetValue(Note)),
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

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            lastToolTipId = -1;
            toolTip.Hide(this);
        }


        //

        private ValueInfo currentFieldValueInfo;

        public void SaveUndoInfo(string tag)
        {
            undoHelper.SaveUndoInfo(false/*forRedo*/, tag);
        }

        public void BeginFieldEdit(ValueInfo valueInfo, int noteIndexNotUsed, string initialText)
        {
            Debug.Assert(Note != null);

            if (textEditControl.Visible)
            {
                CommitFieldEdit(valueInfo);
            }

            int index = Array.IndexOf(ValueInfo.Values, valueInfo);
            Debug.Assert(index >= 0);

            textEditControl.Location = rectsFieldEditableBounds[index].Location;
            textEditControl.Size = rectsFieldEditableBounds[index].Size;
            textEditControl.Text = initialText;
            textEditControl.SelectAll();
            textEditControl.Visible = true;
            textEditControl.Focus();

            currentFieldValueInfo = valueInfo;

            Invalidate();
        }

        private void CommitFieldEdit(ValueInfo valueInfo)
        {
            int index = Array.IndexOf(ValueInfo.Values, valueInfo);
            Debug.Assert(index >= 0);

            if (Note != null)
            {
                if (!String.Equals(textEditControl.Text, valueInfo.GetValue(Note)))
                {
                    undoHelper.SaveUndoInfo(false/*forRedo*/, "Change Note Property");
                    valueInfo.SetValue(Note, textEditControl.Text);
                }
            }

            textEditControl.Visible = false;
            currentFieldValueInfo = null;
            Invalidate();
        }

        private void CancelFieldEdit()
        {
            textEditControl.Visible = false;
            currentFieldValueInfo = null;
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
            else if (keyData == Keys.Enter)
            {
                CommitFieldEdit(currentFieldValueInfo);
                if (focusReturnsTo != null)
                {
                    focusReturnsTo.Focus();
                }
            }
            else if ((keyData & ~Keys.Modifiers) == Keys.Tab)
            {
                int index = Array.IndexOf(ValueInfo.Values, currentFieldValueInfo);
                Debug.Assert(index >= 0);
                CommitFieldEdit(currentFieldValueInfo);
                do
                {
                    int increment = (keyData & Keys.Shift) == 0 ? 1 : ValueInfo.Values.Length - 1;
                    index = (index + increment) % ValueInfo.Values.Length;
                } while (ValueInfo.Values[index] == null);
                BeginFieldEdit(ValueInfo.Values[index], -1, ValueInfo.Values[index].GetValue(Note));
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
                CommitFieldEdit(currentFieldValueInfo);
            }
        }
    }
}
