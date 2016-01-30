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
using System.IO;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;

namespace OutOfPhase
{
    // TODO: for WPF, all this custom drawn code should be rewritten as a hierarchy of modular elements

    public partial class TrackViewControl : UserControl, IGraphicsContext, IUndoClient
    {
        private TrackObjectRec trackObject;

        private int PixelIndent; // horizontal scroll offset
        private int VerticalOffset; // vertical scroll offset

        private bool CursorBarIsVisible;
        private int CursorBarLoc;
        private bool CursorBarIsLocked;
        private int LastCursorBarX;
        private int LastCursorBarY;

        private SelectionModes SelectionMode = SelectionModes.eTrackViewNoSelection;
        private int InsertionPointIndex; /* valid only for eTrackViewNoSelection */
        private NoteObjectRec SelectedNote; /* valid only for single selection */
        private int SelectedNoteFrame; /* valid only for single selection */
        private int RangeSelectStart; /* valid only for eTrackViewRangeSelection */
        private int RangeSelectEnd; /* valid only for eTrackViewRangeSelection */
        private bool RangeSelectStartIsActive; /* valid only for eTrackViewRangeSelection */

        private int phantomInsertionPointIndex = -1;

        private TrackDispScheduleRec Schedule;

        private NoteViewControl noteView;
        private NoteParamStrip noteParamStrip;

        private ITrackViewContextUI Window;

        private Graphics contextGraphics;

        private readonly UndoHelper undoHelper;

        private Brush backBrush;
        private Pen backPen;
        private Brush foreBrush;
        private Pen forePen;
        private Brush greyBrush;
        private Pen greyPen;
        private Brush lightGreyBrush;
        private Pen lightGreyPen;
        private Brush lightLightGreyBrush;


        public TrackViewControl()
        {
            InitializeComponent();

            undoHelper = new UndoHelper(this);

            Schedule = new TrackDispScheduleRec(this, this);
            Schedule.TrackExtentsChanged += new EventHandler(Schedule_TrackExtentsChanged);

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            Disposed += new EventHandler(TrackViewControl_Disposed);
        }

        public void SetTrackObject(TrackObjectRec trackObject)
        {
            if (this.trackObject != null)
            {
                throw new InvalidOperationException();
            }
            this.trackObject = trackObject;

            noteParamStrip.InlineParamVis = trackObject.InlineParamVis;

            Init();
        }

        // Hack for now - controls can't take arguments in constructor. Ideally the primary track object would be passed in
        // as a constructor argument and set to a read-only member, since it never makes sense to change it. Instead, we
        // must set it after construction via SetTrackObject() which then calls Init() to finish construction.
        private bool initialized;
        public void Init()
        {
            if (initialized)
            {
                throw new InvalidOperationException();
            }
            initialized = true;

            RebuildSchedule();
            // TODO: connect as dependent views to both trackObject and all background objects

            RecalcTrackExtent(); // AutoScrollMinSize

            RebuildInlineStrip();

            trackObject.PropertyChanged += new PropertyChangedEventHandler(trackObject_PropertyChanged);
            trackObject.FrameArray.ListChanged += new ListChangedEventHandler(FrameArray_ListChanged);
            trackObject.FrameArrayChanged += new PropertyChangedEventHandler(TrackObject_FrameArrayChanged);
            trackObject.BackgroundObjects.ListChanged += new ListChangedEventHandler(BackgroundObjects_ListChanged);
        }

        private void TrackViewControl_Disposed(object sender, EventArgs e)
        {
            // remove dependent view connections

            if (trackObject != null)
            {
                trackObject.PropertyChanged -= new PropertyChangedEventHandler(trackObject_PropertyChanged);
                trackObject.FrameArray.ListChanged -= new ListChangedEventHandler(FrameArray_ListChanged);
                trackObject.FrameArrayChanged -= new PropertyChangedEventHandler(TrackObject_FrameArrayChanged);
                trackObject.BackgroundObjects.ListChanged -= new ListChangedEventHandler(BackgroundObjects_ListChanged);
            }

            undoHelper.Dispose();

            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;

            ClearGraphicsObjects();
        }

        private void trackObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TrackObjectAltered(trackObject, 0);
        }

        private void FrameArray_ListChanged(object sender, ListChangedEventArgs e)
        {
            TrackObjectAltered(trackObject, 0);

            // TODO: this occurs for every note in the track when undo/redo is invoked
            if (trackObject.InlineParamVis != InlineParamVis.None)
            {
                RebuildInlineStrip();
            }
        }

        private void TrackObject_FrameArrayChanged(object sender, PropertyChangedEventArgs e)
        {
            if (noteView.Note != null)
            {
                noteView.Invalidate();
            }
            if (trackObject.InlineParamVis != InlineParamVis.None)
            {
                RebuildInlineStrip();
            }

            // The event does not convey which note, but it's most likely this one
            if (TrackViewIsASingleCommandSelected() || TrackViewIsASingleNoteSelected())
            {
                TrackViewInvalidateNote(SelectedNote);
            }
        }

        private void RebuildSchedule()
        {
            Schedule = new TrackDispScheduleRec(this, this);
            Schedule.TrackExtentsChanged += new EventHandler(Schedule_TrackExtentsChanged);
            Schedule.Add(trackObject);
            foreach (TrackObjectRec trackObjectBackground in trackObject.BackgroundObjects)
            {
                Schedule.Add(trackObjectBackground);
            }
        }

        private void RecalcTrackExtent()
        {
            int totalWidth;
            using (GraphicsContext gc = new GraphicsContext(this))
            {
                totalWidth = Schedule.TotalWidth;
            }
            const int AutoScrollMinSizeHorizontalExtra = 50;
            AutoScrollMinSize = new Size(totalWidth + AutoScrollMinSizeHorizontalExtra, StaffCalibration.MaxVerticalSize);
        }

