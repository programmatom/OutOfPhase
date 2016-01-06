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
using System.IO;
using System.Text;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    class StreamReader2 : TextReader
    {
        private readonly Stream stream;
        private readonly long streamLength;
        private long streamPosition;

        private byte[] buffer; // UTF8
        private int index;
        private int end;

        private const char LineBreak = '\r';
        private const int Size = 65536;

        public StreamReader2(Stream stream) // UTF8
        {
            this.stream = stream;
            streamLength = stream.Length;
            streamPosition = stream.Position;

            buffer = new byte[Size];
            Fill();
            if ((end >= 3) && (buffer[0] == 239) && (buffer[1] == 187) && (buffer[2] == 191))
            {
                index += 3; // skip UTF8 BOM
            }
        }

        private void Fill()
        {
            index = 0;
            end = 0;
            int c;
            while ((c = stream.Read(buffer, end, Size - end)) != 0)
            {
                end += c;
            }
            streamPosition = stream.Position;
        }

        public override string ReadLine()
        {
            if (Position == streamLength)
            {
                return null;
            }
            int lineBreakIndex = Array.IndexOf(buffer, (byte)LineBreak, index, end - index);
            if (lineBreakIndex >= 0)
            {
                int start = index;
                index = lineBreakIndex + 1;
                return Encoding.UTF8.GetString(buffer, start, lineBreakIndex - start);
            }
            else
            {
                byte[] saved = new byte[end - index];
                Array.Copy(buffer, index, saved, 0, end - index);

                Fill();

                lineBreakIndex = Array.IndexOf(buffer, (byte)LineBreak, 0, end);
                if (lineBreakIndex < 0)
                {
                    lineBreakIndex = end;
                }
                if (lineBreakIndex == Size)
                {
                    // lines longer than Size are not supported
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                byte[] composite = new byte[saved.Length + lineBreakIndex];
                Array.Copy(saved, 0, composite, 0, saved.Length);
                Array.Copy(buffer, 0, composite, saved.Length, lineBreakIndex);
                index = (lineBreakIndex != end) ? lineBreakIndex + 1 : end;
                return Encoding.UTF8.GetString(composite);
            }
        }

        public long Position
        {
            get
            {
                return stream.Position - end + index;
            }
        }

        protected override void Dispose(bool disposing)
        {
            stream.Seek(this.Position, SeekOrigin.Begin);
            base.Dispose(disposing);
        }
    }
}
