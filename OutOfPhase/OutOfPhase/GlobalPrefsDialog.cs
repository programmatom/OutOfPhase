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
    public partial class GlobalPrefsDialog : Form
    {
        private readonly KeyValuePair<string, string>[] devices;
        private int concurrency;

        public GlobalPrefsDialog()
        {
            InitializeComponent();

            devices = OutputDeviceEnumerator.EnumerateAudioOutputDeviceIdentifiers();
            for (int i = 0; i < devices.Length; i++)
            {
                comboBoxOutputDevice.Items.Add(devices[i].Value);
                if (String.Equals(devices[i].Key, Program.Config.OutputDevice))
                {
                    comboBoxOutputDevice.SelectedIndex = i;
                }
            }

            textBoxTabSize.Text = Program.Config.TabSize.ToString();
            checkBoxAutoIndent.Checked = Program.Config.AutoIndent;
            checkBoxAutosaveEnabled.Checked = Program.Config.AutosaveEnabled;
            textBoxAutosaveInterval.Text = Program.Config.AutosaveIntervalSeconds.ToString();

            concurrency = Program.Config.Concurrency;
            UpdateConcurrencyEnables();
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            try
            {
                Program.Config.TabSize = Math.Min(Math.Max(Int32.Parse(textBoxTabSize.Text), Constants.MINTABCOUNT), Constants.MAXTABCOUNT);
            }
            catch (Exception)
            {
            }
            Program.Config.AutoIndent = checkBoxAutoIndent.Checked;

            Program.Config.AutosaveEnabled = checkBoxAutosaveEnabled.Checked;

            try
            {
                Program.Config.AutosaveIntervalSeconds = Math.Min(Math.Max(Int32.Parse(textBoxAutosaveInterval.Text), Constants.MINAUTOSAVEINTERVAL), Constants.MAXAUTOSAVEINTERVAL);
            }
            catch (Exception)
            {
            }

            Program.Config.Concurrency = concurrency;

            Program.Config.OutputDevice = devices[comboBoxOutputDevice.SelectedIndex].Key;
            Program.Config.OutputDeviceName = devices[comboBoxOutputDevice.SelectedIndex].Value;

            Program.SaveSettings();

            Close();
        }

        private void UpdateConcurrencyEnables()
        {
            disable_comboBoxConcurrency_SelectedIndexChanged = true;
            if (concurrency == 0)
            {
                comboBoxConcurrency.SelectedIndex = 0;
                textBoxConcurrency.Enabled = false;
                textBoxConcurrency.Text = String.Empty;
            }
            else if (concurrency == 1)
            {
                comboBoxConcurrency.SelectedIndex = 1;
                textBoxConcurrency.Enabled = false;
                textBoxConcurrency.Text = String.Empty;
            }
            else if (concurrency > 1)
            {
                comboBoxConcurrency.SelectedIndex = 2;
                textBoxConcurrency.Enabled = true;
                textBoxConcurrency.Text = concurrency.ToString();
            }
            else
            {
                comboBoxConcurrency.SelectedIndex = 3;
                textBoxConcurrency.Enabled = true;
                textBoxConcurrency.Text = (-concurrency).ToString();
            }
            disable_comboBoxConcurrency_SelectedIndexChanged = false;
        }

        private bool disable_comboBoxConcurrency_SelectedIndexChanged;
        private void comboBoxConcurrency_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!disable_comboBoxConcurrency_SelectedIndexChanged)
            {
                switch (comboBoxConcurrency.SelectedIndex)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 0:
                        concurrency = 0;
                        break;
                    case 1:
                        concurrency = 1;
                        break;
                    case 2:
                        concurrency = Environment.ProcessorCount;
                        break;
                    case 3:
                        concurrency = -1;
                        break;
                }
                UpdateConcurrencyEnables();
            }
        }

        private void textBoxConcurrency_TextChanged(object sender, EventArgs e)
        {
            switch (comboBoxConcurrency.SelectedIndex)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                case 1:
                    // TODO: figure out why
                    //Debug.Assert(false);
                    //throw new InvalidOperationException();
                    break;
                case 2:
                    {
                        int c;
                        Int32.TryParse(textBoxConcurrency.Text, out c);
                        concurrency = Math.Max(1, c);
                    }
                    break;
                case 3:
                    {
                        int c;
                        Int32.TryParse(textBoxConcurrency.Text, out c);
                        concurrency = Math.Min(-1, -c);
                    }
                    break;
            }
            UpdateConcurrencyEnables();
        }
    }
}
