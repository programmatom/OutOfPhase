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
    public partial class ImportRawSampleDialog : Form
    {
        private Import.ImportRawSettings settings;
        private string path;

        public ImportRawSampleDialog(Import.ImportRawSettings settings, string path)
        {
            this.settings = settings;
            this.path = path;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            foreach (string item in EnumUtility.GetDescriptions(Import.ImportRawSettings.NumBitsAllowedValues))
            {
                comboBoxBits.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(Import.ImportRawSettings.NumChannelsAllowedValues))
            {
                comboBoxChannels.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(Import.ImportRawSettings.SignModeAllowedValues))
            {
                comboBoxSignMode.Items.Add(item);
            }
            foreach (string item in EnumUtility.GetDescriptions(Import.ImportRawSettings.EndiannessAllowedValues))
            {
                comboBoxEndianness.Items.Add(item);
            }

            importRawSettingsBindingSource.Add(settings);

            settings.PropertyChanged += new PropertyChangedEventHandler(settings_PropertyChanged);
            settings_PropertyChanged(this, null); // initial load

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        private void settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            const int PreviewMaxFrames = 65536;

            SampleObjectRec ReturnedSampleObject;
            Import.ImportRawSample(
                new Document(), // dummy document for preview
                path,
                PreviewMaxFrames,
                settings,
                out ReturnedSampleObject);

            sampleView.SampleObject = ReturnedSampleObject;
        }
    }
}
