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
    public partial class TrackWindow : Form, ITrackViewContextUI, IMenuStripManagerHandler, IGlobalNameChange
    {
        private readonly Registration registration;
        private readonly TrackObjectRec trackObject;
        private readonly MainWindow mainWindow;

        public TrackWindow(Registration registration, TrackObjectRec trackObject, MainWindow mainWindow)
        {
            this.registration = registration;
            this.trackObject = trackObject;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            menuStripManager.SetGlobalHandler(mainWindow);

            trackViewControl.NoteView = noteViewControl;
            trackViewControl.ContextUI = this;

            //documentBindingSource.Add(document);
            trackObjectRecBindingSource.Add(trackObject);

            trackViewControl.SetTrackObject(trackObject);
            trackViewControl.RestoreSavedScrollPosition(trackObject.SavedHScrollPos, trackObject.SavedVScrollPos);
            trackViewControl.Scroll += new ScrollEventHandler(trackViewControl_Scroll);

            GlobalNameChanged();

            registration.Register(trackObject, this);

            backgroundToolStripMenuItem = new ToolStripMenuItem("Background", null, new EventHandler(backgroundMenuItem_Click));
            menuStripManager.ContainedMenuStrip.Items.Add(backgroundToolStripMenuItem);

            ToolStripButton[] group;
            //
            group = new ToolStripButton[] { toolStripButtonArrow, toolStripButtonCommand, toolStripButtonSixtyFourth, toolStripButtonThirtySecond, toolStripButtonSixteenth, toolStripButtonEighth, toolStripButtonQuarter, toolStripButtonHalf, toolStripButtonWhole, toolStripButtonDouble, toolStripButtonQuad };
            SetButton(toolStripButtonArrow, Bitmaps.gdiArrowButtonBits, Bitmaps.gdiArrowButtonSelectedBits, group, new ToolStripButton[] { toolStripButtonQuarter, toolStripButtonDiv1, toolStripButtonNatural, toolStripButtonNoDot, toolStripButtonNoteVsRest });
            toolStripButtonArrow.Checked = true;
            //
            SetButton(toolStripButtonCommand, Bitmaps.gdiCommandButtonBits, Bitmaps.gdiCommandButtonSelectedBits, group, null);
            //
            SetButton(toolStripButtonSixtyFourth, Bitmaps.gdiSixtyFourthButtonBits, Bitmaps.gdiSixtyFourthButtonSelectedBits, group, null);
            SetButton(toolStripButtonThirtySecond, Bitmaps.gdiThirtySecondButtonBits, Bitmaps.gdiThirtySecondButtonSelectedBits, group, null);
            SetButton(toolStripButtonSixteenth, Bitmaps.gdiSixteenthButtonBits, Bitmaps.gdiSixteenthButtonSelectedBits, group, null);
            SetButton(toolStripButtonEighth, Bitmaps.gdiEighthButtonBits, Bitmaps.gdiEighthButtonSelectedBits, group, null);
            SetButton(toolStripButtonQuarter, Bitmaps.gdiQuarterButtonBits, Bitmaps.gdiQuarterButtonSelectedBits, group, null);
            SetButton(toolStripButtonHalf, Bitmaps.gdiHalfButtonBits, Bitmaps.gdiHalfButtonSelectedBits, group, null);
            SetButton(toolStripButtonWhole, Bitmaps.gdiWholeButtonBits, Bitmaps.gdiWholeButtonSelectedBits, group, null);
            SetButton(toolStripButtonDouble, Bitmaps.gdiDoubleButtonBits, Bitmaps.gdiDoubleButtonSelectedBits, group, null);
            SetButton(toolStripButtonQuad, Bitmaps.gdiQuadButtonBits, Bitmaps.gdiQuadButtonSelectedBits, group, null);
            //
            group = new ToolStripButton[] { toolStripButtonSharp, toolStripButtonFlat, toolStripButtonNatural };
            SetButton(toolStripButtonSharp, Bitmaps.gdiSharpButtonBits, Bitmaps.gdiSharpButtonSelectedBits, group, null);
            SetButton(toolStripButtonFlat, Bitmaps.gdiFlatButtonBits, Bitmaps.gdiFlatButtonSelectedBits, group, null);
            SetButton(toolStripButtonNatural, Bitmaps.gdiNaturalButtonBits, Bitmaps.gdiNaturalButtonSelectedBits, group, null);
            toolStripButtonNatural.Checked = true;
            //
            group = new ToolStripButton[] { toolStripButtonNoteVsRest, toolStripButtonNoteVsRest2 };
            SetButton(toolStripButtonNoteVsRest, Bitmaps.gdiNoteVsRestButtonBits, Bitmaps.gdiNoteVsRestButtonSelectedBits, group, null);
            SetButton(toolStripButtonNoteVsRest2, Bitmaps.gdiNoteVsRestButtonBits, Bitmaps.gdiNoteVsRestButtonSelectedBits, group, null);
            toolStripButtonNoteVsRest.Checked = true;
            //
            group = new ToolStripButton[] { toolStripButtonNoDot, toolStripButtonYesDot };
            SetButton(toolStripButtonNoDot, Bitmaps.gdiNoDotButtonBits, Bitmaps.gdiNoDotButtonSelectedBits, group, null);
            SetButton(toolStripButtonYesDot, Bitmaps.gdiYesDotButtonBits, Bitmaps.gdiYesDotButtonSelectedBits, group, null);
            toolStripButtonNoDot.Checked = true;
            //
            group = new ToolStripButton[] { toolStripButtonDiv1, toolStripButtonDiv3, toolStripButtonDiv5, toolStripButtonDiv7 };
            SetButton(toolStripButtonDiv1, Bitmaps.gdiDiv1ButtonBits, Bitmaps.gdiDiv1ButtonSelectedBits, group, null);
            SetButton(toolStripButtonDiv3, Bitmaps.gdiDiv3ButtonBits, Bitmaps.gdiDiv3ButtonSelectedBits, group, null);
            SetButton(toolStripButtonDiv5, Bitmaps.gdiDiv5ButtonBits, Bitmaps.gdiDiv5ButtonSelectedBits, group, null);
            SetButton(toolStripButtonDiv7, Bitmaps.gdiDiv7ButtonBits, Bitmaps.gdiDiv7ButtonSelectedBits, group, null);
            toolStripButtonDiv1.Checked = true;
        }

        private void SetButton(ToolStripButton button, Image unselected, Image selected, ToolStripButton[] group, ToolStripButton[] auxSet)
        {
            button.Image = unselected;
            button.Click += new EventHandler(delegate(object sender, EventArgs e)
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
            button.CheckStateChanged += new EventHandler(delegate(object sender, EventArgs e)
            {
                ToolStripButton b = ((ToolStripButton)sender);
                b.Image = b.Checked ? selected : unselected;
            });
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            registration.Unregister(trackObject, this);
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            PersistWindowRect.OnShown(this, trackObject.SavedWindowXLoc, trackObject.SavedWindowYLoc, trackObject.SavedWindowWidth, trackObject.SavedWindowHeight);

            base.OnShown(e);

            // deferred until OnShown because that's when trackViewControl.Height is finally valid
            trackViewControl.SetDefaultScrollPosition();
        }

        private void ResizeMove()
        {
            if (Visible)
            {
                short x, y, width, height;
                PersistWindowRect.OnResize(this, out x, out y, out width, out height);
                trackObject.SavedWindowXLoc = x;
                trackObject.SavedWindowYLoc = y;
                trackObject.SavedWindowWidth = width;
                trackObject.SavedWindowHeight = height;
            }
        }

        protected override void OnMove(EventArgs e)
        {
            ResizeMove();
            base.OnMove(e);
        }

        protected override void OnResize(EventArgs e)
        {
            ResizeMove();
            base.OnResize(e);
        }

        void trackViewControl_Scroll(object sender, ScrollEventArgs e)
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

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            menuStripManager.SetActiveHandler(this);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            menuStripManager.SetActiveHandler(null);
            base.OnDeactivate(e);
        }


        // backgound menu code

        private readonly ToolStripMenuItem backgroundToolStripMenuItem;
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

        private void backgroundMenuItem_Click(object sender, EventArgs e)
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


        // contextual UI service for TrackViewControl

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

        public NoteFlags NoteState
        {
            get
            {
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

            menuStrip.deleteObjectToolStripMenuItem.Enabled = true;
            menuStrip.deleteObjectToolStripMenuItem.Text = "Delete Track";

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
            else if (menuItem == menuStrip.closeDocumentToolStripMenuItem)
            {
                Close();
                return true;
            }
            else if (menuItem == menuStrip.deleteObjectToolStripMenuItem)
            {
                Close();
                mainWindow.DeleteObject(trackObject, mainWindow.Document.TrackList);
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
                int StartIndex;
                List<TrackObjectRec> TrackList = new List<TrackObjectRec>();

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
                TrackList.Add(trackObject);
                if (menuItem == menuStrip.playTrackFromHereToolStripMenuItem)
                {
#if true // prevents "Add New Data Source..." from working
                    SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                        mainWindow.DisplayName,
                        OutputDeviceDestinationHandler.OutputDeviceGetDestination,
                        OutputDeviceDestinationHandler.CreateOutputDeviceDestinationHandler,
                        new OutputDeviceArguments(mainWindow.Document.BufferDuration),
                        SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerMainLoop,
                        new SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                            mainWindow,
                            mainWindow.Document,
                            TrackList,
                            trackObject,
                            StartIndex,
                            mainWindow.Document.SamplingRate,
                            mainWindow.Document.EnvelopeUpdateRate,
                            mainWindow.Document.NumChannels,
                            (LargeBCDType)mainWindow.Document.DefaultBeatsPerMinute,
                            mainWindow.Document.OverallVolumeScalingFactor,
                            (LargeBCDType)mainWindow.Document.ScanningGap,
                            mainWindow.Document.OutputNumBits,
                            false/*clipWarning*/,
                            mainWindow.Document.Oversampling,
                            mainWindow.Document.ShowSummary,
                            mainWindow.Document.Deterministic,
                            mainWindow.Document.Seed,
                            null/*automationSettings*/),
                        SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerCompletion,
                        mainWindow,
                        mainWindow.Document.NumChannels,
                        mainWindow.Document.OutputNumBits,
                        mainWindow.Document.SamplingRate,
                        mainWindow.Document.Oversampling,
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
                            mainWindow.Document.SamplingRate,
                            mainWindow.Document.EnvelopeUpdateRate,
                            mainWindow.Document.NumChannels,
                            (LargeBCDType)mainWindow.Document.DefaultBeatsPerMinute,
                            mainWindow.Document.OverallVolumeScalingFactor,
                            (LargeBCDType)mainWindow.Document.ScanningGap,
                            mainWindow.Document.OutputNumBits,
                            false/*clipWarning*/,
                            mainWindow.Document.Oversampling,
                            mainWindow.Document.ShowSummary,
                            mainWindow.Document.Deterministic,
                            mainWindow.Document.Seed,
                            null/*automationSettings*/),
                        SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerCompletion,
                        mainWindow,
                        mainWindow.Document.NumChannels,
                        mainWindow.Document.OutputNumBits,
                        mainWindow.Document.SamplingRate,
                        mainWindow.Document.Oversampling,
                        true/*showProgressWindow*/,
                        true/*modal*/);
#endif
                }
            }
            else if ((menuItem == menuStrip.playAllFromHereToolStripMenuItem)
                || (menuItem == menuStrip.playAllFromHereToDiskToolStripMenuItem))
            {
                int StartIndex;
                List<TrackObjectRec> TrackList;

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
                TrackList = new List<TrackObjectRec>(mainWindow.Document.TrackList);
                int TrackListScan = 0;
                /* keep only the tracks that are marked to be played */
                while (TrackListScan < TrackList.Count)
                {
                    TrackObjectRec MaybeTrack = TrackList[TrackListScan];
                    if ((MaybeTrack != trackObject) /* we MUST keep the current track! */
                        && !MaybeTrack.IncludeThisTrackInFinalPlayback)
                    {
                        TrackList.RemoveAt(TrackListScan);
                    }
                    else
                    {
                        TrackListScan += 1;
                    }
                }
                if (menuItem == menuStrip.playAllFromHereToolStripMenuItem)
                {
#if true // prevents "Add New Data Source..." from working
                    SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.Do(
                        mainWindow.DisplayName,
                        OutputDeviceDestinationHandler.OutputDeviceGetDestination,
                        OutputDeviceDestinationHandler.CreateOutputDeviceDestinationHandler,
                        new OutputDeviceArguments(mainWindow.Document.BufferDuration),
                        SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerMainLoop,
                        new SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>(
                            mainWindow,
                            mainWindow.Document,
                            TrackList,
                            trackObject,
                            StartIndex,
                            mainWindow.Document.SamplingRate,
                            mainWindow.Document.EnvelopeUpdateRate,
                            mainWindow.Document.NumChannels,
                            (LargeBCDType)mainWindow.Document.DefaultBeatsPerMinute,
                            mainWindow.Document.OverallVolumeScalingFactor,
                            (LargeBCDType)mainWindow.Document.ScanningGap,
                            mainWindow.Document.OutputNumBits,
                            false/*clipWarning*/,
                            mainWindow.Document.Oversampling,
                            mainWindow.Document.ShowSummary,
                            mainWindow.Document.Deterministic,
                            mainWindow.Document.Seed,
                            null/*automationSettings*/),
                        SynthesizerGeneratorParams<OutputDeviceDestination, OutputDeviceArguments>.SynthesizerCompletion,
                        mainWindow,
                        mainWindow.Document.NumChannels,
                        mainWindow.Document.OutputNumBits,
                        mainWindow.Document.SamplingRate,
                        mainWindow.Document.Oversampling,
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
                            mainWindow.Document.SamplingRate,
                            mainWindow.Document.EnvelopeUpdateRate,
                            mainWindow.Document.NumChannels,
                            (LargeBCDType)mainWindow.Document.DefaultBeatsPerMinute,
                            mainWindow.Document.OverallVolumeScalingFactor,
                            (LargeBCDType)mainWindow.Document.ScanningGap,
                            mainWindow.Document.OutputNumBits,
                            false/*clipWarning*/,
                            mainWindow.Document.Oversampling,
                            mainWindow.Document.ShowSummary,
                            mainWindow.Document.Deterministic,
                            mainWindow.Document.Seed,
                            null/*automationSettings*/),
                        SynthesizerGeneratorParams<OutputSelectableFileDestination, OutputSelectableFileArguments>.SynthesizerCompletion,
                        mainWindow,
                        mainWindow.Document.NumChannels,
                        mainWindow.Document.OutputNumBits,
                        mainWindow.Document.SamplingRate,
                        mainWindow.Document.Oversampling,
                        true/*showProgressWindow*/,
                        true/*modal*/);
#endif
                }
            }
            else if (menuItem == menuStrip.showSelectionToolStripMenuItem)
            {
                trackViewControl.TrackViewShowSelection();
            }
            else if (menuItem == menuStrip.transposeToolStripMenuItem)
            {
                double Transpose = 0;
                CmdDlgOneParam.CommandDialogOneParam("Transpose half-steps:", null, ref Transpose);
                if (Transpose != 0)
                {
                    trackViewControl.TrackViewTransposeSelection((int)Transpose);
                }
            }
            else if (menuItem == menuStrip.goToLineToolStripMenuItem)
            {
                double NewMeasure = 0;
                CmdDlgOneParam.CommandDialogOneParam("Go To Measure:", null, ref NewMeasure);
                if (NewMeasure != 0)
                {
                    trackViewControl.TrackViewShowMeasure((int)NewMeasure);
                }
            }
            else if (menuItem == menuStrip.doubleDurationToolStripMenuItem)
            {
                trackViewControl.TrackViewAdjustDuration(2, 1);
            }
            else if (menuItem == menuStrip.halveDurationToolStripMenuItem)
            {
                trackViewControl.TrackViewAdjustDuration(1, 2);
            }

            return false;
        }


        //

        public void GlobalNameChanged()
        {
            this.Text = String.Format("{0} - {1}", mainWindow.DisplayName, trackObject.Name);
        }
    }
}
