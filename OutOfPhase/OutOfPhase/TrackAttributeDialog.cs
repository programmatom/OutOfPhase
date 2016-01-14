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
    public partial class TrackAttributeDialog : Form
    {
        private readonly TrackObjectRec track;

        public TrackAttributeDialog(TrackObjectRec track)
        {
            this.track = track;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            foreach (string item in EnumUtility.GetDescriptions(TrackObjectRec.DefaultReleasePoint1ModeFlagAllowedValues, TrackObjectRec.DefaultReleasePoint1ModeFlag_EnumCategoryName))
            {
                comboBoxDefaultReleasePoint1Flags.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(TrackObjectRec.DefaultReleasePoint2ModeFlagAllowedValues, TrackObjectRec.DefaultReleasePoint2ModeFlag_EnumCategoryName))
            {
                comboBoxDefaultReleasePoint2Flags.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(TrackObjectRec.DefaultPitchDisplacementStartPointModeFlagAllowedValues, TrackObjectRec.DefaultPitchDisplacementStartPointModeFlag_EnumCategoryName))
            {
                comboBoxDefaultPitchDisplacementStartFlags.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(TrackObjectRec.DefaultDurationModeFlagAllowedValues, TrackObjectRec.DefaultDurationModeFlag_EnumCategoryName))
            {
                comboBoxDefaultDurationAdjustModeFlags.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(TrackObjectRec.DefaultDetuneModeFlagAllowedValues, TrackObjectRec.DefaultDetuneModeFlag_EnumCategoryName))
            {
                comboBoxDefaultDetuningModeFlags.Items.Add(item);
            }

            trackObjectRecBindingSource.Add(track);
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
