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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class GroupDefinitionsNewSchoolWindow : Form
    {
        private readonly NewSchoolDocument document;
        private readonly Registration registration;

        private readonly MyGridBinder<NewSchoolGroupDefinition> groupDefinitionBinder;
        private readonly MyGridBinder<NewSchoolExpandedSourceRec> expandedSourceBinder;
        private readonly Dictionary<Moniker, bool> isMemberOfCurrent = new Dictionary<Moniker, bool>();

        private const string Member_PropertyName = "Member";

        public GroupDefinitionsNewSchoolWindow(NewSchoolDocument document, Registration registration)
        {
            this.document = document;
            this.registration = registration;
            registration.Register(document.GroupDefinitions, this);

            InitializeComponent();

            this.myGridGroups.DefineColumns(new int[] { 100 });
            this.myGridGroups.SelectionChanged += MyGridGroups_SelectionChanged;
            this.groupDefinitionBinder = new MyGridBinder<NewSchoolGroupDefinition>(
                this.myGridGroups,
                document.GroupDefinitions,
                new MyGridBinder<NewSchoolGroupDefinition>.CellBinding[]
                {
                    new MyGridBinder<NewSchoolGroupDefinition>.CellBinding(
                        NewSchoolGroupDefinition.GroupDefinition_PropertyName,
                        i => new MyGrid.TextEditCell(),
                        g => g.GroupName,
                        (g, v) => g.GroupName = (string)v),
                });

            this.myGridTracks.DefineColumns(new int[] { 100, 100 });
            this.expandedSourceBinder = new MyGridBinder<NewSchoolExpandedSourceRec>(
                this.myGridTracks,
                document.ExpandedSources,
                new MyGridBinder<NewSchoolExpandedSourceRec>.CellBinding[]
                {
                    new MyGridBinder<NewSchoolExpandedSourceRec>.CellBinding(
                        Member_PropertyName,
                        i => new MyGrid.BoolToggleCell("member"),
                        s => IsGroupMemberOfCurrent(s),
                        (s, v) => SetGroupMemberOfCurrent(s, (bool?)v)),
                    new MyGridBinder<NewSchoolExpandedSourceRec>.CellBinding(
                        null,
                        i => new MyGrid.LabelCell(),
                        s => s.DisplayMoniker,
                        (s, v) => throw new InvalidOperationException()),
                });

            Rebind();
        }

        public static bool TryActivate(NewSchoolDocument document, Registration registration)
        {
            return registration.Activate(document.GroupDefinitions);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            myGridGroups.EndEdit(true/*commit*/);
            expandedSourceBinder.Dispose();
            groupDefinitionBinder.Dispose();
            registration.Unregister(document.GroupDefinitions, this);
            base.OnFormClosed(e);
        }

        private void MyGridGroups_SelectionChanged(object sender, EventArgs e)
        {
            isMemberOfCurrent.Clear();
            Rebind();
        }

        private void Rebind()
        {
            if (myGridGroups.HasSelection)
            {
                expandedSourceBinder.SetDataSource(document.ExpandedSources);

                Dictionary<Moniker, int> indices = new Dictionary<Moniker, int>();
                for (int i = 0; i < document.ExpandedSources.Count; i++)
                {
                    indices.Add(document.ExpandedSources[i].Moniker, i);
                }

                NewSchoolGroupDefinition group = document.GroupDefinitions[myGridGroups.SelectedRow];
                for (int i = 0; i < group.MemberMonikers.Count; i++)
                {
                    Moniker groupMemberMoniker = group.MemberMonikers[i];
                    isMemberOfCurrent[groupMemberMoniker] = true;
                }

                for (int i = 0; i < document.ExpandedSources.Count; i++)
                {
                    expandedSourceBinder.ForcePropertyChanged(i, Member_PropertyName);
                }
            }
            else
            {
                expandedSourceBinder.SetDataSource(null);
            }
        }

        private bool IsGroupMemberOfCurrent(NewSchoolExpandedSourceRec source)
        {
            bool value;
            return isMemberOfCurrent.TryGetValue(source.Moniker, out value) && value;
        }

        private void SetGroupMemberOfCurrent(NewSchoolExpandedSourceRec source, bool? value)
        {
            if (myGridGroups.HasSelection && value.HasValue)
            {
                NewSchoolGroupDefinition currentGroup = document.GroupDefinitions[myGridGroups.SelectedRow];
                isMemberOfCurrent[source.Moniker] = value.Value;
                if (value.Value)
                {
                    currentGroup.MemberMonikers.Add(source.Moniker);
                }
                else
                {
                    currentGroup.MemberMonikers.Remove(source.Moniker);
                }
                expandedSourceBinder.ForcePropertyChanged(document.ExpandedSources.IndexOf(source), Member_PropertyName);

                Debug.Assert(currentGroup.MemberMonikers.Count == isMemberOfCurrent.Count(kvp => kvp.Value));
            }
        }

        private void buttonNewGroup_Click(object sender, EventArgs e)
        {
            document.GroupDefinitions.Add(new NewSchoolGroupDefinition(String.Empty, document));
            myGridGroups.Focus();
            myGridGroups.SetSelection(document.GroupDefinitions.Count - 1, 0);
            myGridGroups.BeginEdit(document.GroupDefinitions.Count - 1, 0, false/*selectAll*/);
        }

        private void buttonDeleteGroup_Click(object sender, EventArgs e)
        {
            if (myGridGroups.HasSelection)
            {
                document.GroupDefinitions.RemoveAt(myGridGroups.SelectedRow);
            }
        }
    }
}
