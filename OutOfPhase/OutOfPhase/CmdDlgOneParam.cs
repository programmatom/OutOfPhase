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
    public partial class CmdDlgOneParam : Form
    {
        [Flags]
        public enum Options
        {
            None = 0,

            Wide = 1,
            Multiline = 2,
        }

        public CmdDlgOneParam(string prompt, string boxName, string initialValue, Options options)
        {
            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            labelPrompt.Text = prompt;
            groupBox1.Text = boxName;
            textBox.Text = initialValue;

            if ((options & Options.Wide) != 0)
            {
                textBox.Size = new Size(366, textBox.Size.Height);
            }
            if ((options & Options.Multiline) != 0)
            {
                textBox.ScrollBars = ScrollBars.Both;
                textBox.Multiline = true;
                textBox.AcceptsReturn = true;
                textBox.AcceptsTab = true;
                textBox.WordWrap = false;
                textBox.Size = new Size(textBox.Size.Width, 100);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Enter))
            {
                buttonOK.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        [Browsable(false)]
        public string Value
        {
            get
            {
                return textBox.Text;
            }
        }

        public static bool CommandDialogOneParam(
            string Prompt,
            string BoxName,
            ref double DataInOut)
        {
            using (CmdDlgOneParam dialog = new CmdDlgOneParam(
                Prompt,
                BoxName,
                DataInOut.ToString("G12"),
                Options.None))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    double v;
                    if (Double.TryParse(dialog.Value, out v))
                    {
                        DataInOut = v;
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool CommandDialogOneString(
            string Prompt,
            string BoxName,
            ref string DataInOut)
        {
            using (CmdDlgOneParam dialog = new CmdDlgOneParam(
                Prompt,
                BoxName,
                DataInOut,
                Options.Wide | Options.Multiline))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    DataInOut = dialog.Value;
                    return true;
                }
            }
            return false;
        }

        public static bool CommandDialogOneString2(
            string Prompt,
            string BoxName,
            ref string DataInOut)
        {
            using (CmdDlgOneParam dialog = new CmdDlgOneParam(
                Prompt,
                BoxName,
                DataInOut,
                Options.Wide))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    DataInOut = dialog.Value;
                    return true;
                }
            }
            return false;
        }
    }
}
