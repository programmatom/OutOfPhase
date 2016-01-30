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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Windows.Forms;

namespace OutOfPhase
{
    public class GlobalPrefs : INotifyPropertyChanged
    {
        // Publicly supported and advertized properties.

        private int _TabSize = 4;
        public int TabSize { get { return _TabSize; } set { _TabSize = value; Notify("TabSize"); } }

        private bool _AutoIndent = true;
        public bool AutoIndent { get { return _AutoIndent; } set { _AutoIndent = value; Notify("AutoIndent"); } }

        private bool _AutosaveEnabled = true;
        public bool AutosaveEnabled { get { return _AutosaveEnabled; } set { _AutosaveEnabled = value; Notify("AutosaveEnabled"); } }

        private int _AutosaveInterval = 5 * 60; // seconds
        public int AutosaveInterval { get { return _AutosaveInterval; } set { _AutosaveInterval = value; Notify("AutosaveInterval"); } }

        private string _OutputDevice = ERole.eMultimedia.ToString();
        public string OutputDevice { get { return _OutputDevice; } set { _OutputDevice = value; Notify("OutputDevice"); } }

        private string _OutputDeviceFriendlyName = String.Empty;
        public string OutputDeviceFriendlyName { get { return _OutputDeviceFriendlyName; } set { _OutputDeviceFriendlyName = value; Notify("OutputDeviceFriendlyName"); } }

        private string _FFTWWisdom = null;
        public string FFTWWisdom { get { return _FFTWWisdom; } set { _FFTWWisdom = value; Notify("FFTWWisdom"); } }

        private int _Concurrency = 0; // 0: default, 1: sequential, >1: use C procs, <0: use N-(-C) procs [i.e. reserve]
        public int Concurrency { get { return _Concurrency; } set { _Concurrency = value; Notify("Concurrency"); } }

        private int _RecentDocumentsMax = 10;
        public int RecentDocumentsMax { get { return _RecentDocumentsMax; } set { _RecentDocumentsMax = value; Notify("RecentDocumentsMax"); } }

        private readonly BindingList<string> _RecentDocuments = new BindingList<string>();
        public BindingList<string> RecentDocuments { get { return _RecentDocuments; } }

        // Unadvertised properties used for diagnostics and controlling experimental features.
        // As a rule, these properties are set at startup and must not change for the duration or broken behavior may occur.

        private bool _EnableCILSet;
        private bool _EnableCIL = true; // enable for .NET jitted code generation (disable for pcode)
        public bool EnableCIL { get { return _EnableCIL; } }

        private bool _EnableEnvelopeSmoothingSet;
        private bool _EnableEnvelopeSmoothing = true; // enable for oscillator envelope smoothing (of loudness and index envelopes)
        public bool EnableEnvelopeSmoothing { get { return _EnableEnvelopeSmoothing; } }

        private bool _EnablePriorityBoostSet;
        private bool _EnablePriorityBoost = true; // enable for synth engine threads priority boost during synthesis
        public bool EnablePriorityBoost { get { return _EnablePriorityBoost; } }

        private bool _EnableDirectWriteSet;
        private bool _EnableDirectWrite = false; // enable DirectWrite rendering of UI instead of GDI rendering
        public bool EnableDirectWrite { get { return _EnableDirectWrite; } }

        private bool _MaximumSmoothedParameterCountSet;
        private int _MaximumSmoothedParameterCount = 16;
        public int MaximumSmoothedParameterCount { get { return _MaximumSmoothedParameterCount; } }


        //

        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        //

        public void ReferenceRecentDocument(string path)
        {
            int i = RecentDocuments.IndexOf(path);
            if (i >= 0)
            {
                RecentDocuments.RemoveAt(i);
            }
            RecentDocuments.Insert(0, path);
#if false // BindingList doesn't support RemoveRange()
            if (RecentDocuments.Count > RecentDocumentsMax)
            {
                RecentDocuments.RemoveRange(RecentDocumentsMax, RecentDocuments.Count - RecentDocumentsMax);
            }
#else
            while (RecentDocuments.Count > RecentDocumentsMax)
            {
                RecentDocuments.RemoveAt(RecentDocuments.Count - 1);
            }
#endif
        }


        //

        public GlobalPrefs()
        {
        }