        private void Schedule_TrackExtentsChanged(object sender, EventArgs e)
        {
            RecalcTrackExtent(); // AutoScrollMinSize
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public NoteViewControl NoteView { set { noteView = value; } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public NoteParamStrip NoteParamStrip
        {
            set
            {
                noteParamStrip = value;
                noteParamStrip.ValueChanged += delegate (object sender, EventArgs e)
                {
                    using (GraphicsContext gc = new GraphicsContext(this))
                    {
                        TrackObjectAltered(trackObject, 0);
                        TrackViewRedrawAll();
                        RebuildInlineStrip();
                    }
                };
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ITrackViewContextUI ContextUI { set { Window = value; } }


        // appearance properties

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color GreyColor { get { return BlendColor(SystemColors.Window, 1, SystemColors.ControlText, 1); } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color LightGreyColor { get { return BlendColor(SystemColors.Window, 3, SystemColors.ControlText, 1); } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color LightLightGreyColor { get { return BlendColor(SystemColors.Window, 7, SystemColors.ControlText, 1); } }

        private Color BlendColor(Color color1, int parts1, Color color2, int parts2)
        {
            Color blendColor = Color.FromArgb(
                (color1.R * parts1 + color2.R * parts2) / (parts1 + parts2),
                (color1.G * parts1 + color2.G * parts2) / (parts1 + parts2),
                (color1.B * parts1 + color2.B * parts2) / (parts1 + parts2));
            return blendColor;
        }

        private void ClearGraphicsObjects()
        {
            if (backBrush != null)
            {
                backBrush.Dispose();
                backBrush = null;
            }
            if (backPen != null)
            {
                backPen.Dispose();
                backPen = null;
            }
            if (foreBrush != null)
            {
                foreBrush.Dispose();
                foreBrush = null;
            }
            if (forePen != null)
            {
                forePen.Dispose();
                forePen = null;
            }
            if (greyBrush != null)
            {
                greyBrush.Dispose();
                greyBrush = null;
            }
            if (greyPen != null)
            {
                greyPen.Dispose();
                greyPen = null;
            }
            if (lightGreyBrush != null)
            {
                lightGreyBrush.Dispose();
                lightGreyBrush = null;
            }
            if (lightGreyPen != null)
            {
                lightGreyPen.Dispose();
                lightGreyPen = null;
            }
            if (lightLightGreyBrush != null)
            {
                lightLightGreyBrush.Dispose();
                lightLightGreyBrush = null;
            }
        }


        // events and binding

        private bool scrollPositionRestored;
        public void RestoreSavedScrollPosition(int h, int v)
        {
            if ((h < 0) || (v < 0))
            {
                return;
            }

            scrollPositionRestored = true;
            HorizontalScroll.Value = h;
            VerticalScroll.Value = v;
        }

        public void SetDefaultScrollPosition()
        {
            if (!scrollPositionRestored)
            {
                VerticalScroll.Value = (StaffCalibration.MaxVerticalSize - Height) / 2;
            }
        }

        private void BackgroundObjects_ListChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    Schedule.Add(((BindingList<TrackObjectRec>)sender)[e.NewIndex]);
#if false // TODO:
		MyBoolean							Result;
		long									FrameIndex;
		MyBoolean							OnAFrameFlag;

		(void)TrackDisplayPixelToIndex(View->Schedule, View->TrackObj, View->PixelIndent,
			&OnAFrameFlag, &FrameIndex);
		Result = AddTrackToDisplaySchedule(View->Schedule, TrackObj);
		if (FrameIndex < TrackObjectGetNumFrames(View->TrackObj))
			{
				(void)TrackDisplayIndexToPixel(View->Schedule, 0, FrameIndex, &(View->PixelIndent));
			}
		 else
			{
				View->PixelIndent = 0;
			}
#endif
                    break;
                case ListChangedType.ItemDeleted:
                    // event does not provide which item was deleted, so rebuild from scratch
                    RebuildSchedule();
#if false // TODO:
		long									FrameIndex;
		MyBoolean							OnAFrameFlag;

		(void)TrackDisplayPixelToIndex(View->Schedule, View->TrackObj, View->PixelIndent,
			&OnAFrameFlag, &FrameIndex);
		DeleteTrackFromDisplaySchedule(View->Schedule, TrackObj);
		if (FrameIndex < TrackObjectGetNumFrames(View->TrackObj))
			{
				(void)TrackDisplayIndexToPixel(View->Schedule, 0, FrameIndex, &(View->PixelIndent));
			}
		 else
			{
				View->PixelIndent = 0;
			}
#endif
                    break;
            }
            Invalidate();
        }


        // focus

        protected override void OnLostFocus(EventArgs e)
        {
            using (GraphicsContext gc = new GraphicsContext(this))
            {
                TrackViewUndrawCursorBar();
            }

            base.OnLostFocus(e);
        }


        // keyboard and mouse handling

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.OemPeriod | Keys.Control)) /* show selection */
            {
                if (TrackViewIsARangeSelected())
                {
                    // swap active end
                    TrackViewRangeSetStartActiveFlag(!TrackViewIsRangeStartActive());
                    Invalidate();
                }
                TrackViewShowSelection();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                default:
                    break;

                case Keys.Escape:

                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:

                case Keys.End:
                case Keys.Home:

                    return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled)
            {
                return;
            }

            using (GraphicsContext gc = new GraphicsContext(this))
            {
                switch (e.KeyCode)
                {
                    case Keys.Left:
                    case Keys.Home:
                        if ((e.KeyData & Keys.Shift) != 0)
                        {
                            int Pivot = 0;
                            int NewActive = 0;

                            if (TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected())
                            {
                                Pivot = TrackViewGetSingleNoteSelectionFrameNumber();
                                /* increment so that frame is included in new selection */
                                Pivot += 1;
                                Debug.Assert(Pivot <= trackObject.FrameArray.Count);
                                NewActive = Pivot;
                            }
                            else if (TrackViewIsThereInsertionPoint())
                            {
                                Pivot = TrackViewGetInsertionPointIndex();
                                NewActive = Pivot;
                            }
                            else if (TrackViewIsARangeSelected())
                            {
                                Pivot = TrackViewGetRangeStart();
                                NewActive = TrackViewGetRangeEndPlusOne();
                                if (TrackViewIsRangeStartActive())
                                {
                                    int Temp = Pivot;
                                    Pivot = NewActive;
                                    NewActive = Temp;
                                }
                            }
                            if (((e.KeyData & Keys.Control) != 0) || (e.KeyCode == Keys.Home))
                            {
                                NewActive = 0;
                            }
                            else
                            {
                                if (NewActive > 0)
                                {
                                    NewActive -= 1;
                                }
                            }
                            TrackViewSetRangeSelection(
                                Pivot,
                                NewActive,
                                false/*end is active*/);
                            TrackViewShowSelection();
                        }
                        else if (((e.KeyData & Keys.Control) == 0)
                            || ((e.KeyData & (Keys.Control | Keys.Alt)) == (Keys.Control | Keys.Alt)))
                        {
                            if (e.KeyCode == Keys.Home)
                            {
                                TrackViewSetInsertionPoint(0);
                            }
                            else if (TrackViewIsARangeSelected())
                            {
                                /* collapse to ip to active end */
                                TrackViewSetInsertionPoint(
                                    TrackViewIsRangeStartActive()
                                        ? TrackViewGetRangeStart()
                                        : TrackViewGetRangeEndPlusOne());
                            }
                            else
                            {
                                if (TrackViewIsThereInsertionPoint())
                                {
                                    if (TrackViewGetInsertionPointIndex() > 0)
                                    {
                                        TrackViewTrySingleNoteSelectionIndexed(
                                            TrackViewGetInsertionPointIndex() - 1,
                                            trackObject.FrameArray[TrackViewGetInsertionPointIndex() - 1].Count - 1);
                                    }
                                }
                                else if (TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected())
                                {
                                    if (TrackViewGetSingleNoteSelectionNoteNumberInFrame() > 0)
                                    {
                                        TrackViewTrySingleNoteSelectionIndexed(
                                            TrackViewGetSingleNoteSelectionFrameNumber(),
                                            TrackViewGetSingleNoteSelectionNoteNumberInFrame() - 1);
                                    }
                                    else
                                    {
                                        if ((TrackViewGetSingleNoteSelectionFrameNumber() == 0)
                                            || !((e.KeyData & (Keys.Control | Keys.Alt)) == (Keys.Control | Keys.Alt)))
                                        {
                                            TrackViewSetInsertionPoint(
                                                TrackViewGetSingleNoteSelectionFrameNumber());
                                        }
                                        else
                                        {
                                            TrackViewTrySingleNoteSelectionIndexed(
                                                TrackViewGetSingleNoteSelectionFrameNumber() - 1,
                                                trackObject.FrameArray[TrackViewGetSingleNoteSelectionFrameNumber() - 1].Count - 1);
                                        }
                                    }
                                }
                                if (((e.KeyData & Keys.Alt) != 0) && ((e.KeyData & Keys.Control) == 0))
                                {
                                    if (TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected())
                                    {
                                        TrackViewSetInsertionPoint(
                                            TrackViewGetSingleNoteSelectionFrameNumber());
                                    }
                                }
                            }
                            TrackViewShowSelection();
                        }
                        else
                        {
                            if (e.KeyCode == Keys.Home)
                            {
                                AutoScrollPosition = new Point(0, -AutoScrollPosition.Y);
                            }
                            else
                            {
                                AutoScrollPosition = new Point(-AutoScrollPosition.X - ClientSize.Width / 8, -AutoScrollPosition.Y);
                            }
                        }
                        e.Handled = true;
                        return;

                    case Keys.Right:
                    case Keys.End:
                        if ((e.KeyData & Keys.Shift) != 0)
                        {
                            int Pivot = 0;
                            int NewActive = 0;

                            if (TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected())
                            {
                                Pivot = TrackViewGetSingleNoteSelectionFrameNumber();
                                NewActive = Pivot;
                            }
                            else if (TrackViewIsThereInsertionPoint())
                            {
                                Pivot = TrackViewGetInsertionPointIndex();
                                NewActive = Pivot;
                            }
                            else if (TrackViewIsARangeSelected())
                            {
                                Pivot = TrackViewGetRangeStart();
                                NewActive = TrackViewGetRangeEndPlusOne();
                                if (TrackViewIsRangeStartActive())
                                {
                                    int Temp = Pivot;
                                    Pivot = NewActive;
                                    NewActive = Temp;
                                }
                            }
                            if (((e.KeyData & Keys.Control) != 0) || (e.KeyCode == Keys.End))
                            {
                                NewActive = trackObject.FrameArray.Count;
                            }
                            else
                            {
                                if (NewActive + 1 <= trackObject.FrameArray.Count)
                                {
                                    NewActive = NewActive + 1;
                                }
                            }
                            TrackViewSetRangeSelection(
                                Pivot,
                                NewActive,
                                false/*end is active*/);
                            TrackViewShowSelection();
                        }
                        else if (((e.KeyData & Keys.Control) == 0)
                            || ((e.KeyData & (Keys.Control | Keys.Alt)) == (Keys.Control | Keys.Alt)))
                        {
                            if (e.KeyCode == Keys.End)
                            {
                                TrackViewSetInsertionPoint(trackObject.FrameArray.Count);
                            }
                            else if (TrackViewIsARangeSelected())
                            {
                                /* collapse to ip to active end */
                                TrackViewSetInsertionPoint(
                                    TrackViewIsRangeStartActive()
                                        ? TrackViewGetRangeStart()
                                        : TrackViewGetRangeEndPlusOne());
                            }
                            else
                            {
                                if (TrackViewIsThereInsertionPoint())
                                {
                                    if (TrackViewGetInsertionPointIndex() < trackObject.FrameArray.Count)
                                    {
                                        TrackViewTrySingleNoteSelectionIndexed(
                                            TrackViewGetInsertionPointIndex(),
                                            0);
                                    }
                                }
                                else if (TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected())
                                {
                                    if (TrackViewGetSingleNoteSelectionNoteNumberInFrame()
                                        < trackObject.FrameArray[TrackViewGetSingleNoteSelectionFrameNumber()].Count - 1)
                                    {
                                        TrackViewTrySingleNoteSelectionIndexed(
                                            TrackViewGetSingleNoteSelectionFrameNumber(),
                                            TrackViewGetSingleNoteSelectionNoteNumberInFrame() + 1);
                                    }
                                    else
                                    {
                                        if ((TrackViewGetSingleNoteSelectionFrameNumber() == trackObject.FrameArray.Count - 1)
                                            || !((e.KeyData & (Keys.Control | Keys.Alt)) == (Keys.Control | Keys.Alt)))
                                        {
                                            Debug.Assert(TrackViewGetSingleNoteSelectionFrameNumber() + 1
                                                <= trackObject.FrameArray.Count);
                                            TrackViewSetInsertionPoint(
                                                TrackViewGetSingleNoteSelectionFrameNumber() + 1);
                                        }
                                        else
                                        {
                                            TrackViewTrySingleNoteSelectionIndexed(
                                                TrackViewGetSingleNoteSelectionFrameNumber() + 1,
                                                0);
                                        }
                                    }
                                }
                                if (((e.KeyData & Keys.Alt) != 0) && ((e.KeyData & Keys.Control) == 0))
                                {
                                    if (TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected())
                                    {
                                        TrackViewSetInsertionPoint(
                                            TrackViewGetSingleNoteSelectionFrameNumber() + 1);
                                    }
                                }
                            }
                            TrackViewShowSelection();
                        }
                        else
                        {
                            if (e.KeyCode == Keys.End)
                            {
                                AutoScrollPosition = new Point(AutoScrollMinSize.Width, -AutoScrollPosition.Y);
                            }
                            else
                            {
                                AutoScrollPosition = new Point(-AutoScrollPosition.X + ClientSize.Width / 8, -AutoScrollPosition.Y);
                            }
                        }
                        e.Handled = true;
                        return;

                    case Keys.Up:
                        if ((e.KeyData & Keys.Control) == 0)
                        {
                            if (Window.NoteReady)
                            {
                                if (!TrackViewIsCursorBarVisible())
                                {
                                    Point p = PointToClient(Cursor.Position);
                                    int X = p.X;
                                    int Y = p.Y;

                                    TrackViewSetCursorBar(
                                        X,
                                        Y,
                                        true);
                                }
                                else
                                {
                                    TrackViewNudgeCursorBar(((e.KeyData & Keys.Alt) == 0) ? 1 : 7);
                                }
                            }
                        }
                        else
                        {
                            AutoScrollPosition = new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y - ClientSize.Height / 16);
                        }
                        e.Handled = true;
                        return;

                    case Keys.Down:
                        if ((e.KeyData & Keys.Control) == 0)
                        {
                            if (Window.NoteReady)
                            {
                                if (!TrackViewIsCursorBarVisible())
                                {
                                    Point p = PointToClient(Cursor.Position);
                                    int X = p.X;
                                    int Y = p.Y;

                                    TrackViewSetCursorBar(
                                        X,
                                        Y,
                                        true);
                                }
                                else
                                {
                                    TrackViewNudgeCursorBar(((e.KeyData & Keys.Alt) == 0) ? -1 : -7);
                                }
                            }
                        }
                        else
                        {
                            AutoScrollPosition = new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y + ClientSize.Height / 16);
                        }
                        e.Handled = true;
                        return;

                    case Keys.OemQuestion: // '/' == 191
                        Window.NoteReady = true;
                        Window.NoteState = NoteFlags.e4thNote | NoteFlags.eDiv1Modifier;
                        e.Handled = true;
                        return;
                    case Keys.Escape:
                        // first escape clears selection, subsequent one resets command to "arrow"
                        if (SelectionMode != SelectionModes.eTrackViewNoSelection)
                        {
                            if (TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected())
                            {
                                int noteIndex;
                                if (!trackObject.FindNote(SelectedNote, out InsertionPointIndex, out noteIndex))
                                {
                                    Debug.Assert(false); // shouldn't happen
                                    InsertionPointIndex = 0;
                                }
                            }
                            SelectionMode = SelectionModes.eTrackViewNoSelection;
                            TrackViewRedrawAll();
                            e.Handled = true;
                            return;
                        }
                        goto ResetCommandBar; // fallthrough
                    case Keys.A:
                        ResetCommandBar:
                        Window.NoteReady = false;
                        //Window.NoteState = NoteFlags.e4thNote | NoteFlags.eDiv1Modifier; /* reset the note */
                        TrackViewUndrawCursorBar();
                        e.Handled = true;
                        return;
                    case Keys.Z:
                        // what did this do?
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        e.Handled = true;
                        return;
                    case Keys.X:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDurationMask) | NoteFlags.e64thNote;
                        e.Handled = true;
                        return;
                    case Keys.T:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDurationMask) | NoteFlags.e32ndNote;
                        e.Handled = true;
                        return;
                    case Keys.S:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDurationMask) | NoteFlags.e16thNote;
                        e.Handled = true;
                        return;
                    case Keys.E:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDurationMask) | NoteFlags.e8thNote;
                        e.Handled = true;
                        return;
                    case Keys.Q:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDurationMask) | NoteFlags.e4thNote;
                        e.Handled = true;
                        return;
                    case Keys.H:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDurationMask) | NoteFlags.e2ndNote;
                        e.Handled = true;
                        return;
                    case Keys.W:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDurationMask) | NoteFlags.eWholeNote;
                        e.Handled = true;
                        return;
                    case Keys.D:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDurationMask) | NoteFlags.eDoubleNote;
                        e.Handled = true;
                        return;
                    case Keys.F:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDurationMask) | NoteFlags.eQuadNote;
                        e.Handled = true;
                        return;
                    case Keys.Oemplus: // '=' or '+'
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eFlatModifier) | NoteFlags.eSharpModifier;
                        e.Handled = true;
                        return;
                    case Keys.OemMinus: // '-' or '_'
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eSharpModifier) | NoteFlags.eFlatModifier;
                        e.Handled = true;
                        return;
                    case Keys.D0: // '0'
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = Window.NoteState & ~(NoteFlags.eSharpModifier | NoteFlags.eFlatModifier);
                        e.Handled = true;
                        return;
                    case Keys.N:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = Window.NoteState & ~NoteFlags.eRestModifier;
                        e.Handled = true;
                        return;
                    case Keys.R:
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = Window.NoteState | NoteFlags.eRestModifier;
                        e.Handled = true;
                        return;
                    case Keys.Oemcomma: // ','
                        if ((e.KeyData & Keys.Shift) == 0)
                        {
                            Window.NoteReady = true;
                            Window.NoteState &= ~NoteFlags.eCommandFlag;
                            Window.NoteState = Window.NoteState & ~NoteFlags.eDotModifier;
                        }
                        else // '<'
                        {
                            /* transpose note down 1 step */
                            if ((TrackViewIsARangeSelected()) || TrackViewIsASingleNoteSelected())
                            {
                                TrackViewTransposeSelection(-1);
                            }
                        }
                        e.Handled = true;
                        return;
                    case Keys.OemPeriod: // '.'
                        if ((e.KeyData & Keys.Shift) == 0)
                        {
                            Window.NoteReady = true;
                            Window.NoteState &= ~NoteFlags.eCommandFlag;
                            Window.NoteState = Window.NoteState | NoteFlags.eDotModifier;
                        }
                        else // '>'
                        {
                            /* transpose note up 1 step */
                            if ((TrackViewIsARangeSelected()) || TrackViewIsASingleNoteSelected())
                            {
                                TrackViewTransposeSelection(1);
                            }
                        }
                        e.Handled = true;
                        return;
                    case Keys.D1: // '1'
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDivisionMask) | NoteFlags.eDiv1Modifier;
                        e.Handled = true;
                        return;
                    case Keys.D3: // '3'
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDivisionMask) | NoteFlags.eDiv3Modifier;
                        e.Handled = true;
                        return;
                    case Keys.D5: // '5'
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDivisionMask) | NoteFlags.eDiv5Modifier;
                        e.Handled = true;
                        return;
                    case Keys.D7: // '7'
                        Window.NoteReady = true;
                        Window.NoteState &= ~NoteFlags.eCommandFlag;
                        Window.NoteState = (Window.NoteState & ~NoteFlags.eDivisionMask) | NoteFlags.eDiv7Modifier;
                        e.Handled = true;
                        return;
                    case Keys.C:
                        Window.NoteReady = true;
                        Window.NoteState |= NoteFlags.eCommandFlag;
                        e.Handled = true;
                        return;

                    case Keys.Delete:
                    case Keys.Back:
                        /* the delete key deletes selected stuff */
                        if (TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected())
                        {
                            TrackViewDeleteSingleNoteOrCommand();
                        }
                        else if (TrackViewIsARangeSelected())
                        {
                            TrackViewDeleteRangeSelection();
                        }
                        e.Handled = true;
                        return;
                    case Keys.Return: /* carriage return brings up attribute dialog for the selected thing */
                        if (TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected())
                        {
                            TrackViewEditSingleSelectionAttributes();
                        }
                        e.Handled = true;
                        return;
                    case Keys.Space: /* insert note or command */
                        if ((TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected()
                            || TrackViewIsThereInsertionPoint()) && Window.NoteReady)
                        {
                            Point p = PointToClient(Cursor.Position);
                            int X = p.X;
                            int Y = p.Y;

                            TrackWindowAddNoteOrCommand(
                                X,
                                Y,
                                true);
                        }
                        e.Handled = true;
                        return;
                }
            }
        }

        // update NoteView and phantom selection visuals
        private void UpdateMouseOverEffects(Graphics graphics, bool mouseInside, int x, int y)
        {
            if (noteView != null)
            {
                NoteObjectRec oldNote = noteView.Note;

                NoteNoteObjectRec Note;
                if (mouseInside && ((Note = TrackViewGetMouseOverNote(graphics, x, y) as NoteNoteObjectRec) != null))
                {
                    TrackViewChangePhantomInsertionPoint(-1);
                    noteView.Note = Note;
                }
                else
                {
                    if (mouseInside)
                    {
                        bool onFrame;
                        int newPhantomInsertionPointIndex;
                        Schedule.TrackDisplayPixelToIndex(trackObject, x + PixelIndent, out onFrame, out newPhantomInsertionPointIndex);
                        TrackViewChangePhantomInsertionPoint(newPhantomInsertionPointIndex);
                    }
                    else
                    {
                        TrackViewChangePhantomInsertionPoint(-1);
                    }

                    if (TrackViewIsASingleNoteSelected())
                    {
                        noteView.Note = (NoteNoteObjectRec)SelectedNote;
                    }
                    else
                    {
                        noteView.Note = null;
                    }
                }

                if (oldNote != noteView.Note)
                {
                    if (oldNote != null)
                    {
                        TrackViewRedrawNote(oldNote);
                    }
                    if (noteView.Note != null)
                    {
                        TrackViewRedrawNote(noteView.Note);
                    }
                }
            }
        }

        private event MouseEventHandler MouseMoveCapture;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            using (GraphicsContext gc = new GraphicsContext(this))
            {
                if (MouseMoveCapture != null)
                {
                    MouseMoveCapture.Invoke(this, e);
                    return;
                }


                // uncaptured mouse move behavior

                if (!Focused)
                {
                    return;
                }

                bool NoteReady = Window.NoteReady;
                TrackViewUpdateMouseCursor(e.X, e.Y, NoteReady);

                /* draw info for this note */
                UpdateMouseOverEffects(gc.graphics, true/*mouseInside*/, e.X, e.Y);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            using (GraphicsContext gc = new GraphicsContext(this))
            {
                UpdateMouseOverEffects(gc.graphics, false/*mouseInside*/, 0, 0);
            }
        }

        private void timerUpdateMouseOverEffect_Tick(object sender, EventArgs e)
        {
            timerUpdateMouseOverEffect.Stop();

            Point localMousePosition = PointToClient(MousePosition);
            OnMouseMove(new MouseEventArgs(MouseButtons, 0, localMousePosition.X, localMousePosition.Y, 0));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            using (GraphicsContext gc = new GraphicsContext(this))
            {
                if ((ModifierKeys & Keys.Control) != 0)
                {
                    TrackViewTrySingleNoteSelection(e.X, e.Y);
                    return;
                }
                else if (((ModifierKeys & Keys.Control) != 0) || ((ModifierKeys & Keys.Alt) != 0))
                {
                    if (TrackViewIsASingleNoteSelected())
                    {
                        TrackViewSetTieOnNote(e.X, e.Y);
                    }
                }
                else
                {
                    if (Window.NoteReady && ((ModifierKeys & Keys.Shift) == 0))
                    {
                        /* make sure note gets inserted at current mouse position */
                        /* rather than previous one from the last idle event. */
                        TrackViewUpdateMouseCursor(e.X, e.Y, Window.NoteReady);
                        TrackWindowAddNoteOrCommand(e.X, e.Y, false);
                    }
                    else
                    {
                        bool ExtendSelection = (ModifierKeys & Keys.Shift) != 0;
                        if (!ExtendSelection && (ModifierKeys == 0) && (TrackViewGetMouseOverNote(gc.graphics, e.X, e.Y) != null))
                        {
                            // in 'arrow' mode, allow click to select single note without having control key pressed
                            TrackViewTrySingleNoteSelection(e.X, e.Y);
                        }
                        else
                        {
                            TrackViewDoBlockSelection(e.X, e.Y, ExtendSelection);
                        }
                    }
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            MouseMoveCapture = null;
        }

        public bool TrackViewIsThereInsertionPoint()
        {
            return SelectionMode == SelectionModes.eTrackViewNoSelection;
        }

        public int TrackViewGetInsertionPointIndex()
        {
            Debug.Assert(SelectionMode == SelectionModes.eTrackViewNoSelection);
            Debug.Assert(InsertionPointIndex <= trackObject.FrameArray.Count);
            return InsertionPointIndex;
        }

        public bool TrackViewIsASingleNoteSelected()
        {
            return SelectionMode == SelectionModes.eTrackViewSingleNoteSelection;
        }

        public bool TrackViewIsASingleCommandSelected()
        {
            return SelectionMode == SelectionModes.eTrackViewSingleCommandSelection;
        }

        public int TrackViewGetSingleNoteSelectionFrameNumber()
        {
            Debug.Assert(TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected());
            return SelectedNoteFrame;
        }

        public int TrackViewGetSingleNoteSelectionNoteNumberInFrame()
        {
            Debug.Assert(TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected());
            int i = trackObject.FrameArray[SelectedNoteFrame].IndexOf(SelectedNote);
            Debug.Assert(i >= 0);
            return i;
        }

        public bool TrackViewIsARangeSelected()
        {
            return SelectionMode == SelectionModes.eTrackViewRangeSelection;
        }

        public int TrackViewGetRangeStart()
        {
            Debug.Assert(SelectionMode == SelectionModes.eTrackViewRangeSelection);
            return RangeSelectStart;
        }

        public int TrackViewGetRangeEndPlusOne()
        {
            Debug.Assert(SelectionMode == SelectionModes.eTrackViewRangeSelection);
            return RangeSelectEnd;
        }

        public bool TrackViewIsRangeStartActive()
        {
            Debug.Assert(SelectionMode == SelectionModes.eTrackViewRangeSelection);
            return RangeSelectStartIsActive;
        }

        public void TrackViewRangeSetStartActiveFlag(bool RangeStartIsActive)
        {
            Debug.Assert(SelectionMode == SelectionModes.eTrackViewRangeSelection);
            RangeSelectStartIsActive = RangeStartIsActive;
        }

        public bool TrackViewIsCursorBarVisible()
        {
            return CursorBarIsVisible;
        }

        /* if cursor bar exists, nudge it by the specified number of steps (positive */
        /* number is increasing pitch) */
        public void TrackViewNudgeCursorBar(int Steps)
        {
            if (CursorBarIsVisible)
            {
                CursorBarIsLocked = true;

                TrackViewUndrawCursorBar();

                CursorBarLoc += Steps * (StaffCalibration.ConvertPitchToPixel(2/*D*/, 0)
                    - StaffCalibration.ConvertPitchToPixel(0/*C*/, 0));

                /* normalize and constrain */
                CursorBarLoc = StaffCalibration.ConvertPitchToPixel(StaffCalibration.ConvertPixelToPitch(CursorBarLoc), 0);

                TrackViewDrawCursorBar();
            }
        }

        /* get the cursor bar pitch.  returns False if it doesn't exist */
        public bool TrackViewGetCursorBarPitch(out short Pitch)
        {
            Pitch = 0;
            if (!CursorBarIsVisible)
            {
                return false;
            }
            Pitch = StaffCalibration.ConvertPixelToPitch(CursorBarLoc);
            return true;
        }

        private void TrackWindowAddNoteOrCommand(int XLoc, int YLoc, bool AddAtInsertionPoint)
        {
            undoHelper.SaveUndoInfo(false/*forRedo*/, (Window.NoteState & NoteFlags.eCommandFlag) == 0 ? "Insert Note" : "Insert Command");

            if ((Window.NoteState & NoteFlags.eCommandFlag) == 0)
            {
                /* adding a note to the track */
                if ((Window.NoteState & NoteFlags.eDivisionMask) != NoteFlags.eDiv1Modifier)
                {
                    NoteFlags State = Window.NoteState;
                    switch ((NoteFlags)(State & NoteFlags.eDurationMask))
                    {
                        default:
                            // duration problem
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case NoteFlags.e64thNote:
                            State = (State & ~NoteFlags.eDurationMask) | NoteFlags.e32ndNote;
                            TrackViewAddNote(XLoc, YLoc, State, AddAtInsertionPoint);
                            break;
                        case NoteFlags.e32ndNote:
                            State = (State & ~NoteFlags.eDurationMask) | NoteFlags.e16thNote;
                            TrackViewAddNote(XLoc, YLoc, State, AddAtInsertionPoint);
                            break;
                        case NoteFlags.e16thNote:
                            State = (State & ~NoteFlags.eDurationMask) | NoteFlags.e8thNote;
                            TrackViewAddNote(XLoc, YLoc, State, AddAtInsertionPoint);
                            break;
                        case NoteFlags.e8thNote:
                            State = (State & ~NoteFlags.eDurationMask) | NoteFlags.e4thNote;
                            TrackViewAddNote(XLoc, YLoc, State, AddAtInsertionPoint);
                            break;
                        case NoteFlags.e4thNote:
                            State = (State & ~NoteFlags.eDurationMask) | NoteFlags.e2ndNote;
                            TrackViewAddNote(XLoc, YLoc, State, AddAtInsertionPoint);
                            break;
                        case NoteFlags.e2ndNote:
                            State = (State & ~NoteFlags.eDurationMask) | NoteFlags.eWholeNote;
                            TrackViewAddNote(XLoc, YLoc, State, AddAtInsertionPoint);
                            break;
                        case NoteFlags.eWholeNote:
                            State = (State & ~NoteFlags.eDurationMask) | NoteFlags.eDoubleNote;
                            TrackViewAddNote(XLoc, YLoc, State, AddAtInsertionPoint);
                            break;
                        case NoteFlags.eDoubleNote:
                            State = (State & ~NoteFlags.eDurationMask) | NoteFlags.eQuadNote;
                            TrackViewAddNote(XLoc, YLoc, State, AddAtInsertionPoint);
                            break;
                        case NoteFlags.eQuadNote:
                            break;
                    }
                }
                else
                {
                    TrackViewAddNote(XLoc, YLoc, Window.NoteState, AddAtInsertionPoint);
                }
            }
            else
            {
                /* adding a command to the track */
                NoteCommands TheCommandTheyWant;
                if (CommandChooser.ChooseCommandFromList(out TheCommandTheyWant))
                {
                    TrackViewAddCommand(
                        XLoc,
                        YLoc,
                        TheCommandTheyWant,
                        AddAtInsertionPoint);
                }
            }
        }

        /* insert a note at the specified mouse coordinate with the attributes */
        private void TrackViewAddNote(int X, int Y, NoteFlags NoteAttributes, bool AddAtInsertionPoint)
        {
            if ((NoteAttributes & NoteFlags.eCommandFlag) != 0)
            {
                // TrackViewAddNote being used to insert command into score
                Debug.Assert(false);
                throw new ArgumentException();
            }

            /* figure out where we are supposed to put this note. */
            bool AddToFrame;
            int Index;
            if (AddAtInsertionPoint)
            {
                switch (SelectionMode)
                {
                    default:
                        return;
                    case SelectionModes.eTrackViewSingleCommandSelection:
                        AddToFrame = false;
                        Index = SelectedNoteFrame;
                        break;
                    case SelectionModes.eTrackViewSingleNoteSelection:
                        AddToFrame = true;
                        Index = SelectedNoteFrame;
                        break;
                    case SelectionModes.eTrackViewNoSelection:
                        AddToFrame = false;
                        Index = InsertionPointIndex;
                        break;
                }
            }
            else
            {
                Schedule.TrackDisplayPixelToIndex(
                    trackObject,
                    X + PixelIndent,
                    out AddToFrame,
                    out Index);
            }
            /* construct the note to be added */
            NoteNoteObjectRec Note = new NoteNoteObjectRec(trackObject);
            NoteFlags NoteSharpFlatTemp;
            short NotePitchTemp;
            StaffCalibration.SetUpNoteInfo(
                out NotePitchTemp,
                out NoteSharpFlatTemp,
                (NoteAttributes & NoteFlags.eSharpModifier) != 0,
                (NoteAttributes & NoteFlags.eFlatModifier) != 0,
                CursorBarIsVisible ? CursorBarLoc : (Y + VerticalOffset));
            Note.PutNotePitch(NotePitchTemp);
            Note.PutNoteDuration(NoteAttributes & NoteFlags.eDurationMask);
            Note.PutNoteDurationDivision(NoteAttributes & NoteFlags.eDivisionMask);
            Note.PutNoteDotStatus((NoteAttributes & NoteFlags.eDotModifier) != 0);
            Note.PutNoteFlatOrSharpStatus(NoteSharpFlatTemp);
            Note.PutNoteIsItARest((NoteAttributes & NoteFlags.eRestModifier) != 0);
            if ((NoteAttributes & ~(NoteFlags.eSharpModifier | NoteFlags.eFlatModifier | NoteFlags.eDurationMask
                | NoteFlags.eDivisionMask | NoteFlags.eDotModifier | NoteFlags.eRestModifier)) != 0)
            {
                // some unknown bits in the note attributes word are set
                Debug.Assert(false);
                throw new ArgumentException();
            }
            /* add note to the appropriate place */
            if (AddToFrame)
            {
                /* add to existing frame */
                FrameObjectRec Frame = trackObject.FrameArray[Index];
                if (Frame.IsThisACommandFrame)
                {
                    /* if it's a command frame, then insert note in new frame after it */
                    Index += 1;
                    Frame = new FrameObjectRec();
                    Frame.Add(Note);
                    trackObject.FrameArray.Insert(Index, Frame);
                }
                else
                {
                    Frame.Add(Note);
                    TrackObjectAltered(trackObject, Index);
                }
            }
            else
            {
                /* we hafta create a new frame */
                FrameObjectRec Frame = new FrameObjectRec();
                Frame.Add(Note);
                trackObject.FrameArray.Insert(Index, Frame);
            }
            /* adjust the insertion point */
            SelectionMode = SelectionModes.eTrackViewSingleNoteSelection;
            SelectedNote = Note;
            SelectedNoteFrame = Index;
            /* redraw what needs to be redrawn.  this definitely needs to be fixed */
            /* up since it's unnecessary to redraw the whole staff just to add a note. */
            TrackViewRedrawAll();

            if (trackObject.InlineParamVis != InlineParamVis.None)
            {
                RebuildInlineStrip();
            }
        }

        /* add a command at the specified mouse coordinates. */
        private void TrackViewAddCommand(int X, int Y, NoteCommands CommandOpcode, bool AddAtInsertionPoint)
        {
            /* figure out where we are supposed to put this note. */
            bool AddToFrame;
            int Index;
            if (AddAtInsertionPoint)
            {
                AddToFrame = false;
                switch (SelectionMode)
                {
                    default:
                        return;
                    case SelectionModes.eTrackViewSingleCommandSelection:
                    case SelectionModes.eTrackViewSingleNoteSelection:
                        Index = SelectedNoteFrame;
                        break;
                    case SelectionModes.eTrackViewNoSelection:
                        Index = InsertionPointIndex;
                        break;
                }
            }
            else
            {
                Schedule.TrackDisplayPixelToIndex(
                    trackObject,
                    X + PixelIndent,
                    out AddToFrame,
                    out Index);
            }
            /* AddToFrame is ignored for adding commands */
            /* construct the command to be added */
            CommandNoteObjectRec Command = new CommandNoteObjectRec(trackObject);
            Command.PutCommandOpcode(CommandOpcode);
            /* add command to the appropriate place */
            /* create a new frame */
            FrameObjectRec Frame = new FrameObjectRec();
            Frame.Add(Command);
            trackObject.FrameArray.Insert(Index, Frame);
            /* we don't have to notify track of change because it knows already. */
            /* adjust the insertion point */
            SelectionMode = SelectionModes.eTrackViewSingleCommandSelection;
            SelectedNote = Command;
            SelectedNoteFrame = Index;
            /* redraw what needs to be redrawn.  this definitely needs to be fixed */
            /* up since it's unnecessary to redraw the whole staff just to add a command. */
            TrackViewRedrawAll();
        }

        /* delete the selected single note or command.  it is an error if no single note */
        /* or command is selected */
        public void TrackViewDeleteSingleNoteOrCommand()
        {
            Debug.Assert((SelectionMode == SelectionModes.eTrackViewSingleNoteSelection)
                || (SelectionMode == SelectionModes.eTrackViewSingleCommandSelection));

            undoHelper.SaveUndoInfo(false/*forRedo*/, SelectionMode == SelectionModes.eTrackViewSingleNoteSelection ? "Delete Note" : "Delete Command");

            FrameObjectRec Frame = trackObject.FrameArray[SelectedNoteFrame];
            int Limit = Frame.Count;
            for (int Scan = 0; Scan < Limit; Scan += 1)
            {
                if (SelectedNote == Frame[Scan])
                {
                    /* we found the note to delete */
                    /* zap ties to this note */
                    trackObject.TrackObjectNullifyTies(SelectedNote);
                    /* indicate that stuff needs to be redrawn */
                    TrackObjectAltered(trackObject, SelectedNoteFrame);
                    /* now do the actual deletion */
                    if (Frame.Count == 1)
                    {
                        /* since this is the only note in the frame, we'll just delete the */
                        /* whole frame. */
                        trackObject.TrackObjectDeleteFrameRun(SelectedNoteFrame, 1);
                    }
                    else
                    {
                        /* there are other notes in the frame, so just delete this one note */
                        Frame.RemoveAt(Scan);
                    }
                    SelectionMode = SelectionModes.eTrackViewNoSelection;
                    InsertionPointIndex = SelectedNoteFrame;
                    Debug.Assert(InsertionPointIndex <= trackObject.FrameArray.Count);
                    SelectedNote = null;
                    SelectedNoteFrame = -1;
                    /* redrawing everything is overkill and should be fixed */
                    TrackViewRedrawAll();

                    if (trackObject.InlineParamVis != InlineParamVis.None)
                    {
                        RebuildInlineStrip();
                    }

                    return;
                }
            }
            Debug.Assert(false); // couldn't find the note
        }

        public void TrackViewSelectAll()
        {
            using (GraphicsContext gc = new GraphicsContext(this))
            {
                int NumFrames = trackObject.FrameArray.Count;
                if (NumFrames > 0)
                {
                    SelectionMode = SelectionModes.eTrackViewRangeSelection;
                    RangeSelectStart = 0;
                    RangeSelectEnd = NumFrames;
                    RangeSelectStartIsActive = false;
                }
                else
                {
                    SelectionMode = SelectionModes.eTrackViewNoSelection;
                    InsertionPointIndex = 0;
                }
                /* redraw with the selection changes */
                TrackViewRedrawAll();
            }
        }

        /* delete the selected range.  it is an error if there is no selected range. it */
        /* returns False if it fails.  undo information is automatically maintained */
        public void TrackViewDeleteRangeSelection()
        {
            Debug.Assert(SelectionMode == SelectionModes.eTrackViewRangeSelection);

            undoHelper.SaveUndoInfo(false/*forRedo*/, "Delete Range");

            /* delete the stuff */
            trackObject.TrackObjectDeleteFrameRun(
                RangeSelectStart,
                RangeSelectEnd - RangeSelectStart);
            SelectionMode = SelectionModes.eTrackViewNoSelection;
            InsertionPointIndex = RangeSelectStart;
            Debug.Assert(InsertionPointIndex <= trackObject.FrameArray.Count);

            /* redraw with changes */
            TrackViewRedrawAll();
            TrackViewShowSelection();
        }

        /* set a tie from the currently selected note to the note at the specified position. */
        /* it is an error if there is no single note selected. */
        private void TrackViewSetTieOnNote(int X, int Y)
        {
            if (SelectionMode != SelectionModes.eTrackViewSingleNoteSelection)
            {
                // single note selection is false.
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            /* figure out where we are supposed to put this note. */
            bool ActuallyOnAFrame;
            int Index;
            Schedule.TrackDisplayPixelToIndex(trackObject, X + PixelIndent, out ActuallyOnAFrame, out Index);

            undoHelper.SaveUndoInfo(false/*forRedo*/, "Edit Tie");

            /* if they didn't click on anything, then cancel the tie */
            if (!ActuallyOnAFrame)
            {
                /* delete the tie */
                Debug.Assert(SelectedNote is NoteNoteObjectRec);
                ((NoteNoteObjectRec)SelectedNote).PutNoteTieTarget(null);
                TrackObjectAltered(trackObject, SelectedNoteFrame);
                return;
            }

            /* get the destination note */
            bool CommandFlag;
            int DestinationFrameIndex;
            NoteObjectRec DestinationNote = Schedule.TrackDisplayGetUnderlyingNote(
                Schedule.TrackDisplayGetTrackIndex(trackObject),
                out CommandFlag,
                X + PixelIndent,
                out DestinationFrameIndex);

            /* change the data */
            if ((DestinationFrameIndex <= SelectedNoteFrame) || (DestinationNote == null) || DestinationNote.IsItACommand)
            {
                /* delete the tie */
                Debug.Assert(SelectedNote is NoteNoteObjectRec);
                ((NoteNoteObjectRec)SelectedNote).PutNoteTieTarget(null);
                TrackObjectAltered(trackObject, SelectedNoteFrame);
            }
            else
            {
                /* create the tie */
                /* patch the tie flag thing */
                trackObject.TrackObjectNullifyTies(DestinationNote);
                Debug.Assert(SelectedNote is NoteNoteObjectRec);
                Debug.Assert(DestinationNote is NoteNoteObjectRec);
                ((NoteNoteObjectRec)SelectedNote).PutNoteTieTarget((NoteNoteObjectRec)DestinationNote);
                TrackObjectAltered(trackObject, SelectedNoteFrame);
                /* change selection */
                SelectedNote = DestinationNote;
                SelectedNoteFrame = DestinationFrameIndex;
            }

            /* redraw */
            TrackViewRedrawAll();
        }

        private void TrackViewDoBlockSelection(int X, int Y, bool ExtendSelection)
        {
            /* get the initial click position */
            bool LandedOnAFrame;
            int BaseSelectPivot;
            int ActiveEnd;
            Schedule.TrackDisplayPixelToIndex(
                trackObject,
                X + PixelIndent,
                out LandedOnAFrame,
                out ActiveEnd);
            if ((ActiveEnd < 0) || (ActiveEnd > trackObject.FrameArray.Count))
            {
                // click frame index is beyond the bounds of the track
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            /* default to  pivot from initial click (used if not extending) */
            BaseSelectPivot = ActiveEnd;
            if (ExtendSelection)
            {
                switch (SelectionMode)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case SelectionModes.eTrackViewRangeSelection:
                        /* if range exists, pivot from opposite of active end */
                        if (!RangeSelectStartIsActive)
                        {
                            BaseSelectPivot = RangeSelectStart;
                        }
                        else
                        {
                            BaseSelectPivot = RangeSelectEnd;
                        }
                        break;
                    case SelectionModes.eTrackViewNoSelection: /* insertion point */
                        BaseSelectPivot = InsertionPointIndex;
                        break;
                    case SelectionModes.eTrackViewSingleNoteSelection:
                    case SelectionModes.eTrackViewSingleCommandSelection:
                        BaseSelectPivot = SelectedNoteFrame;
                        if (BaseSelectPivot >= ActiveEnd)
                        {
                            /* for clicks at or before left edge of selected frame, */
                            /* bump pivot up so that frame is included */
                            BaseSelectPivot += 1;
                        }
                        if (BaseSelectPivot > trackObject.FrameArray.Count)
                        {
                            // pivot frame index is beyond the bounds of the track
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        break;
                }
            }

            /* draw initial selection */
            TrackViewSetRangeSelection(
                BaseSelectPivot,
                ActiveEnd,
                false/*end is active*/);

            /* enter the selection tracking loop */
            // Old Macintosh program used local modal event pump.
            MouseMoveCapture += new MouseEventHandler(delegate (object sender, MouseEventArgs ee)
            {
                TrackViewControl_MouseMoveCapture_SelectionLocalLoopBody(sender, ee, ActiveEnd, BaseSelectPivot);
            });
        }

        private void TrackViewControl_MouseMoveCapture_SelectionLocalLoopBody(object sender, MouseEventArgs e, int ActiveEnd, int BaseSelectPivot)
        {
            using (GraphicsContext gc = new GraphicsContext(this))
            {
                /* scroll if necessary */
                if (e.X < 0)
                {
                    /* scroll left */
                    HorizontalScroll.Value = Math.Max(HorizontalScroll.Value - (-e.X), HorizontalScroll.Minimum);
                }
                if (e.X >= Width)
                {
                    /* scroll right */
                    HorizontalScroll.Value = Math.Min(HorizontalScroll.Value + (e.X - Width), HorizontalScroll.Maximum);
                }
                if (e.Y < 0)
                {
                    /* scroll up */
                    VerticalScroll.Value = Math.Max(VerticalScroll.Value - (-e.Y), VerticalScroll.Minimum);
                }
                if (e.Y >= Height)
                {
                    /* scroll down */
                    VerticalScroll.Value = Math.Min(VerticalScroll.Value + (e.Y - Height), VerticalScroll.Maximum);
                }

                /* figure out where the thang goes now */
                bool LandedOnAFrame;
                int NewActiveEnd;
                Schedule.TrackDisplayPixelToIndex(
                    trackObject,
                    e.X + PixelIndent,
                    out LandedOnAFrame,
                    out NewActiveEnd);
                if ((NewActiveEnd < 0) || (NewActiveEnd > trackObject.FrameArray.Count))
                {
                    // click frame index is beyond the bounds of the track
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                if (NewActiveEnd != ActiveEnd)
                {
                    ActiveEnd = NewActiveEnd;
                    TrackViewSetRangeSelection(
                        BaseSelectPivot,
                        ActiveEnd,
                        false/*end is active*/);
                }
            }
        }

        private NoteObjectRec TrackViewGetMouseOverNote(Graphics graphics, int X, int Y)
        {
            using (GraphicsContext gc = new GraphicsContext(this, graphics))
            {
                int FrameIndex;
                bool CommandFlag;
                NoteObjectRec Note = Schedule.TrackDisplayGetUnderlyingNote(
                    Schedule.TrackDisplayGetTrackIndex(trackObject),
                    out CommandFlag,
                    X + PixelIndent,
                    out FrameIndex);

                return Note;
            }
        }

        /* update the cursor and optionally draw the vertical (pitch) positioning bar */
        private void TrackViewUpdateMouseCursor(int X, int Y, bool DrawFunnyBarThing)
        {
            bool OnAFrameFlag;
            int Unused;

            Schedule.TrackDisplayPixelToIndex(
               trackObject,
               X + PixelIndent,
               out OnAFrameFlag,
               out Unused);
            if (DrawFunnyBarThing)
            {
                TrackViewSetCursorBar(X, Y, false);
            }
            if (OnAFrameFlag)
            {
                Cursor = Bitmaps.ScoreOverlayCursor;
            }
            else
            {
                Cursor = Bitmaps.ScoreIntersticeCursor;
            }
        }

        public void TrackViewTrySingleNoteOrCommandSelection(NoteObjectRec note)
        {
            int frameIndex, noteIndex;
            if (!trackObject.FindNote(note, out frameIndex, out noteIndex))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (note.IsItACommand)
            {
                SelectionMode = SelectionModes.eTrackViewSingleCommandSelection;
            }
            else
            {
                SelectionMode = SelectionModes.eTrackViewSingleNoteSelection;
            }
            SelectedNote = note;
            SelectedNoteFrame = frameIndex;
            Invalidate();
            TrackViewShowSelection();
        }

        /* try to select a single note, if there is one, at the specified mouse location */
        private void TrackViewTrySingleNoteSelection(int X, int Y)
        {
            bool CommandFlag;
            int FrameIndex;
            NoteObjectRec Note = Schedule.TrackDisplayGetUnderlyingNote(
                Schedule.TrackDisplayGetTrackIndex(trackObject),
                out CommandFlag,
                X + PixelIndent,
                out FrameIndex);
            if (Note != null)
            {
                if (CommandFlag)
                {
                    SelectionMode = SelectionModes.eTrackViewSingleCommandSelection;
                }
                else
                {
                    SelectionMode = SelectionModes.eTrackViewSingleNoteSelection;
                }
                SelectedNote = Note;
                SelectedNoteFrame = FrameIndex;
            }
            else
            {
                SelectionMode = SelectionModes.eTrackViewNoSelection;
                InsertionPointIndex = FrameIndex;
                if (InsertionPointIndex > trackObject.FrameArray.Count)
                {
                    // insertion point beyond end of track
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
            }
            TrackViewRedrawAll();
        }

        /* try to select a single note, if there is one, at the specified logical position */
        private void TrackViewTrySingleNoteSelectionIndexed(int FrameIndex, int NoteIndex)
        {
            FrameObjectRec Frame = trackObject.FrameArray[FrameIndex];
            if (Frame.IsThisACommandFrame)
            {
                SelectionMode = SelectionModes.eTrackViewSingleCommandSelection;
            }
            else
            {
                SelectionMode = SelectionModes.eTrackViewSingleNoteSelection;
            }
            SelectedNote = Frame[NoteIndex];
            SelectedNoteFrame = FrameIndex;

            TrackViewRedrawAll();
        }

        /* select a range of frames. */
        private void TrackViewSetRangeSelection(int StartFrame, int EndFramePlusOne, bool StartIsActive)
        {
            if (StartFrame > EndFramePlusOne)
            {
                int Temp = StartFrame;
                StartFrame = EndFramePlusOne;
                EndFramePlusOne = Temp;
                StartIsActive = !StartIsActive;
            }
            if (StartFrame == EndFramePlusOne)
            {
                TrackViewSetInsertionPoint(StartFrame);
                return;
            }
            if (StartFrame < 0)
            {
                // start frame is before beginning of track
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (EndFramePlusOne > trackObject.FrameArray.Count)
            {
                // end frame is after end of track
                Debug.Assert(false);
                throw new ArgumentException();
            }
            SelectionMode = SelectionModes.eTrackViewRangeSelection;
            RangeSelectStart = StartFrame;
            RangeSelectEnd = EndFramePlusOne;
            RangeSelectStartIsActive = StartIsActive;
            /* redraw with the selection changes */
            TrackViewRedrawAll();
        }

        public void TrackViewShowSelection()
        {
            using (GraphicsContext gc = new GraphicsContext(this))
            {
                int FrameIndex;
                int FrameWidth;
                int PixelIndex;

                switch (SelectionMode)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case SelectionModes.eTrackViewNoSelection:
                        FrameIndex = InsertionPointIndex;
                        break;
                    case SelectionModes.eTrackViewSingleNoteSelection:
                    case SelectionModes.eTrackViewSingleCommandSelection:
                        FrameIndex = SelectedNoteFrame;
                        break;
                    case SelectionModes.eTrackViewRangeSelection:
                        FrameIndex = RangeSelectStartIsActive
                            ? RangeSelectStart
                            : RangeSelectEnd;
                        break;
                }
                if (FrameIndex < trackObject.FrameArray.Count)
                {
                    Schedule.TrackDisplayIndexToPixel(0/*first track*/, FrameIndex, out PixelIndex);
                    FrameWidth = FrameDrawUtility.WidthOfFrameAndDraw(
                        gc,
                        0,
                        0,
                        trackObject.FrameArray[FrameIndex],
                        false/*don't draw*/,
                        false,
                        trackObject.InlineParamVis);
                }
                else if (trackObject.FrameArray.Count > 0)
                {
                    Schedule.TrackDisplayIndexToPixel(
                        0/*first track*/,
                        trackObject.FrameArray.Count - 1,
                        out PixelIndex);
                    FrameWidth = FrameDrawUtility.WidthOfFrameAndDraw(
                        gc,
                        0,
                        0,
                        trackObject.FrameArray[trackObject.FrameArray.Count - 1],
                        false/*don't draw*/,
                        false,
                        trackObject.InlineParamVis);
                    PixelIndex += FrameWidth;
                    FrameWidth = 0;
                }
                else
                {
                    /* no frames at all. */
                    PixelIndex = 0;
                    FrameWidth = 0;
                }

                const int ScrollIntoViewMargin = 75;
                if (-AutoScrollPosition.X > PixelIndex - ScrollIntoViewMargin)
                {
                    AutoScrollPosition = new Point(PixelIndex - ScrollIntoViewMargin, -AutoScrollPosition.Y);
                }
                else if (-AutoScrollPosition.X < PixelIndex + FrameWidth - ClientSize.Width + ScrollIntoViewMargin)
                {
                    AutoScrollPosition = new Point(PixelIndex + FrameWidth - ClientSize.Width + ScrollIntoViewMargin, -AutoScrollPosition.Y);
                }
            }
        }

        /* set an insertion point at the specified frame index */
        private void TrackViewSetInsertionPoint(int FrameIndex)
        {
            if ((FrameIndex < 0) || (FrameIndex > trackObject.FrameArray.Count))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            SelectionMode = SelectionModes.eTrackViewNoSelection;
            InsertionPointIndex = FrameIndex;
            /* redraw with the selection changes */
            TrackViewRedrawAll();
        }

        /* draw the vertical (pitch) positioning bar */
        private void TrackViewSetCursorBar(int X, int Y, bool ForceUpdate)
        {
            if (CursorBarIsLocked)
            {
                if ((X - LastCursorBarX < -TrackDisplayConstants.CURSORBARRELEASEDELTA)
                    || (X - LastCursorBarX > TrackDisplayConstants.CURSORBARRELEASEDELTA)
                    || (Y - LastCursorBarY < -TrackDisplayConstants.CURSORBARRELEASEDELTA)
                    || (Y - LastCursorBarY > TrackDisplayConstants.CURSORBARRELEASEDELTA))
                {
                    CursorBarIsLocked = false;
                }
            }
            if (ForceUpdate || !CursorBarIsLocked)
            {
                LastCursorBarX = X;
                LastCursorBarY = Y;
                int NewCursorBar = StaffCalibration.ConvertPitchToPixel(StaffCalibration.ConvertPixelToPitch(Y + VerticalOffset), 0);
                if ((NewCursorBar != CursorBarLoc) || !CursorBarIsVisible)
                {
                    TrackViewUndrawCursorBar();
                    CursorBarLoc = NewCursorBar;
                    TrackViewDrawCursorBar();
                }
            }
        }

        /* transpose the selection by adding AddHalfSteps to the pitch of each selected note */
        /* AddHalfSteps can be negative.  it is an error if there is no single note selection */
        /* or no range selection */
        public void TrackViewTransposeSelection(int AddHalfSteps)
        {
            using (GraphicsContext gc = new GraphicsContext(this))
            {
                undoHelper.SaveUndoInfo(false/*forRedo*/, "Transpose");

                if (TrackViewIsASingleNoteSelected())
                {
                    int NewPitch = ((NoteNoteObjectRec)SelectedNote).GetNotePitch() + AddHalfSteps;
                    if (NewPitch < 0)
                    {
                        NewPitch = 0;
                    }
                    else if (NewPitch > Constants.NUMNOTES - 1)
                    {
                        NewPitch = Constants.NUMNOTES - 1;
                    }
                    ((NoteNoteObjectRec)SelectedNote).PutNotePitch((short)NewPitch);
                    switch (NewPitch % 12)
                    {
                        case 0: /* C */
                        case 2: /* D */
                        case 4: /* E */
                        case 5: /* F */
                        case 7: /* G */
                        case 9: /* A */
                        case 11: /* B */
                            ((NoteNoteObjectRec)SelectedNote).PutNoteFlatOrSharpStatus(0);
                            break;
                        case 1: /* C# / Db */
                        case 3: /* D# / Eb */
                        case 6: /* F# / Gb */
                        case 8: /* G# / Ab */
                        case 10: /* A# / Bb */
                            if (AddHalfSteps >= 0)
                            {
                                if (((NoteNoteObjectRec)SelectedNote).GetNoteFlatOrSharpStatus() == NoteFlags.eFlatModifier)
                                {
                                    ((NoteNoteObjectRec)SelectedNote).PutNoteFlatOrSharpStatus(NoteFlags.eFlatModifier);
                                }
                                else
                                {
                                    ((NoteNoteObjectRec)SelectedNote).PutNoteFlatOrSharpStatus(NoteFlags.eSharpModifier);
                                }
                            }
                            else
                            {
                                if (((NoteNoteObjectRec)SelectedNote).GetNoteFlatOrSharpStatus() == NoteFlags.eSharpModifier)
                                {
                                    ((NoteNoteObjectRec)SelectedNote).PutNoteFlatOrSharpStatus(NoteFlags.eSharpModifier);
                                }
                                else
                                {
                                    ((NoteNoteObjectRec)SelectedNote).PutNoteFlatOrSharpStatus(NoteFlags.eFlatModifier);
                                }
                            }
                            break;
                    }
                }
                else if (TrackViewIsARangeSelected())
                {
                    for (int FrameScan = RangeSelectStart; FrameScan < RangeSelectEnd; FrameScan += 1)
                    {
                        FrameObjectRec Frame = trackObject.FrameArray[FrameScan];
                        if (!Frame.IsThisACommandFrame)
                        {
                            for (int NoteScan = 0; NoteScan < Frame.Count; NoteScan += 1)
                            {
                                NoteNoteObjectRec Note = (NoteNoteObjectRec)Frame[NoteScan];
                                int NewPitch = Note.GetNotePitch() + AddHalfSteps;
                                if (NewPitch < 0)
                                {
                                    NewPitch = 0;
                                }
                                else if (NewPitch > Constants.NUMNOTES - 1)
                                {
                                    NewPitch = Constants.NUMNOTES - 1;
                                }
                                Note.PutNotePitch((short)NewPitch);
                                switch (NewPitch % 12)
                                {
                                    case 0: /* C */
                                    case 2: /* D */
                                    case 4: /* E */
                                    case 5: /* F */
                                    case 7: /* G */
                                    case 9: /* A */
                                    case 11: /* B */
                                        Note.PutNoteFlatOrSharpStatus(0);
                                        break;
                                    case 1: /* C# / Db */
                                    case 3: /* D# / Eb */
                                    case 6: /* F# / Gb */
                                    case 8: /* G# / Ab */
                                    case 10: /* A# / Bb */
                                        if (AddHalfSteps >= 0)
                                        {
                                            if (Note.GetNoteFlatOrSharpStatus() == NoteFlags.eFlatModifier)
                                            {
                                                Note.PutNoteFlatOrSharpStatus(NoteFlags.eFlatModifier);
                                            }
                                            else
                                            {
                                                Note.PutNoteFlatOrSharpStatus(NoteFlags.eSharpModifier);
                                            }
                                        }
                                        else
                                        {
                                            if (Note.GetNoteFlatOrSharpStatus() == NoteFlags.eSharpModifier)
                                            {
                                                Note.PutNoteFlatOrSharpStatus(NoteFlags.eSharpModifier);
                                            }
                                            else
                                            {
                                                Note.PutNoteFlatOrSharpStatus(NoteFlags.eFlatModifier);
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.Assert(false); // improper selection
                }

                TrackViewRedrawAll();
                TrackObjectAltered(trackObject, 0);
            }
        }

        /* present a dialog box for editing the selected object's attributes.  it is an */
        /* error if there is no single note or command selected. */
        public void TrackViewEditSingleSelectionAttributes()
        {
            Debug.Assert(TrackViewIsASingleNoteSelected() || TrackViewIsASingleCommandSelected());

            undoHelper.SaveUndoInfo(false/*forRedo*/, "Edit Note Properties");

            Window.EditNoteProperties();
        }

        /* attempt to paste notes into the track.  returns False if it fails.  undo */
        /* information is automatically maintained. */
        public bool TrackViewAttemptPaste()
        {
            using (GraphicsContext gc = new GraphicsContext(this))
            {
                /* delete any selected range */
                if (SelectionMode == SelectionModes.eTrackViewRangeSelection)
                {
                    TrackViewDeleteRangeSelection();
                }
                Debug.Assert(SelectionMode == SelectionModes.eTrackViewNoSelection);

                /* get the scrap */
                TrackObjectRec track = null;
                IDataObject dataObject = Clipboard.GetDataObject();
                foreach (string format in dataObject.GetFormats())
                {
                    if (String.Equals(format, TrackClipboard.ClipboardIdentifer))
                    {
                        TrackClipboard clipboard = (TrackClipboard)dataObject.GetData(TrackClipboard.ClipboardIdentifer);
                        object o = clipboard.Reconstitute(Window.MainWindow.Document);
                        track = (TrackObjectRec)o;
                        break;
                    }
                }
                if (track == null)
                {
                    return false;
                }

                undoHelper.SaveUndoInfo(false/*forRedo*/, "Insert Range");

                /* scan over all frames and add them */
                int Scan = 0;
                while (track.FrameArray.Count > 0)
                {
                    FrameObjectRec Thing = track.FrameArray[0];
                    trackObject.FrameArray.Insert(Scan + InsertionPointIndex, Thing);
                    track.FrameArray.RemoveAt(0);
                    Scan++;
                }
                TrackObjectAltered(trackObject, InsertionPointIndex);
                /* update display parameters */
                InsertionPointIndex += Scan;
                Debug.Assert(InsertionPointIndex <= trackObject.FrameArray.Count);
                TrackViewRedrawAll();
                TrackViewShowSelection();
            }

            return true;
        }

        /* copy the selected range to the clipboard.  it is an error if there is no selected */
        /* range.  it returns True if successful. */
        public void TrackViewCopyRangeSelection()
        {
            Debug.Assert(SelectionMode == SelectionModes.eTrackViewRangeSelection);

            FrameObjectRec[] CopyOfSelection = trackObject.TrackObjectCopyFrameRun(
                RangeSelectStart,
                RangeSelectEnd - RangeSelectStart);
            TrackObjectRec copy = new TrackObjectRec(Window.MainWindow.Document);
            foreach (FrameObjectRec frame in CopyOfSelection)
            {
                copy.FrameArray.Add(frame);
            }

            TrackClipboard clipboard = new TrackClipboard(copy, Window.MainWindow.Document);
            Clipboard.SetData(TrackClipboard.ClipboardIdentifer, clipboard);
        }

        /* cut the range selection (copy to clipboard, then delete).  returns True if */
        /* successful.  it is an error if there is no range selected.  automatically */
        /* updates undo information. */
        public void TrackViewCutRangeSelection()
        {
            using (GraphicsContext gc = new GraphicsContext(this))
            {
                // Undo handled by TrackViewDeleteRangeSelection()

                TrackViewCopyRangeSelection();
                TrackViewDeleteRangeSelection();
            }
        }

        /* scroll to the specified measure.  invalid measure numbers may be specified. */
        public void TrackViewShowMeasure(int MeasureNumber)
        {
            int FrameIndex;
            Schedule.TrackDisplayMeasureIndexToFrame(MeasureNumber, out FrameIndex);

            int PixelIndex;
            if (FrameIndex < trackObject.FrameArray.Count)
            {
                Schedule.TrackDisplayIndexToPixel(0/*first track*/, FrameIndex, out PixelIndex);
            }
            else if (trackObject.FrameArray.Count > 0)
            {
                Schedule.TrackDisplayIndexToPixel(0/*first track*/, trackObject.FrameArray.Count - 1, out PixelIndex);
            }
            else
            {
                /* no frames at all. */
                PixelIndex = 0;
            }
            PixelIndex -= ClientSize.Width / 3;
            if (PixelIndex < 0)
            {
                PixelIndex = 0;
            }
            AutoScrollPosition = new Point(PixelIndex, -AutoScrollPosition.Y);
        }

        public void TrackViewShowNote(NoteObjectRec note)
        {
            int frameIndex, noteIndex;
            if (!trackObject.FindNote(note, out frameIndex, out noteIndex))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            int PixelIndex;
            Schedule.TrackDisplayIndexToPixel(0/*first track*/, frameIndex, out PixelIndex);
            int offset = Schedule.TrackDisplayGetNoteOffset(0/*first track*/, frameIndex, noteIndex);
            PixelIndex += offset;
            int width = Schedule.TrackDisplayGetNoteInternalWidth(0/*first track*/, frameIndex, noteIndex);

            const int ScrollIntoViewMargin = 75;
            int oldScrollOffset = -AutoScrollPosition.X;
            if (-AutoScrollPosition.X > PixelIndex - ScrollIntoViewMargin)
            {
                AutoScrollPosition = new Point(PixelIndex - ScrollIntoViewMargin, -AutoScrollPosition.Y);
            }
            else if (-AutoScrollPosition.X < PixelIndex + width - ClientSize.Width + ScrollIntoViewMargin)
            {
                AutoScrollPosition = new Point(PixelIndex + width - ClientSize.Width + ScrollIntoViewMargin, -AutoScrollPosition.Y);
            }
            int newScrollOffset = -AutoScrollPosition.X;
            if (noteParamStrip != null)
            {
                noteParamStrip.Shift(oldScrollOffset - newScrollOffset);
            }
        }

        /* helper for TrackViewAdjustDuration: make sure the note can be made of the */
        /* requested duration change */
        private bool TrackViewAdjustDurationValidCheck(
            int Numerator,
            int Denominator,
            NoteObjectRec Note)
        {
            /* commands can just be ignored*/
            if (Note.IsItACommand)
            {
                return true;
            }

            /* if halving, can we cut it in half? */
            if ((Numerator == 1) && (Denominator == 2))
            {
                /* 64th is the only one we can't cut in half */
                if (((NoteNoteObjectRec)Note).GetNoteDuration() == NoteFlags.e64thNote)
                {
                    MessageBox.Show("Selection contains 64th note which can't be halved.");
                    return false;
                }
                return true;
            }

            /* if doubling, can it be doubled? */
            if ((Numerator == 2) && (Denominator == 1))
            {
                /* quad is the only one we can't double */
                if (((NoteNoteObjectRec)Note).GetNoteDuration() == NoteFlags.eQuadNote)
                {
                    MessageBox.Show("Selection contains quad note which can't be doubled.");
                    return false;
                }
                return true;
            }

            /* if we get here, then it's an unrecognized duration change */
            Debug.Assert(false);
            throw new ArgumentException();
        }

        /* helper for TrackViewAdjustDuration: adjust the note's duration */
        private void TrackViewAdjustDurationDoIt(
            int Numerator,
            int Denominator,
            NoteObjectRec Note)
        {
            /* commands can just be ignored*/
            if (Note.IsItACommand)
            {
                return;
            }

            /* halving */
            if ((Numerator == 1) && (Denominator == 2))
            {
                NoteFlags Duration = ((NoteNoteObjectRec)Note).GetNoteDuration();
                switch (Duration)
                {
                    default:
                    case NoteFlags.e64thNote:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NoteFlags.e32ndNote:
                        Duration = NoteFlags.e64thNote;
                        break;
                    case NoteFlags.e16thNote:
                        Duration = NoteFlags.e32ndNote;
                        break;
                    case NoteFlags.e8thNote:
                        Duration = NoteFlags.e16thNote;
                        break;
                    case NoteFlags.e4thNote:
                        Duration = NoteFlags.e8thNote;
                        break;
                    case NoteFlags.e2ndNote:
                        Duration = NoteFlags.e4thNote;
                        break;
                    case NoteFlags.eWholeNote:
                        Duration = NoteFlags.e2ndNote;
                        break;
                    case NoteFlags.eDoubleNote:
                        Duration = NoteFlags.eWholeNote;
                        break;
                    case NoteFlags.eQuadNote:
                        Duration = NoteFlags.eDoubleNote;
                        break;
                }
                ((NoteNoteObjectRec)Note).PutNoteDuration(Duration);
                return;
            }

            /* doubling */
            if ((Numerator == 2) && (Denominator == 1))
            {
                NoteFlags Duration = ((NoteNoteObjectRec)Note).GetNoteDuration();
                switch (Duration)
                {
                    default:
                    case NoteFlags.eQuadNote:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case NoteFlags.e64thNote:
                        Duration = NoteFlags.e32ndNote;
                        break;
                    case NoteFlags.e32ndNote:
                        Duration = NoteFlags.e16thNote;
                        break;
                    case NoteFlags.e16thNote:
                        Duration = NoteFlags.e8thNote;
                        break;
                    case NoteFlags.e8thNote:
                        Duration = NoteFlags.e4thNote;
                        break;
                    case NoteFlags.e4thNote:
                        Duration = NoteFlags.e2ndNote;
                        break;
                    case NoteFlags.e2ndNote:
                        Duration = NoteFlags.eWholeNote;
                        break;
                    case NoteFlags.eWholeNote:
                        Duration = NoteFlags.eDoubleNote;
                        break;
                    case NoteFlags.eDoubleNote:
                        Duration = NoteFlags.eQuadNote;
                        break;
                }
                ((NoteNoteObjectRec)Note).PutNoteDuration(Duration);
                return;
            }

            /* if we get here, then it's an unrecognized duration change */
            Debug.Assert(false);
            throw new ArgumentException();
        }

        /* change the duration of notes in the selection */
        public void TrackViewAdjustDuration(
            int Numerator,
            int Denominator)
        {
            using (GraphicsContext gc = new GraphicsContext(this))
            {
                switch (SelectionMode)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case SelectionModes.eTrackViewSingleNoteSelection:
                        if (TrackViewAdjustDurationValidCheck(Numerator, Denominator, SelectedNote))
                        {
                            undoHelper.SaveUndoInfo(false/*forRedo*/, "Adjust Duration");
                            TrackViewAdjustDurationDoIt(Numerator, Denominator, SelectedNote);
                        }
                        break;
                    case SelectionModes.eTrackViewRangeSelection:
                        {
                            bool AllOK = true;
                            for (int i = RangeSelectStart; AllOK && (i < RangeSelectEnd); i += 1)
                            {
                                FrameObjectRec Frame = trackObject.FrameArray[i];
                                for (int j = 0; AllOK && (j < Frame.Count); j += 1)
                                {
                                    NoteObjectRec Note = Frame[j];
                                    AllOK = AllOK && TrackViewAdjustDurationValidCheck(Numerator, Denominator, Note);
                                }
                            }
                            if (AllOK)
                            {
                                undoHelper.SaveUndoInfo(false/*forRedo*/, "Adjust Duration");
                                for (int i = RangeSelectStart; i < RangeSelectEnd; i += 1)
                                {
                                    FrameObjectRec Frame = trackObject.FrameArray[i];
                                    for (int j = 0; j < Frame.Count; j += 1)
                                    {
                                        NoteObjectRec Note = Frame[j];
                                        TrackViewAdjustDurationDoIt(Numerator, Denominator, Note);
                                    }
                                }
                            }
                        }
                        break;
                }

                Schedule.TrackDisplayScheduleMarkChanged(trackObject, 0);
                TrackViewShowSelection();
                TrackViewRedrawAll();
            }
        }


        // rendering

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            ClearGraphicsObjects();
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (!DesignMode)
            {
                return;
            }
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            // custom paint code here
            if (trackObject != null)
            {
                // TODO: use pe.Graphics.ScaleTransform(factor, factor) to implement zooming
                using (GraphicsContext gc = new GraphicsContext(this, pe.Graphics))
                {
                    TrackViewRedrawAll();
                }
            }
            else
            {
                // draw X to indicate control is operating but has no content (vs. failing to start)
                pe.Graphics.FillRectangle(gc.BackBrush, ClientRectangle);
                pe.Graphics.DrawLine(gc.ForePen, 0, 0, Width, Height);
                pe.Graphics.DrawLine(gc.ForePen, Width, 0, 0, Height);
            }

            // Calling the base class OnPaint
            base.OnPaint(pe);
        }

        // TODO: turn into live properties
        private void SetScrollOffsetsForRendering(Graphics graphics)
        {
            PixelIndent = -AutoScrollPosition.X;
            VerticalOffset = -AutoScrollPosition.Y;
        }

        private void TrackViewRedrawAll()
        {
            SetScrollOffsetsForRendering(gc.graphics);

            bool layoutChanged = Schedule.TrackDisplayScheduleUpdate();

            /* erase the drawing area */
            gc.graphics.FillRectangle(
                gc.BackBrush,
                0,
                0,
                Schedule.TotalWidth + Width/*for overscrolled*/,
                StaffCalibration.MaxVerticalSize + Height/*for overscrolled*/);

            // draw mouse-over highlight effect for single note selection and NoteView inspector pane
            if (noteView.Note != null)
            {
                int frameIndex, noteIndex;
                if (trackObject.FindNote(noteView.Note, out frameIndex, out noteIndex))
                {
                    if (Schedule.TrackDisplayShouldWeDrawIt(0, frameIndex))
                    {
                        int PixelIndex;
                        Schedule.TrackDisplayIndexToPixel(0, frameIndex, out PixelIndex);
                        gc.graphics.FillRectangle(
                            gc.LightLightGreyBrush,
                            PixelIndex - PixelIndent + Schedule.TrackDisplayGetNoteOffset(0, frameIndex, noteIndex),
                            -VerticalOffset,
                            Schedule.TrackDisplayGetNoteInternalWidth(0, frameIndex, noteIndex),
                            StaffCalibration.MaxVerticalSize);
                    }
                }
            }

            // draw phantom insertion point
            if (phantomInsertionPointIndex >= 0)
            {
                int PixelIndex;
                Schedule.TrackDisplayIndexToPixelRobust(0, phantomInsertionPointIndex, out PixelIndex);
                gc.graphics.FillRectangle(
                    gc.LightGreyBrush,
                    PixelIndex - PixelIndent,
                    0,
                    1,
                    ClientSize.Height);
            }

            /* draw the staff */
            TrackViewRedrawStaff(gc.graphics);

            /* now we have to draw the notes */
            int CenterNotePixel = -VerticalOffset + StaffCalibration.CenterNotePixel;

            /* draw ties first, so they are under the notes */
            {
                GraphicsState saved = gc.graphics.Save();
                gc.graphics.SmoothingMode = SmoothingMode.AntiAlias; // they never looked so beautiful!
                List<TieTrackPixelRec.TiePixelRec> TieList = Schedule.TrackDisplayGetTieIntervalList(PixelIndent, Schedule.TotalWidth);
                for (int iTie = 0; iTie < TieList.Count; iTie++)
                {
                    int StartX;
                    int StartY;
                    int EndX;
                    int EndY;

                    TieTrackPixelRec.GetTieTrackIntersectElement(
                        TieList,
                        iTie,
                        out StartX,
                        out StartY,
                        out EndX,
                        out EndY);
                    DrawTieLine(
                        gc.graphics,
                        gc.ForePen,
                        (StartX + TrackDisplayConstants.TIESTARTXCORRECTION) - PixelIndent,
                        (StartY + TrackDisplayConstants.TIESTARTYCORRECTION) - VerticalOffset,
                        (EndX + TrackDisplayConstants.TIEENDXCORRECTION) - (StartX + TrackDisplayConstants.TIESTARTXCORRECTION),
                        (EndY + TrackDisplayConstants.TIEENDYCORRECTION) - (StartY + TrackDisplayConstants.TIESTARTYCORRECTION));
                }
                gc.graphics.Restore(saved);
            }

            /* draw the notes */
            int NumTracks = Schedule.TrackDisplayGetNumTracks();
            /* note the less than or EQUAL to NumTracks in the loop... */
            /* see note inside loop about why */
            for (int iTrack = 0; iTrack <= NumTracks; iTrack++)
            {
                /* we want to draw our track, but we want to draw it last.  to do this, */
                /* we won't draw it when it comes around normally, but we'll loop one extra */
                /* (fake) time, during which we won't do the normal get track but instead, */
                /* we'll just use our own track.  not especially elegant, but it works */
                TrackObjectRec TrackObj;
                if (iTrack < NumTracks)
                {
                    TrackObj = Schedule.TrackDisplayGetParticularTrack(iTrack);
                }
                else
                {
                    TrackObj = trackObject;
                }
                int ActualTrackIndex = Schedule.TrackDisplayGetTrackIndex(TrackObj);
                if ((TrackObj != trackObject) || (iTrack == NumTracks))
                {
                    int Position;
                    bool Unused;

                    Schedule.TrackDisplayPixelToIndex(TrackObj, PixelIndent, out Unused, out Position);

                    /* find out how much to draw.  this definitely needs to be */
                    /* fixed since there is no reason to draw frames until the */
                    /* end of the entire track */
                    int Limit = TrackObj.FrameArray.Count;

                    if (Position > 0)
                    {
                        /* this draws the note just off the left edge of the */
                        /* screen to fix some update region alignment problems. */
                        Position--;
                    }

                    /* draw the insertion point, if there is one */
                    if ((TrackObj == trackObject) && (SelectionMode == SelectionModes.eTrackViewNoSelection))
                    {
                        int TotalNumFrames = TrackObj.FrameArray.Count;
                        if ((InsertionPointIndex < 0) || (InsertionPointIndex > TotalNumFrames))
                        {
                            // insertion point index out of range
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }
                        if (InsertionPointIndex < TotalNumFrames)
                        {
                            int InsertionPixel;

                            /* draw the selection before it's corresponding frame */
                            Schedule.TrackDisplayIndexToPixel(0/*first track*/, InsertionPointIndex, out InsertionPixel);

                            InsertionPixel = -TrackDisplayConstants.INSERTIONPOINTWIDTH / 2
                                + InsertionPixel - PixelIndent;
                            if (InsertionPixel < TrackDisplayConstants.ORDTYPEMIN / 2)
                            {
                                InsertionPixel = TrackDisplayConstants.ORDTYPEMIN / 2;
                            }
                            if (InsertionPixel > TrackDisplayConstants.ORDTYPEMAX / 2)
                            {
                                InsertionPixel = TrackDisplayConstants.ORDTYPEMAX / 2;
                            }
                            gc.graphics.FillRectangle(
                                gc.ForeBrush,
                                InsertionPixel,
                                0,
                                TrackDisplayConstants.INSERTIONPOINTWIDTH,
                                StaffCalibration.MaxVerticalSize);
                        }
                        else if (TotalNumFrames > 0)
                        {
                            int InsertionPixel;

                            /* oops, no frame, so draw it after the last frame */
                            Schedule.TrackDisplayIndexToPixel(0/*first track*/, TrackObj.FrameArray.Count - 1, out InsertionPixel);

                            InsertionPixel += FrameDrawUtility.WidthOfFrameAndDraw(
                                gc,
                                0,
                                0,
                                TrackObj.FrameArray[TotalNumFrames - 1],
                                false/*don't draw*/,
                                false,
                                (TrackObj != trackObject) ? 0 : trackObject.InlineParamVis);
                            InsertionPixel = -TrackDisplayConstants.INSERTIONPOINTWIDTH / 2
                                + InsertionPixel + TrackDisplayConstants.EXTERNALSEPARATION - PixelIndent;
                            if (InsertionPixel < TrackDisplayConstants.ORDTYPEMIN / 2)
                            {
                                InsertionPixel = TrackDisplayConstants.ORDTYPEMIN / 2;
                            }
                            if (InsertionPixel > TrackDisplayConstants.ORDTYPEMAX / 2)
                            {
                                InsertionPixel = TrackDisplayConstants.ORDTYPEMAX / 2;
                            }
                            gc.graphics.FillRectangle(
                                gc.ForeBrush,
                                InsertionPixel,
                                0,
                                TrackDisplayConstants.INSERTIONPOINTWIDTH,
                                StaffCalibration.MaxVerticalSize);
                        }
                        else
                        {
                            /* oops, no frames at all, so just draw it at the beginning */
                            /* oh, well, it doesn't get drawn... should fix. (not that */
                            /* there's any doubt where an insertion would occur...) */
                        }
                    }

                    /* draw all of the frames */
                    for (int i = Position; i < Limit; i++)
                    {
                        int TheFrameWidth = Int32.MinValue;

                        /* see if we can draw.  if TrackDisplayIndexToPixel returns false, */
                        /* then it just won't be drawn.  if TrackDisplayShouldWeDrawIt */
                        /* returns False, then don't draw because it isn't scheduled. */
                        int PixelIndex;
                        Schedule.TrackDisplayIndexToPixel(ActualTrackIndex, i, out PixelIndex);
                        if (Schedule.TrackDisplayShouldWeDrawIt(ActualTrackIndex, i))
                        {
                            /* escape if we have gone past the end of the window */
#if true
                            if (PixelIndex - PixelIndent - FrameDisplayConstants.LEFTNOTEEDGEINSET >= Width)
                            {
                                goto DonePoint;
                            }
#endif
                            // new code: TODO: this should be tightened up
                            if (!gc.graphics.IsVisible(
                                new Rectangle(
                                    PixelIndex - PixelIndent - Width / 2,
                                    -VerticalOffset,
                                    2 * Width,
                                    StaffCalibration.MaxVerticalSize)))
                            {
                                continue;
                            }

                            /* get the frame to draw */
                            FrameObjectRec Frame = TrackObj.FrameArray[i];

                            /* this section is for stuff that only works in */
                            /* the current track */
                            if (TrackObj == trackObject)
                            {
                                /* if we should draw a measure bar, then do so.  this */
                                /* is done before drawing the notes so that the notes */
                                /* will appear on top of the measure bar numbers if */
                                /* there is an overwrite */

                                /* draw the measure bar */
                                int MeasureBarIndex = Schedule.TrackDisplayMeasureBarIndex(i);
                                if (MeasureBarIndex != TrackDisplayConstants.NOMEASUREBAR)
                                {
                                    Color color;
                                    Pen pen;
                                    Brush brush;

                                    if (Schedule.TrackDisplayShouldMeasureBarBeGreyed(i))
                                    {
                                        color = gc.GreyColor;
                                        pen = gc.GreyPen;
                                        brush = gc.GreyBrush;
                                    }
                                    else
                                    {
                                        color = gc.ForeColor;
                                        pen = gc.ForePen;
                                        brush = gc.ForeBrush;
                                    }
                                    /* actually something to be drawn */
                                    string measureNumberText = MeasureBarIndex.ToString();
                                    gc.graphics.DrawLine(
                                        pen,
                                        PixelIndex - 4/*!*/ - PixelIndent,
                                        -VerticalOffset,
                                        PixelIndex - 4/*!*/ - PixelIndent,
                                        -VerticalOffset + StaffCalibration.MaxVerticalSize);
                                    int widthMeasureNumberText = MyTextRenderer.MeasureText(
                                        gc.graphics,
                                        measureNumberText,
                                        gc.font).Width;
                                    MyTextRenderer.DrawText(
                                        gc.graphics,
                                        measureNumberText,
                                        gc.font,
                                        new Point(
                                            PixelIndex - 4/*!*/ - PixelIndent - widthMeasureNumberText / 2,
                                            CenterNotePixel - gc.font.Height / 2),
                                        color,
                                        TextFormatFlags.PreserveGraphicsClipping);
                                }
                            }

                            /* draw the notes */
                            TheFrameWidth = FrameDrawUtility.WidthOfFrameAndDraw(
                                gc,
                                PixelIndex - PixelIndent,
                                CenterNotePixel,
                                Frame,
                                true/*draw*/,
                                (TrackObj != trackObject)/*greyed*/,
                                (TrackObj != trackObject) ? 0 : trackObject.InlineParamVis);

                            /* draw any hilighting for selected items */
                            switch (SelectionMode)
                            {
                                default:
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                case SelectionModes.eTrackViewNoSelection:
                                    break;
                                case SelectionModes.eTrackViewSingleNoteSelection:
                                    {
                                        int FrameEnd = Frame.Count;
                                        if ((SelectedNoteFrame < 0) || (SelectedNoteFrame >= trackObject.FrameArray.Count))
                                        {
                                            // bad selected note frame
                                            Debug.Assert(false);
                                            throw new InvalidOperationException();
                                        }
                                        int j = 0;
                                        while (j < FrameEnd)
                                        {
                                            if (Frame[j] == SelectedNote)
                                            {
                                                int trackIndex = Schedule.TrackDisplayGetTrackIndex(TrackObj);
                                                int offset = Schedule.TrackDisplayGetNoteOffset(trackIndex, i, j);
                                                int width = Schedule.TrackDisplayGetNoteInternalWidth(trackIndex, i, j);
                                                gc.graphics.DrawRectangle(
                                                    gc.GreyPen,
                                                    PixelIndex - PixelIndent + offset,
                                                    -VerticalOffset,
                                                    width,
                                                    StaffCalibration.MaxVerticalSize);
                                                gc.graphics.DrawRectangle(
                                                    gc.GreyPen,
                                                    PixelIndex - PixelIndent + offset + 1,
                                                    -VerticalOffset + 1,
                                                    width - 2,
                                                    StaffCalibration.MaxVerticalSize - 2);
                                                goto SingleNoteSelectDonePoint;
                                            }
                                            j++;
                                        }
                                    }
                                    SingleNoteSelectDonePoint:
                                    break;
                                case SelectionModes.eTrackViewSingleCommandSelection:
                                    if ((SelectedNoteFrame < 0) || (SelectedNoteFrame >= trackObject.FrameArray.Count))
                                    {
                                        // bad selected note frame
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();
                                    }
                                    if (Frame.IsThisACommandFrame)
                                    {
                                        if (SelectedNote == Frame[0])
                                        {
                                            gc.graphics.DrawRectangle(
                                                gc.GreyPen,
                                                PixelIndex - PixelIndent,
                                                -VerticalOffset,
                                                TheFrameWidth,
                                                StaffCalibration.MaxVerticalSize);
                                            gc.graphics.DrawRectangle(
                                                gc.GreyPen,
                                                PixelIndex - PixelIndent + 1,
                                                -VerticalOffset + 1,
                                                TheFrameWidth - 2,
                                                StaffCalibration.MaxVerticalSize - 2);
                                        }
                                    }
                                    break;
                                case SelectionModes.eTrackViewRangeSelection:
                                    // done later, after all notes are drawn
                                    break;
                            }
                        }
                    } /* end of frame scan */
#if true
                    /* jump here when end of visible note series is reached */
                    DonePoint:
                    ;
#endif
                }
            } /* end of track scan */

            /* draw selection box */
            if (SelectionMode == SelectionModes.eTrackViewRangeSelection)
            {
                int OurTrackIndex = Schedule.TrackDisplayGetTrackIndex(trackObject);
                if ((RangeSelectStart < 0) || (RangeSelectStart > trackObject.FrameArray.Count))
                {
                    // bad range start
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                if ((RangeSelectEnd < 0) || (RangeSelectEnd > trackObject.FrameArray.Count))
                {
                    // bad range end
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                if (RangeSelectStart >= RangeSelectEnd)
                {
                    // range selection boundaries are invalid
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                int StartPixelIndex;
                Schedule.TrackDisplayIndexToPixel(OurTrackIndex, RangeSelectStart, out StartPixelIndex);
                StartPixelIndex += -PixelIndent; /* normalize to screen */
                if (StartPixelIndex < TrackDisplayConstants.ORDTYPEMIN / 2)
                {
                    StartPixelIndex = TrackDisplayConstants.ORDTYPEMIN / 2;
                }
                if (StartPixelIndex > TrackDisplayConstants.ORDTYPEMAX / 2)
                {
                    StartPixelIndex = TrackDisplayConstants.ORDTYPEMAX / 2;
                }

                int EndPixelIndex;
                Schedule.TrackDisplayIndexToPixel(OurTrackIndex, RangeSelectEnd - 1, out EndPixelIndex);
                EndPixelIndex += FrameDrawUtility.WidthOfFrameAndDraw(
                    gc,
                    0,
                    0,
                    trackObject.FrameArray[RangeSelectEnd - 1],
                    false/*nodraw*/,
                    false,
                    trackObject.InlineParamVis);
                EndPixelIndex += -PixelIndent; /* normalize to screen */
                if (EndPixelIndex < TrackDisplayConstants.ORDTYPEMIN / 2)
                {
                    EndPixelIndex = TrackDisplayConstants.ORDTYPEMIN / 2;
                }
                if (EndPixelIndex > TrackDisplayConstants.ORDTYPEMAX / 2)
                {
                    EndPixelIndex = TrackDisplayConstants.ORDTYPEMAX / 2;
                }

                /* draw upper and lower edges of bounding box */
                gc.graphics.FillRectangle(
                    gc.ForeBrush,
                    StartPixelIndex,
                    0,
                    EndPixelIndex - StartPixelIndex + TrackDisplayConstants.EXTERNALSEPARATION,
                    TrackDisplayConstants.RANGESELECTTHICKNESS);
                gc.graphics.FillRectangle(
                    gc.ForeBrush,
                    StartPixelIndex,
                    ClientSize.Height - TrackDisplayConstants.RANGESELECTTHICKNESS/*StaffCalibration.MaxVerticalSize - TrackDisplayConstants.RANGESELECTTHICKNESS*/,
                    EndPixelIndex - StartPixelIndex + TrackDisplayConstants.EXTERNALSEPARATION,
                    TrackDisplayConstants.RANGESELECTTHICKNESS);

                /* draw left edge of bounding box */
                gc.graphics.FillRectangle(
                    gc.ForeBrush,
                    StartPixelIndex,
                    0,
                    RangeSelectStartIsActive ? TrackDisplayConstants.RANGESELECTTHICKNESS : 1,
                    ClientSize.Height/*StaffCalibration.MaxVerticalSize*/);

                /* draw right edge of bounding box */
                gc.graphics.FillRectangle(
                    gc.ForeBrush,
                    EndPixelIndex + TrackDisplayConstants.EXTERNALSEPARATION,
                    0,
                    !RangeSelectStartIsActive ? TrackDisplayConstants.RANGESELECTTHICKNESS : 1,
                    ClientSize.Height/*StaffCalibration.MaxVerticalSize*/);
            }

            /* draw cursor bar */
            if (CursorBarIsVisible && Focused)
            {
                TrackViewDrawCursorBar();
            }

            // if positions may have changed, trigger an event to update mouse-over visuals for whatever is now under the mouse.
            // (timer is used as simpler way instead of trying to post fake WM_MOUSEMOVE to message queue.)
            if (layoutChanged)
            {
                timerUpdateMouseOverEffect.Start();
            }
        }

        private void TrackViewRedrawStaff(Graphics graphics)
        {
            /* draw staff bars around C */
            int[] MajorStaffList = StaffCalibration.MajorStaffList;
            for (int i = MajorStaffList.Length - 1; i >= 0; i--)
            {
                int y = StaffCalibration.ConvertPitchToPixel(MajorStaffList[i], 0) - VerticalOffset;
                graphics.DrawLine(
                    gc.ForePen,
                    0,
                    y,
                    Schedule.TotalWidth,
                    y);
            }

            /* draw other staff bars */
            for (int i = StaffCalibration.MinorStaffList.Length - 1; i >= 0; i--)
            {
                int y = StaffCalibration.ConvertPitchToPixel(StaffCalibration.MinorStaffList[i], 0) - VerticalOffset;
                graphics.DrawLine(
                    gc.LightGreyPen,
                    0,
                    y,
                    Schedule.TotalWidth,
                    y);
            }

            /* draw all C lines */
            for (int i = 0; i <= (Constants.NUMNOTES - 1) / 2; i += 12)
            {
                int y = StaffCalibration.ConvertPitchToPixel(Constants.CENTERNOTE - i, 0) - VerticalOffset;
                graphics.DrawLine(
                    (i == 0) ? gc.LightGreyPen : gc.GreyPen,
                    0,
                    y,
                    Schedule.TotalWidth,
                    y);
                y = StaffCalibration.ConvertPitchToPixel(Constants.CENTERNOTE + i, 0) - VerticalOffset;
                graphics.DrawLine(
                    (i == 0) ? gc.LightGreyPen : gc.GreyPen,
                    0,
                    y,
                    Schedule.TotalWidth,
                    y);
            }
        }

        /* draw a cute little paraboloid thing */
        private void DrawTieLine(Graphics graphics, Pen color, int StartX, int StartY, int Width, int Height)
        {
            float[] PixelXLoc = new float[TrackDisplayConstants.NUMTIELINEINTERVALS];
            float[] PixelYLoc = new float[TrackDisplayConstants.NUMTIELINEINTERVALS];

            /* generate coordinates */
            for (int Index = 0; Index < TrackDisplayConstants.NUMTIELINEINTERVALS; Index++)
            {
                float FuncX;

                FuncX = 2 * ((float)Index / (TrackDisplayConstants.NUMTIELINEINTERVALS - 1)) - 1;
                PixelXLoc[Index] = Width * ((float)Index / (TrackDisplayConstants.NUMTIELINEINTERVALS - 1));
                PixelYLoc[Index] = Height * ((float)Index / (TrackDisplayConstants.NUMTIELINEINTERVALS - 1))
                    + ((1 - FuncX * FuncX) * TrackDisplayConstants.MAXTIEDEPTH);
            }
            /* draw the lines */
            for (int Index = 0; Index < TrackDisplayConstants.NUMTIELINEINTERVALS - 1; Index++)
            {
                float X0 = StartX + PixelXLoc[Index];
                float Y0 = StartY + PixelYLoc[Index];
                float X1 = StartX + PixelXLoc[Index + 1];
                float Y1 = StartY + PixelYLoc[Index + 1];

                graphics.DrawLine(
                    color,
                    X0,
                    Y0,
                    X1,
                    Y1);
            }
        }

        /* draw the vertical (pitch) positioning bar.  this assumes that it has been */
        /* previously undrawn (i.e. it doesn't erase any existing bars) */
        private void TrackViewDrawCursorBar()
        {
            CursorBarIsVisible = true;
            gc.graphics.FillRectangle(
                gc.GreyBrush,
                0,
                -VerticalOffset + CursorBarLoc - 1,
                Schedule.TotalWidth,
                3);
        }

        /* erase the vertical (pitch) positioning bar */
        private void TrackViewUndrawCursorBar()
        {
            if (CursorBarIsVisible)
            {
                CursorBarIsVisible = false;
                gc.graphics.IntersectClip(
                    new Rectangle(
                        0,
                        -VerticalOffset + CursorBarLoc - 1,
                        Width,
                        3));
                TrackViewRedrawAll();
                gc.graphics.SetClip(ClientRectangle);
            }
        }

        private void TrackViewRedrawNote(NoteObjectRec note)
        {
            TrackViewRedrawOrInvalidateNote(note, true/*redraw*/);
        }

        private void TrackViewInvalidateNote(NoteObjectRec note)
        {
            TrackViewRedrawOrInvalidateNote(note, false/*redraw*/);
        }

        private void TrackViewRedrawOrInvalidateNote(NoteObjectRec note, bool redraw)
        {
            int frameIndex, noteIndex;
            if (trackObject.FindNote(note, out frameIndex, out noteIndex))
            {
                if (Schedule.TrackDisplayShouldWeDrawIt(0, frameIndex))
                {
                    int PixelIndex;
                    Schedule.TrackDisplayIndexToPixel(0, frameIndex, out PixelIndex);
                    Rectangle rect = new Rectangle(
                        PixelIndex - PixelIndent + Schedule.TrackDisplayGetNoteOffset(0, frameIndex, noteIndex),
                        -VerticalOffset,
                        Schedule.TrackDisplayGetNoteInternalWidth(0, frameIndex, noteIndex),
                        StaffCalibration.MaxVerticalSize);
                    if (redraw)
                    {
                        gc.graphics.IntersectClip(rect);
                        TrackViewRedrawAll();
                        gc.graphics.SetClip(ClientRectangle);
                    }
                    else
                    {
                        Invalidate(rect);
                    }
                }
            }
        }

        private void TrackViewChangePhantomInsertionPoint(int newIP)
        {
            if (phantomInsertionPointIndex != newIP)
            {
                using (GraphicsContext gc = new GraphicsContext(this))
                {
                    // undraw old location
                    if (phantomInsertionPointIndex >= 0)
                    {
                        int PixelIndex;
                        Schedule.TrackDisplayIndexToPixelRobust(0, phantomInsertionPointIndex, out PixelIndex);
                        gc.graphics.SetClip(
                            new Rectangle(
                                PixelIndex - PixelIndent,
                                0,
                                1,
                                ClientSize.Height));

                        phantomInsertionPointIndex = -1;

                        TrackViewRedrawAll();
                    }

                    // draw new location
                    if (newIP >= 0)
                    {
                        phantomInsertionPointIndex = newIP;

                        int PixelIndex;
                        Schedule.TrackDisplayIndexToPixelRobust(0, phantomInsertionPointIndex, out PixelIndex);
                        gc.graphics.SetClip(
                            new Rectangle(
                                PixelIndex - PixelIndent,
                                0,
                                1,
                                ClientSize.Height));
                        TrackViewRedrawAll();
                    }

                    gc.graphics.SetClip(ClientRectangle);
                }
            }
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);

            // Because of the odd way range selection is drawn, artifacts appear on the top and bottom of the
            // canvas during vertical scroll. Invalidate the top and bottom margins to clean them up.
            if ((se.ScrollOrientation == ScrollOrientation.VerticalScroll) && TrackViewIsARangeSelected())
            {
                using (GraphicsContext gc = new GraphicsContext(this))
                {
                    SetScrollOffsetsForRendering(gc.graphics);
                    Invalidate(new Rectangle(
                        PixelIndent,
                        (se.OldValue - se.NewValue),
                        ClientSize.Width,
                        TrackDisplayConstants.RANGESELECTTHICKNESS));
                    Invalidate(new Rectangle(
                        PixelIndent,
                        (se.OldValue - se.NewValue) + (ClientSize.Height - TrackDisplayConstants.RANGESELECTTHICKNESS),
                        ClientSize.Width,
                        TrackDisplayConstants.RANGESELECTTHICKNESS));
                }
            }

            Update(); // redraw scrolled-in area immediately to prevent artifacts

            if ((se.ScrollOrientation == ScrollOrientation.HorizontalScroll)
                && (trackObject.InlineParamVis != InlineParamVis.None))
            {
                noteParamStrip.Shift(se.OldValue - se.NewValue);
            }
        }

        private void TrackObjectAltered(TrackObjectRec trackObj, int startFrame, int endFrame)
        {
            Schedule.TrackDisplayScheduleMarkChanged(trackObject, startFrame);
        }

        private void TrackObjectAltered(TrackObjectRec trackObj, int frame)
        {
            TrackObjectAltered(trackObj, frame, trackObj.FrameArray.Count - 1);
        }

        public void TrackObjectAlteredAtSelection()
        {
            int index;
            switch (SelectionMode)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case SelectionModes.eTrackViewNoSelection:
                    index = InsertionPointIndex;
                    break;
                case SelectionModes.eTrackViewRangeSelection:
                    index = RangeSelectStart;
                    break;
                case SelectionModes.eTrackViewSingleCommandSelection:
                case SelectionModes.eTrackViewSingleNoteSelection:
                    index = SelectedNoteFrame;
                    break;
            }
            TrackObjectAltered(trackObject, index, index);
            Invalidate();
        }


        //

        public bool TryGetSelectedNote(out NoteObjectRec note)
        {
            note = null;
            if (SelectedNote != null)
            {
                note = SelectedNote;
                return true;
            }
            return false;
        }


        //

        private IGraphicsContext gc { get { return this; } }

        Graphics IGraphicsContext.graphics
        {
            get
            {
                if (contextGraphics == null)
                {
                    Debugger.Break();
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
                return contextGraphics;
            }
        }

        Font IGraphicsContext.font
        {
            get { return Font; }
        }

        Brush IGraphicsContext.BackBrush
        {
            get
            {
                if (backBrush == null)
                {
                    backBrush = new SolidBrush(BackColor);
                }
                return backBrush;
            }
        }

        Pen IGraphicsContext.BackPen
        {
            get
            {
                if (backPen == null)
                {
                    backPen = new Pen(BackColor);
                }
                return backPen;
            }
        }

        Brush IGraphicsContext.ForeBrush
        {
            get
            {
                if (foreBrush == null)
                {
                    foreBrush = new SolidBrush(ForeColor);
                }
                return foreBrush;
            }
        }

        Pen IGraphicsContext.ForePen
        {
            get
            {
                if (forePen == null)
                {
                    forePen = new Pen(ForeColor);
                }
                return forePen;
            }
        }

        Brush IGraphicsContext.GreyBrush
        {
            get
            {
                if (greyBrush == null)
                {
                    greyBrush = new SolidBrush(GreyColor);
                }
                return greyBrush;
            }
        }

        Pen IGraphicsContext.GreyPen
        {
            get
            {
                if (greyPen == null)
                {
                    greyPen = new Pen(GreyColor);
                }
                return greyPen;
            }
        }

        Brush IGraphicsContext.LightGreyBrush
        {
            get
            {
                if (lightGreyBrush == null)
                {
                    lightGreyBrush = new SolidBrush(LightGreyColor);
                }
                return lightGreyBrush;
            }
        }

        Pen IGraphicsContext.LightGreyPen
        {
            get
            {
                if (lightGreyPen == null)
                {
                    lightGreyPen = new Pen(LightGreyColor);
                }
                return lightGreyPen;
            }
        }

        Brush IGraphicsContext.LightLightGreyBrush
        {
            get
            {
                if (lightLightGreyBrush == null)
                {
                    lightLightGreyBrush = new SolidBrush(LightLightGreyColor);
                }
                return lightLightGreyBrush;
            }
        }


        private class GraphicsContext : IGraphicsContext, IDisposable
        {
            private readonly TrackViewControl view;
            private readonly Graphics previousGraphics;
            private readonly bool top;

            public GraphicsContext(TrackViewControl view)
            {
                if (view == null)
                {
                    Debug.Assert(false);
                    throw new ArgumentNullException();
                }
                this.view = view;
                previousGraphics = view.contextGraphics;
                if (top = (view.contextGraphics == null))
                {
                    view.contextGraphics = view.CreateGraphics();
                }
            }

            public GraphicsContext(TrackViewControl view, Graphics graphics)
            {
                if ((view == null) || (graphics == null))
                {
                    Debug.Assert(false);
                    throw new ArgumentNullException();
                }
                this.view = view;
                previousGraphics = view.contextGraphics;
                view.contextGraphics = graphics;
            }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Graphics graphics { get { return view.contextGraphics; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Font font { get { return view.Font; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Brush IGraphicsContext.BackBrush { get { return ((IGraphicsContext)view).BackBrush; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Pen IGraphicsContext.BackPen { get { return ((IGraphicsContext)view).BackPen; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Color IGraphicsContext.BackColor { get { return ((IGraphicsContext)view).BackColor; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Brush IGraphicsContext.ForeBrush { get { return ((IGraphicsContext)view).ForeBrush; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Pen IGraphicsContext.ForePen { get { return ((IGraphicsContext)view).ForePen; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Color IGraphicsContext.ForeColor { get { return ((IGraphicsContext)view).ForeColor; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Brush IGraphicsContext.GreyBrush { get { return ((IGraphicsContext)view).GreyBrush; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Pen IGraphicsContext.GreyPen { get { return ((IGraphicsContext)view).GreyPen; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Color IGraphicsContext.GreyColor { get { return ((IGraphicsContext)view).GreyColor; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Brush IGraphicsContext.LightGreyBrush { get { return ((IGraphicsContext)view).LightGreyBrush; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Pen IGraphicsContext.LightGreyPen { get { return ((IGraphicsContext)view).LightGreyPen; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Color IGraphicsContext.LightGreyColor { get { return ((IGraphicsContext)view).LightGreyColor; } }

            [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            Brush IGraphicsContext.LightLightGreyBrush { get { return ((IGraphicsContext)view).LightLightGreyBrush; } }

            public void Dispose()
            {
                if (top)
                {
                    view.contextGraphics.Dispose();
                }
                view.contextGraphics = previousGraphics;
            }
        }


        // Undo

        public IUndoUnit CaptureCurrentStateForUndo()
        {
            return new TrackUndoRec(this);
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UndoHelper UndoHelper { get { return undoHelper; } }

        private class TrackUndoRec : IUndoUnit, IDisposable
        {
            // saved selection info
            private readonly SelectionModes SelectionMode;
            private readonly int InsertionPointIndex;
            private readonly int SelectedNoteIndex;
            private readonly int SelectedNoteFrame;
            private readonly int RangeSelectStart;
            private readonly int RangeSelectEnd;
            private readonly bool RangeSelectStartIsActive;

            // saved data
            private readonly FileStream persistedTrack;

            public TrackUndoRec(
                TrackViewControl trackView)
            {
                this.SelectionMode = trackView.SelectionMode;
                this.InsertionPointIndex = trackView.InsertionPointIndex;
                this.SelectedNoteFrame = trackView.SelectedNoteFrame;
                if ((trackView.SelectionMode == SelectionModes.eTrackViewSingleNoteSelection)
                    || (trackView.SelectionMode == SelectionModes.eTrackViewSingleCommandSelection))
                {
                    this.SelectedNoteIndex = trackView.trackObject.FrameArray[trackView.SelectedNoteFrame].IndexOf(trackView.SelectedNote);
                }
                else
                {
                    this.SelectedNoteIndex = -1;
                }
                this.RangeSelectStart = trackView.RangeSelectStart;
                this.RangeSelectEnd = trackView.RangeSelectEnd;
                this.RangeSelectStartIsActive = trackView.RangeSelectStartIsActive;

                persistedTrack = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, Constants.BufferSize, FileOptions.DeleteOnClose);
                using (BinaryWriter writer = new BinaryWriter(persistedTrack))
                {
                    trackView.trackObject.WriteNoteVector(writer);
                }
            }

            public void Do(IUndoClient client)
            {
                TrackViewControl trackView = (TrackViewControl)client;
                using (GraphicsContext gc = new GraphicsContext(trackView))
                {
                    // reconstitute saved data
                    trackView.trackObject.FrameArray.Clear();
                    persistedTrack.Seek(0, SeekOrigin.Begin);
                    using (BinaryReader reader = new BinaryReader(persistedTrack))
                    {
                        Document document = (Document)trackView.trackObject.Parent;
                        trackView.trackObject.ReadNoteVector(reader, new LoadContext(Document.CurrentFormatVersionNumber, document, LoadContextState.Paste));
                    }

                    // restore selection
                    trackView.SelectionMode = this.SelectionMode;
                    trackView.InsertionPointIndex = this.InsertionPointIndex;
                    trackView.SelectedNoteFrame = this.SelectedNoteFrame;
                    if (this.SelectedNoteIndex >= 0)
                    {
                        trackView.SelectedNote = trackView.trackObject.FrameArray[this.SelectedNoteFrame][this.SelectedNoteIndex];
                    }
                    trackView.RangeSelectStart = this.RangeSelectStart;
                    trackView.RangeSelectEnd = this.RangeSelectEnd;
                    trackView.RangeSelectStartIsActive = this.RangeSelectStartIsActive;

                    // redraw
                    trackView.TrackObjectAltered(trackView.trackObject, 0);
                    trackView.TrackViewShowSelection();
                    trackView.TrackViewRedrawAll();
                    if (trackView.trackObject.InlineParamVis != InlineParamVis.None)
                    {
                        trackView.RebuildInlineStrip();
                    }
                }
            }

            public void Dispose()
            {
                persistedTrack.Dispose();
            }
        }


        //

        private bool expandedWidths;
        public void RebuildInlineStrip()
        {
            if (noteParamStrip != null)
            {
                using (GraphicsContext gc = new GraphicsContext(this))
                {
                    // if CurrentInternalSeparation/CurrentExternalSeparation is changing, invalidate entire layout
                    if (expandedWidths != (trackObject.InlineParamVis != InlineParamVis.None))
                    {
                        expandedWidths = trackObject.InlineParamVis != InlineParamVis.None;
                        RebuildSchedule();
                        TrackObjectAltered(trackObject, 0);
                        Invalidate();
                    }

                    // rebuild parameter strip data array
                    // TODO: provisional - inefficient
                    Schedule.TrackDisplayScheduleUpdate();
                    noteParamStrip.Clear();
                    for (int frameIndex = 0; frameIndex < trackObject.FrameArray.Count; frameIndex++)
                    {
                        for (int noteIndex = 0; noteIndex < trackObject.FrameArray[frameIndex].Count; noteIndex++)
                        {
                            if (!trackObject.FrameArray[frameIndex].IsThisACommandFrame)
                            {
                                int PixelIndex;
                                Schedule.TrackDisplayIndexToPixel(0, frameIndex, out PixelIndex);
                                noteParamStrip.Add(
                                    PixelIndex - PixelIndent + Schedule.TrackDisplayGetNoteOffset(0, frameIndex, noteIndex),
                                    (NoteNoteObjectRec)trackObject.FrameArray[frameIndex][noteIndex],
                                    gc.graphics);
                            }
                        }
                    }
                }
            }
        }
    }

    public interface IGraphicsContext
    {
        Graphics graphics { get; }
        Font font { get; }

        Brush BackBrush { get; }
        Pen BackPen { get; }
        Color BackColor { get; }
        Brush ForeBrush { get; }
        Pen ForePen { get; }
        Color ForeColor { get; }
        Brush GreyBrush { get; }
        Pen GreyPen { get; }
        Color GreyColor { get; }
        Brush LightGreyBrush { get; }
        Pen LightGreyPen { get; }
        Color LightGreyColor { get; }
        Brush LightLightGreyBrush { get; }
    }

    public interface ITrackViewContextUI
    {
        bool NoteReady { get; set; }
        NoteFlags NoteState { get; set; }

        void EditNoteProperties();
        MainWindow MainWindow { get; }
    }

    enum SelectionModes
    {
        eTrackViewNoSelection,
        eTrackViewSingleNoteSelection,
        eTrackViewSingleCommandSelection,
        eTrackViewRangeSelection,
    }


    public static class TrackDisplayConstants
    {
        /* where does the very first note start (to allow insertion before it) */
        public const int FIRSTNOTESTART = 32;

        /* how much space between notes */
        public const int EXTERNALSEPARATION = 16;

        /* so that tips of notes can be seen above last staff line */
        public const int EDGESPACER = 96;

        /* so that you have space to add new notes */
        public const int HORIZONTALEXTENTION = 200;

        /* width of the insertion point */
        public const int INSERTIONPOINTWIDTH = 2;

        /* tie endpoint positioning correction values */
        public const int TIESTARTXCORRECTION = 11;
        public const int TIESTARTYCORRECTION = 9;
        public const int TIEENDXCORRECTION = 4;
        public const int TIEENDYCORRECTION = 9;

        /* tie curve drawing parameters */
        public const int NUMTIELINEINTERVALS = 10;
        public const int MAXTIEDEPTH = 15;

        /* the width of the box drawn for range selection */
        public const int RANGESELECTTHICKNESS = 3;

        /* how far does mouse need to move to release the cursor bar lock */
        public const int CURSORBARRELEASEDELTA = 2;

        // sentinal value
        public const int NOMEASUREBAR = Int32.MinValue;

        public const int ORDTYPEMIN = Int16.MinValue;
        public const int ORDTYPEMAX = Int16.MaxValue;
    }

    public static class FrameDisplayConstants
    {
        /* this is how much space to put between notes in the same frame */
        public const int INTERNALSEPARATION = 12;

        /* this is the width of the note part of an icon */
        public const int ICONWIDTH = 20;

        /* how much from the top of the note to the staff line intersection point */
        public const int TOPNOTESTAFFINTERSECT = 23;

        /* how much from the starting edge of the note icon does the note really start */
        public const int LEFTNOTEEDGEINSET = 6;

        // border width for drawing command boxes
        public const int BORDER = 3;
    }

    public static class StaffCalibration
    {
        /* a note line every 4 scan lines (a note line is 1/2 of a staff line, or */
        /* a line on which a note may be plotted) */
        public const int STAFFSEPARATION = 4;

        /* total number of pixels that are in the drawing range */
        public const int TOTALPIXELS = (Constants.NUMNOTES / 12) * 7 * STAFFSEPARATION;


        /* get the maximum number of vertical pixels needed to represent score range */
        public static int MaxVerticalSize { get { return TOTALPIXELS; } }

        /* get the pixel offset of the center note */
        public static int CenterNotePixel { get { return (TOTALPIXELS - 1) - ((Constants.CENTERNOTE / 12) * (7 * STAFFSEPARATION)); } }

        /* convert pitch to vertical pixel offset. */
        public static int ConvertPitchToPixel(int HalfStep, NoteFlags SharpFlatFlags)
        {
            if ((SharpFlatFlags & ~(NoteFlags.eFlatModifier | NoteFlags.eSharpModifier)) != 0)
            {
                // extraneous bits in SharpFlatFlags
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((HalfStep < 0) || (HalfStep > Constants.NUMNOTES - 1))
            {
                // pitch index is out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }

            int Octave = HalfStep / 12;
            int NoteOffset;
            switch (HalfStep % 12)
            {
                default:
                    throw new ArgumentException();
                case 0: /* B#/C */
                    if ((SharpFlatFlags & NoteFlags.eSharpModifier) != 0)
                    {
                        NoteOffset = -1 * STAFFSEPARATION;
                    }
                    else
                    {
                        NoteOffset = 0 * STAFFSEPARATION;
                    }
                    break;
                case 1: /* C#/Db */
                    if ((SharpFlatFlags & NoteFlags.eSharpModifier) != 0)
                    {
                        NoteOffset = 0 * STAFFSEPARATION;
                    }
                    else
                    {
                        NoteOffset = 1 * STAFFSEPARATION;
                    }
                    break;
                case 2: /* D */
                    NoteOffset = 1 * STAFFSEPARATION;
                    break;
                case 3: /* D#/Eb */
                    if ((SharpFlatFlags & NoteFlags.eSharpModifier) != 0)
                    {
                        NoteOffset = 1 * STAFFSEPARATION;
                    }
                    else
                    {
                        NoteOffset = 2 * STAFFSEPARATION;
                    }
                    break;
                case 4: /* E/Fb */
                    if ((SharpFlatFlags & NoteFlags.eFlatModifier) != 0)
                    {
                        NoteOffset = 3 * STAFFSEPARATION;
                    }
                    else
                    {
                        NoteOffset = 2 * STAFFSEPARATION;
                    }
                    break;
                case 5: /* E#/F */
                    if ((SharpFlatFlags & NoteFlags.eSharpModifier) != 0)
                    {
                        NoteOffset = 2 * STAFFSEPARATION;
                    }
                    else
                    {
                        NoteOffset = 3 * STAFFSEPARATION;
                    }
                    break;
                case 6: /* F#/Gb */
                    if ((SharpFlatFlags & NoteFlags.eSharpModifier) != 0)
                    {
                        NoteOffset = 3 * STAFFSEPARATION;
                    }
                    else
                    {
                        NoteOffset = 4 * STAFFSEPARATION;
                    }
                    break;
                case 7: /* G */
                    NoteOffset = 4 * STAFFSEPARATION;
                    break;
                case 8: /* G#/Ab */
                    if ((SharpFlatFlags & NoteFlags.eSharpModifier) != 0)
                    {
                        NoteOffset = 4 * STAFFSEPARATION;
                    }
                    else
                    {
                        NoteOffset = 5 * STAFFSEPARATION;
                    }
                    break;
                case 9: /* A */
                    NoteOffset = 5 * STAFFSEPARATION;
                    break;
                case 10: /* A#/Bb */
                    if ((SharpFlatFlags & NoteFlags.eSharpModifier) != 0)
                    {
                        NoteOffset = 5 * STAFFSEPARATION;
                    }
                    else
                    {
                        NoteOffset = 6 * STAFFSEPARATION;
                    }
                    break;
                case 11: /* B/Cb */
                    if ((SharpFlatFlags & NoteFlags.eFlatModifier) != 0)
                    {
                        NoteOffset = 7 * STAFFSEPARATION;
                    }
                    else
                    {
                        NoteOffset = 6 * STAFFSEPARATION;
                    }
                    break;
            }
            NoteOffset += 7 * STAFFSEPARATION * Octave;
            return (TOTALPIXELS - 1) - NoteOffset;
        }

        private static readonly int[] OneOctaveHalfStepTable = new int[]
        {
            0, /* pixel 0 == C */
		    2, /* pixel 4 == D */
		    4, /* pixel 8 == E */
		    5, /* pixel 12 == F */
		    7, /* pixel 16 == G */
		    9, /* pixel 20 == A */
		    11 /* pixel 24 == B */
	    };

        /* convert pixel offset to halfstep value */
        public static short ConvertPixelToPitch(int Pixel)
        {
            Pixel = ((TOTALPIXELS - 1) - Pixel + 1) / STAFFSEPARATION;
            if (Pixel < 0)
            {
                Pixel = 0;
            }
            int OctaveCount = 0;
            while (Pixel >= OneOctaveHalfStepTable.Length)
            {
                Pixel -= OneOctaveHalfStepTable.Length;
                OctaveCount++;
            }
            int ReturnValue = OctaveCount * 12 + OneOctaveHalfStepTable[Pixel];
            if (ReturnValue > Constants.NUMNOTES - 1)
            {
                ReturnValue = Constants.NUMNOTES - 1;
            }
            return (short)ReturnValue;
        }

        public static readonly int[] MajorStaffList = new int[]
        {
            Constants.CENTERNOTE + 4, /* E */
		    Constants.CENTERNOTE + 7, /* G */
		    Constants.CENTERNOTE + 11, /* B */
		    Constants.CENTERNOTE + 14, /* D */
		    Constants.CENTERNOTE + 17, /* F */
		    Constants.CENTERNOTE - 4, /* A */
		    Constants.CENTERNOTE - 7, /* F */
		    Constants.CENTERNOTE - 11, /* D */
		    Constants.CENTERNOTE - 14, /* B */
		    Constants.CENTERNOTE - 17 /* G */
	    };

        private static readonly int[] TwoOctaveStaffTable = new int[]
        {
            0, /* C0 */
		    4, /* E0 */
		    7, /* G0 */
		    11, /* B0 */
		    14, /* D1 */
		    17, /* F1 */
		    21 /* A1 */
	    };

        /* get dynamically allocated list of minor staff lines */
        public static readonly int[] MinorStaffList = GetMinorStaffList();
        private static int[] GetMinorStaffList()
        {
            List<int> Table = new List<int>();
            for (int HalfStep = 0; HalfStep < Constants.NUMNOTES; HalfStep++)
            {
                if ((HalfStep < Constants.CENTERNOTE - 17) || (HalfStep > Constants.CENTERNOTE + 17))
                {
                    int TwoOctRelative = HalfStep - Constants.CENTERNOTE;
                    while (TwoOctRelative < 0)
                    {
                        TwoOctRelative += 24;
                    }
                    while (TwoOctRelative >= 24)
                    {
                        TwoOctRelative -= 24;
                    }
                    for (int i = 0; i < TwoOctaveStaffTable.Length; i++)
                    {
                        if (TwoOctRelative == TwoOctaveStaffTable[i])
                        {
                            Table.Add(HalfStep);
                        }
                    }
                }
            }
            return Table.ToArray();
        }

        /* set up note */
        public static void SetUpNoteInfo(out short Pitch, out NoteFlags SharpFlatThing, bool Sharp, bool Flat, int Pixel)
        {
            SharpFlatThing = 0;
            Pitch = ConvertPixelToPitch(Pixel);
            if (Sharp)
            {
                switch (Pitch % 12)
                {
                    case 0: /* C */
                    case 2: /* D */
                    case 5: /* F */
                    case 7: /* G */
                    case 9: /* A */
                        Pitch++;
                        SharpFlatThing |= NoteFlags.eSharpModifier;
                        break;
                    case 4: /* E */
                    case 11: /* B */
                        Pitch++;
                        break;
                    default:
                        /* ? */
                        break;
                }
            }
            if (Flat)
            {
                switch (Pitch % 12)
                {
                    case 2: /* D */
                    case 4: /* E */
                    case 7: /* G */
                    case 9: /* A */
                    case 11: /* B */
                        Pitch--;
                        SharpFlatThing |= NoteFlags.eFlatModifier;
                        break;
                    case 0: /* C */
                    case 5: /* F */
                        Pitch--;
                        break;
                    default:
                        /* ? */
                        break;
                }
            }
            if (Pitch < 0)
            {
                Pitch = 0;
            }
            if (Pitch > Constants.NUMNOTES - 1)
            {
                Pitch = Constants.NUMNOTES - 1;
            }
        }
    }


    public class TrackDispScheduleRec
    {
        private readonly IGraphicsContext gc;
        private readonly TrackViewControl ownerView;

        private struct FrameAttrRec
        {
            public int PixelStart;
            public int Width;
            public bool SquashThisOne; /* True == don't draw this one */

            public int widthIncludingTrailingSpace;
            public NoteMetrics[] metrics;
        }

        public struct NoteMetrics
        {
            public short offset;
            public short width;
        }

        private struct TrackAttrRec
        {
            public TrackObjectRec TrackObj;
            public int NumFrames;
            public FrameAttrRec[] FrameAttrArray;

            public TrackAttrRec(TrackObjectRec TrackObj, int NumFrames, FrameAttrRec[] FrameAttrArray)
            {
                this.TrackObj = TrackObj;
                this.NumFrames = NumFrames;
                this.FrameAttrArray = FrameAttrArray;
            }
        }

        public struct MeasureBarInfoRec
        {
            public int BarIndex; /* NOMEASUREBAR == no bar */
            public bool DrawBarGreyed; /* True == draw bar greyed */
        }

        /* the first track (TrackAttrArray[0]) is the "main" track */
        private TrackAttrRec[] TrackAttrArray = new TrackAttrRec[0];
        private MeasureBarInfoRec[] MainTrackMeasureBars = new MeasureBarInfoRec[0];
        private int totalWidth;
        private bool RecalculationRequired = true;
        private TieTrackPixelRec TiePixelTracker; /* NIL if not up to date */
        private InlineParamVis inlineParamVis;

        public event EventHandler TrackExtentsChanged; // fired after recalc

        public TrackDispScheduleRec(IGraphicsContext gc, TrackViewControl ownerView)
        {
            this.gc = gc;
            this.ownerView = ownerView;
        }

        public void Add(TrackObjectRec TrackObj)
        {
            if (Array.FindIndex(TrackAttrArray, delegate (TrackAttrRec candidate) { return candidate.TrackObj == TrackObj; }) >= 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            if (TrackAttrArray.Length == 0)
            {
                inlineParamVis = TrackObj.InlineParamVis;
            }

            Array.Resize(ref TrackAttrArray, TrackAttrArray.Length + 1);
            TrackAttrArray[TrackAttrArray.Length - 1] = new TrackAttrRec(TrackObj, 0, new FrameAttrRec[0]);

            RecalculationRequired = true;
        }

        public void Remove(TrackObjectRec TrackObj)
        {
            int i = Array.FindIndex(TrackAttrArray, delegate (TrackAttrRec candidate) { return candidate.TrackObj == TrackObj; });
            if (i < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (i == 0)
            {
                // deleting the main track
                Debug.Assert(false);
                throw new ArgumentException();
            }

            Array.Copy(TrackAttrArray, i, TrackAttrArray, i + 1, TrackAttrArray.Length - (i + 1));
            Array.Resize(ref TrackAttrArray, TrackAttrArray.Length - 1);

            RecalculationRequired = true;
        }

        public int TotalWidth
        {
            get
            {
                TrackDisplayScheduleUpdate();
                return totalWidth;
            }
        }

        /* note location information must be available by the time this is called. */
        public void BuildTieRepresentation()
        {
            TrackObjectRec TrackObj = TrackAttrArray[0].TrackObj;
            if (TiePixelTracker == null)
            {
                throw new InvalidOperationException();
            }

            TieTrackRec TieTracker = new TieTrackRec();

            int FrameLimit = TrackObj.FrameArray.Count;
            for (int iFrame = 0; iFrame < FrameLimit; iFrame++)
            {
                FrameObjectRec Frame = TrackObj.FrameArray[iFrame];
                if (!Frame.IsThisACommandFrame)
                {
                    int NoteLimit = Frame.Count;
                    for (int iNote = 0; iNote < NoteLimit; iNote++)
                    {
                        NoteNoteObjectRec Note = (NoteNoteObjectRec)Frame[iNote];

                        NoteNoteObjectRec TieTargetNote = Note.GetNoteTieTarget();

                        /* get any note tied to it */
                        int PreviousTiePixelX;
                        int PreviousTiePixelY;
                        NoteNoteObjectRec PreviousTieNote;
                        bool ThereIsATieToThisNote = TieTracker.GetTieSourceFromDestination(out PreviousTiePixelX, out PreviousTiePixelY, out PreviousTieNote, Note);

                        /* now, deal with all of this jucy information */
                        /* first, get the location of this note, if needed */
                        if ((TieTargetNote != null) || ThereIsATieToThisNote)
                        {
                            /* gather location information */
                            int ThisNotePixelX;
                            TrackDisplayIndexToPixel(0/*main track*/, iFrame, out ThisNotePixelX);
                            ThisNotePixelX += TrackDisplayGetNoteOffset(0, iFrame, iNote);
                            int ThisNotePixelY = StaffCalibration.ConvertPitchToPixel(Note.GetNotePitch(), Note.GetNoteFlatOrSharpStatus());

                            /* do the thing for a note tied to us */
                            if (ThereIsATieToThisNote)
                            {
                                TiePixelTracker.Add(PreviousTiePixelX, PreviousTiePixelY, ThisNotePixelX, ThisNotePixelY);
                            }

                            /* do the thing for us tied to another note */
                            if (TieTargetNote != null)
                            {
                                TieTracker.AddTiePair(Note, ThisNotePixelX, ThisNotePixelY, TieTargetNote);
                            }
                        }
                    }
                }
            }
        }

        /* calculate the pixel index for a frame based on it's array index */
        public void TrackDisplayIndexToPixel(int TrackIndex, int FrameIndex, out int Pixel)
        {
            TrackDisplayScheduleUpdate();
            Debug.Assert(unchecked((uint)TrackIndex < (uint)TrackAttrArray.Length));
            Debug.Assert(unchecked((uint)FrameIndex < (uint)TrackAttrArray[TrackIndex].FrameAttrArray.Length));
            Pixel = TrackAttrArray[TrackIndex].FrameAttrArray[FrameIndex].PixelStart;
        }

        // calculate the pixel index for a frame based on it's array index.
        // this version permits FrameIndex == Count and does the right thing.
        public void TrackDisplayIndexToPixelRobust(int TrackIndex, int FrameIndex, out int Pixel)
        {
            TrackDisplayScheduleUpdate();
            Debug.Assert(unchecked((uint)TrackIndex < (uint)TrackAttrArray.Length));
            //Debug.Assert(unchecked((uint)FrameIndex <= (uint)TrackAttrArray[TrackIndex].FrameAttrArray.Length));
            if (FrameIndex < (uint)TrackAttrArray[TrackIndex].FrameAttrArray.Length)
            {
                Pixel = TrackAttrArray[TrackIndex].FrameAttrArray[FrameIndex].PixelStart;
            }
            else if (TrackAttrArray[TrackIndex].FrameAttrArray.Length > 0)
            {
                int end = TrackAttrArray[TrackIndex].FrameAttrArray.Length - 1;
                Pixel = TrackAttrArray[TrackIndex].FrameAttrArray[end].PixelStart
                    + TrackAttrArray[TrackIndex].FrameAttrArray[end].widthIncludingTrailingSpace;
            }
            else
            {
                Pixel = 0;
            }
        }

        private struct TrackWorkRec
        {
            public int CurrentIndex;
            public FractionRec CurrentFrameStartTime;
            public FractionRec CurrentFrameDuration;

            public TrackWorkRec(int CurrentIndex, FractionRec CurrentFrameStartTime, FractionRec CurrentFrameDuration)
            {
                this.CurrentIndex = CurrentIndex;
                this.CurrentFrameStartTime = CurrentFrameStartTime;
                this.CurrentFrameDuration = CurrentFrameDuration;
            }
        }

        /* apply schedule to tracks. */
        // return value is true if positions were recalculated (may not have changed), false if nothing changed
        public bool TrackDisplayScheduleUpdate()
        {
            /* if everything is up to date, then don't bother */
            if (!RecalculationRequired)
            {
                return false;
            }

            TiePixelTracker = new TieTrackPixelRec();

            /* resize the measure bar table */
            {
                int i = TrackAttrArray[0].TrackObj.FrameArray.Count;
                MainTrackMeasureBars = new MeasureBarInfoRec[i];
                while (i > 0)
                {
                    /* erase the entries in the table */
                    i--;
                    MainTrackMeasureBars[i].BarIndex = TrackDisplayConstants.NOMEASUREBAR; /* no bar */
                }
            }

            /* resize the track location tables */
            for (int i = 0; i < TrackAttrArray.Length; i++)
            {
                int TheirFramesTemp = TrackAttrArray[i].TrackObj.FrameArray.Count;
                if (TrackAttrArray[i].NumFrames != TheirFramesTemp)
                {
                    Array.Resize(ref TrackAttrArray[i].FrameAttrArray, TheirFramesTemp);

                    /* zero the new space in the array so that we make sure it changes */
                    for (int Index = TrackAttrArray[i].NumFrames; Index < TheirFramesTemp; Index++)
                    {
                        TrackAttrArray[i].FrameAttrArray[Index].PixelStart = -1;
                        TrackAttrArray[i].FrameAttrArray[Index].Width = -1;
                        TrackAttrArray[i].FrameAttrArray[Index].SquashThisOne = false;
                    }

                    /* update size number */
                    TrackAttrArray[i].NumFrames = TheirFramesTemp;
                }
            }


            /* initialize local variable counters */
            int CurrentXLocation = TrackDisplayConstants.FIRSTNOTESTART;
            FractionRec CurrentTime = new FractionRec(0, 0, Constants.Denominator);

            FractionRec MeasureBarIntervalThing = new FractionRec(0, 0, Constants.Denominator);
            FractionRec MeasureBarWidth = new FractionRec(1, 0, Constants.Denominator);
            int MeasureBarIndex = 1;


            /* build workspace for each track */
            TrackWorkRec[] WorkArray = new TrackWorkRec[TrackAttrArray.Length];
            for (int i = 0; i < TrackAttrArray.Length; i++)
            {
                WorkArray[i].CurrentIndex = 0;
                WorkArray[i].CurrentFrameStartTime = new FractionRec(0, 0, Constants.Denominator);
            }


            /* perform scheduling sweep */
            /* how it works: */
            /* the start time and duration of the next frame in each channel are */
            /* calculated.  If the start time is NOT in the future, but is now, then */
            /* the note can be scheduled.  When it is scheduled, it's start time is */
            /* advanced by adding it's duration.  This way, notes will be scheduled */
            /* for display on the screen in the same order that they will be played. */
            bool DoneFlag;
            do
            {
                /* stage 1:  calculate duration of next event for all frames */
                /* This also figures out what the smallest duration is */
                /* After stage 1, Frame will be NIL if the channel is not scheduled */
                /* this time around, or valid if it contains the one to schedule */
                int MaximumWidth = 0;
                for (int trackIndex = 0; trackIndex < TrackAttrArray.Length; trackIndex++)
                {
                    if (WorkArray[trackIndex].CurrentIndex < TrackAttrArray[trackIndex].NumFrames)
                    {
                        /* this channel still has more items to schedule. */
                        /* we want to see if this frame will be scheduled this time. */
                        if (FractionRec.FracGreaterThan(CurrentTime, WorkArray[trackIndex].CurrentFrameStartTime))
                        {
                            // current time later than frame start
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        }

                        if (FractionRec.FractionsEqual(CurrentTime, WorkArray[trackIndex].CurrentFrameStartTime))
                        {
                            /* the frame is scheduled this time around */

                            /* obtain the frame reference */
                            FrameObjectRec Frame = TrackAttrArray[trackIndex].TrackObj.FrameArray[WorkArray[trackIndex].CurrentIndex];

                            /* get duration of this frame */
                            Frame.DurationOfFrame(out WorkArray[trackIndex].CurrentFrameDuration);

                            /* decide whether the measure bar flag should be set. */
                            /* this is only done for main track, hence "trackIndex == 0". */
                            if (trackIndex == 0)
                            {
                                /* for big notes (like quads) this may increment */
                                /* MeasureBarIndex several times, but that's the desired */
                                /* behavior. */
                                while (FractionRec.FracGreaterEqual(MeasureBarIntervalThing, MeasureBarWidth))
                                {
                                    /* if the current measure bar index is greater than the */
                                    /* measure bar interval, then set the flag & decrement */
                                    /* the measure bar interval. */
                                    /*  MeasureBarWidth = num whole notes in a measure */
                                    /*  MeasureBarIntervalThing = current accumulated notes */
                                    bool GreyedFlag = !FractionRec.FractionsEqual(MeasureBarIntervalThing, MeasureBarWidth);
                                    FractionRec.SubFractions(MeasureBarIntervalThing, MeasureBarWidth, out MeasureBarIntervalThing);
                                    MainTrackMeasureBars[WorkArray[trackIndex].CurrentIndex].BarIndex = MeasureBarIndex;
                                    MainTrackMeasureBars[WorkArray[trackIndex].CurrentIndex].DrawBarGreyed = GreyedFlag;
                                    MeasureBarIndex++;
                                }

                                /* now, increment the value with the current note */
                                FractionRec.AddFractions(MeasureBarIntervalThing, WorkArray[trackIndex].CurrentFrameDuration, out MeasureBarIntervalThing);

                                /* finally, if the note is a meter adjust command, use */
                                /* it to recalibrate the measure barring */
                                if (Frame.IsThisACommandFrame)
                                {
                                    CommandNoteObjectRec MaybeMeterCmd = (CommandNoteObjectRec)Frame[0];
                                    if (MaybeMeterCmd.GetCommandOpcode() == NoteCommands.eCmdSetMeter)
                                    {
                                        /* set the meter.  this is used by the editor for */
                                        /* placing measure bars.  measuring restarts */
                                        /* immediately after this command */
                                        /* <1i> = numerator, <2i> = denominator */
                                        if (MaybeMeterCmd.GetCommandNumericArg2() >= 1)
                                        {
                                            /* make sure denominator is within range */
                                            if (MaybeMeterCmd.GetCommandNumericArg1() >= 1)
                                            {
                                                /* set the new measure width */
                                                uint denominator = (uint)MaybeMeterCmd.GetCommandNumericArg2();
                                                MeasureBarWidth = new FractionRec(
                                                    (uint)MaybeMeterCmd.GetCommandNumericArg1() / denominator,
                                                    (uint)MaybeMeterCmd.GetCommandNumericArg1() % denominator,
                                                    denominator);

                                                /* reset the current index */
                                                MeasureBarIntervalThing = new FractionRec(0, 0, Constants.Denominator);
                                            }
                                        }
                                    }
                                    else if (MaybeMeterCmd.GetCommandOpcode() == NoteCommands.eCmdSetMeasureNumber)
                                    {
                                        MeasureBarIndex = MaybeMeterCmd.GetCommandNumericArg1();
                                    }
                                }
                            }

                            /* now that we have the frame's duration and have it scheduled */
                            /* for display (Frame), increment the start */
                            /* time to the end of the frame (which is start of next frame) */
                            FractionRec.AddFractions(WorkArray[trackIndex].CurrentFrameDuration, WorkArray[trackIndex].CurrentFrameStartTime, out WorkArray[trackIndex].CurrentFrameStartTime);

                            int currentTrackFrameIndex = WorkArray[trackIndex].CurrentIndex;
                            FrameAttrRec[] currentTrackFrameAttrArray = TrackAttrArray[trackIndex].FrameAttrArray;
                            if ((trackIndex == 0) || !Frame.IsThisACommandFrame)
                            {
                                /* if this is NOT a command frame, then it should be scheduled */
                                /* normally for display. */

                                /* set up the drawing attributes */

                                if (currentTrackFrameAttrArray[currentTrackFrameIndex].metrics == null)
                                {
                                    currentTrackFrameAttrArray[currentTrackFrameIndex].metrics = new NoteMetrics[Frame.Count];
                                }

                                currentTrackFrameAttrArray[currentTrackFrameIndex].PixelStart = CurrentXLocation;
                                int width = FrameDrawUtility.WidthOfFrameAndDraw(
                                    gc,
                                    0,
                                    0,
                                    Frame,
                                    false/*don't draw*/,
                                    false,
                                    inlineParamVis,
                                    ref currentTrackFrameAttrArray[currentTrackFrameIndex].metrics);
                                currentTrackFrameAttrArray[currentTrackFrameIndex].Width = width;
                                currentTrackFrameAttrArray[currentTrackFrameIndex].widthIncludingTrailingSpace
                                    = width + TrackDisplayConstants.EXTERNALSEPARATION;
                                currentTrackFrameAttrArray[currentTrackFrameIndex].SquashThisOne = false;

                                if (currentTrackFrameAttrArray[currentTrackFrameIndex].Width == 0)
                                {
                                    // frame's width is 0
                                    Debug.Assert(false);
                                    throw new InvalidOperationException();
                                }

                                /* we need to obtain the maximum width of these frame so we */
                                /* can figure out what the next X location will be */
                                if (MaximumWidth < currentTrackFrameAttrArray[currentTrackFrameIndex].Width)
                                {
                                    MaximumWidth = currentTrackFrameAttrArray[currentTrackFrameIndex].Width;
                                }
                            }
                            else
                            {
                                /* otherwise this IS a command frame in a non-main track */
                                /* and therefore it should not be scheduled */

                                /* adjust the displaylocation parameters */

                                currentTrackFrameAttrArray[currentTrackFrameIndex].PixelStart = CurrentXLocation;
                                currentTrackFrameAttrArray[currentTrackFrameIndex].Width = 0; /* no width! */
                                currentTrackFrameAttrArray[currentTrackFrameIndex].SquashThisOne = true; /*squish*/
                                currentTrackFrameAttrArray[currentTrackFrameIndex].widthIncludingTrailingSpace = 0;
                                currentTrackFrameAttrArray[currentTrackFrameIndex].metrics = null;
                            }

                            /* advance frame pointer to the next frame in the channel */
                            WorkArray[trackIndex].CurrentIndex++;
                        }
                    }
                }
                /* advance current X position using biggest width at this position */
                if (MaximumWidth != 0)
                {
                    /* MaximumWidth != 0 only happens if some stuff was scheduled */
                    CurrentXLocation += MaximumWidth + TrackDisplayConstants.EXTERNALSEPARATION;
                }


                /* stage 2:  see if we are totally done with the tracks */
                DoneFlag = true;
                for (int i = 0; i < TrackAttrArray.Length; i++)
                {
                    if (WorkArray[i].CurrentIndex < TrackAttrArray[i].NumFrames)
                    {
                        /* this channel still has more items to schedule. */
                        DoneFlag = false;
                    }
                }


                /* stage 3:  advance the current time counter to point to the start time */
                /* of the closest possible next frame, which corresponds to the soonest one */
                /* immediately after the smallest frame that we just scheduled. */
                /* if we aren't done, figure out when the soonest next frame starts */
                if (!DoneFlag)
                {
                    FractionRec SmallestDelta = new FractionRec();

                    /* but there's still more to go, so advance the time counter to */
                    /* the closest possible next frame. */
                    /* This will happen if a very small note ends a track.  The duration */
                    /* of the small note will be added, but since the track is done, */
                    /* there might not be any note whatsoever at that time.  In this */
                    /* case, we need to find when the next note will be. */
                    /* In order to do this, we search for the soonest possible frame */
                    /* that we can find that is past CurrentTime. */
                    bool SmallestDeltaValid = false;
                    for (int i = 0; i < TrackAttrArray.Length; i++)
                    {
                        if (WorkArray[i].CurrentIndex < TrackAttrArray[i].NumFrames)
                        {
                            /* start time of next frame should NEVER be less than */
                            /* current time. */
                            if (FractionRec.FracGreaterEqual(CurrentTime, WorkArray[i].CurrentFrameStartTime)
                                && !FractionRec.FractionsEqual(CurrentTime, WorkArray[i].CurrentFrameStartTime))
                            {
                                // next less than current time
                                Debug.Assert(false);
                                throw new InvalidOperationException();
                            }

                            /* figure out difference between then and now */
                            FractionRec DeltaTemp;
                            FractionRec.SubFractions(WorkArray[i].CurrentFrameStartTime, CurrentTime, out DeltaTemp);
                            if (!SmallestDeltaValid || FractionRec.FracGreaterThan(SmallestDelta, DeltaTemp))
                            {
                                SmallestDelta = DeltaTemp;
                                SmallestDeltaValid = true;
                            }
                        }
                    }


                    /* if SmallestDeltaValid is False at this point, then there aren't */
                    /* any things that can be done, which should never happen. */
                    if (!SmallestDeltaValid)
                    {
                        // no track, but DoneFlag is false
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    FractionRec.AddFractions(SmallestDelta, CurrentTime, out CurrentTime);
                }

                /* make sure things don't get out of hand.  this magic number is derived */
                /* from the denominator of the smallest possible note:  64th note, with */
                /* a possible 3, 5, or 7 division, and (3/2) dot. */
                if (CurrentTime.Denominator > Constants.Denominator)
                {
                    // factoring malfunction
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }
            } while (!DoneFlag);
            totalWidth = CurrentXLocation;
            RecalculationRequired = false;

            /* build tie representation */
            BuildTieRepresentation();

            if (TrackExtentsChanged != null)
            {
                TrackExtentsChanged.Invoke(this, EventArgs.Empty);
            }

            return true;
        }

        /* find out where a pixel position is located in the score.  it returns the */
        /* index of the frame the position is on and *OnFrame is True if it was on a frame. */
        /* otherwise, it returns the index of the frame that the position precedes and */
        /* sets *OnFrame to False.  TrackObj is the track that we want to find positions */
        /* with respect to. */
        public void TrackDisplayPixelToIndex(TrackObjectRec TrackObj, int PixelPosition, out bool OnFrame, out int Index)
        {
            FrameAttrRec[] FrameAttrArray;
            int Limit;

            TrackDisplayScheduleUpdate();
            for (int i = 0; i < TrackAttrArray.Length; i++)
            {
                if (TrackObj == TrackAttrArray[i].TrackObj)
                {
                    FrameAttrArray = TrackAttrArray[i].FrameAttrArray;
                    Limit = TrackAttrArray[i].NumFrames;
                    goto FoundTrackPoint;
                }
            }
            // unknown track specified
            Debug.Assert(false);
            throw new ArgumentException();

            FoundTrackPoint:
            /* perform a binary search to locate the cell responsible for the track */
            /* first, check to make sure index is within range */
            if (PixelPosition < 0)
            {
                OnFrame = false;
                Index = 0;
                return;
            }
            if ((Limit == 0) || (PixelPosition >= FrameAttrArray[Limit - 1].PixelStart + FrameAttrArray[Limit - 1].Width))
            {
                /* since we are beyond the end, we set *OnFrame false to indicate that */
                /* the pixel position given is not a valid frame, and we return the index */
                /* of the [nonexistent] next frame */
                OnFrame = false;
                Index = Limit;
                return;
            }
            /* initialize counter variables.  Invariant:  Limit > 0 */
            int LeftIndex = 0;
            int RightIndex = Limit - 1;
            while (LeftIndex != RightIndex)
            {
                int Midpoint;

                /* find out pivot point for comparison */
                Midpoint = (LeftIndex + RightIndex) / 2;
                /* check to see if index falls on pivot frame */
                if ((FrameAttrArray[Midpoint].PixelStart <= PixelPosition)
                    && (FrameAttrArray[Midpoint].PixelStart + FrameAttrArray[Midpoint].Width > PixelPosition))
                {
                    OnFrame = true;
                    Index = Midpoint;
                    return;
                }
                /* check to see if we can discard left half */
                if (FrameAttrArray[Midpoint].PixelStart <= PixelPosition)
                {
                    LeftIndex = Midpoint + 1;
                }
                /* check to see if we can discard right half */
                if (FrameAttrArray[Midpoint].PixelStart > PixelPosition)
                {
                    RightIndex = Midpoint;
                }
            }
            /* invariant: LeftIndex == RightIndex */
            /* now figure out what the results mean */
            if (FrameAttrArray[LeftIndex].PixelStart > PixelPosition)
            {
                /* it is before the one we found, in the interstice. */
                OnFrame = false;
                Index = LeftIndex;
                return;
            }
            /* otherwise, it must be on the one we found */
            if (FrameAttrArray[LeftIndex].PixelStart + FrameAttrArray[LeftIndex].Width < PixelPosition)
            {
                // error
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            OnFrame = true;
            Index = LeftIndex;
            return;
        }

        /* get the number of tracks controlled by this scheduler */
        public int TrackDisplayGetNumTracks()
        {
            return TrackAttrArray.Length;
        }

        /* look up the track index of a specific track */
        public int TrackDisplayGetTrackIndex(TrackObjectRec TrackObj)
        {
            int i = 0;
            while (i < TrackAttrArray.Length)
            {
                if (TrackAttrArray[i].TrackObj == TrackObj)
                {
                    return i;
                }
                i++;
            }
            Debug.Assert(false);
            throw new ArgumentException();
        }

        /* get the track specified by the index (0..last func return value - 1) */
        public TrackObjectRec TrackDisplayGetParticularTrack(long Index)
        {
            return TrackAttrArray[Index].TrackObj;
        }

        /* get the total length of the track */
        public void TrackDisplayGetTotalLength(out int LongestLength)
        {
            TrackDisplayScheduleUpdate();
            LongestLength = totalWidth;
        }

        /* find out what NOTE (not frame) is at the specified location.  NIL */
        /* is returned if no note is there.  If the flag is clear, then it's a note, */
        /* otherwise it's a command.  If you intend to modify it, then pass an address */
        /* for FrameIndex so you know what to pass to TrackDisplayScheduleMarkChanged. */
        public NoteObjectRec TrackDisplayGetUnderlyingNote(int TrackIndex, out bool CommandFlag, int PixelX, out int FrameIndex)
        {
            bool OnFrame;
            int Index;
            TrackDisplayPixelToIndex(TrackAttrArray[TrackIndex].TrackObj, PixelX, out OnFrame, out Index);
            FrameIndex = Index;
            if (!OnFrame)
            {
                CommandFlag = false;
                return null;
            }
            int BeginningOfFrame;
            TrackDisplayIndexToPixel(TrackIndex, Index, out BeginningOfFrame);
            FrameObjectRec Frame = TrackAttrArray[TrackIndex].TrackObj.FrameArray[Index];
            int FrameWidth = FrameDrawUtility.WidthOfFrameAndDraw(
                gc,
                0,
                0,
                Frame,
                false/*nodraw*/,
                false,
                TrackIndex == 0 ? inlineParamVis : 0);
            CommandFlag = Frame.IsThisACommandFrame;
            if (CommandFlag)
            {
                /* just a single command */
                if ((BeginningOfFrame <= PixelX) && (BeginningOfFrame + FrameWidth > PixelX))
                {
                    return Frame[0];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                /* notes are in the frame. */
                if ((BeginningOfFrame <= PixelX) && (BeginningOfFrame + FrameWidth > PixelX))
                {
                    /* calculate possible note index */
                    int NoteIndex = Frame.Count;
                    while (NoteIndex-- >= 0)
                    {
                        int offset = TrackDisplayGetNoteOffset(TrackIndex, FrameIndex, NoteIndex);
                        if (offset <= PixelX - BeginningOfFrame)
                        {
                            break;
                        }
                    }
                    if (Frame.Count == 0)
                    {
                        // frame doesn't contain any notes
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    if (NoteIndex > Frame.Count - 1)
                    {
                        NoteIndex = Frame.Count - 1;
                    }
                    return Frame[NoteIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        /* find out if a certain frame should be drawn.  this is for squashing command */
        /* frames from channels other than the front channel */
        public bool TrackDisplayShouldWeDrawIt(int TrackIndex, int FrameIndex)
        {
            TrackDisplayScheduleUpdate();
            return !TrackAttrArray[TrackIndex].FrameAttrArray[FrameIndex].SquashThisOne;
        }

        /* find out what index a measure bar should be.  if it shouldn't be a measure */
        /* bar, then it returns NOMEASUREBAR. */
        public int TrackDisplayMeasureBarIndex(int FrameIndex)
        {
            TrackDisplayScheduleUpdate();
            return MainTrackMeasureBars[FrameIndex].BarIndex;
        }

        /* find out the frame index for the specified measure bar.  if it can't find the */
        /* specified measure bar, it returns the location for the largest one less than */
        /* the value specified.  it returns false if calculation fails. */
        public void TrackDisplayMeasureIndexToFrame(int MeasureBarIndex, out int FrameIndexOut)
        {
            TrackDisplayScheduleUpdate();
            for (int i = TrackAttrArray[0].NumFrames - 1; i >= 0; i--)
            {
                if ((MainTrackMeasureBars[i].BarIndex >= 0) && (MainTrackMeasureBars[i].BarIndex <= MeasureBarIndex))
                {
                    FrameIndexOut = i;
                    return;
                }
            }
            FrameIndexOut = TrackAttrArray[0].NumFrames - 1;
            return;
        }

        /* find out if a measure bar should be greyed out or drawn solid.  it returns */
        /* true if the measure bar should be greyed. */
        public bool TrackDisplayShouldMeasureBarBeGreyed(int FrameIndex)
        {
            TrackDisplayScheduleUpdate();
            if (MainTrackMeasureBars[FrameIndex].BarIndex == TrackDisplayConstants.NOMEASUREBAR)
            {
                // no measure bar should be drawn here.
                Debug.Assert(false);
                throw new ArgumentException();
            }
            return MainTrackMeasureBars[FrameIndex].DrawBarGreyed;
        }

        /* get a list of coordinates that need to be tied together */
        /* it might return NIL if there isn't a tie tracker. */
        public List<TieTrackPixelRec.TiePixelRec> TrackDisplayGetTieIntervalList(int StartX, int Width)
        {
            TrackDisplayScheduleUpdate();
            return TiePixelTracker.GetTieTrackPixelIntersecting(StartX, Width);
        }

        /* mark scheduler so that it recalculates all the stuff.  the track and frame */
        /* index specify where recalculation has to start from, so that data before that */
        /* doesn't need to be updated. */
        public void TrackDisplayScheduleMarkChanged(TrackObjectRec TrackObj, int FrameIndex)
        {
            /* we ignore starting position parameters for now. */
            RecalculationRequired = true;
        }

        public int TrackDisplayGetNoteOffset(int trackIndex, int frameIndex, int noteIndex)
        {
            if (trackIndex == 0)
            {
                return TrackAttrArray[0].FrameAttrArray[frameIndex].metrics[noteIndex].offset;
            }
            else
            {
                return noteIndex * FrameDisplayConstants.INTERNALSEPARATION;
            }
        }

        public int TrackDisplayGetNoteInternalWidth(int trackIndex, int frameIndex, int noteIndex)
        {
            if (trackIndex == 0)
            {
                return TrackAttrArray[0].FrameAttrArray[frameIndex].metrics[noteIndex].width;
            }
            else
            {
                return FrameDisplayConstants.ICONWIDTH;
            }
        }
    }


    public class TieTrackRec
    {
        private readonly List<TiePairRec> ListOfRecords = new List<TiePairRec>();

        private class TiePairRec
        {
            public readonly int SourcePixelIndexX;
            public readonly int SourcePixelIndexY;
            public readonly NoteNoteObjectRec SourceNote;
            public readonly NoteNoteObjectRec DestinationNote;

            public TiePairRec(
                int SourcePixelIndexX,
                int SourcePixelIndexY,
                NoteNoteObjectRec SourceNote,
                NoteNoteObjectRec DestinationNote)
            {
                this.SourcePixelIndexX = SourcePixelIndexX;
                this.SourcePixelIndexY = SourcePixelIndexY;
                this.SourceNote = SourceNote;
                this.DestinationNote = DestinationNote;
            }
        }

        /* find out if there is a tie source in the object for the destination. */
        /* the pair is removed from the list */
        public bool GetTieSourceFromDestination(
            out int SourcePixelX,
            out int SourcePixelY,
            out NoteNoteObjectRec SourceNote,
            NoteNoteObjectRec CurrentNote)
        {
            SourcePixelX = Int32.MinValue;
            SourcePixelY = Int32.MinValue;
            SourceNote = null;

            for (int i = 0; i < ListOfRecords.Count; i++)
            {
                TiePairRec TiePair = ListOfRecords[i];
                if (TiePair.DestinationNote == CurrentNote)
                {
                    SourcePixelX = TiePair.SourcePixelIndexX;
                    SourcePixelY = TiePair.SourcePixelIndexY;
                    SourceNote = TiePair.SourceNote;
                    ListOfRecords.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /* add a new tie pair to the list of tie pairs */
        public void AddTiePair(
            NoteNoteObjectRec SourceNote,
            int SourcePixelX,
            int SourcePixelY,
            NoteNoteObjectRec DestinationNote)
        {
            ListOfRecords.Add(
                new TiePairRec(
                    SourcePixelX,
                    SourcePixelY,
                    SourceNote,
                    DestinationNote));
        }
    }


    public class TieTrackPixelRec
    {
        private List<TiePixelRec> ListOfTieThangs = new List<TiePixelRec>(); /* of TiePixelRec's */

        public class TiePixelRec
        {
            public int SourceHorizontalPixel;
            public int SourceVerticalPixel;
            public int DestinationHorizontalPixel;
            public int DestinationVerticalPixel;

            public TiePixelRec(
                int SourceHorizontalPixel,
                int SourceVerticalPixel,
                int DestinationHorizontalPixel,
                int DestinationVerticalPixel)
            {
                this.SourceHorizontalPixel = SourceHorizontalPixel;
                this.SourceVerticalPixel = SourceVerticalPixel;
                this.DestinationHorizontalPixel = DestinationHorizontalPixel;
                this.DestinationVerticalPixel = DestinationVerticalPixel;
            }
        }

        public void Add(int StartX, int StartY, int EndX, int EndY)
        {
            ListOfTieThangs.Add(new TiePixelRec(StartX, StartY, EndX, EndY));
        }

        /* obtain an array of all ties which are in some manner intersecting the */
        /* specified X interval */
        public List<TiePixelRec> GetTieTrackPixelIntersecting(int XLocStart, int XLocWidth)
        {
            List<TiePixelRec> List = new List<TiePixelRec>();
            for (int i = 0; i < ListOfTieThangs.Count; i++)
            {
                TiePixelRec PixelRec = ListOfTieThangs[i];
                if ((PixelRec.SourceHorizontalPixel < XLocStart + XLocWidth)
                    && (PixelRec.DestinationHorizontalPixel > XLocStart))
                {
                    /* some part of the tie-line is visible on the screen */
                    List.Add(PixelRec);
                }
            }

            return List;
        }

        /* get the drawing information about a particular tie thang */
        public static void GetTieTrackIntersectElement(
            List<TiePixelRec> List,
            int Index,
            out int StartX,
            out int StartY,
            out int EndX,
            out int EndY)
        {
            TiePixelRec PixelRec = List[Index];
            StartX = PixelRec.SourceHorizontalPixel;
            StartY = PixelRec.SourceVerticalPixel;
            EndX = PixelRec.DestinationHorizontalPixel;
            EndY = PixelRec.DestinationVerticalPixel;
        }
    }


    public static class FrameDrawUtility
    {
        // TODO: WidthOfFrameAndDraw() creates and destroys a lot of GDI bitmap objects due
        // structure of legacy code. Would be more efficient to use static images as needed
        // and use Graphics.DrawImage() to composite rather than compositing individual
        // bitmap objects for each note.

        /* find out the width of this command/note frame and draw it if the flag is set. */
        /* it assumes the clipping rectangle is set up properly.  the X and Y parameters */
        /* specify the left edge of the note and the Middle C line. */
        /* this routine does not handle drawing of ties. */
        public static int WidthOfFrameAndDraw(
            IGraphicsContext gc,
            int X,
            int Y,
            FrameObjectRec Frame,
            bool ActuallyDraw,
            bool GreyedOut,
            InlineParamVis inlineParamVis,
            ref TrackDispScheduleRec.NoteMetrics[] metrics)
        {
            int extraSpaceWidth = 0;

            // ensure metrics array is correct size - if caller wants it
            if (metrics != null)
            {
                Array.Resize(ref metrics, Frame.Count);
            }

            /* we should be able to find ways of overlapping the notes if they won't */
            /* be on top of each other on screen but we're not going to for now. */
            int Width;
            if (Frame.IsThisACommandFrame)
            {
                /* it's a command frame, so draw using the special command drawing routines */
                Width = DrawCommandOnScreen(gc, X, Y, (CommandNoteObjectRec)Frame[0], ActuallyDraw, GreyedOut);
                if (metrics != null)
                {
                    metrics[0].offset = 0;
                    metrics[0].width = (short)Width;
                }
            }
            else
            {
                /* we have to draw the notes ourselves. */
                X -= FrameDisplayConstants.LEFTNOTEEDGEINSET;
                int relX = 0;
                Width = 0;
                for (int noteIndex = 0; noteIndex < Frame.Count; noteIndex++)
                {
                    /* get the note to be drawn */
                    NoteNoteObjectRec Note = (NoteNoteObjectRec)Frame[noteIndex];
                    /* do the icon stuff */
                    int NoteOffset = 0;
                    if (ActuallyDraw)
                    {
                        ManagedBitmap2 Image;
                        ManagedBitmap2 Mask;

                        /* first, obtain the proper image for the duration */
                        if (!Note.GetNoteIsItARest())
                        {
                            if (NoteFlags.eDiv1Modifier == Note.GetNoteDurationDivision())
                            {
                                /* normal notes */
                                switch (Note.GetNoteDuration())
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new ArgumentException();
                                    case NoteFlags.e64thNote:
                                        Image = Bitmaps.SixtyFourthNoteImage;
                                        Mask = Bitmaps.SixtyFourthNoteMask;
                                        break;
                                    case NoteFlags.e32ndNote:
                                        Image = Bitmaps.ThirtySecondNoteImage;
                                        Mask = Bitmaps.ThirtySecondNoteMask;
                                        break;
                                    case NoteFlags.e16thNote:
                                        Image = Bitmaps.SixteenthNoteImage;
                                        Mask = Bitmaps.SixteenthNoteMask;
                                        break;
                                    case NoteFlags.e8thNote:
                                        Image = Bitmaps.EighthNoteImage;
                                        Mask = Bitmaps.EighthNoteMask;
                                        break;
                                    case NoteFlags.e4thNote:
                                        Image = Bitmaps.QuarterNoteImage;
                                        Mask = Bitmaps.QuarterNoteMask;
                                        break;
                                    case NoteFlags.e2ndNote:
                                        Image = Bitmaps.HalfNoteImage;
                                        Mask = Bitmaps.HalfNoteMask;
                                        break;
                                    case NoteFlags.eWholeNote:
                                        Image = Bitmaps.WholeNoteImage;
                                        Mask = Bitmaps.WholeNoteMask;
                                        break;
                                    case NoteFlags.eDoubleNote:
                                        Image = Bitmaps.DoubleNoteImage;
                                        Mask = Bitmaps.DoubleNoteMask;
                                        break;
                                    case NoteFlags.eQuadNote:
                                        Image = Bitmaps.QuadNoteImage;
                                        Mask = Bitmaps.QuadNoteMask;
                                        break;
                                }
                            }
                            else
                            {
                                /* notes with fractions:  use next symbol down */
                                switch (Note.GetNoteDuration())
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new ArgumentException();
                                    case NoteFlags.e64thNote:
                                        Image = Bitmaps.OneTwentyEighthNoteImage;
                                        Mask = Bitmaps.OneTwentyEighthNoteMask;
                                        break;
                                    case NoteFlags.e32ndNote:
                                        Image = Bitmaps.SixtyFourthNoteImage;
                                        Mask = Bitmaps.SixtyFourthNoteMask;
                                        break;
                                    case NoteFlags.e16thNote:
                                        Image = Bitmaps.ThirtySecondNoteImage;
                                        Mask = Bitmaps.ThirtySecondNoteMask;
                                        break;
                                    case NoteFlags.e8thNote:
                                        Image = Bitmaps.SixteenthNoteImage;
                                        Mask = Bitmaps.SixteenthNoteMask;
                                        break;
                                    case NoteFlags.e4thNote:
                                        Image = Bitmaps.EighthNoteImage;
                                        Mask = Bitmaps.EighthNoteMask;
                                        break;
                                    case NoteFlags.e2ndNote:
                                        Image = Bitmaps.QuarterNoteImage;
                                        Mask = Bitmaps.QuarterNoteMask;
                                        break;
                                    case NoteFlags.eWholeNote:
                                        Image = Bitmaps.HalfNoteImage;
                                        Mask = Bitmaps.HalfNoteMask;
                                        break;
                                    case NoteFlags.eDoubleNote:
                                        Image = Bitmaps.WholeNoteImage;
                                        Mask = Bitmaps.WholeNoteMask;
                                        break;
                                    case NoteFlags.eQuadNote:
                                        Image = Bitmaps.DoubleNoteImage;
                                        Mask = Bitmaps.DoubleNoteMask;
                                        break;
                                }
                            }
                            /* end of note business */
                        }
                        else
                        {
                            if (NoteFlags.eDiv1Modifier == Note.GetNoteDurationDivision())
                            {
                                /* normal rests */
                                switch (Note.GetNoteDuration())
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new ArgumentException();
                                    case NoteFlags.e64thNote:
                                        Image = Bitmaps.SixtyFourthRestImage;
                                        Mask = Bitmaps.SixtyFourthRestMask;
                                        break;
                                    case NoteFlags.e32ndNote:
                                        Image = Bitmaps.ThirtySecondRestImage;
                                        Mask = Bitmaps.ThirtySecondRestMask;
                                        break;
                                    case NoteFlags.e16thNote:
                                        Image = Bitmaps.SixteenthRestImage;
                                        Mask = Bitmaps.SixteenthRestMask;
                                        break;
                                    case NoteFlags.e8thNote:
                                        Image = Bitmaps.EighthRestImage;
                                        Mask = Bitmaps.EighthRestMask;
                                        break;
                                    case NoteFlags.e4thNote:
                                        Image = Bitmaps.QuarterRestImage;
                                        Mask = Bitmaps.QuarterRestMask;
                                        break;
                                    case NoteFlags.e2ndNote:
                                        Image = Bitmaps.HalfRestImage;
                                        Mask = Bitmaps.HalfRestMask;
                                        break;
                                    case NoteFlags.eWholeNote:
                                        Image = Bitmaps.WholeRestImage;
                                        Mask = Bitmaps.WholeRestMask;
                                        break;
                                    case NoteFlags.eDoubleNote:
                                        Image = Bitmaps.DoubleRestImage;
                                        Mask = Bitmaps.DoubleRestMask;
                                        break;
                                    case NoteFlags.eQuadNote:
                                        Image = Bitmaps.QuadRestImage;
                                        Mask = Bitmaps.QuadRestMask;
                                        break;
                                }
                            }
                            else
                            {
                                /* rests with fractions:  use next symbol down */
                                switch (Note.GetNoteDuration())
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new ArgumentException();
                                    case NoteFlags.e64thNote:
                                        Image = Bitmaps.OneTwentyEighthRestImage;
                                        Mask = Bitmaps.OneTwentyEighthRestMask;
                                        break;
                                    case NoteFlags.e32ndNote:
                                        Image = Bitmaps.SixtyFourthRestImage;
                                        Mask = Bitmaps.SixtyFourthRestMask;
                                        break;
                                    case NoteFlags.e16thNote:
                                        Image = Bitmaps.ThirtySecondRestImage;
                                        Mask = Bitmaps.ThirtySecondRestMask;
                                        break;
                                    case NoteFlags.e8thNote:
                                        Image = Bitmaps.SixteenthRestImage;
                                        Mask = Bitmaps.SixteenthRestMask;
                                        break;
                                    case NoteFlags.e4thNote:
                                        Image = Bitmaps.EighthRestImage;
                                        Mask = Bitmaps.EighthRestMask;
                                        break;
                                    case NoteFlags.e2ndNote:
                                        Image = Bitmaps.QuarterRestImage;
                                        Mask = Bitmaps.QuarterRestMask;
                                        break;
                                    case NoteFlags.eWholeNote:
                                        Image = Bitmaps.HalfRestImage;
                                        Mask = Bitmaps.HalfRestMask;
                                        break;
                                    case NoteFlags.eDoubleNote:
                                        Image = Bitmaps.WholeRestImage;
                                        Mask = Bitmaps.WholeRestMask;
                                        break;
                                    case NoteFlags.eQuadNote:
                                        Image = Bitmaps.DoubleRestImage;
                                        Mask = Bitmaps.DoubleRestMask;
                                        break;
                                }
                            }
                            /* end of note business */
                        }
                        /* now, handle divisions */
                        switch (Note.GetNoteDurationDivision())
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case NoteFlags.eDiv1Modifier:
                                /* no change */
                                break;
                            case NoteFlags.eDiv3Modifier:
                                Image = ManagedBitmap2.Or(Bitmaps.Div3Image, Image);
                                Mask = ManagedBitmap2.Or(Bitmaps.Div3Mask, Mask);
                                break;
                            case NoteFlags.eDiv5Modifier:
                                Image = ManagedBitmap2.Or(Bitmaps.Div5Image, Image);
                                Mask = ManagedBitmap2.Or(Bitmaps.Div5Mask, Mask);
                                break;
                            case NoteFlags.eDiv7Modifier:
                                Image = ManagedBitmap2.Or(Bitmaps.Div7Image, Image);
                                Mask = ManagedBitmap2.Or(Bitmaps.Div7Mask, Mask);
                                break;
                        }
                        /* handle dots */
                        if (Note.GetNoteDotStatus())
                        {
                            Image = ManagedBitmap2.Or(Bitmaps.DotImage, Image);
                            Mask = ManagedBitmap2.Or(Bitmaps.DotMask, Mask);
                        }
                        /* sharps and flats require more clever handling */
                        if ((Note.GetNoteFlatOrSharpStatus() & NoteFlags.eSharpModifier) != 0)
                        {
                            Image = ManagedBitmap2.Or(Bitmaps.SharpImage, Image);
                            Mask = ManagedBitmap2.Or(Bitmaps.SharpMask, Mask);
                        }
                        if ((Note.GetNoteFlatOrSharpStatus() & NoteFlags.eFlatModifier) != 0)
                        {
                            Image = ManagedBitmap2.Or(Bitmaps.FlatImage, Image);
                            Mask = ManagedBitmap2.Or(Bitmaps.FlatMask, Mask);
                        }
                        /* perform the drawing */
                        NoteOffset = Y + StaffCalibration.ConvertPitchToPixel(Note.GetNotePitch(), Note.GetNoteFlatOrSharpStatus())
                            - FrameDisplayConstants.TOPNOTESTAFFINTERSECT - StaffCalibration.CenterNotePixel;
                        using (Bitmap gdiImage = Image.ToGDI(Color.Transparent, GreyedOut ? gc.LightGreyColor : gc.ForeColor))
                        {
                            using (Bitmap gdiMask = Mask.ToGDI(Color.Transparent, gc.BackColor))
                            {
                                gc.graphics.DrawImage(
                                    gdiMask,
                                    X,
                                    NoteOffset,
                                    gdiMask.Width,
                                    gdiMask.Height);
                                gc.graphics.DrawImage(
                                    gdiImage,
                                    X,
                                    NoteOffset,
                                    gdiImage.Width,
                                    gdiImage.Height);
                            }
                        }
                    }

                    // compute extra width needed for inline parameter drawing
                    int inlineTextWidth = 0;
                    if (inlineParamVis != 0)
                    {
                        if (extraSpaceWidth == 0)
                        {
                            const string ExtraSpace = "  ";
                            extraSpaceWidth = MyTextRenderer.MeasureText(
                                gc.graphics,
                                ExtraSpace,
                                gc.font,
                                new Size(Int16.MaxValue, gc.font.Height),
                                NoteParamStrip.Flags).Width;
                        }

                        ValueInfo[] values = ValueInfo.Values;
                        for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
                        {
                            ValueInfo info = values[valueIndex];
                            if ((info != null) && ((inlineParamVis & info.InlineParam) != 0))
                            {
                                string text = info.GetValue(Note);
                                int width1 = MyTextRenderer.MeasureText(
                                    gc.graphics,
                                    text,
                                    gc.font,
                                    new Size(Int16.MaxValue, gc.font.Height),
                                    NoteParamStrip.Flags).Width;
                                inlineTextWidth = Math.Max(inlineTextWidth, width1);
                            }
                        }
                    }

                    /* increment X for the next time around */
                    int rightEdge = X + FrameDisplayConstants.INTERNALSEPARATION;
                    int internalWidth = Math.Max(FrameDisplayConstants.INTERNALSEPARATION, inlineTextWidth);
                    int externalWidth = Math.Max(FrameDisplayConstants.INTERNALSEPARATION, inlineTextWidth + extraSpaceWidth);
                    if (metrics != null)
                    {
                        metrics[noteIndex].offset = (short)relX;
                        metrics[noteIndex].width = (short)Math.Max(internalWidth, FrameDisplayConstants.ICONWIDTH);
                    }
                    X += externalWidth;
                    relX += externalWidth;

                    if (ActuallyDraw)
                    {
                        /* draw little attributions below */
                        if (!GreyedOut)
                        {
                            if (Note.GetNoteRetriggerEnvelopesOnTieStatus())
                            {
                                using (Bitmap gdiRetrigger8x8 = Bitmaps.Retrigger8x8.ToGDI(Color.Transparent, gc.ForeColor))
                                {
                                    gc.graphics.DrawImage(
                                        gdiRetrigger8x8,
                                        rightEdge - 1,
                                        NoteOffset + 40,
                                        gdiRetrigger8x8.Width,
                                        gdiRetrigger8x8.Height);
                                }
                            }
                            if (0 != Note.GetNotePortamentoDuration())
                            {
                                if (Note.GetNotePortamentoLeadsBeatFlag())
                                {
                                    /* draw backwards "P" for portamento leading note */
                                    using (Bitmap gdiReversePortamento8x8 = Bitmaps.ReversePortamento8x8.ToGDI(Color.Transparent, gc.ForeColor))
                                    {
                                        gc.graphics.DrawImage(
                                            gdiReversePortamento8x8,
                                            rightEdge - 1,
                                            NoteOffset + 40 + 9,
                                            gdiReversePortamento8x8.Width,
                                            gdiReversePortamento8x8.Height);
                                    }
                                }
                                else
                                {
                                    /* draw "P" for portamento */
                                    using (Bitmap gdiPortamento8x8 = Bitmaps.Portamento8x8.ToGDI(Color.Transparent, gc.ForeColor))
                                    {
                                        gc.graphics.DrawImage(
                                            gdiPortamento8x8,
                                            rightEdge - 1,
                                            NoteOffset + 40 + 9,
                                            gdiPortamento8x8.Width,
                                            gdiPortamento8x8.Height);
                                    }
                                }
                            }
                            if (Note.GetNoteEarlyLateAdjust() < 0)
                            {
                                /* draw "<" for early adjust */
                                using (Bitmap gdiShiftEarly8x8 = Bitmaps.ShiftEarly8x8.ToGDI(Color.Transparent, gc.ForeColor))
                                {
                                    gc.graphics.DrawImage(
                                        gdiShiftEarly8x8,
                                        rightEdge - 1,
                                        NoteOffset + 40 + 2 * 9,
                                        gdiShiftEarly8x8.Width,
                                        gdiShiftEarly8x8.Height);
                                }
                            }
                            else if (Note.GetNoteEarlyLateAdjust() > 0)
                            {
                                /* draw ">" for late adjust */
                                using (Bitmap gdiShiftLate8x8 = Bitmaps.ShiftLate8x8.ToGDI(Color.Transparent, gc.ForeColor))
                                {
                                    gc.graphics.DrawImage(
                                        gdiShiftLate8x8,
                                        rightEdge - 1,
                                        NoteOffset + 40 + 2 * 9,
                                        gdiShiftLate8x8.Width,
                                        gdiShiftLate8x8.Height);
                                }
                            }
                            if (Note.GetNoteDetuning() < 0)
                            {
                                /* draw "-" for detuning down */
                                using (Bitmap gdiPitchDown8x8 = Bitmaps.PitchDown8x8.ToGDI(Color.Transparent, gc.ForeColor))
                                {
                                    gc.graphics.DrawImage(
                                        gdiPitchDown8x8,
                                        rightEdge - 1,
                                        NoteOffset + 40 + 3 * 9,
                                        gdiPitchDown8x8.Width,
                                        gdiPitchDown8x8.Height);
                                }
                            }
                            else if (Note.GetNoteDetuning() > 0)
                            {
                                /* draw "+" for detuning up */
                                using (Bitmap gdiPitchUp8x8 = Bitmaps.PitchUp8x8.ToGDI(Color.Transparent, gc.ForeColor))
                                {
                                    gc.graphics.DrawImage(
                                        gdiPitchUp8x8,
                                        rightEdge - 1,
                                        NoteOffset + 40 + 3 * 9,
                                        gdiPitchUp8x8.Width,
                                        gdiPitchUp8x8.Height);
                                }
                            }
                        }
                    }

                    /* update the width count */
                    if (Width == 0)
                    {
                        /* first time you get the whole width */
                        Width = Math.Max(FrameDisplayConstants.ICONWIDTH - FrameDisplayConstants.LEFTNOTEEDGEINSET, externalWidth);
                    }
                    else
                    {
                        /* other times, you just get whatever extra there is */
                        Width += externalWidth;
                    }
                }
            }

            return Width;
        }

        public static int WidthOfFrameAndDraw(
            IGraphicsContext gc,
            int X,
            int Y,
            FrameObjectRec Frame,
            bool ActuallyDraw,
            bool GreyedOut,
            InlineParamVis inlineParamVis)
        {
            TrackDispScheduleRec.NoteMetrics[] widthsUnused = null;
            return WidthOfFrameAndDraw(
                gc,
                X,
                Y,
                Frame,
                ActuallyDraw,
                GreyedOut,
                inlineParamVis,
                ref widthsUnused);
        }

        /* draw the command on the screen, or measure how many pixels wide the image will be */
        /* if it will draw, it assumes the clipping rectangle to be set up properly */
        public static int DrawCommandOnScreen(
            IGraphicsContext gc,
            int X,
            int Y,
            CommandNoteObjectRec Note,
            bool ActuallyDraw,
            bool GreyedOut)
        {
            /* find out what the command's name is */
            string Name = CommandMapping.GetCommandName((NoteCommands)(Note.Flags & ~NoteFlags.eCommandFlag));
            /* figure out what needs to be drawn for the command */
            CommandAddrMode Params = CommandMapping.GetCommandAddressingMode((NoteCommands)(Note.Flags & ~NoteFlags.eCommandFlag));
            /* perform the actual drawing of the command */
            int ReturnValue;
            switch (Params)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case CommandAddrMode.eNoParameters:
                    ReturnValue = DrawCommandNoParams(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name);
                    break;
                case CommandAddrMode.e1SmallExtParameter: /* <1xs> */
                    ReturnValue = DrawCommand1Param(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        SmallExtBCDType.FromRawInt32(Note.GetCommandNumericArg1()).ToString());
                    break;
                case CommandAddrMode.e2SmallExtParameters: /* <1xs> <2xs> */
                    ReturnValue = DrawCommand2Params(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        SmallExtBCDType.FromRawInt32(Note.GetCommandNumericArg1()).ToString(),
                        SmallExtBCDType.FromRawInt32(Note.GetCommandNumericArg2()).ToString());
                    break;
                case CommandAddrMode.e1LargeParameter: /* <1l> */
                    ReturnValue = DrawCommand1Param(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        LargeBCDType.FromRawInt32(Note.GetCommandNumericArg1()).ToString());
                    break;
                case CommandAddrMode.eFirstLargeSecondSmallExtParameters: /* <1l> <2xs> */
                    ReturnValue = DrawCommand2Params(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        LargeBCDType.FromRawInt32(Note.GetCommandNumericArg1()).ToString(),
                        SmallExtBCDType.FromRawInt32(Note.GetCommandNumericArg2()).ToString());
                    break;
                case CommandAddrMode.e1ParamReleaseOrigin: /* origin <1i> */
                    if ((Note.GetCommandNumericArg1() != 0) && (Note.GetCommandNumericArg1() != -1))
                    {
                        // bad value in argument 1
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    ReturnValue = DrawCommand1Param(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandNumericArg1() >= 0 ? "From End" : "From Start");
                    break;
                case CommandAddrMode.e1PitchDisplacementMode: /* hertz/steps <1i> */
                    if ((Note.GetCommandNumericArg1() != 0) && (Note.GetCommandNumericArg1() != -1))
                    {
                        // bad value in argument 1
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    ReturnValue = DrawCommand1Param(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandNumericArg1() >= 0 ? "Half-Steps" : "Hertz");
                    break;
                case CommandAddrMode.e2IntegerParameters: /* <1i> <2i> */
                    ReturnValue = DrawCommand2Params(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandNumericArg1().ToString(),
                        Note.GetCommandNumericArg2().ToString());
                    break;
                case CommandAddrMode.e1DurationAdjustMode: /* multiplicative/additive <1i> */
                    if ((Note.GetCommandNumericArg1() != 0) && (Note.GetCommandNumericArg1() != -1))
                    {
                        // bad value in argument 1
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    ReturnValue = DrawCommand1Param(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandNumericArg1() >= 0 ? "Additive" : "Multiplicative");
                    break;
                case CommandAddrMode.e1IntegerParameter: /* <1i> */
                    ReturnValue = DrawCommand1Param(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandNumericArg1().ToString());
                    break;
                case CommandAddrMode.e1StringParameter: /* <string> */
                    ReturnValue = DrawCommand1Param(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandStringArg1());
                    break;
                case CommandAddrMode.e1StringParameterWithLineFeeds: /* <string> */
                    ReturnValue = DrawCommand1ParamWithLineFeeds(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandStringArg1());
                    break;
                case CommandAddrMode.e1TrackEffectsMode: /* enable/disable <1i> */
                    if ((Note.GetCommandNumericArg1() != 0) && (Note.GetCommandNumericArg1() != -1))
                    {
                        // bad value in argument 1
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                    ReturnValue = DrawCommand1Param(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandNumericArg1() >= 0 ? "Disable" : "Enable");
                    break;
                case CommandAddrMode.eFirstIntSecondLargeParameters:
                    ReturnValue = DrawCommand2Params(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandNumericArg1().ToString(),
                        LargeBCDType.FromRawInt32(Note.GetCommandNumericArg2()).ToString());
                    break;
                case CommandAddrMode.e2StringParameters: /* <string1> <string2> */
                    ReturnValue = DrawCommand2Params(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandStringArg1(),
                        Note.GetCommandStringArg2());
                    break;
                case CommandAddrMode.e1String1LargeBCDParameters: /* <string1> holds track/group name, <1l> holds number of beats */
                    ReturnValue = DrawCommand2Params(
                        gc,
                        X,
                        Y,
                        ActuallyDraw,
                        GreyedOut,
                        Name,
                        Note.GetCommandStringArg1(),
                        LargeBCDType.FromRawInt32(Note.GetCommandNumericArg1()).ToString());
                    break;
            }
            /* report width of thing to caller */
            return ReturnValue;
        }

        private static int DrawCommandNoParams(
            IGraphicsContext gc,
            int X,
            int Y,
            bool ActuallyDraw,
            bool GreyedOut,
            string String)
        {
            int StrWidth = MyTextRenderer.MeasureText(gc.graphics, String, gc.font).Width;
            if (ActuallyDraw)
            {
                int FontHeight = gc.font.Height;

                gc.graphics.DrawRectangle(
                    gc.ForePen,
                    X,
                    Y,
                    StrWidth + 2 * FrameDisplayConstants.BORDER,
                    FontHeight + 2 * FrameDisplayConstants.BORDER);
                gc.graphics.FillRectangle(
                    gc.BackBrush,
                    X + 1,
                    Y + 1,
                    StrWidth + 2 * FrameDisplayConstants.BORDER - 1,
                    FontHeight + 2 * FrameDisplayConstants.BORDER - 1);
                MyTextRenderer.DrawText(
                    gc.graphics,
                    String,
                    gc.font,
                    new Point(
                        X + FrameDisplayConstants.BORDER,
                        Y + FrameDisplayConstants.BORDER),
                    gc.ForeColor,
                    TextFormatFlags.PreserveGraphicsClipping);
            }
            return StrWidth + 2 * FrameDisplayConstants.BORDER;
        }

        private static int DrawCommand1Param(
            IGraphicsContext gc,
            int X,
            int Y,
            bool ActuallyDraw,
            bool GreyedOut,
            string String,
            string Argument1)
        {
            int TotalWidth = MyTextRenderer.MeasureText(gc.graphics, String, gc.font).Width;
            if (Argument1 != null)
            {
                int OtherWidth = MyTextRenderer.MeasureText(gc.graphics, Argument1, gc.font).Width;
                if (OtherWidth > TotalWidth)
                {
                    TotalWidth = OtherWidth;
                }
            }
            if (ActuallyDraw)
            {
                int FontHeight = gc.font.Height;
                gc.graphics.DrawRectangle(
                    gc.ForePen,
                    X,
                    Y,
                    TotalWidth + 2 * FrameDisplayConstants.BORDER,
                    2 * FontHeight + 2 * FrameDisplayConstants.BORDER);
                gc.graphics.FillRectangle(
                    gc.BackBrush,
                    X + 1,
                    Y + 1,
                    TotalWidth + 2 * FrameDisplayConstants.BORDER - 1,
                    2 * FontHeight + 2 * FrameDisplayConstants.BORDER - 1);
                MyTextRenderer.DrawText(
                    gc.graphics,
                    String,
                    gc.font,
                    new Point(
                        X + FrameDisplayConstants.BORDER,
                        Y + FrameDisplayConstants.BORDER),
                    gc.ForeColor,
                    TextFormatFlags.PreserveGraphicsClipping);
                if (Argument1 != null)
                {
                    MyTextRenderer.DrawText(
                        gc.graphics,
                        Argument1,
                        gc.font,
                        new Point(
                            X + FrameDisplayConstants.BORDER,
                            Y + FrameDisplayConstants.BORDER + FontHeight),
                        gc.ForeColor,
                        TextFormatFlags.PreserveGraphicsClipping);
                }
            }
            return TotalWidth + 2 * FrameDisplayConstants.BORDER;
        }

        private static int DrawCommand2Params(
            IGraphicsContext gc,
            int X,
            int Y,
            bool ActuallyDraw,
            bool GreyedOut,
            string String,
            string Argument1,
            string Argument2)
        {
            int TotalWidth = MyTextRenderer.MeasureText(gc.graphics, String, gc.font).Width;
            if (Argument1 != null)
            {
                int OtherWidth = MyTextRenderer.MeasureText(gc.graphics, Argument1, gc.font).Width;
                if (OtherWidth > TotalWidth)
                {
                    TotalWidth = OtherWidth;
                }
            }
            if (Argument2 != null)
            {
                int OtherWidth = MyTextRenderer.MeasureText(gc.graphics, Argument2, gc.font).Width;
                if (OtherWidth > TotalWidth)
                {
                    TotalWidth = OtherWidth;
                }
            }
            if (ActuallyDraw)
            {
                int FontHeight = gc.font.Height;
                gc.graphics.DrawRectangle(
                    gc.ForePen,
                    X,
                    Y,
                    TotalWidth + 2 * FrameDisplayConstants.BORDER,
                    3 * FontHeight + 2 * FrameDisplayConstants.BORDER);
                gc.graphics.FillRectangle(
                    gc.BackBrush,
                    X + 1,
                    Y + 1,
                    TotalWidth + 2 * FrameDisplayConstants.BORDER - 1,
                    3 * FontHeight + 2 * FrameDisplayConstants.BORDER - 1);
                MyTextRenderer.DrawText(
                    gc.graphics,
                    String,
                    gc.font,
                    new Point(
                        X + FrameDisplayConstants.BORDER,
                        Y + FrameDisplayConstants.BORDER),
                    gc.ForeColor,
                    TextFormatFlags.PreserveGraphicsClipping);
                if (Argument1 != null)
                {
                    MyTextRenderer.DrawText(
                        gc.graphics,
                        Argument1,
                        gc.font,
                        new Point(
                            X + FrameDisplayConstants.BORDER,
                            Y + FrameDisplayConstants.BORDER + FontHeight),
                        gc.ForeColor,
                        TextFormatFlags.PreserveGraphicsClipping);
                }
                if (Argument2 != null)
                {
                    MyTextRenderer.DrawText(
                        gc.graphics,
                        Argument2,
                        gc.font,
                        new Point(
                            X + FrameDisplayConstants.BORDER,
                            Y + FrameDisplayConstants.BORDER + 2 * FontHeight),
                        gc.ForeColor,
                        TextFormatFlags.PreserveGraphicsClipping);
                }
            }
            return TotalWidth + 2 * FrameDisplayConstants.BORDER;
        }

        /* draw a command that has one parameter, but has line feeds in it. */
        private static int DrawCommand1ParamWithLineFeeds(
            IGraphicsContext gc,
            int X,
            int Y,
            bool ActuallyDraw,
            bool GreyedOut,
            string String,
            string Argument1)
        {
            int TotalWidth = MyTextRenderer.MeasureText(gc.graphics, String, gc.font).Width;
            string[] arg1Parts = new string[0];
            if (Argument1 != null)
            {
                arg1Parts = Argument1.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                if ((arg1Parts.Length > 0) && String.IsNullOrEmpty(arg1Parts[arg1Parts.Length - 1]))
                {
                    Array.Resize(ref arg1Parts, arg1Parts.Length - 1);
                }
                foreach (string line in arg1Parts)
                {
                    int OtherWidth = MyTextRenderer.MeasureText(gc.graphics, line, gc.font).Width;
                    if (OtherWidth > TotalWidth)
                    {
                        TotalWidth = OtherWidth;
                    }
                }
            }
            if (ActuallyDraw)
            {
                int FontHeight = gc.font.Height;
                gc.graphics.DrawRectangle(
                    gc.ForePen,
                    X,
                    Y,
                    TotalWidth + 2 * FrameDisplayConstants.BORDER,
                    (1 + arg1Parts.Length) * FontHeight + 2 * FrameDisplayConstants.BORDER);
                gc.graphics.FillRectangle(
                    gc.BackBrush,
                    X + 1,
                    Y + 1,
                    TotalWidth + 2 * FrameDisplayConstants.BORDER - 1,
                    (1 + arg1Parts.Length) * FontHeight + 2 * FrameDisplayConstants.BORDER - 1);
                MyTextRenderer.DrawText(
                    gc.graphics,
                    String,
                    gc.font,
                    new Point(
                        X + FrameDisplayConstants.BORDER,
                        Y + FrameDisplayConstants.BORDER),
                    gc.ForeColor,
                    TextFormatFlags.PreserveGraphicsClipping);
                for (int i = 0; i < arg1Parts.Length; i++)
                {
                    string line = arg1Parts[i];
                    MyTextRenderer.DrawText(
                        gc.graphics,
                        line,
                        gc.font,
                        new Point(
                            X + FrameDisplayConstants.BORDER,
                            Y + FrameDisplayConstants.BORDER + FontHeight * (i + 1)),
                        gc.ForeColor,
                        TextFormatFlags.PreserveGraphicsClipping);
                }
            }
            return TotalWidth + 2 * FrameDisplayConstants.BORDER;
        }
    }
}
