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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace OutOfPhase
{
    public static class XmlUtil
    {
        public static int Val(int v, int l, int h)
        {
            Debug.Assert(l <= h);
            if ((v < l) || (v > h))
            {
                throw new InvalidDataException();
            }
            return v;
        }
    }

    public interface IXmlStack
    {
        XmlLevel Push(string name);
        void Pop();
    }

    public class XmlLevel : IDisposable
    {
        private readonly IXmlStack stack;
        private readonly bool empty;

        public XmlLevel(IXmlStack stack, bool empty)
        {
            this.stack = stack;
            this.empty = empty;
        }

        public void Dispose()
        {
            if (!empty)
            {
                stack.Pop();
            }
        }
    }

    public class XmlReaderStack : IXmlStack, IDisposable
    {
        private readonly XmlReader reader;


        public XmlReaderStack(Stream stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();

            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;

            this.reader = XmlReader.Create(stream, settings);
        }

        public void Dispose()
        {
            reader.Dispose();
        }


        public XmlLevel Push(string name)
        {
            bool empty = reader.IsEmptyElement;
            reader.ReadStartElement(name);
            return new XmlLevel(this, empty);
        }

        public void Pop()
        {
            reader.ReadEndElement();
        }


        public bool Test(string name)
        {
            return (reader.NodeType == XmlNodeType.Element) && String.Equals(reader.LocalName, name);
        }

        public string ReadString(string name)
        {
            if (!Test(name))
            {
                throw new InvalidDataException();
            }
            using (XmlLevel level = Push(name))
            {
                string s = reader.ReadContentAsString();
                return s != null ? s : String.Empty;
            }
        }

        public int ReadInt(string name)
        {
            if (!Test(name))
            {
                throw new InvalidDataException();
            }
            using (XmlLevel level = Push(name))
            {
                return reader.ReadContentAsInt();
            }
        }

        public int? ReadIntNullable(string name)
        {
            if (!Test(name))
            {
                throw new InvalidDataException();
            }
            using (XmlLevel level = Push(name))
            {
                string s = reader.ReadContentAsString();
                if (String.IsNullOrEmpty(s))
                {
                    return null;
                }
                return Int32.Parse(s);
            }
        }

        public double ReadDouble(string name)
        {
            if (!Test(name))
            {
                throw new InvalidDataException();
            }
            using (XmlLevel level = Push(name))
            {
                return reader.ReadContentAsDouble();
            }
        }

        public double? ReadDoubleNullable(string name)
        {
            if (!Test(name))
            {
                throw new InvalidDataException();
            }
            using (XmlLevel level = Push(name))
            {
                string s = reader.ReadContentAsString();
                if (String.IsNullOrEmpty(s))
                {
                    return null;
                }
                return Double.Parse(s);
            }
        }

        public bool ReadBool(string name)
        {
            if (!Test(name))
            {
                throw new InvalidDataException();
            }
            using (XmlLevel level = Push(name))
            {
                return reader.ReadContentAsBoolean();
            }
        }

        public bool? ReadBoolNullable(string name)
        {
            if (!Test(name))
            {
                throw new InvalidDataException();
            }
            using (XmlLevel level = Push(name))
            {
                string s = reader.ReadContentAsString();
                if (String.IsNullOrEmpty(s))
                {
                    return null;
                }
                return Boolean.Parse(s);
            }
        }
    }


    public class XmlWriterStack : IXmlStack, IDisposable
    {
        private readonly XmlWriter writer;


        public XmlWriterStack(Stream stream)
        {
            XmlWriterSettings writerSettings = new XmlWriterSettings();

            // TODO: shrink file size by removing indenting when substantal debugging is no longer needed
            writerSettings.Indent = true;
            writerSettings.IndentChars = new string('\t', 1);
            writerSettings.Encoding = Encoding.UTF8;

            this.writer = XmlWriter.Create(stream, writerSettings);
        }

        public void Dispose()
        {
            writer.Dispose();
        }


        public XmlLevel Push(string name)
        {
            writer.WriteStartElement(name);
            return new XmlLevel(this, false/*empty*/);
        }

        public void Pop()
        {
            writer.WriteEndElement();
        }


        private void Write<T>(string name, T value)
        {
            if (!name.StartsWith("@"))
            {
                writer.WriteStartElement(name);
                writer.WriteValue(value);
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteStartAttribute(name);
                writer.WriteValue(value);
                writer.WriteEndAttribute();
            }
        }

        private void WriteNullable<T>(string name, T? value) where T : struct
        {
            if (!name.StartsWith("@"))
            {
                writer.WriteStartElement(name);
                writer.WriteValue(value);
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteStartAttribute(name);
                writer.WriteValue(value);
                writer.WriteEndAttribute();
            }
        }

        public void WriteString(string name, string value)
        {
            Write(name, value != null ? value : String.Empty);
        }

        public void WriteInt(string name, int value)
        {
            Write(name, value);
        }

        public void WriteIntNullable(string name, int? value)
        {
            WriteNullable(name, value);
        }

        public void WriteDouble(string name, double value)
        {
            Write(name, value);
        }

        public void WriteDoubleNullable(string name, double? value)
        {
            WriteNullable(name, value);
        }

        public void WriteBool(string name, bool value)
        {
            Write(name, value);
        }

        public void WriteBoolNullable(string name, bool? value)
        {
            WriteNullable(name, value);
        }
    }


    public class ValueBox<T>
    {
        private T v;

        public ValueBox()
        {
        }

        public ValueBox(T value)
        {
            v = value;
        }

        public T Value { get { return v; } set { v = value; } }
    }


    public abstract class XmlBase
    {
        public abstract void Read(XmlReaderStack reader);
        public abstract void Write(XmlWriterStack writer);

        public static void Read(XmlGroup outer, XmlReaderStack reader)
        {
            outer.Read(reader);
        }

        public static void Write(XmlGroup outer, XmlWriterStack writer)
        {
            outer.Write(writer);
        }
    }

    public class XmlSeq : XmlBase
    {
        private readonly XmlBase[] members;

        public XmlSeq(XmlBase[] members)
        {
            this.members = members;
        }

        public XmlSeq(XmlBase member)
            : this(new XmlBase[] { member })
        {
        }

        public override void Read(XmlReaderStack reader)
        {
            for (int i = 0; i < members.Length; i++)
            {
                members[i].Read(reader);
            }
        }

        public override void Write(XmlWriterStack writer)
        {
            for (int i = 0; i < members.Length; i++)
            {
                members[i].Write(writer);
            }
        }
    }

    public class XmlGroup : XmlSeq
    {
        private readonly string name;

        public XmlGroup(string name, XmlBase[] members)
            : base(members)
        {
            this.name = name;
        }

        public XmlGroup(string name, XmlBase member)
            : this(name, new XmlBase[] { member })
        {
        }

        public override void Read(XmlReaderStack reader)
        {
            using (XmlLevel level = reader.Push(name))
            {
                base.Read(reader);
            }
        }

        public override void Write(XmlWriterStack writer)
        {
            using (XmlLevel level = writer.Push(name))
            {
                base.Write(writer);
            }
        }
    }

    public class XmlString : XmlBase
    {
        private readonly string name;
        private readonly Func<string> getter;
        private readonly Action<string> setter;
        private readonly Func<bool> emitOptional;

        public XmlString(string name, Func<string> getter, Action<string> setter)
        {
            this.name = name;
            this.getter = getter;
            this.setter = setter;
        }

        public XmlString(string name, Func<string> getter, Action<string> setter, Func<bool> emitOptional)
            : this(name, getter, setter)
        {
            this.emitOptional = emitOptional;
        }

        public override void Read(XmlReaderStack reader)
        {
            if ((emitOptional == null) || reader.Test(name))
            {
                setter(reader.ReadString(name));
            }
        }

        public override void Write(XmlWriterStack writer)
        {
            if ((emitOptional == null) || emitOptional())
            {
                writer.WriteString(name, getter());
            }
        }
    }

    public class XmlInt : XmlBase
    {
        private readonly string name;
        private readonly Func<int> getter;
        private readonly Action<int> setter;
        private readonly Func<bool> emitOptional;

        public XmlInt(string name, Func<int> getter, Action<int> setter)
        {
            this.name = name;
            this.getter = getter;
            this.setter = setter;
        }

        public XmlInt(string name, Func<int> getter, Action<int> setter, Func<bool> emitOptional)
            : this(name, getter, setter)
        {
            this.emitOptional = emitOptional;
        }

        public override void Read(XmlReaderStack reader)
        {
            if ((emitOptional == null) || reader.Test(name))
            {
                setter(reader.ReadInt(name));
            }
        }

        public override void Write(XmlWriterStack writer)
        {
            if ((emitOptional == null) || emitOptional())
            {
                writer.WriteInt(name, getter());
            }
        }
    }

    public class XmlDouble : XmlBase
    {
        private readonly string name;
        private readonly Func<double> getter;
        private readonly Action<double> setter;
        private readonly Func<bool> emitOptional;

        public XmlDouble(string name, Func<double> getter, Action<double> setter)
        {
            this.name = name;
            this.getter = getter;
            this.setter = setter;
        }

        public XmlDouble(string name, Func<double> getter, Action<double> setter, Func<bool> emitOptional)
            : this(name, getter, setter)
        {
            this.emitOptional = emitOptional;
        }

        public override void Read(XmlReaderStack reader)
        {
            if ((emitOptional == null) || reader.Test(name))
            {
                setter(reader.ReadDouble(name));
            }
        }

        public override void Write(XmlWriterStack writer)
        {
            if ((emitOptional == null) || emitOptional())
            {
                writer.WriteDouble(name, getter());
            }
        }
    }

    public class XmlBool : XmlBase
    {
        private readonly string name;
        private readonly Func<bool> getter;
        private readonly Action<bool> setter;
        private readonly Func<bool> emitOptional;

        public XmlBool(string name, Func<bool> getter, Action<bool> setter)
        {
            this.name = name;
            this.getter = getter;
            this.setter = setter;
        }

        public XmlBool(string name, Func<bool> getter, Action<bool> setter, Func<bool> emitOptional)
            : this(name, getter, setter)
        {
            this.emitOptional = emitOptional;
        }

        public override void Read(XmlReaderStack reader)
        {
            if ((emitOptional == null) || reader.Test(name))
            {
                setter(reader.ReadBool(name));
            }
        }

        public override void Write(XmlWriterStack writer)
        {
            if ((emitOptional == null) || emitOptional())
            {
                writer.WriteBool(name, getter());
            }
        }
    }

    public class XmlIf : XmlSeq
    {
        private readonly Func<XmlReaderStack, bool> readCondition;
        private readonly Func<bool> writeCondition;

        public XmlIf(Func<XmlReaderStack, bool> readCondition, Func<bool> writeCondition, XmlBase[] members)
            : base(members)
        {
            this.readCondition = readCondition;
            this.writeCondition = writeCondition;
        }

        public XmlIf(Func<XmlReaderStack, bool> readCondition, Func<bool> writeCondition, XmlBase member)
            : this(readCondition, writeCondition, new XmlBase[] { member })
        {
        }

        public override void Read(XmlReaderStack reader)
        {
            if (readCondition(reader))
            {
                base.Read(reader);
            }
        }

        public override void Write(XmlWriterStack writer)
        {
            if (writeCondition())
            {
                base.Write(writer);
            }
        }
    }

    public class XmlList<T> : XmlBase
    {
        private readonly string collectionName;
        private readonly string elementName;
        private readonly IList<T> list;
        private readonly Func<XmlReaderStack, T> createElement;
        private readonly Action<XmlWriterStack, T> writeElement;

        public XmlList(string collectionName, string elementName, IList<T> list, Func<XmlReaderStack, T> createElement, Action<XmlWriterStack, T> writeElement)
        {
            this.collectionName = collectionName;
            this.elementName = elementName;
            this.list = list;
            this.createElement = createElement;
            this.writeElement = writeElement;
        }

        public override void Read(XmlReaderStack reader)
        {
            using (XmlLevel level = reader.Push(collectionName))
            {
                while (reader.Test(elementName))
                {
                    list.Add(createElement(reader));
                }
            }
        }

        public override void Write(XmlWriterStack writer)
        {
            using (XmlLevel level = writer.Push(collectionName))
            {
                foreach (T element in list)
                {
                    writeElement(writer, element);
                }
            }
        }
    }

    public class XmlObject : XmlBase
    {
        private readonly Action<XmlReaderStack> createObject;
        private readonly Action<XmlWriterStack> writeObject;

        public XmlObject(Action<XmlReaderStack> createObject, Action<XmlWriterStack> writeObject)
        {
            this.createObject = createObject;
            this.writeObject = writeObject;
        }

        public override void Read(XmlReaderStack reader)
        {
            createObject(reader);
        }

        public override void Write(XmlWriterStack writer)
        {
            writeObject(writer);
        }
    }
}
