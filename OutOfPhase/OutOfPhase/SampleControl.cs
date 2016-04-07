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
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class SampleControl : ScrollableControl
    {
        private const float MINIMUMHORIZSCALE = 0.03125f;
        private const float MAXIMUMHORIZSCALE = 65536;

        private int horizontalIndex;

        private SampleObjectRec sampleObject;
        private float xScale = 1;
        private int selectionStart;
        private int selectionEnd;

        private int origin;
        private int loopStart;
        private string loopStartLabel;
        private int loopEnd;
        private string loopEndLabel;

        private Color selectedBackColor = SystemColors.Highlight;
        private Color selectedForeColor = SystemColors.Control;
        private Color selectedBackColorInactive = SystemColors.InactiveCaption;
        private Color selectedForeColorInactive = SystemColors.ControlText;
        private Color afterEndColor = SystemColors.Info;
        private Color insertionPointColor = SystemColors.ButtonShadow;
        private Color trimColor = SystemColors.ButtonShadow;

        private Pen forePen;
        private Brush foreBrush;
        private Brush backBrush;
        private Brush selectedBackBrush;
        private Brush selectedForeBrush;
        private Brush selectedBackBrushInactive;
        private Brush selectedForeBrushInactive;
        private Brush afterEndBrush;
        private Brush insertionPointBrush;
        private Pen trimPen;

        public SampleControl()
        {
            ResizeRedraw = true;

            InitializeComponent();

            Disposed += new EventHandler(SampleControl_Disposed);
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SampleObjectRec SampleObject
        {
            get
            {
                return sampleObject;
            }
            set
            {
                if (sampleObject != null)
                {
                    sampleObject.SampleDataChanged -= new SampleObjectRec.SampleStorageEventHandler(OnSampleDataChanged);
                }

                sampleObject = value;

                if (sampleObject != null)
                {
                    sampleObject.SampleDataChanged += new SampleObjectRec.SampleStorageEventHandler(OnSampleDataChanged);
                }

                AutoScrollMinSize = new Size(ContentWidth, 0);

                Invalidate();
            }
        }

        private void SampleControl_Disposed(object sender, EventArgs e)
        {
            if (sampleObject != null)
            {
                sampleObject.SampleDataChanged -= new SampleObjectRec.SampleStorageEventHandler(OnSampleDataChanged);
            }

            if (forePen != null)
            {
                forePen.Dispose();
            }
            if (foreBrush != null)
            {
                foreBrush.Dispose();
            }
            if (backBrush != null)
            {
                backBrush.Dispose();
            }
            if (selectedBackBrush != null)
            {
                selectedBackBrush.Dispose();
            }
            if (selectedForeBrush != null)
            {
                selectedForeBrush.Dispose();
            }
            if (selectedBackBrushInactive != null)
            {
                selectedBackBrushInactive.Dispose();
            }
            if (selectedForeBrushInactive != null)
            {
                selectedForeBrushInactive.Dispose();
            }
            if (afterEndBrush != null)
            {
                afterEndBrush.Dispose();
            }
            if (insertionPointBrush != null)
            {
                insertionPointBrush.Dispose();
            }
            if (trimPen != null)
            {
                trimPen.Dispose();
            }
        }

        private void EnsureGraphicsObjects()
        {
            if (forePen == null)
            {
                forePen = new Pen(ForeColor);
            }
            if (foreBrush == null)
            {
                foreBrush = new SolidBrush(ForeColor);
            }
            if (backBrush == null)
            {
                backBrush = new SolidBrush(BackColor);
            }
            if (selectedBackBrush == null)
            {
                selectedBackBrush = new SolidBrush(selectedBackColor);
            }
            if (selectedForeBrush == null)
            {
                selectedForeBrush = new SolidBrush(selectedForeColor);
            }
            if (selectedBackBrushInactive == null)
            {
                selectedBackBrushInactive = new SolidBrush(selectedBackColorInactive);
            }
            if (selectedForeBrushInactive == null)
            {
                selectedForeBrushInactive = new SolidBrush(selectedForeColorInactive);
            }
            if (afterEndBrush == null)
            {
                afterEndBrush = new SolidBrush(afterEndColor);
            }
            if (insertionPointBrush == null)
            {
                insertionPointBrush = new SolidBrush(insertionPointColor);
            }
            if (trimPen == null)
            {
                trimPen = new Pen(trimColor);
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ContentWidth { get { return sampleObject != null ? (int)Math.Ceiling(sampleObject.SampleData.NumFrames / xScale) : 0; } }

        public void OnSampleDataChanged(object sender, SampleObjectRec.SampleStorageEventArgs e)
        {
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // background erase not need - entire surface is painted
            if (!DesignMode)
            {
                return;
            }
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            EnsureGraphicsObjects();

            if (sampleObject != null)
            {
                Redraw(pe.Graphics, true/*drawSample*/);
            }
            else
            {
                // draw X to indicate control is operating but has no content (vs. failing to start)
                pe.Graphics.FillRectangle(backBrush, ClientRectangle);
                pe.Graphics.DrawLine(forePen, 0, 0, Width, Height);
                pe.Graphics.DrawLine(forePen, Width, 0, 0, Height);
            }

            base.OnPaint(pe);
        }

        private void SetScrollOffsetsForRendering(Graphics graphics)
        {
            horizontalIndex = -AutoScrollPosition.X;
        }

        private int OVERLINEHEIGHT { get { return FontHeight + 1; } }

        private Rectangle GetBoundsOverline()
        {
            return new Rectangle(0, 0, ClientSize.Width, 3 * OVERLINEHEIGHT);
        }

        private Rectangle GetBoundsWave()
        {
            return new Rectangle(0, 3 * OVERLINEHEIGHT, ClientSize.Width, ClientSize.Height - 3 * OVERLINEHEIGHT);
        }

        private void Redraw(Graphics graphics, bool drawSample)
        {
            EnsureGraphicsObjects();

            SetScrollOffsetsForRendering(graphics);

            Rectangle boundsWave = GetBoundsWave();
            if (sampleObject != null)
            {
                if (drawSample)
                {
                    RedrawSamplePartial(boundsWave.X, boundsWave.X + boundsWave.Width);
                }
                if (sampleObject.NumChannels == NumChannelsType.eSampleStereo)
                {
                    graphics.DrawLine(
                        trimPen,
                        0,
                        boundsWave.Y + boundsWave.Height / 2 - 1,
                        ContentWidth,
                        boundsWave.Y + boundsWave.Height / 2 - 1);
                }
            }

            using (Bitmap offscreenBitmap = new Bitmap(ClientSize.Width, 3 * OVERLINEHEIGHT, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            {
                using (Graphics offscreenGraphics = Graphics.FromImage(offscreenBitmap))
                {
                    string[] lines = new string[] { "Origin", loopStartLabel, loopEndLabel };
                    Rectangle rect = new Rectangle(0, 0, ClientSize.Width, OVERLINEHEIGHT - 1);
                    foreach (string line in lines)
                    {
                        const TextFormatFlags format = TextFormatFlags.Left | TextFormatFlags.LeftAndRightPadding
                            | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.SingleLine;
                        offscreenGraphics.FillRectangle(backBrush, rect);
                        if (scrolling == 0)
                        {
                            MyTextRenderer.DrawText(offscreenGraphics, line, Font, rect, ForeColor, BackColor, format);
                        }
                        offscreenGraphics.DrawLine(forePen, 0, rect.Y + rect.Height, ClientSize.Width, rect.Y + rect.Height);
                        rect.Offset(0, OVERLINEHEIGHT);
                    }

                    int prevIndex = -1;
                    for (int X = 0; X < ClientSize.Width; X++)
                    {
                        int Index = (int)((X + horizontalIndex) * xScale);
                        int IndexNext = (int)((X + 1 + horizontalIndex) * xScale);
                        if (IndexNext < Index + 1)
                        {
                            IndexNext = Index + 1;
                        }

                        if (prevIndex != Index)
                        {
                            bool[] which = new bool[]
                            {
                                (origin >= Index) && (origin < IndexNext),
                                (loopStart >= Index) && (loopStart < IndexNext),
                                (loopEnd >= Index) && (loopEnd < IndexNext),
                            };
                            for (int i = 0; i < which.Length; i++)
                            {
                                if (which[i])
                                {
                                    SmoothingMode oldSmoothingMode = offscreenGraphics.SmoothingMode;
                                    offscreenGraphics.SmoothingMode = SmoothingMode.AntiAlias;

                                    int Location = X;
                                    offscreenGraphics.FillPolygon(
                                        foreBrush,
                                        new Point[]
                                        {
                                            new Point(-4 + Location, i * OVERLINEHEIGHT - 1),
                                            new Point(Location, 7 + i * OVERLINEHEIGHT),
                                            new Point(4 + Location, i * OVERLINEHEIGHT - 1),
                                        });

                                    offscreenGraphics.SmoothingMode = oldSmoothingMode;

                                    offscreenGraphics.DrawLine(
                                        forePen,
                                        Location,
                                        i * OVERLINEHEIGHT,
                                        Location,
                                        Height);
                                }
                            }
                        }

                        prevIndex = Index;
                    }
                }

                graphics.DrawImage(offscreenBitmap, 0, 0, offscreenBitmap.Width, offscreenBitmap.Height);
            }
        }

        private void RedrawSamplePartial(Graphics graphics, int x, int width)
        {
            SetScrollOffsetsForRendering(graphics);

            Rectangle boundsWave = GetBoundsWave();
            if (sampleObject.NumChannels == NumChannelsType.eSampleStereo)
            {
                int centerOffset = boundsWave.Height / 2;
                RedrawSampleViewHelper(
                    graphics,
                    ChannelType.eLeftChannel,
                    new Rectangle(x, boundsWave.Y, width, centerOffset - 1));
                RedrawSampleViewHelper(
                    graphics,
                    ChannelType.eRightChannel,
                    new Rectangle(x, boundsWave.Y + centerOffset, width, boundsWave.Height - centerOffset - 1));
            }
            else
            {
                RedrawSampleViewHelper(
                    graphics,
                    ChannelType.eMonoChannel,
                    new Rectangle(x, boundsWave.Y, width, boundsWave.Height));
            }
        }

        private void RedrawSampleViewHelper(Graphics graphics, ChannelType channel, Rectangle bounds)
        {
            int YMin = bounds.Y;
            int YMax = bounds.Y + bounds.Height;
            if (YMax <= YMin)
            {
                return;
            }

            int NumSampleFrames = sampleObject.SampleData.NumFrames;

            const int OffscreenStripWidth = 16;
            using (Bitmap offscreenBitmap = new Bitmap(OffscreenStripWidth, YMax - YMin, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            {
                int prevIndex = -1;
                for (int XX = bounds.Left; XX < bounds.Left + bounds.Width + (OffscreenStripWidth - 1); XX += OffscreenStripWidth)
                {
                    int Limit = Math.Min(XX + OffscreenStripWidth, bounds.Left + bounds.Width);

                    using (Graphics offscreenGraphics = Graphics.FromImage(offscreenBitmap))
                    {
                        for (int X = XX; X < Limit; X++)
                        {
                            int Index = (int)((X + horizontalIndex) * xScale);
                            int IndexNext = (int)((X + 1 + horizontalIndex) * xScale);
                            if (IndexNext > NumSampleFrames)
                            {
                                IndexNext = NumSampleFrames;
                            }
                            if (IndexNext < Index + 1)
                            {
                                IndexNext = Index + 1;
                            }

                            Brush DotColor;
                            Brush BackgroundColor;
                            if ((Index >= selectionStart) && (Index < selectionEnd))
                            {
                                DotColor = Focused ? selectedForeBrush : selectedForeBrushInactive;
                                BackgroundColor = Focused ? selectedBackBrush : selectedBackBrushInactive;
                            }
                            else if ((selectionStart == selectionEnd) && (selectionStart >= Index) && (SelectionStart < IndexNext))
                            {
                                DotColor = foreBrush;
                                BackgroundColor = insertionPointBrush;
                            }
                            else
                            {
                                DotColor = foreBrush;
                                BackgroundColor = backBrush;
                            }

                            if ((Index >= 0) && (Index < NumSampleFrames))
                            {
                                /* draw left line */
                                float ValMin = 1;
                                float ValMax = -1;
                                for (int i = Index; i < IndexNext; i++)
                                {
                                    float Value;

                                    switch (channel)
                                    {
                                        default:
                                            throw new ArgumentException();
                                        case ChannelType.eMonoChannel:
                                            Value = sampleObject.SampleData.Buffer[i];
                                            break;
                                        case ChannelType.eLeftChannel:
                                            Value = sampleObject.SampleData.Buffer[2 * i + 0];
                                            break;
                                        case ChannelType.eRightChannel:
                                            Value = sampleObject.SampleData.Buffer[2 * i + 1];
                                            break;
                                    }
                                    if (Value < ValMin)
                                    {
                                        ValMin = Value;
                                    }
                                    if (Value > ValMax)
                                    {
                                        ValMax = Value;
                                    }
                                }
                                int PositionMin = (int)(((1 - ValMin) / 2) * (YMax - YMin - 1));
                                int PositionMax = (int)(((1 - ValMax) / 2) * (YMax - YMin - 1));
                                /* zero point:  displace very nearly zero points away from the */
                                /* origin line so you can see they aren't zero */
                                if (PositionMin == (YMax - YMin - 1) / 2)
                                {
                                    if (ValMin < 0)
                                    {
                                        PositionMin += 1;
                                    }
                                    else if (ValMin > 0)
                                    {
                                        PositionMin -= 1;
                                    }
                                }
                                if (PositionMax == (YMax - YMin - 1) / 2)
                                {
                                    if (ValMax < 0)
                                    {
                                        PositionMax += 1;
                                    }
                                    else if (ValMax > 0)
                                    {
                                        PositionMax -= 1;
                                    }
                                }
                                offscreenGraphics.FillRectangle(
                                    BackgroundColor,
                                    X - XX,
                                    YMin - YMin,
                                    1,
                                    YMax - YMin - 1 + 1);
                                offscreenGraphics.FillRectangle(
                                    DotColor,
                                    X - XX,
                                    YMin - YMin + PositionMax,
                                    1,
                                    PositionMin - PositionMax + 1);

                                /* draw separating line */
                                if (((int)(Index / xScale)) % 8 == 0)
                                {
                                    offscreenGraphics.FillRectangle(
                                        DotColor,
                                        X - XX,
                                        YMin - YMin + (YMax - YMin - 1) / 2,
                                        1,
                                        1);
                                }

                                // draw descending overline if needed
                                if ((prevIndex != Index)
                                    && (((origin >= Index) && (origin < IndexNext))
                                        || ((loopStart >= Index) && (loopStart < IndexNext))
                                        || ((loopEnd >= Index) && (loopEnd < IndexNext))))
                                {
                                    offscreenGraphics.DrawLine(
                                        forePen,
                                        X - XX,
                                        2 * OVERLINEHEIGHT - YMin,
                                        X - XX,
                                        Height - YMin);
                                }
                            }
                            else
                            {
                                offscreenGraphics.FillRectangle(
                                    afterEndBrush,
                                    X - XX,
                                    YMin - YMin,
                                    1,
                                    YMax - YMin - 1 + 1);
                            }

                            prevIndex = Index;
                        }
                    }

                    graphics.SetClip(new Rectangle(XX, YMin, Math.Min(OffscreenStripWidth, Limit - XX), YMax - YMin));
                    graphics.DrawImage(offscreenBitmap, XX, YMin, OffscreenStripWidth, YMax - YMin);
                }
            }
        }

        public void Redraw()
        {
            EnsureGraphicsObjects();
            using (Graphics graphics = CreateGraphics())
            {
                Redraw(graphics, true/*drawSample*/);
            }
        }

        public void RedrawSamplePartial(int x, int width)
        {
            EnsureGraphicsObjects();
            using (Graphics graphics = CreateGraphics())
            {
                RedrawSamplePartial(graphics, x, width);
            }
        }

        public void RedrawSampleOneLine(int x)
        {
            EnsureGraphicsObjects();
            using (Graphics graphics = CreateGraphics())
            {
                RedrawSamplePartial(graphics, x, 1);
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int VisibleFrames
        {
            get
            {
                return (int)((ClientSize.Width - 2) * xScale - 1);
            }
        }

        private int scrolling;
        protected override void OnScroll(ScrollEventArgs se)
        {
            if (se.NewValue == se.OldValue)
            {
                base.OnScroll(se);
                return;
            }

            scrolling++;
            try
            {
                // Redraw with scrolling!=0 to suppress annotation of field names, otherwise they leave artifacts
                // as bits are blitted during base.OnScroll().
                using (Graphics graphics = CreateGraphics())
                {
                    Redraw(graphics, false/*drawSample*/); // (remove annotation of field names)
                }

                base.OnScroll(se);
            }
            finally
            {
                Update(); // redraw scrolled-in area immediately (without annotation of field names)
                scrolling--;
            }

            Invalidate(GetBoundsOverline()); // deferred update to repaint with annotation of field names
        }


        // Mouse

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            Rectangle boundsWave = GetBoundsWave();
            if (boundsWave.Contains(e.Location))
            {
                using (Graphics graphics = CreateGraphics())
                {
                    SetScrollOffsetsForRendering(graphics);

                    // sample selection
                    int BaseSelectionStart, BaseSelectionEnd;
                    if ((ModifierKeys & Keys.Shift) != 0)
                    {
                        /* extending existing selection */
                        BaseSelectionStart = selectionStart;
                        BaseSelectionEnd = selectionEnd;
                    }
                    else
                    {
                        /* replacing existing selection */
                        BaseSelectionStart = BaseSelectionEnd = ClientXToFrame(e.X); // needs SetScrollOffsetsForRendering()
                        //SetSelection(BaseSelectionStart, BaseSelectionEnd, true/*startValidatePriority*/);
                    }

                    mouseMoveMethod = delegate (MouseEventArgs e_, bool final)
                    {
                        MouseMoveSelection(e_, final, BaseSelectionStart, BaseSelectionEnd);
                    };
                }
            }
            else if (e.Y <= OVERLINEHEIGHT)
            {
                int initialOrigin = origin;
                mouseMoveMethod = delegate (MouseEventArgs ee, bool final) { MouseMoveOrigin(ee, final, initialOrigin); };
            }
            else if (e.Y <= OVERLINEHEIGHT * 2)
            {
                int initialLoopStart = loopStart;
                mouseMoveMethod = delegate (MouseEventArgs ee, bool final) { MouseMoveLoopStart(ee, final, initialLoopStart); };
            }
            else if (e.Y <= OVERLINEHEIGHT * 3)
            {
                int initialLoopEnd = LoopEnd;
                mouseMoveMethod = delegate (MouseEventArgs ee, bool final) { MouseMoveLoopEnd(ee, final, initialLoopEnd); };
            }
        }

        private void MouseScrollLeft()
        {
            AutoScrollPosition = new Point(-AutoScrollPosition.X - 1 * VisibleFrames / 8, AutoScrollPosition.Y);
            Update();
        }

        private void MouseScrollRight()
        {
            AutoScrollPosition = new Point(-AutoScrollPosition.X + 1 * VisibleFrames / 8, AutoScrollPosition.Y);
            Update();
        }

        // assumes HorizontalIndex is set, i.e. call SetScrollOffsetsForRendering()
        private int ClientXToFrame(int x)
        {
            return (int)((x + horizontalIndex) * xScale);
        }

        // assumes HorizontalIndex is set, i.e. call SetScrollOffsetsForRendering()
        private int FrameToClientX(int frame)
        {
            return (int)(frame / xScale - horizontalIndex);
        }

        private delegate int GetValue();
        private delegate void SetValue(int value);
        private void MouseMoveOverline(
            MouseEventArgs e,
            bool final,
            int initialValue,
            GetValue getValue,
            SetValue setValue,
            EventHandler changed)
        {
            using (Graphics graphics = CreateGraphics())
            {
                if (e.X < 0)
                {
                    MouseScrollLeft();
                }
                else if (e.X >= ClientSize.Width)
                {
                    MouseScrollRight();
                }

                SetScrollOffsetsForRendering(graphics);
                int oldValue = getValue();
                int newValue = (e.Y >= 0) && (e.Y < ClientSize.Height) ? ClientXToFrame(e.X) : initialValue;
                setValue(newValue);
                RedrawSamplePartial(FrameToClientX(oldValue), 1);
                RedrawSamplePartial(FrameToClientX(newValue), 1);
                Redraw(graphics, false/*drawSample*/); // update overline header
                if (changed != null)
                {
                    changed.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void MouseMoveOrigin(MouseEventArgs e, bool final, int initialValue)
        {
            MouseMoveOverline(
                e,
                final,
                initialValue,
                delegate () { return origin; },
                delegate (int value) { origin = value; },
                OriginChanged);
        }

        private void MouseMoveLoopStart(MouseEventArgs e, bool final, int initialValue)
        {
            MouseMoveOverline(
                e,
                final,
                initialValue,
                delegate () { return loopStart; },
                delegate (int value) { loopStart = value; },
                LoopStartChanged);
        }

        private void MouseMoveLoopEnd(MouseEventArgs e, bool final, int initialValue)
        {
            MouseMoveOverline(
                e,
                final,
                initialValue,
                delegate () { return loopEnd; },
                delegate (int value) { loopEnd = value; },
                LoopEndChanged);
        }

        private void MouseMoveSelection(MouseEventArgs e, bool final, int BaseSelectionStart, int BaseSelectionEnd)
        {
            using (Graphics graphics = CreateGraphics())
            {
                if (e.X < 0)
                {
                    MouseScrollLeft();
                }
                else if (e.X >= ClientSize.Width)
                {
                    MouseScrollRight();
                }

                SetScrollOffsetsForRendering(graphics);
                int NewTempSelectStart = BaseSelectionStart;
                int NewTempSelectEnd = BaseSelectionEnd;
                int frame = ClientXToFrame(e.X);
                if (NewTempSelectStart > frame)
                {
                    NewTempSelectStart = frame;
                }
                if (NewTempSelectEnd < frame)
                {
                    NewTempSelectEnd = frame;
                }
                SetSelection(NewTempSelectStart, NewTempSelectEnd, true/*startValidatePriority*/); // events
                Update();
            }
        }

        private delegate void MouseMoveMethod(MouseEventArgs e, bool final);

        private MouseMoveMethod mouseMoveMethod;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (mouseMoveMethod != null)
            {
                mouseMoveMethod(e, false/*final*/);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (mouseMoveMethod != null)
            {
                mouseMoveMethod(e, true/*final*/); // one final update
                mouseMoveMethod = null;
            }
        }


        // Properties

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float XScale
        {
            get
            {
                return xScale;
            }
            set
            {
                float NewHorizontalScale = Math.Min(Math.Max(value, MINIMUMHORIZSCALE), MAXIMUMHORIZSCALE);
                /* if we zoom in or out, try to center it */
                /* if we zoom in a factor of 2, then we lose 1/4 off each side, thus */
                /* adjust Xposition by 1/4 of visible frames */
                /* if we zoom in a factor of 4, then we lose 6/8 of image, or 3/8 off each side */
                /* if we zoom by 0.5, we gain on each side. */
                int HalfVisibleFrames = VisibleFrames / 2;
                float ZoomFactor = xScale / NewHorizontalScale; /* >1 = zoom in, <1 = zoom out */
                xScale = NewHorizontalScale;
                AutoScrollMinSize = new Size(ContentWidth/*uses xScale*/, 0);
                AutoScrollPosition = new Point(
                    Math.Max((int)(-AutoScrollPosition.X + HalfVisibleFrames - (HalfVisibleFrames / ZoomFactor)), 0),
                    AutoScrollPosition.Y);
                Invalidate();
                if (XScaleChanged != null)
                {
                    XScaleChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler XScaleChanged;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionStart
        {
            get
            {
                return selectionStart;
            }
            set
            {
                SetSelection(value, selectionEnd, true/*startValidatePriority*/);
            }
        }
        public event EventHandler SelectionStartChanged;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionEnd
        {
            get
            {
                return selectionEnd;
            }
            set
            {
                SetSelection(selectionStart, value, false/*startValidatePriority*/);
            }
        }
        public event EventHandler SelectionEndChanged;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SelectionNonEmpty { get { return selectionStart < selectionEnd; } }

        public void SetSelection(int start, int end, bool startValidatePriority)
        {
            int oldStart = selectionStart;
            int oldEnd = selectionEnd;
            selectionStart = start;
            selectionEnd = end;
            ValidateSelection(startValidatePriority);
            Invalidate();
            if ((oldStart != selectionStart) && (SelectionStartChanged != null))
            {
                SelectionStartChanged.Invoke(this, EventArgs.Empty);
            }
            if ((oldEnd != selectionEnd) && (SelectionEndChanged != null))
            {
                SelectionEndChanged.Invoke(this, EventArgs.Empty);
            }
        }

        private void ValidateSelection(bool startValidatePriority)
        {
            if (selectionStart > selectionEnd)
            {
                if (startValidatePriority)
                {
                    selectionEnd = selectionStart;
                }
                else
                {
                    selectionStart = selectionEnd;
                }
            }
            selectionStart = Math.Min(Math.Max(selectionStart, 0), sampleObject.NumFrames);
            selectionEnd = Math.Min(Math.Max(selectionEnd, 0), sampleObject.NumFrames);
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Origin
        {
            get
            {
                return origin;
            }
            set
            {
                bool raise = (origin != value);
                origin = value;
                Invalidate();
                if (raise && (OriginChanged != null))
                {
                    OriginChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler OriginChanged;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int LoopStart
        {
            get
            {
                return loopStart;
            }
            set
            {
                bool raise = (loopStart != value);
                loopStart = value;
                Invalidate();
                if (raise && (LoopStartChanged != null))
                {
                    LoopStartChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler LoopStartChanged;

        [Browsable(true), Category("Appearance"), DefaultValue("")]
        public string LoopStartLabel { get { return loopStartLabel; } set { loopStartLabel = value; Invalidate(); } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int LoopEnd
        {
            get
            {
                return loopEnd;
            }
            set
            {
                bool raise = (loopEnd != value);
                loopEnd = value;
                Invalidate();
                if (raise && (LoopEndChanged != null))
                {
                    LoopEndChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler LoopEndChanged;

        [Browsable(true), Category("Appearance"), DefaultValue("")]
        public string LoopEndLabel { get { return loopEndLabel; } set { loopEndLabel = value; Invalidate(); } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int HorizontalIndex { get { return horizontalIndex; } }


        //

        [Category("Appearance"), DefaultValue(typeof(Color), "Highlight")]
        public Color SelectedBackColor { get { return selectedBackColor; } set { selectedBackColor = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "Control")]
        public Color SelectedForeColor { get { return selectedForeColor; } set { selectedForeColor = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "InactiveCaption")]
        public Color SelectedBackColorInactive { get { return selectedBackColorInactive; } set { selectedBackColorInactive = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "ControlText")]
        public Color SelectedForeColorInactive { get { return selectedForeColorInactive; } set { selectedForeColorInactive = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "Info")]
        public Color AfterEndColor { get { return afterEndColor; } set { afterEndColor = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "ButtonShadow")]
        public Color InsertionPointColor { get { return insertionPointColor; } set { insertionPointColor = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "ButtonShadow")]
        public Color TrimColor { get { return trimColor; } set { trimColor = value; } }
    }
}
