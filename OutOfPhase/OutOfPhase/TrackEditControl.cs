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
    public partial class TrackEditControl : UserControl, ITrackViewContextUI, IMenuStripManagerHandler
    {
        private TrackObjectRec trackObject;
        private MainWindow mainWindow;
        private MenuStripManager menuStripManager;

        public TrackEditControl()
        {
            InitializeComponent();
        }

        public void Init(
            TrackObjectRec trackObject,
            MainWindow mainWindow,
            MenuStripManager menuStripManager,
            ToolStripMenuItem backgroundToolStripMenuItem,
            ToolStripMenuItem inlineEditToolStripMenuItem)
        {
            this.trackObject = trackObject;
            this.mainWindow = mainWindow;
            this.menuStripManager = menuStripManager;
            this.backgroundToolStripMenuItem = backgroundToolStripMenuItem;
            this.inlineEditToolStripMenuItem = inlineEditToolStripMenuItem;

            noteViewControl.UndoHelper = trackViewControl.UndoHelper;
            noteParamStrip.UndoHelper = trackViewControl.UndoHelper;

            trackViewControl.NoteView = noteViewControl;
            trackViewControl.NoteParamStrip = noteParamStrip;
            trackViewControl.ContextUI = this;

            //documentBindingSource.Add(document);
            trackObjectRecBindingSource.Add(trackObject);

            trackViewControl.SetTrackObject(trackObject);
            trackViewControl.RestoreSavedScrollPosition(trackObject.SavedHScrollPos, trackObject.SavedVScrollPos);
            trackViewControl.Scroll += new ScrollEventHandler(trackViewControl_Scroll);

            ToolStripButton[] group;
            //
            group = new ToolStripButton[] { toolStripButtonArrow, toolStripButtonCommand, toolStripButtonSixtyFourth, toolStripButtonThirtySecond, toolStripButtonSixteenth, toolStripButtonEighth, toolStripButtonQuarter, toolStripButtonHalf, toolStripButtonWhole, toolStripButtonDouble, toolStripButtonQuad };
            SetButton(toolStripButtonArrow, Bitmaps1Class.gdiArrowButtonBits, Bitmaps1Class.gdiArrowButtonSelectedBits, group, new ToolStripButton[] { toolStripButtonQuarter, toolStripButtonDiv1, toolStripButtonNatural, toolStripButtonNoDot, toolStripButtonNoteVsRest });
            toolStripButtonArrow.Checked = true;
            //
            SetButton(toolStripButtonCommand, Bitmaps1Class.gdiCommandButtonBits, Bitmaps1Class.gdiCommandButtonSelectedBits, group, null);
            //
            SetButton(toolStripButtonSixtyFourth, Bitmaps1Class.gdiSixtyFourthButtonBits, Bitmaps1Class.gdiSixtyFourthButtonSelectedBits, group, null);
            SetButton(toolStripButtonThirtySecond, Bitmaps1Class.gdiThirtySecondButtonBits, Bitmaps1Class.gdiThirtySecondButtonSelectedBits, group, null);
            SetButton(toolStripButtonSixteenth, Bitmaps1Class.gdiSixteenthButtonBits, Bitmaps1Class.gdiSixteenthButtonSelectedBits, group, null);
            SetButton(toolStripButtonEighth, Bitmaps1Class.gdiEighthButtonBits, Bitmaps1Class.gdiEighthButtonSelectedBits, group, null);
            SetButton(toolStripButtonQuarter, Bitmaps1Class.gdiQuarterButtonBits, Bitmaps1Class.gdiQuarterButtonSelectedBits, group, null);
            SetButton(toolStripButtonHalf, Bitmaps1Class.gdiHalfButtonBits, Bitmaps1Class.gdiHalfButtonSelectedBits, group, null);
            SetButton(toolStripButtonWhole, Bitmaps1Class.gdiWholeButtonBits, Bitmaps1Class.gdiWholeButtonSelectedBits, group, null);
            SetButton(toolStripButtonDouble, Bitmaps1Class.gdiDoubleButtonBits, Bitmaps1Class.gdiDoubleButtonSelectedBits, group, null);
            SetButton(toolStripButtonQuad, Bitmaps1Class.gdiQuadButtonBits, Bitmaps1Class.gdiQuadButtonSelectedBits, group, null);
            //
            group = new ToolStripButton[] { toolStripButtonSharp, toolStripButtonFlat, toolStripButtonNatural };
            SetButton(toolStripButtonSharp, Bitmaps1Class.gdiSharpButtonBits, Bitmaps1Class.gdiSharpButtonSelectedBits, group, null);
            SetButton(toolStripButtonFlat, Bitmaps1Class.gdiFlatButtonBits, Bitmaps1Class.gdiFlatButtonSelectedBits, group, null);
            SetButton(toolStripButtonNatural, Bitmaps1Class.gdiNaturalButtonBits, Bitmaps1Class.gdiNaturalButtonSelectedBits, group, null);
            toolStripButtonNatural.Checked = true;
            //
            group = new ToolStripButton[] { toolStripButtonNoteVsRest, toolStripButtonNoteVsRest2 };
            SetButton(toolStripButtonNoteVsRest, Bitmaps1Class.gdiNoteVsRestButtonBits, Bitmaps1Class.gdiNoteVsRestButtonSelectedBits, group, null);
            SetButton(toolStripButtonNoteVsRest2, Bitmaps1Class.gdiRestVsNoteButtonBits, Bitmaps1Class.gdiRestVsNoteButtonSelectedBits, group, null);
            toolStripButtonNoteVsRest.Checked = true;
            //
            group = new ToolStripButton[] { toolStripButtonNoDot, toolStripButtonYesDot };
            SetButton(toolStripButtonNoDot, Bitmaps1Class.gdiNoDotButtonBits, Bitmaps1Class.gdiNoDotButtonSelectedBits, group, null);
            SetButton(toolStripButtonYesDot, Bitmaps1Class.gdiYesDotButtonBits, Bitmaps1Class.gdiYesDotButtonSelectedBits, group, null);
            toolStripButtonNoDot.Checked = true;
            //
            group = new ToolStripButton[] { toolStripButtonDiv1, toolStripButtonDiv3, toolStripButtonDiv5, toolStripButtonDiv7 };
            SetButton(toolStripButtonDiv1, Bitmaps1Class.gdiDiv1ButtonBits, Bitmaps1Class.gdiDiv1ButtonSelectedBits, group, null);
            SetButton(toolStripButtonDiv3, Bitmaps1Class.gdiDiv3ButtonBits, Bitmaps1Class.gdiDiv3ButtonSelectedBits, group, null);
            SetButton(toolStripButtonDiv5, Bitmaps1Class.gdiDiv5ButtonBits, Bitmaps1Class.gdiDiv5ButtonSelectedBits, group, null);
            SetButton(toolStripButtonDiv7, Bitmaps1Class.gdiDiv7ButtonBits, Bitmaps1Class.gdiDiv7ButtonSelectedBits, group, null);
            toolStripButtonDiv1.Checked = true;

            PrepareInlineEditingMenu();

            trackViewControl.Focus();
        }

        private void SetButton(ToolStripButton button, Image unselected, Image selected, ToolStripButton[] group, ToolStripButton[] auxSet)
        {
            button.Image = unselected;
            button.Click += new EventHandler(delegate (object sender, EventArgs e)
            {
                ToolStripButton b = ((ToolStripButton)sender);
                if (!b.Checked)
                {
                    if (auxSet != null)
                    {
                        foreach (ToolStripButton bb in auxSet)
                        {
                            bb.Checked = true;
                        }
                    }
                    b.Checked = true;
                }
                foreach (ToolStripButton bb in group)
                {
                    if ((bb != b) && bb.Checked)
                    {
                        bb.Checked = false;
                    }
                }
            });
            button.CheckStateChanged += new EventHandler(delegate (object sender, EventArgs e)
            {
                ToolStripButton b = ((ToolStripButton)sender);
                b.Image = b.Checked ? selected : unselected;
            });
        }

        public void SetDefaultScrollPosition() // pass-through
        {
            trackViewControl.SetDefaultScrollPosition();
        }

        private void trackViewControl_Scroll(object sender, ScrollEventArgs e)
        {
            switch (e.ScrollOrientation)
            {
                case ScrollOrientation.HorizontalScroll:
                    trackObject.SavedHScrollPos = e.NewValue;
                    break;
                case ScrollOrientation.VerticalScroll:
                    trackObject.SavedVScrollPos = e.NewValue;
                    break;
            }
        }

        public void FocusToView()
        {
            trackViewControl.Focus();
        }

        public TrackViewControl View { get { return trackViewControl; } }


        // backgound menu code

        private ToolStripMenuItem backgroundToolStripMenuItem;
        private Dictionary<ToolStripMenuItem, TrackObjectRec> backgroundItems = new Dictionary<ToolStripMenuItem, TrackObjectRec>();

        private void PrepareMenuItemsBackground()
        {
            backgroundToolStripMenuItem.DropDownItems.Clear();
            backgroundItems.Clear();
            foreach (TrackObjectRec track in mainWindow.Document.TrackList)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(track.Name, null, backgroundMenuItem_Click);
                item.Checked = trackObject.BackgroundObjects.Contains(track);
                if (track == trackObject)
                {
                    item.Enabled = false;
                    item.Checked = true;
                }
                backgroundToolStripMenuItem.DropDownItems.Add(item);
                backgroundItems.Add(item, track);
            }
        }

        public void backgroundMenuItem_Click(object sender, EventArgs e)
        {
            TrackObjectRec selected;
            if (backgroundItems.TryGetValue((ToolStripMenuItem)sender, out selected))
            {
                if (selected != trackObject)
                {
                    if (trackObject.BackgroundObjects.Contains(selected))
                    {
                        trackObject.BackgroundObjects.Remove(selected);
                    }
                    else
                    {
                        trackObject.BackgroundObjects.Add(selected);
                    }
                }
            }
        }


        // inline editing menu code

        private ToolStripMenuItem inlineEditToolStripMenuItem;
        private Dictionary<ToolStripMenuItem, InlineParamVis> inlineEditingItems = new Dictionary<ToolStripMenuItem, InlineParamVis>();
        private KeyValuePair<InlineParamVis, ToolStripMenuItem>[] inlineEditingReverse;

        private void PrepareInlineEditingMenu()
        {
            List<KeyValuePair<InlineParamVis, ToolStripMenuItem>> reverseMap = new List<KeyValuePair<InlineParamVis, ToolStripMenuItem>>();
            for (int i = 0; i <= (int)InlineParamVis.MaximumExponent; i++)
            {
                InlineParamVis param = (InlineParamVis)(1U << i);
                string label = EnumUtility.GetDescription(param, null);
                ToolStripMenuItem menuItem = new ToolStripMenuItem(label, null, inlineEditingMenuItem_Click);
                inlineEditToolStripMenuItem.DropDownItems.Add(menuItem);
                inlineEditingItems.Add(menuItem, param);
                reverseMap.Add(new KeyValuePair<InlineParamVis, ToolStripMenuItem>(param, menuItem));
            }
            inlineEditingReverse = reverseMap.ToArray();
        }

        private KeyValuePair<InlineParamVis, ToolStripMenuItem>[] GetInlineOptions()
        {
            return inlineEditingReverse;
        }

        private bool FindInlineMenuItem(
            ToolStripMenuItem item,
            out InlineParamVis option)
        {
            return inlineEditingItems.TryGetValue(item, out option);
        }

        public void inlineEditingMenuItem_Click(object sender, EventArgs e)
        {
            InlineParamVis inlineOption;
            if (FindInlineMenuItem((ToolStripMenuItem)sender, out inlineOption))
            {
                trackObject.InlineParamVis = trackObject.InlineParamVis ^ inlineOption;
                noteParamStrip.InlineParamVis = trackObject.InlineParamVis; // resizes
                trackViewControl.RebuildInlineStrip();
            }
        }


        // contextual UI service for TrackViewControl

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool NoteReady
        {
            get
            {
                return toolStripButtonCommand.Checked
                    || toolStripButtonSixtyFourth.Checked
                    || toolStripButtonThirtySecond.Checked
                    || toolStripButtonSixteenth.Checked
                    || toolStripButtonEighth.Checked
                    || toolStripButtonQuarter.Checked
                    || toolStripButtonHalf.Checked
                    || toolStripButtonWhole.Checked
                    || toolStripButtonDouble.Checked
                    || toolStripButtonQuad.Checked;
            }
            set
            {
                if (!value)
                {
                    toolStripButtonArrow.PerformClick();
                }
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public NoteFlags NoteState
        {
            get
            {
                if (DesignMode)
                {
                    return (NoteFlags)0;
                }

                if (toolStripButtonCommand.Checked)
                {
                    return NoteFlags.eCommandFlag;
                }

                NoteFlags flags = 0;

                if (toolStripButtonSixtyFourth.Checked)
                {
                    flags |= NoteFlags.e64thNote;
                }
                else if (toolStripButtonThirtySecond.Checked)
                {
                    flags |= NoteFlags.e32ndNote;
                }
                else if (toolStripButtonSixteenth.Checked)
                {
                    flags |= NoteFlags.e16thNote;
                }
                else if (toolStripButtonEighth.Checked)
                {
                    flags |= NoteFlags.e8thNote;
                }
                else if (toolStripButtonQuarter.Checked)
                {
                    flags |= NoteFlags.e4thNote;
                }
                else if (toolStripButtonHalf.Checked)
                {
                    flags |= NoteFlags.e2ndNote;
                }
                else if (toolStripButtonWhole.Checked)
                {
                    flags |= NoteFlags.eWholeNote;
                }
                else if (toolStripButtonDouble.Checked)
                {
                    flags |= NoteFlags.eDoubleNote;
                }
                else if (toolStripButtonQuad.Checked)
                {
                    flags |= NoteFlags.eQuadNote;
                }

                if (toolStripButtonSharp.Checked)
                {
                    flags |= NoteFlags.eSharpModifier;
                }
                else if (toolStripButtonFlat.Checked)
                {
                    flags |= NoteFlags.eFlatModifier;
                }
                else Debug.Assert(toolStripButtonNatural.Checked);

                if (toolStripButtonNoteVsRest2.Checked)
                {
                    flags |= NoteFlags.eRestModifier;
                }
                else Debug.Assert(toolStripButtonNoteVsRest.Checked);

                if (toolStripButtonYesDot.Checked)
                {
                    flags |= NoteFlags.eDotModifier;
                }
                else Debug.Assert(toolStripButtonNoDot.Checked);

                if (toolStripButtonDiv3.Checked)
                {
                    flags |= NoteFlags.eDiv3Modifier;
                }
                else if (toolStripButtonDiv5.Checked)
                {
                    flags |= NoteFlags.eDiv5Modifier;
                }
                else if (toolStripButtonDiv7.Checked)
                {
                    flags |= NoteFlags.eDiv7Modifier;
                }
                else Debug.Assert(toolStripButtonDiv1.Checked && (NoteFlags.eDiv1Modifier == 0));

                return flags;
            }
            set
            {
                if ((value & NoteFlags.eCommandFlag) != 0)
                {
                    toolStripButtonCommand.PerformClick();
                    return;
                }

                if ((value & NoteFlags.eDurationMask) == NoteFlags.e64thNote)
                {
                    toolStripButtonSixtyFourth.PerformClick();
                }
                else if ((value & NoteFlags.eDurationMask) == NoteFlags.e32ndNote)
                {
                    toolStripButtonThirtySecond.PerformClick();
                }
                else if ((value & NoteFlags.eDurationMask) == NoteFlags.e16thNote)
                {
                    toolStripButtonSixteenth.PerformClick();
                }
                else if ((value & NoteFlags.eDurationMask) == NoteFlags.e8thNote)
                {
                    toolStripButtonEighth.PerformClick();
                }
                else if ((value & NoteFlags.eDurationMask) == NoteFlags.e4thNote)
                {
                    toolStripButtonQuarter.PerformClick();
                }
                else if ((value & NoteFlags.eDurationMask) == NoteFlags.e2ndNote)
                {
                    toolStripButtonHalf.PerformClick();
                }
                else if ((value & NoteFlags.eDurationMask) == NoteFlags.eWholeNote)
                {
                    toolStripButtonWhole.PerformClick();
                }
                else if ((value & NoteFlags.eDurationMask) == NoteFlags.eDoubleNote)
                {
                    toolStripButtonDouble.PerformClick();
                }
                else if ((value & NoteFlags.eDurationMask) == NoteFlags.eQuadNote)
                {
                    toolStripButtonQuad.PerformClick();
                }

                if ((value & NoteFlags.eSharpModifier) != 0)
                {
                    toolStripButtonSharp.PerformClick();
                }
                else if ((value & NoteFlags.eFlatModifier) != 0)
                {
                    toolStripButtonFlat.PerformClick();
                }
                else
                {
                    toolStripButtonNatural.PerformClick();
                }

                if ((value & NoteFlags.eRestModifier) != 0)
                {
                    toolStripButtonNoteVsRest2.PerformClick();
                }
                else
                {
                    toolStripButtonNoteVsRest.PerformClick();
                }

                if ((value & NoteFlags.eDotModifier) != 0)
                {
                    toolStripButtonYesDot.PerformClick();
                }
                else
                {
                    toolStripButtonNoDot.PerformClick();
                }

                if ((value & NoteFlags.eDivisionMask) == NoteFlags.eDiv3Modifier)
                {
                    toolStripButtonDiv3.PerformClick();
                }
                else if ((value & NoteFlags.eDivisionMask) == NoteFlags.eDiv5Modifier)
                {
                    toolStripButtonDiv5.PerformClick();
                }
                else if ((value & NoteFlags.eDivisionMask) == NoteFlags.eDiv7Modifier)
                {
                    toolStripButtonDiv7.PerformClick();
                }
                else
                {
                    toolStripButtonDiv1.PerformClick();
                }
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MainWindow MainWindow { get { return mainWindow; } }


        //

        public void EditNoteProperties()
        {
            NoteObjectRec selectedNote;

            if (trackViewControl.TryGetSelectedNote(out selectedNote))
            {
                if (selectedNote is NoteNoteObjectRec)
                {
                    NoteNoteObjectRec note = (NoteNoteObjectRec)selectedNote;
                    NoteNoteObjectRec copy = new NoteNoteObjectRec(note, new TrackObjectRec(new Document()));
                    using (NoteAttributeDialog noteAttributeDialog = new NoteAttributeDialog(copy))
                    {
                        DialogResult result = noteAttributeDialog.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            note.CopyFrom(copy);
                        }
                    }
                }
                else if (selectedNote is CommandNoteObjectRec)
                {
                    CommandNoteObjectRec command = (CommandNoteObjectRec)selectedNote;
                    EditCommandParameters.EditCommandAttributes(command);
                }
            }

            trackViewControl.TrackObjectAlteredAtSelection();
        }

        private void EditTrackProperties()
        {
            TrackObjectRec copy = TrackObjectRec.CloneProperties(trackObject, new Document());
            using (TrackAttributeDialog trackAttributeDialog = new TrackAttributeDialog(copy))
            {
                DialogResult result = trackAttributeDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    trackObject.CopyPropertiesFrom(copy);
                }
            }
        }


        // MenuStripManager methods

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            menuStripManager.ProcessCmdKeyDelegate(ref msg, keyData);

            return base.ProcessCmdKey(ref msg, keyData);
        }

        void IMenuStripManagerHandler.EnableMenuItems(MenuStripManager menuStrip)
        {
            menuStrip.editNotePropertiesToolStripMenuItem.Visible = true;
            if (trackViewControl.TrackViewIsASingleNoteSelected()
                || trackViewControl.TrackViewIsASingleCommandSelected())
            {
                menuStrip.editNotePropertiesToolStripMenuItem.Enabled = true;
                menuStrip.editNotePropertiesToolStripMenuItem.Text = trackViewControl.TrackViewIsASingleNoteSelected()
                    ? "Edit Note Attributes..."
                    : "Edit Command Attributes...";
            }

            menuStrip.editTrackPropertiesToolStripMenuItem.Visible = true;
            menuStrip.editTrackPropertiesToolStripMenuItem.Enabled = true;

            menuStrip.showSelectionToolStripMenuItem.Enabled = true;

            menuStrip.selectAllToolStripMenuItem.Enabled = true;

            if (trackViewControl.TrackViewIsThereInsertionPoint() || trackViewControl.TrackViewIsARangeSelected())
            {
                bool enablePaste = false;
                IDataObject dataObject = Clipboard.GetDataObject();
                foreach (string format in dataObject.GetFormats())
                {
                    if (String.Equals(format, TrackClipboard.ClipboardIdentifer))
                    {
                        enablePaste = true;
                        break;
                    }
                }
                if (enablePaste)
                {
                    menuStrip.pasteToolStripMenuItem.Enabled = true;
                }
            }
            if (trackViewControl.TrackViewIsASingleNoteSelected() || trackViewControl.TrackViewIsASingleCommandSelected())
            {
                menuStrip.clearToolStripMenuItem.Enabled = true;
                menuStrip.clearToolStripMenuItem.Text = "Delete Note";
            }

            if (trackViewControl.TrackViewIsARangeSelected())
            {
                menuStrip.clearToolStripMenuItem.Enabled = true;
                menuStrip.clearToolStripMenuItem.Text = "Delete Selection";
                menuStrip.cutToolStripMenuItem.Enabled = true;
                menuStrip.cutToolStripMenuItem.Text = "Cut Selection";
                menuStrip.copyToolStripMenuItem.Enabled = true;
                menuStrip.copyToolStripMenuItem.Text = "Copy Selection";
            }
            if (trackViewControl.UndoHelper.UndoAvailable)
            {
                menuStrip.undoToolStripMenuItem.Enabled = true;
                string undoLabel = trackViewControl.UndoHelper.UndoLabel;
                menuStrip.undoToolStripMenuItem.Text = String.Format("Undo {0}", undoLabel != null ? undoLabel : "Track Edit");
            }
            if (trackViewControl.UndoHelper.RedoAvailable)
            {
                menuStrip.redoToolStripMenuItem.Enabled = true;
                string redoLabel = trackViewControl.UndoHelper.RedoLabel;
                menuStrip.redoToolStripMenuItem.Text = String.Format("Redo {0}", redoLabel != null ? redoLabel : "Track Edit");
            }

            if (trackViewControl.TrackViewIsARangeSelected() || trackViewControl.TrackViewIsASingleNoteSelected())
            {
                menuStrip.transposeToolStripMenuItem.Enabled = true;
            }

            menuStrip.playTrackFromHereToolStripMenuItem.Enabled = true;
            menuStrip.playTrackFromHereToolStripMenuItem.Visible = true;
            menuStrip.playAllFromHereToolStripMenuItem.Enabled = true;
            menuStrip.playAllFromHereToolStripMenuItem.Visible = true;
            menuStrip.playTrackFromHereToDiskToolStripMenuItem.Enabled = true;
            menuStrip.playTrackFromHereToDiskToolStripMenuItem.Visible = true;
            menuStrip.playAllFromHereToDiskToolStripMenuItem.Enabled = true;
            menuStrip.playAllFromHereToDiskToolStripMenuItem.Visible = true;

            menuStrip.goToLineToolStripMenuItem.Enabled = true;
            menuStrip.goToLineToolStripMenuItem.Text = "Go To Measure...";

            if (trackViewControl.TrackViewIsARangeSelected() || trackViewControl.TrackViewIsASingleNoteSelected())
            {
                menuStrip.doubleDurationToolStripMenuItem.Enabled = true;
                menuStrip.halveDurationToolStripMenuItem.Enabled = true;
            }

            foreach (KeyValuePair<InlineParamVis, ToolStripMenuItem> inlineOption in GetInlineOptions())
            {
                inlineOption.Value.Enabled = true;
                inlineOption.Value.Checked = (trackObject.InlineParamVis & inlineOption.Key) != 0;
            }

            PrepareMenuItemsBackground();
        }

        bool IMenuStripManagerHandler.ExecuteMenuItem(MenuStripManager menuStrip, ToolStripMenuItem menuItem)
        {
            if (menuItem == menuStrip.editNotePropertiesToolStripMenuItem)
            {
                EditNoteProperties();
                return true;
            }
            else if (menuItem == menuStrip.editTrackPropertiesToolStripMenuItem)
            {
                EditTrackProperties();
                return true;
            }
            else if (menuItem == menuStrip.selectAllToolStripMenuItem)
            {
                trackViewControl.TrackViewSelectAll();
                return true;
            }
            else if (menuItem == menuStrip.pasteToolStripMenuItem)
            {
                trackViewControl.TrackViewAttemptPaste();
                return true;
            }
            else if (menuItem == menuStrip.clearToolStripMenuItem)
            {
                if (trackViewControl.TrackViewIsASingleNoteSelected() || trackViewControl.TrackViewIsASingleCommandSelected())
                {
                    trackViewControl.TrackViewDeleteSingleNoteOrCommand();
                }
                else if (trackViewControl.TrackViewIsARangeSelected())
                {
                    trackViewControl.TrackViewDeleteRangeSelection();
                }
                trackViewControl.TrackViewShowSelection();
                return true;
            }
            else if (menuItem == menuStrip.cutToolStripMenuItem)
            {
                trackViewControl.TrackViewCutRangeSelection();
                trackViewControl.TrackViewShowSelection();
                return true;
            }
            else if (menuItem == menuStrip.copyToolStripMenuItem)
            {
                trackViewControl.TrackViewCopyRangeSelection();
                return true;
            }
            else if (menuItem == menuStrip.undoToolStripMenuItem)
            {
                trackViewControl.UndoHelper.Undo();
                return true;
            }
            else if (menuItem == menuStrip.redoToolStripMenuItem)
            {
                trackViewControl.UndoHelper.Redo();
                return true;
            }
            else if ((menuItem == menuStrip.playTrackFromHereToolStripMenuItem)
                || (menuItem == menuStrip.playTrackFromHereToDiskToolStripMenuItem))
            {
                IPlayPrefsProvider playPrefsProvider = mainWindow.GetPlayPrefsProvider();

                int StartIndex;
                if (trackViewControl.TrackViewIsARangeSelected())
                {
                    StartIndex = trackViewControl.TrackViewGetRangeStart();
                }
                else if (trackViewControl.TrackViewIsThereInsertionPoint())
                {
                    StartIndex = trackViewControl.TrackViewGetInsertionPointIndex();
                }
                else if (trackViewControl.TrackViewIsASingleNoteSelected()
                    || trackViewControl.TrackViewIsASingleCommandSelected())
                {
                    StartIndex = trackViewControl.TrackViewGetSingleNoteSelectionFrameNumber();
                }
                else
                {
                    StartIndex = 0; /* default to the beginning for other selections */
                }

                List<TrackObjectRec> TrackList = new List<TrackObjectRec>();
                TrackList.Add(trackObject);

                if (menuItem == menuStrip.playTrackFromHereToolStripMenuItem)
                {
#if true // prevents "Add New Data Source..." from working
                    SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                        mainWindow.DisplayName,
                        OutputDeviceEnumerator.OutputDeviceGetDestination,
                        OutputDeviceEnumerator.CreateOutputDeviceDestinationHandler,
                        new OutputDeviceArguments(playPrefsProvider.BufferDuration),
                        SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerMainLoop,
                        new SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                            mainWindow,
                            mainWindow.Document,
                            TrackList,
                            trackObject,
                            StartIndex,
                            playPrefsProvider.SamplingRate,
                            playPrefsProvider.EnvelopeUpdateRate,
                            playPrefsProvider.NumChannels,
                            (LargeBCDType)playPrefsProvider.DefaultBeatsPerMinute,
                            playPrefsProvider.OverallVolumeScalingFactor,
                            (LargeBCDType)playPrefsProvider.ScanningGap,
                            playPrefsProvider.OutputNumBits,
                            false/*clipWarning*/,
                            playPrefsProvider.Oversampling,
                            playPrefsProvider.ShowSummary,
                            playPrefsProvider.Deterministic,
                            playPrefsProvider.Seed,
                            null/*automationSettings*/),
                        SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerCompletion,
                        mainWindow,
                        playPrefsProvider.NumChannels,
                        playPrefsProvider.OutputNumBits,
                        playPrefsProvider.SamplingRate,
                        playPrefsProvider.Oversampling,
                        true/*showProgressWindow*/,
                        true/*modal*/);
#endif
                }
                else
                {
#if true // prevents "Add New Data Source..." from working
                    SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.Do(
                        mainWindow.DisplayName,
                        OutputSelectableFileDestinationHandler.OutputSelectableFileGetDestination,
                        OutputSelectableFileDestinationHandler.CreateOutputSelectableFileDestinationHandler,
                        new OutputSelectableFileArguments(),
                        SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerMainLoop,
                        new SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>(
                            mainWindow,
                            mainWindow.Document,
                            TrackList,
                            trackObject,
                            StartIndex,
                            playPrefsProvider.SamplingRate,
                            playPrefsProvider.EnvelopeUpdateRate,
                            playPrefsProvider.NumChannels,
                            (LargeBCDType)playPrefsProvider.DefaultBeatsPerMinute,
                            playPrefsProvider.OverallVolumeScalingFactor,
                            (LargeBCDType)playPrefsProvider.ScanningGap,
                            playPrefsProvider.OutputNumBits,
                            false/*clipWarning*/,
                            playPrefsProvider.Oversampling,
                            playPrefsProvider.ShowSummary,
                            playPrefsProvider.Deterministic,
                            playPrefsProvider.Seed,
                            null/*automationSettings*/),
                        SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerCompletion,
                        mainWindow,
                        playPrefsProvider.NumChannels,
                        playPrefsProvider.OutputNumBits,
                        playPrefsProvider.SamplingRate,
                        playPrefsProvider.Oversampling,
                        true/*showProgressWindow*/,
                        true/*modal*/);
#endif
                }
                return true;
            }
            else if ((menuItem == menuStrip.playAllFromHereToolStripMenuItem)
                || (menuItem == menuStrip.playAllFromHereToDiskToolStripMenuItem))
            {
                IPlayPrefsProvider playPrefsProvider = mainWindow.GetPlayPrefsProvider();

                int StartIndex;
                if (trackViewControl.TrackViewIsARangeSelected())
                {
                    StartIndex = trackViewControl.TrackViewGetRangeStart();
                }
                else if (trackViewControl.TrackViewIsThereInsertionPoint())
                {
                    StartIndex = trackViewControl.TrackViewGetInsertionPointIndex();
                }
                else if (trackViewControl.TrackViewIsASingleNoteSelected()
                    || trackViewControl.TrackViewIsASingleCommandSelected())
                {
                    StartIndex = trackViewControl.TrackViewGetSingleNoteSelectionFrameNumber();
                }
                else
                {
                    StartIndex = 0; /* default to the beginning for other selections */
                }

                List<TrackObjectRec> TrackList = new List<TrackObjectRec>();
                TrackList.Add(trackObject); // we must include the current track
                foreach (TrackObjectRec track in playPrefsProvider.IncludedTracks)
                {
                    if (track != trackObject)
                    {
                        TrackList.Add(track);
                    }
                }

                if (menuItem == menuStrip.playAllFromHereToolStripMenuItem)
                {
#if true // prevents "Add New Data Source..." from working
                    SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                        mainWindow.DisplayName,
                        OutputDeviceEnumerator.OutputDeviceGetDestination,
                        OutputDeviceEnumerator.CreateOutputDeviceDestinationHandler,
                        new OutputDeviceArguments(playPrefsProvider.BufferDuration),
                        SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerMainLoop,
                        new SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                            mainWindow,
                            mainWindow.Document,
                            TrackList,
                            trackObject,
                            StartIndex,
                            playPrefsProvider.SamplingRate,
                            playPrefsProvider.EnvelopeUpdateRate,
                            playPrefsProvider.NumChannels,
                            (LargeBCDType)playPrefsProvider.DefaultBeatsPerMinute,
                            playPrefsProvider.OverallVolumeScalingFactor,
                            (LargeBCDType)playPrefsProvider.ScanningGap,
                            playPrefsProvider.OutputNumBits,
                            false/*clipWarning*/,
                            playPrefsProvider.Oversampling,
                            playPrefsProvider.ShowSummary,
                            playPrefsProvider.Deterministic,
                            playPrefsProvider.Seed,
                            null/*automationSettings*/),
                        SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerCompletion,
                        mainWindow,
                        playPrefsProvider.NumChannels,
                        playPrefsProvider.OutputNumBits,
                        playPrefsProvider.SamplingRate,
                        playPrefsProvider.Oversampling,
                        true/*showProgressWindow*/,
                        true/*modal*/);
