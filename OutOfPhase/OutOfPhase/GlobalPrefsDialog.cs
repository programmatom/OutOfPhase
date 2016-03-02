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
        private readonly GlobalPrefs primaryPrefs;
        private readonly GlobalPrefs localPrefs;

        private static readonly KeyValuePair<string, int>[] Priorities = new KeyValuePair<string, int>[]
        {
            new KeyValuePair<string, int>("Default", 0),
            new KeyValuePair<string, int>("Normal", 1),
            new KeyValuePair<string, int>("Above Normal", 2),
            new KeyValuePair<string, int>("Highest", 3),
        };

        private static readonly float[] ZoomLevels = new float[]
        {
            .5f,
            .625f,
            .75f,
            .875f,
            1f,
            1.125f,
            1.25f,
            1.375f,
            1.5f,
            1.75f,
            2f,
            2.5f,
            3f,
        };

        public GlobalPrefsDialog(GlobalPrefs primaryPrefs)
        {
            this.primaryPrefs = primaryPrefs;
            localPrefs = new GlobalPrefs();
            primaryPrefs.CopyTo(localPrefs);

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            devices = OutputDeviceEnumerator.EnumerateAudioOutputDeviceIdentifiers();
            for (int i = 0; i < devices.Length; i++)
            {
                comboBoxOutputDevice.Items.Add(devices[i].Value);
                if (String.Equals(devices[i].Key, localPrefs.OutputDevice))
                {
                    comboBoxOutputDevice.SelectedIndex = i;
                }
            }

            for (int i = 0; i < Priorities.Length; i++)
            {
                comboBoxPriority.Items.Add(Priorities[i].Key);
                if (Priorities[i].Value == localPrefs.PriorityMode)
                {
                    comboBoxPriority.SelectedIndex = i;
                }
            }

            concurrency = localPrefs.Concurrency;
            UpdateConcurrencyEnables();

            bool zoomSet = false;
            for (int i = 0; i < ZoomLevels.Length; i++)
            {
                comboBoxZoom.Items.Add(ZoomLevels[i]);
                if (!zoomSet && (primaryPrefs.AdditionalUIZoom <= ZoomLevels[i]))
                {
                    zoomSet = true;
                    comboBoxZoom.SelectedIndex = i;
                }
            }
            if (!zoomSet)
            {
                comboBoxZoom.SelectedIndex = ZoomLevels.Length - 1;
            }

            globalPrefsBindingSource.Add(localPrefs);
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
            // stage non-databound settings
            localPrefs.Concurrency = concurrency;
            localPrefs.OutputDevice = devices[comboBoxOutputDevice.SelectedIndex].Key;
            localPrefs.OutputDeviceName = devices[comboBoxOutputDevice.SelectedIndex].Value;
            localPrefs.PriorityMode = comboBoxPriority.SelectedIndex;
            localPrefs.AdditionalUIZoom = ZoomLevels[comboBoxZoom.SelectedIndex];

            localPrefs.CopyTo(primaryPrefs);
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
