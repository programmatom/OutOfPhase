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
using System.ComponentModel;
using System.Windows.Forms;

namespace OutOfPhase
{
    public interface IMainWindowServices : IMenuStripManagerHandler
    {
        string DisplayName { get; }

        Document Document { get; }
        string SavePath { get; }

        Form CreateAndShowEditor(object dataObject);

        void DeleteObject(object o, IBindingList list);

        bool MakeUpToDate();
        bool MakeUpToDateFunctions();
        void DefaultBuildFailedCallback(object sender, BuildErrorInfo errorInfo);

        InteractionWindow GetInteractionWindow();
        bool PromptResumableError(string message);

        void AddMiscForm(Form form);
        void RemoveMiscForm(Form form);

        IPlayPrefsProvider GetPlayPrefsProvider();

        // From System.Windows.Forms.Control
        bool InvokeRequired { get; }
        object Invoke(Delegate method);
        object Invoke(Delegate method, params object[] args);
    }
}
