/*
 *  Copyright © 2015-2016 Thomas R. Lawrence
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
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    public class BitVector
    {
        private int count;
        private uint[] u = new uint[0];

        public BitVector()
        {
        }

        public BitVector(int count)
        {
            this.count = count;
            this.u = new uint[(count + 31) / 32];
        }

        public bool this[int index]
        {
            get
            {
                if (unchecked((uint)index >= (uint)count))
                {
                    return false;
                }
                return (u[index / 32] & (1 << (index & 31))) != 0;
            }
            set
            {
                while (unchecked((uint)index >= (uint)(u.Length * 32)))
                {
                    Array.Resize(ref u, Math.Max(u.Length * 2, 1));
                }
                count = Math.Max(index + 1, count);
                if (value)
                {
                    u[index / 32] |= 1U << (index & 31);
                }
                else
                {
                    u[index / 32] &= ~(1U << (index & 31));
                }
            }
        }

        public int Count { get { return count; } }

        public unsafe void Save(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((int)count);
            int elements = (count + 31) / 32;
            for (int i = 0; i < elements; i++)
            {
                writer.Write((uint)u[i]);
            }
        }

        public static BitVector Load(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            int count = reader.ReadInt32();
            BitVector vector = new BitVector(count);
            int elements = (count + 31) / 32;
            for (int i = 0; i < elements; i++)
            {
                vector.u[i] = reader.ReadUInt32();
            }
            return vector;
        }
    }
}