#endif
                }
                else
                {
#if true // prevents "Add New Data Source..." from working
                    SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.Do(
                        mainWindow.DisplayName,
                        OutputSelectableFileDestinationHandler.OutputSelectableFileGetDestination,
                        OutputSelectableFileDestinationHandler.CreateOutputSelectableFileDestinationHandler,
                        new OutputSelectableFileArguments(),
                        SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerMainLoop,
                        new SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>(
                            mainWindow,
                            mainWindow.Document,
                            TrackList,
                            trackObject,
                            StartIndex,
                            playPrefsProvider.SamplingRate,
                            playPrefsProvider.EnvelopeUpdateRate,
                            playPrefsProvider.NumChannels,
                            (LargeBCDType)playPrefsProvider.DefaultBeatsPerMinute,
                            playPrefsProvider.OverallVolumeScalingFactor,
                            (LargeBCDType)playPrefsProvider.ScanningGap,
                            playPrefsProvider.OutputNumBits,
                            false/*clipWarning*/,
                            playPrefsProvider.Oversampling,
                            playPrefsProvider.ShowSummary,
                            playPrefsProvider.Deterministic,
                            playPrefsProvider.Seed,
                            null/*automationSettings*/),
                        SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerCompletion,
                        mainWindow,
                        playPrefsProvider.NumChannels,
                        playPrefsProvider.OutputNumBits,
                        playPrefsProvider.SamplingRate,
                        playPrefsProvider.Oversampling,
                        true/*showProgressWindow*/,
                        true/*modal*/);
#endif
                }
                return true;
            }
            else if (menuItem == menuStrip.showSelectionToolStripMenuItem)
            {
                trackViewControl.TrackViewShowSelection();
                return true;
            }
            else if (menuItem == menuStrip.transposeToolStripMenuItem)
            {
                double Transpose = 0;
                CmdDlgOneParam.CommandDialogOneParam("Transpose half-steps:", null, ref Transpose);
                if (Transpose != 0)
                {
                    trackViewControl.TrackViewTransposeSelection((int)Transpose);
                }
                return true;
            }
            else if (menuItem == menuStrip.goToLineToolStripMenuItem)
            {
                double NewMeasure = 0;
                CmdDlgOneParam.CommandDialogOneParam("Go To Measure:", null, ref NewMeasure);
                if (NewMeasure != 0)
                {
                    trackViewControl.TrackViewShowMeasure((int)NewMeasure);
                }
                return true;
            }
            else if (menuItem == menuStrip.doubleDurationToolStripMenuItem)
            {
                trackViewControl.TrackViewAdjustDuration(2, 1);
                return true;
            }
            else if (menuItem == menuStrip.halveDurationToolStripMenuItem)
            {
                trackViewControl.TrackViewAdjustDuration(1, 2);
                return true;
            }

            return false;
        }
    }
}
