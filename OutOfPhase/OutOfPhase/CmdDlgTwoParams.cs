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
    public partial class CmdDlgTwoParams : Form
    {
        [Flags]
        public enum Options
        {
            None = 0,

            Wide = 1,
            Multiline = 2,
        }

        public CmdDlgTwoParams(
            string prompt,
            string box1Name,
            string initialValue1,
            Options options1,
            string box2Name,
            string initialValue2,
            Options options2)
        {
            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            labelPrompt.Text = prompt;
            groupBox1.Text = box1Name;
            textBox1.Text = initialValue1;
            groupBox2.Text = box2Name;
            textBox2.Text = initialValue2;

            if ((options1 & Options.Wide) != 0)
            {
                textBox1.Size = new Size(366, textBox1.Size.Height);
            }
            if ((options1 & Options.Multiline) != 0)
            {
                textBox1.ScrollBars = ScrollBars.Both;
                textBox1.Multiline = true;
                textBox1.AcceptsReturn = true;
                textBox1.AcceptsTab = true;
                textBox1.WordWrap = false;
                textBox1.Size = new Size(textBox1.Size.Width, 100);
            }

            if ((options2 & Options.Wide) != 0)
            {
                textBox2.Size = new Size(366, textBox2.Size.Height);
            }
            if ((options2 & Options.Multiline) != 0)
            {
                textBox2.ScrollBars = ScrollBars.Both;
                textBox2.Multiline = true;
                textBox2.AcceptsReturn = true;
                textBox2.AcceptsTab = true;
                textBox2.WordWrap = false;
                textBox2.Size = new Size(textBox2.Size.Width, 100);
            }
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        [Browsable(false)]
        public string Value1
        {
            get
            {
                return textBox1.Text;
            }
        }

        [Browsable(false)]
        public string Value2
        {
            get
            {
                return textBox2.Text;
            }
        }

        public static bool CommandDialogTwoParams(
            string Prompt,
            string FirstBoxName,
            ref double FirstDataInOut,
            string SecondBoxName,
            ref double SecondDataInOut)
        {
            using (CmdDlgTwoParams dialog = new CmdDlgTwoParams(
                Prompt,
                FirstBoxName,
                FirstDataInOut.ToString("G12"),
                Options.None,
                SecondBoxName,
                SecondDataInOut.ToString("G12"),
                Options.None))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    double v;
                    if (Double.TryParse(dialog.Value1, out v))
                    {
                        FirstDataInOut = v;
                    }
                    if (Double.TryParse(dialog.Value2, out v))
                    {
                        SecondDataInOut = v;
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool CommandDialogTwoStrings(
            string Prompt,
            string FirstBoxName,
            ref string FirstDataInOut,
            string SecondBoxName,
            ref string SecondDataInOut)
        {
            using (CmdDlgTwoParams dialog = new CmdDlgTwoParams(
                Prompt,
                FirstBoxName,
                FirstDataInOut,
                Options.Wide,
                SecondBoxName,
                SecondDataInOut,
                Options.Wide))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    FirstDataInOut = dialog.Value1;
                    SecondDataInOut = dialog.Value2;
                    return true;
                }
            }
            return false;
        }

        public static bool CommandDialogOneStringOneParam(
            string Prompt,
            string FirstBoxName,
            ref string FirstDataInOut,
            string SecondBoxName,
            ref double SecondDataInOut)
        {
            using (CmdDlgTwoParams dialog = new CmdDlgTwoParams(
                Prompt,
                FirstBoxName,
                FirstDataInOut,
                Options.Wide,
                SecondBoxName,
                SecondDataInOut.ToString("G12"),
                Options.None))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    FirstDataInOut = dialog.Value1;
                    double v;
                    if (Double.TryParse(dialog.Value2, out v))
                    {
                        SecondDataInOut = v;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
