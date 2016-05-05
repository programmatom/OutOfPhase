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
    public partial class SectionEditDialog : Form
    {
        private readonly Registration registration;
        private readonly Document document;
        private readonly IMainWindowServices mainWindow;

        private readonly MyBindingList<object> displayList = new MyBindingList<object>();

        public SectionEditDialog(Registration registration, Document document, IMainWindowServices mainWindow)
        {
            this.registration = registration;
            this.document = document;
            this.mainWindow = mainWindow;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            myListBoxSections.SetUnderlying(displayList, DisplayNameOfListEntry);
            myListBoxSections.SelectionChanged += new EventHandler(delegate (object sender, EventArgs e) { OnSelectionChanged(); });
            myListBoxSections.DoubleClick2 += new MyListBox.DoubleClick2EventHandler(myListBoxSections_DoubleClick2);

            document.SectionList.ListChanged += SectionList_ListChanged;
            document.TrackList.ListChanged += TrackList_ListChanged;

            RebuildScrollingList();
            OnSelectionChanged();

            Disposed += SectionEditDialog_Disposed;

            registration.Register(document.SectionList, this);
        }

        private void SectionEditDialog_Disposed(object sender, EventArgs e)
        {
            document.SectionList.ListChanged -= SectionList_ListChanged;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            registration.Unregister(document.SectionList, this);
            base.OnFormClosed(e);
        }

        private void TrackList_ListChanged(object sender, ListChangedEventArgs e)
        {
            RebuildScrollingList();
        }

        private void SectionList_ListChanged(object sender, ListChangedEventArgs e)
        {
            if ((e.PropertyDescriptor == null/*newitem*/)
                || String.Equals(e.PropertyDescriptor.Name, SectionObjectRec.Name_PropertyName))
            {
                RebuildScrollingList();
            }
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        private static string DisplayNameOfListEntry(object obj)
        {
            if (obj is string)
            {
                return (string)obj;
            }
            else if (obj is SectionObjectRec)
            {
                return String.Format("{0}", ((SectionObjectRec)obj).Name);
            }
            else if (obj is TrackObjectRec)
            {
                return String.Format("   {0}", ((TrackObjectRec)obj).Name);
            }
            else
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
        }

        private void RebuildScrollingList()
        {
            object selection = myListBoxSections.SelectedItem;

            displayList.Clear();

            TrackObjectRec[] tracks = new List<TrackObjectRec>(document.TrackList).ToArray();

            /* set stable sort subkey */
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].AuxVal = i;
            }

            Array.Sort(
                tracks,
                delegate (TrackObjectRec left, TrackObjectRec right)
                {
                    return Synthesizer.SynthStateRec.CompareTracksOnSection(left, right, document.SectionList);
                });

            SectionObjectRec section = null;
            bool firstTry = true;
            for (int i = 0; i < tracks.Length; i++)
            {
                TrackObjectRec track = tracks[i];

                /* create the section name header */
                if (firstTry || (section != track.Section))
                {
                    firstTry = false;
                    section = track.Section;
                    if (section == null)
                    {
                        displayList.Add((string)"(Default)");
                    }
                    else
                    {
                        displayList.Add(section);
                    }
                }

                /* add the track item */
                displayList.Add(track);
            }

            /* add any sections we missed */
            for (int j = 0; j < document.SectionList.Count; j++)
            {
                section = document.SectionList[j];

                bool referenced = false;
                for (int i = 0; !referenced && (i < tracks.Length); i++)
                {
                    TrackObjectRec track = document.TrackList[i];
                    if (track.Section == section)
                    {
                        referenced = true;
                    }
                }

                if (!referenced)
                {
                    displayList.Add(section);
                }
            }

            for (int i = 0; i < displayList.Count; i++)
            {
                if (selection == displayList[i])
                {
                    myListBoxSections.SelectItem(i, true/*clearOtherSelections*/);
                }
            }
        }

        private void OnSelectionChanged()
        {
            if (myListBoxSections.SelectedItem == null)
            {
                buttonDeleteSection.Enabled = false;
                buttonEditSection.Enabled = false;
                buttonChangeSection.Enabled = false;
            }
            else if (myListBoxSections.SelectedItem is string)
            {
                buttonDeleteSection.Enabled = false;
                buttonEditSection.Enabled = false;
                buttonChangeSection.Enabled = false;
            }
            else if (myListBoxSections.SelectedItem is SectionObjectRec)
            {
                buttonDeleteSection.Enabled = true;
                buttonEditSection.Enabled = true;
                buttonChangeSection.Enabled = false;
            }
            else if (myListBoxSections.SelectedItem is TrackObjectRec)
            {
                buttonDeleteSection.Enabled = false;
                buttonEditSection.Enabled = true;
                buttonChangeSection.Enabled = true;
            }
        }

        private void buttonChangeSection_Click(object sender, EventArgs e)
        {
            TrackObjectRec track = myListBoxSections.SelectedItem as TrackObjectRec;
            if (track != null)
            {
                using (SectionChooseDialog dialog = new SectionChooseDialog(document.SectionList, track.Section))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        SectionObjectRec section = dialog.SelectedSection;
                        track.Section = section;
                        RebuildScrollingList();
                    }
                }
            }
        }

        private void buttonNew_Click(object sender, EventArgs e)
        {
            SectionObjectRec newSection = new SectionObjectRec(document);
            document.SectionList.Add(newSection);
            RebuildScrollingList();
            myListBoxSections.SelectItem(newSection, true/*clearOtherSelections*/);
            buttonEditSection_Click(null, null);
        }

        private void buttonDeleteSection_Click(object sender, EventArgs e)
        {
            SectionObjectRec selectedSection = myListBoxSections.SelectedItem as SectionObjectRec;
            if (selectedSection != null)
            {
                DialogResult result = MessageBox.Show("Delete selected section?", "Delete Section", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (result == DialogResult.Yes)
                {
                    if (registration.CloseAll(selectedSection))
                    {
                        document.RemoveSection(selectedSection);
                        RebuildScrollingList();
                    }
                }
            }
        }

        private void buttonEditSection_Click(object sender, EventArgs e)
        {
            {
                SectionObjectRec selectedSection = myListBoxSections.SelectedItem as SectionObjectRec;
                if (selectedSection != null)
                {
                    if (!registration.Activate(selectedSection))
                    {
                        new SectionWindow(registration, selectedSection, mainWindow).Show();
                    }
                    return;
                }
            }

            {
                TrackObjectRec selectedTrack = myListBoxSections.SelectedItem as TrackObjectRec;
                if (selectedTrack != null)
                {
                    if (selectedTrack.Section != null)
                    {
                        if (!registration.Activate(selectedTrack.Section))
                        {
                            new SectionWindow(registration, selectedTrack.Section, mainWindow).Show();
                        }
                    }
                    return;
                }
            }
        }

        private void myListBoxSections_DoubleClick2(object sender, MyListBox.DoubleClick2EventArgs e)
        {
            if (e.Item is SectionObjectRec)
            {
                buttonEditSection_Click(sender, e);
            }
            else if (e.Item is TrackObjectRec)
            {
                buttonChangeSection_Click(sender, e);
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
