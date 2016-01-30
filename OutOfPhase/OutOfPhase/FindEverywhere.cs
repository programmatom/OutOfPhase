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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using TextEditor;

namespace OutOfPhase
{
    public class SearchableAttribute : Attribute
    {
        private readonly bool searchable = true;

        public SearchableAttribute()
        {
        }

        public SearchableAttribute(bool searchable)
        {
            this.searchable = searchable;
        }

        public bool Searchable { get { return searchable; } }
    }

    public class FindInFilesApplication : IFindInFilesApplication
    {
        private readonly MainWindow mainWindow;

        public FindInFilesApplication(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public Icon ApplicationIcon { get { return OutOfPhase.Properties.Resources.Icon2; } }
        public string ApplicationName { get { return Application.ProductName; } }

        public SearchCombos Config_SearchExtensions { get { return new SearchCombos(new string[0], String.Empty); } set { } }
        public SearchCombos Config_SearchPaths { get { return new SearchCombos(new string[0], String.Empty); } set { } }

        public IFindInFilesNode GetNodeForPath(string path)
        {
            return new FindInFilesNode(mainWindow.Document, null);
        }

        public IFindInFilesWindow Open(IFindInFilesItem item)
        {
            Form window;
            if (FindNearestViableActivator(item, out window) == null)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            // This is a bit of a hack for HighlightLine() below. Most editors set the IP to the beginning of the
            // main body edit field in the OnShow() handler. This is going to happen during event processing of
            // MessageBox.Show() below, which blows away the result of HighlightLine(). Unless we do this to get
            // that event dealt with first.
            Application.DoEvents();
            return new FindInFilesWindow(window, this);
        }

        public IFindInFilesItemActivator FindNearestViableActivator(IFindInFilesItem item, out Form window)
        {
            window = null;
            IFindInFilesItemActivator activator = (IFindInFilesItemActivator)item;
            while (activator != null)
            {
                try
                {
                    window = mainWindow.CreateAndShowEditor(activator.DataObject);
                    break;
                }
                catch (ArgumentException)
                {
                    activator = activator.Parent;
                }
            }
            return activator;
        }
    }

    public class FindInFilesWindow : IFindInFilesWindow
    {
        private readonly Form window;
        private readonly FindInFilesApplication application;

        public FindInFilesWindow(Form window, FindInFilesApplication application)
        {
            this.window = window;
            this.application = application;
        }

        public bool IsDisposed { get { return window.IsDisposed; } }

        public void Activate()
        {
            window.Activate();
        }

        public void SetSelection(IFindInFilesItem item, int startLine, int startChar, int endLine, int endCharPlusOne)
        {
            IFindInFilesItemActivator activator = (IFindInFilesItemActivator)item;
            if (activator.DataObject is NoteObjectRec)
            {
                // special case for track elements
                Form window;
                if (application.FindNearestViableActivator(item, out window) == null)
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }
                TrackWindow trackWindow = (TrackWindow)window;
                trackWindow.Edit.View.TrackViewTrySingleNoteOrCommandSelection((NoteObjectRec)activator.DataObject);
            }
            else
            {
                Control edit = FindControl(window.Controls, activator.Property);
                if (edit != null)
                {
                    if (edit is TextEditControl)
                    {
                        ((TextEditControl)edit).SetSelection(startLine, startChar, endLine, endCharPlusOne);
                    }
                    else if (edit is TextBox)
                    {
                        TextBox textBox = (TextBox)edit;
                        int start = textBox.GetFirstCharIndexFromLine(startLine) + startChar;
                        int end = textBox.GetFirstCharIndexFromLine(endLine) + endCharPlusOne;
                        textBox.SelectionStart = start;
                        textBox.SelectionLength = end - start;
                    }
                    edit.Focus();
                }
            }
        }

        private Control FindControl(Control.ControlCollection controls, string property)
        {
            foreach (Control control in controls)
            {
                Control child = FindControl(control.Controls, property);
                if (child != null)
                {
                    return child;
                }
                if (control.DataBindings != null)
                {
                    for (int i = 0; i < control.DataBindings.Count; i++)
                    {
                        Binding binding = control.DataBindings[i];
                        if (String.Equals(property, binding.BindingMemberInfo.BindingField))
                        {
                            return control;
                        }
                    }
                }
            }
            return null;
        }
    }

    public interface IFindInFilesItemActivator
    {
        object DataObject { get; }
        string Property { get; }
        FindInFilesNode Parent { get; }
    }

    public class FindInFilesNode : IFindInFilesNode, IFindInFilesItemActivator
    {
        private readonly object dataObject;
        private readonly FindInFilesNode parent;

