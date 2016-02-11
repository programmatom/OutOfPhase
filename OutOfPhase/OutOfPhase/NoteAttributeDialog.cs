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
    public partial class NoteAttributeDialog : Form
    {
        private readonly NoteNoteObjectRec note;

        public NoteAttributeDialog(NoteNoteObjectRec note)
        {
            this.note = note;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            foreach (string item in EnumUtility.GetDescriptions(NoteNoteObjectRec.ReleasePoint1OriginAllowedValues, NoteNoteObjectRec.ReleasePoint1Origin_EnumCategoryName))
            {
                comboBoxRelease1Origin.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(NoteNoteObjectRec.ReleasePoint2OriginAllowedValues, NoteNoteObjectRec.ReleasePoint2Origin_EnumCategoryName))
            {
                comboBoxRelease2Origin.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(NoteNoteObjectRec.ReleasePoint3OriginAllowedValues, NoteNoteObjectRec.ReleasePoint3Origin_EnumCategoryName))
            {
                comboBoxRelease3Origin.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(NoteNoteObjectRec.PitchDisplacementOriginAllowedValues, NoteNoteObjectRec.PitchDisplacementOrigin_EnumCategoryName))
            {
                comboBoxPitchDisplacementOrigin.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(NoteNoteObjectRec.PortamentoUnitsAllowedValues, NoteNoteObjectRec.PortamentoUnits_EnumCategoryName))
            {
                comboBoxPortamentoUnits.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(NoteNoteObjectRec.DurationAdjustModeAllowedValues, NoteNoteObjectRec.DurationAdjustMode_EnumCategoryName))
            {
                comboBoxDurationAdjustMode.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(NoteNoteObjectRec.DetuningModeAllowedValues, NoteNoteObjectRec.DetuningMode_EnumCategoryName))
            {
                comboBoxDetuningMode.Items.Add(item);
            }

            noteNoteObjectRecBindingSource.Add(this.note);
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren())
            {
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
