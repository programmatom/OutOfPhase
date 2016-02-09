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
    public class Registration
    {
        private readonly Dictionary<object, List<Form>> allForms = new Dictionary<object, List<Form>>();

        public Registration()
        {
        }

        private void EnforceNotRegistered(object dataSource)
        {
            if (allForms.ContainsKey(dataSource))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
        }

        private void EnforceNotRegistered(Form form)
        {
            foreach (KeyValuePair<object, List<Form>> registration in allForms)
            {
                foreach (Form form1 in registration.Value)
                {
                    if (form == form1)
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                }
            }
        }

        public int RegisteredDataSourceCount
        {
            get
            {
                return allForms.Count;
            }
        }

        public int RegisteredFormCount
        {
            get
            {
                int c = 0;
                foreach (KeyValuePair<object, List<Form>> entry in allForms)
                {
                    c += entry.Value.Count;
                }
                return c;
            }
        }

        public FormRegistrationToken Register(object dataSource, Form form)
        {
            EnforceNotRegistered(form);

            List<Form> formsForObject;
            if (!allForms.TryGetValue(dataSource, out formsForObject))
            {
                formsForObject = new List<Form>();
                allForms.Add(dataSource, formsForObject);
            }

            formsForObject.Add(form);

            return new FormRegistrationToken(this, dataSource, form);
        }

        public void Unregister(object dataSource, Form form)
        {
            List<Form> formsForObject;
            if (!allForms.TryGetValue(dataSource, out formsForObject))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            int i = formsForObject.IndexOf(form);
            if (i < 0)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            formsForObject.RemoveAt(i);

            if (formsForObject.Count == 0)
            {
                allForms.Remove(dataSource);
                EnforceNotRegistered(dataSource);
            }

            EnforceNotRegistered(form);
        }

        public bool Activate(object dataSource, out Form form)
        {
            form = null;

            List<Form> formsForObject;
            if (!allForms.TryGetValue(dataSource, out formsForObject))
            {
                return false;
            }

            if (formsForObject.Count == 0)
            {
                return false;
            }

            form = formsForObject[0];
            form.Activate();
            return true;
        }

        public bool Activate(object dataSource)
        {
            Form form;
            return Activate(dataSource, out form);
        }

        public bool CloseAll(object dataSource)
        {
            List<Form> formsForObject;
            if (allForms.TryGetValue(dataSource, out formsForObject))
            {
                while (formsForObject.Count > 0)
                {
                    Form form = formsForObject[0];

                    bool closed = false;
                    FormClosedEventHandler handler = delegate (object sender, FormClosedEventArgs e) { closed = true; };
                    form.FormClosed += handler;
                    form.Close();
                    if (!closed)
                    {
                        form.FormClosed -= handler;
                        return false;
                    }

                    if (formsForObject.IndexOf(form) >= 0)
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }

                allForms.Remove(dataSource);
            }

            EnforceNotRegistered(dataSource);

            return true;
        }

        public bool CloseAll()
        {
            List<object> dataSources = new List<object>(allForms.Keys);
            foreach (object dataSource in dataSources)
            {
                if (!CloseAll(dataSource))
                {
                    return false;
                }
            }
            return true;
        }

        public bool EnsureValidateAndCommit(object dataSource)
        {
            List<Form> formsForObject;
            if (allForms.TryGetValue(dataSource, out formsForObject))
            {
                foreach (Form form in new List<Form>(formsForObject))
                {
                    if (!form.Validate())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool EnsureValidateAndCommit()
        {
            List<object> dataSources = new List<object>(allForms.Keys);
            foreach (object dataSource in dataSources)
            {
                if (!EnsureValidateAndCommit(dataSource))
                {
                    return false;
                }
            }
            return true;
        }

        public void NotifyGlobalNameChanged(object dataSource)
        {
            List<Form> formsForObject;
            if (allForms.TryGetValue(dataSource, out formsForObject))
            {
                foreach (Form form in new List<Form>(formsForObject))
                {
                    IGlobalNameChange nameChange = form as IGlobalNameChange;
                    if (nameChange != null)
                    {
                        nameChange.GlobalNameChanged();
                    }
                }
            }
        }

        public void NotifyGlobalNameChanged()
        {
            List<object> dataSources = new List<object>(allForms.Keys);
            foreach (object dataSource in dataSources)
            {
                NotifyGlobalNameChanged(dataSource);
            }
        }

        public delegate bool SelectionMethod(FormRegistrationToken token);
        public FormRegistrationToken Find(SelectionMethod selector)
        {
            foreach (KeyValuePair<object, List<Form>> dataObject in allForms)
            {
                foreach (Form editor in dataObject.Value)
                {
                    FormRegistrationToken token = new FormRegistrationToken(this, dataObject.Key, editor);
                    if (selector(token))
                    {
                        return token;
                    }
                }
            }
            return null;
        }
    }

    public class FormRegistrationToken
    {
        private readonly Registration registration;
        private readonly object dataSource;
        private readonly Form form;

        private FormRegistrationToken()
        {
        }

        public object DataSource { get { return dataSource; } }
        public Form Editor { get { return form; } }

        public FormRegistrationToken(Registration registration, object dataSource, Form form)
        {
            this.registration = registration;
            this.dataSource = dataSource;
            this.form = form;
        }

        public void Unregister()
        {
            registration.Unregister(dataSource, form);
        }
    }

    public interface IGlobalNameChange
    {
        void GlobalNameChanged();
    }
}
