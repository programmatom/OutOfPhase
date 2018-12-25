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
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class PresetDefinitionNewSchoolWindow : Form
    {
        private readonly NewSchoolDocument document;
        private readonly Registration registration;

        private readonly MyGridBinder<NewSchoolPresetDefinitionRec> presetsBinder;
        private readonly MyGridBinder<NewSchoolParticle> targetsBinder;

        public PresetDefinitionNewSchoolWindow(NewSchoolDocument document, Registration registration)
        {
            this.document = document;
            this.registration = registration;
            registration.Register(document.PresetDefinitions, this);

            InitializeComponent();

            this.myGridPresets.DefineColumns(new int[] { 100 });
            this.myGridPresets.SelectionChanged += MyGridPresets_SelectionChanged;
            this.myGridPresets.CreateRow = (g, i) => NewPreset(i);
            this.presetsBinder = new MyGridBinder<NewSchoolPresetDefinitionRec>(
                this.myGridPresets,
                document.PresetDefinitions,
                new MyGridBinder<NewSchoolPresetDefinitionRec>.CellBinding[]
                {
                    new MyGridBinder<NewSchoolPresetDefinitionRec>.CellBinding(
                        NewSchoolPresetDefinitionRec.PresetName_PropertyName,
                        i => new MyGrid.TextEditCell(),
                        g => g.PresetName ,
                        (g, v) => g.PresetName = (string)v),
                });

            this.myGridTargets.DefineColumns(new int[] { 100, 100 });
            this.targetsBinder = new MyGridBinder<NewSchoolParticle>(
                this.myGridTargets,
                null/*no underlying yet*/,
                new MyGridBinder<NewSchoolParticle>.CellBinding[]
                {
                    new MyGridBinder<NewSchoolParticle>.CellBinding(
                        NewSchoolParticle.Moniker_PropertyName,
                        i => new MyGrid.TextEditCell(),
                        s => s.Moniker.ToString(),
                        (s, v) => s.Moniker = Moniker.Parse((string)v)),
                    new MyGridBinder<NewSchoolParticle>.CellBinding(
                        NewSchoolParticle.Sequence_PropertyName,
                        i => new MyGrid.TextEditCell(),
                        s => s.Sequence,
                        (s, v) => s.Sequence = (string)v),
                });
        }

        public static bool TryActivate(NewSchoolDocument document, Registration registration)
        {
            return registration.Activate(document.PresetDefinitions);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            myGridPresets.EndEdit(true/*commit*/);
            myGridTargets.EndEdit(true/*commit*/);
            presetsBinder.Dispose();
            targetsBinder.Dispose();
            registration.Unregister(document.PresetDefinitions, this);
            base.OnFormClosed(e);
        }

        private void MyGridPresets_SelectionChanged(object arg1, EventArgs arg2)
        {
            if (myGridPresets.HasSelection)
            {
                myGridTargets.CreateRow = (g, i) => NewTarget(i);
                targetsBinder.SetDataSource(document.PresetDefinitions[myGridPresets.SelectedRow].Targets);
            }
            else
            {
                myGridTargets.CreateRow = null;
                targetsBinder.SetDataSource(null);
            }
        }

        private void NewPreset(int i)
        {
            document.PresetDefinitions.Insert(i, new NewSchoolPresetDefinitionRec(String.Empty, document));
            myGridPresets.SetSelection(i, 0);
            myGridPresets.BeginEdit(false/*selectAll*/);
        }

        private void buttonNewPreset_Click(object sender, EventArgs e)
        {
            NewPreset(document.PresetDefinitions.Count);
        }

        private void buttonDeletePreset_Click(object sender, EventArgs e)
        {
            if (myGridPresets.HasSelection)
            {
                document.PresetDefinitions.RemoveAt(myGridPresets.SelectedRow);
            }
        }

        private void NewTarget(int i)
        {
            if (targetsBinder.List != null)
            {
                document.PresetDefinitions[myGridPresets.SelectedRow].Targets.Insert(
                    i,
                    new NewSchoolParticle(
                        document,
                        NewSchoolPresetDefinitionRec.Target_PropertyName,
                        Moniker.Empty,
                        String.Empty));
                myGridTargets.SetSelection(i, 0);
                myGridTargets.BeginEdit(false/*selectAll*/);
            }
        }

        private void buttonNewTarget_Click(object sender, EventArgs e)
        {
            if (targetsBinder.List != null)
            {
                NewTarget(document.PresetDefinitions[myGridPresets.SelectedRow].Targets.Count);
            }
        }

        private void buttonDeleteTarget_Click(object sender, EventArgs e)
        {
            if (myGridTargets.HasSelection)
            {
                document.PresetDefinitions[myGridPresets.SelectedRow].Targets.RemoveAt(myGridTargets.SelectedRow);
            }
        }
    }
}
