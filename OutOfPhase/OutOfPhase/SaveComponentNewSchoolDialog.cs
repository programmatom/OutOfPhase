/*
 *  Copyright © 1994-2002, 2015-2017 Thomas R. Lawrence
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
using System.IO;
using System.Windows.Forms;

namespace OutOfPhase
{
    public partial class SaveComponentNewSchoolDialog : Form
    {
        public SaveComponentNewSchoolDialog(BindingList<NewSchoolSourceRec> components)
        {
            InitializeComponent();

            myListBoxUnsavedComponents.SetUnderlying(components, delegate (object obj) { return Path.GetFileName(((NewSchoolSourceRec)obj).Path); });
            myListBoxUnsavedComponents.SelectAll();
        }


        private void buttonSave_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void buttonDiscard_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Ignore;
        }


        public List<NewSchoolSourceRec> GetListOfItemsToSave()
        {
            List<NewSchoolSourceRec> list = new List<NewSchoolSourceRec>();
            foreach (object o in myListBoxUnsavedComponents.SelectedItems)
            {
                list.Add((NewSchoolSourceRec)o);
            }
            return list;
        }
    }
}
