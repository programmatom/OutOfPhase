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
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhase
{
    public static class ClipboardSerializers
    {
        public const string ClipboardPrefix = "OutOfPhase-646E5A24-0FAD-463d-BCB7-FF8C9EF137D2.";
    }

    [Serializable]
    public abstract class ClipboardBase : ISerializable
    {
        [NonSerialized]
        private Document document;

        [NonSerialized]
        private int version;
        [NonSerialized]
        private byte[] data;

        protected ClipboardBase(Document document)
        {
            this.document = document;
        }

        protected ClipboardBase(SerializationInfo info, StreamingContext context)
        {
            version = (int)info.GetValue("version", typeof(int));
            data = (byte[])info.GetValue("data", typeof(byte[]));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    Save(writer, document);
                }

                info.AddValue("version", Document.CurrentFormatVersionNumber);
                info.AddValue("length", stream.Length);
                info.AddValue("data", stream.ToArray(), typeof(byte[]));
            }
        }

        public object Reconstitute(Document document)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    object o = Load(reader, document);
                    reader.ReadEOF(); // ensure correctness
                    return o;
                }
            }
        }

        protected abstract void Save(BinaryWriter writer, Document document);

        protected abstract object Load(BinaryReader reader, Document document);
    }

    [Serializable]
    public class FunctionClipboard : ClipboardBase
    {
        public const string ClipboardIdentifer = ClipboardSerializers.ClipboardPrefix + "FunctionModule";

        [NonSerialized]
        private FunctionObjectRec function;

        public FunctionClipboard(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FunctionClipboard(FunctionObjectRec function, Document document)
            : base(document)
        {
            this.function = function;
        }

        protected override void Save(BinaryWriter writer, Document document)
        {
            function.Save(writer, new SaveContext(document));
        }

        protected override object Load(BinaryReader reader, Document document)
        {
            function = new FunctionObjectRec(reader, new LoadContext(Document.CurrentFormatVersionNumber, document, LoadContextState.Paste));
            return function;
        }
    }

    [Serializable]
    public class InstrumentClipboard : ClipboardBase
    {
        public const string ClipboardIdentifer = ClipboardSerializers.ClipboardPrefix + "Instrument";

        [NonSerialized]
        private InstrObjectRec instrument;

        public InstrumentClipboard(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public InstrumentClipboard(InstrObjectRec instrument, Document document)
            : base(document)
        {
            this.instrument = instrument;
        }

        protected override void Save(BinaryWriter writer, Document document)
        {
            instrument.Save(writer, new SaveContext(document));
        }

        protected override object Load(BinaryReader reader, Document document)
        {
            instrument = new InstrObjectRec(reader, new LoadContext(Document.CurrentFormatVersionNumber, document, LoadContextState.Paste));
            return instrument;
        }
    }

    [Serializable]
    public class TrackClipboard : ClipboardBase
    {
        public const string ClipboardIdentifer = ClipboardSerializers.ClipboardPrefix + "Track";

        [NonSerialized]
        private TrackObjectRec track;

        public TrackClipboard(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public TrackClipboard(TrackObjectRec track, Document document)
            : base(document)
        {
            this.track = track;
        }

        protected override void Save(BinaryWriter writer, Document document)
        {
            track.Save(writer, new SaveContext(document));
        }

        protected override object Load(BinaryReader reader, Document document)
        {
            track = new TrackObjectRec(reader, new LoadContext(Document.CurrentFormatVersionNumber, document, LoadContextState.Paste));
            return track;
        }
    }

    [Serializable]
    public class SampleClipboard : ClipboardBase
    {
        public const string ClipboardIdentifer = ClipboardSerializers.ClipboardPrefix + "Sample";

        [NonSerialized]
        private SampleObjectRec sample;

        public SampleClipboard(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public SampleClipboard(SampleObjectRec sample, Document document)
            : base(document)
        {
            this.sample = sample;
        }

        protected override void Save(BinaryWriter writer, Document document)
        {
            sample.Save(writer, new SaveContext(document));
        }

        protected override object Load(BinaryReader reader, Document document)
        {
            sample = new SampleObjectRec(reader, new LoadContext(Document.CurrentFormatVersionNumber, document, LoadContextState.Paste));
            return sample;
        }
    }

    [Serializable]
    public class WaveTableClipboard : ClipboardBase
    {
        public const string ClipboardIdentifer = ClipboardSerializers.ClipboardPrefix + "WaveTable";

        [NonSerialized]
        private WaveTableObjectRec waveTable;

        public WaveTableClipboard(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public WaveTableClipboard(WaveTableObjectRec waveTable, Document document)
            : base(document)
        {
            this.waveTable = waveTable;
        }

        protected override void Save(BinaryWriter writer, Document document)
        {
            waveTable.Save(writer, new SaveContext(document));
        }

        protected override object Load(BinaryReader reader, Document document)
        {
            waveTable = new WaveTableObjectRec(reader, new LoadContext(Document.CurrentFormatVersionNumber, document, LoadContextState.Paste));
            return waveTable;
        }
    }

    [Serializable]
    public class AlgoSampClipboard : ClipboardBase
    {
        public const string ClipboardIdentifer = ClipboardSerializers.ClipboardPrefix + "AlgoSamp";

        [NonSerialized]
        private AlgoSampObjectRec algoSamp;

        public AlgoSampClipboard(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AlgoSampClipboard(AlgoSampObjectRec algoSamp, Document document)
            : base(document)
        {
            this.algoSamp = algoSamp;
        }

        protected override void Save(BinaryWriter writer, Document document)
        {
            algoSamp.Save(writer, new SaveContext(document));
        }

        protected override object Load(BinaryReader reader, Document document)
        {
            algoSamp = new AlgoSampObjectRec(reader, new LoadContext(Document.CurrentFormatVersionNumber, document, LoadContextState.Paste));
            return algoSamp;
        }
    }

    [Serializable]
    public class AlgoWaveTableClipboard : ClipboardBase
    {
        public const string ClipboardIdentifer = ClipboardSerializers.ClipboardPrefix + "AlgoWaveTable";

        [NonSerialized]
        private AlgoWaveTableObjectRec algoWaveTable;

        public AlgoWaveTableClipboard(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AlgoWaveTableClipboard(AlgoWaveTableObjectRec algoWaveTable, Document document)
            : base(document)
        {
            this.algoWaveTable = algoWaveTable;
        }

        protected override void Save(BinaryWriter writer, Document document)
        {
            algoWaveTable.Save(writer, new SaveContext(document));
        }

        protected override object Load(BinaryReader reader, Document document)
        {
            algoWaveTable = new AlgoWaveTableObjectRec(reader, new LoadContext(Document.CurrentFormatVersionNumber, document, LoadContextState.Paste));
            return algoWaveTable;
        }
    }
}
