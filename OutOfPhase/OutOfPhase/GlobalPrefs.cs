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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Windows.Forms;

namespace OutOfPhase
{
    public class GlobalPrefs
    {
        // Publicly supported and advertized properties.
        public int TabSize = 4;
        public bool AutoIndent = true;
        public bool AutosaveEnabled = true;
        public int AutosaveInterval = 5 * 60; // seconds
        public string OutputDevice = ERole.eMultimedia.ToString();
        public string OutputDeviceFriendlyName = String.Empty;
        public string FFTWWisdom = null;
        public int Concurrency = 0; // 0: default, 1: sequential, >1: use C procs, <0: use N-(-C) procs [i.e. reserve]
        public int RecentDocumentsMax = 10;
        public readonly List<string> RecentDocuments = new List<string>();

        // Unadvertised properties used for diagnostics and controlling experimental features.
        // As a rule, these properties are set at startup and must not change for the duration or broken behavior may occur.
        private bool EnableCILSet;
        public bool EnableCIL = true; // enable for .NET jitted code generation (disable for pcode)
        private bool EnableEnvelopeSmoothingSet;
        public bool EnableEnvelopeSmoothing = true; // enable for oscillator envelope smoothing (of loudness and index envelopes)

        public void ReferenceRecentDocument(string path)
        {
            int i = RecentDocuments.IndexOf(path);
            if (i >= 0)
            {
                RecentDocuments.RemoveAt(i);
            }
            RecentDocuments.Insert(0, path);
            if (RecentDocuments.Count > RecentDocumentsMax)
            {
                RecentDocuments.RemoveRange(RecentDocumentsMax, RecentDocuments.Count - RecentDocumentsMax);
            }
        }

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

                TabSize = Math.Min(Math.Max(root.SelectSingleNode("/settings/tabSize").ValueAsInt, Constants.MINTABCOUNT), Constants.MAXTABCOUNT);
                AutoIndent = root.SelectSingleNode("/settings/autoIndent").ValueAsBoolean;
                AutosaveEnabled = root.SelectSingleNode("/settings/autosave").ValueAsBoolean;
                AutosaveInterval = Math.Min(Math.Max(root.SelectSingleNode("/settings/autosaveInterval").ValueAsInt, Constants.MINAUTOSAVEINTERVAL), Constants.MAXAUTOSAVEINTERVAL);
                OutputDevice = root.SelectSingleNode("/settings/outputDevice").Value;
                OutputDeviceFriendlyName = root.SelectSingleNode("/settings/outputDevice/@name").Value;
                Concurrency = root.SelectSingleNode("/settings/concurrency").ValueAsInt;
                if ((nav = root.SelectSingleNode("/settings/fftwfWisdom")) != null)
                {
                    FFTWWisdom = nav.Value;
                }
                nav = root.SelectSingleNode("/settings/recentDocuments/@max");
                if (nav != null)
                {
                    RecentDocumentsMax = Math.Max(0, nav.ValueAsInt);
                }
                foreach (XPathNavigator recentNav in root.Select("/settings/recentDocuments/recentDocument"))
                {
                    RecentDocuments.Add(recentNav.Value);
                }

                if ((nav = root.SelectSingleNode("/settings/enableCIL")) != null)
                {
                    EnableCILSet = true;
                    EnableCIL = nav.ValueAsBoolean;
                }
                if ((nav = root.SelectSingleNode("/settings/enableEnvelopeSmoothing")) != null)
                {
                    EnableEnvelopeSmoothingSet = true;
                    EnableEnvelopeSmoothing = nav.ValueAsBoolean;
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
                    writer.WriteValue(TabSize);
                    writer.WriteEndElement();

                    writer.WriteStartElement("autoIndent");
                    writer.WriteValue(AutoIndent);
                    writer.WriteEndElement();

                    writer.WriteStartElement("autosave");
                    writer.WriteValue(AutosaveEnabled);
                    writer.WriteEndElement();

                    writer.WriteStartElement("autosaveInterval");
                    writer.WriteValue(AutosaveInterval);
                    writer.WriteEndElement();

                    writer.WriteStartElement("outputDevice");
                    writer.WriteStartAttribute("name", null);
                    writer.WriteValue(OutputDeviceFriendlyName);
                    writer.WriteEndAttribute();
                    writer.WriteValue(OutputDevice);
                    writer.WriteEndElement();

                    writer.WriteStartElement("concurrency");
                    writer.WriteValue(Concurrency);
                    writer.WriteEndElement();

                    if (!String.IsNullOrEmpty(FFTWWisdom))
                    {
                        writer.WriteStartElement("fftwfWisdom");
                        writer.WriteValue(FFTWWisdom);
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("recentDocuments");
                    writer.WriteStartAttribute("max");
                    writer.WriteValue(RecentDocumentsMax);
                    writer.WriteEndAttribute();
                    foreach (string recent in RecentDocuments)
                    {
                        writer.WriteStartElement("recentDocument");
                        writer.WriteValue(recent);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();


                    // unadvertised settings

                    if (EnableCILSet)
                    {
                        writer.WriteStartElement("enableCIL");
                        writer.WriteValue(EnableCIL);
                        writer.WriteEndElement();
                    }

                    if (EnableEnvelopeSmoothingSet)
                    {
                        writer.WriteStartElement("enableEnvelopeSmoothing");
                        writer.WriteValue(EnableEnvelopeSmoothing);
                        writer.WriteEndElement();
                    }


                    writer.WriteEndElement();
                }
            }
        }
    }
}