        public GlobalPrefs(string path)
            : this()
        {
            try
            {
                XPathNavigator nav;

                XmlDocument xml = new XmlDocument();
                xml.Load(path);
                XPathNavigator root = xml.CreateNavigator();

                _TabSize = Math.Min(Math.Max(root.SelectSingleNode("/settings/tabSize").ValueAsInt, Constants.MINTABCOUNT), Constants.MAXTABCOUNT);
                _AutoIndent = root.SelectSingleNode("/settings/autoIndent").ValueAsBoolean;
                _AutosaveEnabled = root.SelectSingleNode("/settings/autosave").ValueAsBoolean;
                _AutosaveInterval = Math.Min(Math.Max(root.SelectSingleNode("/settings/autosaveInterval").ValueAsInt, Constants.MINAUTOSAVEINTERVAL), Constants.MAXAUTOSAVEINTERVAL);
                _OutputDevice = root.SelectSingleNode("/settings/outputDevice").Value;
                _OutputDeviceFriendlyName = root.SelectSingleNode("/settings/outputDevice/@name").Value;
                _Concurrency = root.SelectSingleNode("/settings/concurrency").ValueAsInt;
                if ((nav = root.SelectSingleNode("/settings/fftwfWisdom")) != null)
                {
                    _FFTWWisdom = nav.Value;
                }
                nav = root.SelectSingleNode("/settings/recentDocuments/@max");
                if (nav != null)
                {
                    _RecentDocumentsMax = Math.Max(0, nav.ValueAsInt);
                }
                foreach (XPathNavigator recentNav in root.Select("/settings/recentDocuments/recentDocument"))
                {
                    _RecentDocuments.Add(recentNav.Value);
                }

                if ((nav = root.SelectSingleNode("/settings/enableCIL")) != null)
                {
                    _EnableCILSet = true;
                    _EnableCIL = nav.ValueAsBoolean;
                }
                if ((nav = root.SelectSingleNode("/settings/enableEnvelopeSmoothing")) != null)
                {
                    _EnableEnvelopeSmoothingSet = true;
                    _EnableEnvelopeSmoothing = nav.ValueAsBoolean;
                }
                if ((nav = root.SelectSingleNode("/settings/enablePriorityBoost")) != null)
                {
                    _EnablePriorityBoostSet = true;
                    _EnablePriorityBoost = nav.ValueAsBoolean;
                }
                if ((nav = root.SelectSingleNode("/settings/enableDirectWrite")) != null)
                {
                    _EnableDirectWriteSet = true;
                    _EnableDirectWrite = nav.ValueAsBoolean;
                }
                if ((nav = root.SelectSingleNode("/settings/maximumSmoothedParameterCount")) != null)
                {
                    _MaximumSmoothedParameterCountSet = true;
                    _MaximumSmoothedParameterCount = nav.ValueAsInt;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(String.Format("An error ocurred parsing the application preferences; some preferences will be reset. {0}", exception), "Out Of Phase");
            }
        }

        public void Save(string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (Stream output = new FileStream(path, FileMode.Create))
            {
                using (XmlWriter writer = XmlWriter.Create(output, settings))
                {
                    writer.WriteStartElement("settings");


                    // advertised settings

                    writer.WriteStartElement("tabSize");
                    writer.WriteValue(_TabSize);
                    writer.WriteEndElement();

                    writer.WriteStartElement("autoIndent");
                    writer.WriteValue(_AutoIndent);
                    writer.WriteEndElement();

                    writer.WriteStartElement("autosave");
                    writer.WriteValue(_AutosaveEnabled);
                    writer.WriteEndElement();

                    writer.WriteStartElement("autosaveInterval");
                    writer.WriteValue(_AutosaveInterval);
                    writer.WriteEndElement();

                    writer.WriteStartElement("outputDevice");
                    writer.WriteStartAttribute("name", null);
                    writer.WriteValue(_OutputDeviceFriendlyName);
                    writer.WriteEndAttribute();
                    writer.WriteValue(_OutputDevice);
                    writer.WriteEndElement();

                    writer.WriteStartElement("concurrency");
                    writer.WriteValue(_Concurrency);
                    writer.WriteEndElement();

                    if (!String.IsNullOrEmpty(FFTWWisdom))
                    {
                        writer.WriteStartElement("fftwfWisdom");
                        writer.WriteValue(_FFTWWisdom);
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("recentDocuments");
                    writer.WriteStartAttribute("max");
                    writer.WriteValue(_RecentDocumentsMax);
                    writer.WriteEndAttribute();
                    foreach (string recent in RecentDocuments)
                    {
                        writer.WriteStartElement("recentDocument");
                        writer.WriteValue(recent);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();


                    // unadvertised settings

                    if (_EnableCILSet)
                    {
                        writer.WriteStartElement("enableCIL");
                        writer.WriteValue(_EnableCIL);
                        writer.WriteEndElement();
                    }

                    if (_EnableEnvelopeSmoothingSet)
                    {
                        writer.WriteStartElement("enableEnvelopeSmoothing");
                        writer.WriteValue(_EnableEnvelopeSmoothing);
                        writer.WriteEndElement();
                    }

                    if (_EnablePriorityBoostSet)
                    {
                        writer.WriteStartElement("enablePriorityBoost");
                        writer.WriteValue(_EnablePriorityBoost);
                        writer.WriteEndElement();
                    }

                    if (_EnableDirectWriteSet)
                    {
                        writer.WriteStartElement("enableDirectWrite");
                        writer.WriteValue(_EnableDirectWrite);
                        writer.WriteEndElement();
                    }

                    if (_MaximumSmoothedParameterCountSet)
                    {
                        writer.WriteStartElement("maximumSmoothedParameterCount");
                        writer.WriteValue(_MaximumSmoothedParameterCount);
                        writer.WriteEndElement();
                    }


                    writer.WriteEndElement();
                }
            }
        }
    }
}
