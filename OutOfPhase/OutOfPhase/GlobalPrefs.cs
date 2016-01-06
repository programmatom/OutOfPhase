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

namespace OutOfPhase
{
    public class GlobalPrefs
    {
        public int TabSize = 4;
        public bool AutoIndent = true;
        public bool AutosaveEnabled = true;
        public int AutosaveInterval = 5 * 60; // seconds
        public string OutputDevice = ERole.eMultimedia.ToString();
        public string FFTWWisdom = null;
        public int Concurrency = 0; // 0: default, 1: sequential, >1: use C procs, <0: use N-(-C) procs [i.e. reserve]

        public GlobalPrefs()
        {
        }

        public GlobalPrefs(string path)
            : this()
        {
            XPathNavigator nav;

            XmlDocument xml = new XmlDocument();
            xml.Load(path);

            TabSize = Math.Min(Math.Max(xml.CreateNavigator().SelectSingleNode("/settings/tabSize").ValueAsInt, Constants.MINTABCOUNT), Constants.MAXTABCOUNT);
            AutoIndent = xml.CreateNavigator().SelectSingleNode("/settings/autoIndent").ValueAsBoolean;
            AutosaveEnabled = xml.CreateNavigator().SelectSingleNode("/settings/autosave").ValueAsBoolean;
            AutosaveInterval = Math.Min(Math.Max(xml.CreateNavigator().SelectSingleNode("/settings/autosaveInterval").ValueAsInt, Constants.MINAUTOSAVEINTERVAL), Constants.MAXAUTOSAVEINTERVAL);
            OutputDevice = xml.CreateNavigator().SelectSingleNode("/settings/outputDevice").Value;
            Concurrency = xml.CreateNavigator().SelectSingleNode("/settings/concurrency").ValueAsInt;
            if ((nav = xml.CreateNavigator().SelectSingleNode("/settings/fftwfWisdom")) != null)
            {
                FFTWWisdom = nav.Value;
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

                    writer.WriteEndElement();
                }
            }
        }
    }
}
