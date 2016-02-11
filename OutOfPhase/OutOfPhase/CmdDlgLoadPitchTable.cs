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
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class CmdDlgLoadPitchTable : Form
    {
        public CmdDlgLoadPitchTable(
            string initialStringArgument1,
            double initialArgument1)
        {
            int tonicOffset = (int)Math.Abs(initialArgument1) % 12;
            bool relativeToCurrent = initialArgument1 < 0;

            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            textEditControl1.TextService = Program.Config.EnableDirectWrite ? TextEditor.TextService.DirectWrite : TextEditor.TextService.Uniscribe;
            textEditControl1.Text =
                "If unchecked, tonic in new table will" + Environment.NewLine +
                "have same pitch as previous entry. If" + Environment.NewLine +
                "checked, tonic will reset to concert" + Environment.NewLine +
                "pitch equal-temperament reference.";

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            bool builtIn;
            string tableName = initialStringArgument1;
            if (!(initialStringArgument1.StartsWith("!") || initialStringArgument1.StartsWith("+!")))
            {
                // built-in
                builtIn = true;
            }
            else
            {
                // user-provided
                builtIn = false;
                if (tableName.StartsWith("+"))
                {
                    checkBoxRelativeCents.Checked = true;
                    tableName = tableName.Substring(1);
                }
                Debug.Assert(tableName.StartsWith("!"));
                tableName = tableName.Substring(1);
            }

            bool found = false;
            comboBoxPitchTable.DisplayMember = "Key";
            comboBoxPitchTable.ValueMember = "Value";
            comboBoxPitchTable.Items.Add(new KeyValuePair<string, int>("User Function", -1));
            for (int i = 0; i < Synthesizer.PitchTables.Length; i++)
            {
                Synthesizer.PitchEntry table = Synthesizer.PitchTables[i];
                comboBoxPitchTable.Items.Add(new KeyValuePair<string, int>(table.Name, i));
                if (builtIn && String.Equals(tableName, table.Name))
                {
                    comboBoxPitchTable.SelectedIndex = i + 1;
                    found = true;
                }
            }
            if (!found)
            {
                // unknown built-in specification is converted to user function
                comboBoxPitchTable.SelectedIndex = 0;
                textBoxCustomTableFunctionName.Text = tableName;
            }

            checkBoxReset.Checked = !relativeToCurrent;

            UpdateUserFunctionEnables();

            comboBoxTonic.DisplayMember = "Key";
            comboBoxTonic.ValueMember = "Value";
            for (int i = 0; i < Tonics.Length; i++)
            {
                KeyValuePair<string, int> tonic = Tonics[i];
                comboBoxTonic.Items.Add(tonic);
                if (tonicOffset % 12 == tonic.Value % 12)
                {
                    comboBoxTonic.SelectedIndex = i;
                }
            }

            comboBoxPitchTable.SelectedIndexChanged += ComboBoxPitchTable_SelectedIndexChanged;
        }

        private static readonly KeyValuePair<string, int>[] Tonics = new KeyValuePair<string, int>[]
        {
            new KeyValuePair<string, int>("C", 12),
            new KeyValuePair<string, int>("C-sharp/D-flat", 1),
            new KeyValuePair<string, int>("D", 2),
            new KeyValuePair<string, int>("D-sharp/E-flat", 3),
            new KeyValuePair<string, int>("E", 4),
            new KeyValuePair<string, int>("F", 5),
            new KeyValuePair<string, int>("F-sharp/G-flat", 6),
            new KeyValuePair<string, int>("G", 7),
            new KeyValuePair<string, int>("G-sharp/A-flat", 8),
            new KeyValuePair<string, int>("A", 9),
            new KeyValuePair<string, int>("A-sharp/B-flat", 10),
            new KeyValuePair<string, int>("B", 11),
        };

        private void UpdateUserFunctionEnables()
        {
            bool enabled = comboBoxPitchTable.SelectedIndex == 0;
            textBoxCustomTableFunctionName.Enabled = enabled;
            checkBoxRelativeCents.Enabled = enabled;
        }

        private void ComboBoxPitchTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUserFunctionEnables();
        }


        //

        public string StringArgument1
        {
            get
            {
                if (comboBoxPitchTable.SelectedIndex != 0)
                {
                    return Synthesizer.PitchTables[comboBoxPitchTable.SelectedIndex - 1].Name;
                }
                else
                {
                    string function = "!" + textBoxCustomTableFunctionName.Text;
                    if (checkBoxRelativeCents.Checked)
                    {
                        function = "+" + function;
                    }
                    return function;
                }
            }
        }

        public double Argument1
        {
            get
            {
                return Tonics[comboBoxTonic.SelectedIndex].Value * (checkBoxReset.Checked ? 1 : -1);
            }
        }

        public static bool CommandDialogOneStringOneParam(
            ref string FirstDataInOut,
            ref double SecondDataInOut)
        {
            using (CmdDlgLoadPitchTable dialog = new CmdDlgLoadPitchTable(
                FirstDataInOut,
                SecondDataInOut))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    FirstDataInOut = dialog.StringArgument1;
                    SecondDataInOut = dialog.Argument1;
                    return true;
                }
            }
            return false;
        }
    }
}