        public FindInFilesNode(object dataObject, FindInFilesNode parent)
        {
            this.dataObject = dataObject;
            this.parent = parent;
        }

        public object DataObject { get { return dataObject; } }
        public string Property { get { return String.Empty; } }
        public FindInFilesNode Parent { get { return parent; } }

        public IFindInFilesNode[] GetDirectories()
        {
            List<IFindInFilesNode> items = new List<IFindInFilesNode>();
            if (dataObject is IList)
            {
                foreach (object subObject in (IList)dataObject)
                {
                    items.Add(new FindInFilesNode(subObject, this));
                }
            }
            foreach (PropertyInfo pi in dataObject.GetType().GetProperties())
            {
                SearchableAttribute searchableAttribute = (SearchableAttribute)pi.GetCustomAttribute(typeof(SearchableAttribute));
                if ((searchableAttribute != null) && searchableAttribute.Searchable)
                {
                    if (typeof(IList).IsAssignableFrom(pi.PropertyType))
                    {
                        foreach (object subObject in (IList)pi.GetValue(dataObject))
                        {
                            items.Add(new FindInFilesNode(subObject, this));
                        }
                    }
                }
            };
            return items.ToArray();
        }

        public IFindInFilesItem[] GetFiles()
        {
            List<IFindInFilesItem> items = new List<IFindInFilesItem>();
            foreach (PropertyInfo pi in dataObject.GetType().GetProperties())
            {
                SearchableAttribute searchableAttribute = (SearchableAttribute)pi.GetCustomAttribute(typeof(SearchableAttribute));
                if ((searchableAttribute != null) && searchableAttribute.Searchable)
                {
                    if (!typeof(IList).IsAssignableFrom(pi.PropertyType))
                    {
                        items.Add(new FindInFilesItem(dataObject, pi.Name, this));
                    }
                }
            };
            return items.ToArray();
        }

        public string GetFileName()
        {
            string s;
            if (dataObject is Document)
            {
                s = "Document";
            }
            else if (dataObject is FunctionObjectRec)
            {
                s = "Functions";
            }
            else if (dataObject is WaveTableObjectRec)
            {
                s = "Wave Tables";
            }
            else if (dataObject is AlgoWaveTableObjectRec)
            {
                s = "Algorithmic Wave Tables";
            }
            else if (dataObject is SampleObjectRec)
            {
                s = "Samples";
            }
            else if (dataObject is AlgoSampObjectRec)
            {
                s = "Algorithmic Samples";
            }
            else if (dataObject is InstrObjectRec)
            {
                s = "Instruments";
            }
            else if (dataObject is TrackObjectRec)
            {
                s = "Tracks";
            }
            else if (dataObject is ScoreEffectsRec)
            {
                s = "Score Effects";
            }
            else if (dataObject is SectionObjectRec)
            {
                s = "Section Effects";
            }
            else if (dataObject is SequencerRec)
            {
                s = "Sequencer Config";
            }
            else if (dataObject is FrameObjectRec)
            {
                s = "Frame";
            }
            else if (dataObject is NoteObjectRec)
            {
                s = "Note";
            }
            else
            {
                s = dataObject.GetType().Name;
            }
            PropertyInfo nameProperty = dataObject.GetType().GetProperty("Name");
            if (nameProperty != null)
            {
                s = String.Concat(s, "/", nameProperty.GetValue(dataObject));
            }
            return s;
        }

        public string GetPath()
        {
            return String.Concat(parent != null ? parent.GetPath() : null, "/", GetFileName());
        }
    }

    public class FindInFilesItem : IFindInFilesItem, IFindInFilesItemActivator
    {
        private readonly object dataObject;
        private readonly string property;
        private readonly FindInFilesNode parent;

        public FindInFilesItem(object dataObject, string property, FindInFilesNode parent)
        {
            this.dataObject = dataObject;
            this.property = property;
            this.parent = parent;
        }

        public object DataObject { get { return dataObject; } }
        public string Property { get { return property; } }
        public FindInFilesNode Parent { get { return parent; } }

        public Stream Open()
        {
            PropertyInfo pi = dataObject.GetType().GetProperty(property);
            object o = pi.GetValue(dataObject);
            string s = String.Empty;
            if (o != null)
            {
                s = o.ToString();
            }
            return new MemoryStream(Encoding.UTF8.GetBytes(s));
        }

        public string GetExtension()
        {
            return String.Empty;
        }

        public string GetFileName()
        {
            return String.Empty;
        }

        public string GetPath()
        {
            return String.Empty;
        }
    }
}
