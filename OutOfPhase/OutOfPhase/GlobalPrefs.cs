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
using System.Diagnostics;
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

        public int TabSize
        {
            get { return FindSimpleProperty<int>("TabSize").Value; }
            set { FindSimpleProperty<int>("TabSize").Value = value; Notify("TabSize"); }
        }

        public bool AutoIndent
        {
            get { return FindSimpleProperty<bool>("AutoIndent").Value; }
            set { FindSimpleProperty<bool>("AutoIndent").Value = value; Notify("AutoIndent"); }
        }

        public bool AutosaveEnabled
        {
            get { return FindSimpleProperty<bool>("AutosaveEnabled").Value; }
            set { FindSimpleProperty<bool>("AutosaveEnabled").Value = value; Notify("AutosaveEnabled"); }
        }

        public int AutosaveIntervalSeconds
        {
            get { return FindSimpleProperty<int>("AutosaveIntervalSeconds").Value; }
            set { FindSimpleProperty<int>("AutosaveIntervalSeconds").Value = value; Notify("AutosaveIntervalSeconds"); }
        }

        public string OutputDevice
        {
            get { return FindSimpleProperty<string>("OutputDevice").Value; }
            set { FindSimpleProperty<string>("OutputDevice").Value = value; Notify("OutputDevice"); }
        }

        public string OutputDeviceName
        {
            get { return FindSimpleProperty<string>("OutputDeviceName").Value; }
            set { FindSimpleProperty<string>("OutputDeviceName").Value = value; Notify("OutputDeviceName"); }
        }

        public string FFTWWisdom
        {
            get { return FindSimpleProperty<string>(Environment.Is64BitProcess ? "FFTWWisdom64f" : "FFTWWisdom32f").Value; }
            set
            {
                FindSimpleProperty<string>(Environment.Is64BitProcess ? "FFTWWisdom64f" : "FFTWWisdom32f").Value = value;
                Notify("FFTWWisdom");
            }
        }

        public int Concurrency // 0: default, 1: sequential, >1: use C procs, <0: use N-(-C) procs [i.e. reserve]
        {
            get { return FindSimpleProperty<int>("Concurrency").Value; }
            set { FindSimpleProperty<int>("Concurrency").Value = value; Notify("Concurrency"); }
        }

        public int RecentDocumentsMax
        {
            get { return FindSimpleProperty<int>("RecentDocumentsMax").Value; }
            set { FindSimpleProperty<int>("RecentDocumentsMax").Value = value; Notify("RecentDocumentsMax"); }
        }

        public BindingList<string> RecentDocuments
        {
            get { return ((GlobalPrefsListItem<string>)FindProperty("RecentDocuments")).List; }
        }

        // Unadvertised properties used for diagnostics and controlling experimental features.
        // As a rule, these properties are set at startup and must not change for the duration or broken behavior may occur.

        public bool EnableCIL // enable for .NET jitted code generation (disable for pcode)
        {
            get { return FindSimpleProperty<bool>("EnableCIL").Value; }
            set { FindSimpleProperty<bool>("EnableCIL").Value = value; Notify("EnableCIL"); }
        }

        public bool EnableEnvelopeSmoothing // enable for oscillator envelope smoothing (of loudness and index envelopes)
        {
            get { return FindSimpleProperty<bool>("EnableEnvelopeSmoothing").Value; }
            set { FindSimpleProperty<bool>("EnableEnvelopeSmoothing").Value = value; Notify("EnableEnvelopeSmoothing"); }
        }

        public int PriorityMode // 0: default (#2), 1: no priority boost, 0: "Audio" (AboveNormal), 3: "Pro Audio" (High)
        {
            get { return FindSimpleProperty<int>("PriorityMode").Value; }
            set { FindSimpleProperty<int>("PriorityMode").Value = value; Notify("PriorityMode"); }
        }

        public bool EnableDirectWrite // enable DirectWrite rendering of UI instead of GDI rendering
        {
            get { return FindSimpleProperty<bool>("EnableDirectWrite").Value; }
            set { FindSimpleProperty<bool>("EnableDirectWrite").Value = value; Notify("EnableDirectWrite"); }
        }

        public int MaximumSmoothedParameterCount // specifies number of vector-aligned workspaces reserved during synth engine creation
        {
            get { return FindSimpleProperty<int>("MaximumSmoothedParameterCount").Value; }
            set { FindSimpleProperty<int>("MaximumSmoothedParameterCount").Value = value; Notify("MaximumSmoothedParameterCount"); }
        }

        public bool EnableTrackViewOffscreenCompositing
        {
            get { return FindSimpleProperty<bool>("EnableTrackViewOffscreenCompositing").Value; }
            set { FindSimpleProperty<bool>("EnableTrackViewOffscreenCompositing").Value = value; Notify("EnableTrackViewOffscreenCompositing"); }
        }

        public bool EnableBravura
        {
            get { return FindSimpleProperty<bool>("EnableBravura").Value; }
            set { FindSimpleProperty<bool>("EnableBravura").Value = value; Notify("EnableBravura"); }
        }


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
            while (RecentDocuments.Count > RecentDocumentsMax) // BindingList doesn't support RemoveRange()
            {
                RecentDocuments.RemoveAt(RecentDocuments.Count - 1);
            }
        }


        //

        private readonly GlobalPrefsItem[] items;

        private static readonly bool WriteAll = false;

        public GlobalPrefs()
        {
            items = new GlobalPrefsItem[]
            {
                // advertised properties
                new GlobalPrefsSimpleItem<int>("TabSize", 4),
                new GlobalPrefsSimpleItem<bool>("AutoIndent", true),
                new GlobalPrefsSimpleItem<bool>("AutosaveEnabled", true),
                new GlobalPrefsSimpleItem<int>("AutosaveIntervalSeconds", 5 * 60),
                new GlobalPrefsSimpleItem<string>("OutputDevice", ERole.eMultimedia.ToString()),
                new GlobalPrefsSimpleItem<string>("OutputDeviceName", String.Empty),
                new GlobalPrefsComputerNameKeyedItem<string>("FFTWWisdom32f", String.Empty),
                new GlobalPrefsComputerNameKeyedItem<string>("FFTWWisdom64f", String.Empty),
                new GlobalPrefsSimpleItem<int>("Concurrency", 0),
                new GlobalPrefsSimpleItem<int>("PriorityMode", 0),
                new GlobalPrefsSimpleItem<int>("RecentDocumentsMax", 10),
                new GlobalPrefsListItem<string>("RecentDocuments"),

                // unadvertised properties
                new GlobalPrefsSimpleItem<bool>("EnableCIL", true),
                new GlobalPrefsSimpleItem<bool>("EnableEnvelopeSmoothing", true),
                new GlobalPrefsSimpleItem<bool>("EnableDirectWrite", false),
                new GlobalPrefsSimpleItem<int>("MaximumSmoothedParameterCount",
                    Synthesizer.PluggableEffectUserEffectFactory.DefaultMaximumSmoothedParameterCount),
                new GlobalPrefsSimpleItem<bool>("EnableTrackViewOffscreenCompositing", true),
                new GlobalPrefsSimpleItem<bool>("EnableBravura", false),
            };
        }

        public GlobalPrefs(string path)
            : this()
        {
            StringBuilder errors = new StringBuilder();

            if (File.Exists(path))
            {
                try
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(path);

                    XPathNavigator settingsContainer = xml.CreateNavigator().SelectSingleNode("/settings");
                    if (settingsContainer == null)
                    {
                        return;
                    }

                    foreach (GlobalPrefsItem item in items)
                    {
                        try
                        {
                            item.Read(settingsContainer);
                        }
                        catch (Exception exception)
                        {
                            errors.AppendLine(String.Format("{0}: {1}", item.PropertyName, exception.ToString()));
                        }
                    }
                }
                catch (Exception exception)
                {
                    errors.AppendLine(exception.ToString());
                }
            }

            if (errors.Length != 0)
            {
                MessageBox.Show(String.Format("An error ocurred parsing the application preferences; some preferences will be reset. {0}", errors), "Out Of Phase");
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

                    foreach (GlobalPrefsItem item in items)
                    {
                        item.Write(writer);
                    }

                    writer.WriteEndElement();
                }
            }
        }

        public bool CopyTo(GlobalPrefs dest)
        {
            bool changed = false;
            Debug.Assert(this.items.Length == dest.items.Length);
            for (int i = 0; i < this.items.Length; i++)
            {
                Debug.Assert(this.items[i].GetType() == dest.items[i].GetType());
                changed = this.items[i].CopyTo(dest.items[i]) || changed;
            }
            return changed;
        }


        // generic property immplementation

        private abstract class GlobalPrefsItem
        {
            protected readonly string propertyName;

            protected GlobalPrefsItem(string propertyName)
            {
                this.propertyName = propertyName;
            }

            public abstract void Read(XPathNavigator containerNav);
            public abstract void Write(XmlWriter writer);

            public string PropertyName { get { return propertyName; } }

            public abstract bool CopyTo(GlobalPrefsItem dest);
        }

        private class GlobalPrefsSimpleItem<T> : GlobalPrefsItem
        {
            private T value;
            protected readonly T defaultValue;
            private bool explicitlySet;

            public GlobalPrefsSimpleItem(string propertyName, T defaultValue)
                : base(propertyName)
            {
                this.value = this.defaultValue = defaultValue;
            }

            public override void Read(XPathNavigator containerNav)
            {
                XPathNavigator nav = containerNav.SelectSingleNode(propertyName);
                if (nav != null)
                {
                    explicitlySet = true;
                    string text = nav.Value;
                    value = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(text);
                }
            }

            public override void Write(XmlWriter writer)
            {
                if (NeedsWriting)
                {
                    writer.WriteStartElement(propertyName);
                    string text = TypeDescriptor.GetConverter(typeof(T)).ConvertToString(value);
                    writer.WriteValue(text);
                    writer.WriteEndElement();
                }
            }

            public virtual T Value { get { return value; } set { this.value = value; explicitlySet = true; } }

            public virtual bool NeedsWriting { get { return explicitlySet || !EqualityComparer<T>.Default.Equals(value, defaultValue) || WriteAll; } }

            public override bool CopyTo(GlobalPrefsItem destBase)
            {
                GlobalPrefsSimpleItem<T> dest = (GlobalPrefsSimpleItem<T>)destBase; // covariant
                if (explicitlySet || !EqualityComparer<T>.Default.Equals(this.value, dest.value))
                {
                    dest.Value = this.value;
                    return true;
                }
                return false;
            }
        }

        // Support for roaming profiles - so machine-specific items can be kept separate
        private class GlobalPrefsComputerNameKeyedItem<T> : GlobalPrefsSimpleItem<T>
        {
            private readonly Dictionary<string, GlobalPrefsSimpleItem<T>> values = new Dictionary<string, GlobalPrefsSimpleItem<T>>();
            private const string ItemName = "item";
            private const string ComputerName = "computerName";
            private const string ValueName = "value";

            public GlobalPrefsComputerNameKeyedItem(string propertyName, T defaultValue)
                : base(propertyName, defaultValue)
            {
            }

            public override void Read(XPathNavigator containerNav)
            {
                XPathNavigator navTop = containerNav.SelectSingleNode(propertyName);
                if (navTop != null)
                {
                    foreach (XPathNavigator navItem in navTop.Select(ItemName))
                    {
                        string computerName = navItem.SelectSingleNode(ComputerName).Value;
                        GlobalPrefsSimpleItem<T> item = new GlobalPrefsSimpleItem<T>(ValueName, defaultValue);
                        item.Read(navItem);
                        values.Add(computerName, item);
                    }
                }
            }

            public override void Write(XmlWriter writer)
            {
                if (NeedsWriting)
                {
                    writer.WriteStartElement(propertyName);

                    foreach (KeyValuePair<string, GlobalPrefsSimpleItem<T>> item in values)
                    {
                        if (item.Value.NeedsWriting)
                        {
                            writer.WriteStartElement(ItemName);

                            writer.WriteStartElement(ComputerName);
                            writer.WriteValue(item.Key);
                            writer.WriteEndElement();

                            item.Value.Write(writer);

                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteEndElement();
                }
            }

            public override T Value
            {
                get
                {
                    GlobalPrefsSimpleItem<T> item = GetItemForCurrentComputer();
                    return item.Value;
                }
                set
                {
                    GlobalPrefsSimpleItem<T> item = GetItemForCurrentComputer();
                    item.Value = value;
                }
            }

            public override bool NeedsWriting
            {
                get
                {
                    bool needsWriting = false;
                    foreach (GlobalPrefsSimpleItem<T> item in values.Values)
                    {
                        if (item.NeedsWriting)
                        {
                            needsWriting = true;
                            break;
                        }
                    }
                    return needsWriting || WriteAll;
                }
            }

            private GlobalPrefsSimpleItem<T> GetItemForCurrentComputer()
            {
                string key = Environment.GetEnvironmentVariable("COMPUTERNAME");
                GlobalPrefsSimpleItem<T> item;
                if (!values.TryGetValue(key, out item))
                {
                    item = new GlobalPrefsSimpleItem<T>(ValueName, defaultValue);
                    values.Add(key, item);
                }
                return item;
            }

            public override bool CopyTo(GlobalPrefsItem destBase)
            {
                bool result = false;
                GlobalPrefsComputerNameKeyedItem<T> dest = (GlobalPrefsComputerNameKeyedItem<T>)destBase; // covariant
                dest.values.Clear();
                foreach (KeyValuePair<string, GlobalPrefsSimpleItem<T>> item in values)
                {
                    GlobalPrefsSimpleItem<T> copy = new GlobalPrefsSimpleItem<T>(ValueName, defaultValue);
                    dest.values.Add(item.Key, copy);
                    result = item.Value.CopyTo(copy) || result;
                }
                return result;
            }
        }

        private class GlobalPrefsListItem<T> : GlobalPrefsItem
        {
            private readonly BindingList<T> items = new BindingList<T>();
            private const string ItemNodeName = "item";

            public GlobalPrefsListItem(string nodeName)
                : base(nodeName)
            {
            }

            public override void Read(XPathNavigator containerNav)
            {
                XPathNavigator topNav = containerNav.SelectSingleNode(propertyName);
                if (topNav != null)
                {
                    foreach (XPathNavigator itemNav in topNav.Select(ItemNodeName))
                    {
                        string text = itemNav.Value;
                        if (text == null)
                        {
                            text = String.Empty;
                        }
                        T value = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(text);
                        items.Add(value);
                    }
                }
            }

            public override void Write(XmlWriter writer)
            {
                if (items.Count != 0)
                {
                    writer.WriteStartElement(propertyName);

                    foreach (T value in items)
                    {
                        writer.WriteStartElement(ItemNodeName);
                        string text = TypeDescriptor.GetConverter(typeof(T)).ConvertToString(value);
                        writer.WriteValue(text);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }
            }

            public BindingList<T> List { get { return items; } }

            public override bool CopyTo(GlobalPrefsItem destBase)
            {
                GlobalPrefsListItem<T> dest = (GlobalPrefsListItem<T>)destBase; // covariant
                if (dest.items.Count == this.items.Count)
                {
                    for (int i = 0; i < this.items.Count; i++)
                    {
                        if (!EqualityComparer<T>.Default.Equals(dest.items[i], this.items[i]))
                        {
                            goto NotEqual;
                        }
                    }
                    return false;
                }
            NotEqual:
                dest.items.Clear();
                for (int i = 0; i < this.items.Count; i++)
                {
                    dest.items.Add(this.items[i]);
                }
                return true;
            }
        }

        private GlobalPrefsItem FindProperty(string propertyName)
        {
            foreach (GlobalPrefsItem item in items)
            {
                if (String.Equals(propertyName, item.PropertyName))
                {
                    return item;
                }
            }
            Debug.Assert(false);
            throw new ArgumentException();
        }

        private GlobalPrefsSimpleItem<T> FindSimpleProperty<T>(string propertyName)
        {
            GlobalPrefsItem item = FindProperty(propertyName);
            Debug.Assert(item is GlobalPrefsSimpleItem<T>);
            return (GlobalPrefsSimpleItem<T>)item;
        }
    }
}
