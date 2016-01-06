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
    public static class Constants
    {
        private const int MaxSmallObjectHeapObjectSize = 85000; // http://msdn.microsoft.com/en-us/magazine/cc534993.aspx, http://blogs.msdn.com/b/dotnet/archive/2011/10/04/large-object-heap-improvements-in-net-4-5.aspx
        private const int PageSize = 4096;
        private const int MaxSmallObjectPageDivisibleSize = MaxSmallObjectHeapObjectSize & ~(PageSize - 1);

        public const int BufferSize = MaxSmallObjectPageDivisibleSize;
    }

    public class TopProxy
    {
        private Top top;

        public Top Top { get { return top; } set { top = value; } }
    }

    public class Top : IDisposable
    {
        public const int AcceleratorVersion = 1;

        private readonly bool level2;
        private readonly long timerBasis;
        private readonly int samplingRate;
        private readonly int envelopeRate;
        private readonly int concurrency;
        private readonly IList<Definition> definitions;
        private readonly Stream logStream;
        private readonly Stream acceleratorStream;
        private readonly BinaryReader acceleratorReader;
        private readonly IList<Epoch> epochs;

        public const int EpochCacheLimit = 200;
        private readonly Dictionary<long, EpochResident> epochCache = new Dictionary<long, EpochResident>();

        public Top(
            Stream logStream,
            bool level2,
            long timerBasis,
            int samplingRate,
            int envelopeRate,
            int concurrency,
            IList<Definition> definitions,
            Stream acceleratorStream,
            BinaryReader acceleratorReader,
            IList<Epoch> epochs)
        {
            this.logStream = logStream;
            this.level2 = level2;
            this.timerBasis = timerBasis;
            this.samplingRate = samplingRate;
            this.envelopeRate = envelopeRate;
            this.concurrency = concurrency;
            this.definitions = definitions;
            this.acceleratorStream = acceleratorStream;
            this.acceleratorReader = acceleratorReader;
            this.epochs = epochs;
        }

        public void Dispose()
        {
            acceleratorReader.Close();
            acceleratorStream.Dispose();
            logStream.Dispose();
        }

        public bool Level2 { get { return level2; } }
        public long TimerBasis { get { return timerBasis; } }
        public int SamplingRate { get { return samplingRate; } }
        public int EnvelopeRate { get { return envelopeRate; } }
        public int Concurrency { get { return concurrency; } }
        public IList<Definition> Definitions { get { return definitions; } }
        public IList<Epoch> Epochs { get { return epochs; } }
        public Stream LogStream { get { return logStream; } }
        public Stream AcceleratorStream { get { return acceleratorStream; } }
        public BinaryReader AcceleratorReader { get { return acceleratorReader; } }

        public EpochResident QueryCache(long offset)
        {
            EpochResident epoch;
            epochCache.TryGetValue(offset, out epoch);
            return epoch; // could be null
        }

        public void AddCache(long offset, EpochResident epoch)
        {
            if (epochCache.Count >= EpochCacheLimit)
            {
                epochCache.Clear();
            }
            epochCache[offset] = epoch;
        }

        public static Top Read(string logPath, Stream logStream)
        {
            TopProxy topProxy = new TopProxy();

            List<Definition> definitions = new List<Definition>();

            bool level2;
            long timerBasis;
            int samplingRate;
            int envelopeRate;
            int concurrency;

            string acceleratorPath = Accelerators.Schedule.QueryAcceleratorPath(logPath, logStream, AcceleratorVersion);
            string level2AcceleratorPath = Accelerators.Events.QueryAcceleratorPath(logPath, logStream, AcceleratorVersion);
            using (TextReader reader = new StreamReader2(logStream))
            {
                // always read header from log file

                string line;
                string[] parts;

                line = reader.ReadLine();
                parts = line.Split('\t');
                Debug.Assert(parts[0] == "version");
                int version = Int32.Parse(parts[1]);
                Debug.Assert(version == 1);

                line = reader.ReadLine();
                parts = line.Split('\t');
                Debug.Assert(parts[0] == "level");
                level2 = Int64.Parse(parts[1]) > 1;

                line = reader.ReadLine();
                parts = line.Split('\t');
                Debug.Assert(parts[0] == "tres");
                timerBasis = Int64.Parse(parts[1]);

                line = reader.ReadLine();
                parts = line.Split('\t');
                Debug.Assert(parts[0] == "srate");
                samplingRate = Int32.Parse(parts[1]);

                line = reader.ReadLine();
                parts = line.Split('\t');
                Debug.Assert(parts[0] == "erate");
                envelopeRate = Int32.Parse(parts[1]);

                line = reader.ReadLine();
                parts = line.Split('\t');
                Debug.Assert(parts[0] == "threads");
                concurrency = Int32.Parse(parts[1]);

                line = reader.ReadLine();
                if (!String.Equals(line, ":"))
                {
                    Debug.Assert(false);
                }

                while (!String.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    parts = line.Split('\t');
                    switch (parts[1])
                    {
                        default:
                            Debug.Assert(false);
                            throw new ArgumentException();
                        case "section":
                            definitions.Add(
                                new Definition(
                                    topProxy,
                                    Int32.Parse(parts[0]),
                                    parts[2],
                                    Kind.Effect,
                                    -1));
                            break;
                        case "track":
                            definitions.Add(
                                new Definition(
                                    topProxy,
                                    Int32.Parse(parts[0]),
                                    parts[3],
                                    Kind.Track,
                                    Int32.Parse(parts[2])));
                            break;
                    }
                }
            }

            // create offsets table for epoch records

            if (acceleratorPath == null)
            {
                acceleratorPath = Path.GetTempFileName();
                level2AcceleratorPath = Path.GetTempFileName();

#if false
                long end = stream.Length;
                using (Stream acceleratorStream = new FileStream(acceleratorPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, Constants.BufferSize))
                {
                    using (BinaryWriter acceleratorWriter = new BinaryWriter(acceleratorStream, Encoding.UTF8))
                    {
                        while (((StreamReader2)reader).Position < end)
                        {
                            acceleratorWriter.Write((long)((StreamReader2)reader).Position);
                            EpochResident.Read(reader, topProxy, definitions.Count);
                        }
                    }
                }
#else
                using (Stream acceleratorStream = new FileStream(acceleratorPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, Constants.BufferSize))
                {
                    using (Stream level2Stream = new FileStream(level2AcceleratorPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, Constants.BufferSize))
                    {
                        FindBoundariesHelper.FindBoundaries(logPath, logStream, acceleratorStream, level2Stream);
                    }
                }
#endif

                Accelerators.Schedule.RecordAcceleratorPath(logPath, logStream, acceleratorPath, AcceleratorVersion);
                Accelerators.Events.RecordAcceleratorPath(logPath, logStream, level2AcceleratorPath, AcceleratorVersion);
            }

            Stream acceleratorStream2 = new FileStream(acceleratorPath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize);
            BinaryReader acceleratorReader2 = new BinaryReader(acceleratorStream2, Encoding.UTF8);
            BitVector level2Bits;
            using (Stream level2Stream = new FileStream(level2AcceleratorPath, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize))
            {
                level2Bits = BitVector.Load(level2Stream);
            }

            IList<Epoch> epochs = new EpochVirtualList(
                topProxy,
                (int)(acceleratorStream2.Length / EpochVirtualList.OffsetRecordLength),
                level2Bits);
            //System.Windows.Forms.MessageBox.Show("Number of epochs: " + epochs.Count.ToString());

            Top top = new Top(
                logStream,
                level2,
                timerBasis,
                samplingRate,
                envelopeRate,
                concurrency,
                definitions,
                acceleratorStream2,
                acceleratorReader2,
                epochs);

            topProxy.Top = top;

            return top;
        }
    }

    public enum Kind
    {
        Track,
        Effect,
    }

    public class Definition
    {
        private readonly TopProxy topProxy;
        private readonly int id;
        private readonly string name;
        private readonly bool quoted;
        private readonly Kind kind;
        private readonly int sectionId; // valid only for Kind.Track

        public Definition(
            TopProxy topProxy,
            int id,
            string name,
            Kind kind,
            int sectionId)
        {
            this.topProxy = topProxy;
            if ((name.Length >= 2) && name.StartsWith("\"") && name.EndsWith("\""))
            {
                name = name.Substring(1, name.Length - 2);
                this.quoted = true;
            }
            this.id = id;
            this.name = name;
            this.kind = kind;
            this.sectionId = sectionId;
        }

        public int Id { get { return id; } }
        public string Name { get { return quoted ? String.Concat("\"", name, "\"") : name; } }
        public string RawName { get { return name; } }
        public Kind Kind { get { return kind; } }
        public string KindAsString { get { return kind.ToString(); } }
        public int SectionId { get { return sectionId; } }
        public string SectionIdAsString { get { return kind == Kind.Track ? topProxy.Top.Definitions[sectionId].Name : null; } }

        public int[] Inputs
        {
            get
            {
                List<int> inputs = new List<int>();
                for (int i = 0; i < topProxy.Top.Definitions.Count; i++)
                {
                    if (topProxy.Top.Definitions[i].sectionId == this.id)
                    {
                        inputs.Add(topProxy.Top.Definitions[i].id);
                    }
                }
                return inputs.ToArray();
            }
        }
        public string InputsAsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                bool c = false;
                foreach (int id in Inputs)
                {
                    if (c)
                    {
                        sb.Append(", ");
                    }
                    c = true;
                    sb.Append(id);
                }
                return sb.ToString();
            }
        }

        public void WriteAccelerator(BinaryWriter writer)
        {
            writer.Write((int)id);
            writer.Write((string)Name);
            writer.Write((int)kind);
            writer.Write((int)sectionId);
        }

        public static Definition ReadAccelerator(BinaryReader reader, TopProxy topProxy)
        {
            int id = reader.ReadInt32();
            string name = reader.ReadString();
            Kind kind = (Kind)reader.ReadInt32();
            int sectionId = reader.ReadInt32();
            return new Definition(topProxy, id, name, kind, sectionId);
        }
    }

    public class DatumEvent
    {
        private readonly int seq;
        private readonly string tag;

        public DatumEvent(string[] parts)
        {
            seq = Int32.Parse(parts[1]);
            tag = parts[2];
        }

        public int Seq { get { return seq; } }
        public string Tag { get { return tag; } }
    }

    public class DatumEvent2 : DatumEvent
    {
        public readonly int frameIndex;
        public readonly int noteIndex;

        public DatumEvent2(string[] parts)
            : base(parts)
        {
            frameIndex = Int32.Parse(parts[3]);
            noteIndex = Int32.Parse(parts[4]);
        }
    }


    public class Datum
    {
        private readonly int id;
        private readonly int processor;
        private readonly long start;
        private readonly long end;
        private readonly DatumEvent[] events;
        private readonly int denormals;

        public Datum(
            int id,
            int processor,
            long start,
            long end,
            DatumEvent[] events,
            int denormals)
        {
            this.id = id;
            this.processor = processor;
            this.start = start;
            this.end = end;
            this.events = events;
            this.denormals = denormals;
        }

        public int Id { get { return id; } }
        public int Processor { get { return processor; } }
        public long Start { get { return start; } }
        public long End { get { return end; } }
        public DatumEvent[] Events { get { return events; } }
        public int Denormals { get { return denormals; } }

        public static Datum[] ReadArray(TextReader reader, int count, long basis, bool level2)
        {
            Datum[] data = new Datum[count];
            for (int c = 0; c < count; c++)
            {
                string line = reader.ReadLine();
                string[] parts = line.Split('\t');
                int id = Int32.Parse(parts[0]);
                int processor = Int32.Parse(parts[1]);
                long start = Int64.Parse(parts[2]);
                long end = Int64.Parse(parts[3]);
                DatumEvent[] events = null;
                int denormals = 0;
                if (level2)
                {
                    line = reader.ReadLine();
                    parts = line.Split('\t');
                    Debug.Assert(String.Equals(parts[1], "dn"));
                    denormals = Int32.Parse(parts[2]);

                    line = reader.ReadLine();
                    parts = line.Split('\t');
                    int eventCount = Int32.Parse(parts[1]);
                    events = new DatumEvent[eventCount];
                    for (int e = 0; e < eventCount; e++)
                    {
                        line = reader.ReadLine();
                        parts = line.Split('\t');
                        events[e] = parts.Length > 3 ? new DatumEvent2(parts) : new DatumEvent(parts);
                    }
                }
                else
                {
                    events = new DatumEvent[0];
                }
                data[id] = new Datum(
                    id,
                    processor,
                    start + basis,
                    end + basis,
                    events,
                    denormals);
            }
            return data;
        }
    }

    public class EpochVirtualList : IList<Epoch>
    {
        private readonly TopProxy topProxy;

        private readonly int count;
        private readonly BitVector level2;

        public const int OffsetRecordLength = 8;

        public static void ReadEpochAcceleratorRecord(
            int index,
            Stream stream,
            BinaryReader reader,
            out long offset)
        {
            stream.Seek((long)index * OffsetRecordLength, SeekOrigin.Begin);
            offset = reader.ReadInt64();
        }

        public static void AppendEpochAcceleratorRecord(
            BinaryWriter writer,
            long offset)
        {
            writer.Write((long)offset);
        }

        public EpochVirtualList(
            TopProxy topProxy,
            int count,
            BitVector level2)
        {
            this.topProxy = topProxy;

            this.count = count;
            this.level2 = level2;
        }

        public int IndexOf(Epoch item)
        {
            Debugger.Break();
            throw new Exception("The method or operation is not implemented.");
        }

        public void Insert(int index, Epoch item)
        {
            Debugger.Break();
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            Debugger.Break();
            throw new NotSupportedException();
        }

        public Epoch this[int index]
        {
            get
            {
                if (unchecked((uint)index >= (uint)count))
                {
                    throw new ArgumentOutOfRangeException();
                }
                long offset;
                ReadEpochAcceleratorRecord(
                    index,
                    topProxy.Top.AcceleratorReader.BaseStream,
                    topProxy.Top.AcceleratorReader,
                    out offset);
                return new EpochSecondaryStorage(topProxy, offset, level2[index]);
            }
            set
            {
                Debugger.Break();
                throw new NotSupportedException();
            }
        }

        public void Add(Epoch item)
        {
            Debugger.Break();
            throw new NotSupportedException();
        }

        public void Clear()
        {
            Debugger.Break();
            throw new NotSupportedException();
        }

        public bool Contains(Epoch item)
        {
            Debugger.Break();
            throw new Exception("The method or operation is not implemented.");
        }

        public void CopyTo(Epoch[] array, int arrayIndex)
        {
            Debugger.Break();
            throw new Exception("The method or operation is not implemented.");
        }

        public int Count { get { return count; } }

        public bool IsReadOnly { get { return true; } }

        public bool Remove(Epoch item)
        {
            Debugger.Break();
            throw new NotSupportedException();
        }

        public IEnumerator<Epoch> GetEnumerator()
        {
            return new EpochVirtualListEnumerator(topProxy, count, level2);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new EpochVirtualListEnumerator(topProxy, count, level2);
        }

        private class EpochVirtualListEnumerator : IEnumerator<Epoch>, System.Collections.IEnumerator
        {
            private readonly TopProxy topProxy;
            private readonly int count;
            private readonly BitVector level2;
            private int index = -1;

            public EpochVirtualListEnumerator(TopProxy topProxy, int count, BitVector level2)
            {
                this.topProxy = topProxy;
                this.count = count;
                this.level2 = level2;
            }

            private Epoch GetCurrent()
            {
                if ((index < 0) || (index > count))
                {
                    throw new InvalidOperationException();
                }
                if (index == count)
                {
                    return null;
                }
                long offset;
                ReadEpochAcceleratorRecord(
                    index,
                    topProxy.Top.AcceleratorReader.BaseStream,
                    topProxy.Top.AcceleratorReader,
                    out offset);
                return new EpochSecondaryStorage(topProxy, offset, level2[index]);
            }

            public Epoch Current
            {
                get
                {
                    return GetCurrent();
                }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return GetCurrent();
                }
            }

            private bool MoveNextInternal()
            {
                if (index == count)
                {
                    return false;
                }
                index++;
                return (index < count);
            }

            public bool MoveNext()
            {
                return MoveNextInternal();
            }

            private void ResetInternal()
            {
                index = -1;
            }

            public void Reset()
            {
                ResetInternal();
            }

            bool System.Collections.IEnumerator.MoveNext()
            {
                return MoveNextInternal();
            }

            void System.Collections.IEnumerator.Reset()
            {
                ResetInternal();
            }
        }
    }

    public static class BinarySearchHelper
    {
        public delegate int Comparer<K, T>(T l, K r);
        public static int BinarySearch<K, T>(IList<T> list, int index, int length, K value, Comparer<K, T> comparer)
        {
            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer(list[i], value);

                if (order == 0) return i;
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }
    }

    public abstract class Epoch
    {
        public abstract long EnvelopeTick { get; }
        public abstract long FrameBase { get; }
        public abstract decimal FrameBaseTime { get; }
        public abstract int FrameCount { get; }
        public abstract long Phase0Start { get; }
        public abstract long Phase1Start { get; }
        public abstract long Phase2Start { get; }
        public abstract long Phase3Start { get; }
        public abstract decimal Duration { get; }
        public abstract bool Level2 { get; }

        public abstract Datum[] Data { get; }
        public abstract int TotalDenormals { get; }
    }

    public class EpochSecondaryStorage : Epoch
    {
        private readonly TopProxy topProxy;
        private readonly long offset;
        private readonly bool level2;

        public EpochSecondaryStorage(TopProxy topProxy, long offset, bool level2)
        {
            this.topProxy = topProxy;
            this.offset = offset;
            this.level2 = level2;
        }

        public EpochResident Resident
        {
            get
            {
                EpochResident epoch = topProxy.Top.QueryCache(offset);
                if (epoch == null)
                {
                    topProxy.Top.LogStream.Seek(offset, SeekOrigin.Begin);
                    using (TextReader reader = new StreamReader2(topProxy.Top.LogStream))
                    {
                        epoch = EpochResident.Read(reader, topProxy, topProxy.Top.Definitions.Count, level2);
                    }
                    topProxy.Top.AddCache(offset, epoch);
                }
                return epoch;
            }
        }

        public override long EnvelopeTick { get { return Resident.EnvelopeTick; } }
        public override long FrameBase { get { return Resident.FrameBase; } }
        public override decimal FrameBaseTime { get { return Resident.FrameBaseTime; } }
        public override int FrameCount { get { return Resident.FrameCount; } }
        public override long Phase0Start { get { return Resident.Phase0Start; } }
        public override long Phase1Start { get { return Resident.Phase1Start; } }
        public override long Phase2Start { get { return Resident.Phase2Start; } }
        public override long Phase3Start { get { return Resident.Phase3Start; } }
        public override decimal Duration { get { return Resident.Duration; } }
        public override bool Level2 { get { return level2; } }

        public override Datum[] Data { get { return Resident.Data; } }

        public override int TotalDenormals { get { return Resident.TotalDenormals; } }
    }

    public class EpochResident : Epoch
    {
        private readonly TopProxy topProxy;

        private readonly long envelopeTick;
        private readonly long frameBase;
        private readonly int frameCount;

        private readonly long basis;

        private readonly int phase0Start;
        private readonly int phase1Start;
        private readonly int phase2Start;
        private readonly int phase3Start;

        private readonly bool level2;

        private readonly Datum[] data;

        public EpochResident(
            TopProxy topProxy,
            long envelopeTick,
            long frameBase,
            int frameCount,
            long basis,
            long phase0Start,
            long phase1Start,
            long phase2Start,
            long phase3Start,
            bool level2,
            Datum[] data)
        {
            this.topProxy = topProxy;
            this.envelopeTick = envelopeTick;
            this.frameBase = frameBase;
            this.frameCount = frameCount;
            this.phase0Start = (int)(phase0Start - basis);
            this.phase1Start = (int)(phase1Start - basis);
            this.phase2Start = (int)(phase2Start - basis);
            this.phase3Start = (int)(phase3Start - basis);
            this.basis = basis;
            this.level2 = level2;
            this.data = data;
        }

        public override long EnvelopeTick { get { return envelopeTick; } }
        public override long FrameBase { get { return frameBase; } }
        public override decimal FrameBaseTime { get { return (decimal)frameBase / (decimal)topProxy.Top.SamplingRate; } }
        public override int FrameCount { get { return frameCount; } }
        public override long Phase0Start { get { return phase0Start + basis; } }
        public override long Phase1Start { get { return phase1Start + basis; } }
        public override long Phase2Start { get { return phase2Start + basis; } }
        public override long Phase3Start { get { return phase3Start + basis; } }
        public override decimal Duration { get { return (decimal)(phase3Start - phase0Start) / topProxy.Top.TimerBasis * 1000; } }
        public override bool Level2 { get { return level2; } }

        public override Datum[] Data { get { return data; } }

        public override int TotalDenormals
        {
            get
            {
                int totalDenormals = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    totalDenormals += data[i].Denormals;
                }
                return totalDenormals;
            }
        }

        public static EpochResident Read(TextReader reader, TopProxy topProxy, int count, bool level2)
        {
            string line;
            string[] parts;

            if (level2)
            {
                line = reader.ReadLine();
                if (!String.Equals(line, "l2"))
                {
                    level2 = false;
                    goto Skip;
                }
            }

            line = reader.ReadLine();
        Skip:
            parts = line.Split('\t');
            Debug.Assert((parts[0] == "t") && (parts.Length == 2));
            long envelopeTick = Int64.Parse(parts[1]);

            line = reader.ReadLine();
            parts = line.Split('\t');
            Debug.Assert((parts[0] == "b") && (parts.Length == 2));
            long basis = Int64.Parse(parts[1]);

            line = reader.ReadLine();
            parts = line.Split('\t');
            Debug.Assert((parts[0] == "fr") && (parts.Length == 3));
            long frameBase = Int64.Parse(parts[1]);
            int frameCount = Int32.Parse(parts[2]);

            line = reader.ReadLine();
            parts = line.Split('\t');
            Debug.Assert((parts[0] == "ph") && (parts.Length == 5));
            long phase0Start = Int64.Parse(parts[1]);
            long phase1Start = Int64.Parse(parts[2]);
            long phase2Start = Int64.Parse(parts[3]);
            long phase3Start = Int64.Parse(parts[4]);

            line = reader.ReadLine();
            if (!String.Equals(line, ":"))
            {
                Debug.Assert(false);
            }

            Datum[] data = Datum.ReadArray(reader, count, basis, level2);

            line = reader.ReadLine();
            Debug.Assert(String.IsNullOrEmpty(line));

            EpochResident epoch = new EpochResident(
                topProxy,
                envelopeTick,
                frameBase,
                frameCount,
                basis,
                phase0Start + basis,
                phase1Start + basis,
                phase2Start + basis,
                phase3Start + basis,
                level2,
                data);
            return epoch;
        }
    }
}
