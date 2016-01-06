/*
 *  Copyright � 1994-2002, 2015-2016 Thomas R. Lawrence
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
    public partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            richTextBox1.Rtf = OutOfPhase._Resources.AboutBoxContent.License;
            pictureBox1.Image = OutOfPhase._Resources.AboutBoxContent.Icon2.ToBitmap();
            pictureBox2.Image = OutOfPhase._Resources.AboutBoxContent.Icon1.ToBitmap();

            richTextBox1.LinkClicked += new LinkClickedEventHandler(richTextBox1_LinkClicked);
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start("explorer.exe", e.LinkText);
        }
    }
}
