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
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class SampleControl : UserControl
    {
        private const float MINIMUMHORIZSCALE = 0.03125f;
        private const float MAXIMUMHORIZSCALE = 65536;

        private const bool UseGDITranslation = false;
        private int HorizontalIndex;

        private SampleObjectRec sampleObject;
        private float xScale = 1;
        private int selectionStart;
        private int selectionEnd;

        private int origin;
        private int loopStart;
        private string loopStartLabel;
        private int loopEnd;
        private string loopEndLabel;

        public SampleControl()
        {
            ResizeRedraw = true;
            //DoubleBuffered = true;

            InitializeComponent();

            Disposed += new EventHandler(SampleControl_Disposed);
        }

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

        void SampleControl_Disposed(object sender, EventArgs e)
        {
            if (sampleObject != null)
            {
                sampleObject.SampleDataChanged -= new SampleObjectRec.SampleStorageEventHandler(OnSampleDataChanged);
            }
        }

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
            // custom paint code
            if (sampleObject != null)
            {
                Redraw(pe.Graphics, true/*drawSample*/);
            }
            else
            {
                // draw X to indicate control is operating but has no content (vs. failing to start)
                pe.Graphics.FillRectangle(Brushes.White, ClientRectangle);
                pe.Graphics.DrawLine(Pens.Black, 0, 0, Width, Height);
                pe.Graphics.DrawLine(Pens.Black, Width, 0, 0, Height);
            }

            // Calling the base class OnPaint
            base.OnPaint(pe);
        }

        private void SetScrollOffsetsForRendering(Graphics graphics)
        {
            if (UseGDITranslation)
            {
                HorizontalIndex = 0;
                graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
            }
            else
            {
                HorizontalIndex = -AutoScrollPosition.X;
            }
        }

        private int OVERLINEHEIGHT { get { return FontHeight + 1; } }

        private Rectangle GetBoundsWave()
        {
            return new Rectangle(0, 3 * OVERLINEHEIGHT, ClientSize.Width, ClientSize.Height - 3 * OVERLINEHEIGHT);
        }

        private void Redraw(Graphics graphics, bool drawSample)
        {
            SetScrollOffsetsForRendering(graphics);

#if false
            graphics.DrawRectangle(Pens.Black, -HorizontalIndex, 0, ContentWidth, OVERLINEHEIGHT + 1);
            graphics.DrawRectangle(Pens.Black, -HorizontalIndex, OVERLINEHEIGHT, ContentWidth, OVERLINEHEIGHT + 1);
            graphics.DrawRectangle(Pens.Black, -HorizontalIndex, 2 * OVERLINEHEIGHT, ContentWidth, OVERLINEHEIGHT + 1);
            graphics.FillRectangle(Brushes.White, -HorizontalIndex + 1, 1, ContentWidth - 2, OVERLINEHEIGHT + 1 - 2);
            graphics.FillRectangle(Brushes.White, -HorizontalIndex + 1, 1 + OVERLINEHEIGHT, ContentWidth - 2, OVERLINEHEIGHT + 1 - 2);
            graphics.FillRectangle(Brushes.White, -HorizontalIndex + 1, 1 + 2 * OVERLINEHEIGHT, ContentWidth - 2, OVERLINEHEIGHT + 1 - 2);
            graphics.DrawString("Origin", Font, Brushes.Black, -HorizontalIndex + 5, 1);
            graphics.DrawString(loopStartLabel, Font, Brushes.Black, -HorizontalIndex + 5, 1 + OVERLINEHEIGHT);
            graphics.DrawString(loopEndLabel, Font, Brushes.Black, -HorizontalIndex + 5, 1 + 2 * OVERLINEHEIGHT);
#else
            {
                string[] lines = new string[] { "Origin", loopStartLabel, loopEndLabel };
                Rectangle rect = new Rectangle(0, 0, ClientSize.Width, OVERLINEHEIGHT - 1);
                foreach (string line in lines)
                {
                    const TextFormatFlags format = TextFormatFlags.Left | TextFormatFlags.LeftAndRightPadding
                        | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.SingleLine;
                    graphics.FillRectangle(Brushes.White, rect);
                    TextRenderer.DrawText(graphics, line, Font, rect, Color.Black, Color.White, format);
                    graphics.DrawLine(Pens.Black, 0, rect.Y + rect.Height, ClientSize.Width, rect.Y + rect.Height);
                    rect.Offset(0, OVERLINEHEIGHT);
                }
            }
#endif

            Rectangle boundsWave = GetBoundsWave();
            if (sampleObject != null)
            {
                if (drawSample)
                {
                    RedrawSamplePartial(boundsWave.X, boundsWave.X + boundsWave.Width);
                }
                if (sampleObject.NumChannels == NumChannelsType.eSampleStereo)
                {
#if false
                    RedrawSampleViewHelper(
                        graphics,
                        ChannelType.eLeftChannel,
                        new Rectangle(boundsWave.X, boundsWave.Y, boundsWave.Width - 1, boundsWave.Height / 2 - 1));
                    RedrawSampleViewHelper(
                        graphics, 
                        ChannelType.eRightChannel, 
                        new Rectangle(boundsWave.Y, boundsWave.Y + boundsWave.Height / 2 - 1 + 1, boundsWave.Width - 1, boundsWave.Height - boundsWave.Height / 2 - 1));
#endif
                    graphics.DrawLine(
                        Pens.Gray,
                        0,
                        boundsWave.Y + boundsWave.Height / 2 - 1,
                        ContentWidth,
                        boundsWave.Y + boundsWave.Height / 2 - 1);
                }
#if false
                else
                {
                    RedrawSampleViewHelper(
                        graphics, 
                        ChannelType.eMonoChannel, 
                        new Rectangle(boundsWave.X, boundsWave.Y, boundsWave.Width - 1, boundsWave.Height - 1));
                }
#endif
            }

            int Location = (int)((origin - HorizontalIndex) / xScale);
            graphics.FillPolygon(Brushes.Black, new Point[] { new Point(-4 + Location, 0), new Point(4 + Location, 0), new Point(Location, 8 - 1) });
            graphics.DrawLine(Pens.Black, Location, 0, Location, Height);

            Location = (int)((loopStart - HorizontalIndex) / xScale);
            graphics.FillPolygon(Brushes.Black, new Point[] { new Point(-4 + Location, OVERLINEHEIGHT), new Point(4 + Location, OVERLINEHEIGHT), new Point(Location, 7 + OVERLINEHEIGHT) });
            graphics.DrawLine(Pens.Black, Location, OVERLINEHEIGHT, Location, Height);

            Location = (int)((loopEnd - HorizontalIndex) / xScale);
            graphics.FillPolygon(Brushes.Black, new Point[] { new Point(-4 + Location, 2 * OVERLINEHEIGHT), new Point(4 + Location, 2 * OVERLINEHEIGHT), new Point(Location, 7 + 2 * OVERLINEHEIGHT) });
            graphics.DrawLine(Pens.Black, Location, 2 * OVERLINEHEIGHT, Location, Height);
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

            int NumSampleFrames = sampleObject.SampleData.NumFrames;

            for (int X = bounds.Left; X < bounds.Left + bounds.Width; X++)
            {
                int Index;
                Brush DotColor;
                Brush BackgroundColor;

                Index = (int)((X * xScale) + HorizontalIndex);

                if ((Index >= selectionStart) && (Index < selectionEnd))
                {
                    DotColor = Brushes.White;
                    BackgroundColor = Brushes.Black;
                }
                else if ((selectionStart == selectionEnd) && (Index == selectionStart))
                {
                    DotColor = Brushes.Black;
                    BackgroundColor = Brushes.LightGray;
                }
                else
                {
                    DotColor = Brushes.Black;
                    BackgroundColor = Brushes.White;
                }

                if ((Index >= 0) && (Index < NumSampleFrames))
                {
                    float ValMin;
                    float ValMax;
                    int IndexNext;
                    int i;
                    int PositionMin;
                    int PositionMax;

                    IndexNext = (int)(((X + 1) * xScale) + HorizontalIndex);
                    if (IndexNext > NumSampleFrames)
                    {
                        IndexNext = NumSampleFrames;
                    }
                    if (IndexNext < Index + 1)
                    {
                        IndexNext = Index + 1;
                    }

                    /* draw left line */
                    ValMin = 1;
                    ValMax = -1;
                    for (i = Index; i < IndexNext; i += 1)
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
                    PositionMin = (int)(((1 - ValMin) / 2) * (YMax - YMin - 1));
                    PositionMax = (int)(((1 - ValMax) / 2) * (YMax - YMin - 1));
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
                    graphics.FillRectangle(
                        BackgroundColor,
                        X,
                        YMin,
                        1,
                        YMax - YMin - 1 + 1);
                    graphics.FillRectangle(
                        DotColor,
                        X,
                        YMin + PositionMax,
                        1,
                        PositionMin - PositionMax + 1);

                    /* draw separating line */
                    if (((int)(Index / xScale)) % 8 == 0)
                    {
                        graphics.FillRectangle(
                            DotColor,
                            X,
                            YMin + (YMax - YMin - 1) / 2,
                            1,
                            1);
                    }
                }
                else
                {
                    graphics.FillRectangle(
                        Brushes.Wheat,
                        X,
                        YMin,
                        1,
                        YMax - YMin - 1 + 1);
                }
            }
        }

        public void Redraw()
        {
            using (Graphics graphics = CreateGraphics())
            {
                Redraw(graphics, true/*drawSample*/);
            }
        }

        public void RedrawSamplePartial(int x, int width)
        {
            using (Graphics graphics = CreateGraphics())
            {
                RedrawSamplePartial(graphics, x, width);
            }
        }

        public void RedrawSampleOneLine(int x)
        {
            using (Graphics graphics = CreateGraphics())
            {
                RedrawSamplePartial(graphics, x, 1);
            }
        }

        public int VisibleFrames
        {
            get
            {
                return (int)((ClientSize.Width - 2) * xScale - 1);
            }
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

                    mouseMoveMethod = delegate(MouseEventArgs e_, bool final)
                    {
                        MouseMoveSelection(e_, final, BaseSelectionStart, BaseSelectionEnd);
                    };
                }
            }
            else if (e.Y <= OVERLINEHEIGHT)
            {
                mouseMoveMethod = MouseMoveOrigin;
            }
            else if (e.Y <= OVERLINEHEIGHT * 2)
            {
                mouseMoveMethod = MouseMoveLoopStart;
            }
            else if (e.Y <= OVERLINEHEIGHT * 3)
            {
                mouseMoveMethod = MouseMoveLoopEnd;
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
            return (int)(x * xScale + HorizontalIndex);
        }

        // assumes HorizontalIndex is set, i.e. call SetScrollOffsetsForRendering()
        private int FrameToClientX(int frame)
        {
            return (int)((frame - HorizontalIndex) / xScale);
        }

        private void MouseMoveOrigin(MouseEventArgs e, bool final)
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
                RedrawSamplePartial(FrameToClientX(origin), 1);
                origin = ClientXToFrame(e.X);
                Redraw(graphics, false/*drawSample*/);
                if (OriginChanged != null)
                {
                    OriginChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void MouseMoveLoopStart(MouseEventArgs e, bool final)
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
                RedrawSamplePartial(FrameToClientX(loopStart), 1);
                loopStart = ClientXToFrame(e.X);
                Redraw(graphics, false/*drawSample*/);
                if (LoopStartChanged != null)
                {
                    LoopStartChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void MouseMoveLoopEnd(MouseEventArgs e, bool final)
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
                RedrawSamplePartial(FrameToClientX(loopEnd), 1);
                loopEnd = ClientXToFrame(e.X);
                Redraw(graphics, false/*drawSample*/);
                if (LoopEndChanged != null)
                {
                    LoopEndChanged.Invoke(this, EventArgs.Empty);
                }
            }
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
    }
}
