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
    public partial class SectionChooseDialog : Form
    {
        private readonly MyBindingList<object> displayList = new MyBindingList<object>();

        public SectionChooseDialog(MyBindingList<SectionObjectRec> sections, SectionObjectRec defaultSection)
        {
            InitializeComponent();
            this.Icon = OutOfPhase.Properties.Resources.Icon2;

            DpiChangeHelper.ScaleFont(this, Program.Config.AdditionalUIZoom);

            string nullSection = "(None)";

            displayList.Add(nullSection);
            foreach (SectionObjectRec section in sections)
            {
                displayList.Add(section);
            }

            myListBoxSections.SetUnderlying(displayList, GetDisplayText);
            myListBoxSections.SelectItem(defaultSection != null ? (object)defaultSection : (object)nullSection, true/*clearOtherSelections*/);
            myListBoxSections.DoubleClick2 += MyListBoxSections_DoubleClick;
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        private void MyListBoxSections_DoubleClick(object sender, MyListBox.DoubleClick2EventArgs e)
        {
            buttonOK.PerformClick();
        }

        private static string GetDisplayText(object obj)
        {
            if (obj is string)
            {
                return (string)obj;
            }
            else
            {
                return ((SectionObjectRec)obj).Name;
            }
        }

        public SectionObjectRec SelectedSection
        {
            get
            {
                object selection = myListBoxSections.SelectedItem;
                if ((selection != null) && (selection is SectionObjectRec))
                {
                    return (SectionObjectRec)selection;
                }
                return null;
            }
        }
    }
}
