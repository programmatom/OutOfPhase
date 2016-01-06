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
    public partial class CmdDlgOneBinaryChoice : Form
    {
        public CmdDlgOneBinaryChoice(string prompt, string trueButtonName, string falseButtonName, bool initialValue)
        {
            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            labelPrompt.Text = prompt;
            radioButtonTrue.Text = trueButtonName;
            radioButtonFalse.Text = falseButtonName;
            if (initialValue)
            {
                radioButtonTrue.Checked = true;
            }
            else
            {
                radioButtonFalse.Checked = true;
            }
        }

        [Browsable(false)]
        public bool Value
        {
            get
            {
                return radioButtonTrue.Checked;
            }
        }

        public static bool CommandDialogOneBinaryChoice(
            string Prompt,
            string TrueButtonName,
            string FalseButtonName,
            ref bool FlagInOut)
        {
            using (CmdDlgOneBinaryChoice dialog = new CmdDlgOneBinaryChoice(
                Prompt,
                TrueButtonName,
                FalseButtonName,
                FlagInOut))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    FlagInOut = dialog.Value;
                    return true;
                }
            }
            return false;
        }
    }
}
