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
using System.Threading;
using System.Text;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    public class FindBoundariesHelper
    {
        public const bool DebugFindBoundaries = true; // enable to slow-check epoch and level2 counts

        private class Workspace : IDisposable
        {
            public readonly long start;
            public readonly long end;
            public readonly Stream logStream1;
            public readonly Stream offsetsStream1;
            public readonly BinaryWriter offsetsWriter1;
            public readonly BitVector level2;
            public readonly Thread thread;
            public readonly ManualResetEvent finished;

            public Workspace(long start, long end, string logPath)
            {
                this.start = start;
                this.end = end;
                this.logStream1 = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize);
                this.logStream1.Seek(start, SeekOrigin.Begin);
                this.offsetsStream1 = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, Constants.BufferSize, FileOptions.DeleteOnClose);
                this.offsetsWriter1 = new BinaryWriter(this.offsetsStream1, Encoding.UTF8);
                this.level2 = new BitVector();
                this.finished = new ManualResetEvent(false);
                this.thread = new Thread(new ThreadStart(ThreadMain));
                this.thread.Start();
            }

            private void ThreadMain()
            {
                int epochCount = 0;
                int l2EpochCount = 0;
                const int ChunkSize = 4096;
                const int Overlap = 3;
                byte[] buffer = new byte[ChunkSize + Overlap];
                int c = -1;
                while (true)
                {
                    int o = c == -1 ? 0 : Overlap;
                    int a = (int)Math.Min(end - logStream1.Position, buffer.Length - o);
                    if (a == 0)
                    {
                        break;
                    }
                    while ((a != 0) && (c = logStream1.Read(buffer, o, a)) != 0)
                    {
                        o += c;
                        a -= c;
                    }

                    int i = 0;
                    while (i < Math.Min(o, ChunkSize + 1/*extra lf*/) - 1)
                    {
                        i = Array.IndexOf(buffer, (byte)'\r', i, Math.Min(o, ChunkSize + 1/*extra lf*/) - i - 1);
                        if (i < 0)
                        {
                            break;
                        }
                        if (buffer[i + 1] == (byte)'\r')
                        {
                            EpochVirtualList.AppendEpochAcceleratorRecord(
                                offsetsWriter1,
                                logStream1.Position - o + i + 2/*first char after break*/);
                            if (i + 3 < o)
                            {
                                bool l2 = buffer[i + 2] == 'l' && buffer[i + 3] == '2';
                                level2[epochCount] = l2;
                                epochCount++;
                                if (l2)
                                {
                                    l2EpochCount++;
                                }
                            }
                            i++;
                        }
                        i++;
                    }

                    Array.Copy(buffer, o - Overlap, buffer, 0, Overlap);
                }

                finished.Set();
            }

            public void Dispose()
            {
                logStream1.Dispose();
                offsetsStream1.Dispose();
                finished.Close();
            }
        }

        // Finds double line break boundaries (i.e. empty line between records). "\r" newline only!
        // logStream - stream to find boundaries, seeked to start of record area
        // offsetsStream - offsets written as 8-byte ints in order
        public static void FindBoundaries(string logPath, Stream logStream, Stream offsetsStream, Stream level2Stream)
        {
            long start = logStream.Position - 2; // caller consumed boundary - restore it for search
            long length = logStream.Length - start;

            int procs = 1;//Environment.ProcessorCount;
            Workspace[] workspaces = new Workspace[procs];
            WaitHandle[] finished = new WaitHandle[procs];
            for (int i = 0; i < procs; i++)
            {
                long start0 = start + ((i + 0) * length) / procs;
                long start1 = start + ((i + 1) * length) / procs;
                Debug.Assert((i < procs - 1) || (start1 == start + length));
                workspaces[i] = new Workspace(start0, start1, logPath);
                finished[i] = workspaces[i].finished;
            }
#if false // Not supported on STA thread
            WaitHandle.WaitAll(finished);
#else
            for (int i = 0; i < procs; i++)
            {
                finished[i].WaitOne();
            }
#endif

            BitVector mergedLevel2 = new BitVector();
            int mergedLevel2Index = 0;
            byte[] buffer = new byte[Constants.BufferSize];
            int l2EpochCount = 0;
            for (int i = 0; i < procs; i++)
            {
                workspaces[i].offsetsStream1.Seek(0, SeekOrigin.Begin);
                int c = -1;
                while (c != 0)
                {
                    int o = 0;
                    while ((buffer.Length - o != 0) && ((c = workspaces[i].offsetsStream1.Read(buffer, o, buffer.Length - o)) != 0))
                    {
                        o += c;
                    }
                    offsetsStream.Write(buffer, 0, o);
                }
                BitVector workspaceLevel2 = workspaces[i].level2;
                for (int j = 0; j < workspaceLevel2.Count; j++)
                {
                    bool l2 = workspaceLevel2[j];
                    mergedLevel2[mergedLevel2Index++] = l2;
                    if (l2)
                    {
                        l2EpochCount++;
                    }
                }
            }
            offsetsStream.Seek(0, SeekOrigin.Begin);
            offsetsStream.SetLength(offsetsStream.Length - EpochVirtualList.OffsetRecordLength); // remove terminating boundary
            Debug.Assert(offsetsStream.Length % EpochVirtualList.OffsetRecordLength == 0);
            int epochCount = (int)(offsetsStream.Length / EpochVirtualList.OffsetRecordLength);
            mergedLevel2.Save(level2Stream);

            for (int i = 0; i < procs; i++)
            {
                workspaces[i].Dispose();
            }

            if (DebugFindBoundaries)
            {
                int debugl2EpochCount = 0, debugEpochCount = 0;
                using (Stream logStreamTest = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
                {
                    using (TextReader reader = new StreamReader(logPath, Encoding.UTF8))
                    {
                        // skip header
                        string line;
                        while (!String.IsNullOrEmpty(line = reader.ReadLine()))
                        {
                        }

                        // epochs
                        while ((line = reader.ReadLine()) != null)
                        {
                            debugEpochCount++;
                            if (String.Equals(line, "l2"))
                            {
                                debugl2EpochCount++;
                            }
                            while (!String.IsNullOrEmpty(line = reader.ReadLine()))
                            {
                            }
                        }
                    }
                }
                Debug.Assert(debugEpochCount == epochCount);
                Debug.Assert(debugl2EpochCount == l2EpochCount);
            }
        }
    }
}
