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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace OutOfPhase
{
    public class BinaryReader : IDisposable
    {
        private Stream stream;

        public BinaryReader(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream = null;
        }

        public string ReadFixedStringASCII(int c)
        {
            byte[] b = new byte[c];
            if (b.Length != stream.Read(b, 0, b.Length))
            {
                throw new InvalidDataException();
            }
            return Encoding.ASCII.GetString(b);
        }

        public byte ReadByte()
        {
            byte[] b = new byte[1];
            if (1 != stream.Read(b, 0, 1))
            {
                throw new InvalidDataException();
            }
            return b[0];
        }

        public byte[] ReadBytes(int count)
        {
            byte[] b = new byte[count];
            if (count != stream.Read(b, 0, count))
            {
                throw new InvalidDataException();
            }
            return b;
        }

        public int ReadInt32() // little-endian
        {
            byte[] b = new byte[4];
            if (4 != stream.Read(b, 0, 4))
            {
                throw new InvalidDataException();
            }
            return b[0] | ((int)b[1] << 8) | ((int)b[2] << 16) | ((int)b[3] << 24);
        }

        public uint ReadUInt32()
        {
            int i = ReadInt32();
            return unchecked((uint)i);
        }

        public int ReadInt32BigEndian()
        {
            byte[] b = new byte[4];
            if (4 != stream.Read(b, 0, 4))
            {
                throw new InvalidDataException();
            }
            return b[3] | ((int)b[2] << 8) | ((int)b[1] << 16) | ((int)b[0] << 24);
        }

        public uint ReadUInt32BigEndian()
        {
            int i = ReadInt32BigEndian();
            return unchecked((uint)i);
        }

        public int ReadInt24() // little-endian
        {
            byte[] b = new byte[3];
            if (3 != stream.Read(b, 0, 3))
            {
                throw new InvalidDataException();
            }
            return b[0] | ((int)b[1] << 8) | ((int)(sbyte)b[2] << 16);
        }

        public int ReadInt24BigEndian()
        {
            byte[] b = new byte[3];
            if (3 != stream.Read(b, 0, 3))
            {
                throw new InvalidDataException();
            }
            return b[2] | ((int)b[1] << 8) | ((int)(sbyte)b[0] << 16);
        }

        public short ReadInt16() // little-endian
        {
            byte[] b = new byte[2];
            if (2 != stream.Read(b, 0, 2))
            {
                throw new InvalidDataException();
            }
            return (short)((int)b[0] | ((int)b[1] << 8));
        }

        public short[] ReadInt16s(int count) // little-endian
        {
            short[] s = new short[count];
            byte[] b = new byte[2 * count];
            if (2 * count != stream.Read(b, 0, 2 * count))
            {
                throw new InvalidDataException();
            }
            for (int i = 0; i < count; i++)
            {
                s[i] = (short)((int)b[2 * i + 0] | ((int)b[2 * i + 1] << 8));
            }
            return s;
        }

        public short ReadInt16BigEndian()
        {
            byte[] b = new byte[2];
            if (2 != stream.Read(b, 0, 2))
            {
                throw new InvalidDataException();
            }
            return (short)((int)b[1] | ((int)b[0] << 8));
        }

        public ushort ReadUInt16BigEndian()
        {
            short s = ReadInt16BigEndian();
            return unchecked((ushort)s);
        }

        // Deprecated - for loading old file versions only
        public string ReadString4Ansi(string storageNewLine)
        {
            int c = ReadInt32();
            byte[] b = new byte[c];
            if (b.Length != stream.Read(b, 0, b.Length))
            {
                throw new InvalidDataException();
            }
            char[] cc = new char[b.Length];
            for (int i = 0; i < cc.Length; i++)
            {
                cc[i] = (char)b[i];
            }
            return new String(cc).Replace(storageNewLine, Environment.NewLine);
        }

        // Deprecated - for loading old file versions only
        public string ReadString4Ansi() // storage format newline = 0x0a
        {
            return ReadString4Ansi("\n");
        }

        public string ReadString4Utf8(string storageNewLine)
        {
            int c = ReadInt32();
            byte[] b = new byte[c];
            if (b.Length != stream.Read(b, 0, b.Length))
            {
                throw new InvalidDataException();
            }
            return Encoding.UTF8.GetString(b).Replace(storageNewLine, Environment.NewLine);
        }

        public string ReadString4Utf8() // storage format newline = 0x0a
        {
            return ReadString4Utf8("\n");
        }

        public SmallBCDType ReadSBCD()
        {
            return SmallBCDType.FromRawInt16(ReadInt16());
        }

        public LargeBCDType ReadLBCD()
        {
            return LargeBCDType.FromRawInt32(ReadInt32());
        }

        public SmallExtBCDType ReadSXBCD()
        {
            return SmallExtBCDType.FromRawInt32(ReadInt32());
        }

        public uint ReadUInt32Delta()
        {
            byte[] buf = new byte[1];
            uint ui = 0;
            int c = 0;
            byte b;
            do
            {
                if (1 != stream.Read(buf, 0, 1))
                {
                    throw new InvalidDataException();
                }
                b = buf[0];

                /* treat overflow as an error */
                if ((((ui << 7) & 0xffffffff) >> 7) != ui)
                {
                    throw new InvalidDataException();
                }

                ui = (ui << 7) | ((uint)b & 0x7f);

                /* treat too many elements as an error */
                c += 1;
                if (c > 5)
                {
                    throw new InvalidDataException();
                }
            } while ((b & 0x80) != 0);
            return ui;
        }

        public void ReadRaw(byte[] buffer, int offset, int count)
        {
            int r = stream.Read(buffer, offset, count);
            if (r != count)
            {
                throw new InvalidDataException();
            }
        }

        public void ReadEOF()
        {
            if (stream.Length != stream.Position)
            {
                throw new InvalidDataException();
            }
        }
    }

    public class BinaryWriter : IDisposable
    {
        private Stream stream;

        public BinaryWriter(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream = null;
        }

        public void WriteFixedStringASCII(int c, string s)
        {
            if (c != s.Length)
            {
                throw new ArgumentException();
            }
            byte[] b = Encoding.ASCII.GetBytes(s);
            if (c != b.Length)
            {
                throw new ArgumentException();
            }
            stream.Write(b, 0, c);
        }

        public void WriteByte(byte v)
        {
            byte[] b = new byte[1];
            b[0] = v;
            stream.Write(b, 0, 1);
        }

        public void WriteInt32(int i) // little-endian
        {
            byte[] b = new byte[4];
            b[0] = (byte)(i & 0xff);
            b[1] = (byte)((i >> 8) & 0xff);
            b[2] = (byte)((i >> 16) & 0xff);
            b[3] = (byte)((i >> 24) & 0xff);
            stream.Write(b, 0, 4);
        }

        public void WriteUInt32(uint u)
        {
            WriteInt32(unchecked((int)u));
        }

        public void WriteInt32BigEndian(int i)
        {
            byte[] b = new byte[4];
            b[3] = (byte)(i & 0xff);
            b[2] = (byte)((i >> 8) & 0xff);
            b[1] = (byte)((i >> 16) & 0xff);
            b[0] = (byte)((i >> 24) & 0xff);
            stream.Write(b, 0, 4);
        }

        public void WriteUInt32BigEndian(uint u)
        {
            WriteInt32BigEndian(unchecked((int)u));
        }

        public void WriteInt24(int i) // little-endian
        {
            byte[] b = new byte[3];
            b[0] = (byte)(i & 0xff);
            b[1] = (byte)((i >> 8) & 0xff);
            b[2] = (byte)((i >> 16) & 0xff);
            stream.Write(b, 0, 3);
        }

        public void WriteInt24BigEndian(int i)
        {
            byte[] b = new byte[3];
            b[2] = (byte)(i & 0xff);
            b[1] = (byte)((i >> 8) & 0xff);
            b[0] = (byte)((i >> 16) & 0xff);
            stream.Write(b, 0, 3);
        }

        public void WriteInt16(short i) // little-endian
        {
            byte[] b = new byte[2];
            b[0] = (byte)(i & 0xff);
            b[1] = (byte)((i >> 8) & 0xff);
            stream.Write(b, 0, 2);
        }

        public void WriteInt16s(short[] s, int offset, int count) // little-endian
        {
            byte[] b = new byte[2 * count];
            for (int j = 0; j < count; j++)
            {
                int i = s[j];
                b[2 * j + 0] = (byte)(i & 0xff);
                b[2 * j + 1] = (byte)((i >> 8) & 0xff);
            }
            stream.Write(b, 0, 2 * count);
        }

        public void WriteInt16BigEndian(short i)
        {
            byte[] b = new byte[2];
            b[1] = (byte)(i & 0xff);
            b[0] = (byte)((i >> 8) & 0xff);
            stream.Write(b, 0, 2);
        }

        // Deprecated - use WriteString4Utf8
        public void WriteString4Ansi(string s, string storageNewLine)
        {
            if (s == null)
            {
                s = String.Empty;
            }
            char[] c = s.Replace(Environment.NewLine, storageNewLine).ToCharArray();
            byte[] b = new byte[c.Length];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = unchecked((byte)c[i]);
            }
            WriteInt32(b.Length);
            stream.Write(b, 0, b.Length);
        }

        // Deprecated - use WriteString4Utf8
        public void WriteString4Ansi(string s) // storage format newline = 0x0a
        {
            WriteString4Ansi(s, "\n");
        }

        public void WriteString4Utf8(string s, string storageNewLine)
        {
            if (s == null)
            {
                s = String.Empty;
            }
            byte[] b = Encoding.UTF8.GetBytes(s.Replace(Environment.NewLine, storageNewLine));
            WriteInt32(b.Length);
            stream.Write(b, 0, b.Length);
        }

        public void WriteString4Utf8(string s) // storage format newline = 0x0a
        {
            WriteString4Utf8(s, "\n");
        }

        public void WriteSBCD(SmallBCDType sbcd)
        {
            WriteInt16(sbcd.rawInt16);
        }

        public void WriteLBCD(LargeBCDType lbcd)
        {
            WriteInt32(lbcd.rawInt32);
        }

        public void ReadSXBCD(SmallExtBCDType sxbcd)
        {
            WriteInt32(sxbcd.rawInt32);
        }

        public void WriteUInt32Delta(uint ui)
        {
            byte[] b = new byte[5];
            int c = 0;
            do
            {
                b[4 - c] = (byte)((ui & 0x7f) | 0x80);
                ui = ui >> 7;
                c += 1;
                Debug.Assert(c <= 5);
            } while (ui != 0);
            b[4] &= 0x7f; /* clear high bit on last one */
            stream.Write(b, 4 - (c - 1), c);
        }

        public void WriteRaw(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }
    }
}
